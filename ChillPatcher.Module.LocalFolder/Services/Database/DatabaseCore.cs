using System;
using System.Data.SQLite;
using System.IO;
using BepInEx.Logging;

namespace ChillPatcher.Module.LocalFolder.Services.Database
{
    /// <summary>
    /// 数据库核心 - 连接管理和表创建
    /// </summary>
    public class DatabaseCore : IDisposable
    {
        private const int DB_VERSION = 3;
        private readonly string _dbPath;
        private readonly ManualLogSource _logger;
        private SQLiteConnection _connection;

        public SQLiteConnection Connection => _connection;

        public DatabaseCore(string dbPath, ManualLogSource logger)
        {
            _dbPath = dbPath;
            _logger = logger;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                var directory = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var connectionString = $"Data Source={_dbPath};Version=3;";
                _connection = new SQLiteConnection(connectionString);
                _connection.Open();

                CreateTables();
                MigrateIfNeeded();

                _logger.LogInfo($"数据库初始化成功: {_dbPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"数据库初始化失败: {ex}");
                throw;
            }
        }

        private void CreateTables()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS db_version (
                    version INTEGER PRIMARY KEY
                );

                CREATE TABLE IF NOT EXISTS favorites (
                    uuid TEXT PRIMARY KEY,
                    added_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS excluded (
                    uuid TEXT PRIMARY KEY,
                    added_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS play_stats (
                    uuid TEXT PRIMARY KEY,
                    play_count INTEGER DEFAULT 0,
                    last_played TEXT
                );

                CREATE TABLE IF NOT EXISTS playlist_cache (
                    tag_id TEXT PRIMARY KEY,
                    display_name TEXT,
                    directory_path TEXT NOT NULL,
                    last_scanned TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS song_cache (
                    uuid TEXT PRIMARY KEY,
                    tag_id TEXT NOT NULL,
                    album_id TEXT,
                    title TEXT,
                    artist TEXT,
                    file_path TEXT NOT NULL,
                    file_modified TEXT,
                    FOREIGN KEY (tag_id) REFERENCES playlist_cache(tag_id)
                );

                CREATE TABLE IF NOT EXISTS album_cache (
                    album_id TEXT PRIMARY KEY,
                    tag_id TEXT NOT NULL,
                    display_name TEXT,
                    directory_path TEXT NOT NULL,
                    is_default INTEGER DEFAULT 0,
                    FOREIGN KEY (tag_id) REFERENCES playlist_cache(tag_id)
                );

                CREATE TABLE IF NOT EXISTS cover_cache (
                    cache_key TEXT PRIMARY KEY,
                    cover_path TEXT,
                    source_type INTEGER DEFAULT 0,
                    cached_at TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_favorites_uuid ON favorites(uuid);
                CREATE INDEX IF NOT EXISTS idx_excluded_uuid ON excluded(uuid);
                CREATE INDEX IF NOT EXISTS idx_song_cache_tag ON song_cache(tag_id);
                CREATE INDEX IF NOT EXISTS idx_song_cache_album ON song_cache(album_id);
                CREATE INDEX IF NOT EXISTS idx_album_cache_tag ON album_cache(tag_id);
            ";

            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.ExecuteNonQuery();
            }

            InitializeVersion();
        }

        private void InitializeVersion()
        {
            var checkVersion = "SELECT COUNT(*) FROM db_version";
            using (var cmd = new SQLiteCommand(checkVersion, _connection))
            {
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    var insertVersion = $"INSERT INTO db_version (version) VALUES ({DB_VERSION})";
                    using (var insertCmd = new SQLiteCommand(insertVersion, _connection))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void MigrateIfNeeded()
        {
            var getVersion = "SELECT version FROM db_version LIMIT 1";
            int currentVersion;

            using (var cmd = new SQLiteCommand(getVersion, _connection))
            {
                currentVersion = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (currentVersion < DB_VERSION)
            {
                _logger.LogInfo($"数据库迁移: {currentVersion} -> {DB_VERSION}");

                if (currentVersion < 3)
                {
                    MigrateToV3();
                }

                var updateVersion = $"UPDATE db_version SET version = {DB_VERSION}";
                using (var cmd = new SQLiteCommand(updateVersion, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void MigrateToV3()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS cover_cache (
                    cache_key TEXT PRIMARY KEY,
                    cover_path TEXT,
                    source_type INTEGER DEFAULT 0,
                    cached_at TEXT NOT NULL
                );
            ";

            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
