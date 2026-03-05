using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bulbul;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.ModuleSystem.Services;
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

            // 获取 Registry 实例
            var tagRegistry = TagRegistry.Instance;
            var albumRegistry = AlbumRegistry.Instance;
            var musicRegistry = MusicRegistry.Instance;

            if (tagRegistry == null || albumRegistry == null || musicRegistry == null)
            {
                Logger.LogWarning("Registry not available, returning simple song list");
                for (int i = 0; i < songs.Count; i++)
                {
                    result.Add(PlaylistListItem.CreateSongItem(songs[i], i));
                }
                return result;
            }

            // 按专辑分组歌曲
            var songsByAlbum = new Dictionary<string, List<(GameAudioInfo song, int idx, MusicInfo info)>>();
            // 未分类的自定义歌曲按Tag分组
            var unknownSongsByTag = new Dictionary<string, List<(GameAudioInfo song, int idx)>>();
            // 游戏默认歌曲按原生Tag分组
            var nativeSongsByTag = new Dictionary<AudioTag, List<(GameAudioInfo song, int idx)>>();

            // 分类歌曲
            for (int i = 0; i < songs.Count; i++)
            {
                var song = songs[i];
                var musicInfo = musicRegistry.GetMusic(song.UUID);
                
                if (musicInfo != null)
                {
                    if (!string.IsNullOrEmpty(musicInfo.AlbumId))
                    {
                        if (!songsByAlbum.ContainsKey(musicInfo.AlbumId))
                            songsByAlbum[musicInfo.AlbumId] = new List<(GameAudioInfo, int, MusicInfo)>();
                        songsByAlbum[musicInfo.AlbumId].Add((song, i, musicInfo));
                    }
                    else
                    {
                        var tagId = musicInfo.TagId ?? "_unknown";
                        if (!unknownSongsByTag.ContainsKey(tagId))
                            unknownSongsByTag[tagId] = new List<(GameAudioInfo, int)>();
                        unknownSongsByTag[tagId].Add((song, i));
                    }
                }
                else
                {
                    var nativeTag = GetPrimaryNativeTag(song.Tag);
                    if (!nativeSongsByTag.ContainsKey(nativeTag))
                        nativeSongsByTag[nativeTag] = new List<(GameAudioInfo, int)>();
                    nativeSongsByTag[nativeTag].Add((song, i));
                }
            }

            Logger.LogInfo($"Grouped: {songsByAlbum.Count} albums, {unknownSongsByTag.Count} tags, {nativeSongsByTag.Count} native");

            // 将专辑分为非增长和增长两组
            var nonGrowableAlbums = new Dictionary<string, List<(GameAudioInfo song, int idx, MusicInfo info)>>();
            var growableAlbums = new Dictionary<string, List<(GameAudioInfo song, int idx, MusicInfo info)>>();
            foreach (var kvp in songsByAlbum)
            {
                var albumInfo = albumRegistry.GetAlbum(kvp.Key);
                if (albumInfo != null && albumInfo.IsGrowableAlbum)
                    growableAlbums[kvp.Key] = kvp.Value;
                else
                    nonGrowableAlbums[kvp.Key] = kvp.Value;
            }

            // 将 Tag 歌曲分为非增长和增长两组
            var nonGrowableTags = new Dictionary<string, List<(GameAudioInfo song, int idx)>>();
            var growableTags = new Dictionary<string, List<(GameAudioInfo song, int idx)>>();
            foreach (var kvp in unknownSongsByTag)
            {
                var tagInfo = tagRegistry.GetTag(kvp.Key);
                if (tagInfo != null && tagInfo.IsGrowableList)
                    growableTags[kvp.Key] = kvp.Value;
                else
                    nonGrowableTags[kvp.Key] = kvp.Value;
            }

            // 先添加所有非增长项
            await AddAlbumSongsToResult(result, nonGrowableAlbums, albumRegistry, loadCovers);
            await AddUnknownSongsToResult(result, nonGrowableTags, tagRegistry, loadCovers);

            // 添加原生歌曲
            await AddNativeSongsToResult(result, nativeSongsByTag, loadCovers);

            // 最后添加所有增长项
            await AddAlbumSongsToResult(result, growableAlbums, albumRegistry, loadCovers);
            await AddUnknownSongsToResult(result, growableTags, tagRegistry, loadCovers);

            return result;
        }

        /// <summary>
        /// 添加有专辑信息的歌曲到结果
        /// </summary>
        private async Task AddAlbumSongsToResult(
            List<PlaylistListItem> result,
            Dictionary<string, List<(GameAudioInfo song, int idx, MusicInfo info)>> songsByAlbum,
            AlbumRegistry albumRegistry,
            bool loadCovers)
        {
            // 按专辑排序，增长专辑排在最后
            var orderedAlbums = songsByAlbum.Keys
                .Select(albumId => new { 
                    AlbumId = albumId, 
                    AlbumInfo = albumRegistry.GetAlbum(albumId)
                })
                .Where(x => x.AlbumInfo != null)
                .OrderBy(x => x.AlbumInfo.IsGrowableAlbum ? 1 : 0)  // 增长专辑排最后
                .ThenBy(x => x.AlbumInfo.SortOrder)
                .ThenBy(x => x.AlbumInfo.DisplayName)
                .ToList();

            foreach (var albumEntry in orderedAlbums)
            {
                var albumId = albumEntry.AlbumId;
                var albumInfo = albumEntry.AlbumInfo;
                var albumSongs = songsByAlbum[albumId];

                // 计算启用的歌曲数量
                int enabledCount = albumSongs.Count;

                // 使用 CoverService 获取封面（同步返回占位图或缓存，异步加载后通过事件更新）
                Sprite coverImage = null;
                if (loadCovers)
                {
                    coverImage = CoverService.Instance.GetAlbumCoverOrPlaceholder(albumId);
                }

                // 创建专辑头
                var header = new AlbumHeaderInfo
                {
                    AlbumId = albumId,
                    DisplayName = albumInfo.DisplayName,
                    Artist = albumInfo.Artist,
                    IsOtherAlbum = false,
                    EnabledSongCount = enabledCount,
                    TotalSongCount = albumSongs.Count,
                    DirectoryPath = albumInfo.DirectoryPath,
                    CoverImage = coverImage
                };

                result.Add(PlaylistListItem.CreateAlbumHeader(header));

                foreach (var (song, originalIndex, _) in albumSongs)
                {
                    result.Add(PlaylistListItem.CreateSongItem(song, originalIndex));
                }
            }
        }

        /// <summary>
        /// 添加未分类的自定义歌曲到结果
        /// </summary>
        private async Task AddUnknownSongsToResult(
            List<PlaylistListItem> result,
            Dictionary<string, List<(GameAudioInfo song, int idx)>> unknownSongsByTag,
            TagRegistry tagRegistry,
            bool loadCovers)
        {
            // 按 Tag 排序（增长/非增长分组已在调用方处理）
            var orderedTags = unknownSongsByTag
                .Select(kvp => new { TagId = kvp.Key, Songs = kvp.Value, TagInfo = tagRegistry?.GetTag(kvp.Key) })
                .OrderBy(x => x.TagInfo?.SortOrder ?? int.MaxValue)
                .ThenBy(x => x.TagInfo?.DisplayName ?? x.TagId)
                .ToList();

            foreach (var entry in orderedTags)
            {
                var tagId = entry.TagId;
                var tagSongs = entry.Songs;
                var tagInfo = entry.TagInfo;
                
                if (tagSongs.Count == 0) continue;

                string displayName = tagInfo?.DisplayName ?? tagId;
                
                var defaultAlbumId = $"{tagId}_default";
                int enabledCount = tagSongs.Count;

                var header = new AlbumHeaderInfo
                {
                    AlbumId = defaultAlbumId,
                    DisplayName = displayName,
                    IsOtherAlbum = false,
                    EnabledSongCount = enabledCount,
                    TotalSongCount = tagSongs.Count
                };

                // 尝试加载封面
                if (loadCovers && tagInfo != null)
                {
                    // 可以从模块获取默认封面
                    header.CoverImage = DefaultCoverProvider.Instance?.GetDefaultCover();
                }

                result.Add(PlaylistListItem.CreateAlbumHeader(header));

                foreach (var (song, originalIndex) in tagSongs)
                {
                    result.Add(PlaylistListItem.CreateSongItem(song, originalIndex));
                }
            }
        }

        /// <summary>
        /// 添加原生歌曲到结果
        /// </summary>
        private async Task AddNativeSongsToResult(
            List<PlaylistListItem> result,
            Dictionary<AudioTag, List<(GameAudioInfo song, int idx)>> nativeSongsByTag,
            bool loadCovers)
        {
            foreach (var kvp in nativeSongsByTag.OrderBy(x => (int)x.Key))
            {
                var nativeTag = kvp.Key;
                var tagSongs = kvp.Value;
                
                if (tagSongs.Count == 0) continue;

                string tagDisplayName = GetNativeTagDisplayName(nativeTag);
                var nativeAlbumId = $"native_{nativeTag}";
                
                int enabledCount = 0;
                foreach (var (song, _) in tagSongs)
                {
                    if (!IsNativeSongExcluded(song.UUID))
                        enabledCount++;
                }
                
                Sprite gameCover;
                string artistName;
                if (nativeTag == AudioTag.Local)
                {
                    gameCover = CoverService.Instance.GetLocalMusicCover();
                    artistName = "导入音乐";
                }
                else
                {
                    gameCover = CoverService.Instance.GetGameCover((int)nativeTag);
                    artistName = "Chill With You Game";
                }
                
                var nativeHeader = new AlbumHeaderInfo
                {
                    AlbumId = nativeAlbumId,
                    DisplayName = tagDisplayName,
                    Artist = artistName,
                    IsOtherAlbum = nativeTag == AudioTag.Local && gameCover == null,
                    CoverImage = gameCover,
                    EnabledSongCount = enabledCount,
                    TotalSongCount = tagSongs.Count
                };

                result.Add(PlaylistListItem.CreateAlbumHeader(nativeHeader));

                foreach (var (song, originalIndex) in tagSongs)
                {
                    result.Add(PlaylistListItem.CreateSongItem(song, originalIndex));
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取歌曲的主要原生Tag
        /// </summary>
        private static AudioTag GetPrimaryNativeTag(AudioTag tag)
        {
            var cleanTagWithoutFavorite = tag & ~AudioTag.Favorite;
            if (cleanTagWithoutFavorite == AudioTag.Local)
                return AudioTag.Local;
            
            var cleanTag = tag & ~AudioTag.Favorite & ~AudioTag.Local;
            
            if (cleanTag.HasFlagFast(AudioTag.Original))
                return AudioTag.Original;
            if (cleanTag.HasFlagFast(AudioTag.Special))
                return AudioTag.Special;
            if (cleanTag.HasFlagFast(AudioTag.Other))
                return AudioTag.Other;
            
            return AudioTag.Other;
        }

        /// <summary>
        /// 获取原生Tag的显示名称
        /// </summary>
        private static string GetNativeTagDisplayName(AudioTag tag)
        {
            switch (tag)
            {
                case AudioTag.Original: return "原创";
                case AudioTag.Special: return "特别";
                case AudioTag.Other: return "其他";
                case AudioTag.Local: return "本地音乐";
                default: return tag.ToString();
            }
        }

        /// <summary>
        /// 检查原生歌曲是否被排除
        /// </summary>
        private static bool IsNativeSongExcluded(string uuid)
        {
            try
            {
                var excludedList = SaveDataManager.Instance?.MusicSetting?.ExcludedFromPlaylistUUIDs;
                return excludedList != null && excludedList.Contains(uuid);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从专辑歌曲中提取艺术家
        /// </summary>
        private string GetAlbumArtist(IList<GameAudioInfo> songs)
        {
            if (songs == null || songs.Count == 0)
                return null;

            var artists = songs
                .Where(s => !string.IsNullOrEmpty(s.Credit) && s.Credit != "Unknown")
                .Select(s => s.Credit.Trim())
                .ToList();

            if (artists.Count == 0)
                return null;

            var artistCounts = artists
                .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ToList();

            var topArtist = artistCounts.First();
            
            if (topArtist.Count() >= songs.Count * 0.5f)
                return topArtist.Key;

            if (artistCounts.Count > 2)
                return "Various Artists";

            return topArtist.Key;
        }
    }
}
