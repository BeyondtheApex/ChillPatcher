package api

import (
	"encoding/json"
	"fmt"
	"qqmusic_bridge/crypto"
	"qqmusic_bridge/models"
	"strings"
)

// GetSongURLFCG tries to get song URL using FCG API (2025.9 format)
func (c *Client) GetSongURLFCG(songMid string, quality models.AudioQuality) (*models.SongURL, error) {
	c.mu.RLock()
	cookies := c.cookies
	uin := c.uin
	c.mu.RUnlock()

	debugLog("[GetSongURLFCG] Getting URL for %s, quality=%s, uin=%d", songMid, quality, uin)

	// Build filename with quality prefix to request specific quality
	prefix := quality.GetFilePrefix()
	ext := quality.GetFileExt()
	filename := fmt.Sprintf("%s%s.%s", prefix, songMid, ext)
	debugLog("[GetSongURLFCG] Requesting filename: %s", filename)

	// Request with filename to specify quality
	reqData := map[string]interface{}{
		"req_1": map[string]interface{}{
			"module": "vkey.GetVkeyServer",
			"method": "CgiGetVkey",
			"param": map[string]interface{}{
				"filename":  []string{filename},
				"guid":      c.guid,
				"songmid":   []string{songMid},
				"songtype":  []int{0},
				"uin":       fmt.Sprintf("%d", uin),
				"loginflag": 1,
				"platform":  "20",
			},
		},
		"comm": map[string]interface{}{
			"format": "json",
			"uin":    uin,
			"ct":     24,
			"cv":     0,
		},
	}

	jsonData, err := json.Marshal(reqData)
	if err != nil {
		return nil, err
	}

	debugLog("[GetSongURLFCG] Request body: %s", string(jsonData))

	resp, err := c.httpClient.R().
		SetHeader("Cookie", cookies).
		SetHeader("Referer", "https://y.qq.com/").
		SetHeader("Origin", "https://y.qq.com").
		SetHeader("Content-Type", "application/json;charset=UTF-8").
		SetHeader("Accept", "application/json, text/plain, */*").
		SetHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8").
		SetBody(jsonData).
		Post("https://u.y.qq.com/cgi-bin/musicu.fcg")

	if err != nil {
		debugLog("[GetSongURLFCG] Request error: %v", err)
		return nil, err
	}

	debugLog("[GetSongURLFCG] Response: %s", string(resp.Body()[:min(500, len(resp.Body()))]))

	var result struct {
		Code int `json:"code"`
		Req1 struct {
			Code int `json:"code"`
			Data struct {
				Msg        string   `json:"msg"`
				Sip        []string `json:"sip"`
				Midurlinfo []struct {
					Purl     string `json:"purl"`
					Songmid  string `json:"songmid"`
					Filename string `json:"filename"`
				} `json:"midurlinfo"`
			} `json:"data"`
		} `json:"req_1"`
	}

	if err := json.Unmarshal(resp.Body(), &result); err != nil {
		debugLog("[GetSongURLFCG] Parse error: %v", err)
		return nil, fmt.Errorf("failed to parse response: %w", err)
	}

	if result.Req1.Code != 0 {
		debugLog("[GetSongURLFCG] Req1 error code: %d", result.Req1.Code)
		return nil, fmt.Errorf("FCG API error: %d", result.Req1.Code)
	}

	// Check if server indicates file not found (404) in message
	if strings.Contains(result.Req1.Data.Msg, "404") {
		debugLog("[GetSongURLFCG] Server indicates 404 in msg: %s", result.Req1.Data.Msg)
		return nil, fmt.Errorf("file not available (404)")
	}

	if len(result.Req1.Data.Midurlinfo) == 0 || result.Req1.Data.Midurlinfo[0].Purl == "" {
		debugLog("[GetSongURLFCG] No purl in response")
		return nil, fmt.Errorf("no URL available")
	}

	// Use sip server from response, fallback to default
	serverURL := "https://ws.stream.qqmusic.qq.com"
	if len(result.Req1.Data.Sip) > 0 && result.Req1.Data.Sip[0] != "" {
		serverURL = strings.TrimSuffix(result.Req1.Data.Sip[0], "/")
	}

	purl := result.Req1.Data.Midurlinfo[0].Purl
	fullURL := serverURL + "/" + purl
	debugLog("[GetSongURLFCG] Got URL: %s", fullURL)

	// Detect actual format from the returned URL
	actualExt := "m4a" // default
	if strings.Contains(purl, ".m4a") {
		actualExt = "m4a"
	} else if strings.Contains(purl, ".mp3") {
		actualExt = "mp3"
	} else if strings.Contains(purl, ".flac") {
		actualExt = "flac"
	}
	debugLog("[GetSongURLFCG] Detected format: %s", actualExt)

	return &models.SongURL{
		Mid:     songMid,
		URL:     fullURL,
		Quality: string(quality),
		Format:  actualExt,
	}, nil
}

