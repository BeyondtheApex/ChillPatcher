using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Logging;
using ChillPatcher.Native;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;

namespace ChillPatcher.ModuleSystem.Services.Streaming
{
    /// <summary>
    /// 核心 PCM 流式读取器
    /// 实现边下边播 + 缓存完成后 O(1) Seek + 延迟 Seek
    /// 
    /// 生命周期:
    /// 1. 创建 → 启动 HTTP 下载 + 增量解码器
    /// 2. 流式模式: 下载数据 → 增量解码 → RingBuffer → ReadFrames
    /// 3. 缓存完成 → 切换到文件解码器 (可寻址)
    /// 4. Seek: 缓存完成前延迟, 完成后立即执行
    /// </summary>
    public class CorePcmStreamReader : IPcmStreamReader
    {
        private static readonly ManualLogSource Logger =
            BepInEx.Logging.Logger.CreateLogSource("CoreStreamReader");

        // 配置
        private readonly string _url;
        private readonly string _format;
        private readonly float _durationHint;
        private readonly Dictionary<string, string> _headers;

        // 组件
        private HttpAudioCache _cache;
        private AudioDecoder.StreamingDecoder _streamingDecoder;
        private AudioDecoder.FileStreamReader _fileDecoder;
        private RingBuffer _ringBuffer;

        // 状态
        private readonly object _lock = new object();
        private ulong _currentFrame;
        private long _pendingSeek = -1;
        private volatile bool _isReady;
        private volatile bool _isEndOfStream;
        private volatile bool _disposed;
        private volatile bool _switchedToFile;
        private PcmStreamInfo _info;

        // 下载线程 → 解码线程
        private Thread _feedThread;
        private volatile bool _stopFeed;

        // 缓冲区大小: 10 秒 stereo
        private const int RING_BUFFER_SAMPLES = 44100 * 2 * 10;

        public PcmStreamInfo Info => _info;
        public ulong CurrentFrame => _currentFrame;
        public bool IsEndOfStream => _isEndOfStream;
        public bool IsReady => _isReady;
        public bool CanSeek => _switchedToFile;
        public bool HasPendingSeek => _pendingSeek >= 0;
        public long PendingSeekFrame => _pendingSeek;
        public double CacheProgress => _cache?.Progress ?? -1;
        public bool IsCacheComplete => _cache?.IsComplete ?? false;

        /// <summary>
        /// 创建核心流式读取器
        /// </summary>
        /// <param name="url">音频 URL</param>
        /// <param name="format">格式: "mp3", "flac", "wav"</param>
        /// <param name="durationSeconds">预估时长 (秒), 用于计算 TotalFrames</param>
        /// <param name="cacheKey">缓存文件名 (不含扩展名)</param>
        /// <param name="headers">可选 HTTP 头</param>
        public CorePcmStreamReader(string url, string format,
            float durationSeconds, string cacheKey,
            Dictionary<string, string> headers = null)
        {
            _url = url;
            _format = format.ToLowerInvariant();
            _durationHint = durationSeconds;
            _headers = headers;

            int estimatedSampleRate = 44100;
            _info = new PcmStreamInfo
            {
                SampleRate = estimatedSampleRate,
                Channels = 2,
                TotalFrames = (ulong)(estimatedSampleRate * durationSeconds),
                Format = _format,
                CanSeek = false
            };

            // 缓存路径
            var cachePath = System.IO.Path.Combine(
                HttpAudioCache.GetCacheDirectory(),
                $"{cacheKey}.{_format}");

            // 检查是否已有缓存
            if (System.IO.File.Exists(cachePath))
            {
                try
                {
                    InitFileDecoder(cachePath);
                    _isReady = true;
                    Logger.LogInfo($"Using cached file: {cachePath}");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Cached file invalid, re-downloading: {ex.Message}");
                }
            }

            // 初始化缓存下载
            _cache = new HttpAudioCache(url, cachePath, headers);
            _cache.OnComplete += OnCacheComplete;

            // MP3 和 FLAC 都支持增量流式解码
            _ringBuffer = new RingBuffer(RING_BUFFER_SAMPLES);

            if (_format == "mp3" || _format == "flac")
            {
                _streamingDecoder = new AudioDecoder.StreamingDecoder(_format);
                _cache.StartDownload();
                StartFeedThread();
            }
            else
            {
                // WAV 等: 等缓存完成后再解码
                _cache.StartDownload();
                Logger.LogInfo($"Format {_format}: waiting for download to complete before playback");
            }
        }

        private void InitFileDecoder(string path)
        {
            _fileDecoder = new AudioDecoder.FileStreamReader(path);
            _switchedToFile = true;

            _info = new PcmStreamInfo
            {
                SampleRate = _fileDecoder.SampleRate,
                Channels = _fileDecoder.Channels,
                TotalFrames = _durationHint > 0
                    ? (ulong)(_fileDecoder.SampleRate * _durationHint)
                    : _fileDecoder.TotalFrames,
                Format = _fileDecoder.Format,
                CanSeek = true
            };
        }

        private void OnCacheComplete()
        {
            lock (_lock)
            {
                if (_disposed) return;

                try
                {
                    // 停止增量解码线程
                    _stopFeed = true;
                    _streamingDecoder?.FeedComplete();

                    // 切换到文件解码器
                    InitFileDecoder(_cache.CachePath);
                    _isReady = true;

                    // 执行延迟 Seek
                    if (_pendingSeek >= 0)
                    {
                        ExecuteSeek((ulong)_pendingSeek);
                        _pendingSeek = -1;
                    }

                    Logger.LogInfo("Switched to file-based decoder (seekable)");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to switch decoder: {ex.Message}");
                }
            }
        }

