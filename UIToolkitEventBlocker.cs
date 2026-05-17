using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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

        /// <summary>当前鼠标按压是否从桌面图标/项目开始</summary>
        public static bool IsDesktopItemBlocking { get; private set; }

        /// <summary>是否应该清空本帧 Unity UI raycast 结果</summary>
        public static bool ShouldClearUnityRaycasts => IsBlocking || IsDesktopItemBlocking;

        /// <summary>是否应阻止游戏场景侧鼠标互动</summary>
        public static bool ShouldBlockGameSceneMouse()
        {
            return UpdateDesktopItemCapture();
        }

        private static bool _wasMouseDown;
        private static bool _desktopItemCaptureActive;
        private static POINT _mouseDownPoint;
        private const int DesktopDragReleaseThresholdPixels = 6;

        private static readonly HashSet<string> DesktopForegroundClasses = new HashSet<string>
        {
            "Progman",
            "WorkerW",
            "SHELLDLL_DefView",
            "SysListView32",
        };

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter,
            string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            int dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
            int dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int LVM_FIRST = 0x1000;
        private const int LVM_HITTEST = LVM_FIRST + 18;
        private const int LVHT_ONITEMICON = 0x0002;
        private const int LVHT_ONITEMLABEL = 0x0004;
        private const int LVHT_ONITEMSTATEICON = 0x0008;
        private const int LVHT_ONITEM = LVHT_ONITEMICON | LVHT_ONITEMLABEL | LVHT_ONITEMSTATEICON;

        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int DESKTOP_LISTVIEW_PROCESS_ACCESS =
            PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE;

        private const int MEM_COMMIT = 0x1000;
        private const int MEM_RESERVE = 0x2000;
        private const int MEM_RELEASE = 0x8000;
        private const int PAGE_READWRITE = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LVHITTESTINFO
        {
            public POINT pt;
            public int flags;
            public int iItem;
            public int iSubItem;
            public int iGroup;
        }

        /// <summary>
        /// 每帧在 EventSystem.Update 之前调用。
        /// 检测鼠标是否在 UIToolkit 可交互元素上，设置 IsBlocking 标志。
        /// </summary>
        public static void Update()
        {
            IsBlocking = false;
            IsDesktopItemBlocking = UpdateDesktopItemCapture();

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
                    IsDesktopItemBlocking = false;
                    return;
                }
            }
        }

        private static bool UpdateDesktopItemCapture()
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value)
                return false;

            bool mouseDown = IsPrimaryMouseDown();
            if (!mouseDown)
            {
                _wasMouseDown = false;
                _desktopItemCaptureActive = false;
                return false;
            }

            if (!_wasMouseDown)
            {
                if (!GetCursorPos(out _mouseDownPoint))
                    _mouseDownPoint = default;

                _desktopItemCaptureActive =
                    IsDesktopForegroundWindow(GetForegroundWindow())
                    && IsDesktopListItemUnderCursor();
            }
            else if (_desktopItemCaptureActive && HasMouseMovedPastDesktopDragThreshold())
            {
                _desktopItemCaptureActive = false;
            }

            _wasMouseDown = true;
            return _desktopItemCaptureActive;
        }

        private static bool IsPrimaryMouseDown()
        {
            return (GetAsyncKeyState(0x01) & 0x8000) != 0;
        }

        private static bool HasMouseMovedPastDesktopDragThreshold()
        {
            if (!GetCursorPos(out var currentPoint))
                return false;

            int dx = currentPoint.x - _mouseDownPoint.x;
            int dy = currentPoint.y - _mouseDownPoint.y;
            int threshold = DesktopDragReleaseThresholdPixels;
            return dx * dx + dy * dy > threshold * threshold;
        }

        public static bool IsDesktopForegroundActive()
        {
            return IsDesktopForegroundWindow(GetForegroundWindow());
        }

        private static bool IsDesktopForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            return DesktopForegroundClasses.Contains(GetWindowClassName(hWnd));
        }

        public static bool IsDesktopListItemUnderCursor()
        {
            if (!GetCursorPos(out var screenPoint))
                return false;

            var listView = FindDesktopListViewWindow();
            if (listView == IntPtr.Zero)
                return false;

            var clientPoint = screenPoint;
            if (!ScreenToClient(listView, ref clientPoint))
                return false;

            return TryDesktopListViewHitTest(listView, clientPoint, out var hit)
                && hit.iItem >= 0
                && (hit.flags & LVHT_ONITEM) != 0;
        }

        public static IntPtr GetDesktopListViewWindow()
        {
            return FindDesktopListViewWindow();
        }

        private static IntPtr FindDesktopListViewWindow()
        {
            var progman = FindWindow("Progman", null);
            var listView = FindDesktopListViewWindowIn(progman);
            if (listView != IntPtr.Zero)
                return listView;

            var worker = IntPtr.Zero;
            while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
            {
                listView = FindDesktopListViewWindowIn(worker);
                if (listView != IntPtr.Zero)
                    return listView;
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindDesktopListViewWindowIn(IntPtr parent)
        {
            if (parent == IntPtr.Zero)
                return IntPtr.Zero;

            var defView = FindWindowEx(parent, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView == IntPtr.Zero)
                return IntPtr.Zero;

            return FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
        }

        private static bool TryDesktopListViewHitTest(IntPtr listView, POINT clientPoint,
            out LVHITTESTINFO hit)
        {
            hit = new LVHITTESTINFO
            {
                pt = clientPoint,
                flags = 0,
                iItem = -1,
                iSubItem = 0,
                iGroup = 0,
            };

            GetWindowThreadProcessId(listView, out var processId);
            if (processId == 0)
                return false;

            var process = OpenProcess(DESKTOP_LISTVIEW_PROCESS_ACCESS, false, processId);
            if (process == IntPtr.Zero)
                return false;

            var size = Marshal.SizeOf(typeof(LVHITTESTINFO));
            var remoteBuffer = IntPtr.Zero;
            var localBuffer = IntPtr.Zero;

            try
            {
                remoteBuffer = VirtualAllocEx(process, IntPtr.Zero, size,
                    MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remoteBuffer == IntPtr.Zero)
                    return false;

                localBuffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(hit, localBuffer, false);

                var bytes = new byte[size];
                Marshal.Copy(localBuffer, bytes, 0, size);

                if (!WriteProcessMemory(process, remoteBuffer, bytes, size, out var written)
                    || written.ToInt64() != size)
                {
                    return false;
                }

                SendMessage(listView, LVM_HITTEST, IntPtr.Zero, remoteBuffer);

                var result = new byte[size];
                if (!ReadProcessMemory(process, remoteBuffer, result, size, out var read)
                    || read.ToInt64() != size)
                {
                    return false;
                }

                Marshal.Copy(result, 0, localBuffer, size);
                hit = (LVHITTESTINFO)Marshal.PtrToStructure(localBuffer, typeof(LVHITTESTINFO));
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (localBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(localBuffer);
                if (remoteBuffer != IntPtr.Zero)
                    VirtualFreeEx(process, remoteBuffer, 0, MEM_RELEASE);
                CloseHandle(process);
            }
        }

        private static string GetWindowClassName(IntPtr hWnd)
        {
            var className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
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
