#define DR_MP3_IMPLEMENTATION
#define DR_FLAC_IMPLEMENTATION
#define DR_WAV_IMPLEMENTATION
#define MINIMP4_IMPLEMENTATION
#define BUILDING_DLL

#include "dr_mp3.h"
#include "dr_flac.h"
#include "dr_wav.h"
#include "minimp4.h"
#include "audio_decoder.h"

// fdk-aac
#include "aacdecoder_lib.h"

#include <cstring>
#include <cstdlib>
#include <cstdio>
#include <cstdarg>
#include <string>
#include <mutex>
#include <vector>
#include <algorithm>

static thread_local std::string g_last_error;

enum class AudioFormat { MP3, FLAC, WAV, AAC, UNKNOWN };

static AudioFormat detect_format_from_path(const wchar_t* path) {
    if (!path) return AudioFormat::UNKNOWN;
    size_t len = wcslen(path);
    if (len < 4) return AudioFormat::UNKNOWN;
    const wchar_t* ext = path + len;
    while (ext > path && *ext != L'.') ext--;
    if (_wcsicmp(ext, L".mp3") == 0) return AudioFormat::MP3;
    if (_wcsicmp(ext, L".flac") == 0) return AudioFormat::FLAC;
    if (_wcsicmp(ext, L".wav") == 0) return AudioFormat::WAV;
    if (_wcsicmp(ext, L".aac") == 0) return AudioFormat::AAC;
    if (_wcsicmp(ext, L".m4a") == 0) return AudioFormat::AAC;
    if (_wcsicmp(ext, L".mp4") == 0) return AudioFormat::AAC;
    return AudioFormat::UNKNOWN;
}

static AudioFormat parse_format_string(const char* fmt) {
    if (!fmt) return AudioFormat::UNKNOWN;
    if (_stricmp(fmt, "mp3") == 0) return AudioFormat::MP3;
    if (_stricmp(fmt, "flac") == 0) return AudioFormat::FLAC;
    if (_stricmp(fmt, "wav") == 0) return AudioFormat::WAV;
    if (_stricmp(fmt, "aac") == 0) return AudioFormat::AAC;
    if (_stricmp(fmt, "m4a") == 0) return AudioFormat::AAC;
    return AudioFormat::UNKNOWN;
}

// ========== AAC/M4A helper: minimp4 read callback for memory buffer ==========
struct Mp4MemoryBuffer {
    const unsigned char* data;
    size_t size;
};

static int mp4_mem_read_cb(int64_t offset, void* buffer, size_t size, void* token) {
    auto* mem = static_cast<Mp4MemoryBuffer*>(token);
    if (offset < 0 || (size_t)offset + size > mem->size) return 1;
    memcpy(buffer, mem->data + offset, size);
    return 0;
}

// fMP4 sample entry: file offset + size of one AAC frame
struct FMp4Sample {
    size_t offset;
    unsigned size;
};

// AAC file decoder context
struct AacFileContext {
    MP4D_demux_t mp4;
    Mp4MemoryBuffer mem_buf;      // holds entire file content for minimp4
    std::vector<unsigned char> file_data;
    HANDLE_AACDECODER aac_decoder;
    int audio_track;              // index of the audio track in MP4
    unsigned current_sample;      // current sample (frame) index for reading
    int sample_rate;
    int channels;
    unsigned long long total_frames;
    // Buffered PCM from decoded AAC frames (float interleaved)
    std::vector<float> pcm_buf;
    size_t pcm_buf_pos;
    // fMP4 support: manually parsed sample table (used when mp4 sample_count == 0)
    bool is_fmp4;
    std::vector<FMp4Sample> fmp4_samples;

