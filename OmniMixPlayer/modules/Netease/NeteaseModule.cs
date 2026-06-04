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


namespace OmniMixPlayer.Module.Netease
{
    /// <summary>
    /// 网易云音乐模块
    /// 从网易云音乐收藏歌单加载音乐
    /// </summary>
    [MusicModule(ModuleInfo.MODULE_ID, ModuleInfo.MODULE_NAME,
        Version = ModuleInfo.MODULE_VERSION,
        Author = ModuleInfo.MODULE_AUTHOR,
        Description = ModuleInfo.MODULE_DESCRIPTION,
        Priority = 50)]
    public class NeteaseModule : IMusicModule, IStreamingMusicSourceProvider, ICoverProvider, IFavoriteExcludeHandler, IDeleteHandler, IModuleUIProvider, ILyricProvider
    {
        private IModuleContext _context;
        private NeteaseBridge _bridge;
        private NeteaseCoverLoader _coverLoader;
        private PersonalFMManager _fmManager;
        private NeteaseFavoriteManager _favoriteManager;
        private NeteaseSongRegistry _songRegistry;
        private QRLoginManager _qrLoginManager;
        private NeteaseSessionManager _sessionManager;
        private NeteaseAccountApi _accountApi;

        private List<Track> _musicList = new List<Track>();
        private List<Track> _fmMusicList = new List<Track>();
        private Dictionary<string, NeteaseBridge.SongInfo> _songInfoMap = new Dictionary<string, NeteaseBridge.SongInfo>();
        private bool _isReady = false;
        private bool _isLoggedIn = false;

        // 登录歌曲常量
        private const string LOGIN_SONG_UUID_PREFIX = "netease_qr_login_";
        private const string LOGIN_ALBUM_ID = "netease_login_album";
        private const string LOGIN_SONG_TITLE = "网易云扫码登录";
        private const float LOGIN_SONG_DURATION = 120f; // 2 分钟

        // 当前登录歌曲的 UUID（每次登录生成新的）
        private string _currentLoginSongUuid;
        private IDisposable _playStartedSubscription;
        private IDisposable _favoriteChangedSubscription;

        // QR 码版本号：每次刷新变化，用于 Flutter 端破坏图片缓存
        private long _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


        // 文件缓存管理器
        private NeteaseFileCache _fileCache;
        // 待写标签队列：UUID → (cachePath, songInfo, expectedSize)
        private readonly Dictionary<string, (string cachePath, NeteaseBridge.SongInfo songInfo, long expectedSize)> _pendingTags
            = new Dictionary<string, (string, NeteaseBridge.SongInfo, long)>();
        private IDisposable _resourcesReleasedSubscription;

        // 自定义歌单
        private Dictionary<long, List<Track>> _customPlaylistMusicLists = new Dictionary<long, List<Track>>();

        #region IMusicModule

        public string ModuleId => ModuleInfo.MODULE_ID;
        public string DisplayName => ModuleInfo.MODULE_NAME;
        public string Version => ModuleInfo.MODULE_VERSION;
        public int Priority => 50;

        public ModuleCapabilities Capabilities => new ModuleCapabilities
        {
            CanDelete = true,
            CanFavorite = true,
            CanExclude = false,
            SupportsLiveUpdate = false,
            ProvidesCover = true,
            ProvidesAlbum = true,
            ProvidesPlaylist = true
        };

        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            _bridge = new NeteaseBridge(context.Logger);
            _coverLoader = new NeteaseCoverLoader(context.Logger);

            // 注册配置项
            RegisterConfig();

            // 初始化文件缓存管理器
            _fileCache = new NeteaseFileCache(context.Logger);
            _fileCache.CacheDirectory = _context.ConfigManager.GetString("MusicCacheDirectory", "");
            _fileCache.UseReadableName = _context.ConfigManager.GetBool("MusicCacheReadableName", false);

            // 订阅资源释放事件，在文件锁释放后写入 ID3 标签和歌词
            _resourcesReleasedSubscription = context.EventBus.Subscribe<MusicResourcesReleasedEvent>(OnMusicResourcesReleased);

            // 使用 DependencyLoader 加载原生 DLL
            var loaded = context.DependencyLoader.LoadNativeLibrary(
                "ChillNetease.dll",
                ModuleId);

            if (!loaded)
            {
                context.Logger.LogError($"[{DisplayName}] 无法加载 ChillNetease.dll");
                context.Logger.LogInformation($"[{DisplayName}] 请确保 DLL 位于模块的 native/x64/ 目录中");
                return;
            }

            // 初始化桥接
            if (!_bridge.Initialize(_context.ConfigManager.GetString("DataDirectory", "")))
            {
                context.Logger.LogError($"[{DisplayName}] 初始化失败");
                return;
            }

            // 信任 cookie 文件，不做额外 API 验证（避免因验证接口问题误删有效 cookie）
            _isLoggedIn = _bridge.IsLoggedIn;
            if (!_isLoggedIn)
            {
                context.Logger.LogWarning($"[{DisplayName}] 未登录网易云音乐，显示二维码登录");

                // 初始化二维码登录管理器
                _qrLoginManager = new QRLoginManager(_bridge, context.Logger);
                _qrLoginManager.OnLoginSuccess += OnQRLoginSuccess;
                _qrLoginManager.OnStatusChanged += OnQRLoginStatusChanged;
                _qrLoginManager.OnQRCodeUpdated += OnQRCodeUpdated;
                _qrLoginManager.OnLoginFailed += OnQRLoginFailed;

                // 初始化会话管理器（未登录状态，绑定 QR 登录）
                _sessionManager = new NeteaseSessionManager(_bridge, context.Logger);
                _sessionManager.SetQRLoginManager(_qrLoginManager);

                // 注册收藏专辑（包含登录歌曲）
                RegisterLoginSongAlbum();

                // 注册登录歌曲
                RegisterLoginSong("请使用网易云 APP 扫码");

                // 监听播放事件：切换到其他歌曲时取消 QR 等待
                _playStartedSubscription = _context.EventBus.Subscribe<PlayStartedEvent>(OnPlayStartedBeforeLogin);

                // 注册账户 API（未登录模式也需要，供 UI 触发登录）
                _accountApi = new NeteaseAccountApi(_sessionManager, context.Logger);
                _accountApi.SetQRLoginManager(_qrLoginManager);

                _isReady = true;
                OnReadyStateChanged?.Invoke(_isReady);

                context.Logger.LogInformation($"[{DisplayName}] ✅ 初始化完成（未登录模式）");
                return;
            }

