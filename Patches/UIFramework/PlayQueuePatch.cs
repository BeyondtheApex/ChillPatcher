using Bulbul;
using ChillPatcher.UIFramework;
using ChillPatcher.UIFramework.Audio;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.ModuleSystem.Services;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using KanKikuchi.AudioManager;
using NestopiSystem;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 播放队列系统的 Harmony Patches
    /// 
    /// 拦截 MusicService 的播放流程，使其使用 PlayQueueManager
    /// </summary>
    [HarmonyPatch]
    public static class PlayQueuePatch
    {
        /// <summary>
        /// 是否启用队列系统
        /// </summary>
        public static bool IsQueueSystemEnabled { get; set; } = true;
        
        /// <summary>
        /// 是否跳过启动时的默认播放（当有保存的播放状态时使用）
        /// 这样可以避免播放状态恢复和默认播放冲突导致双播放
        /// </summary>
        public static bool SkipStartupDefaultPlay { get; set; } = false;
        
        /// <summary>
        /// 当前加载任务的取消令牌源
        /// 用于在切换歌曲时取消之前的加载任务，防止多首歌同时播放
        /// </summary>
        private static CancellationTokenSource _currentLoadCts;
        private static readonly object _ctsLock = new object();
        
        /// <summary>
        /// 获取用于队列填充的播放列表（使用显示顺序）
        /// </summary>
        /// <param name="musicService">MusicService 实例</param>
        /// <returns>显示顺序的歌曲列表，如果不可用则返回 CurrentPlayList</returns>
        private static IReadOnlyList<GameAudioInfo> GetPlaylistForQueue(MusicService musicService)
        {
            // 优先使用显示顺序列表
            var musicManager = ChillUIFramework.Music as MusicUIManager;
            if (musicManager != null)
            {
                var displayOrder = musicManager.DisplayOrderSongs;
                if (displayOrder != null && displayOrder.Count > 0)
                {
                    Plugin.Log.LogDebug($"[PlayQueuePatch] Using display order playlist: {displayOrder.Count} songs");
                    return displayOrder;
                }
            }
            
            // 回退到 CurrentPlayList
            Plugin.Log.LogDebug($"[PlayQueuePatch] Using CurrentPlayList: {musicService.CurrentPlayList.Count} songs");
            return musicService.CurrentPlayList;
        }
        
        /// <summary>
        /// 获取新的 CancellationToken 并取消之前的加载任务
        /// </summary>
        /// <param name="timeoutSeconds">超时时间（秒）</param>
        /// <returns>新的 CancellationToken</returns>
        private static CancellationToken GetNewLoadCancellationToken(int timeoutSeconds = 30)
        {
            lock (_ctsLock)
            {
                // 取消之前的加载任务
                if (_currentLoadCts != null)
                {
                    Plugin.Log.LogInfo("[PlayQueuePatch] Cancelling previous load task");
                    try
                    {
                        _currentLoadCts.Cancel();
                        _currentLoadCts.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 已经 disposed，忽略
                    }
                }
                
                // 创建新的 CancellationTokenSource
                _currentLoadCts = new CancellationTokenSource();
                _currentLoadCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                return _currentLoadCts.Token;
            }
        }
        
        /// <summary>
        /// 检查当前加载是否已被取消
        /// </summary>
        private static bool IsCurrentLoadCancelled(CancellationToken token)
        {
            return token.IsCancellationRequested;
        }
        
        #region SkipCurrentMusic Patch
        
        /// <summary>
        /// 拦截 SkipCurrentMusic，使用队列系统的下一首
        /// </summary>
        [HarmonyPatch(typeof(MusicService), nameof(MusicService.SkipCurrentMusic))]
        [HarmonyPrefix]
        public static bool SkipCurrentMusic_Prefix(MusicService __instance, MusicChangeKind kind, ref UniTask<bool> __result)
        {
            if (!IsQueueSystemEnabled)
                return true;  // 使用原始逻辑
            
            // 使用队列系统
            __result = SkipWithQueueAsync(__instance, kind);
            return false;  // 跳过原始方法
        }
        
        private static async UniTask<bool> SkipWithQueueAsync(MusicService musicService, MusicChangeKind kind)
        {
            var queueManager = PlayQueueManager.Instance;
            var currentPlaylist = GetPlaylistForQueue(musicService);
            Func<GameAudioInfo, bool> isExcludedFunc = audio => musicService.IsContainsExcludedFromPlaylist(audio);
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] SkipWithQueueAsync: IsInHistoryMode={queueManager.IsInHistoryMode}, IsInExtendedMode={queueManager.IsInExtendedMode}, HistoryPosition={queueManager.HistoryPosition}, ExtendedSteps={queueManager.ExtendedSteps}");
            
            // 如果在扩展模式中，先消耗扩展步数
            if (queueManager.IsInExtendedMode)
            {
                Plugin.Log.LogInfo("[PlayQueuePatch] In extended mode, trying GoNextExtended");
                var nextExtended = queueManager.GoNextExtended(currentPlaylist, queueManager.CurrentPlaying, isExcludedFunc);
                Plugin.Log.LogInfo($"[PlayQueuePatch] GoNextExtended returned: {nextExtended?.AudioClipName ?? "null"}");
                
                if (nextExtended != null)
                {
                    // 从扩展模式获取到了下一首
                    queueManager.SetCurrentPlaying(nextExtended, addToHistory: false);
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Go next in extended mode: {nextExtended.AudioClipName}");
                    return await PlayAudioAsync(musicService, nextExtended, kind);
                }
                // 扩展步数用完了，回到历史最早记录，继续从历史前进
                Plugin.Log.LogInfo("[PlayQueuePatch] Extended steps exhausted, continue from history");
            }
            
            // 如果在历史回溯模式中，先尝试从历史前进
            if (queueManager.IsInHistoryMode)
            {
                Plugin.Log.LogInfo("[PlayQueuePatch] In history mode, trying GoNext");
                var nextInHistory = queueManager.GoNext();
                Plugin.Log.LogInfo($"[PlayQueuePatch] GoNext returned: {nextInHistory?.AudioClipName ?? "null"}");
                
                if (nextInHistory != null)
                {
                    // 从历史获取到了下一首
                    queueManager.SetCurrentPlaying(nextInHistory, addToHistory: false);
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Go next in history: {nextInHistory.AudioClipName}");
                    return await PlayAudioAsync(musicService, nextInHistory, kind);
                }
                // 历史用完了，继续使用队列
                Plugin.Log.LogInfo("[PlayQueuePatch] History exhausted, using queue");
            }
            
            // 获取当前播放列表和随机设置
            bool isShuffle = musicService.IsShuffle;
            
            // 从队列获取下一首（使用异步版本支持增长列表）
            var nextAudio = await queueManager.AdvanceToNextAsync(
                currentPlaylist, 
                isShuffle, 
                isExcludedFunc,
                () => GetPlaylistForQueue(musicService)  // 获取更新后的播放列表
            );
            
            if (nextAudio == null)
            {
                Plugin.Log.LogWarning("[PlayQueuePatch] No next audio available");
                return false;
            }
            
            // 播放
            return await PlayAudioAsync(musicService, nextAudio, kind);
        }
        
        #endregion
        
        #region PlayNextMusic Patch
        
        /// <summary>
        /// 拦截 PlayNextMusic
        /// nextCount > 0: 下一首（使用队列）
        /// nextCount < 0: 上一首（清除队列当前项，从歌单取上一首）
        /// </summary>
        [HarmonyPatch(typeof(MusicService), nameof(MusicService.PlayNextMusic))]
        [HarmonyPrefix]
        public static bool PlayNextMusic_Prefix(MusicService __instance, int nextCount, MusicChangeKind changeKind, ref UniTask<bool> __result)
        {
            // 检查是否应该跳过启动时的默认播放
            if (SkipStartupDefaultPlay && nextCount == 0 && changeKind == MusicChangeKind.Auto)
            {
                Plugin.Log.LogInfo("[PlayQueuePatch] Skipping startup default PlayNextMusic(0) - will restore saved playback state instead");
                SkipStartupDefaultPlay = false; // 只跳过一次
                __result = UniTask.FromResult(true);
                return false;
            }
            
            if (!IsQueueSystemEnabled)
                return true;
            
            if (nextCount >= 0)
            {
                // 下一首：使用队列系统
                __result = PlayNextWithQueueAsync(__instance, nextCount, changeKind);
                return false;
            }
            
            // 上一首：清除队列当前项，从歌单取上一首
            __result = PlayPrevWithQueueAsync(__instance, changeKind);
            return false;
        }
        
        /// <summary>
        /// 上一首播放逻辑（使用播放历史记录，历史到头后使用扩展模式）
        /// </summary>
        private static async UniTask<bool> PlayPrevWithQueueAsync(MusicService musicService, MusicChangeKind changeKind)
        {
            var queueManager = PlayQueueManager.Instance;
            var currentPlaylist = GetPlaylistForQueue(musicService);
            Func<GameAudioInfo, bool> isExcludedFunc = audio => musicService.IsContainsExcludedFromPlaylist(audio);
            
            GameAudioInfo prevAudio;
            
            // 如果已经在扩展模式中，继续使用扩展模式
            if (queueManager.IsInExtendedMode)
            {
                prevAudio = queueManager.GoPreviousExtended(currentPlaylist, queueManager.CurrentPlaying, isExcludedFunc);
                if (prevAudio != null)
                {
                    // 设置为当前播放（不添加到历史记录）
                    queueManager.SetCurrentPlaying(prevAudio, addToHistory: false);
                    Plugin.Log.LogInfo($"[PlayQueuePatch] PlayPrev from extended mode: {prevAudio.AudioClipName}");
                    return await PlayAudioAsync(musicService, prevAudio, changeKind);
                }
                // 扩展模式也没有了（不太可能发生），保持当前
                Plugin.Log.LogWarning("[PlayQueuePatch] Extended mode has no more previous songs");
                return false;
            }
            
            // 尝试从历史记录回退
            prevAudio = queueManager.GoPrevious();
            
            if (prevAudio == null)
            {
                // 历史到头了，尝试使用扩展模式
                Plugin.Log.LogInfo("[PlayQueuePatch] History exhausted, trying extended mode");
                prevAudio = queueManager.GoPreviousExtended(currentPlaylist, queueManager.CurrentPlaying, isExcludedFunc);
                
                if (prevAudio == null)
                {
                    Plugin.Log.LogInfo("[PlayQueuePatch] No previous song available (history and extended mode exhausted)");
                    return false;
                }
                
                // 设置为当前播放（不添加到历史记录）
                queueManager.SetCurrentPlaying(prevAudio, addToHistory: false);
                Plugin.Log.LogInfo($"[PlayQueuePatch] PlayPrev from extended mode (first): {prevAudio.AudioClipName}");
                return await PlayAudioAsync(musicService, prevAudio, changeKind);
            }
            
            // 设置为当前播放（不添加到历史记录）
            queueManager.SetCurrentPlaying(prevAudio, addToHistory: false);
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] PlayPrev from history: {prevAudio.AudioClipName}");
            
            return await PlayAudioAsync(musicService, prevAudio, changeKind);
        }
        
        private static async UniTask<bool> PlayNextWithQueueAsync(MusicService musicService, int nextCount, MusicChangeKind changeKind)
        {
            var queueManager = PlayQueueManager.Instance;
            
            // 获取当前播放列表和设置（使用显示顺序）
            var currentPlaylist = GetPlaylistForQueue(musicService);
            bool isShuffle = musicService.IsShuffle;
            
            // 使用 MusicService.IsContainsExcludedFromPlaylist 检查排除
            Func<GameAudioInfo, bool> isExcludedFunc = audio => musicService.IsContainsExcludedFromPlaylist(audio);
            
            GameAudioInfo nextAudio = null;
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] PlayNextWithQueueAsync: nextCount={nextCount}, IsInHistoryMode={queueManager.IsInHistoryMode}, IsInExtendedMode={queueManager.IsInExtendedMode}, HistoryPosition={queueManager.HistoryPosition}, ExtendedSteps={queueManager.ExtendedSteps}");
            
            // 如果在扩展模式中，先消耗扩展步数
            if (queueManager.IsInExtendedMode && nextCount > 0)
            {
                Plugin.Log.LogInfo("[PlayQueuePatch] In extended mode, trying GoNextExtended");
                for (int i = 0; i < nextCount; i++)
                {
                    nextAudio = queueManager.GoNextExtended(currentPlaylist, queueManager.CurrentPlaying, isExcludedFunc);
                    Plugin.Log.LogInfo($"[PlayQueuePatch] GoNextExtended returned: {nextAudio?.AudioClipName ?? "null"}");
                    if (nextAudio == null)
                    {
                        // 扩展步数用完了，回到历史最早记录
                        Plugin.Log.LogInfo("[PlayQueuePatch] Extended steps exhausted, returning to oldest history");
                        // 历史最早记录就是 _history[_historyPosition]
                        if (queueManager.IsInHistoryMode && queueManager.HistoryPosition >= 0)
                        {
                            // 保持在历史最早位置，不做操作
                            nextAudio = queueManager.CurrentPlaying;
                            if (nextAudio != null)
                            {
                                Plugin.Log.LogInfo($"[PlayQueuePatch] Staying at oldest history: {nextAudio.AudioClipName}");
                                return await PlayAudioAsync(musicService, nextAudio, changeKind);
                            }
                        }
                        break;
                    }
                }
                
                if (nextAudio != null)
                {
                    // 从扩展模式获取到了下一首
                    queueManager.SetCurrentPlaying(nextAudio, addToHistory: false);
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Go next in extended mode: {nextAudio.AudioClipName}");
                    return await PlayAudioAsync(musicService, nextAudio, changeKind);
                }
            }
            
            // 如果在历史回溯模式中，先尝试从历史前进
            if (queueManager.IsInHistoryMode && nextCount > 0)
            {
                Plugin.Log.LogInfo("[PlayQueuePatch] In history mode, trying GoNext");
                for (int i = 0; i < nextCount; i++)
                {
                    nextAudio = queueManager.GoNext();
                    Plugin.Log.LogInfo($"[PlayQueuePatch] GoNext returned: {nextAudio?.AudioClipName ?? "null"}");
                    if (nextAudio == null)
                    {
                        // 历史用完了，继续使用队列
                        Plugin.Log.LogInfo("[PlayQueuePatch] History exhausted, using queue");
                        break;
                    }
                }
                
                if (nextAudio != null)
                {
                    // 从历史获取到了下一首
                    queueManager.SetCurrentPlaying(nextAudio, addToHistory: false);
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Go next in history: {nextAudio.AudioClipName}");
                    return await PlayAudioAsync(musicService, nextAudio, changeKind);
                }
            }
            
            if (nextCount == 0)
            {
                // 播放当前（队列第一首，或者从播放列表获取）
                nextAudio = queueManager.CurrentPlaying;
                if (nextAudio == null)
                {
                    nextAudio = await queueManager.AdvanceToNextAsync(
                        currentPlaylist, 
                        isShuffle, 
                        isExcludedFunc,
                        () => GetPlaylistForQueue(musicService)
                    );
                }
            }
            else
            {
                // 跳过 nextCount 首（使用队列系统异步版本）
                for (int i = 0; i < nextCount; i++)
                {
                    nextAudio = await queueManager.AdvanceToNextAsync(
                        currentPlaylist, 
                        isShuffle, 
                        isExcludedFunc,
                        () => GetPlaylistForQueue(musicService)
                    );
                    if (nextAudio == null) break;
                }
            }
            
            if (nextAudio == null)
            {
                Plugin.Log.LogWarning("[PlayQueuePatch] No audio available");
                return false;
            }
            
            return await PlayAudioAsync(musicService, nextAudio, changeKind);
        }
        
        #endregion
        
        #region PlayMusicInPlaylist Patch
        
        /// <summary>
        /// 拦截点击播放列表播放
        /// 将选中的歌曲设为当前播放，并更新播放指针
        /// </summary>
        [HarmonyPatch(typeof(MusicService), nameof(MusicService.PlayMusicInPlaylist))]
        [HarmonyPrefix]
        public static bool PlayMusicInPlaylist_Prefix(MusicService __instance, int index, ref bool __result)
        {
            if (!IsQueueSystemEnabled)
                return true;
            
            // 启动异步播放
            PlayFromPlaylistWithQueueAsync(__instance, index).Forget();
            __result = true;  // 假设成功，实际播放在异步中完成
            return false;
        }
        
        /// <summary>
        /// 异步从播放列表播放，支持模块提供的音频（AudioClip 可能为 null）
        /// </summary>
        private static async UniTaskVoid PlayFromPlaylistWithQueueAsync(MusicService musicService, int index)
        {
            Plugin.Log.LogInfo($"[PlayQueuePatch] PlayFromPlaylistWithQueueAsync START: index={index}");
            
            var currentPlaylist = musicService.CurrentPlayList;
            
            if (currentPlaylist.Count == 0 || index < 0 || index >= currentPlaylist.Count)
            {
                Plugin.Log.LogWarning($"[PlayQueuePatch] Invalid playlist index: {index}");
                return;
            }
            
            var audio = currentPlaylist[index];
            
            if (audio == null)
            {
                Plugin.Log.LogError($"[PlayQueuePatch] Audio is null at index {index}");
                return;
            }
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] Got audio: {audio.Title}, AudioClip={(audio.AudioClip != null ? "exists" : "null")}");
            
            // 使用智能加载 - 自动判断音源类型
            AudioClip audioClip = audio.AudioClip;
            if (audioClip == null)
            {
                // 设置加载标志，防止 UpdateFacility 在加载期间调用 PauseMusic
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = true;
                
                // 立即停止当前播放，减少用户感知延迟
                var playingMusic = musicService.PlayingMusic;
                if (playingMusic != null && playingMusic.AudioClip != null)
                {
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Stopping current: {playingMusic.AudioClipName}");
                    SingletonMonoBehaviour<MusicManager>.Instance.Stop(playingMusic.AudioClip);
                }
                
                // 立即切换 UI 显示到新歌曲，进度条会停在 0 等待加载
                SetPlayingMusic(musicService, audio);
                MusicService_GetProgress_Patch.ResetProgress();  // 重置进度为 0
                InvokeOnChangeMusic(musicService, MusicChangeKind.Manual);
                InvokeOnPlayMusic(musicService, audio);
                
                Plugin.Log.LogInfo($"[PlayQueuePatch] Smart loading audio: {audio.Title}");
                
                // 获取新的 CancellationToken，会自动取消之前的加载任务
                var loadToken = GetNewLoadCancellationToken(30);
                
                try
                {
                    audioClip = await StreamingAudioLoader.SmartLoadAsync(audio, loadToken);
                    
                    // 检查是否在加载期间被取消（用户切换了歌曲）
                    if (loadToken.IsCancellationRequested)
                    {
                        Plugin.Log.LogInfo($"[PlayQueuePatch] Load cancelled (user switched song): {audio.Title}");
                        FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                        return;
                    }
                    
                    Plugin.Log.LogInfo($"[PlayQueuePatch] SmartLoad returned: {(audioClip != null ? audioClip.name : "null")}");
                }
                catch (OperationCanceledException)
                {
                    Plugin.Log.LogWarning($"[PlayQueuePatch] Audio load cancelled or timeout: {audio.Title}");
                    FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                    // 只有超时情况下才自动播放下一首，用户切换时不需要
                    // 通过检查当前播放歌曲来判断
                    if (musicService.PlayingMusic?.UUID == audio.UUID)
                    {
                        await musicService.PlayNextMusic(1, MusicChangeKind.Auto);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[PlayQueuePatch] Audio load failed: {ex.Message}");
                    FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                    await musicService.PlayNextMusic(1, MusicChangeKind.Auto);
                    return;
                }
            }
            
            if (audioClip == null)
            {
                Plugin.Log.LogError($"[PlayQueuePatch] AudioClip is null for: {audio.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                await musicService.PlayNextMusic(1, MusicChangeKind.Auto);
                return;
            }
            
            // 再次检查是否被取消（加载可能成功但用户已切换）
            // 检查当前 UI 显示的歌曲是否还是我们正在加载的
            if (musicService.PlayingMusic?.UUID != audio.UUID)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] User switched song during load, aborting play: {audio.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                return;
            }
            
            // 更新队列管理器
            var queueManager = PlayQueueManager.Instance;
            
            // 检查是否点击的是增长列表的最后一首歌曲
            // 如果是，先触发加载更多，确保指针不会回到第一首
            int newPosition = (index + 1) % currentPlaylist.Count;
            bool isLastSong = (index == currentPlaylist.Count - 1);
            
            if (isLastSong && MusicUI_VirtualScroll_Patch.IsInGrowableListMode())
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] 点击增长列表最后一首，先触发加载更多...");
                
                // 异步加载更多
                var loadedCount = await MusicUI_VirtualScroll_Patch.TriggerLoadMoreAsync();
                
                if (loadedCount > 0)
                {
                    // 加载成功，重新获取播放列表（可能已更新）
                    var updatedPlaylist = musicService.CurrentPlayList;
                    
                    // 如果列表已增长，新位置应该是原来的 index + 1
                    // 而不是回到 0
                    if (updatedPlaylist.Count > currentPlaylist.Count)
                    {
                        newPosition = index + 1;  // 不取模，直接指向新加载的第一首
                        Plugin.Log.LogInfo($"[PlayQueuePatch] 列表已增长: {currentPlaylist.Count} -> {updatedPlaylist.Count}, 新位置: {newPosition}");
                    }
                }
            }
            
            queueManager.SetCurrentPlaying(audio, updatePosition: true, newPosition: newPosition);
            
            // 停止当前播放（如果在加载时还没停止）
            // 注意：如果 AudioClip 已存在则还没停止，需要在这里停止
            if (audio.AudioClip != null)
            {
                var playingMusic = musicService.PlayingMusic;
                if (playingMusic != null && playingMusic.AudioClip != null)
                {
                    SingletonMonoBehaviour<MusicManager>.Instance.Stop(playingMusic.AudioClip);
                }
            }
            
            // 加载音频数据（跳过流式 AudioClip）
            if (audioClip.loadState == AudioDataLoadState.Unloaded && !IsStreamingClip(audioClip))
            {
                audioClip.LoadAudioData();
            }
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] About to play from playlist: {audio.Title}, loadState={audioClip.loadState}");
            
            SingletonMonoBehaviour<MusicManager>.Instance.Play(
                audioClip, 1f, 0f, 1f,
                musicService.IsRepeatOneMusic,
                true, "",
                () => musicService.SkipCurrentMusic(MusicChangeKind.Auto).Forget<bool>()
            );
            
            // 如果是本地音频（AudioClip 已存在），需要在这里更新状态和触发事件
            // 流媒体的话已经在加载前更新过了
            if (audio.AudioClip != null)
            {
                SetPlayingMusic(musicService, audio);
                InvokeOnChangeMusic(musicService, MusicChangeKind.Manual);
                InvokeOnPlayMusic(musicService, audio);
            }
            
            // 清除加载标志
            FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] Now playing: {audio.Title}");
        }
        
        #endregion
        
        #region PlayArugumentMusic Patch
        
        /// <summary>
        /// 拦截直接播放指定歌曲
        /// </summary>
        [HarmonyPatch(typeof(MusicService), nameof(MusicService.PlayArugumentMusic))]
        [HarmonyPrefix]
        public static bool PlayArugumentMusic_Prefix(MusicService __instance, GameAudioInfo audioInfo, MusicChangeKind changeKind)
        {
            if (!IsQueueSystemEnabled)
                return true;
            
            if (audioInfo == null)
            {
                Plugin.Log.LogError("[PlayQueuePatch] audioInfo is null in PlayArugumentMusic");
                return false;
            }
            
            var queueManager = PlayQueueManager.Instance;
            
            // 检查是否在队列视图中点击了队列中的项目
            if (MusicUI_VirtualScroll_Patch.IsShowingQueue)
            {
                int queueIndex = queueManager.IndexOf(audioInfo);
                if (queueIndex >= 0)
                {
                    // 在队列中找到了这个项目
                    if (queueIndex == 0)
                    {
                        // 已经是正在播放的，不需要处理
                        Plugin.Log.LogInfo("[PlayQueuePatch] Already playing this song");
                        return false;
                    }
                    else
                    {
                        // 把点击的项目移动到下一首位置，然后跳过当前播放
                        // 这样逻辑和"下一首"保持一致
                        
                        // 1. 移除点击的项目（从原位置）
                        queueManager.RemoveAt(queueIndex);
                        
                        // 2. 在位置 1 插入（下一首位置）
                        queueManager.Insert(1, audioInfo);
                        
                        // 3. 移除位置 0（当前播放的）→ 点击的项目自动变成位置 0
                        queueManager.RemoveAt(0);
                        
                        // 4. 添加到历史
                        queueManager.AddToHistory(audioInfo);
                        
                        // 5. 触发队列变更事件
                        queueManager.NotifyCurrentChanged(audioInfo);
                        
                        Plugin.Log.LogInfo($"[PlayQueuePatch] Moved queue item to play: {audioInfo.AudioClipName}");
                        
                        // 队列处理完成，后续让原始方法处理实际播放
                        // 不需要再调用 SetCurrentPlaying，因为 audioInfo 已经在队列第一位了
                    }
                }
            }
            else
            {
                // 非队列视图：普通播放更新队列管理器
                // 注意：增长列表相关的处理移到异步方法中
                queueManager.SetCurrentPlaying(audioInfo);
                
                // 检查是否是增长列表的最后一首，如果是则需要异步处理
                var currentPlaylist = __instance.CurrentPlayList;
                int index = FindAudioIndex(audioInfo, currentPlaylist);
                bool isLastSong = index >= 0 && index == currentPlaylist.Count - 1;
                bool isGrowableMode = MusicUI_VirtualScroll_Patch.IsInGrowableListMode();
                
                if (isLastSong && isGrowableMode)
                {
                    // 增长列表最后一首，需要异步处理
                    Plugin.Log.LogInfo($"[PlayQueuePatch] 点击增长列表最后一首: {audioInfo.AudioClipName}, 启动异步处理");
                    PlayArugumentMusicWithGrowableAsync(__instance, audioInfo, changeKind, index).Forget();
                    return false;  // 跳过原方法
                }
                
                // 普通情况：直接设置指针
                queueManager.SetPlaylistPositionByAudio(audioInfo, currentPlaylist);
            }
            
            // 检查是否需要异步加载
            var audioClip = audioInfo.AudioClip;
            bool needsAsyncLoad = audioClip == null && (
                // 本地文件
                (audioInfo.PathType == AudioMode.LocalPc && !string.IsNullOrEmpty(audioInfo.LocalPath)) ||
                // 流媒体源
                StreamingAudioLoader.IsStreamingSource(audioInfo)
            );
            
            if (needsAsyncLoad)
            {
                // 需要异步加载，启动异步播放
                PlayArugumentMusicAsync(__instance, audioInfo, changeKind).Forget();
                return false;  // 跳过原方法
            }
            
            return true;  // 让原始方法处理（AudioClip 已加载）
        }
        
        /// <summary>
        /// 在播放列表中查找歌曲索引
        /// </summary>
        private static int FindAudioIndex(GameAudioInfo audio, IReadOnlyList<GameAudioInfo> playlist)
        {
            if (audio == null || playlist == null) return -1;
            
            for (int i = 0; i < playlist.Count; i++)
            {
                if (playlist[i].UUID == audio.UUID)
                    return i;
            }
            return -1;
        }
        
        /// <summary>
        /// 异步播放增长列表中的歌曲（先触发加载更多）
        /// </summary>
        private static async UniTaskVoid PlayArugumentMusicWithGrowableAsync(
            MusicService musicService, 
            GameAudioInfo audioInfo, 
            MusicChangeKind changeKind,
            int originalIndex)
        {
            var queueManager = PlayQueueManager.Instance;
            var originalCount = musicService.CurrentPlayList.Count;
            
            // 先触发加载更多
            Plugin.Log.LogInfo($"[PlayQueuePatch] 触发增长列表加载更多...");
            var loadedCount = await MusicUI_VirtualScroll_Patch.TriggerLoadMoreAsync();
            
            // 设置播放指针
            if (loadedCount > 0)
            {
                // 加载成功，新位置指向新加载的第一首
                int newPosition = originalIndex + 1;
                queueManager.SetPlaylistPosition(newPosition);
                Plugin.Log.LogInfo($"[PlayQueuePatch] 列表已增长: {originalCount} -> {musicService.CurrentPlayList.Count}, 新位置: {newPosition}");
            }
            else
            {
                // 加载失败，回到开头
                queueManager.SetPlaylistPositionByAudio(audioInfo, musicService.CurrentPlayList);
            }
            
            // 继续播放
            await PlayArugumentMusicCoreAsync(musicService, audioInfo, changeKind);
        }
        
        /// <summary>
        /// 异步播放指定歌曲
        /// </summary>
        private static async UniTaskVoid PlayArugumentMusicAsync(MusicService musicService, GameAudioInfo audioInfo, MusicChangeKind changeKind)
        {
            await PlayArugumentMusicCoreAsync(musicService, audioInfo, changeKind);
        }
        
        /// <summary>
        /// 异步播放的核心逻辑
        /// </summary>
        private static async UniTask PlayArugumentMusicCoreAsync(MusicService musicService, GameAudioInfo audioInfo, MusicChangeKind changeKind)
        {
            // 设置加载标志，防止 UpdateFacility 在加载期间调用 PauseMusic
            FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = true;
            
            // 立即停止当前播放
            var playingMusic = musicService.PlayingMusic;
            if (playingMusic != null && playingMusic.AudioClip != null)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Stopping current: {playingMusic.AudioClipName}");
                SingletonMonoBehaviour<MusicManager>.Instance.Stop(playingMusic.AudioClip);
            }
            
            // 立即切换 UI 显示到新歌曲，进度条会停在 0 等待加载
            SetPlayingMusic(musicService, audioInfo);
            MusicService_GetProgress_Patch.ResetProgress();  // 重置进度为 0
            InvokeOnChangeMusic(musicService, changeKind);
            InvokeOnPlayMusic(musicService, audioInfo);
            
            // 获取新的 CancellationToken，会自动取消之前的加载任务
            var loadToken = GetNewLoadCancellationToken(30);
            
            AudioClip audioClip;
            try
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Smart async loading: {audioInfo.Title}");
                audioClip = await StreamingAudioLoader.SmartLoadAsync(audioInfo, loadToken);
                
                // 检查是否在加载期间被取消（用户切换了歌曲）
                if (loadToken.IsCancellationRequested)
                {
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Load cancelled (user switched song): {audioInfo.Title}");
                    FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                Plugin.Log.LogWarning($"[PlayQueuePatch] Audio load cancelled or timeout: {audioInfo.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                // 只有超时情况下才自动播放下一首
                if (musicService.PlayingMusic?.UUID == audioInfo.UUID)
                {
                    await musicService.PlayNextMusic(1, MusicChangeKind.Auto);
                }
                return;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PlayQueuePatch] Audio load failed: {ex.Message}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                await musicService.PlayNextMusic(1, MusicChangeKind.Auto);
                return;
            }
            
            if (audioClip == null)
            {
                Plugin.Log.LogError($"[PlayQueuePatch] AudioClip is null for: {audioInfo.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                await musicService.PlayNextMusic(1, MusicChangeKind.Auto);
                return;
            }
            
            // 再次检查是否被取消（加载可能成功但用户已切换）
            if (loadToken.IsCancellationRequested)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Load cancelled after completion (user switched song): {audioInfo.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                return;
            }
            
            // 加载音频数据（跳过流式 AudioClip）
            if (audioClip.loadState == AudioDataLoadState.Unloaded && !IsStreamingClip(audioClip))
            {
                audioClip.LoadAudioData();
            }
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] About to play argument music: {audioInfo.Title}, loadState={audioClip.loadState}");
            
            SingletonMonoBehaviour<MusicManager>.Instance.Play(
                audioClip, 1f, 0f, 1f,
                musicService.IsRepeatOneMusic,
                true, "",
                () => musicService.SkipCurrentMusic(MusicChangeKind.Auto).Forget<bool>()
            );
            
            // 清除加载标志（UI 已在加载前更新过了）
            FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] Now playing: {audioInfo.Title}");
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 播放指定歌曲
        /// </summary>
        private static async UniTask<bool> PlayAudioAsync(MusicService musicService, GameAudioInfo audio, MusicChangeKind changeKind)
        {
            if (audio == null)
            {
                Plugin.Log.LogWarning("[PlayQueuePatch] Audio is null");
                return false;
            }
            
            // 重置 EOF 追踪状态，允许新歌曲的 EOF 被检测
            AudioPlayer_Update_Patch.ResetEofTracking();
            
            // 设置加载标志，防止 UpdateFacility 在加载期间调用 PauseMusic
            FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = true;
            
            // 立即停止当前播放，减少用户感知延迟
            var playingMusic = musicService.PlayingMusic;
            if (playingMusic != null && playingMusic.AudioClip != null)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Stopping current: {playingMusic.AudioClipName}");
                SingletonMonoBehaviour<MusicManager>.Instance.Stop(playingMusic.AudioClip);
            }
            
            // 立即切换 UI 显示到新歌曲，进度条会停在 0 等待加载
            SetPlayingMusic(musicService, audio);
            MusicService_GetProgress_Patch.ResetProgress();  // 重置进度为 0
            InvokeOnChangeMusic(musicService, changeKind);
            InvokeOnPlayMusic(musicService, audio);
            
            // 使用智能加载获取 AudioClip
            // 获取新的 CancellationToken，会自动取消之前的加载任务
            var loadToken = GetNewLoadCancellationToken(30);
            
            AudioClip audioClip;
            try
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Smart loading for: {audio.Title}");
                audioClip = await StreamingAudioLoader.SmartLoadAsync(audio, loadToken);
                
                // 检查是否在加载期间被取消（用户切换了歌曲）
                if (loadToken.IsCancellationRequested)
                {
                    Plugin.Log.LogInfo($"[PlayQueuePatch] Load cancelled (user switched song): {audio.Title}");
                    FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                    return false;
                }
                
                Plugin.Log.LogInfo($"[PlayQueuePatch] SmartLoad returned: {(audioClip != null ? audioClip.name : "null")}");
            }
            catch (OperationCanceledException)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Load cancelled or timeout: {audio.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PlayQueuePatch] Failed to load audio clip: {ex.Message}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                return false;
            }
            
            if (audioClip == null)
            {
                Plugin.Log.LogWarning($"[PlayQueuePatch] AudioClip is null for {audio.AudioClipName}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                return false;
            }
            
            // 再次检查是否被取消（加载可能成功但用户已切换）
            if (musicService.PlayingMusic?.UUID != audio.UUID)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] User switched song during load, aborting play: {audio.Title}");
                FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
                return false;
            }
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] AudioClip ready: {audioClip.name}, loadType={audioClip.loadType}");
            
            // 加载音频数据（跳过流式 AudioClip，它们不需要预加载）
            // 流式 clip 的 loadState 永远是 Unloaded 但不能调用 LoadAudioData
            bool isStreaming = IsStreamingClip(audioClip);
            Plugin.Log.LogInfo($"[PlayQueuePatch] loadState={audioClip.loadState}, isStreaming={isStreaming}");
            
            if (audioClip.loadState == AudioDataLoadState.Unloaded && !isStreaming)
            {
                Plugin.Log.LogInfo($"[PlayQueuePatch] Loading audio data...");
                audioClip.LoadAudioData();
            }
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] About to play: {audio.AudioClipName}, loadState={audioClip.loadState}");
            
            SingletonMonoBehaviour<MusicManager>.Instance.Play(
                audioClip, 1f, 0f, 1f,
                musicService.IsRepeatOneMusic,
                true, "",
                () => musicService.SkipCurrentMusic(MusicChangeKind.Auto).Forget<bool>()
            );
            
            // 清除加载标志（UI 已在加载前更新过了）
            FacilityMusic_UpdateFacility_Patch.IsLoadingMusic = false;
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] Playing: {audio.AudioClipName}");
            return true;
        }
        
        /// <summary>
        /// 检查是否是流式 AudioClip
        /// 流式 clip 使用 PCMReaderCallback，loadState 永远是 Unloaded
        /// </summary>
        private static bool IsStreamingClip(AudioClip clip)
        {
            // 流式 clip 的 loadType 是 Streaming
            return clip.loadType == AudioClipLoadType.Streaming;
        }
        
        /// <summary>
        /// 设置 PlayingMusic（Publicize 后可直接访问 private setter）
        /// 同时清理上一首歌曲的资源（模块歌曲的 AudioClip 和封面缓存）
        /// </summary>
        private static void SetPlayingMusic(MusicService musicService, GameAudioInfo audio)
        {
            var previousMusic = musicService.PlayingMusic;
            
            // 清理上一首歌曲的资源
            if (previousMusic != null && previousMusic.UUID != audio?.UUID)
            {
                CleanupPreviousMusicResources(previousMusic);
            }
            
            musicService.PlayingMusic = audio;
        }
        
        /// <summary>
        /// 清理上一首歌曲的资源
        /// </summary>
        private static void CleanupPreviousMusicResources(GameAudioInfo previousMusic)
        {
            if (previousMusic == null) return;
            
            var uuid = previousMusic.UUID;
            
            // 清理模块歌曲的封面缓存
            // 只清理模块导入的歌曲（Tag 包含自定义位，即位 5+）
            bool isModuleImported = ((ulong)previousMusic.Tag & ~31UL) != 0;
            if (isModuleImported)
            {
                CoverService.Instance?.RemoveMusicCover(uuid);
                Plugin.Log.LogDebug($"[PlayQueuePatch] Cleaned up cover for: {previousMusic.Title}");
            }
            
            // 清理流式 AudioClip 的资源（FlacStreamReader）
            if (previousMusic.AudioClip != null)
            {
                AudioResourceManager.Instance.CleanupClip(previousMusic.AudioClip);
            }
            
            // 对于模块导入的歌曲，销毁 AudioClip
            if (isModuleImported && previousMusic.AudioClip != null)
            {
                // 模块导入的歌曲需要完全销毁 AudioClip
                UnityEngine.Object.Destroy(previousMusic.AudioClip);
                previousMusic.AudioClip = null;
                Plugin.Log.LogDebug($"[PlayQueuePatch] Destroyed AudioClip for: {previousMusic.Title}");
            }
        }
        
        /// <summary>
        /// 触发 OnChangeMusic 事件（Publicize 后可直接访问 private 字段）
        /// </summary>
        private static void InvokeOnChangeMusic(MusicService musicService, MusicChangeKind kind)
        {
            musicService.onChangeMusic.OnNext(kind);
        }

        
        /// <summary>
        /// 触发 OnPlayMusic 事件（Publicize 后可直接访问 private 字段）
        /// </summary>
        private static void InvokeOnPlayMusic(MusicService musicService, GameAudioInfo audio)
        {
            musicService.onPlayMusic.OnNext(audio);
        }
        
        #endregion
        
        #region Public API for UI
        
        /// <summary>
        /// 添加歌曲到队列（供 UI 调用）
        /// </summary>
        public static void AddToQueue(GameAudioInfo audio)
        {
            PlayQueueManager.Instance.Enqueue(audio);
        }
        
        /// <summary>
        /// 添加歌曲为下一首播放（供 UI 调用）
        /// </summary>
        public static void PlayNext(GameAudioInfo audio)
        {
            PlayQueueManager.Instance.InsertNext(audio);
        }
        
        /// <summary>
        /// 从队列移除歌曲（供 UI 调用）
        /// </summary>
        public static void RemoveFromQueue(GameAudioInfo audio)
        {
            PlayQueueManager.Instance.Remove(audio);
        }
        
        /// <summary>
        /// 从队列移除指定索引的歌曲（供 UI 调用）
        /// </summary>
        public static void RemoveFromQueueAt(int index)
        {
            PlayQueueManager.Instance.RemoveAt(index);
        }
        
        /// <summary>
        /// 重排序队列（供拖放 UI 调用）
        /// </summary>
        public static void ReorderQueue(int fromIndex, int toIndex)
        {
            PlayQueueManager.Instance.Move(fromIndex, toIndex);
        }
        
        /// <summary>
        /// 清空队列（保留当前播放）
        /// </summary>
        public static void ClearQueue()
        {
            PlayQueueManager.Instance.ClearPending();
        }
        
        /// <summary>
        /// 获取队列内容
        /// </summary>
        public static IReadOnlyList<GameAudioInfo> GetQueue()
        {
            return PlayQueueManager.Instance.Queue;
        }
        
        /// <summary>
        /// 直接设置播放指定歌曲（不走 SkipCurrentMusic 逻辑）
        /// 用于移除当前播放歌曲后，直接播放新的队首
        /// </summary>
        public static void SetPlayingMusicDirect(MusicService musicService, GameAudioInfo audio, MusicChangeKind kind)
        {
            if (musicService == null || audio == null) return;
            
            // 获取 AudioClip
            var audioClip = audio.AudioClip;
            if (audioClip == null)
            {
                Plugin.Log.LogWarning($"[SetPlayingMusicDirect] AudioClip is null for {audio.AudioClipName}");
                return;
            }
            
            // 停止当前播放
            var playingMusic = musicService.PlayingMusic;
            if (playingMusic?.AudioClip != null)
            {
                SingletonMonoBehaviour<MusicManager>.Instance.Stop(playingMusic.AudioClip);
            }
            
            // 加载音频数据（跳过流式 AudioClip）
            if (audioClip.loadState == AudioDataLoadState.Unloaded && !IsStreamingClip(audioClip))
            {
                audioClip.LoadAudioData();
            }
            
            Plugin.Log.LogInfo($"[PlayQueuePatch] About to play from queue: {audio.AudioClipName}, loadState={audioClip.loadState}");
            
            SingletonMonoBehaviour<MusicManager>.Instance.Play(
                audioClip, 1f, 0f, 1f,
                musicService.IsRepeatOneMusic,
                true, "",
                () => musicService.SkipCurrentMusic(MusicChangeKind.Auto).Forget<bool>()
            );
            
            // 更新 MusicService 状态
            SetPlayingMusic(musicService, audio);
            
            // 触发事件
            InvokeOnChangeMusic(musicService, kind);
            InvokeOnPlayMusic(musicService, audio);
            
            Plugin.Log.LogInfo($"[SetPlayingMusicDirect] Now playing: {audio.AudioClipName}");
        }
        
        #endregion
    }
}
