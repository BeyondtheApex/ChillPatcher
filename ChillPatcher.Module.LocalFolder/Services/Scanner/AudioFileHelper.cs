using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChillPatcher.Module.LocalFolder.Services.Scanner
{
    /// <summary>
    /// 音频文件工具
    /// </summary>
    public static class AudioFileHelper
    {
        private static readonly string[] AUDIO_EXTENSIONS = 
        { 
            ".mp3", ".wav", ".ogg", ".egg", ".flac", ".aiff", ".aif", ".m4a" 
        };

        /// <summary>
        /// 检查是否为音频文件
        /// </summary>
        public static bool IsAudioFile(string filePath)
        {
            var ext = Path.GetExtension(filePath)?.ToLower();
            return AUDIO_EXTENSIONS.Contains(ext);
        }

        /// <summary>
        /// 获取目录中的音频文件
        /// </summary>
        public static IEnumerable<string> GetAudioFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(directory)
                .Where(IsAudioFile);
        }

        /// <summary>
        /// 递归获取目录中的音频文件（指定深度）
        /// </summary>
        public static IEnumerable<string> GetAudioFilesRecursive(string directory, int maxDepth = 1)
        {
            if (!Directory.Exists(directory) || maxDepth < 0)
                yield break;

            // 当前目录的文件
            foreach (var file in GetAudioFiles(directory))
            {
                yield return file;
            }

            // 子目录
            if (maxDepth > 0)
            {
                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    foreach (var file in GetAudioFilesRecursive(subDir, maxDepth - 1))
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}
