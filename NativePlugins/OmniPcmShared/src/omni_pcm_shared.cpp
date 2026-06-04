// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 ChillPatcher Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#include "omni_pcm_shared.h"

#include <algorithm>
#include <atomic>
#include <chrono>
#include <cstring>
#include <string>
#include <thread>

#ifdef _WIN32
#include <windows.h>
#endif

namespace {

constexpr int OFF_MAGIC = 0x00;
constexpr int OFF_VERSION = 0x08;
constexpr int OFF_SAMPLE_RATE = 0x0C;
constexpr int OFF_CHANNELS = 0x10;
constexpr int OFF_BYTES_PER_FRAME = 0x12;
constexpr int OFF_BUFFER_FRAMES = 0x14;
constexpr int OFF_LEGACY_PLAY_STATE = 0x18;
constexpr int OFF_SEEK_FRAME = 0x1C;
constexpr int OFF_FLAGS = 0x24;
constexpr int OFF_CURRENT_UUID = 0x28;
constexpr int OFF_WRITE_CURSOR = 0x68;
constexpr int OFF_READ_CURSOR = 0x70;
constexpr int OFF_STREAM_ID = 0x80;
constexpr int OFF_STREAM_STATE = 0x88;
constexpr int OFF_ERROR_CODE = 0x8C;
constexpr int OFF_TOTAL_FRAMES_HINT = 0x90;
constexpr int OFF_DECODED_TOTAL_FRAMES = 0x98;
constexpr int OFF_FINAL_WRITE_CURSOR = 0xA0;
constexpr int OFF_AUDIBLE_CURSOR = 0xA8;
constexpr int OFF_SEEK_GENERATION = 0xB0;
constexpr int OFF_LAST_UPDATE_TICK = 0xB8;
constexpr int OFF_FORMAT_GENERATION = 0xC0;
constexpr int HEADER_SIZE = 256;

template <typename T>
T read_value(const uint8_t* base, int offset) {
    T value{};
    std::memcpy(&value, base + offset, sizeof(T));
    std::atomic_thread_fence(std::memory_order_acquire);
    return value;
}

template <typename T>
void write_value(uint8_t* base, int offset, T value) {
    std::atomic_thread_fence(std::memory_order_release);
    std::memcpy(base + offset, &value, sizeof(T));
}

int64_t utc_ticks_now() {
    using namespace std::chrono;
    constexpr int64_t ticks_at_unix_epoch = 621355968000000000LL;
    auto now = system_clock::now().time_since_epoch();
    return ticks_at_unix_epoch + duration_cast<duration<int64_t, std::ratio<1, 10000000>>>(now).count();
}

struct OmniPcmContext {
#ifdef _WIN32
    HANDLE mapping = nullptr;
#endif
    uint8_t* ptr = nullptr;
    size_t mapped_size = 0;
    std::string last_error;
    std::string current_uuid_cache;
    std::string bound_uuid;
    int64_t bound_stream_id = 0;
    int64_t audible_base_frame = 0;
    int64_t last_seen_seek_generation = 0;

