using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// TopIcons 竖排按钮与 RightIcons（含「隐藏UI」）的自动避让。
    ///
    /// 背景（issue #98）：
    ///   UI 重排列把 IconStory / IconSpecial / IconDecoration / IconEnviroment 追加进 TopIcons。
    ///   TopIcons 是 anchor(1,1) 顶部右对齐、向下排；RightIcons 是 anchor(1,0) 底部右对齐、向上排，
    ///   两根柱子互不相干。追加 4 个按钮后 TopIcons 底端伸进 RightIcons 顶部区域，
    ///   导致「环境」与「隐藏UI」重叠（全屏 2560x1440 下实测重叠约 110px）。
    ///
    /// 实现要点：
    ///   1. UI 在启动阶段处于未激活状态（MostFrontArea activeInHierarchy=False），
    ///      此时布局组还没算，子项全堆在初始位置 —— 必须等激活后再测量，否则会算错。
    ///   2. 用世界矩形（Overlay 画布下即屏幕像素）判断两列是否相交，不依赖容器自身 rect
    ///      （容器 rect 恒为 100x100，不随子项撑开）。
    ///   3. 采用固定控制律：目标重叠 = -Margin，每轮按 (overlap + Margin) 修正偏移量，
    ///      可自动收敛，并在切全屏/切窗口/分辨率变化后自我纠正，不需要额外事件。
    ///   4. 记录基准 anchoredPosition，所有修正都是"基准 + 偏移"，可回退、不累积漂移。
    /// </summary>
    public static class TopIconsAutoFit
    {
        /// <summary>避让后期望保留的间隙（屏幕像素）。</summary>
        public const float Margin = 8f;

        /// <summary>轮询间隔（秒）。0.5s 足够跟上分辨率变化，开销可忽略。</summary>
        public const float PollInterval = 0.25f;

        /// <summary>小于该阈值的修正忽略，避免 hover 动画导致的抖动。</summary>
        public const float Deadband = 2f;

        public const string TopIconsPath = "Paremt/PCPlatform/Canvas/UI/MostFrontArea/TopIcons";
        public const string RightIconsPath = "Paremt/PCPlatform/Canvas/UI/MostFrontArea/RightIcons";

        private static ManualLogSource _logger;
        private static TopIconsAutoFitWatcher _watcher;

        internal static ManualLogSource Logger =>
            _logger ??= BepInEx.Logging.Logger.CreateLogSource("TopIconsAutoFit");

        /// <summary>确保避让监视器存在（幂等）。</summary>
        public static void EnsureWatcher()
        {
            if (_watcher != null)
            {
                return;
            }

            try
            {
                var go = new GameObject("ChillPatcher_TopIconsAutoFit");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _watcher = go.AddComponent<TopIconsAutoFitWatcher>();
                Logger.LogInfo("TopIcons 自动避让监视器已启动");
            }
            catch (Exception ex)
            {
                Logger.LogError("监视器启动失败: " + ex);
            }
        }

        /// <summary>测量两列子按钮的合并世界矩形（Overlay 画布下即屏幕像素）。</summary>
        public static bool TryMeasure(GameObject topIcons, GameObject rightIcons, out Rect topBounds, out Rect rightBounds)
        {
            topBounds = default(Rect);
            rightBounds = default(Rect);

            if (topIcons == null || rightIcons == null || !topIcons.activeInHierarchy || !rightIcons.activeInHierarchy)
            {
                return false;
            }

            var topRt = topIcons.GetComponent<RectTransform>();
            var rightRt = rightIcons.GetComponent<RectTransform>();
            if (topRt == null || rightRt == null)
            {
                return false;
            }

            return TryGetChildrenBounds(topRt, out topBounds) && TryGetChildrenBounds(rightRt, out rightBounds);
        }

        /// <summary>父级缩放链（anchoredPosition 的单位换算系数）。</summary>
        internal static float ParentScaleY(RectTransform rt)
        {
            float local = Mathf.Max(0.0001f, rt.localScale.y);
            return Mathf.Max(0.0001f, rt.lossyScale.y / local);
        }

        /// <summary>
        /// 合并所有激活子节点的世界矩形（GetWorldCorners）。
        /// 容器自身 rect 恒为 100x100、不随子项撑开，因此只能从子项求包围盒。
        /// </summary>
        private static bool TryGetChildrenBounds(RectTransform parent, out Rect bounds)
        {
            bounds = default(Rect);
            bool any = false;
            var corners = new Vector3[4];

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                child.GetWorldCorners(corners);
                var r = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);

                if (!any)
                {
                    bounds = r;
                    any = true;
                }
                else
                {
                    bounds = Rect.MinMaxRect(
                        Mathf.Min(bounds.xMin, r.xMin),
                        Mathf.Min(bounds.yMin, r.yMin),
                        Mathf.Max(bounds.xMax, r.xMax),
                        Mathf.Max(bounds.yMax, r.yMax));
                }
            }

            return any;
        }
    }

    /// <summary>
    /// 轮询式避让执行器：UI 激活后测量一次，之后持续跟随分辨率变化自我纠正。
    /// </summary>
    public class TopIconsAutoFitWatcher : MonoBehaviour
    {
        private GameObject _topIcons;
        private GameObject _rightIcons;
        private RectTransform _topRect;

        private Vector2 _basePos;
        private float _shiftUnits;
        private bool _hasBase;

        private float _nextPoll;
        private float _readyAt;
        private int _lastWidth;
        private int _lastHeight;

        /// <summary>记录初始分辨率；本轮首帧不做避让。</summary>
        private void Start()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
        }

        /// <summary>按轮询间隔驱动一次避让计算；异常只记日志，不影响游戏主循环。</summary>
        private void Update()
        {
            float now = Time.unscaledTime;
            if (now < _nextPoll || now < _readyAt)
            {
                return;
            }

            _nextPoll = now + TopIconsAutoFit.PollInterval;

            try
            {
                Evaluate();
            }
            catch (Exception ex)
            {
                TopIconsAutoFit.Logger.LogError("避让计算异常: " + ex.Message);
            }
        }

        /// <summary>
        /// 解析引用、重建布局、测量两列并修正 TopIcons 的 Y 偏移，
        /// 使「环境」与「隐藏UI」之间保持 Margin 像素间隙。
        /// </summary>
        private void Evaluate()
        {
            if (_topIcons == null || !_topIcons.activeInHierarchy)
            {
                if (!TryResolve())
                {
                    return;
                }
            }

            bool sizeChanged = Screen.width != _lastWidth || Screen.height != _lastHeight;
            if (sizeChanged)
            {
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;
            }

            if (!_hasBase)
            {
                _basePos = _topRect.anchoredPosition;
                _shiftUnits = 0f;
                _hasBase = true;
            }

            // 先让布局组把子项位置算准，再测量
            LayoutRebuilder.ForceRebuildLayoutImmediate(_topRect);

            Rect topBounds;
            Rect rightBounds;
            if (!TopIconsAutoFit.TryMeasure(_topIcons, _rightIcons, out topBounds, out rightBounds))
            {
                return;
            }

            float overlap = rightBounds.yMax - topBounds.yMin;
            float correction = overlap + TopIconsAutoFit.Margin;

            if (Mathf.Abs(correction) < TopIconsAutoFit.Deadband)
            {
                return;
            }

            float scale = TopIconsAutoFit.ParentScaleY(_topRect);

            // 外部写入者可能在我们捕获基准之后整体挪动 TopIcons ——
            // 例如番茄钟插件「隐藏默认番茄钟 UI」会 chill.ui.setPosition(TopIcons, ±250)。
            // 一旦发现实际位置与「基准 + 我们的偏移」不一致，就把当前绝对位置接管为新基准、
            // 并把自身偏移归零，然后才计算 newShift。这样：
            //   1) 后续修正以「外部指定位置」为原点，不会用陈旧基准把对方的位移抹掉；
            //   2) Mathf.Max(0f, ...) 的下限语义变成"不低于外部指定位置"，不会去抵消外部上移。
            Vector2 currentPos = _topRect.anchoredPosition;
            Vector2 impliedPos = _basePos + new Vector2(0f, _shiftUnits);
            if ((currentPos - impliedPos).sqrMagnitude > 0.0001f)
            {
                _basePos = currentPos;
                _shiftUnits = 0f;
                TopIconsAutoFit.Logger.LogInfo(string.Format(
                    "[AutoFit] 检测到外部位置变更，接管为新基准: {0} -> {1}",
                    impliedPos.y.ToString("0.0"), currentPos.y.ToString("0.0")));
            }

            float newShift = Mathf.Max(0f, _shiftUnits + correction / scale);

            if (Mathf.Approximately(newShift, _shiftUnits))
            {
                return;
            }

            TopIconsAutoFit.Logger.LogInfo(string.Format(
                "[AutoFit] overlap={0:0.0}px shift {1:0.0} -> {2:0.0}px ({3})",
                overlap, _shiftUnits * scale, newShift * scale, sizeChanged ? "分辨率变化" : "收敛"));

            _shiftUnits = newShift;
            _topRect.anchoredPosition = _basePos + new Vector2(0f, _shiftUnits);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_topRect);
        }

        /// <summary>
        /// 重新解析 TopIcons / RightIcons 引用。
        /// 只有解析到的 RectTransform 与上一次不是同一个对象（场景切换、UI 重建）时，
        /// 才重置基准位置与偏移量；同一个对象仅是被停用再激活时，必须保留基准，
        /// 否则会把"已偏移过的位置"当作新基准，导致每次激活都向上累积漂移。
        /// </summary>
        private bool TryResolve()
        {
            // TopIcons 的父节点 MostFrontArea 在启动阶段通常未激活；未激活时直接放弃本轮，
            // 等激活后的轮询再处理（否则会按未计算的初始位置误判）。
            var mostFront = GameObject.Find("Paremt/PCPlatform/Canvas/UI/MostFrontArea");
            if (mostFront == null || !mostFront.activeInHierarchy)
            {
                return false;
            }

            var topIcons = GameObject.Find(TopIconsAutoFit.TopIconsPath);
            var rightIcons = GameObject.Find(TopIconsAutoFit.RightIconsPath);

            if (topIcons == null || rightIcons == null)
            {
                return false;
            }

            var topRect = topIcons.GetComponent<RectTransform>();
            if (topRect == null)
            {
                return false;
            }

            bool isSameObject = ReferenceEquals(_topRect, topRect);

            _topIcons = topIcons;
            _rightIcons = rightIcons;
            _topRect = topRect;

            if (!isSameObject)
            {
                // 新场景/新对象：丢弃旧基准与偏移，重新测量
                _hasBase = false;
                _shiftUnits = 0f;
            }

            // 无论是新对象还是重新激活，都给布局组一点时间稳定
            _readyAt = Time.unscaledTime + 0.3f;
            return true;
        }
    }
}
