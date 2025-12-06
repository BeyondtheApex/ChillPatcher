using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;
using UnityEngine;

namespace ChillPatcher.Module.LocalFolder.Services.Cover
{
    /// <summary>
    /// 封面加载器 - 使用优先级搜索和数据库缓存
    /// </summary>
    public class CoverLoader
    {
        private readonly LocalDatabase _database;
        private readonly IDefaultCoverProvider _defaultCover;
        private readonly ManualLogSource _logger;
        private readonly CoverSearcher _searcher;
        private readonly ImageLoader _imageLoader;

        // 内存缓存
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public CoverLoader(LocalDatabase database, IDefaultCoverProvider defaultCover, ManualLogSource logger)
        {
            _database = database;
            _defaultCover = defaultCover;
            _logger = logger;
            _searcher = new CoverSearcher();
            _imageLoader = new ImageLoader(logger);
        }

        /// <summary>
        /// 获取歌曲封面
        /// </summary>
        public async Task<Sprite> GetMusicCoverAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return _defaultCover.LocalMusicCover;

            var cacheKey = $"music:{filePath}";
            if (_spriteCache.TryGetValue(cacheKey, out var cached))
                return cached;

            Sprite cover = null;

            // 1. 尝试从音频文件内嵌封面读取
            var audioBytes = await _imageLoader.ExtractAudioCoverAsync(filePath);
            if (audioBytes != null)
            {
                cover = _imageLoader.CreateSpriteFromBytes(audioBytes);
            }

            // 2. 如果没有，尝试从目录封面读取
            if (cover == null)
            {
                var directory = Path.GetDirectoryName(filePath);
                cover = await LoadFromDirectoryOnlyAsync(directory);
            }

            // 3. 使用默认封面
            if (cover == null)
            {
                cover = _defaultCover.LocalMusicCover;
            }

            _spriteCache[cacheKey] = cover;
            return cover;
        }

        /// <summary>
        /// 获取专辑封面
        /// </summary>
        public async Task<Sprite> GetAlbumCoverAsync(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return _defaultCover.DefaultAlbumCover;

            var cacheKey = $"album:{directoryPath}";
            if (_spriteCache.TryGetValue(cacheKey, out var cached))
                return cached;

            // 检查数据库缓存
            var dbCache = _database.GetCoverCache(cacheKey);
            if (dbCache.HasValue && !string.IsNullOrEmpty(dbCache.Value.coverPath))
            {
                var cover = await LoadFromCacheDataAsync(dbCache.Value.coverPath, dbCache.Value.sourceType);
                if (cover != null)
                {
                    _spriteCache[cacheKey] = cover;
                    return cover;
                }
                // 缓存失效
                _database.RemoveCoverCache(cacheKey);
            }

            // 搜索封面
            var (coverPath, sourceType) = _searcher.SearchFromDirectoryWithAudio(directoryPath);
            if (!string.IsNullOrEmpty(coverPath))
            {
                var cover = await LoadFromPathAsync(coverPath, sourceType);
                if (cover != null)
                {
                    _spriteCache[cacheKey] = cover;
                    _database.SaveCoverCache(cacheKey, coverPath, (int)sourceType);
                    return cover;
                }
            }

            _spriteCache[cacheKey] = _defaultCover.DefaultAlbumCover;
            return _defaultCover.DefaultAlbumCover;
        }

        /// <summary>
        /// 获取歌单封面（仅从目录图片加载）
        /// </summary>
        public async Task<Sprite> GetPlaylistCoverAsync(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return _defaultCover.DefaultAlbumCover;

            var cacheKey = $"playlist:{directoryPath}";
            if (_spriteCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var cover = await LoadFromDirectoryOnlyAsync(directoryPath);
            if (cover == null)
            {
                cover = _defaultCover.DefaultAlbumCover;
            }

            _spriteCache[cacheKey] = cover;
            return cover;
        }

        private async Task<Sprite> LoadFromDirectoryOnlyAsync(string directoryPath)
        {
            var (coverPath, sourceType) = _searcher.SearchFromDirectoryOnly(directoryPath);
            if (string.IsNullOrEmpty(coverPath) || sourceType != CoverSourceType.ImageFile)
                return null;

            return await LoadFromPathAsync(coverPath, sourceType);
        }

        private async Task<Sprite> LoadFromPathAsync(string path, CoverSourceType sourceType)
        {
            byte[] bytes = null;
            if (sourceType == CoverSourceType.ImageFile)
            {
                bytes = await _imageLoader.LoadImageBytesAsync(path);
            }
            else if (sourceType == CoverSourceType.AudioEmbedded)
            {
                bytes = await _imageLoader.ExtractAudioCoverAsync(path);
            }

            return bytes != null ? _imageLoader.CreateSpriteFromBytes(bytes) : null;
        }

        private async Task<Sprite> LoadFromCacheDataAsync(string path, int sourceType)
        {
            if (!File.Exists(path))
                return null;

            return await LoadFromPathAsync(path, (CoverSourceType)sourceType);
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            foreach (var sprite in _spriteCache.Values)
            {
                if (sprite != null && sprite.texture != null)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
            }
            _spriteCache.Clear();
            _database.ClearAllCoverCache();
        }

        /// <summary>
        /// 获取歌曲封面的原始字节数据（用于 SMTC 等需要字节数据的场景）
        /// </summary>
        public async Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return (null, null);

            // 1. 尝试从音频文件内嵌封面读取
            var audioBytes = await _imageLoader.ExtractAudioCoverAsync(filePath);
            if (audioBytes != null && audioBytes.Length > 0)
            {
                // 内嵌封面通常是 JPEG 或 PNG
                string mimeType = DetectImageMimeType(audioBytes);
                return (audioBytes, mimeType);
            }

            // 2. 如果没有，尝试从目录封面读取
            var directory = Path.GetDirectoryName(filePath);
            var (coverPath, sourceType) = _searcher.SearchFromDirectoryOnly(directory);
            if (!string.IsNullOrEmpty(coverPath) && sourceType == CoverSourceType.ImageFile && File.Exists(coverPath))
            {
                var bytes = await _imageLoader.LoadImageBytesAsync(coverPath);
                if (bytes != null && bytes.Length > 0)
                {
                    string mimeType = GetMimeTypeFromPath(coverPath);
                    return (bytes, mimeType);
                }
            }

            return (null, null);
        }

        private static string DetectImageMimeType(byte[] data)
        {
            if (data == null || data.Length < 4)
                return "image/jpeg";

            // PNG: 89 50 4E 47
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";

            // JPEG: FF D8 FF
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";

            // GIF: 47 49 46
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";

            // BMP: 42 4D
            if (data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";

            // WebP: 52 49 46 46 ... 57 45 42 50
            if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";

            return "image/jpeg"; // 默认
        }

        private static string GetMimeTypeFromPath(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }
    }
}
