using System;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Native;

namespace ChillPatcher.SDK.Ipc
{
    public sealed class SharedMemoryPcmStreamReader : IPcmStreamReader
    {
        private readonly OmniPcmShared _omniPcm;
        private readonly string _uuid;
        private readonly long _streamId;
        private bool _disposed;

        public SharedMemoryPcmStreamReader(OmniPcmShared omniPcm, string uuid)
        {
            _omniPcm = omniPcm ?? throw new ArgumentNullException(nameof(omniPcm));
            _uuid = uuid ?? "";
            _omniPcm.BindStream(_uuid);
            var snapshot = _omniPcm.Snapshot;
            _streamId = snapshot.StreamId;
        }

        public string Uuid => _uuid;
        public long StreamId => _streamId;

        public PcmStreamInfo Info
        {
            get
            {
                var info = _omniPcm.Info;
                var totalFrames = info.EffectiveTotalFrames > 0 ? (ulong)info.EffectiveTotalFrames : 0;
                return new PcmStreamInfo
                {
                    SampleRate = info.SampleRate > 0 ? info.SampleRate : 44100,
                    Channels = info.Channels > 0 ? info.Channels : 2,
                    TotalFrames = totalFrames,
                    Format = "pcm",
                    CanSeek = true
                };
            }
        }

        public ulong CurrentFrame
        {
            get
            {
                var snapshot = _omniPcm.Snapshot;
                var frame = snapshot.AudibleCursor > 0 ? snapshot.AudibleCursor : snapshot.ReadCursor;
                return frame > 0 ? (ulong)frame : 0;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                var snapshot = _omniPcm.Snapshot;
                if (!IsSameStream(snapshot)) return false;
                if (snapshot.Version >= 2)
                {
                    var toleranceFrames = Math.Max(1, snapshot.SampleRate * 250 / 1000);
                    return _omniPcm.IsPlaybackComplete(toleranceFrames);
                }

                return snapshot.WriteCursor > 0 &&
                       snapshot.LegacyPlayState == 0 &&
                       snapshot.ReadCursor >= snapshot.WriteCursor;
            }
        }

        public bool IsReady
        {
            get
            {
                var snapshot = _omniPcm.Snapshot;
                var isFormatReady = snapshot.Version < 2 || (snapshot.Flags & (uint)OmniPcmStreamFlags.FormatReady) != 0;
                return IsSameStream(snapshot) && isFormatReady && snapshot.SampleRate > 0 && snapshot.Channels > 0;
            }
        }

        public bool CanSeek => true;
        public bool HasPendingSeek
        {
            get
            {
                var snapshot = _omniPcm.Snapshot;
                return (snapshot.Flags & (uint)OmniPcmStreamFlags.SeekPending) != 0;
            }
        }
        public long PendingSeekFrame => _omniPcm.Snapshot.SeekFrame;
        public double CacheProgress => -1;
        public bool IsCacheComplete => false;

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            if (_disposed || buffer == null || framesToRead <= 0)
                return 0;

            var read = _omniPcm.ReadFrames(buffer, framesToRead);
            if (read < 0)
            {
                Array.Clear(buffer, 0, buffer.Length);
                return 0;
            }
            return read;
        }

        public bool Seek(ulong frameIndex)
        {
            if (_disposed) return false;
            _omniPcm.RequestSeek((long)Math.Min(frameIndex, (ulong)long.MaxValue));
            return true;
        }

        public void CancelPendingSeek()
        {
            if (_disposed) return;
            try
            {
                _omniPcm.CancelPendingSeek();
            }
            catch {}
        }

        public void ReportAudibleFrame(long frame)
        {
            if (_disposed) return;
            _omniPcm.SetAudibleCursor(frame);
        }

        public void ReportAudioSourcePosition(int timeSamples)
        {
            if (_disposed) return;
            _omniPcm.ReportAudioSourcePosition(timeSamples);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private bool IsSameStream(OmniPcmSnapshot snapshot)
        {
            if (!string.Equals(snapshot.CurrentUuid, _uuid, StringComparison.Ordinal))
                return false;
            return snapshot.Version < 2 || _streamId == 0 || snapshot.StreamId == _streamId;
        }
    }
}
