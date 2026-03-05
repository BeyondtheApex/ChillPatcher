package models

// UserInfo represents QQ Music user information
type UserInfo struct {
	UIN       int64  `json:"uin"`
	Nickname  string `json:"nickname"`
	AvatarUrl string `json:"avatarUrl"`
	VipType   int    `json:"vipType"` // 0=normal, 11=green diamond, etc.
}

// SongInfo represents a song from QQ Music
type SongInfo struct {
	Mid      string   `json:"mid"`      // Song MID (unique identifier)
	ID       int64    `json:"id"`       // Song ID
	Name     string   `json:"name"`     // Song title
	Duration float64  `json:"duration"` // Duration in seconds
	Artists  []string `json:"artists"`  // Artist names
	Album    string   `json:"album"`    // Album name
	AlbumMid string   `json:"albumMid"` // Album MID
	CoverUrl string   `json:"coverUrl"` // Album cover URL
	File     SongFile `json:"file"`     // File information
}

// SongFile contains file information for different qualities
type SongFile struct {
	MediaMid string `json:"mediaMid"`
	Size128  int64  `json:"size128"`  // 128kbps MP3 size
	Size320  int64  `json:"size320"`  // 320kbps MP3 size
	SizeFlac int64  `json:"sizeFlac"` // FLAC size
	SizeHRes int64  `json:"sizeHRes"` // Hi-Res size
}

// PlaylistInfo represents a playlist
type PlaylistInfo struct {
	DissID      int64  `json:"dissId"`      // Playlist ID
	DissName    string `json:"dissName"`    // Playlist name
	SongCount   int    `json:"songCount"`   // Number of songs
	CoverUrl    string `json:"coverUrl"`    // Cover image URL
	Creator     string `json:"creator"`     // Creator name
	Description string `json:"description"` // Playlist description
}

// PlaylistDetail contains detailed playlist information including songs
type PlaylistDetail struct {
	DissID    int64      `json:"dissId"`
	DissName  string     `json:"dissName"`
	SongCount int        `json:"songCount"`
	CoverUrl  string     `json:"coverUrl"`
	Songs     []SongInfo `json:"songs"`
}

// SongURL contains the streaming URL for a song
type SongURL struct {
	Mid     string `json:"mid"`
	URL     string `json:"url"`
	Quality string `json:"quality"` // "128", "320", "flac", "hires"
	Format  string `json:"format"`  // "mp3", "flac", "m4a"
	Size    int64  `json:"size"`
}

// AudioQuality represents audio quality levels
type AudioQuality string

const (
	QualityStandard AudioQuality = "128"   // M500 - 128kbps MP3
	QualityHQ       AudioQuality = "320"   // M800 - 320kbps MP3
	QualitySQ       AudioQuality = "flac"  // F000 - FLAC lossless
	QualityHiRes    AudioQuality = "hires" // RS01 - Hi-Res
)

// GetFilePrefix returns the QQ Music file prefix for the quality
func (q AudioQuality) GetFilePrefix() string {
	switch q {
	case QualityStandard:
		return "C400" // 128kbps M4A (free, no VIP required)
	case QualityHQ:
		return "M800" // 320kbps MP3
	case QualitySQ:
		return "F000"
	case QualityHiRes:
		return "RS01"
	default:
		return "C400" // Default to free quality
	}
}

// GetFileExt returns the file extension for the quality
func (q AudioQuality) GetFileExt() string {
	switch q {
	case QualityStandard:
		return "m4a" // C400 uses M4A format
	case QualityHQ:
		return "mp3"
	case QualitySQ:
		return "flac"
	case QualityHiRes:
		return "flac"
	default:
		return "m4a"
	}
}

// PcmStreamInfo contains information about a PCM stream
type PcmStreamInfo struct {
	SampleRate  int    `json:"sampleRate"`
	Channels    int    `json:"channels"`
	TotalFrames uint64 `json:"totalFrames"`
	Format      string `json:"format"`
	CanSeek     bool   `json:"canSeek"`
}

// APIResponse is the generic QQ Music API response wrapper
type APIResponse struct {
	Code    int         `json:"code"`
	Message string      `json:"message,omitempty"`
	Data    interface{} `json:"data,omitempty"`
}

// LoginCookie stores the login cookie information
type LoginCookie struct {
	QQMusicKey string `json:"qqmusic_key"`
	QQMusicUIN string `json:"qqmusic_uin"`
	QQLogin    string `json:"qq_login"`
	Cookies    string `json:"cookies"` // Full cookie string
}

// ErrorInfo contains error details
type ErrorInfo struct {
	Code    int    `json:"code"`
	Message string `json:"message"`
}

// Result is a generic result wrapper for Go bridge functions
type Result struct {
	Success bool        `json:"success"`
	Data    interface{} `json:"data,omitempty"`
	Error   *ErrorInfo  `json:"error,omitempty"`
}

// NewSuccessResult creates a success result
func NewSuccessResult(data interface{}) *Result {
	return &Result{
		Success: true,
		Data:    data,
	}
}

// NewErrorResult creates an error result
func NewErrorResult(code int, message string) *Result {
	return &Result{
		Success: false,
		Error: &ErrorInfo{
			Code:    code,
			Message: message,
		},
	}
}
