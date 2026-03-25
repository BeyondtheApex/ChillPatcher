package main

/*
#include <stdlib.h>
#include "callback.h"
*/
import "C"

import (
	"encoding/json"
	"fmt"
	"strings"
	"sync"
	"unsafe"

	"github.com/evanw/esbuild/pkg/api"
)

// BuildConfig mirrors the esbuild options passed from C#.
type BuildConfig struct {
	WorkingDir  string            `json:"workingDir"`
	EntryPoints []string          `json:"entryPoints"`
	Outfile     string            `json:"outfile"`
	Inject      []string          `json:"inject"`
	Alias       map[string]string `json:"alias"`
	External    []string          `json:"external"`
	Sourcemap   bool              `json:"sourcemap"`
	JsxFactory  string            `json:"jsxFactory"`
	JsxFragment string            `json:"jsxFragment"`
	Platform    string            `json:"platform"`
	Format      string            `json:"format"`
}

func buildOptions(cfg BuildConfig) api.BuildOptions {
	sm := api.SourceMapNone
	if cfg.Sourcemap {
		sm = api.SourceMapLinked
	}
	plat := api.PlatformNode
	if cfg.Platform == "browser" {
		plat = api.PlatformBrowser
	}
	factory := "h"
	if cfg.JsxFactory != "" {
		factory = cfg.JsxFactory
	}
	fragment := "Fragment"
	if cfg.JsxFragment != "" {
		fragment = cfg.JsxFragment
	}
	format := api.FormatDefault
	if cfg.Format == "iife" {
		format = api.FormatIIFE
	} else if cfg.Format == "esm" {
		format = api.FormatESModule
	} else if cfg.Format == "cjs" {
		format = api.FormatCommonJS
	}

	return api.BuildOptions{
		EntryPoints:   cfg.EntryPoints,
		Bundle:        true,
		Outfile:       cfg.Outfile,
		Inject:        cfg.Inject,
		Alias:         cfg.Alias,
		External:      cfg.External,
		Platform:      plat,
		Format:        format,
		Sourcemap:     sm,
		SourceRoot:    cfg.WorkingDir,
		AbsWorkingDir: cfg.WorkingDir,
		JSX:           api.JSXTransform,
		JSXFactory:    factory,
		JSXFragment:   fragment,
		Plugins:       []api.Plugin{importTransformPlugin()},
		Write:         true,
	}
}

func resultError(result api.BuildResult) string {
	if len(result.Errors) == 0 {
		return ""
	}
	var msgs []string
	for _, e := range result.Errors {
		loc := ""
		if e.Location != nil {
			loc = fmt.Sprintf("%s:%d:%d: ", e.Location.File, e.Location.Line, e.Location.Column)
		}
		msgs = append(msgs, loc+e.Text)
	}
	return strings.Join(msgs, "\n")
}

//export EsbuildBuild
func EsbuildBuild(configJson *C.char) *C.char {
	var cfg BuildConfig
	if err := json.Unmarshal([]byte(C.GoString(configJson)), &cfg); err != nil {
		return C.CString("config parse error: " + err.Error())
	}
	result := api.Build(buildOptions(cfg))
	errMsg := resultError(result)
	return C.CString(errMsg)
}

// Watch management
var (
	watchMu     sync.Mutex
	watches     = map[int]api.BuildContext{}
	nextWatchID = 1
)

//export EsbuildWatch
func EsbuildWatch(configJson *C.char) C.int {
	var cfg BuildConfig
	if err := json.Unmarshal([]byte(C.GoString(configJson)), &cfg); err != nil {
		return -1
	}
	ctx, ctxErr := api.Context(buildOptions(cfg))
	if ctxErr != nil {
		return -1
	}
	if err := ctx.Watch(api.WatchOptions{}); err != nil {
		ctx.Cancel()
		return -1
	}
	watchMu.Lock()
	id := nextWatchID
	nextWatchID++
	watches[id] = ctx
	watchMu.Unlock()
	return C.int(id)
}

//export EsbuildStop
func EsbuildStop(watchId C.int) {
	watchMu.Lock()
	ctx, ok := watches[int(watchId)]
	if ok {
		delete(watches, int(watchId))
	}
	watchMu.Unlock()
	if ok {
		ctx.Cancel()
	}
}

//export EsbuildStopAll
func EsbuildStopAll() {
	watchMu.Lock()
	all := watches
	watches = map[int]api.BuildContext{}
	watchMu.Unlock()
	for _, ctx := range all {
		ctx.Cancel()
	}
}

//export NpmInstallFromLock
func NpmInstallFromLock(workingDir *C.char) *C.char {
	err := npmInstall(C.GoString(workingDir))
	if err != nil {
		return C.CString(err.Error())
	}
	return C.CString("")
}

//export NpmInstallWithProgress
func NpmInstallWithProgress(workingDir *C.char, callback C.progress_callback) *C.char {
	var mu sync.Mutex
	progress := func(pkgPath, status, msg string) {
		cPkg := C.CString(pkgPath)
		cStatus := C.CString(status)
		cMsg := C.CString(msg)
		mu.Lock()
		C.callProgressCallback(callback, cPkg, cStatus, cMsg)
		mu.Unlock()
		C.free(unsafe.Pointer(cPkg))
		C.free(unsafe.Pointer(cStatus))
		C.free(unsafe.Pointer(cMsg))
	}
	err := npmInstallWithProgress(C.GoString(workingDir), progress)
	if err != nil {
		return C.CString(err.Error())
	}
	return C.CString("")
}

//export FreeString
func FreeString(ptr *C.char) {
	C.free(unsafe.Pointer(ptr))
}

func main() {}