        private void StartFeedThread()
        {
            _feedThread = new Thread(FeedLoop)
            {
                IsBackground = true,
                Name = "CoreStreamReader_Feed"
            };
            _feedThread.Start();
        }

        private void FeedLoop()
        {
            var readBuffer = new byte[8192];
            var decodeBuffer = new float[4096];

            try
            {
                // 等待缓存开始有数据
                while (!_stopFeed && !_disposed)
                {
                    if (_cache.Downloaded > 0) break;
                    Thread.Sleep(50);
                }

                // 用 FileStream 读取缓存文件, 跟踪已下载位置
                long readPosition = 0;
                using (var readStream = new System.IO.FileStream(
                    _cache.CachePath, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                {
                    while (!_stopFeed && !_disposed)
                    {
                        long downloaded = _cache.Downloaded;
                        long available = downloaded - readPosition;

                        if (available <= 0)
                        {
                            if (_cache.IsComplete) break;
                            Thread.Sleep(10);
                            continue;
                        }

                        int toRead = (int)Math.Min(available, readBuffer.Length);
                        readStream.Seek(readPosition, System.IO.SeekOrigin.Begin);
                        int bytesRead = readStream.Read(readBuffer, 0, toRead);
                        if (bytesRead <= 0)
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        readPosition += bytesRead;
                        _streamingDecoder.FeedData(readBuffer, 0, bytesRead);

                        // 检测音频信息
                        if (!_isReady && _streamingDecoder.IsReady)
                        {
                            if (_streamingDecoder.TryGetInfo(
                                out int sr, out int ch, out ulong _))
                            {
                                _info.SampleRate = sr;
                                _info.Channels = ch;
                                if (_durationHint > 0)
                                    _info.TotalFrames = (ulong)(sr * _durationHint);
                            }
                            _isReady = true;
                        }

                        // 解码到 RingBuffer
                        while (!_stopFeed)
                        {
                            if (_ringBuffer.FreeSpace < decodeBuffer.Length)
                            {
                                Thread.Sleep(5);
                                break;
                            }
                            long frames = _streamingDecoder.ReadFrames(decodeBuffer, 2048);
                            if (frames <= 0) break;
                            int ch = _info.Channels > 0 ? _info.Channels : 2;
                            _ringBuffer.Write(decodeBuffer, 0, (int)frames * ch);
                        }
                    }
                }

                _streamingDecoder?.FeedComplete();
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    Logger.LogError($"Feed thread error: {ex.Message}");
            }
        }

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            if (_disposed) return 0;
            if (!_isReady) return 0;

            int channels = _info.Channels > 0 ? _info.Channels : 2;

            lock (_lock)
            {
                // 检查是否需要执行延迟 Seek
                if (_pendingSeek >= 0 && _switchedToFile && _fileDecoder != null)
                {
                    ExecuteSeek((ulong)_pendingSeek);
                    _pendingSeek = -1;
                }

                // 文件解码器 (缓存完成后)
                if (_switchedToFile && _fileDecoder != null)
                {
                    long read = _fileDecoder.ReadFrames(buffer, framesToRead);
                    if (read <= 0)
                    {
                        _isEndOfStream = true;
                        return 0;
                    }
                    _currentFrame += (ulong)read;
                    return read;
                }
            }

            // 流式模式: 从 RingBuffer 读取
            if (_ringBuffer != null)
            {
                int samplesToRead = framesToRead * channels;
                int samplesRead = _ringBuffer.Read(buffer, 0, samplesToRead);

                if (samplesRead == 0)
                {
                    if (_cache != null && _cache.IsComplete &&
                        _streamingDecoder != null)
                    {
                        lock (_lock) { _isEndOfStream = true; }
                        return 0;
                    }
                    // 暂无数据, 填静音但不推进播放位置
                    Array.Clear(buffer, 0, samplesToRead);
                    return 0;
                }

                int framesRead = samplesRead / channels;
                lock (_lock) { _currentFrame += (ulong)framesRead; }

                // 不足的部分填静音
                if (samplesRead < samplesToRead)
                    Array.Clear(buffer, samplesRead, samplesToRead - samplesRead);

                return framesRead;
            }

            return 0;
        }

        public bool Seek(ulong frameIndex)
        {
            if (_disposed) return false;

            lock (_lock)
            {
                if (_switchedToFile && _fileDecoder != null)
                {
                    return ExecuteSeek(frameIndex);
                }

                // 缓存未完成 → 延迟 Seek
                _pendingSeek = (long)frameIndex;
                _currentFrame = frameIndex;
                _isEndOfStream = false;
                Logger.LogInfo($"Deferred seek to frame {frameIndex}");
                return true;
            }
        }

        private bool ExecuteSeek(ulong frameIndex)
        {
            if (_fileDecoder == null) return false;

            bool ok = _fileDecoder.Seek(frameIndex);
            if (ok)
            {
                _currentFrame = frameIndex;
                _isEndOfStream = false;
            }
            return ok;
        }

        public void CancelPendingSeek()
        {
            _pendingSeek = -1;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stopFeed = true;

            _feedThread?.Join(500);
            _streamingDecoder?.Dispose();
            _fileDecoder?.Dispose();
            _cache?.Dispose();
        }
    }
}
