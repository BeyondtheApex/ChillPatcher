using System;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.Audio
{
    internal sealed class NullPcmReader : IPcmStreamReader
    {
        public PcmStreamInfo Info => new() { SampleRate = 44100, Channels = 2 };
        public ulong CurrentFrame => 0;
        public bool IsEndOfStream => true;
        public bool IsReady => true;
        public bool CanSeek => false;
        public bool HasPendingSeek => false;
        public long PendingSeekFrame => -1;
        public double CacheProgress => -1;
        public bool IsCacheComplete => false;
        public long ReadFrames(float[] buffer, int framesToRead) { Array.Clear(buffer, 0, framesToRead * 2); return 0; }
        public bool Seek(ulong frameIndex) => false;
        public void CancelPendingSeek() { }
        public void Dispose() { }
    }
}