    void set_error(const char* message) {
        last_error = message ? message : "Unknown error";
    }
};

OmniPcmContext* ctx(OmniPcmHandle handle) {
    return static_cast<OmniPcmContext*>(handle);
}

bool validate(OmniPcmContext* c) {
    if (!c || !c->ptr) return false;
    return read_value<uint64_t>(c->ptr, OFF_MAGIC) == OMNI_PCM_MAGIC;
}

uint32_t flags(OmniPcmContext* c) {
    return read_value<uint32_t>(c->ptr, OFF_FLAGS);
}

int64_t effective_total(const OmniPcmSnapshot& s) {
    return s.decoded_total_frames > 0 ? s.decoded_total_frames : s.total_frames_hint;
}

bool same_stream(OmniPcmContext* c, const OmniPcmSnapshot& s) {
    if (c->bound_uuid.empty()) return true;
    if (std::strncmp(s.current_uuid, c->bound_uuid.c_str(), OMNI_PCM_UUID_BYTES) != 0) return false;
    return s.version < OMNI_PCM_VERSION_2 || c->bound_stream_id == 0 || s.stream_id == c->bound_stream_id;
}

int snapshot_internal(OmniPcmContext* c, OmniPcmSnapshot* out) {
    if (!validate(c) || !out) return OMNI_PCM_BAD_ARGUMENT;
    std::memset(out, 0, sizeof(*out));
    out->version = read_value<uint32_t>(c->ptr, OFF_VERSION);
    out->sample_rate = read_value<int32_t>(c->ptr, OFF_SAMPLE_RATE);
    out->channels = read_value<uint16_t>(c->ptr, OFF_CHANNELS);
    out->bytes_per_frame = read_value<uint16_t>(c->ptr, OFF_BYTES_PER_FRAME);
    out->buffer_frames = read_value<int32_t>(c->ptr, OFF_BUFFER_FRAMES);
    out->legacy_play_state = read_value<int32_t>(c->ptr, OFF_LEGACY_PLAY_STATE);
    out->flags = read_value<uint32_t>(c->ptr, OFF_FLAGS);
    out->write_cursor = read_value<int64_t>(c->ptr, OFF_WRITE_CURSOR);
    out->read_cursor = read_value<int64_t>(c->ptr, OFF_READ_CURSOR);
    out->stream_id = read_value<int64_t>(c->ptr, OFF_STREAM_ID);
    out->state = read_value<int32_t>(c->ptr, OFF_STREAM_STATE);
    out->error_code = read_value<int32_t>(c->ptr, OFF_ERROR_CODE);
    out->total_frames_hint = read_value<int64_t>(c->ptr, OFF_TOTAL_FRAMES_HINT);
    out->decoded_total_frames = read_value<int64_t>(c->ptr, OFF_DECODED_TOTAL_FRAMES);
    out->final_write_cursor = read_value<int64_t>(c->ptr, OFF_FINAL_WRITE_CURSOR);
    out->audible_cursor = read_value<int64_t>(c->ptr, OFF_AUDIBLE_CURSOR);
    out->seek_frame = read_value<int64_t>(c->ptr, OFF_SEEK_FRAME);
    out->seek_generation = read_value<int64_t>(c->ptr, OFF_SEEK_GENERATION);
    out->last_update_tick = read_value<int64_t>(c->ptr, OFF_LAST_UPDATE_TICK);
    out->format_generation = read_value<int32_t>(c->ptr, OFF_FORMAT_GENERATION);
    std::memcpy(out->current_uuid, c->ptr + OFF_CURRENT_UUID, OMNI_PCM_UUID_BYTES);
    out->current_uuid[OMNI_PCM_UUID_BYTES - 1] = '\0';
    return OMNI_PCM_OK;
}

void set_flag(OmniPcmContext* c, uint32_t flag, bool enabled) {
    auto* target = reinterpret_cast<volatile long*>(c->ptr + OFF_FLAGS);
    long old_value;
    long new_value;
    do {
        old_value = *target;
        new_value = enabled ? (old_value | static_cast<long>(flag)) : (old_value & ~static_cast<long>(flag));
    } while (InterlockedCompareExchange(target, new_value, old_value) != old_value);
    write_value<int64_t>(c->ptr, OFF_LAST_UPDATE_TICK, utc_ticks_now());
}

std::wstring utf8_to_wide(const char* text) {
    if (!text || !*text) return std::wstring();
#ifdef _WIN32
    int len = MultiByteToWideChar(CP_UTF8, 0, text, -1, nullptr, 0);
    if (len <= 0) return std::wstring();
    std::wstring out(static_cast<size_t>(len - 1), L'\0');
    MultiByteToWideChar(CP_UTF8, 0, text, -1, out.data(), len);
    return out;
#else
    return std::wstring();
#endif
}

} // namespace

