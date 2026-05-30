using System;
using UnityEngine;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 专辑封面图像处理工具类
    /// 提供圆形和方形封面 Sprite 创建功能
    /// </summary>
    public static class AlbumArtReader
    {
        /// <summary>
        /// 创建圆形封面 Sprite
        /// </summary>
        public static Sprite CreateCircularSprite(Texture2D source, int resolution = 88)
        {
            if (source == null)
                return null;

            try
            {
                // 创建圆形蒙版纹理
                var circularTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                
                float radius = resolution / 2f;
                var center = new Vector2(radius, radius);

                // 缩放原图到目标分辨率
                var scaledColors = GetScaledPixels(source, resolution, resolution);

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        var pos = new Vector2(x + 0.5f, y + 0.5f);
                        float distance = Vector2.Distance(pos, center);
                        
                        Color color = scaledColors[y * resolution + x];
                        
                        // 圆形边缘抗锯齿
                        if (distance > radius - 1)
                        {
                            float alpha = Mathf.Clamp01(radius - distance);
                            color.a *= alpha;
                        }
                        
                        if (distance > radius)
                        {
                            color = Color.clear;
                        }
                        
                        circularTexture.SetPixel(x, y, color);
                    }
                }

                circularTexture.Apply();

                return Sprite.Create(
                    circularTexture,
                    new Rect(0, 0, resolution, resolution),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"创建圆形封面失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建方形封面 Sprite
        /// </summary>
        public static Sprite CreateSquareSprite(Texture2D source, int resolution = 256)
        {
            if (source == null)
                return null;

            try
            {
                // 如果源纹理已经是目标分辨率，直接使用
                if (source.width == resolution && source.height == resolution)
                {
                    return Sprite.Create(
                        source,
                        new Rect(0, 0, resolution, resolution),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }

                // 创建缩放后的纹理
                var scaledTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                var scaledColors = GetScaledPixels(source, resolution, resolution);
                scaledTexture.SetPixels(scaledColors);
                scaledTexture.Apply();

                return Sprite.Create(
                    scaledTexture,
                    new Rect(0, 0, resolution, resolution),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"创建方形封面失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 缩放像素数据
        /// </summary>
        private static Color[] GetScaledPixels(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Color[targetWidth * targetHeight];
            
            float xRatio = (float)source.width / targetWidth;
            float yRatio = (float)source.height / targetHeight;
            
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = Mathf.FloorToInt(x * xRatio);
                    int sourceY = Mathf.FloorToInt(y * yRatio);
                    
                    sourceX = Mathf.Clamp(sourceX, 0, source.width - 1);
                    sourceY = Mathf.Clamp(sourceY, 0, source.height - 1);
                    
                    result[y * targetWidth + x] = source.GetPixel(sourceX, sourceY);
                }
            }
            
            return result;
        }
    }
}
