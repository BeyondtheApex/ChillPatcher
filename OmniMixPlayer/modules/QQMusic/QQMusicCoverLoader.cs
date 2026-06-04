using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;


namespace OmniMixPlayer.Module.QQMusic
{
    /// <summary>
    /// Handles loading and caching of album cover images
    /// </summary>
    public class QQMusicCoverLoader
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, (byte[] data, string mimeType)> _coverCache;
        private readonly Dictionary<string, QQMusicBridge.SongInfo> _songInfoMap;
        private byte[] _defaultFavoritesCoverBytes;
        private byte[] _defaultQQMusicCoverBytes;
        private static readonly HttpClient _httpClient = new HttpClient();

        public QQMusicCoverLoader(
            ILogger logger,
            Dictionary<string, QQMusicBridge.SongInfo> songInfoMap)
        {
            _logger = logger;
            _songInfoMap = songInfoMap;
            _coverCache = new Dictionary<string, (byte[] data, string mimeType)>();

            LoadEmbeddedResources();
        }

        private void LoadEmbeddedResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream("OmniMixPlayer.Module.QQMusic.Resources.QQMUSIC.png"))
                {
                    if (stream != null)
                    {
                        _defaultQQMusicCoverBytes = new byte[stream.Length];
                        stream.Read(_defaultQQMusicCoverBytes, 0, _defaultQQMusicCoverBytes.Length);
                    }
                }

                using (var stream = assembly.GetManifestResourceStream("OmniMixPlayer.Module.QQMusic.Resources.FAVORITES.png"))
                {
                    if (stream != null)
                    {
                        _defaultFavoritesCoverBytes = new byte[stream.Length];
                        stream.Read(_defaultFavoritesCoverBytes, 0, _defaultFavoritesCoverBytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to load embedded resources: {ex.Message}");
            }
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid)
        {
            if (uuid == "qqmusic_login_song" || uuid == "qqmusic_login_song_wx")
            {
                return (_defaultQQMusicCoverBytes, "image/png");
            }

            if (_coverCache.TryGetValue(uuid, out var cached))
            {
                return cached;
            }

            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                return (_defaultQQMusicCoverBytes, "image/png");
            }

            if (string.IsNullOrEmpty(songInfo.CoverUrl))
            {
                return (_defaultQQMusicCoverBytes, "image/png");
            }

            var result = await DownloadCoverAsync(songInfo.CoverUrl);
            if (result.data != null)
            {
                _coverCache[uuid] = result;
                return result;
            }

            return (_defaultQQMusicCoverBytes, "image/png");
        }

        public async Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId)
        {
            if (_coverCache.TryGetValue(albumId, out var cached))
            {
                return cached;
            }

            if (albumId?.StartsWith("qqmusic_album_", StringComparison.Ordinal) == true)
            {
                var songInfo = _songInfoMap.Values.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.AlbumMid) &&
                    albumId == $"qqmusic_album_{s.AlbumMid}" &&
                    !string.IsNullOrWhiteSpace(s.CoverUrl));
                if (songInfo != null)
                {
                    var result = await DownloadCoverAsync(songInfo.CoverUrl);
                    if (result.data != null)
                    {
                        _coverCache[albumId] = result;
                        return result;
                    }
                }
            }

            return (_defaultQQMusicCoverBytes, "image/png");
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid)
        {
            if (_coverCache.TryGetValue(uuid, out var cached))
            {
                return cached;
            }

            if (!_songInfoMap.TryGetValue(uuid, out var songInfo))
            {
                return (null, null);
            }

            if (string.IsNullOrEmpty(songInfo.CoverUrl))
            {
                return (null, null);
            }

            var result = await DownloadCoverAsync(songInfo.CoverUrl);
            if (result.data != null && result.data.Length > 0)
            {
                _coverCache[uuid] = result;
                return result;
            }

            return (null, null);
        }

        private async Task<(byte[] data, string mimeType)> DownloadCoverAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync();
                    var mimeType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    return (data, mimeType);
                }

                _logger?.LogWarning($"Cover download failed: {response.StatusCode}");
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to download cover: {ex.Message}");
                return (null, null);
            }
        }

        public void ClearCache()
        {
            _coverCache.Clear();
        }

        public void RemoveMusicCoverCache(string uuid)
        {
            _coverCache.Remove(uuid);
        }

        public void RemoveAlbumCoverCache(string albumId)
        {
            _coverCache.Remove(albumId);
        }
    }
}
