#include "fh6/fmod/dsp_bridge.hpp"
#include "fh6/fmod/sig_scanner.hpp"
#include "fh6/audio_source_manager.hpp"
#include "fh6/log.hpp"
#include "fh6/safe_mem.hpp"

#include <algorithm>
#include <cmath>
#include <cstring>

namespace fh6::fmod_bridge {

namespace {

struct FMODSig {
    const char* anchor;
    const char* pattern;
};

constexpr FMODSig kAnchored[] = {
    {"System::createDSP", "4C 8B DC 56 48 81 EC 70 01 00 00|"
                          "40 53 55 56 57 41 56 48 81 EC 50 01 00 00"},
    {"DSP::release", "48 89 5C 24 10 57 48 81 EC 50 01 00 00"},
    {"ChannelControl::addDSP", "4C 8B DC 56 48 81 EC 70 01 00 00|"
                               "40 53 55 56 57 41 56 48 81 EC 50 01 00 00"},
    {"ChannelControl::removeDSP", "48 89 5C 24 18 48 89 74 24 20 57 48 81 EC 50 01 00 00"},
};

inline float soft_clip(float x) noexcept {
    constexpr float k = 0.85f;
    const float a = std::fabs(x);
    const float over = std::max(0.0f, a - k) / (1.0f - k);
    return std::copysign(std::min(a, k) + (1.0f - k) * over / (1.0f + over), x);
}

constexpr const char* kResolverPattern =
    "48 89 6C 24 18 48 89 74 24 20 57 41 56 41 57 48 83 EC 20 8B F9 "
    "8B C1 C1 EF 11 49 8B F0 D1 E8 81 E7 FF 0F 00 00 0F B7 E8 4C 8B "
    "F2 4C 8B F9";
constexpr const char* kUnlockPattern =
    "48 8B 89 F0 09 01 00 48 85 C9 0F 85 ?? ?? ?? ?? 33 C0 C3";

DSPBridge* g_bridge = nullptr;

struct FMOD_DSP_DESCRIPTION {
    uint32_t pluginsdkversion;
    char name[32];
    uint32_t version;
    int32_t numinputbuffers;
    int32_t numoutputbuffers;
    void* create;
    void* release;
    void* reset;
    void* read;
    void* process;
    void* setposition;
    int32_t numparameters;
    void* paramdesc;
    void* setparamfloat;
    void* setparamint;
    void* setparambool;
    void* setparamdata;
    void* getparamfloat;
    void* getparamint;
    void* getparambool;
    void* getparamdata;
    void* shouldiprocess;
    void* userdata;
    void* sys_register;
    void* sys_deregister;
    void* sys_mix;
};
static_assert(sizeof(FMOD_DSP_DESCRIPTION) == 216);

FMODFns::SystemCreateDSP_t resolve_create_dsp(const PEImage& img) noexcept {
    return reinterpret_cast<FMODFns::SystemCreateDSP_t>(
        find_by_anchor(img, kAnchored[0].anchor, kAnchored[0].pattern));
}

} // namespace

bool resolve_fmod_signatures(const PEImage& img, FMODFns& out) noexcept {
    if (!img.valid()) return false;

    out.host_base = img.base;
    out.system_create_dsp = resolve_create_dsp(img);
    out.dsp_release = reinterpret_cast<FMODFns::DSPRelease_t>(
        find_by_anchor(img, kAnchored[1].anchor, kAnchored[1].pattern));
    out.channel_control_add_dsp = reinterpret_cast<FMODFns::ChannelControlAddDSP_t>(
        find_by_anchor(img, kAnchored[2].anchor, kAnchored[2].pattern));
    out.channel_control_rem_dsp = reinterpret_cast<FMODFns::ChannelControlRemDSP_t>(
        find_by_anchor(img, kAnchored[3].anchor, kAnchored[3].pattern));
    out.handle_resolver =
        reinterpret_cast<FMODFns::HandleResolver_t>(find_by_pattern(img, kResolverPattern));
    out.handle_unlock =
        reinterpret_cast<FMODFns::HandleUnlock_t>(find_by_pattern(img, kUnlockPattern));

    log::info("[sigscan] createDSP={} dsp_release={} addDSP={} removeDSP={} resolver={} unlock={}",
              (void*)out.system_create_dsp, (void*)out.dsp_release,
              (void*)out.channel_control_add_dsp, (void*)out.channel_control_rem_dsp,
              (void*)out.handle_resolver, (void*)out.handle_unlock);

    if (!out.system_create_dsp)
        log::info("[sigscan] System::createDSP not yet visible -- will retry at first install");
    if (!out.handle_unlock)
        log::warn("[sigscan] Handle::unlock not resolved -- resolver locks may leak");
    return out.ready();
}

DSPBridge::DSPBridge(AudioSourceManager& mgr, const FMODFns& fns) : mgr_{mgr}, fns_{fns} {
    g_bridge = this;
}

DSPBridge::~DSPBridge() {
    release_current_dsp_locked();
    if (g_bridge == this) g_bridge = nullptr;
}

void DSPBridge::set_mode(DSPMode m) noexcept {
    auto prev = mode_.exchange(m, std::memory_order_acq_rel);
    if (prev != m) log::info("[dsp] mode {} -> {}", (int)prev, (int)m);
}

bool DSPBridge::validate_handle(uint32_t handle) const noexcept {
    if (!handle) return false;
    void* inst = nullptr;
    uint64_t lock_state = 0;
    uint32_t rc = ~0u;
    if (!seh_call([&] { rc = fns_.handle_resolver(handle, &inst, &lock_state); })) {
        if (last_bad_handle_ != handle) {
            last_bad_handle_ = handle;
            log::warn("[dsp] handle_resolver raised SEH exception");
        }
        return false;
    }
    if (fns_.handle_unlock && lock_state)
        seh_call([&] { fns_.handle_unlock(lock_state); });
    if (rc != 0) {
        if (last_bad_handle_ != handle) {
            last_bad_handle_ = handle;
            log::warn("[dsp] handle_resolver rc={} (handle 0x{:X})", rc, handle);
        }
        return false;
    }
    return inst != nullptr;
}

void DSPBridge::release_current_dsp_locked() noexcept {
    if (!current_dsp_) return;
    const uint32_t handle = current_handle_.load(std::memory_order_relaxed);
    if (handle)
        seh_call([&] { fns_.channel_control_rem_dsp(handle, current_dsp_); });
    seh_call([&] { fns_.dsp_release(current_dsp_); });
    current_dsp_ = nullptr;
    current_handle_.store(0, std::memory_order_release);
}

void DSPBridge::install_dsp_locked(uint32_t handle) noexcept {
    if (!fmod_system_ || !handle) return;

    if (!fns_.system_create_dsp && fns_.host_base) {
        fns_.system_create_dsp = resolve_create_dsp(parse(fns_.host_base));
        if (fns_.system_create_dsp)
            log::info("[dsp] resolved System::createDSP late at {}", (void*)fns_.system_create_dsp);
        else {
            log::warn("[dsp] System::createDSP still unresolved -- install aborted");
            return;
        }
    }

    constexpr uint32_t kVersions[] = {0x00011000u, 0x00011003u, 0x00010000u};

    FMOD_DSP_DESCRIPTION desc{};
    std::memcpy(desc.name, "FH6 OmniMix Radio", 18);
    desc.version = 1;
    desc.numinputbuffers = 1;
    desc.numoutputbuffers = 1;
    desc.read = reinterpret_cast<void*>(&DSPBridge::read_callback);
    desc.userdata = this;

    void* dsp = nullptr;
    uint32_t rc = ~0u;
    for (uint32_t v : kVersions) {
        desc.pluginsdkversion = v;
        if (!seh_call([&] { rc = fns_.system_create_dsp(fmod_system_, &desc, &dsp); })) {
            log::warn("[dsp] createDSP raised SEH (sdkver=0x{:X})", v);
            dsp = nullptr;
            continue;
        }
        if (rc == 0 && dsp) break;
        dsp = nullptr;
    }
    if (!dsp) {
        log::warn("[dsp] createDSP failed r={}", rc);
        return;
    }

    const auto channel = static_cast<uint64_t>(handle);
    if (!seh_call([&] { rc = fns_.channel_control_add_dsp(channel, 0, dsp); }) || rc != 0) {
        log::warn("[dsp] addDSP failed r={}", rc);
        seh_call([&] { fns_.dsp_release(dsp); });
        return;
    }

    current_dsp_ = dsp;
    current_handle_.store(handle, std::memory_order_release);
    log::info("[dsp] installed dsp={} on handle=0x{:X}", dsp, handle);
}

void DSPBridge::set_target(const RadioInstance& inst, void* fmod_system) noexcept {
    fmod_system_ = fmod_system;
    radio_stream_ = inst.radio_stream;
}

uint32_t DSPBridge::read_live_handle(std::byte* radio_stream) const noexcept {
    if (!radio_stream || !fns_.ready()) return 0;
    uint32_t handle = 0;
    if (!safe_read(radio_stream + 0x20, handle) || !handle) return 0;
    return validate_handle(handle) ? handle : 0;
}

void DSPBridge::retarget_if_needed() noexcept {
    if (mode() != DSPMode::pcm || !fmod_system_) return;
    const uint32_t handle = read_live_handle(radio_stream_);
    if (handle == current_handle_.load(std::memory_order_relaxed)) return;
    if (!handle) {
        if (current_handle_.load(std::memory_order_relaxed))
            release_current_dsp_locked();
        return;
    }

    log::info("[dsp] retargeting -> handle 0x{:X}", handle);
    release_current_dsp_locked();
    install_dsp_locked(handle);
}

uint32_t __stdcall DSPBridge::read_callback(void*, float* in_buf, float* out_buf,
                                            uint32_t length, int32_t,
                                            int32_t* out_channels) {
    auto* b = g_bridge;
    if (!b || !out_buf) return 0;
    const DSPMode m = b->mode();

    const int32_t out_ch = b->force_stereo_audio() ? 2 : 1;
    if (out_channels) *out_channels = out_ch;
    const std::size_t total = static_cast<std::size_t>(length) * out_ch;

    auto stats = [&] {
        b->calls_.fetch_add(1, std::memory_order_relaxed);
        b->last_len_.store(length, std::memory_order_relaxed);
        b->last_out_ch_.store(out_ch, std::memory_order_relaxed);
    };

    if (m == DSPMode::silence || m == DSPMode::off) {
        std::memset(out_buf, 0, total * sizeof(float));
        stats();
        return 0;
    }
    if (m == DSPMode::passthrough) {
        if (in_buf) std::memcpy(out_buf, in_buf, total * sizeof(float));
        else std::memset(out_buf, 0, total * sizeof(float));
        stats();
        return 0;
    }

    std::memset(out_buf, 0, total * sizeof(float));

    const float gain = b->gain();
    if (gain <= 0.0f) {
        stats();
        return 0;
    }

    constexpr uint32_t kChunkFrames = 1024;
    float scratch[kChunkFrames * 2];
    auto& ring = b->mgr_.ring();

    for (uint32_t produced = 0; produced < length;) {
        const uint32_t want_frames = std::min(length - produced, kChunkFrames);
        const std::size_t want_bytes = static_cast<std::size_t>(want_frames) * 2 * sizeof(float);
        const std::size_t got = ring.read(scratch, want_bytes);
        const uint32_t got_frames = static_cast<uint32_t>(got / (2 * sizeof(float)));

        for (uint32_t f = 0; f < got_frames; ++f) {
            const float l = soft_clip(scratch[f * 2 + 0] * gain);
            const float r = soft_clip(scratch[f * 2 + 1] * gain);
            float* o = out_buf + static_cast<std::size_t>(produced + f) * out_ch;
            if (out_ch == 1) {
                o[0] = (l + r) * 0.5f;
            } else {
                o[0] = l;
                o[1] = r;
                const float downmix = (l + r) * 0.5f;
                for (int32_t c = 2; c < out_ch; ++c) o[c] = downmix;
            }
        }

        produced += got_frames;
        if (got_frames < want_frames) {
            b->underruns_.fetch_add(1, std::memory_order_relaxed);
            break;
        }
    }

    stats();
    return 0;
}

} // namespace fh6::fmod_bridge

