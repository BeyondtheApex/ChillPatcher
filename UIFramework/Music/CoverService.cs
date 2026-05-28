using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using Cysharp.Threading.Tasks;
using Bulbul;
using ChillPatcher.Patches.UIFramework;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 统一封面服务 (客户端 IPC 版本)
    /// 提供歌曲和专辑封面的中心化获取，远程从 OmniMixPlayer 后端拉取，支持本地资源回退和缓存。
    /// </summary>
    public class CoverService
    {
        private static CoverService _instance;
        public static CoverService Instance => _instance ??= new CoverService();

        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, (byte[] data, string mimeType)> _bytesCache = new Dictionary<string, (byte[], string)>();

        // 默认封面 Sprites
        private Sprite _defaultMusicCover;
        private Sprite _defaultAlbumCover;
        private Sprite _localMusicCover;
        
        // 游戏封面缓存
        private readonly Dictionary<int, Sprite> _gameCoverCache = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, (byte[] data, string mimeType)> _gameCoverBytesCache = new Dictionary<int, (byte[], string)>();

        // 默认封面字节缓存
        private byte[] _defaultCoverBytes;
        private byte[] _localCoverBytes;

        /// <summary>
        /// 专辑封面加载完成事件
        /// 参数: (albumId, cover)
        /// </summary>
        public event Action<string, Sprite> OnAlbumCoverLoaded;

        /// <summary>
        /// 歌曲封面加载完成事件
        /// 参数: (uuid, cover)
        /// </summary>
        public event Action<string, Sprite> OnMusicCoverLoaded;

        public Sprite LoadingPlaceholder => Core.EmbeddedResources.LoadingPlaceholder;

        private CoverService()
        {
            _logger = BepInEx.Logging.Logger.CreateLogSource("CoverService");
            LoadDefaultCovers();
        }

        private void LoadDefaultCovers()
        {
            (_defaultMusicCover, _defaultCoverBytes) = LoadEmbeddedSpriteWithBytes("ChillPatcher.Resources.defaultcover.png");
            (_localMusicCover, _localCoverBytes) = LoadEmbeddedSpriteWithBytes("ChillPatcher.Resources.localcover.jpg");
            _defaultAlbumCover = _defaultMusicCover;
        }

        private (Sprite sprite, byte[] bytes) LoadEmbeddedSpriteWithBytes(string resourceName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return (null, null);
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
                _logger.LogError($"Failed to load embedded resource {resourceName}: {ex.Message}");
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
                    if (stream == null) return null;
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// 获取专辑封面（同步，如果未缓存则返回占位图并触发异步加载）
        /// </summary>
        public Sprite GetAlbumCoverOrPlaceholder(string albumId)
        {
            if (string.IsNullOrEmpty(albumId))
                return GetDefaultAlbumCover();

            var cacheKey = $"album:{albumId}";
            if (_spriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            _ = LoadAlbumCoverAsync(albumId);
            return LoadingPlaceholder;
        }

        /// <summary>
        /// 获取歌曲封面（同步，如果未缓存则返回占位图并触发异步加载）
        /// </summary>
        public Sprite GetMusicCoverOrPlaceholder(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
                return GetDefaultMusicCover();

            var cacheKey = $"music:{uuid}";
            if (_spriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            _ = LoadMusicCoverAsync(uuid);
            return LoadingPlaceholder;
        }

        private async Task LoadAlbumCoverAsync(string albumId)
        {
            await Task.Yield();
            try
            {
                var sprite = await GetAlbumCoverAsync(albumId);
                var cacheKey = $"album:{albumId}";
                _spriteCache[cacheKey] = sprite;
                OnAlbumCoverLoaded?.Invoke(albumId, sprite);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"LoadAlbumCoverAsync failed [{albumId}]: {ex.Message}");
                OnAlbumCoverLoaded?.Invoke(albumId, GetDefaultAlbumCover());
            }
        }

        private async Task LoadMusicCoverAsync(string uuid)
        {
            await Task.Yield();
            try
            {
                var sprite = await GetMusicCoverAsync(uuid);
                var cacheKey = $"music:{uuid}";
                _spriteCache[cacheKey] = sprite;
                OnMusicCoverLoaded?.Invoke(uuid, sprite);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"LoadMusicCoverAsync failed [{uuid}]: {ex.Message}");
                OnMusicCoverLoaded?.Invoke(uuid, GetDefaultMusicCover());
            }
        }

        /// <summary>
        /// 异步获取歌曲封面 Sprite
        /// </summary>
        public async Task<Sprite> GetMusicCoverAsync(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
                return GetDefaultMusicCover();

            var cacheKey = $"music:{uuid}";
            if (_spriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            var bytesResult = await GetMusicCoverBytesAsync(uuid);
            if (bytesResult.data != null && bytesResult.data.Length > 0)
            {
                await UniTask.SwitchToMainThread();
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(bytesResult.data))
                {
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    _spriteCache[cacheKey] = sprite;
                    return sprite;
                }
            }

            return GetDefaultMusicCover();
        }

        /// <summary>
        /// 异步获取专辑封面 Sprite
        /// </summary>
        public async Task<Sprite> GetAlbumCoverAsync(string albumId)
        {
            await Task.Yield();
            return GetDefaultAlbumCover();
        }

        /// <summary>
        /// 异步获取歌曲封面的字节数据
        /// </summary>
        public async Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
                return (null, null);

            var cacheKey = $"music_bytes:{uuid}";
            if (_bytesCache.TryGetValue(cacheKey, out var cached) && cached.data != null)
                return cached;

            try
            {
                var result = await OmniMixIntegration.Instance.GetCoverAsync(uuid);
                if (result.data != null)
                {
                    _bytesCache[cacheKey] = result;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"GetMusicCoverBytesAsync failed [{uuid}]: {ex.Message}");
            }

            return (null, null);
        }

        public Sprite GetDefaultMusicCover() => _defaultMusicCover ?? LoadingPlaceholder;
        public Sprite GetDefaultAlbumCover() => _defaultAlbumCover ?? LoadingPlaceholder;
        public Sprite GetLocalMusicCover() => _localMusicCover ?? LoadingPlaceholder;

        public Sprite GetGameCover(int audioTag)
        {
            if (_gameCoverCache.TryGetValue(audioTag, out var cached))
                return cached;

            var resourceName = GetGameCoverResourceName(audioTag);
            if (resourceName == null)
                return GetDefaultMusicCover();

            var sprite = LoadEmbeddedSprite(resourceName);
            if (sprite != null)
            {
                _gameCoverCache[audioTag] = sprite;
                return sprite;
            }

            return GetDefaultMusicCover();
        }

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
                1 => "ChillPatcher.Resources.gamecover1.jpg",
                2 => "ChillPatcher.Resources.gamecover2.jpg",
                4 => "ChillPatcher.Resources.gamecover3.png",
                _ => null
            };
        }

        public void RemoveMusicCover(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return;
            var spriteKey = $"music:{uuid}";
            if (_spriteCache.TryGetValue(spriteKey, out var sprite))
            {
                if (sprite != null && sprite != GetDefaultMusicCover() && sprite != GetLocalMusicCover())
                {
                    if (sprite.texture != null)
                        UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
                _spriteCache.Remove(spriteKey);
            }
            _bytesCache.Remove($"music_bytes:{uuid}");
        }

        public void RemoveAlbumCover(string albumId)
        {
            if (string.IsNullOrEmpty(albumId)) return;
            var spriteKey = $"album:{albumId}";
            if (_spriteCache.TryGetValue(spriteKey, out var sprite))
            {
                if (sprite != null && sprite != GetDefaultAlbumCover() && sprite != GetLocalMusicCover())
                {
                    if (sprite.texture != null)
                        UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
                _spriteCache.Remove(spriteKey);
            }
        }

        public void ClearCache()
        {
            foreach (var sprite in _spriteCache.Values)
            {
                if (sprite != null && sprite != GetDefaultMusicCover() 
                    && sprite != GetDefaultAlbumCover() 
                    && sprite != GetLocalMusicCover())
                {
                    if (sprite.texture != null)
                        UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
            }
            _spriteCache.Clear();
            _bytesCache.Clear();
        }
    }
}
