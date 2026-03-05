package api

import (
	"encoding/json"
	"fmt"
	"qqmusic_bridge/models"
)

// GetPlaylistDetailFCG gets playlist detail using public FCG API (no auth required for public playlists)
func (c *Client) GetPlaylistDetailFCG(dissID int64) (*models.PlaylistDetail, error) {
	c.mu.RLock()
	cookies := c.cookies
	gtk := c.gtk
	c.mu.RUnlock()

	debugLog("[GetPlaylistDetailFCG] Fetching playlist %d with gtk=%d", dissID, gtk)

	fcgURL := fmt.Sprintf("https://c.y.qq.com/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg?type=1&json=1&utf8=1&onlysong=0&new_format=1&disstid=%d&g_tk=%d&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0",
		dissID, gtk)

	resp, err := c.httpClient.R().
		SetHeader("Cookie", cookies).
		SetHeader("Referer", "https://y.qq.com/n/ryqq/playlist/"+fmt.Sprintf("%d", dissID)).
		SetHeader("Origin", "https://y.qq.com").
		Get(fcgURL)

	if err != nil {
		debugLog("[GetPlaylistDetailFCG] Request error: %v", err)
		return nil, err
	}

	debugLog("[GetPlaylistDetailFCG] Response: %s", string(resp.Body()[:min(1000, len(resp.Body()))]))

	var fcgResult struct {
		Code   int `json:"code"`
		Cdlist []struct {
			DissID   string `json:"disstid"`
			DissName string `json:"dissname"`
			Logo     string `json:"logo"`
			SongNum  int    `json:"songnum"`
			SongList []struct {
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
			} `json:"songlist"`
		} `json:"cdlist"`
	}

	if err := json.Unmarshal(resp.Body(), &fcgResult); err != nil {
		debugLog("[GetPlaylistDetailFCG] Parse error: %v", err)
		return nil, fmt.Errorf("failed to parse response: %w", err)
	}

	if fcgResult.Code != 0 {
		debugLog("[GetPlaylistDetailFCG] API error code: %d", fcgResult.Code)
		return nil, fmt.Errorf("FCG API error code: %d", fcgResult.Code)
	}

	if len(fcgResult.Cdlist) == 0 {
		return nil, fmt.Errorf("playlist not found")
	}

	cd := fcgResult.Cdlist[0]
	var songs []models.SongInfo
	for _, track := range cd.SongList {
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
		})
	}

	debugLog("[GetPlaylistDetailFCG] Got %d songs from playlist %s", len(songs), cd.DissName)

	// Parse DissID from string
	var parsedDissID int64
	fmt.Sscanf(cd.DissID, "%d", &parsedDissID)

	return &models.PlaylistDetail{
		DissID:    parsedDissID,
		DissName:  cd.DissName,
		SongCount: cd.SongNum,
		CoverUrl:  cd.Logo,
		Songs:     songs,
	}, nil
}

