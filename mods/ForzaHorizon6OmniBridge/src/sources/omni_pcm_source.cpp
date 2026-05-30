#include "fh6/sources/omni_pcm_source.hpp"
#include "fh6/log.hpp"
#include "fh6/ring_buffer.hpp"

#include <winhttp.h>
#include <shellapi.h>

#include <algorithm>
#include <cctype>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <type_traits>

#pragma comment(lib, "winhttp.lib")

namespace fh6::sources {

namespace {
constexpr int kFmodRate = 48000;
constexpr int kOutChannels = 2;
constexpr int kFrameBytes = kOutChannels * (int)sizeof(float);
constexpr int kPumpFrames = 2048;

std::wstring read_text_file_w(const std::filesystem::path& path) {
    std::wifstream in{path};
    if (!in) return {};
    std::wstring text;
    std::getline(in, text);
    return text;
}

uint16_t parse_port(std::wstring_view text) {
    while (!text.empty() && iswspace(text.front())) text.remove_prefix(1);
    while (!text.empty() && iswspace(text.back())) text.remove_suffix(1);
    int value = 0;
    try { value = std::stoi(std::wstring{text}); } catch (...) { value = 0; }
    return value > 0 && value <= 65535 ? static_cast<uint16_t>(value) : 0;
}

std::filesystem::path public_omni_dir() {
    wchar_t buf[MAX_PATH]{};
    DWORD n = GetEnvironmentVariableW(L"PUBLIC", buf, MAX_PATH);
    if (n == 0 || n >= MAX_PATH) return std::filesystem::temp_directory_path() / "OmniMixPlayer";
    return std::filesystem::path{buf} / "OmniMixPlayer";
}

/// Resolve the Unix Domain Socket path used by the backend (fallback IPC).
std::filesystem::path socket_path() {
    return public_omni_dir() / "omnimix.sock";
}

/// Check if the socket file exists on disk.
bool socket_file_exists() {
    auto path = socket_path();
    DWORD attr = GetFileAttributesW(path.c_str());
    return attr != INVALID_FILE_ATTRIBUTES;
}

/// Try to start the OmniMixPlayer backend (best-effort).
void try_start_backend() {
    // 1. Try Windows Service
    SC_HANDLE scm = OpenSCManagerW(nullptr, nullptr, SC_MANAGER_CONNECT);
    if (scm) {
        SC_HANDLE svc = OpenServiceW(scm, L"OmniMixPlayerBackend", SERVICE_QUERY_STATUS | SERVICE_START);
        if (svc) {
            SERVICE_STATUS status{};
            if (QueryServiceStatus(svc, &status)) {
                if (status.dwCurrentState == SERVICE_STOPPED) {
                    log::info("[omni] service 'OmniMixPlayerBackend' is stopped; starting...");
                    StartServiceW(svc, 0, nullptr);
                }
            }
            CloseServiceHandle(svc);
        } else {
            // 2. Service not installed — try launching the exe directly
            log::info("[omni] service not installed; trying direct exe launch...");
            std::filesystem::path exeDir;
            // Look for the exe next to the bridge DLL (typical layout)
            wchar_t modPath[MAX_PATH]{};
            if (GetModuleFileNameW(nullptr, modPath, MAX_PATH)) {
                exeDir = std::filesystem::path{modPath}.parent_path();
            }
            auto exePath = exeDir / "OmniMixPlayer.Backend.exe";
            if (std::filesystem::exists(exePath)) {
                SHELLEXECUTEINFOW sei{sizeof(sei)};
                sei.fMask = SEE_MASK_NOASYNC | SEE_MASK_NOCLOSEPROCESS;
                sei.lpVerb = L"open";
                sei.lpFile = exePath.c_str();
                sei.nShow = SW_HIDE;
                if (ShellExecuteExW(&sei) && sei.hProcess) {
                    log::info("[omni] launched OmniMixPlayer.Backend.exe");
                    // Don't wait — just fire and forget
                    CloseHandle(sei.hProcess);
                } else {
                    log::warn("[omni] failed to launch OmniMixPlayer.Backend.exe (error {})",
                              GetLastError());
                }
            } else {
                log::warn("[omni] OmniMixPlayer.Backend.exe not found at {}",
                          std::filesystem::absolute(exePath).string());
            }
        }
        CloseServiceHandle(scm);
    }
}
} // namespace

bool OmniPcmSource::Api::ready() const noexcept {
    return dll && open_utf8 && close && is_open && last_error && snapshot && info &&
           bind_current && format_ready && has_error && complete && read_frames &&
           request_seek && set_audible;
}

// Forward declaration (must precede constructor use)
static std::string read_instance_id();

OmniPcmSource::OmniPcmSource(std::string client_id)
    : client_id_{client_id.empty() || client_id == "fh6" ? read_instance_id() : std::move(client_id)} {}

static std::string read_instance_id() {
    wchar_t exePath[MAX_PATH]{};
    if (!GetModuleFileNameW(nullptr, exePath, MAX_PATH)) return "fh6";
    auto exeDir = std::filesystem::path{exePath}.parent_path();
    auto idFile = exeDir / ".omnimix_instance_id";
    auto text = read_text_file_w(idFile);
    if (text.empty()) return "fh6";
    // Convert wide to narrow
    int len = WideCharToMultiByte(CP_UTF8, 0, text.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (len <= 0) return "fh6";
    std::string result(len - 1, '\0');
    WideCharToMultiByte(CP_UTF8, 0, text.c_str(), -1, &result[0], len, nullptr, nullptr);
    while (!result.empty() && isspace(static_cast<unsigned char>(result.back()))) result.pop_back();
    log::info("[omni] using instance ID from .omnimix_instance_id: {}", result);
    return result;
}

OmniPcmSource::~OmniPcmSource() {
    shutdown();
}

bool OmniPcmSource::initialize() {
    std::scoped_lock lk{mutex_};
    if (!load_api()) return false;

    // Best-effort: try to start the backend before discovery
    try_start_backend();

    if (!discover_port()) log::warn("[omni] port discovery failed; falling back to {}", port_);
    connected_ = connect_backend();
    if (connected_) open_shared_memory();
    next_heartbeat_ = std::chrono::steady_clock::now();
    next_status_refresh_ = std::chrono::steady_clock::now();
    return api_.ready();
}

void OmniPcmSource::shutdown() noexcept {
    std::scoped_lock lk{mutex_};
    close_shared_memory();
    if (connected_) {
        std::string ignored;
        http_post(L"/api/instances/" + widen(client_id_) + L"/disconnect", "{}", ignored);
    }
    connected_ = false;
    if (api_.dll) {
        FreeLibrary(api_.dll);
        api_ = {};
    }
}

void OmniPcmSource::play() {
    std::scoped_lock lk{mutex_};
    if (!connected_) connect_backend();
    command_post(L"/api/instances/" + widen(client_id_) + L"/play");
    playing_ = true;
    refresh_status_if_due(true);
}

void OmniPcmSource::pause() {
    std::scoped_lock lk{mutex_};
    command_post(L"/api/instances/" + widen(client_id_) + L"/pause");
    playing_ = false;
}

void OmniPcmSource::stop() {
    std::scoped_lock lk{mutex_};
    command_post(L"/api/instances/" + widen(client_id_) + L"/pause");
    playing_ = false;
}

void OmniPcmSource::next() {
    std::scoped_lock lk{mutex_};
    command_post(L"/api/instances/" + widen(client_id_) + L"/next");
    reset_stream_state();
    eof_advanced_ = false;
}

void OmniPcmSource::previous() {
    std::scoped_lock lk{mutex_};
    command_post(L"/api/instances/" + widen(client_id_) + L"/prev");
    reset_stream_state();
    eof_advanced_ = false;
}

void OmniPcmSource::seek(uint64_t ms) {
    std::scoped_lock lk{mutex_};
    const double seconds = static_cast<double>(ms) / 1000.0;
    std::string body = "{\"position\":" + std::to_string(seconds) + "}";
    std::string ignored;
    http_post(L"/api/instances/" + widen(client_id_) + L"/seek", body, ignored);
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

void OmniPcmSource::pump(RingBuffer& ring) {
    std::scoped_lock lk{mutex_};
    const auto now = std::chrono::steady_clock::now();

    if (!connected_ && now >= next_connect_attempt_) {
        connected_ = connect_backend();
        next_connect_attempt_ = now + std::chrono::seconds(5);
    }
    if (connected_ && !pcm_) open_shared_memory();
    heartbeat_if_due();
    refresh_status_if_due(false);

    if (!pcm_ || !api_.is_open(pcm_) || !playing_) return;

    // Read snapshot FIRST so we can detect stream transitions and errors
    // even when the new stream's format isn't ready yet. The previous
    // ordering checked format_ready before snapshot/has_error, which
    // caused a permanent stall whenever a new stream never became
    // format-ready (e.g. decoder failure on the backend).
    OmniPcmSnapshot snap{};
    if (api_.snapshot(pcm_, &snap) == OMNI_PCM_OK) {
        snapshot_ = snap;
        bool stream_changed = (current_stream_id_ != snap.stream_id || current_uuid_ != snap.current_uuid);
        bool seek_detected = (last_seen_seek_generation_ != snap.seek_generation);

        if (stream_changed || seek_detected) {
            if (stream_changed) {
                current_stream_id_ = snap.stream_id;
                current_uuid_ = snap.current_uuid;
                api_.bind_current(pcm_);
            }
            last_seen_seek_generation_ = snap.seek_generation;
            reset_stream_state(&ring);
            eof_advanced_ = false;
        }
    }

    // Check for stream errors regardless of format readiness so we can
    // log failures and attempt automatic recovery.
    if (api_.has_error(pcm_)) {
        log::warn("[omni] shared memory stream reported an error: {} — attempting skip",
                  api_.last_error(pcm_));
        command_post(L"/api/instances/" + widen(client_id_) + L"/next");
        reset_stream_state(&ring);
        eof_advanced_ = false;
        return;
    }

    if (!api_.format_ready(pcm_)) return;

    api_.info(pcm_, &info_);
    pending_channels_ = info_.channels > 0 ? info_.channels : 2;

    update_audible_from_ring(ring);

    float out[kPumpFrames * kOutChannels];
    while (ring.writable() >= sizeof(out)) {
        int frames = produce_float_stereo(out, kPumpFrames);
        if (frames <= 0) break;
        const int64_t input_end =
            input_frame_base_ + static_cast<int64_t>(std::floor(resample_pos_));
        if (append_to_ring(ring, out, frames, input_end) <= 0) break;
    }

    update_audible_from_ring(ring);
    maybe_advance_on_complete(ring);
}

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
    caps.seek = true;
    caps.previous = true;
    caps.queue = true;
    return caps;
}

bool OmniPcmSource::load_api() {
    if (api_.ready()) return true;
    api_.dll = LoadLibraryW(L"OmniPcmShared.dll");
    if (!api_.dll) {
        log::error("[omni] failed to load OmniPcmShared.dll ({})", GetLastError());
        return false;
    }

    auto proc = [&](auto& target, const char* name) {
        target = reinterpret_cast<std::remove_reference_t<decltype(target)>>(GetProcAddress(api_.dll, name));
        if (!target) log::error("[omni] missing OmniPcmShared export {}", name);
    };

    proc(api_.open_utf8, "OmniPcm_OpenUtf8");
    proc(api_.close, "OmniPcm_Close");
    proc(api_.is_open, "OmniPcm_IsOpen");
    proc(api_.last_error, "OmniPcm_GetLastError");
    proc(api_.snapshot, "OmniPcm_GetSnapshot");
    proc(api_.info, "OmniPcm_GetInfo");
    proc(api_.bind_current, "OmniPcm_BindCurrentStream");
    proc(api_.format_ready, "OmniPcm_IsFormatReady");
    proc(api_.has_error, "OmniPcm_HasError");
    proc(api_.complete, "OmniPcm_IsPlaybackComplete");
    proc(api_.read_frames, "OmniPcm_ReadFrames");
    proc(api_.request_seek, "OmniPcm_RequestSeek");
    proc(api_.set_audible, "OmniPcm_SetAudibleCursor");
    return api_.ready();
}

bool OmniPcmSource::connect_backend() {
    std::string body;
    const std::string req = "{\"clientId\":\"" + client_id_ +
                            "\",\"role\":\"audio\",\"mode\":\"server\"}";
    if (!http_post(L"/api/instances/connect", req, body)) return false;

    auto shm = json_string(body, "sharedMemoryName");
    if (shm.empty()) shm = "Global\\OmniMixPlayer_PCM_" + client_id_;
    shared_memory_name_ = std::move(shm);
    log::info("[omni] connected backend on port {}, sharedMemory={}", port_, shared_memory_name_);
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
    command_post(L"/api/instances/" + widen(client_id_) + L"/heartbeat");
    next_heartbeat_ = now + std::chrono::seconds(10);
}

void OmniPcmSource::refresh_status_if_due(bool force) {
    auto now = std::chrono::steady_clock::now();
    if (!force && now < next_status_refresh_) return;
    std::string body;
    if (http_get(L"/api/instances/" + widen(client_id_) + L"/status", body)) {
        playing_ = json_bool(body, "IsPlaying", json_bool(body, "isPlaying", playing_));
        const std::string track_obj = [&]() {
            auto obj = json_object(body, "CurrentTrack");
            if (!obj.empty()) return obj;
            return json_object(body, "currentTrack");
        }();
        const std::string_view track_view =
            track_obj.empty() ? std::string_view{body} : std::string_view{track_obj};
        TrackInfo t{};
        t.title = json_string(track_view, "title");
        t.artist = json_string(track_view, "artist");
        t.duration_ms = static_cast<uint64_t>(json_number(track_view, "duration", 0) * 1000.0);
        t.position_ms = static_cast<uint64_t>(json_number(body, "Position",
            json_number(body, "position", 0)) * 1000.0);
        if (t.title.empty()) t.title = "OmniMixPlayer";
        if (t.artist.empty()) t.artist = playing_ ? "Playing" : "Idle";
        track_ = std::move(t);
    }
    next_status_refresh_ = now + std::chrono::milliseconds(500);
}

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
        log::info("[omni] playback complete; server-managed instance will advance");
    }
}

