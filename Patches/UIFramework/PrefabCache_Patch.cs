using Bulbul;
using ChillPatcher.UIFramework.Core;
using HarmonyLib;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// Prefab 缓存 Patch - 在游戏 UI 初始化时自动缓存可复用的 Prefab
    /// </summary>
    [HarmonyPatch]
    public class PrefabCache_Patch
    {
        /// <summary>
        /// 从 SettingUI 缓存 SimpleRectButton Prefab
        /// </summary>
        [HarmonyPatch(typeof(SettingUI), "Setup")]
        [HarmonyPostfix]
        static void SettingUI_Setup_Postfix(SettingUI __instance)
        {
            PrefabFactory.CacheFromSettingUI(__instance);
        }
        
        /// <summary>
        /// 从 MusicUI 缓存 PlayListButtons Prefab
        /// </summary>
        [HarmonyPatch(typeof(MusicUI), "Bulbul.IMusicListUI.Setup")]
        [HarmonyPostfix]
        static void MusicUI_Setup_Postfix(MusicUI __instance)
        {
            PrefabFactory.CacheFromMusicUI(__instance);
        }
    }
}
