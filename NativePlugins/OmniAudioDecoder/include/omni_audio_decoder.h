#ifndef OMNI_AUDIO_DECODER_H
#define OMNI_AUDIO_DECODER_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
    #ifdef BUILDING_DLL
        #define OMNI_AUDIO_API __declspec(dllexport)
    #else
        #define OMNI_AUDIO_API __declspec(dllimport)
    #endif
#else
    #define OMNI_AUDIO_API
#endif

#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>

// ===== Shared structs =====

typedef struct {
    int sample_rate;
    int channels;
    uint64_t total_pcm_frame_count;
    float* pcm_data;
    size_t pcm_data_size;
} FlacAudioInfo;

// ===== File-based API (替代 AudioDecoder_OpenFile + OpenFlacStream) =====

OMNI_AUDIO_API void* AudioDecoder_OpenFile(
    const wchar_t* file_path,
    int* out_sample_rate,
    int* out_channels,
    uint64_t* out_total_frames,
    char* out_format,
    bool is_growing);

OMNI_AUDIO_API int64_t AudioDecoder_ReadFrames(
    void* handle, float* buffer, int frames_to_read);

OMNI_AUDIO_API int AudioDecoder_Seek(
    void* handle, uint64_t frame_index);

OMNI_AUDIO_API void AudioDecoder_Close(void* handle);

OMNI_AUDIO_API const char* AudioDecoder_GetLastError(void);

// ===== Streaming API (替代 FeedData/StreamingRead) =====

OMNI_AUDIO_API void* AudioDecoder_CreateStreaming(const char* format);

OMNI_AUDIO_API int AudioDecoder_FeedData(
    void* handle, const void* data, int size);

OMNI_AUDIO_API void AudioDecoder_FeedComplete(void* handle);

OMNI_AUDIO_API int64_t AudioDecoder_StreamingRead(
    void* handle, float* buffer, int frames_to_read);

OMNI_AUDIO_API int AudioDecoder_StreamingIsReady(void* handle);

OMNI_AUDIO_API int AudioDecoder_StreamingGetInfo(
    void* handle, int* out_sample_rate, int* out_channels, uint64_t* out_total_frames);

OMNI_AUDIO_API void AudioDecoder_CloseStreaming(void* handle);

// ===== FLAC-specific API (替代 ChillFlacDecoder 全部导出) =====

OMNI_AUDIO_API void* OpenFlacStream(
    const wchar_t* file_path, int* out_sample_rate, int* out_channels,
    uint64_t* out_total_pcm_frames);

OMNI_AUDIO_API int64_t ReadFlacFrames(
    void* stream_handle, float* buffer, uint64_t frames_to_read);

OMNI_AUDIO_API int SeekFlacStream(
    void* stream_handle, uint64_t frame_index);

OMNI_AUDIO_API void CloseFlacStream(void* stream_handle);

OMNI_AUDIO_API const char* FlacGetLastError(void);

OMNI_AUDIO_API int DecodeFlacFile(
    const wchar_t* file_path, FlacAudioInfo* out_info);

OMNI_AUDIO_API void FreeFlacData(FlacAudioInfo* info);

#ifdef __cplusplus
}
#endif

#endif // OMNI_AUDIO_DECODER_H
