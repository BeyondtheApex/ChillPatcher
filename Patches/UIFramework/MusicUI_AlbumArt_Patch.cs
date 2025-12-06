using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bulbul;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using R3;
using ChillPatcher.UIFramework.Core;
using ChillPatcher.UIFramework.Music;
using ChillPatcher.ModuleSystem.Services;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 将播放列表切换按钮的图标替换为当前播放音乐的封面
    /// 保持按钮的所有功能不变
    /// 
    /// ✅ 使用 Publicizer 直接访问 private 字段（消除反射开销）
    /// </summary>
    [HarmonyPatch(typeof(MusicUI))]
    public static class MusicUI_AlbumArt_Patch
    {
        
        // 存储原始图标引用
        private static Sprite _originalDeactiveIcon;
        private static Sprite _originalActiveIcon;
        
        // 存储当前的封面 Sprite
        private static Sprite _currentAlbumArtSprite;
        private static string _currentAudioPath;
        
        // 存储 Image 组件引用
        private static Image _iconDeactiveImage;
        private static Image _iconActiveImage;
        
        // 存储按钮引用
        private static InteractableUI _facilityOpenButton;
        
        // 存储原始 Mask 引用
        private static Mask _originalMask;
        private static Image _originalMaskImage;
        
        // 订阅管理
        private static IDisposable _musicPlaySubscription;

        // 当前正在等待的歌曲 UUID（用于事件回调匹配）
        private static string _pendingMusicUuid;

        /// <summary>
        /// 检查是否已经有封面被设置（用于 UI 重排列补丁判断）
        /// </summary>
        public static bool HasAlbumArtSet => _currentAlbumArtSprite != null;

        /// <summary>
        /// 在 Setup 方法执行后初始化封面显示
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("Setup")]
        public static void Setup_Postfix(MusicUI __instance)
        {
            // 检查配置是否启用
            if (!UIFrameworkConfig.EnableAlbumArtDisplay.Value)
            {
                Plugin.Logger.LogDebug("[MusicUI_AlbumArt_Patch] Album art display is disabled");
                return;
            }

            try
            {
                // ✅ 直接访问 _facilityOpenButton（Publicizer 消除反射）
                var facilityOpenButton = __instance._facilityOpenButton;
                if (facilityOpenButton == null)
                {
                    Plugin.Logger.LogWarning("[MusicUI_AlbumArt_Patch] Failed to get _facilityOpenButton");
                    return;
                }
                _facilityOpenButton = facilityOpenButton;

                // ✅ 直接访问 _facilityMusic（Publicizer 消除反射）
                var facilityMusic = __instance._facilityMusic;
                if (facilityMusic == null)
                {
                    Plugin.Logger.LogWarning("[MusicUI_AlbumArt_Patch] Failed to get _facilityMusic");
                    return;
                }

                // 查找 IconDeactivemage 和 IconActiveImage
                var buttonTransform = facilityOpenButton.transform;
                var deactiveImageTransform = buttonTransform.Find("IconDeactivemage");
                var activeImageTransform = buttonTransform.Find("IconActiveImage");

                if (deactiveImageTransform == null || activeImageTransform == null)
                {
                    Plugin.Logger.LogWarning("[MusicUI_AlbumArt_Patch] Failed to find icon images");
                    return;
                }

                _iconDeactiveImage = deactiveImageTransform.GetComponent<Image>();
                _iconActiveImage = activeImageTransform.GetComponent<Image>();

                if (_iconDeactiveImage == null || _iconActiveImage == null)
                {
                    Plugin.Logger.LogWarning("[MusicUI_AlbumArt_Patch] Failed to get Image components");
                    return;
                }

                // 保存原始图标
                if (_originalDeactiveIcon == null)
                {
                    _originalDeactiveIcon = _iconDeactiveImage.sprite;
                    Plugin.Logger.LogInfo($"[MusicUI_AlbumArt_Patch] Saved original deactive icon: {_originalDeactiveIcon?.name}");
                }
                
                if (_originalActiveIcon == null)
                {
                    _originalActiveIcon = _iconActiveImage.sprite;
                    Plugin.Logger.LogInfo($"[MusicUI_AlbumArt_Patch] Saved original active icon: {_originalActiveIcon?.name}");
                }
                
                // 如果启用了 UI 重排列，设置方形模式
                if (UIFrameworkConfig.EnableUIRearrange.Value)
                {
                    SetupSquareMode(buttonTransform);
                }

                // 监听音乐播放事件 - 使用静态字段保存订阅，避免 AddTo 参数问题
                if (_musicPlaySubscription != null)
                {
                    _musicPlaySubscription.Dispose();
                }
                
                _musicPlaySubscription = facilityMusic.MusicService.OnPlayMusic.Subscribe(music =>
                {
                    UpdateAlbumArt(music);
                });

                // 订阅 CoverService 歌曲封面加载完成事件
                CoverService.Instance.OnMusicCoverLoaded -= OnMusicCoverLoaded;
                CoverService.Instance.OnMusicCoverLoaded += OnMusicCoverLoaded;

                // 立即更新当前播放的音乐封面
                if (facilityMusic.PlayingMusic != null)
                {
                    UpdateAlbumArt(facilityMusic.PlayingMusic);
                }

                Plugin.Logger.LogInfo("[MusicUI_AlbumArt_Patch] Album art display initialized");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[MusicUI_AlbumArt_Patch] Error in Setup_Postfix: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 歌曲封面加载完成回调
        /// </summary>
        private static void OnMusicCoverLoaded(string uuid, Sprite cover)
        {
            // 只更新当前等待的歌曲
            if (uuid == _pendingMusicUuid && cover != null)
            {
                bool useSquareMode = UIFrameworkConfig.EnableUIRearrange.Value;
                
                // 如果需要圆形模式，需要处理
                if (!useSquareMode && cover.texture != null)
                {
                    var circularSprite = AlbumArtReader.CreateCircularSprite(cover.texture, 88);
                    if (circularSprite != null)
                    {
                        cover = circularSprite;
                    }
                }
                
                ApplyAlbumArt(cover, uuid, "");
                _pendingMusicUuid = null;
            }
        }

        /// <summary>
        /// 设置方形模式（移除圆形遮罩，添加阴影）
        /// </summary>
        private static void SetupSquareMode(Transform buttonTransform)
        {
            try
            {
                // 查找并禁用 Mask 组件
                var maskObj = buttonTransform.Find("Mask");
                if (maskObj != null)
                {
                    _originalMask = maskObj.GetComponent<Mask>();
                    if (_originalMask != null)
                    {
                        _originalMask.enabled = false;
                        Plugin.Logger.LogInfo("[MusicUI_AlbumArt_Patch] Disabled circular mask for square mode");
                    }
                    
                    // 同时隐藏 Mask 的 Image（圆形边框）
                    _originalMaskImage = maskObj.GetComponent<Image>();
                    if (_originalMaskImage != null)
                    {
                        _originalMaskImage.enabled = false;
                    }
                }
                
                // 方形模式保留原来的悬浮缩放效果，不需要额外添加阴影
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[MusicUI_AlbumArt_Patch] Error setting up square mode: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新封面图标（使用 CoverService 统一加载逻辑）
        /// </summary>
        private static void UpdateAlbumArt(GameAudioInfo audioInfo)
        {
            // 检查配置是否启用
            if (!UIFrameworkConfig.EnableAlbumArtDisplay.Value)
                return;

            if (_iconDeactiveImage == null || _iconActiveImage == null)
                return;

            bool useSquareMode = UIFrameworkConfig.EnableUIRearrange.Value;

            // 如果有 UUID，使用 CoverService 的统一 API
            if (!string.IsNullOrEmpty(audioInfo.UUID))
            {
                // 记录正在等待的 UUID
                _pendingMusicUuid = audioInfo.UUID;
                
                // 使用 CoverService 的同步 API（返回占位图或缓存，异步加载后通过事件更新）
                var sprite = CoverService.Instance.GetMusicCoverOrPlaceholder(audioInfo.UUID);
                
                // 如果需要圆形模式且返回的不是占位图，需要处理
                if (!useSquareMode && sprite != CoverService.Instance.LoadingPlaceholder && sprite?.texture != null)
                {
                    var circularSprite = AlbumArtReader.CreateCircularSprite(sprite.texture, 88);
                    if (circularSprite != null)
                    {
                        sprite = circularSprite;
                    }
                }
                
                _iconDeactiveImage.sprite = sprite;
                _iconActiveImage.sprite = sprite;
                _currentAlbumArtSprite = sprite;
                _currentAudioPath = audioInfo.UUID;
                return;
            }

            // 如果是本地文件且路径存在，尝试加载目录封面
            if (audioInfo.PathType == AudioMode.LocalPc && !string.IsNullOrEmpty(audioInfo.LocalPath))
            {
                _pendingMusicUuid = null;
                var albumArtTexture = TryLoadAlbumCover(audioInfo.LocalPath);
                if (albumArtTexture != null)
                {
                    int resolution = useSquareMode ? UIRearrangePatch.AlbumArtResolution : 88;
                    Sprite albumArtSprite = useSquareMode
                        ? AlbumArtReader.CreateSquareSprite(albumArtTexture, resolution)
                        : AlbumArtReader.CreateCircularSprite(albumArtTexture, resolution);

                    if (albumArtSprite != null)
                    {
                        ApplyAlbumArt(albumArtSprite, audioInfo.LocalPath, audioInfo.Title);
                        return;
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(albumArtTexture);
                    }
                }
                
                // 本地文件但没有封面，使用本地导入专用封面
                TryUseDefaultCover(useSquareMode, isLocalImport: true);
                return;
            }

            // 不是本地文件，使用默认封面
            _pendingMusicUuid = null;
            TryUseDefaultCover(useSquareMode, isLocalImport: false);
        }

        /// <summary>
        /// 应用封面到 UI
        /// </summary>
        private static void ApplyAlbumArt(Sprite sprite, string cacheKey, string title)
        {
            // 只销毁非缓存的封面
            var defaultCover = CoverService.Instance.GetDefaultMusicCover();
            var localCover = CoverService.Instance.GetLocalMusicCover();
            var loadingPlaceholder = CoverService.Instance.LoadingPlaceholder;
            if (_currentAlbumArtSprite != null
                && _currentAlbumArtSprite != defaultCover
                && _currentAlbumArtSprite != localCover
                && _currentAlbumArtSprite != loadingPlaceholder)
            {
                UnityEngine.Object.Destroy(_currentAlbumArtSprite.texture);
                UnityEngine.Object.Destroy(_currentAlbumArtSprite);
            }

            _currentAlbumArtSprite = sprite;
            _currentAudioPath = cacheKey;
            
            _iconDeactiveImage.sprite = sprite;
            _iconActiveImage.sprite = sprite;
            
            if (!string.IsNullOrEmpty(title))
            {
                Plugin.Logger.LogInfo($"[MusicUI_AlbumArt_Patch] Updated album art for: {title}");
            }
        }

        /// <summary>
        /// 尝试加载专辑封面（从文件夹中查找）
        /// </summary>
        private static Texture2D TryLoadAlbumCover(string audioFilePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(audioFilePath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return null;

                // 查找常见的封面文件名
                string[] coverNames = { "cover", "folder", "front", "album", "artwork" };
                string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

                foreach (var name in coverNames)
                {
                    foreach (var ext in extensions)
                    {
                        var coverPath = Path.Combine(directory, name + ext);
                        if (File.Exists(coverPath))
                        {
                            var bytes = File.ReadAllBytes(coverPath);
                            var texture = new Texture2D(2, 2);
                            if (UnityEngine.ImageConversion.LoadImage(texture, bytes))
                            {
                                Plugin.Logger.LogInfo($"[MusicUI_AlbumArt_Patch] Loaded album cover from: {coverPath}");
                                return texture;
                            }
                            UnityEngine.Object.Destroy(texture);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[MusicUI_AlbumArt_Patch] Error loading album cover: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 尝试使用默认封面
        /// </summary>
        /// <param name="useSquareMode">是否使用方形模式</param>
        /// <param name="isLocalImport">是否是本地导入的歌曲</param>
        private static void TryUseDefaultCover(bool useSquareMode, bool isLocalImport = false)
        {
            try
            {
                // 根据是否是本地导入选择不同的封面
                Sprite sprite = isLocalImport 
                    ? CoverService.Instance.GetLocalMusicCover()
                    : CoverService.Instance.GetDefaultMusicCover();
                    
                if (sprite != null)
                {
                    // 只销毁非缓存的封面
                    var defaultCover = CoverService.Instance.GetDefaultMusicCover();
                    var localCover = CoverService.Instance.GetLocalMusicCover();
                    if (_currentAlbumArtSprite != null 
                        && _currentAlbumArtSprite != defaultCover
                        && _currentAlbumArtSprite != localCover)
                    {
                        UnityEngine.Object.Destroy(_currentAlbumArtSprite.texture);
                        UnityEngine.Object.Destroy(_currentAlbumArtSprite);
                    }
                    
                    _currentAlbumArtSprite = sprite;
                    _currentAudioPath = null;
                    
                    _iconDeactiveImage.sprite = sprite;
                    _iconActiveImage.sprite = sprite;
                    
                    var coverType = isLocalImport ? "local import" : "default";
                    Plugin.Logger.LogInfo($"[MusicUI_AlbumArt_Patch] Using {coverType} cover");
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[MusicUI_AlbumArt_Patch] Error loading cover: {ex.Message}");
            }

            // 最后的回退：使用原始图标
            RestoreOriginalIcon();
        }

        /// <summary>
        /// 恢复原始图标
        /// </summary>
        private static void RestoreOriginalIcon()
        {
            if (_iconDeactiveImage != null && _originalDeactiveIcon != null)
            {
                _iconDeactiveImage.sprite = _originalDeactiveIcon;
            }
            
            if (_iconActiveImage != null && _originalActiveIcon != null)
            {
                _iconActiveImage.sprite = _originalActiveIcon;
            }
            
            // 只销毁非缓存的封面
            var defaultCover = CoverService.Instance?.GetDefaultMusicCover();
            var localCover = CoverService.Instance?.GetLocalMusicCover();
            if (_currentAlbumArtSprite != null
                && _currentAlbumArtSprite != defaultCover
                && _currentAlbumArtSprite != localCover)
            {
                UnityEngine.Object.Destroy(_currentAlbumArtSprite.texture);
                UnityEngine.Object.Destroy(_currentAlbumArtSprite);
            }
            _currentAlbumArtSprite = null;
            
            _currentAudioPath = null;
        }
    }
}
