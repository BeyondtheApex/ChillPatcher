using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;




namespace OmniMixPlayer.Module.Netease
{
    /// <summary>
    /// 网易云封面加载器
    /// 负责加载模块默认封面和从网络获取歌曲/专辑封面
    /// </summary>
    public class NeteaseCoverLoader
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, (byte[] data, string mimeType)> _coverCache = new Dictionary<string, (byte[], string)>();
        
        private byte[] _favoritesCoverBytes;
        private byte[] _fmCoverBytes;

        public NeteaseCoverLoader(ILogger logger)
        {
            _logger = logger;
            LoadFavoritesCover();
            LoadFMCover();
        }

        public byte[] DefaultCoverBytes => _favoritesCoverBytes;
        public byte[] FavoritesCoverBytes => _favoritesCoverBytes;
        public byte[] FMCoverBytes => _fmCoverBytes ?? _favoritesCoverBytes;

        private void LoadFavoritesCover()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "ChillPatcher.Module.Netease.Resources.FAVORITES.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        _logger.LogWarning("[NeteaseCoverLoader] 收藏封面资源未找到");
                        return;
                    }

                    using (var memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        _favoritesCoverBytes = memory.ToArray();
                    }
                    _logger.LogInformation($"[NeteaseCoverLoader] 已加载收藏封面: {_favoritesCoverBytes.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NeteaseCoverLoader] 加载收藏封面失败: {ex.Message}");
            }
        }

        private void LoadFMCover()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "ChillPatcher.Module.Netease.Resources.FM.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        _logger.LogWarning("[NeteaseCoverLoader] FM封面资源未找到");
                        return;
                    }

                    using (var memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        _fmCoverBytes = memory.ToArray();
                    }
                    _logger.LogInformation($"[NeteaseCoverLoader] 已加载FM封面: {_fmCoverBytes.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NeteaseCoverLoader] 加载FM封面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从 URL 获取封面字节数据 (返回 data + mimeType)
        /// </summary>
        public async Task<(byte[] data, string mimeType)> GetCoverFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return (_favoritesCoverBytes, "image/png");

            url = EnsureHttps(url);

            if (_coverCache.TryGetValue(url, out var cached))
                return cached;

            return await GetCoverBytesFromUrlAsync(url);
        }

        /// <summary>
        /// 从 URL 获取封面字节数据
        /// </summary>
        public async Task<(byte[] data, string mimeType)> GetCoverBytesFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return (_favoritesCoverBytes, "image/png");

            url = EnsureHttps(url);

            if (_coverCache.TryGetValue(url, out var cached))
                return cached;

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsByteArrayAsync();
                    var mimeType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    _coverCache[url] = (data, mimeType);
                    return (data, mimeType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NeteaseCoverLoader] 获取封面字节失败: {ex.Message}");
            }

            return (_favoritesCoverBytes, "image/png");
        }

        /// <summary>
        /// 清除封面缓存
        /// </summary>
        public void ClearCache()
        {
            _coverCache.Clear();
        }

        /// <summary>
        /// 移除指定 URL 的缓存
        /// </summary>
        public void RemoveCache(string url)
        {
            _coverCache.Remove(url);
        }

        /// <summary>
        /// 确保 URL 使用 HTTPS 协议
        /// 避免 Unity "Insecure connection not allowed" 错误
        /// </summary>
        private static string EnsureHttps(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url.Substring(7);
            }

            return url;
        }
    }
}
