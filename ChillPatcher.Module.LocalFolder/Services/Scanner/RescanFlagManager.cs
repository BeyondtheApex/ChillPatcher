using System;
using System.IO;
using BepInEx.Logging;

namespace ChillPatcher.Module.LocalFolder.Services.Scanner
{
    /// <summary>
    /// 重扫描标记管理
    /// </summary>
    public class RescanFlagManager
    {
        private const string RESCAN_FLAG = "!rescan_playlist";
        private readonly ManualLogSource _logger;

        public RescanFlagManager(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 检查是否需要重新扫描（标志文件不存在）
        /// </summary>
        public bool NeedsRescan(string playlistDir)
        {
            var flagPath = Path.Combine(playlistDir, RESCAN_FLAG);
            return !File.Exists(flagPath);
        }

        /// <summary>
        /// 创建 rescan 标志文件
        /// </summary>
        public void CreateRescanFlag(string playlistDir)
        {
            try
            {
                var flagPath = Path.Combine(playlistDir, RESCAN_FLAG);
                var content = $@"# ChillPatcher Playlist Scan Flag
# 此文件标识该歌单已完成扫描
# 删除此文件后，下次启动将重新扫描

Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
                File.WriteAllText(flagPath, content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"无法创建 rescan 标志文件: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除 rescan 标志文件（强制重扫描）
        /// </summary>
        public void DeleteRescanFlag(string playlistDir)
        {
            try
            {
                var flagPath = Path.Combine(playlistDir, RESCAN_FLAG);
                if (File.Exists(flagPath))
                {
                    File.Delete(flagPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"无法删除 rescan 标志文件: {ex.Message}");
            }
        }
    }
}