            // 初始化会话管理器（已登录状态，验证会话并获取 VIP 信息）
            _sessionManager = new NeteaseSessionManager(_bridge, context.Logger);
            await _sessionManager.ValidateAndRefreshAsync();

            // 获取用户信息
            var userInfo = _bridge.GetUserInfo();
            if (userInfo != null)
            {
                context.Logger.LogInformation($"[{DisplayName}] 已登录: {userInfo.Nickname} (ID: {userInfo.UserId})");
            }

            // 初始化辅助管理器
            _favoriteManager = new NeteaseFavoriteManager(_bridge, context.Logger, _songInfoMap);
            _songRegistry = new NeteaseSongRegistry(context, ModuleId, _songInfoMap, _favoriteManager);
            if (IsPersonalFMEnabled)
            {
                _fmManager = new PersonalFMManager(_bridge);
            }

            // 获取并缓存收藏歌曲 ID 列表
            await _favoriteManager.LoadLikeListAsync();

            // 扫描并注册收藏歌曲
            await ScanAndRegisterAsync();

            if (IsPersonalFMEnabled)
            {
                // 初始化个人 FM 并注册初始歌曲
                await InitializePersonalFMAsync();
            }

            // 搜索并注册自定义歌单（如"献给聪音"）
            await SearchAndRegisterCustomPlaylistsAsync();

            // 根据 ID 导入指定歌单
            await ImportPlaylistsByIdAsync();

            // 订阅收藏变化事件
            SubscribeToFavoriteEvents();

            // 注册歌词 API 和账户 API

            _isReady = true;
            OnReadyStateChanged?.Invoke(_isReady);

            // 统计自定义歌单歌曲数
            var customSongCount = _customPlaylistMusicLists.Values.Sum(list => list.Count);
            var fmPart = IsPersonalFMEnabled ? $"，FM {_fmMusicList.Count} 首" : string.Empty;
            context.Logger.LogInformation($"[{DisplayName}] ✅ 初始化完成，收藏 {_musicList.Count} 首{fmPart}，自定义歌单 {customSongCount} 首");
        }

        public void OnEnable()
        {
            _context?.Logger.LogInformation($"[{DisplayName}] 已启用");
        }

        public void OnDisable()
        {
            _context?.Logger.LogInformation($"[{DisplayName}] 已禁用");
        }

        public void OnUnload()
        {
            _qrLoginManager?.CancelLogin();
            _playStartedSubscription?.Dispose();
            _playStartedSubscription = null;
            _favoriteChangedSubscription?.Dispose();
            _favoriteChangedSubscription = null;
            _sessionManager = null;
            _accountApi = null;
            _resourcesReleasedSubscription?.Dispose();
            _resourcesReleasedSubscription = null;
            _pendingTags.Clear();
            _musicList.Clear();
            _fmMusicList.Clear();
            _songInfoMap.Clear();
            _isReady = false;
        }

        #endregion

        #region IStreamingMusicSourceProvider

        public bool IsReady => _isReady;
        public event Action<bool> OnReadyStateChanged;

        public SourceType SourceType => SourceType.Url;

        public async Task<List<Track>> GetMusicListAsync()
        {
            return _context.Library.QueryTracks(new TrackQuery { ModuleId = ModuleId, Limit = 0 }).ToList();
        }

        public void UnloadAudio(string uuid)
        {
            // 流媒体无需卸载
        }

        public async Task RefreshAsync()
        {
            _context.Logger.LogInformation($"[{DisplayName}] 刷新歌曲列表...");

            // 清除旧数据
            _musicList.Clear();
            _songInfoMap.Clear();

            // 重新注销并注册
            _context.Library.UnregisterModule(ModuleId);

            // 重新扫描
            await ScanAndRegisterAsync();
        }

        #endregion

        #region IPlayableSourceResolver

        public async Task<PlayableSource> ResolveAsync(string uuid, AudioQuality quality = AudioQuality.ExHigh, CancellationToken cancellationToken = default)
        {
            // 处理登录歌曲（使用前缀匹配，因为每次 UUID 都不同）
            if (IsLoginSongUuid(uuid))
            {
                return await ResolveLoginSongAsync(uuid, cancellationToken);
            }

            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                _context.Logger.LogWarning($"[{DisplayName}] 未找到歌曲: {uuid}");
                return null;
            }

            // 使用用户配置的音质（如果没有指定则使用配置值）
            var effectiveQuality = GetEffectiveQuality(quality);
            var bridgeQuality = MapQuality(effectiveQuality);

            // 从配置读取重试和超时设置
            int maxRetries = _context.ConfigManager.GetInt("StreamMaxRetries", 3);
            int readyTimeoutMs = _context.ConfigManager.GetInt("StreamReadyTimeoutMs", 20000);

            // 歌曲元信息（用于缓存文件名）
            string artist = songInfo.ArtistName ?? "Unknown";
            string songName = songInfo.Name ?? "Unknown";

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // 在线程池执行 P/Invoke（GetSongUrl 调用 Go DLL 发 HTTP 请求，会阻塞调用线程）
                var songUrl = await Task.Run(() => _bridge.GetSongUrl(songInfo.Id, bridgeQuality), cancellationToken);
                if (songUrl == null || string.IsNullOrEmpty(songUrl.Url))
                {
                    _context.Logger.LogWarning($"[{DisplayName}] 获取歌曲 URL 失败: {songInfo.Name} (尝试 {attempt}/{maxRetries})");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    return null;
                }

                // 确定格式
                var format = !string.IsNullOrEmpty(songUrl.Type) ? songUrl.Type.ToLowerInvariant() : "mp3";
                var audioFormat = string.Equals(format, "flac", StringComparison.OrdinalIgnoreCase)
                    ? AudioFormat.Flac
                    : AudioFormat.Mp3;

                _context.Logger.LogInformation($"[{DisplayName}] 获取到歌曲 URL: {songInfo.Name} [format={format}, size={songUrl.Size}, isTrial={songUrl.IsTrial}]");

                // 检查本地缓存（大小验证 + 懒迁移）
                string localPath = _fileCache != null
                    ? _fileCache.FindValidCache(songInfo.Id, artist, songName, format, songUrl.Size)
                    : null;

