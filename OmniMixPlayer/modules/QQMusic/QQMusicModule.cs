using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Attributes;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;


namespace OmniMixPlayer.Module.QQMusic
{
    /// <summary>
    /// QQ Music module for ChillPatcher
    /// </summary>
    [MusicModule(ModuleInfo.MODULE_ID, ModuleInfo.MODULE_NAME,
        Version = ModuleInfo.MODULE_VERSION,
        Author = ModuleInfo.MODULE_AUTHOR,
        Description = ModuleInfo.MODULE_DESCRIPTION,
        Priority = 50)]
    public class QQMusicModule : IMusicModule, IStreamingMusicSourceProvider, ICoverProvider, IFavoriteExcludeHandler, ILyricProvider, IModuleUIProvider
    {
        private IModuleContext _context;
        private ILogger _logger;
        private QQMusicBridge _bridge;
        private QQMusicSongRegistry _songRegistry;
        private QQMusicFavoriteManager _favoriteManager;
        private QQMusicCoverLoader _coverLoader;

        // State
        private List<Track> _musicList;
        private List<Track> _recommendMusicList;
        private Dictionary<string, QQMusicBridge.SongInfo> _songInfoMap;
        private Dictionary<long, List<Track>> _customPlaylistMusicLists;
        private bool _isReady;
        private bool _isLoggedIn;
        private QRLoginManager _qrLoginManager;
        private readonly object _stateLock = new object();
        private int _loginSuccessHandling;

        // Subscriptions
        private IDisposable _favoriteChangedSubscription;

        // UI 相关
        private long _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private string _currentLoginType = "qq"; // "qq" | "wx"

        // Config (accessed via _context.ConfigManager.GetValue<T>)

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

            _musicList = new List<Track>();
            _recommendMusicList = new List<Track>();
            _songInfoMap = new Dictionary<string, QQMusicBridge.SongInfo>();
            _customPlaylistMusicLists = new Dictionary<long, List<Track>>();

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
            var dataDir = _context.ConfigManager.GetValue<string>("DataDirectory", "");
            dataDir = string.IsNullOrEmpty(dataDir)
                ? context.GetModuleDataPath(ModuleId)
                : dataDir;

            if (!_bridge.Initialize(dataDir))
            {
                _logger?.LogError($"Failed to initialize QQMusic bridge: {_bridge.GetLastError()}");
                return;
            }

            // Initialize managers
            _songRegistry = new QQMusicSongRegistry(_context, ModuleId);
            _coverLoader = new QQMusicCoverLoader(_logger, _songInfoMap);
            _favoriteManager = new QQMusicFavoriteManager(_bridge, _logger, _songInfoMap);

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
            _logger?.LogInformation("QQ Music module enabled");
        }

