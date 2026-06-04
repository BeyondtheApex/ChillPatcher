using System;
using System.Collections.Generic;
using System.Linq;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.QQMusic
{
    /// <summary>
    /// Handles registration of Playlists, Albums, and Tracks for QQ Music
    /// </summary>
    public class QQMusicSongRegistry
    {
        // Playlist IDs
        public const string PLAYLIST_FAVORITES = "qqmusic_favorites";
        public const string PLAYLIST_RECOMMEND = "qqmusic_recommend";

        private readonly IModuleContext _context;
        private readonly string _moduleId;

        public QQMusicSongRegistry(IModuleContext context, string moduleId)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _moduleId = moduleId;
        }

        #region Playlist Registration

        public void RegisterFavoritesPlaylist()
        {
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_FAVORITES,
                Name = "QQ音乐收藏",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });
        }

        public void RegisterRecommendPlaylist()
        {
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_RECOMMEND,
                Name = "QQ音乐推荐",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });
        }

        public void RegisterPlaylist(long playlistId, string name, string coverUrl = "")
        {
            var playlistTag = GetPlaylistId(playlistId);
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = playlistTag,
                Name = name,
                ModuleId = _moduleId,
                Kind = PlaylistKind.Imported,
                CoverUri = coverUrl ?? ""
            });
        }

        #endregion

        #region Song Registration

        public List<Track> RegisterFavoritesSongs(
            List<QQMusicBridge.SongInfo> songs,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            var tracks = new List<Track>();
            var entries = new List<PlaylistEntrySpec>();
            var position = 0;

            if (songs == null) return tracks;

            foreach (var song in songs)
            {
                var track = UpsertSong(song, isFavorite: true, songInfoMap);
                tracks.Add(track);
                entries.Add(new PlaylistEntrySpec { TrackUuid = track.Uuid, Position = position++ });
            }

            _context.Library.ReplacePlaylistEntries(PLAYLIST_FAVORITES, entries);
            return tracks;
        }

        public List<Track> RegisterRecommendSongs(
            List<QQMusicBridge.SongInfo> songs,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap,
            List<Track> existingFavorites,
            int startPosition = 0)
        {
            var tracks = new List<Track>();
            var position = Math.Max(0, startPosition);
            var existingUuids = new HashSet<string>(existingFavorites.Select(m => m.Uuid));

            if (songs == null) return tracks;

            foreach (var song in songs)
            {
                var uuid = GenerateUUID(song.Mid);

                if (existingUuids.Contains(uuid))
                {
                    var existing = existingFavorites.First(m => m.Uuid == uuid);
                    tracks.Add(existing);
                    _context.Library.InsertPlaylistEntry(
                        PLAYLIST_RECOMMEND,
                        new PlaylistEntrySpec { TrackUuid = existing.Uuid, Position = position },
                        position);
                    position++;
                    continue;
                }

                var track = UpsertSong(song, isFavorite: false, songInfoMap);
                tracks.Add(track);
                _context.Library.InsertPlaylistEntry(
                    PLAYLIST_RECOMMEND,
                    new PlaylistEntrySpec { TrackUuid = track.Uuid, Position = position },
                    position);
                position++;
            }

            return tracks;
        }

        public List<Track> RegisterPlaylistSongs(
            long playlistId,
            List<QQMusicBridge.SongInfo> songs,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            var tracks = new List<Track>();
            var entries = new List<PlaylistEntrySpec>();
            var playlistTag = GetPlaylistId(playlistId);
            var position = 0;

            if (songs == null) return tracks;

            foreach (var song in songs)
            {
                var uuid = GenerateUUID(song.Mid);
                var existing = _context.Library.GetTrack(uuid);
                var track = existing ?? new Track
                {
                    Uuid = uuid,
                    SourceType = SourceType.Stream,
                    SourcePath = song.Mid,
                    ModuleId = _moduleId
                };

                track.Title = song.Name ?? "";
                track.Artist = song.ArtistString ?? "";
                track.AlbumId = UpsertSongAlbum(song);
                track.Duration = (float)song.Duration;
                track.SourceType = SourceType.Stream;
                track.SourcePath = song.Mid;
                track.ModuleId = _moduleId;
                track.IsFavorite = false;
                track.CoverUri = song.CoverUrl ?? "";

                _context.Library.UpsertTrack(track);
                tracks.Add(track);

                songInfoMap[uuid] = song;
                entries.Add(new PlaylistEntrySpec { TrackUuid = uuid, Position = position++ });
            }

            _context.Library.ReplacePlaylistEntries(playlistTag, entries);
            return tracks;
        }

        public Track RegisterLoginSong(string message, string uuid = "qqmusic_login_song", string artist = "请使用 QQ 扫码登录")
        {
            var track = new Track
            {
                Uuid = uuid,
                Title = message,
                Artist = artist,
                SourceType = SourceType.Stream,
                Duration = 120f,
                ModuleId = _moduleId,
                IsFavorite = false
            };

            _context.Library.UpsertTrack(track);
            return track;
        }

        public void UnregisterLoginSong()
        {
            _context.Library.DeleteTrack("qqmusic_login_song");
            _context.Library.DeleteTrack("qqmusic_login_song_wx");
        }

        #endregion

        #region Helpers

        private Track UpsertSong(QQMusicBridge.SongInfo song, bool isFavorite, Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            var uuid = GenerateUUID(song.Mid);
            songInfoMap[uuid] = song;

            var existing = _context.Library.GetTrack(uuid);
            var track = existing ?? new Track
            {
                Uuid = uuid,
                SourceType = SourceType.Stream,
                SourcePath = song.Mid,
                ModuleId = _moduleId
            };

            track.Title = song.Name ?? "";
            track.Artist = song.ArtistString ?? "";
            track.AlbumId = UpsertSongAlbum(song);
            track.Duration = (float)song.Duration;
            track.SourceType = SourceType.Stream;
            track.SourcePath = song.Mid;
            track.ModuleId = _moduleId;
            track.IsFavorite = isFavorite;
            track.CoverUri = song.CoverUrl ?? "";

            _context.Library.UpsertTrack(track);
            return track;
        }

        public static string GenerateUUID(string songMid)
        {
            return $"qqmusic_{songMid}";
        }

        public static string GetPlaylistId(long playlistId)
        {
            return $"qqmusic_playlist_{playlistId}";
        }

        public void MoveSongToFavorites(string uuid, List<Track> fromList, List<Track> toList)
        {
            var track = fromList.FirstOrDefault(m => m.Uuid == uuid);
            if (track == null) return;

            track.IsFavorite = true;

            _context.Library.UpsertTrack(track);

            if (!toList.Any(m => m.Uuid == uuid))
                toList.Add(track);
        }

        private string UpsertSongAlbum(QQMusicBridge.SongInfo song)
        {
            if (song == null || string.IsNullOrWhiteSpace(song.AlbumMid))
                return "";

            var albumId = $"qqmusic_album_{song.AlbumMid}";
            _context.Library.UpsertAlbum(new Album
            {
                Id = albumId,
                Title = song.Album ?? "",
                Artist = song.ArtistString,
                ModuleId = _moduleId,
                CoverUri = song.CoverUrl ?? ""
            });
            return albumId;
        }

        #endregion
    }
}
