using System;
using System.Threading;
using System.Threading.Tasks;
using Bulbul;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChillPatcher.UIFramework.Audio
{
    /// <summary>
    /// 流媒体音频加载器 (IPC 版本)
    /// 通过 SharedMemoryReader 从 OmniMixPlayer 后端读取 PCM 数据
    /// </summary>
    public static class StreamingAudioLoader
    {
        public static bool IsStreamingSource(GameAudioInfo audioInfo)
        {
            if (audioInfo == null) return false;
            // Module songs have empty LocalPath and non-null UUID
            return !string.IsNullOrEmpty(audioInfo.UUID) && string.IsNullOrEmpty(audioInfo.LocalPath);
        }

        public static async Task<AudioClip> LoadFromSharedMemoryAsync(
            string uuid,
            int sampleRate = 44100,
            int channels = 2,
            CancellationToken cancellationToken = default)
        {
            var shm = OmniMixIntegration.Instance?.SharedMemory;
            if (shm == null)
            {
                Plugin.Log.LogWarning("[StreamingLoader] Shared memory not available");
                return null;
            }

            sampleRate = shm.SampleRate > 0 ? shm.SampleRate : sampleRate;
            channels = shm.Channels > 0 ? shm.Channels : channels;

            // Create a large enough clip (5 seconds buffer, stereo float)
            int bufferFrames = sampleRate * 5;
            var clip = AudioClip.Create(
                $"pcm_stream_{uuid}",
                bufferFrames,
                channels,
                sampleRate,
                true,
                (data) => OnPcmDataRead(data, shm),
                (pos) => { });

            // Wait for data to become available
            await UniTask.Delay(500, cancellationToken: cancellationToken);
            return clip;
        }

        private static void OnPcmDataRead(float[] data, ChillPatcher.SDK.Ipc.SharedMemoryReader reader)
        {
            try
            {
                if (reader == null)
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                int framesToRead = data.Length / 2; // stereo
                reader.ReadFrames(data, framesToRead);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[StreamingLoader] PCM read error: {ex.Message}");
                Array.Clear(data, 0, data.Length);
            }
        }

        public static bool IsModuleStreamingEnabled(string moduleId)
        {
            // Always enabled when IPC connected
            return OmniMixIntegration.Instance?.IsConnected ?? false;
        }
    }
}
