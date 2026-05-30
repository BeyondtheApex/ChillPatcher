using HarmonyLib;
using Bulbul;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Patch 4: 解除死锁 - EntryBehavior.StartAsync
    /// 通过修改 SteamManager.IsInitialized 属性绕过等待
    /// 注意：实际的 StartAsync 仍会执行，但由于 SteamManager.Initialize 被patch了，
    /// 它会直接认为Steam已初始化并继续执行
    /// </summary>
    [HarmonyPatch(typeof(EntryBehavior))]
    [HarmonyPatch("VContainer.Unity.IAsyncStartable.StartAsync")]
    public class EntryBehavior_StartAsync_Patch
    {
        static void Prefix()
        {
            Plugin.Logger.LogInfo("[ChillPatcher] EntryBehavior.StartAsync - 已通过 SteamManager patch 绕过死锁");
            // 不需要修改任何东西，因为 SteamManager.Initialize 已经被patch
            // 它会直接设置 isInitialized = false，但 IsInitialized 属性也会被patch
        }
    }

    /// <summary>
    /// Patch: 修复 SteamManager.IsInitialized 属性
    /// 壁纸引擎模式：始终返回 true（完全绕过Steam等待）
    /// 静默启动模式：Steam Pending时也返回 true，让游戏继续启动；连接后由原始逻辑接管
    /// </summary>
    [HarmonyPatch(typeof(NestopiSystem.Steam.SteamManager), "IsInitialized", MethodType.Getter)]
    public class SteamManager_IsInitialized_Patch
    {
        static bool Prefix(ref bool __result)
        {
            if (PluginConfig.EnableWallpaperEngineMode.Value)
            {
                __result = true;
                return false;
            }

            if (SteamConnectionState.CurrentState == SteamConnectionState.State.Pending)
            {
                __result = true; // Steam 待连接中，但不阻塞游戏启动
                return false;
            }

            return true; // 其他情况执行原方法
        }
    }
}
