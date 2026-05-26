using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem.Services.Streaming
{
    public class CoreStreamingService : IStreamingService
    {
        private readonly ILogger _logger;
        public static CoreStreamingService Instance { get; private set; }

        public bool IsAvailable => true;

        public CoreStreamingService(ILogger logger)
        {
            _logger = logger;
            Instance = this;
        }

        public IPcmStreamReader CreateStream(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            Dictionary<string, string> headers = null)
        {
            _logger.LogInformation("Creating stream: format={Format}, duration={Duration:F1}s, key={Key}", format, durationSeconds, cacheKey);
            return new CorePcmStreamReader(url, format, durationSeconds, cacheKey, headers);
        }

        public IPcmStreamReader CreateStreamAndWait(
            string url,
            string format,
            float durationSeconds,
            string cacheKey,
            int timeoutMs = 20000,
            Dictionary<string, string> headers = null)
        {
            var reader = CreateStream(url, format, durationSeconds, cacheKey, headers);
            if (reader == null) return null;
            if (!WaitForReady(reader, timeoutMs)) { _logger.LogWarning("Stream not ready within {Timeout}ms, disposing", timeoutMs); reader.Dispose(); return null; }
            return reader;
        }

        public bool WaitForReady(IPcmStreamReader reader, int timeoutMs)
        {
            int elapsed = 0;
            while (!reader.IsReady && elapsed < timeoutMs) { Thread.Sleep(50); elapsed += 50; }
            return reader.IsReady;
        }

        public async Task<IPcmStreamReader> CreateStreamAndWaitAsync(
            string url, string format, float durationSeconds, string cacheKey,
            int timeoutMs = 20000, Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var reader = CreateStream(url, format, durationSeconds, cacheKey, headers);
            if (reader == null) return null;
            if (!await WaitForReadyAsync(reader, timeoutMs, cancellationToken)) { _logger.LogWarning("Stream not ready within {Timeout}ms, disposing", timeoutMs); reader.Dispose(); return null; }
            return reader;
        }

        public IPcmStreamReader CreateStream(
            string url, string format, float durationSeconds, string cachePath,
            Dictionary<string, string> headers, bool useCachePath)
        {
            _logger.LogInformation("Creating stream: format={Format}, duration={Duration:F1}s, cachePath={CachePath}", format, durationSeconds, cachePath);
            return new CorePcmStreamReader(url, format, durationSeconds, cachePath, headers, useCachePath);
        }

        public async Task<IPcmStreamReader> CreateStreamAndWaitAsync(
            string url, string format, float durationSeconds, string cachePath,
            int timeoutMs, Dictionary<string, string> headers,
            CancellationToken cancellationToken, bool useCachePath)
        {
            var reader = CreateStream(url, format, durationSeconds, cachePath, headers, useCachePath);
            if (reader == null) return null;
            if (!await WaitForReadyAsync(reader, timeoutMs, cancellationToken)) { _logger.LogWarning("Stream not ready within {Timeout}ms, disposing", timeoutMs); reader.Dispose(); return null; }
            return reader;
        }

        public async Task<bool> WaitForReadyAsync(IPcmStreamReader reader, int timeoutMs, CancellationToken cancellationToken = default)
        {
            int elapsed = 0;
            while (!reader.IsReady && elapsed < timeoutMs) { cancellationToken.ThrowIfCancellationRequested(); await Task.Delay(50, cancellationToken).ConfigureAwait(false); elapsed += 50; }
            return reader.IsReady;
        }
    }
}
