package stream

import (
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// debugLog writes debug message to a file
func debugLog(format string, args ...interface{}) {
	f, err := os.OpenFile("qqmusic_debug.log", os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0644)
	if err != nil {
		return
	}
	defer f.Close()
	msg := fmt.Sprintf(format, args...)
	f.WriteString(time.Now().Format("15:04:05") + " " + msg + "\n")
}

const (
	// Cache settings
	CacheSubDir    = "audio_cache"
	MaxCacheSize   = 5 * 1024 * 1024 * 1024 // 5GB
	CacheExpiry    = 7 * 24 * time.Hour     // 7 days
	ChunkSize      = 64 * 1024              // 64KB chunks
	BufferMinSize  = 256 * 1024             // 256KB minimum buffer before playback
)

// CacheManager manages audio file caching
type CacheManager struct {
	cacheDir   string
	httpClient *http.Client
	cookies    string // Cookie string for authenticated downloads
	mu         sync.RWMutex
	downloads  map[string]*downloadTask
}

// downloadTask represents an ongoing download
type downloadTask struct {
	url         string
	filePath    string
	tempPath    string
	cookies     string // Cookie for this download
	totalSize   int64
	downloaded  int64
	complete    bool
	err         error
	mu          sync.RWMutex
	stopChan    chan struct{}
	file        *os.File
	subscribers []chan struct{}
}

// NewCacheManager creates a new cache manager
func NewCacheManager(dataDir string, httpClient *http.Client) (*CacheManager, error) {
	cacheDir := filepath.Join(dataDir, CacheSubDir)
	if err := os.MkdirAll(cacheDir, 0755); err != nil {
		return nil, fmt.Errorf("failed to create cache directory: %w", err)
	}

	cm := &CacheManager{
		cacheDir:   cacheDir,
		httpClient: httpClient,
		downloads:  make(map[string]*downloadTask),
	}

	// Clean old cache files
	go cm.cleanOldCache()

	return cm, nil
}

// GetCacheDir returns the cache directory path
func (cm *CacheManager) GetCacheDir() string {
	return cm.cacheDir
}

// SetCookies sets the cookies for authenticated downloads
func (cm *CacheManager) SetCookies(cookies string) {
	cm.mu.Lock()
	defer cm.mu.Unlock()
	cm.cookies = cookies
}

// GetCachedFile returns the path to a cached file if it exists
func (cm *CacheManager) GetCachedFile(songMid string, quality string) string {
	filename := fmt.Sprintf("%s_%s", songMid, quality)
	// Check both regular files and .tmp files (rename may have failed if file was in use)
	for _, ext := range []string{".mp3", ".flac", ".m4a"} {
		// Check regular file first
		path := filepath.Join(cm.cacheDir, filename+ext)
		if _, err := os.Stat(path); err == nil {
			return path
		}
		// Check .tmp file (valid if download completed but rename failed)
		tmpPath := path + ".tmp"
		if info, err := os.Stat(tmpPath); err == nil && info.Size() > 0 {
			return tmpPath
		}
	}
	return ""
}

// IsCached checks if a song is fully cached
func (cm *CacheManager) IsCached(songMid string, quality string) bool {
	return cm.GetCachedFile(songMid, quality) != ""
}

// StartDownload starts downloading a file
func (cm *CacheManager) StartDownload(songMid, quality, url, format string) (*downloadTask, error) {
	key := fmt.Sprintf("%s_%s", songMid, quality)

	cm.mu.Lock()
	if existing, ok := cm.downloads[key]; ok {
		cm.mu.Unlock()
		return existing, nil
	}

	filename := fmt.Sprintf("%s_%s.%s", songMid, quality, format)
	filePath := filepath.Join(cm.cacheDir, filename)
	tempPath := filePath + ".tmp"

	task := &downloadTask{
		url:         url,
		filePath:    filePath,
		tempPath:    tempPath,
		cookies:     cm.cookies, // Pass cookies to download task
		stopChan:    make(chan struct{}),
		subscribers: make([]chan struct{}, 0),
	}

	cm.downloads[key] = task
	cm.mu.Unlock()

	go cm.download(task, key)

	return task, nil
}

// download performs the actual download
func (cm *CacheManager) download(task *downloadTask, key string) {
	debugLog("[download] Starting download for key=%s, url=%s", key, task.url)

	defer func() {
		cm.mu.Lock()
		delete(cm.downloads, key)
		cm.mu.Unlock()
	}()

	// Create request
	req, err := http.NewRequest("GET", task.url, nil)
	if err != nil {
		debugLog("[download] Failed to create request: %v", err)
		task.setError(err)
		return
	}

	req.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
	req.Header.Set("Referer", "https://y.qq.com/")
	req.Header.Set("Origin", "https://y.qq.com")
	if task.cookies != "" {
		req.Header.Set("Cookie", task.cookies)
		debugLog("[download] Added Cookie header (len=%d)", len(task.cookies))
	}

	debugLog("[download] Sending HTTP request...")
	resp, err := cm.httpClient.Do(req)
	if err != nil {
		debugLog("[download] HTTP request failed: %v", err)
		task.setError(err)
		return
	}
	defer resp.Body.Close()

	debugLog("[download] Response: status=%d, contentLength=%d", resp.StatusCode, resp.ContentLength)

	if resp.StatusCode != http.StatusOK {
		debugLog("[download] HTTP error: %d", resp.StatusCode)
		task.setError(fmt.Errorf("HTTP error: %d", resp.StatusCode))
		return
	}

	task.totalSize = resp.ContentLength

	// Create temp file
	debugLog("[download] Creating temp file: %s", task.tempPath)
	file, err := os.Create(task.tempPath)
	if err != nil {
		debugLog("[download] Failed to create temp file: %v", err)
		task.setError(err)
		return
	}
	task.file = file

	// Download in chunks
	buffer := make([]byte, ChunkSize)
	lastLogTime := time.Now()
	for {
		select {
		case <-task.stopChan:
			debugLog("[download] Download cancelled")
			file.Close()
			os.Remove(task.tempPath)
			task.setError(fmt.Errorf("download cancelled"))
			return
		default:
		}

		n, err := resp.Body.Read(buffer)
		if n > 0 {
			if _, writeErr := file.Write(buffer[:n]); writeErr != nil {
				debugLog("[download] Write error: %v", writeErr)
				file.Close()
				os.Remove(task.tempPath)
				task.setError(writeErr)
				return
			}

			task.mu.Lock()
			task.downloaded += int64(n)
			downloaded := task.downloaded
			// Notify subscribers
			for _, ch := range task.subscribers {
				select {
				case ch <- struct{}{}:
				default:
				}
			}
			task.mu.Unlock()

			// Log progress every 2 seconds
			if time.Since(lastLogTime) > 2*time.Second {
				debugLog("[download] Progress: %d / %d bytes (%.1f%%)", downloaded, task.totalSize, float64(downloaded)/float64(task.totalSize)*100)
				lastLogTime = time.Now()
			}
		}

		if err == io.EOF {
			debugLog("[download] Download complete: %d bytes", task.downloaded)
			break
		}
		if err != nil {
			debugLog("[download] Read error: %v", err)
			file.Close()
			os.Remove(task.tempPath)
			task.setError(err)
			return
		}
	}

	file.Close()

	// Rename temp file to final path
	debugLog("[download] Renaming temp file to: %s", task.filePath)
	if err := os.Rename(task.tempPath, task.filePath); err != nil {
		// Rename may fail if file is in use by decoder - this is OK
		// The temp file is still valid and can be used for playback
		debugLog("[download] Rename error (file may be in use, keeping .tmp): %v", err)
		// Don't remove temp file or set error - it's still usable
	}

	task.mu.Lock()
	task.complete = true
	// Notify all subscribers of completion
	for _, ch := range task.subscribers {
		select {
		case ch <- struct{}{}:
		default:
		}
	}
	task.mu.Unlock()
}

// GetDownloadTask returns an existing download task
func (cm *CacheManager) GetDownloadTask(songMid, quality string) *downloadTask {
	key := fmt.Sprintf("%s_%s", songMid, quality)
	cm.mu.RLock()
	defer cm.mu.RUnlock()
	return cm.downloads[key]
}

// CancelDownload cancels an ongoing download
func (cm *CacheManager) CancelDownload(songMid, quality string) {
	key := fmt.Sprintf("%s_%s", songMid, quality)
	cm.mu.Lock()
	task, ok := cm.downloads[key]
	cm.mu.Unlock()

	if ok {
		close(task.stopChan)
	}
}

// cleanOldCache removes old cache files
func (cm *CacheManager) cleanOldCache() {
	entries, err := os.ReadDir(cm.cacheDir)
	if err != nil {
		return
	}

	var totalSize int64
	type fileInfo struct {
		path    string
		modTime time.Time
		size    int64
	}
	var files []fileInfo

	now := time.Now()
	for _, entry := range entries {
		if entry.IsDir() {
			continue
		}
		// Handle .tmp files - only remove if very old (stale downloads)
		if filepath.Ext(entry.Name()) == ".tmp" {
			info, err := entry.Info()
			if err != nil {
				continue
			}
			// Only remove .tmp files older than 1 hour (stale incomplete downloads)
			if now.Sub(info.ModTime()) > time.Hour {
				os.Remove(filepath.Join(cm.cacheDir, entry.Name()))
			}
			continue
		}

		info, err := entry.Info()
		if err != nil {
			continue
		}

		path := filepath.Join(cm.cacheDir, entry.Name())
		modTime := info.ModTime()
		size := info.Size()

		// Remove files older than expiry
		if now.Sub(modTime) > CacheExpiry {
			os.Remove(path)
			continue
		}

		files = append(files, fileInfo{path, modTime, size})
		totalSize += size
	}

	// If over max size, remove oldest files
	if totalSize > MaxCacheSize {
		// Sort by modification time (oldest first)
		for i := 0; i < len(files)-1; i++ {
			for j := i + 1; j < len(files); j++ {
				if files[i].modTime.After(files[j].modTime) {
					files[i], files[j] = files[j], files[i]
				}
			}
		}

		// Remove oldest files until under limit
		for _, f := range files {
			if totalSize <= MaxCacheSize*8/10 { // Keep 80% capacity
				break
			}
			os.Remove(f.path)
			totalSize -= f.size
		}
	}
}

// ClearCache clears all cached files
func (cm *CacheManager) ClearCache() error {
	entries, err := os.ReadDir(cm.cacheDir)
	if err != nil {
		return err
	}

	for _, entry := range entries {
		if !entry.IsDir() {
			os.Remove(filepath.Join(cm.cacheDir, entry.Name()))
		}
	}

	return nil
}

// downloadTask methods

func (t *downloadTask) setError(err error) {
	t.mu.Lock()
	t.err = err
	t.mu.Unlock()
}

// GetProgress returns the download progress (0.0 - 1.0)
func (t *downloadTask) GetProgress() float64 {
	t.mu.RLock()
	defer t.mu.RUnlock()

	if t.totalSize <= 0 {
		return 0
	}
	return float64(t.downloaded) / float64(t.totalSize)
}

// GetDownloaded returns the number of bytes downloaded
func (t *downloadTask) GetDownloaded() int64 {
	t.mu.RLock()
	defer t.mu.RUnlock()
	return t.downloaded
}

// GetTotalSize returns the total file size
func (t *downloadTask) GetTotalSize() int64 {
	t.mu.RLock()
	defer t.mu.RUnlock()
	return t.totalSize
}

// IsComplete returns whether the download is complete
func (t *downloadTask) IsComplete() bool {
	t.mu.RLock()
	defer t.mu.RUnlock()
	return t.complete
}

// GetError returns any error that occurred
func (t *downloadTask) GetError() error {
	t.mu.RLock()
	defer t.mu.RUnlock()
	return t.err
}

// GetFilePath returns the final file path
func (t *downloadTask) GetFilePath() string {
	return t.filePath
}

// GetTempPath returns the temp file path
func (t *downloadTask) GetTempPath() string {
	return t.tempPath
}

// Subscribe returns a channel that receives notifications on progress updates
func (t *downloadTask) Subscribe() chan struct{} {
	t.mu.Lock()
	defer t.mu.Unlock()
	ch := make(chan struct{}, 1)
	t.subscribers = append(t.subscribers, ch)
	return ch
}

// WaitForMinBuffer waits until minimum buffer is available or download completes
func (t *downloadTask) WaitForMinBuffer(timeout time.Duration) bool {
	debugLog("[WaitForMinBuffer] Starting wait, timeout=%v, BufferMinSize=%d", timeout, BufferMinSize)
	deadline := time.Now().Add(timeout)
	ch := t.Subscribe()

	checkCount := 0
	for {
		t.mu.RLock()
		downloaded := t.downloaded
		complete := t.complete
		err := t.err
		t.mu.RUnlock()

		checkCount++
		if checkCount%10 == 1 { // Log every 10 checks
			debugLog("[WaitForMinBuffer] Check %d: downloaded=%d, complete=%v, err=%v", checkCount, downloaded, complete, err)
		}

		if err != nil {
			debugLog("[WaitForMinBuffer] Error detected: %v", err)
			return false
		}
		if complete || downloaded >= BufferMinSize {
			debugLog("[WaitForMinBuffer] Success: downloaded=%d, complete=%v", downloaded, complete)
			return true
		}

		remaining := time.Until(deadline)
		if remaining <= 0 {
			debugLog("[WaitForMinBuffer] Timeout! downloaded=%d", downloaded)
			return false
		}

		select {
		case <-ch:
			// Progress update, check again
		case <-time.After(remaining):
			debugLog("[WaitForMinBuffer] Timeout (no signal)! downloaded=%d", downloaded)
			return false
		}
	}
}

// OpenForReading opens the file for reading (works with temp file too)
func (t *downloadTask) OpenForReading() (*os.File, error) {
	t.mu.RLock()
	complete := t.complete
	t.mu.RUnlock()

	if complete {
		return os.Open(t.filePath)
	}
	return os.Open(t.tempPath)
}
