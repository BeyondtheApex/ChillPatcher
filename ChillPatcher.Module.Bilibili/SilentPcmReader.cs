using System;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.Module.Bilibili
{
    public class SilentPcmReader : IPcmStreamReader
    {
        public PcmStreamInfo Info => new PcmStreamInfo { SampleRate = 44100, Channels = 2, TotalFrames = 44100 * 60, Format = "pcm_s16le" };
        public bool IsReady => true;
        public ulong CurrentFrame => 0;
        public bool IsEndOfStream => false;
        public bool CanSeek => false;
        public double CacheProgress => 100.0;
        public bool IsCacheComplete => true;
        public bool HasPendingSeek => false;
        public long PendingSeekFrame => -1;
        public long ReadFrames(float[] buffer, int framesToRead) { Array.Clear(buffer, 0, buffer.Length); return framesToRead; }
        public bool Seek(ulong frameIndex) => true;
        public void CancelPendingSeek() { }
        public void Dispose() { }
    }
}