using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Module.LocalFolder.Services.Scanner
{
    /// <summary>
    /// 缓存管理 - 数据库缓存加载和保存
    /// </summary>
    public class CacheManager
    {
        private readonly LocalDatabase _database;
        private readonly ILogger _logger;

        public CacheManager(LocalDatabase database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <summary>
        /// 从数据库缓存加载歌单
        /// </summary>
        public bool LoadFromCache(string tagId, string displayName, string playlistDir, ScanResult result)
        {
            try
            {
                // 加载专辑
                var albums = _database.GetAlbumCacheByPlaylist(tagId);
                foreach (var (albumId, albumDisplayName, directoryPath, isDefault) in albums)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        _logger.LogDebug($"缓存的专辑目录不存在，跳过: {directoryPath}");
                        continue;
                    }

                    var finalDisplayName = MetadataReader.ReadAlbumName(directoryPath) ?? albumDisplayName;

                    result.Albums.Add(new AlbumInfo
                    {
                        AlbumId = albumId,
                        DisplayName = finalDisplayName,
                        TagId = tagId,
                        DirectoryPath = directoryPath,
                        IsDefault = isDefault
                    });
                }

                // 加载歌曲
                var songs = _database.GetSongCacheByPlaylist(tagId);
                int validCount = 0;
                foreach (var (uuid, albumId, title, artist, filePath, duration) in songs)
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogDebug($"缓存的歌曲文件不存在，跳过: {filePath}");
                        continue;
                    }

                    result.Music.Add(new MusicInfo
                    {
                        UUID = uuid,
                        Title = title ?? Path.GetFileNameWithoutExtension(filePath),
                        Artist = artist,
                        AlbumId = albumId,
                        TagId = tagId,
                        SourceType = MusicSourceType.File,
                        SourcePath = filePath,
                        IsUnlocked = true,
                        Duration = (float)duration
                    });
                    validCount++;
                }

                _logger.LogDebug($"从缓存加载: {validCount} 首歌曲, {albums.Count} 个专辑");
                return validCount > 0;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning($"从缓存加载失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存到数据库缓存
        /// </summary>
        public void SaveToCache(string tagId, string displayName, string playlistDir, ScanResult result)
        {
            try
            {
                _database.ClearPlaylistCache(tagId);
                _database.SavePlaylistCache(tagId, displayName, playlistDir);

                foreach (var album in result.Albums.Where(a => a.TagId == tagId))
                {
                    _database.SaveAlbumCache(album.AlbumId, tagId, album.DisplayName, album.DirectoryPath, album.IsDefault);
                }

                var songsToSave = result.Music
                    .Where(m => m.TagId == tagId)
                    .Select(m => (m.UUID, tagId, m.AlbumId, m.Title, m.Artist, m.SourcePath, (double)m.Duration));
                _database.SaveSongCacheBatch(songsToSave);

                _logger.LogDebug($"保存缓存: {tagId} ({result.Music.Count(m => m.TagId == tagId)} 首歌曲)");
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning($"保存缓存失败: {ex.Message}");
            }
        }
    }
}
