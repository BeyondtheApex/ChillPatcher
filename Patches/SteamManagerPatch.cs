using HarmonyLib;
using NestopiSystem.Steam;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Patch 2: 核心阻断 - SteamManager.Tick
    /// 切断心跳
    /// </summary>
    [HarmonyPatch(typeof(SteamManager), "VContainer.Unity.ITickable.Tick")]
    public class SteamManager_Tick_Patch
    {
        static bool Prefix()
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return true; // 不屏蔽，执行原方法
                
            return false; // 阻止原方法执行
        }
    }

    /// <summary>
    /// Patch 3: 核心阻断 - SteamManager.IsInstalledDLC
    /// 根据配置返回是否启用DLC
    /// </summary>
    [HarmonyPatch(typeof(SteamManager), "IsInstalledDLC")]
    public class SteamManager_IsInstalledDLC_Patch
    {
        static bool Prefix(ref bool __result)
        {
            __result = PluginConfig.EnableDLC.Value;
            if (__result)
            {
                Plugin.Logger.LogInfo("[ChillPatcher] IsInstalledDLC - DLC已启用");
            }
            return false; // 阻止原方法执行
        }
    }
}
