#include "fh6/sources/omni_pcm_source.hpp"
#include "fh6/log.hpp"
#include "fh6/ring_buffer.hpp"

#include <algorithm>
#include <cmath>
#include <type_traits>

namespace fh6::sources {

namespace {
constexpr int kFmodRate                 = 48000;
constexpr int kOutChannels              = 2;
constexpr int kPumpFrames               = 2048;
constexpr int kLocalBufferTargetDivisor = 10;    // matches Flutter: rate/10 = 100ms
constexpr int kLocalBufferMinFrames     = 3072;  // matches Flutter: 1024*3
constexpr float kMinTargetLatency       = 0.1f;  // floor for target_latency to avoid 0

// ── Flutter built-in player capability flags ─────────────────────
//
// These match exactly what the Flutter GUI player (OmniMixPlayer
// Backend's PlaybackController) declares when connecting as a GUI
// instance. The only difference is this mod uses FMOD for output
// instead of a system audio device.
constexpr uint32_t kFlutterPlayerCapabilities =
    OMNI_PCM_CAP_SERVER_CONTROLLED_PLAYBACK |
    OMNI_PCM_CAP_QUEUE_MANAGEMENT           |
    OMNI_PCM_CAP_PLAYLIST_MANAGEMENT        |
    OMNI_PCM_CAP_SHUFFLE                    |
    OMNI_PCM_CAP_REPEAT                     |
    OMNI_PCM_CAP_SEEK                       |
    OMNI_PCM_CAP_VOLUME_CONTROL             |
    OMNI_PCM_CAP_EQUALIZER                  |
    OMNI_PCM_CAP_MULTIPLE_PLAYLISTS         |
    OMNI_PCM_CAP_TAG_FILTERING             |
    OMNI_PCM_CAP_UNLIMITED_TAGS            |
    OMNI_PCM_CAP_ALBUM_FILTERING           |
    OMNI_PCM_CAP_AUDIO_PLAYBACK;

// ── Helpers ──────────────────────────────────────────────────────

// (instance ID and backend startup are handled by the SDK —
//  see OmniPcmClient_Create / discover_instance_id in omni_pcm_control.cpp)

// Forward declaration is no longer needed — SDK handles instance ID detection

// ── Event queue push/pop (lock-free SPSC) ───────────────────────

inline bool event_queue_push(OmniPcmEventInfo* queue, std::size_t cap,
                             std::atomic<std::size_t>& w, std::atomic<std::size_t>& r,
                             const OmniPcmEventInfo& evt) {
    const auto wi = w.load(std::memory_order_relaxed);
    const auto ri = r.load(std::memory_order_acquire);
    if (wi - ri >= cap) return false; // full
    queue[wi % cap] = evt;
    w.store(wi + 1, std::memory_order_release);
    return true;
}

inline bool event_queue_pop(OmniPcmEventInfo* queue, std::size_t cap,
                            std::atomic<std::size_t>& w, std::atomic<std::size_t>& r,
                            OmniPcmEventInfo& out) {
    const auto ri = r.load(std::memory_order_relaxed);
    if (ri == w.load(std::memory_order_acquire)) return false; // empty
    out = queue[ri % cap];
    r.store(ri + 1, std::memory_order_release);
    return true;
}

} // namespace

// ═══════════════════════════════════════════════════════════════════
//  Api::ready
// ═══════════════════════════════════════════════════════════════════

bool OmniPcmSource::Api::ready() const noexcept {
    return dll
        && open_utf8 && close && is_open && last_error && snapshot && info
        && bind_current && format_ready && has_error && complete && read_frames
        && request_seek && set_audible
        && client_create && client_destroy && client_last_error
        && client_connect && client_heartbeat && client_disconnect
        && client_status && client_command && client_play && client_seek
        && client_start_events && client_stop_events
        && client_get_target_latency && client_set_target_latency;
}

// ═══════════════════════════════════════════════════════════════════
//  Construction / destruction
// ═══════════════════════════════════════════════════════════════════

OmniPcmSource::OmniPcmSource(std::string client_id)
    : client_id_{std::move(client_id)} {}

OmniPcmSource::~OmniPcmSource() {
    shutdown();
}

// (read_instance_id moved into SDK — see discover_instance_id in omni_pcm_control.cpp)

// ═══════════════════════════════════════════════════════════════════
//  Initialisation / shutdown
// ═══════════════════════════════════════════════════════════════════

bool OmniPcmSource::initialize() {
    std::scoped_lock lk{mutex_};
    if (!load_api()) return false;

    // Backend auto-start and instance ID detection are handled by the SDK.
    connected_ = connect_backend();
    if (connected_) {
        open_shared_memory();
        if (api_.client_start_events) {
            api_.client_start_events(client_, on_sdk_event, this);
            log::info("[omni] subscribed to backend events");
        }
    }
    next_heartbeat_ = std::chrono::steady_clock::now();
    return api_.ready();
}

void OmniPcmSource::shutdown() noexcept {
    std::scoped_lock lk{mutex_};
    // Stop events first so the callback won't touch freed memory
    if (client_ && api_.client_stop_events) {
        api_.client_stop_events(client_);
    }
    close_shared_memory();
    if (connected_ && client_ && api_.client_disconnect) {
        api_.client_disconnect(client_, instance_id_.empty() ? client_id_.c_str() : instance_id_.c_str());
    }
    connected_ = false;
    if (client_ && api_.client_destroy) {
        api_.client_destroy(client_);
        client_ = nullptr;
    }
    if (api_.dll) {
        FreeLibrary(api_.dll);
        api_ = {};
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Playback transport (called from ControlLoop / HTTP server)
// ═══════════════════════════════════════════════════════════════════

void OmniPcmSource::play() {
    std::scoped_lock lk{mutex_};
    if (!connected_) {
        connected_ = connect_backend();
        if (connected_) {
            open_shared_memory();
            next_heartbeat_ = std::chrono::steady_clock::now();
            eof_advanced_ = false;
        }
    }
    if (connected_) api_.client_play(client_, instance_id_.c_str(), nullptr);
    playing_ = true;
}

void OmniPcmSource::pause() {
    std::scoped_lock lk{mutex_};
    if (connected_) api_.client_command(client_, instance_id_.c_str(), OMNI_PCM_COMMAND_PAUSE);
    playing_ = false;
}

void OmniPcmSource::stop() {
    std::scoped_lock lk{mutex_};
    if (connected_) api_.client_command(client_, instance_id_.c_str(), OMNI_PCM_COMMAND_STOP);
    playing_ = false;
}

void OmniPcmSource::next() {
    std::scoped_lock lk{mutex_};
    if (connected_) api_.client_command(client_, instance_id_.c_str(), OMNI_PCM_COMMAND_NEXT);
    reset_stream_state();
    eof_advanced_ = false;
}

void OmniPcmSource::previous() {
    std::scoped_lock lk{mutex_};
    if (connected_) api_.client_command(client_, instance_id_.c_str(), OMNI_PCM_COMMAND_PREV);
    reset_stream_state();
    eof_advanced_ = false;
}

void OmniPcmSource::seek(uint64_t ms) {
    std::scoped_lock lk{mutex_};
    const double seconds = static_cast<double>(ms) / 1000.0;
    if (connected_) api_.client_seek(client_, instance_id_.c_str(), static_cast<float>(seconds));
    if (pcm_ && api_.request_seek) {
        const int rate = info_.sample_rate > 0 ? info_.sample_rate : kFmodRate;
        api_.request_seek(pcm_, static_cast<int64_t>(seconds * rate));
    }
    reset_stream_state();
}

bool OmniPcmSource::skip_next() {
    next();
    return true;
}

bool OmniPcmSource::restart_current() {
    seek(0);
    return true;
}

// ═══════════════════════════════════════════════════════════════════
//  Pump — main audio + housekeeping loop
// ═══════════════════════════════════════════════════════════════════

void OmniPcmSource::pump(RingBuffer& ring) {
    std::scoped_lock lk{mutex_};

    // 1. Process incoming SDK events (track metadata, state changes)
    process_pending_events();

    // 2. Heartbeat && status polling fallback
    heartbeat_if_due();

    // 3. Reconnect if needed (with cooldown to avoid log spam)
    if (!connected_) {
        auto now = std::chrono::steady_clock::now();
        if (now >= next_connect_attempt_) {
            connected_ = connect_backend();
            next_connect_attempt_ = now + std::chrono::seconds(5);
            if (connected_) {
                open_shared_memory();
                next_heartbeat_ = std::chrono::steady_clock::now();
                eof_advanced_ = false;
            }
        }
    }
    if (connected_ && !pcm_) open_shared_memory();

    if (!pcm_ || !api_.is_open(pcm_) || !playing_) return;

    // 4. Read snapshot to detect stream transitions & errors
    OmniPcmSnapshot snap{};
    if (api_.snapshot(pcm_, &snap) == OMNI_PCM_OK) {
        snapshot_ = snap;
        bool stream_changed = (current_stream_id_ != snap.stream_id || current_uuid_ != snap.current_uuid);
        bool seek_detected  = (last_seen_seek_generation_ != snap.seek_generation);

        if (stream_changed || seek_detected) {
            if (stream_changed) {
                current_stream_id_ = snap.stream_id;
                current_uuid_      = snap.current_uuid;
                api_.bind_current(pcm_);
            }
            last_seen_seek_generation_ = snap.seek_generation;
            reset_stream_state(&ring);
            eof_advanced_ = false;
        }
    }

    // 5. Check for stream errors
    if (api_.has_error(pcm_)) {
        log::warn("[omni] shared memory stream error: {} — requesting skip",
                  api_.last_error(pcm_));
        if (connected_) api_.client_command(client_, instance_id_.c_str(), OMNI_PCM_COMMAND_NEXT);
        reset_stream_state(&ring);
        eof_advanced_ = false;
        return;
    }

    if (!api_.format_ready(pcm_)) return;

    // 6. Get latest format info
    api_.info(pcm_, &info_);
    pending_channels_ = info_.channels > 0 ? info_.channels : 2;

    // 7. Report audible cursor (lets backend know how far the game consumed)
    update_audible_from_ring(ring);

    // 8. Pull PCM from shared memory → resample → push to ring buffer.
    //    Throttle local pre-buffering to ~100ms (matching Flutter's
    //    LOCAL_BUFFER_TARGET_DIVISOR=10) so the ring buffer does not
    //    bypass the server's target_latency control.
    constexpr std::size_t kFrameBytes = kOutChannels * sizeof(float);
    const int target_frames = std::max(kFmodRate / kLocalBufferTargetDivisor,
                                       kLocalBufferMinFrames);
    const std::size_t target_bytes = static_cast<std::size_t>(target_frames) * kFrameBytes;
    if (ring.readable() >= target_bytes) return;

    float out[kPumpFrames * kOutChannels];
    while (ring.readable() + sizeof(out) <= target_bytes
           && ring.writable() >= sizeof(out)) {
        int frames = produce_float_stereo(out, kPumpFrames);
        if (frames <= 0) break;
        const int64_t input_end =
            input_frame_base_ + static_cast<int64_t>(std::floor(resample_pos_));
        if (append_to_ring(ring, out, frames, input_end) <= 0) break;
    }

    update_audible_from_ring(ring);
    maybe_advance_on_complete(ring);
}

// ═══════════════════════════════════════════════════════════════════
//  Metadata queries
// ═══════════════════════════════════════════════════════════════════

TrackInfo OmniPcmSource::current_track() const {
    std::scoped_lock lk{mutex_};
    return track_;
}

PlaybackState OmniPcmSource::playback_state() const noexcept {
    std::scoped_lock lk{mutex_};
    if (!connected_ || !pcm_) return PlaybackState::stopped;
    if (!playing_) return PlaybackState::paused;
    return api_.format_ready(pcm_) ? PlaybackState::playing : PlaybackState::buffering;
}

AuthState OmniPcmSource::auth_state() const noexcept {
    std::scoped_lock lk{mutex_};
    return connected_ ? AuthState::authenticated : AuthState::error;
}

std::string OmniPcmSource::auth_instructions() const {
    return "Start OmniMixPlayer, then keep its backend running while Forza Horizon 6 is open.";
}

SourceCapabilities OmniPcmSource::capabilities() const noexcept {
    SourceCapabilities caps{};
    caps.seek     = true;
    caps.previous = true;
    caps.queue    = true;
    return caps;
}

// ═══════════════════════════════════════════════════════════════════
//  SDK event callback
// ═══════════════════════════════════════════════════════════════════

void OmniPcmSource::on_sdk_event(const OmniPcmEventInfo* evt, void* user_data) {
    if (!evt || !user_data) return;
    auto* self = static_cast<OmniPcmSource*>(user_data);
    // Push onto lock-free queue; drop if full (shouldn't happen in practice)
    event_queue_push(self->event_queue_, kEventQueueSize,
                     self->event_write_, self->event_read_, *evt);
}

void OmniPcmSource::handle_sdk_event(const OmniPcmEventInfo& evt) {
    // Update cached TrackInfo from backend status events
    std::string type{evt.type};
    if (type == "playback_status" || type == "track_change" || type == "play_state") {
        TrackInfo t{};
        if (evt.title[0])   t.title       = evt.title;
        else                t.title       = "OmniMixPlayer";
        if (evt.artist[0])  t.artist      = evt.artist;
        else                t.artist      = playing_ ? "Playing" : "Idle";
        t.album      = evt.album_id;
        t.duration_ms = static_cast<uint64_t>(std::max(0.0f, evt.duration) * 1000.0f);
        t.position_ms = static_cast<uint64_t>(std::max(0.0f, evt.position) * 1000.0f);
        track_ = std::move(t);
        playing_ = (evt.state == OMNI_PCM_STATE_PLAYING);
    }
}

void OmniPcmSource::process_pending_events() {
    OmniPcmEventInfo evt{};
    while (event_queue_pop(event_queue_, kEventQueueSize,
                           event_write_, event_read_, evt)) {
        handle_sdk_event(evt);
    }
}

// ═══════════════════════════════════════════════════════════════════
//  SDK loading & backend connection
// ═══════════════════════════════════════════════════════════════════

bool OmniPcmSource::load_api() {
    if (api_.ready()) return true;
    api_.dll = LoadLibraryW(L"OmniPcmShared.dll");
    if (!api_.dll) {
        log::error("[omni] failed to load OmniPcmShared.dll ({})", GetLastError());
        return false;
    }

    auto proc = [&](auto& target, const char* name) {
        target = reinterpret_cast<std::remove_reference_t<decltype(target)>>(
            GetProcAddress(api_.dll, name));
        if (!target) log::error("[omni] missing OmniPcmShared export: {}", name);
    };

    // Shared memory API
    proc(api_.open_utf8,      "OmniPcm_OpenUtf8");
    proc(api_.close,          "OmniPcm_Close");
    proc(api_.is_open,        "OmniPcm_IsOpen");
    proc(api_.last_error,     "OmniPcm_GetLastError");
    proc(api_.snapshot,       "OmniPcm_GetSnapshot");
    proc(api_.info,           "OmniPcm_GetInfo");
    proc(api_.bind_current,   "OmniPcm_BindCurrentStream");
    proc(api_.format_ready,   "OmniPcm_IsFormatReady");
    proc(api_.has_error,      "OmniPcm_HasError");
    proc(api_.complete,       "OmniPcm_IsPlaybackComplete");
    proc(api_.read_frames,    "OmniPcm_ReadFrames");
    proc(api_.request_seek,   "OmniPcm_RequestSeek");
    proc(api_.set_audible,    "OmniPcm_SetAudibleCursor");

    // Control-plane client API
    proc(api_.client_create,       "OmniPcmClient_Create");
    proc(api_.client_destroy,      "OmniPcmClient_Destroy");
    proc(api_.client_last_error,   "OmniPcmClient_GetLastError");
    proc(api_.client_connect,      "OmniPcmClient_ConnectInstance");
    proc(api_.client_heartbeat,    "OmniPcmClient_Heartbeat");
    proc(api_.client_disconnect,   "OmniPcmClient_DisconnectInstance");
    proc(api_.client_status,       "OmniPcmClient_GetStatus");
    proc(api_.client_command,      "OmniPcmClient_PlaybackCommand");
    proc(api_.client_play,         "OmniPcmClient_Play");
    proc(api_.client_seek,         "OmniPcmClient_Seek");
    proc(api_.client_start_events,        "OmniPcmClient_StartEvents");
    proc(api_.client_stop_events,         "OmniPcmClient_StopEvents");
    proc(api_.client_get_target_latency,  "OmniPcmClient_GetTargetLatency");
    proc(api_.client_set_target_latency,  "OmniPcmClient_SetTargetLatency");

    return api_.ready();
}

bool OmniPcmSource::connect_backend() {
    if (!api_.ready()) return false;
    if (!client_) {
        OmniPcmClientConfig cfg{};
        cfg.timeout_ms = 2500;
        // port=0 → SDK discovers port automatically via omnimix_port.txt
        client_ = api_.client_create(&cfg);
        if (!client_) return false;
    }

    OmniPcmConnectOptions options{};
    options.client_id        = client_id_.empty() ? nullptr : client_id_.c_str();
    options.mod_id           = "forza_horizon_6";
    options.game_name        = "Forza Horizon 6";
    options.display_name     = "Forza Horizon 6";
    options.kind             = OMNI_PCM_INSTANCE_KIND_GAME_MOD;
    options.capability_flags = kFlutterPlayerCapabilities;

    OmniPcmConnectionInfo info{};
    int result = api_.client_connect(client_, &options, &info);
    if (result != OMNI_PCM_OK) {
        log::warn("[omni] backend connect failed: {}", api_.client_last_error(client_));
        return false;
    }

    instance_id_ = info.instance_id[0] ? info.instance_id : client_id_;
    shared_memory_name_ = "Global\\OmniMixPlayer_PCM_" + instance_id_;
    log::info("[omni] connected backend, instance={}, sharedMemory={}",
              instance_id_, shared_memory_name_);

    // Ensure target_latency is never stuck at 0 (proto3 default).
    // Flutter sets a sane floor; apply the same guard on first connect.
    float current_latency = 0.0f;
    if (api_.client_get_target_latency &&
        api_.client_get_target_latency(client_, instance_id_.c_str(), &current_latency) == OMNI_PCM_OK) {
        if (current_latency < kMinTargetLatency) {
            log::info("[omni] target_latency {:.3f} < {:.3f}, clamping to floor",
                      current_latency, kMinTargetLatency);
            api_.client_set_target_latency(client_, instance_id_.c_str(), kMinTargetLatency);
        } else {
            log::info("[omni] target_latency = {:.3f}", current_latency);
        }
    }

    return true;
}

bool OmniPcmSource::open_shared_memory() {
    if (!api_.ready() || shared_memory_name_.empty()) return false;
    close_shared_memory();
    pcm_ = api_.open_utf8(shared_memory_name_.c_str());
    if (!pcm_ || !api_.is_open(pcm_)) {
        log::warn("[omni] failed to open shared memory '{}': {}",
                  shared_memory_name_, pcm_ ? api_.last_error(pcm_) : "null handle");
        close_shared_memory();
        return false;
    }
    api_.bind_current(pcm_);
    api_.info(pcm_, &info_);
    OmniPcmSnapshot snap{};
    if (api_.snapshot(pcm_, &snap) == OMNI_PCM_OK) {
        snapshot_ = snap;
        last_seen_seek_generation_ = snap.seek_generation;
        current_stream_id_ = snap.stream_id;
        current_uuid_ = snap.current_uuid;
    }
    log::info("[omni] opened shared memory '{}'", shared_memory_name_);
    return true;
}

void OmniPcmSource::close_shared_memory() noexcept {
    if (pcm_ && api_.close) api_.close(pcm_);
    pcm_ = nullptr;
}

void OmniPcmSource::heartbeat_if_due() {
    auto now = std::chrono::steady_clock::now();
    if (!connected_ || now < next_heartbeat_) return;
    int alive = 0;
    if (api_.client_heartbeat(client_, instance_id_.c_str(), &alive) != OMNI_PCM_OK || !alive) {
        connected_ = false;
        close_shared_memory();
        next_heartbeat_ = now + std::chrono::seconds(10);
        return;
    }
    // Fallback: poll status every heartbeat tick as a safety net in case
    // SDK events are delayed or dropped. Only backfills metadata; once
    // events have populated track_ the poll becomes a no-op for title/artist.
    OmniPcmPlaybackStatusInfo status{};
    if (api_.client_status(client_, instance_id_.c_str(), &status) == OMNI_PCM_OK) {
        playing_ = status.is_playing != 0;
        if (track_.title.empty() && status.title[0]) {
            TrackInfo t{};
            t.title       = status.title;
            t.artist      = status.artist;
            t.duration_ms = static_cast<uint64_t>(std::max(0.0f, status.duration) * 1000.0f);
            t.position_ms = static_cast<uint64_t>(std::max(0.0f, status.position) * 1000.0f);
            track_ = std::move(t);
        }
    }
    next_heartbeat_ = now + std::chrono::seconds(10);
}

// ═══════════════════════════════════════════════════════════════════
//  Stream state management
// ═══════════════════════════════════════════════════════════════════

void OmniPcmSource::reset_stream_state(RingBuffer* ring) {
    pending_input_.clear();
    pending_read_ofs_ = 0;
    segments_.clear();
    resample_pos_ = 0.0;
    input_frame_base_ = snapshot_.read_cursor;
    last_audible_input_ = input_frame_base_;
    if (ring) ring->drain();
}

void OmniPcmSource::update_audible_from_ring(const RingBuffer& ring) {
    const auto read = ring.read_position();
    while (!segments_.empty() && segments_.front().ring_end <= read) {
        last_audible_input_ = std::max(last_audible_input_, segments_.front().input_end);
        segments_.pop_front();
    }
    if (pcm_ && api_.set_audible && last_audible_input_ > 0)
        api_.set_audible(pcm_, last_audible_input_, 0);
}

void OmniPcmSource::maybe_advance_on_complete(const RingBuffer& ring) {
    if (!pcm_ || eof_advanced_ || ring.readable() > 0) return;
    const int rate = info_.sample_rate > 0 ? info_.sample_rate : kFmodRate;
    if (api_.complete(pcm_, rate / 4)) {
        eof_advanced_ = true;
        log::info("[omni] playback complete; server will auto-advance");
    }
}

// ═══════════════════════════════════════════════════════════════════
//  PCM pipeline internals
// ═══════════════════════════════════════════════════════════════════

bool OmniPcmSource::ensure_pending_input(int min_frames) {
    if (!pcm_ || !api_.read_frames) return false;
    const int channels = std::max(1, pending_channels_);
    int pending_frames = static_cast<int>((pending_input_.size() - pending_read_ofs_) / channels);
    while (pending_frames < min_frames) {
        const int want = 1024;
        const std::size_t need = static_cast<std::size_t>(want) * channels;
        if (read_buf_.size() < need) read_buf_.resize(need);
        int64_t got = api_.read_frames(pcm_, read_buf_.data(), want);
        if (got <= 0) break;
        pending_input_.insert(pending_input_.end(), read_buf_.begin(),
                              read_buf_.begin() + static_cast<std::ptrdiff_t>(got * channels));
        pending_frames += static_cast<int>(got);
    }
    return pending_frames >= min_frames;
}

int OmniPcmSource::produce_float_stereo(float* out, int max_frames) {
    if (!out || max_frames <= 0) return 0;
    const int in_rate  = info_.sample_rate > 0 ? info_.sample_rate : kFmodRate;
    const int channels = std::max(1, pending_channels_);
    const double step  = static_cast<double>(in_rate) / static_cast<double>(kFmodRate);
    int produced = 0;

    while (produced < max_frames) {
        const int need = static_cast<int>(std::floor(resample_pos_)) + 2;
        if (!ensure_pending_input(need)) break;
        const int pending_frames = static_cast<int>((pending_input_.size() - pending_read_ofs_) / channels);
        const int i0 = static_cast<int>(std::floor(resample_pos_));
        if (i0 + 1 >= pending_frames) break;
        const double frac = resample_pos_ - i0;

        auto sample = [&](int frame, int ch) {
            ch = std::min(ch, channels - 1);
            return pending_input_[pending_read_ofs_ + static_cast<std::size_t>(frame * channels + ch)];
        };
        float l0 = sample(i0, 0), l1 = sample(i0 + 1, 0);
        float r0 = channels > 1 ? sample(i0, 1) : l0;
        float r1 = channels > 1 ? sample(i0 + 1, 1) : l1;

        out[produced * 2 + 0] = static_cast<float>(l0 + (l1 - l0) * frac);
        out[produced * 2 + 1] = static_cast<float>(r0 + (r1 - r0) * frac);
        ++produced;
        resample_pos_ += step;
    }

    trim_pending_input();
    return produced;
}

int OmniPcmSource::append_to_ring(RingBuffer& ring, const float* stereo, int frames,
                                  int64_t input_end) {
    if (!stereo || frames <= 0) return 0;
    const std::size_t bytes = static_cast<std::size_t>(frames) * 2 * sizeof(float);
    const std::size_t before = ring.write_position();
    const std::size_t wrote = ring.write(stereo, bytes);
    if (wrote == 0) return 0;
    segments_.push_back({before + wrote, input_end});
    return static_cast<int>(wrote / (2 * sizeof(float)));
}

void OmniPcmSource::trim_pending_input() {
    const int channels = std::max(1, pending_channels_);
    const int drop = static_cast<int>(std::floor(resample_pos_));
    if (drop <= 0) return;
    const std::size_t samples = static_cast<std::size_t>(drop) * channels;
    pending_read_ofs_ += samples;
    input_frame_base_ += drop;
    resample_pos_ -= drop;

    if (pending_read_ofs_ >= pending_input_.size() - pending_read_ofs_) {
        if (pending_read_ofs_ >= pending_input_.size()) {
            pending_input_.clear();
        } else {
            pending_input_.erase(pending_input_.begin(),
                                 pending_input_.begin() + pending_read_ofs_);
        }
        pending_read_ofs_ = 0;
    }
}

} // namespace fh6::sources
