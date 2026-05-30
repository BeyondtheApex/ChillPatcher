using System;
using UnityEngine;
using BepInEx.Logging;

namespace ChillPatcher.UIFramework.Core
{
    /// <summary>
    /// 嵌入资源加载器
    /// 提供缓存的 loading 和 default 占位图
    /// </summary>
    public static class EmbeddedResources
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("EmbeddedResources");
        
        private static Sprite _loadingPlaceholder;
        private static Sprite _defaultPlaceholder;
        private static bool _loadingLoadAttempted = false;
        private static bool _defaultLoadAttempted = false;

        /// <summary>
        /// 获取加载中占位图 (loading.png)
        /// </summary>
        public static Sprite LoadingPlaceholder
        {
            get
            {
                if (_loadingPlaceholder == null && !_loadingLoadAttempted)
                {
                    _loadingLoadAttempted = true;
                    _loadingPlaceholder = LoadEmbeddedSprite("ChillPatcher.Resources.loading.png", "loading");
                }
                
                // 后备：使用灰色纹理
                if (_loadingPlaceholder == null)
                {
                    _loadingPlaceholder = CreateColoredSprite(new Color(0.3f, 0.3f, 0.3f, 1f));
                }
                
                return _loadingPlaceholder;
            }
        }

        /// <summary>
        /// 获取默认占位图 (defaultcover.png)
        /// </summary>
        public static Sprite DefaultPlaceholder
        {
            get
            {
                if (_defaultPlaceholder == null && !_defaultLoadAttempted)
                {
                    _defaultLoadAttempted = true;
                    _defaultPlaceholder = LoadEmbeddedSprite("ChillPatcher.Resources.defaultcover.png", "default cover");
                }
                
                // 后备：使用浅灰色纹理
                if (_defaultPlaceholder == null)
                {
                    _defaultPlaceholder = CreateColoredSprite(new Color(0.85f, 0.85f, 0.85f, 1f));
                }
                
                return _defaultPlaceholder;
            }
        }

        /// <summary>
        /// 从嵌入资源加载 Sprite
        /// </summary>
        private static Sprite LoadEmbeddedSprite(string resourceName, string displayName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(bytes))
                        {
                            var sprite = Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f)
                            );
                            Logger.LogInfo($"Loaded embedded {displayName}");
                            return sprite;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load embedded {displayName}: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// 创建纯色 Sprite
        /// </summary>
        private static Sprite CreateColoredSprite(Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(
                texture,
                new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f)
            );
        }
    }
}
