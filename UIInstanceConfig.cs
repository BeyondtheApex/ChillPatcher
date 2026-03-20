using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ChillPatcher
{
    /// <summary>
    /// 单个 UI 实例的配置数据。
    /// </summary>
    public class UIInstanceEntry
    {
        /// <summary>唯一标识符</summary>
        public string Id { get; set; }

        /// <summary>UI 工作目录（相对于插件目录或绝对路径）</summary>
        public string WorkingDir { get; set; }

        /// <summary>入口脚本文件（相对于 WorkingDir）</summary>
        public string EntryFile { get; set; } = "@outputs/esbuild/app.js";

        /// <summary>层叠排序值，越大越靠前</summary>
        public int SortingOrder { get; set; } = 1000;

        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>是否允许交互（鼠标事件穿透到此层）</summary>
        public bool Interactive { get; set; } = false;

        // BepInEx config entries (backing store)
        internal ConfigEntry<bool> CfgEnabled;
        internal ConfigEntry<int> CfgSortingOrder;
        internal ConfigEntry<bool> CfgInteractive;
        internal ConfigEntry<string> CfgEntryFile;
        internal ConfigEntry<string> CfgWorkingDir;
    }

    /// <summary>
    /// UI 实例配置的持久化容器。
    /// </summary>
    public class UIInstanceConfigData
    {
        public List<UIInstanceEntry> Instances { get; set; } = new List<UIInstanceEntry>();
    }

    /// <summary>
    /// 管理 UI 实例配置的加载与保存。使用 BepInEx ConfigFile 持久化。
    /// </summary>
    public static class UIInstanceConfig
    {
        private static ConfigFile _config;
        private static ManualLogSource _log;
        private static UIInstanceConfigData _data;

        // UI 实例配置版本号 — 当默认值发生变化时递增，旧配置会被重置
        private const int CurrentVersion = 3;

        // 需要重置默认值的实例 ID 集合
        private static readonly HashSet<string> _resetSections = new HashSet<string>();

        /// <summary>
        /// 检查指定实例的 App 配置是否需要重置为默认值。
        /// 由 JS 端 appGetOrCreate 调用，重置后自动清除标记。
        /// </summary>
        public static bool ShouldResetAppConfig(string instanceId)
        {
            return _resetSections.Contains(instanceId);
        }

        /// <summary>
        /// 清除指定实例的重置标记（在所有 App 配置绑定完成后调用）。
        /// </summary>
        public static void ClearResetFlag(string instanceId)
        {
            _resetSections.Remove(instanceId);
        }

        /// <summary>当前已加载的配置数据</summary>
        public static UIInstanceConfigData Data => _data;

        /// <summary>
        /// 初始化配置系统。
        /// 扫描 uiBaseDir 下的子目录自动发现实例，并绑定到 BepInEx ConfigFile。
        /// </summary>
        public static void Initialize(ConfigFile config, string uiBaseDir, ManualLogSource log)
        {
            _log = log;
            _config = config;
            _data = new UIInstanceConfigData();
            _resetSections.Clear();

            // 检查版本，标记需要重置的实例
            CheckAndResetInstanceVersion(config, "default");
            CheckAndResetInstanceVersion(config, "window-manager");

            // 扫描 uiBaseDir 下的子目录
            if (Directory.Exists(uiBaseDir))
            {
                int nextOrder = 100;
                foreach (var dir in Directory.GetDirectories(uiBaseDir))
                {
                    var dirName = Path.GetFileName(dir);

                    // 跳过没有入口文件或 package.json 的目录
                    var hasEntry = File.Exists(Path.Combine(dir, "@outputs", "esbuild", "app.js"));
                    var hasPkg = File.Exists(Path.Combine(dir, "package.json"));
                    if (!hasEntry && !hasPkg) continue;

                    var isDefault = dirName == "default";
                    var entry = BindInstance(dirName, dir,
                        defaultSortingOrder: isDefault ? 1000 : nextOrder,
                        defaultEnabled: true,
                        defaultInteractive: true);
                    _data.Instances.Add(entry);

                    if (!isDefault) nextOrder += 100;
                    log.LogInfo($"[UIInstanceConfig] Instance: {dirName} (sortOrder={entry.SortingOrder}, enabled={entry.Enabled}, interactive={entry.Interactive})");
                }
            }

            // 确保至少有 default 实例
            if (!_data.Instances.Exists(i => i.Id == "default"))
            {
                var defaultDir = Path.Combine(uiBaseDir, "default");
                var entry = BindInstance("default", defaultDir, 1000, true, true);
                _data.Instances.Insert(0, entry);
            }

            _config.Save();
        }

        /// <summary>
        /// 检查指定 UI 实例的配置版本，若过期则标记为需要重置。
        /// </summary>
        private static void CheckAndResetInstanceVersion(ConfigFile config, string instanceId)
        {
            // 绑定版本号（默认值 1 表示未初始化/旧配置）
            var versionEntry = config.Bind("_Version", $"UIInstance.{instanceId}",
                1, "UI实例配置版本号（请勿手动修改）");

            if (versionEntry.Value < CurrentVersion)
            {
                _resetSections.Add(instanceId);
                _log.LogInfo($"[UIInstanceConfig] 配置版本升级: UIInstance.{instanceId} (v{versionEntry.Value} → v{CurrentVersion})");
                versionEntry.Value = CurrentVersion;
            }
        }

        private static UIInstanceEntry BindInstance(string id, string workingDir,
            int defaultSortingOrder, bool defaultEnabled, bool defaultInteractive)
        {
            var section = $"UIInstance.{id}";
            var entry = new UIInstanceEntry { Id = id, WorkingDir = workingDir };

            entry.CfgWorkingDir = _config.Bind(section, "WorkingDir", workingDir,
                "UI 工作目录（自动检测，一般无需修改）");
            entry.CfgEnabled = _config.Bind(section, "Enabled", defaultEnabled,
                "是否启用此 UI 实例");
            entry.CfgSortingOrder = _config.Bind(section, "SortingOrder", defaultSortingOrder,
                "层叠排序值（越大越靠前，游戏 UI 约为 0）");
            entry.CfgInteractive = _config.Bind(section, "Interactive", defaultInteractive,
                "是否允许交互（true = 接收鼠标事件并可遮挡下层 UI）");
            entry.CfgEntryFile = _config.Bind(section, "EntryFile", "@outputs/esbuild/app.js",
                "入口脚本文件（相对于上方 WorkingDir 目录）");

            // 版本重置：Bind 会消费 orphaned entries 中的旧值，此处强制覆盖为默认值
            if (_resetSections.Contains(id))
            {
                entry.CfgWorkingDir.Value = workingDir;
                entry.CfgEnabled.Value = defaultEnabled;
                entry.CfgSortingOrder.Value = defaultSortingOrder;
                entry.CfgInteractive.Value = defaultInteractive;
                entry.CfgEntryFile.Value = "@outputs/esbuild/app.js";
                _log.LogInfo($"[UIInstanceConfig] 已覆盖默认值: UIInstance.{id} (sortOrder={defaultSortingOrder}, interactive={defaultInteractive})");
            }

            // 从 config 读取实际值
            entry.WorkingDir = entry.CfgWorkingDir.Value;
            entry.Enabled = entry.CfgEnabled.Value;
            entry.SortingOrder = entry.CfgSortingOrder.Value;
            entry.Interactive = entry.CfgInteractive.Value;
            entry.EntryFile = entry.CfgEntryFile.Value;

            return entry;
        }

        /// <summary>
        /// 保存当前配置到磁盘。
        /// </summary>
        public static void Save()
        {
            _config?.Save();
        }

        /// <summary>
        /// 添加一个实例配置。
        /// </summary>
        public static bool AddInstance(UIInstanceEntry entry)
        {
            if (_data == null || entry == null || string.IsNullOrEmpty(entry.Id)) return false;
            if (_data.Instances.Exists(i => i.Id == entry.Id)) return false;

            // 绑定到 BepInEx config
            var bound = BindInstance(entry.Id, entry.WorkingDir,
                entry.SortingOrder, entry.Enabled, entry.Interactive);
            bound.CfgEntryFile.Value = entry.EntryFile;
            bound.EntryFile = entry.EntryFile;
            _data.Instances.Add(bound);
            _config?.Save();
            return true;
        }

        /// <summary>
        /// 移除一个实例配置。
        /// </summary>
        public static bool RemoveInstance(string id)
        {
            if (_data == null || string.IsNullOrEmpty(id)) return false;
            var removed = _data.Instances.RemoveAll(i => i.Id == id) > 0;
            if (removed)
            {
                // 从 config 文件中移除该 section
                var section = $"UIInstance.{id}";
                _config?.Remove(new ConfigDefinition(section, "WorkingDir"));
                _config?.Remove(new ConfigDefinition(section, "Enabled"));
                _config?.Remove(new ConfigDefinition(section, "SortingOrder"));
                _config?.Remove(new ConfigDefinition(section, "Interactive"));
                _config?.Remove(new ConfigDefinition(section, "EntryFile"));
                _config?.Save();
            }
            return removed;
        }

        /// <summary>
        /// 获取实例配置。
        /// </summary>
        public static UIInstanceEntry GetInstance(string id)
        {
            return _data?.Instances?.Find(i => i.Id == id);
        }

        /// <summary>
        /// 更新实例的启用状态并保存。
        /// </summary>
        public static void SetEnabled(string id, bool enabled)
        {
            var entry = GetInstance(id);
            if (entry != null)
            {
                entry.Enabled = enabled;
                if (entry.CfgEnabled != null) entry.CfgEnabled.Value = enabled;
                _config?.Save();
            }
        }

        /// <summary>
        /// 更新实例的排序值并保存。
        /// </summary>
        public static void SetSortingOrder(string id, int order)
        {
            var entry = GetInstance(id);
            if (entry != null)
            {
                entry.SortingOrder = order;
                if (entry.CfgSortingOrder != null) entry.CfgSortingOrder.Value = order;
                _config?.Save();
            }
        }

        /// <summary>
        /// 更新实例的交互模式并保存。
        /// </summary>
        public static void SetInteractive(string id, bool interactive)
        {
            var entry = GetInstance(id);
            if (entry != null)
            {
                entry.Interactive = interactive;
                if (entry.CfgInteractive != null) entry.CfgInteractive.Value = interactive;
                _config?.Save();
            }
        }
    }
}
