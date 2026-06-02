using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMixPlayer.SDK.Interfaces
{
    #region 核心枚举

    /// <summary>
    /// 音频来源类型
    /// </summary>
    /// <summary>
    /// 音频格式
    /// </summary>
    public enum PlayableSourceType
    {
        Local = 0,
        Cached = 1,
        Remote = 2
    }

    public enum AudioFormat
    {
        Unknown = 0,
        Mp3 = 1,
        Ogg = 2,
        Wav = 3,
        Flac = 4,
        Aac = 5
    }

    /// <summary>
    /// 音质等级
    /// </summary>
    public enum AudioQuality
    {
        /// <summary>标准 (128kbps)</summary>
        Standard = 0,
        /// <summary>较高 (192kbps)</summary>
        Higher = 1,
        /// <summary>极高 (320kbps)</summary>
        ExHigh = 2,
        /// <summary>无损 (FLAC)</summary>
        Lossless = 3,
        /// <summary>Hi-Res (24bit)</summary>
        HiRes = 4,
        /// <summary>高清环绕声</summary>
        JYEffect = 5,
        /// <summary>沉浸环绕声</summary>
        Sky = 6,
        /// <summary>超清母带</summary>
        JYMaster = 7
    }

    #endregion

    #region PCM 流式接口

    /// <summary>
    /// PCM 音频流信息
    /// 用于模块提供解码后的 PCM 数据
    /// </summary>
    public class PcmStreamInfo
    {
        /// <summary>采样率 (Hz)</summary>
        public int SampleRate { get; set; }

        /// <summary>声道数</summary>
        public int Channels { get; set; }

        /// <summary>总 PCM 帧数（0 表示未知/流式）</summary>
        public ulong TotalFrames { get; set; }

        /// <summary>音频格式 ("mp3", "flac" 等)</summary>
        public string Format { get; set; }

        /// <summary>是否支持 Seek</summary>
        public bool CanSeek { get; set; }

        /// <summary>时长（秒），0 表示未知</summary>
        public float Duration => TotalFrames > 0 && SampleRate > 0 
            ? (float)TotalFrames / SampleRate 
            : 0f;
    }

    /// <summary>
    /// PCM 数据读取器接口
    /// 模块实现此接口来提供流式解码的 PCM 数据
    /// 
    /// 使用场景：
    /// - 流媒体 FLAC（模块负责下载 + 解码）
    /// - 其他 Unity 不原生支持的格式
    /// 
    /// 工作流程：
    /// 1. 模块从 URL 下载数据到缓存
    /// 2. 模块使用解码器（如 dr_flac）解码
    /// 3. 主插件调用 ReadFrames 获取 PCM 数据
    /// 4. 主插件使用 PCMReaderCallback 填充 AudioClip
    /// </summary>
    public interface IPcmStreamReader : IDisposable
    {
        /// <summary>PCM 流信息</summary>
        PcmStreamInfo Info { get; }

        /// <summary>当前帧位置</summary>
        ulong CurrentFrame { get; }

        /// <summary>是否已到达末尾</summary>
        bool IsEndOfStream { get; }

        /// <summary>是否已准备好读取（有足够的缓冲数据）</summary>
        bool IsReady { get; }

        /// <summary>
        /// 读取 PCM 帧
        /// </summary>
        /// <param name="buffer">交错格式的 float 缓冲区（长度 = 帧数 * 声道数）</param>
        /// <param name="framesToRead">要读取的帧数</param>
        /// <returns>实际读取的帧数，-1 表示错误</returns>
        long ReadFrames(float[] buffer, int framesToRead);

        /// <summary>
        /// 定位到指定帧
        /// </summary>
        /// <param name="frameIndex">目标帧索引</param>
        /// <returns>是否成功（或已设置延迟 Seek）</returns>
        bool Seek(ulong frameIndex);

        #region Seek 支持（流媒体扩展）

        /// <summary>
        /// 是否支持 Seek 操作
        /// 流媒体在缓存下载完成前可能不支持
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// 是否有待定的 Seek 操作（缓存未完成时设置的延迟 Seek）
        /// </summary>
        bool HasPendingSeek { get; }

        /// <summary>
        /// 待定 Seek 的目标帧（-1 表示无待定）
        /// </summary>
        long PendingSeekFrame { get; }

        /// <summary>
        /// 取消待定的 Seek 操作
        /// </summary>
        void CancelPendingSeek();

        #endregion

        #region 缓存进度（流媒体扩展）

        /// <summary>
        /// 缓存下载进度（0-100）
        /// 边下边播时，可用于显示缓冲进度
        /// 如果不支持或不适用，返回 -1
        /// </summary>
        double CacheProgress { get; }

        /// <summary>
        /// 缓存是否下载完成
        /// </summary>
        bool IsCacheComplete { get; }

        #endregion
    }

    #endregion

    #region 核心接口

    public class PlayableSource
    {
        public string UUID { get; set; }
        public PlayableSourceType SourceType { get; set; }
        public string LocalPath { get; set; }
        public string Url { get; set; }
        public AudioFormat Format { get; set; }
        public AudioQuality Quality { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public long? FileSize { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string CacheKey { get; set; }
        public string CachePath { get; set; }
        public bool UseCachePath { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;
        public bool IsRemote => SourceType == PlayableSourceType.Remote;
        public string GetPath() => IsRemote ? Url : LocalPath;

        public static PlayableSource FromLocal(string uuid, string path, AudioFormat format = AudioFormat.Unknown)
        {
            return new PlayableSource
            {
                UUID = uuid,
                SourceType = PlayableSourceType.Local,
                LocalPath = path,
                Format = format != AudioFormat.Unknown ? format : AudioFormatExtensions.FromExtension(System.IO.Path.GetExtension(path))
            };
        }

        public static PlayableSource FromUrl(string uuid, string url, AudioFormat format, DateTime? expiresAt = null)
        {
            return new PlayableSource
            {
                UUID = uuid,
                SourceType = PlayableSourceType.Remote,
                Url = url,
                Format = format,
                ExpiresAt = expiresAt
            };
        }

    }

    public interface IPlayableSourceResolver
    {
        Task<PlayableSource> ResolveAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default);

        Task<PlayableSource> RefreshUrlAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
     /// Module-owned audio decoder provider.
     /// Implement this when a module needs to decode a track itself and expose
    /// decoded float PCM to the backend playback/shared-memory pipeline.
    /// </summary>
    public interface IModuleAudioDecoderProvider
    {
        /// <summary>
        /// Return true when this module can provide decoded PCM for the track.
        /// </summary>
        bool CanDecode(string uuid);

        /// <summary>
        /// Create a PCM reader for the track. Return null to let the backend try
        /// other module playback paths.
        /// </summary>
        Task<IPcmStreamReader> CreateDecoderAsync(
            string uuid,
            AudioQuality quality = AudioQuality.ExHigh,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 流媒体音乐源提供器
    /// 继承 IMusicSourceProvider，增加 URL 解析能力
    /// 
    /// 模块职责：
    /// - 注册固定的流媒体歌单
    /// - 处理登录认证（模块内部解决）
    /// - 提供 URL 解析
    /// </summary>
    public interface IStreamingMusicSourceProvider : IMusicSourceProvider, IPlayableSourceResolver
    {
        /// <summary>
        /// 是否已就绪（如登录状态等）
        /// 主插件可以根据此状态显示/隐藏该模块的内容
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 就绪状态变化事件
        /// </summary>
        event Action<bool> OnReadyStateChanged;
    }

    #endregion

    #region 辅助扩展

    /// <summary>
    /// AudioFormat 扩展方法
    /// </summary>
    public static class AudioFormatExtensions
    {
        /// <summary>
        /// 从文件扩展名解析音频格式
        /// </summary>
        public static AudioFormat FromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return AudioFormat.Unknown;

            extension = extension.TrimStart('.').ToLowerInvariant();
            return extension switch
            {
                "mp3" => AudioFormat.Mp3,
                "ogg" => AudioFormat.Ogg,
                "wav" => AudioFormat.Wav,
                "flac" => AudioFormat.Flac,
                "aac" or "m4a" => AudioFormat.Aac,
                _ => AudioFormat.Unknown
            };
        }

        /// <summary>
        /// 获取格式的文件扩展名
        /// </summary>
        public static string ToExtension(this AudioFormat format)
        {
            return format switch
            {
                AudioFormat.Mp3 => ".mp3",
                AudioFormat.Ogg => ".ogg",
                AudioFormat.Wav => ".wav",
                AudioFormat.Flac => ".flac",
                AudioFormat.Aac => ".aac",
                _ => ""
            };
        }

        /// <summary>
        /// 检查格式是否被 Unity 原生支持（不需要自定义解码器）
        /// </summary>
        public static bool IsUnitySupportedNatively(this AudioFormat format)
        {
            return format switch
            {
                AudioFormat.Mp3 or AudioFormat.Ogg or AudioFormat.Wav or AudioFormat.Aac => true,
                AudioFormat.Flac => false, // 需要自定义解码器
                _ => false
            };
        }
    }

    #endregion
}
