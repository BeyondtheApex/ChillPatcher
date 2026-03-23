using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChillPatcher.Module.Spotify
{
    public class SpotifySession
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string Product { get; set; } // "premium" or "free"

        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(RefreshToken);

        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);

        [JsonIgnore]
        public bool IsPremium => string.Equals(Product, "premium", StringComparison.OrdinalIgnoreCase);
    }

    // ========== User ==========

    public class SpotifyUser
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("product")] public string Product { get; set; }
        [JsonProperty("images")] public List<SpotifyImage> Images { get; set; }
    }

    public class SpotifyImage
    {
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("width")] public int? Width { get; set; }
        [JsonProperty("height")] public int? Height { get; set; }
    }

    // ========== Track ==========

    public class SpotifyTrack
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("uri")] public string Uri { get; set; }
        [JsonProperty("duration_ms")] public int DurationMs { get; set; }
        [JsonProperty("explicit")] public bool Explicit { get; set; }
        [JsonProperty("popularity")] public int Popularity { get; set; }
        [JsonProperty("preview_url")] public string PreviewUrl { get; set; }
        [JsonProperty("track_number")] public int TrackNumber { get; set; }
        [JsonProperty("artists")] public List<SpotifyArtist> Artists { get; set; }
        [JsonProperty("album")] public SpotifyAlbum Album { get; set; }
        [JsonProperty("external_urls")] public Dictionary<string, string> ExternalUrls { get; set; }

        [JsonIgnore]
        public string ArtistName => Artists != null && Artists.Count > 0
            ? string.Join(", ", Artists.ConvertAll(a => a.Name))
            : "Unknown";

        [JsonIgnore]
        public float DurationSeconds => DurationMs / 1000f;

        [JsonIgnore]
        public string BestCoverUrl
        {
            get
            {
                if (Album?.Images == null || Album.Images.Count == 0) return null;
                // 优先取 300x300 左右的中等尺寸
                foreach (var img in Album.Images)
                    if (img.Width >= 200 && img.Width <= 400) return img.Url;
                return Album.Images[0].Url;
            }
        }
    }

    public class SpotifyArtist
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }

    public class SpotifyAlbum
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("uri")] public string Uri { get; set; }
        [JsonProperty("release_date")] public string ReleaseDate { get; set; }
        [JsonProperty("images")] public List<SpotifyImage> Images { get; set; }

        [JsonIgnore]
        public string BestCoverUrl
        {
            get
            {
                if (Images == null || Images.Count == 0) return null;
                foreach (var img in Images)
                    if (img.Width >= 200 && img.Width <= 400) return img.Url;
                return Images[0].Url;
            }
        }
    }

    // ========== Playlist ==========

    public class SpotifyPlaylist
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("images")] public List<SpotifyImage> Images { get; set; }
        [JsonProperty("owner")] public SpotifyPlaylistOwner Owner { get; set; }
        [JsonProperty("tracks")] public SpotifyPlaylistTracksRef Tracks { get; set; }

        [JsonIgnore]
        public string BestCoverUrl
        {
            get
            {
                if (Images == null || Images.Count == 0) return null;
                return Images[0].Url;
            }
        }
    }

    public class SpotifyPlaylistOwner
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
    }

    public class SpotifyPlaylistTracksRef
    {
        [JsonProperty("total")] public int Total { get; set; }
    }

    public class SpotifyPlaylistTrackItem
    {
        [JsonProperty("track")] public SpotifyTrack Track { get; set; }
        [JsonProperty("added_at")] public string AddedAt { get; set; }
    }

    // ========== Playback (Connect) ==========

    public class SpotifyPlaybackState
    {
        [JsonProperty("is_playing")] public bool IsPlaying { get; set; }
        [JsonProperty("progress_ms")] public int? ProgressMs { get; set; }
        [JsonProperty("item")] public SpotifyTrack Item { get; set; }
        [JsonProperty("device")] public SpotifyDevice Device { get; set; }
        [JsonProperty("repeat_state")] public string RepeatState { get; set; }
        [JsonProperty("shuffle_state")] public bool ShuffleState { get; set; }
        [JsonProperty("timestamp")] public long Timestamp { get; set; }
    }

    public class SpotifyDevice
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("is_active")] public bool IsActive { get; set; }
        [JsonProperty("is_restricted")] public bool IsRestricted { get; set; }
        [JsonProperty("volume_percent")] public int? VolumePercent { get; set; }
    }

    public class SpotifyDevicesResponse
    {
        [JsonProperty("devices")] public List<SpotifyDevice> Devices { get; set; }
    }

    // ========== Paginated Response ==========

    public class SpotifyPaged<T>
    {
        [JsonProperty("items")] public List<T> Items { get; set; }
        [JsonProperty("total")] public int Total { get; set; }
        [JsonProperty("limit")] public int Limit { get; set; }
        [JsonProperty("offset")] public int Offset { get; set; }
        [JsonProperty("next")] public string Next { get; set; }
    }

    // ========== Token Response ==========

    public class SpotifyTokenResponse
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }
        [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
        [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
        [JsonProperty("token_type")] public string TokenType { get; set; }
    }

    // ========== Library (Saved Tracks) ==========

    public class SpotifySavedTrackItem
    {
        [JsonProperty("track")] public SpotifyTrack Track { get; set; }
        [JsonProperty("added_at")] public string AddedAt { get; set; }
    }
}
