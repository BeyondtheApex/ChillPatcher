using System;
using System.Collections.Generic;
using System.Linq;
using Bulbul;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.SDK.Models;
using ChillPatcher.UIFramework.Music;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChillPatcher.Patches.UIFramework
{
    /// <summary>
    /// MusicTagListUI补丁：隐藏空Tag + 自定义Tag + 队列操作按钮
    /// </summary>
    [HarmonyPatch(typeof(MusicTagListUI))]
    public class MusicTagListUI_Patches
    {
        private static List<GameObject> _customTagButtons = new List<GameObject>();
        
        // 队列操作按钮
        private static GameObject _clearAllQueueButton;
        private static GameObject _clearFutureQueueButton;
        private static GameObject _clearHistoryButton;
        
        // TodoSwitchFinishButton 缓存
        private static GameObject _todoSwitchFinishButton;
        private static bool _todoSwitchFinishButtonWasActive = false;
        
        // 缓存的原始状态
        private static MusicTagListUI _cachedTagListUI;
        private static bool _isQueueMode = false;
        
        // 下拉框滚动支持
        private static ScrollRect _dropdownScrollRect;
        private static GameObject _scrollViewport;
        private const int MAX_VISIBLE_BUTTONS = 6;
        private const float BUTTON_HEIGHT = 45f;
        private const float SCROLL_PADDING_TOP = 70f;
        private const float SCROLL_PADDING_BOTTOM = 15f;
        
        // 保存 TagList 原始 RectTransform 状态，以便移出 viewport 时恢复
        private static bool _originalRectStored;
        private static Vector2 _originalAnchorMin;
        private static Vector2 _originalAnchorMax;
        private static Vector2 _originalPivot;
        private static Vector2 _originalOffsetMin;
        private static Vector2 _originalOffsetMax;
        private static Vector2 _originalAnchoredPosition;

        /// <summary>
        /// Setup后处理：隐藏空Tag + 添加自定义Tag按钮
        /// </summary>
        [HarmonyPatch("Setup")]
        [HarmonyPostfix]
        static void Setup_Postfix(MusicTagListUI __instance)
        {
            try
            {
                // 检查是否有模块注册了标签
                bool hasModuleTags = TagRegistry.Instance?.GetAllTags()?.Count > 0;
                
                // 1. 隐藏空Tag功能
                if (PluginConfig.HideEmptyTags.Value)
                {
                    HideEmptyTags(__instance);
                }

                // 2. 添加自定义Tag按钮（如果有模块注册了标签）
                if (hasModuleTags)
                {
                    AddCustomTagButtons(__instance);
                }

                // 3. 更新下拉框高度
                if (PluginConfig.HideEmptyTags.Value || hasModuleTags)
                {
                    UpdateDropdownHeight(__instance);
                }
                
                // 4. 打印按钮高度调试信息
                DebugPrintButtonHeights(__instance);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in MusicTagListUI_Patches.Setup_Postfix: {ex}");
            }
        }
        
        /// <summary>
        /// 刷新自定义 Tag 按钮（供模块在运行时添加 Tag 后调用）
        /// </summary>
        public static void RefreshCustomTagButtons()
        {
            try
            {
                var tagListUI = UnityEngine.Object.FindObjectOfType<MusicTagListUI>();
                if (tagListUI == null)
                {
                    Plugin.Log.LogWarning("[RefreshCustomTagButtons] Cannot find MusicTagListUI");
                    return;
                }
                
                // 检查是否有模块注册了标签
                bool hasModuleTags = TagRegistry.Instance?.GetAllTags()?.Count > 0;
                if (!hasModuleTags)
                {
                    Plugin.Log.LogDebug("[RefreshCustomTagButtons] No module tags registered");
                    return;
                }
                
                // 重新添加自定义 Tag 按钮
                AddCustomTagButtons(tagListUI);
                
                // 更新下拉框高度
                UpdateDropdownHeight(tagListUI);
                
                Plugin.Log.LogInfo("[RefreshCustomTagButtons] Custom tag buttons refreshed");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in RefreshCustomTagButtons: {ex}");
            }
        }
        
        /// <summary>
        /// 打印按钮高度调试信息（延迟执行以确保布局已更新）
        /// </summary>
        private static void DebugPrintButtonHeights(MusicTagListUI tagListUI)
        {
            // 延迟执行
            DebugPrintButtonHeightsAsync(tagListUI).Forget();
        }
        
        private static async UniTaskVoid DebugPrintButtonHeightsAsync(MusicTagListUI tagListUI)
        {
            // 等待2帧让布局更新
            await UniTask.DelayFrame(2);
            
            try
            {
                var pulldown = Traverse.Create(tagListUI).Field("_pulldown").GetValue<PulldownListUI>();
                if (pulldown == null) return;
                
                var pullDownParentRect = Traverse.Create(pulldown).Field("_pullDownParentRect").GetValue<RectTransform>();
                if (pullDownParentRect == null) return;
                
                var tagListContainer = pullDownParentRect.Find("TagList");
                if (tagListContainer == null && _scrollViewport != null)
                    tagListContainer = _scrollViewport.transform.Find("TagList");
                if (tagListContainer == null) return;
                
                // 强制刷新布局
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(tagListContainer as RectTransform);
                
                var layout = tagListContainer.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    Plugin.Log.LogInfo($"[DebugLayout] VerticalLayoutGroup: spacing={layout.spacing}, padding=(T:{layout.padding.top}, B:{layout.padding.bottom}, L:{layout.padding.left}, R:{layout.padding.right})");
                }
                
                // 使用ContentSizeFitter的preferredSize
                var contentSizeFitter = tagListContainer.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    var layoutElement = tagListContainer.GetComponent<LayoutElement>();
                    Plugin.Log.LogInfo($"[DebugLayout] ContentSizeFitter found, mode: H={contentSizeFitter.horizontalFit}, V={contentSizeFitter.verticalFit}");
                }
                
                var buttons = Traverse.Create(tagListUI).Field("buttons").GetValue<MusicTagListButton[]>();
                if (buttons != null && buttons.Length >= 2)
                {
                    // 统计可见按钮
                    var visibleButtons = buttons.Where(b => b != null && b.gameObject.activeSelf).ToArray();
                    Plugin.Log.LogInfo($"[DebugLayout] Total buttons: {buttons.Length}, Visible: {visibleButtons.Length}");
                    
                    if (visibleButtons.Length >= 2)
                    {
                        var btn1 = visibleButtons[0];
                        var btn2 = visibleButtons[1];
                        
                        var rect1 = btn1.GetComponent<RectTransform>();
                        var rect2 = btn2.GetComponent<RectTransform>();
                        
                        if (rect1 != null && rect2 != null)
                        {
                            Plugin.Log.LogInfo($"[DebugLayout] Button1 '{btn1.name}': position={rect1.anchoredPosition}, size={rect1.rect.size}, localPos={rect1.localPosition}");
                            Plugin.Log.LogInfo($"[DebugLayout] Button2 '{btn2.name}': position={rect2.anchoredPosition}, size={rect2.rect.size}, localPos={rect2.localPosition}");
                            
                            // 计算实际高度差（Y坐标差值的绝对值）
                            float heightDiff = Mathf.Abs(rect1.localPosition.y - rect2.localPosition.y);
                            Plugin.Log.LogInfo($"[DebugLayout] Height difference between buttons: {heightDiff}");
                            
                            // 计算真实按钮高度（包含spacing）
                            float buttonHeight = rect1.rect.height;
                            Plugin.Log.LogInfo($"[DebugLayout] Single button height: {buttonHeight}");
                            Plugin.Log.LogInfo($"[DebugLayout] Effective row height (button + spacing): {heightDiff}");
                        }
                    }
                }
                
                // 打印TagList容器的实际大小
                var tagListRect = tagListContainer as RectTransform;
                if (tagListRect != null)
                {
                    Plugin.Log.LogInfo($"[DebugLayout] TagList container size: {tagListRect.rect.size}, sizeDelta: {tagListRect.sizeDelta}");
                }
                
                // 打印原始下拉框高度设置
                float openHeight = Traverse.Create(pulldown).Field("_openPullDownSizeDeltaY").GetValue<float>();
                Plugin.Log.LogInfo($"[DebugLayout] Original _openPullDownSizeDeltaY: {openHeight}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[DebugLayout] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 隐藏没有歌曲的Tag按钮
        /// </summary>
        private static void HideEmptyTags(MusicTagListUI tagListUI)
        {
            // 获取MusicService引用
            var musicService = Traverse.Create(tagListUI)
                .Field("musicService")
                .GetValue<MusicService>();

            if (musicService == null)
                return;

            // 获取所有Tag按钮
            var buttons = Traverse.Create(tagListUI)
                .Field("buttons")
                .GetValue<MusicTagListButton[]>();

            if (buttons == null || buttons.Length == 0)
                return;

            // 获取所有歌曲列表
            var allMusicList = musicService.AllMusicList;
            if (allMusicList == null)
                return;

            // 检查每个Tag按钮
            foreach (var button in buttons)
            {
                var tag = button.Tag;

                // 跳过All（总是显示）
                if (tag == AudioTag.All)
                    continue;

                // 检查是否有歌曲属于这个Tag
                bool hasMusic = allMusicList.Any(audio => audio.Tag.HasFlagFast(tag));

                // 如果没有歌曲，隐藏这个按钮
                if (!hasMusic)
                {
                    button.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 添加自定义Tag按钮
        /// </summary>
        private static void AddCustomTagButtons(MusicTagListUI tagListUI)
        {
            // 清除旧的自定义按钮
            foreach (var btn in _customTagButtons)
            {
                if (btn != null)
                    UnityEngine.Object.Destroy(btn);
            }
            _customTagButtons.Clear();

            // 获取按钮容器
            var buttons = Traverse.Create(tagListUI).Field("buttons").GetValue<MusicTagListButton[]>();
            if (buttons == null || buttons.Length == 0)
                return;

            // ✅ 获取MusicService以便同步按钮状态
            var musicService = Traverse.Create(tagListUI).Field("musicService").GetValue<MusicService>();
            if (musicService == null)
            {
                Plugin.Log.LogWarning("[AddCustomTagButtons] MusicService is null, cannot sync button states");
                return;
            }

            // 获取TagList容器（父物体）
            var firstButton = buttons[0];
            var container = firstButton.transform.parent;

            // 获取按钮预制体（克隆第一个按钮）
            var buttonPrefab = firstButton.gameObject;

            // 添加自定义Tag按钮
            var customTags = TagRegistry.Instance?.GetAllTags() ?? new List<TagInfo>();
            foreach (var customTag in customTags)
            {
                // 克隆按钮
                var newButtonObj = UnityEngine.Object.Instantiate(buttonPrefab, container);
                newButtonObj.name = $"CustomTag_{customTag.TagId}";

                var newButton = newButtonObj.GetComponent<MusicTagListButton>();
                if (newButton != null)
                {
                    // ✅ 设置按钮Tag为实际的位值
                    Traverse.Create(newButton).Field("Tag").SetValue((AudioTag)customTag.BitValue);

                    // ✅ 找到Buttons/TagName子物体并替换为纯Text
                    var buttonsContainer = newButtonObj.transform.Find("Buttons");
                    if (buttonsContainer != null)
                    {
                        // 找到原来的TagName
                        var oldTagName = buttonsContainer.Find("TagName");
                        if (oldTagName != null)
                        {
                            // 保存布局和样式信息
                            var oldRect = oldTagName.GetComponent<RectTransform>();
                            var oldText = oldTagName.GetComponent<TMPro.TMP_Text>();
                            
                            // 记录位置信息
                            Vector2 anchorMin = oldRect.anchorMin;
                            Vector2 anchorMax = oldRect.anchorMax;
                            Vector2 anchoredPosition = oldRect.anchoredPosition;
                            Vector2 sizeDelta = oldRect.sizeDelta;
                            Vector2 pivot = oldRect.pivot;
                            Vector3 localScale = oldRect.localScale;
                            
                            // 记录文本样式
                            TMPro.TMP_FontAsset font = oldText.font;
                            float fontSize = oldText.fontSize;
                            Color color = oldText.color;
                            TMPro.TextAlignmentOptions alignment = oldText.alignment;
                            bool enableAutoSizing = oldText.enableAutoSizing;
                            float fontSizeMin = oldText.fontSizeMin;
                            float fontSizeMax = oldText.fontSizeMax;
                            bool raycastTarget = oldText.raycastTarget;
                            
                        // 销毁旧的TagName（带本地化组件）
                        UnityEngine.Object.Destroy(oldTagName.gameObject);                            // 创建新的TagName（不带本地化组件）
                            var newTagName = new GameObject("TagName");
                            newTagName.transform.SetParent(buttonsContainer, false);
                            
                            // 复制RectTransform
                            var newRect = newTagName.AddComponent<RectTransform>();
                            newRect.anchorMin = anchorMin;
                            newRect.anchorMax = anchorMax;
                            newRect.anchoredPosition = anchoredPosition;
                            newRect.sizeDelta = sizeDelta;
                            newRect.pivot = pivot;
                            newRect.localScale = localScale;
                            
                            // 添加TMP_Text（复制样式但不添加本地化组件）
                            var newText = newTagName.AddComponent<TMPro.TextMeshProUGUI>();
                            newText.text = customTag.DisplayName;  // ← 设置自定义文本
                            newText.font = font;
                            newText.fontSize = fontSize;
                            newText.color = color;
                            newText.alignment = alignment;
                            newText.enableAutoSizing = enableAutoSizing;
                            newText.fontSizeMin = fontSizeMin;
                            newText.fontSizeMax = fontSizeMax;
                            newText.raycastTarget = raycastTarget;
                            
                            // 保存到MusicTagListButton的_text字段
                            Traverse.Create(newButton).Field("_text").SetValue(newText);
                            
                            Plugin.Log.LogInfo($"[CustomTag] Created pure text button: {customTag.DisplayName}");
                        }
                    }

                    // ✅ 设置点击事件（直接操作MusicService.CurrentAudioTag）
                    SetupCustomTagButton(newButton, customTag, tagListUI);
                    
                    // ✅ 同步按钮初始状态 (根据CurrentAudioTag是否包含该位)
                    var currentTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value;
                    bool isActive = currentTag.HasFlagFast((AudioTag)customTag.BitValue);
                    newButton.SetCheck(isActive);
                    Plugin.Log.LogDebug($"[CustomTag] Button '{customTag.DisplayName}' initial state: {(isActive ? "Checked" : "Unchecked")} (CurrentTag: {currentTag})");

                    _customTagButtons.Add(newButtonObj);
                }
            }

            // ✅ 添加完所有自定义Tag后，强制刷新容器布局
            if (_customTagButtons.Count > 0 && container != null)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container as UnityEngine.RectTransform);
                Plugin.Log.LogInfo($"[AddCustomTagButtons] Added {_customTagButtons.Count} custom tag buttons, forced layout rebuild");
            }
        }

        /// <summary>
        /// 设置自定义Tag按钮点击事件
        /// ✅ 直接操作MusicService.CurrentAudioTag，完全复用游戏筛选逻辑
        /// </summary>
        private static void SetupCustomTagButton(MusicTagListButton button, TagInfo customTag, MusicTagListUI tagListUI)
        {
            var musicService = Traverse.Create(tagListUI).Field("musicService").GetValue<MusicService>();
            if (musicService == null)
                return;

            // 订阅按钮点击
            button.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
            {
                // 获取当前Tag状态
                var currentTag = SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value;
                bool hasTag = currentTag.HasFlagFast((AudioTag)customTag.BitValue);

                // ✅ 处理增长列表的互斥逻辑
                if (customTag.IsGrowableList)
                {
                    HandleGrowableTagClick(customTag, hasTag, currentTag, tagListUI);
                }
                else
                {
                    // 普通 Tag：使用位运算切换
                    if (hasTag)
                    {
                        SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value = currentTag.RemoveFlag((AudioTag)customTag.BitValue);
                        Plugin.Log.LogInfo($"[CustomTag] Removed: {customTag.DisplayName} ({customTag.BitValue})");
                    }
                    else
                    {
                        SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value = currentTag.AddFlag((AudioTag)customTag.BitValue);
                        Plugin.Log.LogInfo($"[CustomTag] Added: {customTag.DisplayName} ({customTag.BitValue})");
                    }
                }

                // 更新按钮UI
                button.SetCheck(!hasTag);
                
                // ✅ 直接调用 SetTitle 更新标题显示（Publicizer 消除反射）
                tagListUI.SetTitle();
                
                // ✅ CurrentAudioTag变化会自动触发游戏的筛选逻辑！
                // 不需要手动调用ApplyFilter，游戏已经订阅了ReactiveProperty
            });
        }

        /// <summary>
        /// 处理增长列表 Tag 的点击（互斥逻辑）
        /// </summary>
        private static void HandleGrowableTagClick(TagInfo clickedTag, bool wasActive, AudioTag currentTag, MusicTagListUI tagListUI)
        {
            var tagRegistry = TagRegistry.Instance;
            if (tagRegistry == null)
                return;

            if (wasActive)
            {
                // 取消选中：移除该增长列表
                SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value = currentTag.RemoveFlag((AudioTag)clickedTag.BitValue);
                tagRegistry.SetCurrentGrowableTag(null);
                Plugin.Log.LogInfo($"[GrowableTag] Removed: {clickedTag.DisplayName}");
            }
            else
            {
                // 选中：先移除其他增长列表，再添加当前
                var newTag = currentTag;
                
                // 移除其他增长列表 Tag
                var otherGrowableTags = tagRegistry.GetGrowableTags();
                foreach (var otherTag in otherGrowableTags)
                {
                    if (otherTag.TagId != clickedTag.TagId)
                    {
                        newTag = newTag.RemoveFlag((AudioTag)otherTag.BitValue);
                        
                        // 更新其他增长列表按钮的UI状态
                        UpdateGrowableTagButtonUI(otherTag.TagId, false, tagListUI);
                    }
                }
                
                // 添加当前增长列表
                newTag = newTag.AddFlag((AudioTag)clickedTag.BitValue);
                SaveDataManager.Instance.MusicSetting.CurrentAudioTag.Value = newTag;
                tagRegistry.SetCurrentGrowableTag(clickedTag.TagId);
                
                Plugin.Log.LogInfo($"[GrowableTag] Added (exclusive): {clickedTag.DisplayName}");
            }
        }

        /// <summary>
        /// 更新指定增长列表 Tag 按钮的 UI 状态
        /// </summary>
        private static void UpdateGrowableTagButtonUI(string tagId, bool isChecked, MusicTagListUI tagListUI)
        {
            var buttonObj = _customTagButtons.FirstOrDefault(b => b.name == $"CustomTag_{tagId}");
            if (buttonObj != null)
            {
                var btn = buttonObj.GetComponent<MusicTagListButton>();
                btn?.SetCheck(isChecked);
            }
        }

        /// <summary>
        /// 更新下拉框高度（超过 MAX_VISIBLE_BUTTONS 时启用滚动）
        /// </summary>
        private static void UpdateDropdownHeight(MusicTagListUI tagListUI)
        {
            var pulldown = Traverse.Create(tagListUI)
                .Field("_pulldown")
                .GetValue<PulldownListUI>();

            if (pulldown == null)
                return;

            // 获取下拉列表的Content
            var pullDownParentRect = Traverse.Create(pulldown)
                .Field("_pullDownParentRect")
                .GetValue<RectTransform>();

            if (pullDownParentRect == null)
                return;

            // TagList 可能在 pullDownParentRect 下面或在 viewport 里
            var contentTransform = pullDownParentRect.Find("TagList");
            if (contentTransform == null && _scrollViewport != null)
                contentTransform = _scrollViewport.transform.Find("TagList");
            if (contentTransform == null)
                return;

            // 统计实际显示的按钮数量
            int visibleNativeButtonCount = 0;
            var nativeButtons = Traverse.Create(tagListUI).Field("buttons").GetValue<MusicTagListButton[]>();
            if (nativeButtons != null)
            {
                foreach (var btn in nativeButtons)
                {
                    if (btn != null && btn.gameObject.activeSelf)
                        visibleNativeButtonCount++;
                }
            }
            
            int customButtonCount = _customTagButtons.Count;
            int totalVisibleButtonCount = visibleNativeButtonCount + customButtonCount;
            
            Plugin.Log.LogInfo($"[UpdateDropdownHeight] Native (visible): {visibleNativeButtonCount}, Custom: {customButtonCount}, Total: {totalVisibleButtonCount}");

            // 强制刷新布局
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(pullDownParentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform as RectTransform);

            float a = PluginConfig.TagDropdownHeightMultiplier.Value;
            float b = PluginConfig.TagDropdownHeightOffset.Value;

            // 超过 MAX_VISIBLE_BUTTONS 时，限制高度并启用滚动
            bool needsScroll = totalVisibleButtonCount > MAX_VISIBLE_BUTTONS;
            float finalHeight;

            if (needsScroll)
            {
                // 滚动模式：高度 = 上padding + 可见按钮区 + 下padding
                finalHeight = SCROLL_PADDING_TOP + (MAX_VISIBLE_BUTTONS * BUTTON_HEIGHT) + SCROLL_PADDING_BOTTOM;
                
                EnsureScrollRect(pullDownParentRect, contentTransform as RectTransform);
                
                // 设置 TagList 的高度为实际内容高度（让 ScrollRect 知道可滚动范围）
                var contentRect = contentTransform as RectTransform;
                if (contentRect != null)
                {
                    // 确保 ContentSizeFitter 在垂直方向为 PreferredSize
                    var fitter = contentRect.GetComponent<ContentSizeFitter>();
                    if (fitter == null)
                    {
                        fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
                
                Plugin.Log.LogInfo($"[UpdateDropdownHeight] Scroll enabled: {totalVisibleButtonCount} buttons, showing {MAX_VISIBLE_BUTTONS}");
            }
            else
            {
                // 不需要滚动：高度 = a × (按钮数 × 按钮高) + b
                finalHeight = a * (totalVisibleButtonCount * BUTTON_HEIGHT) + b;
                
                // 禁用 ScrollRect 并把 TagList 移回原父级
                if (_dropdownScrollRect != null)
                    _dropdownScrollRect.enabled = false;
                
                var contentRect = contentTransform as RectTransform;
                if (contentRect != null && _scrollViewport != null && contentRect.parent == _scrollViewport.transform)
                {
                    contentRect.SetParent(pullDownParentRect, false);
                    RestoreContentRectState(contentRect);
                    Plugin.Log.LogInfo("[UpdateDropdownHeight] Moved TagList back to pulldown parent (no scroll needed)");
                }
            }

            Traverse.Create(pulldown).Field("_openPullDownSizeDeltaY").SetValue(finalHeight);
            Plugin.Log.LogInfo($"Tag dropdown height: {finalHeight:F1} (scroll={needsScroll}, buttons={totalVisibleButtonCount})");
        }
        
        /// <summary>
        /// 确保下拉框有 ScrollRect 和 Viewport 来支持滚动
        /// 使用 Viewport 对象实现正确的上下 padding 裁剪
        /// </summary>
        private static void EnsureScrollRect(RectTransform pullDownParentRect, RectTransform contentRect)
        {
            if (pullDownParentRect == null || contentRect == null)
                return;
            
            // 创建 Viewport（如果还没有）
            if (_scrollViewport == null || _scrollViewport.transform.parent != pullDownParentRect)
            {
                // 检查是否已有旧的 viewport
                var existingViewport = pullDownParentRect.Find("ChillPatcher_ScrollViewport");
                if (existingViewport != null)
                {
                    _scrollViewport = existingViewport.gameObject;
                }
                else
                {
                    _scrollViewport = new GameObject("ChillPatcher_ScrollViewport");
                    _scrollViewport.transform.SetParent(pullDownParentRect, false);
                    
                    // 添加 RectMask2D 到 viewport（裁剪溢出内容）
                    _scrollViewport.AddComponent<RectMask2D>();
                    
                    Plugin.Log.LogInfo("[EnsureScrollRect] Created scroll viewport with RectMask2D");
                }
            }
            
            // 设置 Viewport 的 RectTransform，留出上下 padding
            var viewportRect = _scrollViewport.GetComponent<RectTransform>();
            if (viewportRect == null)
                viewportRect = _scrollViewport.AddComponent<RectTransform>();
            
            // Viewport 拉伸填满父容器，但留出上下间距
            viewportRect.anchorMin = new Vector2(0, 0);
            viewportRect.anchorMax = new Vector2(1, 1);
            viewportRect.pivot = new Vector2(0.5f, 1);
            viewportRect.offsetMin = new Vector2(0, SCROLL_PADDING_BOTTOM);  // 底部留 5px
            viewportRect.offsetMax = new Vector2(0, -SCROLL_PADDING_TOP);     // 顶部留 100px
            
            // 将 TagList 移入 Viewport（如果还没有移入）
            if (contentRect.parent != viewportRect)
            {
                contentRect.SetParent(viewportRect, false);
            }
            
            // 保存原始 RectTransform 状态（用于之后恢复）
            if (!_originalRectStored)
            {
                _originalAnchorMin = contentRect.anchorMin;
                _originalAnchorMax = contentRect.anchorMax;
                _originalPivot = contentRect.pivot;
                _originalOffsetMin = contentRect.offsetMin;
                _originalOffsetMax = contentRect.offsetMax;
                _originalAnchoredPosition = contentRect.anchoredPosition;
                _originalRectStored = true;
                Plugin.Log.LogInfo($"[EnsureScrollRect] Saved original rect: anchors=({_originalAnchorMin},{_originalAnchorMax}), pivot={_originalPivot}, offset=({_originalOffsetMin},{_originalOffsetMax}), pos={_originalAnchoredPosition}");
            }
            
            // 确保内容的锚点和枢轴正确（顶部对齐，ScrollRect 需要）
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            // 确保 Viewport 顺序在 TagList 的同级或之前（避免渲染层级问题）
            _scrollViewport.transform.SetAsFirstSibling();
            
            // 添加或获取 ScrollRect（放在 _pullDownParentRect 上）
            _dropdownScrollRect = pullDownParentRect.GetComponent<ScrollRect>();
            if (_dropdownScrollRect == null)
            {
                _dropdownScrollRect = pullDownParentRect.gameObject.AddComponent<ScrollRect>();
                Plugin.Log.LogInfo("[EnsureScrollRect] Added ScrollRect");
            }
            
            _dropdownScrollRect.content = contentRect;
            _dropdownScrollRect.viewport = viewportRect;
            _dropdownScrollRect.horizontal = false;
            _dropdownScrollRect.vertical = true;
            _dropdownScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _dropdownScrollRect.scrollSensitivity = BUTTON_HEIGHT;
            _dropdownScrollRect.enabled = true;
        }
        
        /// <summary>
        /// 恢复 TagList 的原始 RectTransform 状态
        /// </summary>
        private static void RestoreContentRectState(RectTransform contentRect)
        {
            if (contentRect == null || !_originalRectStored) return;
            
            contentRect.anchorMin = _originalAnchorMin;
            contentRect.anchorMax = _originalAnchorMax;
            contentRect.pivot = _originalPivot;
            contentRect.offsetMin = _originalOffsetMin;
            contentRect.offsetMax = _originalOffsetMax;
            contentRect.anchoredPosition = _originalAnchoredPosition;
        }
        
        #region 队列模式切换
        
        /// <summary>
        /// 切换到队列模式 - 隐藏Tag显示队列操作按钮
        /// </summary>
        public static void SwitchToQueueMode()
        {
            if (_isQueueMode) return;
            _isQueueMode = true;
            
            var tagListUI = UnityEngine.Object.FindObjectOfType<MusicTagListUI>();
            if (tagListUI == null)
            {
                Plugin.Log.LogWarning("[TagListUI] Cannot find MusicTagListUI");
                return;
            }
            
            _cachedTagListUI = tagListUI;
            
            // 获取下拉框
            var pulldown = Traverse.Create(tagListUI).Field("_pulldown").GetValue<PulldownListUI>();
            if (pulldown == null) return;
            
            // 关闭下拉框（如果打开的话）
            pulldown.ClosePullDown(true);
            
            // 更改标题为"队列动作"
            pulldown.ChangeSelectContentText("队列动作");
            
            // 获取Tag按钮容器
            var pullDownParentRect = Traverse.Create(pulldown).Field("_pullDownParentRect").GetValue<RectTransform>();
            if (pullDownParentRect == null) return;
            
            var tagListContainer = pullDownParentRect.Find("TagList");
            if (tagListContainer == null && _scrollViewport != null)
                tagListContainer = _scrollViewport.transform.Find("TagList");
            if (tagListContainer == null) return;
            
            // 队列模式不需要滚动，把 TagList 移回原父级（如果在 viewport 里）
            if (_scrollViewport != null && tagListContainer.parent == _scrollViewport.transform)
            {
                tagListContainer.SetParent(pullDownParentRect, false);
                RestoreContentRectState(tagListContainer as RectTransform);
                Plugin.Log.LogInfo("[SwitchToQueueMode] Moved TagList back to pulldown parent");
            }
            if (_dropdownScrollRect != null)
                _dropdownScrollRect.enabled = false;
            
            // 隐藏 TodoSwitchFinishButton
            HideTodoSwitchFinishButton(tagListContainer);
            
            // 隐藏所有原生Tag按钮
            var buttons = Traverse.Create(tagListUI).Field("buttons").GetValue<MusicTagListButton[]>();
            if (buttons != null)
            {
                foreach (var btn in buttons)
                {
                    if (btn != null)
                        btn.gameObject.SetActive(false);
                }
            }
            
            // 隐藏自定义Tag按钮
            foreach (var btn in _customTagButtons)
            {
                if (btn != null)
                    btn.SetActive(false);
            }
            
            // 创建队列操作按钮（直接添加到容器）并获取按钮数量
            int queueButtonCount = CreateQueueButtons(tagListContainer);
            
            // 更新下拉框高度（根据实际创建的按钮数量）
            UpdateDropdownHeightForQueueMode(pulldown, queueButtonCount);
            
            Plugin.Log.LogInfo($"[TagListUI] Switched to queue mode with {queueButtonCount} buttons");
        }
        
        /// <summary>
        /// 切换回正常模式 - 恢复Tag显示
        /// </summary>
        public static void SwitchToNormalMode()
        {
            if (!_isQueueMode) return;
            _isQueueMode = false;
            
            var tagListUI = _cachedTagListUI ?? UnityEngine.Object.FindObjectOfType<MusicTagListUI>();
            if (tagListUI == null) return;
            
            // 获取下拉框
            var pulldown = Traverse.Create(tagListUI).Field("_pulldown").GetValue<PulldownListUI>();
            if (pulldown == null) return;
            
            // 关闭下拉框
            pulldown.ClosePullDown(true);
            
            // 销毁队列操作按钮
            if (_clearAllQueueButton != null)
            {
                UnityEngine.Object.Destroy(_clearAllQueueButton);
                _clearAllQueueButton = null;
            }
            if (_clearFutureQueueButton != null)
            {
                UnityEngine.Object.Destroy(_clearFutureQueueButton);
                _clearFutureQueueButton = null;
            }
            if (_clearHistoryButton != null)
            {
                UnityEngine.Object.Destroy(_clearHistoryButton);
                _clearHistoryButton = null;
            }
            
            // 恢复 TodoSwitchFinishButton
            ShowTodoSwitchFinishButton();
            
            // 恢复原生Tag按钮
            var buttons = Traverse.Create(tagListUI).Field("buttons").GetValue<MusicTagListButton[]>();
            if (buttons != null)
            {
                foreach (var btn in buttons)
                {
                    if (btn != null)
                        btn.gameObject.SetActive(true);
                }
            }
            
            // 恢复自定义Tag按钮
            foreach (var btn in _customTagButtons)
            {
                if (btn != null)
                    btn.SetActive(true);
            }
            
            // 重新应用隐藏空Tag
            if (PluginConfig.HideEmptyTags.Value)
            {
                HideEmptyTags(tagListUI);
            }
            
            // 恢复标题
            tagListUI.SetTitle();
            
            // 更新下拉框高度
            UpdateDropdownHeight(tagListUI);
            
            Plugin.Log.LogInfo("[TagListUI] Switched to normal mode");
        }
        
        /// <summary>
        /// 创建队列操作按钮（克隆原生Tag按钮样式）
        /// </summary>
        /// <summary>
        /// 创建队列操作按钮
        /// </summary>
        /// <returns>创建的按钮数量</returns>
        private static int CreateQueueButtons(Transform container)
        {
            int buttonCount = 0;
            
            // 如果已存在，先销毁
            if (_clearAllQueueButton != null)
            {
                UnityEngine.Object.Destroy(_clearAllQueueButton);
                _clearAllQueueButton = null;
            }
            if (_clearFutureQueueButton != null)
            {
                UnityEngine.Object.Destroy(_clearFutureQueueButton);
                _clearFutureQueueButton = null;
            }
            if (_clearHistoryButton != null)
            {
                UnityEngine.Object.Destroy(_clearHistoryButton);
                _clearHistoryButton = null;
            }
            
            // 获取原生按钮作为模板
            var buttons = Traverse.Create(_cachedTagListUI).Field("buttons").GetValue<MusicTagListButton[]>();
            if (buttons == null || buttons.Length == 0)
            {
                Plugin.Log.LogError("[CreateQueueButtons] No template button found");
                return buttonCount;
            }
            
            var templateButton = buttons[0];
            
            // 创建"清空全部队列"按钮
            _clearAllQueueButton = CreateQueueButtonFromTemplate(
                container,
                templateButton,
                "ClearAllQueue",
                "清空全部队列",
                OnClearAllQueueClicked
            );
            if (_clearAllQueueButton != null) buttonCount++;
            
            // 创建"清空未来队列"按钮
            _clearFutureQueueButton = CreateQueueButtonFromTemplate(
                container,
                templateButton,
                "ClearFutureQueue",
                "清空未来队列",
                OnClearFutureQueueClicked
            );
            if (_clearFutureQueueButton != null) buttonCount++;
            
            // 创建"清空播放历史"按钮
            _clearHistoryButton = CreateQueueButtonFromTemplate(
                container,
                templateButton,
                "ClearHistory",
                "清空播放历史",
                OnClearHistoryClicked
            );
            if (_clearHistoryButton != null) buttonCount++;
            
            // 强制刷新布局
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
            
            return buttonCount;
        }
        
        /// <summary>
        /// 从模板创建队列操作按钮（保持原生样式）
        /// </summary>
        private static GameObject CreateQueueButtonFromTemplate(
            Transform parent, 
            MusicTagListButton template, 
            string name,
            string displayText, 
            UnityEngine.Events.UnityAction onClick)
        {
            // 克隆模板按钮
            var buttonObj = UnityEngine.Object.Instantiate(template.gameObject, parent);
            buttonObj.name = name;
            buttonObj.SetActive(true);
            
            // 移除原有的MusicTagListButton行为（我们不需要Tag逻辑）
            var tagButton = buttonObj.GetComponent<MusicTagListButton>();
            if (tagButton != null)
            {
                UnityEngine.Object.Destroy(tagButton);
            }
            
            // 隐藏复选框（队列操作不需要）
            var buttonsContainer = buttonObj.transform.Find("Buttons");
            if (buttonsContainer != null)
            {
                var checkBox = buttonsContainer.Find("CheckBox");
                if (checkBox != null)
                {
                    checkBox.gameObject.SetActive(false);
                }
                
                // 查找并修改TagName文本
                var tagName = buttonsContainer.Find("TagName");
                if (tagName != null)
                {
                    // 获取TMP_Text组件
                    var tmpText = tagName.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        // 移除本地化组件（如果有）
                        var localization = tagName.GetComponent<TextLocalizationBehaviour>();
                        if (localization != null)
                        {
                            UnityEngine.Object.Destroy(localization);
                        }
                        
                        // 设置文本
                        tmpText.text = displayText;
                    }
                }
            }
            
            // 设置点击事件
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                // 清除原有监听器
                button.onClick.RemoveAllListeners();
                // 添加新的监听器
                button.onClick.AddListener(onClick);
            }
            else
            {
                // 如果没有Button组件，添加一个
                button = buttonObj.AddComponent<Button>();
                button.onClick.AddListener(onClick);
            }
            
            Plugin.Log.LogInfo($"[CreateQueueButton] Created queue button: {displayText}");
            return buttonObj;
        }
        
        /// <summary>
        /// 隐藏 TodoSwitchFinishButton
        /// </summary>
        private static void HideTodoSwitchFinishButton(Transform tagListContainer)
        {
            // 在 TagList 下搜索所有的 TagCell，找到包含 TodoSwitchFinishButton 的那个
            foreach (Transform child in tagListContainer)
            {
                if (child.name.StartsWith("TagCell"))
                {
                    var buttons = child.Find("Buttons");
                    if (buttons != null)
                    {
                        var todoButton = buttons.Find("TodoSwitchFinishButton");
                        if (todoButton != null)
                        {
                            _todoSwitchFinishButton = todoButton.gameObject;
                            _todoSwitchFinishButtonWasActive = todoButton.gameObject.activeSelf;
                            todoButton.gameObject.SetActive(false);
                            Plugin.Log.LogInfo("[TagListUI] Hidden TodoSwitchFinishButton");
                            return;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 显示 TodoSwitchFinishButton
        /// </summary>
        private static void ShowTodoSwitchFinishButton()
        {
            if (_todoSwitchFinishButton != null)
            {
                _todoSwitchFinishButton.SetActive(_todoSwitchFinishButtonWasActive);
                _todoSwitchFinishButton = null;
                Plugin.Log.LogInfo("[TagListUI] Restored TodoSwitchFinishButton");
            }
        }
        
        /// <summary>
        /// 更新队列模式下的下拉框高度（使用与正常模式相同的计算公式）
        /// </summary>
        private static void UpdateDropdownHeightForQueueMode(PulldownListUI pulldown, int buttonCount)
        {
            float a = PluginConfig.TagDropdownHeightMultiplier.Value;
            float b = PluginConfig.TagDropdownHeightOffset.Value;
            int displayCount = Math.Min(buttonCount, MAX_VISIBLE_BUTTONS);
            float finalHeight = a * (displayCount * BUTTON_HEIGHT) + b;
            
            // 队列模式按钮数通常很少，不需要滚动，禁用 ScrollRect
            if (_dropdownScrollRect != null)
                _dropdownScrollRect.enabled = false;
            
            Traverse.Create(pulldown).Field("_openPullDownSizeDeltaY").SetValue(finalHeight);
            Plugin.Log.LogInfo($"[QueueMode] Dropdown height: {a} × ({displayCount} × {BUTTON_HEIGHT}) + {b} = {finalHeight:F1}");
        }
        
        /// <summary>
        /// 清空全部队列按钮点击
        /// </summary>
        private static void OnClearAllQueueClicked()
        {
            Plugin.Log.LogInfo("[TagListUI] Clear all queue clicked");
            
            // 清空整个队列
            PlayQueueManager.Instance.Clear();
            
            // 从播放列表获取下一首开始播放
            // 这会触发 AdvanceToNext，从播放列表补充
            var musicService = Traverse.Create(_cachedTagListUI)
                .Field("musicService")
                .GetValue<MusicService>();
                
            if (musicService != null)
            {
                // 播放下一首（从播放列表位置或随机）
                musicService.SkipCurrentMusic(MusicChangeKind.Manual).Forget();
            }
            
            // 自动切换回播放列表视图
            PlayQueueButton_Patch.SwitchToPlaylist();
        }
        
        /// <summary>
        /// 清空未来队列按钮点击（保留当前播放）
        /// </summary>
        private static void OnClearFutureQueueClicked()
        {
            Plugin.Log.LogInfo("[TagListUI] Clear future queue clicked");
            
            // 只清空待播放的，保留当前播放
            PlayQueueManager.Instance.ClearPending();
            
            // 自动切换回播放列表视图
            PlayQueueButton_Patch.SwitchToPlaylist();
        }
        
        /// <summary>
        /// 清空播放历史按钮点击
        /// </summary>
        private static void OnClearHistoryClicked()
        {
            Plugin.Log.LogInfo("[TagListUI] Clear history clicked");
            
            // 清空播放历史
            PlayQueueManager.Instance.ClearHistory();
            
            // 自动切换回播放列表视图
            PlayQueueButton_Patch.SwitchToPlaylist();
            
            Plugin.Log.LogInfo("[TagListUI] Play history cleared");
        }
        
        /// <summary>
        /// 当前是否处于队列模式
        /// </summary>
        public static bool IsQueueMode => _isQueueMode;
        
        #endregion
    }
}
