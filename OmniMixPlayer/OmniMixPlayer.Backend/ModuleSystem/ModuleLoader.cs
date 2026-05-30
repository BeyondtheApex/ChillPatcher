using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem
{
    public class ModuleContextFactory
    {
        private readonly string _pluginPath;
        private readonly string _configDirectory;
        private readonly ILogger _logger;
        private readonly ITagRegistry _tagRegistry;
        private readonly IAlbumRegistry _albumRegistry;
        private readonly IMusicRegistry _musicRegistry;
        private readonly IEventBus _eventBus;
        private readonly IDefaultCoverProvider _defaultCover;
        private readonly IDependencyLoader _dependencyLoader;
        private readonly IStreamingService _streamingService;
        private readonly IPlayQueue _playQueue;

        public ModuleContextFactory(
            string pluginPath,
            string configDirectory,
            ILogger logger,
            ITagRegistry tagRegistry,
            IAlbumRegistry albumRegistry,
            IMusicRegistry musicRegistry,
            IEventBus eventBus,
            IDefaultCoverProvider defaultCover,
            IDependencyLoader dependencyLoader,
            IStreamingService streamingService,
            IPlayQueue playQueue)
        {
            _pluginPath = pluginPath;
            _configDirectory = configDirectory;
            _logger = logger;
            _tagRegistry = tagRegistry;
            _albumRegistry = albumRegistry;
            _musicRegistry = musicRegistry;
            _eventBus = eventBus;
            _defaultCover = defaultCover;
            _dependencyLoader = dependencyLoader;
            _streamingService = streamingService;
            _playQueue = playQueue;
        }

        public IModuleContext CreateContext(string moduleId)
        {
            return new ModuleContext(
                _pluginPath,
                _logger,
                moduleId,
                _tagRegistry,
                _albumRegistry,
                _musicRegistry,
                _eventBus,
                _defaultCover,
                _dependencyLoader,
                _streamingService,
                _playQueue,
                _configDirectory);
        }
    }

    public class ModuleLoader : IDisposable
    {
        private static ModuleLoader _instance;
        public static ModuleLoader Instance => _instance;

        private readonly string _modulesPath;
        private readonly ILogger _logger;
        private readonly List<LoadedModule> _loadedModules = new List<LoadedModule>();
        private readonly ModuleContextFactory _contextFactory;
        private readonly IModuleConfigManager _moduleConfigManager;

        public IReadOnlyList<LoadedModule> LoadedModules => _loadedModules;
        public event Action<IMusicModule> OnModuleLoaded;
        public event Action OnAllModulesLoaded;

        public static void Initialize(string modulesPath, ModuleContextFactory contextFactory, ILogger logger, IModuleConfigManager moduleConfigManager)
        {
            if (_instance != null)
            {
                logger.LogWarning("ModuleLoader already initialized");
                return;
            }

            _instance = new ModuleLoader(modulesPath, contextFactory, logger, moduleConfigManager);
        }

        private ModuleLoader(string modulesPath, ModuleContextFactory contextFactory, ILogger logger, IModuleConfigManager moduleConfigManager)
        {
            _modulesPath = modulesPath;
            _contextFactory = contextFactory;
            _logger = logger;
            _moduleConfigManager = moduleConfigManager;

            if (!Directory.Exists(_modulesPath))
            {
                Directory.CreateDirectory(_modulesPath);
                _logger.LogInformation("Created modules directory: {Path}", _modulesPath);
            }
        }

        public async Task LoadAllModulesAsync()
        {
            _logger.LogInformation("Scanning modules directory: {Path}", _modulesPath);

            var moduleDirectories = Directory.GetDirectories(_modulesPath);
            var discoveredModules = new List<(IMusicModule module, Assembly assembly)>();

            foreach (var moduleDir in moduleDirectories)
            {
                try
                {
                    var modules = DiscoverModulesInDirectory(moduleDir);
                    discoveredModules.AddRange(modules);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scan module directory {Path}", moduleDir);
                }
            }

            discoveredModules.Sort((a, b) => a.module.Priority.CompareTo(b.module.Priority));
            _logger.LogInformation("Found {Count} modules, loading...", discoveredModules.Count);

            foreach (var (module, assembly) in discoveredModules)
            {
                try
                {
                    await InitializeModuleAsync(module, assembly);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize module {ModuleId}", module.ModuleId);
                }
            }

            _logger.LogInformation("Module loading complete. {Count} modules loaded.", _loadedModules.Count);
            OnAllModulesLoaded?.Invoke();
        }

        private List<(IMusicModule module, Assembly assembly)> DiscoverModulesInDirectory(string directory)
        {
            var result = new List<(IMusicModule, Assembly)>();
            var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    if (Path.GetFileName(dllPath).StartsWith("OmniMixPlayer.SDK"))
                        continue;
                    if (Path.GetFileName(dllPath).StartsWith("ChillPatcher.SDK"))
                        continue;

                    var assembly = Assembly.LoadFrom(dllPath);
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => typeof(IMusicModule).IsAssignableFrom(t)
                                   && !t.IsInterface
                                   && !t.IsAbstract);

                    foreach (var moduleType in moduleTypes)
                    {
                        try
                        {
                            var module = (IMusicModule)Activator.CreateInstance(moduleType);
                            result.Add((module, assembly));
                            _logger.LogInformation("Discovered module: {Name} ({Id}) v{Version}", module.DisplayName, module.ModuleId, module.Version);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to instantiate module type {Type}", moduleType.FullName);
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    _logger.LogWarning("Failed to load assembly {Path}, some types failed", dllPath);
                    foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
                    {
                        _logger.LogWarning("  - {Message}", loaderEx.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to load assembly {Path}: {Message}", dllPath, ex.Message);
                }
            }

            return result;
        }

        private async Task InitializeModuleAsync(IMusicModule module, Assembly assembly)
        {
            _logger.LogInformation("Initializing module: {Name} ({Id})", module.DisplayName, module.ModuleId);

            if (!IsModuleEnabled(module.ModuleId))
            {
                _logger.LogInformation("Module {Name} is disabled in config, skipping", module.DisplayName);
                var skippedModule = new LoadedModule
                {
                    Module = module,
                    Assembly = assembly,
                    ModuleDirectory = Path.GetDirectoryName(assembly.Location),
                    LoadedAt = DateTime.Now,
                    Context = null
                };
                _loadedModules.Add(skippedModule);
                return;
            }

            var moduleContext = _contextFactory.CreateContext(module.ModuleId);
            await module.InitializeAsync(moduleContext);
            module.OnEnable();

            var loadedModule = new LoadedModule
            {
                Module = module,
                Assembly = assembly,
                ModuleDirectory = Path.GetDirectoryName(assembly.Location),
                LoadedAt = DateTime.Now,
                Context = moduleContext
            };

            _loadedModules.Add(loadedModule);
            OnModuleLoaded?.Invoke(module);
            _logger.LogInformation("Module {Name} loaded successfully", module.DisplayName);
        }

        public IMusicModule GetModule(string moduleId)
        {
            return _loadedModules.FirstOrDefault(m => m.Module.ModuleId == moduleId)?.Module;
        }

        public T GetProvider<T>(string moduleId) where T : class
        {
            var module = GetModule(moduleId);
            return module as T;
        }

        public IEnumerable<T> GetAllProviders<T>() where T : class
        {
            return _loadedModules
                .Select(m => m.Module as T)
                .Where(p => p != null);
        }

        public void UnloadAllModules()
        {
            _logger.LogInformation("Unloading all modules...");

            foreach (var loadedModule in _loadedModules.ToList())
            {
                try
                {
                    loadedModule.Module.OnDisable();
                    loadedModule.Module.OnUnload();
                    _logger.LogInformation("Module {Name} unloaded", loadedModule.Module.DisplayName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to unload module {ModuleId}", loadedModule.Module.ModuleId);
                }
            }

            _loadedModules.Clear();
        }

        public bool IsModuleEnabled(string moduleId)
        {
            if (_moduleConfigManager == null) return true;
            return _moduleConfigManager.GetBool($"Enable_{moduleId}", true);
        }

        public void SetModuleEnabled(string moduleId, bool enabled)
        {
            if (_moduleConfigManager == null) return;
            _moduleConfigManager.SetValue($"Enable_{moduleId}", enabled);
            _moduleConfigManager.Save();
        }

        public void Dispose()
        {
            UnloadAllModules();
            _instance = null;
        }
    }

    public class LoadedModule
    {
        public IMusicModule Module { get; set; }
        public Assembly Assembly { get; set; }
        public string ModuleDirectory { get; set; }
        public DateTime LoadedAt { get; set; }
        public IModuleContext Context { get; set; }
    }
}
