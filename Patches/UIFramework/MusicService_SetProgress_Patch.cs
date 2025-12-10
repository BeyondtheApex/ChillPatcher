using HarmonyLib;
using Bulbul;
using KanKikuchi.AudioManager;
using UnityEngine;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 拦截 MusicService.SetMusicProgress，支持流媒体的智能 Seek
    /// 
    /// 原生 MusicUI 行为：
    /// - PointerDown: 触发 SetMusicProgress (dragging=true)
    /// - OnDrag: 每帧触发 SetMusicProgress (dragging=null)
    /// - PointerUp: 触发 SetMusicProgress (dragging=false)
    /// 
    /// 问题：
    /// 1. 点击会触发两次 Seek（按下+抬起）
    /// 2. 拖动会频繁 Seek，导致杂音和性能问题
    /// 
    /// 解决方案：
    /// 1. 追踪拖动状态（通过反射读取 MusicUI.isDraggingProgressSlider）
    /// 2. 拖动过程中只更新预览进度，不执行实际 Seek
    /// 3. 只在 PointerUp（拖动结束）时执行真正的 Seek
    /// </summary>
    [HarmonyPatch]
    public static class MusicService_SetProgress_Patch
    {
        /// <summary>
        /// 当前活跃的 PCM 流读取器（用于检测 Seek 状态）
        /// </summary>
        public static ChillPatcher.SDK.Interfaces.IPcmStreamReader ActivePcmReader { get; set; }

        /// <summary>
        /// 是否正在从 SetProgress 进行 Seek（防止 PCMSetPositionCallback 重复 Seek）
        /// </summary>
        public static bool IsSeekingFromSetProgress { get; set; }

        /// <summary>
        /// 拖动预览进度（拖动过程中显示的进度，但不实际 Seek）
        /// </summary>
        public static float PreviewProgress { get; private set; } = -1f;

        /// <summary>
        /// 是否正在拖动进度条
        /// </summary>
        public static bool IsDragging { get; private set; }

        /// <summary>
        /// 上次有效 Seek 的时间（用于 PointerUp 防抖）
        /// </summary>
        private static System.DateTime _lastSeekTime;
        private static ulong _lastSeekFrame;

        /// <summary>
        /// 设置活跃的 PCM 读取器
        /// </summary>
        public static void SetActivePcmReader(ChillPatcher.SDK.Interfaces.IPcmStreamReader reader)
        {
            // 如果有旧的待定 Seek，取消它
            if (ActivePcmReader != null && ActivePcmReader != reader)
            {
                FacilityMusic_UpdateFacility_Patch.IsWaitingForSeek = false;
                FacilityMusic_UpdateFacility_Patch.PendingSeekProgress = 0f;
            }
            ActivePcmReader = reader;
        }

        /// <summary>
        /// 清除活跃的 PCM 读取器
        /// </summary>
        public static void ClearActivePcmReader()
        {
            ActivePcmReader = null;
            FacilityMusic_UpdateFacility_Patch.IsWaitingForSeek = false;
            FacilityMusic_UpdateFacility_Patch.PendingSeekProgress = 0f;
            
            // 重置进度跟踪
            MusicService_GetProgress_Patch.ResetProgress();
        }

        [HarmonyPatch(typeof(MusicService), nameof(MusicService.SetMusicProgress))]
        [HarmonyPrefix]
        public static bool SetMusicProgress_Prefix(MusicService __instance, float progress)
        {
            var playingMusic = __instance.PlayingMusic;
            if (playingMusic == null || string.IsNullOrEmpty(playingMusic.AudioClipName))
            {
                return true; // 使用原始逻辑
            }

            // 检查是否是流媒体歌曲
            var music = MusicRegistry.Instance?.GetMusic(playingMusic.UUID);
            if (music == null || music.SourceType != MusicSourceType.Stream)
            {
                return true; // 不是流媒体，使用原始逻辑
            }

            // 获取 AudioPlayer
            var musicManager = SingletonMonoBehaviour<MusicManager>.Instance;
            var player = musicManager.GetPlayer(playingMusic.AudioClip);
            
            if (player == null || player.AudioSource == null || player.AudioSource.clip == null)
            {
                Plugin.Log.LogWarning("[SetProgress_Patch] No audio player found");
                return false; // 跳过原始逻辑
            }

            // 通过反射读取 MusicUI.isDraggingProgressSlider 来判断拖动状态
            bool currentlyDragging = GetMusicUIDraggingState();
            
            // 状态转换检测
            bool wasNotDragging = !IsDragging;
            bool isNowDragging = currentlyDragging;
            bool justStartedDragging = wasNotDragging && isNowDragging;
            bool justStoppedDragging = IsDragging && !currentlyDragging;
            
            // 更新拖动状态
            IsDragging = currentlyDragging;

            var clip = player.AudioSource.clip;
            
            // 【重要】使用原始时长计算目标帧，而不是 clip.samples（包含 30 分钟余量）
            // 优先使用 PCM reader 的 TotalFrames（原始歌曲时长）
            ulong totalFrames = (ActivePcmReader != null && ActivePcmReader.Info.TotalFrames > 0)
                ? ActivePcmReader.Info.TotalFrames
                : (ulong)clip.samples;
            var targetFrame = (ulong)(totalFrames * progress);

            // 如果正在拖动，只更新预览进度，不执行实际 Seek
            if (currentlyDragging)
            {
                PreviewProgress = progress;
                // Plugin.Log.LogDebug($"[SetProgress_Patch] Dragging preview: {progress:P1}");
                return false; // 跳过原始逻辑和 Seek
            }

            // 拖动刚结束（PointerUp），执行真正的 Seek
            if (justStoppedDragging)
            {
                PreviewProgress = -1f; // 清除预览进度
                Plugin.Log.LogInfo($"[SetProgress_Patch] Drag ended, executing seek to {progress:P1}");
                return ExecuteSeek(__instance, player, clip, progress, targetFrame);
            }

            // 不在拖动状态，且不是刚开始拖动（这是单击的情况）
            // PointerDown 后会立即设置 isDraggingProgressSlider = true
            // 所以如果现在不是拖动状态，这应该是 PointerUp（单击释放）
            // 但为了防止重复，使用防抖
            var now = System.DateTime.Now;
            if (targetFrame == _lastSeekFrame && (now - _lastSeekTime).TotalMilliseconds < 100)
            {
                // Plugin.Log.LogDebug($"[SetProgress_Patch] Debounced seek to same position");
                return false;
            }

            Plugin.Log.LogInfo($"[SetProgress_Patch] Click seek to {progress:P1}");
            return ExecuteSeek(__instance, player, clip, progress, targetFrame);
        }

        /// <summary>
        /// 执行实际的 Seek 操作
        /// </summary>
        private static bool ExecuteSeek(MusicService musicService, AudioPlayer player, AudioClip clip, float progress, ulong targetFrame)
        {
            if (ActivePcmReader == null)
            {
                return true; // 没有 PCM 读取器，使用原始逻辑
            }

            // 记录 Seek 位置和时间（用于防抖）
            _lastSeekFrame = targetFrame;
            _lastSeekTime = System.DateTime.Now;

            // 尝试 Seek
            bool success = ActivePcmReader.Seek(targetFrame);
            
            if (success)
            {
                // 使用接口属性检查是否是延迟 Seek
                if (ActivePcmReader.HasPendingSeek)
                {
                    // 延迟 Seek - 设置等待状态
                    FacilityMusic_UpdateFacility_Patch.IsWaitingForSeek = true;
                    FacilityMusic_UpdateFacility_Patch.PendingSeekProgress = progress;
                    Plugin.Log.LogInfo($"[SetProgress_Patch] Pending seek set to {progress:P1}, waiting for cache (progress: {ActivePcmReader.CacheProgress:F1}%)");
                    return false;
                }
                
                // 立即 Seek 成功
                FacilityMusic_UpdateFacility_Patch.IsWaitingForSeek = false;
                FacilityMusic_UpdateFacility_Patch.PendingSeekProgress = 0f;
                
                // 同步更新 AudioSource.time（虽然 PCM 流控制实际位置，但需要同步 UI）
                // 【重要】使用原始时长，而不是 clip.length（包含 30 分钟余量）
                IsSeekingFromSetProgress = true;
                try
                {
                    float originalDuration = ActivePcmReader != null ? ActivePcmReader.Info.Duration : clip.length;
                    if (originalDuration <= 0) originalDuration = clip.length;
                    player.AudioSource.time = Mathf.Clamp(originalDuration * progress, 0f, clip.length);
                }
                finally
                {
                    IsSeekingFromSetProgress = false;
                }
                
                Plugin.Log.LogInfo($"[SetProgress_Patch] Seek succeeded to {progress:P1}");
                return false;
            }
            else
            {
                Plugin.Log.LogWarning($"[SetProgress_Patch] Seek failed");
                return false;
            }
        }

        /// <summary>
        /// 获取 MusicUI 的拖动状态
        /// </summary>
        private static bool GetMusicUIDraggingState()
        {
            try
            {
                // 尝试找到 MusicUI 实例
                var musicUI = UnityEngine.Object.FindObjectOfType<MusicUI>();
                if (musicUI == null) return false;
                
                // 由于使用了 Publicizer，可以直接访问 isDraggingProgressSlider
                return musicUI.isDraggingProgressSlider;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 当延迟 Seek 完成时调用（由 Go 端触发）
        /// </summary>
        public static void OnPendingSeekCompleted()
        {
            if (FacilityMusic_UpdateFacility_Patch.IsWaitingForSeek)
            {
                FacilityMusic_UpdateFacility_Patch.IsWaitingForSeek = false;
                Plugin.Log.LogInfo("[SetProgress_Patch] Pending seek completed, resuming progress updates");
            }
        }
    }
}
