using System;
using Bulbul;
using HarmonyLib;
using ChillPatcher.UIFramework.Audio;
using Cysharp.Threading.Tasks;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 拦截MusicService的收藏操作，通过事件通知模块处理
    /// </summary>
    [HarmonyPatch(typeof(MusicService))]
    public class MusicService_Favorite_Patch
    {
        /// <summary>
        /// 收藏状态变化事件 (songUUID, isFavorite)
        /// 用于通知 UI 刷新
        /// </summary>
        public static event Action<string, bool> OnSongFavoriteChanged;

        /// <summary>
        /// 拦截添加收藏
        /// </summary>
        [HarmonyPatch("RegisterFavoriteMusic")]
        [HarmonyPrefix]
        static bool RegisterFavoriteMusic_Prefix(GameAudioInfo gameAudioInfo)
        {
            try
            {
                if (gameAudioInfo == null)
                    return true;

                if (gameAudioInfo.Tag.HasFlagFast(AudioTag.Favorite))
                    return false;

                if (StreamingAudioLoader.IsStreamingSource(gameAudioInfo))
                {
                    gameAudioInfo.Tag = gameAudioInfo.Tag | AudioTag.Favorite;
                    OmniMixIntegration.Instance.SetFavorite(gameAudioInfo.UUID, true).Forget();
                    OnSongFavoriteChanged?.Invoke(gameAudioInfo.UUID, true);
                    Plugin.Log.LogInfo($"[Favorite] Stream song favorited: {gameAudioInfo.UUID}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Favorite] Add failed: {ex}");
                return true;
            }
        }

        /// <summary>
        /// 拦截移除收藏
        /// </summary>
        [HarmonyPatch("UnregisterFavoriteMusic")]
        [HarmonyPrefix]
        static bool UnregisterFavoriteMusic_Prefix(MusicService __instance, GameAudioInfo gameAudioInfo)
        {
            try
            {
                if (gameAudioInfo == null)
                    return true;

                if (!gameAudioInfo.Tag.HasFlagFast(AudioTag.Favorite))
                    return false;

                if (StreamingAudioLoader.IsStreamingSource(gameAudioInfo))
                {
                    gameAudioInfo.Tag = gameAudioInfo.Tag & ~AudioTag.Favorite;
                    
                    var currentAudioTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag;
                    var currentValue = currentAudioTag.CurrentValue;
                    
                    if (!currentValue.HasFlagFast(gameAudioInfo.Tag))
                    {
                        var currentPlayList = Traverse.Create(__instance)
                            .Field("CurrentPlayList")
                            .GetValue<System.Collections.Generic.List<GameAudioInfo>>();
                        var shuffleList = Traverse.Create(__instance)
                            .Field("shuffleList")
                            .GetValue<System.Collections.Generic.List<GameAudioInfo>>();
                        
                        currentPlayList?.Remove(gameAudioInfo);
                        shuffleList?.Remove(gameAudioInfo);
                    }
                    
                    OmniMixIntegration.Instance.SetFavorite(gameAudioInfo.UUID, false).Forget();
                    OnSongFavoriteChanged?.Invoke(gameAudioInfo.UUID, false);
                    
                    Plugin.Log.LogInfo($"[Favorite] Stream song unfavorited: {gameAudioInfo.UUID}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Favorite] Remove failed: {ex}");
                return true;
            }
        }
    }
}
