#define DR_MP3_IMPLEMENTATION
#define DR_FLAC_IMPLEMENTATION
#define DR_WAV_IMPLEMENTATION
#define BUILDING_DLL

#include "dr_mp3.h"
#include "dr_flac.h"
#include "dr_wav.h"
#include "audio_decoder.h"

#include <cstring>
#include <cstdlib>
#include <string>
#include <mutex>
#include <vector>
#include <algorithm>

static thread_local std::string g_last_error;

enum class AudioFormat { MP3, FLAC, WAV, UNKNOWN };

static AudioFormat detect_format_from_path(const wchar_t* path) {
    if (!path) return AudioFormat::UNKNOWN;
    size_t len = wcslen(path);
    if (len < 4) return AudioFormat::UNKNOWN;
    const wchar_t* ext = path + len;
    while (ext > path && *ext != L'.') ext--;
    if (_wcsicmp(ext, L".mp3") == 0) return AudioFormat::MP3;
    if (_wcsicmp(ext, L".flac") == 0) return AudioFormat::FLAC;
    if (_wcsicmp(ext, L".wav") == 0) return AudioFormat::WAV;
    return AudioFormat::UNKNOWN;
}

static AudioFormat parse_format_string(const char* fmt) {
    if (!fmt) return AudioFormat::UNKNOWN;
    if (_stricmp(fmt, "mp3") == 0) return AudioFormat::MP3;
    if (_stricmp(fmt, "flac") == 0) return AudioFormat::FLAC;
    if (_stricmp(fmt, "wav") == 0) return AudioFormat::WAV;
    return AudioFormat::UNKNOWN;
}

// ========== File stream handle (seekable) ==========
struct FileStreamHandle {
    static const unsigned int MAGIC = 0x46494C45; // "FILE"
    unsigned int magic;
    AudioFormat format;
    union {
        drmp3* mp3;
        drflac* flac;
        drwav* wav;
    };
    int sample_rate;
    int channels;
    unsigned long long total_frames;

    FileStreamHandle() : magic(MAGIC), format(AudioFormat::UNKNOWN), mp3(nullptr),
                         sample_rate(0), channels(0), total_frames(0) {}
    ~FileStreamHandle() { close(); magic = 0; }

    void close() {
        switch (format) {
            case AudioFormat::MP3:  if (mp3)  { drmp3_uninit(mp3); free(mp3); mp3 = nullptr; } break;
            case AudioFormat::FLAC: if (flac) { drflac_close(flac); flac = nullptr; } break;
            case AudioFormat::WAV:  if (wav)  { drwav_uninit(wav); free(wav); wav = nullptr; } break;
            default: break;
        }
    }
};

// ========== Streaming handle (incremental feed) ==========
struct StreamingHandle {
    AudioFormat format;
    std::mutex mutex;

    // Input buffer (raw audio bytes from HTTP download)
    std::vector<unsigned char> input_buffer;
    size_t read_cursor;    // decoder read position in input_buffer
    bool feed_complete;

    // Decoded PCM output buffer
    std::vector<float> pcm_buffer;
    size_t pcm_read_pos;

    // Audio info
    int sample_rate;
    int channels;
    unsigned long long total_frames;
    bool info_detected;
    bool is_ready;
    bool is_eof;
    bool decoder_initialized;

    // Decoder (only one is active based on format)
    drmp3* mp3_decoder;
    drflac* flac_decoder;

    // Magic number to distinguish from FileStreamHandle
    static const unsigned int MAGIC = 0x53545245; // "STRE"
    unsigned int magic;

    StreamingHandle()
        : format(AudioFormat::UNKNOWN), read_cursor(0), feed_complete(false),
          pcm_read_pos(0), sample_rate(0), channels(0), total_frames(0),
          info_detected(false), is_ready(false), is_eof(false),
          decoder_initialized(false), mp3_decoder(nullptr), flac_decoder(nullptr),
          magic(MAGIC) {}

    ~StreamingHandle() {
        if (mp3_decoder) { drmp3_uninit(mp3_decoder); free(mp3_decoder); }
        if (flac_decoder) { drflac_close(flac_decoder); }
        magic = 0;
    }
};