OMNI_PCM_API OmniPcmHandle OmniPcm_Open(const wchar_t* map_name) {
    auto* c = new OmniPcmContext();
#ifdef _WIN32
    const wchar_t* name = (map_name && *map_name) ? map_name : OMNI_PCM_DEFAULT_MAP_NAME;
    c->mapping = OpenFileMappingW(FILE_MAP_WRITE, FALSE, name);
    if (!c->mapping) {
        std::wstring prefixed = L"AppContainerNamedObjects\\";
        prefixed += name;
        c->mapping = OpenFileMappingW(FILE_MAP_WRITE, FALSE, prefixed.c_str());
    }
    if (!c->mapping) {
        c->set_error("OpenFileMappingW failed");
        return c;
    }
    c->ptr = static_cast<uint8_t*>(MapViewOfFile(c->mapping, FILE_MAP_WRITE, 0, 0, 0));
    if (!c->ptr) {
        c->set_error("MapViewOfFile failed");
        CloseHandle(c->mapping);
        c->mapping = nullptr;
        return c;
    }
    if (!validate(c)) {
        c->set_error("Invalid shared memory magic");
    }
#else
    c->set_error("OmniPcmShared currently supports Windows named shared memory only");
#endif
    return c;
}

OMNI_PCM_API OmniPcmHandle OmniPcm_OpenUtf8(const char* map_name_utf8) {
    auto wide = utf8_to_wide(map_name_utf8);
    return OmniPcm_Open(wide.empty() ? nullptr : wide.c_str());
}

OMNI_PCM_API void OmniPcm_Close(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!c) return;
#ifdef _WIN32
    if (c->ptr) UnmapViewOfFile(c->ptr);
    if (c->mapping) CloseHandle(c->mapping);
#endif
    delete c;
}

OMNI_PCM_API int OmniPcm_IsOpen(OmniPcmHandle handle) {
    return validate(ctx(handle)) ? 1 : 0;
}

OMNI_PCM_API uint32_t OmniPcm_GetVersion(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    return validate(c) ? read_value<uint32_t>(c->ptr, OFF_VERSION) : 0;
}

OMNI_PCM_API const char* OmniPcm_GetLastError(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    return c ? c->last_error.c_str() : "Invalid handle";
}

OMNI_PCM_API int OmniPcm_GetSnapshot(OmniPcmHandle handle, OmniPcmSnapshot* out_snapshot) {
    return snapshot_internal(ctx(handle), out_snapshot);
}

OMNI_PCM_API int OmniPcm_GetInfo(OmniPcmHandle handle, OmniPcmInfo* out_info) {
    auto* c = ctx(handle);
    if (!validate(c) || !out_info) return OMNI_PCM_BAD_ARGUMENT;
    OmniPcmSnapshot s{};
    int r = snapshot_internal(c, &s);
    if (r != OMNI_PCM_OK) return r;
    out_info->sample_rate = s.sample_rate > 0 ? s.sample_rate : 44100;
    out_info->channels = s.channels > 0 ? s.channels : 2;
    out_info->bytes_per_frame = s.bytes_per_frame > 0 ? s.bytes_per_frame : out_info->channels * 4;
    out_info->buffer_frames = s.buffer_frames;
    out_info->total_frames_hint = s.total_frames_hint;
    out_info->decoded_total_frames = s.decoded_total_frames;
    out_info->effective_total_frames = effective_total(s);
    return OMNI_PCM_OK;
}

OMNI_PCM_API const char* OmniPcm_GetCurrentUuid(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!validate(c)) return "";
    char uuid[OMNI_PCM_UUID_BYTES]{};
    std::memcpy(uuid, c->ptr + OFF_CURRENT_UUID, OMNI_PCM_UUID_BYTES);
    uuid[OMNI_PCM_UUID_BYTES - 1] = '\0';
    c->current_uuid_cache = uuid;
    return c->current_uuid_cache.c_str();
}

