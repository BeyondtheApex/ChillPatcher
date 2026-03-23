using HarmonyLib;
using Steamworks;
using System;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Patch: SteamManager.Initialize — 壁纸引擎模式的延迟/静默启动
    /// 跳过会导致 Application.Quit() 的步骤，直接尝试 SteamAPI.Init()；
    /// 失败时标记为 Pending（后台继续尝试重连）而非退出游戏。
    /// </summary>
    [HarmonyPatch(typeof(NestopiSystem.Steam.SteamManager), "Initialize")]
    public class SteamManager_Initialize_Patch
    {
        static bool Prefix(NestopiSystem.Steam.SteamManager __instance)
        {
            // 仅在壁纸引擎模式下接管初始化流程
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return true;

            SteamConnectionState.ManagerInstance = __instance;

            var isInitField = AccessTools.Field(
                typeof(NestopiSystem.Steam.SteamManager), "isInitialized");

            if ((bool)isInitField.GetValue(__instance))
                return false; // 已初始化，跳过

            try
            {
                // 跳过 RestartAppIfNecessary（不运行中的情况下会触发 Application.Quit）
                string initError;
                bool success = SteamAPI.InitEx(out initError) == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
                isInitField.SetValue(__instance, success);

                if (success)
                {
                    SteamConnectionState.MarkConnected();
                    SteamUserStats.RequestUserStats(SteamUser.GetSteamID()); // 预热当前用户 stats
                    Plugin.Logger.LogInfo("[WallpaperEngine] Steam 连接成功");
                    // 启动时直接连上：成就同步由 AchievementSyncManager 的定时器处理
                }
                else
                {
                    SteamConnectionState.MarkPending();
                    Plugin.Logger.LogInfo(
                        $"[WallpaperEngine] Steam 初始化未成功，将在后台等待并重连。原因: {initError}");
                }
            }
            catch (DllNotFoundException ex)
            {
                isInitField.SetValue(__instance, false);
                SteamConnectionState.MarkFailed($"steam_api.dll 缺失: {ex.Message}");
            }
            catch (Exception ex)
            {
                isInitField.SetValue(__instance, false);
                SteamConnectionState.MarkPending();
                Plugin.Logger.LogWarning($"[WallpaperEngine] Steam 初始化异常（将重试）: {ex.Message}");
            }

            return false; // 跳过原始方法
        }
    }

    /// <summary>
    /// Patch: SteamUtils.GetAppID
    /// 壁纸引擎模式且 Steam 尚未就绪时，返回游戏实际 AppID 避免异常。
    /// Steam 连接后放行，使用真实 API。
    /// </summary>
    [HarmonyPatch(typeof(SteamUtils), nameof(SteamUtils.GetAppID))]
    public class SteamUtils_GetAppID_Patch
    {
        static bool Prefix(ref AppId_t __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value) return true;
            if (SteamConnectionState.IsSteamActuallyInitialized) return true; // Steam 已连接，使用真实值

            __result = new AppId_t(3548580);
            return false;
        }
    }

    /// <summary>
    /// Patch: SteamFriends.GetPersonaName
    /// 壁纸引擎模式且 Steam 尚未就绪时，返回配置的离线用户名。
    /// Steam 连接后放行，使用真实 API。
    /// </summary>
    [HarmonyPatch(typeof(SteamFriends), nameof(SteamFriends.GetPersonaName))]
    public class SteamFriends_GetPersonaName_Patch
    {
        static bool Prefix(ref string __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value) return true;
            if (SteamConnectionState.IsSteamActuallyInitialized) return true;

            __result = PluginConfig.OfflineUserId.Value;
            return false;
        }
    }

    /// <summary>
    /// Patch: SteamUser.GetSteamID
    /// 壁纸引擎模式且 Steam 尚未就绪时，返回配置的离线 ID。
    /// Steam 连接后放行，使用真实 API。
    /// </summary>
    [HarmonyPatch(typeof(SteamUser), nameof(SteamUser.GetSteamID))]
    public class SteamUser_GetSteamID_Patch
    {
        static bool Prefix(ref CSteamID __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value) return true;
            if (SteamConnectionState.IsSteamActuallyInitialized) return true;

            ulong steamId;
            if (ulong.TryParse(PluginConfig.OfflineUserId.Value, out steamId))
                __result = new CSteamID(steamId);
            else
                __result = new CSteamID(76561198000000000UL);

            Plugin.Logger.LogInfo($"[WallpaperEngine] SteamUser.GetSteamID → 离线ID: {__result}");
            return false;
        }
    }
}

