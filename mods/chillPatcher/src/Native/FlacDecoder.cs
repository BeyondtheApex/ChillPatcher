using System;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;

namespace ChillPatcher.Native
{
    /// <summary>
    /// FLAC 解码器 Native Plugin 接口
    /// </summary>
    public static class FlacDecoder
    {
        // DLL 名称 - .NET 会自动搜索路径
        private const string DLL_NAME = "ChillFlacDecoder";

        // DLL 完整路径（在类初始化时设置）
        private static string DllPath;
        private static IntPtr DllHandle = IntPtr.Zero;

        // 静态构造函数：手动加载 DLL
        static FlacDecoder()
        {
            try
            {
                // BepInEx 插件目录
                var pluginDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);

                // 检测架构并选择正确的 DLL
                var arch = IntPtr.Size == 8 ? "x64" : "x86";
                DllPath = Path.Combine(pluginDir, "native", arch, "ChillFlacDecoder.dll");

                if (!File.Exists(DllPath))
                {
                    Plugin.Log.LogWarning($"[FlacDecoder] DLL not found at: {DllPath}");
                    return;
                }

                // Windows: 使用 LoadLibrary 手动加载 DLL
                DllHandle = LoadLibrary(DllPath);
                if (DllHandle == IntPtr.Zero)
                {
                    Plugin.Log.LogError($"[FlacDecoder] Failed to load DLL from: {DllPath}");
                }
                else
                {
                    Plugin.Log.LogInfo($"[FlacDecoder] ✅ Loaded Native DLL from: {DllPath}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FlacDecoder] Exception loading DLL: {ex}");
            }
        }

        // Windows LoadLibrary
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// FLAC 音频信息结构（与 C++ 对应）
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct FlacAudioInfo
        {
            public int sampleRate;
            public int channels;
            public ulong totalPcmFrameCount;
            public IntPtr pcmData;        // float*
            public UIntPtr pcmDataSize;
        }

        /// <summary>
        /// 解码 FLAC 文件
        /// </summary>

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern int DecodeFlacFile(string filePath, out FlacAudioInfo info);

        /// <summary>
        /// 释放 PCM 数据
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeFlacData(ref FlacAudioInfo info);

        /// <summary>
        /// 获取最后的错误消息
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr FlacGetLastError();

        // ========== 流式解码 API ==========

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenFlacStream(
            string filePath,
            out int sampleRate,
            out int channels,
            out ulong totalPcmFrames);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern long ReadFlacFrames(
            IntPtr streamHandle,
            [Out] float[] buffer,
            ulong framesToRead);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SeekFlacStream(IntPtr streamHandle, ulong frameIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CloseFlacStream(IntPtr streamHandle);

        /// <summary>
        /// [已废弃] 解码 FLAC 文件并创建 Unity AudioClip（一次性全部加载到内存）
        /// 
        /// ⚠️ 此方法会将整个 FLAC 文件解码到内存中，对于高采样率的文件会占用大量内存。
        /// 建议使用 FlacStreamReader + CreateStreamingAudioClip 实现流式播放。
        /// 
        /// 保留此方法仅用于调试和特殊场景。
        /// </summary>
        /// <param name="filePath">FLAC 文件路径</param>
        /// <param name="clipName">AudioClip 名称</param>
        /// <returns>Unity AudioClip 对象，失败返回 null</returns>
        [Obsolete("Use FlacStreamReader for streaming playback to reduce memory usage")]
        public static AudioClip DecodeFlacToAudioClip(string filePath, string clipName)
        {
            if (!File.Exists(filePath))
            {
                Plugin.Log.LogError($"[FlacDecoder] File not found: {filePath}");
                return null;
            }

            FlacAudioInfo info = default;

            try
            {
                // 调用 Native Plugin 解码
                int result = DecodeFlacFile(filePath, out info);

                if (result != 0)
                {
                    string error = GetErrorMessage();
                    Plugin.Log.LogError($"[FlacDecoder] Failed to decode FLAC: {error} (code={result})");
                    return null;
                }

                // 验证音频参数
                if (info.channels < 1 || info.channels > 8)
                {
                    Plugin.Log.LogError($"[FlacDecoder] Invalid channel count: {info.channels}");
                    return null;
                }

                if (info.sampleRate < 8000 || info.sampleRate > 192000)
                {
                    Plugin.Log.LogError($"[FlacDecoder] Invalid sample rate: {info.sampleRate}");
                    return null;
                }

                if (info.totalPcmFrameCount == 0 || info.pcmData == IntPtr.Zero)
                {
                    Plugin.Log.LogError("[FlacDecoder] No PCM data decoded");
                    return null;
                }

                // 复制 PCM 数据到托管数组
                int sampleCount = (int)(info.totalPcmFrameCount * (ulong)info.channels);
                float[] pcmData = new float[sampleCount];
                Marshal.Copy(info.pcmData, pcmData, 0, sampleCount);

                // 创建 Unity AudioClip
                AudioClip clip = AudioClip.Create(
                    clipName,
                    (int)info.totalPcmFrameCount,
                    info.channels,
                    info.sampleRate,
                    false
                );

                if (clip == null)
                {
                    Plugin.Log.LogError("[FlacDecoder] Failed to create AudioClip");
                    return null;
                }

                // 设置 PCM 数据
                if (!clip.SetData(pcmData, 0))
                {
                    Plugin.Log.LogError("[FlacDecoder] Failed to set AudioClip data");
                    UnityEngine.Object.Destroy(clip);
                    return null;
                }

                Plugin.Log.LogInfo($"[FlacDecoder] ✅ Decoded: {Path.GetFileName(filePath)} " +
                    $"({info.sampleRate}Hz, {info.channels}ch, {info.totalPcmFrameCount} frames)");

                return clip;
            }
            catch (DllNotFoundException ex)
            {
                Plugin.Log.LogError($"[FlacDecoder] Native DLL not found: {ex.Message}");
                Plugin.Log.LogError($"[FlacDecoder] Please ensure ChillFlacDecoder.dll is in BepInEx/plugins/ChillPatcher/native/");
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FlacDecoder] Exception: {ex}");
                return null;
            }
            finally
            {
                // 释放 Native 分配的内存
                if (info.pcmData != IntPtr.Zero)
                {
                    FreeFlacData(ref info);
                }
            }
        }

