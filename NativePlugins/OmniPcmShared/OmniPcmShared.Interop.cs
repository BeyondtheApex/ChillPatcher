using System;
using System.Runtime.InteropServices;

// ═══════════════════════════════════════════════════════════════════
//  OmniPcmShared.Interop  —  Standalone P/Invoke bindings
//
//  Drop these two files into any C# project that needs to talk to
//  OmniMixPlayer.  No NuGet dependencies.  Just make sure
//  OmniPcmShared.dll is in the output directory.
//
//  This file: enums, structs, OmniPcmShared (PCM shared memory).
//  OmniPcmClient.cs: gRPC-Web client wrapper.
// ═══════════════════════════════════════════════════════════════════

namespace OmniPcmShared.Interop
{
    // ── Enums ────────────────────────────────────────────────────

    public enum OmniPcmResult
    {
        Ok = 0,
        Error = -1,
        NotReady = -2,
        Eof = -3,
        BadArgument = -4,
        Unsupported = -5,
        WrongStream = -6
    }

    [Flags]
    public enum OmniPcmStreamFlags : uint
    {
        None = 0,
        FormatReady = 1u << 0,
        DecoderEof = 1u << 1,
        StreamError = 1u << 2,
        SeekPending = 1u << 3,
        Discontinuity = 1u << 4,
        ClientDrained = 1u << 5,
        SyntheticEof = 1u << 6
    }

    public enum OmniPcmStreamState
    {
        Stopped = 0,
        Preparing = 1,
        Playing = 2,
        Paused = 3,
        Draining = 4,
        Ended = 5,
        Error = 6
    }

    public enum OmniPcmStreamError
    {
        None = 0,
        DecoderFailed = 1,
        SourceEndedUnexpectedly = 2,
        StalledNearEnd = 3,
        FormatInvalid = 4
    }

    public enum OmniPcmPlaybackCommand
    {
        Play = 1,
        Pause = 2,
        Resume = 3,
        Toggle = 4,
        Next = 5,
        Prev = 6,
        Stop = 7
    }

    public enum OmniPcmCommand
    {
        Play = 1,
        Pause = 2,
        Resume = 3,
        Toggle = 4,
        Next = 5,
        Prev = 6,
        Stop = 7
    }

    public enum OmniPcmInstanceKind
    {
        GameMod = 1,
        Gui = 2,
        ExternalClient = 3,
        Observer = 4
    }

    [Flags]
    public enum OmniPcmCapabilityFlags : uint
    {
        None = 0,
        ServerControlledPlayback = 1u << 0,
        QueueManagement = 1u << 2,
        PlaylistManagement = 1u << 3,
        Shuffle = 1u << 4,
        Repeat = 1u << 5,
        Seek = 1u << 6,
        VolumeControl = 1u << 7,
        Equalizer = 1u << 8,
        MultiplePlaylists = 1u << 9,
        TagFiltering = 1u << 10,
        UnlimitedTags = 1u << 11,
        AlbumFiltering = 1u << 12,
        AudioPlayback = 1u << 13,
        CustomSystemMediaService = 1u << 14
    }

