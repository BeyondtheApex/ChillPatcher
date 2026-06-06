using System;
using System.Collections.Generic;
using System.Linq;
using Bulbul;
using HarmonyLib;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 拦截MusicService的播放顺序操作，通过事件通知模块
    /// </summary>
    [HarmonyPatch(typeof(MusicService))]
    public class MusicService_PlaylistOrder_Patch
    {
        /// <summary>
        /// 拦截添加音乐到播放列表
        /// </summary>
        [HarmonyPatch("AddMusicItem")]
        [HarmonyPostfix]
        static void AddMusicItem_Postfix(bool __result, GameAudioInfo music)
        {
            // TODO: IPC bridge needed - MusicRegistry/EventBus removed for playlist order events
        }

        /// <summary>
        /// 拦截添加本地音乐
        /// </summary>
        [HarmonyPatch("AddLocalMusicItem")]
        [HarmonyPostfix]
        static void AddLocalMusicItem_Postfix(bool __result, GameAudioInfo music)
        {
            // TODO: IPC bridge needed - MusicRegistry/EventBus removed for playlist order events
        }

        /// <summary>
        /// 拦截移除本地音乐 - Prefix 处理模块歌曲
        /// </summary>
        [HarmonyPatch("RemoveLocalMusicItem")]
        [HarmonyPrefix]
        static bool RemoveLocalMusicItem_Prefix(MusicService __instance, GameAudioInfo music)
        {
            // TODO: IPC bridge needed - MusicRegistry/ModuleLoader removed for module song deletion
            return true;
        }

        /// <summary>
        /// 拦截移除本地音乐 - Postfix 发布事件
        /// </summary>
        [HarmonyPatch("RemoveLocalMusicItem")]
        [HarmonyPostfix]
        static void RemoveLocalMusicItem_Postfix(GameAudioInfo music)
        {
            // TODO: IPC bridge needed - MusicRegistry/EventBus removed for playlist order events
        }

        /// <summary>
        /// 拦截交换顺序
        /// </summary>
        [HarmonyPatch("SwapAfter")]
        [HarmonyPostfix]
        static void SwapAfter_Postfix(MusicService __instance, GameAudioInfo target, GameAudioInfo origin)
        {
            // TODO: IPC bridge needed - MusicRegistry/EventBus removed for playlist order events
        }
    }
}
