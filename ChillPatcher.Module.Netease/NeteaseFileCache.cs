using System.IO;
using System.Linq;
using BepInEx.Logging;

namespace ChillPatcher.Module.Netease
{
    /// <summary>
    /// 音乐文件缓存管理器
    /// 支持人类可读文件名和从旧缓存目录的懒迁移
    /// </summary>
    public class NeteaseFileCache
    {
        private static readonly char[] InvalidFileNameChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        private const string DefaultCacheDirName = "chillpatcher_audio_cache";
        private const string LogTag = "[NeteaseCache]";

        private readonly ManualLogSource _logger;

        /// <summary>
        /// 缓存目录路径，为空时使用默认路径
        /// </summary>
        public string CacheDirectory { get; set; }

        /// <summary>
        /// 是否使用人类可读文件名 (Artist - SongName.ext)
        /// </summary>
        public bool UseReadableName { get; set; }

        public NeteaseFileCache(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取歌曲的缓存文件路径
        /// </summary>
        public string GetCachePath(long songId, string artist, string songName, string format)
        {
            var dir = GetEffectiveCacheDirectory();

            var fileName = UseReadableName
                ? SanitizeFileName(artist.Replace(", ", ",") + " - " + songName) + "." + format
                : "netease_" + songId + "." + format;

            return Path.Combine(dir, fileName);
        }

        /// <summary>
        /// 查找有效的本地缓存文件
        /// 如果在新路径找不到，会尝试从旧缓存目录懒迁移
        /// </summary>
        /// <returns>有效缓存文件的完整路径，未找到返回 null</returns>
        public string FindValidCache(long songId, string artist, string songName, string format, long expectedSize)
        {
            var targetPath = GetCachePath(songId, artist, songName, format);

            // 1. 检查目标路径是否已存在
            if (File.Exists(targetPath))
            {
                if (ValidateFileSize(targetPath, expectedSize))
                {
                    _logger.LogDebug(LogTag + " 缓存命中: " + targetPath);
                    return targetPath;
                }
                return null;
            }

            // 2. 尝试从旧默认目录懒迁移
            var oldPath = GetOldCachePath(songId, format);
            if (oldPath != null && oldPath != targetPath && File.Exists(oldPath))
            {
                return TryMigrate(oldPath, targetPath, expectedSize);
            }

            return null;
        }

        /// <summary>
        /// 获取生效的缓存目录
        /// </summary>
        private string GetEffectiveCacheDirectory()
        {
            if (!string.IsNullOrEmpty(CacheDirectory))
                return CacheDirectory;

            return Path.Combine(Path.GetTempPath(), DefaultCacheDirName);
        }

        /// <summary>
        /// 获取旧缓存路径 (始终为默认目录 + 旧格式文件名)
        /// </summary>
        private string GetOldCachePath(long songId, string format)
        {
            var oldDir = Path.Combine(Path.GetTempPath(), DefaultCacheDirName);
            return Path.Combine(oldDir, "netease_" + songId + "." + format);
        }

        /// <summary>
        /// 尝试从旧路径迁移到新路径
        /// </summary>
        private string TryMigrate(string oldPath, string targetPath, long expectedSize)
        {
            try
            {
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Move(oldPath, targetPath);
                _logger.LogInfo(LogTag + " 缓存迁移: " + oldPath + " -> " + targetPath);

                if (ValidateFileSize(targetPath, expectedSize))
                    return targetPath;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(LogTag + " 缓存迁移失败: " + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// 验证文件大小是否完整
        /// 文件大小应不低于预期大小
        /// </summary>
        private bool ValidateFileSize(string path, long expectedSize)
        {
            if (expectedSize <= 0)
                return true;

            var fileInfo = new FileInfo(path);
            var actualSize = fileInfo.Length;

            if (actualSize >= expectedSize)
                return true;

            _logger.LogWarning(LogTag + " 缓存文件过小，已删除: " + path
                + " (实际: " + actualSize + ", 预期: " + expectedSize + ")");
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(LogTag + " 删除无效缓存失败: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// 替换文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (InvalidFileNameChars.Contains(chars[i]))
                    chars[i] = '_';
            }
            return new string(chars);
        }
    }
}
