using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using ChillPatcher.Native;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.ModuleSystem.Services.Streaming
{
    /// <summary>
    /// 核心流式服务
    /// 实现 IStreamingService 接口, 模块通过 IModuleContext.StreamingService 获取
    /// 
    /// 用法:
    ///   var reader = context.StreamingService.CreateStream(
    ///       url, "mp3", duration, "mysong_123",
    ///       new Dictionary&lt;string, string&gt; { ["Referer"] = "https://example.com" });
    ///   return PlayableSource.FromPcmStream(uuid, reader, AudioFormat.Mp3);
    /// </summary>
    public class CoreStreamingService : IStreamingService
    {
        private static readonly ManualLogSource Logger =
            BepInEx.Logging.Logger.CreateLogSource("CoreStreaming");

        /// <summary>全局单例</summary>
        public static readonly CoreStreamingService Instance = new CoreStreamingService();

        public bool IsAvailable => AudioDecoder.IsAvailable;

        public IPcmStreamReader CreateStream(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            Dictionary<string, string> headers = null)
        {
            if (!AudioDecoder.IsAvailable)
            {
                Logger.LogError("AudioDecoder native plugin is not available!");
                return null;
            }

            Logger.LogInfo($"Creating stream: format={format}, " +
                           $"duration={durationSeconds:F1}s, key={cacheKey}");

            return new CorePcmStreamReader(
                url, format, durationSeconds, cacheKey, headers);
        }

        public IPcmStreamReader CreateStreamAndWait(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            int timeoutMs = 20000,
            Dictionary<string, string> headers = null)
        {
            var reader = CreateStream(url, format, durationSeconds,
                                      cacheKey, headers);
            if (reader == null) return null;

            if (!WaitForReady(reader, timeoutMs))
            {
                Logger.LogWarning($"Stream not ready within {timeoutMs}ms, disposing");
                reader.Dispose();
                return null;
            }

            return reader;
        }

        public bool WaitForReady(IPcmStreamReader reader, int timeoutMs)
        {
            int elapsed = 0;
            while (!reader.IsReady && elapsed < timeoutMs)
            {
                Thread.Sleep(50);
                elapsed += 50;
            }
            return reader.IsReady;
        }

        public async Task<IPcmStreamReader> CreateStreamAndWaitAsync(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            int timeoutMs = 20000,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var reader = CreateStream(url, format, durationSeconds,
                                      cacheKey, headers);
            if (reader == null) return null;

            if (!await WaitForReadyAsync(reader, timeoutMs, cancellationToken))
            {
                Logger.LogWarning($"Stream not ready within {timeoutMs}ms, disposing");
                reader.Dispose();
                return null;
            }

            return reader;
        }

        public async Task<bool> WaitForReadyAsync(IPcmStreamReader reader, int timeoutMs, CancellationToken cancellationToken = default)
        {
            int elapsed = 0;
            while (!reader.IsReady && elapsed < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                elapsed += 50;
            }
            return reader.IsReady;
        }
    }
}
