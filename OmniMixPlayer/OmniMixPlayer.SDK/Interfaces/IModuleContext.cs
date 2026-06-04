using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.SDK.Interfaces
{
    /// <summary>
    /// 模块上下文 — 模块通过此接口与平台交互
    /// </summary>
    public interface IModuleContext
    {
        /// <summary>音乐库注册接口（声明式 upsert）</summary>
        ILibraryRegistry Library { get; }

        /// <summary>模块配置管理器</summary>
        IModuleConfigManager ConfigManager { get; }

        /// <summary>事件总线</summary>
        IEventBus EventBus { get; }

        /// <summary>日志</summary>
        ILogger Logger { get; }

        /// <summary>默认封面提供者</summary>
        IDefaultCoverProvider DefaultCover { get; }

        /// <summary>流式音频解码服务</summary>
        IStreamingService StreamingService { get; }

        /// <summary>原生依赖加载器</summary>
        IDependencyLoader DependencyLoader { get; }

        /// <summary>模块数据目录</summary>
        string GetModuleDataPath(string moduleId);

        /// <summary>模块原生库目录</summary>
        string GetModuleNativePath(string moduleId);
    }
}
