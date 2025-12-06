using System.Collections.Generic;
using Bulbul;
using ChillPatcher.UIFramework.Core;
using ChillPatcher.UIFramework.Music;
using HarmonyLib;
using NestopiSystem.DIContainers;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// MusicPlayListButtons 补丁: 添加队列操作按钮
    /// 在播放列表视图中，为每个项目添加:
    /// - X按钮(旋转45度) = 添加到队列末尾
    /// - 循环箭头按钮 = 下一首播放
    /// 
    /// 播放状态显示逻辑:
    /// - 正在播放的歌曲: 显示 PlayIcon，隐藏 QueueButtons
    /// - 未在播放的歌曲: 隐藏 PlayIcon，显示 QueueButtons
    /// </summary>
    [HarmonyPatch(typeof(MusicPlayListButtons))]
    [HarmonyPatch("Setup", typeof(GameAudioInfo), typeof(FacilityMusic))]
    public class MusicPlayListButtons_QueueActions_Patch
    {
        // 按钮名称
        private const string AddToQueueButtonName = "ChillPatcher_AddToQueue";
        private const string PlayNextButtonName = "ChillPatcher_PlayNext";
        private const string QueueButtonsContainerName = "ChillPatcher_QueueButtons";
        
        // 跟踪所有活动的 MusicPlayListButtons 实例
        private static readonly List<MusicPlayListButtons> _activeInstances = new List<MusicPlayListButtons>();
        
        // 订阅管理
        private static System.IDisposable _musicChangeSubscription;
        private static bool _queueChangeSubscribed = false;
        private static bool _prefabEventSubscribed = false;
        
        /// <summary>
        /// 确保订阅了 Prefab 缓存事件
        /// </summary>
        private static void EnsurePrefabEventSubscription()
        {
            if (_prefabEventSubscribed)
                return;
                
            _prefabEventSubscribed = true;
            
            // 订阅 Prefab 缓存完成事件
            PrefabFactory.OnXCloseButtonPrefabCached += OnPrefabCached;
            PrefabFactory.OnCircleArrowButtonPrefabCached += OnPrefabCached;
        }
        
        /// <summary>
        /// 当 Prefab 缓存完成时，刷新所有活动实例的按钮
        /// </summary>
        private static void OnPrefabCached()
        {
            Plugin.Log.LogInfo("[QueueActions] Prefab cached, refreshing existing instances...");
            
            // 清理已销毁的实例
            _activeInstances.RemoveAll(instance => instance == null);
            
            // 刷新所有活动实例
            foreach (var instance in _activeInstances)
            {
                try
                {
                    var audioInfo = instance.AudioInfo;
                    if (audioInfo == null)
                        continue;
                        
                    var contents = instance.transform.Find("PlayListMusicPlayButton/Contents");
                    if (contents == null)
                        continue;
                        
                    var container = contents.Find(QueueButtonsContainerName);
                    if (container != null)
                    {
                        // 尝试补充缺失的按钮
                        EnsureQueueButtons(container, audioInfo);
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogDebug($"[QueueActions] Error refreshing instance: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 确保容器内有所有需要的按钮
        /// </summary>
        private static void EnsureQueueButtons(Transform container, GameAudioInfo audioInfo)
        {
            // 获取或创建数据组件
            var data = container.GetComponent<QueueButtonData>();
            if (data == null)
            {
                data = container.gameObject.AddComponent<QueueButtonData>();
                data.AudioInfo = audioInfo;
            }
            
            // 获取按钮大小（从容器的 RectTransform）
            var containerRect = container.GetComponent<RectTransform>();
            float buttonSize = containerRect != null ? containerRect.sizeDelta.y : 30f;
            
            // 检查并创建 AddToQueue 按钮
            var addToQueueBtn = container.Find(AddToQueueButtonName);
            if (addToQueueBtn == null && PrefabFactory.XCloseButtonPrefab != null)
            {
                addToQueueBtn = CreateAddToQueueButton(container, audioInfo);
                if (addToQueueBtn != null)
                {
                    // 添加 LayoutElement 控制大小
                    var layoutElement = addToQueueBtn.gameObject.AddComponent<LayoutElement>();
                    layoutElement.preferredWidth = buttonSize;
                    layoutElement.preferredHeight = buttonSize;
                    // 禁用大小变化动画
                    DisableScaleAnimations(addToQueueBtn.gameObject);
                    // 确保按钮在 PlayNext 之前
                    addToQueueBtn.SetAsFirstSibling();
                    
                    Plugin.Log.LogDebug($"[QueueActions] Late-created AddToQueue button for {audioInfo.AudioClipName}");
                }
            }
            
            // 获取 AddToQueue 按钮的数据引用
            QueueButtonData addToQueueData = null;
            if (addToQueueBtn != null)
            {
                addToQueueData = addToQueueBtn.GetComponent<QueueButtonData>();
            }
            
            // 检查并创建 PlayNext 按钮
            var playNextBtn = container.Find(PlayNextButtonName);
            if (playNextBtn == null && PrefabFactory.CircleArrowButtonPrefab != null)
            {
                playNextBtn = CreatePlayNextButton(container, audioInfo, addToQueueData);
                if (playNextBtn != null)
                {
                    // 添加 LayoutElement 控制大小
                    var layoutElement = playNextBtn.gameObject.AddComponent<LayoutElement>();
                    layoutElement.preferredWidth = buttonSize;
                    layoutElement.preferredHeight = buttonSize;
                    // 禁用大小变化动画
                    DisableScaleAnimations(playNextBtn.gameObject);
                    
                    Plugin.Log.LogDebug($"[QueueActions] Late-created PlayNext button for {audioInfo.AudioClipName}");
                }
            }
        }
        
        /// <summary>
        /// Patch Setup方法 - 添加队列操作按钮
        /// </summary>
        [HarmonyPostfix]
        static void Setup_AddQueueButtons_Postfix(MusicPlayListButtons __instance)
        {
            // 确保订阅了 Prefab 缓存事件
            EnsurePrefabEventSubscription();
            
            try
            {
                // 只在播放列表视图中添加(非队列视图)
                if (MusicUI_VirtualScroll_Patch.IsShowingQueue)
                    return;
                
                // 查找 Contents 子对象 (路径: MusicCell/PlayListMusicPlayButton/Contents)
                var contents = __instance.transform.Find("PlayListMusicPlayButton/Contents");
                if (contents == null)
                {
                    Plugin.Log.LogWarning("[QueueActions] PlayListMusicPlayButton/Contents not found");
                    return;
                }
                
                var audioInfo = __instance.AudioInfo;
                if (audioInfo == null)
                    return;
                
                // 查找或创建按钮容器
                var container = contents.Find(QueueButtonsContainerName);
                if (container == null)
                {
                    container = CreateQueueButtonsContainer(contents, audioInfo);
                }
                else
                {
                    // 更新容器内按钮关联的audioInfo
                    UpdateContainerButtons(container, audioInfo);
                }
                
                // 注册实例以便监听播放状态变化
                if (!_activeInstances.Contains(__instance))
                {
                    _activeInstances.Add(__instance);
                }
                
                // 确保订阅了音乐变化事件
                EnsureMusicChangeSubscription();
                
                // 更新当前项的显示状态
                UpdatePlayingState(__instance, contents);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[QueueActions] Error adding queue buttons: {ex}");
            }
        }
        
        /// <summary>
        /// 确保订阅了音乐变化事件
        /// </summary>
        private static void EnsureMusicChangeSubscription()
        {
            // 订阅 MusicService.OnChangeMusic
            if (_musicChangeSubscription == null)
            {
                try
                {
                    var musicService = ProjectLifetimeScope.Resolve<MusicService>();
                    if (musicService != null)
                    {
                        _musicChangeSubscription = musicService.OnChangeMusic
                            .Subscribe(_ => OnMusicChanged());
                        
                        Plugin.Log.LogDebug("[QueueActions] Subscribed to music change events");
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogWarning($"[QueueActions] Failed to subscribe to music events: {ex.Message}");
                }
            }
            
            // 订阅 PlayQueueManager.OnQueueChanged
            if (!_queueChangeSubscribed)
            {
                PlayQueueManager.Instance.OnQueueChanged += OnQueueChanged;
                _queueChangeSubscribed = true;
                Plugin.Log.LogDebug("[QueueActions] Subscribed to queue change events");
            }
        }
        
        /// <summary>
        /// 音乐变化时更新所有实例的显示状态
        /// </summary>
        private static void OnMusicChanged()
        {
            UpdateAllInstances();
        }
        
        /// <summary>
        /// 队列变化时更新所有实例的按钮状态
        /// </summary>
        private static void OnQueueChanged()
        {
            UpdateAllInstancesQueueState();
        }
        
        /// <summary>
        /// 更新所有实例的显示状态（播放状态）
        /// </summary>
        private static void UpdateAllInstances()
        {
            // 清理已销毁的实例
            _activeInstances.RemoveAll(instance => instance == null);
            
            foreach (var instance in _activeInstances)
            {
                try
                {
                    var contents = instance.transform.Find("PlayListMusicPlayButton/Contents");
                    if (contents != null)
                    {
                        UpdatePlayingState(instance, contents);
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogWarning($"[QueueActions] Error updating instance: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 更新所有实例的队列按钮状态
        /// </summary>
        private static void UpdateAllInstancesQueueState()
        {
            // 清理已销毁的实例
            _activeInstances.RemoveAll(instance => instance == null);
            
            foreach (var instance in _activeInstances)
            {
                try
                {
                    var contents = instance.transform.Find("PlayListMusicPlayButton/Contents");
                    if (contents == null) continue;
                    
                    var container = contents.Find(QueueButtonsContainerName);
                    if (container == null) continue;
                    
                    var audioInfo = instance.AudioInfo;
                    if (audioInfo == null) continue;
                    
                    // 更新 AddToQueue 按钮状态
                    var addToQueueBtn = container.Find(AddToQueueButtonName);
                    if (addToQueueBtn != null)
                    {
                        var data = addToQueueBtn.GetComponent<QueueButtonData>();
                        if (data != null)
                        {
                            bool isInQueue = PlayQueueManager.Instance.Contains(audioInfo.UUID);
                            // 使用动画更新，让状态变化更平滑
                            data.SetInQueueWithAnimation(isInQueue);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogWarning($"[QueueActions] Error updating queue state: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 更新单个实例的播放状态显示
        /// </summary>
        private static void UpdatePlayingState(MusicPlayListButtons instance, Transform contents)
        {
            var audioInfo = instance.AudioInfo;
            if (audioInfo == null)
                return;
            
            // 获取当前播放的音乐
            GameAudioInfo playingMusic = null;
            try
            {
                var musicService = ProjectLifetimeScope.Resolve<MusicService>();
                playingMusic = musicService?.PlayingMusic;
            }
            catch { }
            
            // 判断是否是当前播放的歌曲
            bool isCurrentPlaying = playingMusic != null && playingMusic.UUID == audioInfo.UUID;
            
            // 查找 PlayIcon 和 QueueButtons
            var playIcon = contents.Find("PlayIcon");
            var queueButtons = contents.Find(QueueButtonsContainerName);
            
            if (playIcon != null && queueButtons != null)
            {
                // 正在播放: 显示 PlayIcon, 隐藏 QueueButtons
                // 未在播放: 隐藏 PlayIcon, 显示 QueueButtons
                playIcon.gameObject.SetActive(isCurrentPlaying);
                bool wasHidden = !queueButtons.gameObject.activeSelf;
                queueButtons.gameObject.SetActive(!isCurrentPlaying);
                
                // 如果按钮从隐藏变为显示，更新其队列状态
                if (wasHidden && !isCurrentPlaying)
                {
                    var addToQueueBtn = queueButtons.Find(AddToQueueButtonName);
                    if (addToQueueBtn != null)
                    {
                        var data = addToQueueBtn.GetComponent<QueueButtonData>();
                        if (data != null)
                        {
                            bool isInQueue = PlayQueueManager.Instance.Contains(audioInfo.UUID);
                            // 使用立即设置，因为按钮刚刚显示，不需要动画
                            data.SetInQueueImmediate(isInQueue);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 创建队列按钮容器，替换 PlayIcon
        /// </summary>
        private static Transform CreateQueueButtonsContainer(Transform contents, GameAudioInfo audioInfo)
        {
            // 找到 PlayIcon
            var playIcon = contents.Find("PlayIcon");
            if (playIcon == null)
            {
                Plugin.Log.LogWarning("[QueueActions] PlayIcon not found");
                return null;
            }
            
            int playIconIndex = playIcon.GetSiblingIndex();
            
            // 获取 PlayIcon 的大小作为参考
            var playIconRect = playIcon.GetComponent<RectTransform>();
            float buttonSize = playIconRect != null ? playIconRect.sizeDelta.x : 30f;
            
            // 创建容器 GameObject
            var containerObj = new GameObject(QueueButtonsContainerName);
            containerObj.transform.SetParent(contents, false);
            containerObj.transform.SetSiblingIndex(playIconIndex);
            
            // 添加 RectTransform
            var containerRect = containerObj.AddComponent<RectTransform>();
            
            // 设置容器大小（两个按钮 + 间距）
            float spacing = 5f;
            containerRect.sizeDelta = new Vector2(buttonSize * 2 + spacing, buttonSize);
            
            // 复制 PlayIcon 的锚点设置
            if (playIconRect != null)
            {
                containerRect.anchorMin = playIconRect.anchorMin;
                containerRect.anchorMax = playIconRect.anchorMax;
                containerRect.pivot = playIconRect.pivot;
            }
            
            // 添加 HorizontalLayoutGroup 让按钮横向排列
            var layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.spacing = spacing;
            
            // 创建 AddToQueue 按钮
            var addToQueueBtn = CreateAddToQueueButton(containerObj.transform, audioInfo);
            QueueButtonData addToQueueData = null;
            if (addToQueueBtn != null)
            {
                addToQueueData = addToQueueBtn.GetComponent<QueueButtonData>();
                // 添加 LayoutElement 控制大小
                var layoutElement = addToQueueBtn.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = buttonSize;
                layoutElement.preferredHeight = buttonSize;
                // 禁用大小变化动画
                DisableScaleAnimations(addToQueueBtn.gameObject);
            }
            
            // 创建 PlayNext 按钮，传入 AddToQueue 按钮的数据引用
            var playNextBtn = CreatePlayNextButton(containerObj.transform, audioInfo, addToQueueData);
            if (playNextBtn != null)
            {
                // 添加 LayoutElement 控制大小
                var layoutElement = playNextBtn.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = buttonSize;
                layoutElement.preferredHeight = buttonSize;
                // 禁用大小变化动画
                DisableScaleAnimations(playNextBtn.gameObject);
            }
            
            // 注意: 不在这里设置 PlayIcon 和 QueueButtons 的可见性
            // 由 UpdatePlayingState 方法根据播放状态动态设置
            
            return containerObj.transform;
        }
        
        /// <summary>
        /// 禁用按钮的大小变化动画（保留颜色变化）
        /// </summary>
        private static void DisableScaleAnimations(GameObject buttonObj)
        {
            // 立即销毁所有 HoldButtonAnimation 组件（包括子对象）
            var holdAnims = buttonObj.GetComponentsInChildren<HoldButtonAnimation>(true);
            foreach (var holdAnim in holdAnims)
            {
                if (holdAnim != null)
                {
                    Object.DestroyImmediate(holdAnim);
                }
            }
            
            // 重置所有子对象的 scale
            var transforms = buttonObj.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                t.localScale = Vector3.one;
            }
            
            // 修复 UpButtonActiveImage 的大小 - 让它完全填充父对象
            var activeImage = buttonObj.transform.Find("UpButtonActiveImage");
            if (activeImage != null)
            {
                var activeRect = activeImage.GetComponent<RectTransform>();
                if (activeRect != null)
                {
                    // 设置锚点为全部拉伸
                    activeRect.anchorMin = Vector2.zero;
                    activeRect.anchorMax = Vector2.one;
                    activeRect.offsetMin = Vector2.zero;
                    activeRect.offsetMax = Vector2.zero;
                    activeRect.sizeDelta = Vector2.zero;
                }
            }
            
            // 删除 LongPressButton 子对象
            var longPressButton = buttonObj.transform.Find("LongPressButton");
            if (longPressButton != null)
            {
                Object.DestroyImmediate(longPressButton.gameObject);
            }
        }
        
        /// <summary>
        /// 更新容器内按钮的 audioInfo
        /// </summary>
        private static void UpdateContainerButtons(Transform container, GameAudioInfo audioInfo)
        {
            QueueButtonData addToQueueData = null;
            
            var addToQueueBtn = container.Find(AddToQueueButtonName);
            if (addToQueueBtn != null)
            {
                addToQueueData = addToQueueBtn.GetComponent<QueueButtonData>();
                UpdateAddToQueueButton(addToQueueBtn.gameObject, audioInfo, addToQueueData);
                // 确保禁用缩放动画
                DisableScaleAnimations(addToQueueBtn.gameObject);
            }
            
            var playNextBtn = container.Find(PlayNextButtonName);
            if (playNextBtn != null)
            {
                UpdatePlayNextButton(playNextBtn.gameObject, audioInfo, addToQueueData);
                // 确保禁用缩放动画
                DisableScaleAnimations(playNextBtn.gameObject);
            }
        }
        
        /// <summary>
        /// 创建 "添加到队列" 按钮 (X图标旋转45度)
        /// </summary>
        private static Transform CreateAddToQueueButton(Transform parent, GameAudioInfo audioInfo)
        {
            // 尝试获取 XCloseButton Prefab
            if (PrefabFactory.XCloseButtonPrefab == null)
            {
                Plugin.Log.LogWarning("[QueueActions] XCloseButtonPrefab not cached yet");
                return null;
            }
            
            // 克隆按钮
            var buttonObj = Object.Instantiate(PrefabFactory.XCloseButtonPrefab, parent);
            buttonObj.name = AddToQueueButtonName;
            buttonObj.SetActive(true);
            
            // 存储audioInfo引用
            var data = buttonObj.AddComponent<QueueButtonData>();
            data.AudioInfo = audioInfo;
            
            // 检查是否已在队列中，设置初始旋转状态
            bool isInQueue = PlayQueueManager.Instance.Contains(audioInfo.UUID);
            data.SetInQueueImmediate(isInQueue);
            
            // 设置按钮点击事件
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnAddToQueueClicked(audioInfo, data));
            }
            
            Plugin.Log.LogDebug($"[QueueActions] Created AddToQueue button for {audioInfo.AudioClipName}");
            return buttonObj.transform;
        }
        
        /// <summary>
        /// 创建 "下一首播放" 按钮 (循环箭头图标)
        /// </summary>
        private static Transform CreatePlayNextButton(Transform parent, GameAudioInfo audioInfo, QueueButtonData addToQueueButtonData)
        {
            // 尝试获取 CircleArrowButton Prefab
            if (PrefabFactory.CircleArrowButtonPrefab == null)
            {
                Plugin.Log.LogWarning("[QueueActions] CircleArrowButtonPrefab not cached yet");
                return null;
            }
            
            // 克隆按钮
            var buttonObj = Object.Instantiate(PrefabFactory.CircleArrowButtonPrefab, parent);
            buttonObj.name = PlayNextButtonName;
            buttonObj.SetActive(true);
            
            // 设置按钮点击事件
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnPlayNextClicked(audioInfo, addToQueueButtonData));
            }
            
            // 存储audioInfo引用
            var data = buttonObj.AddComponent<QueueButtonData>();
            data.AudioInfo = audioInfo;
            
            Plugin.Log.LogDebug($"[QueueActions] Created PlayNext button for {audioInfo.AudioClipName}");
            return buttonObj.transform;
        }
        
        /// <summary>
        /// 更新 AddToQueue 按钮 (用于虚拟滚动复用)
        /// </summary>
        private static void UpdateAddToQueueButton(GameObject buttonObj, GameAudioInfo audioInfo, QueueButtonData data)
        {
            if (data != null)
            {
                data.AudioInfo = audioInfo;
                // 立即更新队列状态（无动画，因为是复用）
                bool isInQueue = PlayQueueManager.Instance.Contains(audioInfo.UUID);
                data.SetInQueueImmediate(isInQueue);
            }
            
            // 更新按钮点击事件
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnAddToQueueClicked(audioInfo, data));
            }
        }
        
        /// <summary>
        /// 更新 PlayNext 按钮 (用于虚拟滚动复用)
        /// </summary>
        private static void UpdatePlayNextButton(GameObject buttonObj, GameAudioInfo audioInfo, QueueButtonData addToQueueData)
        {
            var data = buttonObj.GetComponent<QueueButtonData>();
            if (data != null)
            {
                data.AudioInfo = audioInfo;
            }
            
            // 更新按钮点击事件
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnPlayNextClicked(audioInfo, addToQueueData));
            }
        }
        
        /// <summary>
        /// 添加到队列末尾 / 从队列移除
        /// </summary>
        private static void OnAddToQueueClicked(GameAudioInfo audioInfo, QueueButtonData buttonData)
        {
            if (audioInfo == null) return;
            
            var queueManager = PlayQueueManager.Instance;
            bool isInQueue = queueManager.Contains(audioInfo.UUID);
            
            if (isInQueue)
            {
                // 从队列移除
                queueManager.RemoveByUUID(audioInfo.UUID);
                Plugin.Log.LogInfo($"[QueueActions] Removed from queue: {audioInfo.AudioClipName}");
                buttonData?.SetInQueueWithAnimation(false);
            }
            else
            {
                // 添加到队列
                queueManager.Enqueue(audioInfo);
                Plugin.Log.LogInfo($"[QueueActions] Added to queue: {audioInfo.AudioClipName}");
                buttonData?.SetInQueueWithAnimation(true);
            }
        }
        
        /// <summary>
        /// 下一首播放（InsertNext）
        /// 如果在历史回溯模式，退出历史模式
        /// 如果歌曲已在队列：
        ///   - 已在索引1（下一首位置）：忽略操作
        ///   - 在其他位置：移动到索引1
        /// </summary>
        private static void OnPlayNextClicked(GameAudioInfo audioInfo, QueueButtonData addToQueueButtonData)
        {
            if (audioInfo == null) return;
            
            var queueManager = PlayQueueManager.Instance;
            
            // 检查歌曲是否已在队列中
            int existingIndex = queueManager.IndexOf(audioInfo);
            
            if (existingIndex >= 0)
            {
                // 已经在队列中
                if (existingIndex == 1)
                {
                    // 已经在下一首位置（索引1），忽略操作
                    Plugin.Log.LogInfo($"[QueueActions] Play next: {audioInfo.AudioClipName} already at next position, ignored");
                    return;
                }
                else if (existingIndex == 0)
                {
                    // 正在播放，不能移动到下一首
                    Plugin.Log.LogInfo($"[QueueActions] Play next: {audioInfo.AudioClipName} is currently playing, ignored");
                    return;
                }
                else
                {
                    // 在其他位置，移动到索引1
                    queueManager.Remove(audioInfo);
                    queueManager.Insert(1, audioInfo);
                    Plugin.Log.LogInfo($"[QueueActions] Play next: moved {audioInfo.AudioClipName} from index {existingIndex} to next position");
                    // 更新按钮状态（仍然在队列中）
                    addToQueueButtonData?.SetInQueueWithAnimation(true);
                    return;
                }
            }
            
            // 如果在历史回溯模式，退出历史模式
            if (queueManager.IsInHistoryMode)
            {
                Plugin.Log.LogInfo("[QueueActions] Exiting history mode due to InsertNext");
                queueManager.ResetHistoryPosition();
            }
            
            queueManager.InsertNext(audioInfo);
            Plugin.Log.LogInfo($"[QueueActions] Play next: {audioInfo.AudioClipName}");
            
            // 更新 AddToQueue 按钮状态（进入了队列）
            addToQueueButtonData?.SetInQueueWithAnimation(true);
        }
    }
    
    /// <summary>
    /// 用于存储按钮关联的AudioInfo和队列状态
    /// </summary>
    public class QueueButtonData : MonoBehaviour
    {
        public GameAudioInfo AudioInfo { get; set; }
        
        /// <summary>
        /// 当前是否在队列中（用于确定旋转状态）
        /// </summary>
        public bool IsInQueue { get; set; } = false;
        
        /// <summary>
        /// 旋转动画协程
        /// </summary>
        private Coroutine _rotationCoroutine;
        
        /// <summary>
        /// 带动画地切换队列状态
        /// </summary>
        public void SetInQueueWithAnimation(bool inQueue)
        {
            if (IsInQueue == inQueue) return;
            
            IsInQueue = inQueue;
            
            // 停止之前的动画
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
            }
            
            // 开始新的旋转动画
            // 在队列中: 旋转到 0° (显示 X)
            // 不在队列中: 旋转到 45° (显示 +)
            float targetAngle = inQueue ? 0f : 45f;
            _rotationCoroutine = StartCoroutine(RotateToAngle(targetAngle, 0.2f));
        }
        
        /// <summary>
        /// 立即设置状态（无动画）
        /// </summary>
        public void SetInQueueImmediate(bool inQueue)
        {
            IsInQueue = inQueue;
            float targetAngle = inQueue ? 0f : 45f;
            transform.localRotation = Quaternion.Euler(0, 0, targetAngle);
        }
        
        private System.Collections.IEnumerator RotateToAngle(float targetAngle, float duration)
        {
            float startAngle = transform.localEulerAngles.z;
            // 处理角度跨越问题
            if (startAngle > 180f) startAngle -= 360f;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float currentAngle = Mathf.Lerp(startAngle, targetAngle, t);
                transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
                yield return null;
            }
            
            transform.localRotation = Quaternion.Euler(0, 0, targetAngle);
            _rotationCoroutine = null;
        }
    }
}
