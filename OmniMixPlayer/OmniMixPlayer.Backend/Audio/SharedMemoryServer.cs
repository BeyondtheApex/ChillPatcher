using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Ipc;

namespace OmniMixPlayer.Backend.Audio
{
    public unsafe class SharedMemoryServer : IDisposable
    {
        public const string DefaultMapName = @"Global\OmniMixPlayer_PCM";
        private const int DefaultSampleRate = 44100;
        private const int DefaultChannels = 2;
        private const int BufferSeconds = 5;
        private const int MaxCapacityBytes = 16 * 1024 * 1024; // 16 MB


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, ref SECURITY_ATTRIBUTES lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool InitializeSecurityDescriptor(out SECURITY_DESCRIPTOR pSecurityDescriptor, uint dwRevision);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetSecurityDescriptorDacl(ref SECURITY_DESCRIPTOR pSecurityDescriptor, bool bDaclPresent, IntPtr pDacl, bool bDaclDefaulted);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint SetEntriesInAcl(int cCountOfExplicitEntries, ref EXPLICIT_ACCESS pListOfExplicitEntries, IntPtr OldAcl, out IntPtr NewAcl);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr Sid);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        private const uint PAGE_READWRITE = 0x04;
        private const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        private const uint SECURITY_DESCRIPTOR_REVISION = 1;
        private const uint GRANT_ACCESS = 1;
        private const uint TRUSTEE_IS_SID = 0;
        private const uint TRUSTEE_IS_WELL_KNOWN_GROUP = 3;
        private const uint NO_INHERITANCE = 0;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint SYNCHRONIZE = 0x00100000;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_DESCRIPTOR
        {
            public byte Revision;
            public byte Sbz1;
            public short Control;
            public IntPtr Owner;
            public IntPtr Group;
            public IntPtr Sacl;
            public IntPtr Dacl;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EXPLICIT_ACCESS
        {
            public uint grfAccessPermissions;
            public uint grfAccessMode;
            public uint grfInheritance;
            public TRUSTEE Trustee;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TRUSTEE
        {
            public IntPtr pMultipleTrustee;
            public uint MultipleTrusteeOperation;
            public uint TrusteeForm;
            public uint TrusteeType;
            public IntPtr ptstrName;
        }

        private readonly ILogger _logger;
        private readonly string _mapName;
        private byte* _ptr;
        private IntPtr _mapHandle;
        private IntPtr _viewHandle;
        private long _capacity;
        private bool _disposed;

        public int SampleRate { get; set; } = DefaultSampleRate;
        public int Channels { get; set; } = DefaultChannels;
        public int BufferFrames => SampleRate * BufferSeconds;
        public int BufferSize => BufferFrames * Channels * sizeof(float);
        public int TotalSize => SharedMemoryProtocol.HeaderSize + BufferSize;

        public string MapName => _mapName;

        public SharedMemoryServer(ILogger logger, string mapName = DefaultMapName)
        {
            _logger = logger;
            _mapName = string.IsNullOrWhiteSpace(mapName) ? DefaultMapName : mapName;
        }

        public bool Initialize()
        {
            try
            {
                _capacity = MaxCapacityBytes;
                uint sizeHigh = (uint)(_capacity >> 32);
                uint sizeLow = (uint)(_capacity & 0xFFFFFFFF);

                IntPtr pAcl = IntPtr.Zero;
                SECURITY_ATTRIBUTES sa = default;

                try
                {
                    // S-1-15-2-1 = ALL APPLICATION PACKAGES (UWP)
                    // S-1-1-0 = Everyone (required when running as SYSTEM service)
                    if (!ConvertStringSidToSid("S-1-15-2-1", out IntPtr pAppSid))
                    {
                        _logger.LogWarning("ConvertStringSidToSid (ALL APPLICATION PACKAGES) failed, using default security");
                    }
                    else if (!ConvertStringSidToSid("S-1-1-0", out IntPtr pEveryoneSid))
                    {
                        _logger.LogWarning("ConvertStringSidToSid (Everyone) failed, using default security");
                        LocalFree(pAppSid);
                    }
                    else
                    {
                        var entries = new EXPLICIT_ACCESS[2]
                        {
                            new EXPLICIT_ACCESS
                            {
                                grfAccessPermissions = GENERIC_READ | GENERIC_WRITE | SYNCHRONIZE,
                                grfAccessMode = GRANT_ACCESS,
                                grfInheritance = NO_INHERITANCE,
                                Trustee = new TRUSTEE { TrusteeForm = TRUSTEE_IS_SID, TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP, ptstrName = pAppSid }
                            },
                            new EXPLICIT_ACCESS
                            {
                                grfAccessPermissions = GENERIC_READ | GENERIC_WRITE | SYNCHRONIZE,
                                grfAccessMode = GRANT_ACCESS,
                                grfInheritance = NO_INHERITANCE,
                                Trustee = new TRUSTEE { TrusteeForm = TRUSTEE_IS_SID, TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP, ptstrName = pEveryoneSid }
                            }
                        };

                        uint result = SetEntriesInAcl(2, ref entries[0], IntPtr.Zero, out pAcl);
                        if (result != 0)
                        {
                            _logger.LogWarning("SetEntriesInAcl failed: {Error}", result);
                            pAcl = IntPtr.Zero;
                        }

                        LocalFree(pAppSid);
                        LocalFree(pEveryoneSid);
                    }

                    if (pAcl != IntPtr.Zero)
                    {
                        InitializeSecurityDescriptor(out SECURITY_DESCRIPTOR sd, SECURITY_DESCRIPTOR_REVISION);
                        SetSecurityDescriptorDacl(ref sd, true, pAcl, false);

                        sa.nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
                        sa.lpSecurityDescriptor = Marshal.AllocHGlobal(Marshal.SizeOf<SECURITY_DESCRIPTOR>());
                        Marshal.StructureToPtr(sd, sa.lpSecurityDescriptor, false);
                        sa.bInheritHandle = 0;

                        _mapHandle = CreateFileMapping(INVALID_HANDLE_VALUE, ref sa, PAGE_READWRITE, sizeHigh, sizeLow, _mapName);

                        Marshal.FreeHGlobal(sa.lpSecurityDescriptor);
                        sa.lpSecurityDescriptor = IntPtr.Zero;
                    }
                    else
                    {
                        _mapHandle = CreateFileMapping(INVALID_HANDLE_VALUE, ref sa, PAGE_READWRITE, sizeHigh, sizeLow, _mapName);
                    }
                }
                finally
                {
                    if (pAcl != IntPtr.Zero) LocalFree(pAcl);
                }

                if (_mapHandle == IntPtr.Zero)
                {
                    _logger.LogError("CreateFileMapping failed: {Error}", Marshal.GetLastWin32Error());
                    return false;
                }

                _viewHandle = MapViewOfFile(_mapHandle, FILE_MAP_ALL_ACCESS, 0, 0, (UIntPtr)_capacity);
                if (_viewHandle == IntPtr.Zero)
                {
                    _logger.LogError("MapViewOfFile failed: {Error}", Marshal.GetLastWin32Error());
                    return false;
                }

                _ptr = (byte*)_viewHandle;
                InitializeHeader();
                _logger.LogInformation("Shared memory created: {Name}, size={Size} bytes, buffer={BufferFrames} frames", _mapName, TotalSize, BufferFrames);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shared memory");
                return false;
            }
        }

        private void InitializeHeader()
        {
            var header = new Span<byte>(_ptr, SharedMemoryProtocol.HeaderSize);
            header.Clear();

            *(ulong*)_ptr = SharedMemoryProtocol.MagicValue;

            WriteU32(SharedMemoryProtocol.Version, SharedMemoryProtocol.Version2);
            WriteU32(SharedMemoryProtocol.SampleRate, (uint)SampleRate);
            WriteU16(SharedMemoryProtocol.Channels, (ushort)Channels);
            WriteU16(SharedMemoryProtocol.BytesPerFrame, (ushort)(Channels * sizeof(float)));
            WriteU32(SharedMemoryProtocol.BufferFrames, (uint)BufferFrames);
            WriteI32(SharedMemoryProtocol.LegacyPlayState, 0); // PlayState: stop
            WriteI64(SharedMemoryProtocol.SeekFrame, 0);
            WriteU32(SharedMemoryProtocol.Flags, 0);
            WriteF32(SharedMemoryProtocol.GapSeconds, 0f);
            WriteI64(SharedMemoryProtocol.StreamId, 0);
            WriteI32(SharedMemoryProtocol.StreamState, (int)SharedMemoryStreamState.Stopped);
            WriteI32(SharedMemoryProtocol.ErrorCode, (int)SharedMemoryStreamError.None);
            WriteI64(SharedMemoryProtocol.LastUpdateTick, DateTime.UtcNow.Ticks);
        }

        public void SetPlayState(int state)
        {
            WriteI32(SharedMemoryProtocol.LegacyPlayState, state);
            if (_ptr == null) return;
            if (ReadI32(SharedMemoryProtocol.StreamState) == (int)SharedMemoryStreamState.Error)
                return;
            var streamState = state switch
            {
                1 => SharedMemoryStreamState.Playing,
                2 => SharedMemoryStreamState.Paused,
                _ => SharedMemoryStreamState.Stopped
            };
            SetStreamState(streamState);
        }

        public void SetCurrentUuid(string uuid)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(uuid ?? "");
            for (int i = 0; i < SharedMemoryProtocol.CurrentUuidLength && i < bytes.Length; i++)
                _ptr[SharedMemoryProtocol.CurrentUuid + i] = bytes[i];
            for (int i = bytes.Length; i < SharedMemoryProtocol.CurrentUuidLength; i++)
                _ptr[SharedMemoryProtocol.CurrentUuid + i] = 0;
        }

        public void UpdateFormat(int sampleRate, int channels)
        {
            if (_ptr == null) return;
            this.SampleRate = sampleRate;
            this.Channels = channels;
            WriteU32(SharedMemoryProtocol.SampleRate, (uint)sampleRate);
            WriteU16(SharedMemoryProtocol.Channels, (ushort)channels);
            WriteU16(SharedMemoryProtocol.BytesPerFrame, (ushort)(channels * sizeof(float)));
            WriteU32(SharedMemoryProtocol.BufferFrames, (uint)BufferFrames);
            Interlocked.Increment(ref *(int*)(_ptr + SharedMemoryProtocol.FormatGeneration));
            SetFlag(SharedMemoryStreamFlags.FormatReady, true);
            Touch();
        }

        public void WriteFrames(float[] pcm, int frameCount)
        {
            if (_ptr == null) return;
            var writeCursor = ReadI64(SharedMemoryProtocol.WriteCursor);
            var startFrame = (int)(writeCursor % BufferFrames);
            var sampleCount = frameCount * Channels;
            var totalSamples = BufferFrames * Channels;
            var startSample = startFrame * Channels;

            if (startSample + sampleCount <= totalSamples)
            {
                Marshal.Copy(pcm, 0, (IntPtr)(_ptr + SharedMemoryProtocol.HeaderSize + startSample * sizeof(float)), sampleCount);
            }
            else
            {
                var firstPartSamples = totalSamples - startSample;
                Marshal.Copy(pcm, 0, (IntPtr)(_ptr + SharedMemoryProtocol.HeaderSize + startSample * sizeof(float)), firstPartSamples);
                Marshal.Copy(pcm, firstPartSamples, (IntPtr)(_ptr + SharedMemoryProtocol.HeaderSize), sampleCount - firstPartSamples);
            }

            Interlocked.Exchange(ref *(long*)(_ptr + SharedMemoryProtocol.WriteCursor), writeCursor + frameCount);
            Touch();
        }

        public int GetReadableFrames()
        {
            if (_ptr == null) return 0;
            var write = Volatile.Read(ref *(long*)(_ptr + SharedMemoryProtocol.WriteCursor));
            var read = Volatile.Read(ref *(long*)(_ptr + SharedMemoryProtocol.ReadCursor));
            return (int)(write - read);
        }

        public long GetWriteCursor() => ReadI64(SharedMemoryProtocol.WriteCursor);
        public long GetReadCursor() => ReadI64(SharedMemoryProtocol.ReadCursor);
        public long GetAudibleCursor() => ReadI64(SharedMemoryProtocol.AudibleCursor);
        public long GetSeekGeneration() => ReadI64(SharedMemoryProtocol.SeekGeneration);
        public long GetSeekFrame() => ReadI64(SharedMemoryProtocol.SeekFrame);

        public long RequestSeek(long frame)
        {
            if (_ptr == null) return 0;
            if (frame < 0) frame = 0;
            WriteI64(SharedMemoryProtocol.SeekFrame, frame);
            SetFlag(SharedMemoryStreamFlags.SeekPending, true);
            SetFlag(SharedMemoryStreamFlags.Discontinuity, true);
            var generation = Interlocked.Increment(ref *(long*)(_ptr + SharedMemoryProtocol.SeekGeneration));
            Touch();
            return generation;
        }

        public void ResetCursors()
        {
            if (_ptr == null) return;
            WriteI64(SharedMemoryProtocol.WriteCursor, 0);
            WriteI64(SharedMemoryProtocol.ReadCursor, 0);
            WriteI64(SharedMemoryProtocol.AudibleCursor, 0);
            WriteI64(SharedMemoryProtocol.FinalWriteCursor, 0);
            WriteI64(SharedMemoryProtocol.DecodedTotalFrames, 0);
            
            // Clear the PCM ring buffer memory to prevent residual sound
            byte* bufferStart = _ptr + SharedMemoryProtocol.HeaderSize;
            int bufferBytes = BufferFrames * Channels * sizeof(float);
            var span = new Span<byte>(bufferStart, bufferBytes);
            span.Clear();
            Touch();
        }

        public void DiscardUnreadFrames()
        {
            if (_ptr == null) return;
            var write = ReadI64(SharedMemoryProtocol.WriteCursor);
            WriteI64(SharedMemoryProtocol.ReadCursor, write);
            Touch();
        }

        public long BeginStream(string uuid, long totalFramesHint)
        {
            if (_ptr == null) return 0;
            var streamId = Interlocked.Increment(ref *(long*)(_ptr + SharedMemoryProtocol.StreamId));
            SetCurrentUuid(uuid);
            ResetCursors();
            WriteI64(SharedMemoryProtocol.TotalFramesHint, Math.Max(0, totalFramesHint));
            WriteI64(SharedMemoryProtocol.DecodedTotalFrames, 0);
            WriteI64(SharedMemoryProtocol.FinalWriteCursor, 0);
            WriteI32(SharedMemoryProtocol.ErrorCode, (int)SharedMemoryStreamError.None);
            WriteU32(SharedMemoryProtocol.Flags, 0);
            SetStreamState(SharedMemoryStreamState.Preparing);
            Touch();
            return streamId;
        }

        public void MarkFormatReady(int sampleRate, int channels, long totalFramesHint)
        {
            UpdateFormat(sampleRate, channels);
            if (totalFramesHint > 0)
                WriteI64(SharedMemoryProtocol.TotalFramesHint, totalFramesHint);
            SetFlag(SharedMemoryStreamFlags.FormatReady, true);
            SetStreamState(SharedMemoryStreamState.Playing);
            Touch();
        }

        public void MarkDecoderEof(long decodedTotalFrames = 0)
        {
            var finalCursor = GetWriteCursor();
            WriteI64(SharedMemoryProtocol.FinalWriteCursor, finalCursor);
            WriteI64(SharedMemoryProtocol.DecodedTotalFrames, decodedTotalFrames > 0 ? decodedTotalFrames : finalCursor);
            SetFlag(SharedMemoryStreamFlags.DecoderEof, true);
            SetFlag(SharedMemoryStreamFlags.SeekPending, false);
            SetStreamState(SharedMemoryStreamState.Draining);
            Touch();
        }

        public void MarkEnded()
        {
            SetFlag(SharedMemoryStreamFlags.ClientDrained, true);
            SetStreamState(SharedMemoryStreamState.Ended);
            WriteI32(SharedMemoryProtocol.LegacyPlayState, 0);
            Touch();
        }

        public void MarkError(SharedMemoryStreamError error, bool syntheticEof = false)
        {
            WriteI32(SharedMemoryProtocol.ErrorCode, (int)error);
            SetFlag(SharedMemoryStreamFlags.StreamError, true);
            if (syntheticEof)
            {
                SetFlag(SharedMemoryStreamFlags.SyntheticEof, true);
                WriteI64(SharedMemoryProtocol.FinalWriteCursor, GetWriteCursor());
            }
            SetStreamState(SharedMemoryStreamState.Error);
            Touch();
        }

        public void SetStreamState(SharedMemoryStreamState state)
        {
            if (_ptr == null) return;
            WriteI32(SharedMemoryProtocol.StreamState, (int)state);
            Touch();
        }

        public void SetFlag(SharedMemoryStreamFlags flag, bool enabled)
        {
            if (_ptr == null) return;
            uint oldValue;
            uint newValue;
            do
            {
                oldValue = Volatile.Read(ref *(uint*)(_ptr + SharedMemoryProtocol.Flags));
                newValue = enabled ? oldValue | (uint)flag : oldValue & ~(uint)flag;
            } while (Interlocked.CompareExchange(ref *(int*)(_ptr + SharedMemoryProtocol.Flags), unchecked((int)newValue), unchecked((int)oldValue)) != unchecked((int)oldValue));
            Touch();
        }

        public bool IsClientDrained(long toleranceFrames)
        {
            if (_ptr == null) return false;
            var finalCursor = ReadI64(SharedMemoryProtocol.FinalWriteCursor);
            if (finalCursor <= 0) finalCursor = GetWriteCursor();
            return GetReadCursor() >= finalCursor && GetAudibleCursor() + toleranceFrames >= finalCursor;
        }

        // Header field helpers (public for PlaybackController)
        public void WriteI32(int offset, int value) { *(int*)(_ptr + offset) = value; Touch(); }
        public void WriteI64(int offset, long value) { *(long*)(_ptr + offset) = value; Touch(); }
        public long ReadI64(int offset) { return Volatile.Read(ref *(long*)(_ptr + offset)); }
        public int ReadI32(int offset) { return Volatile.Read(ref *(int*)(_ptr + offset)); }
        private void WriteU32(int offset, uint value) { *(uint*)(_ptr + offset) = value; }
        private void WriteU16(int offset, ushort value) { *(ushort*)(_ptr + offset) = value; }
        private void WriteF32(int offset, float value) { *(float*)(_ptr + offset) = value; }
        private void Touch()
        {
            if (_ptr != null)
                Interlocked.Exchange(ref *(long*)(_ptr + SharedMemoryProtocol.LastUpdateTick), DateTime.UtcNow.Ticks);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_viewHandle != IntPtr.Zero) { UnmapViewOfFile(_viewHandle); _viewHandle = IntPtr.Zero; }
            if (_mapHandle != IntPtr.Zero) { CloseHandle(_mapHandle); _mapHandle = IntPtr.Zero; }
            _ptr = null;
        }
    }
}
