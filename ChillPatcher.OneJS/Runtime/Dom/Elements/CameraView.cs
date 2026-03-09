using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom
{
    /// <summary>
    /// Custom VisualElement that creates a scene camera and displays its
    /// output as a live texture. High-performance: frame-interval based
    /// rendering, cached StyleBackground, proper cleanup.
    ///
    /// Usage in JSX:
    ///   &lt;camera-view fov={60} interval={2} resolution-scale={0.5}
    ///                pos-x={0} pos-y={1} pos-z={-10}
    ///                rot-x={0} rot-y={0} rot-z={0}
    ///                near-clip={0.3} far-clip={1000}
    ///                clear-color="#000000" depth={-10} /&gt;
    ///
    /// Attributes:
    ///   fov              - Field of view in degrees (1-179, default 60).
    ///   interval         - Render every N game frames (1+, default 2).
    ///   resolution-scale - Output resolution multiplier (0.1-2, default 0.5). Lower = cheaper.
    ///   pos-x/y/z        - Camera world position (default 0, 1, -10).
    ///   rot-x/y/z        - Camera euler rotation in degrees (default 0, 0, 0).
    ///   near-clip        - Near clip plane (default 0.3).
    ///   far-clip         - Far clip plane (default 1000).
    ///   clear-color      - Background clear colour as hex string (default "#000000").
    ///   depth            - Camera depth/priority (default -10).
    ///   culling-mask     - Culling mask as int bitfield (default -1 = Everything).
    /// </summary>
    public class CameraView : VisualElement
    {
        // --------------- Public properties ---------------

        public float Fov
        {
            get => _fov;
            set { _fov = Mathf.Clamp(value, 1f, 179f); ApplyToCamera(); }
        }

        public int Interval
        {
            get => _frameInterval;
            set => _frameInterval = Mathf.Max(1, value);
        }

        public float ResolutionScale
        {
            get => _resScale;
            set => _resScale = Mathf.Clamp(value, 0.1f, 2f);
        }

        public float PosX
        {
            get => _posX;
            set { _posX = value; ApplyTransform(); }
        }

        public float PosY
        {
            get => _posY;
            set { _posY = value; ApplyTransform(); }
        }

        public float PosZ
        {
            get => _posZ;
            set { _posZ = value; ApplyTransform(); }
        }

        public float RotX
        {
            get => _rotX;
            set { _rotX = value; ApplyTransform(); }
        }

        public float RotY
        {
            get => _rotY;
            set { _rotY = value; ApplyTransform(); }
        }

        public float RotZ
        {
            get => _rotZ;
            set { _rotZ = value; ApplyTransform(); }
        }

        public float NearClip
        {
            get => _nearClip;
            set { _nearClip = Mathf.Max(0.01f, value); ApplyToCamera(); }
        }

        public float FarClip
        {
            get => _farClip;
            set { _farClip = Mathf.Max(_nearClip + 0.01f, value); ApplyToCamera(); }
        }

        public string ClearColor
        {
            get => _clearColorStr;
            set
            {
                _clearColorStr = value;
                if (!string.IsNullOrEmpty(value))
                {
                    var hex = value.StartsWith("#") ? value : "#" + value;
                    if (ColorUtility.TryParseHtmlString(hex, out var c))
                        _clearColor = c;
                }
                ApplyToCamera();
            }
        }

        public int Depth
        {
            get => _depth;
            set { _depth = value; ApplyToCamera(); }
        }

        public int CullingMask
        {
            get => _cullingMask;
            set { _cullingMask = value; ApplyToCamera(); }
        }

        // --------------- Internal state ---------------

        float _fov = 60f;
        int _frameInterval = 2;
        float _resScale = 0.5f;
        float _posX = 0f, _posY = 1f, _posZ = -10f;
        float _rotX = 0f, _rotY = 0f, _rotZ = 0f;
        float _nearClip = 0.3f;
        float _farClip = 1000f;
        string _clearColorStr = "#000000";
        Color _clearColor = Color.black;
        int _depth = -10;
        int _cullingMask = -1; // Everything

        GameObject _camGO;
        Camera _cam;
        RenderTexture _rt;
        bool _attached;
        IVisualElementScheduledItem _scheduledItem;
        int _lastRenderFrame;

        // --------------- Lifecycle ---------------

        public CameraView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        void OnAttach(AttachToPanelEvent e)
        {
            _attached = true;
            _lastRenderFrame = Time.frameCount;
            CreateCamera();
            _scheduledItem = schedule.Execute(Tick).Every(16);
        }

        void OnDetach(DetachFromPanelEvent e)
        {
            _attached = false;
            _scheduledItem?.Pause();
            _scheduledItem = null;
            DestroyCamera();
            ReleaseRT();
        }

        // --------------- Camera management ---------------

        void CreateCamera()
        {
            if (_camGO != null) return;

            // Try to clone the main game camera to preserve post-processing
            var sourceCamera = Camera.main;
            if (sourceCamera != null)
            {
                _camGO = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
                _camGO.name = "CameraView_Clone";
                _camGO.hideFlags = HideFlags.HideAndDontSave;

                // Keep only Camera and post-processing related components;
                // remove scripts, audio listeners, and other logic components
                foreach (var comp in _camGO.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;
                    if (comp is Transform) continue;
                    if (comp is Camera) continue;
                    // Unity built-in rendering/post-processing components
                    // typically live in UnityEngine.Rendering namespace
                    var ns = comp.GetType().Namespace ?? "";
                    if (ns.StartsWith("UnityEngine.Rendering")) continue;

                    try { UnityEngine.Object.DestroyImmediate(comp); }
                    catch { /* some components resist destruction */ }
                }

                _cam = _camGO.GetComponent<Camera>();
                _cam.enabled = false; // We render manually
            }
            else
            {
                // Fallback: create bare camera if no main camera found
                _camGO = new GameObject("CameraView_Cam");
                _camGO.hideFlags = HideFlags.HideAndDontSave;
                _cam = _camGO.AddComponent<Camera>();
                _cam.enabled = false;
            }

            ApplyToCamera();
            ApplyTransform();
        }

        void DestroyCamera()
        {
            if (_camGO != null)
            {
                UnityEngine.Object.Destroy(_camGO);
                _camGO = null;
                _cam = null;
            }
        }

        void ApplyToCamera()
        {
            if (_cam == null) return;
            _cam.fieldOfView = _fov;
            _cam.nearClipPlane = _nearClip;
            _cam.farClipPlane = _farClip;
            _cam.backgroundColor = _clearColor;
            _cam.depth = _depth;
            _cam.cullingMask = _cullingMask;
        }

        void ApplyTransform()
        {
            if (_camGO == null) return;
            _camGO.transform.position = new Vector3(_posX, _posY, _posZ);
            _camGO.transform.eulerAngles = new Vector3(_rotX, _rotY, _rotZ);
        }

        // --------------- Per-tick logic ---------------

        void Tick()
        {
            if (!_attached || _cam == null) return;

            int frame = Time.frameCount;
            if (frame - _lastRenderFrame < _frameInterval) return;
            _lastRenderFrame = frame;

            var rect = worldBound;
            if (float.IsNaN(rect.width) || float.IsNaN(rect.height)) return;
            if (rect.width < 2f || rect.height < 2f) return;

            try
            {
                RenderCamera(rect);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CameraView] Render failed: {ex.Message}");
            }
        }

        void RenderCamera(Rect panelRect)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(panelRect.width * _resScale));
            int h = Mathf.Max(1, Mathf.RoundToInt(panelRect.height * _resScale));

            bool rtCreated = EnsureRT(w, h);

            var prevTarget = _cam.targetTexture;
            try
            {
                _cam.targetTexture = _rt;
                _cam.Render();
            }
            finally
            {
                _cam.targetTexture = prevTarget;
            }

            if (rtCreated)
            {
                style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_rt));
            }

            MarkDirtyRepaint();
        }

        // --------------- RT management ---------------

        bool EnsureRT(int w, int h)
        {
            if (_rt != null && _rt.width == w && _rt.height == h)
                return false;

            ReleaseRT();
            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.Default);
            _rt.filterMode = FilterMode.Bilinear;
            _rt.wrapMode = TextureWrapMode.Clamp;
            _rt.name = "CameraView_RT";
            return true;
        }

        void ReleaseRT()
        {
            if (_rt != null)
            {
                _rt.Release();
                UnityEngine.Object.Destroy(_rt);
                _rt = null;
            }
        }
    }
}
