using System;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;

namespace ChillPatcher.Module.LocalFolder.Services.Cover
{
    /// <summary>
    /// 图片加载器 - 从文件或字节加载图片
    /// </summary>
    public class ImageLoader
    {
        private readonly ManualLogSource _logger;

        public ImageLoader(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 从文件加载图片字节
        /// </summary>
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

        /// <summary>
        /// 从音频文件提取封面
        /// </summary>
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

        /// <summary>
        /// 从字节创建 Texture2D
        /// </summary>
        public Texture2D CreateTexture(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            try
            {
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(data))
                    return texture;
                
                UnityEngine.Object.Destroy(texture);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"创建纹理失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从 Texture2D 创建 Sprite
        /// </summary>
        public Sprite CreateSprite(Texture2D texture)
        {
            if (texture == null)
                return null;

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        /// <summary>
        /// 从字节创建 Sprite
        /// </summary>
        public Sprite CreateSpriteFromBytes(byte[] data)
        {
            var texture = CreateTexture(data);
            return texture != null ? CreateSprite(texture) : null;
        }
    }
}
