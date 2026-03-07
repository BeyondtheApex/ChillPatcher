using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Bulbul;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.ModuleSystem.Services;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.SDK.Models;
using ChillPatcher.UIFramework.Music;
using UnityEngine;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 歌单/播放列表 API：Tag、专辑、歌曲查询、队列、收藏等
    /// JS 端通过 chill.playlist 访问
    /// </summary>
    public class ChillPlaylistApi
    {
        private readonly ManualLogSource _logger;

        public ChillPlaylistApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region Tag 查询

        /// <summary>
        /// 获取所有已注册的 Tag
        /// </summary>
        public string getAllTags()
        {
            var tags = TagRegistry.Instance?.GetAllTags();
            if (tags == null) return "[]";

            return JSApiHelper.ToJson(tags.Select(t => new Dictionary<string, object>
            {
                ["tagId"] = t.TagId,
                ["displayName"] = t.DisplayName,
                ["moduleId"] = t.ModuleId,
                ["bitValue"] = (double)t.BitValue,
                ["isGrowable"] = t.IsGrowableList,
                ["songCount"] = MusicRegistry.Instance?.GetMusicByTag(t.TagId)?.Count ?? 0
            }).ToArray());
        }

        /// <summary>
        /// 获取 Tag 信息
        /// </summary>
        public string getTag(string tagId)
        {
            var tag = TagRegistry.Instance?.GetTag(tagId);
            if (tag == null) return "null";

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["tagId"] = tag.TagId,
                ["displayName"] = tag.DisplayName,
                ["moduleId"] = tag.ModuleId,
                ["bitValue"] = (double)tag.BitValue,
                ["isGrowable"] = tag.IsGrowableList
            });
        }

        #endregion

        #region 专辑查询

        /// <summary>
        /// 获取所有专辑
        /// </summary>
        public string getAllAlbums()
        {
            var albums = AlbumRegistry.Instance?.GetAllAlbums();
            if (albums == null) return "[]";

            return JSApiHelper.ToJson(albums.Select(MapAlbum).ToArray());
        }

        /// <summary>
        /// 获取指定 Tag 下的专辑
        /// </summary>
        public string getAlbumsByTag(string tagId)
        {
            var albums = AlbumRegistry.Instance?.GetAlbumsByTag(tagId);
            if (albums == null) return "[]";

            return JSApiHelper.ToJson(albums.Select(MapAlbum).ToArray());
        }

        /// <summary>
        /// 获取专辑信息
        /// </summary>
        public string getAlbum(string albumId)
        {
            var album = AlbumRegistry.Instance?.GetAlbum(albumId);
            return album != null ? JSApiHelper.ToJson(MapAlbum(album)) : "null";
        }

        private object MapAlbum(AlbumInfo a)
        {
            return new Dictionary<string, object>
            {
                ["albumId"] = a.AlbumId,
                ["displayName"] = a.DisplayName,
                ["tagId"] = a.TagId,
                ["moduleId"] = a.ModuleId,
                ["isGrowable"] = a.IsGrowableAlbum,
                ["songCount"] = MusicRegistry.Instance?.GetMusicByAlbum(a.AlbumId)?.Count ?? 0
            };
        }

        #endregion

        #region 歌曲查询

        /// <summary>
        /// 获取所有歌曲
        /// </summary>
        public string getAllSongs()
        {
            var songs = MusicRegistry.Instance?.GetAllMusic();
            if (songs == null) return "[]";

            return JSApiHelper.ToJson(songs.Select(MapSong).ToArray());
        }

        /// <summary>
        /// 获取指定专辑下的歌曲
        /// </summary>
        public string getSongsByAlbum(string albumId)
        {
            var songs = MusicRegistry.Instance?.GetMusicByAlbum(albumId);
            if (songs == null) return "[]";

            return JSApiHelper.ToJson(songs.Select(MapSong).ToArray());
        }

        /// <summary>
        /// 获取指定 Tag 下的歌曲
        /// </summary>
        public string getSongsByTag(string tagId)
        {
            var songs = MusicRegistry.Instance?.GetMusicByTag(tagId);
            if (songs == null) return "[]";

            return JSApiHelper.ToJson(songs.Select(MapSong).ToArray());
        }

        /// <summary>
        /// 获取指定模块的所有歌曲
        /// </summary>
        public string getSongsByModule(string moduleId)
        {
            var songs = MusicRegistry.Instance?.GetMusicByModule(moduleId);
            if (songs == null) return "[]";

            return JSApiHelper.ToJson(songs.Select(MapSong).ToArray());
        }

        /// <summary>
        /// 通过 UUID 获取歌曲信息
        /// </summary>
        public string getSong(string uuid)
        {
            var song = MusicRegistry.Instance?.GetMusic(uuid);
            return song != null ? JSApiHelper.ToJson(MapSong(song)) : "null";
        }

        private object MapSong(MusicInfo m)
        {
            return new Dictionary<string, object>
            {
                ["uuid"] = m.UUID,
                ["title"] = m.Title ?? "",
                ["artist"] = m.Artist ?? "",
                ["albumId"] = m.AlbumId ?? "",
                ["tagIds"] = m.TagIds?.ToArray() ?? Array.Empty<string>(),
                ["duration"] = m.Duration,
                ["moduleId"] = m.ModuleId ?? "",
                ["sourceType"] = m.SourceType.ToString(),
                ["isUnlocked"] = m.IsUnlocked,
                ["isFavorite"] = m.IsFavorite,
                ["isExcluded"] = m.IsExcluded,
                ["playCount"] = m.PlayCount
            };
        }

        #endregion

        #region 播放队列

        /// <summary>
        /// 获取当前播放队列
        /// </summary>
        public string getQueue()
        {
            var queue = PlayQueueManager.Instance?.Queue;
            if (queue == null) return "[]";

            return JSApiHelper.ToJson(queue.Select(MapGameAudio).ToArray());
        }

        /// <summary>
        /// 获取播放历史
        /// </summary>
        public string getHistory()
        {
            var history = PlayQueueManager.Instance?.History;
            if (history == null) return "[]";

            return JSApiHelper.ToJson(history.Select(MapGameAudio).ToArray());
        }

        /// <summary>
        /// 获取当前播放列表（按当前 Tag 筛选后的列表）
        /// </summary>
        public string getCurrentPlaylist()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var playlist = musicService?.CurrentPlayList;
            if (playlist == null) return "[]";

            return JSApiHelper.ToJson(playlist.Select(MapGameAudio).ToArray());
        }

        private object MapGameAudio(GameAudioInfo g)
        {
            if (g == null) return null;
            var musicInfo = MusicRegistry.Instance?.GetMusic(g.UUID);
            return new Dictionary<string, object>
            {
                ["uuid"] = g.UUID ?? "",
                ["title"] = g.Title ?? "",
                ["artist"] = g.Credit ?? "",
                ["isStream"] = musicInfo?.SourceType == MusicSourceType.Stream,
                ["moduleId"] = musicInfo?.ModuleId ?? ""
            };
        }

        /// <summary>
        /// 获取队列元素数量（不包括正在播放的）
        /// </summary>
        public int getQueueCount()
        {
            return PlayQueueManager.Instance?.PendingCount ?? 0;
        }

        /// <summary>
        /// 通过 UUID 将歌曲添加到队列末尾
        /// </summary>
        public bool addToQueue(string uuid)
        {
            var audio = FindGameAudioByUuid(uuid);
            if (audio == null) return false;
            PlayQueueManager.Instance?.Enqueue(audio);
            return true;
        }

        /// <summary>
        /// 通过 UUID 将歌曲设为下一首播放
        /// </summary>
        public bool playNext(string uuid)
        {
            var audio = FindGameAudioByUuid(uuid);
            if (audio == null) return false;
            PlayQueueManager.Instance?.InsertNext(audio);
            return true;
        }

        /// <summary>
        /// 从队列中移除指定索引的歌曲
        /// </summary>
        public bool removeFromQueue(int index)
        {
            return PlayQueueManager.Instance?.RemoveAt(index) ?? false;
        }

        /// <summary>
        /// 从队列中移除指定 UUID 的歌曲
        /// </summary>
        public bool removeFromQueueByUuid(string uuid)
        {
            var queue = PlayQueueManager.Instance;
            if (queue == null) return false;

            for (int i = 0; i < queue.Queue.Count; i++)
            {
                if (queue.Queue[i].UUID == uuid)
                    return queue.RemoveAt(i);
            }
            return false;
        }

        /// <summary>
        /// 移动队列中歌曲的位置
        /// </summary>
        public void moveInQueue(int fromIndex, int toIndex)
        {
            PlayQueueManager.Instance?.Move(fromIndex, toIndex);
        }

        /// <summary>
        /// 清空待播放队列（保留正在播放的）
        /// </summary>
        public void clearQueue()
        {
            PlayQueueManager.Instance?.ClearPending();
        }

        /// <summary>
        /// 清空播放历史
        /// </summary>
        public void clearHistory()
        {
            PlayQueueManager.Instance?.ClearHistory();
        }

        /// <summary>
        /// 是否可以上一首
        /// </summary>
        public bool canGoPrevious()
        {
            return PlayQueueManager.Instance?.CanGoPrevious ?? false;
        }

        /// <summary>
        /// 获取队列状态信息
        /// </summary>
        public string getQueueState()
        {
            var queue = PlayQueueManager.Instance;
            if (queue == null) return "null";

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["queueCount"] = queue.PendingCount,
                ["historyCount"] = queue.History.Count,
                ["isInHistoryMode"] = queue.IsInHistoryMode,
                ["isInExtendedMode"] = queue.IsInExtendedMode,
                ["canGoPrevious"] = queue.CanGoPrevious,
                ["playlistPosition"] = queue.PlaylistPosition,
                ["currentUuid"] = queue.CurrentPlaying?.UUID ?? ""
            });
        }

        private GameAudioInfo FindGameAudioByUuid(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService == null) return null;

            var all = musicService.AllMusicList;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].UUID == uuid)
                    return all[i];
            }
            return null;
        }

        #endregion

        #region 收藏/排除

        /// <summary>
        /// 设置收藏状态
        /// </summary>
        public void setFavorite(string uuid, bool favorite)
        {
            var music = MusicRegistry.Instance?.GetMusic(uuid);
            if (music != null)
            {
                music.IsFavorite = favorite;
                MusicRegistry.Instance.UpdateMusic(music);
            }
        }

        /// <summary>
        /// 设置排除状态
        /// </summary>
        public void setExcluded(string uuid, bool excluded)
        {
            var music = MusicRegistry.Instance?.GetMusic(uuid);
            if (music != null)
            {
                music.IsExcluded = excluded;
                MusicRegistry.Instance.UpdateMusic(music);
            }
        }

        #endregion

        #region 封面

        /// <summary>
        /// 获取歌曲封面的 Sprite 对象（用于 UIToolkit Image）
        /// </summary>
        public Sprite getSongCover(string uuid)
        {
            return CoverService.Instance?.GetMusicCoverOrPlaceholder(uuid);
        }

        /// <summary>
        /// 获取专辑封面的 Sprite 对象
        /// </summary>
        public Sprite getAlbumCover(string albumId)
        {
            return CoverService.Instance?.GetAlbumCoverOrPlaceholder(albumId);
        }

        #endregion
    }
}
