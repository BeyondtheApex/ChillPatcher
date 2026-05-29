using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using OmniMixPlayer.SDK.Attributes;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;
using Newtonsoft.Json;



namespace OmniMixPlayer.Module.Spotify
{
    [MusicModule("com.chillpatcher.spotify", "Spotify",
        Version = "1.0.0",
        Author = "ChillPatcher",
        Description = "Spotify Connect playback control and playlist sync")]
    public class SpotifyModule : IMusicModule, IStreamingMusicSourceProvider, IModuleAudioDecoderProvider, ICoverProvider, IFavoriteExcludeHandler, IModuleUIProvider
    {
        public string ModuleId => "com.chillpatcher.spotify";
        public string DisplayName => "Spotify";
        public string Version => "1.0.0";
        public int Priority => 20;

        public ModuleCapabilities Capabilities => new ModuleCapabilities
        {
            CanDelete = false,
            CanFavorite = true,   // 收藏持久化在 Spotify 服务端
            CanExclude = false,
            SupportsLiveUpdate = false,
            ProvidesCover = true,
            ProvidesAlbum = true
        };

        public MusicSourceType SourceType => MusicSourceType.Stream;
        public bool IsReady => _bridge != null && _bridge.IsLoggedIn;
        public event Action<bool> OnReadyStateChanged;

        private IModuleContext _context;
        private ILogger _logger;
        private SpotifyBridge _bridge;
        private OAuthManager _oauthManager;
        private SpotifySongRegistry _registry;

        private string _dataPath;

        // 收藏状态缓存 (Spotify track ID -> saved)，初始数据来自 Liked Songs 加载
        private readonly Dictionary<string, bool> _savedCache = new Dictionary<string, bool>();

        // OAuth 登录进行中标志
        private bool _isLoggingIn;

        // Client ID 未配置标志
        private bool _needsClientId;

        // 当前选定的设备
        private string _activeDeviceId;
        private string _activeDeviceName;
        private bool _nativeBridgeLoaded;

        // 事件订阅
        private IDisposable _pauseSubscription;

        // =====================================================================
        // 生命周期
        // =====================================================================

        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            _logger = context.Logger;
            _logger.LogInformation("Spotify module initializing...");

            // 数据目录
            _dataPath = context.GetModuleDataPath(ModuleId);
            Directory.CreateDirectory(_dataPath);

            // 读取配置
            var clientId = _context.ConfigManager.GetString("ClientId", "YOUR_SPOTIFY_CLIENT_ID");
            _logger.LogInformation($"Config loaded: ClientId={(string.IsNullOrEmpty(clientId) ? "(empty)" : clientId.Substring(0, Math.Min(8, clientId.Length)) + "...")}");

            if (string.IsNullOrEmpty(clientId) || clientId == "YOUR_SPOTIFY_CLIENT_ID")
            {
                _logger.LogWarning("Spotify Client ID not configured. Will prompt on first play.");
                _needsClientId = true;
                _registry = new SpotifySongRegistry(_context, ModuleId);
                OnReadyStateChanged?.Invoke(false);
                return;
            }

            // 初始化组件
            await InitWithClientIdAsync();
        }

        private async Task InitWithClientIdAsync()
        {
            var clientId = _context.ConfigManager.GetString("ClientId", "YOUR_SPOTIFY_CLIENT_ID");
            _bridge = new SpotifyBridge(clientId, _dataPath, _logger);
            _registry = new SpotifySongRegistry(_context, ModuleId);

            InitOAuthManager();
            SubscribePlaybackEvents();
            _nativeBridgeLoaded = _context.DependencyLoader.LoadNativeLibrary(
                "SpotifyLibrespotBridge.dll",
                ModuleId);
            if (!_nativeBridgeLoaded)
                _logger.LogWarning("[Spotify] SpotifyLibrespotBridge.dll not loaded; native Spotify decode is disabled");

            // 加载已保存的 session
            _bridge.LoadSession();

            if (_bridge.IsLoggedIn)
            {
                // 尝试刷新 token 并加载歌单
                if (await _bridge.EnsureTokenValidAsync())
                {
                    var user = await _bridge.GetCurrentUserAsync();
                    if (user != null)
                    {
                        _bridge.Session.UserId = user.Id;
                        _bridge.Session.DisplayName = user.DisplayName;
                        _bridge.Session.Product = user.Product;
                        _bridge.SaveSession();

                        _logger.LogInformation($"Spotify logged in as {user.DisplayName} ({user.Product})");

                        if (!_bridge.Session.IsPremium)
                            _logger.LogWarning("Spotify Free account - playback control requires Premium");

                        await LoadPlaylistsAsync();
                        OnReadyStateChanged?.Invoke(true);
                        return;
                    }
                }

                _logger.LogWarning("Spotify session expired, clearing");
                _bridge.ClearSession();
            }

            // 未登录
            OnReadyStateChanged?.Invoke(false);
        }

