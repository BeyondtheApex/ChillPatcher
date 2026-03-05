package main

/*
#include <stdlib.h>
#include <stdint.h>
*/
import "C"
import (
	"encoding/json"
	"fmt"
	"qqmusic_bridge/api"
	"qqmusic_bridge/models"
	"qqmusic_bridge/stream"
	"sync"
	"unsafe"
)

var (
	client        *api.Client
	cacheManager  *stream.CacheManager
	streamManager *stream.StreamManager
	lastError     string
	mu            sync.RWMutex
)

func main() {}

// Helper functions

func setError(err error) {
	mu.Lock()
	if err != nil {
		lastError = err.Error()
	} else {
		lastError = ""
	}
	mu.Unlock()
}

func toJSON(v interface{}) string {
	data, err := json.Marshal(v)
	if err != nil {
		return "{\"error\": \"json marshal failed\"}"
	}
	return string(data)
}

// ==================== Initialization ====================

//export QQMusicInit
func QQMusicInit(dataDir *C.char) C.int {
	mu.Lock()
	defer mu.Unlock()

	dir := C.GoString(dataDir)

	var err error
	client, err = api.NewClient(dir)
	if err != nil {
		setError(err)
		return -1
	}

	cacheManager, err = stream.NewCacheManager(dir, client.GetHTTPClient())
	if err != nil {
		setError(err)
		return -2
	}

	// Sync cookies to cache manager if client has cookies loaded
	if cookies := client.GetCookies(); cookies != "" {
		cacheManager.SetCookies(cookies)
	}

	streamManager = stream.NewStreamManager(cacheManager)

	return 0
}

//export QQMusicIsLoggedIn
func QQMusicIsLoggedIn() C.int {
	mu.RLock()
	defer mu.RUnlock()

	if client == nil {
		return 0
	}
	if client.IsLoggedIn() {
		return 1
	}
	return 0
}

//export QQMusicGetUserInfo
func QQMusicGetUserInfo() *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	info, err := c.FetchUserInfo()
	if err != nil {
		setError(err)
		return nil
	}

	return C.CString(toJSON(info))
}

//export QQMusicSetCookie
func QQMusicSetCookie(cookie *C.char) C.int {
	mu.Lock()
	c := client
	cm := cacheManager
	mu.Unlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return -1
	}

	cookieStr := C.GoString(cookie)
	err := c.SetCookies(cookieStr)
	if err != nil {
		setError(err)
		return -1
	}

	// Also set cookies for the cache manager (for authenticated downloads)
	if cm != nil {
		cm.SetCookies(cookieStr)
	}

	return 0
}

//export QQMusicRefreshLogin
func QQMusicRefreshLogin() C.int {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return -1
	}

	err := c.RefreshLogin()
	if err != nil {
		setError(err)
		return -1
	}
	return 0
}

//export QQMusicLogout
func QQMusicLogout() C.int {
	mu.Lock()
	c := client
	mu.Unlock()

	if c == nil {
		return 0
	}

	c.ClearCookies()
	return 0
}

// ==================== Songs & Playlists ====================

//export QQMusicGetLikeSongs
func QQMusicGetLikeSongs(getAll C.int) *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	songs, err := c.GetLikeSongs(getAll != 0)
	if err != nil {
		setError(err)
		return nil
	}

	return C.CString(toJSON(songs))
}

//export QQMusicGetUserPlaylists
func QQMusicGetUserPlaylists() *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	created, err := c.GetUserCreatedPlaylists()
	if err != nil {
		setError(err)
		return nil
	}

	collected, _ := c.GetUserCollectedPlaylists()

	result := struct {
		Created   []models.PlaylistInfo `json:"created"`
		Collected []models.PlaylistInfo `json:"collected"`
	}{
		Created:   created,
		Collected: collected,
	}

	return C.CString(toJSON(result))
}

//export QQMusicGetPlaylistSongs
func QQMusicGetPlaylistSongs(playlistId C.longlong) *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	detail, err := c.GetPlaylistDetail(int64(playlistId))
	if err != nil {
		setError(err)
		return nil
	}

	return C.CString(toJSON(detail))
}

//export QQMusicGetSongURL
func QQMusicGetSongURL(songMid *C.char, quality *C.char) *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	q := models.AudioQuality(C.GoString(quality))
	if q == "" {
		q = models.QualityHQ // Default to 320kbps
	}

	url, err := c.GetSongURLWithFallback(C.GoString(songMid), q)
	if err != nil {
		setError(err)
		return nil
	}

	return C.CString(toJSON(url))
}

//export QQMusicGetSongInfo
func QQMusicGetSongInfo(songMid *C.char) *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	info, err := c.GetSongInfo(C.GoString(songMid))
	if err != nil {
		setError(err)
		return nil
	}

	return C.CString(toJSON(info))
}

//export QQMusicLikeSong
func QQMusicLikeSong(songMid *C.char, like C.int) C.int {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return -1
	}

	err := c.LikeSong(C.GoString(songMid), like != 0)
	if err != nil {
		setError(err)
		return -1
	}
	return 0
}

//export QQMusicSearchSongs
func QQMusicSearchSongs(keyword *C.char, page, pageSize C.int) *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	songs, total, err := c.SearchSongs(C.GoString(keyword), int(page), int(pageSize))
	if err != nil {
		setError(err)
		return nil
	}

	result := struct {
		Songs []models.SongInfo `json:"songs"`
		Total int               `json:"total"`
	}{
		Songs: songs,
		Total: total,
	}

	return C.CString(toJSON(result))
}

