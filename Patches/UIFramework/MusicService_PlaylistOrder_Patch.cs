using System;
using System.Collections.Generic;
using System.Linq;
using Bulbul;
using ChillPatcher.ModuleSystem;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Events;
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
            try
            {
                if (!__result || music == null)
                    return;

                var musicInfo = MusicRegistry.Instance?.GetMusic(music.UUID);
                if (musicInfo != null)
                {
                    EventBus.Instance?.Publish(new PlaylistOrderChangedEvent
                    {
                        UpdateType = PlaylistUpdateType.SongAdded,
                        AffectedSongUUIDs = new string[] { music.UUID },
                        ModuleId = musicInfo.ModuleId
                    });
                    Plugin.Log.LogInfo($"[PlaylistOrder] Music added: {music.UUID}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PlaylistOrder] Add failed: {ex}");
            }
        }

        /// <summary>
        /// 拦截添加本地音乐
        /// </summary>
        [HarmonyPatch("AddLocalMusicItem")]
        [HarmonyPostfix]
        static void AddLocalMusicItem_Postfix(bool __result, GameAudioInfo music)
        {
            try
            {
                if (!__result || music == null)
                    return;

                var musicInfo = MusicRegistry.Instance?.GetMusic(music.UUID);
                if (musicInfo != null)
                {
                    EventBus.Instance?.Publish(new PlaylistOrderChangedEvent
                    {
                        UpdateType = PlaylistUpdateType.SongAdded,
                        AffectedSongUUIDs = new string[] { music.UUID },
                        ModuleId = musicInfo.ModuleId
                    });
                    Plugin.Log.LogInfo($"[PlaylistOrder] Local music added: {music.UUID}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PlaylistOrder] Add local failed: {ex}");
            }
        }

        /// <summary>
        /// 拦截移除本地音乐
        /// </summary>
        [HarmonyPatch("RemoveLocalMusicItem")]
        [HarmonyPostfix]
        static void RemoveLocalMusicItem_Postfix(GameAudioInfo music)
        {
            try
            {
                if (music == null)
                    return;

                var musicInfo = MusicRegistry.Instance?.GetMusic(music.UUID);
                if (musicInfo != null)
                {
                    EventBus.Instance?.Publish(new PlaylistOrderChangedEvent
                    {
                        UpdateType = PlaylistUpdateType.SongRemoved,
                        AffectedSongUUIDs = new string[] { music.UUID },
                        ModuleId = musicInfo.ModuleId
                    });
                    Plugin.Log.LogInfo($"[PlaylistOrder] Music removed: {music.UUID}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PlaylistOrder] Remove failed: {ex}");
            }
        }

        /// <summary>
        /// 拦截交换顺序
        /// </summary>
        [HarmonyPatch("SwapAfter")]
        [HarmonyPostfix]
        static void SwapAfter_Postfix(MusicService __instance, GameAudioInfo target, GameAudioInfo origin)
        {
            try
            {
                if (target == null)
                    return;

                var isShuffle = Traverse.Create(__instance).Property("IsShuffle").GetValue<bool>();
                if (isShuffle)
                    return;

                var musicInfo = MusicRegistry.Instance?.GetMusic(target.UUID);
                if (musicInfo != null)
                {
                    var currentAudioTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag.CurrentValue;
                    if (currentAudioTag == AudioTag.Favorite)
                        return;

                    // 获取当前所有音乐的UUID顺序
                    var allMusicList = Traverse.Create(__instance)
                        .Field("_allMusicList")
                        .GetValue<List<GameAudioInfo>>();

                    if (allMusicList != null)
                    {
                        var sameTagUuids = allMusicList
                            .Where(m => MusicRegistry.Instance?.GetMusic(m.UUID)?.TagId == musicInfo.TagId)
                            .Select(m => m.UUID)
                            .ToArray();

                        EventBus.Instance?.Publish(new PlaylistOrderChangedEvent
                        {
                            UpdateType = PlaylistUpdateType.Reordered,
                            AffectedSongUUIDs = sameTagUuids,
                            ModuleId = musicInfo.ModuleId
                        });
                        
                        Plugin.Log.LogInfo($"[PlaylistOrder] Order updated: {sameTagUuids.Length} songs");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PlaylistOrder] Swap failed: {ex}");
            }
        }
    }
}
