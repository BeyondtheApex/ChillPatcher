using System;

namespace ChillPatcher.SDK.Ipc
{
    public static class SharedMemoryProtocol
    {
        public const ulong MagicValue = 0x4D43504C4C494843UL; // "CHILLPCM" little-endian
        public const uint Version1 = 1;
        public const uint Version2 = 2;
        public const int HeaderSize = 256;

        public const int Magic = 0x00;
        public const int Version = 0x08;
        public const int SampleRate = 0x0C;
        public const int Channels = 0x10;
        public const int BytesPerFrame = 0x12;
        public const int BufferFrames = 0x14;
        public const int LegacyPlayState = 0x18;
        public const int SeekFrame = 0x1C;
        public const int Flags = 0x24;
        public const int CurrentUuid = 0x28;
        public const int CurrentUuidLength = 64;
        public const int WriteCursor = 0x68;
        public const int ReadCursor = 0x70;
        public const int GapSeconds = 0x78;

        public const int StreamId = 0x80;
        public const int StreamState = 0x88;
        public const int ErrorCode = 0x8C;
        public const int TotalFramesHint = 0x90;
        public const int DecodedTotalFrames = 0x98;
        public const int FinalWriteCursor = 0xA0;
        public const int AudibleCursor = 0xA8;
        public const int SeekGeneration = 0xB0;
        public const int LastUpdateTick = 0xB8;
        public const int FormatGeneration = 0xC0;
    }

    [Flags]
    public enum SharedMemoryStreamFlags : uint
    {
        None = 0,
        FormatReady = 1 << 0,
        DecoderEof = 1 << 1,
        StreamError = 1 << 2,
        SeekPending = 1 << 3,
        Discontinuity = 1 << 4,
        ClientDrained = 1 << 5,
        SyntheticEof = 1 << 6
    }

    public enum SharedMemoryStreamState
    {
        Stopped = 0,
        Preparing = 1,
        Playing = 2,
        Paused = 3,
        Draining = 4,
        Ended = 5,
        Error = 6
    }

    public enum SharedMemoryStreamError
    {
        None = 0,
        DecoderFailed = 1,
        SourceEndedUnexpectedly = 2,
        StalledNearEnd = 3,
        FormatInvalid = 4
    }

    public struct SharedMemoryStreamSnapshot
    {
        public uint Version;
        public int SampleRate;
        public int Channels;
        public int BytesPerFrame;
        public int BufferFrames;
        public int LegacyPlayState;
        public SharedMemoryStreamFlags Flags;
        public long WriteCursor;
        public long ReadCursor;
        public long StreamId;
        public SharedMemoryStreamState State;
        public SharedMemoryStreamError ErrorCode;
        public long TotalFramesHint;
        public long DecodedTotalFrames;
        public long FinalWriteCursor;
        public long AudibleCursor;
        public long SeekFrame;
        public long SeekGeneration;
        public long LastUpdateTick;
        public int FormatGeneration;
        public string CurrentUuid;

        public bool IsFormatReady => Version < SharedMemoryProtocol.Version2 || Flags.HasFlag(SharedMemoryStreamFlags.FormatReady);
        public bool HasDecoderEof => Flags.HasFlag(SharedMemoryStreamFlags.DecoderEof) || Flags.HasFlag(SharedMemoryStreamFlags.SyntheticEof);
        public bool HasError => Flags.HasFlag(SharedMemoryStreamFlags.StreamError) || State == SharedMemoryStreamState.Error;
        public long EffectiveTotalFrames => DecodedTotalFrames > 0 ? DecodedTotalFrames : TotalFramesHint;
    }
}
