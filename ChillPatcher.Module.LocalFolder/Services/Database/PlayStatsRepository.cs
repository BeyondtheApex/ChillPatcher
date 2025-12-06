using System;
using System.Data.SQLite;

namespace ChillPatcher.Module.LocalFolder.Services.Database
{
    /// <summary>
    /// 播放统计数据访问
    /// </summary>
    public class PlayStatsRepository
    {
        private readonly SQLiteConnection _connection;

        public PlayStatsRepository(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public void UpdatePlayCount(string uuid)
        {
            var sql = @"
                INSERT INTO play_stats (uuid, play_count, last_played) VALUES (@uuid, 1, @now)
                ON CONFLICT(uuid) DO UPDATE SET 
                    play_count = play_count + 1,
                    last_played = @now
            ";

            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@uuid", uuid);
                cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public int GetPlayCount(string uuid)
        {
            var sql = "SELECT play_count FROM play_stats WHERE uuid = @uuid";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@uuid", uuid);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        public DateTime? GetLastPlayed(string uuid)
        {
            var sql = "SELECT last_played FROM play_stats WHERE uuid = @uuid";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@uuid", uuid);
                var result = cmd.ExecuteScalar();
                if (result != null && DateTime.TryParse(result.ToString(), out var dt))
                    return dt;
                return null;
            }
        }
    }
}
