using System;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Logging;
using Bulbul;
using ChillPatcher.Native;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.ModuleSystem;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.ModuleSystem.Services;
using ChillPatcher.SDK.Interfaces;
using UnityEngine;

namespace ChillPatcher.UIFramework.Audio
{
    /// <summary>
    /// 系统媒体传输控制 (SMTC) 服务
    /// 将游戏的音乐播放状态同步到 Windows 系统媒体控制
    /// </summary>
    public class SystemMediaTransportService : IDisposable
    {
        private static SystemMediaTransportService _instance;
        public static SystemMediaTransportService Instance => _instance ??= new SystemMediaTransportService();

        private readonly ManualLogSource _log;
        private bool _initialized;
        private bool _disposed;
        
        // 当前播放信息缓存
        private string _currentTitle;
        private string _currentArtist;
        private string _currentAlbum;
        private string _currentMusicUuid; // 当前播放的歌曲 UUID
        private bool _isPlaying;

        // 游戏服务引用
        private MusicService _musicService;
        private Bulbul.FacilityMusic _facilityMusic;

        private SystemMediaTransportService()
        {
            _log = BepInEx.Logging.Logger.CreateLogSource("SmtcService");
        }

        /// <summary>
        /// 初始化 SMTC 服务
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            
            if (!PluginConfig.EnableSystemMediaTransport.Value)
            {
                _log.LogInfo("系统媒体控制功能已禁用");
                return;
            }