// GetSongURL gets the streaming URL for a song
func (c *Client) GetSongURL(songMid string, quality models.AudioQuality) (*models.SongURL, error) {
	// Try FCG API first
	url, err := c.GetSongURLFCG(songMid, quality)
	if err == nil && url != nil && url.URL != "" {
		return url, nil
	}
	debugLog("[GetSongURL] FCG API failed: %v, trying CGI API", err)

	uin := c.GetUIN()
	guid := c.GetGUID()

	// Build filename based on quality
	prefix := quality.GetFilePrefix()
	ext := quality.GetFileExt()
	filename := fmt.Sprintf("%s%s.%s", prefix, songMid, ext)

	params := map[string]interface{}{
		"filename":     []string{filename},
		"guid":         guid,
		"songmid":      []string{songMid},
		"songtype":     []int{0},
		"uin":          fmt.Sprintf("%d", uin),
		"loginflag":    1,
		"platform":     "20",
	}

	data, err := c.RequestCGI("music.vkey.GetVkey", "GetVkey", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get song URL: %w", err)
	}

	var result struct {
		Sip      []string `json:"sip"`
		Midurlinfo []struct {
			Purl     string `json:"purl"`
			Songmid  string `json:"songmid"`
			Filename string `json:"filename"`
		} `json:"midurlinfo"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse song URL: %w", err)
	}

	if len(result.Midurlinfo) == 0 || result.Midurlinfo[0].Purl == "" {
		return nil, fmt.Errorf("no URL available for this quality, may need VIP")
	}

	// Get a server URL
	serverURL := StreamURL
	if len(result.Sip) > 0 && result.Sip[0] != "" {
		serverURL = strings.TrimSuffix(result.Sip[0], "/")
	}

	purl := result.Midurlinfo[0].Purl
	fullURL := serverURL + "/" + purl

	// Detect actual format from the returned URL (server may return different format)
	actualExt := ext
	if strings.Contains(purl, ".m4a") {
		actualExt = "m4a"
	} else if strings.Contains(purl, ".mp3") {
		actualExt = "mp3"
	} else if strings.Contains(purl, ".flac") {
		actualExt = "flac"
	}
	if actualExt != ext {
		debugLog("[GetSongURL] Format mismatch: requested %s, got %s", ext, actualExt)
	}

	return &models.SongURL{
		Mid:     songMid,
		URL:     fullURL,
		Quality: string(quality),
		Format:  actualExt,
	}, nil
}

// GetSongURLWithFallback tries to get song URL with quality fallback
// Strategy: VIP mode first (for VIP-only songs), then guest mode (for stability)
func (c *Client) GetSongURLWithFallback(songMid string, preferredQuality models.AudioQuality) (*models.SongURL, error) {
	debugLog("[GetSongURLWithFallback] Getting URL for %s (preferred: %s)", songMid, preferredQuality)

	// Step 1: Try VIP mode first (some songs require VIP even for 128k)
	url, err := c.GetSongURLAutoVIP(songMid)
	if err == nil && url.URL != "" {
		debugLog("[GetSongURLWithFallback] VIP mode succeeded")
		return url, nil
	}
	debugLog("[GetSongURLWithFallback] VIP mode failed: %v, trying guest mode...", err)

	// Step 2: Fallback to guest mode (more stable for free songs)
	url, err = c.GetSongURLAuto(songMid)
	if err == nil && url.URL != "" {
		debugLog("[GetSongURLWithFallback] Guest mode succeeded")
		return url, nil
	}

	return nil, fmt.Errorf("failed to get song URL (both VIP and guest mode failed): %v", err)
}

// GetSongURLAutoVIP gets song URL using VIP mode (real uin)
// Some songs require VIP even for 128k quality
// Does NOT specify filename - let server auto-select to avoid CDN 404
func (c *Client) GetSongURLAutoVIP(songMid string) (*models.SongURL, error) {
	c.mu.RLock()
	cookies := c.cookies
	uin := c.uin
	c.mu.RUnlock()

	debugLog("[GetSongURLAutoVIP] Getting URL for %s (VIP mode, uin=%d)", songMid, uin)

	reqData := map[string]interface{}{
		"req_1": map[string]interface{}{
			"module": "vkey.GetVkeyServer",
			"method": "CgiGetVkey",
			"param": map[string]interface{}{
				"guid":      c.guid,
				"songmid":   []string{songMid},
				"songtype":  []int{0},
				"uin":       fmt.Sprintf("%d", uin),
				"loginflag": 1,
				"platform":  "20",
			},
		},
		"comm": map[string]interface{}{
			"format": "json",
			"uin":    uin,
			"ct":     24,
			"cv":     0,
		},
	}

	jsonData, err := json.Marshal(reqData)
	if err != nil {
		return nil, err
	}

	debugLog("[GetSongURLAutoVIP] Request body: %s", string(jsonData))

	resp, err := c.httpClient.R().
		SetHeader("Cookie", cookies).
		SetHeader("Referer", "https://y.qq.com/").
		SetHeader("Origin", "https://y.qq.com").
		SetHeader("Content-Type", "application/json;charset=UTF-8").
		SetBody(jsonData).
		Post("https://u.y.qq.com/cgi-bin/musicu.fcg")

	if err != nil {
		return nil, err
	}

	debugLog("[GetSongURLAutoVIP] Response: %s", string(resp.Body()[:min(500, len(resp.Body()))]))

	var result struct {
		Code int `json:"code"`
		Req1 struct {
			Code int `json:"code"`
			Data struct {
				Msg        string   `json:"msg"`
				Sip        []string `json:"sip"`
				MidURLInfo []struct {
					Purl     string `json:"purl"`
					FileName string `json:"filename"`
				} `json:"midurlinfo"`
			} `json:"data"`
		} `json:"req_1"`
	}

	if err := json.Unmarshal(resp.Body(), &result); err != nil {
		return nil, err
	}

	if result.Code != 0 || result.Req1.Code != 0 {
		return nil, fmt.Errorf("API error: code=%d, req1.code=%d", result.Code, result.Req1.Code)
	}

	if len(result.Req1.Data.MidURLInfo) == 0 || result.Req1.Data.MidURLInfo[0].Purl == "" {
		debugLog("[GetSongURLAutoVIP] No purl in response")
		return nil, fmt.Errorf("no URL available (VIP mode)")
	}

	purl := result.Req1.Data.MidURLInfo[0].Purl

	// Detect format from purl
	ext := "m4a"
	if strings.Contains(purl, ".mp3") {
		ext = "mp3"
	} else if strings.Contains(purl, ".flac") {
		ext = "flac"
	}

	baseURL := "http://aqqmusic.tc.qq.com/"
	if len(result.Req1.Data.Sip) > 0 && result.Req1.Data.Sip[0] != "" {
		baseURL = result.Req1.Data.Sip[0]
	}

	fullURL := baseURL + purl
	debugLog("[GetSongURLAutoVIP] Got URL: %s", fullURL)
	debugLog("[GetSongURLAutoVIP] Detected format: %s", ext)

	return &models.SongURL{
		URL:     fullURL,
		Quality: "128",
		Format:  ext,
	}, nil
}

// GetSongURLAuto gets song URL without specifying quality (guest mode for stability)
func (c *Client) GetSongURLAuto(songMid string) (*models.SongURL, error) {
	c.mu.RLock()
	cookies := c.cookies
	c.mu.RUnlock()

	// Use guest mode (uin=0) for maximum stability
	// High quality downloads require APP-specific auth that we can't replicate
	uin := int64(0)
	debugLog("[GetSongURLAuto] Getting URL for %s (guest mode, uin=0)", songMid)

	// Request without filename - let server auto-select best available quality for this user
	reqData := map[string]interface{}{
		"req_1": map[string]interface{}{
			"module": "vkey.GetVkeyServer",
			"method": "CgiGetVkey",
			"param": map[string]interface{}{
				"guid":      c.guid,
				"songmid":   []string{songMid},
				"songtype":  []int{0},
				"uin":       fmt.Sprintf("%d", uin),
				"loginflag": 1,
				"platform":  "20",
			},
		},
		"comm": map[string]interface{}{
			"format": "json",
			"uin":    uin,
			"ct":     24,
			"cv":     0,
		},
	}

	jsonData, err := json.Marshal(reqData)
	if err != nil {
		return nil, err
	}

	debugLog("[GetSongURLAuto] Request body: %s", string(jsonData))

	resp, err := c.httpClient.R().
		SetHeader("Cookie", cookies).
		SetHeader("Referer", "https://y.qq.com/").
		SetHeader("Origin", "https://y.qq.com").
		SetHeader("Content-Type", "application/json;charset=UTF-8").
		SetBody(jsonData).
		Post("https://u.y.qq.com/cgi-bin/musicu.fcg")

	if err != nil {
		return nil, err
	}

	debugLog("[GetSongURLAuto] Response: %s", string(resp.Body()[:min(500, len(resp.Body()))]))

	var result struct {
		Code int `json:"code"`
		Req1 struct {
			Code int `json:"code"`
			Data struct {
				Msg        string   `json:"msg"`
				Sip        []string `json:"sip"`
				Midurlinfo []struct {
					Purl     string `json:"purl"`
					Songmid  string `json:"songmid"`
					Filename string `json:"filename"`
				} `json:"midurlinfo"`
			} `json:"data"`
		} `json:"req_1"`
	}

	if err := json.Unmarshal(resp.Body(), &result); err != nil {
		return nil, fmt.Errorf("failed to parse response: %w", err)
	}

	if result.Req1.Code != 0 {
		return nil, fmt.Errorf("FCG API error: %d", result.Req1.Code)
	}

	if len(result.Req1.Data.Midurlinfo) == 0 || result.Req1.Data.Midurlinfo[0].Purl == "" {
		debugLog("[GetSongURLAuto] No purl in response")
		return nil, fmt.Errorf("no URL available")
	}

	serverURL := "https://ws.stream.qqmusic.qq.com"
	if len(result.Req1.Data.Sip) > 0 && result.Req1.Data.Sip[0] != "" {
		serverURL = strings.TrimSuffix(result.Req1.Data.Sip[0], "/")
	}

	purl := result.Req1.Data.Midurlinfo[0].Purl
	fullURL := serverURL + "/" + purl
	debugLog("[GetSongURLAuto] Got URL: %s", fullURL)

	// Detect format from URL
	actualExt := "m4a"
	if strings.Contains(purl, ".m4a") {
		actualExt = "m4a"
	} else if strings.Contains(purl, ".mp3") {
		actualExt = "mp3"
	} else if strings.Contains(purl, ".flac") {
		actualExt = "flac"
	}
	debugLog("[GetSongURLAuto] Detected format: %s", actualExt)

	return &models.SongURL{
		Mid:     songMid,
		URL:     fullURL,
		Quality: "auto",
		Format:  actualExt,
	}, nil
}

// GetSongInfo gets detailed information about a song
func (c *Client) GetSongInfo(songMid string) (*models.SongInfo, error) {
	params := map[string]interface{}{
		"songMid": []string{songMid},
	}

	data, err := c.RequestCGI("music.trackInfo.UniformRuleCtrl", "GetTrackInfo", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get song info: %w", err)
	}

	var result struct {
		Tracks []struct {
			Mid      string `json:"mid"`
			Id       int64  `json:"id"`
			Name     string `json:"name"`
			Title    string `json:"title"`
			Interval int    `json:"interval"`
			Singer   []struct {
				Name string `json:"name"`
				Mid  string `json:"mid"`
			} `json:"singer"`
			Album struct {
				Name string `json:"name"`
				Mid  string `json:"mid"`
			} `json:"album"`
			File struct {
				MediaMid  string `json:"media_mid"`
				Size128   int64  `json:"size_128"`
				Size320   int64  `json:"size_320"`
				SizeFlac  int64  `json:"size_flac"`
				SizeHires int64  `json:"size_hires"`
			} `json:"file"`
		} `json:"tracks"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse song info: %w", err)
	}

	if len(result.Tracks) == 0 {
		return nil, fmt.Errorf("song not found")
	}

	track := result.Tracks[0]
	artists := make([]string, len(track.Singer))
	for i, singer := range track.Singer {
		artists[i] = singer.Name
	}

	name := track.Name
	if name == "" {
		name = track.Title
	}

	return &models.SongInfo{
		Mid:      track.Mid,
		ID:       track.Id,
		Name:     name,
		Duration: float64(track.Interval),
		Artists:  artists,
		Album:    track.Album.Name,
		AlbumMid: track.Album.Mid,
		CoverUrl: buildCoverUrl(track.Album.Mid),
		File: models.SongFile{
			MediaMid: track.File.MediaMid,
			Size128:  track.File.Size128,
			Size320:  track.File.Size320,
			SizeFlac: track.File.SizeFlac,
			SizeHRes: track.File.SizeHires,
		},
	}, nil
}

