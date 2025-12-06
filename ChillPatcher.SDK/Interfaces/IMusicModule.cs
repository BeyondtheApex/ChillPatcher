using System.Threading.Tasks;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 音乐模块基础接口
    /// 所有音乐源模块必须实现此接口
    /// </summary>
    public interface IMusicModule
    {
        /// <summary>
        /// 模块唯一标识符
        /// 建议格式: "com.author.modulename"
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// 模块显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 模块版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 加载优先级 (越小越先加载)
        /// 默认值: 100
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 模块能力声明
        /// </summary>
        ModuleCapabilities Capabilities { get; }

        /// <summary>
        /// 初始化模块
        /// 在此阶段注册 Tag、专辑和歌曲
        /// </summary>
        /// <param name="context">主程序提供的上下文</param>
        Task InitializeAsync(IModuleContext context);

        /// <summary>
        /// 启用模块
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 禁用模块
        /// </summary>
        void OnDisable();

        /// <summary>
        /// 卸载模块
        /// 清理资源
        /// </summary>
        void OnUnload();
    }

    /// <summary>
    /// 模块能力声明
    /// </summary>
    public class ModuleCapabilities
    {
        /// <summary>
        /// 是否支持删除歌曲
        /// </summary>
        public bool CanDelete { get; set; } = false;

        /// <summary>
        /// 是否支持收藏
        /// </summary>
        public bool CanFavorite { get; set; } = true;

        /// <summary>
        /// 是否支持排除
        /// </summary>
        public bool CanExclude { get; set; } = true;

        /// <summary>
        /// 是否支持实时更新 (文件监控等)
        /// </summary>
        public bool SupportsLiveUpdate { get; set; } = false;

        /// <summary>
        /// 是否提供自己的封面
        /// </summary>
        public bool ProvidesCover { get; set; } = true;

        /// <summary>
        /// 是否提供自己的专辑
        /// </summary>
        public bool ProvidesAlbum { get; set; } = true;
    }
}