        /// <summary>
        /// 获取 Native Plugin 错误消息
        /// </summary>
        private static string GetErrorMessage()
        {
            try
            {
                IntPtr ptr = FlacGetLastError();
                if (ptr != IntPtr.Zero)
                {
                    return Marshal.PtrToStringAnsi(ptr) ?? "Unknown error";
                }
            }
            catch { }
            return "Unknown error";
        }

        /// <summary>
        /// 检查 Native Plugin 是否可用
        /// </summary>
        public static bool IsAvailable()
        {
            return DllHandle != IntPtr.Zero;
        }

        /// <summary>
        /// FLAC 流式读取器
        /// </summary>
        public class FlacStreamReader : IDisposable
        {
            private IntPtr _streamHandle;
            private bool _disposed = false;

            public int SampleRate { get; private set; }
            public int Channels { get; private set; }
            public ulong TotalPcmFrames { get; private set; }
            public ulong CurrentFrame { get; private set; }

            public FlacStreamReader(string filePath)
            {
                _streamHandle = OpenFlacStream(
                    filePath,
                    out int sampleRate,
                    out int channels,
                    out ulong totalFrames);

                if (_streamHandle == IntPtr.Zero)
                {
                    throw new Exception($"Failed to open FLAC stream: {GetErrorMessage()}");
                }

                SampleRate = sampleRate;
                Channels = channels;
                TotalPcmFrames = totalFrames;
                CurrentFrame = 0;

                Plugin.Log.LogInfo($"[FlacStreamReader] Opened stream: {sampleRate}Hz, {channels}ch, {totalFrames} frames");
            }

            /// <summary>
            /// 读取 PCM 帧到缓冲区
            /// </summary>
            /// <param name="buffer">交错格式的 float 缓冲区（长度 = 帧数 * 声道数）</param>
            /// <param name="framesToRead">要读取的帧数</param>
            /// <returns>实际读取的帧数</returns>
            public long ReadFrames(float[] buffer, ulong framesToRead)
            {
                if (_disposed || _streamHandle == IntPtr.Zero)
                    throw new ObjectDisposedException(nameof(FlacStreamReader));

                long framesRead = ReadFlacFrames(_streamHandle, buffer, framesToRead);
                if (framesRead > 0)
                {
                    CurrentFrame += (ulong)framesRead;
                }

                return framesRead;
            }

            /// <summary>
            /// 定位到指定帧
            /// </summary>
            public bool Seek(ulong frameIndex)
            {
                if (_disposed || _streamHandle == IntPtr.Zero)
                    throw new ObjectDisposedException(nameof(FlacStreamReader));

                int result = SeekFlacStream(_streamHandle, frameIndex);
                if (result == 0)
                {
                    CurrentFrame = frameIndex;
                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    if (_streamHandle != IntPtr.Zero)
                    {
                        CloseFlacStream(_streamHandle);
                        _streamHandle = IntPtr.Zero;
                        Plugin.Log.LogDebug($"[FlacStreamReader] Closed stream");
                    }
                    _disposed = true;
                }
            }
        }
    }
}
