using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ChillPatcher.Module.LocalFolder.Services.Database
{
    /// <summary>
    /// 收藏数据访问
    /// </summary>
    public class FavoriteRepository
    {
        private readonly SQLiteConnection _connection;

        public FavoriteRepository(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public bool IsFavorite(string uuid)
        {
            var sql = "SELECT COUNT(*) FROM favorites WHERE uuid = @uuid";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@uuid", uuid);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void Add(string uuid)
        {
            var sql = "INSERT OR REPLACE INTO favorites (uuid, added_at) VALUES (@uuid, @added_at)";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@uuid", uuid);
                cmd.Parameters.AddWithValue("@added_at", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public void Remove(string uuid)
        {
            var sql = "DELETE FROM favorites WHERE uuid = @uuid";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@uuid", uuid);
                cmd.ExecuteNonQuery();
            }
        }

        public IReadOnlyList<string> GetAll()
        {
            var result = new List<string>();
            var sql = "SELECT uuid FROM favorites";

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
    }
}
