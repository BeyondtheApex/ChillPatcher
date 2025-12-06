using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Module.LocalFolder.Services.Database;

namespace ChillPatcher.Module.LocalFolder.Services
{
    /// <summary>
    /// 本地模块数据库 - 统一访问入口
    /// </summary>
    public class LocalDatabase : IDisposable
    {
        private readonly DatabaseCore _core;
        private readonly FavoriteRepository _favorites;
        private readonly ExcludedRepository _excluded;
        private readonly PlayStatsRepository _playStats;
        private readonly CacheRepository _cache;
        private readonly CoverCacheRepository _coverCache;
        private readonly CleanupService _cleanup;

        public LocalDatabase(string dbPath, ManualLogSource logger)
        {
            _core = new DatabaseCore(dbPath, logger);
            _favorites = new FavoriteRepository(_core.Connection);
            _excluded = new ExcludedRepository(_core.Connection);
            _playStats = new PlayStatsRepository(_core.Connection);
            _cache = new CacheRepository(_core.Connection);
            _coverCache = new CoverCacheRepository(_core.Connection);
            _cleanup = new CleanupService(_core.Connection);
        }

        #region Favorites

        public bool IsFavorite(string uuid) => _favorites.IsFavorite(uuid);
        public void AddFavorite(string uuid) => _favorites.Add(uuid);
        public void RemoveFavorite(string uuid) => _favorites.Remove(uuid);
        public IReadOnlyList<string> GetAllFavorites() => _favorites.GetAll();

        #endregion

        #region Excluded

        public bool IsExcluded(string uuid) => _excluded.IsExcluded(uuid);
        public void AddExcluded(string uuid) => _excluded.Add(uuid);
        public void RemoveExcluded(string uuid) => _excluded.Remove(uuid);
        public IReadOnlyList<string> GetAllExcluded() => _excluded.GetAll();

        #endregion

        #region Play Stats

        public void UpdatePlayCount(string uuid) => _playStats.UpdatePlayCount(uuid);
        public int GetPlayCount(string uuid) => _playStats.GetPlayCount(uuid);
        public DateTime? GetLastPlayed(string uuid) => _playStats.GetLastPlayed(uuid);

        #endregion

        #region Playlist Cache

        public bool HasPlaylistCache(string tagId) => _cache.HasPlaylistCache(tagId);
        public void SavePlaylistCache(string tagId, string displayName, string directoryPath) 
            => _cache.SavePlaylistCache(tagId, displayName, directoryPath);
        public void ClearPlaylistCache(string tagId) => _cache.ClearPlaylistCache(tagId);

        #endregion

        #region Album Cache

        public void SaveAlbumCache(string albumId, string tagId, string displayName, string directoryPath, bool isDefault)
            => _cache.SaveAlbumCache(albumId, tagId, displayName, directoryPath, isDefault);
        public List<(string albumId, string displayName, string directoryPath, bool isDefault)> GetAlbumCacheByPlaylist(string tagId)
            => _cache.GetAlbumCacheByPlaylist(tagId);

        #endregion

        #region Song Cache

        public void SaveSongCacheBatch(IEnumerable<(string uuid, string tagId, string albumId, string title, string artist, string filePath)> songs)
            => _cache.SaveSongCacheBatch(songs);
        public List<(string uuid, string albumId, string title, string artist, string filePath)> GetSongCacheByPlaylist(string tagId)
            => _cache.GetSongCacheByPlaylist(tagId);

        #endregion

        #region Cover Cache

        public (string coverPath, int sourceType)? GetCoverCache(string cacheKey) => _coverCache.GetCoverCache(cacheKey);
        public void SaveCoverCache(string cacheKey, string coverPath, int sourceType) => _coverCache.SaveCoverCache(cacheKey, coverPath, sourceType);
        public void RemoveCoverCache(string cacheKey) => _coverCache.RemoveCoverCache(cacheKey);
        public void ClearAllCoverCache() => _coverCache.ClearAllCoverCache();

        #endregion

        #region Cleanup

        public (int favorites, int excluded, int playStats) CleanupOrphanRecords(HashSet<string> validUuids)
            => _cleanup.CleanupAllOrphans(validUuids);
        public int CleanupStalePlaylistCache(HashSet<string> validTagIds)
            => _cleanup.CleanupStalePlaylistCache(validTagIds);

        #endregion

        public void Dispose()
        {
            _core?.Dispose();
        }
    }
}
