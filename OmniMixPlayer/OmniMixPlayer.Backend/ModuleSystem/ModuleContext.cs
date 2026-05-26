using System;
using System.IO;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem
{
    public class ModuleContext : IModuleContext
    {
        private readonly string _pluginPath;
        private readonly string _moduleId;

        public ITagRegistry TagRegistry { get; }
        public IAlbumRegistry AlbumRegistry { get; }
        public IMusicRegistry MusicRegistry { get; }
        public IModuleConfigManager ConfigManager { get; }
        public IEventBus EventBus { get; }
        public ILogger Logger { get; }
        public IDefaultCoverProvider DefaultCover { get; }
        public IStreamingService StreamingService { get; }
        public IDependencyLoader DependencyLoader { get; }
        public IPlayQueue PlayQueue { get; }

        public ModuleContext(
            string pluginPath,
            ILogger logger,
            string moduleId,
            ITagRegistry tagRegistry,
            IAlbumRegistry albumRegistry,
            IMusicRegistry musicRegistry,
            IEventBus eventBus,
            IDefaultCoverProvider defaultCover,
            IDependencyLoader dependencyLoader,
            IStreamingService streamingService,
            IPlayQueue playQueue,
            string configDirectory)
        {
            _pluginPath = pluginPath;
            _moduleId = moduleId;

            TagRegistry = tagRegistry;
            AlbumRegistry = albumRegistry;
            MusicRegistry = musicRegistry;
            EventBus = eventBus;
            DefaultCover = defaultCover;
            DependencyLoader = dependencyLoader;
            StreamingService = streamingService;
            PlayQueue = playQueue;
            Logger = logger;
            ConfigManager = new ModuleConfigManager(moduleId, configDirectory);
        }

        public string GetModuleDataPath(string moduleId)
        {
            var path = Path.Combine(_pluginPath, "modules", moduleId, "data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public string GetModuleNativePath(string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var path = Path.Combine(_pluginPath, "modules", moduleId, "native", arch);
            return path;
        }
    }
}
