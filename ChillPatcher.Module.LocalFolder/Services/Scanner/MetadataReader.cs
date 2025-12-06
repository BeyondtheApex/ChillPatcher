using System;
using System.IO;
using ChillPatcher.Module.LocalFolder.Models;
using Newtonsoft.Json;

namespace ChillPatcher.Module.LocalFolder.Services.Scanner
{
    /// <summary>
    /// 元数据读取器 - 读取 playlist.json 和 album.json
    /// </summary>
    public static class MetadataReader
    {
        private const string PLAYLIST_JSON = "playlist.json";
        private const string ALBUM_JSON = "album.json";

        /// <summary>
        /// 读取歌单显示名称
        /// </summary>
        public static string ReadPlaylistName(string directory)
        {
            var jsonPath = Path.Combine(directory, PLAYLIST_JSON);
            if (!File.Exists(jsonPath))
                return null;

            try
            {
                var json = File.ReadAllText(jsonPath);
                var data = JsonConvert.DeserializeObject<PlaylistJsonModel>(json);
                return data?.DisplayName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 读取专辑显示名称
        /// </summary>
        public static string ReadAlbumName(string directory)
        {
            var jsonPath = Path.Combine(directory, ALBUM_JSON);
            if (!File.Exists(jsonPath))
                return null;

            try
            {
                var json = File.ReadAllText(jsonPath);
                var data = JsonConvert.DeserializeObject<AlbumJsonModel>(json);
                return data?.DisplayName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 读取专辑艺术家
        /// </summary>
        public static string ReadAlbumArtist(string directory)
        {
            var jsonPath = Path.Combine(directory, ALBUM_JSON);
            if (!File.Exists(jsonPath))
                return null;

            try
            {
                var json = File.ReadAllText(jsonPath);
                var data = JsonConvert.DeserializeObject<AlbumJsonModel>(json);
                return data?.Artist;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 保存歌单元数据（如果不存在则创建）
        /// </summary>
        public static void SavePlaylistMetadata(string directory, string displayName)
        {
            try
            {
                var jsonPath = Path.Combine(directory, PLAYLIST_JSON);
                var data = new PlaylistJsonModel { DisplayName = displayName };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
            }
            catch
            {
                // 忽略保存失败
            }
        }

        /// <summary>
        /// 保存专辑元数据（如果不存在则创建）
        /// </summary>
        public static void SaveAlbumMetadata(string directory, string displayName, string artist = null)
        {
            try
            {
                var jsonPath = Path.Combine(directory, ALBUM_JSON);
                var data = new AlbumJsonModel { DisplayName = displayName, Artist = artist };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
            }
            catch
            {
                // 忽略保存失败
            }
        }

        /// <summary>
        /// 确保歌单元数据文件存在（不存在则创建默认值）
        /// </summary>
        public static void EnsurePlaylistMetadata(string directory, string defaultDisplayName)
        {
            var jsonPath = Path.Combine(directory, PLAYLIST_JSON);
            if (!File.Exists(jsonPath))
            {
                SavePlaylistMetadata(directory, defaultDisplayName);
            }
        }

        /// <summary>
        /// 确保专辑元数据文件存在（不存在则创建默认值）
        /// </summary>
        public static void EnsureAlbumMetadata(string directory, string defaultDisplayName, string defaultArtist = null)
        {
            var jsonPath = Path.Combine(directory, ALBUM_JSON);
            if (!File.Exists(jsonPath))
            {
                SaveAlbumMetadata(directory, defaultDisplayName, defaultArtist);
            }
        }
    }
}
