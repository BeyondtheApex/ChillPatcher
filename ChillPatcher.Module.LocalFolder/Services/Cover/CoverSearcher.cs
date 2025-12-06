using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;

namespace ChillPatcher.Module.LocalFolder.Services.Cover
{
    /// <summary>
    /// 封面来源类型
    /// </summary>
    public enum CoverSourceType
    {
        None = 0,
        ImageFile = 1,      // 图片文件
        AudioEmbedded = 2   // 音频内嵌封面
    }

    /// <summary>
    /// 封面搜索器 - 按优先级搜索封面
    /// </summary>
    public class CoverSearcher
    {
        // 优先级前缀（按顺序搜索）
        private static readonly string[] COVER_PREFIXES = { "cover", "folder", "front", "album", "thumb" };

        // 支持的图片扩展名
        private static readonly string[] IMAGE_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

        // 音频扩展名
        private static readonly string[] AUDIO_EXTENSIONS = { ".mp3", ".flac", ".ogg", ".m4a", ".wav" };

        /// <summary>
        /// 从目录搜索封面（仅图片文件）
        /// </summary>
        public (string path, CoverSourceType type) SearchFromDirectoryOnly(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return (null, CoverSourceType.None);

            try
            {
                var allImages = GetImageFiles(directoryPath);
                if (allImages.Count == 0)
                    return (null, CoverSourceType.None);

                // 按优先级前缀搜索（完全匹配）
                foreach (var prefix in COVER_PREFIXES)
                {
                    var match = allImages.FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).Equals(prefix, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        return (match, CoverSourceType.ImageFile);
                }

                // 按优先级前缀搜索（前缀匹配）
                foreach (var prefix in COVER_PREFIXES)
                {
                    var match = allImages.FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        return (match, CoverSourceType.ImageFile);
                }

                // 返回第一个图片
                return (allImages[0], CoverSourceType.ImageFile);
            }
            catch
            {
                return (null, CoverSourceType.None);
            }
        }

        /// <summary>
        /// 从目录搜索封面（包含音频内嵌）
        /// </summary>
        public (string path, CoverSourceType type) SearchFromDirectoryWithAudio(string directoryPath, string[] audioFiles = null)
        {
            // 先搜索图片
            var imageResult = SearchFromDirectoryOnly(directoryPath);
            if (imageResult.type == CoverSourceType.ImageFile)
                return imageResult;

            // 再搜索音频内嵌
            if (audioFiles != null && audioFiles.Length > 0)
            {
                var firstAudio = audioFiles.FirstOrDefault(f => File.Exists(f));
                if (firstAudio != null)
                    return (firstAudio, CoverSourceType.AudioEmbedded);
            }
            else if (Directory.Exists(directoryPath))
            {
                foreach (var ext in AUDIO_EXTENSIONS)
                {
                    var files = Directory.GetFiles(directoryPath, $"*{ext}");
                    if (files.Length > 0)
                        return (files[0], CoverSourceType.AudioEmbedded);
                }
            }

            return (null, CoverSourceType.None);
        }

        private List<string> GetImageFiles(string directoryPath)
        {
            var allImages = new List<string>();
            foreach (var ext in IMAGE_EXTENSIONS)
            {
                allImages.AddRange(Directory.GetFiles(directoryPath, $"*{ext}", SearchOption.TopDirectoryOnly));
            }
            return allImages;
        }
    }
}
