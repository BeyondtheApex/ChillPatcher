using System;
using System.IO;
using BepInEx.Logging;
using OneJS;
using Puerts;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChillPatcher
{
    /// <summary>
    /// Initializes and manages the OneJS scripting engine for custom UI overlays.
    /// Creates a UIDocument + ScriptEngine + Runner at runtime.
    /// </summary>
    public static class OneJSBridge
    {
        private static GameObject _rootGo;
        private static ScriptEngine _engine;
        private static Runner _runner;

        public static ScriptEngine Engine => _engine;
        public static bool IsInitialized => _engine != null;

        /// <summary>
        /// Initialize the OneJS engine with a working directory for user scripts.
        /// </summary>
        /// <param name="workingDir">Root directory for JS scripts (e.g. BepInEx/plugins/ChillPatcher/ui)</param>
        /// <param name="log">Logger</param>
        public static void Initialize(string workingDir, ManualLogSource log)
        {
            if (_rootGo != null)
            {
                log.LogWarning("[OneJS] Already initialized.");
                return;
            }

            try
            {
                // Ensure the working directory exists
                if (!Directory.Exists(workingDir))
                    Directory.CreateDirectory(workingDir);

                // Create the root GameObject (initially inactive to configure before Awake)
                _rootGo = new GameObject("ChillPatcher.OneJS");
                _rootGo.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(_rootGo);

                // Create PanelSettings at runtime
                var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.sortingOrder = 1000; // Render on top of game UI

                // Add UIDocument
                var uiDoc = _rootGo.AddComponent<UIDocument>();
                uiDoc.panelSettings = panelSettings;

                // Add ScriptEngine
                _engine = _rootGo.AddComponent<ScriptEngine>();

                // Configure paths: set basePath to empty so loader root == workingDir
                _engine.basePath = "";

                // Configure player working dir to point to our plugin's ui folder
                // WorkingDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), relativePath)
                // So relativePath = relative path from game root to workingDir
                var gameRoot = Path.GetDirectoryName(Application.dataPath);
                var relativePath = GetRelativePath(gameRoot, workingDir);
                _engine.playerWorkingDirInfo = new PlayerWorkingDirInfo
                {
                    baseDir = PlayerWorkingDirInfo.PlayerBaseDir.AppPath,
                    relativePath = relativePath
                };

                // Set custom loader: BepInExLoader handles bootstrap from embedded resources + user scripts from file system
                _engine.SetJsEnvLoader(new BepInExLoader(workingDir));

                // Empty arrays for serialized fields
                _engine.preloads = new TextAsset[0];
                _engine.styleSheets = new StyleSheet[0];
                _engine.globalObjects = new ObjectMappingPair[0];
                _engine.dtsGenerator = new DTSGenerator();
                _engine.miscSettings = new MiscSettings();
                _engine.editorWorkingDirInfo = new EditorWorkingDirInfo();

                // Register pre-init to define __addToGlobal (normally provided by a preload TextAsset)
                _engine.OnPreInit += jsEnv =>
                {
                    log.LogInfo("[OneJS] OnPreInit: defining __addToGlobal");
                    jsEnv.Eval("function __addToGlobal(name, obj) { globalThis[name] = obj; }");
                };

                _engine.OnPostInit += jsEnv =>
                {
                    log.LogInfo("[OneJS] OnPostInit: engine initialized successfully");
                };

                _engine.OnError += ex =>
                {
                    log.LogError($"[OneJS] Engine error: {ex}");
                };

                // Add Runner for entry file execution and hot reload
                _runner = _rootGo.AddComponent<Runner>();
                _runner.entryFile = "app.js";
                _runner.runOnStart = true;
                _runner.liveReload = true;
                _runner.pollingInterval = 500;
                _runner.standalone = true; // Enable live reload in standalone builds

                log.LogInfo($"[OneJS] GameRoot: {gameRoot}");
                log.LogInfo($"[OneJS] WorkingDir relative path: {relativePath}");
                log.LogInfo($"[OneJS] Computed WorkingDir: {_engine.WorkingDir}");
                log.LogInfo($"[OneJS] app.js exists: {File.Exists(Path.Combine(_engine.WorkingDir, "app.js"))}");

                // Activate: triggers Awake -> OnEnable -> Init
                _rootGo.SetActive(true);

                // Post-activation checks
                var uiDocCheck = _rootGo.GetComponent<UIDocument>();
                log.LogInfo($"[OneJS] UIDocument: {uiDocCheck != null}");
                log.LogInfo($"[OneJS] PanelSettings: {uiDocCheck?.panelSettings != null}");
                log.LogInfo($"[OneJS] rootVisualElement: {uiDocCheck?.rootVisualElement != null}");
                log.LogInfo($"[OneJS] Initialized. Scripts dir: {workingDir}");
            }
            catch (Exception ex)
            {
                log.LogError($"[OneJS] Initialization failed: {ex}");
                Shutdown();
            }
        }

        /// <summary>
        /// Shutdown the OneJS engine and clean up.
        /// </summary>
        public static void Shutdown()
        {
            if (_rootGo != null)
            {
                UnityEngine.Object.Destroy(_rootGo);
                _rootGo = null;
                _engine = null;
                _runner = null;
            }
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
