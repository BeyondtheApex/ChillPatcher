using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using Bulbul;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.UIFramework.Audio;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 歌单/播放列表 API (IPC 版本)
    /// 数据源从 OmniMixPlayer 后端拉取，游戏内队列由 PlayQueueManager 管理
    /// </summary>
    public class ChillPlaylistApi
    {
        private readonly ManualLogSource _logger;

        public ChillPlaylistApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region 能力声明 / 数据查询 (from OmniMixPlayer)

        /// <summary>
        /// 获取后端分配的能力声明（Tag 位映射等）
        /// </summary>
        public async Task<string> getCapabilities()
        {
            try { return await OmniMixIntegration.Instance.GetTagsJson(); } catch { return "[]"; }
        }

        #endregion

        #region 歌曲查询

        public async Task<string> getAllSongs()
        {
            return await OmniMixIntegration.Instance.GetSongsJson();
        }

        public async Task<string> getSongsByAlbum(string albumId)
        {
            return await OmniMixIntegration.Instance.GetSongsJson(albumId);
        }

        public async Task<string> getSongsByTag(string tagId)
        {
            return await OmniMixIntegration.Instance.GetSongsJson(null, tagId);
        }

        #endregion

        #region 播放队列 (后端 Profile 权威，游戏侧只读镜像)

        public string getQueue()
        {
            var queue = PlayQueueManager.Instance?.Queue;
            if (queue == null) return "[]";
            return JSApiHelper.ToJson(queue.Select(MapGameAudio).ToArray());
        }

        public string getHistory()
        {
            var history = PlayQueueManager.Instance?.History;
            if (history == null) return "[]";
            return JSApiHelper.ToJson(history.Select(MapGameAudio).ToArray());
        }

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
            return new Dictionary<string, object>
            {
                ["uuid"] = g.UUID ?? "",
                ["title"] = g.Title ?? "",
                ["artist"] = g.Credit ?? "",
                ["isStream"] = StreamingAudioLoader.IsStreamingSource(g)
            };
        }

        public int getQueueCount() => PlayQueueManager.Instance?.PendingCount ?? 0;

        public bool addToQueue(string uuid)
        {
            var audio = FindGameAudioByUuid(uuid);
            if (audio == null) return false;
            PlayQueueManager.Instance?.Enqueue(audio);
            _ = OmniMixIntegration.Instance.AddToQueue(uuid);
            return true;
        }

        public bool playNext(string uuid)
        {
            var audio = FindGameAudioByUuid(uuid);
            if (audio == null) return false;
            PlayQueueManager.Instance?.InsertNext(audio);
            return true;
        }

        public bool removeFromQueue(int index) => PlayQueueManager.Instance?.RemoveAt(index) ?? false;

        public bool removeFromQueueByUuid(string uuid)
        {
            var queue = PlayQueueManager.Instance;
            if (queue == null) return false;
            for (int i = 0; i < queue.Queue.Count; i++)
                if (queue.Queue[i].UUID == uuid) return queue.RemoveAt(i);
            return false;
        }

        public void moveInQueue(int from, int to) => PlayQueueManager.Instance?.Move(from, to);
        public void clearQueue() => PlayQueueManager.Instance?.ClearPending();
        public void clearHistory() => PlayQueueManager.Instance?.ClearHistory();
        public bool canGoPrevious() => PlayQueueManager.Instance?.CanGoPrevious ?? false;

        public string getQueueState()
        {
            var q = PlayQueueManager.Instance;
            if (q == null) return "null";
            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["queueCount"] = q.PendingCount,
                ["historyCount"] = q.History.Count,
                ["isInHistoryMode"] = q.IsInHistoryMode,
                ["isInExtendedMode"] = q.IsInExtendedMode,
                ["canGoPrevious"] = q.CanGoPrevious,
                ["playlistPosition"] = q.PlaylistPosition,
                ["currentUuid"] = q.CurrentPlaying?.UUID ?? ""
            });
        }

        private GameAudioInfo FindGameAudioByUuid(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService == null) return null;
            var all = musicService.AllMusicList;
            for (int i = 0; i < all.Count; i++)
                if (all[i].UUID == uuid) return all[i];
            return null;
        }

        #endregion

        #region 收藏/排除 (同步到 OmniMixPlayer)

        public async void setFavorite(string uuid, bool favorite)
        {
            await OmniMixIntegration.Instance.SetFavorite(uuid, favorite);
        }

        public async void setExcluded(string uuid, bool excluded)
        {
            await OmniMixIntegration.Instance.SetExcluded(uuid, excluded);
        }

        #endregion

        #region 封面

        public Sprite getSongCover(string uuid)
        {
            return null; // Cover loading now via OmniMixPlayer proxy
        }

        public Sprite getAlbumCover(string albumId)
        {
            return null;
        }

        #endregion
    }
}
