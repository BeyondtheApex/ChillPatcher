# OmniPcmShared

Native C ABI SDK for consuming the OmniMixPlayer shared-memory PCM protocol from game integrations.

It hides the shared-memory offsets, ring-buffer reads, stream identity checks, EOF/drain semantics, seek generation, and audible cursor handling behind a small ABI that can be used from Unity/BepInEx, Unreal, Godot, or any other native host.

It also exposes a lightweight control-plane client for game integrations. The DLL speaks gRPC-Web over `cpp-httplib` and uses `protoc` generated protobuf-lite types internally, so games do not need to depend on gRPC or protobuf directly.

## Build

Windows:

```bat
build.bat
```

Outputs:

```text
bin/native/x64/OmniPcmShared.dll
bin/native/x86/OmniPcmShared.dll
```

## Typical Client Flow

1. Connect to the backend and get the shared memory name.
2. `OmniPcm_Open(sharedMemoryName)`.
3. Request playback from the backend.
4. `OmniPcm_WaitForFormatReady(handle, uuid, timeoutMs)`.
5. Create the game's streaming audio object with `OmniPcmInfo.sample_rate`, `channels`, and `effective_total_frames`.
6. In the audio callback, call `OmniPcm_ReadFrames`.
7. In the game update loop, call `OmniPcm_ReportAudioSourcePosition(timeSamples)`.
8. Also in the update loop, call `OmniPcm_IsPlaybackComplete`; when true, let the client choose the next track.
9. For seek, call `OmniPcm_RequestSeek(frame)` and reset the game audio source. The SDK tracks seek generation and allows the audible cursor to move backward only for real seeks.

## Control Plane Flow

```c
OmniPcmClientConfig config = { 0 };
OmniPcmClientHandle client = OmniPcmClient_Create(&config); /* discovers omnimix_port.txt */

OmniPcmConnectOptions options = { 0 };
options.client_id = "fh6";
options.mod_id = "forza_horizon_6";
options.game_name = "Forza Horizon 6";
options.display_name = "Forza Horizon 6";
options.kind = OMNI_PCM_INSTANCE_KIND_GAME_MOD;
options.capability_flags =
    OMNI_PCM_CAP_SEEK |
    OMNI_PCM_CAP_VOLUME_CONTROL;

OmniPcmConnectionInfo info;
OmniPcmClient_ConnectInstance(client, &options, &info);
OmniPcmClient_Heartbeat(client, info.instance_id, NULL);
OmniPcmClient_GetStatus(client, info.instance_id, &status);
OmniPcmClient_PlaybackCommand(client, info.instance_id, OMNI_PCM_COMMAND_NEXT);
```

Events are protobuf WebSocket messages from `/ws`, decoded inside the DLL and delivered as flat `OmniPcmEventInfo` values:

```c
void on_event(const OmniPcmEventInfo* event_info, void* user_data) {
    /* event_info->type, instance_id, track_uuid, position, state, ... */
}

OmniPcmClient_StartEvents(client, on_event, NULL);
```

List-style APIs use a C buffer/count convention. Pass `out == NULL` with
`*inout_count = 0` to query the required count; allocate that many entries and
call again. If the supplied buffer is too small the function returns
`OMNI_PCM_NOT_READY` and still writes the required count.

Implemented control-plane groups:

- Instance lifecycle: connect, heartbeat, disconnect, delete, list.
- Profile/archive: get profile, update exposed profile fields, archive, list/get/delete archives, inherit from archive.
- Playback: play, pause, resume, toggle, next, previous, stop, seek, status.
- Settings: volume, target latency, shuffle, repeat, equalizer.
- Queue/history: get, add/insert/set/remove/move/clear queue; get/remove/move/clear history.
- Playlist sources: get and set.
- Backend: health/version and stop.
- Events: protobuf WebSocket events decoded to `OmniPcmEventInfo`.

## Regenerating C++ Protobuf Types

```powershell
cd g:\Csharp\Chill
& "$env:TEMP\protoc\bin\protoc.exe" `
  --proto_path=OmniMixPlayer\OmniMixPlayer.SDK\Protos `
  --cpp_out=lite:NativePlugins\OmniPcmShared\generated `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\common.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\track.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\album.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\tag.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\playlist.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\query.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\instance.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\events\ws_events.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\services\library.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\services\playback.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\services\instance.proto
```

## EOF Semantics

For v2 protocol streams, playback is complete only when:

```text
DecoderEof or SyntheticEof
and readCursor >= finalWriteCursor
and audibleCursor >= finalWriteCursor - tolerance
```

`readCursor` means the game audio callback has copied PCM out of shared memory. `audibleCursor` means the game believes those frames have actually reached the audible playback position. This prevents cutting off the last buffered audio.

## C# Wrapper

The main plugin also includes `ChillPatcher.Native.OmniPcmShared`, a thin P/Invoke wrapper for Unity/BepInEx integrations.
