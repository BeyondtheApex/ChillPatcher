using System;
using Bulbul;
using ChillPatcher.ModuleSystem;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Events;
using HarmonyLib;

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

        /// <summary>
        /// Patch ExcludeFromPlaylist - 排除歌曲
        /// </summary>
        [HarmonyPatch("ExcludeFromPlaylist")]
        [HarmonyPrefix]
        static bool ExcludeFromPlaylist_Prefix(MusicService __instance, GameAudioInfo gameAudioInfo, ref bool __result)
        {
            try
            {
                // 检查是否是模块注册的歌曲
                var musicInfo = MusicRegistry.Instance?.GetMusic(gameAudioInfo.UUID);
                if (musicInfo != null)
                {
                    if (__instance.IsContainsExcludedFromPlaylist(gameAudioInfo))
                    {
                        __result = false;
                        return false;
                    }

                    // 发布排除事件，让模块处理
                    EventBus.Instance?.Publish(new ExcludeChangedEvent
                    {
                        Music = musicInfo,
                        IsExcluded = true,
                        ModuleId = musicInfo.ModuleId
                    });
                    
                    __result = true;
                    OnSongExcludedChanged?.Invoke(gameAudioInfo.UUID, true);
                    
                    Plugin.Log.LogInfo($"[Excluded] Module song excluded: {gameAudioInfo.UUID}");
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
            var musicInfo = MusicRegistry.Instance?.GetMusic(gameAudioInfo.UUID);
            if (__result && musicInfo == null)
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
                var musicInfo = MusicRegistry.Instance?.GetMusic(gameAudioInfo.UUID);
                if (musicInfo != null)
                {
                    if (!__instance.IsContainsExcludedFromPlaylist(gameAudioInfo))
                    {
                        __result = false;
                        return false;
                    }

                    // 发布包含事件
                    EventBus.Instance?.Publish(new ExcludeChangedEvent
                    {
                        Music = musicInfo,
                        IsExcluded = false,
                        ModuleId = musicInfo.ModuleId
                    });
                    
                    __result = true;
                    OnSongExcludedChanged?.Invoke(gameAudioInfo.UUID, false);
                    
                    Plugin.Log.LogInfo($"[Excluded] Module song included: {gameAudioInfo.UUID}");
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
            try
            {
                var musicInfo = MusicRegistry.Instance?.GetMusic(gameAudioInfo.UUID);
                if (musicInfo != null)
                {
                    // 模块歌曲使用 MusicInfo.IsExcluded 属性
                    __result = musicInfo.IsExcluded;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Excluded] Check status failed: {ex}");
                return true;
            }
        }
    }
}
