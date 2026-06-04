using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;
using OmniMixPlayer.SDK.Protos.Services;

namespace OmniMixPlayer.Backend.Services
{
    /// <summary>
    /// LibraryService gRPC 实现 — 委托给 ILibraryRegistry
    /// </summary>
    public class LibraryServiceImpl : LibraryService.LibraryServiceBase
    {
        private readonly ILibraryRegistry _registry;

        public LibraryServiceImpl(ILibraryRegistry registry, ILogger<LibraryServiceImpl> logger)
        {
            _registry = registry;
        }

        // ── Track ──

        public override Task<UpsertTrackResponse> UpsertTrack(UpsertTrackRequest request, ServerCallContext context)
        {
            var result = _registry.UpsertTrack(request.Track);
            return Task.FromResult(new UpsertTrackResponse
            {
                Created = result.Created,
                Track = request.Track
            });
        }

        public override Task<UpsertTracksResponse> UpsertTracks(UpsertTracksRequest request, ServerCallContext context)
        {
            int created = 0, updated = 0;
            foreach (var t in request.Tracks)
            {
                var r = _registry.UpsertTrack(t);
                if (r.Created) created++; else updated++;
            }
            return Task.FromResult(new UpsertTracksResponse { Created = created, Updated = updated });
        }

        public override Task<Track> GetTrack(GetTrackRequest request, ServerCallContext context)
        {
            var track = _registry.GetTrack(request.Uuid);
            if (track == null) throw new RpcException(new Status(StatusCode.NotFound, "Track not found"));
            return Task.FromResult(track);
        }

        public override Task<QueryTracksResponse> QueryTracks(TrackQuery request, ServerCallContext context)
        {
            var tracks = _registry.QueryTracks(request);
            var resp = new QueryTracksResponse();
            resp.Tracks.AddRange(tracks);
            resp.Pagination = new Pagination { Total = _registry.CountTracks(request), Offset = request.Offset, Limit = request.Limit };
            return Task.FromResult(resp);
        }

        public override Task<DeleteTrackResponse> DeleteTrack(DeleteTrackRequest request, ServerCallContext context)
        {
            var ok = _registry.DeleteTrack(request.Uuid);
            return Task.FromResult(new DeleteTrackResponse { Success = ok });
        }

        // ── Track Tags ──

        public override Task<SetTrackTagsResponse> SetTrackTags(SetTrackTagsRequest request, ServerCallContext context)
        {
            var result = _registry.SetTrackTags(request.TrackUuid, request.TagIds);
            return Task.FromResult(new SetTrackTagsResponse { Success = true, TagCount = request.TagIds.Count });
        }

        public override Task<ModifyTrackTagResponse> AddTrackTag(ModifyTrackTagRequest request, ServerCallContext context)
        {
            var result = _registry.AddTrackTag(request.TrackUuid, request.TagId);
            return Task.FromResult(new ModifyTrackTagResponse { Success = result.Success });
        }

        public override Task<ModifyTrackTagResponse> RemoveTrackTag(ModifyTrackTagRequest request, ServerCallContext context)
        {
            var result = _registry.RemoveTrackTag(request.TrackUuid, request.TagId);
            return Task.FromResult(new ModifyTrackTagResponse { Success = result.Success });
        }

        public override Task<GetTrackTagsResponse> GetTrackTags(GetTrackTagsRequest request, ServerCallContext context)
        {
            var tags = _registry.GetTrackTags(request.TrackUuid);
            var resp = new GetTrackTagsResponse { TrackUuid = request.TrackUuid };
            resp.TagIds.AddRange(tags);
            return Task.FromResult(resp);
        }

        // ── Album ──

        public override Task<UpsertAlbumResponse> UpsertAlbum(UpsertAlbumRequest request, ServerCallContext context)
        {
            var result = _registry.UpsertAlbum(request.Album);
            return Task.FromResult(new UpsertAlbumResponse { Created = result.Created, Album = request.Album });
        }

        public override Task<UpsertAlbumsResponse> UpsertAlbums(UpsertAlbumsRequest request, ServerCallContext context)
        {
            var result = _registry.UpsertAlbums(request.Albums);
            return Task.FromResult(new UpsertAlbumsResponse { Created = request.Albums.Count, Updated = 0 });
        }

        public override Task<Album> GetAlbum(GetAlbumRequest request, ServerCallContext context)
        {
            var album = _registry.GetAlbum(request.Id);
            if (album == null) throw new RpcException(new Status(StatusCode.NotFound, "Album not found"));
            return Task.FromResult(album);
        }

        public override Task<QueryAlbumsResponse> QueryAlbums(AlbumQuery request, ServerCallContext context)
        {
            var albums = _registry.QueryAlbums(request);
            var resp = new QueryAlbumsResponse();
            resp.Albums.AddRange(albums);
            resp.Pagination = new Pagination { Total = _registry.CountAlbums(request), Offset = request.Offset, Limit = request.Limit };
            return Task.FromResult(resp);
        }

        public override Task<DeleteAlbumResponse> DeleteAlbum(DeleteAlbumRequest request, ServerCallContext context)
        {
            var ok = _registry.DeleteAlbum(request.Id);
            return Task.FromResult(new DeleteAlbumResponse { Success = ok });
        }