// GetPlaylistDetail gets detailed information about a playlist including songs
func (c *Client) GetPlaylistDetail(dissID int64) (*models.PlaylistDetail, error) {
	// Try FCG API first (works for public playlists)
	detail, err := c.GetPlaylistDetailFCG(dissID)
	if err == nil && detail != nil && len(detail.Songs) > 0 {
		return detail, nil
	}
	debugLog("[GetPlaylistDetail] FCG API failed, trying CGI API: %v", err)

	// Fall back to CGI API
	params := map[string]interface{}{
		"disstid":   dissID,
		"song_num":  500,
		"song_begin": 0,
		"onlysonglist": 0,
	}

	data, err := c.RequestCGI("music.srfDissInfo.DissInfo", "CgiGetDiss", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get playlist detail: %w", err)
	}

	var result struct {
		DirInfo struct {
			ID       int64  `json:"id"`
			Title    string `json:"title"`
			PicUrl   string `json:"picurl"`
			SongNum  int    `json:"songnum"`
			VisitNum int    `json:"visitnum"`
		} `json:"dirinfo"`
		SongList []struct {
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
		} `json:"songlist"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse playlist detail: %w", err)
	}

	var songs []models.SongInfo
	for _, track := range result.SongList {
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

	return &models.PlaylistDetail{
		DissID:    result.DirInfo.ID,
		DissName:  result.DirInfo.Title,
		SongCount: result.DirInfo.SongNum,
		CoverUrl:  result.DirInfo.PicUrl,
		Songs:     songs,
	}, nil
}

// GetPlaylistSongs gets songs from a playlist with pagination
func (c *Client) GetPlaylistSongs(dissID int64, offset, limit int) ([]models.SongInfo, int, error) {
	if limit <= 0 {
		limit = 100
	}

	params := map[string]interface{}{
		"disstid":    dissID,
		"song_num":   limit,
		"song_begin": offset,
		"onlysonglist": 1,
	}

	data, err := c.RequestCGI("music.srfDissInfo.DissInfo", "CgiGetDiss", params)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to get playlist songs: %w", err)
	}

	var result struct {
		DirInfo struct {
			SongNum int `json:"songnum"`
		} `json:"dirinfo"`
		SongList []struct {
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
		} `json:"songlist"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, 0, fmt.Errorf("failed to parse playlist songs: %w", err)
	}

	var songs []models.SongInfo
	for _, track := range result.SongList {
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

	return songs, result.DirInfo.SongNum, nil
}

// GetAllPlaylistSongs gets all songs from a playlist
func (c *Client) GetAllPlaylistSongs(dissID int64) ([]models.SongInfo, error) {
	var allSongs []models.SongInfo
	offset := 0
	pageSize := 100

	for {
		songs, total, err := c.GetPlaylistSongs(dissID, offset, pageSize)
		if err != nil {
			if len(allSongs) > 0 {
				return allSongs, nil
			}
			return nil, err
		}

		allSongs = append(allSongs, songs...)

		if len(songs) < pageSize || len(allSongs) >= total {
			break
		}

		offset += pageSize
	}

	return allSongs, nil
}

// SearchPlaylists searches for playlists by keyword
func (c *Client) SearchPlaylists(keyword string, page, pageSize int) ([]models.PlaylistInfo, int, error) {
	if pageSize <= 0 {
		pageSize = 30
	}
	if page < 1 {
		page = 1
	}

	params := map[string]interface{}{
		"query":        keyword,
		"page_num":     page,
		"num_per_page": pageSize,
		"search_type":  3, // 3: playlists
	}

	data, err := c.RequestCGI("music.search.SearchCgiService", "DoSearchForQQMusicDesktop", params)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to search playlists: %w", err)
	}

	var result struct {
		Body struct {
			Gedaan struct {
				TotalNum int `json:"totalnum"`
				List     []struct {
					DissID   int64  `json:"dissid"`
					DissName string `json:"dissname"`
					SongNum  int    `json:"song_count"`
					ImgUrl   string `json:"imgurl"`
					Creator  struct {
						Name string `json:"name"`
					} `json:"creator"`
					Intro string `json:"intro"`
				} `json:"list"`
			} `json:"gedaan"`
		} `json:"body"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, 0, fmt.Errorf("failed to parse search results: %w", err)
	}

	var playlists []models.PlaylistInfo
	for _, item := range result.Body.Gedaan.List {
		playlists = append(playlists, models.PlaylistInfo{
			DissID:      item.DissID,
			DissName:    item.DissName,
			SongCount:   item.SongNum,
			CoverUrl:    item.ImgUrl,
			Creator:     item.Creator.Name,
			Description: item.Intro,
		})
	}

	return playlists, result.Body.Gedaan.TotalNum, nil
}

// GetHotPlaylists gets hot/recommended playlists
func (c *Client) GetHotPlaylists(categoryID int, offset, limit int) ([]models.PlaylistInfo, error) {
	if limit <= 0 {
		limit = 30
	}

	params := map[string]interface{}{
		"categoryId": categoryID,
		"sin":        offset,
		"ein":        offset + limit - 1,
		"sortId":     5, // by hot
	}

	data, err := c.RequestCGI("music.playlist.PlaylistSquare", "GetPlaylistByCategory", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get hot playlists: %w", err)
	}

	var result struct {
		Data struct {
			List []struct {
				Basic struct {
					DissID   int64  `json:"dissid"`
					Title    string `json:"title"`
					SongCnt  int    `json:"song_cnt"`
					CoverUrl string `json:"cover_url_small"`
					Creator  struct {
						Name string `json:"name"`
					} `json:"creator"`
				} `json:"basic"`
			} `json:"v_playlist"`
		} `json:"data"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse hot playlists: %w", err)
	}

	var playlists []models.PlaylistInfo
	for _, item := range result.Data.List {
		playlists = append(playlists, models.PlaylistInfo{
			DissID:    item.Basic.DissID,
			DissName:  item.Basic.Title,
			SongCount: item.Basic.SongCnt,
			CoverUrl:  item.Basic.CoverUrl,
			Creator:   item.Basic.Creator.Name,
		})
	}

	return playlists, nil
}

