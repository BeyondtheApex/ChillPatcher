using HarmonyLib;
using Bulbul;
using ChillPatcher.Integration;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Patch 5: 修复路径 - BulbulConstant.CreateSaveDirectoryPath (重载1)
    /// 壁纸引擎模式/多存档模式 → 使用配置用户ID
    /// Postfix 始终追加 profile 子路径（支持存档 Profile 切换）
    /// </summary>
    [HarmonyPatch(typeof(BulbulConstant), "CreateSaveDirectoryPath", new System.Type[] { typeof(bool), typeof(string) })]
    public class BulbulConstant_CreateSaveDirectoryPath1_Patch
    {
        static bool Prefix(bool isDemo, string version, ref string __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value && !PluginConfig.UseMultipleSaveSlots.Value)
                return true;
                
            string userID = PluginConfig.OfflineUserId.Value;
            __result = System.IO.Path.Combine("SaveData", isDemo ? "Demo" : "Release", version, userID);
            return false;
        }

        static void Postfix(ref string __result)
        {
            __result = SaveProfileService.GetActiveSaveRelativePath(__result);
        }
    }

    /// <summary>
    /// Patch 6: 修复路径 - BulbulConstant.CreateSaveDirectoryPath (重载2)
    /// 壁纸引擎模式/多存档模式 → 使用配置用户ID
    /// Postfix 始终追加 profile 子路径
    /// </summary>
    [HarmonyPatch(typeof(BulbulConstant), "CreateSaveDirectoryPath", new System.Type[] { typeof(string) })]
    public class BulbulConstant_CreateSaveDirectoryPath2_Patch
    {
        static bool Prefix(string versionDirectory, ref string __result)
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value && !PluginConfig.UseMultipleSaveSlots.Value)
                return true;
                
            string userID = PluginConfig.OfflineUserId.Value;
            __result = System.IO.Path.Combine("SaveData", versionDirectory, userID);
            return false;
        }

        static void Postfix(ref string __result)
        {
            __result = SaveProfileService.GetActiveSaveRelativePath(__result);
        }
    }
}
