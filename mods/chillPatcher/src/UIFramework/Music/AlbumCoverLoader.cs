using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 封面来源类型
    /// </summary>
    public enum CoverSourceType
    {
        None = 0,
        ImageFile = 1,
        AudioEmbedded = 2
    }

    /// <summary>
    /// 专辑封面加载器 - 使用内存缓存
    /// </summary>
    public class AlbumCoverLoader
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = 
            BepInEx.Logging.Logger.CreateLogSource("AlbumCoverLoader");

        private static readonly string[] CoverPrefixes = { "cover", "folder", "front", "album", "thumb" };
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

        private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        
        private static Sprite _defaultCoverSprite;
        private static Sprite _localCoverSprite;

        public AlbumCoverLoader() { }

        /// <summary>
        /// 异步加载专辑封面
        /// </summary>
        public async Task<Sprite> LoadCoverAsync(string albumId, string directoryPath, string[] audioFiles = null)
        {
            if (string.IsNullOrEmpty(albumId) || string.IsNullOrEmpty(directoryPath))
                return null;

            if (_spriteCache.TryGetValue(albumId, out var cachedSprite) && cachedSprite != null)
                return cachedSprite;

            var texture = await LoadCoverTextureAsync(albumId, directoryPath, audioFiles);
            if (texture == null)
                return null;

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            
            _spriteCache[albumId] = sprite;
            return sprite;
        }

        /// <summary>
        /// 仅从目录加载封面
        /// </summary>
        public async Task<Sprite> LoadCoverFromDirectoryOnly(string albumId, string directoryPath)
        {
            if (string.IsNullOrEmpty(albumId) || string.IsNullOrEmpty(directoryPath))
                return null;

            if (_spriteCache.TryGetValue(albumId, out var cachedSprite) && cachedSprite != null)
                return cachedSprite;

            try
            {
                var (coverPath, sourceType) = await SearchCoverFromDirectoryOnlyAsync(directoryPath);
                
                if (!string.IsNullOrEmpty(coverPath) && sourceType == CoverSourceType.ImageFile)
                {
                    var imageBytes = await LoadImageBytesAsync(coverPath);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        var texture = CreateTextureFromBytes(imageBytes);
                        if (texture != null)
                        {
                            _textureCache[albumId] = texture;
                            var sprite = Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f),
                                100f
                            );
                            _spriteCache[albumId] = sprite;
                            return sprite;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load cover [{albumId}]: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 加载封面纹理
        /// </summary>
        private async Task<Texture2D> LoadCoverTextureAsync(string albumId, string directoryPath, string[] audioFiles)
        {
            if (_textureCache.TryGetValue(albumId, out var cached) && cached != null)
                return cached;

            try
            {
                var (coverPath, sourceType) = await SearchCoverAsync(directoryPath);
                
                if (!string.IsNullOrEmpty(coverPath) && sourceType == CoverSourceType.ImageFile)
                {
                    var imageBytes = await LoadImageBytesAsync(coverPath);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        var texture = CreateTextureFromBytes(imageBytes);
                        if (texture != null)
                        {
                            _textureCache[albumId] = texture;
                            return texture;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Load cover failed [{albumId}]: {ex.Message}");
            }

            return null;
        }

        private async Task<(string path, CoverSourceType type)> SearchCoverFromDirectoryOnlyAsync(string directoryPath)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                    return (null, CoverSourceType.None);

                try
                {
                    var allImages = new List<string>();
                    foreach (var ext in ImageExtensions)
                        allImages.AddRange(Directory.GetFiles(directoryPath, $"*{ext}", SearchOption.TopDirectoryOnly));

                    if (allImages.Count == 0)
                        return (null, CoverSourceType.None);

                    foreach (var prefix in CoverPrefixes)
                    {
                        var match = allImages.FirstOrDefault(f => 
                            Path.GetFileNameWithoutExtension(f).Equals(prefix, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                            return (match, CoverSourceType.ImageFile);
                    }

                    return (allImages.First(), CoverSourceType.ImageFile);
                }
                catch
                {
                    return (null, CoverSourceType.None);
                }
            });
        }

        private async Task<(string path, CoverSourceType type)> SearchCoverAsync(string directoryPath)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                    return (null, CoverSourceType.None);

                try
                {
                    var allImages = new List<string>();
                    foreach (var ext in ImageExtensions)
                        allImages.AddRange(Directory.GetFiles(directoryPath, $"*{ext}", SearchOption.TopDirectoryOnly));

                    foreach (var prefix in CoverPrefixes)
                    {
                        var match = allImages.FirstOrDefault(f => 
                            Path.GetFileNameWithoutExtension(f).Equals(prefix, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                            return (match, CoverSourceType.ImageFile);
                    }

                    foreach (var prefix in CoverPrefixes)
                    {
                        var match = allImages.FirstOrDefault(f => 
                            Path.GetFileNameWithoutExtension(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                            return (match, CoverSourceType.ImageFile);
                    }

                    if (allImages.Count > 0)
                        return (allImages[0], CoverSourceType.ImageFile);
                }
                catch { }

                return (null, CoverSourceType.None);
            });
        }

        private async Task<byte[]> LoadImageBytesAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                        return null;
                    return File.ReadAllBytes(filePath);
                }
                catch
                {
                    return null;
                }
            });
        }

        private Texture2D CreateTextureFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
            {
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }

            UnityEngine.Object.Destroy(texture);
            return null;
        }

        /// <summary>
        /// 从嵌入资源加载游戏封面
        /// </summary>
        public Sprite LoadGameCoverFromEmbeddedResource(int tagIndex)
        {
            string resourceName = $"ChillPatcher.Resources.gamecover{tagIndex}.jpg";
            
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        resourceName = $"ChillPatcher.Resources.gamecover{tagIndex}.png";
                        using (var pngStream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (pngStream == null)
                                return null;
                            return LoadSpriteFromStream(pngStream);
                        }
                    }
                    return LoadSpriteFromStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        private Sprite LoadSpriteFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                var texture = CreateTextureFromBytes(bytes);
                if (texture == null)
                    return null;

                return Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
        }

        /// <summary>
        /// 获取默认封面
        /// </summary>
        public static Sprite GetDefaultCoverSprite()
        {
            if (_defaultCoverSprite != null)
                return _defaultCoverSprite;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("ChillPatcher.Resources.defaultcover.png"))
                {
                    if (stream == null)
                        return null;

                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        var bytes = ms.ToArray();
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(bytes))
                        {
                            _defaultCoverSprite = Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f),
                                100f
                            );
                            return _defaultCoverSprite;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 获取本地音乐封面
        /// </summary>
        public static Sprite GetLocalCoverSprite()
        {
            if (_localCoverSprite != null)
                return _localCoverSprite;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("ChillPatcher.Resources.localcover.jpg"))
                {
                    if (stream == null)
                        return null;

                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        var bytes = ms.ToArray();
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(bytes))
                        {
                            _localCoverSprite = Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f),
                                100f
                            );
                            return _localCoverSprite;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public void ClearCache()
        {
            foreach (var texture in _textureCache.Values)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
            _textureCache.Clear();
            _spriteCache.Clear();
        }

        /// <summary>
        /// 从嵌入资源加载封面纹理（静态方法）
        /// 根据标签索引加载游戏内置封面
        /// </summary>
        public static Texture2D LoadEmbeddedCoverTexture(int tagIndex)
        {
            string[] resourceNames = new[]
            {
                $"ChillPatcher.Resources.gamecover{tagIndex}.jpg",
                $"ChillPatcher.Resources.gamecover{tagIndex}.png"
            };

            var assembly = Assembly.GetExecutingAssembly();
            
            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                            continue;

                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            var bytes = ms.ToArray();
                            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (texture.LoadImage(bytes))
                            {
                                texture.filterMode = FilterMode.Bilinear;
                                texture.wrapMode = TextureWrapMode.Clamp;
                                return texture;
                            }
                            UnityEngine.Object.Destroy(texture);
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
