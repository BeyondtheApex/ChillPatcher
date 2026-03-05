package api

import (
	"encoding/json"
	"fmt"
	"qqmusic_bridge/models"
)

// FetchUserInfo fetches the current user's information from API
func (c *Client) FetchUserInfo() (*models.UserInfo, error) {
	// Check cache first
	if cached := c.GetUserInfo(); cached != nil {
		return cached, nil
	}

	params := map[string]interface{}{
		"ct": 24,
	}

	data, err := c.RequestCGI("music.UnifiedSearch.Profile", "GetUserProfile", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get user info: %w", err)
	}

	var result struct {
		Creator struct {
			Uin      int64  `json:"uin"`
			Nick     string `json:"nick"`
			HeadPic  string `json:"headpic"`
			EncUin   string `json:"encrypt_uin"`
		} `json:"creator"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse user info: %w", err)
	}

	userInfo := &models.UserInfo{
		UIN:       result.Creator.Uin,
		Nickname:  result.Creator.Nick,
		AvatarUrl: result.Creator.HeadPic,
	}

	// If UIN is 0, try to get from the client's stored UIN
	if userInfo.UIN == 0 {
		userInfo.UIN = c.GetUIN()
	}

	// Cache the result
	c.SetUserInfo(userInfo)

	return userInfo, nil
}

// GetUserVipInfo fetches the user's VIP status
func (c *Client) GetUserVipInfo() (int, error) {
	params := map[string]interface{}{
		"uin": c.GetUIN(),
	}

	data, err := c.RequestCGI("music.vip.VipUserInfo", "GetVipUserInfo", params)
	if err != nil {
		return 0, fmt.Errorf("failed to get VIP info: %w", err)
	}

	var result struct {
		VipInfo struct {
			VipType int `json:"vip_type"`
		} `json:"vip_info"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return 0, fmt.Errorf("failed to parse VIP info: %w", err)
	}

	return result.VipInfo.VipType, nil
}

// GetLikeSongs gets the user's favorite songs (like list)
func (c *Client) GetLikeSongs(getAll bool) ([]models.SongInfo, error) {
	uin := c.GetUIN()
	if uin == 0 {
		return nil, fmt.Errorf("not logged in")
	}

	// Try the old FCG API first which might work with QQ cookies
	songs, err := c.getLikeSongsFCG(uin, getAll)
	if err == nil && len(songs) > 0 {
		return songs, nil
	}

	// Fall back to CGI API
	var allSongs []models.SongInfo
	pageSize := 100
	pageNum := 0

	for {
		params := map[string]interface{}{
			"uin":       uin,
			"num":       pageSize,
			"start":     pageNum * pageSize,
			"order":     0, // 0: newest first
			"type":      1, // 1: songs
		}

		data, err := c.RequestCGI("music.musicBox.PlayList", "GetPlayListDetail", params)
		if err != nil {
			if len(allSongs) > 0 {
				return allSongs, nil // Return what we have
			}
			return nil, fmt.Errorf("failed to get like songs: %w", err)
		}

		var result struct {
			Total    int `json:"total"`
			SongList []struct {
				SongMid  string `json:"mid"`
				SongId   int64  `json:"id"`
				SongName string `json:"name"`
				Interval int    `json:"interval"` // duration in seconds
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
		}

		if err := json.Unmarshal(data, &result); err != nil {
			return nil, fmt.Errorf("failed to parse like songs: %w", err)
		}

		for _, song := range result.SongList {
			artists := make([]string, len(song.Singer))
			for i, singer := range song.Singer {
				artists[i] = singer.Name
			}

			songInfo := models.SongInfo{
				Mid:      song.SongMid,
				ID:       song.SongId,
				Name:     song.SongName,
				Duration: float64(song.Interval),
				Artists:  artists,
				Album:    song.Album.Name,
				AlbumMid: song.Album.Mid,
				CoverUrl: buildCoverUrl(song.Album.Mid),
				File: models.SongFile{
					MediaMid: song.File.MediaMid,
					Size128:  song.File.Size128,
					Size320:  song.File.Size320,
					SizeFlac: song.File.SizeFlac,
					SizeHRes: song.File.SizeHires,
				},
			}
			allSongs = append(allSongs, songInfo)
		}

		// Check if we have all songs
		if !getAll || len(result.SongList) < pageSize || len(allSongs) >= result.Total {
			break
		}

		pageNum++
	}

	return allSongs, nil
}

// LikeSong adds or removes a song from the like list
func (c *Client) LikeSong(songMid string, like bool) error {
	uin := c.GetUIN()
	if uin == 0 {
		return fmt.Errorf("not logged in")
	}

	var module, method string
	if like {
		module = "music.musicBox.PlayList"
		method = "AddToPlayList"
	} else {
		module = "music.musicBox.PlayList"
		method = "DelFromPlayList"
	}

	params := map[string]interface{}{
		"uin":      uin,
		"mid_list": []string{songMid},
	}

	_, err := c.RequestCGI(module, method, params)
	if err != nil {
		return fmt.Errorf("failed to update like status: %w", err)
	}

	return nil
}

// CheckLoginValid checks if the current login is still valid
func (c *Client) CheckLoginValid() bool {
	if !c.IsLoggedIn() {
		return false
	}

	// Try to get user info as a validation check
	_, err := c.FetchUserInfo()
	return err == nil
}

// RefreshLogin attempts to refresh the login session
func (c *Client) RefreshLogin() error {
	// For QQ Music, we typically need to re-login if the session expires
	// This is a placeholder - actual implementation would depend on
	// the specific refresh mechanism available
	if !c.IsLoggedIn() {
		return fmt.Errorf("not logged in")
	}

	// Verify the current session is still valid
	if !c.CheckLoginValid() {
		c.ClearCookies()
		return fmt.Errorf("session expired, please re-login")
	}

	return nil
}

// buildCoverUrl builds a cover URL from album mid
func buildCoverUrl(albumMid string) string {
	if albumMid == "" {
		return ""
	}
	return fmt.Sprintf("https://y.gtimg.cn/music/photo_new/T002R300x300M000%s.jpg", albumMid)
}

// getLikeSongsFCG tries to get like songs using the old FCG API
func (c *Client) getLikeSongsFCG(uin int64, getAll bool) ([]models.SongInfo, error) {
	c.mu.RLock()
	cookies := c.cookies
	gtk := c.gtk
	c.mu.RUnlock()

	debugLog("[getLikeSongsFCG] Trying FCG API with uin=%d, gtk=%d", uin, gtk)

	// Use the old c.y.qq.com FCG API
	fcgURL := fmt.Sprintf("https://c.y.qq.com/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg?type=1&json=1&utf8=1&onlysong=0&new_format=1&disstid=%d&g_tk=%d&loginUin=%d&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0",
		uin, gtk, uin)

	resp, err := c.httpClient.R().
		SetHeader("Cookie", cookies).
		SetHeader("Referer", "https://y.qq.com/n/ryqq/profile").
		SetHeader("Origin", "https://y.qq.com").
		Get(fcgURL)

	if err != nil {
		debugLog("[getLikeSongsFCG] Request error: %v", err)
		return nil, err
	}

	debugLog("[getLikeSongsFCG] Response: %s", string(resp.Body()[:min(500, len(resp.Body()))]))

	var altResult struct {
		Code   int `json:"code"`
		Cdlist []struct {
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
			} `json:"songlist"`
		} `json:"cdlist"`
	}

	if err := json.Unmarshal(resp.Body(), &altResult); err != nil {
		debugLog("[getLikeSongsFCG] Parse error: %v", err)
		return nil, err
	}

	if altResult.Code != 0 {
		debugLog("[getLikeSongsFCG] API error code: %d", altResult.Code)
		return nil, fmt.Errorf("FCG API error: %d", altResult.Code)
	}

	var songs []models.SongInfo
	if len(altResult.Cdlist) > 0 {
		for _, song := range altResult.Cdlist[0].SongList {
			artists := make([]string, len(song.Singer))
			for i, singer := range song.Singer {
				artists[i] = singer.Name
			}
			songs = append(songs, models.SongInfo{
				Mid:      song.Mid,
				ID:       song.Id,
				Name:     song.Name,
				Duration: float64(song.Interval),
				Artists:  artists,
				Album:    song.Album.Name,
				AlbumMid: song.Album.Mid,
				CoverUrl: buildCoverUrl(song.Album.Mid),
			})
		}
	}

	debugLog("[getLikeSongsFCG] Got %d songs", len(songs))
	return songs, nil
}

