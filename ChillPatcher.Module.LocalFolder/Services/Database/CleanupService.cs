using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace ChillPatcher.Module.LocalFolder.Services.Database
{
    /// <summary>
    /// 数据清理 - 清理孤儿记录
    /// </summary>
    public class CleanupService
    {
        private readonly SQLiteConnection _connection;

        public CleanupService(SQLiteConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// 清理不存在的收藏记录
        /// </summary>
        public int CleanupOrphanFavorites(HashSet<string> validUuids)
        {
            return CleanupOrphans("favorites", validUuids);
        }

        /// <summary>
        /// 清理不存在的排除记录
        /// </summary>
        public int CleanupOrphanExcluded(HashSet<string> validUuids)
        {
            return CleanupOrphans("excluded", validUuids);
        }

        /// <summary>
        /// 清理不存在的播放统计记录
        /// </summary>
        public int CleanupOrphanPlayStats(HashSet<string> validUuids)
        {
            return CleanupOrphans("play_stats", validUuids);
        }

        /// <summary>
        /// 清理所有孤儿记录
        /// </summary>
        public (int favorites, int excluded, int playStats) CleanupAllOrphans(HashSet<string> validUuids)
        {
            var favorites = CleanupOrphanFavorites(validUuids);
            var excluded = CleanupOrphanExcluded(validUuids);
            var playStats = CleanupOrphanPlayStats(validUuids);
            return (favorites, excluded, playStats);
        }

        /// <summary>
        /// 清理不存在歌单的缓存数据
        /// </summary>
        public int CleanupStalePlaylistCache(HashSet<string> validTagIds)
        {
            int count = 0;
            var allTagIds = GetAllCachedTagIds();

            foreach (var tagId in allTagIds)
            {
                if (!validTagIds.Contains(tagId))
                {
                    DeletePlaylistCache(tagId);
                    count++;
                }
            }

            return count;
        }

        private int CleanupOrphans(string tableName, HashSet<string> validUuids)
        {
            int count = 0;
            var orphans = new List<string>();

            // 获取所有记录的 UUID
            var sql = $"SELECT uuid FROM {tableName}";
            using (var cmd = new SQLiteCommand(sql, _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var uuid = reader.GetString(0);
                    if (!validUuids.Contains(uuid))
                    {
                        orphans.Add(uuid);
                    }
                }
            }

            // 删除孤儿记录
            foreach (var uuid in orphans)
            {
                var deleteSql = $"DELETE FROM {tableName} WHERE uuid = @uuid";
                using (var cmd = new SQLiteCommand(deleteSql, _connection))
                {
                    cmd.Parameters.AddWithValue("@uuid", uuid);
                    cmd.ExecuteNonQuery();
                    count++;
                }
            }

            return count;
        }

        private List<string> GetAllCachedTagIds()
        {
            var result = new List<string>();
            var sql = "SELECT tag_id FROM playlist_cache";
            using (var cmd = new SQLiteCommand(sql, _connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(reader.GetString(0));
                }
            }
            return result;
        }

        private void DeletePlaylistCache(string tagId)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM song_cache WHERE tag_id = @tagId", _connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@tagId", tagId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM album_cache WHERE tag_id = @tagId", _connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@tagId", tagId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM playlist_cache WHERE tag_id = @tagId", _connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@tagId", tagId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
