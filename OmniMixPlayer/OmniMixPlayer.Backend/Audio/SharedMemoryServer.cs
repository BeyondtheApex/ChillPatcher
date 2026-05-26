using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Backend.Audio
{
    public unsafe class SharedMemoryServer : IDisposable
    {
        private const string MapName = @"Global\OmniMixPlayer_PCM";
        private const ulong MagicValue = 0x4D43504C4C494843UL; // "CHILLPCM" little-endian
        private const uint Version = 1;
        private const int DefaultSampleRate = 44100;
        private const int DefaultChannels = 2;
        private const int BufferSeconds = 5;
        private const int HeaderSize = 256;

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
        private byte* _ptr;
        private IntPtr _mapHandle;
        private IntPtr _viewHandle;
        private long _capacity;
        private bool _disposed;

        public int SampleRate { get; set; } = DefaultSampleRate;
        public int Channels { get; set; } = DefaultChannels;
        public int BufferFrames => SampleRate * BufferSeconds;
        public int BufferSize => BufferFrames * Channels * sizeof(float);
        public int TotalSize => HeaderSize + BufferSize;

        public SharedMemoryServer(ILogger logger)
        {
            _logger = logger;
        }

        public bool Initialize()
        {
            try
            {
                _capacity = TotalSize;
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
                                grfAccessPermissions = GENERIC_READ | SYNCHRONIZE,
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

                        _mapHandle = CreateFileMapping(INVALID_HANDLE_VALUE, ref sa, PAGE_READWRITE, sizeHigh, sizeLow, MapName);

                        Marshal.FreeHGlobal(sa.lpSecurityDescriptor);
                        sa.lpSecurityDescriptor = IntPtr.Zero;
                    }
                    else
                    {
                        _mapHandle = CreateFileMapping(INVALID_HANDLE_VALUE, ref sa, PAGE_READWRITE, sizeHigh, sizeLow, MapName);
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
                _logger.LogInformation("Shared memory created: {Name}, size={Size} bytes, buffer={BufferFrames} frames", MapName, TotalSize, BufferFrames);
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
            *(ulong*)_ptr = MagicValue;

            WriteU32(0x08, Version);
            WriteU32(0x0C, (uint)SampleRate);
            WriteU16(0x10, (ushort)Channels);
            WriteU16(0x12, (ushort)(Channels * sizeof(float)));
            WriteU32(0x14, (uint)BufferFrames);
            WriteI32(0x18, 0); // PlayState: stop
            WriteI64(0x1C, 0); // SeekFrame
            WriteU32(0x24, 0); // Flags
            WriteF32(0x78, 0f); // GapSeconds
        }

        public void SetPlayState(int state)
        {
            WriteI32(0x18, state);
        }

        public void SetCurrentUuid(string uuid)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(uuid ?? "");
            for (int i = 0; i < 64 && i < bytes.Length; i++)
                _ptr[0x28 + i] = bytes[i];
            for (int i = bytes.Length; i < 64; i++)
                _ptr[0x28 + i] = 0;
        }

        public void WriteFrames(float[] pcm, int frameCount)
        {
            if (_ptr == null) return;
            var writeCursor = ReadI64(0x68);
            var start = (int)(writeCursor % BufferFrames);
            var sampleCount = frameCount * Channels;
            var totalSamples = BufferFrames * Channels;

            if (start + sampleCount <= totalSamples)
            {
                Marshal.Copy(pcm, 0, (IntPtr)(_ptr + HeaderSize + start * sizeof(float)), sampleCount);
            }
            else
            {
                var first = totalSamples - start;
                Marshal.Copy(pcm, 0, (IntPtr)(_ptr + HeaderSize + start * sizeof(float)), first);
                Marshal.Copy(pcm, first, (IntPtr)(_ptr + HeaderSize), sampleCount - first);
            }

            Interlocked.Exchange(ref *(long*)(_ptr + 0x68), writeCursor + frameCount);
        }

        public int GetReadableFrames()
        {
            if (_ptr == null) return 0;
            var write = Volatile.Read(ref *(long*)(_ptr + 0x68));
            var read = Volatile.Read(ref *(long*)(_ptr + 0x70));
            return (int)(write - read);
        }

        public long GetWriteCursor() => ReadI64(0x68);
        public long GetReadCursor() => ReadI64(0x70);

        // Header field helpers (public for PlaybackController)
        public void WriteI32(int offset, int value) { *(int*)(_ptr + offset) = value; }
        public void WriteI64(int offset, long value) { *(long*)(_ptr + offset) = value; }
        public long ReadI64(int offset) { return *(long*)(_ptr + offset); }
        private void WriteU32(int offset, uint value) { *(uint*)(_ptr + offset) = value; }
        private void WriteU16(int offset, ushort value) { *(ushort*)(_ptr + offset) = value; }
        private void WriteF32(int offset, float value) { *(float*)(_ptr + offset) = value; }

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
