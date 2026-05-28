using HarmonyLib;
using Bulbul;
using KanKikuchi.AudioManager;
using ChillPatcher.UIFramework.Audio;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 拦截 MusicService.GetCurrentMusicProgress，支持流媒体的正确进度显示
    /// 
    /// 问题：
    /// 1. 原始实现使用 AudioSource.time / clip.length 计算进度
    /// 2. 对于流式 PCM 播放，AudioSource.time 可能不准确（特别是 Seek 后）
    /// 3. 流暂停时（网络波动、等待缓存），进度条应该也暂停
    /// 4. Go 端完成待定 Seek 后，C# 端不会收到通知
    /// 
    /// 解决方案：
    /// 1. 对于流媒体歌曲，使用 PCM reader 的 CurrentFrame / TotalFrames 计算进度
    /// 2. 直接从 PCM reader 检查 HasPendingSeek，不依赖 C# 端的标志
    /// 3. 当 HasPendingSeek 时，返回待定位置的进度
    /// 4. Seek 完成后自动恢复正常进度更新
    /// </summary>
    [HarmonyPatch]
    public static class MusicService_GetProgress_Patch
    {
        /// <summary>
        /// 上次有效的进度值（用于流暂停时保持进度）
        /// </summary>
        private static float _lastValidProgress = 0f;

        [HarmonyPatch(typeof(MusicService), nameof(MusicService.GetCurrentMusicProgress))]
        [HarmonyPrefix]
        public static bool GetCurrentMusicProgress_Prefix(MusicService __instance, ref float __result)
        {
            var playingMusic = __instance.PlayingMusic;
            if (playingMusic == null || string.IsNullOrEmpty(playingMusic.AudioClipName))
            {
                __result = 0f;
                return false; // 跳过原始逻辑
            }

            // 检查是否是流媒体歌曲
            if (!StreamingAudioLoader.IsStreamingSource(playingMusic))
            {
                // 不是流媒体，使用原始逻辑
                return true;
            }

            // 如果正在拖动且有预览进度，返回预览进度
            if (MusicService_SetProgress_Patch.IsDragging && MusicService_SetProgress_Patch.PreviewProgress >= 0)
            {
                __result = MusicService_SetProgress_Patch.PreviewProgress;
                return false;
            }

            var reader = MusicService_SetProgress_Patch.ActivePcmReader;
            if (reader != null && reader.Info.TotalFrames > 0)
            {
                float progress = (float)reader.CurrentFrame / (float)reader.Info.TotalFrames;
                __result = UnityEngine.Mathf.Clamp01(progress);
                _lastValidProgress = __result;
                return false;
            }

            var integration = OmniMixIntegration.Instance;
            if (integration != null && integration.CurrentTrackDuration > 0)
            {
                float progress = integration.CurrentTrackPosition / integration.CurrentTrackDuration;
                __result = UnityEngine.Mathf.Clamp01(progress);
                _lastValidProgress = __result;
                return false;
            }

            __result = _lastValidProgress;
            return false;
        }

        /// <summary>
        /// 重置进度（切换歌曲时调用）
        /// </summary>
        public static void ResetProgress()
        {
            _lastValidProgress = 0f;
        }
    }
}