static const size_t PREFILL_FRAMES = 22050; // ~0.5s at 44100Hz

extern "C" {

AUDIO_API void* AudioDecoder_OpenFile(
    const wchar_t* file_path,
    int* out_sample_rate,
    int* out_channels,
    unsigned long long* out_total_frames,
    char* out_format)
{
    if (!file_path) {
        g_last_error = "File path is NULL";
        return nullptr;
    }

    AudioFormat fmt = detect_format_from_path(file_path);
    if (fmt == AudioFormat::UNKNOWN) {
        g_last_error = "Unsupported audio format";
        return nullptr;
    }

    auto* h = new FileStreamHandle();
    h->format = fmt;

    switch (fmt) {
    case AudioFormat::MP3: {
        h->mp3 = (drmp3*)calloc(1, sizeof(drmp3));
        if (!drmp3_init_file_w(h->mp3, file_path, nullptr)) {
            g_last_error = "Failed to open MP3 file";
            delete h; return nullptr;
        }
        h->sample_rate = h->mp3->sampleRate;
        h->channels = h->mp3->channels;
        h->total_frames = drmp3_get_pcm_frame_count(h->mp3);
        if (out_format) strcpy(out_format, "mp3");
        break;
    }
    case AudioFormat::FLAC: {
        h->flac = drflac_open_file_w(file_path, nullptr);
        if (!h->flac) {
            g_last_error = "Failed to open FLAC file";
            delete h; return nullptr;
        }
        h->sample_rate = h->flac->sampleRate;
        h->channels = h->flac->channels;
        h->total_frames = h->flac->totalPCMFrameCount;
        if (out_format) strcpy(out_format, "flac");
        break;
    }
    case AudioFormat::WAV: {
        h->wav = (drwav*)calloc(1, sizeof(drwav));
        if (!drwav_init_file_w(h->wav, file_path, nullptr)) {
            g_last_error = "Failed to open WAV file";
            delete h; return nullptr;
        }
        h->sample_rate = h->wav->sampleRate;
        h->channels = h->wav->channels;
        h->total_frames = h->wav->totalPCMFrameCount;
        if (out_format) strcpy(out_format, "wav");
        break;
    }
    default: delete h; return nullptr;
    }

    if (out_sample_rate) *out_sample_rate = h->sample_rate;
    if (out_channels) *out_channels = h->channels;
    if (out_total_frames) *out_total_frames = h->total_frames;
    return h;
}

AUDIO_API long long AudioDecoder_ReadFrames(
    void* handle,
    float* buffer,
    int frames_to_read)
{
    if (!handle || !buffer || frames_to_read <= 0) {
        g_last_error = "Invalid parameters";
        return -1;
    }
    auto* h = static_cast<FileStreamHandle*>(handle);
    if (h->magic != FileStreamHandle::MAGIC) {
        g_last_error = "Invalid file handle (wrong magic)";
        return -1;
    }

    switch (h->format) {
    case AudioFormat::MP3: {
        drmp3_uint64 read = drmp3_read_pcm_frames_f32(h->mp3, (drmp3_uint64)frames_to_read, buffer);
        return (long long)read;
    }
    case AudioFormat::FLAC: {
        drflac_uint64 read = drflac_read_pcm_frames_f32(h->flac, (drflac_uint64)frames_to_read, buffer);
        return (long long)read;
    }
    case AudioFormat::WAV: {
        drwav_uint64 read = drwav_read_pcm_frames_f32(h->wav, (drwav_uint64)frames_to_read, buffer);
        return (long long)read;
    }
    default:
        g_last_error = "Unknown format";
        return -1;
    }
}

AUDIO_API int AudioDecoder_Seek(void* handle, unsigned long long frame_index) {
    if (!handle) { g_last_error = "Handle is NULL"; return -1; }
    auto* h = static_cast<FileStreamHandle*>(handle);
    if (h->magic != FileStreamHandle::MAGIC) {
        g_last_error = "Invalid file handle (wrong magic)";
        return -1;
    }

    switch (h->format) {
    case AudioFormat::MP3: {
        drmp3_bool32 ok = drmp3_seek_to_pcm_frame(h->mp3, (drmp3_uint64)frame_index);
        if (!ok) { g_last_error = "MP3 seek failed"; return -1; }
        return 0;
    }
    case AudioFormat::FLAC: {
        drflac_bool32 ok = drflac_seek_to_pcm_frame(h->flac, (drflac_uint64)frame_index);
        if (!ok) { g_last_error = "FLAC seek failed"; return -1; }
        return 0;
    }
    case AudioFormat::WAV: {
        drwav_bool32 ok = drwav_seek_to_pcm_frame(h->wav, (drwav_uint64)frame_index);
        if (!ok) { g_last_error = "WAV seek failed"; return -1; }
        return 0;
    }
    default:
        g_last_error = "Unknown format";
        return -1;
    }
}

AUDIO_API void AudioDecoder_Close(void* handle) {
    if (!handle) return;
    auto* h = static_cast<FileStreamHandle*>(handle);
    if (h->magic != FileStreamHandle::MAGIC) return; // safety check
    delete h;
}

AUDIO_API const char* AudioDecoder_GetLastError(void) {
    return g_last_error.c_str();
}

// ========== Streaming (incremental feed) ==========

// drmp3 callback: read from StreamingHandle's input_buffer
static size_t streaming_mp3_read_cb(void* pUserData, void* pBufferOut, size_t bytesToRead) {
    auto* s = static_cast<StreamingHandle*>(pUserData);
    // Note: mutex is already held by the caller (FeedData/StreamingRead)
    size_t available = s->input_buffer.size() - s->read_cursor;
    if (available == 0) return 0; // no data yet, drmp3 will stop decoding

    size_t to_read = std::min(available, bytesToRead);
    memcpy(pBufferOut, s->input_buffer.data() + s->read_cursor, to_read);
    s->read_cursor += to_read;
    return to_read;
}

// drmp3 callback: seek within input_buffer (limited to buffered range)
static drmp3_bool32 streaming_mp3_seek_cb(void* pUserData, int offset, drmp3_seek_origin origin) {
    auto* s = static_cast<StreamingHandle*>(pUserData);
    size_t new_cursor;
    if (origin == DRMP3_SEEK_SET) {
        new_cursor = (size_t)offset;
    } else { // DRMP3_SEEK_CUR
        new_cursor = s->read_cursor + (size_t)offset;
    }
    if (new_cursor > s->input_buffer.size()) return DRMP3_FALSE;
    s->read_cursor = new_cursor;
    return DRMP3_TRUE;
}

// drmp3 callback: tell current position in input_buffer
static drmp3_bool32 streaming_mp3_tell_cb(void* pUserData, drmp3_int64* pCursor) {
    auto* s = static_cast<StreamingHandle*>(pUserData);
    if (pCursor) *pCursor = (drmp3_int64)s->read_cursor;
    return DRMP3_TRUE;
}

// Internal: try to init or decode more MP3 data from input buffer
// Caller must hold s->mutex
static void streaming_mp3_decode(StreamingHandle* s) {
    if (!s->decoder_initialized) {
        if (s->input_buffer.size() < 4096) return; // need minimum data for header

        s->mp3_decoder = (drmp3*)calloc(1, sizeof(drmp3));
        if (!drmp3_init(s->mp3_decoder, streaming_mp3_read_cb, streaming_mp3_seek_cb,
                        streaming_mp3_tell_cb, nullptr, s, nullptr)) {
            free(s->mp3_decoder);
            s->mp3_decoder = nullptr;
            return;
        }
        s->sample_rate = s->mp3_decoder->sampleRate;
        s->channels = s->mp3_decoder->channels;
        s->info_detected = true;
        s->decoder_initialized = true;
    }

    // Decode available frames into pcm_buffer
    float temp[4096]; // 2048 frames * 2ch max
    int ch = s->channels > 0 ? s->channels : 2;
    drmp3_uint64 read = drmp3_read_pcm_frames_f32(s->mp3_decoder, 2048, temp);
    if (read > 0) {
        size_t samples = (size_t)read * ch;
        s->pcm_buffer.insert(s->pcm_buffer.end(), temp, temp + samples);
    }

    // Check readiness
    size_t available_frames = (s->pcm_buffer.size() - s->pcm_read_pos) / ch;
    if (available_frames >= PREFILL_FRAMES) {
        s->is_ready = true;
    }
}

// ---- FLAC streaming callbacks ----
static size_t streaming_flac_read_cb(void* pUserData, void* pBufferOut, size_t bytesToRead) {
    auto* s = static_cast<StreamingHandle*>(pUserData);
    size_t available = s->input_buffer.size() - s->read_cursor;
    if (available == 0) return 0;

    size_t to_read = std::min(available, bytesToRead);
    memcpy(pBufferOut, s->input_buffer.data() + s->read_cursor, to_read);
    s->read_cursor += to_read;
    return to_read;
}

static drflac_bool32 streaming_flac_seek_cb(void* pUserData, int offset, drflac_seek_origin origin) {
    auto* s = static_cast<StreamingHandle*>(pUserData);
    size_t new_cursor;
    if (origin == DRFLAC_SEEK_SET) {
        new_cursor = (size_t)offset;
    } else {
        new_cursor = s->read_cursor + (size_t)offset;
    }
    if (new_cursor > s->input_buffer.size()) return DRFLAC_FALSE;
    s->read_cursor = new_cursor;
    return DRFLAC_TRUE;
}

static drflac_bool32 streaming_flac_tell_cb(void* pUserData, drflac_int64* pCursor) {
    auto* s = static_cast<StreamingHandle*>(pUserData);
    if (pCursor) *pCursor = (drflac_int64)s->read_cursor;
    return DRFLAC_TRUE;
}

// Internal: try to init or decode more FLAC data from input buffer
// FLAC needs the STREAMINFO header block. We retry drflac_open on each FeedData
// until enough header bytes have arrived.
// Caller must hold s->mutex
static void streaming_flac_decode(StreamingHandle* s) {
    if (!s->decoder_initialized) {
        if (s->input_buffer.size() < 8192) return; // need enough for FLAC header

        size_t saved_cursor = s->read_cursor;
        s->read_cursor = 0; // drflac_open reads from the beginning

        s->flac_decoder = drflac_open(streaming_flac_read_cb, streaming_flac_seek_cb,
                                       streaming_flac_tell_cb, s, nullptr);
        if (!s->flac_decoder) {
            s->read_cursor = saved_cursor; // restore cursor on failure
            return;
        }

        s->sample_rate = (int)s->flac_decoder->sampleRate;
        s->channels = (int)s->flac_decoder->channels;
        s->total_frames = s->flac_decoder->totalPCMFrameCount;
        s->info_detected = true;
        s->decoder_initialized = true;
    }

    // Decode available frames
    float temp[4096];
    int ch = s->channels > 0 ? s->channels : 2;
    drflac_uint64 read = drflac_read_pcm_frames_f32(s->flac_decoder, 2048, temp);
    if (read > 0) {
        size_t samples = (size_t)read * ch;
        s->pcm_buffer.insert(s->pcm_buffer.end(), temp, temp + samples);
    }

    size_t available_frames = (s->pcm_buffer.size() - s->pcm_read_pos) / ch;
    if (available_frames >= PREFILL_FRAMES) {
        s->is_ready = true;
    }
}

AUDIO_API void* AudioDecoder_CreateStreaming(const char* format) {
    AudioFormat fmt = parse_format_string(format);
    if (fmt == AudioFormat::UNKNOWN) {
        g_last_error = "Unsupported streaming format";
        return nullptr;
    }
    if (fmt == AudioFormat::WAV) {
        g_last_error = "WAV streaming is not supported. Use file-based decoding.";
        return nullptr;
    }

    auto* s = new StreamingHandle();
    s->format = fmt;
    return s;
}

AUDIO_API int AudioDecoder_FeedData(void* handle, const void* data, int size) {
    if (!handle || !data || size <= 0) return -1;
    auto* s = static_cast<StreamingHandle*>(handle);

    std::lock_guard<std::mutex> lock(s->mutex);
    auto* bytes = static_cast<const unsigned char*>(data);
    s->input_buffer.insert(s->input_buffer.end(), bytes, bytes + size);

    if (s->format == AudioFormat::MP3) {
        streaming_mp3_decode(s);
    } else if (s->format == AudioFormat::FLAC) {
        streaming_flac_decode(s);
    }
    return 0;
}

AUDIO_API void AudioDecoder_FeedComplete(void* handle) {
    if (!handle) return;
    auto* s = static_cast<StreamingHandle*>(handle);
    std::lock_guard<std::mutex> lock(s->mutex);
    s->feed_complete = true;

    // Final decode pass
    if (s->format == AudioFormat::MP3) {
        streaming_mp3_decode(s);
    } else if (s->format == AudioFormat::FLAC) {
        streaming_flac_decode(s);
    }
}

AUDIO_API long long AudioDecoder_StreamingRead(
    void* handle,
    float* buffer,
    int frames_to_read)
{
    if (!handle || !buffer || frames_to_read <= 0) return -1;
    auto* s = static_cast<StreamingHandle*>(handle);

    std::lock_guard<std::mutex> lock(s->mutex);

    if (s->is_eof) return -2;

    int ch = s->channels > 0 ? s->channels : 2;
    size_t samples_needed = (size_t)frames_to_read * ch;
    size_t available = s->pcm_buffer.size() - s->pcm_read_pos;

    if (available == 0) {
        // Try to decode more
        if (s->format == AudioFormat::MP3) streaming_mp3_decode(s);
        else if (s->format == AudioFormat::FLAC) streaming_flac_decode(s);
        available = s->pcm_buffer.size() - s->pcm_read_pos;

        if (available == 0) {
            if (s->feed_complete) { s->is_eof = true; return -2; }
            return 0; // no data yet
        }
    }

    size_t to_copy = std::min(available, samples_needed);
    memcpy(buffer, s->pcm_buffer.data() + s->pcm_read_pos, to_copy * sizeof(float));
    s->pcm_read_pos += to_copy;

    // Compact PCM buffer periodically (when >1MB consumed)
    if (s->pcm_read_pos > 262144) {
        s->pcm_buffer.erase(s->pcm_buffer.begin(),
                            s->pcm_buffer.begin() + s->pcm_read_pos);
        s->pcm_read_pos = 0;
    }

    // Compact input buffer: discard bytes already consumed by the decoder
    // Keep a margin for potential re-reads by the decoder
    static const size_t INPUT_COMPACT_THRESHOLD = 1024 * 1024; // 1MB
    static const size_t INPUT_COMPACT_MARGIN = 65536; // 64KB safety margin
    if (s->read_cursor > INPUT_COMPACT_THRESHOLD) {
        size_t discard = s->read_cursor - INPUT_COMPACT_MARGIN;
        s->input_buffer.erase(s->input_buffer.begin(),
                              s->input_buffer.begin() + discard);
        s->read_cursor -= discard;
    }

    return (long long)(to_copy / ch);
}

AUDIO_API int AudioDecoder_StreamingIsReady(void* handle) {
    if (!handle) return 0;
    auto* s = static_cast<StreamingHandle*>(handle);
    std::lock_guard<std::mutex> lock(s->mutex);
    return s->is_ready ? 1 : 0;
}

AUDIO_API int AudioDecoder_StreamingGetInfo(
    void* handle,
    int* out_sample_rate,
    int* out_channels,
    unsigned long long* out_total_frames)
{
    if (!handle) return -1;
    auto* s = static_cast<StreamingHandle*>(handle);
    std::lock_guard<std::mutex> lock(s->mutex);

    if (!s->info_detected) return -1;

    if (out_sample_rate) *out_sample_rate = s->sample_rate;
    if (out_channels) *out_channels = s->channels;
    if (out_total_frames) *out_total_frames = s->total_frames;
    return 0;
}

AUDIO_API void AudioDecoder_CloseStreaming(void* handle) {
    if (!handle) return;
    auto* s = static_cast<StreamingHandle*>(handle);
    if (s->magic != StreamingHandle::MAGIC) return; // safety check
    delete s;
}

} // extern "C"
