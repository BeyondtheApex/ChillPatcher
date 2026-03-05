using System;
using System.Collections.Generic;
using System.Linq;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// Handles registration of Tags, Albums, and Songs for QQ Music
    /// </summary>
    public class QQMusicSongRegistry
    {
        // Tag IDs
        public const string TAG_FAVORITES = "qqmusic_favorites";
        public const string TAG_RECOMMEND = "qqmusic_recommend";

        // Album IDs
        public const string FAVORITES_ALBUM_ID = "qqmusic_favorites_album";
        public const string RECOMMEND_ALBUM_ID = "qqmusic_recommend_album";
        public const string LOGIN_ALBUM_ID = "qqmusic_login_album";

        // Prefix for playlist albums
        public const string PLAYLIST_ALBUM_PREFIX = "qqmusic_playlist_";
        public const string PLAYLIST_TAG_PREFIX = "qqmusic_playlist_tag_";

        private readonly IModuleContext _context;
        private readonly string _moduleId;

        public QQMusicSongRegistry(IModuleContext context, string moduleId)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _moduleId = moduleId;
        }

        #region Tag Registration

        public void RegisterFavoritesTag()
        {
            _context.TagRegistry.RegisterTag(TAG_FAVORITES, "QQ音乐收藏", _moduleId);
        }

        public void RegisterRecommendTag()
        {
            var tag = _context.TagRegistry.RegisterTag(TAG_RECOMMEND, "QQ音乐推荐", _moduleId);
            _context.TagRegistry.MarkAsGrowableTag(TAG_RECOMMEND, RECOMMEND_ALBUM_ID);
        }

        public void RegisterPlaylistTag(long playlistId, string name)
        {
            var tagId = GetPlaylistTagId(playlistId);
            _context.TagRegistry.RegisterTag(tagId, name, _moduleId);
        }

        #endregion

        #region Album Registration

        public void RegisterLoginSongAlbum()
        {
            var album = new AlbumInfo
            {
                AlbumId = LOGIN_ALBUM_ID,
                DisplayName = "QQ音乐登录",
                Artist = "ChillPatcher",
                TagIds = new List<string> { TAG_FAVORITES },
                ModuleId = _moduleId,
                SongCount = 1,
                SortOrder = 0
            };
            _context.AlbumRegistry.RegisterAlbum(album, _moduleId);
        }

        public void UnregisterLoginSongAlbum()
        {
            _context.AlbumRegistry.UnregisterAlbum(LOGIN_ALBUM_ID);
        }

        public void RegisterFavoritesAlbum(int songCount)
        {
            var album = new AlbumInfo
            {
                AlbumId = FAVORITES_ALBUM_ID,
                DisplayName = "我喜欢的音乐",
                Artist = "QQ音乐",
                TagIds = new List<string> { TAG_FAVORITES, TAG_RECOMMEND },
                ModuleId = _moduleId,
                SongCount = songCount,
                SortOrder = 1
            };
            _context.AlbumRegistry.RegisterAlbum(album, _moduleId);
        }

        public void RegisterRecommendAlbum(int songCount)
        {
            var album = new AlbumInfo
            {
                AlbumId = RECOMMEND_ALBUM_ID,
                DisplayName = "每日推荐",
                Artist = "QQ音乐",
                TagIds = new List<string> { TAG_RECOMMEND },
                ModuleId = _moduleId,
                SongCount = songCount,
                SortOrder = 1000,
                IsGrowableAlbum = true
            };
            _context.AlbumRegistry.RegisterAlbum(album, _moduleId);
        }

        public void RegisterPlaylistAlbum(long playlistId, string name, int songCount, string coverUrl)
        {
            var albumId = GetPlaylistAlbumId(playlistId);
            var tagId = GetPlaylistTagId(playlistId);

            var album = new AlbumInfo
            {
                AlbumId = albumId,
                DisplayName = name,
                Artist = "QQ音乐歌单",
                TagIds = new List<string> { tagId },
                ModuleId = _moduleId,
                SongCount = songCount,
                SortOrder = 100,
                ExtendedData = new { CoverUrl = coverUrl, PlaylistId = playlistId }
            };
            _context.AlbumRegistry.RegisterAlbum(album, _moduleId);
        }

        #endregion

        #region Song Registration

        public List<MusicInfo> RegisterFavoritesSongs(
            List<QQMusicBridge.SongInfo> songs,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            var musicList = new List<MusicInfo>();

            foreach (var song in songs)
            {
                var uuid = GenerateUUID(song.Mid);

                var music = new MusicInfo
                {
                    UUID = uuid,
                    Title = song.Name,
                    Artist = song.ArtistString,
                    AlbumId = FAVORITES_ALBUM_ID,
                    TagIds = new List<string> { TAG_FAVORITES },
                    SourceType = MusicSourceType.Stream,
                    Duration = (float)song.Duration,
                    ModuleId = _moduleId,
                    IsUnlocked = true,
                    IsFavorite = true,
                    ExtendedData = song
                };

                _context.MusicRegistry.RegisterMusic(music, _moduleId);
                musicList.Add(music);
                songInfoMap[uuid] = song;
            }

            return musicList;
        }

        public List<MusicInfo> RegisterRecommendSongs(
            List<QQMusicBridge.SongInfo> songs,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap,
            List<MusicInfo> existingFavorites)
        {
            var musicList = new List<MusicInfo>();
            var existingUuids = new HashSet<string>(existingFavorites.Select(m => m.UUID));

            foreach (var song in songs)
            {
                var uuid = GenerateUUID(song.Mid);

                if (existingUuids.Contains(uuid))
                {
                    // Song already exists in favorites, just add the recommend tag
                    var existing = existingFavorites.First(m => m.UUID == uuid);
                    if (!existing.TagIds.Contains(TAG_RECOMMEND))
                    {
                        existing.TagIds.Add(TAG_RECOMMEND);
                        _context.MusicRegistry.UpdateMusic(existing);
                    }
                    musicList.Add(existing);
                    continue;
                }

                var music = new MusicInfo
                {
                    UUID = uuid,
                    Title = song.Name,
                    Artist = song.ArtistString,
                    AlbumId = RECOMMEND_ALBUM_ID,
                    TagIds = new List<string> { TAG_RECOMMEND },
                    SourceType = MusicSourceType.Stream,
                    Duration = (float)song.Duration,
                    ModuleId = _moduleId,
                    IsUnlocked = true,
                    IsFavorite = false,
                    ExtendedData = song
                };

                _context.MusicRegistry.RegisterMusic(music, _moduleId);
                musicList.Add(music);
                songInfoMap[uuid] = song;
            }

            return musicList;
        }

        public List<MusicInfo> RegisterPlaylistSongs(
            long playlistId,
            List<QQMusicBridge.SongInfo> songs,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            var musicList = new List<MusicInfo>();
            var albumId = GetPlaylistAlbumId(playlistId);
            var tagId = GetPlaylistTagId(playlistId);

            foreach (var song in songs)
            {
                // Use playlist-specific UUID to ensure each playlist has independent entries
                var uuid = GeneratePlaylistSongUUID(playlistId, song.Mid);

                var music = new MusicInfo
                {
                    UUID = uuid,
                    Title = song.Name,
                    Artist = song.ArtistString,
                    AlbumId = albumId,
                    TagIds = new List<string> { tagId },
                    SourceType = MusicSourceType.Stream,
                    Duration = (float)song.Duration,
                    ModuleId = _moduleId,
                    IsUnlocked = true,
                    IsFavorite = false,
                    ExtendedData = song
                };

                _context.MusicRegistry.RegisterMusic(music, _moduleId);
                musicList.Add(music);

                // Store song info with both the playlist-specific UUID and the base UUID
                songInfoMap[uuid] = song;
                var baseUuid = GenerateUUID(song.Mid);
                if (!songInfoMap.ContainsKey(baseUuid))
                {
                    songInfoMap[baseUuid] = song;
                }
            }

            return musicList;
        }

        public MusicInfo RegisterLoginSong(string message)
        {
            var uuid = "qqmusic_login_song";
            var music = new MusicInfo
            {
                UUID = uuid,
                Title = message,
                Artist = "请使用QQ音乐APP扫码登录",
                AlbumId = LOGIN_ALBUM_ID,
                TagIds = new List<string> { TAG_FAVORITES },
                SourceType = MusicSourceType.Stream,
                Duration = 30f,
                ModuleId = _moduleId,
                IsUnlocked = true,
                IsFavorite = false
            };

            _context.MusicRegistry.RegisterMusic(music, _moduleId);
            return music;
        }

        public void UnregisterLoginSong()
        {
            _context.MusicRegistry.UnregisterMusic("qqmusic_login_song");
        }

        #endregion

        #region Helpers

        public static string GenerateUUID(string songMid)
        {
            return $"qqmusic_{songMid}";
        }

        public static string GeneratePlaylistSongUUID(long playlistId, string songMid)
        {
            return $"qqmusic_pl{playlistId}_{songMid}";
        }

        public static string GetPlaylistAlbumId(long playlistId)
        {
            return $"{PLAYLIST_ALBUM_PREFIX}{playlistId}";
        }

        public static string GetPlaylistTagId(long playlistId)
        {
            return $"{PLAYLIST_TAG_PREFIX}{playlistId}";
        }

        public void MoveSongToFavorites(string uuid, List<MusicInfo> fromList, List<MusicInfo> toList)
        {
            var music = fromList.FirstOrDefault(m => m.UUID == uuid);
            if (music == null) return;

            // Update album and tags
            music.AlbumId = FAVORITES_ALBUM_ID;
            if (!music.TagIds.Contains(TAG_FAVORITES))
            {
                music.TagIds.Add(TAG_FAVORITES);
            }
            music.IsFavorite = true;

            _context.MusicRegistry.UpdateMusic(music);

            // Move to favorites list if not already there
            if (!toList.Any(m => m.UUID == uuid))
            {
                toList.Add(music);
            }
        }

        #endregion
    }
}