        public void OnEnable() { }

        public void OnDisable() { }

        public void OnUnload()
        {
            _pauseSubscription?.Dispose();
            _oauthManager?.Dispose();
            _bridge?.Dispose();
            _savedCache.Clear();
        }

        // =====================================================================
        // 配置
        // =====================================================================

        // Config via OmniMixPlayer config file (SDK IModuleConfigManager)
        // ClientId = _context.ConfigManager.GetString("ClientId", "YOUR_SPOTIFY_CLIENT_ID")

        // =====================================================================
        // OAuth
        // =====================================================================

        private void InitOAuthManager()
        {
            var clientId = _context.ConfigManager.GetString("ClientId", "YOUR_SPOTIFY_CLIENT_ID");
            _oauthManager = new OAuthManager(clientId, _dataPath, _logger);

            _oauthManager.OnStatusChanged += (status) =>
            {
                _logger.LogInformation($"OAuth status: {status}");
                PushUI?.Invoke(BuildUI());
            };

            _oauthManager.OnTokenReceived += async (tokenResponse) =>
            {
                PushUI?.Invoke(BuildUI());

                _bridge.SetTokens(tokenResponse);

                // 获取用户信息
                var user = await _bridge.GetCurrentUserAsync();
                if (user != null)
                {
                    _bridge.Session.UserId = user.Id;
                    _bridge.Session.DisplayName = user.DisplayName;
                    _bridge.Session.Product = user.Product;
                    _bridge.SaveSession();
                    _logger.LogInformation($"Logged in as {user.DisplayName} ({user.Product})");
                }

                // 加载歌单
                await LoadPlaylistsAsync();

                _isLoggingIn = false;
                OnReadyStateChanged?.Invoke(true);

                // 推送登录成功后的 UI
                PushUI?.Invoke(BuildUI());
            };

            _oauthManager.OnLoginFailed += (error) =>
            {
                _logger.LogError($"Login failed: {error}");
                _isLoggingIn = false;
                PushUI?.Invoke(BuildUI());
            };
        }

        private void SubscribePlaybackEvents()
        {
            _pauseSubscription?.Dispose();
            _pauseSubscription = _context.EventBus.Subscribe<PlayPausedEvent>(async e =>
            {
                // 只处理属于本模块的歌曲
                if (e.Music == null || e.Music.ModuleId != ModuleId) return;
                if (_bridge == null || !_bridge.IsLoggedIn || !_bridge.Session.IsPremium) return;

                try
                {
                    if (e.IsPaused)
                    {
                        _logger.LogInformation("[Spotify] Game paused → pausing Spotify");
                        await _bridge.PauseAsync();
                    }
                    else
                    {
                        _logger.LogInformation("[Spotify] Game resumed → resuming Spotify");
                        await _bridge.ResumeAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Spotify] Failed to sync pause state: {ex.Message}");
                }
            });
            _logger.LogInformation("[Spotify] Subscribed to PlayPausedEvent");
        }

        // =====================================================================
        // 歌单加载
        // =====================================================================

        private async Task LoadPlaylistsAsync()
        {
            try
            {
                // 加载 Liked Songs
                _logger.LogInformation("Loading Liked Songs...");
                var likedTracks = await _bridge.GetSavedTracksAsync(500);
                if (likedTracks.Count > 0)
                {
                    _registry.RegisterLikedSongs(likedTracks);
                    foreach (var t in likedTracks)
                        _savedCache[t.Id] = true;
                }

                // 加载用户歌单
                _logger.LogInformation("Loading playlists...");
                var playlists = await _bridge.GetUserPlaylistsAsync();
                foreach (var playlist in playlists)
                {
                    try
                    {
                        var tracks = await _bridge.GetPlaylistTracksAsync(playlist.Id);
                        if (tracks.Count > 0)
                            _registry.RegisterPlaylist(playlist, tracks);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to load playlist '{playlist.Name}': {ex.Message}");
                    }
                }

                _logger.LogInformation($"Loaded {playlists.Count} playlists");

                // 加载可用设备
                await RefreshDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load playlists: {ex.Message}");
            }
        }

