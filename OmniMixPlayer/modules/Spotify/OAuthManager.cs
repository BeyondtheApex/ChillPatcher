using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OmniMixPlayer.Module.Spotify
{
    /// <summary>
    /// Spotify OAuth 2.0 PKCE manager.
    /// Uses a loopback HTTP callback so it also works when the backend runs as a Windows service.
    /// </summary>
    public class OAuthManager : IDisposable
    {
        private const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
        private const string TokenUrl = "https://accounts.spotify.com/api/token";
        private const string LoopbackHost = "127.0.0.1";
        private const string CallbackPath = "/callback";

        private static readonly int[] CallbackPorts = { 17891, 17892, 17893, 17894, 17895 };
        public const string DashboardRedirectUris = "http://127.0.0.1:17891/callback, http://127.0.0.1:17892/callback, http://127.0.0.1:17893/callback, http://127.0.0.1:17894/callback, http://127.0.0.1:17895/callback";

        private static readonly string[] Scopes =
        {
            "user-read-private",
            "user-read-playback-state",
            "user-modify-playback-state",
            "user-read-currently-playing",
            "streaming",
            "playlist-read-private",
            "playlist-read-collaborative",
            "user-library-read",
            "user-library-modify"
        };

        private readonly string _clientId;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private string _codeVerifier;
        private string _state;
        private string _redirectUri;
        private CancellationTokenSource _cts;

        public event Action<SpotifyTokenResponse> OnTokenReceived;
        public event Action<string> OnLoginFailed;
        public event Action<string> OnStatusChanged;
        public event Action<string> OnAuthorizationUrlReady;

        public OAuthManager(string clientId, string dataPath, ILogger logger)
        {
            _clientId = clientId;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task StartLoginAsync(CancellationToken externalToken = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            (_codeVerifier, var codeChallenge) = GeneratePKCE();
            _state = GenerateRandomString(16);

            var listener = TryStartLoopbackListener(out var selectedPort);
            if (listener == null)
            {
                OnLoginFailed?.Invoke("无法启动本地授权回调监听，预设端口均被占用");
                return;
            }

            using (listener)
            {
                _redirectUri = $"http://{LoopbackHost}:{selectedPort}{CallbackPath}";
                _logger.LogInformation($"Listening for Spotify OAuth callback: {_redirectUri}");

                var authUrl = BuildAuthorizationUrl(codeChallenge, _redirectUri);
                _logger.LogInformation($"Spotify authorization URL: {authUrl}");
                OnAuthorizationUrlReady?.Invoke(authUrl);
                OnStatusChanged?.Invoke("请在界面中打开 Spotify 授权页面...");

                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);
                    var callbackUrl = await WaitForLoopbackCallbackAsync(listener, linkedCts.Token).ConfigureAwait(false);
                    _logger.LogInformation($"OAuth callback received: {callbackUrl}");
                    await ProcessCallbackUrlAsync(callbackUrl).ConfigureAwait(false);
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
            }
        }

        private TcpListener TryStartLoopbackListener(out int selectedPort)
        {
            foreach (var port in CallbackPorts)
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                try
                {
                    listener.Start();
                    selectedPort = port;
                    return listener;
                }
                catch (SocketException ex)
                {
                    _logger.LogWarning($"Spotify OAuth callback port {port} unavailable: {ex.Message}");
                    try
                    {
                        listener.Stop();
                    }
                    catch { }
                }
            }

            selectedPort = 0;
            _logger.LogError($"All Spotify OAuth callback ports are unavailable: {string.Join(", ", CallbackPorts)}");
            return null;
        }

        private string BuildAuthorizationUrl(string codeChallenge, string redirectUri)
        {
            var scope = string.Join(" ", Scopes);
            return $"{AuthorizeUrl}" +
                $"?client_id={Uri.EscapeDataString(_clientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&code_challenge={codeChallenge}" +
                $"&code_challenge_method=S256" +
                $"&state={_state}";
        }

        private async Task<string> WaitForLoopbackCallbackAsync(TcpListener listener, CancellationToken token)
        {
            using var stopRegistration = token.Register(() => listener.Stop());

            while (true)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }
                catch (SocketException) when (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }

                using (client)
                {
                    token.ThrowIfCancellationRequested();
                    var request = await ReadHttpRequestAsync(client, token).ConfigureAwait(false);
                    var callbackUrl = TryExtractCallbackUrl(request);

                    if (!string.IsNullOrEmpty(callbackUrl))
                    {
                        await WriteHttpResponseAsync(
                            client,
                            "Spotify authorization complete. You can return to OmniMixPlayer.",
                            token).ConfigureAwait(false);
                        return callbackUrl;
                    }

                    await WriteHttpResponseAsync(
                        client,
                        "OmniMixPlayer Spotify OAuth callback endpoint.",
                        token).ConfigureAwait(false);
                }
            }
        }

        private static async Task<string> ReadHttpRequestAsync(TcpClient client, CancellationToken token)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            using var ms = new MemoryStream();

            while (ms.Length < 8192)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                if (read <= 0)
                    break;

                ms.Write(buffer, 0, read);
                var text = Encoding.UTF8.GetString(ms.ToArray());
                if (text.Contains("\r\n\r\n"))
                    return text;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string TryExtractCallbackUrl(string request)
        {
            if (string.IsNullOrEmpty(request))
                return null;

            var firstLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
            var firstLine = firstLineEnd >= 0 ? request.Substring(0, firstLineEnd) : request;
            var parts = firstLine.Split(' ');
            if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
                return null;

            var target = parts[1];
            if (!target.StartsWith(CallbackPath, StringComparison.OrdinalIgnoreCase))
                return null;

            return $"http://{LoopbackHost}{target}";
        }

        private static async Task WriteHttpResponseAsync(TcpClient client, string message, CancellationToken token)
        {
            var body = "<!doctype html><meta charset=\"utf-8\"><title>OmniMixPlayer Spotify</title>" +
                $"<body style=\"font-family: sans-serif; padding: 32px;\">{WebUtility.HtmlEncode(message)}</body>";
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var headerBytes = Encoding.UTF8.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Connection: close\r\n\r\n");

            var stream = client.GetStream();
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token).ConfigureAwait(false);
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length, token).ConfigureAwait(false);
        }

        private async Task ProcessCallbackUrlAsync(string callbackUrl)
        {
            OnStatusChanged?.Invoke("收到授权回调，正在验证...");

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
            await ExchangeCodeAsync(code).ConfigureAwait(false);
        }

        private async Task ExchangeCodeAsync(string code)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _redirectUri,
                ["client_id"] = _clientId,
                ["code_verifier"] = _codeVerifier
            });

            try
            {
                var response = await _httpClient.PostAsync(TokenUrl, content).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Token exchange failed ({response.StatusCode}): {json}");
                    OnLoginFailed?.Invoke("Token 交换失败");
                    return;
                }

                var tokenResponse = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);
                _logger.LogInformation("Successfully obtained Spotify tokens");
                OnTokenReceived?.Invoke(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Token exchange error: {ex.Message}");
                OnLoginFailed?.Invoke($"Token 交换异常: {ex.Message}");
            }
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        private static Dictionary<string, string> ParseQueryString(string url)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var queryStart = url.IndexOf('?');
            if (queryStart < 0)
                return result;

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
