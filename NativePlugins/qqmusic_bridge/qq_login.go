package main

import (
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"math/rand"
	"net/http"
	"net/http/cookiejar"
	"net/url"
	"regexp"
	"strings"
	"sync"
	"time"
)

// ptlogin2 QQ扫码登录管理器
type QRLoginManager struct {
	qrsig      string
	ptqrtoken  int
	httpClient *http.Client
	mu         sync.Mutex
}

var (
	qrLoginMgr *QRLoginManager
	qrMu       sync.Mutex
)

const (
	// QQ Music 的 appid
	qqMusicAppID    = "716027609"
	qqMusicDaid     = "383"
	qqMusicPt3rdAid = "100497308"

	ptqrshowURL = "https://ssl.ptlogin2.qq.com/ptqrshow"
	ptqrloginURL = "https://ssl.ptlogin2.qq.com/ptqrlogin"

	// 微信登录
	wxAppID       = "wx48db31d50e334801"
	wxQRConnectURL = "https://open.weixin.qq.com/connect/qrconnect"
	wxQRCodeURL    = "https://open.weixin.qq.com/connect/qrcode/"
	wxQRPollURL    = "https://lp.open.weixin.qq.com/connect/l/qrconnect"
)

// 登录类型
const (
	LoginTypeQQ = "qq"
	LoginTypeWX = "wx"
)

// calcPtqrtoken 计算 ptqrtoken（从 qrsig cookie）
func calcPtqrtoken(qrsig string) int {
	hash := 0
	for _, c := range qrsig {
		hash += (hash << 5) + int(c)
	}
	return hash & 0x7fffffff
}

// newQRLoginManager 创建新的 QR 登录管理器
func newQRLoginManager() *QRLoginManager {
	jar, _ := cookiejar.New(nil)
	return &QRLoginManager{
		httpClient: &http.Client{
			Jar:     jar,
			Timeout: 30 * time.Second,
			// 不自动跟随重定向，手动处理
			CheckRedirect: func(req *http.Request, via []*http.Request) error {
				return http.ErrUseLastResponse
			},
		},
	}
}

// GetQRCode 获取二维码 PNG 图片
func (m *QRLoginManager) GetQRCode() ([]byte, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	// 构建请求 URL
	params := url.Values{}
	params.Set("appid", qqMusicAppID)
	params.Set("e", "2")
	params.Set("l", "M")
	params.Set("s", "3")
	params.Set("d", "72")
	params.Set("v", "4")
	params.Set("t", fmt.Sprintf("0.%d", rand.Intn(1000000)))
	params.Set("daid", qqMusicDaid)
	params.Set("pt_3rd_aid", qqMusicPt3rdAid)

	reqURL := ptqrshowURL + "?" + params.Encode()

	req, err := http.NewRequest("GET", reqURL, nil)
	if err != nil {
		return nil, fmt.Errorf("create request failed: %w", err)
	}
	req.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")

	resp, err := m.httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("request failed: %w", err)
	}
	defer resp.Body.Close()

	// 读取 PNG 图片
	imgData, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("read body failed: %w", err)
	}

	// 从 cookie 中提取 qrsig
	u, _ := url.Parse("https://ssl.ptlogin2.qq.com")
	for _, cookie := range m.httpClient.Jar.Cookies(u) {
		if cookie.Name == "qrsig" {
			m.qrsig = cookie.Value
			m.ptqrtoken = calcPtqrtoken(cookie.Value)
			break
		}
	}

	if m.qrsig == "" {
		return nil, fmt.Errorf("qrsig cookie not found in response")
	}

	return imgData, nil
}

