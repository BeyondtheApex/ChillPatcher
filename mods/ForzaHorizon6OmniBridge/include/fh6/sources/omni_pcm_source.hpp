#pragma once

#include "fh6/audio_source.hpp"
#include "omni_pcm_shared.h"

#include <windows.h>

#include <chrono>
#include <cstdint>
#include <deque>
#include <mutex>
#include <string>
#include <vector>

namespace fh6::sources {

class OmniPcmSource final : public IAudioSource {
public:
    explicit OmniPcmSource(std::string client_id = "fh6");
    ~OmniPcmSource() override;

    std::string_view name() const noexcept override { return "omnimix"; }
    std::string_view display_name() const noexcept override { return "OmniMixPlayer"; }

    bool initialize() override;
    void shutdown() noexcept override;

    void play() override;
    void pause() override;
    void stop() override;
    void next() override;
    void previous() override;
    void seek(uint64_t ms) override;
    bool skip_next() override;
    bool restart_current() override;
    void pump(RingBuffer& ring) override;

    TrackInfo current_track() const override;
    PlaybackState playback_state() const noexcept override;
    AuthState auth_state() const noexcept override;
    std::string auth_instructions() const override;
    SourceCapabilities capabilities() const noexcept override;

private:
    using OpenUtf8Fn = OmniPcmHandle (*)(const char*);
    using CloseFn = void (*)(OmniPcmHandle);
    using IsOpenFn = int (*)(OmniPcmHandle);
    using GetLastErrorFn = const char* (*)(OmniPcmHandle);
    using GetSnapshotFn = int (*)(OmniPcmHandle, OmniPcmSnapshot*);
    using GetInfoFn = int (*)(OmniPcmHandle, OmniPcmInfo*);
    using BindCurrentStreamFn = int (*)(OmniPcmHandle);
    using IsFormatReadyFn = int (*)(OmniPcmHandle);
    using HasErrorFn = int (*)(OmniPcmHandle);
    using IsPlaybackCompleteFn = int (*)(OmniPcmHandle, int64_t);
    using ReadFramesFn = int64_t (*)(OmniPcmHandle, float*, int32_t);
    using RequestSeekFn = int (*)(OmniPcmHandle, int64_t);
    using SetAudibleCursorFn = int (*)(OmniPcmHandle, int64_t, int);
    using ClientCreateFn = OmniPcmClientHandle (*)(const OmniPcmClientConfig*);
    using ClientDestroyFn = void (*)(OmniPcmClientHandle);
    using ClientLastErrorFn = const char* (*)(OmniPcmClientHandle);
    using ClientGetPortFn = int32_t (*)(OmniPcmClientHandle);
    using ClientConnectFn = int (*)(OmniPcmClientHandle, const OmniPcmConnectOptions*, OmniPcmConnectionInfo*);
    using ClientHeartbeatFn = int (*)(OmniPcmClientHandle, const char*, int*);
    using ClientDisconnectFn = int (*)(OmniPcmClientHandle, const char*);
    using ClientGetStatusFn = int (*)(OmniPcmClientHandle, const char*, OmniPcmPlaybackStatusInfo*);
    using ClientPlaybackCommandFn = int (*)(OmniPcmClientHandle, const char*, int32_t);
    using ClientPlayFn = int (*)(OmniPcmClientHandle, const char*, const char*);
    using ClientSeekFn = int (*)(OmniPcmClientHandle, const char*, float);

    struct Api {
        HMODULE dll = nullptr;
        OpenUtf8Fn open_utf8 = nullptr;
        CloseFn close = nullptr;
        IsOpenFn is_open = nullptr;
        GetLastErrorFn last_error = nullptr;
        GetSnapshotFn snapshot = nullptr;
        GetInfoFn info = nullptr;
        BindCurrentStreamFn bind_current = nullptr;
        IsFormatReadyFn format_ready = nullptr;
        HasErrorFn has_error = nullptr;
        IsPlaybackCompleteFn complete = nullptr;
        ReadFramesFn read_frames = nullptr;
        RequestSeekFn request_seek = nullptr;
        SetAudibleCursorFn set_audible = nullptr;
        ClientCreateFn client_create = nullptr;
        ClientDestroyFn client_destroy = nullptr;
        ClientLastErrorFn client_last_error = nullptr;
        ClientGetPortFn client_get_port = nullptr;
        ClientConnectFn client_connect = nullptr;
        ClientHeartbeatFn client_heartbeat = nullptr;
        ClientDisconnectFn client_disconnect = nullptr;
        ClientGetStatusFn client_status = nullptr;
        ClientPlaybackCommandFn client_command = nullptr;
        ClientPlayFn client_play = nullptr;
        ClientSeekFn client_seek = nullptr;

        bool ready() const noexcept;
    };

    struct Segment {
        std::size_t ring_end = 0;
        int64_t input_end = 0;
    };

    bool load_api();
    bool connect_backend();
    bool open_shared_memory();
    void close_shared_memory() noexcept;
    void heartbeat_if_due();
    void refresh_status_if_due(bool force = false);
    void reset_stream_state(RingBuffer* ring = nullptr);
    void update_audible_from_ring(const RingBuffer& ring);
    void maybe_advance_on_complete(const RingBuffer& ring);

    bool discover_port();

    bool ensure_pending_input(int min_frames);
    int produce_float_stereo(float* out, int max_frames);
    int append_to_ring(RingBuffer& ring, const float* stereo, int frames, int64_t input_end);
    void trim_pending_input();

    mutable std::mutex mutex_;
    Api api_;
    OmniPcmHandle pcm_ = nullptr;
    OmniPcmClientHandle client_ = nullptr;
    std::string client_id_;
    std::string instance_id_;
    std::string shared_memory_name_;
    uint16_t port_ = 17890;
    bool connected_ = false;
    bool playing_ = false;
    bool eof_advanced_ = false;
    std::string current_uuid_;
    TrackInfo track_;

    OmniPcmInfo info_{};
    OmniPcmSnapshot snapshot_{};
    std::vector<float> pending_input_;
    std::size_t pending_read_ofs_ = 0;
    std::vector<float> read_buf_;
    int pending_channels_ = 2;
    int64_t input_frame_base_ = 0;
    double resample_pos_ = 0.0;
    std::deque<Segment> segments_;
    int64_t last_audible_input_ = 0;
    int64_t last_seen_seek_generation_ = 0;
    int64_t current_stream_id_ = 0;

    std::chrono::steady_clock::time_point next_connect_attempt_{};
    std::chrono::steady_clock::time_point next_heartbeat_{};
    std::chrono::steady_clock::time_point next_status_refresh_{};
};

} // namespace fh6::sources

