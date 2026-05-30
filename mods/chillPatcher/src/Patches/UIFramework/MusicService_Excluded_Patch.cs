using System;
using Bulbul;
using HarmonyLib;
using ChillPatcher.UIFramework.Audio;
using Cysharp.Threading.Tasks;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 拦截MusicService的排除列表操作，对模块歌曲使用事件通知
    /// </summary>
    [HarmonyPatch(typeof(MusicService))]
    public class MusicService_Excluded_Patch
    {
        /// <summary>
        /// 歌曲排除状态变化事件
        /// </summary>
        public static event Action<string, bool> OnSongExcludedChanged;

        public static void RaiseOnSongExcludedChanged(string uuid, bool isExcluded)
        {
            OnSongExcludedChanged?.Invoke(uuid, isExcluded);
        }

        /// <summary>
        /// Patch ExcludeFromPlaylist - 排除歌曲
        /// </summary>
        [HarmonyPatch("ExcludeFromPlaylist")]
        [HarmonyPrefix]
        static bool ExcludeFromPlaylist_Prefix(MusicService __instance, GameAudioInfo gameAudioInfo, ref bool __result)
        {
            try
            {
                if (StreamingAudioLoader.IsStreamingSource(gameAudioInfo))
                {
                    OmniMixIntegration.Instance.SetExcluded(gameAudioInfo.UUID, true).Forget();
                    __result = true;
                    OnSongExcludedChanged?.Invoke(gameAudioInfo.UUID, true);
                    Plugin.Log.LogInfo($"[Excluded] Stream song excluded: {gameAudioInfo.UUID}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Excluded] Exclude failed: {ex}");
                return true;
            }
        }

        [HarmonyPatch("ExcludeFromPlaylist")]
        [HarmonyPostfix]
        static void ExcludeFromPlaylist_Postfix(GameAudioInfo gameAudioInfo, bool __result)
        {
            if (__result && !StreamingAudioLoader.IsStreamingSource(gameAudioInfo))
            {
                OnSongExcludedChanged?.Invoke(gameAudioInfo.UUID, true);
            }
        }

        /// <summary>
        /// Patch IncludeInPlaylist - 重新包含歌曲
        /// </summary>
        [HarmonyPatch("IncludeInPlaylist")]
        [HarmonyPrefix]
        static bool IncludeInPlaylist_Prefix(MusicService __instance, GameAudioInfo gameAudioInfo, ref bool __result)
        {
            try
            {
                if (StreamingAudioLoader.IsStreamingSource(gameAudioInfo))
                {
                    OmniMixIntegration.Instance.SetExcluded(gameAudioInfo.UUID, false).Forget();
                    __result = true;
                    OnSongExcludedChanged?.Invoke(gameAudioInfo.UUID, false);
                    Plugin.Log.LogInfo($"[Excluded] Stream song included: {gameAudioInfo.UUID}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Excluded] Include failed: {ex}");
                return true;
            }
        }

        /// <summary>
        /// Patch IsContainsExcludedFromPlaylist - 检查排除状态
        /// </summary>
        [HarmonyPatch("IsContainsExcludedFromPlaylist")]
        [HarmonyPrefix]
        static bool IsContainsExcludedFromPlaylist_Prefix(GameAudioInfo gameAudioInfo, ref bool __result)
        {
            if (gameAudioInfo == null) return true;
            if (StreamingAudioLoader.IsStreamingSource(gameAudioInfo))
            {
                __result = OmniMixIntegration.Instance != null && OmniMixIntegration.Instance.IsExcluded(gameAudioInfo.UUID);
                return false;
            }
            return true;
        }
    }
}
