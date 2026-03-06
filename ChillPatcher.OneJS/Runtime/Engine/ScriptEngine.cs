using System;
using System.Collections;
using System.IO;
using System.Linq;
using OneJS.Dom;
using Puerts;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS {
    [RequireComponent(typeof(UIDocument))] [AddComponentMenu("OneJS/ScriptEngine")]
    public class ScriptEngine : MonoBehaviour, IScriptEngine, IDisposable {
        public int Tick => _tick;

        #region Public Fields
        [Tooltip("Set the OneJS Working Directory for Editor Mode.")]
        [PairMapping("baseDir", "relativePath", "/", "Editor WorkingDir")]
        public EditorWorkingDirInfo editorWorkingDirInfo;

        [Tooltip("Set the OneJS Working Directory for Standalone build.")]
        [PairMapping("baseDir", "relativePath", "/", "Player WorkingDir")]
        [SerializeField] public PlayerWorkingDirInfo playerWorkingDirInfo;

        [Tooltip("JS files that you want to preload before running anything else.")]
        public TextAsset[] preloads;

        [Tooltip("Global objects that you want to expose to the JS environment. This list accepts any UnityEngine.Object, not just MonoBehaviours. There's a little trick in picking a specific MonoBehaviour component. You right-click on the Inspector Tab of the selected GameObject and pick Properties. A standalone window will pop up for you to drag the specifc MonoBehavior from.")]
        [PairMapping("obj", "name")]
        public ObjectMappingPair[] globalObjects;

        [Tooltip("Include here any global USS you'd need. i.e. if you are working with Tailwind, make sure to include the output *.uss here.")]
        public StyleSheet[] styleSheets;

        public DTSGenerator dtsGenerator;

        public bool debuggerSupport = false;
        public string basePath = "@outputs/esbuild/";
        public int port = 8080;
        public MiscSettings miscSettings;
        #endregion

        #region Events
        public event Action<JsEnv> OnPreInit;
        public event Action<JsEnv> OnPostInit;
        public event Action OnReload;
        public event Action OnDispose;
        public event Action<Exception> OnError;
        #endregion

        #region Private Fields
        JsEnv _jsEnv;
        EngineHost _engineHost;

        UIDocument _uiDocument;
        Document _document;
        Resource _resource;
        ILoader _jsEnvLoader;
        int _tick;

        Action<string, object> _addToGlobal;
        #endregion

        #region Lifecycles
        void Awake() {
            _uiDocument = GetComponent<UIDocument>();
            _resource = new Resource(this);
            if (_jsEnvLoader == null)
                _jsEnvLoader = new DefaultLoader(Path.Combine(WorkingDir, basePath));
        }

        void OnEnable() {
            Init();
        }

        void OnDisable() {
            Dispose();
        }

        void Update() {
            try {
                _jsEnv.Tick();
                _tick++;
            } catch (Exception e) {
                Debug.LogError($"OneJS Error: {e.Message}\n{e.StackTrace}");
                OnError?.Invoke(e);
            }
        }
        #endregion

        #region Properties
        public string WorkingDir {
            get {
#if UNITY_EDITOR
                var path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!,
                    editorWorkingDirInfo.relativePath);
                if (editorWorkingDirInfo.baseDir == EditorWorkingDirInfo.EditorBaseDir.PersistentDataPath)
                    path = Path.Combine(Application.persistentDataPath, editorWorkingDirInfo.relativePath);
                if (editorWorkingDirInfo.baseDir == EditorWorkingDirInfo.EditorBaseDir.StreamingAssetsPath)
                    path = Path.Combine(Application.streamingAssetsPath, editorWorkingDirInfo.relativePath);
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
#if MULTIPLAYER_PLAYMODE_ENABLED
                if (path.Contains($"Library{Path.DirectorySeparatorChar}VP")) {
                    // MPPM is active
                    path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!,
                        "..", "..", "..",
                        editorWorkingDirInfo.relativePath);
                }
#endif
                return path;
#else
                var path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!,
                    playerWorkingDirInfo.relativePath);
                if (playerWorkingDirInfo.baseDir == PlayerWorkingDirInfo.PlayerBaseDir.PersistentDataPath)
                    path = Path.Combine(Application.persistentDataPath, playerWorkingDirInfo.relativePath);
                if (playerWorkingDirInfo.baseDir == PlayerWorkingDirInfo.PlayerBaseDir.StreamingAssetsPath)
                    path = Path.Combine(Application.streamingAssetsPath, playerWorkingDirInfo.relativePath);
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                return path;
#endif
            }
        }

        public JsEnv JsEnv => _jsEnv;

        public UIDocument UIDocument => _uiDocument;

        public Action<string, object> AddToGlobal => _addToGlobal;
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the full path of a filepath relative to the WorkingDir.
        /// </summary>
        /// <param name="filepath">The filepath relative to the WorkingDir</param>
        /// <returns>The full path of the filepath</returns>
        public string GetFullPath(string filepath) {
            var normalizedPath = filepath.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(WorkingDir, normalizedPath));
        }

        public void Dispose() {
            OnDispose?.Invoke();
            if (_jsEnv != null) {
                _jsEnv.Dispose();
            }
            if (_uiDocument.rootVisualElement != null) {
                _uiDocument.rootVisualElement.Clear();
                _uiDocument.rootVisualElement.styleSheets.Clear();
            }
        }

        public void Reload() {
            OnReload?.Invoke();
            Dispose();
            Init();
#if UNITY_EDITOR
            StartCoroutine(RefreshStyleSheets());
#endif
        }

        /**
         * Performs a lightweight refresh without recreating the JsEnv.
         * Clears the rootVisualElement and triggers OnReload events,
         * but does not dispose or reinitialize the JS engine.
         * Useful on platforms like WebGL where full reloads are not possible.
         */
        public void Refresh() {
            OnReload?.Invoke();
            _document.clearRuntimeStyleSheets();
            if (_uiDocument.rootVisualElement != null) {
                _uiDocument.rootVisualElement.Clear();
            }
        }

        /// MARK: Init
        /// <summary>
        /// Initializes the JS environment, sets up the DOM bridge, and exposes global objects to JavaScript.
        /// </summary>
        void Init() {
            if (_jsEnv != null) {
                _jsEnv.Dispose();
            }

            _jsEnv = new JsEnv(_jsEnvLoader, debuggerSupport ? port : -1);

#if UNITY_WEBGL && UNITY_STANDALONE
            _jsEnv.Eval("globalThis.ONEJS_WEBGL = true;");
#endif
#if UNITY_6000_0_OR_NEWER
            _jsEnv.Eval("globalThis.UNITY_6000_0_OR_NEWER = true;");
#endif

            // Some default UsingActions here. Please use OnPreInit to add more if needed (in your own code).
            _jsEnv.UsingAction<Action>();
            _jsEnv.UsingAction<float>();
            _jsEnv.UsingAction<int>();
            _jsEnv.UsingAction<string>();
            _jsEnv.UsingAction<bool>();

            Dom.Dom.AddEventsFromTypes(dtsGenerator.GetAllTypes());

            OnPreInit?.Invoke(_jsEnv);

            _engineHost = new EngineHost(this);

            if (_uiDocument.rootVisualElement != null) {
                _uiDocument.rootVisualElement.Clear();
                _uiDocument.rootVisualElement.styleSheets.Clear();
                _uiDocument.rootVisualElement.AddToClassList("root");
            }

            foreach (var preload in preloads) {
                _jsEnv.Eval(preload.text);
            }
            styleSheets.ToList().ForEach(s => _uiDocument.rootVisualElement.styleSheets.Add(s));
            _document = new Document(_uiDocument.rootVisualElement, this);
            _addToGlobal = _jsEnv.Eval<Action<string, object>>(@"__addToGlobal");
            _addToGlobal("___document", _document);
            _addToGlobal("___workingDir", WorkingDir);
            _addToGlobal("resource", _resource);
            _addToGlobal("onejs", _engineHost);
            foreach (var obj in globalObjects) {
                _addToGlobal(obj.name, obj.obj);
            }
            OnPostInit?.Invoke(_jsEnv);
        }

        /// <summary>
        /// Evaluate a script file at the given path.
        /// </summary>
        /// <param name="filepath">Relative to the WorkingDir</param>
        public void EvalFile(string filepath) {
            var fullpath = GetFullPath(filepath);
            if (!File.Exists(fullpath)) {
                Debug.LogError($"Entry file not found: {fullpath}");
                return;
            }
            // var filename = Path.GetFileName(fullpath);
            var code = File.ReadAllText(fullpath);
            _jsEnv.Eval(code, filepath);
        }

        /// <summary>
        /// Evaluate a code string.
        /// </summary>
        /// <param name="code">The code string</param>
        /// <param name="chunkName">The name of the chunk</param>
        public void Eval(string code, string chunkName = "chunk") {
            _jsEnv.Eval(code, chunkName);
        }

        public void SetJsEnvLoader(ILoader loader) {
            _jsEnvLoader = loader;
        }
        #endregion

        #region Private Methods
