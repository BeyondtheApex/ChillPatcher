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
typedef void* OmniPcmClientHandle;

typedef enum OmniPcmPlaybackCommand {
    OMNI_PCM_COMMAND_PLAY = 1,
    OMNI_PCM_COMMAND_PAUSE = 2,
    OMNI_PCM_COMMAND_RESUME = 3,
    OMNI_PCM_COMMAND_TOGGLE = 4,
    OMNI_PCM_COMMAND_NEXT = 5,
    OMNI_PCM_COMMAND_PREV = 6,
    OMNI_PCM_COMMAND_STOP = 7
} OmniPcmPlaybackCommand;

typedef enum OmniPcmInstanceKind {
    OMNI_PCM_INSTANCE_KIND_GAME_MOD = 1,
    OMNI_PCM_INSTANCE_KIND_GUI = 2,
    OMNI_PCM_INSTANCE_KIND_EXTERNAL_CLIENT = 3,
    OMNI_PCM_INSTANCE_KIND_OBSERVER = 4
} OmniPcmInstanceKind;

typedef enum OmniPcmCapabilityFlags {
    OMNI_PCM_CAP_SERVER_CONTROLLED_PLAYBACK = 1u << 0,
    OMNI_PCM_CAP_QUEUE_MANAGEMENT = 1u << 2,
    OMNI_PCM_CAP_PLAYLIST_MANAGEMENT = 1u << 3,
    OMNI_PCM_CAP_SHUFFLE = 1u << 4,
    OMNI_PCM_CAP_REPEAT = 1u << 5,
    OMNI_PCM_CAP_SEEK = 1u << 6,
    OMNI_PCM_CAP_VOLUME_CONTROL = 1u << 7,
    OMNI_PCM_CAP_EQUALIZER = 1u << 8,
    OMNI_PCM_CAP_MULTIPLE_PLAYLISTS = 1u << 9,
    OMNI_PCM_CAP_TAG_FILTERING = 1u << 10,
    OMNI_PCM_CAP_UNLIMITED_TAGS = 1u << 11,
    OMNI_PCM_CAP_ALBUM_FILTERING = 1u << 12,
    OMNI_PCM_CAP_AUDIO_PLAYBACK = 1u << 13,
    OMNI_PCM_CAP_CUSTOM_SYSTEM_MEDIA_SERVICE = 1u << 14
} OmniPcmCapabilityFlags;

typedef struct OmniPcmClientConfig {
    const char* host;          /* default: 127.0.0.1 */
    int32_t port;             /* 0 = discover omnimix_port.txt */
    int32_t timeout_ms;       /* default: 3000 */
} OmniPcmClientConfig;

typedef struct OmniPcmConnectOptions {
    const char* client_id;
    const char* mod_id;
    const char* game_name;
    const char* display_name;
    int32_t kind;
    uint32_t capability_flags;
    int32_t no_instance;
    int32_t max_imported_playlists;  /* 0 = unspecified / no limit */
    int32_t max_tags;
    int32_t max_playlist_entries;
} OmniPcmConnectOptions;

typedef struct OmniPcmConnectionInfo {
    char instance_id[128];
    int32_t is_new;
    int32_t no_instance;
} OmniPcmConnectionInfo;

typedef struct OmniPcmPlaybackStatusInfo {
    char track_uuid[OMNI_PCM_UUID_BYTES];
    char title[256];
    char artist[256];
    char album_id[128];
    float duration;
    float position;
    int32_t is_playing;
    int32_t shuffle;
    int32_t repeat_mode;
    float volume;
} OmniPcmPlaybackStatusInfo;

typedef struct OmniPcmInstanceSummaryInfo {
    char instance_id[128];
    char display_name[256];
    char mod_id[128];
    char game_name[256];
    char current_track_uuid[OMNI_PCM_UUID_BYTES];
    int32_t kind;
    int32_t is_online;
    int32_t queue_count;
    int32_t mode;
    int64_t connected_at;
} OmniPcmInstanceSummaryInfo;