//export QQMusicGetRecommendSongs
func QQMusicGetRecommendSongs() *C.char {
	mu.RLock()
	c := client
	mu.RUnlock()

	if c == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	songs, err := c.GetRecommendSongs()
	if err != nil {
		setError(err)
		return nil
	}

	return C.CString(toJSON(songs))
}

// ==================== PCM Streaming ====================

//export QQMusicCreatePcmStream
func QQMusicCreatePcmStream(songMid *C.char, quality *C.char, duration C.double) C.longlong {
	mu.RLock()
	c := client
	sm := streamManager
	mu.RUnlock()

	if c == nil || sm == nil {
		setError(fmt.Errorf("not initialized"))
		return -1
	}

	mid := C.GoString(songMid)
	q := models.AudioQuality(C.GoString(quality))
	if q == "" {
		q = models.QualityHQ
	}

	// Get song URL
	url, err := c.GetSongURLWithFallback(mid, q)
	if err != nil {
		setError(err)
		return -2
	}

	// Create stream
	streamID, err := sm.CreateStream(mid, string(q), url.URL, url.Format, float64(duration))
	if err != nil {
		setError(err)
		return -3
	}

	return C.longlong(streamID)
}

//export QQMusicGetPcmStreamInfo
func QQMusicGetPcmStreamInfo(streamId C.longlong) *C.char {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		setError(fmt.Errorf("not initialized"))
		return nil
	}

	s := sm.GetStream(int64(streamId))
	if s == nil {
		setError(fmt.Errorf("stream not found"))
		return nil
	}

	info := s.GetInfo()
	return C.CString(toJSON(info))
}

//export QQMusicReadPcmFrames
func QQMusicReadPcmFrames(streamId C.longlong, buffer unsafe.Pointer, framesToRead C.int) C.int {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return -1
	}

	s := sm.GetStream(int64(streamId))
	if s == nil {
		return -1
	}

	// Convert buffer to Go slice
	info := s.GetInfo()
	samplesNeeded := int(framesToRead) * info.Channels
	samples := (*[1 << 30]float32)(buffer)[:samplesNeeded:samplesNeeded]

	return C.int(s.ReadFrames(samples, int(framesToRead)))
}

//export QQMusicSeekPcmStream
func QQMusicSeekPcmStream(streamId C.longlong, frameIndex C.longlong) C.int {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return -1
	}

	s := sm.GetStream(int64(streamId))
	if s == nil {
		return -1
	}

	if s.Seek(uint64(frameIndex)) {
		return 0
	}
	return -1
}

//export QQMusicClosePcmStream
func QQMusicClosePcmStream(streamId C.longlong) {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm != nil {
		sm.CloseStream(int64(streamId))
	}
}

//export QQMusicIsPcmStreamReady
func QQMusicIsPcmStreamReady(streamId C.longlong) C.int {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return 0
	}

	s := sm.GetStream(int64(streamId))
	if s == nil || !s.IsReady() {
		return 0
	}
	return 1
}

//export QQMusicGetCacheProgress
func QQMusicGetCacheProgress(streamId C.longlong) C.double {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return -1
	}

	s := sm.GetStream(int64(streamId))
	if s == nil {
		return -1
	}

	return C.double(s.GetCacheProgress())
}

//export QQMusicGetPcmStreamCurrentFrame
func QQMusicGetPcmStreamCurrentFrame(streamId C.longlong) C.ulonglong {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return 0
	}

	s := sm.GetStream(int64(streamId))
	if s == nil {
		return 0
	}

	return C.ulonglong(s.GetCurrentFrame())
}

//export QQMusicHasPendingSeek
func QQMusicHasPendingSeek(streamId C.longlong) C.int {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return 0
	}

	s := sm.GetStream(int64(streamId))
	if s == nil || !s.HasPendingSeek() {
		return 0
	}
	return 1
}

//export QQMusicGetPendingSeekFrame
func QQMusicGetPendingSeekFrame(streamId C.longlong) C.longlong {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return -1
	}

	s := sm.GetStream(int64(streamId))
	if s == nil {
		return -1
	}

	return C.longlong(s.GetPendingSeek())
}

//export QQMusicCancelPendingSeek
func QQMusicCancelPendingSeek(streamId C.longlong) {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return
	}

	s := sm.GetStream(int64(streamId))
	if s != nil {
		s.CancelPendingSeek()
	}
}

//export QQMusicIsCacheComplete
func QQMusicIsCacheComplete(streamId C.longlong) C.int {
	mu.RLock()
	sm := streamManager
	mu.RUnlock()

	if sm == nil {
		return 0
	}

	s := sm.GetStream(int64(streamId))
	if s == nil || !s.IsCacheComplete() {
		return 0
	}
	return 1
}

// ==================== Utility ====================

//export QQMusicFreeString
func QQMusicFreeString(ptr *C.char) {
	C.free(unsafe.Pointer(ptr))
}

//export QQMusicFreeBytes
func QQMusicFreeBytes(ptr *C.char) {
	C.free(unsafe.Pointer(ptr))
}

//export QQMusicGetLastError
func QQMusicGetLastError() *C.char {
	mu.RLock()
	err := lastError
	mu.RUnlock()

	if err == "" {
		return nil
	}
	return C.CString(err)
}

//export QQMusicClearCache
func QQMusicClearCache() C.int {
	mu.RLock()
	cm := cacheManager
	mu.RUnlock()

	if cm == nil {
		return -1
	}

	err := cm.ClearCache()
	if err != nil {
		setError(err)
		return -1
	}
	return 0
}

//export QQMusicGetCacheDir
func QQMusicGetCacheDir() *C.char {
	mu.RLock()
	cm := cacheManager
	mu.RUnlock()

	if cm == nil {
		return nil
	}

	return C.CString(cm.GetCacheDir())
}