// GetSongInfoBatch gets info for multiple songs
func (c *Client) GetSongInfoBatch(songMids []string) ([]models.SongInfo, error) {
	if len(songMids) == 0 {
		return nil, nil
	}

	params := map[string]interface{}{
		"songMid": songMids,
	}

	data, err := c.RequestCGI("music.trackInfo.UniformRuleCtrl", "GetTrackInfo", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get song info batch: %w", err)
	}

	var result struct {
		Tracks []struct {
			Mid      string `json:"mid"`
			Id       int64  `json:"id"`
			Name     string `json:"name"`
			Title    string `json:"title"`
			Interval int    `json:"interval"`
			Singer   []struct {
				Name string `json:"name"`
				Mid  string `json:"mid"`
			} `json:"singer"`
			Album struct {
				Name string `json:"name"`
				Mid  string `json:"mid"`
			} `json:"album"`
			File struct {
				MediaMid  string `json:"media_mid"`
				Size128   int64  `json:"size_128"`
				Size320   int64  `json:"size_320"`
				SizeFlac  int64  `json:"size_flac"`
				SizeHires int64  `json:"size_hires"`
			} `json:"file"`
		} `json:"tracks"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse song info batch: %w", err)
	}

	var songs []models.SongInfo
	for _, track := range result.Tracks {
		artists := make([]string, len(track.Singer))
		for i, singer := range track.Singer {
			artists[i] = singer.Name
		}

		name := track.Name
		if name == "" {
			name = track.Title
		}

		songs = append(songs, models.SongInfo{
			Mid:      track.Mid,
			ID:       track.Id,
			Name:     name,
			Duration: float64(track.Interval),
			Artists:  artists,
			Album:    track.Album.Name,
			AlbumMid: track.Album.Mid,
			CoverUrl: buildCoverUrl(track.Album.Mid),
			File: models.SongFile{
				MediaMid: track.File.MediaMid,
				Size128:  track.File.Size128,
				Size320:  track.File.Size320,
				SizeFlac: track.File.SizeFlac,
				SizeHRes: track.File.SizeHires,
			},
		})
	}

	return songs, nil
}

// SearchSongs searches for songs by keyword
func (c *Client) SearchSongs(keyword string, page, pageSize int) ([]models.SongInfo, int, error) {
	if pageSize <= 0 {
		pageSize = 30
	}
	if page < 1 {
		page = 1
	}

	params := map[string]interface{}{
		"searchid": crypto.GenerateSearchID(),
		"query":    keyword,
		"page_num": page,
		"num_per_page": pageSize,
		"search_type":  0, // 0: songs
	}

	data, err := c.RequestCGI("music.search.SearchCgiService", "DoSearchForQQMusicDesktop", params)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to search songs: %w", err)
	}

	var result struct {
		Body struct {
			Song struct {
				TotalNum int `json:"totalnum"`
				List     []struct {
					Mid      string `json:"mid"`
					Id       int64  `json:"id"`
					Name     string `json:"name"`
					Interval int    `json:"interval"`
					Singer   []struct {
						Name string `json:"name"`
						Mid  string `json:"mid"`
					} `json:"singer"`
					Album struct {
						Name string `json:"name"`
						Mid  string `json:"mid"`
					} `json:"album"`
					File struct {
						MediaMid  string `json:"media_mid"`
						Size128   int64  `json:"size_128"`
						Size320   int64  `json:"size_320"`
						SizeFlac  int64  `json:"size_flac"`
						SizeHires int64  `json:"size_hires"`
					} `json:"file"`
				} `json:"list"`
			} `json:"song"`
		} `json:"body"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, 0, fmt.Errorf("failed to parse search results: %w", err)
	}

	var songs []models.SongInfo
	for _, track := range result.Body.Song.List {
		artists := make([]string, len(track.Singer))
		for i, singer := range track.Singer {
			artists[i] = singer.Name
		}

		songs = append(songs, models.SongInfo{
			Mid:      track.Mid,
			ID:       track.Id,
			Name:     track.Name,
			Duration: float64(track.Interval),
			Artists:  artists,
			Album:    track.Album.Name,
			AlbumMid: track.Album.Mid,
			CoverUrl: buildCoverUrl(track.Album.Mid),
			File: models.SongFile{
				MediaMid: track.File.MediaMid,
				Size128:  track.File.Size128,
				Size320:  track.File.Size320,
				SizeFlac: track.File.SizeFlac,
				SizeHRes: track.File.SizeHires,
			},
		})
	}

	return songs, result.Body.Song.TotalNum, nil
}

