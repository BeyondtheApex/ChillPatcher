#include "fh6/log.hpp"
#include "fh6/audio_source_manager.hpp"
#include "fh6/config.hpp"
#include "fh6/fmod/dsp_bridge.hpp"
#include "fh6/fmod/dsp_control_loop.hpp"
#include "fh6/fmod/pe_image.hpp"
#include "fh6/sources/omni_pcm_source.hpp"

#include <windows.h>

#include <chrono>
#include <filesystem>
#include <memory>
#include <thread>

// Bridge version — parsed by build_all.py for version_info.json
#define FH6_BRIDGE_VERSION "2.0.0"

namespace fh6 {

namespace {

std::filesystem::path module_directory(HMODULE self) {
    wchar_t buf[MAX_PATH]{};
    DWORD n = GetModuleFileNameW(self, buf, MAX_PATH);
    if (n == 0 || n >= MAX_PATH) return {};
    return std::filesystem::path{buf}.parent_path();
}

} // namespace

void run_bridge(HMODULE self) noexcept {
    const auto dir = module_directory(self);
    const auto data_dir = dir / "fh6-omnimix";
    std::error_code ec;
    std::filesystem::create_directories(data_dir, ec);

    log::init(data_dir / "bridge.log");
    log::info("[bridge] FH6 OmniMix bridge v{} starting; data_dir={}",
              FH6_BRIDGE_VERSION, data_dir.string());

    // ── Parse host PE for FMOD signature scanning ──────────────────
    auto img = fmod_bridge::parse(reinterpret_cast<std::byte*>(GetModuleHandleW(nullptr)));
    if (!img.valid()) {
        log::error("[bridge] failed to parse host PE image; aborting");
        return;
    }

    fmod_bridge::FMODFns fns;
    if (!fmod_bridge::resolve_fmod_signatures(img, fns)) {
        log::warn("[bridge] some FMOD signatures unresolved; DSP injection may retry late");
    }

    // ── Create OmniMixPlayer audio source ─────────────────────────
    // The source declares Flutter-built-in-player capabilities
    // (SERVER_CONTROLLED_PLAYBACK + AUDIO_PLAYBACK + full library mgmt)
    // and uses the OmniPcmShared SDK for all backend communication.
    constexpr std::size_t ring_bytes = 8u << 20;
    AudioSourceManager mgr{ring_bytes};

    auto omni = std::make_unique<sources::OmniPcmSource>("");
    if (!omni->initialize()) {
        log::error("[bridge] OmniPcmSource init failed; retrying via pump loop");
    }
    mgr.register_source(std::move(omni));
    mgr.switch_to("omnimix");

    // ── FMOD DSP injection bridge ─────────────────────────────────
    fmod_bridge::DSPBridge bridge{mgr, fns};
    bridge.set_gain(1.0f);
    bridge.set_force_stereo_audio(true);

    PlaybackConfig playback;
    playback.force_stereo_audio  = true;
    playback.race_start_playback = "next";
    playback.quick_station_skip  = true;

    std::unique_ptr<fmod_bridge::ControlLoop> ctrl;
    if (fns.ready()) {
        ctrl = std::make_unique<fmod_bridge::ControlLoop>(bridge, img, playback, 1.0f);
    } else {
        log::warn("[bridge] control loop not started (missing FMOD signatures)");
    }

    // ── Main pump loop ────────────────────────────────────────────
    // The pump drives both the audio source (shared-memory reads,
    // heartbeat, SDK event processing) and the ControlLoop (FMOD radio
    // discovery, DSP retargeting). The mutex inside AudioSourceManager
    // keeps them safely interleaved.
    log::info("[bridge] entering main loop");
    using namespace std::chrono_literals;
    for (;;) {
        mgr.pump_once();
        std::this_thread::sleep_for(20ms);
    }
}

} // namespace fh6

