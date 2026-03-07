using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChillPatcher.ModuleSystem;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 模块管理 API
    /// 
    /// JS 端用法：
    ///   chill.modules.getAll()
    ///   chill.modules.get("com.chillpatcher.netease")
    ///   chill.modules.disable("com.chillpatcher.netease")
    ///   chill.modules.enable("com.chillpatcher.netease")
    ///   chill.modules.isEnabled("com.chillpatcher.netease")
    /// </summary>
    public class ChillModuleApi
    {
        private readonly ManualLogSource _logger;
        private const string ConfigSection = "Modules";

        public ChillModuleApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有已加载模块的信息
        /// </summary>
        public string getAll()
        {
            var loader = ModuleLoader.Instance;
            if (loader == null) return "[]";

            var modules = loader.LoadedModules;
            var result = new object[modules.Count];
            for (int i = 0; i < modules.Count; i++)
                result[i] = MapModule(modules[i]);
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 获取指定模块的信息
        /// </summary>
        public string get(string moduleId)
        {
            var loaded = FindModule(moduleId);
            return loaded != null ? JSApiHelper.ToJson(MapModule(loaded)) : "null";
        }

        /// <summary>
        /// 获取所有模块 ID
        /// </summary>
        public string getIds()
        {
            var loader = ModuleLoader.Instance;
            if (loader == null) return "[]";

            var modules = loader.LoadedModules;
            var result = new string[modules.Count];
            for (int i = 0; i < modules.Count; i++)
                result[i] = modules[i].Module.ModuleId;
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 获取模块的能力声明
        /// </summary>
        public string getCapabilities(string moduleId)
        {
            var loaded = FindModule(moduleId);
            if (loaded == null) return "null";

            var caps = loaded.Module.Capabilities;
            if (caps == null) return "null";

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["canDelete"] = caps.CanDelete,
                ["canFavorite"] = caps.CanFavorite,
                ["canExclude"] = caps.CanExclude,
                ["supportsLiveUpdate"] = caps.SupportsLiveUpdate,
                ["providesCover"] = caps.ProvidesCover,
                ["providesAlbum"] = caps.ProvidesAlbum
            });
        }

        /// <summary>
        /// 禁用模块（持久化，重启后仍然禁用）
        /// </summary>
        public bool disable(string moduleId)
        {
            var loaded = FindModule(moduleId);
            if (loaded == null)
            {
                _logger.LogWarning($"[ModuleApi] 模块不存在: {moduleId}");
                return false;
            }

            try
            {
                loaded.Module.OnDisable();
                SetModuleEnabledConfig(moduleId, false);
                _logger.LogInfo($"[ModuleApi] 模块已禁用（已持久化）: {moduleId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ModuleApi] 禁用模块失败: {moduleId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用模块（持久化，重启后仍然启用）
        /// </summary>
        public bool enable(string moduleId)
        {
            var loaded = FindModule(moduleId);
            if (loaded == null)
            {
                _logger.LogWarning($"[ModuleApi] 模块不存在: {moduleId}");
                return false;
            }

            try
            {
                loaded.Module.OnEnable();
                SetModuleEnabledConfig(moduleId, true);
                _logger.LogInfo($"[ModuleApi] 模块已启用（已持久化）: {moduleId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ModuleApi] 启用模块失败: {moduleId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查模块是否已启用（包括配置中的状态）
        /// </summary>
        public bool isEnabled(string moduleId)
        {
            return GetModuleEnabledConfig(moduleId);
        }

        /// <summary>
        /// 获取已加载模块数量
        /// </summary>
        public int getCount()
        {
            return ModuleLoader.Instance?.LoadedModules.Count ?? 0;
        }

        #region 内部方法

        private LoadedModule FindModule(string moduleId)
        {
            var loader = ModuleLoader.Instance;
            if (loader == null || string.IsNullOrEmpty(moduleId)) return null;

            foreach (var loaded in loader.LoadedModules)
            {
                if (loaded.Module.ModuleId == moduleId)
                    return loaded;
            }
            return null;
        }

        private object MapModule(LoadedModule loaded)
        {
            var module = loaded.Module;
            var caps = module.Capabilities;

            return new Dictionary<string, object>
            {
                ["moduleId"] = module.ModuleId ?? "",
                ["displayName"] = module.DisplayName ?? "",
                ["version"] = module.Version ?? "",
                ["priority"] = module.Priority,
                ["directory"] = loaded.ModuleDirectory ?? "",
                ["loadedAt"] = loaded.LoadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["enabled"] = GetModuleEnabledConfig(module.ModuleId),
                ["capabilities"] = caps != null ? new Dictionary<string, object>
                {
                    ["canDelete"] = caps.CanDelete,
                    ["canFavorite"] = caps.CanFavorite,
                    ["canExclude"] = caps.CanExclude,
                    ["supportsLiveUpdate"] = caps.SupportsLiveUpdate,
                    ["providesCover"] = caps.ProvidesCover,
                    ["providesAlbum"] = caps.ProvidesAlbum
                } : null
            };
        }

        private ConfigFile GetConfigFile()
        {
            var pluginInstance = BepInEx.Bootstrap.Chainloader.PluginInfos.Values
                .FirstOrDefault(p => p.Metadata.GUID == MyPluginInfo.PLUGIN_GUID)?
                .Instance as BepInEx.BaseUnityPlugin;
            return pluginInstance?.Config;
        }

        private bool GetModuleEnabledConfig(string moduleId)
        {
            var config = GetConfigFile();
            if (config == null) return true;

            var entry = config.Bind(ConfigSection, $"Enable_{moduleId}", true,
                $"是否启用模块 {moduleId}（true=启用, false=禁用）");
            return entry.Value;
        }

        private void SetModuleEnabledConfig(string moduleId, bool enabled)
        {
            var config = GetConfigFile();
            if (config == null) return;

            var entry = config.Bind(ConfigSection, $"Enable_{moduleId}", true,
                $"是否启用模块 {moduleId}（true=启用, false=禁用）");
            entry.Value = enabled;
            config.Save();
        }

        #endregion
    }
}
