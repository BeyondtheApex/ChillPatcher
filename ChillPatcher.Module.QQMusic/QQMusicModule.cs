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

        // Subscriptions
        private IDisposable _favoriteChangedSubscription;

        // Config
        private ConfigEntry<string> _dataDir;
        private ConfigEntry<int> _audioQuality;
        private ConfigEntry<string> _customPlaylistIds;
        private ConfigEntry<int> _streamReadyTimeoutMs;
        private ConfigEntry<string> _manualCookie;

        #region IMusicModule Implementation

        public string ModuleId => ModuleInfo.MODULE_ID;
        public string DisplayName => ModuleInfo.MODULE_NAME;
        public string Version => ModuleInfo.MODULE_VERSION;
        public int Priority => 50;

        public ModuleCapabilities Capabilities => new ModuleCapabilities
        {
            CanDelete = false,
            CanFavorite = true,
            CanExclude = true,
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

            // Check login status
            _isLoggedIn = _bridge.IsLoggedIn;

            // Always try to set manual cookie if configured (to update cached cookie)
            if (!string.IsNullOrWhiteSpace(_manualCookie?.Value))
            {
                _logger?.LogInfo("Setting manual cookie from config...");
                if (_bridge.SetCookie(_manualCookie.Value))
                {
                    _isLoggedIn = _bridge.IsLoggedIn;
                    _logger?.LogInfo($"Manual cookie set successfully, login status: {_isLoggedIn}");
                }
                else
                {
                    _logger?.LogWarning($"Failed to set manual cookie: {_bridge.GetLastError()}");
                }
            }

            // Tags will be registered when songs are found (in ScanAndRegisterAsync)

            if (!_isLoggedIn)
            {
                // Not logged in - show login prompt
                await HandleNotLoggedInAsync();
            }
            else
            {
                // Logged in - load music
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
            var allMusic = new List<MusicInfo>();
            allMusic.AddRange(_musicList);
            allMusic.AddRange(_recommendMusicList.Where(m => !_musicList.Any(f => f.UUID == m.UUID)));

            foreach (var playlist in _customPlaylistMusicLists.Values)
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
            // Handle login song
            if (uuid == _currentLoginSongUuid || uuid == "qqmusic_login_song")
            {
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
            return _coverLoader.GetMusicCoverAsync(uuid);
        }

        public Task<Sprite> GetAlbumCoverAsync(string albumId)
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

            _manualCookie = configManager.Bind(
                "",
                "ManualCookie",
                "",
                "手动输入cookie (从浏览器复制y.qq.com的cookie，包含qqmusic_key等)");

            // Watch for manual cookie changes
            _manualCookie.SettingChanged += OnManualCookieChanged;
        }

        private void OnManualCookieChanged(object sender, EventArgs e)
        {
            var cookie = _manualCookie?.Value;
            if (string.IsNullOrWhiteSpace(cookie)) return;

            _logger?.LogInfo("Manual cookie detected, attempting login...");

            try
            {
                if (_bridge.SetCookie(cookie))
                {
                    _logger?.LogInfo("Manual cookie set successfully");
                    _isLoggedIn = true;

                    // Trigger login success flow
                    OnLoginSuccess();
                }
                else
                {
                    _logger?.LogError($"Failed to set manual cookie: {_bridge.GetLastError()}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Manual cookie error: {ex.Message}");
            }
        }

        private Task HandleNotLoggedInAsync()
        {
            // Register login UI - show message to configure cookie manually
            _songRegistry.RegisterLoginSongAlbum();
            var loginSong = _songRegistry.RegisterLoginSong("请在配置文件中填入QQ音乐Cookie");
            _currentLoginSongUuid = loginSong.UUID;
            _musicList.Add(loginSong);

            _logger?.LogWarning("QQ音乐未登录，请在配置文件中填入ManualCookie");
            return Task.CompletedTask;
        }

        private async void OnLoginSuccess()
        {
            try
            {
                _logger?.LogInfo("Cookie login successful, loading music...");

                // Clean up login UI
                _songRegistry.UnregisterLoginSong();
                _songRegistry.UnregisterLoginSongAlbum();
                _musicList.Clear();
                _currentLoginSongUuid = null;

                _isLoggedIn = true;

                // Load music
                await ScanAndRegisterAsync();

                // Notify UI
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

                if (likeSongs != null && likeSongs.Count > 0)
                {
                    // Only register favorites tag if there are songs
                    _songRegistry.RegisterFavoritesTag();
                    _songRegistry.RegisterFavoritesAlbum(likeSongs.Count);
                    _musicList = _songRegistry.RegisterFavoritesSongs(likeSongs, _songInfoMap);
                    _logger?.LogInfo($"Registered {_musicList.Count} favorite songs");
                }

                // Load recommended songs
                _logger?.LogInfo("ScanAndRegisterAsync: Getting recommend songs...");
                var recommendSongs = await Task.Run(() => _bridge.GetRecommendSongs());
                _logger?.LogInfo($"ScanAndRegisterAsync: Got {recommendSongs?.Count ?? 0} recommend songs, error: {_bridge.GetLastError()}");

                if (recommendSongs != null && recommendSongs.Count > 0)
                {
                    // Only register recommend tag if there are songs
                    _songRegistry.RegisterRecommendTag();
                    _songRegistry.RegisterRecommendAlbum(recommendSongs.Count);
                    _recommendMusicList = _songRegistry.RegisterRecommendSongs(recommendSongs, _songInfoMap, _musicList);
                    _logger?.LogInfo($"Registered {_recommendMusicList.Count} recommend songs");

                    // Set up load more callback
                    _context.TagRegistry.SetLoadMoreCallback(
                        QQMusicSongRegistry.TAG_RECOMMEND,
                        LoadMoreRecommendSongsAsync);
                }

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
            var reader = new SilentPcmReader(30f);
            return PlayableSource.FromPcmStream(uuid, reader, AudioFormat.Unknown);
        }

        #endregion
    }
}
