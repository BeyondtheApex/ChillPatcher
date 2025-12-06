using System;

namespace ChillPatcher.SDK.Attributes
{
    /// <summary>
    /// 标记一个类为音乐模块
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MusicModuleAttribute : Attribute
    {
        /// <summary>
        /// 模块 ID
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// 模块显示名称
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 模块版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 加载优先级 (越小越先加载)
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// 模块作者
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 模块描述
        /// </summary>
        public string Description { get; set; }

        public MusicModuleAttribute(string moduleId, string displayName)
        {
            ModuleId = moduleId;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// 标记模块依赖
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ModuleDependencyAttribute : Attribute
    {
        /// <summary>
        /// 依赖的模块 ID
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// 最低版本要求
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// 是否为可选依赖
        /// </summary>
        public bool Optional { get; set; } = false;

        public ModuleDependencyAttribute(string moduleId)
        {
            ModuleId = moduleId;
        }
    }
}
