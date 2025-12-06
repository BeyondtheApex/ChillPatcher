using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;
using UnityEngine;

namespace ChillPatcher.ModuleSystem
{
    /// <summary>
    /// 模块上下文实现
    /// 提供给模块使用的各种服务
    /// </summary>
    public class ModuleContext : IModuleContext
    {
        private readonly string _pluginPath;
        private readonly ConfigFile _config;
        private readonly ManualLogSource _logger;

        public ITagRegistry TagRegistry { get; }
        public IAlbumRegistry AlbumRegistry { get; }
        public IMusicRegistry MusicRegistry { get; }
        public IModuleConfigManager ConfigManager { get; }
        public IEventBus EventBus { get; }
        public ManualLogSource Logger => _logger;
        public IDefaultCoverProvider DefaultCover { get; }
        public IAudioLoader AudioLoader { get; }
        public IDependencyLoader DependencyLoader { get; }

        public ModuleContext(
            string pluginPath,
            ConfigFile config,
            ManualLogSource logger,
            ITagRegistry tagRegistry,
            IAlbumRegistry albumRegistry,
            IMusicRegistry musicRegistry,
            IEventBus eventBus,
            IDefaultCoverProvider defaultCover,
            IAudioLoader audioLoader,
            IDependencyLoader dependencyLoader)
        {
            _pluginPath = pluginPath;
            _config = config;
            _logger = logger;

            TagRegistry = tagRegistry;
            AlbumRegistry = albumRegistry;
            MusicRegistry = musicRegistry;
            EventBus = eventBus;
            DefaultCover = defaultCover;
            AudioLoader = audioLoader;
            DependencyLoader = dependencyLoader;

            ConfigManager = new ModuleConfigManager(config);
        }

        public string GetModuleDataPath(string moduleId)
        {
            var path = Path.Combine(_pluginPath, "Modules", moduleId, "data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public string GetModuleNativePath(string moduleId)
        {
            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var path = Path.Combine(_pluginPath, "Modules", moduleId, "native", arch);
            return path;
        }
    }

    /// <summary>
    /// 模块配置管理器实现
    /// </summary>
    public class ModuleConfigManager : IModuleConfigManager
    {
        private readonly ConfigFile _config;
        private readonly Dictionary<string, object> _overrides = new Dictionary<string, object>();

        public ConfigFile Config => _config;

        public ModuleConfigManager(ConfigFile config)
        {
            _config = config;
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return _config.Bind(section, key, defaultValue, description);
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription description)
        {
            return _config.Bind(section, key, defaultValue, description);
        }

        public bool Override<T>(string section, string key, T value)
        {
            var configKey = $"{section}.{key}";
            _overrides[configKey] = value;

            // 尝试直接设置配置值
            if (_config.TryGetEntry<T>(section, key, out var entry))
            {
                entry.Value = value;
                return true;
            }

            return false;
        }

        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            var configKey = $"{section}.{key}";
            
            // 先检查覆盖值
            if (_overrides.TryGetValue(configKey, out var overrideValue))
            {
                return (T)overrideValue;
            }

            // 从配置文件获取
            if (_config.TryGetEntry<T>(section, key, out var entry))
            {
                return entry.Value;
            }

            return defaultValue;
        }

        public void SetValue<T>(string section, string key, T value)
        {
            if (_config.TryGetEntry<T>(section, key, out var entry))
            {
                entry.Value = value;
            }
        }

        public bool HasKey(string section, string key)
        {
            return _config.TryGetEntry<object>(section, key, out _);
        }

        public void Save()
        {
            _config.Save();
        }
    }
}
