using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using KanKikuchi.AudioManager;
using UnityEngine;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.UIFramework.Audio;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 音频控制 API (IPC 版本)
    /// 播放控制通过 OmniMixPlayer API，游戏内状态通过 MusicService
    /// </summary>
    public class ChillAudioApi
    {
        private readonly ManualLogSource _logger;

        public ChillAudioApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region 播放控制

        public void togglePause()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.OnClickButtonPlayOrPauseMusic();
            // Game handles play/pause state internally; no backend Toggle needed.
        }

        public void pause()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.PauseMusic();
            _ = OmniMixIntegration.Instance.Pause();
        }

        public void resume()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.UnPauseMusic();
            _ = OmniMixIntegration.Instance.Resume();
        }

        public void next()
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.OnClickButtonSkip();
            // Game's own queue determines the next UUID → Play(uuid).
        }

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
            // Game's own queue determines the previous UUID → Play(uuid).
        }

        public void playByIndex(int index)
        {
            var facility = GetFacilityMusic();
            if (facility == null) return;
            facility.PlayMusic(index);
        }

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
                    _ = OmniMixIntegration.Instance.Play(uuid);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 进度控制

        public void setProgress(float progress)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            musicService?.SetMusicProgress(Mathf.Clamp01(progress));
        }

        public float getProgress()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            return musicService?.GetCurrentMusicProgress() ?? 0f;
        }

        #endregion

        #region 播放模式

        public void setShuffle(bool enabled)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            musicService?.SetShuffle(enabled);
        }

        public bool getShuffle()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            return musicService?.IsShuffle ?? false;
        }

        public void setRepeatOne(bool enabled)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            musicService?.SetRepeat(enabled);
        }

        public bool getRepeatOne()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            return musicService?.IsRepeatOneMusic ?? false;
        }

        #endregion

        #region 当前播放信息

        public string getCurrentSong()
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var playing = musicService?.PlayingMusic;
            if (playing == null) return "null";

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["uuid"] = playing.UUID ?? "",
                ["title"] = playing.Title ?? "",
                ["artist"] = playing.Credit ?? "",
                ["duration"] = playing.AudioClip != null ? playing.AudioClip.length : 0f,
                ["isStream"] = StreamingAudioLoader.IsStreamingSource(playing)
            });
        }

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