OMNI_PCM_API int OmniPcm_BindCurrentStream(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!validate(c)) return OMNI_PCM_BAD_ARGUMENT;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    c->bound_uuid = s.current_uuid;
    c->bound_stream_id = s.stream_id;
    c->audible_base_frame = std::max<int64_t>(0, s.audible_cursor);
    c->last_seen_seek_generation = s.seek_generation;
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcm_BindStream(OmniPcmHandle handle, const char* uuid) {
    auto* c = ctx(handle);
    if (!validate(c) || !uuid) return OMNI_PCM_BAD_ARGUMENT;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    c->bound_uuid = uuid;
    c->bound_stream_id = (std::strncmp(s.current_uuid, uuid, OMNI_PCM_UUID_BYTES) == 0) ? s.stream_id : 0;
    c->audible_base_frame = std::max<int64_t>(0, s.audible_cursor);
    c->last_seen_seek_generation = s.seek_generation;
    return OMNI_PCM_OK;
}

OMNI_PCM_API int64_t OmniPcm_GetBoundStreamId(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    return c ? c->bound_stream_id : 0;
}

OMNI_PCM_API int OmniPcm_IsFormatReady(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!validate(c)) return 0;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    if (!same_stream(c, s)) return 0;
    return (s.version < OMNI_PCM_VERSION_2 || (s.flags & OMNI_PCM_FLAG_FORMAT_READY) != 0) ? 1 : 0;
}

OMNI_PCM_API int OmniPcm_WaitForFormatReady(OmniPcmHandle handle, const char* uuid, int timeout_ms) {
    auto* c = ctx(handle);
    if (!validate(c)) return OMNI_PCM_BAD_ARGUMENT;
    if (uuid && *uuid) OmniPcm_BindStream(handle, uuid);
    auto start = std::chrono::steady_clock::now();
    while (true) {
        if (OmniPcm_IsFormatReady(handle)) return OMNI_PCM_OK;
        if (timeout_ms >= 0) {
            auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::steady_clock::now() - start).count();
            if (elapsed >= timeout_ms) return OMNI_PCM_NOT_READY;
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(10));
    }
}

OMNI_PCM_API int OmniPcm_HasDecoderEof(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!validate(c)) return 0;
    uint32_t f = flags(c);
    return (f & (OMNI_PCM_FLAG_DECODER_EOF | OMNI_PCM_FLAG_SYNTHETIC_EOF)) ? 1 : 0;
}

OMNI_PCM_API int OmniPcm_IsPlaybackComplete(OmniPcmHandle handle, int64_t tolerance_frames) {
    auto* c = ctx(handle);
    if (!validate(c)) return 0;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    if (!same_stream(c, s)) return 0;
    if (s.version < OMNI_PCM_VERSION_2) {
        return s.write_cursor > 0 && s.legacy_play_state == 0 && s.read_cursor >= s.write_cursor ? 1 : 0;
    }
    if ((s.flags & (OMNI_PCM_FLAG_DECODER_EOF | OMNI_PCM_FLAG_SYNTHETIC_EOF)) == 0) return 0;
    int64_t final_cursor = s.final_write_cursor > 0 ? s.final_write_cursor : s.write_cursor;
    if (s.read_cursor < final_cursor) return 0;
    return s.audible_cursor + std::max<int64_t>(0, tolerance_frames) >= final_cursor ? 1 : 0;
}

OMNI_PCM_API int OmniPcm_HasError(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!validate(c)) return 0;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    return (s.flags & OMNI_PCM_FLAG_STREAM_ERROR) || s.state == OMNI_PCM_STATE_ERROR ? 1 : 0;
}