// CheckStatus 检查扫码状态
// 返回: code (66=等待扫码, 67=已扫待确认, 0=成功, 65=过期), nickname, cookies, error
func (m *QRLoginManager) CheckStatus() (int, string, string, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	if m.qrsig == "" {
		return -1, "", "", fmt.Errorf("QR code not generated yet")
	}

	// 构建轮询 URL
	params := url.Values{}
	params.Set("u1", "https://graph.qq.com/oauth2.0/login_jump")
	params.Set("ptqrtoken", fmt.Sprintf("%d", m.ptqrtoken))
	params.Set("ptredirect", "0")
	params.Set("h", "1")
	params.Set("t", "1")
	params.Set("g", "1")
	params.Set("from_ui", "1")
	params.Set("ptlang", "2052")
	params.Set("action", fmt.Sprintf("0-0-%d", time.Now().Unix()))
	params.Set("js_ver", "20102616")
	params.Set("js_type", "1")
	params.Set("pt_uistyle", "40")
	params.Set("aid", qqMusicAppID)
	params.Set("daid", qqMusicDaid)
	params.Set("pt_3rd_aid", qqMusicPt3rdAid)
	params.Set("has_onekey", "1")

	reqURL := "https://ssl.ptlogin2.qq.com/ptqrlogin?" + params.Encode()

	req, err := http.NewRequest("GET", reqURL, nil)
	if err != nil {
		return -1, "", "", fmt.Errorf("create request failed: %w", err)
	}
	req.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
	req.Header.Set("Referer", "https://xui.ptlogin2.qq.com/")
	req.Header.Set("Cookie", "qrsig="+m.qrsig)

	checkClient := &http.Client{
		Jar:     m.httpClient.Jar,
		Timeout: 30 * time.Second,
	}
	resp, err := checkClient.Do(req)
	if err != nil {
		return -1, "", "", fmt.Errorf("request failed: %w", err)
	}
	defer resp.Body.Close()

	body, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		return -1, "", "", fmt.Errorf("read body failed: %w", err)
	}

	bodyStr := string(body)
	// ptuiCB 可能有6或7个参数，用宽松正则匹配
	re := regexp.MustCompile(`ptuiCB\('(\d+)',\s*'[^']*',\s*'([^']*)',\s*'[^']*',\s*'([^']*)',\s*'([^']*)'`)
	matches := re.FindStringSubmatch(bodyStr)
	if matches == nil {
		return -1, "", "", fmt.Errorf("unexpected response (len=%d): %s", len(bodyStr), bodyStr)
	}

	var code int
	fmt.Sscanf(matches[1], "%d", &code)
	redirectURL := matches[2]
	nickname := matches[4]

	// 登录成功: 需要从 URL 提取 ptsigx 和 uin，然后走 authorize 流程
	if code == 0 && redirectURL != "" {
		cookies, err := m.authorizeQQLogin(redirectURL)
		if err != nil {
			return 0, nickname, "", fmt.Errorf("authorize failed: %w", err)
		}
		return 0, nickname, cookies, nil
	}

	return code, nickname, "", nil
}