typedef struct OmniPcmInstanceProfileInfo {
    char instance_id[128];
    char display_name[256];
    char mod_id[128];
    char game_name[256];
    int32_t kind;
    uint32_t capability_flags;
    float volume;
    float target_latency;
    int32_t mode;
    int32_t max_imported_playlists;
    int32_t max_tags;
    int32_t max_playlist_entries;
    int64_t created_at;
    int64_t updated_at;
} OmniPcmInstanceProfileInfo;

typedef struct OmniPcmQueueTrackInfo {
    int32_t index;
    char uuid[OMNI_PCM_UUID_BYTES];
    char title[256];
    char artist[256];
    char album_id[128];
    char module_id[128];
    char cover_uri[512];
    float duration;
} OmniPcmQueueTrackInfo;

typedef struct OmniPcmPlaylistSourceInfo {
    char id[128];
    char name[256];
    char ref_id[256];
    int32_t song_count;
    int32_t kind;
} OmniPcmPlaylistSourceInfo;

typedef struct OmniPcmPlaylistSourceSpec {
    const char* id;
    const char* name;
    const char* ref_id;
    int32_t kind;
    const char* const* uuids;
    int32_t uuid_count;
} OmniPcmPlaylistSourceSpec;

typedef struct OmniPcmEqualizerPointInfo {
    char id[64];
    float frequency;
    float gain_db;
    float q;
    int32_t type;
} OmniPcmEqualizerPointInfo;

typedef struct OmniPcmEqualizerStateInfo {
    int32_t enabled;
    float global_gain_db;
    int32_t soft_clip_enabled;
} OmniPcmEqualizerStateInfo;

typedef struct OmniPcmBackendInfo {
    char status[32];
    char version[64];
    char name[128];
    int64_t timestamp;
} OmniPcmBackendInfo;

#define OMNI_PCM_TRACK_TITLE_SZ 256
#define OMNI_PCM_TRACK_ARTIST_SZ 256
#define OMNI_PCM_ALBUM_TITLE_SZ 256
#define OMNI_PCM_TAG_NAME_SZ 128
#define OMNI_PCM_PLAYLIST_NAME_SZ 256
#define OMNI_PCM_RESOURCE_URI_SZ 512
#define OMNI_PCM_COLOR_SZ 32

typedef struct OmniPcmTrackInfo {
    char uuid[OMNI_PCM_UUID_BYTES];
    char title[OMNI_PCM_TRACK_TITLE_SZ];
    char artist[OMNI_PCM_TRACK_ARTIST_SZ];
    char album_id[128];
    char module_id[128];
    char cover_uri[OMNI_PCM_RESOURCE_URI_SZ];
    int32_t track_number;
    float duration;
    int32_t is_excluded;
    int64_t created_at;
    int64_t last_played_at;
} OmniPcmTrackInfo;

typedef struct OmniPcmAlbumInfo {
    char id[128];
    char title[OMNI_PCM_ALBUM_TITLE_SZ];
    char artist[OMNI_PCM_TRACK_ARTIST_SZ];
    char module_id[128];
    char cover_uri[OMNI_PCM_RESOURCE_URI_SZ];
    int32_t track_count;
} OmniPcmAlbumInfo;

typedef struct OmniPcmTagInfo {
    char id[128];
    char name[OMNI_PCM_TAG_NAME_SZ];
    char module_id[128];
    char color[OMNI_PCM_COLOR_SZ];
} OmniPcmTagInfo;

typedef struct OmniPcmPlaylistInfo {
    char id[128];
    char name[OMNI_PCM_PLAYLIST_NAME_SZ];
    char module_id[128];
    char cover_uri[OMNI_PCM_RESOURCE_URI_SZ];
    int32_t track_count;
} OmniPcmPlaylistInfo;

typedef struct OmniPcmTrackQuery {
    const char* album_id;
    const char* tag_id;
    const char* playlist_id;
    const char* module_id;
    int32_t is_excluded;    /* -1=no filter, 0=only not excluded, 1=only excluded */
    int32_t limit;
    int32_t offset;
} OmniPcmTrackQuery;

typedef struct OmniPcmLibraryQuery {
    const char* module_id;
    int32_t limit;
    int32_t offset;
} OmniPcmLibraryQuery;