        private async Task RefreshDevicesAsync()
        {
            try
            {
                var devices = await _bridge.GetAvailableDevicesAsync();
                _logger.LogInformation($"[Spotify] Found {devices.Count} devices");

                // 自动选中活跃设备
                if (devices.Count > 0)
                {
                    var active = devices.Find(d => d.IsActive);
                    if (active != null)
                    {
                        _activeDeviceId = active.Id;
                        _activeDeviceName = active.Name;
                    }
                    else if (string.IsNullOrEmpty(_activeDeviceId))
                    {
                        // 没有活跃设备，选第一个
                        _activeDeviceId = devices[0].Id;
                        _activeDeviceName = devices[0].Name;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Spotify] Failed to load devices: {ex.Message}");
            }
        }

        // =====================================================================
        // IStreamingMusicSourceProvider
        // =====================================================================

        public Task<List<MusicInfo>> GetMusicListAsync()
        {
            var all = _context.MusicRegistry.GetMusicByModule(ModuleId);
            return Task.FromResult(all?.ToList() ?? new List<MusicInfo>());
        }

        public void UnloadAudio(string uuid) { }

        public async Task RefreshAsync()
        {
            if (!_bridge.IsLoggedIn) return;

            _registry.UnregisterAll();
            _savedCache.Clear();
            await LoadPlaylistsAsync();
        }

        // =====================================================================
        // IModuleAudioDecoderProvider (librespot pipe -> PCM shared memory)
        // =====================================================================

        public bool CanDecode(string uuid)
        {
            if (!UseNativeLibrespotDecoder() || !_nativeBridgeLoaded || _bridge == null || !_bridge.IsLoggedIn || !_bridge.Session.IsPremium)
                return false;

            var music = _context?.MusicRegistry?.GetMusic(uuid);
            return GetTrackMeta(music) != null;
        }

        public async Task<IPcmStreamReader> CreateDecoderAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default)
        {
            var music = _context.MusicRegistry.GetMusic(uuid);
            var meta = GetTrackMeta(music);
            if (music == null || meta == null || string.IsNullOrEmpty(meta.SpotifyUri))
                return null;

            if (!UseNativeLibrespotDecoder())
            {
                _logger.LogInformation("[Spotify] Native librespot decoder disabled by config; using Spotify Connect fallback");
                return null;
            }

            if (!_nativeBridgeLoaded)
            {
                _logger.LogWarning("[Spotify] SpotifyLibrespotBridge.dll is not loaded; falling back to Spotify Connect silent reader");
                return null;
            }

            var deviceName = _context.ConfigManager.GetString(
                "LibrespotDeviceName",
                $"OmniMixPlayer-{Environment.ProcessId}");
            var cacheDir = Path.Combine(_dataPath, "librespot-cache");

            var reader = new NativeLibrespotPcmReader(
                _bridge.Session.AccessToken,
                deviceName,
                cacheDir,
                music.Duration,
                _logger);

            try
            {
                if (!reader.Start())
                {
                    reader.Dispose();
                    return null;
                }

                if (!reader.WaitForReady(15000, cancellationToken))
                {
                    _logger.LogWarning("[Spotify] native librespot bridge did not become ready: {DeviceName}", deviceName);
                    reader.Dispose();
                    return null;
                }

                var deviceId = await WaitForLibrespotDeviceAsync(deviceName, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(deviceId))
                {
                    _logger.LogWarning("[Spotify] native librespot device did not appear: {DeviceName}", deviceName);
                    reader.Dispose();
                    return null;
                }

                await _bridge.TransferPlaybackAsync(deviceId, play: false).ConfigureAwait(false);
                var ok = await _bridge.PlayTrackAsync(meta.SpotifyUri, deviceId).ConfigureAwait(false);
                if (!ok)
                {
                    _logger.LogWarning("[Spotify] Failed to start librespot playback: {Title}", music.Title);
                    reader.Dispose();
                    return null;
                }

                _activeDeviceId = deviceId;
                _activeDeviceName = deviceName;
                _logger.LogInformation("[Spotify] native librespot decoder started: {Title}", music.Title);
                return reader;
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }

        private async Task<string> WaitForLibrespotDeviceAsync(string deviceName, CancellationToken cancellationToken)
        {
            for (int i = 0; i < 30; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var devices = await _bridge.GetAvailableDevicesAsync().ConfigureAwait(false);
                var device = devices.FirstOrDefault(d =>
                    string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
                if (device != null)
                    return device.Id;
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            return null;
        }

        private bool UseNativeLibrespotDecoder()
        {
            var backend = _context?.ConfigManager?.GetString("PlaybackBackend", "native") ?? "native";
            if (backend.Equals("connect", StringComparison.OrdinalIgnoreCase) ||
                backend.Equals("spotify_connect", StringComparison.OrdinalIgnoreCase) ||
                backend.Equals("client", StringComparison.OrdinalIgnoreCase))
                return false;

            return _context?.ConfigManager?.GetBool("UseNativeLibrespotDecoder", true) ?? true;
        }

        // =====================================================================
        // IPlayableSourceResolver (Spotify Connect 播放)
        // =====================================================================

        public async Task<PlayableSource> ResolveAsync(string uuid, AudioQuality quality = AudioQuality.ExHigh, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[Spotify] ResolveAsync called: uuid={uuid}");

            // 常规歌曲：通过 Spotify Connect 播放
            var music = _context.MusicRegistry.GetMusic(uuid);
            if (music == null)
            {
                _logger.LogWarning($"[Spotify] Music not found: {uuid}");
                return null;
            }

            var meta = GetTrackMeta(music);
            if (meta == null || string.IsNullOrEmpty(meta.SpotifyUri))
            {
                _logger.LogWarning($"[Spotify] No Spotify URI for: {music.Title}");
                return null;
            }

            if (!_bridge.Session.IsPremium)
            {
                _logger.LogWarning("[Spotify] Playback control requires Spotify Premium");
                return PlayableSource.FromPcmStream(uuid, new SilentPcmReader(music.Duration), AudioFormat.Mp3);
            }

            // 如果没有选定设备，尝试自动检测
            if (string.IsNullOrEmpty(_activeDeviceId))
            {
                var devices = await _bridge.GetAvailableDevicesAsync();
                var active = devices.Find(d => d.IsActive);
                if (active != null)
                {
                    _activeDeviceId = active.Id;
                    _logger.LogInformation($"[Spotify] Auto-detected active device: {active.Name}");
                }
                else if (devices.Count > 0)
                {
                    _activeDeviceId = devices[0].Id;
                    _logger.LogInformation($"[Spotify] Using first available device: {devices[0].Name}");
                    await _bridge.TransferPlaybackAsync(_activeDeviceId, play: false);
                }
                else
                {
                    _logger.LogWarning("[Spotify] No Spotify devices found. Please open Spotify and select a device.");
                    return null;
                }
            }

            _logger.LogInformation($"[Spotify] Playing: {music.Title} on device {_activeDeviceId}");
            var playSuccess = await _bridge.PlayTrackAsync(meta.SpotifyUri, _activeDeviceId);
            if (playSuccess)
                _logger.LogInformation($"[Spotify] Playback started: {music.Title}");
            else
                _logger.LogWarning($"[Spotify] Failed to start playback: {music.Title}");

            // 返回静默 PCM（音频由 Spotify 客户端播放）
            return PlayableSource.FromPcmStream(uuid, new SilentPcmReader(music.Duration), AudioFormat.Mp3);
        }

        public Task<PlayableSource> RefreshUrlAsync(string uuid, AudioQuality quality = AudioQuality.ExHigh, CancellationToken cancellationToken = default)
        {
            return ResolveAsync(uuid, quality, cancellationToken);
        }

        // =====================================================================
        // ICoverProvider
        // =====================================================================

        public async Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid)
        {
            var music = _context.MusicRegistry.GetMusic(uuid);
            if (music == null)
                return (_context.DefaultCover.DefaultMusicCover, "image/png");

            var meta = GetTrackMeta(music);
            if (meta == null || string.IsNullOrEmpty(meta.CoverUrl))
                return (_context.DefaultCover.DefaultMusicCover, "image/png");

            var result = await DownloadCoverAsync(meta.CoverUrl);
            return result ?? (_context.DefaultCover.DefaultMusicCover, "image/png");
        }

        public async Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId)
        {
            var album = _context.AlbumRegistry.GetAlbum(albumId);
            if (album?.ExtendedData is string coverUrl && !string.IsNullOrEmpty(coverUrl))
            {
                var result = await DownloadCoverAsync(coverUrl);
                return result ?? (_context.DefaultCover.DefaultAlbumCover, "image/png");
            }

            return (_context.DefaultCover.DefaultAlbumCover, "image/png");
        }

        public void RemoveMusicCoverCache(string uuid) { }
        public void RemoveAlbumCoverCache(string albumId) { }
        public void ClearCache() { }

        private async Task<(byte[] data, string mimeType)?> DownloadCoverAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var data = await httpClient.GetByteArrayAsync(url);
                return (data, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to download cover: {ex.Message}");
                return null;
            }
        }