bool OmniPcmSource::http_get(const std::wstring& path, std::string& body) {
    return http_request(L"GET", path, {}, body);
}

bool OmniPcmSource::http_post(const std::wstring& path, std::string_view json_body,
                              std::string& body) {
    return http_request(L"POST", path, json_body, body);
}

bool OmniPcmSource::http_request(const wchar_t* verb, const std::wstring& path,
                                 std::string_view json_body, std::string& body) {
    body.clear();
    HINTERNET session = WinHttpOpen(L"FH6 OmniMix Bridge/1.0", WINHTTP_ACCESS_TYPE_NO_PROXY,
                                    WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!session) return false;
    HINTERNET connect = WinHttpConnect(session, L"127.0.0.1", port_, 0);
    if (!connect) {
        WinHttpCloseHandle(session);
        return false;
    }
    HINTERNET request = WinHttpOpenRequest(connect, verb, path.c_str(), nullptr,
                                           WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, 0);
    if (!request) {
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return false;
    }
    WinHttpSetTimeouts(request, 800, 800, 1200, 1200);

    const wchar_t* headers = L"Content-Type: application/json\r\n";
    DWORD body_size = static_cast<DWORD>(json_body.size());
    BOOL ok = WinHttpSendRequest(request, headers, (DWORD)-1,
                                 body_size ? (LPVOID)json_body.data() : WINHTTP_NO_REQUEST_DATA,
                                 body_size, body_size, 0);
    if (ok) ok = WinHttpReceiveResponse(request, nullptr);
    if (ok) {
        DWORD status = 0, status_size = sizeof(status);
        WinHttpQueryHeaders(request,
                            WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                            WINHTTP_HEADER_NAME_BY_INDEX, &status, &status_size,
                            WINHTTP_NO_HEADER_INDEX);
        ok = status >= 200 && status < 300;
    }
    if (ok) {
        for (;;) {
            DWORD avail = 0;
            if (!WinHttpQueryDataAvailable(request, &avail) || avail == 0) break;
            std::string chunk(avail, '\0');
            DWORD read = 0;
            if (!WinHttpReadData(request, chunk.data(), avail, &read) || read == 0) break;
            chunk.resize(read);
            body += chunk;
        }
    }

    WinHttpCloseHandle(request);
    WinHttpCloseHandle(connect);
    WinHttpCloseHandle(session);
    return ok == TRUE;
}

