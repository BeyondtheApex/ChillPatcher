using System;
using BepInEx.Logging;
using ChillPatcher.Patches;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChillPatcher
{
    /// <summary>
    /// 将键盘钩子 / RIME 输入分发到 UIToolkit TextField。
    /// 在 PlayerLoop Tick 中调用（早于 TMP_InputField.LateUpdate），
    /// 若 UIToolkit TextField 获焦则抢先消费队列，TMP patch 自动跳过。
    /// </summary>
    public static class UIToolkitInputDispatcher
    {
        private static ManualLogSource _log;

        /// <summary>当前帧是否有 UIToolkit TextField 获焦</summary>
        public static bool IsUIToolkitTextFieldFocused { get; private set; }

        /// <summary>Rime Context 变化事件（主线程触发，JSON 数据或 "null"）</summary>
        public static event Action<string, string> OnImeContextChanged;

        /// <summary>输入模式变化事件（主线程触发）</summary>
        public static event Action<bool> OnInputModeChanged;

        /// <summary>当前获焦的 TextField（供 IME API 读取位置）</summary>
        private static TextField _currentFocusedTextField;

        /// <summary>最后一次获焦 UIToolkit TextField 的面板坐标（跨帧缓存）</summary>
        private static UnityEngine.Rect? _lastTextFieldRect;

        /// <summary>UGUI TMP_InputField 屏幕坐标（Y-up 像素坐标），持久缓存直到失焦清除</summary>
        private static UnityEngine.Rect? _lastTMPScreenRect;

        /// <summary>preedit 期间锁定位置，避免候选窗跟随光标跳动</summary>
        private static bool _positionLocked;

        public static void Initialize(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>
        /// 由 TMP_InputField_LateUpdate_Patch 调用，设置当前获焦 TMP 的屏幕坐标（Y-up 像素）。
        /// 如果位置已锁定（preedit 进行中）则不更新。
        /// </summary>
        public static void SetFocusedTMPScreenRect(UnityEngine.Rect screenRect)
        {
            if (_positionLocked) return;
            _lastTMPScreenRect = screenRect;
        }

        /// <summary>
        /// 由 TMP_InputField_LateUpdate_Patch 在 TMP 失焦时调用，清除缓存的坐标
        /// </summary>
        public static void ClearFocusedTMPScreenRect()
        {
            _lastTMPScreenRect = null;
            _positionLocked = false;
        }

        /// <summary>
        /// 锁定当前位置，preedit 开始时调用。
        /// 锁定后 SetFocusedTMPScreenRect 和 UIToolkit rect 不再更新，直到 UnlockPosition。
        /// </summary>
        public static void LockPosition()
        {
            _positionLocked = true;
        }

        /// <summary>
        /// 解锁位置，preedit 结束（提交/清空）时调用。
        /// 下次 preedit 开始时会重新捕获位置。
        /// </summary>
        public static void UnlockPosition()
        {
            _positionLocked = false;
        }

        /// <summary>
        /// 获取当前获焦输入框的面板坐标（UIToolkit 坐标系，Y-down）。
        /// 优先返回 UIToolkit TextField rect，其次返回 TMP rect（经坐标转换）。
        /// </summary>
        public static UnityEngine.Rect? GetFocusedTextFieldRect()
        {
            // 优先 UIToolkit TextField
            if (_lastTextFieldRect.HasValue)
                return _lastTextFieldRect;

            // 其次 UGUI TMP（持久缓存，失焦时由 ClearFocusedTMPScreenRect 清除）
            if (_lastTMPScreenRect.HasValue)
            {
                var r = _lastTMPScreenRect.Value;
                float scaleFactor = ComputePanelScaleFactor();
                // 屏幕像素 (Y-up) → 面板坐标 (Y-down, 按 referenceResolution 缩放)
                return new UnityEngine.Rect(
                    r.x / scaleFactor,
                    (Screen.height - r.y - r.height) / scaleFactor,
                    r.width / scaleFactor,
                    r.height / scaleFactor
                );
            }

            return null;
        }

        /// <summary>
        /// 计算 PanelSettings ScaleWithScreenSize(MatchWidthOrHeight, match=0.5) 的缩放因子
        /// </summary>
        private static float ComputePanelScaleFactor()
        {
            const float refWidth = 1920f;
            const float refHeight = 1080f;
            const float match = 0.5f;

            float logWidth = Mathf.Log(Screen.width / refWidth, 2f);
            float logHeight = Mathf.Log(Screen.height / refHeight, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match));
        }

        /// <summary>
        /// 每帧调用。检查 UIToolkit 焦点并分发输入。
        /// 应在 TMP_InputField.LateUpdate 之前运行（即在 Update 阶段）。
        /// </summary>
        public static void Tick()
        {
            IsUIToolkitTextFieldFocused = false;
            _currentFocusedTextField = null;

            if (!OneJSBridge.IsInitialized) return;

            foreach (var kvp in OneJSBridge.Instances)
            {
                var inst = kvp.Value;
                if (!inst.Enabled || !inst.IsInitialized) continue;

                var uiDoc = inst.Engine?.GetComponent<UIDocument>();
                var root = uiDoc?.rootVisualElement;
                if (root?.panel?.focusController == null) continue;

                var focused = root.panel.focusController.focusedElement;
                var tf = FindTextField(focused);
                if (tf != null)
                {
                    IsUIToolkitTextFieldFocused = true;
                    _currentFocusedTextField = tf;

                    // 缓存 TextField 的面板坐标（preedit 锁定期间不更新）
                    if (!_positionLocked)
                    {
                        var ve = tf as VisualElement;
                        if (ve?.panel != null)
                            _lastTextFieldRect = ve.worldBound;
                    }

                    DispatchInput(tf);
                    CheckAndEmitImeContextChanged();
                    return;
                }
            }

            // 无 TextField 获焦时清除缓存
            _lastTextFieldRect = null;
            CheckAndEmitImeContextChanged();
        }

        /// <summary>
        /// 检测 Rime Context 和输入模式是否有变化，有则触发事件推送给 JS 层
        /// </summary>
        private static void CheckAndEmitImeContextChanged()
        {
            // 检测输入模式变化
            if (KeyboardHookPatch.InputModeDirty)
            {
                KeyboardHookPatch.InputModeDirty = false;
                try
                {
                    OnInputModeChanged?.Invoke(KeyboardHookPatch.IsGameMode);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[UIToolkitInputDispatcher] InputMode event error: {ex.Message}");
                }
            }

            // 检测 Rime Context 变化
            if (!KeyboardHookPatch.RimeContextDirty) return;
            KeyboardHookPatch.RimeContextDirty = false;

            if (OnImeContextChanged == null) return;

            try
            {
                var ctx = KeyboardHookPatch.GetRimeContext();
                string ctxJson = ctx != null && !string.IsNullOrEmpty(ctx.Preedit)
                    ? JSApi.JSApiHelper.ToJson(new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["preedit"] = ctx.Preedit,
                        ["cursorPos"] = ctx.CursorPos,
                        ["highlightedIndex"] = ctx.HighlightedIndex,
                        ["candidates"] = BuildCandidatesList(ctx),
                    })
                    : "null";

                var rect = GetFocusedTextFieldRect();
                string rectJson = rect.HasValue
                    ? JSApi.JSApiHelper.ToJson(new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["x"] = rect.Value.x,
                        ["y"] = rect.Value.y,
                        ["width"] = rect.Value.width,
                        ["height"] = rect.Value.height,
                    })
                    : "null";

                OnImeContextChanged.Invoke(ctxJson, rectJson);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[UIToolkitInputDispatcher] IME context event error: {ex.Message}");
            }
        }

        private static System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> BuildCandidatesList(Rime.RimeContextInfo ctx)
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
            if (ctx.Candidates != null)
            {
                foreach (var c in ctx.Candidates)
                {
                    var item = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["text"] = c.Text ?? "",
                    };
                    if (!string.IsNullOrEmpty(c.Comment))
                        item["comment"] = c.Comment;
                    list.Add(item);
                }
            }
            return list;
        }

        /// <summary>
        /// 清除所有 UIToolkit panel 的焦点，使 TMP_InputField 恢复键盘注入。
        /// </summary>
        private static void BlurAllPanels()
        {
            foreach (var kvp in OneJSBridge.Instances)
            {
                var inst = kvp.Value;
                if (!inst.Enabled || !inst.IsInitialized) continue;

                var uiDoc = inst.Engine?.GetComponent<UIDocument>();
                var root = uiDoc?.rootVisualElement;
                var focused = root?.panel?.focusController?.focusedElement;
                if (focused is UnityEngine.UIElements.VisualElement ve)
                    ve.Blur();
            }
        }

        /// <summary>
        /// 从焦点元素出发向上查找 TextField。
        /// UIToolkit 可能将焦点设在 TextField 的内部 TextInput 子元素上。
        /// </summary>
        private static TextField FindTextField(Focusable focused)
        {
            if (focused == null) return null;
            if (focused is TextField tf) return tf;

            // 向上遍历查找 TextField 父元素
            var ve = focused as VisualElement;
            while (ve != null)
            {
                if (ve is TextField parentTf) return parentTf;
                ve = ve.parent;
            }
            return null;
        }

        private static void DispatchInput(TextField tf)
        {
            try
            {
                // 检查 Rime preedit 状态，管理位置锁定
                var rimeCtx = KeyboardHookPatch.GetRimeContext();
                bool hasPreedit = rimeCtx != null && !string.IsNullOrEmpty(rimeCtx.Preedit);
                if (hasPreedit && !_positionLocked)
                {
                    LockPosition();
                }
                else if (!hasPreedit && _positionLocked)
                {
                    UnlockPosition();
                }

                // 1. Rime 提交文本
                string commit = KeyboardHookPatch.GetCommittedText();
                if (!string.IsNullOrEmpty(commit))
                {
                    InsertTextAtCursor(tf, commit);
                    // 提交后解锁位置，下次 preedit 重新捕获
                    UnlockPosition();
                    return;
                }

                // 2. 简单队列字符
                string input = KeyboardHookPatch.GetAndClearInputBuffer();
                if (!string.IsNullOrEmpty(input))
                {
                    foreach (char c in input)
                    {
                        if (c == '\b')
                        {
                            HandleBackspace(tf);
                        }
                        else if (c == '\n')
                        {
                            // 向 UIToolkit 派发 KeyDown 事件，让 JS onKeyDown 能收到 Enter
                            DispatchReturnKeyEvent(tf);
                        }
                        else
                        {
                            InsertTextAtCursor(tf, c.ToString());
                        }
                    }
                }

                // 3. 导航键
                uint? navKey = KeyboardHookPatch.GetNavigationKey();
                while (navKey.HasValue)
                {
                    HandleNavigationKey(tf, navKey.Value);
                    navKey = KeyboardHookPatch.GetNavigationKey();
                }

                // 4. 剪贴板操作 (Ctrl+V/A/C/X)
                string pasteText;
                var clipAction = KeyboardHookPatch.GetClipboardAction(out pasteText);
                while (clipAction.HasValue)
                {
                    switch (clipAction.Value)
                    {
                        case KeyboardHookPatch.ClipAction.Paste:
                            if (!string.IsNullOrEmpty(pasteText))
                                InsertTextAtCursor(tf, pasteText);
                            break;
                        case KeyboardHookPatch.ClipAction.SelectAll:
                            tf.SelectAll();
                            break;
                        case KeyboardHookPatch.ClipAction.Copy:
                            CopySelectionToClipboard(tf);
                            break;
                        case KeyboardHookPatch.ClipAction.Cut:
                            CopySelectionToClipboard(tf);
                            DeleteSelection(tf);
                            break;
                    }
                    clipAction = KeyboardHookPatch.GetClipboardAction(out pasteText);
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[UIToolkitInput] Dispatch error: {ex.Message}");
            }
        }

        private static void InsertTextAtCursor(TextField tf, string text)
        {
            var curText = tf.value ?? "";
            int cursor = GetCursorIndex(tf, curText.Length);
            int select = GetSelectIndex(tf, cursor);

            // 有选区时先删除选中文本
            if (cursor != select)
            {
                int start = Math.Min(cursor, select);
                int end = Math.Max(cursor, select);
                curText = curText.Remove(start, end - start);
                cursor = start;
            }

            tf.value = curText.Insert(cursor, text);
            int newPos = cursor + text.Length;
            tf.cursorIndex = newPos;
            tf.selectIndex = newPos;
        }

        private static void HandleBackspace(TextField tf)
        {
            var curText = tf.value ?? "";
            if (string.IsNullOrEmpty(curText)) return;

            int cursor = GetCursorIndex(tf, curText.Length);
            int select = GetSelectIndex(tf, cursor);

            if (cursor != select)
            {
                // 删除选区
                int start = Math.Min(cursor, select);
                int end = Math.Max(cursor, select);
                tf.value = curText.Remove(start, end - start);
                tf.cursorIndex = start;
                tf.selectIndex = start;
            }
            else if (cursor > 0)
            {
                // 删除光标前一个字符
                tf.value = curText.Remove(cursor - 1, 1);
                tf.cursorIndex = cursor - 1;
                tf.selectIndex = cursor - 1;
            }
        }

        private static void HandleNavigationKey(TextField tf, uint vk)
        {
            var curText = tf.value ?? "";
            int cursor = GetCursorIndex(tf, curText.Length);

            switch (vk)
            {
                case 0x25: // Left
                    if (cursor > 0)
                    {
                        tf.cursorIndex = cursor - 1;
                        tf.selectIndex = cursor - 1;
                    }
                    break;
                case 0x27: // Right
                    if (cursor < curText.Length)
                    {
                        tf.cursorIndex = cursor + 1;
                        tf.selectIndex = cursor + 1;
                    }
                    break;
                case 0x24: // Home
                    tf.cursorIndex = 0;
                    tf.selectIndex = 0;
                    break;
                case 0x23: // End
                    tf.cursorIndex = curText.Length;
                    tf.selectIndex = curText.Length;
                    break;
                case 0x2E: // Delete
                    if (cursor < curText.Length)
                    {
                        tf.value = curText.Remove(cursor, 1);
                        tf.cursorIndex = cursor;
                        tf.selectIndex = cursor;
                    }
                    break;
            }
        }

        /// <summary>将 TextField 选中文本复制到系统剪贴板</summary>
        private static void CopySelectionToClipboard(TextField tf)
        {
            var curText = tf.value ?? "";
            int cursor = GetCursorIndex(tf, curText.Length);
            int select = GetSelectIndex(tf, cursor);
            if (cursor == select) return;

            int start = Math.Min(cursor, select);
            int end = Math.Max(cursor, select);
            string selected = curText.Substring(start, end - start);
            GUIUtility.systemCopyBuffer = selected;
        }

        /// <summary>删除 TextField 选中文本（不影响剪贴板）</summary>
        private static void DeleteSelection(TextField tf)
        {
            var curText = tf.value ?? "";
            int cursor = GetCursorIndex(tf, curText.Length);
            int select = GetSelectIndex(tf, cursor);
            if (cursor == select) return;

            int start = Math.Min(cursor, select);
            int end = Math.Max(cursor, select);
            tf.value = curText.Remove(start, end - start);
            tf.cursorIndex = start;
            tf.selectIndex = start;
        }

        private static void DispatchReturnKeyEvent(TextField tf)
        {
            try
            {
                var ve = tf as VisualElement;
                if (ve?.panel == null) return;

                using (var evt = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None))
                {
                    evt.target = ve;
                    ve.SendEvent(evt);
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[UIToolkitInput] DispatchReturnKeyEvent error: {ex.Message}");
            }
        }

        private static int GetCursorIndex(TextField tf, int maxLen)
        {
            int idx = tf.cursorIndex;
            if (idx < 0) idx = 0;
            if (idx > maxLen) idx = maxLen;
            return idx;
        }

        private static int GetSelectIndex(TextField tf, int fallback)
        {
            int idx = tf.selectIndex;
            int maxLen = (tf.value ?? "").Length;
            if (idx < 0) idx = 0;
            if (idx > maxLen) idx = maxLen;
            return idx;
        }
    }
}