        // =====================================================================
        // IFavoriteExcludeHandler — 收藏持久化在 Spotify 服务端
        // =====================================================================

        public bool IsFavorite(string uuid)
        {
            var meta = GetTrackMetaByUuid(uuid);
            if (meta == null) return false;
            return _savedCache.TryGetValue(meta.SpotifyId, out var saved) && saved;
        }

        public async void SetFavorite(string uuid, bool isFavorite)
        {
            var meta = GetTrackMetaByUuid(uuid);
            if (meta == null || _bridge == null || !_bridge.IsLoggedIn) return;

            bool success;
            if (isFavorite)
                success = await _bridge.SaveTracksAsync(new List<string> { meta.SpotifyId });
            else
                success = await _bridge.RemoveTracksAsync(new List<string> { meta.SpotifyId });

            if (success)
            {
                _savedCache[meta.SpotifyId] = isFavorite;
                var music = _context.MusicRegistry.GetMusic(uuid);
                if (music != null) music.IsFavorite = isFavorite;
                _logger.LogInformation($"[Spotify] Favorite {(isFavorite ? "saved" : "removed")}: {music?.Title}");
            }
            else
            {
                _logger.LogWarning($"[Spotify] Failed to {(isFavorite ? "save" : "remove")} favorite: {uuid}");
            }
        }

