using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Module.LocalFolder.Services.Cover
{
    public class ImageLoader
    {
        private readonly ILogger _logger;

        public ImageLoader(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> LoadImageBytesAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                        return null;
                    return File.ReadAllBytes(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"加载图片失败: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<byte[]> ExtractAudioCoverAsync(string audioFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(audioFilePath))
                        return null;

                    using (var file = TagLib.File.Create(audioFilePath))
                    {
                        if (file.Tag.Pictures != null && file.Tag.Pictures.Length > 0)
                        {
                            return file.Tag.Pictures[0].Data.Data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"提取音频封面失败: {ex.Message}");
                }
                return null;
            });
        }
    }
}