// GetSongLyric gets the lyrics for a song
func (c *Client) GetSongLyric(songMid string) (string, error) {
	c.mu.RLock()
	cookies := c.cookies
	c.mu.RUnlock()

	debugLog("[GetSongLyric] songMid=%s", songMid)

	// Use the traditional lyrics endpoint which is more reliable
	reqURL := "https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg"

	resp, err := c.httpClient.R().
		SetHeader("Referer", "https://y.qq.com/portal/player.html").
		SetHeader("Cookie", cookies).
		SetQueryParam("songmid", songMid).
		SetQueryParam("format", "json").
		SetQueryParam("nobase64", "0").
		Get(reqURL)

	if err != nil {
		return "", fmt.Errorf("failed to get lyrics: %w", err)
	}

	debugLog("[GetSongLyric] Response: %s", string(resp.Body()[:min(300, len(resp.Body()))]))

	var result struct {
		RetCode int    `json:"retcode"`
		Code    int    `json:"code"`
		Lyric   string `json:"lyric"`
		Trans   string `json:"trans"`
	}

	if err := json.Unmarshal(resp.Body(), &result); err != nil {
		return "", fmt.Errorf("failed to parse lyrics response: %w", err)
	}

	if result.RetCode != 0 && result.Code != 0 {
		// Fallback: try CGI method with corrected params
		debugLog("[GetSongLyric] Traditional endpoint failed (retcode=%d), trying CGI fallback", result.RetCode)
		params := map[string]interface{}{
			"songMID": songMid,
			"songID":  0,
		}
		data, err := c.RequestCGI("music.musichallSong.PlayLyricInfo", "GetPlayLyricInfo", params)
		if err != nil {
			return "", fmt.Errorf("failed to get lyrics (both methods): %w", err)
		}
		var cgiResult struct {
			Lyric string `json:"lyric"`
		}
		if err := json.Unmarshal(data, &cgiResult); err != nil {
			return "", fmt.Errorf("failed to parse CGI lyrics: %w", err)
		}
		return cgiResult.Lyric, nil
	}

	return result.Lyric, nil
}

