using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.ModuleSystem.Registry
{
    /// <summary>
    /// 实现 ILibraryRegistry，封装 LibraryStorage
    /// </summary>
    public class LibraryRegistry : ILibraryRegistry
    {
        private readonly Audio.LibraryStorage _storage;
        private readonly ILogger _logger;

        public LibraryRegistry(Audio.LibraryStorage storage, ILogger logger)
        {
            _storage = storage;
            _logger = logger;
        }

        // ── Track ──

        public UpsertResult UpsertTrack(Track track)
        {
            var created = _storage.UpsertTrack(track);
            _logger.LogDebug("UpsertTrack {Uuid}: created={Created}", track.Uuid, created);
            return new UpsertResult { Created = created, Updated = !created };
        }

        public UpsertResult UpsertTracks(IEnumerable<Track> tracks)
        {
            int created = 0;
            int updated = 0;
            foreach (var t in tracks)
            {
                if (_storage.UpsertTrack(t)) created++;
                else updated++;
            }
            _logger.LogInformation("UpsertTracks: created={Created}, updated={Updated}", created, updated);
            return new UpsertResult { Created = created > 0, Updated = updated > 0 };
        }

        public Track GetTrack(string uuid)
        {
            var doc = _storage.GetTrack(uuid);
            return doc != null ? Audio.LibraryStorage.ToTrackProto(doc) : null;
        }

        public IReadOnlyList<Track> QueryTracks(TrackQuery query)
        {
            var docs = _storage.QueryTracks(query);
            return docs.Select(Audio.LibraryStorage.ToTrackProto).ToList();
        }

        public int CountTracks(TrackQuery query) => _storage.CountTracks(query);

        public bool DeleteTrack(string uuid)
        {
            return _storage.DeleteTrack(uuid);
        }

        // ── Track Tags ──

        public UpsertResult SetTrackTags(string trackUuid, IEnumerable<string> tagIds)
        {
            _storage.SetTrackTags(trackUuid, tagIds);
            return new UpsertResult { Created = true };
        }

        public UpsertResult AddTrackTag(string trackUuid, string tagId)
        {
            var ok = _storage.AddTrackTag(trackUuid, tagId);
            return new UpsertResult { Created = ok };
        }

        public UpsertResult RemoveTrackTag(string trackUuid, string tagId)
        {
            var ok = _storage.RemoveTrackTag(trackUuid, tagId);
            return new UpsertResult { Created = ok };
        }

        public IReadOnlyList<string> GetTrackTags(string trackUuid)
        {
            return _storage.GetTrackTags(trackUuid);
        }

        // ── Album ──

        public UpsertResult UpsertAlbum(Album album)
        {
            var created = _storage.UpsertAlbum(album);
            return new UpsertResult { Created = created, Updated = !created };
        }

        public UpsertResult UpsertAlbums(IEnumerable<Album> albums)
        {
            var created = _storage.UpsertAlbums(albums);
            return new UpsertResult { Created = created > 0, Updated = true };
        }

        public Album GetAlbum(string id)
        {
            var doc = _storage.GetAlbum(id);
            return doc != null ? Audio.LibraryStorage.ToAlbumProto(doc) : null;
        }

        public IReadOnlyList<Album> QueryAlbums(AlbumQuery query)
        {
            return _storage.QueryAlbums(query).Select(Audio.LibraryStorage.ToAlbumProto).ToList();
        }

        public int CountAlbums(AlbumQuery query) => _storage.CountAlbums(query);

        public bool DeleteAlbum(string id) => _storage.DeleteAlbum(id);

        // ── Tag ──

        public UpsertResult UpsertTag(Tag tag)
        {
            var created = _storage.UpsertTag(tag);
            return new UpsertResult { Created = created, Updated = !created };
        }

        public UpsertResult UpsertTags(IEnumerable<Tag> tags)
        {
            var created = _storage.UpsertTags(tags);
            return new UpsertResult { Created = created > 0, Updated = true };
        }

        public Tag GetTag(string id)
        {
            var doc = _storage.GetTag(id);
            return doc != null ? Audio.LibraryStorage.ToTagProto(doc) : null;
        }

        public IReadOnlyList<Tag> QueryTags(TagQuery query)
        {
            return _storage.QueryTags(query).Select(Audio.LibraryStorage.ToTagProto).ToList();
        }

        public int CountTags(TagQuery query) => _storage.CountTags(query);

        public bool DeleteTag(string id) => _storage.DeleteTag(id);

        // ── Playlist ──

        public UpsertResult UpsertPlaylist(Playlist playlist)
        {
            var created = _storage.UpsertPlaylist(playlist);
            return new UpsertResult { Created = created, Updated = !created };
        }

        public Playlist GetPlaylist(string id)
        {
            var doc = _storage.GetPlaylist(id);
            return doc != null ? Audio.LibraryStorage.ToPlaylistProto(doc) : null;
        }

        public IReadOnlyList<Playlist> QueryPlaylists(PlaylistQuery query)
        {
            return _storage.QueryPlaylists(query).Select(Audio.LibraryStorage.ToPlaylistProto).ToList();
        }

        public int CountPlaylists(PlaylistQuery query) => _storage.CountPlaylists(query);

        public bool DeletePlaylist(string id) => _storage.DeletePlaylist(id);

        // ── Playlist Entries ──

        public UpsertResult ReplacePlaylistEntries(string playlistId, IEnumerable<PlaylistEntrySpec> entries)
        {
            _storage.ReplacePlaylistEntries(playlistId, entries);
            return new UpsertResult { Created = true };
        }

        public UpsertResult InsertPlaylistEntry(string playlistId, PlaylistEntrySpec entry, int index)
        {
            var doc = _storage.InsertPlaylistEntry(playlistId, entry, index);
            return new UpsertResult { Created = doc != null };
        }

        public bool RemovePlaylistEntry(string entryId)
        {
            return _storage.RemovePlaylistEntry(entryId);
        }

        public bool MovePlaylistEntry(string entryId, int newIndex)
        {
            return _storage.MovePlaylistEntry(entryId, newIndex);
        }

        public PlaylistWithEntries GetPlaylistWithEntries(string playlistId)
        {
            var playlist = _storage.GetPlaylist(playlistId);
            var entries = _storage.GetPlaylistEntries(playlistId);

            var result = new PlaylistWithEntries
            {
                PlaylistId = playlistId,
                PlaylistName = playlist?.Name ?? ""
            };

            foreach (var entry in entries)
            {
                var trackDoc = _storage.GetTrack(entry.TrackUuid);
                result.Entries.Add(new PlaylistEntryWithTrack
                {
                    EntryId = entry.Id,
                    TrackUuid = entry.TrackUuid,
                    Title = trackDoc?.Title ?? "",
                    Artist = trackDoc?.Artist ?? "",
                    Duration = trackDoc?.Duration ?? 0,
                    AlbumId = trackDoc?.AlbumId ?? "",
                    CoverUri = trackDoc?.CoverUri ?? "",
                    Position = entry.Position
                });
            }

            return result;
        }

        // ── Module cleanup ──

        public UnregisterStats UnregisterModule(string moduleId)
        {
            var (tracks, albums, tags, playlists) = _storage.UnregisterModule(moduleId);
            _logger.LogInformation("UnregisterModule {ModuleId}: tracks={Tracks}, albums={Albums}, tags={Tags}, playlists={Playlists}",
                moduleId, tracks, albums, tags, playlists);
            return new UnregisterStats
            {
                Success = true,
                TracksRemoved = tracks,
                AlbumsRemoved = albums,
                TagsRemoved = tags,
                PlaylistsRemoved = playlists
            };
        }
    }
}