// GetUserCreatedPlaylists gets playlists created by the user
func (c *Client) GetUserCreatedPlaylists() ([]models.PlaylistInfo, error) {
	uin := c.GetUIN()
	if uin == 0 {
		return nil, fmt.Errorf("not logged in")
	}

	params := map[string]interface{}{
		"uin":          uin,
		"size":         200,
		"offset":       0,
		"sort":         5, // by update time
	}

	data, err := c.RequestCGI("music.srfDissInfo.aiDissInfo", "get_uin_diss", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get playlists: %w", err)
	}

	var result struct {
		DissList []struct {
			TID      int64  `json:"tid"`
			DirName  string `json:"diss_name"`
			SongCnt  int    `json:"song_cnt"`
			DirPic   string `json:"diss_cover"`
			Creator  struct {
				Name string `json:"name"`
			} `json:"creator"`
		} `json:"disslist"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse playlists: %w", err)
	}

	var playlists []models.PlaylistInfo
	for _, diss := range result.DissList {
		playlists = append(playlists, models.PlaylistInfo{
			DissID:    diss.TID,
			DissName:  diss.DirName,
			SongCount: diss.SongCnt,
			CoverUrl:  diss.DirPic,
			Creator:   diss.Creator.Name,
		})
	}

	return playlists, nil
}

// GetUserCollectedPlaylists gets playlists collected by the user
func (c *Client) GetUserCollectedPlaylists() ([]models.PlaylistInfo, error) {
	uin := c.GetUIN()
	if uin == 0 {
		return nil, fmt.Errorf("not logged in")
	}

	params := map[string]interface{}{
		"uin":    uin,
		"size":   200,
		"offset": 0,
	}

	data, err := c.RequestCGI("music.srfDissInfo.aiDissInfo", "get_collect_diss", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get collected playlists: %w", err)
	}

	var result struct {
		DissList []struct {
			TID      int64  `json:"tid"`
			DirName  string `json:"diss_name"`
			SongCnt  int    `json:"song_cnt"`
			DirPic   string `json:"diss_cover"`
			Creator  struct {
				Name string `json:"name"`
			} `json:"creator"`
		} `json:"disslist"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse playlists: %w", err)
	}

	var playlists []models.PlaylistInfo
	for _, diss := range result.DissList {
		playlists = append(playlists, models.PlaylistInfo{
			DissID:    diss.TID,
			DissName:  diss.DirName,
			SongCount: diss.SongCnt,
			CoverUrl:  diss.DirPic,
			Creator:   diss.Creator.Name,
		})
	}

	return playlists, nil
}