    // ── Structs  (layout must match omni_pcm_shared.h) ───────────

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmInfo
    {
        public int SampleRate;
        public int Channels;
        public int BytesPerFrame;
        public int BufferFrames;
        public long TotalFramesHint;
        public long DecodedTotalFrames;
        public long EffectiveTotalFrames;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmSnapshot
    {
        public uint Version;
        public int SampleRate;
        public int Channels;
        public int BytesPerFrame;
        public int BufferFrames;
        public int LegacyPlayState;
        public uint Flags;
        public long WriteCursor;
        public long ReadCursor;
        public long StreamId;
        public int State;
        public int ErrorCode;
        public long TotalFramesHint;
        public long DecodedTotalFrames;
        public long FinalWriteCursor;
        public long AudibleCursor;
        public long SeekFrame;
        public long SeekGeneration;
        public long LastUpdateTick;
        public int FormatGeneration;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string CurrentUuid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmClientConfig
    {
        [MarshalAs(UnmanagedType.LPStr)] public string Host;
        public int Port;
        public int TimeoutMs;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmConnectOptions
    {
        [MarshalAs(UnmanagedType.LPStr)] public string ClientId;
        [MarshalAs(UnmanagedType.LPStr)] public string ModId;
        [MarshalAs(UnmanagedType.LPStr)] public string GameName;
        [MarshalAs(UnmanagedType.LPStr)] public string DisplayName;
        public int Kind;
        public uint CapabilityFlags;
        public int NoInstance;
        public int MaxImportedPlaylists;
        public int MaxTags;
        public int MaxPlaylistEntries;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmConnectionInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string InstanceId;
        public int IsNew;
        public int NoInstance;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmPlaybackStatusInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string TrackUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string AlbumId;
        public float Duration;
        public float Position;
        public int IsPlaying;
        public int Shuffle;
        public int RepeatMode;
        public float Volume;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmInstanceSummaryInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string InstanceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string GameName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string CurrentTrackUuid;
        public int Kind;
        public int IsOnline;
        public int QueueCount;
        public int Mode;
        public long ConnectedAt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmInstanceProfileInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string InstanceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string GameName;
        public int Kind;
        public uint CapabilityFlags;
        public float Volume;
        public float TargetLatency;
        public int Mode;
        public int MaxImportedPlaylists;
        public int MaxTags;
        public int MaxPlaylistEntries;
        public long CreatedAt;
        public long UpdatedAt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmQueueTrackInfo
    {
        public int Index;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string AlbumId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModuleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string CoverUri;
        public float Duration;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmPlaylistSourceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string RefId;
        public int SongCount;
        public int Kind;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmPlaylistSourceSpec
    {
        [MarshalAs(UnmanagedType.LPStr)] public string Id;
        [MarshalAs(UnmanagedType.LPStr)] public string Name;
        [MarshalAs(UnmanagedType.LPStr)] public string RefId;
        public int Kind;
        public IntPtr Uuids;
        public int UuidCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmEqualizerPointInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Id;
        public float Frequency;
        public float GainDb;
        public float Q;
        public int Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmEqualizerStateInfo
    {
        public int Enabled;
        public float GlobalGainDb;
        public int SoftClipEnabled;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmBackendInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Status;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Name;
        public long Timestamp;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmTrackInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string AlbumId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModuleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string CoverUri;
        public int TrackNumber;
        public float Duration;
        public int IsExcluded;
        public long CreatedAt;
        public long LastPlayedAt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmAlbumInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModuleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string CoverUri;
        public int TrackCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmTagInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModuleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Color;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmPlaylistInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModuleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string CoverUri;
        public int TrackCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmTrackQuery
    {
        [MarshalAs(UnmanagedType.LPStr)] public string AlbumId;
        [MarshalAs(UnmanagedType.LPStr)] public string TagId;
        [MarshalAs(UnmanagedType.LPStr)] public string PlaylistId;
        [MarshalAs(UnmanagedType.LPStr)] public string ModuleId;
        public int IsExcluded;
        public int Limit;
        public int Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmLibraryQuery
    {
        [MarshalAs(UnmanagedType.LPStr)] public string ModuleId;
        public int Limit;
        public int Offset;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmEventInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Type;
        public long Timestamp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string InstanceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string TrackUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string AlbumId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string ModuleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string SourceRefId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string ChangeType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DisplayName;
        public float Duration;
        public float Position;
        public int State;
        public int QueueLength;
        public int BackendRunning;
        public int BoolValue;
        public int SongCount;
        public int InstanceCount;
        public float Volume;
        public float Latency;
    }

    // ── Delegate ─────────────────────────────────────────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OmniPcmEventCallback(ref OmniPcmEventInfo eventInfo, IntPtr userData);

    // ══════════════════════════════════════════════════════════════
    //  OmniPcmShared  – PCM shared-memory reader / controller
    // ══════════════════════════════════════════════════════════════

    public sealed class OmniPcmShared : IDisposable
    {
        private const string DllName = "OmniPcmShared";
        private IntPtr _handle;

        public OmniPcmShared(string mapName = null)
        {
            _handle = OmniPcm_Open(mapName);
            if (_handle == IntPtr.Zero || !IsOpen)
                throw new InvalidOperationException(GetLastError());
        }

        public static OmniPcmShared OpenUtf8(string mapNameUtf8 = null)
        {
            var handle = OmniPcm_OpenUtf8(mapNameUtf8);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("OmniPcm_OpenUtf8 returned null");
            var result = new OmniPcmShared { _handle = handle };
            if (!result.IsOpen)
                throw new InvalidOperationException(result.GetLastError());
            return result;
        }

        private OmniPcmShared() { }

        public bool IsOpen => _handle != IntPtr.Zero && OmniPcm_IsOpen(_handle) != 0;
        public uint Version => _handle != IntPtr.Zero ? OmniPcm_GetVersion(_handle) : 0;
        public long BoundStreamId => _handle != IntPtr.Zero ? OmniPcm_GetBoundStreamId(_handle) : 0;

        public OmniPcmSnapshot Snapshot { get { ThrowIfDisposed(); OmniPcm_GetSnapshot(_handle, out var s); return s; } }
        public OmniPcmInfo Info { get { ThrowIfDisposed(); OmniPcm_GetInfo(_handle, out var i); return i; } }
        public string CurrentUuid { get { ThrowIfDisposed(); return Marshal.PtrToStringAnsi(OmniPcm_GetCurrentUuid(_handle)) ?? ""; } }

        public void BindCurrentStream() { ThrowIfDisposed(); Check(OmniPcm_BindCurrentStream(_handle)); }
        public void BindStream(string uuid) { ThrowIfDisposed(); Check(OmniPcm_BindStream(_handle, uuid)); }
        public bool IsFormatReady() { ThrowIfDisposed(); return OmniPcm_IsFormatReady(_handle) != 0; }
        public bool WaitForFormatReady(string uuid, int timeoutMs) { ThrowIfDisposed(); return OmniPcm_WaitForFormatReady(_handle, uuid, timeoutMs) == 0; }
        public bool HasDecoderEof() { ThrowIfDisposed(); return OmniPcm_HasDecoderEof(_handle) != 0; }
        public bool IsPlaybackComplete(long toleranceFrames) { ThrowIfDisposed(); return OmniPcm_IsPlaybackComplete(_handle, toleranceFrames) != 0; }
        public bool HasError() { ThrowIfDisposed(); return OmniPcm_HasError(_handle) != 0; }

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            ThrowIfDisposed();
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return OmniPcm_ReadFrames(_handle, buffer, framesToRead);
        }

        public void RequestSeek(long frame) { ThrowIfDisposed(); Check(OmniPcm_RequestSeek(_handle, frame)); }
        public void CancelPendingSeek() { ThrowIfDisposed(); Check(OmniPcm_CancelPendingSeek(_handle)); }
        public void SetAudibleCursor(long frame, bool allowBackward = false) { ThrowIfDisposed(); Check(OmniPcm_SetAudibleCursor(_handle, frame, allowBackward ? 1 : 0)); }
        public void ReportAudioSourcePosition(int timeSamples) { ThrowIfDisposed(); Check(OmniPcm_ReportAudioSourcePosition(_handle, timeSamples)); }

        public string GetLastError()
        {
            if (_handle == IntPtr.Zero) return "OmniPcmShared handle is null";
            return Marshal.PtrToStringAnsi(OmniPcm_GetLastError(_handle)) ?? "";
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero) return;
            OmniPcm_Close(_handle);
            _handle = IntPtr.Zero;
        }

        private void ThrowIfDisposed() { if (_handle == IntPtr.Zero) throw new ObjectDisposedException(nameof(OmniPcmShared)); }
        private void Check(int result) { if (result < 0) throw new InvalidOperationException(GetLastError()); }

        // ── P/Invoke ──────────────────────────────────────────────

        [DllImport(DllName, EntryPoint = "OmniPcm_Open", ExactSpelling = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcm_Open(string mapName);

        [DllImport(DllName, EntryPoint = "OmniPcm_OpenUtf8", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcm_OpenUtf8([MarshalAs(UnmanagedType.LPStr)] string mapNameUtf8);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void OmniPcm_Close(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_IsOpen(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint OmniPcm_GetVersion(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcm_GetLastError(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_GetSnapshot(IntPtr handle, out OmniPcmSnapshot snapshot);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_GetInfo(IntPtr handle, out OmniPcmInfo info);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcm_GetCurrentUuid(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_BindCurrentStream(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_BindStream(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string uuid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern long OmniPcm_GetBoundStreamId(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_IsFormatReady(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_WaitForFormatReady(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string uuid, int timeoutMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_HasDecoderEof(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_IsPlaybackComplete(IntPtr handle, long toleranceFrames);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_HasError(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern long OmniPcm_ReadFrames(IntPtr handle, [Out] float[] buffer, int framesToRead);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_RequestSeek(IntPtr handle, long frame);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_CancelPendingSeek(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_SetAudibleCursor(IntPtr handle, long frame, int allowBackward);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcm_ReportAudioSourcePosition(IntPtr handle, int timeSamples);
    }
}
