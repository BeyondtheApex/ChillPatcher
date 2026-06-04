# LocalFolder Module

LocalFolder scans a user-selected music root and declares the discovered library
through `IModuleContext.Library`.

## Folder Model

The scanner treats top-level playlist folders as playlists and tags. Album
folders become albums. Tracks are upserted once by UUID and then linked to tags
and playlist entries.

Example:

```text
Library/
  Playlist A/
    playlist.json
    cover.jpg
    Album X/
      album.json
      track-1.mp3
      track-2.flac
  loose-track.mp3
```

## Backend Declarations

During scan the module:

- upserts tags for playlist folders;
- upserts playlists for playlist folders;
- upserts albums for album folders;
- upserts tracks for audio files;
- sets many-to-many track tags;
- replaces playlist entries with ordered `PlaylistEntrySpec` values.

The backend owns deduplication, filtering, playlist membership, tag membership,
and CRUD persistence. The module keeps only local scan/cache metadata for file
system state, favorite/excluded flags, play stats, and cover cache.

## Rescan

`RefreshAsync` unregisters this module's backend library declarations and then
runs a fresh scan. The backend cleanup removes tracks, albums, tags, playlists,
playlist entries, and track-tag links that belong to this module.
