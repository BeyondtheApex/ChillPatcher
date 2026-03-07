using System.Collections.Generic;
using System.Reflection;
using OneJS;
using OneJS.Dom;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChillPatcher
{
    /// <summary>
    /// 检测鼠标是否在 UIToolkit (OneJS) 可交互区域上，提供 IsBlocking 标志。
    ///
    /// 使用方式：由 EventSystem_Update_Patch 在 EventSystem.Update 之前调用 Update()。
    /// 由 EventSystem_RaycastAll_Patch 在 RaycastAll 之后清空结果，使 UGUI/InputController 不响应。
    ///
    /// 不使用 Canvas blocker（会导致 IsPointerOverGameObject() 返回 true，
    /// Unity RuntimeEventSystem 会跳过 UIToolkit 事件处理）。
    /// </summary>
    public static class UIToolkitEventBlocker
    {
        // 反射缓存
        private static FieldInfo _seDocumentField;   // ScriptEngine._document
        private static FieldInfo _domCallbacksField;  // Dom._registeredCallbacks
        private static bool _reflectionReady;

        // 被视为「可交互」的事件名（与 Dom.addEventListener 中的 nameLower 对应）
        private static readonly HashSet<string> InteractiveEvents = new HashSet<string>
        {
            "click", "clickevent",
            "pointerdown", "pointerdownevent",
            "pointerup", "pointerupevent",
            "mousedown", "mousedownevent",
            "mouseup", "mouseupevent",
        };

        /// <summary>当前帧是否正在拦截 UGUI</summary>
        public static bool IsBlocking { get; private set; }

        /// <summary>
        /// 每帧在 EventSystem.Update 之前调用。
        /// 检测鼠标是否在 UIToolkit 可交互元素上，设置 IsBlocking 标志。
        /// </summary>
        public static void Update()
        {
            IsBlocking = false;

            if (!OneJSBridge.IsInitialized)
                return;

            EnsureReflection();

            var mousePos = Input.mousePosition;

            foreach (var kvp in OneJSBridge.Instances)
            {
                var inst = kvp.Value;
                if (!inst.Enabled || !inst.Interactive || !inst.IsInitialized) continue;
                if (inst.SortingOrder <= 0) continue;

                var engine = inst.Engine;
                if (engine == null) continue;

                var rootVE = engine.UIDocument?.rootVisualElement;
                if (rootVE?.panel == null) continue;

                var panelPos = RuntimePanelUtils.ScreenToPanel(rootVE.panel, mousePos);
                var picked = rootVE.panel.Pick(panelPos);

                if (picked == null || picked == rootVE) continue;

                // 通过 OneJS DOM 检查 picked 元素或其祖先是否有交互内容
                var document = GetDocument(engine);
                if (document == null) continue;

                if (HasInteractiveAncestor(picked, rootVE, document, out _))
                {
                    IsBlocking = true;
                    break;
                }
            }
        }

        #region DOM 交互检测

        private static void EnsureReflection()
        {
            if (_reflectionReady) return;
            _seDocumentField = typeof(ScriptEngine).GetField("_document",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _domCallbacksField = typeof(Dom).GetField("_registeredCallbacks",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _reflectionReady = true;
        }

        private static Document GetDocument(ScriptEngine engine)
        {
            return _seDocumentField?.GetValue(engine) as Document;
        }

        /// <summary>
        /// 从 picked 元素开始向上遍历到 root，逐层计算累积透明度。
        /// 拦截条件（优先级从高到低）：
        /// 1. 注册了交互事件回调（onClick / onPointerDown 等）→ 直接拦截
        /// 2. 白名单组件类型（ScrollView 等）→ 即使透明也拦截
        /// 3. 有可见视觉内容 且 累积 opacity &gt; 0 → 拦截
        /// 不在白名单且透明 → 穿透
        /// </summary>
        private static bool HasInteractiveAncestor(VisualElement picked, VisualElement root,
            Document document, out string hitInfo)
        {
            var current = picked;
            int depth = 0;
            float cumulativeOpacity = 1f;

            while (current != null && current != root)
            {
                cumulativeOpacity *= current.resolvedStyle.opacity;

                // 1) DOM 交互回调 → 无条件拦截
                var dom = document.getDomFromVE(current);
                if (dom != null)
                {
                    var eventName = GetFirstInteractiveEvent(dom);
                    if (eventName != null)
                    {
                        hitInfo = $"HIT callback depth={depth} ve={current.GetType().Name}(name={current.name}) event={eventName}";
                        return true;
                    }
                }

                // 2) 白名单交互组件 → 即使透明也拦截
                if (IsInteractiveWidget(current))
                {
                    hitInfo = $"HIT widget depth={depth} type={current.GetType().Name}";
                    return true;
                }

                // 累积 opacity 过低 → 后续可见性检查无意义，跳过
                if (cumulativeOpacity <= 0.01f)
                {
                    current = current.parent;
                    depth++;
                    continue;
                }

                // 3) 可见视觉内容（跳过 body 全屏容器）
                if (dom != null && dom != document.body && HasVisualContent(current))
                {
                    hitInfo = $"HIT visual depth={depth} ve={current.GetType().Name}(name={current.name}) opacity={cumulativeOpacity:F2}";
                    return true;
                }

                current = current.parent;
                depth++;
            }
            hitInfo = null;
            return false;
        }

        /// <summary>
        /// 白名单：已知的可交互 UIToolkit 组件，即使透明也应拦截。
        /// </summary>
        private static bool IsInteractiveWidget(VisualElement ve)
        {
            return ve is ScrollView || ve is Scroller
                || ve is UnityEngine.UIElements.Slider
                || ve is UnityEngine.UIElements.Toggle
                || ve is BaseField<string>
                || ve is BaseField<float> || ve is BaseField<int>
                || ve is BaseField<bool>;
        }

        /// <summary>
        /// 检查 VisualElement 是否有可见的视觉内容（不透明背景、边框、图片或文本）。
        /// </summary>
        private static bool HasVisualContent(VisualElement ve)
        {
            var rs = ve.resolvedStyle;
            // 背景色有 alpha
            if (rs.backgroundColor.a > 0.01f) return true;
            // 背景图片
            if (rs.backgroundImage.texture != null || rs.backgroundImage.sprite != null) return true;
            // 边框
            if (rs.borderTopWidth > 0 || rs.borderBottomWidth > 0
                || rs.borderLeftWidth > 0 || rs.borderRightWidth > 0) return true;
            // 文本元素
            if (ve is TextElement te && !string.IsNullOrEmpty(te.text)) return true;
            return false;
        }

        private static string GetFirstInteractiveEvent(Dom dom)
        {
            if (_domCallbacksField == null) return null;
            var callbacks = _domCallbacksField.GetValue(dom)
                as Dictionary<string, List<RegisteredCallbackHolder>>;
            if (callbacks == null || callbacks.Count == 0) return null;

            foreach (var key in callbacks.Keys)
            {
                if (InteractiveEvents.Contains(key))
                    return key;
            }
            return null;
        }

        #endregion
    }
}
