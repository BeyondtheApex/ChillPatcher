using System;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public sealed class PlaylistSourceRequest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string[] uuids { get; set; } = Array.Empty<string>();
        public PlaylistSourceKind kind { get; set; } = PlaylistSourceKind.Unspecified;
        public string refId { get; set; }
    }

    public sealed class PlaylistSourceInfo
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public int SongCount { get; init; }
        public PlaylistSourceKind Kind { get; init; }
        public string RefId { get; init; }
    }
}
