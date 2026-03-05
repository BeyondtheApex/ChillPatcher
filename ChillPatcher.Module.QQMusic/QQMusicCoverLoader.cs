using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// Handles loading and caching of album cover images
    /// </summary>
    public class QQMusicCoverLoader
    {
        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, Sprite> _coverCache;
        private readonly Dictionary<string, byte[]> _coverBytesCache;
        private readonly Dictionary<string, QQMusicBridge.SongInfo> _songInfoMap;
        private Sprite _defaultFavoritesSprite;
        private Sprite _defaultQQMusicSprite;

        public QQMusicCoverLoader(
            ManualLogSource logger,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            _logger = logger;
            _songInfoMap = songInfoMap;
            _coverCache = new Dictionary<string, Sprite>();
            _coverBytesCache = new Dictionary<string, byte[]>();

            LoadEmbeddedResources();
        }

        private void LoadEmbeddedResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Load default QQ Music cover
                using (var stream = assembly.GetManifestResourceStream("ChillPatcher.Module.QQMusic.Resources.QQMUSIC.png"))
                {
                    if (stream != null)
                    {
                        _defaultQQMusicSprite = LoadSpriteFromStream(stream);
                    }
                }

                // Load favorites cover
                using (var stream = assembly.GetManifestResourceStream("ChillPatcher.Module.QQMusic.Resources.FAVORITES.png"))
                {
                    if (stream != null)
                    {
                        _defaultFavoritesSprite = LoadSpriteFromStream(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to load embedded resources: {ex.Message}");
            }
        }

        private Sprite LoadSpriteFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                return CreateSpriteFromBytes(bytes);
            }
        }

        private Sprite CreateSpriteFromBytes(byte[] bytes)
        {
            var texture = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        public async Task<Sprite> GetMusicCoverAsync(string uuid)
        {
            // Check if this is the login song - return default cover
            if (uuid == "qqmusic_login_song")
            {
                return _defaultQQMusicSprite;
            }

            // Check cache first
            if (_coverCache.TryGetValue(uuid, out var cachedSprite))
            {
                return cachedSprite;
            }

            // Get song info to find cover URL
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                return _defaultQQMusicSprite;
            }

            if (string.IsNullOrEmpty(songInfo.CoverUrl))
            {
                return _defaultQQMusicSprite;
            }

            // Download cover
            var sprite = await DownloadCoverAsync(songInfo.CoverUrl);
            if (sprite != null)
            {
                _coverCache[uuid] = sprite;
                return sprite;
            }

            return _defaultQQMusicSprite;
        }

        public async Task<Sprite> GetAlbumCoverAsync(string albumId)
        {
            // Check cache
            if (_coverCache.TryGetValue(albumId, out var cachedSprite))
            {
                return cachedSprite;
            }

            // Return default covers for built-in albums
            if (albumId == QQMusicSongRegistry.FAVORITES_ALBUM_ID)
            {
                return _defaultFavoritesSprite ?? _defaultQQMusicSprite;
            }

            if (albumId == QQMusicSongRegistry.RECOMMEND_ALBUM_ID ||
                albumId == QQMusicSongRegistry.LOGIN_ALBUM_ID)
            {
                return _defaultQQMusicSprite;
            }

            // For playlist albums, try to get cover from extended data
            // This would require access to the album registry which we don't have here
            // So just return the default
            return _defaultQQMusicSprite;
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid)
        {
            // Check bytes cache
            if (_coverBytesCache.TryGetValue(uuid, out var cachedBytes))
            {
                return (cachedBytes, "image/jpeg");
            }

            // Get song info
            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                return (null, null);
            }

            if (string.IsNullOrEmpty(songInfo.CoverUrl))
            {
                return (null, null);
            }

            // Download bytes
            var bytes = await DownloadCoverBytesAsync(songInfo.CoverUrl);
            if (bytes != null && bytes.Length > 0)
            {
                _coverBytesCache[uuid] = bytes;
                return (bytes, "image/jpeg");
            }

            return (null, null);
        }

        private async Task<Sprite> DownloadCoverAsync(string url)
        {
            try
            {
                var bytes = await DownloadCoverBytesAsync(url);
                if (bytes == null || bytes.Length == 0)
                {
                    return null;
                }

                return CreateSpriteFromBytes(bytes);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to download cover: {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]> DownloadCoverBytesAsync(string url)
        {
            try
            {
                using (var request = UnityWebRequestTexture.GetTexture(url))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Delay(50);
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        _logger?.LogWarning($"Cover download failed: {request.error}");
                        return null;
                    }

                    return request.downloadHandler.data;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to download cover bytes: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            foreach (var sprite in _coverCache.Values)
            {
                if (sprite != null && sprite != _defaultFavoritesSprite && sprite != _defaultQQMusicSprite)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
            }
            _coverCache.Clear();
            _coverBytesCache.Clear();
        }

        public void RemoveMusicCoverCache(string uuid)
        {
            if (_coverCache.TryGetValue(uuid, out var sprite))
            {
                if (sprite != null && sprite != _defaultFavoritesSprite && sprite != _defaultQQMusicSprite)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
                _coverCache.Remove(uuid);
            }
            _coverBytesCache.Remove(uuid);
        }

        public void RemoveAlbumCoverCache(string albumId)
        {
            if (_coverCache.TryGetValue(albumId, out var sprite))
            {
                if (sprite != null && sprite != _defaultFavoritesSprite && sprite != _defaultQQMusicSprite)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
                _coverCache.Remove(albumId);
            }
        }
    }
}