#if UNITY_EDITOR
        /// <summary>
        /// This is for convenience for Live-Reload. Stylesheets need explicit refreshing
        /// when Unity Editor doesn't have focus. Otherwise, stylesheet changes won't be
        /// reflected in the Editor until it gains focus.
        /// </summary>
        IEnumerator RefreshStyleSheets() {
            yield return new WaitForSeconds(miscSettings.styleSheetRefreshDelay);
            foreach (var ss in styleSheets) {
                if (ss != null) {
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(ss);
                    if (!string.IsNullOrEmpty(assetPath)) {
                        UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                    }
                }
            }
        }
#endif
        #endregion

        #region ContextMenus
#if UNITY_EDITOR
        [ContextMenu("Generate Globals Definitions")]
        public void GenerateGlobalsDefinitions() {
            var filename = OneJS.Editor.EditorInputDialog.Show("Enter the file name", "", "globals.d.ts");
            if (string.IsNullOrEmpty(filename))
                return;
            var definitionContents = "";
            foreach (var obj in globalObjects) {
                // var objType = obj.obj.GetType();
                // if (string.IsNullOrEmpty(objType.Namespace))
                //     continue;
                definitionContents += $"declare const {obj.name}: any;\n";
            }
            File.WriteAllText(Path.Combine(Application.dataPath, $"Gen/Typing/csharp/{filename}"), definitionContents);
        }
