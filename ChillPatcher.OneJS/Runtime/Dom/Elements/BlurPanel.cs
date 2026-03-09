using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom
{
    /// <summary>
    /// Custom VisualElement that renders a frosted-glass (毛玻璃) blur effect
    /// by capturing the camera output behind this element, applying a
    /// dual-pass downsample+upsample blur, and setting the result as backgroundImage.
    ///
    /// Usage in JSX:
    ///   &lt;blur-panel downsample={2} blur-iterations={4} interval={3} tint="#ffffff40" /&gt;
    ///
    /// Attributes:
    ///   downsample       - Capture resolution divisor (1-8, default 2). Higher = cheaper + softer.
    ///   blur-iterations  - Number of downsample+upsample blur passes (1-8, default 4). Higher = more blur.
    ///   interval         - Update every N game frames (1+, default 3). 1 = every frame.
    ///   blur-radius      - (Legacy alias) Same as blur-iterations.
    ///   tint             - Overlay tint colour as hex string, e.g. "#ffffff40".
    /// </summary>
    public class BlurPanel : VisualElement
    {
        // --------------- Shared screen capture (one cam.Render() per frame for all BlurPanels) ---------------

        static RenderTexture s_sharedCapture;
        static int s_sharedCaptureFrame = -1;

        static RenderTexture GetOrCreateSharedCapture(Camera cam)
        {
            int frame = Time.frameCount;
            int w = cam.pixelWidth;
            int h = cam.pixelHeight;

            if (frame == s_sharedCaptureFrame && s_sharedCapture != null
                && s_sharedCapture.width == w && s_sharedCapture.height == h)
                return s_sharedCapture;

            if (s_sharedCapture == null || s_sharedCapture.width != w || s_sharedCapture.height != h)
            {
                if (s_sharedCapture != null)
                {
                    s_sharedCapture.Release();
                    UnityEngine.Object.Destroy(s_sharedCapture);
                }
                s_sharedCapture = new RenderTexture(w, h, 16, RenderTextureFormat.Default);
                s_sharedCapture.name = "BlurPanel_SharedCapture";
            }

            var prevTarget = cam.targetTexture;
            var prevRect = cam.rect;
            try
            {
                cam.targetTexture = s_sharedCapture;
                cam.Render();
            }
            finally
            {
                cam.targetTexture = prevTarget;
                cam.rect = prevRect;
            }

            s_sharedCaptureFrame = frame;
            return s_sharedCapture;
        }

        // --------------- Public properties (settable from JSX via setAttribute reflection) ---------------

        /// <summary>Capture resolution divisor (1-8). Higher = cheaper + softer overall.</summary>
        public int Downsample
        {
            get => _downsample;
            set => _downsample = Mathf.Clamp(value, 1, 8);
        }

        /// <summary>Number of downsample blur passes (1-8). Higher = blurrier.</summary>
        public int BlurIterations
        {
            get => _blurIterations;
            set => _blurIterations = Mathf.Clamp(value, 1, 8);
        }

        /// <summary>Legacy alias for BlurIterations.</summary>
        public int BlurRadius
        {
            get => _blurIterations;
            set => _blurIterations = Mathf.Clamp(value, 1, 8);
        }

        /// <summary>Update every N game frames (1+). 1 = every frame.</summary>
        public int Interval
        {
            get => _frameInterval;
            set => _frameInterval = Mathf.Max(1, value);
        }

        /// <summary>Legacy: converts fps to frame interval (assumes 60fps). Use Interval instead.</summary>
        public int Fps
        {
            get => Mathf.Max(1, 60 / _frameInterval);
            set => _frameInterval = Mathf.Max(1, 60 / Mathf.Clamp(value, 1, 60));
        }

        /// <summary>Legacy: same as Downsample.</summary>
        public int CaptureDivisor
        {
            get => _downsample;
            set => _downsample = Mathf.Clamp(value, 1, 8);
        }

        /// <summary>Optional tint colour as "#RRGGBB" or "#RRGGBBAA" hex string.</summary>
        public string Tint
        {
            get => _tintString;
            set
            {
                _tintString = value;
                if (!string.IsNullOrEmpty(value))
                {
                    var hex = value.StartsWith("#") ? value : "#" + value;
                    if (ColorUtility.TryParseHtmlString(hex, out var c))
                        _tintColor = c;
                }
                else
                {
                    _tintColor = Color.clear;
                }
            }
        }

        // --------------- Internal state ---------------

        int _downsample = 2;
        int _blurIterations = 4;
        int _frameInterval = 3;
        string _tintString;
        Color _tintColor = Color.clear;

        RenderTexture _blurResult;    // persistent RT shown as background
        bool _attached;
        IVisualElementScheduledItem _scheduledItem;
        int _lastUpdateFrame;

        // --------------- Lifecycle ---------------

        public BlurPanel()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        void OnAttach(AttachToPanelEvent e)
        {
            _attached = true;
            _lastUpdateFrame = Time.frameCount;
            _scheduledItem = schedule.Execute(Tick).Every(16);
        }

        void OnDetach(DetachFromPanelEvent e)
        {
            _attached = false;
            _scheduledItem?.Pause();
            _scheduledItem = null;
            ReleaseBlurRT();
        }

        // --------------- Per-tick logic ---------------

        void Tick()
        {
            if (!_attached) return;

            int frame = Time.frameCount;
            if (frame - _lastUpdateFrame < _frameInterval) return;
            _lastUpdateFrame = frame;

            var rect = worldBound;
            if (float.IsNaN(rect.width) || float.IsNaN(rect.height)) return;
            if (rect.width < 2f || rect.height < 2f) return;

            try
            {
                CaptureAndBlur(rect);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlurPanel] CaptureAndBlur failed: {ex.Message}");
            }
        }

        // --------------- Core blur pipeline ---------------

        void CaptureAndBlur(Rect panelRect)
        {
            // 1. Find main camera
            var cam = Camera.main;
            if (cam == null)
            {
                var all = Camera.allCameras;
                if (all.Length > 0) cam = all[0];
                else return;
            }

            int camW = cam.pixelWidth;
            int camH = cam.pixelHeight;
            if (camW < 1 || camH < 1) return;

            // 2. Get shared full-resolution capture (one cam.Render() per frame across all BlurPanels)
            var capture = GetOrCreateSharedCapture(cam);

            // 3. Calculate the UV crop region
            float dpiScale = 1f;
            if (panel != null)
            {
                var root = panel.visualTree;
                if (root != null && root.layout.width > 0)
                    dpiScale = (float)camW / root.layout.width;
            }

            float normX = Mathf.Clamp01((panelRect.x * dpiScale) / camW);
            float normY = Mathf.Clamp01((camH - (panelRect.y + panelRect.height) * dpiScale) / camH);
            float normW = Mathf.Clamp((panelRect.width * dpiScale) / camW, 0.001f, 1f - normX);
            float normH = Mathf.Clamp((panelRect.height * dpiScale) / camH, 0.001f, 1f - normY);

            // Crop+downsample in one blit (combines step 2 old + step 4 old)
            int cropW = Mathf.Max(1, Mathf.RoundToInt(panelRect.width / _downsample));
            int cropH = Mathf.Max(1, Mathf.RoundToInt(panelRect.height / _downsample));

            // 4. Crop, blur, and write to persistent RT
            RenderTexture current = RenderTexture.GetTemporary(cropW, cropH, 0, RenderTextureFormat.Default);
            try
            {
                Graphics.Blit(capture, current, new Vector2(normW, normH), new Vector2(normX, normY));

                // 5. Downsample chain
                for (int i = 0; i < _blurIterations; i++)
                {
                    int tw = Mathf.Max(1, current.width / 2);
                    int th = Mathf.Max(1, current.height / 2);
                    var next = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.Default);
                    next.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(current, next);
                    RenderTexture.ReleaseTemporary(current);
                    current = next;
                }

                // 6. Upsample chain: 2× steps with √2 factor for smoother results
                int upsampleSteps = _blurIterations * 2;
                for (int i = 0; i < upsampleSteps; i++)
                {
                    int tw, th;
                    if (i == upsampleSteps - 1)
                    {
                        tw = cropW;
                        th = cropH;
                    }
                    else
                    {
                        tw = Mathf.Min(cropW, Mathf.Max(1, Mathf.RoundToInt(current.width * 1.41f)));
                        th = Mathf.Min(cropH, Mathf.Max(1, Mathf.RoundToInt(current.height * 1.41f)));
                    }
                    var up = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.Default);
                    up.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(current, up);
                    RenderTexture.ReleaseTemporary(current);
                    current = up;
                }

                // 7. Copy to persistent RT; set backgroundImage only on RT (re)creation
                bool rtCreated = EnsureBlurRT(cropW, cropH);
                Graphics.Blit(current, _blurResult);

                if (rtCreated)
                {
                    style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_blurResult));
                    if (_tintColor.a > 0.001f)
                        style.unityBackgroundImageTintColor = _tintColor;
                }

                MarkDirtyRepaint();
            }
            finally
            {
                if (current != null)
                    RenderTexture.ReleaseTemporary(current);
            }
        }

        // --------------- RT management ---------------

        bool EnsureBlurRT(int w, int h)
        {
            if (_blurResult != null && _blurResult.width == w && _blurResult.height == h)
                return false;

            ReleaseBlurRT();
            _blurResult = new RenderTexture(w, h, 0, RenderTextureFormat.Default);
            _blurResult.filterMode = FilterMode.Bilinear;
            _blurResult.wrapMode = TextureWrapMode.Clamp;
            _blurResult.name = "BlurPanel_Result";
            return true;
        }

        void ReleaseBlurRT()
        {
            if (_blurResult != null)
            {
                _blurResult.Release();
                UnityEngine.Object.Destroy(_blurResult);
                _blurResult = null;
            }
        }
    }
}
