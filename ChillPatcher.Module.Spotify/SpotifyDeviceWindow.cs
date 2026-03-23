using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChillPatcher.Module.Spotify
{
    /// <summary>
    /// 设备选择弹窗 — 贴近游戏 UI 风格（半透明、圆角、柔和色调）。
    /// </summary>
    public class SpotifyDeviceWindow : MonoBehaviour
    {
        private List<SpotifyDevice> _devices;
        private string _activeDeviceId;
        private Action<SpotifyDevice> _onSelect;
        private Action _onCancel;
        private bool _visible = true;
        private int _language;
        private float _fadeAlpha;

        private Rect _windowRect;
        private Vector2 _scrollPos;
        private bool _stylesInitialized;

        // 纹理
        private Texture2D _windowBgTex;
        private Texture2D _overlayTex;
        private Texture2D _cardNormalTex;
        private Texture2D _cardHoverTex;
        private Texture2D _cardActiveTex;
        private Texture2D _accentDotTex;
        private Texture2D _closeBtnTex;
        private Texture2D _closeBtnHoverTex;
        private Texture2D _separatorTex;

        // 样式
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _emptyStyle;
        private GUIStyle _closeStyle;

        // 颜色
        private static readonly Color SpotifyGreen = new Color(0.114f, 0.725f, 0.329f, 1f);

        public static SpotifyDeviceWindow Show(
            List<SpotifyDevice> devices,
            string activeDeviceId,
            Action<SpotifyDevice> onSelect,
            Action onCancel = null)
        {
            var go = new GameObject("SpotifyDeviceWindow");
            DontDestroyOnLoad(go);
            var w = go.AddComponent<SpotifyDeviceWindow>();
            w._devices = devices ?? new List<SpotifyDevice>();
            w._activeDeviceId = activeDeviceId;
            w._onSelect = onSelect;
            w._onCancel = onCancel;
            w._language = GetCurrentLanguage();
            return w;
        }

        private void Start()
        {
            var rows = Mathf.Max(_devices.Count, 1);
            var h = Mathf.Min(90 + rows * 58 + 56, 440);
            _windowRect = new Rect(
                Screen.width / 2f - 200,
                Screen.height / 2f - h / 2f,
                400, h);
        }

        private void Update()
        {
            _fadeAlpha = Mathf.MoveTowards(_fadeAlpha, 1f, Time.unscaledDeltaTime * 6f);
        }

        private void OnDestroy()
        {
            Tex.Destroy(_windowBgTex);
            Tex.Destroy(_overlayTex);
            Tex.Destroy(_cardNormalTex);
            Tex.Destroy(_cardHoverTex);
            Tex.Destroy(_cardActiveTex);
            Tex.Destroy(_accentDotTex);
            Tex.Destroy(_closeBtnTex);
            Tex.Destroy(_closeBtnHoverTex);
            Tex.Destroy(_separatorTex);
        }

        // =====================================================================
        // 多语言
        // =====================================================================

        private string L(string zh, string en, string ja)
        {
            switch (_language) { case 1: return ja; case 2: return en; default: return zh; }
        }

        private string TitleText => L("选择播放设备", "Select Device", "デバイスを選択");
        private string SubText => L("选择 Spotify 播放设备", "Choose a Spotify device", "Spotify デバイスを選択");
        private string EmptyText => L("未找到设备，请打开 Spotify", "No devices found. Open Spotify.", "デバイスが見つかりません");
        private string ActiveText => L("正在使用", "Active", "使用中");
        private string CloseText => L("关闭", "Close", "閉じる");

        private static string DeviceTypeLabel(string type)
        {
            if (string.IsNullOrEmpty(type)) return "";
            switch (type.ToLower())
            {
                case "computer": return "Computer";
                case "smartphone": return "Phone";
                case "speaker": return "Speaker";
                case "tv": return "TV";
                case "tablet": return "Tablet";
                default: return type;
            }
        }

        private static int GetCurrentLanguage()
        {
            try
            {
                var ct = Type.GetType("ChillPatcher.PluginConfig, ChillPatcher");
                if (ct != null)
                {
                    var p = ct.GetProperty("DefaultLanguage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (p != null)
                    {
                        var ce = p.GetValue(null);
                        var vp = ce.GetType().GetProperty("Value");
                        if (vp != null) return (int)vp.GetValue(ce);
                    }
                }
            }
            catch { }
            return 3;
        }

        // =====================================================================
        // 纹理生成（圆角）
        // =====================================================================

        private static class Tex
        {
            public static Texture2D Solid(Color c)
            {
                var t = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = c;
                t.SetPixels(px); t.Apply(); return t;
            }

            public static Texture2D RoundRect(int w, int h, int r, Color fill)
            {
                var t = new Texture2D(w, h);
                var clear = new Color(0, 0, 0, 0);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        t.SetPixel(x, y, IsInsideRoundRect(x, y, w, h, r) ? fill : clear);
                t.Apply();
                t.filterMode = FilterMode.Bilinear;
                return t;
            }

            private static bool IsInsideRoundRect(int x, int y, int w, int h, int r)
            {
                // 四个角检查圆弧
                if (x < r && y < r) return Dist(x, y, r, r) <= r;
                if (x >= w - r && y < r) return Dist(x, y, w - r - 1, r) <= r;
                if (x < r && y >= h - r) return Dist(x, y, r, h - r - 1) <= r;
                if (x >= w - r && y >= h - r) return Dist(x, y, w - r - 1, h - r - 1) <= r;
                return true;
            }

            private static float Dist(int x1, int y1, int x2, int y2)
            {
                return Mathf.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            }

            public static void Destroy(Texture2D t) { if (t != null) UnityEngine.Object.Destroy(t); }
        }

        // =====================================================================
        // 样式初始化
        // =====================================================================

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            var winW = (int)_windowRect.width;
            var winH = (int)_windowRect.height;

            // 半透明毛玻璃风格（贴近游戏）
            _windowBgTex = Tex.RoundRect(winW, winH, 14, new Color(0.08f, 0.08f, 0.12f, 0.92f));
            _overlayTex = Tex.Solid(new Color(0, 0, 0, 0.45f));
            _cardNormalTex = Tex.RoundRect(winW - 32, 52, 8, new Color(1f, 1f, 1f, 0.05f));
            _cardHoverTex = Tex.RoundRect(winW - 32, 52, 8, new Color(1f, 1f, 1f, 0.12f));
            _cardActiveTex = Tex.RoundRect(winW - 32, 52, 8, new Color(0.114f, 0.725f, 0.329f, 0.15f));
            _accentDotTex = Tex.Solid(SpotifyGreen);
            _closeBtnTex = Tex.RoundRect(80, 28, 6, new Color(1f, 1f, 1f, 0.06f));
            _closeBtnHoverTex = Tex.RoundRect(80, 28, 6, new Color(1f, 1f, 1f, 0.14f));
            _separatorTex = Tex.Solid(new Color(1f, 1f, 1f, 0.08f));

            _windowStyle = new GUIStyle
            {
                normal = { background = _windowBgTex },
                border = new RectOffset(14, 14, 14, 14),
                padding = new RectOffset(0, 0, 0, 0)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 0.97f) }
            };

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.65f) }
            };

            _emptyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.55f) }
            };

            _closeStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 28,
                normal = { background = _closeBtnTex, textColor = new Color(0.7f, 0.7f, 0.73f) },
                hover = { background = _closeBtnHoverTex, textColor = new Color(0.95f, 0.95f, 0.97f) },
                active = { background = _closeBtnTex, textColor = new Color(0.95f, 0.95f, 0.97f) },
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(16, 16, 2, 2)
            };
        }

        // =====================================================================
        // 绘制
        // =====================================================================

        private void OnGUI()
        {
            if (!_visible) return;
            InitStyles();

            // 淡入遮罩
            var savedColor = GUI.color;
            GUI.color = new Color(1, 1, 1, _fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            // 点击遮罩关闭
            if (Event.current.type == EventType.MouseDown
                && !_windowRect.Contains(Event.current.mousePosition))
            {
                Close();
                _onCancel?.Invoke();
                Event.current.Use();
                GUI.color = savedColor;
                return;
            }

            _windowRect = GUI.Window(98766, _windowRect, DrawWindow, "", _windowStyle);
            GUI.color = savedColor;
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(16);
            GUILayout.Label(TitleText, _titleStyle);
            GUILayout.Space(1);
            GUILayout.Label(SubText, _subtitleStyle);
            GUILayout.Space(10);

            // 分隔线
            DrawSeparator();
            GUILayout.Space(6);

            if (_devices.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(EmptyText, _emptyStyle);
                GUILayout.FlexibleSpace();
            }
            else
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false,
                    GUIStyle.none, GUIStyle.none, GUIStyle.none);

                foreach (var device in _devices)
                {
                    DrawDeviceRow(device);
                    GUILayout.Space(3);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(6);
            DrawSeparator();
            GUILayout.Space(8);

            // 关闭按钮
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(CloseText, _closeStyle, GUILayout.Width(80)))
            {
                Close();
                _onCancel?.Invoke();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // 只允许标题区拖动
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 56));
        }

        private void DrawDeviceRow(SpotifyDevice device)
        {
            var isActive = device.Id == _activeDeviceId || device.IsActive;
            var rect = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));

            // 内缩 16px
            var cardRect = new Rect(rect.x + 16, rect.y, rect.width - 32, rect.height);
            var isHover = cardRect.Contains(Event.current.mousePosition);

            // 卡片背景
            var bg = isActive ? _cardActiveTex : (isHover ? _cardHoverTex : _cardNormalTex);
            GUI.DrawTexture(cardRect, bg, ScaleMode.StretchToFill);

            // 活跃指示条（左侧 3px 圆角条）
            if (isActive)
            {
                var barRect = new Rect(cardRect.x + 1, cardRect.y + 10, 3, cardRect.height - 20);
                GUI.DrawTexture(barRect, _accentDotTex);
            }

            // 设备名称（截断过长名称）
            var nameColor = isActive ? SpotifyGreen : new Color(0.92f, 0.92f, 0.94f);
            var nameRect = new Rect(cardRect.x + 14, cardRect.y + 8, cardRect.width - 100, 20);
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = nameColor },
                clipping = TextClipping.Clip
            };
            GUI.Label(nameRect, device.Name ?? "Unknown", nameStyle);

            // 设备类型（第二行小字）
            var typeLabel = DeviceTypeLabel(device.Type);
            if (device.VolumePercent.HasValue)
                typeLabel += $"  ·  Vol {device.VolumePercent}%";
            var typeRect = new Rect(cardRect.x + 14, cardRect.y + 28, cardRect.width - 100, 18);
            var typeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.5f, 0.5f, 0.55f) }
            };
            GUI.Label(typeRect, typeLabel, typeStyle);

            // 右侧状态
            if (isActive)
            {
                // 绿色圆点 + 状态文字
                var dotRect = new Rect(cardRect.xMax - 78, cardRect.y + 20, 6, 6);
                GUI.DrawTexture(dotRect, _accentDotTex);

                var statusRect = new Rect(cardRect.xMax - 68, cardRect.y + 15, 58, 20);
                var statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    normal = { textColor = SpotifyGreen }
                };
                GUI.Label(statusRect, ActiveText, statusStyle);
            }

            // 点击
            if (Event.current.type == EventType.MouseDown && isHover)
            {
                Event.current.Use();
                Close();
                _onSelect?.Invoke(device);
            }
        }

        private void DrawSeparator()
        {
            var r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            r.x += 16; r.width -= 32;
            GUI.DrawTexture(r, _separatorTex);
        }

        private void Close()
        {
            _visible = false;
            Destroy(gameObject);
        }
    }
}
