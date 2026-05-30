using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Backend.Audio
{
    public static class DecoderEngine
    {
        private static ILogger _logger;
        private static bool _initialized;

        public static bool IsAvailable => _audioDecoderAvailable;

        private static bool _audioDecoderAvailable;
        private static bool _flacDecoderAvailable;

        public static void Initialize(ILogger logger, string baseDirectory)
        {
            if (_initialized) return;
            _initialized = true;
            _logger = logger;

            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var nativeDir = Path.Combine(baseDirectory, "native", arch);

            _audioDecoderAvailable = TryLoadDll(nativeDir, "ChillAudioDecoder.dll");
            _flacDecoderAvailable = TryLoadDll(nativeDir, "ChillFlacDecoder.dll");
        }

        private static bool TryLoadDll(string dir, string fileName)
        {
            var path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                _logger?.LogWarning("[DecoderEngine] {Name} not found at: {Path}", fileName, path);
                return false;
            }
            var handle = LoadLibrary(path);
            if (handle == IntPtr.Zero)
            {
                _logger?.LogWarning("[DecoderEngine] Failed to load {Name}: error {Error}", fileName, Marshal.GetLastWin32Error());
                return false;
            }
            _logger?.LogInformation("[DecoderEngine] Loaded {Name}", fileName);
            return true;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        // ========== ChillAudioDecoder.dll P/Invoke ==========

        private const string AUDIO_DLL = "ChillAudioDecoder";

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr AudioDecoder_OpenFile(string filePath, out int sampleRate, out int channels, out ulong totalFrames, [Out] byte[] format);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern long AudioDecoder_ReadFrames(IntPtr handle, [Out] float[] buffer, int framesToRead);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_Seek(IntPtr handle, ulong frameIndex);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_Close(IntPtr handle);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AudioDecoder_GetLastError();

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr AudioDecoder_CreateStreaming(string format);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_FeedData(IntPtr handle, byte[] data, int size);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_FeedComplete(IntPtr handle);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern long AudioDecoder_StreamingRead(IntPtr handle, [Out] float[] buffer, int framesToRead);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_StreamingIsReady(IntPtr handle);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_StreamingGetInfo(IntPtr handle, out int sampleRate, out int channels, out ulong totalFrames);

        [DllImport(AUDIO_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_CloseStreaming(IntPtr handle);

        private static string AudioGetLastError()
        {
            try
            {
                var ptr = AudioDecoder_GetLastError();
                return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : "Unknown";
            }
            catch { return "Unknown"; }
        }

        // ========== ChillFlacDecoder.dll P/Invoke ==========

        private const string FLAC_DLL = "ChillFlacDecoder";

        [DllImport(FLAC_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenFlacStream(string filePath, out int sampleRate, out int channels, out ulong totalPcmFrames);

        [DllImport(FLAC_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern long ReadFlacFrames(IntPtr streamHandle, [Out] float[] buffer, ulong framesToRead);

        [DllImport(FLAC_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SeekFlacStream(IntPtr streamHandle, ulong frameIndex);

        [DllImport(FLAC_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CloseFlacStream(IntPtr streamHandle);

        [DllImport(FLAC_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr FlacGetLastError();

        private static string FlacGetLastErrorStr()
        {
            try
            {
                var ptr = FlacGetLastError();
                return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : "Unknown";
            }
            catch { return "Unknown"; }
        }

        // ========== Public Decoder Classes ==========

        /// <summary>
        /// 文件流式读取器 (可寻址, 用于缓存完成后的高效 Seek)
        /// 使用 ChillAudioDecoder.dll
        /// </summary>
        public class FileStreamReader : IDisposable
        {
            private IntPtr _handle;
            private bool _disposed;

            public int SampleRate { get; }
            public int Channels { get; }
            public ulong TotalFrames { get; }
            public string Format { get; }

            public FileStreamReader(string filePath)
            {
                var formatBuf = new byte[16];
                _handle = AudioDecoder_OpenFile(filePath, out int sr, out int ch, out ulong frames, formatBuf);
                if (_handle == IntPtr.Zero)
                    throw new Exception($"Failed to open audio file: {AudioGetLastError()}");

                SampleRate = sr;
                Channels = ch;
                TotalFrames = frames;
                int len = Array.IndexOf(formatBuf, (byte)0);
                Format = len > 0 ? System.Text.Encoding.ASCII.GetString(formatBuf, 0, len) : "unknown";
            }

            public long ReadFrames(float[] buffer, int framesToRead)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(FileStreamReader));
                return AudioDecoder_ReadFrames(_handle, buffer, framesToRead);
            }

            public bool Seek(ulong frameIndex)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(FileStreamReader));
                return AudioDecoder_Seek(_handle, frameIndex) == 0;
            }

            public void Dispose()
            {
                if (!_disposed && _handle != IntPtr.Zero)
                {
                    AudioDecoder_Close(_handle);
                    _handle = IntPtr.Zero;
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// 增量流式解码器 (用于边下边播)
        /// 使用 ChillAudioDecoder.dll
        /// </summary>
        public class StreamingDecoder : IDisposable
        {
            private IntPtr _handle;
            private bool _disposed;

            public StreamingDecoder(string format)
            {
                _handle = AudioDecoder_CreateStreaming(format);
                if (_handle == IntPtr.Zero)
                    throw new Exception($"Failed to create streaming decoder: {AudioGetLastError()}");
            }

            public void FeedData(byte[] data, int offset, int count)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(StreamingDecoder));
                if (offset == 0 && count == data.Length)
                    AudioDecoder_FeedData(_handle, data, count);
                else
                {
                    var chunk = new byte[count];
                    Buffer.BlockCopy(data, offset, chunk, 0, count);
                    AudioDecoder_FeedData(_handle, chunk, count);
                }
            }

            public void FeedComplete()
            {
                if (!_disposed) AudioDecoder_FeedComplete(_handle);
            }

            /// <returns>frames read, 0=no data, -2=EOF</returns>
            public long ReadFrames(float[] buffer, int framesToRead)
            {
                if (_disposed) return -1;
                return AudioDecoder_StreamingRead(_handle, buffer, framesToRead);
            }

            public bool IsReady
            {
                get
                {
                    if (_disposed) return false;
                    return AudioDecoder_StreamingIsReady(_handle) == 1;
                }
            }

            public bool TryGetInfo(out int sampleRate, out int channels, out ulong totalFrames)
            {
                if (_disposed) { sampleRate = 0; channels = 0; totalFrames = 0; return false; }
                return AudioDecoder_StreamingGetInfo(_handle, out sampleRate, out channels, out totalFrames) == 0;
            }

            public void Dispose()
            {
                if (!_disposed && _handle != IntPtr.Zero)
                {
                    AudioDecoder_CloseStreaming(_handle);
                    _handle = IntPtr.Zero;
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// FLAC 流式读取器 (可寻址, 推荐用于 FLAC)
        /// 使用 ChillFlacDecoder.dll
        /// </summary>
        public class FlacStreamReader : IDisposable
        {
            private IntPtr _handle;
            private bool _disposed;

            public int SampleRate { get; }
            public int Channels { get; }
            public ulong TotalPcmFrames { get; }
            public ulong CurrentFrame { get; private set; }

            public FlacStreamReader(string filePath)
            {
                _handle = OpenFlacStream(filePath, out int sr, out int ch, out ulong total);
                if (_handle == IntPtr.Zero)
                    throw new Exception($"Failed to open FLAC stream: {FlacGetLastErrorStr()}");

                SampleRate = sr;
                Channels = ch;
                TotalPcmFrames = total;
            }

            public long ReadFrames(float[] buffer, int framesToRead)
            {
                if (_disposed || _handle == IntPtr.Zero)
                    throw new ObjectDisposedException(nameof(FlacStreamReader));
                long read = ReadFlacFrames(_handle, buffer, (ulong)framesToRead);
                if (read > 0) CurrentFrame += (ulong)read;
                return read;
            }

            public bool Seek(ulong frameIndex)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(FlacStreamReader));
                if (SeekFlacStream(_handle, frameIndex) == 0) { CurrentFrame = frameIndex; return true; }
                return false;
            }

            public void Dispose()
            {
                if (!_disposed && _handle != IntPtr.Zero)
                {
                    CloseFlacStream(_handle);
                    _handle = IntPtr.Zero;
                    _disposed = true;
                }
            }
        }
    }
}