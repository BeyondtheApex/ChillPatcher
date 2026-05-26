using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChillPatcher.SDK.Ipc
{
    public unsafe class SharedMemoryReader : IDisposable
    {
        private const string MapName = @"Global\OmniMixPlayer_PCM";
        private const string UwpPrefix = @"AppContainerNamedObjects\";

        private const ulong MagicValue = 0x4D43504C4C494843UL; // "CHILLPCM" little-endian
        private const int HeaderSize = 256;

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private byte* _ptr;
        private bool _disposed;

        public bool IsDisposed => _disposed;

        public int SampleRate { get; private set; }
        public int Channels { get; private set; }
        public int BytesPerFrame { get; private set; }
        public int BufferFrames { get; private set; }
        public int BufferSize { get; private set; }

        public bool Initialize()
        {
            try
            {
                _mmf = OpenMap(MapName);
            }
            catch
            {
                try
                {
                    _mmf = OpenMap(UwpPrefix + MapName);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            if (_mmf == null)
                return false;

            _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            byte* ptr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            _ptr = ptr;

            if (!ValidateMagic())
            {
                Dispose();
                return false;
            }

            ReadHeader();
            return true;
        }

        private static MemoryMappedFile OpenMap(string name)
        {
            try
            {
                return MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read);
            }
            catch
            {
                return null;
            }
        }

        private bool ValidateMagic()
        {
            if (_ptr == null) return false;
            return *(ulong*)(_ptr + 0) == MagicValue;
        }

        private void ReadHeader()
        {
            SampleRate = *(int*)(_ptr + 0x0C);
            Channels = *(ushort*)(_ptr + 0x10);
            BytesPerFrame = *(ushort*)(_ptr + 0x12);
            BufferFrames = *(int*)(_ptr + 0x14);
            BufferSize = BufferFrames * Channels * sizeof(float);
        }

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            if (_ptr == null) return -1;

            var writeCursor = Volatile.Read(ref *(long*)(_ptr + 0x68));
            var readCursor = Volatile.Read(ref *(long*)(_ptr + 0x70));

            var available = (int)(writeCursor - readCursor);
            if (available <= 0) return 0;

            var framesToCopy = Math.Min(framesToRead, available);
            var sampleCount = framesToCopy * Channels;
            var totalSamples = BufferFrames * Channels;
            var start = (int)(readCursor % BufferFrames);

            if (start + sampleCount <= totalSamples)
            {
                Marshal.Copy((IntPtr)(_ptr + HeaderSize + start * sizeof(float)), buffer, 0, sampleCount);
            }
            else
            {
                var firstPart = totalSamples - start;
                Marshal.Copy((IntPtr)(_ptr + HeaderSize + start * sizeof(float)), buffer, 0, firstPart);
                Marshal.Copy((IntPtr)(_ptr + HeaderSize), buffer, firstPart, sampleCount - firstPart);
            }

            Interlocked.Exchange(ref *(long*)(_ptr + 0x70), readCursor + framesToCopy);
            return framesToCopy;
        }

        public string GetCurrentUuid()
        {
            if (_ptr == null) return null;
            var chars = new char[64];
            var len = 0;
            for (int i = 0; i < 64; i++)
            {
                var b = _ptr[0x28 + i];
                if (b == 0) break;
                chars[len++] = (char)b;
            }
            return new string(chars, 0, len);
        }

        public int GetPlayState()
        {
            if (_ptr == null) return 0;
            return *(int*)(_ptr + 0x18);
        }

        public long GetSeekFrame()
        {
            if (_ptr == null) return 0;
            return *(long*)(_ptr + 0x1C);
        }

        public uint GetFlags()
        {
            if (_ptr == null) return 0;
            return *(uint*)(_ptr + 0x24);
        }

        public float GetGapSeconds()
        {
            if (_ptr == null) return 0f;
            return *(float*)(_ptr + 0x78);
        }

        public long GetWriteCursor()
        {
            if (_ptr == null) return 0;
            return Volatile.Read(ref *(long*)(_ptr + 0x68));
        }

        public long GetReadCursor()
        {
            if (_ptr == null) return 0;
            return Volatile.Read(ref *(long*)(_ptr + 0x70));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_ptr != null)
            {
                _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
                _ptr = null;
            }
            _accessor?.Dispose();
            _mmf?.Dispose();
        }
    }
}
