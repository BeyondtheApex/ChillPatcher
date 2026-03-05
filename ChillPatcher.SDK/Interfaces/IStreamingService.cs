using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// 流式音频服务接口
    /// 由主插件提供，模块通过 IModuleContext 获取
    /// 封装了 CoreStreamingService / AudioDecoder 原生解码能力
    /// </summary>
    public interface IStreamingService
    {
        /// <summary>
        /// 原生解码器是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 创建 PCM 流式读取器 (边下边播)
        /// </summary>
        /// <param name="url">音频文件 URL</param>
        /// <param name="format">"mp3", "flac", "wav"</param>
        /// <param name="durationSeconds">预估时长 (秒), 用于计算 TotalFrames</param>
        /// <param name="cacheKey">缓存标识 (如 "netease_12345")</param>
        /// <param name="headers">可选 HTTP 请求头</param>
        /// <returns>可用于 PlayableSource.FromPcmStream 的读取器, 不可用时返回 null</returns>
        IPcmStreamReader CreateStream(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            Dictionary<string, string> headers = null);

        /// <summary>
        /// 创建流并等待就绪
        /// </summary>
        /// <param name="url">音频文件 URL</param>
        /// <param name="format">"mp3", "flac", "wav"</param>
        /// <param name="durationSeconds">预估时长 (秒)</param>
        /// <param name="cacheKey">缓存标识</param>
        /// <param name="timeoutMs">等待超时 (毫秒)</param>
        /// <param name="headers">可选 HTTP 请求头</param>
        IPcmStreamReader CreateStreamAndWait(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            int timeoutMs = 20000,
            Dictionary<string, string> headers = null);

        /// <summary>
        /// 等待读取器就绪
        /// </summary>
        bool WaitForReady(IPcmStreamReader reader, int timeoutMs);

        /// <summary>
        /// 创建流并异步等待就绪（不阻塞主线程）
        /// </summary>
        Task<IPcmStreamReader> CreateStreamAndWaitAsync(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            int timeoutMs = 20000,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步等待读取器就绪（不阻塞主线程）
        /// </summary>
        Task<bool> WaitForReadyAsync(IPcmStreamReader reader, int timeoutMs, CancellationToken cancellationToken = default);
    }
}
