# OmniPcmShared

Native C ABI SDK for consuming the OmniMixPlayer shared-memory PCM protocol from game integrations.

It hides the shared-memory offsets, ring-buffer reads, stream identity checks, EOF/drain semantics, seek generation, and audible cursor handling behind a small ABI that can be used from Unity/BepInEx, Unreal, Godot, or any other native host.

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
