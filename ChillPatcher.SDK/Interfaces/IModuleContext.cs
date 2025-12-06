using BepInEx.Configuration;
using BepInEx.Logging;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 模块上下文接口
    /// 由主程序提供给模块，包含所有可用的服务
    /// </summary>
    public interface IModuleContext
    {
        /// <summary>
        /// Tag 注册表
        /// 用于注册和查询自定义 Tag
        /// </summary>
        ITagRegistry TagRegistry { get; }

        /// <summary>
        /// 专辑注册表
        /// 用于注册和查询专辑
        /// </summary>
        IAlbumRegistry AlbumRegistry { get; }

        /// <summary>
        /// 歌曲注册表
        /// 用于注册和查询歌曲
        /// </summary>
        IMusicRegistry MusicRegistry { get; }

        /// <summary>
        /// 配置管理器
        /// 用于注册模块配置项
        /// </summary>
        IModuleConfigManager ConfigManager { get; }

        /// <summary>
        /// 事件总线
        /// 用于订阅和发布事件
        /// </summary>
        IEventBus EventBus { get; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        ManualLogSource Logger { get; }

        /// <summary>
        /// 默认封面提供器
        /// </summary>
        IDefaultCoverProvider DefaultCover { get; }

        /// <summary>
        /// 音频加载器
        /// 用于加载音频文件
        /// </summary>
        IAudioLoader AudioLoader { get; }

        /// <summary>
        /// 依赖加载器
        /// 用于加载原生 DLL 依赖
        /// </summary>
        IDependencyLoader DependencyLoader { get; }

        /// <summary>
        /// 模块数据目录
        /// 每个模块独立的数据存储目录
        /// </summary>
        string GetModuleDataPath(string moduleId);

        /// <summary>
        /// 模块原生库目录
        /// 每个模块独立的原生 DLL 存储目录
        /// </summary>
        string GetModuleNativePath(string moduleId);
    }
}
