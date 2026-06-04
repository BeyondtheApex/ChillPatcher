using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.Spotify
{
    /// <summary>
    /// 歌曲/专辑/播放列表注册辅助类，负责将 Spotify 数据注册到统一 Library 中。
    /// </summary>
    public class SpotifySongRegistry
    {
        public const string CONNECT_LIVE_SOURCE = "spotify_connect_live";
        public const string PLAYLIST_CONNECT = "spotify_connect";
        public const string PLAYLIST_LIKED = "spotify_liked_songs";

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
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_CONNECT,
                Name = "Spotify Connect",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });

            var uuid = GenerateUuid(CONNECT_LIVE_SOURCE);
            _context.Library.UpsertTrack(new Track
            {
                Uuid = uuid,
                Title = "Spotify Connect Live",
                Artist = "Spotify",
                SourceType = SourceType.Stream,
                SourcePath = CONNECT_LIVE_SOURCE,
                Duration = 0,
                ModuleId = _moduleId,
                IsFavorite = false
            });

            _context.Library.ReplacePlaylistEntries(PLAYLIST_CONNECT,
                new[] { new PlaylistEntrySpec { TrackUuid = uuid, Position = 0 } });
        }

        public static string GetConnectLiveUuid() => GenerateUuid(CONNECT_LIVE_SOURCE);

        // =====================================================================
        // Liked Songs（用户收藏）
        // =====================================================================

        public void RegisterLikedSongs(List<SpotifyTrack> tracks)
        {
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_LIKED,
                Name = "Liked Songs",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });

            RegisterTracks(tracks, PLAYLIST_LIKED);
        }

        // =====================================================================
        // 歌单注册
        // =====================================================================

        public void RegisterPlaylist(SpotifyPlaylist playlist, List<SpotifyTrack> tracks)
        {
            var playlistId = $"spotify_playlist_{playlist.Id}";
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = playlistId,
                Name = playlist.Name,
                ModuleId = _moduleId,
                Kind = PlaylistKind.Imported,
                CoverUri = playlist.BestCoverUrl ?? ""
            });

            RegisterTracks(tracks, playlistId);
        }

        // =====================================================================
        // 曲目注册
        // =====================================================================

        private void RegisterTracks(List<SpotifyTrack> tracks, string playlistId)
        {
            var entries = new List<PlaylistEntrySpec>();
            int position = 0;

            foreach (var track in tracks)
            {
                var uuid = GenerateUuid($"spotify_{track.Id}");
                var albumId = UpsertAlbum(track);

                _context.Library.UpsertTrack(new Track
                {
                    Uuid = uuid,
                    Title = track.Name,
                    Artist = track.ArtistName,
                    AlbumId = albumId,
                    SourceType = SourceType.Stream,
                    SourcePath = track.Uri,
                    Duration = track.DurationSeconds,
                    ModuleId = _moduleId,
                    IsFavorite = false,
                    CoverUri = track.BestCoverUrl ?? ""
                });

                entries.Add(new PlaylistEntrySpec { TrackUuid = uuid, Position = position++ });
            }

            _context.Library.ReplacePlaylistEntries(playlistId, entries);
            _logger.LogInformation($"Registered {tracks.Count} tracks for [{playlistId}]");
        }

        private string UpsertAlbum(SpotifyTrack track)
        {
            if (track?.Album == null || string.IsNullOrWhiteSpace(track.Album.Id))
                return "";

            var albumId = $"spotify_album_{track.Album.Id}";
            _context.Library.UpsertAlbum(new Album
            {
                Id = albumId,
                Title = track.Album.Name ?? "",
                Artist = track.ArtistName,
                ModuleId = _moduleId,
                CoverUri = track.Album.BestCoverUrl ?? ""
            });
            return albumId;
        }

        // =====================================================================
        // 清理
        // =====================================================================

        public void UnregisterAll()
        {
            _context.Library.UnregisterModule(_moduleId);
        }

        public static string GenerateUuid(string source)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source));
            return new System.Guid(hash).ToString("N");
        }
    }

    /// <summary>
    /// 存储在 Track.ExtendedData 中的 Spotify 元数据（不再使用 ExtendedData，改为直接字段）。
    /// 保留此类供 NativeLibrespotPcmReader 使用。
    /// </summary>
    public class SpotifyTrackMeta
    {
        public string SpotifyId { get; set; }
        public string SpotifyUri { get; set; }
        public string CoverUrl { get; set; }
        public bool IsConnectLive { get; set; }
    }
}
