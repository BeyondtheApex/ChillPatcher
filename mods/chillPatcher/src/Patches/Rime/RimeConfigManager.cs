using System;
using System.IO;
using System.Reflection;
using BepInEx;

namespace ChillPatcher.Rime
{
    /// <summary>
    /// Rime配置管理器 - 智能查找配置目录
    /// </summary>
    public static class RimeConfigManager
    {
        private static string _sharedDataDir;
        private static string _userDataDir;

        /// <summary>
        /// 获取Rime共享数据目录 (Schema文件的只读目录)
        /// 构建时复制所有 .schema.yaml, .dict.yaml, .yaml 文件
        /// </summary>
        public static string GetSharedDataDirectory()
        {
            if (_sharedDataDir != null)
                return _sharedDataDir;

            // 使用自定义路径或插件目录
            if (!string.IsNullOrEmpty(PluginConfig.RimeSharedDataPath.Value))
            {
                _sharedDataDir = PluginConfig.RimeSharedDataPath.Value;
                Plugin.Logger.LogInfo($"[Rime] SharedData: {_sharedDataDir}");
            }
            else
            {
                _sharedDataDir = Path.Combine(GetBepInExRimeDataPath(), "shared");
                Plugin.Logger.LogInfo($"[Rime] SharedData (默认): {_sharedDataDir}");
            }

            return _sharedDataDir;
        }

        /// <summary>
        /// 获取Rime用户数据目录 (可读写,存放编译结果和用户配置)
        /// 这里会生成 build/ 目录, *.userdb/, installation.yaml 等
        /// </summary>
        public static string GetUserDataDirectory()
        {
            if (_userDataDir != null)
                return _userDataDir;

            // 使用自定义路径或插件子目录
            if (!string.IsNullOrEmpty(PluginConfig.RimeUserDataPath.Value))
            {
                _userDataDir = PluginConfig.RimeUserDataPath.Value;
                Plugin.Logger.LogInfo($"[Rime] UserData: {_userDataDir}");
            }
            else
            {
                // 使用独立的用户数据目录
                _userDataDir = Path.Combine(GetBepInExRimeDataPath(), "user");
                Plugin.Logger.LogInfo($"[Rime] UserData (默认): {_userDataDir}");
            }

            return _userDataDir;
        }

        /// <summary>
        /// 获取BepInEx插件目录下的Rime数据路径
        /// </summary>
        private static string GetBepInExRimeDataPath()
        {
            // 获取ChillPatcher.dll所在目录
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string pluginDir = Path.GetDirectoryName(assemblyPath);
            
            // 构建路径: BepInEx/plugins/ChillPatcher/rime-data
            return Path.Combine(pluginDir, "rime-data");
        }

        /// <summary>
        /// 获取系统AppData Rime目录
        /// </summary>
        private static string GetAppDataRimePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Rime");
        }

        /// <summary>
        /// 检查目录是否有有效的Schema文件
        /// </summary>
        private static bool HasValidSchemaFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return false;

            // 检查是否有.schema.yaml文件
            var schemaFiles = Directory.GetFiles(directory, "*.schema.yaml", SearchOption.TopDirectoryOnly);
            if (schemaFiles.Length > 0)
                return true;

            // 检查是否有default.yaml
            string defaultYaml = Path.Combine(directory, "default.yaml");
            if (File.Exists(defaultYaml))
                return true;

            return false;
        }

        /// <summary>
        /// 获取配置目录信息(用于日志和调试)
        /// </summary>
        public static string GetConfigInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Rime配置目录 ===");
            info.AppendLine($"SharedData: {GetSharedDataDirectory()}");
            info.AppendLine($"UserData: {GetUserDataDirectory()}");
            
            // 检查文件
            string sharedDir = GetSharedDataDirectory();
            if (Directory.Exists(sharedDir))
            {
                var schemas = Directory.GetFiles(sharedDir, "*.schema.yaml");
                info.AppendLine($"Schema文件数: {schemas.Length}");
                if (schemas.Length > 0)
                {
                    info.AppendLine("Schema列表:");
                    foreach (var schema in schemas)
                    {
                        info.AppendLine($"  - {Path.GetFileName(schema)}");
                    }
                }
            }
            else
            {
                info.AppendLine("⚠ 共享数据目录不存在");
            }

            return info.ToString();
        }

        /// <summary>
        /// 复制示例配置(如果需要)
        /// </summary>
        public static void CopyExampleConfig()
        {
            string sharedDir = GetSharedDataDirectory();
            
            // 检查是否已有配置
            if (HasValidSchemaFiles(sharedDir))
            {
                Plugin.Logger.LogInfo("[Rime] 已存在配置文件,跳过示例复制");
                return;
            }

            Plugin.Logger.LogWarning("[Rime] 未找到配置文件");
            Plugin.Logger.LogWarning("[Rime] 请手动复制Rime配置到: " + sharedDir);
            Plugin.Logger.LogWarning("[Rime] 或安装小狼毫后,从以下位置复制:");
            Plugin.Logger.LogWarning($"[Rime]   {GetAppDataRimePath()}");
        }

        /// <summary>
        /// 重置缓存(用于重新加载配置)
        /// </summary>
        public static void ResetCache()
        {
            _sharedDataDir = null;
            _userDataDir = null;
        }
    }
}
