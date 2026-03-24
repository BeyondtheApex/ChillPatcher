package main

import (
	"archive/tar"
	"bytes"
	"compress/gzip"
	"crypto/sha512"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"sync"
	"time"
)

// ProgressCallback is called for each package during npm install.
// status: "skip", "download", "done", "error"
type ProgressCallback func(pkgPath string, status string, msg string)

// Lockfile represents package-lock.json v3 format.
type Lockfile struct {
	LockfileVersion int                `json:"lockfileVersion"`
	Packages        map[string]LockPkg `json:"packages"`
}

// LockPkg represents a single package entry in the lockfile.
type LockPkg struct {
	Version   string   `json:"version"`
	Resolved  string   `json:"resolved"`
	Integrity string   `json:"integrity"`
	Optional  bool     `json:"optional"`
	Dev       bool     `json:"dev"`
	OS        []string `json:"os"`
	CPU       []string `json:"cpu"`
}

// SimplePackageJSON just reads the version from an installed package.
type SimplePackageJSON struct {
	Version string `json:"version"`
}

// downloadTask represents a single package to download.
type downloadTask struct {
	pkgPath   string
	pkg       LockPkg
	absPath   string
}

const (
	httpTimeout       = 30 * time.Second
	maxConcurrentDL   = 4
	maxRetries        = 2
	retryDelay        = 2 * time.Second
)

var httpClient = &http.Client{
	Timeout: httpTimeout,
}

func npmInstall(workingDir string) error {
	return npmInstallWithProgress(workingDir, nil)
}

func npmInstallWithProgress(workingDir string, progress ProgressCallback) error {
	lockPath := filepath.Join(workingDir, "package-lock.json")
	data, err := os.ReadFile(lockPath)
	if err != nil {
		return fmt.Errorf("cannot read package-lock.json: %w", err)
	}

	var lock Lockfile
	if err := json.Unmarshal(data, &lock); err != nil {
		return fmt.Errorf("cannot parse package-lock.json: %w", err)
	}

	if lock.LockfileVersion < 2 {
		return fmt.Errorf("unsupported lockfile version %d (need >=2)", lock.LockfileVersion)
	}

	goOS := mapNpmOS(runtime.GOOS)
	goArch := mapNpmArch(runtime.GOARCH)

	// Collect tasks
	var tasks []downloadTask
	for pkgPath, pkg := range lock.Packages {
		if pkgPath == "" {
			continue // root entry
		}
		if !matchesPlatform(pkg, goOS, goArch) {
			if progress != nil {
				progress(pkgPath, "skip", "platform mismatch")
			}
			continue
		}

		absPath := filepath.Join(workingDir, filepath.FromSlash(pkgPath))

		if isInstalled(absPath, pkg.Version) {
			if progress != nil {
				progress(pkgPath, "skip", "already installed")
			}
			continue
		}

		if pkg.Resolved == "" {
			if progress != nil {
				progress(pkgPath, "skip", "no resolved URL")
			}
			continue
		}

		tasks = append(tasks, downloadTask{pkgPath: pkgPath, pkg: pkg, absPath: absPath})
	}

	if len(tasks) == 0 {
		return nil
	}

	if progress != nil {
		progress("", "download", fmt.Sprintf("downloading %d packages (concurrency=%d)", len(tasks), maxConcurrentDL))
	}

	// Concurrent download with worker pool
	var (
		mu     sync.Mutex
		errors []string
		wg     sync.WaitGroup
		sem    = make(chan struct{}, maxConcurrentDL)
	)

	for _, task := range tasks {
		wg.Add(1)
		sem <- struct{}{} // acquire semaphore
		go func(t downloadTask) {
			defer wg.Done()
			defer func() { <-sem }() // release semaphore

			if progress != nil {
				progress(t.pkgPath, "download", t.pkg.Version)
			}

			var lastErr error
			for attempt := 0; attempt <= maxRetries; attempt++ {
				if attempt > 0 {
					if progress != nil {
						progress(t.pkgPath, "download", fmt.Sprintf("retry %d/%d: %s", attempt, maxRetries, t.pkg.Version))
					}
					time.Sleep(retryDelay)
				}
				lastErr = downloadAndExtract(t.pkg.Resolved, t.absPath, t.pkg.Integrity)
				if lastErr == nil {
					break
				}
			}

			if lastErr != nil {
				mu.Lock()
				errors = append(errors, fmt.Sprintf("%s: %v", t.pkgPath, lastErr))
				mu.Unlock()
				if progress != nil {
					progress(t.pkgPath, "error", lastErr.Error())
				}
			} else {
				if progress != nil {
					progress(t.pkgPath, "done", t.pkg.Version)
				}
			}
		}(task)
	}

	wg.Wait()

	if len(errors) > 0 {
		return fmt.Errorf("npm install errors:\n%s", strings.Join(errors, "\n"))
	}
	return nil
}