#endif
        #endregion
    }

    #region Extras
    [Serializable]
    public class ObjectMappingPair {
        public UnityEngine.Object obj;
        public string name;

        public ObjectMappingPair(UnityEngine.Object obj, string m) {
            this.obj = obj;
            this.name = m;
        }
    }

    [Serializable]
    public class EditorWorkingDirInfo {
        public EditorBaseDir baseDir;
        public string relativePath = "App";

        public enum EditorBaseDir {
            ProjectPath,
            PersistentDataPath,
            StreamingAssetsPath
        }

        public override string ToString() {
            var basePath = baseDir switch {
                EditorBaseDir.ProjectPath => Path.GetDirectoryName(Application.dataPath),
                EditorBaseDir.PersistentDataPath => Application.persistentDataPath,
                _ => throw new ArgumentOutOfRangeException()
            };
            return Path.Combine(basePath, relativePath);
        }
    }

    [Serializable]
    public class PlayerWorkingDirInfo {
        public PlayerBaseDir baseDir;
        public string relativePath = "App";

        public enum PlayerBaseDir {
            PersistentDataPath,
            StreamingAssetsPath,
            AppPath,
        }

        public override string ToString() {
            var basePath = baseDir switch {
                PlayerBaseDir.PersistentDataPath => Application.persistentDataPath,
                PlayerBaseDir.AppPath => Path.GetDirectoryName(Application.dataPath),
                _ => throw new ArgumentOutOfRangeException()
            };
            return Path.Combine(basePath, relativePath);
        }
    }

    [Serializable]
    public class MiscSettings {
        [Tooltip("Delay before forcing stylesheet re-import to allow live changes to register properly")]
        public float styleSheetRefreshDelay = 0.1f;
    }
    #endregion
}