        public bool IsExcluded(string uuid) => false;
        public void SetExcluded(string uuid, bool isExcluded) { }

        public IReadOnlyList<string> GetFavorites()
        {
            var result = new List<string>();
            foreach (var kvp in _savedCache)
            {
                if (!kvp.Value) continue;
                var uuid = MusicInfo.GenerateUUID($"spotify_{kvp.Key}");
                result.Add(uuid);
            }
            return result;
        }

        public IReadOnlyList<string> GetExcluded() => new List<string>();

        // =====================================================================
        // 辅助方法
        // =====================================================================

        private SpotifyTrackMeta GetTrackMeta(MusicInfo music)
        {
            if (music?.ExtendedData == null) return null;

            if (music.ExtendedData is SpotifyTrackMeta meta)
                return meta;

            // 可能从 JSON 反序列化为 JObject
            try
            {
                return JsonConvert.DeserializeObject<SpotifyTrackMeta>(
                    JsonConvert.SerializeObject(music.ExtendedData));
            }
            catch
            {
                return null;
            }
        }

        private SpotifyTrackMeta GetTrackMetaByUuid(string uuid)
        {
            var music = _context.MusicRegistry.GetMusic(uuid);
            return GetTrackMeta(music);
        }

        // =====================================================================
        // IModuleUIProvider
        // =====================================================================

        public Action<SlintNode> PushUI { get; set; }

        public bool HasQuickLinks => _bridge?.IsLoggedIn ?? false;

        public IReadOnlyList<ModuleLinkEntry> GetQuickLinks()
        {
            return new List<ModuleLinkEntry>
            {
                new ModuleLinkEntry("devices", "选择设备", "speaker",
                    "#1db954", "#ffffff")
            };
        }

