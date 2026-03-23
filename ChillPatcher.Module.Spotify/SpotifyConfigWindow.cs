using System;
using UnityEngine;

namespace ChillPatcher.Module.Spotify
{
    /// <summary>
    /// Client ID 配置窗口 — 与 DeviceWindow 统一设计语言。
    /// 半透明圆角窗体，贴近游戏 UI 风格。
    /// </summary>
    public class SpotifyConfigWindow : MonoBehaviour
    {
        private string _clientId = "";
        private bool _visible = true;
        private float _fadeAlpha;
        private Action<string> _onSubmit;
        private Action _onCancel;
        private int _language;

        private Rect _windowRect;
        private bool _stylesInitialized;

        // 纹理
        private Texture2D _overlayTex;
        private Texture2D _windowBgTex;
        private Texture2D _fieldBgTex;
        private Texture2D _fieldFocusTex;
        private Texture2D _btnPrimaryTex;
        private Texture2D _btnPrimaryHoverTex;
        private Texture2D _btnDisabledTex;
        private Texture2D _btnSecondaryTex;
        private Texture2D _btnSecondaryHoverTex;
        private Texture2D _separatorTex;

        // 样式
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _btnPrimaryStyle;
        private GUIStyle _btnDisabledStyle;
        private GUIStyle _btnSecondaryStyle;

        private static readonly Color SpotifyGreen = new Color(0.114f, 0.725f, 0.329f, 1f);

        public static SpotifyConfigWindow Show(Action<string> onSubmit, Action onCancel = null)
        {
            var go = new GameObject("SpotifyConfigWindow");
            DontDestroyOnLoad(go);
            var w = go.AddComponent<SpotifyConfigWindow>();
            w._onSubmit = onSubmit;
            w._onCancel = onCancel;
            w._language = GetCurrentLanguage();
            return w;
        }

        private void Start()
        {
            _windowRect = new Rect(
                Screen.width / 2f - 200,
                Screen.height / 2f - 140,
                400, 280);
        }

        private void Update()
        {
            _fadeAlpha = Mathf.MoveTowards(_fadeAlpha, 1f, Time.unscaledDeltaTime * 6f);
        }

        private void OnDestroy()
        {
            D(_overlayTex); D(_windowBgTex); D(_fieldBgTex); D(_fieldFocusTex);
            D(_btnPrimaryTex); D(_btnPrimaryHoverTex); D(_btnDisabledTex);
            D(_btnSecondaryTex); D(_btnSecondaryHoverTex); D(_separatorTex);
        }

        private static void D(Texture2D t) { if (t != null) Destroy(t); }

        // =====================================================================
        // 多语言
        // =====================================================================

        private string L(string zh, string en, string ja)
        {
            switch (_language) { case 1: return ja; case 2: return en; default: return zh; }
        }

        private string TitleText => L("Spotify 配置", "Spotify Setup", "Spotify 設定");
        private string SubText => L("连接你的 Spotify 开发者账号", "Connect your Spotify Developer account", "Spotify Developer アカウントを接続");
        private string InputLabel => L("Client ID", "Client ID", "Client ID");
        private string HintText => L(
            "1. 前往 developer.spotify.com/dashboard 创建应用\n" +
            "2. 复制 Client ID 粘贴到上方\n" +
            "3. 添加 Redirect URI：fullstop://callback",
            "1. Go to developer.spotify.com/dashboard, create an App\n" +
            "2. Copy the Client ID and paste above\n" +
            "3. Add Redirect URI: fullstop://callback",
            "1. developer.spotify.com/dashboard でアプリを作成\n" +
            "2. Client ID をコピーして上に貼り付け\n" +
            "3. Redirect URI を追加：fullstop://callback"
        );
        private string CancelText => L("取消", "Cancel", "キャンセル");
        private string OkText => L("连接", "Connect", "接続");

