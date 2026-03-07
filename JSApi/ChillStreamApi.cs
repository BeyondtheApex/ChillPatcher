using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using KanKikuchi.AudioManager;
using Puerts;
using UnityEngine;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 音频流/可视化 API：获取频谱数据、PCM 波形数据、流信息
    /// JS 端通过 chill.stream 访问
    /// 
    /// 性能设计：
    /// - 频谱/波形数据只在 JS 调用时才采集（按需获取，不主动推送）
    /// - 缓冲区复用，避免 GC 压力
    /// - 不使用时零开销
    /// </summary>
    public class ChillStreamApi
    {
        private readonly ManualLogSource _logger;

        // 复用的采样缓冲区
        private float[] _spectrumBuffer;
        private float[] _waveformBuffer;
        private int _lastSpectrumSize;
        private int _lastWaveformSize;

        public ChillStreamApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region 频谱数据 (FFT)

        /// <summary>
        /// 获取 FFT 频谱数据（适合柱状图/波浪可视化）
        /// </summary>
        /// <param name="size">FFT 大小，必须为 2 的幂 (64/128/256/512/1024)，默认 256</param>
        /// <returns>ArrayBuffer → JS 端 new Float32Array(buf) 获得原生数组</returns>
        public ArrayBuffer getSpectrum(int size = 256)
        {
            // 确保是 2 的幂
            size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(size, 64, 8192));

            var source = GetCurrentAudioSource();
            if (source == null) return null;

            // 复用缓冲区
            if (_spectrumBuffer == null || _lastSpectrumSize != size)
            {
                _spectrumBuffer = new float[size];
                _lastSpectrumSize = size;
            }

            try
            {
                source.GetSpectrumData(_spectrumBuffer, 0, FFTWindow.BlackmanHarris);
                return JSApiHelper.ToFloat32Buffer(_spectrumBuffer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[JSApi.Stream] GetSpectrum error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 使用 AudioListener 获取频谱（全局音频，不依赖特定 AudioSource）
        /// </summary>
        public ArrayBuffer getListenerSpectrum(int size = 256)
        {
            size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(size, 64, 8192));

            if (_spectrumBuffer == null || _lastSpectrumSize != size)
            {
                _spectrumBuffer = new float[size];
                _lastSpectrumSize = size;
            }

            try
            {
                AudioListener.GetSpectrumData(_spectrumBuffer, 0, FFTWindow.BlackmanHarris);
                return JSApiHelper.ToFloat32Buffer(_spectrumBuffer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[JSApi.Stream] GetListenerSpectrum error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 波形数据 (PCM Output)

        /// <summary>
        /// 获取当前音频输出的 PCM 波形数据
        /// </summary>
        /// <param name="size">采样数量，必须为 2 的幂，默认 1024</param>
        /// <returns>ArrayBuffer → JS 端 new Float32Array(buf) 获得原生数组</returns>
        public ArrayBuffer getWaveform(int size = 1024)
        {
            size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(size, 64, 8192));

            var source = GetCurrentAudioSource();
            if (source == null) return null;

            if (_waveformBuffer == null || _lastWaveformSize != size)
            {
                _waveformBuffer = new float[size];
                _lastWaveformSize = size;
            }

            try
            {
                source.GetOutputData(_waveformBuffer, 0);
                return JSApiHelper.ToFloat32Buffer(_waveformBuffer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[JSApi.Stream] GetWaveform error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 使用 AudioListener 获取波形（全局音频输出）
        /// </summary>
        public ArrayBuffer getListenerWaveform(int size = 1024)
        {
            size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(size, 64, 8192));

            if (_waveformBuffer == null || _lastWaveformSize != size)
            {
                _waveformBuffer = new float[size];
                _lastWaveformSize = size;
            }

            try
            {
                AudioListener.GetOutputData(_waveformBuffer, 0);
                return JSApiHelper.ToFloat32Buffer(_waveformBuffer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[JSApi.Stream] GetListenerWaveform error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region PCM 流信息

        /// <summary>
        /// 获取当前活跃的 PCM 流信息（流媒体播放时）
        /// </summary>
        public string getStreamInfo()
        {
            var reader = MusicService_SetProgress_Patch.ActivePcmReader;
            if (reader == null) return "null";

            var info = reader.Info;
            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["sampleRate"] = info.SampleRate,
                ["channels"] = info.Channels,
                ["totalFrames"] = (double)info.TotalFrames,
                ["format"] = info.Format ?? "",
                ["duration"] = info.Duration,
                ["canSeek"] = reader.CanSeek,
                ["isReady"] = reader.IsReady,
                ["isEndOfStream"] = reader.IsEndOfStream,
                ["currentFrame"] = (double)reader.CurrentFrame,
                ["cacheProgress"] = reader.CacheProgress,
                ["isCacheComplete"] = reader.IsCacheComplete,
                ["hasPendingSeek"] = reader.HasPendingSeek
            });
        }

        /// <summary>
        /// 获取缓存进度 (0-100)
        /// </summary>
        public double getCacheProgress()
        {
            var reader = MusicService_SetProgress_Patch.ActivePcmReader;
            return reader?.CacheProgress ?? -1;
        }

        /// <summary>
        /// 是否有活跃的 PCM 流
        /// </summary>
        public bool hasActiveStream()
        {
            return MusicService_SetProgress_Patch.ActivePcmReader != null;
        }

        #endregion

        #region 辅助方法

        private AudioSource GetCurrentAudioSource()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var playing = musicService?.PlayingMusic;
            if (playing?.AudioClip == null) return null;

            var musicManager = SingletonMonoBehaviour<MusicManager>.Instance;
            if (musicManager == null) return null;

            var player = musicManager.GetPlayer(playing.AudioClip);
            return player?.AudioSource;
        }

        #endregion
    }
}
