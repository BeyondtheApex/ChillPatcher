using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem.Services.Streaming
{
    public class CorePcmStreamReader : IPcmStreamReader
    {
        private ILogger _logger;

        private string _url;
        private string _format;
        private float _durationHint;

        private HttpAudioCache _cache;
        private DecoderEngine.StreamingDecoder _streamingDecoder;
        private DecoderEngine.FileStreamReader _fileDecoder;
        private DecoderEngine.FlacStreamReader _flacDecoder;
        private RingBuffer _ringBuffer;

        private readonly object _lock = new object();
        private ulong _currentFrame;
        private long _pendingSeek = -1;
        private volatile bool _isReady;
        private volatile bool _isEndOfStream;
        private volatile bool _disposed;
        private volatile bool _switchedToFile;
        private PcmStreamInfo _info;

        private Thread _feedThread;
        private volatile bool _stopFeed;

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

        public CorePcmStreamReader(string url, string format, float durationSeconds, string cacheKey,
            Dictionary<string, string> headers = null, ILogger logger = null)
        {
            var cachePath = Path.Combine(HttpAudioCache.GetCacheDirectory(), $"{cacheKey}.{format.ToLowerInvariant()}");
            Init(url, format, durationSeconds, cachePath, headers, logger);
        }

        public CorePcmStreamReader(string url, string format, float durationSeconds, string cachePath,
            Dictionary<string, string> headers, bool useCachePath, ILogger logger = null)
        {
            var dir = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            Init(url, format, durationSeconds, cachePath, headers, logger);
        }

        private void Init(string url, string format, float durationSeconds, string cachePath,
            Dictionary<string, string> headers, ILogger logger)
        {
            _logger = logger;
            _url = url;
            _format = format.ToLowerInvariant();
            _durationHint = durationSeconds;
            _info = new PcmStreamInfo
            {
                SampleRate = 44100,
                Channels = 2,
                TotalFrames = (ulong)(44100 * durationSeconds),
                Format = _format,
                CanSeek = false
            };

            if (File.Exists(cachePath))
            {
                try
                {
                    InitFileDecoder(cachePath, _format);
                    _isReady = true;
                    _logger?.LogInformation("Using cached file: {Path}", cachePath);
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Cached file invalid, re-downloading: {Msg}", ex.Message);
                }
            }

            _cache = new HttpAudioCache(url, cachePath, headers);
            _cache.OnComplete += OnCacheComplete;

            _ringBuffer = new RingBuffer(RING_BUFFER_SAMPLES);

            if (_format == "mp3" || _format == "flac" || _format == "aac")
            {
                try
                {
                    _streamingDecoder = new DecoderEngine.StreamingDecoder(_format);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Streaming decoder unavailable for {Format}: {Msg}", _format, ex.Message);
                }
                _cache.StartDownload();
                StartFeedThread();
            }
            else
            {
                _cache.StartDownload();
                _logger?.LogInformation("Format {Format}: waiting for download to complete before playback", _format);
            }
        }

        private void InitFileDecoder(string path, string format)
        {
            if (format == "flac" && DecoderEngine.IsAvailable)
            {
                try
                {
                    _flacDecoder = new DecoderEngine.FlacStreamReader(path);
                    _info.SampleRate = _flacDecoder.SampleRate;
                    _info.Channels = _flacDecoder.Channels;
                    _info.TotalFrames = _flacDecoder.TotalPcmFrames;
                    _info.CanSeek = true;
                    _switchedToFile = true;
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("FLAC-specific decoder failed, falling back to generic: {Msg}", ex.Message);
                }
            }

            _fileDecoder = new DecoderEngine.FileStreamReader(path);
            _info.SampleRate = _fileDecoder.SampleRate;
            _info.Channels = _fileDecoder.Channels;
            _info.TotalFrames = _fileDecoder.TotalFrames;
            _info.CanSeek = true;
            _switchedToFile = true;
        }

        private void OnCacheComplete()
        {
            lock (_lock)
            {
                if (_disposed) return;
                try
                {
                    _stopFeed = true;
                    _streamingDecoder?.FeedComplete();
                    InitFileDecoder(_cache.CachePath, _format);
                    _isReady = true;

                    if (_pendingSeek >= 0)
                    {
                        ExecuteSeek((ulong)_pendingSeek);
                        _pendingSeek = -1;
                    }
                    _logger?.LogInformation("Switched to file-based decoder (seekable)");
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Failed to switch decoder: {Msg}", ex.Message);
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
                while (!_stopFeed && !_disposed)
                {
                    if (_cache.Downloaded > 0) break;
                    Thread.Sleep(50);
                }

                long readPosition = 0;
                using (var readStream = new FileStream(_cache.CachePath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite))
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
                        readStream.Seek(readPosition, SeekOrigin.Begin);
                        int bytesRead = readStream.Read(readBuffer, 0, toRead);
                        if (bytesRead <= 0) { Thread.Sleep(10); continue; }
                        readPosition += bytesRead;

                        _streamingDecoder?.FeedData(readBuffer, 0, bytesRead);

                        if (!_isReady && _streamingDecoder is { IsReady: true })
                        {
                            if (_streamingDecoder.TryGetInfo(out int sr, out int ch, out ulong _))
                            {
                                _info.SampleRate = sr;
                                _info.Channels = ch;
                                if (_durationHint > 0)
                                    _info.TotalFrames = (ulong)(sr * _durationHint);
                            }
                            _isReady = true;
                        }

                        while (!_stopFeed && _ringBuffer != null)
                        {
                            if (_ringBuffer.FreeSpace < decodeBuffer.Length) { Thread.Sleep(5); break; }
                            long frames = _streamingDecoder?.ReadFrames(decodeBuffer, 2048) ?? 0;
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
                if (!_disposed) _logger?.LogError("Feed thread error: {Msg}", ex.Message);
            }
        }

        public long ReadFrames(float[] buffer, int framesToRead)
        {
            if (_disposed) return 0;
            if (!_isReady) return 0;

            int channels = _info.Channels > 0 ? _info.Channels : 2;

            lock (_lock)
            {
                if (_pendingSeek >= 0 && _switchedToFile)
                {
                    ExecuteSeek((ulong)_pendingSeek);
                    _pendingSeek = -1;
                }
            }

            if (_switchedToFile)
            {
                long read;
                if (_flacDecoder != null)
                    read = _flacDecoder.ReadFrames(buffer, framesToRead);
                else if (_fileDecoder != null)
                    read = _fileDecoder.ReadFrames(buffer, framesToRead);
                else
                    read = 0;

                if (read > 0) lock (_lock) { _currentFrame += (ulong)read; }
                if (read < framesToRead)
                {
                    Array.Clear(buffer, (int)(read * channels), (int)((framesToRead - read) * channels));
                    lock (_lock) { _isEndOfStream = true; }
                }
                return read;
            }

            if (_ringBuffer != null)
            {
                int samplesToRead = framesToRead * channels;
                int samplesRead = _ringBuffer.Read(buffer, 0, samplesToRead);

                if (samplesRead == 0)
                {
                    if (_cache is { IsComplete: true } && _streamingDecoder != null)
                    {
                        lock (_lock) { _isEndOfStream = true; }
                        return 0;
                    }
                    Array.Clear(buffer, 0, samplesToRead);
                    return 0;
                }

                int framesRead = samplesRead / channels;
                lock (_lock) { _currentFrame += (ulong)framesRead; }

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
                if (_switchedToFile)
                    return ExecuteSeek(frameIndex);

                _pendingSeek = (long)frameIndex;
                _currentFrame = frameIndex;
                _isEndOfStream = false;
                return true;
            }
        }

        private bool ExecuteSeek(ulong frameIndex)
        {
            if (_flacDecoder != null)
            {
                bool ok = _flacDecoder.Seek(frameIndex);
                if (ok) { _currentFrame = frameIndex; _isEndOfStream = false; }
                return ok;
            }
            if (_fileDecoder != null)
            {
                bool ok = _fileDecoder.Seek(frameIndex);
                if (ok) { _currentFrame = frameIndex; _isEndOfStream = false; }
                return ok;
            }
            return false;
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
            _flacDecoder?.Dispose();
            _cache?.Dispose();
        }
    }
}