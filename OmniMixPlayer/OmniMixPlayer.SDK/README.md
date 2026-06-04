# OmniMixPlayer SDK

The SDK exposes the module-facing contracts and shared protobuf models used by
the backend, modules, gRPC APIs, and WebSocket events.

## Current Model

- Tracks are unique by `Track.uuid`.
- Albums, tags, playlists, and playlist entries are stored by the backend
  library.
- A track can belong to many tags.
- A playlist stores ordered entries, so the same track can appear in multiple
  playlists and can appear more than once if a playlist chooses to do so.
- Modules declare library data through `ILibraryRegistry` upsert APIs.
- Public backend APIs and normal WebSocket events use protobuf messages.

## Module Context

Modules receive an `IModuleContext` during `InitializeAsync`.

Important services:

- `Library`: upsert/query/delete tracks, albums, tags, playlists, and playlist
  entries.
- `ConfigManager`: per-module configuration.
- `EventBus`: in-process module events.
- `DefaultCover`: fallback cover provider.
- `StreamingService`: backend streaming/PCM reader creation.
- `DependencyLoader`: native dependency loader.

## Library API

Use `ILibraryRegistry` for all library declarations:

- `UpsertTrack`, `UpsertTracks`, `GetTrack`, `QueryTracks`, `CountTracks`,
  `DeleteTrack`
- `SetTrackTags`, `AddTrackTag`, `RemoveTrackTag`, `GetTrackTags`
- `UpsertAlbum`, `UpsertAlbums`, `GetAlbum`, `QueryAlbums`, `CountAlbums`,
  `DeleteAlbum`
- `UpsertTag`, `UpsertTags`, `GetTag`, `QueryTags`, `CountTags`, `DeleteTag`
- `UpsertPlaylist`, `GetPlaylist`, `QueryPlaylists`, `CountPlaylists`,
  `DeletePlaylist`
- `ReplacePlaylistEntries`, `InsertPlaylistEntry`, `RemovePlaylistEntry`,
  `MovePlaylistEntry`, `GetPlaylistWithEntries`
- `UnregisterModule`

All model types are generated from `Protos/omni_mix_player/models/*.proto`.

## Instance And Playback APIs

Instance profiles are persistent. A connection creates or reattaches a runtime
session. Runtime session resources, such as shared memory and stream readers,
exist only while the instance is online and capable of server-controlled
playback.

Persistent profile fields include volume, target latency, equalizer state,
imported playlist ids, pinned tag ids, and playback queue state.

## WebSocket Events

Normal backend events are sent as binary protobuf `WsEvent` messages from
`Protos/omni_mix_player/events/ws_events.proto`.

Module UI push messages remain JSON text frames because UI trees are dynamic
and not part of the stable playback/library event contract.
