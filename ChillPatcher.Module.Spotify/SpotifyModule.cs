using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChillPatcher.SDK.Attributes;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ChillPatcher.Module.Spotify
{
    [MusicModule("com.chillpatcher.spotify", "Spotify",
        Version = "1.0.0",
        Author = "ChillPatcher",
        Description = "Spotify Connect playback control and playlist sync")]
    public class SpotifyModule : IMusicModule, IStreamingMusicSourceProvider, ICoverProvider, IFavoriteExcludeHandler
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
        private ManualLogSource _logger;
        private SpotifyBridge _bridge;
        private OAuthManager _oauthManager;
        private SpotifySongRegistry _registry;

        private string _dataPath;
        private string _clientId;
        private string _pluginPath;

        // 封面缓存
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // 收藏状态缓存 (Spotify track ID -> saved)，初始数据来自 Liked Songs 加载
        private readonly Dictionary<string, bool> _savedCache = new Dictionary<string, bool>();


        // OAuth 登录进行中标志
        private bool _isLoggingIn;

        // Client ID 未配置标志
        private bool _needsClientId;

        // 当前选定的设备
        private string _activeDeviceId;
        private string _activeDeviceName;

        // 事件订阅
        private IDisposable _pauseSubscription;

        // Win32 API: 授权完成后将游戏窗口拉回前台
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessId();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // =====================================================================
        // 生命周期
        // =====================================================================

        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            _logger = context.Logger;
            _logger.LogInfo("Spotify module initializing...");

            // 数据目录
            _pluginPath = Path.GetDirectoryName(typeof(SpotifyModule).Assembly.Location);
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChillPatcher", ModuleId);
            Directory.CreateDirectory(_dataPath);

            // 读取配置
            LoadConfig(_pluginPath);
            _logger.LogInfo($"Config loaded: ClientId={(string.IsNullOrEmpty(_clientId) ? "(empty)" : _clientId.Substring(0, Math.Min(8, _clientId.Length)) + "...")}");


            if (string.IsNullOrEmpty(_clientId) || _clientId == "YOUR_SPOTIFY_CLIENT_ID")
            {
                _logger.LogWarning("Spotify Client ID not configured. Will prompt on first play.");
                _needsClientId = true;
                _registry = new SpotifySongRegistry(_context, ModuleId);
                _registry.RegisterLoginSong(">>> 点击播放以配置 Spotify <<<");
                OnReadyStateChanged?.Invoke(false);
                return;
            }

            // 初始化组件
            await InitWithClientIdAsync();
        }

        private void ShowConfigWindow()
        {
            _logger.LogInfo("Showing Spotify Client ID configuration window");
            SpotifyConfigWindow.Show(
                onSubmit: async (clientId) =>
                {
                    _logger.LogInfo("Client ID submitted via config window");
                    _clientId = clientId;
                    _needsClientId = false;
                    SaveClientId(clientId);

                    _registry.UpdateLoginStatus("Client ID 已保存，正在初始化...");

                    // 初始化 Bridge 和 OAuthManager（不走 InitWithClientIdAsync 以避免注册新登录歌曲）
                    _bridge = new SpotifyBridge(_clientId, _dataPath, _logger);
                    InitOAuthManager();
                    SubscribePlaybackEvents();

                    // 直接启动 OAuth 登录流程
                    _logger.LogInfo("Starting OAuth flow after config...");
                    _ = Task.Run(() => _oauthManager.StartLoginAsync());
                },
                onCancel: () =>
                {
                    _logger.LogInfo("Spotify config window cancelled");
                    _isLoggingIn = false;
                    _registry.UpdateLoginStatus(">>> 点击播放以配置 Spotify <<<");
                }
            );
        }

        private void SaveClientId(string clientId)
        {
            var bepInExConfigPath = Path.Combine(_pluginPath, "..", "..", "config", "com.chillpatcher.plugin.cfg");
            var configFile = new ConfigFile(bepInExConfigPath, false);
            var entry = configFile.Bind(
                $"Module:{ModuleId}",
                "ClientId",
                "YOUR_SPOTIFY_CLIENT_ID",
                "Spotify Developer App Client ID.");
            entry.Value = clientId;
            configFile.Save();
            _logger.LogInfo("Client ID saved to config file");
        }

        private async Task InitWithClientIdAsync()
        {
            _bridge = new SpotifyBridge(_clientId, _dataPath, _logger);
            _registry = new SpotifySongRegistry(_context, ModuleId);

            InitOAuthManager();
            SubscribePlaybackEvents();

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

                        _logger.LogInfo($"Spotify logged in as {user.DisplayName} ({user.Product})");

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

            // 未登录，显示登录歌曲
            _registry.RegisterLoginSong("点击播放以登录 Spotify");
            OnReadyStateChanged?.Invoke(false);
        }

        public void OnEnable() { }

        public void OnDisable() { }

        public void OnUnload()
        {
            _pauseSubscription?.Dispose();
            _oauthManager?.Dispose();
            _bridge?.Dispose();
            _spriteCache.Clear();
            _savedCache.Clear();
        }

        // =====================================================================
        // 配置
        // =====================================================================

        private void LoadConfig(string pluginPath)
        {
            // 直接读取 BepInEx 配置文件
            var bepInExConfigPath = Path.Combine(pluginPath, "..", "..", "config", "com.chillpatcher.plugin.cfg");
            var configFile = new ConfigFile(bepInExConfigPath, false);

            _clientId = configFile.Bind(
                $"Module:{ModuleId}",
                "ClientId",
                "YOUR_SPOTIFY_CLIENT_ID",
                "Spotify Developer App Client ID.\n" +
                "在 https://developer.spotify.com/dashboard 创建 App 获取。\n" +
                "Redirect URI 设置为: fullstop://callback"
            ).Value;

            configFile.Save();
        }

        // =====================================================================
        // OAuth
        // =====================================================================

        private void InitOAuthManager()
        {
            _oauthManager = new OAuthManager(_clientId, _dataPath, _logger);

            _oauthManager.OnStatusChanged += (status) =>
            {
                _logger.LogInfo($"OAuth status: {status}");
                _registry.UpdateLoginStatus(status);
            };

            _oauthManager.OnTokenReceived += async (tokenResponse) =>
            {
                _registry.UpdateLoginStatus("授权成功，正在获取用户信息...");
                BringGameToForeground();

                _bridge.SetTokens(tokenResponse);

                // 获取用户信息
                var user = await _bridge.GetCurrentUserAsync();
                if (user != null)
                {
                    _bridge.Session.UserId = user.Id;
                    _bridge.Session.DisplayName = user.DisplayName;
                    _bridge.Session.Product = user.Product;
                    _bridge.SaveSession();
                    _logger.LogInfo($"Logged in as {user.DisplayName} ({user.Product})");
                }

                // 移除登录歌曲，加载歌单
                _registry.UpdateLoginStatus("正在加载歌单...");
                _registry.UnregisterLoginSong();
                await LoadPlaylistsAsync();

                _isLoggingIn = false;
                OnReadyStateChanged?.Invoke(true);
            };

            _oauthManager.OnLoginFailed += (error) =>
            {
                _logger.LogError($"Login failed: {error}");
                _registry.UpdateLoginStatus($"登录失败: {error}，再次播放以重试");
                _isLoggingIn = false;
                BringGameToForeground();
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
                        _logger.LogInfo("[Spotify] Game paused → pausing Spotify");
                        await _bridge.PauseAsync();
                    }
                    else
                    {
                        _logger.LogInfo("[Spotify] Game resumed → resuming Spotify");
                        await _bridge.ResumeAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Spotify] Failed to sync pause state: {ex.Message}");
                }
            });
            _logger.LogInfo("[Spotify] Subscribed to PlayPausedEvent");
        }

        // =====================================================================
        // 歌单加载
        // =====================================================================

        private async Task LoadPlaylistsAsync()
        {
            try
            {
                // 加载 Liked Songs
                _logger.LogInfo("Loading Liked Songs...");
                var likedTracks = await _bridge.GetSavedTracksAsync(500);
                if (likedTracks.Count > 0)
                {
                    _registry.RegisterLikedSongs(likedTracks);
                    foreach (var t in likedTracks)
                        _savedCache[t.Id] = true;
                }

                // 加载用户歌单
                _logger.LogInfo("Loading playlists...");
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

                _logger.LogInfo($"Loaded {playlists.Count} playlists");

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
                _logger.LogInfo($"[Spotify] Found {devices.Count} devices");

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

                // 注册/更新设备选择歌曲
                _registry.RegisterDeviceSelector(_activeDeviceName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Spotify] Failed to load devices: {ex.Message}");
                _registry.RegisterDeviceSelector(null);
            }
        }

        private void ShowDeviceSelector()
        {
            _ = ShowDeviceSelectorAsync();
        }

        private async Task ShowDeviceSelectorAsync()
        {
            var devices = await _bridge.GetAvailableDevicesAsync();
            _logger.LogInfo($"[Spotify] Showing device selector, {devices.Count} devices");

            SpotifyDeviceWindow.Show(
                devices,
                _activeDeviceId,
                onSelect: async (device) =>
                {
                    _logger.LogInfo($"[Spotify] User selected device: {device.Name} ({device.Id})");
                    _activeDeviceId = device.Id;
                    _activeDeviceName = device.Name;

                    var success = await _bridge.TransferPlaybackAsync(device.Id, play: false);
                    if (success)
                        _logger.LogInfo($"[Spotify] Transferred playback to: {device.Name}");
                    else
                        _logger.LogWarning($"[Spotify] Failed to transfer to: {device.Name}");

                    _registry.UpdateDeviceStatus(device.Name);
                },
                onCancel: () =>
                {
                    _logger.LogInfo("[Spotify] Device selection cancelled");
                }
            );
        }

        // =====================================================================
        // IStreamingMusicSourceProvider
        // =====================================================================

        public Task<List<MusicInfo>> GetMusicListAsync()
        {
            var all = _context.MusicRegistry.GetMusicByModule(ModuleId);
            return Task.FromResult(all?.ToList() ?? new List<MusicInfo>());
        }

        public Task<AudioClip> LoadAudioAsync(string uuid) => Task.FromResult<AudioClip>(null);

        public Task<AudioClip> LoadAudioAsync(string uuid, CancellationToken token) => Task.FromResult<AudioClip>(null);

        public void UnloadAudio(string uuid) { }

        public async Task RefreshAsync()
        {
            if (!_bridge.IsLoggedIn) return;

            _registry.UnregisterAll();
            _savedCache.Clear();
            await LoadPlaylistsAsync();
        }

        // =====================================================================
        // IPlayableSourceResolver (Spotify Connect 播放)
        // =====================================================================

        public async Task<PlayableSource> ResolveAsync(string uuid, AudioQuality quality = AudioQuality.ExHigh, CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"[Spotify] ResolveAsync called: uuid={uuid}");

            // 登录歌曲：触发配置或 OAuth 流程
            if (uuid == SpotifySongRegistry.UUID_LOGIN)
            {
                if (!_isLoggingIn)
                {
                    _isLoggingIn = true;

                    if (_needsClientId)
                    {
                        // Client ID 未配置，弹出配置窗口
                        _registry.UpdateLoginStatus("请在弹出窗口中输入 Client ID");
                        ShowConfigWindow();
                    }
                    else
                    {
                        // Client ID 已配置，启动 OAuth 登录
                        _ = Task.Run(() => _oauthManager.StartLoginAsync(cancellationToken));
                    }
                }

                return PlayableSource.FromPcmStream(uuid, new SilentPcmReader(120f), AudioFormat.Mp3);
            }

            // 设备选择：弹出设备选择窗口
            if (uuid == SpotifySongRegistry.UUID_DEVICE_SELECT)
            {
                _logger.LogInfo("[Spotify] Device selector triggered");
                ShowDeviceSelector();
                return PlayableSource.FromPcmStream(uuid, new SilentPcmReader(30f), AudioFormat.Mp3);
            }

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
                    _logger.LogInfo($"[Spotify] Auto-detected active device: {active.Name}");
                }
                else if (devices.Count > 0)
                {
                    _activeDeviceId = devices[0].Id;
                    _logger.LogInfo($"[Spotify] Using first available device: {devices[0].Name}");
                    await _bridge.TransferPlaybackAsync(_activeDeviceId, play: false);
                }
                else
                {
                    _logger.LogWarning("[Spotify] No Spotify devices found. Please open Spotify and select a device.");
                    return null;
                }
            }

            _logger.LogInfo($"[Spotify] Playing: {music.Title} on device {_activeDeviceId}");
            var playSuccess = await _bridge.PlayTrackAsync(meta.SpotifyUri, _activeDeviceId);
            if (playSuccess)
                _logger.LogInfo($"[Spotify] Playback started: {music.Title}");
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

        public async Task<Sprite> GetMusicCoverAsync(string uuid)
        {
            if (uuid == SpotifySongRegistry.UUID_LOGIN)
                return _context.DefaultCover.DefaultMusicCover;

            var music = _context.MusicRegistry.GetMusic(uuid);
            if (music == null) return _context.DefaultCover.DefaultMusicCover;

            var meta = GetTrackMeta(music);
            if (meta == null || string.IsNullOrEmpty(meta.CoverUrl))
                return _context.DefaultCover.DefaultMusicCover;

            return await DownloadSpriteAsync(meta.CoverUrl) ?? _context.DefaultCover.DefaultMusicCover;
        }

        public async Task<Sprite> GetAlbumCoverAsync(string albumId)
        {
            var album = _context.AlbumRegistry.GetAlbum(albumId);
            if (album?.ExtendedData is string coverUrl && !string.IsNullOrEmpty(coverUrl))
                return await DownloadSpriteAsync(coverUrl) ?? _context.DefaultCover.DefaultAlbumCover;

            return _context.DefaultCover.DefaultAlbumCover;
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid)
        {
            var music = _context.MusicRegistry.GetMusic(uuid);
            var meta = GetTrackMeta(music);
            if (meta == null || string.IsNullOrEmpty(meta.CoverUrl))
                return (null, null);

            try
            {
                var request = UnityWebRequest.Get(meta.CoverUrl);
                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Delay(50);

                if (request.result != UnityWebRequest.Result.Success)
                    return (null, null);

                return (request.downloadHandler.data, "image/jpeg");
            }
            catch
            {
                return (null, null);
            }
        }

        public void RemoveMusicCoverCache(string uuid) { }
        public void RemoveAlbumCoverCache(string albumId) { }
        public void ClearCache() => _spriteCache.Clear();

        private async Task<Sprite> DownloadSpriteAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (_spriteCache.TryGetValue(url, out var cached)) return cached;

            try
            {
                var request = UnityWebRequestTexture.GetTexture(url);
                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Delay(50);

                if (request.result != UnityWebRequest.Result.Success)
                    return null;

                var texture = DownloadHandlerTexture.GetContent(request);

                // 中心裁剪为正方形（与 Bilibili 模块一致）
                int size = Mathf.Min(texture.width, texture.height);
                int offsetX = (texture.width - size) / 2;
                int offsetY = (texture.height - size) / 2;
                var rect = new Rect(offsetX, offsetY, size, size);
                var pivot = new Vector2(0.5f, 0.5f);

                var sprite = Sprite.Create(texture, rect, pivot);
                _spriteCache[url] = sprite;
                return sprite;
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
                _logger.LogInfo($"[Spotify] Favorite {(isFavorite ? "saved" : "removed")}: {music?.Title}");
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

        /// <summary>
        /// 将游戏窗口拉回前台（OAuth 完成后从浏览器切回）。
        /// </summary>
        private void BringGameToForeground()
        {
            try
            {
                var pid = GetCurrentProcessId();
                IntPtr gameWindow = IntPtr.Zero;

                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint windowPid);
                    if (windowPid == pid && IsWindowVisible(hWnd))
                    {
                        gameWindow = hWnd;
                        return false; // 找到第一个可见窗口就停止
                    }
                    return true;
                }, IntPtr.Zero);

                if (gameWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(gameWindow);
                    _logger.LogInfo("Game window brought to foreground");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to bring game to foreground: {ex.Message}");
            }
        }
    }
}
