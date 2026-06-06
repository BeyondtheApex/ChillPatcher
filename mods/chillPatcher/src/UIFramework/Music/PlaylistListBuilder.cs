using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bulbul;
// ModuleSystem removed - IPC bridge
using ChillPatcher.SDK.Models;
using UnityEngine;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 播放列表构建器 - 将歌曲列表转换为包含专辑分隔的复合列表
    /// 使用新的模块系统 Registry
    /// </summary>
    public class PlaylistListBuilder
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger =
            BepInEx.Logging.Logger.CreateLogSource("PlaylistBuilder");

        public PlaylistListBuilder()
        {
        }

        /// <summary>
        /// 构建带专辑分隔的播放列表（根据歌曲的专辑信息分组）
        /// </summary>
        public async Task<List<PlaylistListItem>> BuildWithAlbumHeaders(
            IReadOnlyList<GameAudioInfo> songs,
            bool loadCovers = true)
        {
            var result = new List<PlaylistListItem>();
            Logger.LogInfo($"BuildWithAlbumHeaders called with {songs?.Count ?? 0} songs");

            if (songs == null || songs.Count == 0)
                return result;

            var groups = new List<AlbumGroup>();
            var groupMap = new Dictionary<string, AlbumGroup>();

            for (int i = 0; i < songs.Count; i++)
            {
                var song = songs[i];
                string albumId = "";
                var mi = OmniMixIntegration.Instance?.GetCachedSong(song.UUID);
                if (mi != null && !string.IsNullOrEmpty(mi.AlbumId))
                {
                    albumId = mi.AlbumId;
                }

                if (!groupMap.TryGetValue(albumId, out var group))
                {
                    group = new AlbumGroup { AlbumId = albumId, Songs = new List<SongWithIndex>() };
                    groupMap[albumId] = group;
                    groups.Add(group);
                }
                group.Songs.Add(new SongWithIndex { Song = song, Index = i });
            }

            foreach (var group in groups)
            {
                // 单曲专辑不渲染专辑头，直接列出歌曲
                bool isSingleSongGroup = group.Songs.Count <= 1;

                AlbumHeaderInfo header;
                if (!string.IsNullOrEmpty(group.AlbumId))
                {
                    var album = OmniMixIntegration.Instance?.GetCachedAlbum(group.AlbumId);
                    header = new AlbumHeaderInfo
                    {
                        AlbumId = group.AlbumId,
                        DisplayName = album?.DisplayName ?? "未知专辑",
                        Artist = album?.Artist ?? "",
                        TotalSongCount = group.Songs.Count,
                        EnabledSongCount = group.Songs.Count,
                        IsOtherAlbum = false
                    };

                    if (loadCovers && album != null && !string.IsNullOrEmpty(album.CoverPath))
                    {
                        header.CoverImage = CoverService.Instance.GetAlbumCoverOrPlaceholder(group.AlbumId);
                    }
                }
                else
                {
                    header = new AlbumHeaderInfo
                    {
                        AlbumId = "other_songs",
                        DisplayName = "其它歌曲",
                        Artist = "",
                        TotalSongCount = group.Songs.Count,
                        EnabledSongCount = group.Songs.Count,
                        IsOtherAlbum = true
                    };
                }

                if (!isSingleSongGroup)
                {
                    result.Add(PlaylistListItem.CreateAlbumHeader(header));
                }

                foreach (var swi in group.Songs)
                {
                    result.Add(PlaylistListItem.CreateSongItem(swi.Song, swi.Index));
                }
            }

            return result;
        }

        private class AlbumGroup
        {
            public string AlbumId { get; set; }
            public List<SongWithIndex> Songs { get; set; }
        }

        private class SongWithIndex
        {
            public GameAudioInfo Song { get; set; }
            public int Index { get; set; }
        }
    }
}
