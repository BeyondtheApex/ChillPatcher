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

        public ILibraryRegistry Library { get; }
        public IModuleConfigManager ConfigManager { get; }
        public IEventBus EventBus { get; }
        public ILogger Logger { get; }
        public IDefaultCoverProvider DefaultCover { get; }
        public IStreamingService StreamingService { get; }
        public IDependencyLoader DependencyLoader { get; }

        public ModuleContext(
            string pluginPath,
            ILogger logger,
            string moduleId,
            ILibraryRegistry library,
            IEventBus eventBus,
            IDefaultCoverProvider defaultCover,
            IDependencyLoader dependencyLoader,
            IStreamingService streamingService,
            string configDirectory)
        {
            _pluginPath = pluginPath;
            _moduleId = moduleId;

            Library = library;
            EventBus = eventBus;
            DefaultCover = defaultCover;
            DependencyLoader = dependencyLoader;
            StreamingService = streamingService;
            Logger = logger;
            ConfigManager = new ModuleConfigManager(moduleId, configDirectory);
        }

        public string GetModuleDataPath(string moduleId)
        {
            var path = Path.Combine(_pluginPath, "modules", moduleId, "data");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        public string GetModuleNativePath(string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            return Path.Combine(_pluginPath, "modules", moduleId, "native", arch);
        }
    }
}