        public SlintNode BuildUI()
        {
            // 状态 1: Client ID 未配置
            if (_needsClientId)
            {
                return BuildConfigUI();
            }

            // 状态 2: 未登录
            if (!_bridge?.IsLoggedIn ?? true)
            {
                var statusText = _isLoggingIn ? "正在打开浏览器进行 Spotify 授权..." : "点击下方按钮登录 Spotify";
                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(SlintUi.Text("Spotify", fontSize: 18))
                    .AddChild(SlintUi.Text("未登录", fontSize: 14))
                    .AddChild(SlintUi.Text(statusText, fontSize: 12, color: "#94a3b8"))
                    .AddChild(
                        SlintUi.Button("login_btn", "登录 Spotify", variant: "primary")
                    );
            }

            // 状态 3: 已登录
            return BuildUIWithStatus();
        }

        public void HandleUIEvent(string nodeId, string action, string value)
        {
            _logger?.LogInformation(
                "[{DisplayName}] UI Event: node={NodeId}, action={Action}, value={Value}",
                DisplayName, nodeId, action, value);

            switch (nodeId)
            {
                case "login_btn":
                    if (!_isLoggingIn)
                    {
                        _isLoggingIn = true;
                        PushUI?.Invoke(BuildUI());
                        _ = Task.Run(() => _oauthManager.StartLoginAsync());
                    }
                    break;

                case "logout_btn":
                    _ = LogoutSpotifyAsync();
                    break;

                case "refresh_devices":
                    _ = RefreshDevicesAndPushUI();
                    break;

                case "config_client_id":
                    // Client ID 通常 32 字符，只有输入足够长才触发连接
                    if (!string.IsNullOrEmpty(value) && value.Length >= 20 && value != "YOUR_SPOTIFY_CLIENT_ID")
                    {
                        _context?.ConfigManager?.SetValue("ClientId", value);
                        _context?.ConfigManager?.Save();
                        _needsClientId = false;
                        PushUI?.Invoke(BuildUIWithStatus("正在连接..."));
                        _ = InitWithClientIdAsync();
                    }
                    break;
            }
        }

        private async Task RefreshDevicesAndPushUI()
        {
            await RefreshDevicesAsync();
            PushUI?.Invoke(BuildUI());
        }

        public SlintNode BuildLinkUI(string linkId)
        {
            if (linkId == "devices")
            {
                return BuildDeviceListUI();
            }
            return null;
        }