// authorizeQQLogin 完成 QQ 扫码登录的完整鉴权流程
// Step 1: 从 ptqrlogin 的 redirect URL 提取 ptsigx 和 uin
// Step 2: 访问 check_sig 获取 p_skey
// Step 3: 用 p_skey POST authorize 获取 code
// Step 4: 用 code 调用 QQ Music API 获取 musickey
func (m *QRLoginManager) authorizeQQLogin(redirectURL string) (string, error) {
	noRedirectClient := &http.Client{
		Jar:     m.httpClient.Jar,
		Timeout: 30 * time.Second,
		CheckRedirect: func(req *http.Request, via []*http.Request) error {
			return http.ErrUseLastResponse
		},
	}

	// Step 1: 从 redirect URL 提取 ptsigx 和 uin
	sigxRe := regexp.MustCompile(`[?&]ptsigx=([^&]+)`)
	uinRe := regexp.MustCompile(`[?&]uin=([^&]+)`)

	sigxMatch := sigxRe.FindStringSubmatch(redirectURL)
	uinMatch := uinRe.FindStringSubmatch(redirectURL)
	if sigxMatch == nil || uinMatch == nil {
		return "", fmt.Errorf("failed to extract ptsigx/uin from URL")
	}
	sigx := sigxMatch[1]
	uin := uinMatch[1]

	// Step 2: 访问 check_sig 获取 p_skey cookie
	checkSigParams := url.Values{}
	checkSigParams.Set("uin", uin)
	checkSigParams.Set("pttype", "1")
	checkSigParams.Set("service", "ptqrlogin")
	checkSigParams.Set("nodirect", "0")
	checkSigParams.Set("ptsigx", sigx)
	checkSigParams.Set("s_url", "https://graph.qq.com/oauth2.0/login_jump")
	checkSigParams.Set("ptlang", "2052")
	checkSigParams.Set("ptredirect", "100")
	checkSigParams.Set("aid", qqMusicAppID)
	checkSigParams.Set("daid", qqMusicDaid)
	checkSigParams.Set("j_later", "0")
	checkSigParams.Set("low_login_hour", "0")
	checkSigParams.Set("regmaster", "0")
	checkSigParams.Set("pt_login_type", "3")
	checkSigParams.Set("pt_aid", "0")
	checkSigParams.Set("pt_aaid", "16")
	checkSigParams.Set("pt_light", "0")
	checkSigParams.Set("pt_3rd_aid", qqMusicPt3rdAid)

	checkSigURL := "https://ssl.ptlogin2.graph.qq.com/check_sig?" + checkSigParams.Encode()
	req, _ := http.NewRequest("GET", checkSigURL, nil)
	req.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
	req.Header.Set("Referer", "https://xui.ptlogin2.qq.com/")

	// 跟随重定向链收集 cookie
	allCookies := make(map[string]string)
	currentURL := checkSigURL
	for i := 0; i < 10; i++ {
		req, _ := http.NewRequest("GET", currentURL, nil)
		req.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
		req.Header.Set("Referer", "https://xui.ptlogin2.qq.com/")
		resp, err := noRedirectClient.Do(req)
		if err != nil {
			break
		}
		for _, c := range resp.Cookies() {
			if c.Value != "" {
				allCookies[c.Name] = c.Value
			}
		}
		resp.Body.Close()
		if resp.StatusCode >= 300 && resp.StatusCode < 400 {
			location := resp.Header.Get("Location")
			if location == "" {
				break
			}
			currentURL = location
			continue
		}
		break
	}

	pSkey, ok := allCookies["p_skey"]
	if !ok || pSkey == "" {
		return "", fmt.Errorf("failed to get p_skey from check_sig")
	}

	// Step 3: POST authorize 获取 code
	// g_tk = hash33(p_skey, init=5381)
	gtk2 := 5381
	for _, c := range pSkey {
		gtk2 += (gtk2 << 5) + int(c)
	}
	gtk2 = gtk2 & 0x7fffffff

	// 构建 cookie 字符串用于 authorize 请求
	var cookieParts []string
	for k, v := range allCookies {
		cookieParts = append(cookieParts, k+"="+v)
	}
	cookieStr := strings.Join(cookieParts, "; ")

	authData := url.Values{}
	authData.Set("response_type", "code")
	authData.Set("client_id", qqMusicPt3rdAid)
	authData.Set("redirect_uri", "https://y.qq.com/portal/wx_redirect.html?login_type=1&surl=https://y.qq.com/")
	authData.Set("scope", "get_user_info,get_app_friends")
	authData.Set("state", "state")
	authData.Set("switch", "")
	authData.Set("from_ptlogin", "1")
	authData.Set("src", "1")
	authData.Set("update_auth", "1")
	authData.Set("openapi", "1010_1030")
	authData.Set("g_tk", fmt.Sprintf("%d", gtk2))
	authData.Set("auth_time", fmt.Sprintf("%d", time.Now().UnixMilli()))
	authData.Set("ui", fmt.Sprintf("%d", rand.Int63()))

	authReq, _ := http.NewRequest("POST", "https://graph.qq.com/oauth2.0/authorize", strings.NewReader(authData.Encode()))
	authReq.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	authReq.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
	authReq.Header.Set("Cookie", cookieStr)

	authResp, err := noRedirectClient.Do(authReq)
	if err != nil {
		return "", fmt.Errorf("authorize request failed: %w", err)
	}
	defer authResp.Body.Close()

	location := authResp.Header.Get("Location")
	codeRe := regexp.MustCompile(`code=([^&]+)`)
	codeMatch := codeRe.FindStringSubmatch(location)
	if codeMatch == nil {
		return "", fmt.Errorf("failed to extract code from authorize redirect: %s", location)
	}
	authCode := codeMatch[1]
	log.Printf("[QRLogin] authorize code=%s, redirect=%s", authCode, location)

	// Step 4: 用 code 调用 QQ Music API 获取 musickey
	musicReqData := map[string]interface{}{
		"comm": map[string]interface{}{
			"g_tk":         0,
			"platform":     "yqq.json",
			"ct":           24,
			"cv":           0,
			"tmeLoginType": 2,
		},
		"req_0": map[string]interface{}{
			"module": "QQConnectLogin.LoginServer",
			"method": "QQLogin",
			"param": map[string]interface{}{
				"code": authCode,
			},
		},
	}

	jsonData, _ := json.Marshal(musicReqData)
	musicReq, _ := http.NewRequest("POST",
		fmt.Sprintf("https://u.y.qq.com/cgi-bin/musicu.fcg?_=%d", time.Now().UnixMilli()),
		strings.NewReader(string(jsonData)))
	musicReq.Header.Set("Content-Type", "application/json")
	musicReq.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
	musicReq.Header.Set("Referer", "https://y.qq.com/")

	normalClient := &http.Client{Timeout: 30 * time.Second}
	musicResp, err := normalClient.Do(musicReq)
	if err != nil {
		return "", fmt.Errorf("music login request failed: %w", err)
	}
	defer musicResp.Body.Close()

	musicBody, _ := ioutil.ReadAll(musicResp.Body)

	var musicResult struct {
		Code int `json:"code"`
		Req0 struct {
			Code int `json:"code"`
			Data struct {
				Musicid            int64  `json:"musicid"`
				Musickey           string `json:"musickey"`
				LoginType          int    `json:"loginType"`
				Openid             string `json:"openid"`
				RefreshToken       string `json:"refresh_token"`
				AccessToken        string `json:"access_token"`
				Unionid            string `json:"unionid"`
				MusickeyCreateTime int64  `json:"musickeyCreateTime"`
				EncryptUin         string `json:"encryptUin"`
				StrMusicid         string `json:"str_musicid"`
			} `json:"data"`
		} `json:"req_0"`
	}

	// 记录完整的 QQLogin 响应用于调试
	log.Printf("[QRLogin] QQLogin response: %s", string(musicBody))

	if err := json.Unmarshal(musicBody, &musicResult); err != nil {
		return "", fmt.Errorf("failed to parse music login response: %w", err)
	}

	if musicResult.Code != 0 || musicResult.Req0.Code != 0 {
		return "", fmt.Errorf("music login failed: code=%d, req0.code=%d, body=%.500s", musicResult.Code, musicResult.Req0.Code, string(musicBody))
	}

	log.Printf("[QRLogin] musicid=%d, musickey_len=%d, loginType=%d, openid=%s",
		musicResult.Req0.Data.Musicid, len(musicResult.Req0.Data.Musickey),
		musicResult.Req0.Data.LoginType, musicResult.Req0.Data.Openid)

	d := musicResult.Req0.Data
	if d.Musickey == "" {
		return "", fmt.Errorf("musickey is empty in response")
	}

	// 构建和浏览器一致的 cookie 格式
	finalCookies := map[string]string{
		"qqmusic_key":              d.Musickey,
		"qm_keyst":                 d.Musickey,
		"uin":                      fmt.Sprintf("%d", d.Musicid),
		"euin":                     d.EncryptUin,
		"tmeLoginType":             fmt.Sprintf("%d", d.LoginType),
		"psrf_qqopenid":            d.Openid,
		"psrf_qqaccess_token":      d.AccessToken,
		"psrf_qqunionid":           d.Unionid,
		"psrf_qqrefresh_token":     d.RefreshToken,
		"psrf_musickey_createtime": fmt.Sprintf("%d", d.MusickeyCreateTime),
		"login_type":               "1",
	}

	var finalParts []string
	for k, v := range finalCookies {
		if v != "" {
			finalParts = append(finalParts, k+"="+v)
		}
	}

	return strings.Join(finalParts, "; "), nil
}