        // ── Tag ──

        public override Task<UpsertTagResponse> UpsertTag(UpsertTagRequest request, ServerCallContext context)
        {
            var result = _registry.UpsertTag(request.Tag);
            return Task.FromResult(new UpsertTagResponse { Created = result.Created, Tag = request.Tag });
        }

        public override Task<UpsertTagsResponse> UpsertTags(UpsertTagsRequest request, ServerCallContext context)
        {
            var result = _registry.UpsertTags(request.Tags);
            return Task.FromResult(new UpsertTagsResponse { Created = request.Tags.Count, Updated = 0 });
        }

        public override Task<Tag> GetTag(GetTagRequest request, ServerCallContext context)
        {
            var tag = _registry.GetTag(request.Id);
            if (tag == null) throw new RpcException(new Status(StatusCode.NotFound, "Tag not found"));
            return Task.FromResult(tag);
        }

        public override Task<QueryTagsResponse> QueryTags(TagQuery request, ServerCallContext context)
        {
            var tags = _registry.QueryTags(request);
            var resp = new QueryTagsResponse();
            resp.Tags.AddRange(tags);
            resp.Pagination = new Pagination { Total = _registry.CountTags(request), Offset = request.Offset, Limit = request.Limit };
            return Task.FromResult(resp);
        }

        public override Task<DeleteTagResponse> DeleteTag(DeleteTagRequest request, ServerCallContext context)
        {
            var ok = _registry.DeleteTag(request.Id);
            return Task.FromResult(new DeleteTagResponse { Success = ok });
        }

        // ── Playlist ──

        public override Task<UpsertPlaylistResponse> UpsertPlaylist(UpsertPlaylistRequest request, ServerCallContext context)
        {
            var result = _registry.UpsertPlaylist(request.Playlist);
            return Task.FromResult(new UpsertPlaylistResponse { Created = result.Created, Playlist = request.Playlist });
        }

        public override Task<Playlist> GetPlaylist(GetPlaylistRequest request, ServerCallContext context)
        {
            var pl = _registry.GetPlaylist(request.Id);
            if (pl == null) throw new RpcException(new Status(StatusCode.NotFound, "Playlist not found"));
            return Task.FromResult(pl);
        }

        public override Task<QueryPlaylistsResponse> QueryPlaylists(PlaylistQuery request, ServerCallContext context)
        {
            var playlists = _registry.QueryPlaylists(request);
            var resp = new QueryPlaylistsResponse();
            resp.Playlists.AddRange(playlists);
            resp.Pagination = new Pagination { Total = _registry.CountPlaylists(request), Offset = request.Offset, Limit = request.Limit };
            return Task.FromResult(resp);
        }

        public override Task<DeletePlaylistResponse> DeletePlaylist(DeletePlaylistRequest request, ServerCallContext context)
        {
            var ok = _registry.DeletePlaylist(request.Id);
            return Task.FromResult(new DeletePlaylistResponse { Success = ok });
        }

        // ── Playlist Entries ──

        public override Task<ReplacePlaylistEntriesResponse> ReplacePlaylistEntries(ReplacePlaylistEntriesRequest request, ServerCallContext context)
        {
            var result = _registry.ReplacePlaylistEntries(request.PlaylistId, request.Entries);
            return Task.FromResult(new ReplacePlaylistEntriesResponse { Success = true, EntryCount = request.Entries.Count });
        }

        public override Task<InsertPlaylistEntryResponse> InsertPlaylistEntry(InsertPlaylistEntryRequest request, ServerCallContext context)
        {
            var result = _registry.InsertPlaylistEntry(request.PlaylistId, request.Entry, request.Index);
            return Task.FromResult(new InsertPlaylistEntryResponse { Success = result.Success });
        }

        public override Task<RemovePlaylistEntryResponse> RemovePlaylistEntry(RemovePlaylistEntryRequest request, ServerCallContext context)
        {
            var ok = _registry.RemovePlaylistEntry(request.EntryId);
            return Task.FromResult(new RemovePlaylistEntryResponse { Success = ok });
        }

        public override Task<MovePlaylistEntryResponse> MovePlaylistEntry(MovePlaylistEntryRequest request, ServerCallContext context)
        {
            var ok = _registry.MovePlaylistEntry(request.EntryId, request.NewIndex);
            return Task.FromResult(new MovePlaylistEntryResponse { Success = ok });
        }

        public override Task<PlaylistWithEntries> GetPlaylistWithEntries(GetPlaylistWithEntriesRequest request, ServerCallContext context)
        {
            var result = _registry.GetPlaylistWithEntries(request.PlaylistId);
            return Task.FromResult(result);
        }

        // ── Module cleanup ──

        public override Task<UnregisterModuleResponse> UnregisterModule(UnregisterModuleRequest request, ServerCallContext context)
        {
            var stats = _registry.UnregisterModule(request.ModuleId);
            return Task.FromResult(new UnregisterModuleResponse
            {
                Success = stats.Success,
                TracksRemoved = stats.TracksRemoved,
                AlbumsRemoved = stats.AlbumsRemoved,
                TagsRemoved = stats.TagsRemoved,
                PlaylistsRemoved = stats.PlaylistsRemoved
            });
        }
    }
}
