using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bulbul;
using ChillPatcher.SDK.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChillPatcher.ModuleSystem.Services
{
    /// <summary>
    /// 音频加载器实现
    /// </summary>
    public class CoreAudioLoader : IAudioLoader
    {
        private static CoreAudioLoader _instance;
        public static CoreAudioLoader Instance => _instance;

        private static readonly string[] SUPPORTED_FORMATS = { ".mp3", ".wav", ".ogg", ".egg", ".flac", ".aiff", ".aif" };

        public string[] SupportedFormats => SUPPORTED_FORMATS;

        public static void Initialize()
        {
            if (_instance != null)
                return;

            _instance = new CoreAudioLoader();
            Plugin.Logger.LogInfo("CoreAudioLoader 初始化完成");
        }

        private CoreAudioLoader()
        {
        }

        public bool IsSupportedFormat(string filePath)
        {
            var extension = Path.GetExtension(filePath)?.ToLower();
            return SUPPORTED_FORMATS.Contains(extension);
        }

        public async Task<AudioClip> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Plugin.Logger.LogWarning($"文件不存在: {filePath}");
                return null;
            }

            if (!IsSupportedFormat(filePath))
            {
                Plugin.Logger.LogWarning($"不支持的格式: {filePath}");
                return null;
            }

            try
            {
                var result = await GameAudioInfo.DownloadAudioFile(filePath, CancellationToken.None);
                return result.Item1;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"加载音频失败 '{filePath}': {ex.Message}");
                return null;
            }
        }

        public async Task<AudioClip> LoadFromUrlAsync(string url)
        {
            // URL 加载暂不支持，返回 null
            // 如果需要支持，需要添加 UnityEngine.UnityWebRequestAudioModule 引用
            Plugin.Logger.LogWarning($"URL 音频加载暂不支持: {url}");
            await Task.CompletedTask;
            return null;
        }

        public async Task<(AudioClip clip, string title, string artist)> LoadWithMetadataAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (null, null, null);
            }

            if (!IsSupportedFormat(filePath))
            {
                return (null, null, null);
            }

            try
            {
                var result = await GameAudioInfo.DownloadAudioFile(filePath, CancellationToken.None);
                
                var clip = result.Item1;
                var title = result.Item2;
                var artist = result.Item3;

                // 如果没有标题，使用文件名
                if (string.IsNullOrEmpty(title))
                {
                    title = Path.GetFileNameWithoutExtension(filePath);
                }

                return (clip, title, artist);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"加载音频元数据失败 '{filePath}': {ex.Message}");
                return (null, null, null);
            }
        }

        public void UnloadClip(AudioClip clip)
        {
            if (clip != null)
            {
                UnityEngine.Object.Destroy(clip);
            }
        }
    }
}
