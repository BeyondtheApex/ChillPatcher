using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChillPatcher.SDK.Interfaces;
using UnityEngine;

namespace ChillPatcher.ModuleSystem.Services
{
    /// <summary>
    /// 默认封面提供器实现
    /// </summary>
    public class DefaultCoverProvider : IDefaultCoverProvider
    {
        private static DefaultCoverProvider _instance;
        public static DefaultCoverProvider Instance => _instance;

        private Sprite _defaultMusicCover;
        private Sprite _defaultAlbumCover;
        private Sprite _localMusicCover;
        
        // 游戏封面缓存
        private readonly Dictionary<int, Sprite> _gameCoverCache = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, (byte[] data, string mimeType)> _gameCoverBytesCache = new Dictionary<int, (byte[], string)>();
        
        // 默认封面字节缓存
        private byte[] _defaultCoverBytes;
        private byte[] _localCoverBytes;

        public Sprite DefaultMusicCover => _defaultMusicCover;
        public Sprite DefaultAlbumCover => _defaultAlbumCover;
        public Sprite LocalMusicCover => _localMusicCover;

        public static void Initialize()
        {
            if (_instance != null)
                return;

            _instance = new DefaultCoverProvider();
            _instance.LoadDefaultCovers();
        }

        private DefaultCoverProvider()
        {
        }

        private void LoadDefaultCovers()
        {
            // 从嵌入资源加载默认封面
            (_defaultMusicCover, _defaultCoverBytes) = LoadEmbeddedSpriteWithBytes("ChillPatcher.Resources.defaultcover.png");
            (_localMusicCover, _localCoverBytes) = LoadEmbeddedSpriteWithBytes("ChillPatcher.Resources.localcover.jpg");
            
            // 默认专辑封面使用默认音乐封面
            _defaultAlbumCover = _defaultMusicCover;

            Plugin.Logger.LogInfo("默认封面加载完成");
        }

        private (Sprite sprite, byte[] bytes) LoadEmbeddedSpriteWithBytes(string resourceName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Plugin.Logger.LogWarning($"嵌入资源不存在: {resourceName}");
                        return (null, null);
                    }

                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);

                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(data))
                    {
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        return (sprite, data);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"加载嵌入资源失败 '{resourceName}': {ex.Message}");
            }

            return (null, null);
        }

        private Sprite LoadEmbeddedSprite(string resourceName)
        {
            return LoadEmbeddedSpriteWithBytes(resourceName).sprite;
        }

        private byte[] LoadEmbeddedBytes(string resourceName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;

                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取游戏封面 Sprite
        /// </summary>
        /// <param name="audioTag">音频标签 (Original=1, Special=2, Other=4)</param>
        public Sprite GetGameCover(int audioTag)
        {
            if (_gameCoverCache.TryGetValue(audioTag, out var cached))
                return cached;

            var resourceName = GetGameCoverResourceName(audioTag);
            if (resourceName == null)
                return _defaultMusicCover;

            var sprite = LoadEmbeddedSprite(resourceName);
            if (sprite != null)
            {
                _gameCoverCache[audioTag] = sprite;
                return sprite;
            }

            return _defaultMusicCover;
        }

        /// <summary>
        /// 获取游戏封面字节数据
        /// </summary>
        public (byte[] data, string mimeType) GetGameCoverBytes(int audioTag)
        {
            if (_gameCoverBytesCache.TryGetValue(audioTag, out var cached))
                return cached;

            var resourceName = GetGameCoverResourceName(audioTag);
            if (resourceName == null)
                return (_defaultCoverBytes, "image/png");

            var bytes = LoadEmbeddedBytes(resourceName);
            if (bytes != null && bytes.Length > 0)
            {
                var mimeType = resourceName.EndsWith(".png") ? "image/png" : "image/jpeg";
                var result = (bytes, mimeType);
                _gameCoverBytesCache[audioTag] = result;
                return result;
            }

            return (_defaultCoverBytes, "image/png");
        }

        /// <summary>
        /// 获取默认封面字节数据
        /// </summary>
        /// <param name="isLocal">是否是本地导入歌曲</param>
        public (byte[] data, string mimeType) GetDefaultCoverBytes(bool isLocal)
        {
            if (isLocal)
                return (_localCoverBytes, "image/jpeg");
            return (_defaultCoverBytes, "image/png");
        }

        private string GetGameCoverResourceName(int audioTag)
        {
            return audioTag switch
            {
                1 => "ChillPatcher.Resources.gamecover1.jpg", // Original
                2 => "ChillPatcher.Resources.gamecover2.jpg", // Special
                4 => "ChillPatcher.Resources.gamecover3.png", // Other
                _ => null
            };
        }

        /// <summary>
        /// 设置自定义默认封面
        /// </summary>
        public void SetDefaultMusicCover(Sprite sprite)
        {
            if (sprite != null)
            {
                _defaultMusicCover = sprite;
            }
        }

        /// <summary>
        /// 设置自定义专辑默认封面
        /// </summary>
        public void SetDefaultAlbumCover(Sprite sprite)
        {
            if (sprite != null)
            {
                _defaultAlbumCover = sprite;
            }
        }

        /// <summary>
        /// 获取默认封面 (通用方法)
        /// </summary>
        public Sprite GetDefaultCover()
        {
            return _defaultMusicCover;
        }
    }
}
