using Bulbul;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// MusicPlayListButtons Patch - 移除拖动柄
    /// </summary>
    [HarmonyPatch]
    public class MusicPlayListButtons_ReorderPatch
    {
        /// <summary>
        /// Patch MusicPlayListButtons.Setup - 清理旧订阅并移除拖动柄
        /// </summary>
        [HarmonyPatch(typeof(MusicPlayListButtons), "Setup", typeof(GameAudioInfo), typeof(FacilityMusic))]
        [HarmonyPrefix]
        static void Setup_Prefix(MusicPlayListButtons __instance)
        {

            try
            {
                // 清理Button的onClick监听器
                var playButton = Traverse.Create(__instance)
                    .Field("_playMusicbutton")
                    .GetValue<UnityEngine.UI.Button>();
                    
                if (playButton != null)
                {
                    playButton.onClick.RemoveAllListeners();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error cleaning subscriptions: {ex}");
            }
        }

        /// <summary>
        /// Patch MusicPlayListButtons.Start - 阻止访问被销毁的reorderTrigger
        /// </summary>
        [HarmonyPatch(typeof(MusicPlayListButtons), "Start")]
        [HarmonyPrefix]
        static bool Start_Prefix(MusicPlayListButtons __instance)
        {

            try
            {
                // 检查reorderTrigger是否存在
                var reorderTrigger = Traverse.Create(__instance)
                    .Field("reorderTrigger")
                    .GetValue<EventTrigger>();

                if (reorderTrigger == null || reorderTrigger.gameObject == null)
                {
                    // reorderTrigger已被销毁，跳过Start方法
                    // 只初始化必要的部分
                    var dragInteractableUI = Traverse.Create(__instance)
                        .Field("_dragInteractableUI")
                        .GetValue<InteractableUI>();
                    
                    if (dragInteractableUI != null)
                    {
                        dragInteractableUI.Setup();
                    }
                    
                    return false; // 跳过原Start方法
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in Start_Prefix: {ex}");
            }

            return true; // 执行原Start方法
        }
        
        /// <summary>
        /// Patch MusicPlayListButtons.Setup - 移除拖动柄
        /// </summary>
        [HarmonyPatch(typeof(MusicPlayListButtons), "Setup", typeof(GameAudioInfo), typeof(FacilityMusic))]
        [HarmonyPostfix]
        static void Setup_Postfix(MusicPlayListButtons __instance, GameAudioInfo audioInfo, FacilityMusic facilityMusic)
        {
            // 队列模式保留拖动柄
            if (MusicUI_VirtualScroll_Patch.IsShowingQueue)
                return;

            try
            {
                // 获取原来的拖动柄
                var reorderTrigger = Traverse.Create(__instance)
                    .Field("reorderTrigger")
                    .GetValue<EventTrigger>();

                if (reorderTrigger != null)
                {
                    // 直接销毁拖动柄GameObject，让HorizontalLayoutGroup重新布局
                    UnityEngine.Object.Destroy(reorderTrigger.gameObject);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error removing reorder handle: {ex}");
            }
        }
    }
}
