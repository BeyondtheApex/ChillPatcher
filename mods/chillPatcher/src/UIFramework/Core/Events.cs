using System;

namespace ChillPatcher.UIFramework.Core
{
    /// <summary>
    /// UI框架事件定义
    /// </summary>
    public static class UIFrameworkEvents
    {
        /// <summary>
        /// 框架初始化完成事件
        /// </summary>
        public static event Action OnFrameworkInitialized;

        /// <summary>
        /// 框架清理事件
        /// </summary>
        public static event Action OnFrameworkCleanup;

        internal static void RaiseInitialized()
        {
            OnFrameworkInitialized?.Invoke();
        }

        internal static void RaiseCleanup()
        {
            OnFrameworkCleanup?.Invoke();
        }
    }
}
