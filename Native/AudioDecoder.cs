using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ChillPatcher.Native
{
    /// <summary>
    /// 统一音频解码器 Native Plugin 接口
    /// 支持 MP3/FLAC/WAV 文件解码和 MP3/FLAC 增量流式解码
    /// </summary>
    public static class AudioDecoder
    {
        private const string DLL_NAME = "ChillAudioDecoder";
        private static IntPtr DllHandle = IntPtr.Zero;

        static AudioDecoder()
        {
            try
            {
                var pluginDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                var arch = IntPtr.Size == 8 ? "x64" : "x86";
                var dllPath = Path.Combine(pluginDir, "native", arch, "ChillAudioDecoder.dll");

                if (!File.Exists(dllPath))
                {
                    Plugin.Log.LogWarning($"[AudioDecoder] DLL not found at: {dllPath}");
                    return;
                }

                DllHandle = LoadLibrary(dllPath);
                if (DllHandle == IntPtr.Zero)
                    Plugin.Log.LogError($"[AudioDecoder] Failed to load DLL from: {dllPath}");
                else
                    Plugin.Log.LogInfo($"[AudioDecoder] Loaded Native DLL from: {dllPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AudioDecoder] Exception loading DLL: {ex}");
            }
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public static bool IsAvailable => DllHandle != IntPtr.Zero;

        // ========== File-based (seekable) P/Invoke ==========

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr AudioDecoder_OpenFile(
            string filePath,
            out int sampleRate,
            out int channels,
            out ulong totalFrames,
            [Out] byte[] format);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern long AudioDecoder_ReadFrames(
            IntPtr handle,
            [Out] float[] buffer,
            int framesToRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_Seek(IntPtr handle, ulong frameIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_Close(IntPtr handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AudioDecoder_GetLastError();

        // ========== Streaming (incremental) P/Invoke ==========

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr AudioDecoder_CreateStreaming(string format);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_FeedData(IntPtr handle, byte[] data, int size);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_FeedComplete(IntPtr handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern long AudioDecoder_StreamingRead(
            IntPtr handle,
            [Out] float[] buffer,
            int framesToRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_StreamingIsReady(IntPtr handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_StreamingGetInfo(
            IntPtr handle,
            out int sampleRate,
            out int channels,
            out ulong totalFrames);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_CloseStreaming(IntPtr handle);

        // ========== Helper ==========

        private static string GetError()
        {
            try
            {
                var ptr = AudioDecoder_GetLastError();
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : "Unknown error";
            }
            catch { return "Unknown error"; }
        }

        /// <summary>
        /// 文件流式读取器 (可寻址, 用于缓存完成后的高效 Seek)
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
                _handle = AudioDecoder_OpenFile(filePath,
                    out int sr, out int ch, out ulong frames, formatBuf);

                if (_handle == IntPtr.Zero)
                    throw new Exception($"Failed to open audio: {GetError()}");

                SampleRate = sr;
                Channels = ch;
                TotalFrames = frames;

                int len = Array.IndexOf<byte>(formatBuf, 0);
                Format = len > 0
                    ? System.Text.Encoding.ASCII.GetString(formatBuf, 0, len)
                    : "unknown";
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
        /// </summary>
        public class StreamingDecoder : IDisposable
        {
            private IntPtr _handle;
            private bool _disposed;

            public StreamingDecoder(string format)
            {
                _handle = AudioDecoder_CreateStreaming(format);
                if (_handle == IntPtr.Zero)
                    throw new Exception($"Failed to create streaming decoder: {GetError()}");
            }

            public void FeedData(byte[] data, int offset, int count)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(StreamingDecoder));
                if (offset == 0 && count == data.Length)
                {
                    AudioDecoder_FeedData(_handle, data, count);
                }
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
                if (_disposed)
                {
                    sampleRate = 0; channels = 0; totalFrames = 0;
                    return false;
                }
                return AudioDecoder_StreamingGetInfo(_handle,
                    out sampleRate, out channels, out totalFrames) == 0;
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
    }
}
