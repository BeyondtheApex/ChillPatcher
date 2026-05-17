using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChillPatcher.ModuleSystem;
using ChillPatcher.SDK.Interfaces;
using UnityEngine;

namespace ChillPatcher.Module.LocalIntegration
{
    public sealed class LocalIntegrationRunner : MonoBehaviour
    {
        private LocalIntegrationBus _bus;
        private ManualLogSource _logger;
        private readonly HashSet<Assembly> _registeredAssemblies = new HashSet<Assembly>();

        public void Initialize(ManualLogSource logger, string host, int port, string token)
        {
            _logger = logger;
            _bus = new LocalIntegrationBus(logger, host, port, token);
            RegisterLoadedModuleHandlers(logger);

            var loader = ModuleLoader.Instance;
            if (loader != null)
            {
                loader.OnModuleLoaded += OnModuleLoaded;
            }
        }

        public void StartBus()
        {
            _bus?.Start();
        }

        public void StopBus()
        {
            var loader = ModuleLoader.Instance;
            if (loader != null)
            {
                loader.OnModuleLoaded -= OnModuleLoaded;
            }

            _bus?.Dispose();
            _bus = null;
            _registeredAssemblies.Clear();
        }

        private void Update()
        {
            _bus?.Tick();
        }

        private void OnDestroy()
        {
            StopBus();
        }

        private void OnModuleLoaded(IMusicModule module)
        {
            RegisterHandlersFromAssembly(module?.GetType().Assembly, _logger);
        }

        private void RegisterLoadedModuleHandlers(ManualLogSource logger)
        {
            var loader = ModuleLoader.Instance;
            if (loader == null) return;

            foreach (var loaded in loader.LoadedModules)
            {
                RegisterHandlersFromAssembly(loaded.Assembly, logger);
            }
        }

        private void RegisterHandlersFromAssembly(Assembly assembly, ManualLogSource logger)
        {
            if (_bus == null || assembly == null || _registeredAssemblies.Contains(assembly))
                return;

            _registeredAssemblies.Add(assembly);

            var handlerType = typeof(ILocalIntegrationHandler);
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
                foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
                {
                    logger?.LogWarning($"[本地集成总线] 扫描处理器时部分类型加载失败：{loaderEx.Message}");
                }
            }

            var handlers = types
                .Where(type => !type.IsAbstract && handlerType.IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var type in handlers)
            {
                try
                {
                    if (!(Activator.CreateInstance(type, true) is ILocalIntegrationHandler handler))
                        continue;

                    _bus.Register(handler);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"[本地集成总线] 注册处理器失败 {type.FullName}：{ex.Message}");
                }
            }
        }
    }
}
