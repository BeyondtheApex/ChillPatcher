using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Logging;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 文件 IO API（限制在插件目录内）
    /// 
    /// JS 端用法：
    ///   chill.io.readText("licenses/MIT.txt")
    ///   chill.io.listFiles("licenses")
    ///   chill.io.exists("licenses/MIT.txt")
    ///   chill.io.writeText("data/cache.json", jsonStr)
    /// </summary>
    public class ChillIOApi
    {
        private readonly ManualLogSource _logger;
        private readonly string _basePath;

        public ChillIOApi(ManualLogSource logger)
        {
            _logger = logger;
            _basePath = Plugin.PluginPath;
        }

        /// <summary>
        /// 插件根目录路径
        /// </summary>
        public string basePath => _basePath;

        /// <summary>
        /// 读取文本文件内容（相对于插件根目录）
        /// </summary>
        public string readText(string relativePath)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null || !File.Exists(fullPath)) return null;
            return File.ReadAllText(fullPath, Encoding.UTF8);
        }

        /// <summary>
        /// 写入文本文件（相对于插件根目录）
        /// </summary>
        public bool writeText(string relativePath, string content)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null) return false;

            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content, Encoding.UTF8);
            return true;
        }

        /// <summary>
        /// 追加文本到文件
        /// </summary>
        public bool appendText(string relativePath, string content)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null) return false;

            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.AppendAllText(fullPath, content, Encoding.UTF8);
            return true;
        }

        /// <summary>
        /// 检查文件或目录是否存在
        /// </summary>
        public bool exists(string relativePath)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null) return false;
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        /// <summary>
        /// 列出目录下的文件
        /// </summary>
        public string listFiles(string relativePath)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null || !Directory.Exists(fullPath))
                return "[]";

            var files = Directory.GetFiles(fullPath);
            var result = new object[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                var fi = new FileInfo(files[i]);
                result[i] = new Dictionary<string, object>
                {
                    ["name"] = fi.Name,
                    ["nameWithoutExt"] = Path.GetFileNameWithoutExtension(fi.Name),
                    ["extension"] = fi.Extension,
                    ["size"] = fi.Length
                };
            }
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 列出子目录
        /// </summary>
        public string listDirs(string relativePath)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null || !Directory.Exists(fullPath))
                return "[]";

            var dirs = Directory.GetDirectories(fullPath);
            var result = new string[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
                result[i] = Path.GetFileName(dirs[i]);
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public bool deleteFile(string relativePath)
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null || !File.Exists(fullPath)) return false;

            File.Delete(fullPath);
            return true;
        }

        /// <summary>
        /// 安全解析路径，防止路径遍历攻击
        /// 只允许访问插件根目录内的文件
        /// </summary>
        private string ResolveSafePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
                // 确保解析后的路径仍在 basePath 内
                if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"[IOApi] 路径越界被阻止: {relativePath}");
                    return null;
                }
                return fullPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
