using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace ChillPatcher.Module.Spotify
{
    /// <summary>
    /// Spotify OAuth 2.0 PKCE 认证管理器。
    /// 使用 fullstop:// 自定义 URI 协议接收回调，与 sfo 项目保持一致。
    /// </summary>
    public class OAuthManager : IDisposable
    {
        private const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
        private const string TokenUrl = "https://accounts.spotify.com/api/token";
        private const string ProtocolScheme = "fullstop";
        private const string RedirectUri = "fullstop://callback";

        private static readonly string[] Scopes = new[]
        {
            "user-read-private",
            "user-read-playback-state",
            "user-modify-playback-state",
            "user-read-currently-playing",
            "playlist-read-private",
            "playlist-read-collaborative",
            "user-library-read",
            "user-library-modify"
        };

        private readonly string _clientId;
        private readonly string _dataPath;
        private readonly ManualLogSource _logger;
        private readonly HttpClient _httpClient;
        private readonly string _callbackFilePath;
        private readonly string _handlerScriptPath;

        private string _codeVerifier;
        private string _codeChallenge;
        private string _state;
        private CancellationTokenSource _cts;

        public event Action<SpotifyTokenResponse> OnTokenReceived;
        public event Action<string> OnLoginFailed;
        public event Action<string> OnStatusChanged;

        public OAuthManager(string clientId, string dataPath, ManualLogSource logger)
        {
            _clientId = clientId;
            _dataPath = dataPath;
            _logger = logger;
            _httpClient = new HttpClient();

            _callbackFilePath = Path.Combine(Path.GetTempPath(), "fullstop_callback.txt");
            _handlerScriptPath = Path.Combine(dataPath, "fullstop_handler.ps1");

            EnsureProtocolRegistered();
        }

        // =====================================================================
        // 自定义 URI 协议注册
        // =====================================================================

        private void EnsureProtocolRegistered()
        {
            try
            {
                // 创建 PowerShell 处理脚本
                Directory.CreateDirectory(_dataPath);
                var scriptContent = $"[IO.File]::WriteAllText(\"{_callbackFilePath.Replace("\\", "\\\\")}\", $args[0])";
                File.WriteAllText(_handlerScriptPath, scriptContent, Encoding.UTF8);
                _logger.LogInfo($"Protocol handler script created: {_handlerScriptPath}");

                // 注册 fullstop:// 协议到 Windows 注册表
                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProtocolScheme}"))
                {
                    key.SetValue("", $"URL:{ProtocolScheme} Protocol");
                    key.SetValue("URL Protocol", "");

                    using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        var command = $"powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{_handlerScriptPath}\" \"%1\"";
                        commandKey.SetValue("", command);
                    }
                }

                _logger.LogInfo($"Registered '{ProtocolScheme}://' protocol handler");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to register protocol handler: {ex.Message}");
            }
        }

        // =====================================================================
        // OAuth 登录流程
        // =====================================================================

        /// <summary>
        /// 启动 OAuth 登录流程：生成 PKCE、打开浏览器、等待回调文件。
        /// </summary>
        public async Task StartLoginAsync(CancellationToken externalToken = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            // 清理旧的回调文件
            CleanupCallbackFile();

            // 生成 PKCE
            (_codeVerifier, _codeChallenge) = GeneratePKCE();
            _state = GenerateRandomString(16);

            // 构建授权 URL
            var scope = string.Join(" ", Scopes);
            var authUrl = $"{AuthorizeUrl}" +
                $"?client_id={Uri.EscapeDataString(_clientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&code_challenge={_codeChallenge}" +
                $"&code_challenge_method=S256" +
                $"&state={_state}";

            // 打开浏览器
            OnStatusChanged?.Invoke("正在打开浏览器...");
            try
            {
                System.Diagnostics.Process.Start(authUrl);
                _logger.LogInfo("Opened browser for Spotify authorization");
                OnStatusChanged?.Invoke("等待浏览器授权...");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open browser: {ex.Message}");
                OnLoginFailed?.Invoke("无法打开浏览器，请手动访问授权页面");
                return;
            }

            // 轮询回调文件（5 分钟超时）
            try
            {
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);
                await PollForCallbackFileAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("OAuth flow timed out or was cancelled");
                OnLoginFailed?.Invoke("登录超时或已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError($"OAuth error: {ex.Message}");
                OnLoginFailed?.Invoke($"登录失败: {ex.Message}");
            }
            finally
            {
                CleanupCallbackFile();
            }
        }

        /// <summary>
        /// 轮询临时文件，等待协议处理器写入回调 URL。
        /// </summary>
        private async Task PollForCallbackFileAsync(CancellationToken token)
        {
            _logger.LogInfo($"Polling for callback file: {_callbackFilePath}");

            while (!token.IsCancellationRequested)
            {
                if (File.Exists(_callbackFilePath))
                {
                    // 短暂等待确保文件写入完成
                    await Task.Delay(200, token);

                    string callbackUrl;
                    try
                    {
                        callbackUrl = File.ReadAllText(_callbackFilePath).Trim();
                    }
                    catch (IOException)
                    {
                        // 文件可能还在被写入，等一下重试
                        await Task.Delay(500, token);
                        continue;
                    }

                    _logger.LogInfo($"OAuth callback received: {callbackUrl}");
                    CleanupCallbackFile();

                    ProcessCallbackUrl(callbackUrl);
                    return;
                }

                await Task.Delay(500, token);
            }
        }

        private async void ProcessCallbackUrl(string callbackUrl)
        {
            OnStatusChanged?.Invoke("收到授权回调，正在验证...");

            // 解析 URL 参数: fullstop://callback?code=xxx&state=yyy
            var queryParams = ParseQueryString(callbackUrl);

            var error = GetParam(queryParams, "error");
            if (!string.IsNullOrEmpty(error))
            {
                OnLoginFailed?.Invoke($"用户拒绝授权: {error}");
                return;
            }

            var callbackState = GetParam(queryParams, "state");
            if (callbackState != _state)
            {
                _logger.LogWarning($"State mismatch: expected={_state}, got={callbackState}");
                OnLoginFailed?.Invoke("State 校验失败，请重试");
                return;
            }

            var code = GetParam(queryParams, "code");
            if (string.IsNullOrEmpty(code))
            {
                OnLoginFailed?.Invoke("未收到授权码");
                return;
            }

            OnStatusChanged?.Invoke("正在交换 Token...");
            await ExchangeCodeAsync(code);
        }

        private async Task ExchangeCodeAsync(string code)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = RedirectUri,
                ["client_id"] = _clientId,
                ["code_verifier"] = _codeVerifier
            });

            try
            {
                var response = await _httpClient.PostAsync(TokenUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Token exchange failed ({response.StatusCode}): {json}");
                    OnLoginFailed?.Invoke("Token 交换失败");
                    return;
                }

                var tokenResponse = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);
                _logger.LogInfo("Successfully obtained Spotify tokens");
                OnTokenReceived?.Invoke(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Token exchange error: {ex.Message}");
                OnLoginFailed?.Invoke($"Token 交换异常: {ex.Message}");
            }
        }

        // =====================================================================
        // 辅助方法
        // =====================================================================

        public void Cancel()
        {
            _cts?.Cancel();
            CleanupCallbackFile();
        }

        private void CleanupCallbackFile()
        {
            try
            {
                if (File.Exists(_callbackFilePath))
                    File.Delete(_callbackFilePath);
            }
            catch { }
        }

        /// <summary>
        /// 手动解析 URL 中的查询参数（避免依赖 System.Web）。
        /// 支持 fullstop://callback?code=xxx&amp;state=yyy 格式。
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string url)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var queryStart = url.IndexOf('?');
            if (queryStart < 0) return result;

            var query = url.Substring(queryStart + 1);
            foreach (var pair in query.Split('&'))
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                    result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                else if (parts.Length == 1)
                    result[Uri.UnescapeDataString(parts[0])] = "";
            }

            return result;
        }

        private static string GetParam(Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var val) ? val : null;
        }

        // =====================================================================
        // PKCE
        // =====================================================================

        private static (string verifier, string challenge) GeneratePKCE()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            var verifier = Base64UrlEncode(bytes);

            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
                var challenge = Base64UrlEncode(challengeBytes);
                return (verifier, challenge);
            }
        }

        private static string GenerateRandomString(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public void Dispose()
        {
            Cancel();
            _httpClient?.Dispose();
            _cts?.Dispose();
        }
    }
}
