using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Native;
using UnityEngine;

namespace ChillPatcher.UIFramework.Audio
{
    /// <summary>
    /// 音频资源管理器
    /// 负责跟踪和清理流式 AudioClip 及其关联资源（如 FlacStreamReader）
    /// </summary>
    public class AudioResourceManager
    {
        private static AudioResourceManager _instance;
        public static AudioResourceManager Instance => _instance ??= new AudioResourceManager();

        private readonly ManualLogSource _logger;
        
        /// <summary>
        /// 跟踪 AudioClip 和其关联的 FlacStreamReader
        /// </summary>
        private readonly Dictionary<int, FlacDecoder.FlacStreamReader> _streamReaders = new Dictionary<int, FlacDecoder.FlacStreamReader>();
        
        /// <summary>
        /// 跟踪 UUID 和其关联的 AudioClip InstanceID
        /// </summary>
        private readonly Dictionary<string, int> _uuidToClipId = new Dictionary<string, int>();

        private AudioResourceManager()
        {
            _logger = BepInEx.Logging.Logger.CreateLogSource("AudioResourceManager");
        }

        /// <summary>
        /// 注册流式 AudioClip 及其关联的 FlacStreamReader
        /// </summary>
        /// <param name="uuid">歌曲 UUID</param>
        /// <param name="clip">AudioClip</param>
        /// <param name="streamReader">FlacStreamReader</param>
        public void RegisterStreamingClip(string uuid, AudioClip clip, FlacDecoder.FlacStreamReader streamReader)
        {
            if (clip == null) return;

            var clipId = clip.GetInstanceID();
            
            _streamReaders[clipId] = streamReader;
            
            if (!string.IsNullOrEmpty(uuid))
            {
                // 如果这个 UUID 已经有关联的 clip，先清理旧的
                if (_uuidToClipId.TryGetValue(uuid, out var oldClipId) && oldClipId != clipId)
                {
                    CleanupClipInternal(oldClipId);
                }
                _uuidToClipId[uuid] = clipId;
            }

            _logger.LogDebug($"Registered streaming clip: {uuid} (ClipID: {clipId})");
        }

        /// <summary>
        /// 清理指定歌曲的音频资源
        /// </summary>
        /// <param name="uuid">歌曲 UUID</param>
        public void CleanupByUUID(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return;

            if (_uuidToClipId.TryGetValue(uuid, out var clipId))
            {
                CleanupClipInternal(clipId);
                _uuidToClipId.Remove(uuid);
                _logger.LogDebug($"Cleaned up audio resources for: {uuid}");
            }
        }

        /// <summary>
        /// 清理指定 AudioClip 的资源
        /// </summary>
        /// <param name="clip">AudioClip</param>
        public void CleanupClip(AudioClip clip)
        {
            if (clip == null) return;
            CleanupClipInternal(clip.GetInstanceID());
        }

        /// <summary>
        /// 内部清理方法
        /// </summary>
        private void CleanupClipInternal(int clipId)
        {
            if (_streamReaders.TryGetValue(clipId, out var reader))
            {
                try
                {
                    reader?.Dispose();
                    _logger.LogDebug($"Disposed FlacStreamReader for ClipID: {clipId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error disposing FlacStreamReader: {ex.Message}");
                }
                _streamReaders.Remove(clipId);
            }
        }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        public void CleanupAll()
        {
            foreach (var reader in _streamReaders.Values)
            {
                try
                {
                    reader?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error disposing FlacStreamReader: {ex.Message}");
                }
            }
            _streamReaders.Clear();
            _uuidToClipId.Clear();
            _logger.LogInfo("Cleaned up all audio resources");
        }

        /// <summary>
        /// 获取当前跟踪的资源数量
        /// </summary>
        public int TrackedCount => _streamReaders.Count;
    }
}