// Cancel 取消登录
func (m *QRLoginManager) Cancel() {
	m.mu.Lock()
	defer m.mu.Unlock()
	m.qrsig = ""
	m.ptqrtoken = 0
}

// ==================== 微信二维码登录 ====================

// WXQRLoginManager 微信扫码登录管理器
type WXQRLoginManager struct {
	uuid       string
	httpClient *http.Client
	mu         sync.Mutex
}

var (
	wxLoginMgr *WXQRLoginManager
	wxMu       sync.Mutex
)

func newWXQRLoginManager() *WXQRLoginManager {
	return &WXQRLoginManager{
		httpClient: &http.Client{Timeout: 40 * time.Second},
	}
}

// GetQRCode 获取微信登录二维码
func (m *WXQRLoginManager) GetQRCode() ([]byte, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	// Step 1: 获取 uuid
	params := url.Values{}
	params.Set("appid", wxAppID)
	params.Set("redirect_uri", "https://y.qq.com/portal/wx_redirect.html?login_type=2&surl=https://y.qq.com/")
	params.Set("response_type", "code")
	params.Set("scope", "snsapi_login")
	params.Set("state", "STATE")
	params.Set("href", "https://y.qq.com/mediastyle/music_v17/src/css/popup_wechat.css#wechat_redirect")

	resp, err := m.httpClient.Get(wxQRConnectURL + "?" + params.Encode())
	if err != nil {
		return nil, fmt.Errorf("get wx qrconnect failed: %w", err)
	}
	defer resp.Body.Close()

	body, _ := ioutil.ReadAll(resp.Body)
	uuidRe := regexp.MustCompile(`uuid=(.+?)"`)
	uuidMatch := uuidRe.FindStringSubmatch(string(body))
	if uuidMatch == nil {
		return nil, fmt.Errorf("failed to extract wx uuid")
	}
	m.uuid = uuidMatch[1]

	// Step 2: 获取二维码图片
	req, _ := http.NewRequest("GET", wxQRCodeURL+m.uuid, nil)
	req.Header.Set("Referer", "https://open.weixin.qq.com/connect/qrconnect")
	imgResp, err := m.httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("get wx qrcode image failed: %w", err)
	}
	defer imgResp.Body.Close()

	imgData, _ := ioutil.ReadAll(imgResp.Body)
	return imgData, nil
}

