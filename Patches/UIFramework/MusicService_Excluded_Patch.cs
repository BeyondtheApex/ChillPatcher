using System;
using Bulbul;
using ChillPatcher.ModuleSystem;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;
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
                    // 使用模块的 IFavoriteExcludeHandler 检查当前状态
                    var handler = ModuleLoader.Instance?.GetProvider<IFavoriteExcludeHandler>(musicInfo.ModuleId);
                    bool currentlyExcluded = handler?.IsExcluded(gameAudioInfo.UUID) ?? musicInfo.IsExcluded;
                    
                    if (currentlyExcluded)
                    {
                        __result = false;
                        return false;
                    }

                    // 通过模块处理器设置排除状态（这会保存到数据库并发布事件）
                    handler?.SetExcluded(gameAudioInfo.UUID, true);
                    
                    // 更新 MusicInfo.IsExcluded 属性以保持同步
                    musicInfo.IsExcluded = true;
                    
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
                    // 使用模块的 IFavoriteExcludeHandler 检查当前状态
                    var handler = ModuleLoader.Instance?.GetProvider<IFavoriteExcludeHandler>(musicInfo.ModuleId);
                    bool currentlyExcluded = handler?.IsExcluded(gameAudioInfo.UUID) ?? musicInfo.IsExcluded;
                    
                    if (!currentlyExcluded)
                    {
                        __result = false;
                        return false;
                    }

                    // 通过模块处理器设置包含状态（这会保存到数据库并发布事件）
                    handler?.SetExcluded(gameAudioInfo.UUID, false);
                    
                    // 更新 MusicInfo.IsExcluded 属性以保持同步
                    musicInfo.IsExcluded = false;
                    
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
                    // 从模块的 IFavoriteExcludeHandler 获取最新状态
                    var handler = ModuleLoader.Instance?.GetProvider<IFavoriteExcludeHandler>(musicInfo.ModuleId);
                    if (handler != null)
                    {
                        __result = handler.IsExcluded(gameAudioInfo.UUID);
                    }
                    else
                    {
                        // 回退到 MusicInfo.IsExcluded 属性
                        __result = musicInfo.IsExcluded;
                    }
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
