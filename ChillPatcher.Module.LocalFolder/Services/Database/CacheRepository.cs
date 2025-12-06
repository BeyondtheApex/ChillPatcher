using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ChillPatcher.Module.LocalFolder.Services.Database
{
    /// <summary>
    /// 缓存数据访问 - 歌单、专辑、歌曲缓存
    /// </summary>
    public class CacheRepository
    {
        private readonly SQLiteConnection _connection;

        public CacheRepository(SQLiteConnection connection)
        {
            _connection = connection;
        }

        #region Playlist Cache

        public bool HasPlaylistCache(string tagId)
        {
            var sql = "SELECT COUNT(*) FROM playlist_cache WHERE tag_id = @tagId";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@tagId", tagId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void SavePlaylistCache(string tagId, string displayName, string directoryPath)
        {
            var sql = @"
                INSERT OR REPLACE INTO playlist_cache (tag_id, display_name, directory_path, last_scanned)
                VALUES (@tagId, @displayName, @directoryPath, @lastScanned)
            ";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@tagId", tagId);
                cmd.Parameters.AddWithValue("@displayName", displayName);
                cmd.Parameters.AddWithValue("@directoryPath", directoryPath);
                cmd.Parameters.AddWithValue("@lastScanned", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public void ClearPlaylistCache(string tagId)
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

        #endregion

        #region Album Cache

        public void SaveAlbumCache(string albumId, string tagId, string displayName, string directoryPath, bool isDefault)
        {
            var sql = @"
                INSERT OR REPLACE INTO album_cache (album_id, tag_id, display_name, directory_path, is_default)
                VALUES (@albumId, @tagId, @displayName, @directoryPath, @isDefault)
            ";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@albumId", albumId);
                cmd.Parameters.AddWithValue("@tagId", tagId);
                cmd.Parameters.AddWithValue("@displayName", displayName);
                cmd.Parameters.AddWithValue("@directoryPath", directoryPath);
                cmd.Parameters.AddWithValue("@isDefault", isDefault ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        public List<(string albumId, string displayName, string directoryPath, bool isDefault)> GetAlbumCacheByPlaylist(string tagId)
        {
            var result = new List<(string, string, string, bool)>();
            var sql = "SELECT album_id, display_name, directory_path, is_default FROM album_cache WHERE tag_id = @tagId";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@tagId", tagId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add((
                            reader.GetString(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.GetString(2),
                            reader.GetInt32(3) == 1
                        ));
                    }
                }
            }
            return result;
        }

        #endregion

        #region Song Cache

        public void SaveSongCacheBatch(IEnumerable<(string uuid, string tagId, string albumId, string title, string artist, string filePath)> songs)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        INSERT OR REPLACE INTO song_cache (uuid, tag_id, album_id, title, artist, file_path, file_modified)
                        VALUES (@uuid, @tagId, @albumId, @title, @artist, @filePath, @fileModified)
                    ";

                    foreach (var song in songs)
                    {
                        var fileModified = File.Exists(song.filePath)
                            ? File.GetLastWriteTime(song.filePath).ToString("o")
                            : null;

                        using (var cmd = new SQLiteCommand(sql, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@uuid", song.uuid);
                            cmd.Parameters.AddWithValue("@tagId", song.tagId);
                            cmd.Parameters.AddWithValue("@albumId", (object)song.albumId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@title", (object)song.title ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@artist", (object)song.artist ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@filePath", song.filePath);
                            cmd.Parameters.AddWithValue("@fileModified", (object)fileModified ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
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

        public List<(string uuid, string albumId, string title, string artist, string filePath)> GetSongCacheByPlaylist(string tagId)
        {
            var result = new List<(string, string, string, string, string)>();
            var sql = "SELECT uuid, album_id, title, artist, file_path FROM song_cache WHERE tag_id = @tagId";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@tagId", tagId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add((
                            reader.GetString(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.IsDBNull(2) ? null : reader.GetString(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.GetString(4)
                        ));
                    }
                }
            }
            return result;
        }

        #endregion
    }
}