// CheckStatus 检查微信扫码状态
// 返回: code (408=等待, 404=已扫, 405=成功, 402=过期), wxCode, error
func (m *WXQRLoginManager) CheckStatus() (int, string, string, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	if m.uuid == "" {
		return -1, "", "", fmt.Errorf("WX QR code not generated yet")
	}

	pollClient := &http.Client{Timeout: 40 * time.Second}
	req, _ := http.NewRequest("GET",
		fmt.Sprintf("%s?uuid=%s&_=%d", wxQRPollURL, m.uuid, time.Now().UnixMilli()), nil)
	req.Header.Set("Referer", "https://open.weixin.qq.com/")

	resp, err := pollClient.Do(req)
	if err != nil {
		// 超时视为等待扫码
		return 408, "", "", nil
	}
	defer resp.Body.Close()

	body, _ := ioutil.ReadAll(resp.Body)
	bodyStr := string(body)

	// 解析 window.wx_errcode=xxx;window.wx_code='yyy'
	statusRe := regexp.MustCompile(`window\.wx_errcode=(\d+);window\.wx_code='([^']*)'`)
	match := statusRe.FindStringSubmatch(bodyStr)
	if match == nil {
		return -1, "", "", fmt.Errorf("unexpected wx response: %.200s", bodyStr)
	}

	var wxErrcode int
	fmt.Sscanf(match[1], "%d", &wxErrcode)
	wxCode := match[2]

	// 405 = 登录成功，需要用 wxCode 换取 musickey
	if wxErrcode == 405 && wxCode != "" {
		cookies, err := m.authorizeWXLogin(wxCode)
		if err != nil {
			return 405, "", "", fmt.Errorf("wx authorize failed: %w", err)
		}
		return 405, "", cookies, nil
	}

	return wxErrcode, "", "", nil
}