bool OmniPcmSource::command_post(const std::wstring& path) {
    std::string body;
    return http_post(path, "{}", body);
}

bool OmniPcmSource::discover_port() {
    std::vector<uint16_t> candidates;

    // 1. Port file from PUBLIC/OmniMixPlayer (default shared location)
    auto port_file = public_omni_dir() / "omnimix_port.txt";
    if (auto p = parse_port(read_text_file_w(port_file)); p) candidates.push_back(p);

    // 2. Port file from game's own directory (where Flutter writes during install)
    wchar_t exePath[MAX_PATH]{};
    if (GetModuleFileNameW(nullptr, exePath, MAX_PATH)) {
        auto exeDir = std::filesystem::path{exePath}.parent_path();
        auto gamePortFile = exeDir / "omnimix_port.txt";
        if (auto p = parse_port(read_text_file_w(gamePortFile)); p && p != (candidates.empty() ? 0 : candidates[0])) {
            candidates.insert(candidates.begin(), p);
        }
    }

    candidates.push_back(17890);
    for (uint16_t p = 17891; p < 17900; ++p) candidates.push_back(p);

    for (auto p : candidates) {
        port_ = p;
        std::string body;
        if (http_get(L"/api/health", body)) return true;
    }

    // TCP port scanning failed; check if socket file exists as fallback
    if (socket_file_exists()) {
        log::info("[omni] TCP scan failed but socket file exists; assuming port 17890");
        port_ = 17890;
        return true;
    }

    port_ = candidates.front();
    return false;
}

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
    const int in_rate = info_.sample_rate > 0 ? info_.sample_rate : kFmodRate;
    const int channels = std::max(1, pending_channels_);
    const double step = static_cast<double>(in_rate) / static_cast<double>(kFmodRate);
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
    // DSPBridge::read_callback (local overlay variant) expects float stereo
    // (8 bytes/frame). Write the float samples directly — no conversion needed.
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

    // Compact periodically so the offset doesn't grow unbounded.
    // A single erase is still O(remaining) but it runs rarely.
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

