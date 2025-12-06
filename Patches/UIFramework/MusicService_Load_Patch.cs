using Bulbul;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using System;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 在MusicService.Load后加载模块
    /// </summary>
    [HarmonyPatch(typeof(MusicService), "Load")]
    public static class MusicService_Load_Patch
    {
        private static bool _modulesLoaded = false;
        
        [HarmonyPostfix]
        static void Postfix(MusicService __instance)
        {
            // ✅ 首先保存实例引用，确保后续代码可用
            MusicService_RemoveLimit_Patch.CurrentInstance = __instance;
            
            if (_modulesLoaded)
                return;
            
            _modulesLoaded = true;
            
            var logger = BepInEx.Logging.Logger.CreateLogSource("MusicService_Load_Patch");
            
            // 延迟加载模块
            UniTask.Void(async () =>
            {
                try
                {
                    logger.LogInfo("Starting module loading...");
                    await Plugin.LoadModulesAsync();
                    logger.LogInfo("✅ Modules loaded successfully!");
                }
                catch (Exception ex)
                {
                    logger.LogError($"❌ Module loading failed: {ex}");
                }
            });
        }
    }
}
