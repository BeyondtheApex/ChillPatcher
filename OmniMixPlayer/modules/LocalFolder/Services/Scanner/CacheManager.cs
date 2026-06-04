using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Module.LocalFolder;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.LocalFolder.Services.Scanner
{
    public class CacheManager
    {
        private readonly LocalDatabase _database;
        private readonly ILogger _logger;

        public CacheManager(LocalDatabase database, ILogger logger) { _database = database; _logger = logger; }

        public bool LoadFromCache(string tagId, string displayName, string playlistDir, ScanResult result)
        {
            try
            {
                var albums = _database.GetAlbumCacheByPlaylist(tagId);
                foreach (var (albumId, albumDisplayName, directoryPath, isDefault) in albums)
                {
                    var finalDisplayName = !string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath)
                        ? MetadataReader.ReadAlbumName(directoryPath) ?? albumDisplayName
                        : albumDisplayName;
                    result.Albums.Add(new Album { Id = albumId, Title = finalDisplayName ?? albumId, ModuleId = ModuleInfo.MODULE_ID });
                }
                var songs = _database.GetSongCacheByPlaylist(tagId);
                int validCount = 0;
                foreach (var (uuid, albumId, title, artist, filePath, duration) in songs)
                {
                    if (!File.Exists(filePath)) { _logger.LogDebug($"缓存歌曲不存在: {filePath}"); continue; }
                    result.Music.Add(new Track { Uuid = uuid, Title = title ?? Path.GetFileNameWithoutExtension(filePath), Artist = artist, AlbumId = albumId, SourceType = SourceType.File, SourcePath = filePath, Duration = (float)duration, ModuleId = ModuleInfo.MODULE_ID, CoverUri = filePath });
                    result.AddPlaylistMembership(uuid, tagId);
                    validCount++;
                }
                return validCount > 0;
            }
            catch (System.Exception ex) { _logger.LogWarning($"缓存加载失败: {ex.Message}"); return false; }
        }

        public void SaveToCache(string tagId, string displayName, string playlistDir, ScanResult result)
        {
            try
            {
                _database.ClearPlaylistCache(tagId);
                _database.SavePlaylistCache(tagId, displayName, playlistDir);

                var songsInTag = result.Music
                    .Where(m => result.TrackPlaylistTags.TryGetValue(m.Uuid, out var tags) && tags.Contains(tagId))
                    .ToList();
                var albumIdsInTag = new HashSet<string>(songsInTag.Where(m => !string.IsNullOrEmpty(m.AlbumId)).Select(m => m.AlbumId));
                foreach (var album in result.Albums.Where(a => albumIdsInTag.Contains(a.Id)))
                    _database.SaveAlbumCache(album.Id, tagId, album.Title, playlistDir, album.Id.EndsWith("_other"));
                var songsToSave = songsInTag.Select(m => (m.Uuid, tagId, m.AlbumId, m.Title, m.Artist, m.SourcePath, (double)m.Duration));
                _database.SaveSongCacheBatch(songsToSave);
            }
            catch (System.Exception ex) { _logger.LogWarning($"缓存保存失败: {ex.Message}"); }
        }
    }
}