        public void OnDisable()
        {
            _logger?.LogInformation("QQ Music module disabled");
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
        public SourceType SourceType => SourceType.Stream;

        public Task<List<Track>> GetMusicListAsync()
        {
            return Task.FromResult(_context.Library.QueryTracks(new TrackQuery { ModuleId = ModuleId, Limit = 0 }).ToList());
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
                SourceRefId = QQMusicSongRegistry.PLAYLIST_FAVORITES,
                UpdateType = PlaylistUpdateType.FullRefresh
            });
        }

        public async Task<PlayableSource> ResolveAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default)
        {
            // Get song info
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                _logger?.LogWarning($"Song not found: {uuid}");
                return null;
            }

            // Map quality and get song URL
            var bridgeQuality = MapQuality(quality);
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

                _logger?.LogInformation($"Got URL for {songInfo.Name} [format={format}, size={songUrl.Size}] (attempt {attempt}/{maxRetries})");

                return new PlayableSource
                {
                    UUID = uuid,
                    SourceType = PlayableSourceType.Remote,
                    Url = songUrl.URL,
                    Format = audioFormat,
                    Headers = new Dictionary<string, string> { ["User-Agent"] = "Mozilla/5.0" },
                    CacheKey = $"qqmusic_{songInfo.Mid}"
                };
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

        public Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid)
        {
            return _coverLoader.GetMusicCoverAsync(uuid);
        }

        public Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId)
        {
            return _coverLoader.GetAlbumCoverAsync(albumId);
        }

        public Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid)
        {
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
            var track = _context.Library.GetTrack(uuid);
            return track?.IsExcluded ?? false;
        }

        public void SetExcluded(string uuid, bool isExcluded)
        {
            var track = _context.Library.GetTrack(uuid);
            if (track != null)
            {
                track.IsExcluded = isExcluded;
                _context.Library.UpsertTrack(track);
            }
        }

        public IReadOnlyList<string> GetFavorites()
        {
            return _context.Library.QueryTracks(new TrackQuery { ModuleId = ModuleId, IsFavorite = true, Limit = 0 })
                .Select(m => m.Uuid).ToList();
        }

        public IReadOnlyList<string> GetExcluded()
        {
            return _context.Library.QueryTracks(new TrackQuery { ModuleId = ModuleId, IsExcluded = true, Limit = 0 })
                .Select(m => m.Uuid).ToList();
        }

        #endregion

        #region Private Methods

        private Task HandleNotLoggedInAsync()
        {
            Interlocked.Exchange(ref _loginSuccessHandling, 0);

            // 初始化 QR 登录管理器
            _qrLoginManager = new QRLoginManager(_bridge, _logger);
            _qrLoginManager.OnLoginSuccess += OnQRLoginSuccess;
            _qrLoginManager.OnStatusChanged += OnQRLoginStatusChanged;
            _qrLoginManager.OnQRCodeUpdated += OnQRCodeUpdated;
            _qrLoginManager.OnLoginFailed += (err) =>
            {
                _logger?.LogError($"[QQMusic] 登录失败: {err}");
                PushUI?.Invoke(BuildUI());
            };

            _logger?.LogInformation("QQ音乐未登录，等待 UI 扫码登录");
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
                _logger?.LogInformation("[QQ音乐] 登录成功，开始加载音乐...");

                // 先解绑并停止轮询，避免清理过程中继续触发状态回调
                var qrManager = _qrLoginManager;
                if (qrManager != null)
                {
                    qrManager.OnLoginSuccess -= OnQRLoginSuccess;
                    qrManager.OnStatusChanged -= OnQRLoginStatusChanged;
                    qrManager.OnQRCodeUpdated -= OnQRCodeUpdated;
                    qrManager.CancelLogin();
                }

                lock (_stateLock)
                {
                    _musicList.Clear();
                    _qrLoginManager = null;
                }

                // 注销旧的所有专辑，重新注册
                _context.Library.UnregisterModule(ModuleId);

                _isLoggedIn = true;

                // 加载音乐
                await ScanAndRegisterAsync();

                _logger?.LogInformation($"[QQ音乐] ✅ 登录后初始化完成，收藏 {_musicList.Count} 首");

                // 通知 UI 刷新（触发跳转到歌曲列表）
                _context.EventBus.Publish(new PlaylistUpdatedEvent
                {
                    SourceRefId = QQMusicSongRegistry.PLAYLIST_FAVORITES,
                    UpdateType = PlaylistUpdateType.FullRefresh
                });

                _context.EventBus.Publish(new CoverInvalidatedEvent
                {
                    Reason = "Login completed"
                });

                // 通知 UI 更新为已登录状态
                PushUI?.Invoke(BuildUI());
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
            _logger?.LogInformation("[QQ音乐] QR扫码登录成功！");
            OnLoginSuccess();
        }


        private void OnQRLoginStatusChanged(string status)
        {
            _logger?.LogInformation($"[QQ音乐] QR状态: {status}");

            // 推送状态更新到 UI
            PushUI?.Invoke(BuildUI());
        }

        private void OnQRCodeUpdated((byte[] data, string mimeType) qrCode)
        {
            // QR 码更新后推送新 UI 到前端
            PushUI?.Invoke(BuildUI());
        }

        private async Task ScanAndRegisterAsync()
        {
            try
            {
                _logger?.LogInformation("ScanAndRegisterAsync: Starting...");

                // Load favorites
                await _favoriteManager.LoadLikeListAsync();

                _logger?.LogInformation("ScanAndRegisterAsync: Getting like songs...");
                var likeSongs = await Task.Run(() => _bridge.GetLikeSongs(true));
                _logger?.LogInformation($"ScanAndRegisterAsync: Got {likeSongs?.Count ?? 0} like songs, error: {_bridge.GetLastError()}");

                // 即使收藏为空也注册收藏 Playlist/Album，避免 UI 在刷新时访问到缺失实体
                _songRegistry.RegisterFavoritesPlaylist();

                var registeredFavorites = likeSongs != null && likeSongs.Count > 0
                    ? _songRegistry.RegisterFavoritesSongs(likeSongs, _songInfoMap)
                    : new List<Track>();

                lock (_stateLock)
                {
                    _musicList = registeredFavorites;
                }

                _logger?.LogInformation($"Registered {registeredFavorites.Count} favorite songs");

                // Import custom playlists
                await ImportCustomPlaylistsAsync();
                _logger?.LogInformation("ScanAndRegisterAsync: Completed");
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
                    !_recommendMusicList.Any(m => m.Uuid == QQMusicSongRegistry.GenerateUUID(s.Mid))).ToList();

                if (newSongs.Count == 0) return 0;

                // 确保 Playlist 和 Album 已注册
                _songRegistry.RegisterRecommendPlaylist();

                var newMusic = _songRegistry.RegisterRecommendSongs(newSongs, _songInfoMap, _musicList, _recommendMusicList.Count);
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
            var playlistIdsStr = _context.ConfigManager.GetValue<string>("CustomPlaylistIds", "");
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

                    _songRegistry.RegisterPlaylist(playlistId, detail.DissName, detail.CoverUrl);

                    var musicList = _songRegistry.RegisterPlaylistSongs(playlistId, detail.Songs, _songInfoMap);
                    _customPlaylistMusicLists[playlistId] = musicList;

                    _logger?.LogInformation($"Imported playlist: {detail.DissName} ({musicList.Count} songs)");
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
                var track = _context.Library.GetTrack(uuid);
                if (track != null)
                {
                    track.IsFavorite = isFavorite;
                    _context.Library.UpsertTrack(track);

                    // If favorited from recommend, move to favorites
                    if (isFavorite && _recommendMusicList.Any(m => m.Uuid == uuid))
                    {
                        _songRegistry.MoveSongToFavorites(uuid, _recommendMusicList, _musicList);
                    }
                }
            });
        }

        private QQMusicBridge.AudioQuality MapQuality(AudioQuality quality)
        {
            // First check config override
            var configQuality = _context.ConfigManager.GetValue("AudioQuality", 1);
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

        public string GetLyric(string uuid)
        {
            if (_songInfoMap == null || _bridge == null) return null;
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo)) return null;
            try { return _bridge.GetSongLyric(songInfo.Mid); }
            catch { return null; }
        }

        #endregion

        #region IModuleUIProvider

        public bool HasSettingsUI => true;

        public Action<SlintNode> PushUI { get; set; }

        public SlintNode BuildUI()
        {
            if (_isLoggedIn)
            {
                var audioQuality = _context?.ConfigManager?.GetValue("AudioQuality", 1) ?? 1;
                var customPlaylistIds = _context?.ConfigManager?.GetValue<string>("CustomPlaylistIds", "") ?? "";

                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(
                        SlintUi.Row(spacing: 12)
                            .AddChild(
                                SlintUi.Column(spacing: 4)
                                    .AddChild(SlintUi.Text("QQ Music", fontSize: 18))
                                    .AddChild(SlintUi.Text("已登录", fontSize: 12, color: "#4caf50"))
                            )
                    )
                    .AddChild(SlintUi.Text("播放设置", fontSize: 16))
                    .AddChild(
                        SlintUi.Select("quality", "音质", audioQuality.ToString(), new List<SlintOption>
                        {
                            new SlintOption("0", "标准"),
                            new SlintOption("1", "较高"),
                            new SlintOption("2", "无损"),
                            new SlintOption("3", "Hi-Res"),
                        })
                    )
                    .AddChild(SlintUi.Text("歌单导入", fontSize: 16))
                    .AddChild(
                        SlintUi.Column(spacing: 4)
                            .AddChild(SlintUi.Text("自定义歌单 ID (逗号分隔)", fontSize: 12, color: "#94a3b8"))
                            .AddChild(
                                SlintUi.Input("custom_playlist_ids", "例如: 123456,789012", customPlaylistIds)
                            )
                    )
                    .AddChild(
                        SlintUi.Button("logout_btn", "退出登录", variant: "danger")
                    );
            }
            else
            {
                var qrReady = _qrLoginManager?.QRCodeBytes != null;
                var qrStatusText = qrReady
                    ? (_qrLoginManager?.StatusMessage ?? "请使用 QQ 扫码登录")
                    : "二维码加载中...";

                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(SlintUi.Text("未登录", fontSize: 16))
                    .AddChild(SlintUi.Text("请扫码登录 QQ 音乐", fontSize: 12))
                    .AddChild(
                        SlintUi.Select("login_type", "登录方式", _currentLoginType, new List<SlintOption>
                        {
                            new SlintOption("qq", "QQ 登录"),
                            new SlintOption("wx", "微信登录"),
                        })
                    )
                    .AddChild(
                        SlintUi.Image("qr_image",
                            "/api/modules/" + ModuleId + "/content/qr-image?type=" + _currentLoginType + "&v=" + _qrVersion,
                            width: 200, height: 200))
                    .AddChild(SlintUi.Text(qrStatusText, fontSize: 12))
                    .AddChild(
                        SlintUi.Button("qr_refresh", qrReady ? "刷新二维码" : "获取二维码"));
            }
        }

        public void HandleUIEvent(string nodeId, string action, string value)
        {
            _logger?.LogInformation(
                "[{DisplayName}] UI Event: node={NodeId}, action={Action}, value={Value}",
                DisplayName, nodeId, action, value);

            switch (nodeId)
            {
                case "logout_btn":
                    LogoutQQMusic();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "quality":
                    if (int.TryParse(value, out var q))
                    {
                        _context?.ConfigManager?.SetValue("AudioQuality", q);
                        _context?.ConfigManager?.Save();
                        _logger?.LogInformation("[{DisplayName}] Audio quality set to {Quality}", DisplayName, q);
                    }
                    PushUI?.Invoke(BuildUI());
                    break;

                case "custom_playlist_ids":
                    _context?.ConfigManager?.SetValue("CustomPlaylistIds", value ?? "");
                    _context?.ConfigManager?.Save();
                    _logger?.LogInformation("[{DisplayName}] Custom playlist IDs set", DisplayName);
                    _ = RefreshAsync();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "login_type":
                    _currentLoginType = value == "wx" ? "wx" : "qq";
                    _ = RefreshQRCodeAsync();
                    break;

                case "qr_refresh":
                    _ = RefreshQRCodeAsync();
                    break;
            }
        }

        private async Task RefreshQRCodeAsync()
        {
            if (_qrLoginManager == null) return;
            _logger?.LogInformation("[{DisplayName}] 刷新二维码...", DisplayName);
            _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PushUI?.Invoke(BuildUI()); // 先推 UI 显示加载状态
            var success = await _qrLoginManager.StartLoginAsync(_currentLoginType);
            // OnQRCodeUpdated / OnStatusChanged 回调中已包含 PushUI
            PushUI?.Invoke(BuildUI());
        }

        public async Task<byte[]> ServeRawContent(string path)
        {
            if (path == "qr-image")
            {
                if (_qrLoginManager != null)
                {
                    // 二维码不存在或轮询已停止（超时/过期），重新开始
                    if (_qrLoginManager.QRCodeBytes == null || !_qrLoginManager.IsWaitingForLogin)
                    {
                        await _qrLoginManager.StartLoginAsync(_currentLoginType);
                    }
                    return _qrLoginManager.QRCodeBytes;
                }
            }
            return null;
        }

        public string ServeRawContentType(string path)
        {
            if (path == "qr-image") return "image/png";
            return null;
        }

        private void LogoutQQMusic()
        {
            try
            {
                _bridge?.Logout();
                _isLoggedIn = false;
                _qrLoginManager?.CancelLogin();
                _qrLoginManager = null;
                _musicList?.Clear();
                _recommendMusicList?.Clear();
                _songInfoMap?.Clear();
                _customPlaylistMusicLists?.Clear();

                _context?.Library?.UnregisterModule(ModuleId);

                // 重新初始化 QR 登录
                _qrLoginManager = new QRLoginManager(_bridge, _logger);
                _qrLoginManager.OnLoginSuccess += OnQRLoginSuccess;
                _qrLoginManager.OnStatusChanged += OnQRLoginStatusChanged;
                _qrLoginManager.OnQRCodeUpdated += OnQRCodeUpdated;
                _qrLoginManager.OnLoginFailed += (err) =>
                {
                    _logger?.LogError($"[QQMusic] 登录失败: {err}");
                    PushUI?.Invoke(BuildUI());
                };

                PushUI?.Invoke(BuildUI());

                _logger?.LogInformation("[{DisplayName}] 已退出登录", DisplayName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[{DisplayName}] 退出登录失败", DisplayName);
            }
        }

        #endregion
    }
}