std::wstring OmniPcmSource::widen(std::string_view text) {
    if (text.empty()) return {};
    int len = MultiByteToWideChar(CP_UTF8, 0, text.data(), static_cast<int>(text.size()),
                                  nullptr, 0);
    std::wstring out(static_cast<std::size_t>(len), L'\0');
    MultiByteToWideChar(CP_UTF8, 0, text.data(), static_cast<int>(text.size()),
                        out.data(), len);
    return out;
}

std::string OmniPcmSource::json_string(std::string_view body, std::string_view key) {
    std::string needle = "\"" + std::string{key} + "\"";
    auto p = body.find(needle);
    if (p == std::string_view::npos) return {};
    p = body.find(':', p + needle.size());
    if (p == std::string_view::npos) return {};
    p = body.find('"', p);
    if (p == std::string_view::npos) return {};
    std::string out;
    bool esc = false;
    auto append_utf8 = [&](unsigned cp) {
        if (cp <= 0x7F) {
            out.push_back(static_cast<char>(cp));
        } else if (cp <= 0x7FF) {
            out.push_back(static_cast<char>(0xC0 | (cp >> 6)));
            out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
        } else {
            out.push_back(static_cast<char>(0xE0 | (cp >> 12)));
            out.push_back(static_cast<char>(0x80 | ((cp >> 6) & 0x3F)));
            out.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
        }
    };
    for (++p; p < body.size(); ++p) {
        char c = body[p];
        if (esc) {
            if (c == 'u' && p + 4 < body.size()) {
                unsigned cp = 0;
                bool ok = true;
                for (int i = 1; i <= 4; ++i) {
                    char h = body[p + i];
                    cp <<= 4;
                    if (h >= '0' && h <= '9') cp |= static_cast<unsigned>(h - '0');
                    else if (h >= 'a' && h <= 'f') cp |= static_cast<unsigned>(h - 'a' + 10);
                    else if (h >= 'A' && h <= 'F') cp |= static_cast<unsigned>(h - 'A' + 10);
                    else { ok = false; break; }
                }
                if (ok) {
                    append_utf8(cp);
                    p += 4;
                } else {
                    out.push_back(c);
                }
            } else {
                switch (c) {
                    case '"': out.push_back('"'); break;
                    case '\\': out.push_back('\\'); break;
                    case '/': out.push_back('/'); break;
                    case 'b': out.push_back('\b'); break;
                    case 'f': out.push_back('\f'); break;
                    case 'n': out.push_back('\n'); break;
                    case 'r': out.push_back('\r'); break;
                    case 't': out.push_back('\t'); break;
                    default: out.push_back(c); break;
                }
            }
            esc = false;
        } else if (c == '\\') {
            esc = true;
        } else if (c == '"') {
            break;
        } else {
            out.push_back(c);
        }
    }
    return out;
}

