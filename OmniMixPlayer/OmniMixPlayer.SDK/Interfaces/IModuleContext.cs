using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.SDK.Interfaces
{
    public interface IModuleContext
    {
        ITagRegistry TagRegistry { get; }
        IAlbumRegistry AlbumRegistry { get; }
        IMusicRegistry MusicRegistry { get; }
        IModuleConfigManager ConfigManager { get; }
        IEventBus EventBus { get; }
        ILogger Logger { get; }
        IDefaultCoverProvider DefaultCover { get; }
        IStreamingService StreamingService { get; }
        IDependencyLoader DependencyLoader { get; }
        IPlayQueue PlayQueue { get; }
        string GetModuleDataPath(string moduleId);
        string GetModuleNativePath(string moduleId);
    }
}
