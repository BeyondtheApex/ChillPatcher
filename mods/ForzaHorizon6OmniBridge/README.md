# ForzaHorizon6OmniBridge

Thin OmniMixPlayer integration layer for `fh6-universal-radio`.

The bridge intentionally keeps the upstream repository as a submodule and does
not modify its files. This project compiles the upstream FH6 injection pieces
directly:

- `src/proxy/dll_main.cpp`
- `src/fmod/pe_image.cpp`
- `src/fmod/sig_scanner.cpp`
- `src/fmod/radio_discovery.cpp`
- `src/fmod/game_state_probe.cpp`
- `src/fmod/metadata_injector.cpp`
- `src/fmod/dsp_control_loop.cpp`

The local overlay owns only:

- `src/bridge.cpp` - startup wiring, registers only `OmniPcmSource`
- `src/sources/omni_pcm_source.cpp` - OmniMixPlayer backend/shared-memory source
- `src/fmod/dsp_bridge.cpp` - float PCM DSP callback variant
- `src/ring_buffer.cpp` - float-frame ring buffer with read/write positions

This keeps the FH6 RTTI/signature/discovery/metadata logic easy to follow
upstream while isolating OmniMix-specific behavior to a small surface.

Build:

```bat
build.bat
```

Runtime files for FH6:

```text
version.dll
OmniPcmShared.dll
```

