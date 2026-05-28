using HarmonyLib;
using Bulbul;
using System;
using R3;
using ChillPatcher.SDK.Events;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.UIFramework.Audio;
using Cysharp.Threading.Tasks;


namespace ChillPatcher.Patches
{
    /// <summary>
    /// 系统媒体传输控制补丁 - 将游戏播放状态同步到 Windows SMTC
    /// </summary>
    [HarmonyPatch]
    public class SystemMediaTransport_Patches
    {
        private static IDisposable _playMusicSubscription;
        private static IDisposable _changeMusicSubscription;
        private static IDisposable _progressEventSubscription;

        /// <summary>
        /// 在 FacilityMusic.Setup 之后初始化 SMTC 服务
        /// </summary>
        [HarmonyPatch(typeof(FacilityMusic), "Setup")]
        [HarmonyPostfix]
        static void FacilityMusic_Setup_Postfix(FacilityMusic __instance)
        {
            try
            {
                if (!PluginConfig.EnableSystemMediaTransport.Value)
                    return;

                // 初始化 SMTC 服务
                SystemMediaTransportService.Instance.Initialize();
                
                // 设置游戏服务引用
                SystemMediaTransportService.Instance.SetGameServices(
                    __instance.MusicService,
                    __instance
                );

                // 释放旧订阅，防止场景重载后泄漏
                _playMusicSubscription?.Dispose();
                _changeMusicSubscription?.Dispose();
                _progressEventSubscription?.Dispose();

                // 订阅播放事件
                _playMusicSubscription = __instance.MusicService.onPlayMusic.Subscribe(OnPlayMusic);
                _changeMusicSubscription = __instance.MusicService.onChangeMusic.Subscribe(OnChangeMusic);

                Plugin.Log.LogInfo("[SMTC] 服务已初始化并绑定到游戏");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SMTC] 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 当音乐播放时更新 SMTC
        /// </summary>
        private static void OnPlayMusic(GameAudioInfo audioInfo)
        {
            try
            {
                if (audioInfo == null) return;
                
                SystemMediaTransportService.Instance.UpdateMediaInfo(audioInfo);
                SystemMediaTransportService.Instance.SetPlaybackStatus(true);
                
                // 更新时间线：优先使用 PCM reader 的真实时长
                var reader = MusicService_SetProgress_Patch.ActivePcmReader;
                if (reader != null && reader.Info.Duration > 0)
                {
                    long durationMs = (long)(reader.Info.Duration * 1000);
                    SystemMediaTransportService.Instance.UpdateTimeline(durationMs, 0);
                }
                else if (audioInfo.AudioClip != null)
                {
                    long durationMs = (long)(audioInfo.AudioClip.length * 1000);
                    SystemMediaTransportService.Instance.UpdateTimeline(durationMs, 0);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SMTC] OnPlayMusic 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 当音乐切换时更新 SMTC
        /// </summary>
        private static void OnChangeMusic(MusicChangeKind changeKind)
        {
            // 切换时状态会短暂变为 Changing
            // 实际信息由 OnPlayMusic 更新
        }



        /// <summary>
        /// 当暂停音乐时更新 SMTC 状态并同步到后端
        /// </summary>
        [HarmonyPatch(typeof(FacilityMusic), "PauseMusic")]
        [HarmonyPostfix]
        static void PauseMusic_Postfix()
        {
            try
            {
                if (PluginConfig.EnableSystemMediaTransport.Value)
                    SystemMediaTransportService.Instance.SetPlaybackStatus(false);

                if (OmniMixIntegration.Instance != null && OmniMixIntegration.Instance.IsConnected)
                {
                    OmniMixIntegration.Instance.Pause().Forget();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SMTC] PauseMusic 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 当恢复播放时更新 SMTC 状态并同步到后端
        /// </summary>
        [HarmonyPatch(typeof(FacilityMusic), "UnPauseMusic")]
        [HarmonyPostfix]
        static void UnPauseMusic_Postfix()
        {
            try
            {
                if (PluginConfig.EnableSystemMediaTransport.Value)
                    SystemMediaTransportService.Instance.SetPlaybackStatus(true);

                if (OmniMixIntegration.Instance != null && OmniMixIntegration.Instance.IsConnected)
                {
                    OmniMixIntegration.Instance.Resume().Forget();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SMTC] UnPauseMusic 异常: {ex.Message}");
            }
        }
    }
}