                if (localPath != null)
                {
                    _context.Logger.LogInformation($"[{DisplayName}] 使用本地缓存: {songInfo.Name} [{localPath}]");
                }
                else
                {
                    // 无本地缓存时才做试听检测和会话恢复
                    if (songUrl.IsTrial && _sessionManager != null)
                    {
                        _context.Logger.LogWarning($"[{DisplayName}] Trial song detected: {songInfo.Name}, attempting recovery...");
                        var recoveryResult = await _sessionManager.HandleTrialAsync(songInfo.Id, bridgeQuality, cancellationToken);
                        switch (recoveryResult)
                        {
                            case TrialRecoveryResult.Recovered:
                                songUrl = _sessionManager.RecoveredSongUrl;
                                _context.Logger.LogInformation($"[{DisplayName}] Recovery succeeded for: {songInfo.Name}");
                                break;
                            case TrialRecoveryResult.VipRestricted:
                                _context.Logger.LogWarning($"[{DisplayName}] Song requires VIP: {songInfo.Name}, skipping");
                                return null;
                            case TrialRecoveryResult.NetworkError:
                                _context.Logger.LogWarning($"[{DisplayName}] Network error during recovery for: {songInfo.Name}, skipping");
                                return null;
                        }
                    }

                    // 恢复后仍为试听版本，跳过
                    if (songUrl.IsTrial)
                    {
                        _context.Logger.LogWarning($"[{DisplayName}] 歌曲为试听版本, 跳过: {songInfo.Name}");
                        return null;
                    }
                }

                // 确定缓存路径（本地已有则复用，否则生成新路径）
                string cachePath = localPath
                    ?? (_fileCache != null
                        ? _fileCache.GetCachePath(songInfo.Id, artist, songName, format)
                        : System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chillpatcher_audio_cache", $"netease_{songInfo.Id}.{format}"));

                // 记录待写标签信息，在歌曲资源释放后异步写入
                _pendingTags[uuid] = (cachePath, songInfo, songUrl.Size);

                return new PlayableSource
                {
                    UUID = uuid,
                    SourceType = PlayableSourceType.Remote,
                    Url = songUrl.Url ?? "",
                    Format = audioFormat,
                    Headers = new Dictionary<string, string> { ["User-Agent"] = "Mozilla/5.0" },
                    CachePath = cachePath,
                    UseCachePath = true
                };
            }