// authorizeWXLogin 用微信 code 换取 QQ Music musickey
func (m *WXQRLoginManager) authorizeWXLogin(wxCode string) (string, error) {
	musicReqData := map[string]interface{}{
		"comm": map[string]interface{}{
			"g_tk":         0,
			"platform":     "yqq.json",
			"ct":           24,
			"cv":           0,
			"tmeLoginType": 1,
		},
		"req_0": map[string]interface{}{
			"module": "music.login.LoginServer",
			"method": "Login",
			"param": map[string]interface{}{
				"code":      wxCode,
				"strAppid":  wxAppID,
			},
		},
	}

	jsonData, _ := json.Marshal(musicReqData)
	req, _ := http.NewRequest("POST",
		fmt.Sprintf("https://u.y.qq.com/cgi-bin/musicu.fcg?_=%d", time.Now().UnixMilli()),
		strings.NewReader(string(jsonData)))
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")

	normalClient := &http.Client{Timeout: 30 * time.Second}
	resp, err := normalClient.Do(req)
	if err != nil {
		return "", fmt.Errorf("wx music login request failed: %w", err)
	}
	defer resp.Body.Close()

	musicBody, _ := ioutil.ReadAll(resp.Body)
	log.Printf("[WXLogin] response: %s", string(musicBody))

	var musicResult struct {
		Code int `json:"code"`
		Req0 struct {
			Code int `json:"code"`
			Data struct {
				Musicid            int64  `json:"musicid"`
				Musickey           string `json:"musickey"`
				LoginType          int    `json:"loginType"`
				Openid             string `json:"openid"`
				RefreshToken       string `json:"refresh_token"`
				AccessToken        string `json:"access_token"`
				Unionid            string `json:"unionid"`
				MusickeyCreateTime int64  `json:"musickeyCreateTime"`
				EncryptUin         string `json:"encryptUin"`
			} `json:"data"`
		} `json:"req_0"`
	}

	if err := json.Unmarshal(musicBody, &musicResult); err != nil {
		return "", fmt.Errorf("failed to parse wx login response: %w", err)
	}

	if musicResult.Code != 0 || musicResult.Req0.Code != 0 {
		return "", fmt.Errorf("wx music login failed: code=%d, req0.code=%d", musicResult.Code, musicResult.Req0.Code)
	}

	d := musicResult.Req0.Data
	if d.Musickey == "" {
		return "", fmt.Errorf("musickey is empty in wx login response")
	}

	finalCookies := map[string]string{
		"qqmusic_key":              d.Musickey,
		"qm_keyst":                 d.Musickey,
		"uin":                      fmt.Sprintf("%d", d.Musicid),
		"euin":                     d.EncryptUin,
		"tmeLoginType":             fmt.Sprintf("%d", d.LoginType),
		"wxopenid":                 d.Openid,
		"wxunionid":                d.Unionid,
		"wxrefresh_token":          d.RefreshToken,
		"psrf_musickey_createtime": fmt.Sprintf("%d", d.MusickeyCreateTime),
		"login_type":               "2",
	}

	var finalParts []string
	for k, v := range finalCookies {
		if v != "" {
			finalParts = append(finalParts, k+"="+v)
		}
	}

	return strings.Join(finalParts, "; "), nil
}

func (m *WXQRLoginManager) Cancel() {
	m.mu.Lock()
	defer m.mu.Unlock()
	m.uuid = ""
}

// ==================== 导出函数 ====================

// QRLoginStatus 返回给 C# 的状态结构
type QRLoginStatus struct {
	Code     int    `json:"code"`
	Msg      string `json:"msg"`
	Nickname string `json:"nickname"`
}

// 当前登录类型
var currentLoginType string

// getOrCreateQRLoginMgr 获取或创建 QQ QR 登录管理器
func getOrCreateQRLoginMgr() *QRLoginManager {
	qrMu.Lock()
	defer qrMu.Unlock()
	if qrLoginMgr == nil {
		qrLoginMgr = newQRLoginManager()
	}
	return qrLoginMgr
}

