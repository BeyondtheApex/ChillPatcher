using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using OmniMixPlayer.SDK.Attributes;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;



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

        public SourceType SourceType => SourceType.Stream;
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
        private string _authorizationUrl;

        // Client ID 未配置标志
        private bool _needsClientId;

        // 本模块创建的 Spotify Connect PCM 设备
        private string _activeDeviceId;
        private string _activeDeviceName;
        private string _deviceStatusText;
        private List<SpotifyDevice> _cachedDevices = new List<SpotifyDevice>();
        private bool _nativeBridgeLoaded;
        private NativeLibrespotPcmReader _nativeConnectReader;
        private readonly SemaphoreSlim _nativeConnectLock = new SemaphoreSlim(1, 1);

        // 事件订阅
        private readonly List<IDisposable> _playbackSubscriptions = new List<IDisposable>();

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

            // 先释放旧对象，避免 HttpClient/内存泄漏
            var oldBridge = _bridge;
            var oldOAuth = _oauthManager;
            _bridge = null;
            _oauthManager = null;
            oldBridge?.Dispose();
            oldOAuth?.Dispose();

            _bridge = new SpotifyBridge(clientId, _dataPath, _logger);
            _registry = new SpotifySongRegistry(_context, ModuleId);

            InitOAuthManager();
            SubscribePlaybackEvents();
            _nativeBridgeLoaded = _context.DependencyLoader.LoadNativeLibrary(
                "SpotifyLibrespotBridge.dll",
                ModuleId);
            if (!_nativeBridgeLoaded)
                _logger.LogWarning("[Spotify] SpotifyLibrespotBridge.dll not loaded; local Spotify Connect PCM is disabled");

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
                        await EnsureNativeConnectReadyAsync(CancellationToken.None);
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
            DisposePlaybackSubscriptions();
            StopNativeConnectReader();
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

            _oauthManager.OnAuthorizationUrlReady += (url) =>
            {
                _authorizationUrl = url;
                PushUI?.Invoke(BuildUI());
            };

            _oauthManager.OnTokenReceived += async (tokenResponse) =>
            {
                PushUI?.Invoke(BuildUI());

                _bridge.SetTokens(tokenResponse);
                _authorizationUrl = null;

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
                await EnsureNativeConnectReadyAsync(CancellationToken.None);

                _isLoggingIn = false;
                OnReadyStateChanged?.Invoke(true);

                // 推送登录成功后的 UI
                PushUI?.Invoke(BuildUI());
            };

            _oauthManager.OnLoginFailed += (error) =>
            {
                _logger.LogError($"Login failed: {error}");
                _isLoggingIn = false;
                _authorizationUrl = null;
                PushUI?.Invoke(BuildUI());
            };
        }

        private void SubscribePlaybackEvents()
        {
            DisposePlaybackSubscriptions();

            _playbackSubscriptions.Add(_context.EventBus.Subscribe<PlayStartedEvent>(async e =>
            {
                if (e.Music == null)
                    return;

                if (e.Music.ModuleId != ModuleId)
                {
                    await PauseAndStopConnectAsync(
                        $"another module started: {e.Music.ModuleId ?? "host"}",
                        pauseRemote: true).ConfigureAwait(false);
                }
            }));

            _playbackSubscriptions.Add(_context.EventBus.Subscribe<PlayPausedEvent>(async e =>
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
            }));

            _playbackSubscriptions.Add(_context.EventBus.Subscribe<PlaySeekEvent>(async e =>
            {
                if (e.Music == null || e.Music.ModuleId != ModuleId) return;
                if (e.SourceModuleId == ModuleId) return;
                if (e.IsCompleted && !e.IsPending) return;
                if (_bridge == null || !_bridge.IsLoggedIn || !_bridge.Session.IsPremium) return;

                var track = e.Music;
                if (track == null || string.IsNullOrEmpty(track.SourcePath) || track.SourcePath == SpotifySongRegistry.CONNECT_LIVE_SOURCE) return;

                try
                {
                    var positionMs = Math.Max(0, (int)(e.TargetTime * 1000));
                    if (string.IsNullOrEmpty(_activeDeviceId))
                    {
                        await EnsureNativeConnectReadyAsync(CancellationToken.None).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(_activeDeviceName))
                            _activeDeviceId = await WaitForLibrespotDeviceAsync(_activeDeviceName, CancellationToken.None, maxRetries: 5).ConfigureAwait(false);
                    }

                    _logger.LogInformation("[Spotify] Seeking Spotify playback to {PositionMs}ms", positionMs);
                    var ok = await _bridge.SeekAsync(positionMs, _activeDeviceId).ConfigureAwait(false);
                    if (!ok && !string.IsNullOrEmpty(_activeDeviceId))
                    {
                        _logger.LogInformation("[Spotify] Seek failed; transferring playback to {DeviceId} and retrying", _activeDeviceId);
                        await _bridge.TransferPlaybackAsync(_activeDeviceId, play: true).ConfigureAwait(false);
                        ok = await _bridge.SeekAsync(positionMs, _activeDeviceId).ConfigureAwait(false);
                    }

                    if (ok)
                    {
                        _context.EventBus.Publish(new PlaySeekEvent
                        {
                            SourceModuleId = ModuleId,
                            Music = e.Music,
                            Progress = e.Music.Duration > 0 ? e.TargetTime / e.Music.Duration : 0,
                            TargetTime = e.TargetTime,
                            IsPending = false,
                            IsCompleted = true
                        });
                    }
                    else
                    {
                        _logger.LogWarning("[Spotify] Spotify seek was rejected; keeping local progress unchanged");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Spotify] Failed to seek Spotify playback: {ex.Message}");
                }
            }));

            _logger.LogInformation("[Spotify] Subscribed to playback events");
        }

        private void DisposePlaybackSubscriptions()
        {
            foreach (var subscription in _playbackSubscriptions)
                subscription?.Dispose();
            _playbackSubscriptions.Clear();
        }

        private async Task PauseAndStopConnectAsync(string reason, bool pauseRemote)
        {
            var hadReader = _nativeConnectReader != null;
            if (!hadReader)
                return;

            _logger.LogInformation("[Spotify] Leaving local Connect playback ({Reason})", reason);

            if (pauseRemote && _bridge != null && _bridge.IsLoggedIn && _bridge.Session.IsPremium)
            {
                try
                {
                    await _bridge.PauseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[Spotify] Failed to pause Spotify while leaving Connect playback: {Message}", ex.Message);
                }
            }

            StopNativeConnectReader();
        }

        // =====================================================================
        // 歌单加载
        // =====================================================================

        private async Task LoadPlaylistsAsync()
        {
            try
            {
                _registry.RegisterConnectLive();

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
                var loadedPlaylistCount = 0;
                foreach (var playlist in playlists)
                {
                    try
                    {
                        var tracks = await _bridge.GetPlaylistTracksAsync(playlist.Id);
                        if (tracks.Count > 0)
                        {
                            _registry.RegisterPlaylist(playlist, tracks);
                            loadedPlaylistCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to load playlist '{playlist.Name}': {ex.Message}");
                    }
                }

                _logger.LogInformation($"Loaded {loadedPlaylistCount}/{playlists.Count} playlists");

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
                _cachedDevices = devices ?? new List<SpotifyDevice>();
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

        public Task<List<Track>> GetMusicListAsync()
        {
            return Task.FromResult(_context.Library.QueryTracks(new TrackQuery { ModuleId = ModuleId, Limit = 0 }).ToList());
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
        // Local Spotify Connect PCM device (librespot -> shared-memory pipeline)
        // =====================================================================

        private async Task<NativeLibrespotPcmReader> EnsureNativeConnectReadyAsync(CancellationToken cancellationToken)
        {
            if (_bridge == null || !_bridge.IsLoggedIn || !_bridge.Session.IsPremium)
                return null;
            if (!_nativeBridgeLoaded)
                return null;

            await _nativeConnectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_nativeConnectReader != null && _nativeConnectReader.IsReady && !_nativeConnectReader.IsEndOfStream)
                    return _nativeConnectReader;

                StopNativeConnectReader();

                if (!await _bridge.EnsureTokenValidAsync().ConfigureAwait(false))
                    return null;

                var deviceName = _context.ConfigManager.GetString(
                    "LibrespotDeviceName",
                    $"OmniMixPlayer-{Environment.ProcessId}");
                var cacheDir = Path.Combine(_dataPath, "librespot-cache");
                var reader = new NativeLibrespotPcmReader(
                    _bridge.Session.AccessToken,
                    deviceName,
                    cacheDir,
                    0,
                    _logger);

                if (!reader.Start())
                {
                    reader.Dispose();
                    return null;
                }

                if (!reader.WaitForReady(15000, cancellationToken))
                {
                    _logger.LogWarning("[Spotify] local Spotify Connect PCM device did not become ready: {DeviceName}", deviceName);
                    reader.Dispose();
                    return null;
                }

                _nativeConnectReader = reader;
                _activeDeviceName = deviceName;
                _activeDeviceId = await WaitForLibrespotDeviceAsync(deviceName, cancellationToken, maxRetries: 5).ConfigureAwait(false);
                _logger.LogInformation(
                    "[Spotify] local Spotify Connect PCM device ready: {DeviceName}, format={SampleRate}Hz/{Channels}ch/f32",
                    deviceName,
                    reader.SampleRate,
                    reader.Channels);
                return reader;
            }
            finally
            {
                _nativeConnectLock.Release();
            }
        }

        private void StopNativeConnectReader()
        {
            var reader = _nativeConnectReader;
            _nativeConnectReader = null;
            reader?.Dispose();
        }

        private async Task<string> WaitForLibrespotDeviceAsync(string deviceName, CancellationToken cancellationToken, int maxRetries = 30)
        {
            // 指数退避：500ms → 1s → 2s → 4s → 4s...，30 次总时长约 2 分钟
            var delay = TimeSpan.FromMilliseconds(500);
            const double backoffMultiplier = 2.0;
            const int maxDelayMs = 4000;

            for (int i = 0; i < maxRetries; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var devices = await _bridge.GetAvailableDevicesAsync().ConfigureAwait(false);
                    var device = devices.FirstOrDefault(d =>
                        string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
                    if (device != null)
                        return device.Id;
                }
                catch (Exception ex)
                {
                    // API 可能限流，退避后重试
                    _logger.LogWarning("[Spotify] Device poll error (attempt {Attempt}/{Max}): {Message}", i + 1, maxRetries, ex.Message);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * backoffMultiplier, maxDelayMs));
            }
            return null;
        }

        // =====================================================================
        // IModuleAudioDecoderProvider (local Spotify Connect PCM sink)
        // =====================================================================

        public bool CanDecode(string uuid)
        {
            if (_bridge == null || !_bridge.IsLoggedIn || !_bridge.Session.IsPremium || !_nativeBridgeLoaded)
                return false;

            var track = _context?.Library?.GetTrack(uuid);
            if (track == null) return false;
            return track.SourcePath == SpotifySongRegistry.CONNECT_LIVE_SOURCE || track.SourcePath.StartsWith("spotify:track:");
        }

        public async Task<IPcmStreamReader> CreateDecoderAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Spotify] Create PCM reader called: uuid={Uuid}", uuid);

            var track = _context.Library.GetTrack(uuid);
            if (track == null)
            {
                _logger.LogWarning($"[Spotify] Track not found: {uuid}");
                return null;
            }

            var spotifyUri = track.SourcePath;
            var isConnectLive = spotifyUri == SpotifySongRegistry.CONNECT_LIVE_SOURCE;
            if (!isConnectLive && !spotifyUri.StartsWith("spotify:track:"))
            {
                _logger.LogWarning($"[Spotify] No Spotify URI for: {track.Title}");
                return null;
            }

            if (_bridge == null || !_bridge.IsLoggedIn)
            {
                _logger.LogWarning("[Spotify] Not logged in");
                return null;
            }

            if (!_bridge.Session.IsPremium)
            {
                _logger.LogWarning("[Spotify] Playback control requires Spotify Premium");
                return null;
            }

            var reader = await EnsureNativeConnectReadyAsync(cancellationToken).ConfigureAwait(false);
            if (reader == null)
            {
                _logger.LogWarning("[Spotify] Local Spotify Connect PCM device is unavailable");
                return null;
            }

            if (!isConnectLive && string.IsNullOrEmpty(_activeDeviceId))
                _activeDeviceId = await WaitForLibrespotDeviceAsync(_activeDeviceName, cancellationToken).ConfigureAwait(false);

            if (isConnectLive)
            {
                _logger.LogInformation(
                    "[Spotify] Connect Live receiver is active. Select device '{DeviceName}' in Spotify to stream audio.",
                    _activeDeviceName);
            }
            else if (!string.IsNullOrEmpty(_activeDeviceId))
            {
                await _bridge.TransferPlaybackAsync(_activeDeviceId, play: false).ConfigureAwait(false);
                var playSuccess = await _bridge.PlayTrackAsync(spotifyUri, _activeDeviceId).ConfigureAwait(false);
                if (playSuccess)
                    _logger.LogInformation("[Spotify] Playback started on local Connect PCM device: {Title}", track.Title);
                else
                    _logger.LogWarning("[Spotify] Failed to start playback on local Connect PCM device: {Title}", track.Title);
            }
            else
            {
                _logger.LogInformation(
                    "[Spotify] Local Connect PCM reader is ready. Select device '{DeviceName}' in Spotify to stream audio.",
                    _activeDeviceName);
            }

            return new NativeLibrespotPcmReaderLease(reader, isConnectLive ? 0 : track.Duration);
        }

        public async Task<PlayableSource> ResolveAsync(string uuid, AudioQuality quality = AudioQuality.ExHigh, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return null;
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
            var track = _context.Library.GetTrack(uuid);
            if (track == null || string.IsNullOrWhiteSpace(track.CoverUri))
                return (_context.DefaultCover.DefaultMusicCover, "image/png");

            var result = await DownloadCoverAsync(track.CoverUri);
            return result ?? (_context.DefaultCover.DefaultMusicCover, "image/png");
        }

        public async Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId)
        {
            var album = _context.Library.GetAlbum(albumId);
            if (album != null && !string.IsNullOrWhiteSpace(album.CoverUri))
            {
                var result = await DownloadCoverAsync(album.CoverUri);
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
            var track = _context.Library.GetTrack(uuid);
            if (track == null) return false;
            var spotifyId = ExtractSpotifyId(track);
            if (spotifyId == null) return false;
            return _savedCache.TryGetValue(spotifyId, out var saved) && saved;
        }

        public void SetFavorite(string uuid, bool isFavorite)
        {
            _ = SetFavoriteAsync(uuid, isFavorite);
        }

        private async Task SetFavoriteAsync(string uuid, bool isFavorite)
        {
            var track = _context.Library.GetTrack(uuid);
            var spotifyId = ExtractSpotifyId(track);
            if (spotifyId == null || _bridge == null || !_bridge.IsLoggedIn) return;

            bool success;
            if (isFavorite)
                success = await _bridge.SaveTracksAsync(new List<string> { spotifyId });
            else
                success = await _bridge.RemoveTracksAsync(new List<string> { spotifyId });

            if (success)
            {
                _savedCache[spotifyId] = isFavorite;
                if (track != null) track.IsFavorite = isFavorite;
                _logger.LogInformation($"[Spotify] Favorite {(isFavorite ? "saved" : "removed")}: {track?.Title}");
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
                var uuid = SpotifySongRegistry.GenerateUuid($"spotify_{kvp.Key}");
                result.Add(uuid);
            }
            return result;
        }

        public IReadOnlyList<string> GetExcluded() => new List<string>();

        // =====================================================================
        // 辅助方法
        // =====================================================================

        /// <summary>
        /// 从 Track 的 SourcePath (spotify:track:xxx) 提取 Spotify track ID
        /// </summary>
        private static string ExtractSpotifyId(Track track)
        {
            if (track == null || string.IsNullOrEmpty(track.SourcePath)) return null;
            var uri = track.SourcePath;
            const string prefix = "spotify:track:";
            if (uri.StartsWith(prefix)) return uri.Substring(prefix.Length);
            return null;
        }

        // =====================================================================
        // IModuleUIProvider
        // =====================================================================

        public Action<SlintNode> PushUI { get; set; }

        public bool HasSettingsUI => true;

        public bool HasQuickLinks => true;

        public IReadOnlyList<ModuleLinkEntry> GetQuickLinks()
        {
            return new List<ModuleLinkEntry>
            {
                new ModuleLinkEntry("devices", "选择设备", "speaker",
                    "#1db954", "#ffffff",
                    svg: "spotify_icon.png")
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
                var statusText = _isLoggingIn ? "正在准备 Spotify 授权..." : "点击下方按钮登录 Spotify";
                var column = SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(SlintUi.Text("Spotify", fontSize: 18))
                    .AddChild(SlintUi.Text("未登录", fontSize: 14))
                    .AddChild(SlintUi.Text(statusText, fontSize: 12, color: "#94a3b8"));

                if (!_isLoggingIn)
                {
                    column.AddChild(SlintUi.Button("login_btn", "登录 Spotify", variant: "primary"));
                }
                else if (!string.IsNullOrEmpty(_authorizationUrl))
                {
                    column
                        .AddChild(new SlintNode
                        {
                            Id = "spotify_auth_url",
                            NodeType = "ExternalLink",
                            Text = "打开 Spotify 授权页面",
                            Value = _authorizationUrl,
                            ButtonVariant = "primary"
                        })
                        .AddChild(SlintUi.Text("如果按钮无效，请复制下面的授权 URL 到浏览器。", fontSize: 11, color: "#94a3b8"))
                        .AddChild(SlintUi.Input("spotify_auth_url_copy", "授权 URL", _authorizationUrl, inputType: "text"))
                        .AddChild(SlintUi.Button("cancel_login_btn", "取消登录", variant: null));
                }

                return column;
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
                        _authorizationUrl = null;
                        PushUI?.Invoke(BuildUI());
                        _ = Task.Run(() => _oauthManager.StartLoginAsync());
                    }
                    break;

                case "logout_btn":
                    _ = LogoutSpotifyAsync();
                    break;

                case "cancel_login_btn":
                    _oauthManager?.Cancel();
                    _isLoggingIn = false;
                    _authorizationUrl = null;
                    PushUI?.Invoke(BuildUI());
                    break;

                case "open_devices":
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
            PushUI?.Invoke(BuildDeviceListUI());
        }

        public SlintNode BuildLinkUI(string linkId)
        {
            if (linkId == "devices")
            {
                return BuildDeviceListUI();
            }
            return null;
        }

        public void HandleLinkUIEvent(string linkId, string nodeId, string action, string value)
        {
            _ = HandleLinkUIEventAsync(linkId, nodeId, action, value);
        }

        private async Task HandleLinkUIEventAsync(string linkId, string nodeId, string action, string value)
        {
            if (linkId == "devices")
            {
                if (nodeId == "refresh_devices_link")
                {
                    _deviceStatusText = null;
                    await EnsureNativeConnectReadyAsync(CancellationToken.None);
                    await RefreshDevicesAndPushUI();
                }
                else if (nodeId.StartsWith("select_device_", StringComparison.Ordinal))
                {
                    var indexText = nodeId.Substring("select_device_".Length);
                    if (int.TryParse(indexText, out var index))
                    {
                        await SelectDeviceAndPushUIAsync(index);
                    }
                }
            }
        }

        private async Task SelectDeviceAndPushUIAsync(int index)
        {
            var devices = _cachedDevices ?? new List<SpotifyDevice>();
            if (index < 0 || index >= devices.Count)
            {
                _deviceStatusText = "设备列表已过期，请刷新后重试。";
                PushUI?.Invoke(BuildDeviceListUI());
                return;
            }

            var device = devices[index];
            if (device.IsRestricted || string.IsNullOrEmpty(device.Id))
            {
                _deviceStatusText = $"无法切换到 {device.Name ?? "Unknown"}：Spotify 标记该设备为受限。";
                PushUI?.Invoke(BuildDeviceListUI());
                return;
            }

            if (device.Id == _activeDeviceId || device.IsActive)
            {
                _activeDeviceId = device.Id;
                _activeDeviceName = device.Name;
                _deviceStatusText = $"{device.Name ?? "Unknown"} 已经是当前设备。";
                await RefreshDevicesAndPushUI();
                return;
            }

            try
            {
                var ok = await _bridge.TransferPlaybackAsync(device.Id, play: true);
                if (ok)
                {
                    _activeDeviceId = device.Id;
                    _activeDeviceName = device.Name;
                    _deviceStatusText = $"已切换到 {device.Name ?? "Unknown"}。";
                    _logger.LogInformation("[Spotify] Transferred playback to device: {DeviceName}", device.Name);
                }
                else
                {
                    _deviceStatusText = $"切换到 {device.Name ?? "Unknown"} 失败，请确认账号为 Premium 且设备可用。";
                }
            }
            catch (Exception ex)
            {
                _deviceStatusText = $"切换设备失败：{ex.Message}";
                _logger.LogWarning("[Spotify] Failed to transfer playback to device {DeviceName}: {Message}", device.Name, ex.Message);
            }

            await RefreshDevicesAndPushUI();
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
                .AddChild(SlintUi.Text("3. 添加 Redirect URI 端口池:", fontSize: 10, color: "#94a3b8"))
                .AddChild(SlintUi.Text(OAuthManager.DashboardRedirectUris, fontSize: 10, color: "#94a3b8"));
        }

        private SlintNode BuildUIWithStatus(string statusText = null)
        {
            var displayName = _bridge?.Session?.DisplayName ?? "Spotify 用户";
            var accountType = _bridge?.Session?.Product ?? "";
            var isPremium = _bridge?.Session?.IsPremium ?? false;
            var deviceName = _activeDeviceName ?? "正在启动本地 Spotify Connect 设备...";

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
                .AddChild(SlintUi.Text("本地 Spotify Connect 设备", fontSize: 16))
                .AddChild(
                    SlintUi.Row(spacing: 8)
                        .AddChild(SlintUi.Text(deviceName, fontSize: 14))
                        .AddChild(SlintUi.Button("open_devices", "查看状态 →", variant: null))
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
            // 获取设备列表（使用缓存，避免同步阻塞 HTTP 线程）
            var devices = _cachedDevices ?? new List<SpotifyDevice>();
            bool isLoggedIn = _bridge?.IsLoggedIn ?? false;

            // 全黑背景容器
            var column = SlintUi.Column(spacing: 0, padding: 0);
            column.Color = "#000000";
            column.CrossAxisAlignment = "center";

            // ── 顶部区域：Logo + OmniMix ──
            var topSection = SlintUi.Column(spacing: 12, padding: 24);
            topSection.CrossAxisAlignment = "center";
            // 居中 Logo (通过模块内容 API 提供)
            topSection.Children.Add(new SlintNode
            {
                NodeType = "Image",
                Source = "/api/modules/com.chillpatcher.spotify/content/spotify_icon.png",
                ImageWidth = 64,
                ImageHeight = 64,
                ImageFit = "contain"
            });
            topSection.Children.Add(SlintUi.Text("本地 Spotify Connect PCM 设备", fontSize: 12, color: "#b3b3b3"));
            topSection.Children.Add(SlintUi.Text("", fontSize: 4, color: "#000000")); // 间距
            topSection.Children.Add(SlintUi.Text(_activeDeviceName ?? "OmniMixPlayer", fontSize: 18, color: "#ffffff"));
            topSection.Children.Add(SlintUi.Text(
                _nativeConnectReader?.IsReady == true
                    ? "已连接 Spotify。也可以在手机 Spotify 中选择此设备。"
                    : "正在连接 Spotify...",
                fontSize: 11,
                color: "#b3b3b3"));
            column.Children.Add(topSection);

            // ── 分割线 ──
            column.Children.Add(SlintUi.Text("────────────────────", fontSize: 10, color: "#333333"));

            // ── 中间区域：设备列表 或 未登录提示 ──
            var middleSection = SlintUi.Column(spacing: 0, padding: 12);
            middleSection.CrossAxisAlignment = "center";
            if (isLoggedIn)
            {
                if (devices.Count == 0)
                {
                    middleSection.Children.Add(
                        SlintUi.Text("没有发现可用设备，请先打开 Spotify 客户端或刷新。", fontSize: 12, color: "#b3b3b3")
                    );
                }

                for (var i = 0; i < devices.Count; i++)
                {
                    var device = devices[i];
                    var isActive = device.Id == _activeDeviceId || device.IsActive;
                    var deviceLabel = device.Name ?? "Unknown";
                    if (device.VolumePercent.HasValue)
                        deviceLabel += $"  ·  Vol {device.VolumePercent}%";

                    var details = device.Type ?? "Device";
                    if (device.IsRestricted)
                        details += " · 受限";
                    if (isActive)
                        details += " · 当前";

                    var deviceRow = SlintUi.Row(spacing: 8, padding: 4);
                    deviceRow.CrossAxisAlignment = "center";

                    deviceRow.Children.Add(SlintUi.Button(
                        $"select_device_{i}",
                        $"{(isActive ? "✓ " : "")}{deviceLabel}",
                        isActive ? "primary" : "ghost"));

                    deviceRow.Children.Add(SlintUi.Text(
                        details,
                        fontSize: 10,
                        color: device.IsRestricted ? "#f59e0b" : "#b3b3b3"));

                    middleSection.Children.Add(deviceRow);
                }

                if (!string.IsNullOrEmpty(_deviceStatusText))
                {
                    middleSection.Children.Add(SlintUi.Text(_deviceStatusText, fontSize: 11, color: "#b3b3b3"));
                }
            }
            else
            {
                middleSection.Children.Add(
                    SlintUi.Text("请先登录后再使用", fontSize: 14, color: "#ffffff")
                );
            }
            column.Children.Add(middleSection);

            // ── 分割线 ──
            column.Children.Add(SlintUi.Text("────────────────────", fontSize: 10, color: "#333333"));

            // ── 刷新按钮 ──
            column.Children.Add(
                SlintUi.Row(spacing: 8, padding: 12)
                    .AddChild(new SlintNode
                    {
                        NodeType = "Button",
                        Id = "refresh_devices_link",
                        Text = "🔄 刷新设备",
                        ButtonVariant = "ghost"
                    })
            );

            return column;
        }

        public Task<byte[]> ServeRawContent(string path)
        {
            if (path == "spotify_icon.png")
            {
                var assembly = GetType().Assembly;
                using var stream = assembly.GetManifestResourceStream(
                    "OmniMixPlayer.Module.Spotify.Resources.spotify_icon.png");
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return Task.FromResult(ms.ToArray());
                }
            }
            return Task.FromResult<byte[]>(null);
        }
        public string ServeRawContentType(string path) => path == "spotify_icon.png" ? "image/png" : null;

        private async Task LogoutSpotifyAsync()
        {
            try
            {
                _bridge?.ClearSession();
                StopNativeConnectReader();
                _savedCache.Clear();
                _activeDeviceId = null;
                _activeDeviceName = null;
                _cachedDevices.Clear();
                _isLoggingIn = false;

                // 取消正在进行的 OAuth 流程
                _oauthManager?.Cancel();

                _context?.Library?.UnregisterModule(ModuleId);

                OnReadyStateChanged?.Invoke(false);
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