OMNI_PCM_API int64_t OmniPcm_ReadFrames(OmniPcmHandle handle, float* buffer, int32_t frames_to_read) {
    auto* c = ctx(handle);
    if (!validate(c) || !buffer || frames_to_read <= 0) return OMNI_PCM_BAD_ARGUMENT;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    if (!same_stream(c, s)) return OMNI_PCM_WRONG_STREAM;
    if (!OmniPcm_IsFormatReady(handle)) return OMNI_PCM_NOT_READY;
    int channels = s.channels > 0 ? s.channels : 2;
    int64_t available = s.write_cursor - s.read_cursor;
    if (available <= 0) return 0;

    int32_t frames_to_copy = static_cast<int32_t>(std::min<int64_t>(frames_to_read, available));
    int64_t start_frame = s.buffer_frames > 0 ? s.read_cursor % s.buffer_frames : 0;
    int64_t sample_count = static_cast<int64_t>(frames_to_copy) * channels;
    int64_t total_samples = static_cast<int64_t>(s.buffer_frames) * channels;
    int64_t start_sample = start_frame * channels;
    float* pcm = reinterpret_cast<float*>(c->ptr + HEADER_SIZE);

    if (start_sample + sample_count <= total_samples) {
        std::memcpy(buffer, pcm + start_sample, static_cast<size_t>(sample_count) * sizeof(float));
    } else {
        int64_t first_part = total_samples - start_sample;
        std::memcpy(buffer, pcm + start_sample, static_cast<size_t>(first_part) * sizeof(float));
        std::memcpy(buffer + first_part, pcm, static_cast<size_t>(sample_count - first_part) * sizeof(float));
    }

    write_value<int64_t>(c->ptr, OFF_READ_CURSOR, s.read_cursor + frames_to_copy);
    write_value<int64_t>(c->ptr, OFF_LAST_UPDATE_TICK, utc_ticks_now());
    return frames_to_copy;
}

OMNI_PCM_API int OmniPcm_RequestSeek(OmniPcmHandle handle, int64_t frame) {
    auto* c = ctx(handle);
    if (!validate(c)) return OMNI_PCM_BAD_ARGUMENT;
    if (frame < 0) frame = 0;
    write_value<int64_t>(c->ptr, OFF_SEEK_FRAME, frame);
    auto* target = reinterpret_cast<volatile long long*>(c->ptr + OFF_SEEK_GENERATION);
    InterlockedIncrement64(target);
    set_flag(c, OMNI_PCM_FLAG_SEEK_PENDING, true);
    set_flag(c, OMNI_PCM_FLAG_DISCONTINUITY, true);
    c->audible_base_frame = frame;
    c->last_seen_seek_generation = read_value<int64_t>(c->ptr, OFF_SEEK_GENERATION);
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcm_CancelPendingSeek(OmniPcmHandle handle) {
    auto* c = ctx(handle);
    if (!validate(c)) return OMNI_PCM_BAD_ARGUMENT;
    set_flag(c, OMNI_PCM_FLAG_SEEK_PENDING, false);
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcm_SetAudibleCursor(OmniPcmHandle handle, int64_t frame, int allow_backward) {
    auto* c = ctx(handle);
    if (!validate(c)) return OMNI_PCM_BAD_ARGUMENT;
    if (frame < 0) frame = 0;
    int64_t current = read_value<int64_t>(c->ptr, OFF_AUDIBLE_CURSOR);
    uint32_t f = flags(c);
    bool can_move_backward = allow_backward != 0 || (f & (OMNI_PCM_FLAG_SEEK_PENDING | OMNI_PCM_FLAG_DISCONTINUITY)) != 0;
    if (frame < current && !can_move_backward) return OMNI_PCM_OK;
    write_value<int64_t>(c->ptr, OFF_AUDIBLE_CURSOR, frame);
    write_value<int64_t>(c->ptr, OFF_LAST_UPDATE_TICK, utc_ticks_now());
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcm_ReportAudioSourcePosition(OmniPcmHandle handle, int32_t time_samples) {
    auto* c = ctx(handle);
    if (!validate(c)) return OMNI_PCM_BAD_ARGUMENT;
    OmniPcmSnapshot s{};
    snapshot_internal(c, &s);
    bool allow_backward = false;
    if (s.version >= OMNI_PCM_VERSION_2 && s.seek_generation != c->last_seen_seek_generation) {
        c->last_seen_seek_generation = s.seek_generation;
        c->audible_base_frame = std::max<int64_t>(0, s.seek_frame);
        allow_backward = true;
    }
    int64_t frame = c->audible_base_frame + std::max<int32_t>(0, time_samples);
    return OmniPcm_SetAudibleCursor(handle, frame, allow_backward ? 1 : 0);
}