// CreatePlaylist creates a new playlist
func (c *Client) CreatePlaylist(name string) (int64, error) {
	uin := c.GetUIN()
	if uin == 0 {
		return 0, fmt.Errorf("not logged in")
	}

	params := map[string]interface{}{
		"dirname": name,
		"uin":     uin,
	}

	data, err := c.RequestCGI("music.playlist.PlayListMng", "CreatePlaylist", params)
	if err != nil {
		return 0, fmt.Errorf("failed to create playlist: %w", err)
	}

	var result struct {
		DissID int64 `json:"dirid"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return 0, fmt.Errorf("failed to parse result: %w", err)
	}

	return result.DissID, nil
}

// AddSongsToPlaylist adds songs to a playlist
func (c *Client) AddSongsToPlaylist(dissID int64, songMids []string) error {
	if len(songMids) == 0 {
		return nil
	}

	params := map[string]interface{}{
		"dirid":    dissID,
		"song_mid": songMids,
	}

	_, err := c.RequestCGI("music.playlist.PlayListMng", "AddSongToPlaylist", params)
	if err != nil {
		return fmt.Errorf("failed to add songs to playlist: %w", err)
	}

	return nil
}

// RemoveSongsFromPlaylist removes songs from a playlist
func (c *Client) RemoveSongsFromPlaylist(dissID int64, songMids []string) error {
	if len(songMids) == 0 {
		return nil
	}

	params := map[string]interface{}{
		"dirid":    dissID,
		"song_mid": songMids,
	}

	_, err := c.RequestCGI("music.playlist.PlayListMng", "DelSongFromPlaylist", params)
	if err != nil {
		return fmt.Errorf("failed to remove songs from playlist: %w", err)
	}

	return nil
}

// DeletePlaylist deletes a playlist
func (c *Client) DeletePlaylist(dissID int64) error {
	params := map[string]interface{}{
		"dirid": dissID,
	}

	_, err := c.RequestCGI("music.playlist.PlayListMng", "DeletePlaylist", params)
	if err != nil {
		return fmt.Errorf("failed to delete playlist: %w", err)
	}

	return nil
}

// GetAlbumDetail gets album information and songs
func (c *Client) GetAlbumDetail(albumMid string) (*models.PlaylistDetail, error) {
	params := map[string]interface{}{
		"albumMid": albumMid,
	}

	data, err := c.RequestCGI("music.musichallAlbum.AlbumInfoServer", "GetAlbumDetail", params)
	if err != nil {
		return nil, fmt.Errorf("failed to get album detail: %w", err)
	}

	var result struct {
		BasicInfo struct {
			AlbumMid   string `json:"albumMid"`
			AlbumName  string `json:"albumName"`
			SingerName string `json:"singerName"`
			PicUrl     string `json:"headPicUrl"`
		} `json:"basicInfo"`
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
		} `json:"songList"`
	}

	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse album detail: %w", err)
	}

	var songs []models.SongInfo
	for _, track := range result.SongList {
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

	return &models.PlaylistDetail{
		DissName:  result.BasicInfo.AlbumName,
		SongCount: len(songs),
		CoverUrl:  result.BasicInfo.PicUrl,
		Songs:     songs,
	}, nil
}