// GetRecommendSongs gets daily recommended songs (similar to personal FM)
func (c *Client) GetRecommendSongs() ([]models.SongInfo, error) {
	params := map[string]interface{}{
		"id": 0,
	}

	data, err := c.RequestCGI("music.recommend.RecommendSongList", "get_daily_recommend_song", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get recommend songs: %w", err)
	}

	var result struct {
		Data struct {
			SongList []struct {
				Mid      string `json:"mid"`
				Id       int64  `json:"id"`
				Name     string `json:"name"`
				Interval int    `json:"interval"`
				Singer   []struct {
					Name string `json:"name"`
					Mid  string `json:"mid"`
				} `json:"singer"`
				Album struct {
					Name string `json:"name"`
					Mid  string `json:"mid"`
				} `json:"album"`
				File struct {
					MediaMid  string `json:"media_mid"`
					Size128   int64  `json:"size_128"`
					Size320   int64  `json:"size_320"`
					SizeFlac  int64  `json:"size_flac"`
					SizeHires int64  `json:"size_hires"`
				} `json:"file"`
			} `json:"songlist"`
		} `json:"data"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse recommend songs: %w", err)
	}

	var songs []models.SongInfo
	for _, track := range result.Data.SongList {
		artists := make([]string, len(track.Singer))
		for i, singer := range track.Singer {
			artists[i] = singer.Name
		}

		songs = append(songs, models.SongInfo{
			Mid:      track.Mid,
			ID:       track.Id,
			Name:     track.Name,
			Duration: float64(track.Interval),
			Artists:  artists,
			Album:    track.Album.Name,
			AlbumMid: track.Album.Mid,
			CoverUrl: buildCoverUrl(track.Album.Mid),
			File: models.SongFile{
				MediaMid: track.File.MediaMid,
				Size128:  track.File.Size128,
				Size320:  track.File.Size320,
				SizeFlac: track.File.SizeFlac,
				SizeHRes: track.File.SizeHires,
			},
		})
	}

	return songs, nil
}

// GetAvailableQuality returns the best available quality for a song
func (c *Client) GetAvailableQuality(song *models.SongInfo) models.AudioQuality {
	// Check from highest to lowest quality
	if song.File.SizeHRes > 0 {
		return models.QualityHiRes
	}
	if song.File.SizeFlac > 0 {
		return models.QualitySQ
	}
	if song.File.Size320 > 0 {
		return models.QualityHQ
	}
	return models.QualityStandard
}
