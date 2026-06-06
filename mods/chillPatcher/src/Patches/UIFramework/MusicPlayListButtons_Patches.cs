using Bulbul;
using HarmonyLib;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// MusicPlayListButtons补丁: 根据歌曲的可删除设置控制删除按钮显示
    /// </summary>
    [HarmonyPatch(typeof(MusicPlayListButtons))]
    public class MusicPlayListButtons_Patches
    {
        /// <summary>
        /// Patch Setup方法 - 根据歌曲/模块设置控制删除按钮
        /// </summary>
        [HarmonyPatch("Setup")]
        [HarmonyPostfix]
        static void Setup_Postfix(MusicPlayListButtons __instance)
        {
            // TODO: IPC bridge needed - MusicRegistry and ModuleLoader removed
        }
    }
}
