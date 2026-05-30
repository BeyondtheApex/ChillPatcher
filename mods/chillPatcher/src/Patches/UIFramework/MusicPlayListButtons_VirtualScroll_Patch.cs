using Bulbul;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 修复虚拟滚动中 MusicPlayListButtons 的状态重置问题
    /// 当按钮从对象池中重用时，确保其内部状态被正确重置
    /// 
    /// ✅ 使用 Publicizer 直接访问 private 字段（消除反射开销）
    /// </summary>
    [HarmonyPatch(typeof(MusicPlayListButtons))]
    public static class MusicPlayListButtons_VirtualScroll_Patch
    {

        /// <summary>
        /// 在 Setup 方法执行后，重置按钮的内部状态
        /// 这确保了虚拟滚动重用按钮时，所有状态都是干净的
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MusicPlayListButtons.Setup))]
        public static void Setup_Postfix(MusicPlayListButtons __instance)
        {

            try
            {
                // 1. 杀死所有 DOTween 动画（关键！）
                __instance.transform.DOKill();

                // 2. 强制重置 localScale 到原始大小
                __instance.transform.localScale = Vector3.one;

                // 3. ✅ 直接访问 MusicPlayListButtons 的 private 字段（Publicizer 消除反射）
                __instance.isMouseOver = false;
                __instance.isDirty = true;  // 设置为 true 以触发一次更新
                __instance.isDrag = false;

                // 4. 重置所有 HoldButtonAnimation 组件的状态
                var holdButtonAnims = __instance.GetComponentsInChildren<HoldButtonAnimation>(true);
                foreach (var holdAnim in holdButtonAnims)
                {
                    if (holdAnim != null)
                    {
                        // 杀死该组件上的 DOTween 动画
                        holdAnim.transform.DOKill();
                        
                        // 重置 transform scale
                        holdAnim.transform.localScale = Vector3.one;

                        // ✅ 直接访问 HoldButtonAnimation 的 private 字段（Publicizer 消除反射）
                        holdAnim.isMouseOvered = false;
                        holdAnim.hoverScaled = false;
                        holdAnim.clickScaled = false;
                        holdAnim.isActivated = false;
                        holdAnim.defaultScale = Vector3.one;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[MusicPlayListButtons_VirtualScroll_Patch] Error resetting state: {ex.Message}");
            }
        }
    }
}
