using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChillPatcher.SDK.Attributes;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;
using UnityEngine;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// QQ Music module for ChillPatcher
    /// </summary>
    [MusicModule(ModuleInfo.MODULE_ID, ModuleInfo.MODULE_NAME,
        Version = ModuleInfo.MODULE_VERSION,
        Author = ModuleInfo.MODULE_AUTHOR,
        Description = ModuleInfo.MODULE_DESCRIPTION,
        Priority = 50)]
    public class QQMusicModule : IMusicModule, IStreamingMusicSourceProvider, ICoverProvider, IFavoriteExcludeHandler
    {
        private IModuleContext _context;
        private ManualLogSource _logger;
        private QQMusicBridge _bridge;
        private QQMusicSongRegistry _songRegistry;
        private QQMusicFavoriteManager _favoriteManager;
        private QQMusicCoverLoader _coverLoader;

        // State
        private List<MusicInfo> _musicList;
        private List<MusicInfo> _recommendMusicList;
        private Dictionary<string, QQMusicBridge.SongInfo> _songInfoMap;
        private Dictionary<long, List<MusicInfo>> _customPlaylistMusicLists;
        private bool _isReady;
        private bool _isLoggedIn;
        private string _currentLoginSongUuid;
        private string _wxLoginSongUuid;
        private QRLoginManager _qrLoginManager;
        private readonly object _stateLock = new object();
        private int _loginSuccessHandling;

        // Subscriptions
        private IDisposable _favoriteChangedSubscription;

        // Config
        private ConfigEntry<string> _dataDir;
        private ConfigEntry<int> _audioQuality;
        private ConfigEntry<string> _customPlaylistIds;
        private ConfigEntry<int> _streamReadyTimeoutMs;

        #region IMusicModule Implementation

        public string ModuleId => ModuleInfo.MODULE_ID;
        public string DisplayName => ModuleInfo.MODULE_NAME;
        public string Version => ModuleInfo.MODULE_VERSION;
        public int Priority => 50;

        public ModuleCapabilities Capabilities => new ModuleCapabilities
        {
            CanDelete = false,
            CanFavorite = true,
            CanExclude = false,
            SupportsLiveUpdate = false,
            ProvidesCover = true,
            ProvidesAlbum = true
        };

        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Logger;

            _musicList = new List<MusicInfo>();
            _recommendMusicList = new List<MusicInfo>();
            _songInfoMap = new Dictionary<string, QQMusicBridge.SongInfo>();
            _customPlaylistMusicLists = new Dictionary<long, List<MusicInfo>>();

            // Register config
            RegisterConfig();

            // Load native DLL
            try
            {
                context.DependencyLoader?.LoadNativeLibrary($"{ModuleInfo.NATIVE_DLL}.dll", ModuleInfo.MODULE_ID);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load native DLL: {ex.Message}");
                return;
            }

            // Initialize bridge
            _bridge = new QQMusicBridge(_logger);
            var dataDir = string.IsNullOrEmpty(_dataDir.Value)
                ? context.GetModuleDataPath(ModuleId)
                : _dataDir.Value;

            if (!_bridge.Initialize(dataDir))
            {
                _logger?.LogError($"Failed to initialize QQMusic bridge: {_bridge.GetLastError()}");
                return;
            }

            // Initialize managers
            _songRegistry = new QQMusicSongRegistry(_context, ModuleId);
            _coverLoader = new QQMusicCoverLoader(_logger, _songInfoMap);
            _favoriteManager = new QQMusicFavoriteManager(_bridge, _logger, _songInfoMap);

            // Register lyric API for JS frontend
            RegisterLyricApi();

            // Check login status — 信任 cookie 文件，不做额外 API 验证
            // GetUserProfile 接口可能不支持 musickey 认证，会误判为过期并删除有效 cookie
            // 如果 cookie 确实过期，后续 API 调用会自然失败
            _isLoggedIn = _bridge.IsLoggedIn;

            if (!_isLoggedIn)
            {
                await HandleNotLoggedInAsync();
            }
            else
            {
                await ScanAndRegisterAsync();
            }

            // Subscribe to events
            _favoriteChangedSubscription = _context.EventBus.Subscribe<FavoriteChangedEvent>(OnFavoriteChanged);

            _isReady = true;
            OnReadyStateChanged?.Invoke(true);
        }

        public void OnEnable()
        {
            _logger?.LogInfo("QQ Music module enabled");
        }

        public void OnDisable()
        {
            _logger?.LogInfo("QQ Music module disabled");
        }

        public void OnUnload()
        {
            _qrLoginManager?.CancelLogin();
            _coverLoader?.ClearCache();
            _favoriteChangedSubscription?.Dispose();
        }

        #endregion

        #region IStreamingMusicSourceProvider Implementation

        public bool IsReady => _isReady;
        public event Action<bool> OnReadyStateChanged;
        public MusicSourceType SourceType => MusicSourceType.Stream;

        public Task<List<MusicInfo>> GetMusicListAsync()
        {
            List<MusicInfo> musicListSnapshot;
            List<MusicInfo> recommendSnapshot;
            List<List<MusicInfo>> customPlaylistsSnapshot;

            lock (_stateLock)
            {
                musicListSnapshot = _musicList?.ToList() ?? new List<MusicInfo>();
                recommendSnapshot = _recommendMusicList?.ToList() ?? new List<MusicInfo>();
                customPlaylistsSnapshot = _customPlaylistMusicLists?.Values
                    .Select(list => list?.ToList() ?? new List<MusicInfo>())
                    .ToList() ?? new List<List<MusicInfo>>();
            }

            var allMusic = new List<MusicInfo>();
            allMusic.AddRange(musicListSnapshot);
            allMusic.AddRange(recommendSnapshot.Where(m => !musicListSnapshot.Any(f => f.UUID == m.UUID)));

            foreach (var playlist in customPlaylistsSnapshot)
            {
                allMusic.AddRange(playlist.Where(m => !allMusic.Any(e => e.UUID == m.UUID)));
            }

            return Task.FromResult(allMusic);
        }

        public Task<AudioClip> LoadAudioAsync(string uuid)
        {
            return LoadAudioAsync(uuid, CancellationToken.None);
        }

        public Task<AudioClip> LoadAudioAsync(string uuid, CancellationToken cancellationToken)
        {
            // Not used for streaming - we use ResolveAsync instead
            return Task.FromResult<AudioClip>(null);
        }

        public void UnloadAudio(string uuid)
        {
            // Nothing to unload for streaming
        }

        public async Task RefreshAsync()
        {
            if (!_isLoggedIn) return;

            await ScanAndRegisterAsync();

            _context.EventBus.Publish(new PlaylistUpdatedEvent
            {
                TagId = QQMusicSongRegistry.TAG_FAVORITES,
                UpdateType = PlaylistUpdateType.FullRefresh
            });
        }

        public async Task<PlayableSource> ResolveAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default)
        {
            // Handle login song - 触发二维码登录
            if (uuid == _currentLoginSongUuid || uuid == "qqmusic_login_song")
            {
                if (_qrLoginManager != null && (_qrLoginManager.QRCodeSprite == null || _qrLoginManager.LoginType != "qq"))
                {
                    await _qrLoginManager.StartLoginAsync("qq");
                }
                _context.EventBus.Publish(new CoverInvalidatedEvent { MusicUuid = uuid, Reason = "login song played" });
                return CreateSilentSource(uuid);
            }
            if (uuid == _wxLoginSongUuid || uuid == "qqmusic_login_song_wx")
            {
                if (_qrLoginManager != null && (_qrLoginManager.QRCodeSprite == null || _qrLoginManager.LoginType != "wx"))
                {
                    await _qrLoginManager.StartLoginAsync("wx");
                }
                _context.EventBus.Publish(new CoverInvalidatedEvent { MusicUuid = uuid, Reason = "login song played" });
                return CreateSilentSource(uuid);
            }

            // Get song info
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                _logger?.LogWarning($"Song not found: {uuid}");
                return null;
            }

            // Check streaming service availability
            if (!_context.StreamingService.IsAvailable)
            {
                _logger?.LogError("流式服务不可用 (原生解码器未加载)");
                return null;
            }

            // Map quality and get song URL
            var bridgeQuality = MapQuality(quality);
            var timeoutMs = _streamReadyTimeoutMs?.Value ?? 20000;
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var songUrl = await Task.Run(() => _bridge.GetSongURL(songInfo.Mid, bridgeQuality), cancellationToken);
                if (songUrl == null || string.IsNullOrEmpty(songUrl.URL))
                {
                    _logger?.LogWarning($"Failed to get song URL for {songInfo.Name} (attempt {attempt}/{maxRetries})");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    return null;
                }

                // Determine format
                var format = !string.IsNullOrEmpty(songUrl.Format) ? songUrl.Format.ToLowerInvariant() : "mp3";
                var audioFormat = string.Equals(format, "flac", StringComparison.OrdinalIgnoreCase)
                    ? AudioFormat.Flac
                    : AudioFormat.Mp3;

                _logger?.LogInfo($"Got URL for {songInfo.Name} [format={format}, size={songUrl.Size}] (attempt {attempt}/{maxRetries})");

                // Use main plugin's streaming service
                var reader = await _context.StreamingService.CreateStreamAndWaitAsync(
                    songUrl.URL,
                    format,
                    (float)songInfo.Duration,
                    $"qqmusic_{songInfo.Mid}",
                    timeoutMs,
                    new Dictionary<string, string> { ["User-Agent"] = "Mozilla/5.0" },
                    cancellationToken);

                if (reader != null)
                {
                    _logger?.LogInfo($"PCM stream ready: {songInfo.Name} [{reader.Info.SampleRate}Hz, {reader.Info.Channels}ch, {reader.Info.Format ?? format}]");
                    return PlayableSource.FromPcmStream(uuid, reader, audioFormat);
                }

                _logger?.LogWarning($"PCM stream create/ready failed for {songInfo.Name} (attempt {attempt}/{maxRetries})");
                if (attempt < maxRetries)
                    await Task.Delay(1000, cancellationToken);
            }

            return null;
        }

        public Task<PlayableSource> RefreshUrlAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default)
        {
            return ResolveAsync(uuid, quality, cancellationToken);
        }

        #endregion

        #region ICoverProvider Implementation

        public Task<Sprite> GetMusicCoverAsync(string uuid)
        {
            // 登录歌曲返回二维码封面
            if ((uuid == _currentLoginSongUuid || uuid == _wxLoginSongUuid) && _qrLoginManager?.QRCodeSprite != null)
            {
                return Task.FromResult(_qrLoginManager.QRCodeSprite);
            }
            return _coverLoader.GetMusicCoverAsync(uuid);
        }

        public Task<Sprite> GetAlbumCoverAsync(string albumId)
        {
            return _coverLoader.GetAlbumCoverAsync(albumId);
        }

        public Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid)
        {
            if (_qrLoginManager?.QRCodeBytes != null)
            {
                if ((uuid == _currentLoginSongUuid && _qrLoginManager.LoginType == "qq") ||
                    (uuid == _wxLoginSongUuid && _qrLoginManager.LoginType == "wx"))
                {
                    return Task.FromResult((_qrLoginManager.QRCodeBytes, "image/png"));
                }
            }
            return _coverLoader.GetMusicCoverBytesAsync(uuid);
        }

        public void ClearCache()
        {
            _coverLoader.ClearCache();
        }

        public void RemoveMusicCoverCache(string uuid)
        {
            _coverLoader.RemoveMusicCoverCache(uuid);
        }

        public void RemoveAlbumCoverCache(string albumId)
        {
            _coverLoader.RemoveAlbumCoverCache(albumId);
        }

        #endregion

        #region IFavoriteExcludeHandler Implementation

        public bool IsFavorite(string uuid)
        {
            return _favoriteManager.IsFavorite(uuid);
        }

        public void SetFavorite(string uuid, bool isFavorite)
        {
            Task.Run(async () =>
            {
                await _favoriteManager.SetFavoriteAsync(uuid, isFavorite);
            });
        }

        public bool IsExcluded(string uuid)
        {
            var music = _musicList.FirstOrDefault(m => m.UUID == uuid)
                ?? _recommendMusicList.FirstOrDefault(m => m.UUID == uuid);
            return music?.IsExcluded ?? false;
        }

        public void SetExcluded(string uuid, bool isExcluded)
        {
            var music = _musicList.FirstOrDefault(m => m.UUID == uuid)
                ?? _recommendMusicList.FirstOrDefault(m => m.UUID == uuid);
            if (music != null)
            {
                music.IsExcluded = isExcluded;
                _context.MusicRegistry.UpdateMusic(music);
            }
        }

        public IReadOnlyList<string> GetFavorites()
        {
            return _musicList.Where(m => m.IsFavorite).Select(m => m.UUID).ToList();
        }

        public IReadOnlyList<string> GetExcluded()
        {
            var allMusic = new List<MusicInfo>();
            allMusic.AddRange(_musicList);
            allMusic.AddRange(_recommendMusicList);
            return allMusic.Where(m => m.IsExcluded).Select(m => m.UUID).ToList();
        }

        #endregion

        #region Private Methods

        private void RegisterLyricApi()
        {
            // UI instances initialize after modules, so retry until they become available.
            // Register on ALL instances (default + window-manager) so chill.custom.get("lyric") works everywhere.
            Task.Run(async () =>
            {
                const int maxRetries = 30;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        // Get OneJSBridge.Instances (Dictionary<string, UIInstance>)
                        var bridgeType = Type.GetType("ChillPatcher.OneJSBridge, ChillPatcher");
                        if (bridgeType == null)
                        {
                            _logger?.LogWarning("OneJSBridge type not found, skipping lyric API registration");
                            return;
                        }

                        var instancesProp = bridgeType.GetProperty("Instances",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var instances = instancesProp?.GetValue(null) as System.Collections.IEnumerable;
                        if (instances == null)
                        {
                            await Task.Delay(1000);
                            continue;
                        }

                        // Count total instances and how many have JSApi ready
                        int total = 0;
                        int withJsApi = 0;
                        var registeredIds = new System.Collections.Generic.List<string>();

                        foreach (var kv in instances)
                        {
                            total++;
                            var kvType = kv.GetType();
                            var valueProp = kvType.GetProperty("Value");
                            var uiInstance = valueProp?.GetValue(kv);
                            if (uiInstance == null) continue;

                            var jsApiProp = uiInstance.GetType().GetProperty("JSApi");
                            var jsApi = jsApiProp?.GetValue(uiInstance);
                            if (jsApi == null) continue;
                            withJsApi++;

                            // Check if already registered on this instance
                            var getMethod = jsApi.GetType().GetMethod("GetCustomApi");
                            var existing = getMethod?.Invoke(jsApi, new object[] { "lyric" });
                            if (existing != null) {
                                registeredIds.Add(kvType.GetProperty("Key")?.GetValue(kv) as string ?? "?");
                                continue;
                            }

                            var lyricApi = new QQMusicLyricApi(_bridge, _logger);
                            var registerMethod = jsApi.GetType().GetMethod("RegisterCustomApi");
                            registerMethod?.Invoke(jsApi, new object[] { "lyric", lyricApi });

                            var keyProp = kvType.GetProperty("Key");
                            var instanceId = keyProp?.GetValue(kv) as string ?? "?";
                            _logger?.LogInfo($"Lyric API registered on instance: {instanceId}");
                            registeredIds.Add(instanceId);
                        }

                        _logger?.LogInfo($"Lyric API: {registeredIds.Count}/{total} instances (jsApi ready: {withJsApi}), attempt {attempt + 1}");

                        // Keep retrying for at least 15 attempts to catch late-initializing instances
                        // Only stop early after minimum attempts AND all found instances are registered
                        if (attempt >= 14 && registeredIds.Count > 0)
                        {
                            _logger?.LogInfo($"Lyric API registration complete: {registeredIds.Count} instance(s)");
                            return;
                        }

                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Failed to register lyric API (attempt {attempt + 1}): {ex.Message}");
                        await Task.Delay(1000);
                    }
                }
                _logger?.LogError("Lyric API registration failed after all retries");
            });
        }

        private void RegisterConfig()
        {
            var configManager = _context.ConfigManager;

            _dataDir = configManager.Bind(
                "",
                "DataDirectory",
                "",
                "QQ Music data directory (empty = default)");

            _audioQuality = configManager.Bind(
                "",
                "AudioQuality",
                1,
                "Quality: 0=Standard(128k), 1=HQ(320k), 2=SQ(FLAC), 3=Hi-Res");

            _customPlaylistIds = configManager.Bind(
                "",
                "CustomPlaylistIds",
                "",
                "Comma-separated playlist IDs to import");

            _streamReadyTimeoutMs = configManager.Bind(
                "",
                "StreamReadyTimeoutMs",
                20000,
                "PCM stream ready timeout (milliseconds)");

        }

        private Task HandleNotLoggedInAsync()
        {
            Interlocked.Exchange(ref _loginSuccessHandling, 0);

            // 初始化 QR 登录管理器
            _qrLoginManager = new QRLoginManager(_bridge, _logger);
            _qrLoginManager.OnLoginSuccess += OnQRLoginSuccess;
            _qrLoginManager.OnStatusChanged += OnQRLoginStatusChanged;
            _qrLoginManager.OnQRCodeUpdated += OnQRCodeUpdated;

            // 必须先注册 Tag，否则登录歌曲找不到 Tag
            _songRegistry.RegisterFavoritesTag();
            _songRegistry.RegisterLoginSongAlbum();

            // QQ 登录歌曲
            var qqLoginSong = _songRegistry.RegisterLoginSong("QQ 扫码登录", "qqmusic_login_song", "请使用 QQ 扫码登录");
            _currentLoginSongUuid = qqLoginSong.UUID;
            _musicList.Add(qqLoginSong);

            // 微信登录歌曲
            var wxLoginSong = _songRegistry.RegisterLoginSong("微信扫码登录", "qqmusic_login_song_wx", "请使用微信扫码登录");
            _wxLoginSongUuid = wxLoginSong.UUID;
            _musicList.Add(wxLoginSong);

            _logger?.LogInfo("QQ音乐未登录，等待扫码登录");

            // 预先获取 QQ 二维码，这样歌曲列表显示时封面就能看到二维码
            _ = _qrLoginManager.StartLoginAsync("qq");

            return Task.CompletedTask;
        }

        private async void OnLoginSuccess()
        {
            if (Interlocked.Exchange(ref _loginSuccessHandling, 1) == 1)
            {
                _logger?.LogWarning("[QQ音乐] 忽略重复登录成功回调");
                return;
            }

            try
            {
                _logger?.LogInfo("[QQ音乐] 登录成功，开始加载音乐...");

                // 先解绑并停止轮询，避免清理过程中继续触发状态回调
                var qrManager = _qrLoginManager;
                if (qrManager != null)
                {
                    qrManager.OnLoginSuccess -= OnQRLoginSuccess;
                    qrManager.OnStatusChanged -= OnQRLoginStatusChanged;
                    qrManager.OnQRCodeUpdated -= OnQRCodeUpdated;
                    qrManager.CancelLogin();
                }

                // 清理登录歌曲
                _songRegistry.UnregisterLoginSong();
                lock (_stateLock)
                {
                    _musicList.Clear();
                    _currentLoginSongUuid = null;
                    _wxLoginSongUuid = null;
                    _qrLoginManager = null;
                }

                // 注销旧的所有专辑（包括登录专辑），重新注册
                _context.AlbumRegistry.UnregisterAllByModule(ModuleId);

                _isLoggedIn = true;

                // 加载音乐
                await ScanAndRegisterAsync();

                _logger?.LogInfo($"[QQ音乐] ✅ 登录后初始化完成，收藏 {_musicList.Count} 首");

                // 通知 UI 刷新（触发跳转到歌曲列表）
                _context.EventBus.Publish(new PlaylistUpdatedEvent
                {
                    TagId = QQMusicSongRegistry.TAG_FAVORITES,
                    UpdateType = PlaylistUpdateType.FullRefresh
                });

                _context.EventBus.Publish(new CoverInvalidatedEvent
                {
                    Reason = "Login completed"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"OnLoginSuccess error: {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _loginSuccessHandling, 0);
            }
        }

        private void OnQRLoginSuccess()
        {
            _logger?.LogInfo("[QQ音乐] QR扫码登录成功！");
            OnLoginSuccess();
        }

        private void OnQRLoginStatusChanged(string status)
        {
            _logger?.LogInfo($"[QQ音乐] QR状态: {status}");

            // 更新登录歌曲的 Artist 字段显示状态
            string loginSongUuid;
            lock (_stateLock)
            {
                loginSongUuid = _currentLoginSongUuid;
            }

            if (loginSongUuid != null)
            {
                MusicInfo loginSong;
                lock (_stateLock)
                {
                    loginSong = _musicList.FirstOrDefault(m => m.UUID == loginSongUuid);
                }

                if (loginSong != null)
                {
                    loginSong.Artist = status;
                    _context.MusicRegistry.UpdateMusic(loginSong);
                }
            }
        }

        private void OnQRCodeUpdated(Sprite newQRCode)
        {
            // 清除两首登录歌曲的封面缓存，让 UI 重新加载新的二维码
            string qqUuid, wxUuid;
            lock (_stateLock)
            {
                qqUuid = _currentLoginSongUuid;
                wxUuid = _wxLoginSongUuid;
            }

            // 清除 QQ 登录歌曲封面缓存
            if (!string.IsNullOrEmpty(qqUuid))
            {
                _coverLoader?.RemoveMusicCoverCache(qqUuid);
                _context.EventBus.Publish(new CoverInvalidatedEvent
                {
                    MusicUuid = qqUuid,
                    Reason = "QR code updated"
                });
            }
            // 清除微信登录歌曲封面缓存
            if (!string.IsNullOrEmpty(wxUuid))
            {
                _coverLoader?.RemoveMusicCoverCache(wxUuid);
                _context.EventBus.Publish(new CoverInvalidatedEvent
                {
                    MusicUuid = wxUuid,
                    Reason = "QR code updated"
                });
            }
        }

        private async Task ScanAndRegisterAsync()
        {
            try
            {
                _logger?.LogInfo("ScanAndRegisterAsync: Starting...");

                // Load favorites
                await _favoriteManager.LoadLikeListAsync();

                _logger?.LogInfo("ScanAndRegisterAsync: Getting like songs...");
                var likeSongs = await Task.Run(() => _bridge.GetLikeSongs(true));
                _logger?.LogInfo($"ScanAndRegisterAsync: Got {likeSongs?.Count ?? 0} like songs, error: {_bridge.GetLastError()}");

                // 即使收藏为空也注册收藏 Tag/专辑，避免 UI 在刷新时访问到缺失实体
                _songRegistry.RegisterFavoritesTag();
                _songRegistry.RegisterFavoritesAlbum(likeSongs?.Count ?? 0);

                var registeredFavorites = likeSongs != null && likeSongs.Count > 0
                    ? _songRegistry.RegisterFavoritesSongs(likeSongs, _songInfoMap)
                    : new List<MusicInfo>();

                lock (_stateLock)
                {
                    _musicList = registeredFavorites;
                }

                _logger?.LogInfo($"Registered {registeredFavorites.Count} favorite songs");

                // Import custom playlists
                await ImportCustomPlaylistsAsync();
                _logger?.LogInfo("ScanAndRegisterAsync: Completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"ScanAndRegisterAsync error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task<int> LoadMoreRecommendSongsAsync()
        {
            try
            {
                var songs = await Task.Run(() => _bridge.GetRecommendSongs());
                if (songs == null || songs.Count == 0) return 0;

                var newSongs = songs.Where(s =>
                    !_recommendMusicList.Any(m => m.UUID == QQMusicSongRegistry.GenerateUUID(s.Mid))).ToList();

                if (newSongs.Count == 0) return 0;

                var newMusic = _songRegistry.RegisterRecommendSongs(newSongs, _songInfoMap, _musicList);
                _recommendMusicList.AddRange(newMusic);

                return newMusic.Count;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"LoadMoreRecommendSongsAsync error: {ex.Message}");
                return 0;
            }
        }

        private async Task ImportCustomPlaylistsAsync()
        {
            var playlistIdsStr = _customPlaylistIds?.Value;
            if (string.IsNullOrWhiteSpace(playlistIdsStr)) return;

            var ids = playlistIdsStr.Split(',')
                .Select(s => s.Trim())
                .Where(s => long.TryParse(s, out _))
                .Select(long.Parse)
                .ToList();

            foreach (var playlistId in ids)
            {
                try
                {
                    var detail = await Task.Run(() => _bridge.GetPlaylistSongs(playlistId));
                    if (detail == null || detail.Songs == null) continue;

                    _songRegistry.RegisterPlaylistTag(playlistId, detail.DissName);
                    _songRegistry.RegisterPlaylistAlbum(playlistId, detail.DissName, detail.SongCount, detail.CoverUrl);

                    var musicList = _songRegistry.RegisterPlaylistSongs(playlistId, detail.Songs, _songInfoMap);
                    _customPlaylistMusicLists[playlistId] = musicList;

                    _logger?.LogInfo($"Imported playlist: {detail.DissName} ({musicList.Count} songs)");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to import playlist {playlistId}: {ex.Message}");
                }
            }
        }

        private void OnFavoriteChanged(FavoriteChangedEvent evt)
        {
            _favoriteManager.HandleFavoriteChanged(evt, ModuleId, (uuid, isFavorite) =>
            {
                var music = _musicList.FirstOrDefault(m => m.UUID == uuid)
                    ?? _recommendMusicList.FirstOrDefault(m => m.UUID == uuid);

                if (music != null)
                {
                    music.IsFavorite = isFavorite;
                    _context.MusicRegistry.UpdateMusic(music);

                    // If favorited from recommend, move to favorites
                    if (isFavorite && _recommendMusicList.Contains(music))
                    {
                        _songRegistry.MoveSongToFavorites(uuid, _recommendMusicList, _musicList);
                    }
                }
            });
        }

        private QQMusicBridge.AudioQuality MapQuality(AudioQuality quality)
        {
            // First check config override
            var configQuality = _audioQuality?.Value ?? 1;
            if (configQuality >= 0 && configQuality <= 3)
            {
                return configQuality switch
                {
                    0 => QQMusicBridge.AudioQuality.Standard,
                    1 => QQMusicBridge.AudioQuality.HQ,
                    2 => QQMusicBridge.AudioQuality.SQ,
                    3 => QQMusicBridge.AudioQuality.HiRes,
                    _ => QQMusicBridge.AudioQuality.HQ
                };
            }

            // Fall back to SDK quality
            return quality switch
            {
                AudioQuality.Standard => QQMusicBridge.AudioQuality.Standard,
                AudioQuality.Higher => QQMusicBridge.AudioQuality.HQ,
                AudioQuality.ExHigh => QQMusicBridge.AudioQuality.HQ,
                AudioQuality.Lossless => QQMusicBridge.AudioQuality.SQ,
                AudioQuality.HiRes => QQMusicBridge.AudioQuality.HiRes,
                _ => QQMusicBridge.AudioQuality.HQ
            };
        }

        private PlayableSource CreateSilentSource(string uuid)
        {
            // Return a silent PCM stream for login song
            var reader = new SilentPcmReader(120f);
            return PlayableSource.FromPcmStream(uuid, reader, AudioFormat.Mp3);
        }

        #endregion
    }
}
