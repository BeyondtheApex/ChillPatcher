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
#define FH6_BRIDGE_VERSION "1.0.0"

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
    log::info("[bridge] FH6 OmniMix bridge starting; data_dir={}", data_dir.string());

    auto img = fmod_bridge::parse(reinterpret_cast<std::byte*>(GetModuleHandleW(nullptr)));
    if (!img.valid()) {
        log::error("[bridge] failed to parse host PE image; aborting");
        return;
    }

    fmod_bridge::FMODFns fns;
    if (!fmod_bridge::resolve_fmod_signatures(img, fns)) {
        log::warn("[bridge] some FMOD signatures unresolved -- DSP injection may retry late");
    }

    constexpr std::size_t ring_bytes = 8u << 20;
    AudioSourceManager mgr{ring_bytes};

    auto omni = std::make_unique<sources::OmniPcmSource>("fh6");
    if (!omni->initialize()) {
        log::error("[bridge] OmniPcmSource initialization failed; bridge will keep retrying via source pump");
    }
    mgr.register_source(std::move(omni));
    mgr.switch_to("omnimix");

    fmod_bridge::DSPBridge bridge{mgr, fns};
    bridge.set_gain(1.0f);
    bridge.set_force_stereo_audio(true);

    PlaybackConfig playback;
    playback.force_stereo_audio = true;
    playback.race_start_playback = "next";
    playback.quick_station_skip = true;

    std::unique_ptr<fmod_bridge::ControlLoop> ctrl;
    if (fns.ready()) {
        ctrl = std::make_unique<fmod_bridge::ControlLoop>(bridge, img, playback, 1.0f);
    } else {
        log::warn("[bridge] control loop not started because required FMOD signatures are missing");
    }

    // Keep the audio source pump running even while the ControlLoop is
    // still discovering the radio target (player on main menu).  Without
    // this, heartbeats stall during discovery and the backend will
    // disconnect the client after its heartbeat timeout (30 s).
    // pump_once() is guarded by a mutex so it coexists safely with the
    // ControlLoop's own pump ticks once discovery succeeds.
    log::info("[bridge] running");
    using namespace std::chrono_literals;
    for (;;) {
        mgr.pump_once();
        std::this_thread::sleep_for(20ms);
    }
}

} // namespace fh6

