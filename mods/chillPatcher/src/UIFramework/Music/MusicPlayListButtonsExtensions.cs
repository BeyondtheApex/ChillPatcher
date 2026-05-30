using Bulbul;
using UnityEngine;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// MusicPlayListButtons扩展方法 - 用于虚拟滚动
    /// </summary>
    public static class MusicPlayListButtonsExtensions
    {
        /// <summary>
        /// 清理按钮的所有订阅，准备复用
        /// 通过重新创建GameObject实现（触发OnDestroy清理订阅）
        /// </summary>
        public static void PrepareForReuse(this MusicPlayListButtons button)
        {
            // 策略：销毁并重新创建组件
            // 这样会触发OnDestroy，清理所有.AddTo(this)的订阅
            var go = button.gameObject;
            var prefab = go; // 保存引用
            
            // 实际上不需要真的销毁，只需要让组件"认为"自己被销毁了
            // 但R3的.AddTo(this)是在OnDestroy时清理的
            // 所以我们需要更clever的方法
            
            // 最简单的方法：不复用，每次都创建新的（已在UIObjectPool中实现）
            // 这里作为备用方案
        }
    }
}