        public async void HandleLinkUIEvent(string linkId, string nodeId, string action, string value)
        {
            if (linkId == "devices")
            {
                if (nodeId == "select_device" && !string.IsNullOrEmpty(value))
                {
                    try
                    {
                        await _bridge.TransferPlaybackAsync(value, play: false);
                        var devices = await _bridge.GetAvailableDevicesAsync();
                        var selected = devices.Find(d => d.Id == value);
                        if (selected != null)
                        {
                            _activeDeviceId = selected.Id;
                            _activeDeviceName = selected.Name;
                        }
                        _logger?.LogInformation($"[Spotify] Device selected: {_activeDeviceName}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"[Spotify] Failed to select device: {ex.Message}");
                    }
                    PushUI?.Invoke(BuildUI());
                }
                else if (nodeId == "refresh_devices_link")
                {
                    await RefreshDevicesAndPushUI();
                }
            }
        }

        private SlintNode BuildConfigUI()
        {
            return SlintUi.Column(spacing: 16, padding: 20)
                .AddChild(SlintUi.Text("Spotify", fontSize: 18))
                .AddChild(SlintUi.Text("需要配置", fontSize: 14))
                .AddChild(SlintUi.Text("请在下方输入您的 Spotify Client ID", fontSize: 12, color: "#94a3b8"))
                .AddChild(
                    SlintUi.Input("config_client_id", "输入 Client ID...", inputType: "text")
                )
                .AddChild(SlintUi.Text("1. 访问 developer.spotify.com/dashboard 创建应用", fontSize: 10, color: "#94a3b8"))
                .AddChild(SlintUi.Text("2. 复制 Client ID 并粘贴到上方", fontSize: 10, color: "#94a3b8"))
                .AddChild(SlintUi.Text("3. 添加 Redirect URI: fullstop://callback", fontSize: 10, color: "#94a3b8"));
        }

        private SlintNode BuildUIWithStatus(string statusText = null)
        {
            var displayName = _bridge?.Session?.DisplayName ?? "Spotify 用户";
            var accountType = _bridge?.Session?.Product ?? "";
            var isPremium = _bridge?.Session?.IsPremium ?? false;
            var deviceName = _activeDeviceName ?? "未选择设备";

            var column = SlintUi.Column(spacing: 16, padding: 20)
                .AddChild(
                    SlintUi.Row(spacing: 12)
                        .AddChild(
                            SlintUi.Column(spacing: 4)
                                .AddChild(SlintUi.Text("Spotify", fontSize: 18))
                                .AddChild(SlintUi.Text("已登录", fontSize: 12, color: "#1db954"))
                        )
                )
                .AddChild(SlintUi.Text("账户", fontSize: 16))
                .AddChild(
                    SlintUi.Column(spacing: 4)
                        .AddChild(SlintUi.Text(displayName, fontSize: 14))
                        .AddChild(SlintUi.Text(
                            isPremium ? "Premium" : "Free (需要 Premium 才能控制播放)",
                            fontSize: 11,
                            color: isPremium ? "#1db954" : "#f59e0b"))
                )
                .AddChild(SlintUi.Text("播放设备", fontSize: 16))
                .AddChild(
                    SlintUi.Row(spacing: 8)
                        .AddChild(SlintUi.Text(deviceName, fontSize: 14))
                        .AddChild(SlintUi.Button("refresh_devices", "刷新", variant: null))
                )
                .AddChild(
                    SlintUi.Button("logout_btn", "退出登录", variant: "danger")
                );

            if (!string.IsNullOrEmpty(statusText))
            {
                column.AddChild(SlintUi.Text(statusText, fontSize: 12, color: "#94a3b8"));
            }

            return column;
        }

        private SlintNode BuildDeviceListUI()
        {
            // 尝试获取设备列表
            var devices = new List<SpotifyDevice>();
            try
            {
                devices = _bridge?.GetAvailableDevicesAsync().GetAwaiter().GetResult() ?? new List<SpotifyDevice>();
            }
            catch { }

            var column = SlintUi.Column(spacing: 16, padding: 20)
                .AddChild(SlintUi.Text("选择播放设备", fontSize: 18))
                .AddChild(SlintUi.Text("选择 Spotify 播放设备", fontSize: 12, color: "#94a3b8"));

            if (devices.Count == 0)
            {
                column.AddChild(SlintUi.Text("未找到设备。请打开 Spotify 客户端。", fontSize: 12, color: "#94a3b8"));
            }
            else
            {
                foreach (var device in devices)
                {
                    var isActive = device.Id == _activeDeviceId || device.IsActive;
                    var deviceLabel = device.Name ?? "Unknown";
                    if (device.VolumePercent.HasValue)
                        deviceLabel += $"  ·  Vol {device.VolumePercent}%";

                    column.AddChild(
                        SlintUi.Button("select_device_" + device.Id, deviceLabel,
                            variant: isActive ? "primary" : null)
                            .SetId("select_device")
                    );
                    // Store device id in value
                    // Note: The last button's value will be used
                    column.Children[column.Children.Count - 1].Value = device.Id;
                }
            }

            column.AddChild(
                SlintUi.Row(spacing: 8)
                    .AddChild(SlintUi.Button("refresh_devices_link", "刷新", variant: null))
            );

            return column;
        }

        public Task<byte[]> ServeRawContent(string path) => Task.FromResult<byte[]>(null);
        public string ServeRawContentType(string path) => null;

        private async Task LogoutSpotifyAsync()
        {
            try
            {
                _bridge?.ClearSession();
                _savedCache.Clear();
                _activeDeviceId = null;
                _activeDeviceName = null;
                _isLoggingIn = false;

                _context?.MusicRegistry?.UnregisterAllByModule(ModuleId);
                _context?.AlbumRegistry?.UnregisterAllByModule(ModuleId);
                _context?.TagRegistry?.UnregisterAllByModule(ModuleId);

                PushUI?.Invoke(BuildUI());

                _logger?.LogInformation("[{DisplayName}] 已退出登录", DisplayName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[{DisplayName}] 退出登录失败", DisplayName);
            }
        }

    }
}