std::string OmniPcmSource::json_object(std::string_view body, std::string_view key) {
    std::string needle = "\"" + std::string{key} + "\"";
    auto p = body.find(needle);
    if (p == std::string_view::npos) return {};
    p = body.find(':', p + needle.size());
    if (p == std::string_view::npos) return {};
    p = body.find('{', p);
    if (p == std::string_view::npos) return {};

    int depth = 0;
    bool in_str = false;
    bool esc = false;
    const std::size_t start = p;
    for (; p < body.size(); ++p) {
        const char c = body[p];
        if (in_str) {
            if (esc) {
                esc = false;
            } else if (c == '\\') {
                esc = true;
            } else if (c == '"') {
                in_str = false;
            }
            continue;
        }
        if (c == '"') {
            in_str = true;
            continue;
        }
        if (c == '{') {
            depth++;
        } else if (c == '}') {
            depth--;
            if (depth == 0) {
                return std::string(body.substr(start, p - start + 1));
            }
        }
    }
    return {};
}

double OmniPcmSource::json_number(std::string_view body, std::string_view key, double fallback) {
    std::string needle = "\"" + std::string{key} + "\"";
    auto p = body.find(needle);
    if (p == std::string_view::npos) return fallback;
    p = body.find(':', p + needle.size());
    if (p == std::string_view::npos) return fallback;
    ++p;
    while (p < body.size() && std::isspace(static_cast<unsigned char>(body[p]))) ++p;
    char* end = nullptr;
    std::string tmp{body.substr(p, std::min<std::size_t>(body.size() - p, 64))};
    double value = std::strtod(tmp.c_str(), &end);
    return end && end != tmp.c_str() ? value : fallback;
}

bool OmniPcmSource::json_bool(std::string_view body, std::string_view key, bool fallback) {
    std::string needle = "\"" + std::string{key} + "\"";
    auto p = body.find(needle);
    if (p == std::string_view::npos) return fallback;
    p = body.find(':', p + needle.size());
    if (p == std::string_view::npos) return fallback;
    ++p;
    while (p < body.size() && std::isspace(static_cast<unsigned char>(body[p]))) ++p;
    if (body.substr(p, 4) == "true") return true;
    if (body.substr(p, 5) == "false") return false;
    return fallback;
}

} // namespace fh6::sources
