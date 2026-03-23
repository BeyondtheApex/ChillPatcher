using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChillPatcher.Module.Spotify
{
    /// <summary>
    /// Spotify Web API 客户端，封装所有 HTTP 调用。
    /// 支持 token 自动刷新（401 时重试一次）。
    /// </summary>
    public class SpotifyBridge : IDisposable
    {
        private const string ApiBase = "https://api.spotify.com/v1";
        private const string TokenUrl = "https://accounts.spotify.com/api/token";

        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _dataPath;
        private readonly ManualLogSource _logger;

        private SpotifySession _session;

        public bool IsLoggedIn => _session != null && _session.IsValid;
        public SpotifySession Session => _session;

        public SpotifyBridge(string clientId, string dataPath, ManualLogSource logger)
        {
            _clientId = clientId;
            _dataPath = dataPath;
            _logger = logger;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // =====================================================================
        // Session 管理
        // =====================================================================

        public void LoadSession()
        {
            var path = GetSessionPath();
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path);
                _session = JsonConvert.DeserializeObject<SpotifySession>(json);
                _logger.LogInfo($"Loaded Spotify session for {_session?.DisplayName ?? "unknown"}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to load session: {ex.Message}");
                _session = null;
            }
        }

        public void SaveSession()
        {
            if (_session == null) return;
            try
            {
                Directory.CreateDirectory(_dataPath);
                File.WriteAllText(GetSessionPath(), JsonConvert.SerializeObject(_session, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save session: {ex.Message}");
            }
        }

        public void ClearSession()
        {
            _session = null;
            var path = GetSessionPath();
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }

        public void SetTokens(SpotifyTokenResponse tokenResponse)
        {
            if (_session == null) _session = new SpotifySession();
            _session.AccessToken = tokenResponse.AccessToken;
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                _session.RefreshToken = tokenResponse.RefreshToken;
            _session.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            SaveSession();
        }

        private string GetSessionPath() => Path.Combine(_dataPath, "spotify_session.json");

        // =====================================================================
        // Token 刷新
        // =====================================================================

        public async Task<bool> RefreshTokenAsync()
        {
            if (_session == null || string.IsNullOrEmpty(_session.RefreshToken))
                return false;

            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = _session.RefreshToken,
                    ["client_id"] = _clientId
                });

                var response = await _httpClient.PostAsync(TokenUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Token refresh failed ({response.StatusCode}): {json}");
                    return false;
                }

                var tokenResponse = JsonConvert.DeserializeObject<SpotifyTokenResponse>(json);
                SetTokens(tokenResponse);
                _logger.LogInfo("Spotify token refreshed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Token refresh error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 确保 token 有效，过期则自动刷新。
        /// </summary>
        public async Task<bool> EnsureTokenValidAsync()
        {
            if (_session == null || !_session.IsValid) return false;
            if (!_session.IsExpired) return true;
            return await RefreshTokenAsync();
        }

        // =====================================================================
        // User
        // =====================================================================

        public async Task<SpotifyUser> GetCurrentUserAsync()
        {
            return await GetAsync<SpotifyUser>("/me");
        }

        // =====================================================================
        // Playlists
        // =====================================================================

        /// <summary>
        /// 获取用户所有歌单（自动分页）。
        /// </summary>
        public async Task<List<SpotifyPlaylist>> GetUserPlaylistsAsync()
        {
            _logger.LogInfo("Fetching user playlists...");
            var all = new List<SpotifyPlaylist>();
            int offset = 0;
            const int limit = 50;

            while (true)
            {
                var page = await GetAsync<SpotifyPaged<SpotifyPlaylist>>($"/me/playlists?limit={limit}&offset={offset}");
                if (page?.Items == null || page.Items.Count == 0) break;

                all.AddRange(page.Items);
                _logger.LogInfo($"  Fetched {all.Count}/{page.Total} playlists");
                if (string.IsNullOrEmpty(page.Next)) break;
                offset += limit;
            }

            _logger.LogInfo($"Total playlists fetched: {all.Count}");
            return all;
        }

        /// <summary>
        /// 获取歌单中的所有曲目（自动分页）。
        /// </summary>
        public async Task<List<SpotifyTrack>> GetPlaylistTracksAsync(string playlistId)
        {
            _logger.LogInfo($"Fetching tracks for playlist {playlistId}...");
            var tracks = new List<SpotifyTrack>();
            int offset = 0;
            const int limit = 100;

            while (true)
            {
                var page = await GetAsync<SpotifyPaged<SpotifyPlaylistTrackItem>>(
                    $"/playlists/{playlistId}/tracks?limit={limit}&offset={offset}&fields=items(track(id,name,uri,duration_ms,explicit,popularity,artists(id,name),album(id,name,uri,images,release_date))),total,limit,offset,next");

                if (page?.Items == null || page.Items.Count == 0) break;

                foreach (var item in page.Items)
                {
                    // 过滤掉 null track（已删除或不可用的曲目）
                    if (item?.Track != null && !string.IsNullOrEmpty(item.Track.Id))
                        tracks.Add(item.Track);
                }

                if (string.IsNullOrEmpty(page.Next)) break;
                offset += limit;
            }

            return tracks;
        }

        // =====================================================================
        // Saved Tracks (用户收藏的音乐)
        // =====================================================================

        /// <summary>
        /// 获取用户收藏的曲目（"Liked Songs"），自动分页。
        /// </summary>
        public async Task<List<SpotifyTrack>> GetSavedTracksAsync(int maxCount = 500)
        {
            var tracks = new List<SpotifyTrack>();
            int offset = 0;
            const int limit = 50;

            while (tracks.Count < maxCount)
            {
                var page = await GetAsync<SpotifyPaged<SpotifySavedTrackItem>>($"/me/tracks?limit={limit}&offset={offset}");
                if (page?.Items == null || page.Items.Count == 0) break;

                foreach (var item in page.Items)
                {
                    if (item?.Track != null && !string.IsNullOrEmpty(item.Track.Id))
                        tracks.Add(item.Track);
                }

                if (string.IsNullOrEmpty(page.Next)) break;
                offset += limit;
            }

            return tracks;
        }

        /// <summary>
        /// 检查曲目是否在用户收藏中。
        /// </summary>
        public async Task<Dictionary<string, bool>> CheckSavedTracksAsync(List<string> trackIds)
        {
            var result = new Dictionary<string, bool>();
            // API 最多 50 个 ID
            for (int i = 0; i < trackIds.Count; i += 50)
            {
                var batch = trackIds.GetRange(i, Math.Min(50, trackIds.Count - i));
                var ids = string.Join(",", batch);
                var checks = await GetAsync<List<bool>>($"/me/tracks/contains?ids={ids}");

                if (checks != null)
                {
                    for (int j = 0; j < batch.Count && j < checks.Count; j++)
                        result[batch[j]] = checks[j];
                }
            }
            return result;
        }

        public async Task<bool> SaveTracksAsync(List<string> trackIds)
        {
            var body = JsonConvert.SerializeObject(new { ids = trackIds });
            return await PutAsync("/me/tracks", body);
        }

        public async Task<bool> RemoveTracksAsync(List<string> trackIds)
        {
            var body = JsonConvert.SerializeObject(new { ids = trackIds });
            return await DeleteAsync("/me/tracks", body);
        }

        // =====================================================================
        // Player (Spotify Connect)
        // =====================================================================

        public async Task<SpotifyPlaybackState> GetPlaybackStateAsync()
        {
            // 可能返回 204 No Content（无活跃设备）
            return await GetAsync<SpotifyPlaybackState>("/me/player", allowEmpty: true);
        }

        public async Task<List<SpotifyDevice>> GetAvailableDevicesAsync()
        {
            var resp = await GetAsync<SpotifyDevicesResponse>("/me/player/devices");
            return resp?.Devices ?? new List<SpotifyDevice>();
        }

        /// <summary>
        /// 播放指定曲目。
        /// </summary>
        /// <param name="trackUri">例如 "spotify:track:4iV5W9uYEdYUVa79Axb7Rh"</param>
        /// <param name="deviceId">目标设备 ID，null 则使用当前活跃设备</param>
        public async Task<bool> PlayTrackAsync(string trackUri, string deviceId = null)
        {
            var url = "/me/player/play";
            if (!string.IsNullOrEmpty(deviceId))
                url += $"?device_id={deviceId}";

            var body = JsonConvert.SerializeObject(new
            {
                uris = new[] { trackUri },
                position_ms = 0
            });

            return await PutAsync(url, body);
        }

        /// <summary>
        /// 在某个播放上下文中播放（如歌单内顺序播放）。
        /// </summary>
        public async Task<bool> PlayContextAsync(string contextUri, string trackUri = null, string deviceId = null)
        {
            var url = "/me/player/play";
            if (!string.IsNullOrEmpty(deviceId))
                url += $"?device_id={deviceId}";

            var bodyObj = new JObject { ["context_uri"] = contextUri };
            if (!string.IsNullOrEmpty(trackUri))
                bodyObj["offset"] = new JObject { ["uri"] = trackUri };

            return await PutAsync(url, bodyObj.ToString());
        }

        public async Task<bool> PauseAsync()
        {
            return await PutAsync("/me/player/pause", null);
        }

        public async Task<bool> ResumeAsync()
        {
            return await PutAsync("/me/player/play", null);
        }

        public async Task<bool> SkipNextAsync()
        {
            return await PostAsync("/me/player/next");
        }

        public async Task<bool> SkipPreviousAsync()
        {
            return await PostAsync("/me/player/previous");
        }

        public async Task<bool> SeekAsync(int positionMs)
        {
            return await PutAsync($"/me/player/seek?position_ms={positionMs}", null);
        }

        public async Task<bool> SetVolumeAsync(int volumePercent)
        {
            volumePercent = Math.Max(0, Math.Min(100, volumePercent));
            return await PutAsync($"/me/player/volume?volume_percent={volumePercent}", null);
        }

        public async Task<bool> SetRepeatAsync(string state)
        {
            // state: "off", "context", "track"
            return await PutAsync($"/me/player/repeat?state={state}", null);
        }

        public async Task<bool> SetShuffleAsync(bool enabled)
        {
            return await PutAsync($"/me/player/shuffle?state={enabled.ToString().ToLower()}", null);
        }

        public async Task<bool> TransferPlaybackAsync(string deviceId, bool play = true)
        {
            var body = JsonConvert.SerializeObject(new
            {
                device_ids = new[] { deviceId },
                play
            });
            return await PutAsync("/me/player", body);
        }

        // =====================================================================
        // HTTP 辅助方法（带 401 自动刷新重试）
        // =====================================================================

        private async Task<T> GetAsync<T>(string endpoint, bool allowEmpty = false) where T : class
        {
            return await SendWithRetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + endpoint);
                SetAuthHeader(request);
                return await _httpClient.SendAsync(request);
            }, allowEmpty) is string json ? JsonConvert.DeserializeObject<T>(json) : default;
        }

        private async Task<bool> PutAsync(string endpoint, string body)
        {
            return await SendWithRetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, ApiBase + endpoint);
                SetAuthHeader(request);
                if (body != null)
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                return await _httpClient.SendAsync(request);
            }, allowEmpty: true) != null;
        }

        private async Task<bool> PostAsync(string endpoint, string body = null)
        {
            return await SendWithRetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, ApiBase + endpoint);
                SetAuthHeader(request);
                if (body != null)
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                return await _httpClient.SendAsync(request);
            }, allowEmpty: true) != null;
        }

        private async Task<bool> DeleteAsync(string endpoint, string body)
        {
            return await SendWithRetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, ApiBase + endpoint);
                SetAuthHeader(request);
                if (body != null)
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                return await _httpClient.SendAsync(request);
            }, allowEmpty: true) != null;
        }

        /// <summary>
        /// 发送请求，401 时自动刷新 token 并重试一次。
        /// 返回响应 body 字符串，或空请求返回 ""。失败返回 null。
        /// </summary>
        private async Task<string> SendWithRetryAsync(Func<Task<HttpResponseMessage>> requestFunc, bool allowEmpty = false)
        {
            if (!await EnsureTokenValidAsync())
            {
                _logger.LogWarning("No valid token available");
                return null;
            }

            var response = await requestFunc();

            // 401: 尝试刷新 token 后重试
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInfo("Got 401, attempting token refresh...");
                if (await RefreshTokenAsync())
                {
                    response = await requestFunc();
                }
                else
                {
                    _logger.LogError("Token refresh failed, user needs to re-login");
                    return null;
                }
            }

            // 204 No Content
            if (response.StatusCode == HttpStatusCode.NoContent)
                return allowEmpty ? "" : null;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Spotify API error {(int)response.StatusCode}: {errorBody}");
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }

        private void SetAuthHeader(HttpRequestMessage request)
        {
            if (_session != null && !string.IsNullOrEmpty(_session.AccessToken))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