func matchesPlatform(pkg LockPkg, goOS, goArch string) bool {
	if pkg.Optional {
		// Optional deps must match platform
		if len(pkg.OS) > 0 && !contains(pkg.OS, goOS) {
			return false
		}
		if len(pkg.CPU) > 0 && !contains(pkg.CPU, goArch) {
			return false
		}
	}
	return true
}

func contains(arr []string, val string) bool {
	for _, v := range arr {
		if v == val {
			return true
		}
	}
	return false
}

func isInstalled(absPath, expectedVersion string) bool {
	pkgJsonPath := filepath.Join(absPath, "package.json")
	data, err := os.ReadFile(pkgJsonPath)
	if err != nil {
		return false
	}
	var pkg SimplePackageJSON
	if json.Unmarshal(data, &pkg) != nil {
		return false
	}
	return pkg.Version == expectedVersion
}

func mapNpmOS(goos string) string {
	switch goos {
	case "windows":
		return "win32"
	case "darwin":
		return "darwin"
	default:
		return goos
	}
}

func mapNpmArch(goarch string) string {
	switch goarch {
	case "amd64":
		return "x64"
	case "386":
		return "ia32"
	case "arm64":
		return "arm64"
	default:
		return goarch
	}
}

// verifyIntegrity checks the integrity hash (e.g., "sha512-<base64>") against the data.
func verifyIntegrity(data []byte, integrity string) error {
	parts := strings.SplitN(integrity, "-", 2)
	if len(parts) != 2 {
		return fmt.Errorf("invalid integrity format: %s", integrity)
	}
	algo, expected := parts[0], parts[1]

	switch algo {
	case "sha512":
		h := sha512.Sum512(data)
		actual := base64.StdEncoding.EncodeToString(h[:])
		if actual != expected {
			return fmt.Errorf("integrity mismatch (sha512): expected %s, got %s", expected, actual)
		}
	default:
		// Unknown algorithm — skip verification but don't fail
		return nil
	}
	return nil
}

func downloadAndExtract(url, destDir string, integrity string) error {
	resp, err := httpClient.Get(url)
	if err != nil {
		return fmt.Errorf("download failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return fmt.Errorf("download returned status %d", resp.StatusCode)
	}

	// Read entire body for integrity check (npm tarballs are typically small)
	// Limit to 64MB to prevent abuse
	bodyBytes, err := io.ReadAll(io.LimitReader(resp.Body, 64<<20))
	if err != nil {
		return fmt.Errorf("read body failed: %w", err)
	}

	// Verify integrity (sha512-<base64>) if provided
	if integrity != "" {
		if err := verifyIntegrity(bodyBytes, integrity); err != nil {
			return err
		}
	}

	// Decompress from memory
	gz, err := gzip.NewReader(bytes.NewReader(bodyBytes))
	if err != nil {
		return fmt.Errorf("gzip error: %w", err)
	}
	defer gz.Close()

	tr := tar.NewReader(gz)

	// Remove existing directory
	os.RemoveAll(destDir)

	for {
		hdr, err := tr.Next()
		if err == io.EOF {
			break
		}
		if err != nil {
			return fmt.Errorf("tar error: %w", err)
		}

		// npm tarballs have a "package/" prefix — strip it
		name := hdr.Name
		if idx := strings.Index(name, "/"); idx >= 0 {
			name = name[idx+1:]
		}
		if name == "" || name == "." {
			continue
		}

		target := filepath.Join(destDir, filepath.FromSlash(name))

		// Security: prevent path traversal
		if !strings.HasPrefix(filepath.Clean(target), filepath.Clean(destDir)) {
			continue
		}

		switch hdr.Typeflag {
		case tar.TypeDir:
			os.MkdirAll(target, 0755)
		case tar.TypeReg:
			os.MkdirAll(filepath.Dir(target), 0755)
			f, err := os.Create(target)
			if err != nil {
				return fmt.Errorf("create file %s: %w", name, err)
			}
			// Limit copy to prevent zip bomb (256MB per file)
			_, copyErr := io.Copy(f, io.LimitReader(tr, 256<<20))
			f.Close()
			if copyErr != nil {
				return fmt.Errorf("write file %s: %w", name, copyErr)
			}
		}
	}
	return nil
}
