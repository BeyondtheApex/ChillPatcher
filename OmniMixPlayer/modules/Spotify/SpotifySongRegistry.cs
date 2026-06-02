using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Module.Spotify
{
    /// <summary>
    /// 歌曲/专辑/标签注册辅助类，负责将 Spotify 数据注册到 ChillPatcher 注册表中。
    /// </summary>
    public class SpotifySongRegistry
    {
        public const string CONNECT_LIVE_SOURCE = "spotify_connect_live";
        public static readonly string CONNECT_LIVE_UUID = MusicInfo.GenerateUUID(CONNECT_LIVE_SOURCE);
        public const string TAG_CONNECT = "spotify_connect";
        public const string ALBUM_CONNECT = "spotify_connect_album";
        public const string TAG_LIKED = "spotify_liked_songs";
        public const string ALBUM_LIKED = "spotify_liked_album";

        private readonly IModuleContext _context;
        private readonly string _moduleId;
        private readonly ILogger _logger;

        public SpotifySongRegistry(IModuleContext context, string moduleId)
        {
            _context = context;
            _moduleId = moduleId;
            _logger = context.Logger;
        }

        public void RegisterConnectLive()
        {
            _context.TagRegistry.RegisterTag(TAG_CONNECT, "Spotify Connect", _moduleId);

            _context.AlbumRegistry.RegisterAlbum(new AlbumInfo
            {
                AlbumId = ALBUM_CONNECT,
                DisplayName = "Spotify Connect",
                Artist = "Spotify",
                TagId = TAG_CONNECT,
                ModuleId = _moduleId,
                SortOrder = -100,
                SongCount = 1
            }, _moduleId);

            _context.MusicRegistry.RegisterMusic(new MusicInfo
            {
                UUID = CONNECT_LIVE_UUID,
                Title = "Spotify Connect Live",
                Artist = "Spotify",
                AlbumId = ALBUM_CONNECT,
                TagId = TAG_CONNECT,
                SourceType = MusicSourceType.Stream,
                SourcePath = CONNECT_LIVE_SOURCE,
                Duration = 0,
                ModuleId = _moduleId,
                IsUnlocked = true,
                ExtendedData = new SpotifyTrackMeta
                {
                    SpotifyUri = CONNECT_LIVE_SOURCE,
                    IsConnectLive = true
                }
            }, _moduleId);
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
                CoverPath = playlist.BestCoverUrl,
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
                    CoverUrl = track.BestCoverUrl,
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
            _logger.LogInformation($"Registered {musicList.Count} tracks for [{tagId}]");
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
        public bool IsConnectLive { get; set; }
    }

}
