package api

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"net/http"
	"net/http/cookiejar"
	"net/url"
	"os"
	"path/filepath"
	"qqmusic_bridge/crypto"
	"qqmusic_bridge/models"
	"strings"
	"sync"
	"time"

	"github.com/go-resty/resty/v2"
)

const (
	// API endpoints
	BaseURL       = "https://u.y.qq.com"
	MusicURL      = "https://c.y.qq.com"
	StreamURL     = "https://dl.stream.qqmusic.qq.com"
	ImgURL        = "https://y.gtimg.cn/music/photo_new"
	QRLoginURL    = "https://ssl.ptlogin2.qq.com"
	CheckLoginURL = "https://ptlogin2.qq.com"

	// User Agent
	UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"

	// Timeouts
	DefaultTimeout = 30 * time.Second

	// Cookie file
	CookieFileName = "qqmusic_cookie.json"
)

// Client is the QQ Music API client
type Client struct {
	httpClient *resty.Client
	cookieJar  *cookiejar.Jar
	cookies    string
	uin        int64
	gtk        int
	guid       string
	dataDir    string
	mu         sync.RWMutex
	loggedIn   bool
	userInfo   *models.UserInfo
}

// NewClient creates a new QQ Music API client
func NewClient(dataDir string) (*Client, error) {
	jar, err := cookiejar.New(nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create cookie jar: %w", err)
	}

	client := &Client{
		cookieJar: jar,
		dataDir:   dataDir,
		guid:      crypto.GenerateGUID(),
	}

	client.httpClient = resty.New().
		SetTimeout(DefaultTimeout).
		SetHeader("User-Agent", UserAgent).
		SetHeader("Referer", "https://y.qq.com/").
		SetHeader("Origin", "https://y.qq.com").
		SetCookieJar(jar)

	// Try to load saved cookies
	if err := client.loadCookies(); err == nil {
		client.loggedIn = true
	}

	return client, nil
}

// SetCookies sets the cookies for authentication
func (c *Client) SetCookies(cookies string) error {
	c.mu.Lock()
	defer c.mu.Unlock()

	c.cookies = cookies

	fmt.Printf("[SetCookies] Raw cookies length: %d\n", len(cookies))
	fmt.Printf("[SetCookies] Raw cookies: %s\n", cookies)

	// Parse cookies and extract UIN
	cookieParts := strings.Split(cookies, ";")
	for _, part := range cookieParts {
		part = strings.TrimSpace(part)
		// Check for UIN in various cookie names
		if strings.HasPrefix(part, "uin=") || strings.HasPrefix(part, "wxuin=") ||
			strings.HasPrefix(part, "p_uin=") || strings.HasPrefix(part, "pt2gguin=") {
			uin := part
			// Remove the cookie name prefix
			if idx := strings.Index(uin, "="); idx >= 0 {
				uin = uin[idx+1:]
			}
			// Remove leading 'o' if present (QQ uses o prefix for UIN)
			uin = strings.TrimPrefix(uin, "o")
			var parsedUin int64
			fmt.Sscanf(uin, "%d", &parsedUin)
			if parsedUin > 0 && c.uin == 0 {
				c.uin = parsedUin
			}
		}
		// Check for skey (can be skey, p_skey, qqmusic_key, or qm_keyst)
		if strings.HasPrefix(part, "skey=") || strings.HasPrefix(part, "p_skey=") ||
			strings.HasPrefix(part, "qqmusic_key=") || strings.HasPrefix(part, "qm_keyst=") {
			skey := part
			if idx := strings.Index(skey, "="); idx >= 0 {
				skey = skey[idx+1:]
			}
			if skey != "" {
				c.gtk = crypto.GtkHash(skey)
				debugLog("[SetCookies] Calculated gtk=%d from %s", c.gtk, part[:strings.Index(part, "=")])
			}
		}
	}

	// Set cookies on the HTTP client
	u, _ := url.Parse("https://y.qq.com")
	var httpCookies []*http.Cookie
	for _, part := range cookieParts {
		part = strings.TrimSpace(part)
		if idx := strings.Index(part, "="); idx > 0 {
			httpCookies = append(httpCookies, &http.Cookie{
				Name:  part[:idx],
				Value: part[idx+1:],
			})
		}
	}
	c.cookieJar.SetCookies(u, httpCookies)

	// Save cookies
	if err := c.saveCookies(); err != nil {
		return err
	}

	c.loggedIn = true
	return nil
}

// GetCookies returns the current cookies
func (c *Client) GetCookies() string {
	c.mu.RLock()
	defer c.mu.RUnlock()
	return c.cookies
}

// IsLoggedIn returns whether the client is logged in
func (c *Client) IsLoggedIn() bool {
	c.mu.RLock()
	defer c.mu.RUnlock()
	return c.loggedIn
}

// GetUIN returns the user UIN
func (c *Client) GetUIN() int64 {
	c.mu.RLock()
	defer c.mu.RUnlock()
	return c.uin
}