            try
            {
                // 初始化原生桥接
                if (!SmtcBridge.Initialize())
                {
                    _log.LogWarning("SMTC 初始化失败，可能缺少 ChillSmtcBridge.dll");
                    return;
                }

                // 注册按钮事件
                SmtcBridge.OnButtonPressed += OnButtonPressed;
                
                // 订阅封面加载完成事件（用于异步更新封面）
                CoverService.Instance.OnMusicCoverLoaded += OnMusicCoverLoaded;
                
                // 设置媒体类型
                SmtcBridge.SetMediaType(SmtcBridge.MediaType.Music);
                
                _initialized = true;
                _log.LogInfo("SMTC 服务已初始化");
            }
            catch (Exception ex)
            {
                _log.LogError($"SMTC 服务初始化异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 歌曲封面加载完成回调
        /// </summary>
        private async void OnMusicCoverLoaded(string uuid, UnityEngine.Sprite sprite)
        {
            // 只更新当前播放歌曲的封面
            if (uuid == _currentMusicUuid && _initialized)
            {
                try
                {
                    // 重新获取封面字节数据并更新 SMTC
                    var (data, mimeType) = await CoverService.Instance.GetMusicCoverBytesAsync(uuid);
                    if (data != null && data.Length > 0)
                    {
                        if (SmtcBridge.SetThumbnailFromMemory(data, mimeType ?? "image/jpeg"))
                        {
                            SmtcBridge.UpdateDisplay();
                            _log.LogDebug($"封面异步加载完成，已更新 SMTC 封面: {uuid}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug($"异步更新 SMTC 封面失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 设置游戏服务引用
        /// </summary>
        public void SetGameServices(MusicService musicService, Bulbul.FacilityMusic facilityMusic)
        {
            _musicService = musicService;
            _facilityMusic = facilityMusic;
        }

        /// <summary>
        /// 更新媒体信息
        /// </summary>
        public void UpdateMediaInfo(string title, string artist, string album)
        {
            if (!_initialized) return;

            _currentTitle = title ?? "";
            _currentArtist = artist ?? "";
            _currentAlbum = album ?? "";

            SmtcBridge.SetMusicInfo(_currentTitle, _currentArtist, _currentAlbum);
            SmtcBridge.UpdateDisplay();
            
            _log.LogDebug($"更新媒体信息: {_currentTitle} - {_currentArtist}");
        }

        /// <summary>
        /// 更新媒体信息（从 GameAudioInfo）
        /// </summary>
        public void UpdateMediaInfo(GameAudioInfo audioInfo)
        {
            if (audioInfo == null) return;

            string title = audioInfo.Title ?? audioInfo.AudioClipName;
            string artist = audioInfo.Credit ?? "Unknown Artist";
            
            // 尝试从 AlbumManager 获取专辑名称
            string album = GetAlbumName(audioInfo);

            UpdateMediaInfo(title, artist, album);

            // 尝试设置封面
            TrySetThumbnail(audioInfo);
        }
        
        /// <summary>
        /// 获取歌曲的专辑名称
        /// </summary>
        private string GetAlbumName(GameAudioInfo audioInfo)
        {
            try
            {
                // 从 AlbumRegistry 获取专辑名称
                if (!string.IsNullOrEmpty(audioInfo.UUID))
                {
                    var musicInfo = MusicRegistry.Instance?.GetByUUID(audioInfo.UUID);
                    if (musicInfo != null && !string.IsNullOrEmpty(musicInfo.AlbumId))
                    {
                        var albumInfo = AlbumRegistry.Instance?.GetAlbum(musicInfo.AlbumId);
                        if (albumInfo != null && !string.IsNullOrEmpty(albumInfo.DisplayName))
                        {
                            return albumInfo.DisplayName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"获取专辑名称失败: {ex.Message}");
            }
            
            // 默认返回游戏名
            return "Chill With You";
        }

        /// <summary>
        /// 尝试设置封面图片（异步，不阻塞主线程）
        /// </summary>
        private void TrySetThumbnail(GameAudioInfo audioInfo)
        {
            // 记录当前播放的歌曲 UUID
            _currentMusicUuid = audioInfo.UUID;
            
            // 使用 fire-and-forget 模式，避免阻塞主线程
            _ = TrySetThumbnailAsync(audioInfo);
        }

        /// <summary>
        /// 异步设置封面图片
        /// </summary>
        private async Task TrySetThumbnailAsync(GameAudioInfo audioInfo)
        {
            try
            {
                bool thumbnailSet = false;
                
                // 1. 如果有 UUID，尝试从 CoverService 获取封面（异步）
                if (!string.IsNullOrEmpty(audioInfo.UUID))
                {
                    thumbnailSet = await TrySetThumbnailFromCoverServiceAsync(audioInfo.UUID);
                }

                // 2. 如果是游戏原生歌曲，使用游戏内置封面
                if (!thumbnailSet && audioInfo.PathType == AudioMode.Normal && audioInfo.Tag != AudioTag.Local)
                {
                    thumbnailSet = TrySetGameCover((int)audioInfo.Tag);
                }

                // 3. 如果还是没有封面，使用默认封面
                if (!thumbnailSet)
                {
                    thumbnailSet = TrySetDefaultCover(audioInfo.Tag == AudioTag.Local);
                }
                
                SmtcBridge.UpdateDisplay();
            }
            catch (Exception ex)
            {
                _log.LogWarning($"设置封面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步从 CoverService 获取封面（不阻塞主线程）
        /// </summary>
        private async Task<bool> TrySetThumbnailFromCoverServiceAsync(string uuid)
        {
            try
            {
                // 使用统一的 CoverService 获取封面字节（真正的异步）
                var (data, mimeType) = await CoverService.Instance.GetMusicCoverBytesAsync(uuid);
                
                if (data != null && data.Length > 0)
                {
                    if (SmtcBridge.SetThumbnailFromMemory(data, mimeType ?? "image/jpeg"))
                    {
                        _log.LogDebug($"从 CoverService 获取封面成功: {uuid}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug($"从 CoverService 获取封面失败: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// 尝试设置游戏内置封面
        /// </summary>
        private bool TrySetGameCover(int audioTag)
        {
            try
            {
                var (coverData, mimeType) = CoverService.Instance.GetGameCoverBytes(audioTag);
                if (coverData != null && coverData.Length > 0)
                {
                    if (SmtcBridge.SetThumbnailFromMemory(coverData, mimeType))
                    {
                        _log.LogDebug($"从游戏内置封面设置缩略图成功: tag={audioTag}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug($"设置游戏封面失败: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 尝试设置默认封面
        /// </summary>
        private bool TrySetDefaultCover(bool isLocal)
        {
            try
            {
                var (coverData, mimeType) = CoverService.Instance.GetDefaultCoverBytes(isLocal);
                if (coverData != null && coverData.Length > 0)
                {
                    if (SmtcBridge.SetThumbnailFromMemory(coverData, mimeType))
                    {
                        _log.LogDebug($"使用默认封面设置缩略图成功");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug($"设置默认封面失败: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 设置播放状态
        /// </summary>
        public void SetPlaybackStatus(bool isPlaying)
        {
            if (!_initialized) return;

            _isPlaying = isPlaying;
            var status = isPlaying ? SmtcBridge.PlaybackStatus.Playing : SmtcBridge.PlaybackStatus.Paused;
            SmtcBridge.SetPlaybackStatus(status);
        }

        /// <summary>
        /// 更新时间线
        /// </summary>
        public void UpdateTimeline(long durationMs, long positionMs)
        {
            if (!_initialized) return;
            SmtcBridge.SetTimelineProperties(0, durationMs, positionMs);
        }

        /// <summary>
        /// 处理按钮事件
        /// </summary>
        private void OnButtonPressed(SmtcBridge.ButtonType buttonType)
        {
            _log.LogDebug($"SMTC 按钮按下: {buttonType}");

            // 在主线程执行
            MainThreadDispatcher.Instance?.Enqueue(() => HandleButtonPress(buttonType));
        }

        /// <summary>
        /// 在主线程处理按钮事件
        /// </summary>
        private void HandleButtonPress(SmtcBridge.ButtonType buttonType)
        {
            try
            {
                if (_facilityMusic == null || _musicService == null)
                {
                    _log.LogWarning("游戏服务未设置，无法处理按钮事件");
                    return;
                }

                switch (buttonType)
                {
                    case SmtcBridge.ButtonType.Play:
                        if (_facilityMusic.IsPaused)
                        {
                            _facilityMusic.UnPauseMusic();
                            SetPlaybackStatus(true);
                        }
                        break;

                    case SmtcBridge.ButtonType.Pause:
                        if (!_facilityMusic.IsPaused)
                        {
                            _facilityMusic.PauseMusic();
                            SetPlaybackStatus(false);
                        }
                        break;

                    case SmtcBridge.ButtonType.Next:
                        _musicService.PlayNextMusic(1, MusicChangeKind.Manual);
                        break;

                    case SmtcBridge.ButtonType.Previous:
                        _musicService.PlayNextMusic(-1, MusicChangeKind.Manual);
                        break;

                    case SmtcBridge.ButtonType.Stop:
                        _facilityMusic.PauseMusic();
                        SetPlaybackStatus(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"处理按钮事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        public void Shutdown()
        {
            if (!_initialized) return;

            try
            {
                SmtcBridge.OnButtonPressed -= OnButtonPressed;
                CoverService.Instance.OnMusicCoverLoaded -= OnMusicCoverLoaded;
                SmtcBridge.SetPlaybackStatus(SmtcBridge.PlaybackStatus.Closed);
                SmtcBridge.Shutdown();
                
                _initialized = false;
                _log.LogInfo("SMTC 服务已关闭");
            }
            catch (Exception ex)
            {
                _log.LogError($"SMTC 服务关闭异常: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Shutdown();
            _log.Dispose();
        }
    }
}