typedef struct OmniPcmEventInfo {
    char type[64];
    int64_t timestamp;
    char instance_id[128];
    char track_uuid[OMNI_PCM_UUID_BYTES];
    char title[256];
    char artist[256];
    char album_id[128];
    char module_id[128];
    char source_ref_id[256];
    char change_type[64];
    char display_name[256];
    float duration;
    float position;
    int32_t state;
    int32_t queue_length;
    int32_t backend_running;
    int32_t bool_value;
    int32_t song_count;
    int32_t instance_count;
    float volume;
    float latency;
} OmniPcmEventInfo;

typedef void (*OmniPcmEventCallback)(const OmniPcmEventInfo* event_info, void* user_data);

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

OMNI_PCM_API OmniPcmClientHandle OmniPcmClient_Create(const OmniPcmClientConfig* config);
OMNI_PCM_API void OmniPcmClient_Destroy(OmniPcmClientHandle client);
OMNI_PCM_API const char* OmniPcmClient_GetLastError(OmniPcmClientHandle client);
OMNI_PCM_API int32_t OmniPcmClient_GetPort(OmniPcmClientHandle client);

OMNI_PCM_API int OmniPcmClient_ConnectInstance(
    OmniPcmClientHandle client,
    const OmniPcmConnectOptions* options,
    OmniPcmConnectionInfo* out_info);