// GetGUID returns the GUID
func (c *Client) GetGUID() string {
	return c.guid
}

// saveCookies saves cookies to file
func (c *Client) saveCookies() error {
	if c.dataDir == "" {
		return nil
	}

	cookieData := models.LoginCookie{
		Cookies: c.cookies,
	}

	data, err := json.Marshal(cookieData)
	if err != nil {
		return err
	}

	cookiePath := filepath.Join(c.dataDir, CookieFileName)
	return ioutil.WriteFile(cookiePath, data, 0600)
}

// loadCookies loads cookies from file
func (c *Client) loadCookies() error {
	if c.dataDir == "" {
		return fmt.Errorf("no data directory")
	}

	cookiePath := filepath.Join(c.dataDir, CookieFileName)
	data, err := ioutil.ReadFile(cookiePath)
	if err != nil {
		return err
	}

	var cookieData models.LoginCookie
	if err := json.Unmarshal(data, &cookieData); err != nil {
		return err
	}

	if cookieData.Cookies == "" {
		return fmt.Errorf("empty cookies")
	}

	return c.SetCookies(cookieData.Cookies)
}

// ClearCookies clears saved cookies
func (c *Client) ClearCookies() error {
	c.mu.Lock()
	defer c.mu.Unlock()

	c.cookies = ""
	c.uin = 0
	c.gtk = 5381
	c.loggedIn = false
	c.userInfo = nil

	if c.dataDir != "" {
		cookiePath := filepath.Join(c.dataDir, CookieFileName)
		os.Remove(cookiePath)
	}

	return nil
}

// Request makes a request to QQ Music API
func (c *Client) Request(method, endpoint string, params map[string]interface{}) ([]byte, error) {
	c.mu.RLock()
	cookies := c.cookies
	uin := c.uin
	c.mu.RUnlock()

	// Build the request URL
	reqURL := endpoint
	if !strings.HasPrefix(endpoint, "http") {
		reqURL = BaseURL + endpoint
	}

	// Add common parameters
	if params == nil {
		params = make(map[string]interface{})
	}
	params["g_tk"] = c.gtk
	params["loginUin"] = uin
	params["hostUin"] = 0
	params["inCharset"] = "utf8"
	params["outCharset"] = "utf-8"
	params["format"] = "json"
	params["platform"] = "yqq.json"
	params["needNewCode"] = 0

	req := c.httpClient.R()
	if cookies != "" {
		req.SetHeader("Cookie", cookies)
	}

	var resp *resty.Response
	var err error

	if method == "GET" {
		// Convert params to query string
		queryParams := make(map[string]string)
		for k, v := range params {
			queryParams[k] = fmt.Sprintf("%v", v)
		}
		resp, err = req.SetQueryParams(queryParams).Get(reqURL)
	} else {
		// POST with JSON body
		resp, err = req.SetBody(params).Post(reqURL)
	}

	if err != nil {
		return nil, fmt.Errorf("request failed: %w", err)
	}

	if resp.StatusCode() != 200 {
		return nil, fmt.Errorf("HTTP error: %d", resp.StatusCode())
	}

	return resp.Body(), nil
}

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

// RequestCGI makes a CGI request (used for many QQ Music APIs)
func (c *Client) RequestCGI(module, method string, params map[string]interface{}) (json.RawMessage, error) {
	c.mu.RLock()
	uin := c.uin
	cookies := c.cookies
	gtk := c.gtk
	c.mu.RUnlock()

	debugLog("[RequestCGI] module=%s, method=%s, uin=%d, gtk=%d, cookies_len=%d", module, method, uin, gtk, len(cookies))

	// Extract authst (qqmusic_key) from cookies for authentication
	authst := ""
	tmeLoginType := 0
	for _, part := range strings.Split(cookies, ";") {
		part = strings.TrimSpace(part)
		if strings.HasPrefix(part, "qqmusic_key=") || strings.HasPrefix(part, "qm_keyst=") {
			if idx := strings.Index(part, "="); idx >= 0 {
				authst = part[idx+1:]
			}
		}
		if strings.HasPrefix(part, "tmeLoginType=") {
			if idx := strings.Index(part, "="); idx >= 0 {
				fmt.Sscanf(part[idx+1:], "%d", &tmeLoginType)
			}
		}
	}

	// Use yqq.json platform with QQ login credentials
	reqData := map[string]interface{}{
		"comm": map[string]interface{}{
			"g_tk":         gtk,
			"uin":          uin,
			"loginUin":     uin,
			"format":       "json",
			"inCharset":    "utf-8",
			"outCharset":   "utf-8",
			"notice":       0,
			"platform":     "yqq.json",
			"needNewCode":  0,
			"ct":           20,
			"cv":           4747474,
			"authst":       authst,
			"tmeLoginType": tmeLoginType,
			"tmeAppID":     "qqmusic",
		},
		"req_0": map[string]interface{}{
			"module": module,
			"method": method,
			"param":  params,
		},
	}

	jsonData, err := json.Marshal(reqData)
	if err != nil {
		return nil, err
	}

	debugLog("[RequestCGI] Request body: %s", string(jsonData))

	reqURL := BaseURL + "/cgi-bin/musics.fcg"
	req := c.httpClient.R().
		SetHeader("Content-Type", "application/json").
		SetQueryParam("_", fmt.Sprintf("%d", time.Now().UnixMilli())).
		SetQueryParam("g_tk", fmt.Sprintf("%d", gtk)).
		SetBody(jsonData)

	if cookies != "" {
		req.SetHeader("Cookie", cookies)
	}

	resp, err := req.Post(reqURL)
	if err != nil {
		return nil, fmt.Errorf("CGI request failed: %w", err)
	}

	var result struct {
		Code  int `json:"code"`
		Req0  struct {
			Code int             `json:"code"`
			Data json.RawMessage `json:"data"`
		} `json:"req_0"`
	}

	debugLog("[RequestCGI] Response body: %s", string(resp.Body()[:min(500, len(resp.Body()))]))

	if err := json.Unmarshal(resp.Body(), &result); err != nil {
		return nil, fmt.Errorf("failed to parse response: %w", err)
	}

	debugLog("[RequestCGI] Result code=%d, req0.code=%d", result.Code, result.Req0.Code)

	if result.Code != 0 {
		return nil, fmt.Errorf("API error code: %d", result.Code)
	}

	if result.Req0.Code != 0 {
		return nil, fmt.Errorf("request error code: %d", result.Req0.Code)
	}

	return result.Req0.Data, nil
}

