using System.Collections.Generic;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.SDK.Interfaces
{
    /// <summary>
    /// Unified library declaration API. Modules describe library state through
    /// idempotent upsert calls; the backend owns storage, filtering, and CRUD.
    /// </summary>
    public interface ILibraryRegistry
    {
        UpsertResult UpsertTrack(Track track);
        UpsertResult UpsertTracks(IEnumerable<Track> tracks);
        Track GetTrack(string uuid);
        IReadOnlyList<Track> QueryTracks(TrackQuery query);
        int CountTracks(TrackQuery query);
        bool DeleteTrack(string uuid);

        UpsertResult SetTrackTags(string trackUuid, IEnumerable<string> tagIds);
        UpsertResult AddTrackTag(string trackUuid, string tagId);
        UpsertResult RemoveTrackTag(string trackUuid, string tagId);
        IReadOnlyList<string> GetTrackTags(string trackUuid);

        UpsertResult UpsertAlbum(Album album);
        UpsertResult UpsertAlbums(IEnumerable<Album> albums);
        Album GetAlbum(string id);
        IReadOnlyList<Album> QueryAlbums(AlbumQuery query);
        int CountAlbums(AlbumQuery query);
        bool DeleteAlbum(string id);

        UpsertResult UpsertTag(Tag tag);
        UpsertResult UpsertTags(IEnumerable<Tag> tags);
        Tag GetTag(string id);
        IReadOnlyList<Tag> QueryTags(TagQuery query);
        int CountTags(TagQuery query);
        bool DeleteTag(string id);

        UpsertResult UpsertPlaylist(Playlist playlist);
        Playlist GetPlaylist(string id);
        IReadOnlyList<Playlist> QueryPlaylists(PlaylistQuery query);
        int CountPlaylists(PlaylistQuery query);
        bool DeletePlaylist(string id);

        UpsertResult ReplacePlaylistEntries(string playlistId, IEnumerable<PlaylistEntrySpec> entries);
        UpsertResult InsertPlaylistEntry(string playlistId, PlaylistEntrySpec entry, int index);
        bool RemovePlaylistEntry(string entryId);
        bool MovePlaylistEntry(string entryId, int newIndex);
        PlaylistWithEntries GetPlaylistWithEntries(string playlistId);

        UnregisterStats UnregisterModule(string moduleId);
    }

    public sealed class UpsertResult
    {
        public bool Created { get; init; }
        public bool Updated { get; init; }
        public bool Success => Created || Updated;
    }

    public sealed class UnregisterStats
    {
        public bool Success { get; set; }
        public int TracksRemoved { get; set; }
        public int AlbumsRemoved { get; set; }
        public int TagsRemoved { get; set; }
        public int PlaylistsRemoved { get; set; }
    }
}
