using BepInEx.Logging;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ChillPatcher.Module.LocalIntegration
{
    public sealed class LocalIntegrationRunner : MonoBehaviour
    {
        private LocalIntegrationBus _bus;

        public void Initialize(ManualLogSource logger, string host, int port, string token)
        {
            _bus = new LocalIntegrationBus(logger, host, port, token);
            RegisterModuleHandlers(_bus, logger);
        }

        public void StartBus()
        {
            _bus?.Start();
        }

        public void StopBus()
        {
            _bus?.Dispose();
            _bus = null;
        }

        private void Update()
        {
            _bus?.Tick();
        }

        private void OnDestroy()
        {
            StopBus();
        }

        private static void RegisterModuleHandlers(LocalIntegrationBus bus, ManualLogSource logger)
        {
            var handlerType = typeof(ILocalIntegrationHandler);
            var handlers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => !type.IsAbstract && handlerType.IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var type in handlers)
            {
                try
                {
                    if (!(Activator.CreateInstance(type, true) is ILocalIntegrationHandler handler))
                        continue;

                    bus.Register(handler);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"[本地集成总线] 注册处理器失败 {type.FullName}：{ex.Message}");
                }
            }
        }
    }
}
