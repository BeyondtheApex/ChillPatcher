using System;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// 静音 PCM 读取器 - 用于登录歌曲占位
    /// </summary>
    public class SilentPcmReader : IPcmStreamReader
    {
        private readonly ulong _totalFrames;
        private ulong _currentFrame;

        public SilentPcmReader(float durationSeconds = 30f)
        {
            _totalFrames = (ulong)(44100 * durationSeconds);
        }

        public PcmStreamInfo Info => new PcmStreamInfo
        {
            SampleRate = 44100,
            Channels = 2,
            TotalFrames = _totalFrames
        };

        public bool IsReady => true;
        public ulong CurrentFrame => _currentFrame;
        public bool IsEndOfStream => _currentFrame >= _totalFrames;
        public bool CanSeek => true;
        public double CacheProgress => 100.0;
        public bool IsCacheComplete => true;
        public bool HasPendingSeek => false;
        public long PendingSeekFrame => -1;

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            ulong remaining = _totalFrames - _currentFrame;
            int actual = (int)Math.Min((ulong)framesToRead, remaining);
            Array.Clear(buffer, 0, actual * 2);
            _currentFrame += (ulong)actual;
            return actual;
        }

        public bool Seek(ulong frameIndex)
        {
            _currentFrame = Math.Min(frameIndex, _totalFrames);
            return true;
        }

        public void CancelPendingSeek() { }
        public void Dispose() { }
    }
}
