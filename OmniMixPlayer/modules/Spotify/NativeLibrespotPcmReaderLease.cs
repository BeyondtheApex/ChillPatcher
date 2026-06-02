using System;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Module.Spotify
{
    /// <summary>
    /// Playback-pipeline view over the module-owned librespot Connect device.
    /// Disposing this lease must not shut down the Spotify Connect device.
    /// </summary>
    public sealed class NativeLibrespotPcmReaderLease : IPcmStreamReader
    {
        private readonly NativeLibrespotPcmReader _inner;
        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly ulong _totalFrames;
        private ulong _currentFrame;

        public NativeLibrespotPcmReaderLease(NativeLibrespotPcmReader inner, float durationSeconds)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _sampleRate = inner.SampleRate > 0 ? inner.SampleRate : 44100;
            _channels = inner.Channels > 0 ? inner.Channels : 2;
            _totalFrames = durationSeconds > 0
                ? (ulong)(_sampleRate * durationSeconds)
                : 0UL;
        }

        public PcmStreamInfo Info => new()
        {
            SampleRate = _sampleRate,
            Channels = _channels,
            TotalFrames = _totalFrames,
            Format = "spotify/connect-pcm-f32",
            CanSeek = false
        };

        public ulong CurrentFrame => _currentFrame;
        public bool IsEndOfStream => _inner.IsEndOfStream || (_totalFrames > 0 && _currentFrame >= _totalFrames);
        public bool IsReady => _inner.IsReady;
        public bool CanSeek => false;
        public bool HasPendingSeek => false;
        public long PendingSeekFrame => -1;
        public double CacheProgress => -1;
        public bool IsCacheComplete => _totalFrames > 0;

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            if (IsEndOfStream)
                return 0;

            if (_totalFrames > 0)
            {
                var remaining = _totalFrames - _currentFrame;
                framesToRead = (int)Math.Min((ulong)Math.Max(0, framesToRead), remaining);
                if (framesToRead <= 0)
                    return 0;
            }

            var frames = _inner.ReadFrames(buffer, framesToRead);
            if (frames > 0)
                _currentFrame += (ulong)frames;
            return frames;
        }

        public bool Seek(ulong frameIndex) => false;
        public void CancelPendingSeek() { }
        public void Dispose() { }
    }
}
