using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// Mod 配置系统 API
    /// 
    /// JS 端用法：
    ///   chill.config.getSections()
    ///   chill.config.getAll("Audio")
    ///   chill.config.get("Audio", "EnableAutoMuteOnOtherAudio")
    ///   chill.config.set("Audio", "EnableAutoMuteOnOtherAudio", true)
    ///   chill.config.save()
    /// </summary>
    public class ChillConfigApi
    {
        private readonly ManualLogSource _logger;

        public ChillConfigApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region 配置读取

        /// <summary>
        /// 获取所有配置分区名称
        /// </summary>
        public string getSections()
        {
            var config = GetConfigFile();
            if (config == null) return "[]";

            var sections = new HashSet<string>();
            foreach (var key in config.Keys)
                sections.Add(key.Section);

            var result = new string[sections.Count];
            sections.CopyTo(result);
            Array.Sort(result);
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 获取指定分区下的所有配置项
        /// </summary>
        public string getAll(string section)
        {
            var config = GetConfigFile();
            if (config == null) return "[]";

            var entries = new List<object>();
            foreach (var key in config.Keys)
            {
                if (key.Section == section)
                {
                    entries.Add(MapConfigEntry(config[key]));
                }
            }
            return JSApiHelper.ToJson(entries);
        }

        /// <summary>
        /// 获取所有配置项（按分区分组）
        /// </summary>
        public string getAllGrouped()
        {
            var config = GetConfigFile();
            if (config == null) return "null";

            var groups = new Dictionary<string, object>();
            var sectionEntries = new Dictionary<string, List<object>>();

            foreach (var key in config.Keys)
            {
                if (!sectionEntries.TryGetValue(key.Section, out var list))
                {
                    list = new List<object>();
                    sectionEntries[key.Section] = list;
                }
                list.Add(MapConfigEntry(config[key]));
            }

            foreach (var kv in sectionEntries)
                groups[kv.Key] = kv.Value;

            return JSApiHelper.ToJson(groups);
        }

        /// <summary>
        /// 获取指定配置项的信息
        /// </summary>
        public string get(string section, string key)
        {
            var entry = FindEntry(section, key);
            if (entry == null) return "null";
            return JSApiHelper.ToJson(MapConfigEntry(entry));
        }

        /// <summary>
        /// 获取指定配置项的当前值
        /// </summary>
        public object getValue(string section, string key)
        {
            var entry = FindEntry(section, key);
            return entry?.BoxedValue;
        }

        /// <summary>
        /// 检查配置项是否存在
        /// </summary>
        public bool has(string section, string key)
        {
            return FindEntry(section, key) != null;
        }

        #endregion

        #region 配置编辑

        /// <summary>
        /// 设置配置项的值
        /// 类型会自动匹配：bool/int/float/string
        /// </summary>
        public bool set(string section, string key, object value)
        {
            var entry = FindEntry(section, key);
            if (entry == null)
            {
                _logger.LogWarning($"[ConfigApi] 配置项不存在: [{section}] {key}");
                return false;
            }

            try
            {
                entry.BoxedValue = ConvertValue(value, entry.SettingType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ConfigApi] 设置配置失败: [{section}] {key} = {value}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重置配置项为默认值
        /// </summary>
        public bool reset(string section, string key)
        {
            var entry = FindEntry(section, key);
            if (entry == null) return false;

            entry.BoxedValue = entry.DefaultValue;
            return true;
        }

        /// <summary>
        /// 重置整个分区为默认值
        /// </summary>
        public int resetSection(string section)
        {
            var config = GetConfigFile();
            if (config == null) return 0;

            int count = 0;
            foreach (var key in config.Keys)
            {
                if (key.Section == section)
                {
                    config[key].BoxedValue = config[key].DefaultValue;
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void save()
        {
            GetConfigFile()?.Save();
        }

        /// <summary>
        /// 重新加载配置文件
        /// </summary>
        public void reload()
        {
            GetConfigFile()?.Reload();
        }

        #endregion

        #region 配置监听

        private readonly Dictionary<string, Action<object>>
            _watchers = new Dictionary<string, Action<object>>();

        /// <summary>
        /// 监听配置项变化
        /// </summary>
        /// <param name="section">分区</param>
        /// <param name="key">键</param>
        /// <param name="callback">变化回调（接收新值）</param>
        /// <returns>是否成功</returns>
        public bool watch(string section, string key, Action<object> callback)
        {
            var entry = FindEntry(section, key);
            if (entry == null || callback == null) return false;

            var watchKey = $"{section}::{key}";

            // 先取消已有监听
            unwatch(section, key);

            EventHandler handler = (sender, args) =>
            {
                try { callback(entry.BoxedValue); }
                catch (Exception ex)
                {
                    _logger.LogError($"[ConfigApi] watch callback error: {ex.Message}");
                }
            };

            entry.GetType()
                .GetEvent("SettingChanged")?
                .AddEventHandler(entry, handler);

            _watchers[watchKey] = callback;
            return true;
        }

        /// <summary>
        /// 取消监听配置项变化
        /// </summary>
        public bool unwatch(string section, string key)
        {
            var watchKey = $"{section}::{key}";
            return _watchers.Remove(watchKey);
        }

        #endregion

        #region 内部方法

        private ConfigFile GetConfigFile()
        {
            var pluginInstance = BepInEx.Bootstrap.Chainloader.PluginInfos.Values
                .FirstOrDefault(p => p.Metadata.GUID == MyPluginInfo.PLUGIN_GUID)?
                .Instance as BepInEx.BaseUnityPlugin;
            return pluginInstance?.Config;
        }

        private ConfigEntryBase FindEntry(string section, string key)
        {
            var config = GetConfigFile();
            if (config == null) return null;

            var def = new ConfigDefinition(section, key);
            return config.Keys.Contains(def) ? config[def] : null;
        }

        private object MapConfigEntry(ConfigEntryBase entry)
        {
            var desc = entry.Description;
            var dict = new Dictionary<string, object>
            {
                ["section"] = entry.Definition.Section,
                ["key"] = entry.Definition.Key,
                ["value"] = entry.BoxedValue,
                ["defaultValue"] = entry.DefaultValue,
                ["type"] = GetFriendlyTypeName(entry.SettingType),
                ["description"] = desc?.Description ?? ""
            };

            // 如果有取值范围
            if (desc?.AcceptableValues != null)
            {
                var av = desc.AcceptableValues;
                dict["acceptableValues"] = new Dictionary<string, object>
                {
                    ["type"] = av.GetType().Name,
                    ["description"] = av.ToDescriptionString()
                };
            }

            return dict;
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(double)) return "double";
            return type.Name;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            // JS 数字类型转换
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            if (targetType == typeof(double))
                return Convert.ToDouble(value);
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            if (targetType == typeof(string))
                return value.ToString();

            return Convert.ChangeType(value, targetType);
        }

        #endregion
    }
}
