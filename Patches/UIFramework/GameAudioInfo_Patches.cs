using Bulbul;
using HarmonyLib;
using System.IO;
using UnityEngine;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.UIFramework.Audio;
using ChillPatcher.Native;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// GameAudioInfo补丁集合
    /// </summary>
    [HarmonyPatch]
    public class GameAudioInfo_Patches
    {
        private static bool _nativeDecoderChecked = false;
        private static bool _nativeDecoderAvailable = false;

        /// <summary>
        /// 临时存储 AudioClip 到 FlacStreamReader 的映射
        /// 用于在 LoadLocalFile 中获取并注册到 AudioResourceManager
        /// </summary>
        private static readonly Dictionary<int, FlacDecoder.FlacStreamReader> _pendingStreamReaders 
            = new Dictionary<int, FlacDecoder.FlacStreamReader>();

        /// <summary>
        /// 拦截 DownloadAudioFile 方法，为 FLAC 提供流式加载
        /// </summary>
        [HarmonyPatch(typeof(GameAudioInfo), "DownloadAudioFile")]
        [HarmonyPrefix]
        static bool DownloadAudioFile_Prefix(string uri, CancellationToken ct, ref UniTask<(AudioClip, string, string)> __result)
        {
            var ext = Path.GetExtension(uri)?.ToLower();
            
            // 只拦截 FLAC 文件
            if (ext != ".flac")
            {
                return true; // 执行原方法（其他格式使用官方实现）
            }

            // 检查 Native 解码器
            if (!_nativeDecoderChecked)
            {
                _nativeDecoderAvailable = FlacDecoder.IsAvailable();
                _nativeDecoderChecked = true;
            }

            if (!_nativeDecoderAvailable)
            {
                Plugin.Log.LogError($"[FlacDecoder] Cannot stream FLAC: Native decoder unavailable");
                __result = UniTask.FromResult<(AudioClip, string, string)>((null, "", ""));
                return false;
            }

            __result = CreateStreamingFlacClip(uri, ct);
            return false; // 使用我们的流式实现
        }

        /// <summary>
        /// 创建流式 FLAC AudioClip
        /// </summary>
        private static async UniTask<(AudioClip, string, string)> CreateStreamingFlacClip(string filePath, CancellationToken ct)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Plugin.Log.LogError($"[FlacStreamLoader] File not found: {filePath}");
                    return (null, "", "");
                }

                FlacDecoder.FlacStreamReader streamReader = null;
                string title = Path.GetFileNameWithoutExtension(filePath);

                // 在后台线程打开流
                await UniTask.RunOnThreadPool(() =>
                {
                    streamReader = new FlacDecoder.FlacStreamReader(filePath);
                }, cancellationToken: ct);

                if (streamReader == null)
                {
                    Plugin.Log.LogError($"[FlacStreamLoader] Failed to open FLAC stream");
                    return (null, "", "");
                }

                var clipName = title;
                
                // 在主线程创建流式 AudioClip
                AudioClip clip = null;
                await UniTask.SwitchToMainThread();
                clip = CreateStreamingAudioClip(streamReader, clipName);

                if (clip == null)
                {
                    streamReader?.Dispose();
                    return (null, "", "");
                }

                // 暂存 streamReader，等待 LoadLocalFile 中注册到 AudioResourceManager
                _pendingStreamReaders[clip.GetInstanceID()] = streamReader;

                Plugin.Log.LogInfo($"[FlacStreamLoader] ✅ Created streaming clip: {clipName} ({streamReader.SampleRate}Hz, {streamReader.Channels}ch)");
                
                return (clip, title, "");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FlacStreamLoader] Exception: {ex}");
                return (null, "", "");
            }
        }

        /// <summary>
        /// 使用 Unity AudioClip.Create 创建流式播放
        /// </summary>
        private static AudioClip CreateStreamingAudioClip(FlacDecoder.FlacStreamReader streamReader, string clipName)
        {
            const int BUFFER_SIZE_FRAMES = 4096; // 每次读取的帧数
            float[] readBuffer = new float[BUFFER_SIZE_FRAMES * streamReader.Channels];
            object lockObj = new object();

            // 创建流式 AudioClip（stream = true）
            var clip = AudioClip.Create(
                clipName,
                (int)streamReader.TotalPcmFrames,
                streamReader.Channels,
                streamReader.SampleRate,
                stream: true, // 启用流式播放
                (float[] data) => // PCM 读取回调
                {
                    // Unity 在音频线程调用此回调
                    lock (lockObj)
                    {
                        try
                        {
                            int samplesNeeded = data.Length;
                            int samplesWritten = 0;

                            while (samplesWritten < samplesNeeded)
                            {
                                int framesToRead = Math.Min(BUFFER_SIZE_FRAMES, (samplesNeeded - samplesWritten) / streamReader.Channels);
                                long framesRead = streamReader.ReadFrames(readBuffer, (ulong)framesToRead);

                                if (framesRead <= 0)
                                {
                                    // 到达末尾或错误，填充静音
                                    Array.Clear(data, samplesWritten, samplesNeeded - samplesWritten);
                                    break;
                                }

                                int samplesToCopy = (int)framesRead * streamReader.Channels;
                                Array.Copy(readBuffer, 0, data, samplesWritten, samplesToCopy);
                                samplesWritten += samplesToCopy;
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogError($"[FlacStreamLoader] Error in PCM callback: {ex.Message}");
                            Array.Clear(data, 0, data.Length);
                        }
                    }
                },
                (int newPosition) => // PCM 位置设置回调
                {
                    // Unity 调用此回调进行 seek
                    lock (lockObj)
                    {
                        try
                        {
                            streamReader.Seek((ulong)newPosition);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogError($"[FlacStreamLoader] Error in seek callback: {ex.Message}");
                        }
                    }
                });

            return clip;
        }

        /// <summary>
        /// 扩展音频格式支持 - 可配置开关
        /// 默认关闭，不影响原游戏行为和存档
        /// </summary>
        [HarmonyPatch(typeof(GameAudioInfo), "<DownloadAudioFile>g__GetAudioType|18_0")]
        [HarmonyPrefix]
        static bool GetAudioType_Prefix(string uri, ref AudioType __result)
        {
            // 检查配置开关
            if (!UIFrameworkConfig.EnableExtendedFormats.Value)
            {
                return true; // 配置关闭，执行原方法
            }

            var ext = Path.GetExtension(uri)?.ToLower();

            // FLAC 文件已经被 DownloadAudioFile patch 拦截，不应该走到这里
            // 但为了兼容性，仍然返回 UNKNOWN
            if (ext == ".flac")
            {
                Plugin.Log.LogDebug($"[GetAudioType] FLAC file (already handled by DownloadAudioFile patch): {uri}");
                __result = AudioType.UNKNOWN;
                return false;
            }

            __result = ext switch
            {
                ".mp3" => AudioType.MPEG,
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                ".egg" => AudioType.OGGVORBIS,  // .egg是伪装的Ogg Vorbis（Beat Saber）
                ".aiff" => AudioType.AIFF,
                ".aif" => AudioType.AIFF,
                _ => AudioType.UNKNOWN
            };

            Plugin.Log.LogDebug($"Extended format: {ext} -> {__result}");

            // 阻止原方法执行
            return false;
        }

        /// <summary>
        /// 拦截 LoadLocalFile 方法，保留模块设置的元数据（Title, Credit）
        /// 模块导入的歌曲元数据已在扫描时读取，不需要在播放时再读取
        /// 原游戏会在加载音频时用 DownloadAudioFile 返回值覆盖这些字段
        /// 
        /// 注意：这是异步方法，我们需要完全替换实现
        /// </summary>
        [HarmonyPatch(typeof(GameAudioInfo), "LoadLocalFile")]
        [HarmonyPrefix]
        static bool LoadLocalFile_Prefix(GameAudioInfo __instance, string filepath, CancellationToken ct, ref UniTask<AudioClip> __result)
        {
            // 检查是否是模块导入的歌曲（Tag 包含自定义位，即位 5+）
            // 游戏原生 Tag 只使用位 0-4 (值 1-16, All=31)
            bool isModuleImported = ((ulong)__instance.Tag & ~31UL) != 0;
            
            if (!isModuleImported)
            {
                // 非模块导入的歌曲，使用原方法
                return true;
            }
            
            // 模块导入的歌曲：保留原有元数据
            string savedTitle = __instance.Title;
            string savedCredit = __instance.Credit;
            
            Plugin.Log.LogDebug($"[LoadLocalFile] Module imported song: {savedTitle}, preserving metadata");
            
            // 替换为自定义实现
            __result = LoadLocalFilePreserveMetadata(__instance, filepath, ct, savedTitle, savedCredit);
            return false;
        }

        /// <summary>
        /// 自定义 LoadLocalFile 实现，保留模块设置的元数据
        /// </summary>
        private static async UniTask<AudioClip> LoadLocalFilePreserveMetadata(
            GameAudioInfo instance, 
            string filepath, 
            CancellationToken ct,
            string savedTitle,
            string savedCredit)
        {
            instance.LocalPath = filepath;
            
            var result = await GameAudioInfo.DownloadAudioFile(filepath, ct);
            var clip = result.Item1;
            
            if (clip == null)
            {
                Plugin.Log.LogError($"{filepath} is not audio");
                return null;
            }
            
            // 如果是流式 AudioClip（FLAC），注册到资源管理器
            if (clip != null)
            {
                var clipId = clip.GetInstanceID();
                if (_pendingStreamReaders.TryGetValue(clipId, out var streamReader))
                {
                    _pendingStreamReaders.Remove(clipId);
                    AudioResourceManager.Instance.RegisterStreamingClip(instance.UUID, clip, streamReader);
                }
            }
            
            // 恢复模块设置的元数据（而不是使用 DownloadAudioFile 返回的）
            if (!string.IsNullOrEmpty(savedTitle))
            {
                instance.Title = savedTitle;
            }
            else
            {
                // 如果原来没有标题，使用 DownloadAudioFile 返回的或文件名
                var downloadedTitle = result.Item2;
                instance.Title = string.IsNullOrEmpty(downloadedTitle) 
                    ? Path.GetFileNameWithoutExtension(filepath) 
                    : downloadedTitle;
            }
            
            clip.name = instance.Title;
            
            // 恢复艺术家信息
            if (!string.IsNullOrEmpty(savedCredit))
            {
                instance.Credit = savedCredit;
            }
            // 如果原来没有 Credit，保持为空（不覆盖为 DownloadAudioFile 返回的值）
            
            Plugin.Log.LogDebug($"[LoadLocalFile] Restored metadata: Title={instance.Title}, Credit={instance.Credit}");
            
            return clip;
        }
    }
}
