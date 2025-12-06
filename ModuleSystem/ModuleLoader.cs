using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.ModuleSystem
{
    /// <summary>
    /// 模块加载器
    /// 负责扫描、加载和管理音乐模块
    /// </summary>
    public class ModuleLoader : IDisposable
    {
        private static ModuleLoader _instance;
        public static ModuleLoader Instance => _instance;

        private readonly string _modulesPath;
        private readonly ManualLogSource _logger;
        private readonly List<LoadedModule> _loadedModules = new List<LoadedModule>();
        private readonly IModuleContext _context;

        /// <summary>
        /// 已加载的模块列表
        /// </summary>
        public IReadOnlyList<LoadedModule> LoadedModules => _loadedModules;

        /// <summary>
        /// 模块加载完成事件
        /// </summary>
        public event Action<IMusicModule> OnModuleLoaded;

        /// <summary>
        /// 所有模块加载完成事件
        /// </summary>
        public event Action OnAllModulesLoaded;

        /// <summary>
        /// 初始化模块加载器
        /// </summary>
        public static void Initialize(string modulesPath, IModuleContext context, ManualLogSource logger)
        {
            if (_instance != null)
            {
                logger.LogWarning("ModuleLoader 已经初始化");
                return;
            }

            _instance = new ModuleLoader(modulesPath, context, logger);
        }

        private ModuleLoader(string modulesPath, IModuleContext context, ManualLogSource logger)
        {
            _modulesPath = modulesPath;
            _context = context;
            _logger = logger;

            // 确保模块目录存在
            if (!Directory.Exists(_modulesPath))
            {
                Directory.CreateDirectory(_modulesPath);
                _logger.LogInfo($"创建模块目录: {_modulesPath}");
            }
        }

        /// <summary>
        /// 扫描并加载所有模块
        /// </summary>
        public async Task LoadAllModulesAsync()
        {
            _logger.LogInfo($"开始扫描模块目录: {_modulesPath}");

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
                    _logger.LogError($"扫描模块目录失败 '{moduleDir}': {ex.Message}");
                }
            }

            // 按优先级排序
            discoveredModules.Sort((a, b) => a.module.Priority.CompareTo(b.module.Priority));

            _logger.LogInfo($"发现 {discoveredModules.Count} 个模块，开始加载...");

            // 依次初始化模块
            foreach (var (module, assembly) in discoveredModules)
            {
                try
                {
                    await InitializeModuleAsync(module, assembly);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"初始化模块 '{module.ModuleId}' 失败: {ex}");
                }
            }

            _logger.LogInfo($"模块加载完成，共加载 {_loadedModules.Count} 个模块");
            OnAllModulesLoaded?.Invoke();
        }

        /// <summary>
        /// 在目录中发现模块
        /// </summary>
        private List<(IMusicModule module, Assembly assembly)> DiscoverModulesInDirectory(string directory)
        {
            var result = new List<(IMusicModule, Assembly)>();
            var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    // 跳过 SDK DLL
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
                            _logger.LogInfo($"发现模块: {module.DisplayName} ({module.ModuleId}) v{module.Version}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"实例化模块类型 '{moduleType.FullName}' 失败: {ex.Message}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    _logger.LogWarning($"加载程序集 '{dllPath}' 时部分类型加载失败");
                    foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
                    {
                        _logger.LogWarning($"  - {loaderEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"加载程序集 '{dllPath}' 失败: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 初始化单个模块
        /// </summary>
        private async Task InitializeModuleAsync(IMusicModule module, Assembly assembly)
        {
            _logger.LogInfo($"初始化模块: {module.DisplayName} ({module.ModuleId})");

            // 调用模块初始化
            await module.InitializeAsync(_context);

            // 启用模块
            module.OnEnable();

            // 记录已加载的模块
            var loadedModule = new LoadedModule
            {
                Module = module,
                Assembly = assembly,
                ModuleDirectory = Path.GetDirectoryName(assembly.Location),
                LoadedAt = DateTime.Now
            };

            _loadedModules.Add(loadedModule);

            OnModuleLoaded?.Invoke(module);
            _logger.LogInfo($"模块 '{module.DisplayName}' 加载成功");
        }

        /// <summary>
        /// 获取模块
        /// </summary>
        public IMusicModule GetModule(string moduleId)
        {
            return _loadedModules.FirstOrDefault(m => m.Module.ModuleId == moduleId)?.Module;
        }

        /// <summary>
        /// 获取模块的提供器
        /// </summary>
        public T GetProvider<T>(string moduleId) where T : class
        {
            var module = GetModule(moduleId);
            return module as T;
        }

        /// <summary>
        /// 获取所有实现指定接口的模块
        /// </summary>
        public IEnumerable<T> GetAllProviders<T>() where T : class
        {
            return _loadedModules
                .Select(m => m.Module as T)
                .Where(p => p != null);
        }

        /// <summary>
        /// 卸载所有模块
        /// </summary>
        public void UnloadAllModules()
        {
            _logger.LogInfo("开始卸载所有模块...");

            foreach (var loadedModule in _loadedModules.ToList())
            {
                try
                {
                    loadedModule.Module.OnDisable();
                    loadedModule.Module.OnUnload();
                    _logger.LogInfo($"模块 '{loadedModule.Module.DisplayName}' 已卸载");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"卸载模块 '{loadedModule.Module.ModuleId}' 失败: {ex.Message}");
                }
            }

            _loadedModules.Clear();
        }

        public void Dispose()
        {
            UnloadAllModules();
            _instance = null;
        }
    }

    /// <summary>
    /// 已加载的模块信息
    /// </summary>
    public class LoadedModule
    {
        public IMusicModule Module { get; set; }
        public Assembly Assembly { get; set; }
        public string ModuleDirectory { get; set; }
        public DateTime LoadedAt { get; set; }
    }
}
