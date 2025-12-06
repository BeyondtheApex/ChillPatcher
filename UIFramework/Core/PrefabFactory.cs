using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bulbul;
using HarmonyLib;

namespace ChillPatcher.UIFramework.Core
{
    /// <summary>
    /// Prefab 缓存和工厂类 - 用于复用游戏的 UI 组件
    /// </summary>
    public static class PrefabFactory
    {
        #region Cached Prefabs
        
        /// <summary>
        /// 简单矩形按钮 Prefab (来自 GeneralInitButton)
        /// 结构: Button + InteractableUI + HoldButtonAnimation + TMP_Text
        /// </summary>
        public static GameObject SimpleRectButtonPrefab { get; private set; }
        
        /// <summary>
        /// 简单胶囊按钮 Prefab (来自 LocalMusicImportButton)
        /// 结构: MusicTitleText, icon, baseActiveImage
        /// 路径: Canvas/UI/ChangeOrderObjects/MusicPlayList/Contents/LocalMusicImportButton
        /// </summary>
        public static GameObject SimpleCapsuleButtonPrefab { get; private set; }
        
        /// <summary>
        /// 播放列表按钮 Prefab
        /// </summary>
        public static GameObject PlayListButtonsPrefab { get; private set; }
        
        /// <summary>
        /// 循环箭头按钮 Prefab (来自番茄钟的上下按钮)
        /// 路径: Canvas/UI/UI_FacilityPomodoro/PomodoroSetting/LoopTime/UpButtonUI
        /// </summary>
        public static GameObject CircleArrowButtonPrefab { get; private set; }
        
        /// <summary>
        /// X关闭按钮 Prefab (来自播放列表的删除按钮)
        /// 路径: PlayListMusicPlayButton/RemoveButton
        /// </summary>
        public static GameObject XCloseButtonPrefab { get; private set; }
        
        /// <summary>
        /// Prefab 是否已缓存
        /// </summary>
        public static bool IsInitialized => SimpleRectButtonPrefab != null || SimpleCapsuleButtonPrefab != null;
        
        /// <summary>
        /// 当 XCloseButtonPrefab 被缓存时触发
        /// </summary>
        public static event System.Action OnXCloseButtonPrefabCached;
        