        private static int GetCurrentLanguage()
        {
            try
            {
                var ct = Type.GetType("ChillPatcher.PluginConfig, ChillPatcher");
                if (ct != null)
                {
                    var p = ct.GetProperty("DefaultLanguage",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
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
        // 纹理（圆角）— 复用 DeviceWindow 的生成方法
        // =====================================================================

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = c;
            t.SetPixels(px); t.Apply(); return t;
        }

        private static Texture2D RoundRect(int w, int h, int r, Color fill)
        {
            var t = new Texture2D(w, h);
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, InRR(x, y, w, h, r) ? fill : clear);
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        private static bool InRR(int x, int y, int w, int h, int r)
        {
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

        // =====================================================================
        // 样式
        // =====================================================================

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            var winW = (int)_windowRect.width;
            var winH = (int)_windowRect.height;

            _overlayTex = Solid(new Color(0, 0, 0, 0.45f));
            _windowBgTex = RoundRect(winW, winH, 14, new Color(0.08f, 0.08f, 0.12f, 0.92f));
            _fieldBgTex = RoundRect(winW - 48, 34, 8, new Color(1f, 1f, 1f, 0.06f));
            _fieldFocusTex = RoundRect(winW - 48, 34, 8, new Color(1f, 1f, 1f, 0.10f));
            _btnPrimaryTex = RoundRect(120, 34, 8, SpotifyGreen);
            _btnPrimaryHoverTex = RoundRect(120, 34, 8, new Color(0.13f, 0.82f, 0.38f, 1f));
            _btnDisabledTex = RoundRect(120, 34, 8, new Color(1f, 1f, 1f, 0.06f));
            _btnSecondaryTex = RoundRect(100, 34, 8, new Color(1f, 1f, 1f, 0.06f));
            _btnSecondaryHoverTex = RoundRect(100, 34, 8, new Color(1f, 1f, 1f, 0.14f));
            _separatorTex = Solid(new Color(1f, 1f, 1f, 0.08f));

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

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.73f) },
                padding = new RectOffset(24, 24, 0, 0)
            };

            _fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                fixedHeight = 34,
                normal = { background = _fieldBgTex, textColor = new Color(0.92f, 0.92f, 0.94f) },
                focused = { background = _fieldFocusTex, textColor = Color.white },
                hover = { background = _fieldBgTex, textColor = new Color(0.92f, 0.92f, 0.94f) },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(12, 12, 8, 8)
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.5f, 0.5f, 0.55f) },
                padding = new RectOffset(24, 24, 0, 0)
            };

            _btnPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 34,
                normal = { background = _btnPrimaryTex, textColor = Color.white },
                hover = { background = _btnPrimaryHoverTex, textColor = Color.white },
                active = { background = _btnPrimaryTex, textColor = Color.white },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(16, 16, 4, 4)
            };

            _btnDisabledStyle = new GUIStyle(_btnPrimaryStyle)
            {
                normal = { background = _btnDisabledTex, textColor = new Color(0.4f, 0.4f, 0.43f) },
                hover = { background = _btnDisabledTex, textColor = new Color(0.4f, 0.4f, 0.43f) },
                active = { background = _btnDisabledTex, textColor = new Color(0.4f, 0.4f, 0.43f) }
            };

            _btnSecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 34,
                normal = { background = _btnSecondaryTex, textColor = new Color(0.7f, 0.7f, 0.73f) },
                hover = { background = _btnSecondaryHoverTex, textColor = new Color(0.95f, 0.95f, 0.97f) },
                active = { background = _btnSecondaryTex, textColor = new Color(0.95f, 0.95f, 0.97f) },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(16, 16, 4, 4)
            };
        }

        // =====================================================================
        // 绘制
        // =====================================================================

        private void OnGUI()
        {
            if (!_visible) return;
            InitStyles();

            var saved = GUI.color;
            GUI.color = new Color(1, 1, 1, _fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            // 点击遮罩关闭
            if (Event.current.type == EventType.MouseDown
                && !_windowRect.Contains(Event.current.mousePosition))
            {
                Close();
                _onCancel?.Invoke();
                Event.current.Use();
                GUI.color = saved;
                return;
            }

            _windowRect = GUI.Window(98765, _windowRect, DrawWindow, "", _windowStyle);
            GUI.color = saved;
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(16);
            GUILayout.Label(TitleText, _titleStyle);
            GUILayout.Space(1);
            GUILayout.Label(SubText, _subtitleStyle);
            GUILayout.Space(10);

            // 分隔线
            DrawSep();
            GUILayout.Space(10);

            // Client ID 标签
            GUILayout.Label(InputLabel, _labelStyle);
            GUILayout.Space(4);

            // 输入框（两侧留白）
            GUILayout.BeginHorizontal();
            GUILayout.Space(24);
            GUI.SetNextControlName("ClientIdField");
            _clientId = GUILayout.TextField(_clientId, _fieldStyle);
            GUILayout.Space(24);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // 提示
            GUILayout.Label(HintText, _hintStyle);

            GUILayout.FlexibleSpace();

            // 分隔线
            DrawSep();
            GUILayout.Space(10);

            // 按钮区
            GUILayout.BeginHorizontal();
            GUILayout.Space(24);

            if (GUILayout.Button(CancelText, _btnSecondaryStyle, GUILayout.Width(100)))
            {
                Close();
                _onCancel?.Invoke();
            }

            GUILayout.FlexibleSpace();

            var valid = !string.IsNullOrWhiteSpace(_clientId)
                && _clientId.Length >= 10
                && _clientId != "YOUR_SPOTIFY_CLIENT_ID";

            if (valid)
            {
                if (GUILayout.Button(OkText, _btnPrimaryStyle, GUILayout.Width(120)))
                {
                    Close();
                    _onSubmit?.Invoke(_clientId.Trim());
                }
            }
            else
            {
                GUILayout.Button(OkText, _btnDisabledStyle, GUILayout.Width(120));
            }

            GUILayout.Space(24);
            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 56));
        }

        private void DrawSep()
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
