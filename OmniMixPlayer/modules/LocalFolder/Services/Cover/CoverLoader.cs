using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Module.LocalFolder.Services.Cover
{
    public class CoverLoader
    {
        private readonly LocalDatabase _database;
        private readonly IDefaultCoverProvider _defaultCover;
        private readonly ILogger _logger;
        private readonly CoverSearcher _searcher;
        private readonly ImageLoader _imageLoader;

        private readonly Dictionary<string, (byte[] data, string mimeType)> _coverCache = new Dictionary<string, (byte[] data, string mimeType)>();

        public CoverLoader(LocalDatabase database, IDefaultCoverProvider defaultCover, ILogger logger)
        {
            _database = database;
            _defaultCover = defaultCover;
            _logger = logger;
            _searcher = new CoverSearcher();
            _imageLoader = new ImageLoader(logger);
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return (_defaultCover.DefaultMusicCover, "image/jpeg");

            var cacheKey = $"music:{filePath}";
            if (_coverCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.data != null)
                    return cached;
                _coverCache.Remove(cacheKey);
            }

            (byte[] data, string mimeType) cover = (null, null);

            var audioBytes = await _imageLoader.ExtractAudioCoverAsync(filePath);
            if (audioBytes != null)
            {
                cover = (audioBytes, DetectImageMimeType(audioBytes));
            }

            if (cover.data == null)
            {
                var directory = Path.GetDirectoryName(filePath);
                cover = await LoadFromDirectoryOnlyAsync(directory);
            }

            if (cover.data == null)
            {
                cover = (_defaultCover.DefaultMusicCover, "image/jpeg");
            }

            _coverCache[cacheKey] = cover;
            return cover;
        }

        public async Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return (_defaultCover.DefaultAlbumCover, "image/jpeg");

            var cacheKey = $"album:{directoryPath}";
            if (_coverCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.data != null)
                    return cached;
                _coverCache.Remove(cacheKey);
            }

            var dbCache = _database.GetCoverCache(cacheKey);
            if (dbCache.HasValue && !string.IsNullOrEmpty(dbCache.Value.coverPath))
            {
                var cover = await LoadFromCacheDataAsync(dbCache.Value.coverPath, dbCache.Value.sourceType);
                if (cover.data != null)
                {
                    _coverCache[cacheKey] = cover;
                    return cover;
                }
                _database.RemoveCoverCache(cacheKey);
            }

            var (coverPath, sourceType) = _searcher.SearchFromDirectoryWithAudio(directoryPath);
            if (!string.IsNullOrEmpty(coverPath))
            {
                var cover = await LoadFromPathAsync(coverPath, sourceType);
                if (cover.data != null)
                {
                    _coverCache[cacheKey] = cover;
                    _database.SaveCoverCache(cacheKey, coverPath, (int)sourceType);
                    return cover;
                }
            }

            _coverCache[cacheKey] = (_defaultCover.DefaultAlbumCover, "image/jpeg");
            return (_defaultCover.DefaultAlbumCover, "image/jpeg");
        }

        public async Task<(byte[] data, string mimeType)> GetPlaylistCoverAsync(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return (_defaultCover.DefaultAlbumCover, "image/jpeg");

            var cacheKey = $"playlist:{directoryPath}";
            if (_coverCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.data != null)
                    return cached;
                _coverCache.Remove(cacheKey);
            }

            var cover = await LoadFromDirectoryOnlyAsync(directoryPath);
            if (cover.data == null)
            {
                cover = (_defaultCover.DefaultAlbumCover, "image/jpeg");
            }

            _coverCache[cacheKey] = cover;
            return cover;
        }

        private async Task<(byte[] data, string mimeType)> LoadFromDirectoryOnlyAsync(string directoryPath)
        {
            var (coverPath, sourceType) = _searcher.SearchFromDirectoryOnly(directoryPath);
            if (string.IsNullOrEmpty(coverPath) || sourceType != CoverSourceType.ImageFile)
                return (null, null);

            return await LoadFromPathAsync(coverPath, sourceType);
        }

        private async Task<(byte[] data, string mimeType)> LoadFromPathAsync(string path, CoverSourceType sourceType)
        {
            byte[] bytes = null;
            string mimeType = "image/jpeg";
            if (sourceType == CoverSourceType.ImageFile)
            {
                bytes = await _imageLoader.LoadImageBytesAsync(path);
                if (bytes != null)
                    mimeType = GetMimeTypeFromPath(path);
            }
            else if (sourceType == CoverSourceType.AudioEmbedded)
            {
                bytes = await _imageLoader.ExtractAudioCoverAsync(path);
                if (bytes != null)
                    mimeType = DetectImageMimeType(bytes);
            }

            return bytes != null ? (bytes, mimeType) : (null, null);
        }

        private async Task<(byte[] data, string mimeType)> LoadFromCacheDataAsync(string path, int sourceType)
        {
            if (!File.Exists(path))
                return (null, null);

            return await LoadFromPathAsync(path, (CoverSourceType)sourceType);
        }

        public void ClearCache()
        {
            _coverCache.Clear();
            _database.ClearAllCoverCache();
        }

        public void RemoveMusicCoverCache(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var cacheKey = $"music:{filePath}";
            _coverCache.Remove(cacheKey);
        }

        public void RemoveAlbumCoverCache(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;

            var cacheKey = $"album:{directoryPath}";
            _coverCache.Remove(cacheKey);
            _database.RemoveCoverCache(cacheKey);
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return (null, null);

            var audioBytes = await _imageLoader.ExtractAudioCoverAsync(filePath);
            if (audioBytes != null && audioBytes.Length > 0)
            {
                string mimeType = DetectImageMimeType(audioBytes);
                return (audioBytes, mimeType);
            }

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

            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";

            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";

            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";

            if (data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";

            if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";

            return "image/jpeg";
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