func getOrCreateWXLoginMgr() *WXQRLoginManager {
	wxMu.Lock()
	defer wxMu.Unlock()
	if wxLoginMgr == nil {
		wxLoginMgr = newWXQRLoginManager()
	}
	return wxLoginMgr
}

// applyLoginCookies 将 cookie 设置到 API client
func applyLoginCookies(cookies string) error {
	if client != nil && cookies != "" {
		if err := client.SetCookies(cookies); err != nil {
			return err
		}
		if cacheManager != nil {
			cacheManager.SetCookies(cookies)
		}
	}
	return nil
}

// GetQRImageBase64 获取 QR 码图片的 base64 编码（支持 QQ 和 WX）
func GetQRImageBase64(loginType string) (string, error) {
	currentLoginType = loginType

	var imgData []byte
	var err error

	if loginType == LoginTypeWX {
		mgr := getOrCreateWXLoginMgr()
		imgData, err = mgr.GetQRCode()
	} else {
		mgr := getOrCreateQRLoginMgr()
		imgData, err = mgr.GetQRCode()
	}

	if err != nil {
		return "", err
	}
	return base64.StdEncoding.EncodeToString(imgData), nil
}

// CheckQRLoginStatus 检查 QR 登录状态（根据当前登录类型）
func CheckQRLoginStatus() (*QRLoginStatus, error) {
	if currentLoginType == LoginTypeWX {
		return checkWXLoginStatus()
	}
	return checkQQLoginStatus()
}

func checkQQLoginStatus() (*QRLoginStatus, error) {
	mgr := getOrCreateQRLoginMgr()
	code, nickname, cookies, err := mgr.CheckStatus()
	if err != nil && code != 0 {
		return nil, err
	}

	status := &QRLoginStatus{
		Code:     code,
		Nickname: nickname,
	}

	switch code {
	case 66:
		status.Msg = "等待扫码"
	case 67:
		status.Msg = "已扫码，请在手机上确认"
	case 0:
		status.Msg = "登录成功"
		if err := applyLoginCookies(cookies); err != nil {
			status.Msg = "登录成功但设置Cookie失败: " + err.Error()
		}
		qrMu.Lock()
		qrLoginMgr = nil
		qrMu.Unlock()
	case 65:
		status.Msg = "二维码已过期"
		mgr.Cancel()
	default:
		status.Msg = fmt.Sprintf("未知状态: %d", code)
	}

	return status, nil
}

func checkWXLoginStatus() (*QRLoginStatus, error) {
	mgr := getOrCreateWXLoginMgr()
	wxCode, _, cookies, err := mgr.CheckStatus()
	if err != nil && wxCode != 405 {
		return nil, err
	}

	status := &QRLoginStatus{}

	switch wxCode {
	case 408:
		status.Code = 66 // 映射为统一的等待扫码
		status.Msg = "等待扫码"
	case 404:
		status.Code = 67 // 已扫码
		status.Msg = "已扫码，请在微信上确认"
	case 405:
		status.Code = 0 // 登录成功
		status.Msg = "登录成功"
		if err := applyLoginCookies(cookies); err != nil {
			status.Msg = "登录成功但设置Cookie失败: " + err.Error()
		}
		wxMu.Lock()
		wxLoginMgr = nil
		wxMu.Unlock()
	case 402:
		status.Code = 65 // 过期
		status.Msg = "二维码已过期"
		mgr.Cancel()
	case 403:
		status.Code = 65
		status.Msg = "用户拒绝登录"
		mgr.Cancel()
	default:
		status.Code = wxCode
		status.Msg = fmt.Sprintf("未知状态: %d", wxCode)
	}

	return status, nil
}

// CancelQRLogin 取消 QR 登录
func CancelQRLogin() {
	qrMu.Lock()
	if qrLoginMgr != nil {
		qrLoginMgr.Cancel()
		qrLoginMgr = nil
	}
	qrMu.Unlock()

	wxMu.Lock()
	if wxLoginMgr != nil {
		wxLoginMgr.Cancel()
		wxLoginMgr = nil
	}
	wxMu.Unlock()

	currentLoginType = ""
}
