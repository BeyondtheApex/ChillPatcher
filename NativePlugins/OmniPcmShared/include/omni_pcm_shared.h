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

#ifndef OMNI_PCM_SHARED_H
#define OMNI_PCM_SHARED_H

#include <stdint.h>
#include <wchar.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
    #ifdef BUILDING_OMNI_PCM_SHARED
        #define OMNI_PCM_API __declspec(dllexport)
    #else
        #define OMNI_PCM_API __declspec(dllimport)
    #endif
#else
    #define OMNI_PCM_API
#endif

#define OMNI_PCM_DEFAULT_MAP_NAME L"Global\\OmniMixPlayer_PCM"
#define OMNI_PCM_MAGIC 0x4D43504C4C494843ULL
#define OMNI_PCM_VERSION_1 1u
#define OMNI_PCM_VERSION_2 2u
#define OMNI_PCM_UUID_BYTES 64

typedef enum OmniPcmResult {
    OMNI_PCM_OK = 0,
    OMNI_PCM_ERROR = -1,
    OMNI_PCM_NOT_READY = -2,
    OMNI_PCM_EOF = -3,
    OMNI_PCM_BAD_ARGUMENT = -4,
    OMNI_PCM_UNSUPPORTED = -5,
    OMNI_PCM_WRONG_STREAM = -6
} OmniPcmResult;

typedef enum OmniPcmStreamFlags {
    OMNI_PCM_FLAG_NONE = 0,
    OMNI_PCM_FLAG_FORMAT_READY = 1u << 0,
    OMNI_PCM_FLAG_DECODER_EOF = 1u << 1,
    OMNI_PCM_FLAG_STREAM_ERROR = 1u << 2,
    OMNI_PCM_FLAG_SEEK_PENDING = 1u << 3,
    OMNI_PCM_FLAG_DISCONTINUITY = 1u << 4,
    OMNI_PCM_FLAG_CLIENT_DRAINED = 1u << 5,
    OMNI_PCM_FLAG_SYNTHETIC_EOF = 1u << 6
} OmniPcmStreamFlags;

typedef enum OmniPcmStreamState {
    OMNI_PCM_STATE_STOPPED = 0,
    OMNI_PCM_STATE_PREPARING = 1,
    OMNI_PCM_STATE_PLAYING = 2,
    OMNI_PCM_STATE_PAUSED = 3,
    OMNI_PCM_STATE_DRAINING = 4,
    OMNI_PCM_STATE_ENDED = 5,
    OMNI_PCM_STATE_ERROR = 6
} OmniPcmStreamState;

typedef enum OmniPcmStreamError {
    OMNI_PCM_STREAM_ERROR_NONE = 0,
    OMNI_PCM_STREAM_ERROR_DECODER_FAILED = 1,
    OMNI_PCM_STREAM_ERROR_SOURCE_ENDED_UNEXPECTEDLY = 2,
    OMNI_PCM_STREAM_ERROR_STALLED_NEAR_END = 3,
    OMNI_PCM_STREAM_ERROR_FORMAT_INVALID = 4
} OmniPcmStreamError;

typedef struct OmniPcmInfo {
    int32_t sample_rate;
    int32_t channels;
    int32_t bytes_per_frame;
    int32_t buffer_frames;
    int64_t total_frames_hint;
    int64_t decoded_total_frames;
    int64_t effective_total_frames;
} OmniPcmInfo;

typedef struct OmniPcmSnapshot {
    uint32_t version;
    int32_t sample_rate;
    int32_t channels;
    int32_t bytes_per_frame;
    int32_t buffer_frames;
    int32_t legacy_play_state;
    uint32_t flags;
    int64_t write_cursor;
    int64_t read_cursor;
    int64_t stream_id;
    int32_t state;
    int32_t error_code;
    int64_t total_frames_hint;
    int64_t decoded_total_frames;
    int64_t final_write_cursor;
    int64_t audible_cursor;
    int64_t seek_frame;
    int64_t seek_generation;
    int64_t last_update_tick;
    int32_t format_generation;
    char current_uuid[OMNI_PCM_UUID_BYTES];
} OmniPcmSnapshot;

typedef void* OmniPcmHandle;

OMNI_PCM_API OmniPcmHandle OmniPcm_Open(const wchar_t* map_name);
OMNI_PCM_API OmniPcmHandle OmniPcm_OpenUtf8(const char* map_name_utf8);
OMNI_PCM_API void OmniPcm_Close(OmniPcmHandle handle);

OMNI_PCM_API int OmniPcm_IsOpen(OmniPcmHandle handle);
OMNI_PCM_API uint32_t OmniPcm_GetVersion(OmniPcmHandle handle);
OMNI_PCM_API const char* OmniPcm_GetLastError(OmniPcmHandle handle);

OMNI_PCM_API int OmniPcm_GetSnapshot(OmniPcmHandle handle, OmniPcmSnapshot* out_snapshot);
OMNI_PCM_API int OmniPcm_GetInfo(OmniPcmHandle handle, OmniPcmInfo* out_info);
OMNI_PCM_API const char* OmniPcm_GetCurrentUuid(OmniPcmHandle handle);

OMNI_PCM_API int OmniPcm_BindCurrentStream(OmniPcmHandle handle);
OMNI_PCM_API int OmniPcm_BindStream(OmniPcmHandle handle, const char* uuid);
OMNI_PCM_API int64_t OmniPcm_GetBoundStreamId(OmniPcmHandle handle);

OMNI_PCM_API int OmniPcm_IsFormatReady(OmniPcmHandle handle);
OMNI_PCM_API int OmniPcm_WaitForFormatReady(OmniPcmHandle handle, const char* uuid, int timeout_ms);
OMNI_PCM_API int OmniPcm_HasDecoderEof(OmniPcmHandle handle);
OMNI_PCM_API int OmniPcm_IsPlaybackComplete(OmniPcmHandle handle, int64_t tolerance_frames);
OMNI_PCM_API int OmniPcm_HasError(OmniPcmHandle handle);

OMNI_PCM_API int64_t OmniPcm_ReadFrames(OmniPcmHandle handle, float* buffer, int32_t frames_to_read);
OMNI_PCM_API int OmniPcm_RequestSeek(OmniPcmHandle handle, int64_t frame);
OMNI_PCM_API int OmniPcm_CancelPendingSeek(OmniPcmHandle handle);
OMNI_PCM_API int OmniPcm_SetAudibleCursor(OmniPcmHandle handle, int64_t frame, int allow_backward);
OMNI_PCM_API int OmniPcm_ReportAudioSourcePosition(OmniPcmHandle handle, int32_t time_samples);

#ifdef __cplusplus
}
#endif

#endif
