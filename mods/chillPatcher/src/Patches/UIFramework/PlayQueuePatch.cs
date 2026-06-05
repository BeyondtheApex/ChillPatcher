using Bulbul;
using ChillPatcher.UIFramework;
using ChillPatcher.UIFramework.Music;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using KanKikuchi.AudioManager;
using System.Collections.Generic;
using UnityEngine;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 播放队列系统 Patches（后端代理模式）
    /// 
    /// 拦截 MusicService.PlayNextMusic 和 MusicService.SkipCurrentMusic
    /// 转发到后端 OmniMixPlayer Profile 处理
    /// 后端负责队列管理、历史记录和下一首决策
    /// </summary>
    [HarmonyPatch]
    public static class PlayQueuePatch
    {
        public static bool IsQueueSystemEnabled { get; set; } = true;

        /// <summary>最新的 MusicService 实例（跨场景重载更新）</summary>
        private static MusicService _latestMusicService;

        /// <summary>场景正在重载中</summary>
        private static bool _isSceneReloading = false;

        public static void PrepareForSceneReload()
        {
            Plugin.Log.LogInfo("[PlayQueuePatch] PrepareForSceneReload: keeping music alive");
            _isSceneReloading = true;
            try
            {
                var mm = SingletonMonoBehaviour<MusicManager>.Instance;
                if (mm != null) UnityEngine.Object.DontDestroyOnLoad(mm.gameObject);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[PlayQueuePatch] DontDestroyOnLoad failed: {ex.Message}");
            }
        }

        private static void CompleteSceneReload(MusicService newMusicService)
        {
            Plugin.Log.LogInfo("[PlayQueuePatch] CompleteSceneReload: refreshing queue references");
            _isSceneReloading = false;
            _latestMusicService = newMusicService;

            var qm = PlayQueueManager.Instance;
            var playlist = GetPlaylistForQueue(newMusicService);
            qm.RestoreFromUUIDs(qm.GetQueueUUIDs(), playlist);
            qm.RestoreHistoryFromUUIDs(qm.GetHistoryUUIDs(), playlist);

            var current = qm.CurrentPlaying;
            if (current != null) SetPlayingMusic(newMusicService, current);
        }

        private static IReadOnlyList<GameAudioInfo> GetPlaylistForQueue(MusicService musicService)
        {
            var musicManager = ChillUIFramework.Music as MusicUIManager;
            if (musicManager?.DisplayOrderSongs != null && musicManager.DisplayOrderSongs.Count > 0)
                return musicManager.DisplayOrderSongs;
            return musicService.CurrentPlayList;
        }

        private static void SetPlayingMusic(MusicService musicService, GameAudioInfo audio)
        {
            if (audio == null) return;
            var playingField = Traverse.Create(musicService).Field("PlayingMusic");
            if (playingField != null) playingField.SetValue(audio);
        }

        public static void SetPlayingMusicDirect(MusicService musicService, GameAudioInfo audio, MusicChangeKind changeKind)
        {
            if (musicService == null || audio == null) return;
            _latestMusicService = musicService;
            PlayQueueManager.Instance.SetCurrentPlaying(audio, addToHistory: false);
            try
            {
                musicService.PlayArugumentMusic(audio, changeKind);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[PlayQueuePatch] SetPlayingMusicDirect local UI sync failed: {ex.Message}");
                SetPlayingMusic(musicService, audio);
            }
        }

        #region SkipCurrentMusic → 转发到后端 Next

        [HarmonyPatch(typeof(MusicService), nameof(MusicService.SkipCurrentMusic))]
        [HarmonyPrefix]
        public static bool SkipCurrentMusic_Prefix(MusicService __instance, MusicChangeKind kind, ref UniTask<bool> __result)
        {
            if (!IsQueueSystemEnabled) return true;

            _ = OmniMixIntegration.Instance.Next();
            __result = UniTask.FromResult(true);
            return false;
        }

        #endregion

        #region PlayNextMusic → 转发到后端

        /// <summary>
        /// nextCount >= 0: 下一首 → 后端 Next
        /// nextCount < 0: 上一首 → 后端 Prev
        /// </summary>
        [HarmonyPatch(typeof(MusicService), nameof(MusicService.PlayNextMusic))]
        [HarmonyPrefix]
        public static bool PlayNextMusic_Prefix(MusicService __instance, int nextCount, MusicChangeKind changeKind, ref UniTask<bool> __result)
        {
            _latestMusicService = __instance;

            if (_isSceneReloading)
            {
                CompleteSceneReload(__instance);
                __result = UniTask.FromResult(true);
                return false;
            }

            if (!IsQueueSystemEnabled) return true;

            if (nextCount < 0)
                _ = OmniMixIntegration.Instance.Prev();
            else
                _ = OmniMixIntegration.Instance.Next();

            __result = UniTask.FromResult(true);
            return false;
        }

        #endregion
    }
}
