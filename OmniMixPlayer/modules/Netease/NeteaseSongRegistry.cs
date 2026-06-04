using System;
using System.Collections.Generic;
using System.Linq;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.Netease
{
    public class NeteaseSongRegistry
    {
        private readonly IModuleContext _context;
        private readonly string _moduleId;
        private readonly Dictionary<string, NeteaseBridge.SongInfo> _songInfoMap;
        private readonly NeteaseFavoriteManager _favoriteManager;

        public const string PLAYLIST_FAVORITES = "netease_favorites";
        public const string PLAYLIST_PERSONAL_FM = "netease_personal_fm";

        public NeteaseSongRegistry(
            IModuleContext context,
            string moduleId,
            Dictionary<string, NeteaseBridge.SongInfo> songInfoMap,
            NeteaseFavoriteManager favoriteManager)
        {
            _context = context;
            _moduleId = moduleId;
            _songInfoMap = songInfoMap;
            _favoriteManager = favoriteManager;
        }

        public void RegisterFavoritesPlaylist(int songCount)
        {
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_FAVORITES,
                Name = "Netease Favorites",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });
        }

        public void RegisterFMPlaylist(int songCount)
        {
            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = PLAYLIST_PERSONAL_FM,
                Name = "Personal FM",
                ModuleId = _moduleId,
                Kind = PlaylistKind.System
            });
            _context.Library.ReplacePlaylistEntries(PLAYLIST_PERSONAL_FM, Array.Empty<PlaylistEntrySpec>());
        }

        public List<Track> RegisterFavoritesSongs(IEnumerable<NeteaseBridge.SongInfo> songs)
        {
            var tracks = new List<Track>();
            var entries = new List<PlaylistEntrySpec>();
            var position = 0;

            foreach (var song in songs ?? Enumerable.Empty<NeteaseBridge.SongInfo>())
            {
                var track = UpsertSong(song, isFavorite: true);
                tracks.Add(track);
                entries.Add(new PlaylistEntrySpec { TrackUuid = track.Uuid, Position = position++ });
            }

            _context.Library.ReplacePlaylistEntries(PLAYLIST_FAVORITES, entries);
            return tracks;
        }

        public List<Track> RegisterFMSongs(IEnumerable<NeteaseBridge.SongInfo> songs)
        {
            var tracks = new List<Track>();
            var position = _context.Library.GetPlaylistWithEntries(PLAYLIST_PERSONAL_FM)?.Entries.Count ?? 0;

            foreach (var song in songs ?? Enumerable.Empty<NeteaseBridge.SongInfo>())
            {
                var track = UpsertSong(song, _favoriteManager.IsSongLiked(song.Id));
                tracks.Add(track);
                _context.Library.InsertPlaylistEntry(
                    PLAYLIST_PERSONAL_FM,
                    new PlaylistEntrySpec { TrackUuid = track.Uuid, Position = position },
                    position);
                position++;
            }

            return tracks;
        }

        public void MoveSongToFavorites(string uuid, List<Track> sourceList, List<Track> favorites)
        {
            var track = sourceList.FirstOrDefault(m => m.Uuid == uuid) ?? _context.Library.GetTrack(uuid);
            if (track == null)
                return;

            track.IsFavorite = true;
            _context.Library.UpsertTrack(track);

            if (!favorites.Any(m => m.Uuid == uuid))
                favorites.Add(track);
        }

        public static string PlaylistId(long playlistId) => $"netease_playlist_{playlistId}";

        public void RegisterPlaylist(long playlistId, string name, string coverUrl = "")
        {
            var id = PlaylistId(playlistId);

            _context.Library.UpsertPlaylist(new Playlist
            {
                Id = id,
                Name = name ?? id,
                ModuleId = _moduleId,
                Kind = PlaylistKind.Imported,
                CoverUri = coverUrl ?? ""
            });
        }

        public List<Track> RegisterPlaylistSongs(long playlistId, IEnumerable<NeteaseBridge.SongInfo> songs)
        {
            var tracks = new List<Track>();
            var entries = new List<PlaylistEntrySpec>();
            var playlistTag = PlaylistId(playlistId);
            var position = 0;

            foreach (var song in songs ?? Enumerable.Empty<NeteaseBridge.SongInfo>())
            {
                var track = UpsertSong(song, _favoriteManager.IsSongLiked(song.Id));
                tracks.Add(track);
                entries.Add(new PlaylistEntrySpec { TrackUuid = track.Uuid, Position = position++ });
            }

            _context.Library.ReplacePlaylistEntries(playlistTag, entries);
            return tracks;
        }

        public static string GenerateUUID(long songId)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"netease:{songId}"));
            return new Guid(hash).ToString("N");
        }

        private Track UpsertSong(NeteaseBridge.SongInfo song, bool isFavorite)
        {
            var uuid = GenerateUUID(song.Id);
            _songInfoMap[uuid] = song;
            UpsertRealAlbum(song);

            var existing = _context.Library.GetTrack(uuid);
            var track = existing ?? new Track
            {
                Uuid = uuid,
                SourceType = SourceType.Stream,
                SourcePath = song.Id.ToString(),
                ModuleId = _moduleId
            };

            track.Title = song.Name ?? "";
            track.Artist = song.ArtistName ?? "";
            track.AlbumId = RealAlbumId(song);
            track.Duration = (float)song.Duration;
            track.SourceType = SourceType.Stream;
            track.SourcePath = song.Id.ToString();
            track.ModuleId = _moduleId;
            track.IsFavorite = isFavorite;
            track.CoverUri = song.CoverUrl ?? "";

            _context.Library.UpsertTrack(track);
            return track;
        }

        private static string RealAlbumId(NeteaseBridge.SongInfo song)
        {
            return song.AlbumId > 0 ? $"netease_album_{song.AlbumId}" : "netease_album_unknown";
        }

        private void UpsertRealAlbum(NeteaseBridge.SongInfo song)
        {
            _context.Library.UpsertAlbum(new Album
            {
                Id = RealAlbumId(song),
                Title = string.IsNullOrWhiteSpace(song.Album) ? "Unknown Album" : song.Album,
                Artist = song.ArtistName ?? "",
                ModuleId = _moduleId,
                CoverUri = song.CoverUrl ?? ""
            });
        }
    }
}
