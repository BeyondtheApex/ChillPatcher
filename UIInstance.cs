using System;
using System.IO;
using BepInEx.Logging;
using ChillPatcher.JSApi;
using OneJS;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChillPatcher
{
    /// <summary>
    /// 表示一个隔离的 JS 引擎 UI 实例。
    /// 每个实例拥有独立的 GameObject、UIDocument、PanelSettings、ScriptEngine 和 Runner。
    /// 多个实例通过 PanelSettings.sortingOrder 实现层叠渲染。
    /// </summary>
    public class UIInstance : IDisposable
    {
        private readonly ManualLogSource _log;
        private GameObject _rootGo;
        private ScriptEngine _engine;
        private Runner _runner;
        private ChillJSApi _jsApi;
        private DateTime _lastWriteTime;
        private float _lastCheckTime;
        private bool _enabled;
        private bool _isWallpaperEngineMode;

        private static readonly float HotReloadInterval = 0.3f;

        /// <summary>唯一标识符</summary>
        public string Id { get; }

        /// <summary>UI 工作目录</summary>
        public string WorkingDir { get; }

        /// <summary>入口脚本文件（相对于 WorkingDir）</summary>
        public string EntryFile { get; }

        /// <summary>UIDocument 层叠排序值，越大越靠前</summary>
        public int SortingOrder { get; private set; }

        /// <summary>是否允许交互（鼠标事件不穿透）</summary>
        public bool Interactive { get; set; }

        /// <summary>ScriptEngine 引用</summary>
        public ScriptEngine Engine => _engine;

        /// <summary>此实例的 JS API</summary>
        public ChillJSApi JSApi => _jsApi;

        /// <summary>是否已完成初始化</summary>
        public bool IsInitialized => _engine != null;

        /// <summary>实例是否已启用</summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (_rootGo != null)
                    _rootGo.SetActive(value);
            }
        }

        public UIInstance(string id, string workingDir, string entryFile, int sortingOrder, bool enabled, bool interactive, ManualLogSource log)
        {
            Id = id;
            WorkingDir = workingDir;
            EntryFile = entryFile;
            SortingOrder = sortingOrder;
            _enabled = enabled;
            Interactive = interactive;
            _log = log;

            if (!Directory.Exists(workingDir))
                Directory.CreateDirectory(workingDir);
        }

        /// <summary>
        /// 创建 GameObject 层级和所有 Unity 组件，启动 JS 引擎。
        /// 应在 RoomScene 加载后的主线程上调用。
        /// </summary>
        public void Initialize()
        {
            if (_rootGo != null) return;

            _log.LogInfo($"[UIInstance:{Id}] Initializing (sortOrder={SortingOrder}, enabled={_enabled})...");

            _rootGo = new GameObject($"ChillPatcher.OneJS.{Id}");
            UnityEngine.Object.DontDestroyOnLoad(_rootGo);
            _rootGo.SetActive(false); // 配置完再激活

            // PanelSettings
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.sortingOrder = SortingOrder;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;

            var theme = Resources.Load<ThemeStyleSheet>("UnityThemes/UnityDefaultRuntimeTheme");
            if (theme != null)
                panelSettings.themeStyleSheet = theme;

            // UIDocument
            var uiDoc = _rootGo.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;

            // ScriptEngine
            _engine = _rootGo.AddComponent<ScriptEngine>();
            _engine.basePath = "@outputs/esbuild/";

            var gameRoot = Path.GetDirectoryName(Application.dataPath);
            var relativePath = GetRelativePath(gameRoot, WorkingDir);
            _engine.playerWorkingDirInfo = new PlayerWorkingDirInfo
            {
                baseDir = PlayerWorkingDirInfo.PlayerBaseDir.AppPath,
                relativePath = relativePath
            };

            _engine.SetJsEnvLoader(new BepInExLoader(WorkingDir));
            _engine.preloads = new TextAsset[0];
            _engine.styleSheets = new StyleSheet[0];
            _engine.globalObjects = new ObjectMappingPair[0];
            _engine.dtsGenerator = new DTSGenerator();
            _engine.miscSettings = new MiscSettings();
            _engine.editorWorkingDirInfo = new EditorWorkingDirInfo();

            _engine.OnPreInit += jsEnv =>
            {
                jsEnv.Eval("function __addToGlobal(name, obj) { globalThis[name] = obj; }");
            };

            _engine.OnPostInit += jsEnv =>
            {
                try
                {
                    _jsApi = new ChillJSApi(_log, WorkingDir);
                    // 第一个初始化的实例设为全局 Instance（保持兼容）
                    if (ChillJSApi.Instance == null)
                        ChillJSApi.Instance = _jsApi;

                    _engine.AddToGlobal("chill", _jsApi);
                    _engine.AddToGlobal("__instanceId", Id);
                    _log.LogInfo($"[UIInstance:{Id}] chill API injected");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[UIInstance:{Id}] Failed to inject ChillJSApi: {ex}");
                }
            };

            _engine.OnError += ex =>
            {
                _log.LogError($"[UIInstance:{Id}] Engine error: {ex}");
            };

            // Runner
            _runner = _rootGo.AddComponent<Runner>();
            _runner.entryFile = EntryFile;
            _runner.runOnStart = true;
            _runner.liveReload = false;
            _runner.standalone = false;

            // 根据 enabled 状态激活
            _rootGo.SetActive(_enabled);

            // 设置根视觉元素
            if (_enabled)
                ConfigureRootVisualElement(uiDoc);

            // 记录初始文件时间用于热重载
            RecordInitialWriteTime();

            _log.LogInfo($"[UIInstance:{Id}] Initialized successfully");
        }

        /// <summary>
        /// 每帧调用：驱动 JsEnv.Tick() 并轮询热重载。
        /// </summary>
        public void Tick()
        {
            if (!_enabled || _engine == null || _engine.JsEnv == null) return;

            try
            {
                _engine.JsEnv.Tick();
            }
            catch (Exception ex)
            {
                _log.LogError($"[UIInstance:{Id}] JsEnv.Tick error: {ex}");
            }

            // WE 模式 + 非交互：JsEnv.Tick() 后立即刷新（Preact 在 Tick 中创建新元素）
            if (_isWallpaperEngineMode && !Interactive)
            {
                var rootVE = _engine.GetComponent<UIDocument>()?.rootVisualElement;
                if (rootVE != null)
                    SetAllPickingModeIgnore(rootVE);
            }

            // 热重载
            var now = Time.realtimeSinceStartup;
            if (now - _lastCheckTime >= HotReloadInterval)
            {
                _lastCheckTime = now;
                try
                {
                    var fullpath = _engine.GetFullPath(_runner.entryFile);
                    if (File.Exists(fullpath))
                    {
                        var writeTime = File.GetLastWriteTime(fullpath);
                        if (_lastWriteTime != default && writeTime != _lastWriteTime)
                        {
                            _log.LogInfo($"[UIInstance:{Id}] Hot reload: {_runner.entryFile} changed");
                            _runner.Reload();
                        }
                        _lastWriteTime = writeTime;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[UIInstance:{Id}] Hot reload check error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 重新加载此实例的 JS 引擎。
        /// </summary>
        public void Reload()
        {
            if (_runner != null)
            {
                _log.LogInfo($"[UIInstance:{Id}] Reloading...");
                _runner.Reload();
            }
        }

        /// <summary>
        /// 更新层叠排序值。
        /// </summary>
        public void SetSortingOrder(int order)
        {
            SortingOrder = order;
            if (_rootGo != null)
            {
                var uiDoc = _rootGo.GetComponent<UIDocument>();
                if (uiDoc?.panelSettings != null)
                    uiDoc.panelSettings.sortingOrder = order;
            }
        }

        public void Dispose()
        {
            if (_jsApi != null)
            {
                // 只在全局 Instance 是自己时清除
                if (ChillJSApi.Instance == _jsApi)
                {
                    ChillJSApi.Instance = null;
                }
                _jsApi.Dispose();
                _jsApi = null;
            }

            if (_rootGo != null)
            {
                UnityEngine.Object.Destroy(_rootGo);
                _rootGo = null;
                _engine = null;
                _runner = null;
            }

            _log.LogInfo($"[UIInstance:{Id}] Disposed");
        }

        /// <summary>
        /// 递归设所有 VisualElement 为 PickingMode.Ignore，
        /// 使 WE 模式下整个 panel 对输入完全透明。
        /// 必须使用 hierarchy（包含内部元素如滚动条），而非 contentContainer。
        /// </summary>
        private static void SetAllPickingModeIgnore(VisualElement ve)
        {
            ve.pickingMode = PickingMode.Ignore;
            var h = ve.hierarchy;
            for (int i = 0; i < h.childCount; i++)
                SetAllPickingModeIgnore(h[i]);
        }

        private void ConfigureRootVisualElement(UIDocument uiDoc)
        {
            var rootVE = uiDoc.rootVisualElement;
            if (rootVE == null) return;

            rootVE.style.width = new StyleLength(Length.Percent(100));
            rootVE.style.height = new StyleLength(Length.Percent(100));

            _isWallpaperEngineMode = PluginConfig.EnableWallpaperEngineMode?.Value == true;

            if (_isWallpaperEngineMode)
            {
                // WE 模式：root=Ignore → WE 内置的 UIToolkit/UGUI 优先级正确工作
                // WE 会独立向子元素投递事件，不依赖 root pickingMode
                rootVE.pickingMode = PickingMode.Ignore;

                if (!Interactive)
                {
                    // WE 模式下 root=Ignore 不足以阻止子元素交互，
                    // 需要递归设所有子元素为 Ignore
                    SetAllPickingModeIgnore(rootVE);
                }
            }
            else
            {
                // 桌面模式：root=Position 才能让 RuntimeEventSystem 处理 UIToolkit 事件
                // Interactive=false 时 root=Ignore → panel 被跳过，UIToolkit 不接收事件
                rootVE.pickingMode = Interactive ? PickingMode.Position : PickingMode.Ignore;
            }

            // 加载字体：优先使用实例本地字体，其次全局配置
            Font loadedFont = null;

            // 1. 扫描实例目录下的 fonts/ 子文件夹
            var instanceFontsDir = Path.Combine(WorkingDir, "fonts");
            if (Directory.Exists(instanceFontsDir))
            {
                try
                {
                    var fontFiles = Directory.GetFiles(instanceFontsDir, "*.*");
                    foreach (var ff in fontFiles)
                    {
                        var ext = Path.GetExtension(ff).ToLowerInvariant();
                        if (ext != ".ttf" && ext != ".otf") continue;

                        try
                        {
                            var instanceFont = new Font(ff);
                            _log.LogInfo($"[UIInstance:{Id}] Auto-loaded instance font: {Path.GetFileName(ff)}");
                            if (loadedFont == null)
                                loadedFont = instanceFont;
                        }
                        catch (Exception ex)
                        {
                            _log.LogWarning($"[UIInstance:{Id}] Failed to load instance font {Path.GetFileName(ff)}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[UIInstance:{Id}] Failed to scan instance fonts dir: {ex.Message}");
                }
            }

            if (loadedFont != null)
            {
                rootVE.style.unityFont = new StyleFont(loadedFont);
            }
        }

        private void RecordInitialWriteTime()
        {
            try
            {
                if (_engine != null)
                {
                    var entryFullPath = _engine.GetFullPath(_runner.entryFile);
                    if (File.Exists(entryFullPath))
                        _lastWriteTime = File.GetLastWriteTime(entryFullPath);
                }
            }
            catch { }
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            var fromUri = new Uri(fromPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var toUri = new Uri(toPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var relUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relUri.ToString()).Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}