OMNI_PCM_API int OmniPcmClient_Heartbeat(OmniPcmClientHandle client, const char* instance_id, int* out_alive);
OMNI_PCM_API int OmniPcmClient_DisconnectInstance(OmniPcmClientHandle client, const char* instance_id);
OMNI_PCM_API int OmniPcmClient_DeleteInstance(OmniPcmClientHandle client, const char* instance_id, int* out_deleted);
OMNI_PCM_API int OmniPcmClient_ListInstances(
    OmniPcmClientHandle client,
    OmniPcmInstanceSummaryInfo* out_instances,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_GetProfile(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmInstanceProfileInfo* out_profile);
OMNI_PCM_API int OmniPcmClient_UpdateProfile(
    OmniPcmClientHandle client,
    const OmniPcmInstanceProfileInfo* profile,
    int* out_saved);
OMNI_PCM_API int OmniPcmClient_ArchiveInstance(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* label,
    OmniPcmInstanceProfileInfo* out_archive);
OMNI_PCM_API int OmniPcmClient_ListArchives(
    OmniPcmClientHandle client,
    OmniPcmInstanceProfileInfo* out_archives,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_GetArchive(
    OmniPcmClientHandle client,
    const char* archive_id,
    OmniPcmInstanceProfileInfo* out_archive);
OMNI_PCM_API int OmniPcmClient_DeleteArchive(OmniPcmClientHandle client, const char* archive_id, int* out_deleted);
OMNI_PCM_API int OmniPcmClient_InheritFromArchive(
    OmniPcmClientHandle client,
    const char* new_instance_id,
    const char* archive_id,
    OmniPcmInstanceProfileInfo* out_profile);
OMNI_PCM_API int OmniPcmClient_GetStatus(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmPlaybackStatusInfo* out_status);
OMNI_PCM_API int OmniPcmClient_PlaybackCommand(
    OmniPcmClientHandle client,
    const char* instance_id,
    int32_t command);
OMNI_PCM_API int OmniPcmClient_Play(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* track_uuid);
OMNI_PCM_API int OmniPcmClient_Seek(OmniPcmClientHandle client, const char* instance_id, float position_seconds);
OMNI_PCM_API int OmniPcmClient_SetVolume(OmniPcmClientHandle client, const char* instance_id, float volume);
OMNI_PCM_API int OmniPcmClient_GetVolume(OmniPcmClientHandle client, const char* instance_id, float* out_volume);
OMNI_PCM_API int OmniPcmClient_SetTargetLatency(OmniPcmClientHandle client, const char* instance_id, float latency);
OMNI_PCM_API int OmniPcmClient_GetTargetLatency(OmniPcmClientHandle client, const char* instance_id, float* out_latency);
OMNI_PCM_API int OmniPcmClient_SetShuffle(OmniPcmClientHandle client, const char* instance_id, int enabled);
OMNI_PCM_API int OmniPcmClient_SetRepeatMode(OmniPcmClientHandle client, const char* instance_id, int32_t repeat_mode);

OMNI_PCM_API int OmniPcmClient_GetQueue(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmQueueTrackInfo* out_tracks,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_AddToQueue(OmniPcmClientHandle client, const char* instance_id, const char* uuid);
OMNI_PCM_API int OmniPcmClient_InsertIntoQueue(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* const* uuids,
    int32_t uuid_count,
    int32_t index);
OMNI_PCM_API int OmniPcmClient_SetQueue(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* const* uuids,
    int32_t uuid_count);
OMNI_PCM_API int OmniPcmClient_RemoveFromQueueIndex(OmniPcmClientHandle client, const char* instance_id, int32_t index);
OMNI_PCM_API int OmniPcmClient_RemoveFromQueueUuid(OmniPcmClientHandle client, const char* instance_id, const char* uuid);
OMNI_PCM_API int OmniPcmClient_MoveInQueue(OmniPcmClientHandle client, const char* instance_id, int32_t from_index, int32_t to_index);
OMNI_PCM_API int OmniPcmClient_ClearQueue(OmniPcmClientHandle client, const char* instance_id);

OMNI_PCM_API int OmniPcmClient_GetHistory(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmQueueTrackInfo* out_tracks,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_RemoveFromHistory(OmniPcmClientHandle client, const char* instance_id, int32_t index);
OMNI_PCM_API int OmniPcmClient_MoveInHistory(OmniPcmClientHandle client, const char* instance_id, int32_t from_index, int32_t to_index);
OMNI_PCM_API int OmniPcmClient_ClearHistory(OmniPcmClientHandle client, const char* instance_id);

OMNI_PCM_API int OmniPcmClient_GetPlaylistSources(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmPlaylistSourceInfo* out_sources,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_SetPlaylistSources(
    OmniPcmClientHandle client,
    const char* instance_id,
    const OmniPcmPlaylistSourceSpec* sources,
    int32_t source_count);

OMNI_PCM_API int OmniPcmClient_GetEqualizer(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmEqualizerStateInfo* out_state,
    OmniPcmEqualizerPointInfo* out_points,
    int32_t* inout_point_count);
OMNI_PCM_API int OmniPcmClient_SetEqualizer(
    OmniPcmClientHandle client,
    const char* instance_id,
    const OmniPcmEqualizerStateInfo* state,
    const OmniPcmEqualizerPointInfo* points,
    int32_t point_count);

OMNI_PCM_API int OmniPcmClient_GetBackendInfo(OmniPcmClientHandle client, OmniPcmBackendInfo* out_info);
OMNI_PCM_API int OmniPcmClient_StopBackend(OmniPcmClientHandle client);

OMNI_PCM_API int OmniPcmClient_StartEvents(
    OmniPcmClientHandle client,
    OmniPcmEventCallback callback,
    void* user_data);
OMNI_PCM_API void OmniPcmClient_StopEvents(OmniPcmClientHandle client);

/* ── Library queries ── */
OMNI_PCM_API int OmniPcmClient_QueryTracks(
    OmniPcmClientHandle client,
    const OmniPcmTrackQuery* query,
    OmniPcmTrackInfo* out_tracks,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_QueryAlbums(
    OmniPcmClientHandle client,
    const OmniPcmLibraryQuery* query,
    OmniPcmAlbumInfo* out_albums,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_QueryTags(
    OmniPcmClientHandle client,
    const OmniPcmLibraryQuery* query,
    OmniPcmTagInfo* out_tags,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_QueryPlaylists(
    OmniPcmClientHandle client,
    const OmniPcmLibraryQuery* query,
    OmniPcmPlaylistInfo* out_playlists,
    int32_t* inout_count);
OMNI_PCM_API int OmniPcmClient_GetTrack(
    OmniPcmClientHandle client,
    const char* uuid,
    OmniPcmTrackInfo* out_track);
OMNI_PCM_API int OmniPcmClient_SetTrackExcluded(
    OmniPcmClientHandle client,
    const char* uuid,
    int32_t excluded);

#ifdef __cplusplus
}
#endif

#endif
