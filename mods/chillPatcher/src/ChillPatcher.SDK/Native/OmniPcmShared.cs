using System;
using System.Runtime.InteropServices;

namespace ChillPatcher.SDK.Native
{
    [Flags]
    public enum OmniPcmStreamFlags : uint
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

        public bool IsOpen => _handle != IntPtr.Zero && OmniPcm_IsOpen(_handle) != 0;
        public uint Version => _handle != IntPtr.Zero ? OmniPcm_GetVersion(_handle) : 0;
        public long BoundStreamId => _handle != IntPtr.Zero ? OmniPcm_GetBoundStreamId(_handle) : 0;

        public OmniPcmSnapshot Snapshot
        {
            get
            {
                ThrowIfDisposed();
                OmniPcm_GetSnapshot(_handle, out var snapshot);
                return snapshot;
            }
        }

        public OmniPcmInfo Info
        {
            get
            {
                ThrowIfDisposed();
                OmniPcm_GetInfo(_handle, out var info);
                return info;
            }
        }

        public string CurrentUuid
        {
            get
            {
                ThrowIfDisposed();
                return Marshal.PtrToStringAnsi(OmniPcm_GetCurrentUuid(_handle)) ?? "";
            }
        }

        public void BindCurrentStream()
        {
            ThrowIfDisposed();
            Check(OmniPcm_BindCurrentStream(_handle));
        }

        public void BindStream(string uuid)
        {
            ThrowIfDisposed();
            Check(OmniPcm_BindStream(_handle, uuid));
        }

        public bool IsFormatReady()
        {
            ThrowIfDisposed();
            return OmniPcm_IsFormatReady(_handle) != 0;
        }

        public bool WaitForFormatReady(string uuid, int timeoutMs)
        {
            ThrowIfDisposed();
            return OmniPcm_WaitForFormatReady(_handle, uuid, timeoutMs) == 0;
        }

        public bool HasDecoderEof()
        {
            ThrowIfDisposed();
            return OmniPcm_HasDecoderEof(_handle) != 0;
        }

        public bool IsPlaybackComplete(long toleranceFrames)
        {
            ThrowIfDisposed();
            return OmniPcm_IsPlaybackComplete(_handle, toleranceFrames) != 0;
        }

        public bool HasError()
        {
            ThrowIfDisposed();
            return OmniPcm_HasError(_handle) != 0;
        }

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            ThrowIfDisposed();
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return OmniPcm_ReadFrames(_handle, buffer, framesToRead);
        }

        public void RequestSeek(long frame)
        {
            ThrowIfDisposed();
            Check(OmniPcm_RequestSeek(_handle, frame));
        }

        public void CancelPendingSeek()
        {
            ThrowIfDisposed();
            Check(OmniPcm_CancelPendingSeek(_handle));
        }

        public void SetAudibleCursor(long frame, bool allowBackward = false)
        {
            ThrowIfDisposed();
            Check(OmniPcm_SetAudibleCursor(_handle, frame, allowBackward ? 1 : 0));
        }

        public void ReportAudioSourcePosition(int timeSamples)
        {
            ThrowIfDisposed();
            Check(OmniPcm_ReportAudioSourcePosition(_handle, timeSamples));
        }

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

        private void ThrowIfDisposed()
        {
            if (_handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(OmniPcmShared));
        }

        private void Check(int result)
        {
            if (result < 0)
                throw new InvalidOperationException(GetLastError());
        }

        [DllImport(DllName, EntryPoint = "OmniPcm_Open", ExactSpelling = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcm_Open(string mapName);

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
}
