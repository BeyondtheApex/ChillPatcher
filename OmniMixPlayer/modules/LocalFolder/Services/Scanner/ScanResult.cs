using System.Collections.Generic;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Module.LocalFolder.Services.Scanner
{
    public class ScanResult
    {
        public List<PlaylistInfo> Playlists { get; set; } = new List<PlaylistInfo>();
        public List<Album> Albums { get; set; } = new List<Album>();
        public List<Track> Music { get; set; } = new List<Track>();

        /// <summary>
        /// 每首歌所属的 playlist tag ID 列表（一首歌可属于多个 playlist）
        /// </summary>
        public Dictionary<string, List<string>> TrackPlaylistTags { get; set; } = new();

        public void AddPlaylistMembership(string trackUuid, string tagId)
        {
            if (string.IsNullOrWhiteSpace(trackUuid) || string.IsNullOrWhiteSpace(tagId))
                return;

            if (!TrackPlaylistTags.TryGetValue(trackUuid, out var tags))
            {
                tags = new List<string>();
                TrackPlaylistTags[trackUuid] = tags;
            }

            if (!tags.Contains(tagId))
                tags.Add(tagId);
        }
    }

    public class PlaylistInfo
    {
        public string TagId { get; set; }
        public string DisplayName { get; set; }
        public string DirectoryPath { get; set; }
    }
}
