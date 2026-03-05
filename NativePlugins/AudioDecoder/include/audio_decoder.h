#ifndef CHILL_AUDIO_DECODER_H
#define CHILL_AUDIO_DECODER_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
    #ifdef BUILDING_DLL
        #define AUDIO_API __declspec(dllexport)
    #else
        #define AUDIO_API __declspec(dllimport)
    #endif
#else
    #define AUDIO_API
#endif

// ========== 文件流式解码 API (可寻址) ==========

/**
 * 打开音频文件用于流式读取 (自动检测格式: MP3/FLAC/WAV)
 *
 * @param file_path 文件路径 (宽字符)
 * @param out_sample_rate 输出采样率
 * @param out_channels 输出声道数
 * @param out_total_frames 输出总帧数
 * @param out_format 输出格式字符串 ("mp3"/"flac"/"wav"), 至少 16 字节
 * @return 流句柄, 失败返回 NULL
 */
AUDIO_API void* AudioDecoder_OpenFile(
    const wchar_t* file_path,
    int* out_sample_rate,
    int* out_channels,
    unsigned long long* out_total_frames,
    char* out_format);

/**
 * 从流读取 PCM 帧 (交错 float32, [-1.0, 1.0])
 *
 * @return 实际读取帧数, 0=EOF, -1=错误
 */
AUDIO_API long long AudioDecoder_ReadFrames(
    void* handle,
    float* buffer,
    int frames_to_read);

/**
 * 寻址到指定帧 (O(1) 对于文件流)
 *
 * @return 0=成功, -1=失败
 */
AUDIO_API int AudioDecoder_Seek(void* handle, unsigned long long frame_index);

/**
 * 关闭流并释放资源
 */
AUDIO_API void AudioDecoder_Close(void* handle);

/**
 * 获取最后的错误信息
 */
AUDIO_API const char* AudioDecoder_GetLastError(void);

// ========== 增量流式解码 API (边下边播) ==========

/**
 * 创建增量流式解码器
 *
 * @param format "mp3" / "flac" / "aac" / "m4a"
 * @return 解码器句柄, 失败返回 NULL
 */
AUDIO_API void* AudioDecoder_CreateStreaming(const char* format);

/**
 * 向增量解码器喂入数据块
 *
 * @param data 音频数据块
 * @param size 数据大小 (字节)
 * @return 0=成功, -1=错误
 */
AUDIO_API int AudioDecoder_FeedData(void* handle, const void* data, int size);

/**
 * 通知增量解码器已收到全部数据
 */
AUDIO_API void AudioDecoder_FeedComplete(void* handle);

/**
 * 从增量解码器读取已解码的 PCM 帧
 *
 * @return 实际读取帧数, 0=暂无数据, -1=错误, -2=EOF
 */
AUDIO_API long long AudioDecoder_StreamingRead(
    void* handle,
    float* buffer,
    int frames_to_read);

/**
 * 查询增量解码器是否准备好输出
 * 有足够预缓冲数据时返回 1
 *
 * @return 1=就绪, 0=未就绪
 */
AUDIO_API int AudioDecoder_StreamingIsReady(void* handle);

/**
 * 获取增量解码器已检测到的音频信息
 *
 * @return 0=成功(信息已填入), -1=尚未检测到
 */
AUDIO_API int AudioDecoder_StreamingGetInfo(
    void* handle,
    int* out_sample_rate,
    int* out_channels,
    unsigned long long* out_total_frames);

/**
 * 关闭增量流式解码器并释放资源
 * (与 AudioDecoder_Close 分开, 因为内部结构不同)
 */
AUDIO_API void AudioDecoder_CloseStreaming(void* handle);

#ifdef __cplusplus
}
#endif

#endif // CHILL_AUDIO_DECODER_H
