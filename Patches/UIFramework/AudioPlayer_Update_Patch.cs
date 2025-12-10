using HarmonyLib;
using KanKikuchi.AudioManager;
using UnityEngine;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Models;
using Bulbul;
using NestopiSystem.DIContainers;
using ChillPatcher.UIFramework.Audio;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 修复 AudioPlayer.Update 对流媒体播放结束的错误判断
    /// 
    /// 问题：
    /// 1. AudioPlayer.Update() 通过检测 !isPlaying && time == 0 来判断歌曲结束
    /// 2. 对于流式 PCM 播放，当等待数据时（网络加载、Seek 等待缓存），
    ///    Unity 可能报告 isPlaying = false，但歌曲实际上还没播放完
    /// 3. 这会导致歌曲提前跳到下一首
    /// 
    /// 解决方案：
    /// 1. 拦截 AudioPlayer.Update()
    /// 2. 通过检查 clip.name 是否以 "pcm_stream_" 开头来快速判断是否是流媒体
    ///    这样可以避免影响语音、音效等其他 AudioPlayer
    /// 3. 对于流媒体歌曲，检查进度是否接近结尾（>= 99%）来判断是否真正结束
    /// 4. 如果进度接近结尾，主动触发下一首
    /// 5. 【新增】使用防重入锁防止同一首歌的 EOF 被重复触发导致多首歌同时播放
    /// 6. 【新增】超时保护：如果播放进度长时间不变（默认 10 秒），认为流已结束
    /// </summary>
    [HarmonyPatch]
    public static class AudioPlayer_Update_Patch
    {
        /// <summary>
        /// 防重入锁：防止 EOF 触发后、异步跳转完成前被重复触发
        /// </summary>
        private static bool _isSkippingToNext = false;
        
        /// <summary>
        /// 记录已经触发过 EOF 的 stream ID，防止同一个 stream 被多次触发
        /// </summary>
        private static string _lastEofTriggeredStreamId = null;
        
        /// <summary>
        /// 超时保护：上次播放位置变化的时间
        /// </summary>
        private static float _lastProgressChangeTime = 0f;
        private static float _lastKnownProgress = 0f;
        private const float STALL_TIMEOUT_SECONDS = 10f; // 10秒不变化则认为结束
        
        /// <summary>
        /// 重置 EOF 追踪状态（在新歌曲开始播放时调用）
        /// </summary>
        public static void ResetEofTracking()
        {
            _lastEofTriggeredStreamId = null;
            _isSkippingToNext = false;
            _lastProgressChangeTime = Time.time;
            _lastKnownProgress = 0f;
        }
        
        [HarmonyPatch(typeof(AudioPlayer), nameof(AudioPlayer.Update))]
        [HarmonyPrefix]
        public static bool Update_Prefix(AudioPlayer __instance)
        {
            // 只在 Playing 状态时进行特殊处理
            if (__instance.CurrentState != AudioPlayer.State.Playing)
            {
                return true; // 使用原始逻辑
            }

            var audioSource = __instance.AudioSource;
            if (audioSource == null || audioSource.clip == null)
            {
                return true; // 使用原始逻辑
            }

            // 快速检查：是否是流媒体相关的 AudioClip
            // 流媒体 clip 的名称以 "pcm_stream_" 开头
            var clipName = audioSource.clip.name;
            if (string.IsNullOrEmpty(clipName) || !clipName.StartsWith("pcm_stream_"))
            {
                // 不是流媒体音频，使用原始逻辑
                // 这样就不会影响语音、音效等其他 AudioPlayer
                return true;
            }

            // 是流媒体音乐，进行特殊处理
            var musicService = ProjectLifetimeScope.Resolve<MusicService>();
            if (musicService == null)
            {
                return true;
            }

            var playingMusic = musicService.PlayingMusic;
            if (playingMusic == null || string.IsNullOrEmpty(playingMusic.UUID))
            {
                return true;
            }

            var music = MusicRegistry.Instance?.GetMusic(playingMusic.UUID);
            if (music == null || music.SourceType != MusicSourceType.Stream)
            {
                // 不是流媒体，使用原始逻辑
                return true;
            }

            // ===== 流媒体特殊处理 =====
            
            // 获取当前 clip 对应的 PCM 读取器
            var currentClip = audioSource.clip;
            var reader = AudioResourceManager.Instance?.GetPcmStreamReader(currentClip);
            
            // 如果没找到，回退到 ActivePcmReader（兼容性）
            if (reader == null)
            {
                reader = MusicService_SetProgress_Patch.ActivePcmReader;
            }

            // 获取当前播放进度
            float currentProgress = audioSource.time;
            if (reader != null)
            {
                // 优先使用 reader 的帧位置，因为 audioSource.time 可能不准
                currentProgress = (float)reader.CurrentFrame / reader.Info.SampleRate;
            }
            
            // 【超时保护】只在播放超过原始时长后才启用检查
            // 这样可以避免正常播放过程中误触发
            float originalDuration = reader?.Info?.Duration ?? 0f;
            bool isPassedOriginalDuration = originalDuration > 0 && currentProgress >= originalDuration - 1f; // 提前1秒开始检查
            
            if (Mathf.Abs(currentProgress - _lastKnownProgress) > 0.1f)
            {
                // 进度有变化，重置计时器
                _lastKnownProgress = currentProgress;
                _lastProgressChangeTime = Time.time;
            }
            else if (isPassedOriginalDuration)
            {
                // 进度没变化，且已经超过原始时长，检查是否超时
                float stallDuration = Time.time - _lastProgressChangeTime;
                if (stallDuration > STALL_TIMEOUT_SECONDS)
                {
                    // 超时了！进度卡住超过 10 秒
                    if (!_isSkippingToNext && clipName != _lastEofTriggeredStreamId)
                    {
                        Plugin.Log.LogWarning($"[AudioPlayer_Patch] Playback stalled for {stallDuration:F1}s at {currentProgress:F1}s (original duration: {originalDuration:F1}s), forcing next song (Go EOF timeout)");
                        
                        _lastEofTriggeredStreamId = clipName;
                        _isSkippingToNext = true;
                        
                        try
                        {
                            musicService.SkipCurrentMusic(MusicChangeKind.Auto);
                        }
                        catch (System.Exception ex)
                        {
                            Plugin.Log.LogError($"[AudioPlayer_Patch] Error triggering next song on stall: {ex.Message}");
                        }
                        
                        return false;
                    }
                }
            }

            // 【重要】始终检查 Go 端的 EOF 信号
            // 因为我们使用超长余量策略，Unity 永远不会认为歌曲到达结尾
            // 必须主动检查 Go 返回的 EOF 来触发下一首
            if (reader != null)
            {
                // 如果有待定的 Seek，不触发 EOF
                if (reader.HasPendingSeek)
                {
                    return true; // 继续使用原始逻辑
                }

                // 检查是否到达 EOF
                if (reader.IsEndOfStream)
                {
                    // 【防重入检查】防止同一个 stream 被多次触发
                    if (_isSkippingToNext)
                    {
                        return false; // 阻止原始逻辑，等待跳转完成
                    }
                    
                    if (clipName == _lastEofTriggeredStreamId)
                    {
                        return false; // 已经触发过，阻止原始逻辑
                    }
                    
                    // 标记防重入
                    _lastEofTriggeredStreamId = clipName;
                    _isSkippingToNext = true;
                    
                    Plugin.Log.LogInfo($"[AudioPlayer_Patch] PCM stream EOF confirmed, triggering next song");
                    
                    // 主动触发下一首
                    try
                    {
                        musicService.SkipCurrentMusic(MusicChangeKind.Auto);
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogError($"[AudioPlayer_Patch] Error triggering next song: {ex.Message}");
                    }
                    
                    return false; // 阻止原始逻辑
                }
            }
            
            // 原始的结束判断条件：Unity 认为播放结束
            // 如果使用 30 分钟余量，这意味着余量也用完了
            bool originalEndCondition = !audioSource.isPlaying && Mathf.Approximately(audioSource.time, 0f);

            if (originalEndCondition)
            {
                // 【重要】30 分钟余量用完了，直接跳转，不再等待 Go EOF
                // 这是最后的兜底，无论 Go 状态如何
                if (!_isSkippingToNext && clipName != _lastEofTriggeredStreamId)
                {
                    Plugin.Log.LogWarning($"[AudioPlayer_Patch] Unity playback finished (30min margin exhausted), forcing next song without waiting for Go EOF");
                    
                    _lastEofTriggeredStreamId = clipName;
                    _isSkippingToNext = true;
                    
                    try
                    {
                        musicService.SkipCurrentMusic(MusicChangeKind.Auto);
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogError($"[AudioPlayer_Patch] Error triggering next song on margin exhausted: {ex.Message}");
                    }
                    
                    return false;
                }
                
                // 已经在跳转中，阻止原始逻辑
                return false;
            }

            // 使用原始逻辑
            return true;
        }
    }
}
