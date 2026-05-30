using Bulbul;
using ChillPatcher.UIFramework.Core;
using ChillPatcher.UIFramework.Music;
using DG.Tweening;
using HarmonyLib;
using ObservableCollections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// 添加播放队列按钮到音乐播放列表 UI
    /// 位置: Paremt/PCPlatform/Canvas/UI/ChangeOrderObjects/MusicPlayList/Contents
    /// </summary>
    [HarmonyPatch]
    public class PlayQueueButton_Patch
    {
        #region Constants - 可手动调整
        
        /// <summary>
        /// 按钮宽度
        /// </summary>
        private const float ButtonWidth = 200f;
        
        /// <summary>
        /// 按钮 X 位置（相对于父容器 Contents）
        /// </summary>
        private const float ButtonPositionX = 460f;
        
        /// <summary>
        /// 按钮 Y 位置（相对于父容器 Contents）
        /// </summary>
        private const float ButtonPositionY = -22f;
        
        /// <summary>
        /// 淡入淡出动画时长（秒）
        /// </summary>
        private const float FadeDuration = 0.2f;
        
        #endregion
        
        private static SimpleCapsuleButton _playQueueButton;
        private static bool _buttonCreated = false;
        private static bool _isShowingQueue = false;
        private static bool _isAnimating = false;
        private static CanvasGroup _listCanvasGroup;
        
        /// <summary>
        /// 队列数据（直接从 PlayQueueManager 获取）
        /// </summary>
        public static IReadOnlyList<GameAudioInfo> QueueData => PlayQueueManager.Instance.Queue;
        
        /// <summary>
        /// 当前是否显示队列
        /// </summary>
        public static bool IsShowingQueue => _isShowingQueue;
        
        /// <summary>
        /// 在 MusicUI.Setup 后添加播放队列按钮
        /// </summary>
        [HarmonyPatch(typeof(MusicUI), "Bulbul.IMusicListUI.Setup")]
        [HarmonyPostfix]
        static void MusicUI_Setup_Postfix(MusicUI __instance)
        {
            // 确保只创建一次
            if (_buttonCreated && _playQueueButton?.GameObject != null)
                return;
                
            try
            {
                CreatePlayQueueButton(__instance);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to create PlayQueue button: {ex}");
            }
        }
        
        /// <summary>
        /// 创建播放队列按钮
        /// </summary>
        private static void CreatePlayQueueButton(MusicUI musicUI)
        {
            // 查找 Contents 容器
            var contentsPath = "Paremt/PCPlatform/Canvas/UI/ChangeOrderObjects/MusicPlayList/Contents";
            var contents = GameObject.Find(contentsPath);
            
            if (contents == null)
            {
                // 尝试从 Canvas 查找
                var canvas = GameObject.Find("Paremt/PCPlatform/Canvas");
                if (canvas != null)
                {
                    var target = canvas.transform.Find("UI/ChangeOrderObjects/MusicPlayList/Contents");
                    if (target != null)
                        contents = target.gameObject;
                }
            }
            
            if (contents == null)
            {
                Plugin.Log.LogError("Cannot find MusicPlayList/Contents");
                return;
            }
            
            // 确保胶囊按钮 Prefab 已缓存
            if (PrefabFactory.SimpleCapsuleButtonPrefab == null)
            {
                PrefabFactory.CacheCapsuleButtonFromScene();
                
                if (PrefabFactory.SimpleCapsuleButtonPrefab == null)
                {
                    Plugin.Log.LogWarning("SimpleCapsuleButtonPrefab not available. PlayQueue button will not be created.");
                    return;
                }
            }
            
            // 创建播放队列按钮
            _playQueueButton = PrefabFactory.CreateSimpleCapsuleButton(
                parent: contents.transform,
                text: "播放队列",
                width: ButtonWidth,
                onClick: OnPlayQueueButtonClicked
            );
            
            if (_playQueueButton == null)
            {
                Plugin.Log.LogError("Failed to create SimpleCapsuleButton");
                return;
            }
            
            // 隐藏图标
            _playQueueButton.SetIconVisible(false);
            
            // 使用绝对位置（相对于父容器 Contents）
            _playQueueButton.SetPosition(ButtonPositionX, ButtonPositionY);
            
            // 设置名称便于调试
            _playQueueButton.GameObject.name = "PlayQueueButton";
            
            _buttonCreated = true;
            Plugin.Log.LogInfo($"PlayQueue button created at position ({ButtonPositionX}, {ButtonPositionY}), width={ButtonWidth}");
        }
        
        /// <summary>
        /// 播放队列按钮点击回调 - 切换队列/播放列表显示
        /// </summary>
        private static void OnPlayQueueButtonClicked()
        {
            // 防止动画过程中重复点击
            if (_isAnimating) return;
            
            if (_isShowingQueue)
            {
                // 切换回播放列表（带动画）
                SwitchToPlaylistWithAnimation();
            }
            else
            {
                // 切换到队列（数据直接从 PlayQueueManager 获取，带动画）
                SwitchToQueueWithAnimation();
            }
        }
        
        /// <summary>
        /// 确保列表容器有 CanvasGroup 组件
        /// 注意: 游戏更新后 _playListButtonsParent 从 MusicUI 移到了 MusicPlayListView
        /// </summary>
        private static CanvasGroup EnsureCanvasGroup()
        {
            if (_listCanvasGroup != null) return _listCanvasGroup;
            
            var musicUI = UnityEngine.Object.FindObjectOfType<MusicUI>();
            if (musicUI == null) return null;
            
            // 游戏更新后: 从 MusicUI 获取 MusicPlayListView，再获取 _playListButtonsParent
            var playListView = Traverse.Create(musicUI)
                .Field("_musicPlayListView")
                .GetValue<Bulbul.MusicPlayListView>();
                
            if (playListView == null) return null;
            
            var playListButtonsParent = Traverse.Create(playListView)
                .Field("_playListButtonsParent")
                .GetValue<GameObject>();
                
            if (playListButtonsParent == null) return null;
            
            _listCanvasGroup = playListButtonsParent.GetComponent<CanvasGroup>();
            if (_listCanvasGroup == null)
            {
                _listCanvasGroup = playListButtonsParent.AddComponent<CanvasGroup>();
            }
            
            return _listCanvasGroup;
        }
        
        /// <summary>
        /// 带动画切换到队列
        /// </summary>
        private static void SwitchToQueueWithAnimation()
        {
            var canvasGroup = EnsureCanvasGroup();
            if (canvasGroup == null)
            {
                // 无法获取 CanvasGroup，直接切换
                SwitchToQueue();
                return;
            }
            
            _isAnimating = true;
            
            // 淡出 → 切换 → 淡入
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, FadeDuration)
                .OnComplete(() =>
                {
                    _isShowingQueue = true;
                    _playQueueButton?.SetText("返回列表");
                    MusicUI_VirtualScroll_Patch.SwitchToQueue();
                    MusicTagListUI_Patches.SwitchToQueueMode();  // 切换 TagListUI 到队列模式
                    
                    DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, FadeDuration)
                        .OnComplete(() =>
                        {
                            _isAnimating = false;
                        });
                    
                    Plugin.Log.LogInfo($"Switched to queue view with {QueueData.Count} items (animated)");
                });
        }
        
        /// <summary>
        /// 带动画切换回播放列表
        /// </summary>
        private static void SwitchToPlaylistWithAnimation()
        {
            var canvasGroup = EnsureCanvasGroup();
            if (canvasGroup == null)
            {
                // 无法获取 CanvasGroup，直接切换
                SwitchToPlaylist();
                return;
            }
            
            _isAnimating = true;
            
            // 淡出 → 切换 → 淡入
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, FadeDuration)
                .OnComplete(() =>
                {
                    _isShowingQueue = false;
                    _playQueueButton?.SetText("播放队列");
                    MusicUI_VirtualScroll_Patch.SwitchToPlaylist();
                    MusicTagListUI_Patches.SwitchToNormalMode();  // 恢复 TagListUI 到正常模式
                    
                    DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, FadeDuration)
                        .OnComplete(() =>
                        {
                            _isAnimating = false;
                        });
                    
                    Plugin.Log.LogInfo("Switched to playlist view (animated)");
                });
        }
        
        /// <summary>
        /// 切换到队列显示
        /// </summary>
        public static void SwitchToQueue()
        {
            _isShowingQueue = true;
            _playQueueButton?.SetText("返回列表");
            
            // 使用 MusicUI_VirtualScroll_Patch 的队列模式
            MusicUI_VirtualScroll_Patch.SwitchToQueue();
            MusicTagListUI_Patches.SwitchToQueueMode();  // 切换 TagListUI 到队列模式;
            
            Plugin.Log.LogInfo($"Switched to queue view with {QueueData.Count} items");
        }
        
        /// <summary>
        /// 切换回播放列表显示
        /// </summary>
        public static void SwitchToPlaylist()
        {
            _isShowingQueue = false;
            _playQueueButton?.SetText("播放队列");
            
            // 使用 MusicUI_VirtualScroll_Patch 返回正常模式
            MusicUI_VirtualScroll_Patch.SwitchToPlaylist();
            MusicTagListUI_Patches.SwitchToNormalMode();  // 恢复 TagListUI 到正常模式
            
            Plugin.Log.LogInfo("Switched to playlist view");
        }
        
        /// <summary>
        /// 添加项目到队列（通过 PlayQueueManager）
        /// </summary>
        public static void AddToQueue(GameAudioInfo audioInfo)
        {
            PlayQueueManager.Instance.Enqueue(audioInfo);
            
            // 如果正在显示队列，刷新显示
            if (_isShowingQueue)
            {
                MusicUI_VirtualScroll_Patch.SwitchToQueue();
            }
        }
        
        /// <summary>
        /// 从队列移除项目（通过 PlayQueueManager）
        /// </summary>
        public static void RemoveFromQueue(string uuid)
        {
            var audio = QueueData.FirstOrDefault(a => a.UUID == uuid);
            if (audio != null)
            {
                PlayQueueManager.Instance.Remove(audio);
            }
            
            // 如果正在显示队列，刷新显示
            if (_isShowingQueue)
            {
                MusicUI_VirtualScroll_Patch.SwitchToQueue();
            }
        }
        
        /// <summary>
        /// 清空队列（通过 PlayQueueManager）
        /// </summary>
        public static void ClearQueue()
        {
            PlayQueueManager.Instance.ClearPending();
            
            // 如果正在显示队列，刷新显示
            if (_isShowingQueue)
            {
                MusicUI_VirtualScroll_Patch.SwitchToQueue();
            }
        }
    }
}
