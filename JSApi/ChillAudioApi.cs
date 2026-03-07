using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using KanKikuchi.AudioManager;
using UnityEngine;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.ModuleSystem.Registry;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 音频控制 API：播放/暂停/切歌/进度/音量 等
    /// JS 端通过 chill.audio 访问
    /// </summary>
    public class ChillAudioApi
    {
        private readonly ManualLogSource _logger;

        public ChillAudioApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region 播放控制

        /// <summary>
        /// 暂停/恢复切换
        /// </summary>
        public void togglePause()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.OnClickButtonPlayOrPauseMusic();
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void pause()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.PauseMusic();
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void resume()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.UnPauseMusic();
        }

        /// <summary>
        /// 下一首
        /// </summary>
        public void next()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.OnClickButtonSkip();
        }

        /// <summary>
        /// 上一首
        /// </summary>
        public void previous()
        {
            var queue = PlayQueueManager.Instance;
            if (queue == null || !queue.CanGoPrevious) return;

            var prev = queue.GoPrevious();
            if (prev != null)
            {
                var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
                musicService?.PlayArugumentMusic(prev, MusicChangeKind.Manual);
            }
        }

        /// <summary>
        /// 播放指定索引的歌曲
        /// </summary>
        public void playByIndex(int index)
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.PlayMusic(index);
        }

        /// <summary>
        /// 通过 UUID 播放指定歌曲
        /// </summary>
        public bool playByUuid(string uuid)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService == null) return false;

            var playlist = musicService.CurrentPlayList;
            for (int i = 0; i < playlist.Count; i++)
            {
                if (playlist[i].UUID == uuid)
                {
                    var facility = GetFacilityMusic();
                    facility?.PlayMusic(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 进度控制

        /// <summary>
        /// 设置播放进度 (0-1)
        /// </summary>
        public void setProgress(float progress)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            musicService?.SetMusicProgress(Mathf.Clamp01(progress));
        }

        /// <summary>
        /// 获取当前播放进度 (0-1)
        /// </summary>
        public float getProgress()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            return musicService?.GetCurrentMusicProgress() ?? 0f;
        }

        #endregion

        #region 播放模式

        /// <summary>
        /// 设置随机播放
        /// </summary>
        public void setShuffle(bool enabled)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            musicService?.SetShuffle(enabled);
        }

        /// <summary>
        /// 获取是否随机播放
        /// </summary>
        public bool getShuffle()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            return musicService?.IsShuffle ?? false;
        }

        /// <summary>
        /// 设置单曲循环
        /// </summary>
        public void setRepeatOne(bool enabled)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            musicService?.SetRepeat(enabled);
        }

        /// <summary>
        /// 获取是否单曲循环
        /// </summary>
        public bool getRepeatOne()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            return musicService?.IsRepeatOneMusic ?? false;
        }

        #endregion

        #region 当前播放信息

        /// <summary>
        /// 获取当前播放歌曲信息
        /// </summary>
        public string getCurrentSong()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var playing = musicService?.PlayingMusic;
            if (playing == null) return "null";

            var musicInfo = MusicRegistry.Instance?.GetMusic(playing.UUID);

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["uuid"] = playing.UUID ?? "",
                ["title"] = playing.Title ?? "",
                ["artist"] = playing.Credit ?? "",
                ["duration"] = playing.AudioClip != null ? playing.AudioClip.length : 0f,
                ["isStream"] = musicInfo?.SourceType == SDK.Models.MusicSourceType.Stream,
                ["moduleId"] = musicInfo?.ModuleId ?? "",
                ["albumId"] = musicInfo?.AlbumId ?? "",
                ["isFavorite"] = musicInfo?.IsFavorite ?? false,
                ["isExcluded"] = musicInfo?.IsExcluded ?? false
            });
        }

        /// <summary>
        /// 获取播放状态
        /// </summary>
        public string getPlaybackState()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var musicManager = SingletonMonoBehaviour<MusicManager>.Instance;
            var playing = musicService?.PlayingMusic;

            bool isPlaying = false;
            float currentTime = 0f;
            float totalTime = 0f;

            if (playing?.AudioClip != null && musicManager != null)
            {
                var player = musicManager.GetPlayer(playing.AudioClip);
                if (player != null)
                {
                    isPlaying = player.CurrentState == AudioPlayer.State.Playing;
                    currentTime = player.PlayedTime;
                    totalTime = playing.AudioClip.length;
                }
                else if (musicManager.IsPlaying())
                {
                    isPlaying = true;
                    totalTime = playing.AudioClip.length;
                    currentTime = totalTime * (musicService?.GetCurrentMusicProgress() ?? 0f);
                }
            }

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["isPlaying"] = isPlaying,
                ["isPaused"] = !isPlaying && playing != null,
                ["currentTime"] = currentTime,
                ["totalTime"] = totalTime,
                ["progress"] = totalTime > 0 ? currentTime / totalTime : 0f,
                ["shuffle"] = musicService?.IsShuffle ?? false,
                ["repeatOne"] = musicService?.IsRepeatOneMusic ?? false
            });
        }

        #endregion

        #region 辅助

        private FacilityMusic GetFacilityMusic()
        {
            return UnityEngine.Object.FindObjectOfType<FacilityMusic>();
        }

        #endregion
    }
}
