using System.Collections.Generic;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Module.LocalFolder.Services.Scanner
{
    /// <summary>
    /// 扫描结果
    /// </summary>
    public class ScanResult
    {
        public List<PlaylistInfo> Playlists { get; set; } = new List<PlaylistInfo>();
        public List<AlbumInfo> Albums { get; set; } = new List<AlbumInfo>();
        public List<MusicInfo> Music { get; set; } = new List<MusicInfo>();
    }

    /// <summary>
    /// 歌单信息
    /// </summary>
    public class PlaylistInfo
    {
        public string TagId { get; set; }
        public string DisplayName { get; set; }
        public string DirectoryPath { get; set; }
    }
}