    AacFileContext() : aac_decoder(nullptr), audio_track(-1), current_sample(0),
                       sample_rate(0), channels(0), total_frames(0), pcm_buf_pos(0),
                       is_fmp4(false) {
        memset(&mp4, 0, sizeof(mp4));
        memset(&mem_buf, 0, sizeof(mem_buf));
    }
    ~AacFileContext() {
        if (aac_decoder) aacDecoder_Close(aac_decoder);
        MP4D_close(&mp4);
    }
};

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
    AacFileContext* aac_ctx; // AAC-specific context (separate due to complex lifecycle)
    int sample_rate;
    int channels;
    unsigned long long total_frames;

    FileStreamHandle() : magic(MAGIC), format(AudioFormat::UNKNOWN), mp3(nullptr),
                         aac_ctx(nullptr), sample_rate(0), channels(0), total_frames(0) {}
    ~FileStreamHandle() { close(); magic = 0; }

    void close() {
        switch (format) {
            case AudioFormat::MP3:  if (mp3)  { drmp3_uninit(mp3); free(mp3); mp3 = nullptr; } break;
            case AudioFormat::FLAC: if (flac) { drflac_close(flac); flac = nullptr; } break;
            case AudioFormat::WAV:  if (wav)  { drwav_uninit(wav); free(wav); wav = nullptr; } break;
            case AudioFormat::AAC:  if (aac_ctx) { delete aac_ctx; aac_ctx = nullptr; } break;
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

    // AAC streaming state
    HANDLE_AACDECODER aac_decoder;
    MP4D_demux_t aac_mp4;
    bool mp4_parsed;
    int aac_audio_track;
    unsigned aac_current_sample;
    unsigned aac_total_samples;
    std::vector<unsigned char> aac_file_data_copy; // copy for minimp4 random reads

    // Magic number to distinguish from FileStreamHandle
    static const unsigned int MAGIC = 0x53545245; // "STRE"
    unsigned int magic;

    StreamingHandle()
        : format(AudioFormat::UNKNOWN), read_cursor(0), feed_complete(false),
          pcm_read_pos(0), sample_rate(0), channels(0), total_frames(0),
          info_detected(false), is_ready(false), is_eof(false),
          decoder_initialized(false), mp3_decoder(nullptr), flac_decoder(nullptr),
          aac_decoder(nullptr), mp4_parsed(false), aac_audio_track(-1),
          aac_current_sample(0), aac_total_samples(0),
          magic(MAGIC) {}

    ~StreamingHandle() {
        if (mp3_decoder) { drmp3_uninit(mp3_decoder); free(mp3_decoder); }
        if (flac_decoder) { drflac_close(flac_decoder); }
        if (aac_decoder) { aacDecoder_Close(aac_decoder); }
        if (mp4_parsed) { MP4D_close(&aac_mp4); }
        magic = 0;
    }
};

static const size_t PREFILL_FRAMES = 22050; // ~0.5s at 44100Hz

// ========== AAC helper: decode samples from MP4 track using fdk-aac ==========

// Initialize fdk-aac decoder with ASC (AudioSpecificConfig) from MP4 track DSI
static HANDLE_AACDECODER aac_init_decoder(const unsigned char* dsi, unsigned dsi_bytes) {
    HANDLE_AACDECODER dec = aacDecoder_Open(TT_MP4_RAW, 1);
    if (!dec) return nullptr;

    UCHAR* conf[] = { const_cast<UCHAR*>(dsi) };
    UINT conf_len[] = { dsi_bytes };
    if (aacDecoder_ConfigRaw(dec, conf, conf_len) != AAC_DEC_OK) {
        aacDecoder_Close(dec);
        return nullptr;
    }
    return dec;
}

// Debug logging for AAC decoder — disabled in release builds
#ifdef CHILL_DEBUG_AAC
static void aac_debug_log(const char* fmt, ...) {
    static FILE* logfile = nullptr;
    if (!logfile) {
        char path[MAX_PATH];
        if (GetTempPathA(MAX_PATH, path))
            strcat_s(path, "chill_aac_debug.log");
        else
            strcpy_s(path, "chill_aac_debug.log");
        logfile = fopen(path, "w");
        if (!logfile) return;
    }
    va_list args;
    va_start(args, fmt);
    vfprintf(logfile, fmt, args);
    va_end(args);
    fprintf(logfile, "\n");
    fflush(logfile);
}
#else
#define aac_debug_log(...) ((void)0)
#endif

// Decode one AAC sample/frame from MP4, output float PCM into out_pcm
// Returns number of PCM frames decoded, or 0 on error
static size_t aac_decode_one_frame(
    HANDLE_AACDECODER dec,
    const unsigned char* frame_data, unsigned frame_size,
    int channels,
    std::vector<float>& out_pcm)
{
    UCHAR* in_buf[] = { const_cast<UCHAR*>(frame_data) };
    UINT in_size[] = { frame_size };
    UINT bytes_valid = frame_size;

    AAC_DECODER_ERROR fill_err = aacDecoder_Fill(dec, in_buf, in_size, &bytes_valid);
    if (fill_err != AAC_DEC_OK) {
        aac_debug_log("aacDecoder_Fill failed: 0x%x, frame_size=%u", (int)fill_err, frame_size);
        return 0;
    }

    // Decode - fdk-aac outputs INT_PCM (short) interleaved
    INT_PCM pcm_out[2048 * 8]; // max 2048 samples * 8 channels
    AAC_DECODER_ERROR err = aacDecoder_DecodeFrame(dec, pcm_out, sizeof(pcm_out) / sizeof(INT_PCM), 0);
    if (!IS_OUTPUT_VALID(err)) {
        aac_debug_log("aacDecoder_DecodeFrame failed: 0x%x, frame_size=%u, bytes_valid_after_fill=%u",
                       (int)err, frame_size, bytes_valid);
        return 0;
    }

    CStreamInfo* info = aacDecoder_GetStreamInfo(dec);
    if (!info || info->numChannels <= 0 || info->frameSize <= 0) {
        aac_debug_log("aacDecoder_GetStreamInfo: info=%p, ch=%d, frameSize=%d",
                       info, info ? info->numChannels : -1, info ? info->frameSize : -1);
        return 0;
    }

    aac_debug_log("Decoded frame: ch=%d, frameSize=%d, sr=%d, frame_size_bytes=%u",
                   info->numChannels, info->frameSize, info->sampleRate, frame_size);

    // Convert INT_PCM (short) to float [-1.0, 1.0]
    int total_samples = info->frameSize * info->numChannels;
    for (int i = 0; i < total_samples; i++) {
        out_pcm.push_back((float)pcm_out[i] / 32768.0f);
    }
    return (size_t)info->frameSize;
}

// Find audio track index in MP4
static int aac_find_audio_track(const MP4D_demux_t* mp4) {
    for (unsigned i = 0; i < mp4->track_count; i++) {
        // object_type_indication 0x40 = Audio ISO/IEC 14496-3 (AAC)
        // also check handler_type for 'soun'
        if (mp4->track[i].handler_type == MP4D_HANDLER_TYPE_SOUN)
            return (int)i;
    }
    return -1;
}

// ========== fMP4 (fragmented MP4) parser ==========
// Bilibili DASH audio uses fMP4 where samples are in MOOF/TRAF/TRUN boxes
// instead of the regular MOOV sample table. This parser extracts sample offsets
// and sizes from TRUN boxes paired with MDAT offsets.

static inline uint32_t read_be32(const unsigned char* p) {
    return ((uint32_t)p[0] << 24) | ((uint32_t)p[1] << 16) | ((uint32_t)p[2] << 8) | p[3];
}

static inline uint64_t read_be64(const unsigned char* p) {
    return ((uint64_t)read_be32(p) << 32) | read_be32(p + 4);
}

// Parse fMP4 and fill ctx->fmp4_samples with absolute file offsets and sizes
// Returns true if fMP4 samples were found
static bool aac_parse_fmp4_samples(AacFileContext* ctx) {
    const unsigned char* data = ctx->file_data.data();
    size_t file_size = ctx->file_data.size();
    size_t pos = 0;
    
    // We'll collect TRUN entries paired with the MDAT data offset that follows
    // fMP4 structure: ... [moof [mfhd] [traf [tfhd] [trun]]] [mdat] ...
    
    while (pos + 8 <= file_size) {
        uint32_t box_size = read_be32(data + pos);
        uint32_t box_type = read_be32(data + pos + 4);
        
        if (box_size < 8) break; // invalid
        
        uint64_t actual_size = box_size;
        size_t header_size = 8;
        if (box_size == 1 && pos + 16 <= file_size) {
            actual_size = read_be64(data + pos + 8);
            header_size = 16;
        }
        if (pos + actual_size > file_size) break;
        
        // 'moof' box - contains traf/trun
        if (box_type == 0x6D6F6F66) { // "moof"
            size_t moof_end = pos + (size_t)actual_size;
            size_t moof_start = pos;
            size_t inner = pos + header_size;
            
            // Per-traf state to collect from tfhd, then use in trun
            // We do a single pass: for each traf, process tfhd then trun sequentially
            size_t scan = inner;
            while (scan + 8 <= moof_end) {
                uint32_t isz = read_be32(data + scan);
                uint32_t ityp = read_be32(data + scan + 4);
                if (isz < 8) break;
                uint64_t iactual = isz;
                size_t ihdr = 8;
                if (isz == 1 && scan + 16 <= moof_end) {
                    iactual = read_be64(data + scan + 8);
                    ihdr = 16;
                }
                
                if (ityp == 0x74726166) { // "traf"
                    size_t traf_end = scan + (size_t)iactual;
                    size_t j = scan + ihdr;
                    
                    // Per-traf defaults
                    uint32_t traf_track_id = 0;
                    uint32_t default_sample_size = 0;
                    uint64_t base_data_offset = moof_start; // default per spec
                    bool has_base_data_offset = false;
                    
                    while (j + 8 <= traf_end) {
                        uint32_t jsz = read_be32(data + j);
                        uint32_t jtyp = read_be32(data + j + 4);
                        if (jsz < 8) break;
                        
                        if (jtyp == 0x74666864 && j + 12 <= traf_end) { // "tfhd"
                            uint32_t flags = read_be32(data + j + 8) & 0x00FFFFFF;
                            size_t off = j + 12;
                            // track_id (always present)
                            if (off + 4 <= j + jsz) {
                                traf_track_id = read_be32(data + off);
                                off += 4;
                            }
                            if (flags & 0x000001) { // base_data_offset
                                if (off + 8 <= j + jsz) {
                                    base_data_offset = read_be64(data + off);
                                    has_base_data_offset = true;
                                }
                                off += 8;
                            }
                            if (flags & 0x000002) off += 4; // sample_description_index
                            if (flags & 0x000008) off += 4; // default_sample_duration
                            if (flags & 0x000010) { // default_sample_size
                                if (off + 4 <= j + jsz)
                                    default_sample_size = read_be32(data + off);
                            }
                        }
                        else if (jtyp == 0x7472756E && j + 12 <= traf_end) { // "trun"
                            // Filter by track ID: only process the audio track
                            // MP4 track IDs are 1-based, audio_track is 0-based index
                            if (traf_track_id != 0 && traf_track_id != (uint32_t)(ctx->audio_track + 1)) {
                                j += jsz;
                                continue;
                            }
                            
                            uint32_t ver_flags = read_be32(data + j + 8);
                            uint32_t flags = ver_flags & 0x00FFFFFF;
                            size_t off = j + 12;
                            
                            if (off + 4 > j + jsz) { j += jsz; continue; }
                            uint32_t sample_count = read_be32(data + off);
                            off += 4;
                            
                            // Sanity check: reject absurd sample counts
                            if (sample_count > 1000000) { j += jsz; continue; }
                            
                            int32_t data_offset = 0;
                            if (flags & 0x000001) {
                                if (off + 4 > j + jsz) { j += jsz; continue; }
                                data_offset = (int32_t)read_be32(data + off);
                                off += 4;
                            }
                            
                            if (flags & 0x000004) off += 4; // first_sample_flags
                            
                            // base: base_data_offset if present, else moof_start
                            size_t sample_data_pos = (size_t)base_data_offset + data_offset;
                            
                            for (uint32_t s = 0; s < sample_count; s++) {
                                uint32_t size = default_sample_size;
                                
                                if (flags & 0x000100) { // sample_duration
                                    if (off + 4 > j + jsz) break;
                                    off += 4;
                                }
                                if (flags & 0x000200) { // sample_size
                                    if (off + 4 > j + jsz) break;
                                    size = read_be32(data + off); off += 4;
                                }
                                if (flags & 0x000400) { // sample_flags
                                    if (off + 4 > j + jsz) break;
                                    off += 4;
                                }
                                if (flags & 0x000800) { // sample_composition_time_offset
                                    if (off + 4 > j + jsz) break;
                                    off += 4;
                                }
                                
                                if (size > 0 && sample_data_pos + size <= file_size) {
                                    ctx->fmp4_samples.push_back({sample_data_pos, size});
                                }
                                sample_data_pos += size;
                            }
                        }
                        j += jsz;
                    }
                }
                scan += (size_t)iactual;
            }
        }
        
        pos += (size_t)actual_size;
    }
    
    if (!ctx->fmp4_samples.empty()) {
        ctx->is_fmp4 = true;
        aac_debug_log("fMP4 parsed: %zu samples found", ctx->fmp4_samples.size());
        return true;
    }
    return false;
}

// Open AAC from file data (already loaded into memory)
static AacFileContext* aac_open_from_memory(const std::vector<unsigned char>& file_data) {
    auto* ctx = new AacFileContext();
    ctx->file_data = file_data;
    ctx->mem_buf.data = ctx->file_data.data();
    ctx->mem_buf.size = ctx->file_data.size();

    if (!MP4D_open(&ctx->mp4, mp4_mem_read_cb, &ctx->mem_buf, (int64_t)ctx->mem_buf.size)) {
        g_last_error = "Failed to parse MP4/M4A container";
        delete ctx;
        return nullptr;
    }

    ctx->audio_track = aac_find_audio_track(&ctx->mp4);
    if (ctx->audio_track < 0) {
        g_last_error = "No audio track found in MP4/M4A";
        delete ctx;
        return nullptr;
    }

    auto* tr = &ctx->mp4.track[ctx->audio_track];
    if (!tr->dsi || tr->dsi_bytes == 0) {
        g_last_error = "No AudioSpecificConfig (DSI) in MP4 audio track";
        delete ctx;
        return nullptr;
    }

    ctx->aac_decoder = aac_init_decoder(tr->dsi, tr->dsi_bytes);
    if (!ctx->aac_decoder) {
        g_last_error = "Failed to initialize fdk-aac decoder";
        delete ctx;
        return nullptr;
    }

    // Get audio info from DSI
    ctx->sample_rate = (int)tr->SampleDescription.audio.samplerate_hz;
    ctx->channels = (int)tr->SampleDescription.audio.channelcount;
    ctx->total_frames = 0;

    // Decode first frame to get actual info from fdk-aac
    if (tr->sample_count > 0) {
        unsigned frame_bytes = 0, timestamp = 0, duration = 0;
        MP4D_file_offset_t ofs = MP4D_frame_offset(
            &ctx->mp4, ctx->audio_track, 0, &frame_bytes, &timestamp, &duration);
        aac_debug_log("First frame: ofs=%llu, frame_bytes=%u, file_size=%zu, sample_count=%u, dsi_bytes=%u",
                       (unsigned long long)ofs, frame_bytes, ctx->file_data.size(), tr->sample_count, tr->dsi_bytes);
        if (frame_bytes > 0 && (size_t)ofs + frame_bytes <= ctx->file_data.size()) {
            const unsigned char* fdata = ctx->file_data.data() + ofs;
            std::vector<float> tmp;
            size_t decoded = aac_decode_one_frame(ctx->aac_decoder, fdata, frame_bytes, ctx->channels, tmp);
            aac_debug_log("First frame decode result: decoded=%zu, tmp.size=%zu", decoded, tmp.size());

            CStreamInfo* si = aacDecoder_GetStreamInfo(ctx->aac_decoder);
            if (si && si->sampleRate > 0) {
                ctx->sample_rate = si->sampleRate;
                ctx->channels = si->numChannels;
                aac_debug_log("Detected: sr=%d, ch=%d", ctx->sample_rate, ctx->channels);
            }
            // Store first frame PCM
            ctx->pcm_buf = std::move(tmp);
            ctx->current_sample = 1;
        }
        // Estimate total frames from MP4 duration
        if (tr->timescale > 0) {
            unsigned long long dur = ((unsigned long long)tr->duration_hi << 32) | tr->duration_lo;
            ctx->total_frames = (dur * ctx->sample_rate) / tr->timescale;
        }
    }
    else {
        // sample_count == 0: likely fMP4 (fragmented MP4, e.g. Bilibili DASH)
        // Parse MOOF/TRUN boxes to build sample table
        aac_debug_log("sample_count=0, attempting fMP4 parse...");
        if (aac_parse_fmp4_samples(ctx) && !ctx->fmp4_samples.empty()) {
            aac_debug_log("fMP4: found %zu samples, decoding first frame...",
                           ctx->fmp4_samples.size());
            // Decode first frame to detect actual sample rate / channels
            auto& first = ctx->fmp4_samples[0];
            if (first.offset + first.size <= ctx->file_data.size()) {
                std::vector<float> tmp;
                size_t decoded = aac_decode_one_frame(ctx->aac_decoder,
                    ctx->file_data.data() + first.offset, first.size, ctx->channels, tmp);
                aac_debug_log("fMP4 first frame: decoded=%zu, tmp.size=%zu", decoded, tmp.size());

                CStreamInfo* si = aacDecoder_GetStreamInfo(ctx->aac_decoder);
                if (si && si->sampleRate > 0) {
                    ctx->sample_rate = si->sampleRate;
                    ctx->channels = si->numChannels;
                    aac_debug_log("fMP4 detected: sr=%d, ch=%d", ctx->sample_rate, ctx->channels);
                }
                ctx->pcm_buf = std::move(tmp);
                ctx->current_sample = 1;
            }
            // Estimate total frames: sample_count * 1024 (typical AAC frame size)
            ctx->total_frames = (unsigned long long)ctx->fmp4_samples.size() * 1024;
            // Also try MP4 duration if available
            if (tr->timescale > 0 && ctx->sample_rate > 0) {
                unsigned long long dur = ((unsigned long long)tr->duration_hi << 32) | tr->duration_lo;
                if (dur > 0) {
                    unsigned long long frames_from_dur = (dur * ctx->sample_rate) / tr->timescale;
                    if (frames_from_dur > 0)
                        ctx->total_frames = frames_from_dur;
                }
            }
        } else {
            g_last_error = "No samples found (neither regular MP4 nor fMP4)";
            delete ctx;
            return nullptr;
        }
    }

    return ctx;
}

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
    case AudioFormat::AAC: {
        // Load entire file into memory for minimp4 random access
        FILE* f = _wfopen(file_path, L"rb");
        if (!f) { g_last_error = "Failed to open AAC/M4A file"; delete h; return nullptr; }
        fseek(f, 0, SEEK_END);
        long fsize = ftell(f);
        fseek(f, 0, SEEK_SET);
        std::vector<unsigned char> fdata((size_t)fsize);
        fread(fdata.data(), 1, (size_t)fsize, f);
        fclose(f);

        h->aac_ctx = aac_open_from_memory(fdata);
        if (!h->aac_ctx) { delete h; return nullptr; }
        h->sample_rate = h->aac_ctx->sample_rate;
        h->channels = h->aac_ctx->channels;
        h->total_frames = h->aac_ctx->total_frames;
        if (out_format) strcpy(out_format, "aac");
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
    case AudioFormat::AAC: {
        auto* ctx = h->aac_ctx;
        if (!ctx) { g_last_error = "AAC context is NULL"; return -1; }
        int ch = ctx->channels > 0 ? ctx->channels : 2;
        size_t samples_needed = (size_t)frames_to_read * ch;

        if (ctx->is_fmp4) {
            // fMP4 mode: use manually parsed sample table
            unsigned total_samples = (unsigned)ctx->fmp4_samples.size();
            aac_debug_log("ReadFrames(fMP4): frames_to_read=%d, ch=%d, pcm_buf=%zu, pcm_buf_pos=%zu, current_sample=%u, total=%u",
                           frames_to_read, ch, ctx->pcm_buf.size(), ctx->pcm_buf_pos, ctx->current_sample, total_samples);
            while (ctx->pcm_buf.size() - ctx->pcm_buf_pos < samples_needed
                   && ctx->current_sample < total_samples) {
                auto& sample = ctx->fmp4_samples[ctx->current_sample];
                ctx->current_sample++;
                if (sample.offset + sample.size > ctx->file_data.size())
                    continue;
                aac_decode_one_frame(ctx->aac_decoder,
                    ctx->file_data.data() + sample.offset, sample.size, ch, ctx->pcm_buf);
            }
        } else {
            // Regular MP4 mode: use minimp4 sample table
            auto* tr = &ctx->mp4.track[ctx->audio_track];
            aac_debug_log("ReadFrames: frames_to_read=%d, ch=%d, pcm_buf=%zu, pcm_buf_pos=%zu, current_sample=%u, sample_count=%u",
                           frames_to_read, ch, ctx->pcm_buf.size(), ctx->pcm_buf_pos, ctx->current_sample, tr->sample_count);
            while (ctx->pcm_buf.size() - ctx->pcm_buf_pos < samples_needed
                   && ctx->current_sample < tr->sample_count) {
                unsigned frame_bytes = 0, timestamp = 0, duration = 0;
                MP4D_file_offset_t ofs = MP4D_frame_offset(
                    &ctx->mp4, ctx->audio_track, ctx->current_sample,
                    &frame_bytes, &timestamp, &duration);
                ctx->current_sample++;
                if (frame_bytes == 0 || (size_t)ofs + frame_bytes > ctx->file_data.size())
                    continue;
                aac_decode_one_frame(ctx->aac_decoder,
                    ctx->file_data.data() + ofs, frame_bytes, ch, ctx->pcm_buf);
            }
        }

        size_t avail = ctx->pcm_buf.size() - ctx->pcm_buf_pos;
        if (avail == 0) return 0;
        size_t to_copy = std::min(avail, samples_needed);
        memcpy(buffer, ctx->pcm_buf.data() + ctx->pcm_buf_pos, to_copy * sizeof(float));
        ctx->pcm_buf_pos += to_copy;
        // Compact
        if (ctx->pcm_buf_pos > 262144) {
            ctx->pcm_buf.erase(ctx->pcm_buf.begin(), ctx->pcm_buf.begin() + ctx->pcm_buf_pos);
            ctx->pcm_buf_pos = 0;
        }
        return (long long)(to_copy / ch);
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
    case AudioFormat::AAC: {
        auto* ctx = h->aac_ctx;
        if (!ctx) { g_last_error = "AAC context is NULL"; return -1; }
        auto* tr = &ctx->mp4.track[ctx->audio_track];
        // Re-create fdk-aac decoder to flush internal state
        if (ctx->aac_decoder) aacDecoder_Close(ctx->aac_decoder);
        ctx->aac_decoder = aac_init_decoder(tr->dsi, tr->dsi_bytes);
        if (!ctx->aac_decoder) { g_last_error = "AAC seek: failed to reinit decoder"; return -1; }
        ctx->pcm_buf.clear();
        ctx->pcm_buf_pos = 0;
        // Find the MP4 sample corresponding to frame_index
        // Each AAC frame typically has 1024 PCM frames
        unsigned target_sample = (unsigned)(frame_index / 1024);
        
        // Clamp target_sample and compute start for decoder priming
        if (ctx->is_fmp4) {
            unsigned total = (unsigned)ctx->fmp4_samples.size();
            if (target_sample >= total) target_sample = total > 0 ? total - 1 : 0;
        } else {
            if (target_sample >= tr->sample_count) target_sample = tr->sample_count > 0 ? tr->sample_count - 1 : 0;
        }
        unsigned start = target_sample > 2 ? target_sample - 2 : 0;
        
        if (ctx->is_fmp4) {
            unsigned total = (unsigned)ctx->fmp4_samples.size();
            for (unsigned i = start; i <= target_sample && i < total; i++) {
                auto& sample = ctx->fmp4_samples[i];
                if (sample.offset + sample.size <= ctx->file_data.size()) {
                    aac_decode_one_frame(ctx->aac_decoder, ctx->file_data.data() + sample.offset, sample.size, ctx->channels, ctx->pcm_buf);
                }
            }
        } else {
            for (unsigned i = start; i <= target_sample && i < tr->sample_count; i++) {
                unsigned frame_bytes = 0, timestamp = 0, duration = 0;
                MP4D_file_offset_t ofs = MP4D_frame_offset(&ctx->mp4, ctx->audio_track, i, &frame_bytes, &timestamp, &duration);
                if (frame_bytes > 0 && (size_t)ofs + frame_bytes <= ctx->file_data.size()) {
                    aac_decode_one_frame(ctx->aac_decoder, ctx->file_data.data() + ofs, frame_bytes, ctx->channels, ctx->pcm_buf);
                }
            }
        }
        size_t skip_frames = (size_t)(frame_index - (unsigned long long)start * 1024);
        size_t skip_samples = skip_frames * ctx->channels;
        if (skip_samples > ctx->pcm_buf.size()) skip_samples = ctx->pcm_buf.size();
        ctx->pcm_buf_pos = skip_samples;
        ctx->current_sample = target_sample + 1;
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

// ---- AAC/M4A streaming: parse MP4 container and decode AAC frames ----
// minimp4 read callback for streaming: reads from aac_file_data_copy
static int streaming_mp4_read_cb(int64_t offset, void* buffer, size_t size, void* token) {
    auto* s = static_cast<StreamingHandle*>(token);
    if (offset < 0 || (size_t)offset + size > s->aac_file_data_copy.size()) return 1;
    memcpy(buffer, s->aac_file_data_copy.data() + offset, size);
    return 0;
}

// Caller must hold s->mutex
static void streaming_aac_decode(StreamingHandle* s) {
    // AAC in M4A: we need the full moov atom before we can parse.
    // We attempt parsing once feed_complete or enough data available.
    if (!s->mp4_parsed) {
        // Need at least some data; try parsing when we have a decent amount or feed is done
        if (s->input_buffer.size() < 32768 && !s->feed_complete) return;

        // Make a snapshot copy for minimp4 random access
        s->aac_file_data_copy = s->input_buffer;

        memset(&s->aac_mp4, 0, sizeof(s->aac_mp4));
        if (!MP4D_open(&s->aac_mp4, streaming_mp4_read_cb, s, (int64_t)s->aac_file_data_copy.size())) {
            // Not enough data yet, or invalid
            if (!s->feed_complete) return;
            // Feed is complete but parse failed
            s->is_eof = true;
            return;
        }
        s->mp4_parsed = true;

        s->aac_audio_track = aac_find_audio_track(&s->aac_mp4);
        if (s->aac_audio_track < 0) {
            s->is_eof = true;
            return;
        }

        auto* tr = &s->aac_mp4.track[s->aac_audio_track];
        s->aac_total_samples = tr->sample_count;

        if (!tr->dsi || tr->dsi_bytes == 0) {
            s->is_eof = true;
            return;
        }

        s->aac_decoder = aac_init_decoder(tr->dsi, tr->dsi_bytes);
        if (!s->aac_decoder) {
            s->is_eof = true;
            return;
        }

        s->sample_rate = (int)tr->SampleDescription.audio.samplerate_hz;
        s->channels = (int)tr->SampleDescription.audio.channelcount;
        if (tr->timescale > 0) {
            unsigned long long dur = ((unsigned long long)tr->duration_hi << 32) | tr->duration_lo;
            s->total_frames = (dur * s->sample_rate) / tr->timescale;
        }
        s->decoder_initialized = true;
        s->aac_current_sample = 0;
    }

    if (!s->aac_decoder || s->aac_audio_track < 0) return;

    // Decode more AAC frames
    auto* tr = &s->aac_mp4.track[s->aac_audio_track];
    int ch = s->channels > 0 ? s->channels : 2;
    int frames_decoded = 0;
    while (s->aac_current_sample < s->aac_total_samples && frames_decoded < 16) {
        unsigned frame_bytes = 0, timestamp = 0, duration = 0;
        MP4D_file_offset_t ofs = MP4D_frame_offset(
            &s->aac_mp4, s->aac_audio_track, s->aac_current_sample,
            &frame_bytes, &timestamp, &duration);
        s->aac_current_sample++;
        if (frame_bytes == 0) continue;
        if ((size_t)ofs + frame_bytes > s->aac_file_data_copy.size()) {
            // Not enough data downloaded yet
            s->aac_current_sample--;
            break;
        }
        size_t decoded = aac_decode_one_frame(s->aac_decoder,
            s->aac_file_data_copy.data() + ofs, frame_bytes, ch, s->pcm_buffer);

        if (decoded > 0 && !s->info_detected) {
            CStreamInfo* si = aacDecoder_GetStreamInfo(s->aac_decoder);
            if (si && si->sampleRate > 0) {
                s->sample_rate = si->sampleRate;
                s->channels = si->numChannels;
            }
            s->info_detected = true;
        }
        frames_decoded++;
    }

    // Check readiness
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
    } else if (s->format == AudioFormat::AAC) {
        streaming_aac_decode(s);
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
    } else if (s->format == AudioFormat::AAC) {
        streaming_aac_decode(s);
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
        else if (s->format == AudioFormat::AAC) streaming_aac_decode(s);
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
