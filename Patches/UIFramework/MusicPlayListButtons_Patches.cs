using Bulbul;
using HarmonyLib;
using ChillPatcher.ModuleSystem.Registry;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// MusicPlayListButtons补丁: 隐藏歌单歌曲的删除按钮
    /// </summary>
    [HarmonyPatch(typeof(MusicPlayListButtons))]
    public class MusicPlayListButtons_Patches
    {
        /// <summary>
        /// Patch Setup方法 - 对歌单歌曲隐藏删除按钮
        /// </summary>
        [HarmonyPatch("Setup")]
        [HarmonyPostfix]
        static void Setup_Postfix(MusicPlayListButtons __instance)
        {
            try
            {
                // 获取当前歌曲信息
                var audioInfo = __instance.AudioInfo;
                if (audioInfo == null)
                    return;
                
                // 检查是否是自定义歌曲 (来自模块)
                var musicInfo = MusicRegistry.Instance?.GetByUUID(audioInfo.UUID);
                if (musicInfo == null)
                    return;  // 不是自定义歌曲,保持原样
                
                // ✅ 是自定义歌曲,隐藏删除按钮
                var removeInteractableUI = Traverse.Create(__instance)
                    .Field("removeInteractableUI")
                    .GetValue<InteractableUI>();
                
                if (removeInteractableUI != null)
                {
                    removeInteractableUI.gameObject.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[HideDeleteButton] Error: {ex}");
            }
        }
    }
}