// RequestMultiCGI makes multiple CGI requests in one call
func (c *Client) RequestMultiCGI(requests map[string]struct {
	Module string
	Method string
	Params map[string]interface{}
}) (map[string]json.RawMessage, error) {
	c.mu.RLock()
	uin := c.uin
	c.mu.RUnlock()

	reqData := map[string]interface{}{
		"comm": map[string]interface{}{
			"g_tk":         c.gtk,
			"uin":          uin,
			"format":       "json",
			"inCharset":    "utf-8",
			"outCharset":   "utf-8",
			"notice":       0,
			"platform":     "h5",
			"needNewCode":  1,
			"ct":           23,
			"cv":           0,
			"guid":         c.guid,
		},
	}

	for key, req := range requests {
		reqData[key] = map[string]interface{}{
			"module": req.Module,
			"method": req.Method,
			"param":  req.Params,
		}
	}

	jsonData, err := json.Marshal(reqData)
	if err != nil {
		return nil, err
	}

	reqURL := BaseURL + "/cgi-bin/musics.fcg"
	req := c.httpClient.R().
		SetHeader("Content-Type", "application/json").
		SetQueryParam("_", fmt.Sprintf("%d", time.Now().UnixMilli())).
		SetBody(jsonData)

	if c.cookies != "" {
		req.SetHeader("Cookie", c.cookies)
	}

	resp, err := req.Post(reqURL)
	if err != nil {
		return nil, fmt.Errorf("multi CGI request failed: %w", err)
	}

	var result map[string]json.RawMessage
	if err := json.Unmarshal(resp.Body(), &result); err != nil {
		return nil, fmt.Errorf("failed to parse response: %w", err)
	}

	// Extract data from each request
	responses := make(map[string]json.RawMessage)
	for key := range requests {
		if rawResp, ok := result[key]; ok {
			var reqResult struct {
				Code int             `json:"code"`
				Data json.RawMessage `json:"data"`
			}
			if err := json.Unmarshal(rawResp, &reqResult); err == nil && reqResult.Code == 0 {
				responses[key] = reqResult.Data
			}
		}
	}

	return responses, nil
}

// DownloadFile downloads a file from URL
func (c *Client) DownloadFile(fileURL string) ([]byte, error) {
	resp, err := c.httpClient.R().Get(fileURL)
	if err != nil {
		return nil, fmt.Errorf("download failed: %w", err)
	}

	if resp.StatusCode() != 200 {
		return nil, fmt.Errorf("HTTP error: %d", resp.StatusCode())
	}

	return resp.Body(), nil
}

// GetHTTPClient returns the underlying HTTP client for streaming
func (c *Client) GetHTTPClient() *http.Client {
	return c.httpClient.GetClient()
}

// SetUserInfo sets the cached user info
func (c *Client) SetUserInfo(info *models.UserInfo) {
	c.mu.Lock()
	defer c.mu.Unlock()
	c.userInfo = info
}

// GetUserInfo returns the cached user info
func (c *Client) GetUserInfo() *models.UserInfo {
	c.mu.RLock()
	defer c.mu.RUnlock()
	return c.userInfo
}

// EnsureDataDir ensures the data directory exists
func (c *Client) EnsureDataDir() error {
	if c.dataDir == "" {
		return nil
	}
	return os.MkdirAll(c.dataDir, 0755)
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}