            return null;
        }

        public async Task<PlayableSource> RefreshUrlAsync(string uuid, AudioQuality quality = AudioQuality.ExHigh, CancellationToken cancellationToken = default)
        {
            return await ResolveAsync(uuid, quality, cancellationToken);
        }

        /// <summary>
        /// 处理登录歌曲的播放 - 返回静音流并启动二维码登录
        /// </summary>
        private async Task<PlayableSource> ResolveLoginSongAsync(string uuid, CancellationToken cancellationToken)
        {
            // 如果二维码已经就绪，直接返回
            if (_qrLoginManager != null && _qrLoginManager.QRCodeBytes != null)
            {
                _context.Logger.LogInformation($"[{DisplayName}] 二维码已就绪，直接返回");
            }
            else if (_qrLoginManager != null)
            {
                _context.Logger.LogInformation($"[{DisplayName}] 开始二维码登录流程...");
                var success = await _qrLoginManager.StartLoginAsync();
                if (success)
                {
                    UpdateLoginSongStatus("请使用网易云 APP 扫码");
                }
                else
                {
                    UpdateLoginSongStatus("获取二维码失败，请重试");
                }
            }

            // 强制刷新封面缓存
            _context.EventBus.Publish(new CoverInvalidatedEvent { MusicUuid = uuid, Reason = "login song played" });

            return null;
        }

        #endregion

        #region ICoverProvider

        public async Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid)
        {
            // 登录歌曲使用二维码作为封面
            if (IsLoginSongUuid(uuid))
            {
                var qrBytes = _qrLoginManager?.QRCodeBytes;
                return qrBytes != null ? (qrBytes, "image/png") : (_coverLoader.FavoritesCoverBytes, "image/png");
            }

            // 歌曲封面：从网易云下载
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                _context.Logger.LogDebug($"[{DisplayName}] Cover: UUID not in songInfoMap: {uuid}");
                return (_coverLoader.FavoritesCoverBytes, "image/png");
            }

            if (string.IsNullOrEmpty(songInfo.CoverUrl))
            {
                _context.Logger.LogDebug($"[{DisplayName}] Cover: CoverUrl is empty for: {uuid}");
                return (_coverLoader.FavoritesCoverBytes, "image/png");
            }

            _context.Logger.LogDebug($"[{DisplayName}] Cover: Loading from URL for: {uuid}");
            return await _coverLoader.GetCoverFromUrlAsync(songInfo.CoverUrl);
        }

        public async Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId)
        {
            if (albumId == LOGIN_ALBUM_ID)
            {
                return (_coverLoader.FavoritesCoverBytes, "image/png");
            }

            var album = _context.Library.GetAlbum(albumId);
            if (album != null && !string.IsNullOrWhiteSpace(album.CoverUri))
            {
                return await _coverLoader.GetCoverFromUrlAsync(album.CoverUri);
            }

            return (_coverLoader.DefaultCoverBytes, "image/png");
        }

        public void ClearCache()
        {
            _coverLoader?.ClearCache();
        }

        public void RemoveMusicCoverCache(string uuid)
        {
            // 登录歌曲的封面是动态生成的 QR 码，不在 _coverLoader 缓存中
            // 由于 UUID 是动态生成的，CoverService 会用新 UUID 请求新封面
            if (IsLoginSongUuid(uuid))
            {
                // 模块无法直接访问 CoverService，但登录歌曲每次登录会生成新 UUID
                // 所以 CoverService 的缓存不会命中旧的登录歌曲
                return;
            }

            // 普通歌曲：从 NeteaseCoverLoader 缓存中移除
            if (_songInfoMap.TryGetValue(uuid, out var songInfo) && !string.IsNullOrEmpty(songInfo.CoverUrl))
            {
                // 需要使用 HTTPS 版本的 URL，因为缓存时已转换
                var httpsUrl = EnsureHttps(songInfo.CoverUrl);
                _coverLoader?.RemoveCache(httpsUrl);
            }
        }

        public void RemoveAlbumCoverCache(string albumId)
        {
            // 专辑使用嵌入封面，无需清理缓存
        }

        /// <summary>
        /// 确保 URL 使用 HTTPS 协议
        /// </summary>
        private static string EnsureHttps(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url.Substring(7);
            }

            return url;
        }

        #endregion

        #region Private Methods

        private void RegisterConfig()
        {
            // Config: DataDirectory="" (TODO: set via config file)
            // Config: AudioQuality=2 (TODO: set via config file)
            // Config: SatonePlaylistKeywords="For Satone|献给聪音" (TODO: set via config file)
            // Config: CustomPlaylistIds="" (TODO: set via config file)
            // Config: StreamReadyTimeoutMs=20000 (TODO: set via config file)
            // Config: StreamMaxRetries=3 (TODO: set via config file)
            // Config: EnablePersonalFM=false (TODO: set via config file)
            // Config: MusicCacheDirectory="" (TODO: set via config file)
            // Config: MusicCacheReadableName=false (TODO: set via config file)
        }

        private bool IsPersonalFMEnabled => _context.ConfigManager.GetBool("EnablePersonalFM", false);

        #region Login Song Methods

        /// <summary>
        /// 注册登录歌曲所在的专辑
        /// </summary>
        private void RegisterLoginSongAlbum()
        {
            var album = new Album
            {
                Id = LOGIN_ALBUM_ID,
                Title = "网易云登录",
                Artist = "请扫码登录",
                ModuleId = ModuleId
            };
            _context.Library.UpsertAlbum(album);
        }

        /// <summary>
        /// 注册登录歌曲
        /// </summary>
        private void RegisterLoginSong(string statusText)
        {
            // 每次生成新的 UUID，避免缓存问题
            _currentLoginSongUuid = LOGIN_SONG_UUID_PREFIX + Guid.NewGuid().ToString("N").Substring(0, 8);

            var loginMusic = new Track
            {
                Uuid = _currentLoginSongUuid,
                Title = LOGIN_SONG_TITLE,
                Artist = statusText,
                AlbumId = LOGIN_ALBUM_ID,
                SourceType = SourceType.Stream,
                SourcePath = "login",
                Duration = LOGIN_SONG_DURATION,
                ModuleId = ModuleId,
                IsFavorite = false
            };

            _musicList.Add(loginMusic);
            _context.Library.UpsertTrack(loginMusic);
            _context.Logger.LogInformation($"[{DisplayName}] 已注册登录歌曲: {_currentLoginSongUuid}");
        }

        /// <summary>
        /// 判断是否为登录歌曲 UUID
        /// </summary>
        private bool IsLoginSongUuid(string uuid)
        {
            return uuid != null && uuid.StartsWith(LOGIN_SONG_UUID_PREFIX, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 更新登录歌曲的状态文本
        /// </summary>
        private void UpdateLoginSongStatus(string statusText)
        {
            var loginMusic = _musicList.FirstOrDefault(m => IsLoginSongUuid(m.Uuid));
            if (loginMusic != null)
            {
                loginMusic.Artist = statusText;
                _context.Library.UpsertTrack(loginMusic);
            }
        }

        /// <summary>
        /// 删除登录歌曲
        /// </summary>
        private void RemoveLoginSong()
        {
            var loginMusic = _musicList.FirstOrDefault(m => IsLoginSongUuid(m.Uuid));
            if (loginMusic != null)
            {
                _musicList.Remove(loginMusic);
                _context.Library.DeleteTrack(loginMusic.Uuid);
                _currentLoginSongUuid = null;
                _context.Logger.LogInformation($"[{DisplayName}] 已删除登录歌曲");
            }
        }

        /// <summary>
        /// 歌曲资源释放回调：文件锁已释放，异步写入 ID3 标签和歌词
        /// </summary>
        private void OnMusicResourcesReleased(MusicResourcesReleasedEvent evt)
        {
            var uuid = evt?.Music?.Uuid;
            if (string.IsNullOrEmpty(uuid)) return;

            if (!_pendingTags.TryGetValue(uuid, out var pending)) return;
            _pendingTags.Remove(uuid);

            var cachePath = pending.cachePath;
            var songInfo = pending.songInfo;
            var expectedSize = pending.expectedSize;
            var bridge = _bridge;
            var logger = _context.Logger;

            Task.Run(() =>
            {
                // 校验文件完整性：残缺文件直接删除
                if (System.IO.File.Exists(cachePath) && expectedSize > 0)
                {
                    var actualSize = new System.IO.FileInfo(cachePath).Length;
                    if (actualSize < expectedSize)
                    {
                        logger.LogWarning($"[{DisplayName}] 缓存文件不完整 ({actualSize}/{expectedSize}), 删除: {songInfo.Name}");
                        try { System.IO.File.Delete(cachePath); } catch { }
                        return;
                    }
                }

                // 获取歌词（同时用于嵌入 ID3 和保存 .lrc）
                string lyricText = null;
                try { lyricText = bridge?.GetSongLyric(songInfo.Id); } catch { }

                NeteaseMediaTagger.EnsureTags(cachePath, songInfo, logger, lyricText);
                NeteaseMediaTagger.EnsureLyrics(cachePath, songInfo.Id, bridge, logger, lyricText);
            });
        }

        /// <summary>
        /// 二维码登录成功回调
        /// </summary>
        private async void OnQRLoginSuccess()
        {
            _context.Logger.LogInformation($"[{DisplayName}] 二维码登录成功！");
            _isLoggedIn = true;

            // 通知会话管理器登录成功
            _sessionManager?.NotifyLoginSuccess();

            // 停止登录前的播放监听
            _playStartedSubscription?.Dispose();
            _playStartedSubscription = null;
            _favoriteChangedSubscription?.Dispose();
            _favoriteChangedSubscription = null;

            // 获取用户信息
            var userInfo = _bridge.GetUserInfo();
            if (userInfo != null)
            {
                _context.Logger.LogInformation($"[{DisplayName}] 已登录: {userInfo.Nickname} (ID: {userInfo.UserId})");
            }

            // 删除登录歌曲
            RemoveLoginSong();

            // 注销旧的专辑
            _context.Library.UnregisterModule(ModuleId);

            // 初始化辅助管理器
            _favoriteManager = new NeteaseFavoriteManager(_bridge, _context.Logger, _songInfoMap);
            _songRegistry = new NeteaseSongRegistry(_context, ModuleId, _songInfoMap, _favoriteManager);
            if (IsPersonalFMEnabled)
            {
                _fmManager = new PersonalFMManager(_bridge);
                RegisterFMTag();
            }

            // 获取并缓存收藏歌曲 ID 列表
            await _favoriteManager.LoadLikeListAsync();

            // 扫描并注册收藏歌曲
            await ScanAndRegisterAsync();

            if (IsPersonalFMEnabled)
            {
                // 初始化个人 FM 并注册初始歌曲
                await InitializePersonalFMAsync();
            }

            // 搜索并注册自定义歌单（如"献给聪音"）
            await SearchAndRegisterCustomPlaylistsAsync();

            // 根据 ID 导入指定歌单
            await ImportPlaylistsByIdAsync();

            // 订阅收藏变化事件
            SubscribeToFavoriteEvents();

            // 注册歌词 API 和账户 API

            // 统计自定义歌单歌曲数
            var customSongCount = _customPlaylistMusicLists.Values.Sum(list => list.Count);
            var fmPart = IsPersonalFMEnabled ? $"，FM {_fmMusicList.Count} 首" : string.Empty;
            _context.Logger.LogInformation($"[{DisplayName}] ✅ 登录后初始化完成，收藏 {_musicList.Count} 首{fmPart}，自定义歌单 {customSongCount} 首");

            // 发布刷新事件
            _context.EventBus.Publish(new SDK.Events.PlaylistUpdatedEvent
            {
                SourceRefId = NeteaseSongRegistry.PLAYLIST_FAVORITES,
                UpdateType = SDK.Events.PlaylistUpdateType.FullRefresh
            });

            // 更新 UI 到已登录状态
            PushUI?.Invoke(BuildUI());
        }

        private void OnPlayStartedBeforeLogin(PlayStartedEvent evt)
        {
            var uuid = evt?.Music?.Uuid;
            if (uuid == null || IsLoginSongUuid(uuid)) return;
            _qrLoginManager?.CancelLogin();
        }

        /// <summary>
        /// 二维码登录状态变化回调
        /// </summary>
        private void OnQRLoginStatusChanged(string status)
        {
            _context.Logger.LogInformation($"[{DisplayName}] 登录状态: {status}");
            UpdateLoginSongStatus(status);
        }

        /// <summary>
        /// 二维码登录失败回调
        /// </summary>
        private void OnQRLoginFailed(string error)
        {
            _context.Logger.LogError($"[{DisplayName}] 登录失败: {error}");
            // 通知前端
            _context.EventBus.Publish(new ErrorEvent
            {
                Code = "netease_login_failed",
                Message = $"网易云登录失败: {error}"
            });
            PushUI?.Invoke(BuildUI());
        }

        /// <summary>
        /// 二维码更新回调（二维码过期后重新生成时调用）
        /// </summary>
        private void OnQRCodeUpdated(byte[] newQRCode)
        {
            // 变更版本号，使 Flutter 端请求新图片而非使用缓存
            _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 清除登录歌曲的封面缓存，以便显示新的二维码
            if (!string.IsNullOrEmpty(_currentLoginSongUuid))
            {
                _context.Logger.LogInformation($"[{DisplayName}] 二维码已更新，清除封面缓存");

                // 通过事件总线通知 CoverService 清除缓存
                _context.EventBus.Publish(new CoverInvalidatedEvent
                {
                    MusicUuid = _currentLoginSongUuid,
                    Reason = "QR code expired and regenerated"
                });
            }

            // 推送更新后的 UI（URL 中版本号已变，Flutter 会重新加载图片）
            PushUI?.Invoke(BuildUI());
        }

        #endregion

        private void SubscribeToFavoriteEvents()
        {
            _favoriteChangedSubscription?.Dispose();
            _favoriteChangedSubscription = _context.EventBus.Subscribe<FavoriteChangedEvent>(OnFavoriteChanged);
        }

        private void OnFavoriteChanged(FavoriteChangedEvent evt)
        {
            // 委托给 FavoriteManager 处理
            _favoriteManager.HandleFavoriteChanged(evt, ModuleId, (uuid, isFavorite) =>
            {
                UpdateMusicFavoriteState(uuid, isFavorite);

                // FM 歌曲收藏时，移动到收藏专辑
                if (isFavorite)
                {
                    _songRegistry.MoveSongToFavorites(uuid, _fmMusicList, _musicList);
                }
            });
        }

        private async Task ScanAndRegisterAsync()
        {
            var songs = _bridge.GetLikeSongs(true);
            if (songs == null || songs.Count == 0)
            {
                _context.Logger.LogWarning($"[{DisplayName}] 未获取到收藏歌曲");
                return;
            }

            _context.Logger.LogInformation($"[{DisplayName}] 获取到 {songs.Count} 首收藏歌曲");

            // 使用 SongRegistry 注册专辑和歌曲
            _songRegistry.RegisterFavoritesPlaylist(songs.Count);
            _musicList = _songRegistry.RegisterFavoritesSongs(songs);

            _context.Logger.LogInformation($"[{DisplayName}] 已注册 1 个专辑(歌单), {_musicList.Count} 首歌曲");
        }

        private async Task InitializePersonalFMAsync()
        {
            // 使用异步版本初始化，避免阻塞主线程
            if (!await _fmManager.InitializeAsync())
            {
                _context.Logger.LogWarning($"[{DisplayName}] 个人FM 初始化失败");
                return;
            }

            // 使用 SongRegistry 注册 FM 专辑和歌曲
            _songRegistry.RegisterFMPlaylist(_fmManager.Count);
            _fmMusicList = _songRegistry.RegisterFMSongs(_fmManager.Songs);

            _context.Logger.LogInformation($"[{DisplayName}] 个人FM 已初始化，{_fmMusicList.Count} 首歌曲");
        }

        private async Task<int> LoadMoreFMSongsAsync()
        {
            var previousCount = _fmManager.Count;

            // 使用异步版本，避免阻塞主线程
            var loaded = await _fmManager.LoadMoreAsync();

            if (loaded <= 0)
            {
                _context.Logger.LogWarning($"[{DisplayName}] 个人FM 加载更多失败");
                return 0;
            }

            // 注册新加载的歌曲
            var newSongs = _fmManager.Songs.Skip(previousCount).ToList();
            var newMusicList = _songRegistry.RegisterFMSongs(newSongs);
            _fmMusicList.AddRange(newMusicList);

            _context.Logger.LogInformation($"[{DisplayName}] 个人FM 已加载 {loaded} 首新歌曲");
            return loaded;
        }

        /// <summary>
        /// 搜索并注册自定义歌单（根据配置的关键词）
        /// </summary>
        private async Task SearchAndRegisterCustomPlaylistsAsync()
        {
            var keywords = _context.ConfigManager.GetString("SatonePlaylistKeywords", "For Satone|献给聪音");
            if (string.IsNullOrWhiteSpace(keywords))
            {
                _context.Logger.LogInformation($"[{DisplayName}] 未配置自定义歌单关键词，跳过");
                return;
            }

            _context.Logger.LogInformation($"[{DisplayName}] 搜索包含关键词的歌单: {keywords}");

            // 在后台线程中搜索歌单
            var playlists = await Task.Run(() => _bridge.SearchPlaylistsByKeyword(keywords));

            if (playlists == null || playlists.Count == 0)
            {
                _context.Logger.LogInformation($"[{DisplayName}] 未找到匹配的歌单");
                return;
            }

            _context.Logger.LogInformation($"[{DisplayName}] 找到 {playlists.Count} 个匹配的歌单");

            // 注册每个歌单
            foreach (var playlist in playlists)
            {
                await RegisterCustomPlaylistAsync(playlist);
            }
        }

        /// <summary>
        /// 注册单个自定义歌单
        /// </summary>
        private async Task RegisterCustomPlaylistAsync(NeteaseBridge.PlaylistInfo playlist)
        {
            _context.Logger.LogInformation($"[{DisplayName}] 正在注册歌单: {playlist.Name} ({playlist.SongCount} 首)");

            // 注册 Tag
            _songRegistry.RegisterPlaylist(playlist.Id, playlist.Name);

            // 获取歌单中的歌曲
            var songs = await Task.Run(() => _bridge.GetPlaylistSongs(playlist.Id));

            // 注册歌曲
            var musicList = _songRegistry.RegisterPlaylistSongs(playlist.Id, songs);
            _customPlaylistMusicLists[playlist.Id] = musicList;

            _context.Logger.LogInformation($"[{DisplayName}] ✅ 歌单 {playlist.Name} 已注册，{musicList.Count} 首歌曲");
        }

        /// <summary>
        /// 根据 ID 导入指定歌单（根据配置的 ID 列表）
        /// </summary>
        private async Task ImportPlaylistsByIdAsync()
        {
            var idsConfig = _context.ConfigManager.GetString("CustomPlaylistIds", "");
            if (string.IsNullOrWhiteSpace(idsConfig))
            {
                _context.Logger.LogInformation($"[{DisplayName}] 未配置自定义歌单 ID，跳过");
                return;
            }

            // 解析 ID 列表
            var ids = new List<long>();
            foreach (var part in idsConfig.Split(','))
            {
                var trimmed = part.Trim();
                if (long.TryParse(trimmed, out var id))
                {
                    ids.Add(id);
                }
            }

            if (ids.Count == 0)
            {
                _context.Logger.LogWarning($"[{DisplayName}] 配置的歌单 ID 格式无效: {idsConfig}");
                return;
            }

            _context.Logger.LogInformation($"[{DisplayName}] 正在导入 {ids.Count} 个歌单...");

            foreach (var id in ids)
            {
                await ImportPlaylistByIdAsync(id);
            }
        }

        /// <summary>
        /// 根据 ID 导入单个歌单
        /// </summary>
        private async Task ImportPlaylistByIdAsync(long playlistId)
        {
            _context.Logger.LogInformation($"[{DisplayName}] 正在获取歌单详情: {playlistId}");

            // 在后台线程中获取歌单详情
            var detail = await Task.Run(() => _bridge.GetPlaylistDetail(playlistId));

            if (detail == null)
            {
                _context.Logger.LogWarning($"[{DisplayName}] 无法获取歌单 {playlistId} 的详情");
                return;
            }

            _context.Logger.LogInformation($"[{DisplayName}] 歌单: {detail.Name} ({detail.Songs?.Count ?? 0} 首)");

            // 检查是否已经注册（避免重复）
            if (_customPlaylistMusicLists.ContainsKey(playlistId))
            {
                _context.Logger.LogInformation($"[{DisplayName}] 歌单 {detail.Name} 已经注册，跳过");
                return;
            }

            // 注册 Tag

            // 注册专辑
            _songRegistry.RegisterPlaylist(playlistId, detail.Name, detail.CoverUrl);

            // 注册歌曲
            if (detail.Songs != null && detail.Songs.Count > 0)
            {
                var musicList = _songRegistry.RegisterPlaylistSongs(playlistId, detail.Songs);
                _customPlaylistMusicLists[playlistId] = musicList;
                _context.Logger.LogInformation($"[{DisplayName}] ✅ 歌单 {detail.Name} 已导入，{musicList.Count} 首歌曲");
            }
            else
            {
                _context.Logger.LogWarning($"[{DisplayName}] 歌单 {detail.Name} 没有歌曲");
            }
        }

        private void UpdateMusicFavoriteState(string uuid, bool isFavorite)
        {
            var music = _musicList.FirstOrDefault(m => m.Uuid == uuid)
                     ?? _fmMusicList.FirstOrDefault(m => m.Uuid == uuid);
            if (music != null)
            {
                music.IsFavorite = isFavorite;

                _context.Library.UpsertTrack(music);
            }
        }

        /// <summary>
        /// 获取有效的音质设置
        /// 如果传入的是默认值，则使用用户配置的音质
        /// </summary>
        private AudioQuality GetEffectiveQuality(AudioQuality requestedQuality)
        {
            var configuredQuality = _context.ConfigManager.GetInt("AudioQuality", 2);
            // 配置值: 0=标准, 1=较高, 2=极高, 3=无损, 4=Hi-Res, 5=高清环绕声, 6=沉浸环绕声, 7=超清母带
            return configuredQuality switch
            {
                0 => AudioQuality.Standard,
                1 => AudioQuality.Higher,
                2 => AudioQuality.ExHigh,
                3 => AudioQuality.Lossless,
                4 => AudioQuality.HiRes,
                5 => AudioQuality.JYEffect,
                6 => AudioQuality.Sky,
                7 => AudioQuality.JYMaster,
                _ => requestedQuality
            };
        }

        private NeteaseBridge.Quality MapQuality(AudioQuality quality)
        {
            return quality switch
            {
                AudioQuality.Standard => NeteaseBridge.Quality.Standard,
                AudioQuality.Higher => NeteaseBridge.Quality.Higher,
                AudioQuality.ExHigh => NeteaseBridge.Quality.ExHigh,
                AudioQuality.Lossless => NeteaseBridge.Quality.Lossless,
                AudioQuality.HiRes => NeteaseBridge.Quality.HiRes,
                AudioQuality.JYEffect => NeteaseBridge.Quality.JYEffect,
                AudioQuality.Sky => NeteaseBridge.Quality.Sky,
                AudioQuality.JYMaster => NeteaseBridge.Quality.JYMaster,
                _ => NeteaseBridge.Quality.ExHigh
            };
        }

        #endregion

        #region IFavoriteExcludeHandler

        public bool IsFavorite(string uuid) => _favoriteManager.IsFavorite(uuid);

        public void SetFavorite(string uuid, bool isFavorite)
        {
            _favoriteManager.SetFavorite(uuid, isFavorite);
            UpdateMusicFavoriteState(uuid, isFavorite);

            // 收藏时加入收藏列表；保留原有 FM/自定义歌单成员关系。
            if (isFavorite)
            {
                _songRegistry.MoveSongToFavorites(uuid, _fmMusicList, _musicList);
            }
        }

        public bool IsExcluded(string uuid) => _favoriteManager?.IsExcluded(uuid) ?? false;

        public void SetExcluded(string uuid, bool isExcluded)
        {
            _favoriteManager?.SetExcluded(uuid, isExcluded);
        }

        public IReadOnlyList<string> GetFavorites() => _favoriteManager?.GetFavorites() ?? Array.Empty<string>();

        public IReadOnlyList<string> GetExcluded() => _favoriteManager?.GetExcluded() ?? Array.Empty<string>();

        #endregion

        #region IDeleteHandler

        /// <summary>
        /// 是否支持删除（模块级别设置）
        /// 实际删除权限由每首歌曲的 IsDeletable 控制
        /// </summary>
        public bool CanDelete => true;  // 允许删除（由歌曲级别控制）

        /// <summary>
        /// 删除歌曲
        /// - FM 专辑中的歌曲：调用 FMTrash API（不喜欢）+ 从列表移除
        /// - 收藏专辑中的歌曲（取消收藏后）：仅从列表移除，不调用 API
        /// </summary>
        public bool Delete(string uuid)
        {
            try
            {
                // 查找歌曲
                var music = _musicList.FirstOrDefault(m => m.Uuid == uuid);

                if (music == null)
                {
                    music = _fmMusicList.FirstOrDefault(m => m.Uuid == uuid);
                }

                if (music == null)
                {
                    music = _customPlaylistMusicLists.Values
                        .SelectMany(list => list)
                        .FirstOrDefault(m => m.Uuid == uuid);
                }

                if (music == null)
                {
                    _context.Logger.LogWarning($"[{DisplayName}] 未找到歌曲: {uuid}");
                    return false;
                }

                // 判断是否在 FM 播放列表中（需要调用不喜欢 API）
                bool isInFMAlbum = _fmMusicList.Any(m => m.Uuid == uuid);

                // 如果在 FM 专辑中，调用 FMTrash API
                if (isInFMAlbum)
                {
                    // 从 UUID 解析出网易云歌曲 ID
                    if (_songInfoMap.TryGetValue(uuid, out var songInfo))
                    {
                        _context.Logger.LogInformation($"[{DisplayName}] 将歌曲标记为不喜欢: {music.Title} (ID: {songInfo.Id})");
                        var trashResult = _bridge.FMTrash(songInfo.Id);
                        if (!trashResult)
                        {
                            _context.Logger.LogWarning($"[{DisplayName}] FMTrash API 调用失败，但仍会从本地列表移除");
                        }
                    }
                    else
                    {
                        _context.Logger.LogWarning($"[{DisplayName}] 未找到歌曲信息，无法调用 FMTrash API: {uuid}");
                    }
                }
                else
                {
                    // 不在 FM 专辑中（已收藏后取消收藏的歌曲），只从列表移除
                    _context.Logger.LogInformation($"[{DisplayName}] 仅从列表移除（不调用不喜欢 API）: {music.Title}");
                }

                // 从本地列表移除
                _musicList.RemoveAll(m => m.Uuid == uuid);
                _fmMusicList.RemoveAll(m => m.Uuid == uuid);
                foreach (var customList in _customPlaylistMusicLists.Values)
                    customList.RemoveAll(m => m.Uuid == uuid);

                // 从 songInfoMap 移除
                _songInfoMap.Remove(uuid);

                _context.Library.DeleteTrack(uuid);

                // 清理封面缓存
                RemoveMusicCoverCache(uuid);

                _context.Logger.LogInformation($"[{DisplayName}] 已从列表移除: {uuid}");
                return true;
            }
            catch (Exception ex)
            {
                _context.Logger.LogError($"[{DisplayName}] 删除失败: {ex}");
                return false;
            }
        }

        #endregion

        #region IModuleUIProvider

        public Action<SlintNode> PushUI { get; set; }

        public SlintNode BuildUI()
        {
            var isLoggedIn = _sessionManager?.State == SessionState.LoggedIn;

            if (isLoggedIn)
            {
                var userInfo = _bridge?.GetUserInfo();
                var nickname = userInfo?.Nickname ?? "未知用户";
                var avatarUrl = userInfo?.AvatarUrl ?? "";
                var audioQuality = _context?.ConfigManager?.GetInt("AudioQuality", 2) ?? 2;
                var fmEnabled = _context?.ConfigManager?.GetBool("EnablePersonalFM", false) ?? false;
                var customPlaylistIds = _context?.ConfigManager?.GetString("CustomPlaylistIds", "") ?? "";
                var satoneKeywords = _context?.ConfigManager?.GetString("SatonePlaylistKeywords", "For Satone|献给聪音") ?? "";

                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(
                        SlintUi.Row(spacing: 12)
                            .AddChild(
                                SlintUi.Image("avatar", string.IsNullOrEmpty(avatarUrl)
                                    ? ""
                                    : avatarUrl, width: 48, height: 48))
                            .AddChild(
                                SlintUi.Column(spacing: 4)
                                    .AddChild(SlintUi.Text(nickname, fontSize: 18))
                                    .AddChild(SlintUi.Text("已登录", fontSize: 12, color: "#4caf50"))
                            )
                    )
                    .AddChild(SlintUi.Text("播放设置", fontSize: 16))
                    .AddChild(
                        SlintUi.Select("quality", "音质", audioQuality.ToString(), new List<SlintOption>
                        {
                            new SlintOption("0", "标准"),
                            new SlintOption("1", "较高"),
                            new SlintOption("2", "极高"),
                            new SlintOption("3", "无损"),
                        })
                    )
                    .AddChild(
                        SlintUi.Switch("fm_toggle", "私人FM", fmEnabled)
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
                        SlintUi.Column(spacing: 4)
                            .AddChild(SlintUi.Text("歌单关键词搜索 (|分隔)", fontSize: 12, color: "#94a3b8"))
                            .AddChild(
                                SlintUi.Input("satone_keywords", "例如: For Satone|献给聪音", satoneKeywords)
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
                    ? (_qrLoginManager?.StatusMessage ?? "请使用网易云 APP 扫码")
                    : "二维码加载中...";

                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(SlintUi.Text("未登录", fontSize: 16))
                    .AddChild(SlintUi.Text("请使用网易云音乐 App 扫描二维码登录", fontSize: 12))
                    .AddChild(
                        SlintUi.Image("qr_image", "/api/modules/" + ModuleId + "/content/qr-image?v=" + _qrVersion,
                            width: 200, height: 200))
                    .AddChild(SlintUi.Text(qrStatusText, fontSize: 12))
                    .AddChild(
                        SlintUi.Button("qr_refresh", qrReady ? "刷新二维码" : "获取二维码"));
            }
        }

        public void HandleUIEvent(string nodeId, string action, string value)
        {
            _context?.Logger.LogInformation(
                "[{DisplayName}] UI Event: node={NodeId}, action={Action}, value={Value}",
                DisplayName, nodeId, action, value);

            switch (nodeId)
            {
                case "logout_btn":
                    LogoutNetease();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "quality":
                    if (int.TryParse(value, out var q))
                    {
                        _context?.ConfigManager?.SetValue("AudioQuality", q);
                        _context?.ConfigManager?.Save();
                        _context?.Logger.LogInformation("[{DisplayName}] Audio quality set to {Quality}", DisplayName, q);
                    }
                    PushUI?.Invoke(BuildUI());
                    break;

                case "fm_toggle":
                    var enabled = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    _context?.ConfigManager?.SetValue("EnablePersonalFM", enabled);
                    _context?.ConfigManager?.Save();
                    _context?.Logger.LogInformation("[{DisplayName}] Personal FM toggled: {Enabled}", DisplayName, enabled);
                    PushUI?.Invoke(BuildUI());
                    break;

                case "custom_playlist_ids":
                    _context?.ConfigManager?.SetValue("CustomPlaylistIds", value ?? "");
                    _context?.ConfigManager?.Save();
                    _context?.Logger.LogInformation("[{DisplayName}] Custom playlist IDs set", DisplayName);
                    _ = RefreshCustomPlaylistsAsync();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "satone_keywords":
                    _context?.ConfigManager?.SetValue("SatonePlaylistKeywords", value ?? "");
                    _context?.ConfigManager?.Save();
                    _context?.Logger.LogInformation("[{DisplayName}] Satone keywords set", DisplayName);
                    _ = RefreshCustomPlaylistsAsync();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "qr_refresh":
                    _ = RefreshQRCodeAsync();
                    break;
            }
        }

        private async Task RefreshCustomPlaylistsAsync()
        {
            // 重新搜索并导入自定义歌单
            if (!_isLoggedIn) return;
            try
            {
                var previousPlaylistIds = _context.Library
                    .QueryPlaylists(new PlaylistQuery { ModuleId = ModuleId, Limit = 0 })
                    .Where(p => p.Kind == PlaylistKind.Imported)
                    .Select(p => p.Id)
                    .ToHashSet();

                _customPlaylistMusicLists.Clear();
                await SearchAndRegisterCustomPlaylistsAsync();
                await ImportPlaylistsByIdAsync();
                CleanupRemovedCustomPlaylists(
                    previousPlaylistIds,
                    _customPlaylistMusicLists.Keys.Select(NeteaseSongRegistry.PlaylistId));

                _context?.EventBus?.Publish(new PlaylistUpdatedEvent
                {
                    SourceRefId = NeteaseSongRegistry.PLAYLIST_FAVORITES,
                    UpdateType = PlaylistUpdateType.FullRefresh
                });
            }
            catch (Exception ex)
            {
                _context?.Logger.LogError(ex, "[Netease] 刷新自定义歌单失败");
            }
        }

        private void CleanupRemovedCustomPlaylists(HashSet<string> previousPlaylistIds, IEnumerable<string> currentPlaylistIds)
        {
            var current = currentPlaylistIds.ToHashSet();
            foreach (var playlistId in previousPlaylistIds.Where(id => !current.Contains(id)))
            {
                var playlist = _context.Library.GetPlaylistWithEntries(playlistId);
                var trackUuids = playlist?.Entries.Select(e => e.TrackUuid).Distinct().ToList() ?? new List<string>();

                foreach (var uuid in trackUuids)
                    _context.Library.RemoveTrackTag(uuid, playlistId);

                _context.Library.DeletePlaylist(playlistId);
                _context.Library.DeleteTag(playlistId);

                foreach (var uuid in trackUuids)
                {
                    var remainingTags = _context.Library.GetTrackTags(uuid);
                    if (remainingTags.Count == 0)
                    {
                        _context.Library.DeleteTrack(uuid);
                        _songInfoMap.Remove(uuid);
                    }
                }
            }
        }

        private async Task RefreshQRCodeAsync()
        {
            if (_qrLoginManager == null) return;
            _context?.Logger.LogInformation("[{DisplayName}] 刷新二维码...", DisplayName);
            var success = await _qrLoginManager.StartLoginAsync();
            if (success)
            {
                _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                UpdateLoginSongStatus("请使用网易云 APP 扫码");
            }
            else
            {
                UpdateLoginSongStatus("获取二维码失败，请重试");
            }
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
                        await _qrLoginManager.StartLoginAsync();
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

        public string GetLyric(string uuid)
        {
            if (_songInfoMap == null || _bridge == null) return null;
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo)) return null;
            try { return _bridge.GetSongLyric(songInfo.Id); }
            catch { return null; }
        }

        private void LogoutNetease()
        {
            try
            {
                _bridge?.Logout();
                _isLoggedIn = false;
                _sessionManager = null;
                _favoriteManager = null;
                _fmManager = null;
                _musicList.Clear();
                _fmMusicList.Clear();
                _songInfoMap.Clear();
                _customPlaylistMusicLists.Clear();

                _context?.Library?.UnregisterModule(ModuleId);

                RegisterFavoritesTag();
                RegisterLoginSongAlbum();
                RegisterLoginSong("请使用网易云 APP 扫码");

                _qrLoginManager = new QRLoginManager(_bridge, _context?.Logger);
                _qrLoginManager.OnLoginSuccess += OnQRLoginSuccess;
                _qrLoginManager.OnStatusChanged += OnQRLoginStatusChanged;
                _qrLoginManager.OnQRCodeUpdated += OnQRCodeUpdated;
                _qrLoginManager.OnLoginFailed += OnQRLoginFailed;

                _sessionManager = new NeteaseSessionManager(_bridge, _context?.Logger);
                _sessionManager.SetQRLoginManager(_qrLoginManager);

                _context?.EventBus?.Publish(new PlaylistUpdatedEvent
                {
                    SourceRefId = NeteaseSongRegistry.PLAYLIST_FAVORITES,
                    UpdateType = PlaylistUpdateType.FullRefresh
                });

                PushUI?.Invoke(BuildUI());

                _context?.Logger?.LogInformation("[{DisplayName}] 已退出登录", DisplayName);
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogError(ex, "[{DisplayName}] 退出登录失败", DisplayName);
            }
        }

        #endregion
    }
}


