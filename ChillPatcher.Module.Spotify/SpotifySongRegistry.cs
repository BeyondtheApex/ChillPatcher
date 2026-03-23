using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Module.Spotify
{
    /// <summary>
    /// 歌曲/专辑/标签注册辅助类，负责将 Spotify 数据注册到 ChillPatcher 注册表中。
    /// </summary>
    public class SpotifySongRegistry
    {
        public const string TAG_LOGIN = "spotify_login_tag";
        public const string ALBUM_LOGIN = "spotify_login_album";
        public const string UUID_LOGIN = "spotify_login_action";
        public const string TAG_LIKED = "spotify_liked_songs";
        public const string ALBUM_LIKED = "spotify_liked_album";
        public const string TAG_DEVICES = "spotify_devices_tag";
        public const string ALBUM_DEVICES = "spotify_devices_album";
        public const string UUID_DEVICE_SELECT = "spotify_device_select";

        private readonly IModuleContext _context;
        private readonly string _moduleId;
        private readonly ManualLogSource _logger;

        public SpotifySongRegistry(IModuleContext context, string moduleId)
        {
            _context = context;
            _moduleId = moduleId;
            _logger = context.Logger;
        }

        // =====================================================================
        // 登录歌曲（触发 OAuth 流程）
        // =====================================================================

        public void RegisterLoginSong(string statusText)
        {
            _context.TagRegistry.RegisterTag(TAG_LOGIN, "Spotify 登录", _moduleId);

            _context.AlbumRegistry.RegisterAlbum(new AlbumInfo
            {
                AlbumId = ALBUM_LOGIN,
                DisplayName = "Spotify 登录",
                Artist = "ChillPatcher",
                TagId = TAG_LOGIN,
                ModuleId = _moduleId,
                SortOrder = 0
            }, _moduleId);

            _context.MusicRegistry.RegisterMusic(new MusicInfo
            {
                UUID = UUID_LOGIN,
                Title = "Spotify 登录",
                Artist = statusText,
                AlbumId = ALBUM_LOGIN,
                TagId = TAG_LOGIN,
                SourceType = MusicSourceType.Stream,
                SourcePath = "login_trigger",
                Duration = 120f,
                ModuleId = _moduleId,
                IsUnlocked = true
            }, _moduleId);
        }

        public void UnregisterLoginSong()
        {
            _context.MusicRegistry.UnregisterMusic(UUID_LOGIN);
            _context.AlbumRegistry.UnregisterAlbum(ALBUM_LOGIN);
            _context.TagRegistry.UnregisterTag(TAG_LOGIN);
        }

        public void UpdateLoginStatus(string statusText)
        {
            var music = _context.MusicRegistry.GetMusic(UUID_LOGIN);
            if (music != null)
            {
                music.Artist = statusText;
            }
        }

        // =====================================================================
        // Liked Songs（用户收藏）
        // =====================================================================

        public void RegisterLikedSongs(List<SpotifyTrack> tracks)
        {
            _context.TagRegistry.RegisterTag(TAG_LIKED, "Liked Songs", _moduleId);

            _context.AlbumRegistry.RegisterAlbum(new AlbumInfo
            {
                AlbumId = ALBUM_LIKED,
                DisplayName = "Liked Songs",
                Artist = "Spotify",
                TagId = TAG_LIKED,
                ModuleId = _moduleId,
                SortOrder = 0,
                SongCount = tracks.Count
            }, _moduleId);

            RegisterTracks(tracks, TAG_LIKED, ALBUM_LIKED);
        }

        // =====================================================================
        // 歌单注册
        // =====================================================================

        public void RegisterPlaylist(SpotifyPlaylist playlist, List<SpotifyTrack> tracks)
        {
            var tagId = $"spotify_playlist_{playlist.Id}";
            var albumId = $"spotify_album_{playlist.Id}";

            _context.TagRegistry.RegisterTag(tagId, playlist.Name, _moduleId);

            _context.AlbumRegistry.RegisterAlbum(new AlbumInfo
            {
                AlbumId = albumId,
                DisplayName = playlist.Name,
                Artist = playlist.Owner?.DisplayName ?? "Spotify",
                TagId = tagId,
                ModuleId = _moduleId,
                SongCount = tracks.Count,
                // 歌单封面 URL 存入 ExtendedData
                ExtendedData = playlist.BestCoverUrl
            }, _moduleId);

            RegisterTracks(tracks, tagId, albumId);
        }

        // =====================================================================
        // 曲目注册
        // =====================================================================

        private void RegisterTracks(List<SpotifyTrack> tracks, string tagId, string albumId)
        {
            var musicList = new List<MusicInfo>();

            foreach (var track in tracks)
            {
                var uuid = MusicInfo.GenerateUUID($"spotify_{track.Id}");
                musicList.Add(new MusicInfo
                {
                    UUID = uuid,
                    Title = track.Name,
                    Artist = track.ArtistName,
                    AlbumId = albumId,
                    TagId = tagId,
                    SourceType = MusicSourceType.Stream,
                    SourcePath = track.Uri,  // spotify:track:xxx，用于 Connect 播放
                    Duration = track.DurationSeconds,
                    ModuleId = _moduleId,
                    IsUnlocked = true,
                    // ExtendedData 存储 track 元数据供封面和收藏使用
                    ExtendedData = new SpotifyTrackMeta
                    {
                        SpotifyId = track.Id,
                        SpotifyUri = track.Uri,
                        CoverUrl = track.BestCoverUrl
                    }
                });
            }

            _context.MusicRegistry.RegisterMusicBatch(musicList, _moduleId);
            _logger.LogInfo($"Registered {musicList.Count} tracks for [{tagId}]");
        }

        // =====================================================================
        // 设备列表
        // =====================================================================

        /// <summary>
        /// 注册"选择设备"歌曲（单首，点击弹窗）。
        /// </summary>
        public void RegisterDeviceSelector(string currentDeviceName = null)
        {
            UnregisterDeviceSelector();

            _context.TagRegistry.RegisterTag(TAG_DEVICES, "Spotify 设备", _moduleId);

            _context.AlbumRegistry.RegisterAlbum(new AlbumInfo
            {
                AlbumId = ALBUM_DEVICES,
                DisplayName = "Spotify 设备",
                Artist = "Spotify",
                TagId = TAG_DEVICES,
                ModuleId = _moduleId,
                SortOrder = -1
            }, _moduleId);

            var statusText = string.IsNullOrEmpty(currentDeviceName)
                ? ">>> 点击选择播放设备 <<<"
                : $"当前: {currentDeviceName}  (点击切换)";

            _context.MusicRegistry.RegisterMusic(new MusicInfo
            {
                UUID = UUID_DEVICE_SELECT,
                Title = "选择播放设备",
                Artist = statusText,
                AlbumId = ALBUM_DEVICES,
                TagId = TAG_DEVICES,
                SourceType = MusicSourceType.Stream,
                SourcePath = "device_select",
                Duration = 10f,
                ModuleId = _moduleId,
                IsUnlocked = true
            }, _moduleId);
        }

        public void UpdateDeviceStatus(string deviceName)
        {
            var music = _context.MusicRegistry.GetMusic(UUID_DEVICE_SELECT);
            if (music != null)
            {
                music.Artist = string.IsNullOrEmpty(deviceName)
                    ? ">>> 点击选择播放设备 <<<"
                    : $"当前: {deviceName}  (点击切换)";
            }
        }

        public void UnregisterDeviceSelector()
        {
            _context.MusicRegistry.UnregisterMusic(UUID_DEVICE_SELECT);
            _context.AlbumRegistry.UnregisterAlbum(ALBUM_DEVICES);
            _context.TagRegistry.UnregisterTag(TAG_DEVICES);
        }

        // =====================================================================
        // 清理
        // =====================================================================

        public void UnregisterAll()
        {
            _context.MusicRegistry.UnregisterAllByModule(_moduleId);
            _context.AlbumRegistry.UnregisterAllByModule(_moduleId);
            _context.TagRegistry.UnregisterAllByModule(_moduleId);
        }
    }

    /// <summary>
    /// 存储在 MusicInfo.ExtendedData 中的 Spotify 元数据。
    /// </summary>
    public class SpotifyTrackMeta
    {
        public string SpotifyId { get; set; }
        public string SpotifyUri { get; set; }
        public string CoverUrl { get; set; }
    }

}