        /// <summary>
        /// 当 CircleArrowButtonPrefab 被缓存时触发
        /// </summary>
        public static event System.Action OnCircleArrowButtonPrefabCached;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 从 SettingUI 缓存 SimpleRectButton Prefab
        /// 应在 SettingUI.Setup 的 Postfix 中调用
        /// </summary>
        public static void CacheFromSettingUI(SettingUI settingUI)
        {
            if (SimpleRectButtonPrefab != null)
                return;
                
            try
            {
                // 获取 _generalInitButton 字段
                var initButton = Traverse.Create(settingUI)
                    .Field("_generalInitButton")
                    .GetValue<SettingInitButton>();
                    
                if (initButton != null)
                {
                    // 保存原始 GameObject 作为 "Prefab" (实际是场景实例)
                    // 我们需要克隆它来创建真正的模板
                    SimpleRectButtonPrefab = CreatePrefabFromInstance(initButton.gameObject);
                    Plugin.Log.LogInfo($"Cached SimpleRectButtonPrefab from GeneralInitButton");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to cache SimpleRectButtonPrefab: {ex}");
            }
        }
        
        /// <summary>
        /// 从 MusicUI 缓存 PlayListButtons Prefab
        /// </summary>
        public static void CacheFromMusicUI(MusicUI musicUI)
        {
            if (PlayListButtonsPrefab != null)
                return;
                
            try
            {
                PlayListButtonsPrefab = Traverse.Create(musicUI)
                    .Field("_playListButtonsPrefab")
                    .GetValue<GameObject>();
                    
                if (PlayListButtonsPrefab != null)
                {
                    Plugin.Log.LogInfo($"Cached PlayListButtonsPrefab");
                    
                    // 从 PlayListButtonsPrefab 缓存 XCloseButton
                    CacheXCloseButtonFromPlaylistButton();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to cache PlayListButtonsPrefab: {ex}");
            }
            
            // 同时尝试缓存 LocalMusicImportButton (胶囊按钮)
            CacheCapsuleButtonFromScene();
            
            // 缓存循环箭头按钮
            CacheCircleArrowButtonFromScene();
        }
        
        /// <summary>
        /// 从场景中缓存 SimpleCapsuleButton Prefab
        /// 路径: Canvas/UI/ChangeOrderObjects/MusicPlayList/Contents/LocalMusicImportButton
        /// </summary>
        public static void CacheCapsuleButtonFromScene()
        {
            if (SimpleCapsuleButtonPrefab != null)
                return;
                
            try
            {
                // 尝试通过路径查找
                var localMusicImportButton = GameObject.Find("Canvas/UI/ChangeOrderObjects/MusicPlayList/Contents/LocalMusicImportButton");
                
                if (localMusicImportButton == null)
                {
                    // 尝试备用路径
                    var canvas = GameObject.Find("Canvas");
                    if (canvas != null)
                    {
                        var target = canvas.transform.Find("UI/ChangeOrderObjects/MusicPlayList/Contents/LocalMusicImportButton");
                        if (target != null)
                            localMusicImportButton = target.gameObject;
                    }
                }
                
                if (localMusicImportButton != null)
                {
                    SimpleCapsuleButtonPrefab = CreateCapsuleButtonPrefab(localMusicImportButton);
                    Plugin.Log.LogInfo($"Cached SimpleCapsuleButtonPrefab from LocalMusicImportButton");
                }
                else
                {
                    Plugin.Log.LogWarning("LocalMusicImportButton not found in scene. SimpleCapsuleButton will not be available.");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to cache SimpleCapsuleButtonPrefab: {ex}");
            }
        }
        
        /// <summary>
        /// 从场景中缓存 CircleArrowButton Prefab（循环箭头按钮）
        /// 路径: Canvas/UI/UI_FacilityPomodoro/PomodoroSetting/LoopTime/UpButtonUI
        /// </summary>
        public static void CacheCircleArrowButtonFromScene()
        {
            if (CircleArrowButtonPrefab != null)
                return;
                
            try
            {
                // 尝试查找番茄钟的上下按钮
                var canvas = GameObject.Find("Canvas");
                if (canvas != null)
                {
                    var target = canvas.transform.Find("UI/UI_FacilityPomodoro/PomodoroSetting/LoopTime/UpButtonUI");
                    if (target != null)
                    {
                        CircleArrowButtonPrefab = CreateGenericButtonPrefab(target.gameObject, "CircleArrowButton_Prefab");
                        Plugin.Log.LogInfo($"Cached CircleArrowButtonPrefab from PomodoroSetting/LoopTime/UpButtonUI");
                        
                        // 触发缓存完成事件
                        OnCircleArrowButtonPrefabCached?.Invoke();
                    }
                    else
                    {
                        Plugin.Log.LogWarning("UpButtonUI not found in scene. CircleArrowButton will not be available.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to cache CircleArrowButtonPrefab: {ex}");
            }
        }
        
        /// <summary>
        /// 从 MusicUI 缓存 XCloseButton Prefab（X关闭按钮）
        /// 需要在 PlayListButtonsPrefab 缓存后调用
        /// </summary>
        public static void CacheXCloseButtonFromPlaylistButton()
        {
            if (XCloseButtonPrefab != null)
                return;
                
            if (PlayListButtonsPrefab == null)
            {
                Plugin.Log.LogWarning("PlayListButtonsPrefab not cached yet. XCloseButton cache skipped.");
                return;
            }
            
            try
            {
                // 通过 MusicPlayListButtons 组件获取 removeInteractableUI 字段
                var musicPlayListButtons = PlayListButtonsPrefab.GetComponent<MusicPlayListButtons>();
                if (musicPlayListButtons != null)
                {
                    // 使用 Publicizer 直接访问 removeInteractableUI 字段
                    var removeInteractableUI = musicPlayListButtons.removeInteractableUI;
                    if (removeInteractableUI != null)
                    {
                        XCloseButtonPrefab = CreateGenericButtonPrefab(removeInteractableUI.gameObject, "XCloseButton_Prefab");
                        Plugin.Log.LogInfo($"Cached XCloseButtonPrefab from MusicPlayListButtons.removeInteractableUI");
                        
                        // 触发缓存完成事件
                        OnXCloseButtonPrefabCached?.Invoke();
                    }
                    else
                    {
                        Plugin.Log.LogWarning("removeInteractableUI is null in MusicPlayListButtons.");
                    }
                }
                else
                {
                    Plugin.Log.LogWarning("MusicPlayListButtons component not found on PlayListButtonsPrefab.");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to cache XCloseButtonPrefab: {ex}");
            }
        }
        
        /// <summary>
        /// 创建通用按钮 Prefab
        /// </summary>
        private static GameObject CreateGenericButtonPrefab(GameObject instance, string prefabName)
        {
            // 创建一个隐藏的克隆作为模板
            var prefab = Object.Instantiate(instance);
            prefab.name = prefabName;
            prefab.SetActive(false);
            
            // 将其移到 DontDestroyOnLoad 确保不被销毁
            Object.DontDestroyOnLoad(prefab);
            
            // 重置状态 - 清除所有点击事件
            var button = prefab.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
            }
            
            return prefab;
        }
        
        /// <summary>
        /// 从 LocalMusicImportButton 实例创建胶囊按钮 Prefab
        /// </summary>
        private static GameObject CreateCapsuleButtonPrefab(GameObject instance)
        {
            // 创建一个隐藏的克隆作为模板
            var prefab = Object.Instantiate(instance);
            prefab.name = "SimpleCapsuleButton_Prefab";
            prefab.SetActive(false);
            
            // 将其移到 DontDestroyOnLoad 确保不被销毁
            Object.DontDestroyOnLoad(prefab);
            
            // 重置状态 - 清除所有点击事件
            var button = prefab.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
            }
            
            // 重置 InteractableUI 状态
            var interactableUI = prefab.GetComponent<InteractableUI>();
            if (interactableUI != null)
            {
                interactableUI.enabled = true;
            }
            
            // 清除 HoldButtonAnimation 的状态
            var holdAnim = prefab.GetComponent<HoldButtonAnimation>();
            if (holdAnim != null)
            {
                holdAnim.enabled = true;
            }
            
            return prefab;
        }
        
        /// <summary>
        /// 从场景实例创建一个干净的 Prefab 模板
        /// </summary>
        private static GameObject CreatePrefabFromInstance(GameObject instance)
        {
            // 创建一个隐藏的克隆作为模板
            var prefab = Object.Instantiate(instance);
            prefab.name = "SimpleRectButton_Prefab";
            prefab.SetActive(false);
            
            // 将其移到 DontDestroyOnLoad 确保不被销毁
            Object.DontDestroyOnLoad(prefab);
            
            // 重置状态
            var button = prefab.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
            }
            
            // 重置 InteractableUI 状态
            var interactableUI = prefab.GetComponent<InteractableUI>();
            if (interactableUI != null)
            {
                interactableUI.enabled = true;
            }
            
            return prefab;
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// 创建简单矩形按钮
        /// </summary>
        /// <param name="parent">父 Transform</param>
        /// <param name="text">按钮文字</param>
        /// <param name="width">按钮宽度 (null 保持原始宽度)</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>创建的按钮 GameObject</returns>
        public static SimpleRectButton CreateSimpleRectButton(
            Transform parent, 
            string text = "Button", 
            float? width = null,
            System.Action onClick = null)
        {
            if (SimpleRectButtonPrefab == null)
            {
                Plugin.Log.LogError("SimpleRectButtonPrefab not cached! Make sure SettingUI.Setup has been called.");
                return null;
            }
            
            // 克隆 Prefab
            var go = Object.Instantiate(SimpleRectButtonPrefab, parent);
            go.name = $"SimpleRectButton_{text}";
            go.SetActive(true);
            
            // 创建包装类
            var wrapper = new SimpleRectButton(go);
            
            // 设置文字
            wrapper.SetText(text);
            
            // 设置宽度
            if (width.HasValue)
            {
                wrapper.SetWidth(width.Value);
            }
            
            // 设置点击事件
            if (onClick != null)
            {
                wrapper.OnClick(onClick);
            }
            
            return wrapper;
        }
        
        /// <summary>
        /// 创建简单胶囊按钮 (来自 LocalMusicImportButton)
        /// </summary>
        /// <param name="parent">父 Transform</param>
        /// <param name="text">按钮文字</param>
        /// <param name="width">按钮宽度 (null 保持原始宽度)</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>创建的按钮包装类</returns>
        public static SimpleCapsuleButton CreateSimpleCapsuleButton(
            Transform parent,
            string text = "Button",
            float? width = null,
            System.Action onClick = null)
        {
            if (SimpleCapsuleButtonPrefab == null)
            {
                Plugin.Log.LogError("SimpleCapsuleButtonPrefab not cached! Make sure MusicUI.Setup has been called and LocalMusicImportButton exists in scene.");
                return null;
            }
            
            // 克隆 Prefab
            var go = Object.Instantiate(SimpleCapsuleButtonPrefab, parent);
            go.name = $"SimpleCapsuleButton_{text}";
            go.SetActive(true);
            
            // 创建包装类
            var wrapper = new SimpleCapsuleButton(go);
            
            // 设置文字
            wrapper.SetText(text);
            
            // 设置宽度
            if (width.HasValue)
            {
                wrapper.SetWidth(width.Value);
            }
            
            // 设置点击事件
            if (onClick != null)
            {
                wrapper.OnClick(onClick);
            }
            
            // 初始化组件
            wrapper.Setup();
            
            return wrapper;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 简单矩形按钮的包装类，提供便捷的 API
    /// </summary>
    public class SimpleRectButton
    {
        public GameObject GameObject { get; }
        public RectTransform RectTransform { get; }
        public Button Button { get; }
        public TMP_Text Text { get; }
        public InteractableUI InteractableUI { get; }
        public HoldButtonAnimation HoldAnimation { get; }
        
        /// <summary>
        /// ActiveImage 子对象
        /// </summary>
        public Image ActiveImage { get; }
        
        public SimpleRectButton(GameObject go)
        {
            GameObject = go;
            RectTransform = go.GetComponent<RectTransform>();
            Button = go.GetComponent<Button>();
            InteractableUI = go.GetComponent<InteractableUI>();
            HoldAnimation = go.GetComponent<HoldButtonAnimation>();
            
            // 获取 _text 字段 (私有)
            var settingInitButton = go.GetComponent<SettingInitButton>();
            if (settingInitButton != null)
            {
                Text = Traverse.Create(settingInitButton)
                    .Field("_text")
                    .GetValue<TMP_Text>();
            }
            
            // 如果没有 SettingInitButton，尝试直接查找 TMP_Text
            if (Text == null)
            {
                Text = go.GetComponentInChildren<TMP_Text>();
            }
            
            // 查找 ActiveImage
            var activeImageTransform = go.transform.Find("ActiveImage");
            if (activeImageTransform != null)
            {
                ActiveImage = activeImageTransform.GetComponent<Image>();
            }
        }
        
        #region API Methods
        
        /// <summary>
        /// 设置按钮文字
        /// </summary>
        public SimpleRectButton SetText(string text)
        {
            if (Text != null)
            {
                Text.text = text;
            }
            return this;
        }
        
        /// <summary>
        /// 设置按钮宽度（保持文字居中）
        /// </summary>
        public SimpleRectButton SetWidth(float width)
        {
            if (RectTransform != null)
            {
                var size = RectTransform.sizeDelta;
                size.x = width;
                RectTransform.sizeDelta = size;
            }
            return this;
        }
        
        /// <summary>
        /// 设置按钮高度
        /// </summary>
        public SimpleRectButton SetHeight(float height)
        {
            if (RectTransform != null)
            {
                var size = RectTransform.sizeDelta;
                size.y = height;
                RectTransform.sizeDelta = size;
            }
            return this;
        }
        
        /// <summary>
        /// 设置按钮大小
        /// </summary>
        public SimpleRectButton SetSize(float width, float height)
        {
            if (RectTransform != null)
            {
                RectTransform.sizeDelta = new Vector2(width, height);
            }
            return this;
        }
        
        /// <summary>
        /// 设置位置 (anchoredPosition)
        /// </summary>
        public SimpleRectButton SetPosition(float x, float y)
        {
            if (RectTransform != null)
            {
                RectTransform.anchoredPosition = new Vector2(x, y);
            }
            return this;
        }
        
        /// <summary>
        /// 添加点击事件
        /// </summary>
        public SimpleRectButton OnClick(System.Action callback)
        {
            if (Button != null && callback != null)
            {
                Button.onClick.AddListener(() => callback());
            }
            return this;
        }
        
        /// <summary>
        /// 清除所有点击事件
        /// </summary>
        public SimpleRectButton ClearClickListeners()
        {
            if (Button != null)
            {
                Button.onClick.RemoveAllListeners();
            }
            return this;
        }
        
        /// <summary>
        /// 启用按钮
        /// </summary>
        public SimpleRectButton Enable()
        {
            var settingInitButton = GameObject.GetComponent<SettingInitButton>();
            if (settingInitButton != null)
            {
                settingInitButton.Activate();
            }
            else if (Button != null)
            {
                Button.interactable = true;
            }
            return this;
        }
        
        /// <summary>
        /// 禁用按钮
        /// </summary>
        public SimpleRectButton Disable()
        {
            var settingInitButton = GameObject.GetComponent<SettingInitButton>();
            if (settingInitButton != null)
            {
                settingInitButton.Deactivate();
            }
            else if (Button != null)
            {
                Button.interactable = false;
            }
            return this;
        }
        
        /// <summary>
        /// 设置字体颜色
        /// </summary>
        public SimpleRectButton SetTextColor(Color color)
        {
            if (Text != null)
            {
                Text.color = color;
            }
            return this;
        }
        
        /// <summary>
        /// 设置字体大小
        /// </summary>
        public SimpleRectButton SetFontSize(float size)
        {
            if (Text != null)
            {
                Text.fontSize = size;
            }
            return this;
        }
        
        /// <summary>
        /// 显示按钮
        /// </summary>
        public SimpleRectButton Show()
        {
            GameObject.SetActive(true);
            return this;
        }
        
        /// <summary>
        /// 隐藏按钮
        /// </summary>
        public SimpleRectButton Hide()
        {
            GameObject.SetActive(false);
            return this;
        }
        
        /// <summary>
        /// 销毁按钮
        /// </summary>
        public void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 简单胶囊按钮的包装类 (来自 LocalMusicImportButton)
    /// 结构: MusicTitleText, icon, baseActiveImage
    /// </summary>
    public class SimpleCapsuleButton
    {
        public GameObject GameObject { get; }
        public RectTransform RectTransform { get; }
        public Button Button { get; }
        public InteractableUI InteractableUI { get; }
        public HoldButtonAnimation HoldAnimation { get; }
        
        /// <summary>
        /// 文字组件 (MusicTitleText)
        /// </summary>
        public TMP_Text TitleText { get; }
        
        /// <summary>
        /// 图标组件 (icon)
        /// </summary>
        public Image Icon { get; }
        
        /// <summary>
        /// 激活状态图片 (baseActiveImage)
        /// </summary>
        public Image BaseActiveImage { get; }
        
        public SimpleCapsuleButton(GameObject go)
        {
            GameObject = go;
            RectTransform = go.GetComponent<RectTransform>();
            Button = go.GetComponent<Button>();
            InteractableUI = go.GetComponent<InteractableUI>();
            HoldAnimation = go.GetComponent<HoldButtonAnimation>();
            
            // 查找 MusicTitleText
            var titleTextTransform = go.transform.Find("MusicTitleText");
            if (titleTextTransform != null)
            {
                TitleText = titleTextTransform.GetComponent<TMP_Text>();
            }
            
            // 如果没找到，尝试直接查找 TMP_Text
            if (TitleText == null)
            {
                TitleText = go.GetComponentInChildren<TMP_Text>();
            }
            
            // 查找 icon
            var iconTransform = go.transform.Find("icon");
            if (iconTransform != null)
            {
                Icon = iconTransform.GetComponent<Image>();
            }
            
            // 查找 baseActiveImage
            var activeImageTransform = go.transform.Find("baseActiveImage");
            if (activeImageTransform != null)
            {
                BaseActiveImage = activeImageTransform.GetComponent<Image>();
            }
        }
        
        #region Initialization
        
        /// <summary>
        /// 初始化按钮组件
        /// </summary>
        public SimpleCapsuleButton Setup()
        {
            // 初始化 InteractableUI
            if (InteractableUI != null)
            {
                InteractableUI.Setup();
            }
            
            // 初始化 HoldButtonAnimation
            if (HoldAnimation != null)
            {
                HoldAnimation.Setup();
            }
            
            return this;
        }
        
        #endregion
        
        #region API Methods
        
        /// <summary>
        /// 设置按钮文字
        /// </summary>
        public SimpleCapsuleButton SetText(string text)
        {
            if (TitleText != null)
            {
                TitleText.text = text;
            }
            return this;
        }
        
        /// <summary>
        /// 获取当前文字
        /// </summary>
        public string GetText()
        {
            return TitleText?.text ?? string.Empty;
        }
        
        /// <summary>
        /// 设置按钮宽度（保持文字显示）
        /// </summary>
        public SimpleCapsuleButton SetWidth(float width)
        {
            if (RectTransform != null)
            {
                var size = RectTransform.sizeDelta;
                size.x = width;
                RectTransform.sizeDelta = size;
            }
            return this;
        }
        
        /// <summary>
        /// 设置按钮高度
        /// </summary>
        public SimpleCapsuleButton SetHeight(float height)
        {
            if (RectTransform != null)
            {
                var size = RectTransform.sizeDelta;
                size.y = height;
                RectTransform.sizeDelta = size;
            }
            return this;
        }
        
        /// <summary>
        /// 设置按钮大小
        /// </summary>
        public SimpleCapsuleButton SetSize(float width, float height)
        {
            if (RectTransform != null)
            {
                RectTransform.sizeDelta = new Vector2(width, height);
            }
            return this;
        }
        
        /// <summary>
        /// 设置位置 (anchoredPosition)
        /// </summary>
        public SimpleCapsuleButton SetPosition(float x, float y)
        {
            if (RectTransform != null)
            {
                RectTransform.anchoredPosition = new Vector2(x, y);
            }
            return this;
        }
        
        /// <summary>
        /// 添加点击事件
        /// </summary>
        public SimpleCapsuleButton OnClick(System.Action callback)
        {
            if (Button != null && callback != null)
            {
                Button.onClick.AddListener(() => callback());
            }
            return this;
        }
        
        /// <summary>
        /// 清除所有点击事件
        /// </summary>
        public SimpleCapsuleButton ClearClickListeners()
        {
            if (Button != null)
            {
                Button.onClick.RemoveAllListeners();
            }
            return this;
        }
        
        /// <summary>
        /// 启用按钮
        /// </summary>
        public SimpleCapsuleButton Enable()
        {
            if (Button != null)
            {
                Button.interactable = true;
            }
            if (InteractableUI != null)
            {
                InteractableUI.enabled = true;
            }
            return this;
        }
        
        /// <summary>
        /// 禁用按钮
        /// </summary>
        public SimpleCapsuleButton Disable()
        {
            if (Button != null)
            {
                Button.interactable = false;
            }
            if (InteractableUI != null)
            {
                InteractableUI.enabled = false;
                InteractableUI.DeactivateAllUI(false);
            }
            return this;
        }
        
        /// <summary>
        /// 设置文字颜色
        /// </summary>
        public SimpleCapsuleButton SetTextColor(Color color)
        {
            if (TitleText != null)
            {
                TitleText.color = color;
            }
            return this;
        }
        
        /// <summary>
        /// 设置字体大小
        /// </summary>
        public SimpleCapsuleButton SetFontSize(float size)
        {
            if (TitleText != null)
            {
                TitleText.fontSize = size;
            }
            return this;
        }
        
        /// <summary>
        /// 设置图标
        /// </summary>
        public SimpleCapsuleButton SetIcon(Sprite sprite)
        {
            if (Icon != null)
            {
                Icon.sprite = sprite;
            }
            return this;
        }
        
        /// <summary>
        /// 显示/隐藏图标
        /// </summary>
        public SimpleCapsuleButton SetIconVisible(bool visible)
        {
            if (Icon != null)
            {
                Icon.gameObject.SetActive(visible);
            }
            return this;
        }
        
        /// <summary>
        /// 显示按钮
        /// </summary>
        public SimpleCapsuleButton Show()
        {
            GameObject.SetActive(true);
            return this;
        }
        
        /// <summary>
        /// 隐藏按钮
        /// </summary>
        public SimpleCapsuleButton Hide()
        {
            GameObject.SetActive(false);
            return this;
        }
        
        /// <summary>
        /// 销毁按钮
        /// </summary>
        public void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
            }
        }
        
        #endregion
    }
}
