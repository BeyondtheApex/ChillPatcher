using System;
using System.Data.SQLite;

namespace ChillPatcher.Module.LocalFolder.Services.Database
{
    /// <summary>
    /// 封面缓存数据访问
    /// </summary>
    public class CoverCacheRepository
    {
        private readonly SQLiteConnection _connection;

        public CoverCacheRepository(SQLiteConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// 获取缓存的封面路径
        /// </summary>
        public (string coverPath, int sourceType)? GetCoverCache(string cacheKey)
        {
            var sql = "SELECT cover_path, source_type FROM cover_cache WHERE cache_key = @key";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@key", cacheKey);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.GetInt32(1)
                        );
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 保存封面缓存
        /// </summary>
        public void SaveCoverCache(string cacheKey, string coverPath, int sourceType)
        {
            var sql = @"
                INSERT OR REPLACE INTO cover_cache (cache_key, cover_path, source_type, cached_at)
                VALUES (@key, @coverPath, @sourceType, @cachedAt)
            ";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@key", cacheKey);
                cmd.Parameters.AddWithValue("@coverPath", (object)coverPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sourceType", sourceType);
                cmd.Parameters.AddWithValue("@cachedAt", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 移除封面缓存
        /// </summary>
        public void RemoveCoverCache(string cacheKey)
        {
            var sql = "DELETE FROM cover_cache WHERE cache_key = @key";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@key", cacheKey);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 清除所有封面缓存
        /// </summary>
        public void ClearAllCoverCache()
        {
            var sql = "DELETE FROM cover_cache";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
