using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Module.Spotify
{
    public sealed class NativeLibrespotPcmReader : IPcmStreamReader
    {
        private const string DllName = "SpotifyLibrespotBridge";

        private readonly string _accessToken;
        private readonly string _deviceName;
        private readonly string _cacheDir;
        private readonly float _durationSeconds;
        private readonly ILogger _logger;

        private IntPtr _handle;
        private ulong _currentFrame;
        private bool _disposed;

        public NativeLibrespotPcmReader(
            string accessToken,
            string deviceName,
            string cacheDir,
            float durationSeconds,
            ILogger logger)
        {
            _accessToken = accessToken;
            _deviceName = deviceName;
            _cacheDir = cacheDir;
            _durationSeconds = durationSeconds > 0 ? durationSeconds : 240f;
            _logger = logger;
        }

        public PcmStreamInfo Info => new()
        {
            SampleRate = SampleRate,
            Channels = Channels,
            TotalFrames = (ulong)(SampleRate * _durationSeconds),
            Format = "spotify/librespot-native",
            CanSeek = false
        };

        public int SampleRate => SafeCall(omni_spotify_sample_rate, 44100);
        public int Channels => SafeCall(omni_spotify_channels, 2);
        public ulong CurrentFrame => _currentFrame;
        public bool IsEndOfStream => _handle == IntPtr.Zero || omni_spotify_is_eof(_handle) != 0;
        public bool IsReady => _handle != IntPtr.Zero && omni_spotify_is_ready(_handle) != 0;
        public bool CanSeek => false;
        public bool HasPendingSeek => false;
        public long PendingSeekFrame => -1;
        public double CacheProgress => 100.0;
        public bool IsCacheComplete => true;

        public bool Start()
        {
            try
            {
                Directory.CreateDirectory(_cacheDir);
                _handle = omni_spotify_create(_accessToken, _deviceName, _cacheDir);
                if (_handle == IntPtr.Zero)
                {
                    _logger?.LogWarning("[Spotify] SpotifyLibrespotBridge returned null handle");
                    return false;
                }
                return true;
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is BadImageFormatException)
            {
                _logger?.LogWarning(ex, "[Spotify] SpotifyLibrespotBridge is unavailable");
                return false;
            }
        }

        public bool WaitForReady(int timeoutMs, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsReady)
                    return true;
                if (_handle != IntPtr.Zero && omni_spotify_last_error(_handle) != 0)
                    return false;
                // 使用 WaitHandle 替代 Thread.Sleep，cancel 时能立即响应
                cancellationToken.WaitHandle.WaitOne(50);
            }
            return IsReady;
        }

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            if (_disposed || _handle == IntPtr.Zero || framesToRead <= 0)
                return 0;

            var frames = omni_spotify_read(_handle, buffer, framesToRead);
            if (frames > 0)
                _currentFrame += (ulong)frames;
            return frames;
        }

        public bool Seek(ulong frameIndex) => false;
        public void CancelPendingSeek() { }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var handle = _handle;
            _handle = IntPtr.Zero;
            if (handle != IntPtr.Zero)
                omni_spotify_destroy(handle);
        }

        private static int SafeCall(Func<int> call, int fallback)
        {
            try { return call(); }
            catch { return fallback; }
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr omni_spotify_create(string accessToken, string deviceName, string cacheDir);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void omni_spotify_destroy(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int omni_spotify_read(IntPtr handle, [Out] float[] output, int framesToRead);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int omni_spotify_is_ready(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int omni_spotify_is_eof(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint omni_spotify_last_error(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int omni_spotify_sample_rate();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int omni_spotify_channels();
    }
}
