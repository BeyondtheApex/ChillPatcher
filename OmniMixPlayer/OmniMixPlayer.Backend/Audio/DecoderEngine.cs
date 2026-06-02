using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Backend.Audio
{
    /// <summary>
    /// 统一音频解码引擎 — 基于 Rust Symphonia (OmniAudioDecoder.dll)
    /// 替代旧的 ChillAudioDecoder.dll + ChillFlacDecoder.dll
    /// 
    /// 核心能力:
    ///   - 自动格式检测 (MP3/FLAC/WAV/AAC/Vorbis/ALAC/PCM)
    ///   - 原生支持增长文件 (HTTP 下载中的文件也可读)
    ///   - Seek 始终可用
    /// </summary>
    public static class DecoderEngine
    {
        private const string OMNI_DLL = "OmniAudioDecoder";

        private static ILogger _logger;
        private static bool _initialized;
        private static bool _available;

        public static bool IsAvailable => _available;

        // ========== Init ==========

        public static void Initialize(ILogger logger, string baseDirectory)
        {
            if (_initialized) return;
            _initialized = true;
            _logger = logger;

            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var nativeDir = Path.Combine(baseDirectory, "native", arch);
            _available = TryLoadDll(nativeDir, "OmniAudioDecoder.dll");
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
            _logger?.LogInformation("[DecoderEngine] Loaded {Name} (Symphonia)", fileName);
            return true;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        // ========== P/Invoke: OmniAudioDecoder (Rust Symphonia) ==========

        [DllImport(OMNI_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr AudioDecoder_OpenFile(
            string filePath, out int sampleRate, out int channels,
            out ulong totalFrames, [Out] byte[] format, [MarshalAs(UnmanagedType.U1)] bool isGrowing);

        [DllImport(OMNI_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern long AudioDecoder_ReadFrames(IntPtr handle, [Out] float[] buffer, int framesToRead);

        [DllImport(OMNI_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AudioDecoder_Seek(IntPtr handle, ulong frameIndex);

        [DllImport(OMNI_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AudioDecoder_Close(IntPtr handle);

        [DllImport(OMNI_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AudioDecoder_GetLastError();

        private static string GetError()
        {
            try
            {
                var ptr = AudioDecoder_GetLastError();
                return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : "Unknown";
            }
            catch { return "Unknown"; }
        }

        // ========== Public Decoder ==========

        /// <summary>
        /// 统一文件解码器。
        /// 支持完整文件、增长文件（HTTP 边下边播）、所有格式。
        /// Symphonia 原生处理增长文件: ReadFrames 在数据不足时返回 0，下次重试即可。
        /// </summary>
        public class OmniFileDecoder : IDisposable
        {
            private IntPtr _handle;
            private bool _disposed;

            public int SampleRate { get; }
            public int Channels { get; }
            /// <summary>0 = 未知（增长文件中），非零 = 完整文件的总帧数</summary>
            public ulong TotalFrames { get; }
            public string Format { get; }

            public OmniFileDecoder(string filePath, bool isGrowing)
            {
                var formatBuf = new byte[16];
                _handle = AudioDecoder_OpenFile(filePath,
                    out int sr, out int ch, out ulong frames, formatBuf, isGrowing);

                if (_handle == IntPtr.Zero)
                    throw new Exception($"Failed to open audio: {GetError()}");

                SampleRate = sr;
                Channels = ch;
                TotalFrames = frames;
                int len = Array.IndexOf(formatBuf, (byte)0);
                Format = len > 0
                    ? System.Text.Encoding.ASCII.GetString(formatBuf, 0, len)
                    : "unknown";
            }

            /// <summary>
            /// 读取 PCM 帧（交错 f32）。
            /// 增长文件: 数据不够返回 0，下次重试即可。
            /// 完整文件: 返回 0 表示 EOF。
            /// </summary>
            public long ReadFrames(float[] buffer, int framesToRead)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(OmniFileDecoder));
                return AudioDecoder_ReadFrames(_handle, buffer, framesToRead);
            }

            public bool Seek(ulong frameIndex)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(OmniFileDecoder));
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
    }
}
