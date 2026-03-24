using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChillPatcher.JSApi;
using ChillPatcher.Native;
using OneJS;
using UnityEngine;

namespace ChillPatcher
{
    /// <summary>
    /// 管理多个隔离的 OneJS 脚本引擎实例。
    /// 每个实例拥有独立的 GameObject / UIDocument / ScriptEngine，通过 sortingOrder 层叠渲染。
    /// 支持独立开关、热重载和配置持久化。
    /// </summary>
    public static class OneJSBridge
    {
        private static ManualLogSource _log;
        private static string _workingDir;
        private static bool _initRequested;
        private static bool _initDone;
        private static int _tickCount;
        private static bool _diagDone;
        private static bool _buildSetupDone;

        private static readonly Dictionary<string, UIInstance> _instances
            = new Dictionary<string, UIInstance>();

        // 向后兼容：指向 "default" 实例
        public static ScriptEngine Engine => GetInstance("default")?.Engine;
        public static bool IsInitialized => _initDone;
        public static ChillJSApi JSApi => GetInstance("default")?.JSApi;

        /// <summary>所有实例</summary>
        public static IReadOnlyDictionary<string, UIInstance> Instances => _instances;

        /// <summary>获取指定 ID 的实例，不存在则返回 null</summary>
        public static UIInstance GetInstance(string id)
        {
            _instances.TryGetValue(id, out var inst);
            return inst;
        }

        /// <summary>
        /// 初始化请求。实际创建延迟到 PlayerLoop 首次 tick 且 RoomScene 加载后。
        /// </summary>
        public static void Initialize(string workingDir, ConfigFile config, ManualLogSource log)
        {
            if (_initRequested)
            {
                log.LogWarning("[OneJS] Already initialized/requested.");
                return;
            }

            _log = log;
            _workingDir = workingDir;
            _initRequested = true;

            if (!Directory.Exists(workingDir))
                Directory.CreateDirectory(workingDir);

            // 注册进程退出回调作为安全网（应对崩溃/强制终止等 OnApplicationQuit 未调用的场景）
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            // 加载实例配置（写入 BepInEx 主配置文件）
            UIInstanceConfig.Initialize(config, workingDir, log);

            log.LogInfo("[OneJS] Initialization deferred until first PlayerLoop tick.");
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            try { Shutdown(); } catch { }
        }

        /// <summary>
        /// 每帧由 PlayerLoopInjector 调用。处理延迟初始化和所有实例的 tick。
        /// </summary>
        public static void Tick()
        {
            if (!_initRequested) return;
            _tickCount++;

            if (!_initDone)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.name != "RoomScene")
                    return;

                if (!_buildSetupDone)
                {
                    EnsureBuildSetup();
                    return;
                }

                try
                {
                    DoDeferredInit();
                    _initDone = true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[OneJS] Deferred init failed: {ex}");
                    _initRequested = false;
                    return;
                }

                _tickCount = 0;
                return;
            }

            // UIToolkit 键盘输入分发（早于 TMP LateUpdate 消费队列）
            UIToolkitInputDispatcher.Tick();

            // Tick 所有启用的实例
            foreach (var kv in _instances)
            {
                kv.Value.Tick();
            }

            if (_tickCount == 5 && !_diagDone)
            {
                _diagDone = true;
                RunDiagnostics();
            }
        }

        /// <summary>
        /// Ensures npm install is done and esbuild watch is running for each instance.
        /// Uses Go-based EsbuildBridge DLL (no Node.js dependency).
        /// Falls back to pre-built bundles if DLL is not available.
        /// </summary>
        private static void EnsureBuildSetup()
        {
            var config = UIInstanceConfig.Data;
            if (config?.Instances == null || config.Instances.Count == 0)
            {
                _buildSetupDone = true;
                return;
            }

            if (!PluginConfig.EnableNpmAndEsbuild.Value)
            {
                _log.LogInfo("[OneJS] npm/esbuild 已禁用，使用已编译的JS文件");
                _buildSetupDone = true;
                return;
            }

            if (!EsbuildBridge.IsLoaded)
            {
                _log.LogInfo("[OneJS] EsbuildBridge DLL not available, using pre-built bundles");
                _buildSetupDone = true;
                return;
            }

            foreach (var entry in config.Instances)
            {
                var dir = entry.WorkingDir;
                var packageJson = Path.Combine(dir, "package.json");
                if (!File.Exists(packageJson)) continue;

                // npm install from lockfile (Go-based, no system npm needed)
                bool npmFailed = false;
                if (!Directory.Exists(Path.Combine(dir, "node_modules")))
                {
                    _log.LogInfo($"[OneJS:{entry.Id}] Installing npm packages...");
                    var npmErr = EsbuildBridge.NpmInstall(dir, (pkgPath, status, msg) =>
                    {
                        switch (status)
                        {
                            case "download":
                                if (string.IsNullOrEmpty(pkgPath))
                                    _log.LogInfo($"[OneJS:{entry.Id}] npm: {msg}");
                                else
                                    _log.LogInfo($"[OneJS:{entry.Id}] npm: downloading {pkgPath}@{msg}");
                                break;
                            case "done":
                                _log.LogInfo($"[OneJS:{entry.Id}] npm: installed {pkgPath}@{msg}");
                                break;
                            case "error":
                                _log.LogWarning($"[OneJS:{entry.Id}] npm: FAILED {pkgPath}: {msg}");
                                break;
                        }
                    });
                    if (npmErr != null)
                    {
                        _log.LogWarning($"[OneJS:{entry.Id}] npm install error: {npmErr}");
                        npmFailed = true;
                    }
                    else
                    {
                        _log.LogInfo($"[OneJS:{entry.Id}] npm install completed successfully");
                    }
                }

                // If npm install failed (e.g. network timeout), fall back to pre-built @outputs
                if (npmFailed)
                {
                    var fallbackOutput = Path.Combine(dir, "@outputs", "esbuild", "app.js");
                    if (File.Exists(fallbackOutput))
                    {
                        _log.LogWarning($"[OneJS:{entry.Id}] npm install failed, falling back to pre-built @outputs");
                        continue; // skip build/watch, use existing @outputs
                    }
                    else
                    {
                        _log.LogError($"[OneJS:{entry.Id}] npm install failed and no pre-built @outputs found, UI may not load");
                        continue;
                    }
                }

                // Build framework entry once if output missing
                var esbuildOutput = Path.Combine(dir, "@outputs", "esbuild", "app.js");
                if (!File.Exists(esbuildOutput))
                {
                    _log.LogInfo($"[OneJS:{entry.Id}] Building UI...");
                    var buildErr = EsbuildBridge.Build(BuildEsbuildConfigJson(dir, "index.tsx", "@outputs/esbuild/app.js"));
                    if (buildErr != null)
                        _log.LogWarning($"[OneJS:{entry.Id}] Build failed: {buildErr}");
                }

                // Start watch for framework
                StartEsbuildWatchViaGo(dir, entry.Id, "index.tsx", "@outputs/esbuild/app.js");

                // Discover and watch plugins
                var pluginsDir = Path.Combine(dir, "plugins");
                if (Directory.Exists(pluginsDir))
                {
                    foreach (var pluginPath in Directory.GetDirectories(pluginsDir))
                    {
                        var pluginName = Path.GetFileName(pluginPath);
                        var pluginEntry = $"plugins/{pluginName}/index.tsx";
                        if (!File.Exists(Path.Combine(dir, pluginEntry))) continue;

                        var pluginOut = $"@outputs/plugins/{pluginName}/app.js";
                        if (!File.Exists(Path.Combine(dir, pluginOut)))
                        {
                            var err = EsbuildBridge.Build(BuildEsbuildConfigJson(dir, pluginEntry, pluginOut));
                            if (err != null)
                                _log.LogWarning($"[OneJS:{entry.Id}] Plugin '{pluginName}' build error: {err}");
                        }
                        StartEsbuildWatchViaGo(dir, entry.Id, pluginEntry, pluginOut);
                    }
                }
            }

            _buildSetupDone = true;
        }

        private static readonly List<int> _esbuildWatchIds = new List<int>();

        private static void StartEsbuildWatchViaGo(string workingDir, string instanceId, string entryPoint, string outfile)
        {
            var configJson = BuildEsbuildConfigJson(workingDir, entryPoint, outfile);
            var watchId = EsbuildBridge.Watch(configJson);
            if (watchId > 0)
            {
                _esbuildWatchIds.Add(watchId);
                _log.LogInfo($"[OneJS:{instanceId}] esbuild watch started (watchId={watchId}, entry={entryPoint})");
            }
            else
            {
                _log.LogWarning($"[OneJS:{instanceId}] Failed to start esbuild watch for {entryPoint}");
            }
        }

        private static string BuildEsbuildConfigJson(string workingDir, string entryPoint, string outfile)
        {
            var escaped = workingDir.Replace("\\", "\\\\");
            var entry = entryPoint.Replace("\\", "/");
            return "{" +
                "\"workingDir\":\"" + escaped + "\"," +
                "\"entryPoints\":[\"" + entry + "\"]," +
                "\"outfile\":\"" + outfile + "\"," +
                "\"inject\":[\"node_modules/onejs-core/dist/index.js\"]," +
                "\"alias\":{" +
                    "\"onejs\":\"onejs-core\"," +
                    "\"preact\":\"onejs-preact\"," +
                    "\"react\":\"onejs-preact/compat\"," +
                    "\"react-dom\":\"onejs-preact/compat\"" +
                "}," +
                "\"sourcemap\":true," +
                "\"jsxFactory\":\"h\"," +
                "\"jsxFragment\":\"Fragment\"," +
                "\"platform\":\"node\"" +
            "}";
        }

        private static void DoDeferredInit()
        {
            _log.LogInfo("[OneJS] Starting deferred initialization (multi-instance)...");

            var config = UIInstanceConfig.Data;
            if (config?.Instances == null || config.Instances.Count == 0)
            {
                _log.LogWarning("[OneJS] No instances configured");
                return;
            }

            foreach (var entry in config.Instances)
            {
                try
                {
                    var instance = new UIInstance(
                        entry.Id, entry.WorkingDir, entry.EntryFile,
                        entry.SortingOrder, entry.Enabled, entry.Interactive, _log);
                    instance.Initialize();
                    _instances[entry.Id] = instance;
                }
                catch (Exception ex)
                {
                    _log.LogError($"[OneJS] Failed to init instance '{entry.Id}': {ex}");
                }
            }

            _log.LogInfo($"[OneJS] {_instances.Count} instance(s) initialized");

            UIToolkitInputDispatcher.Initialize(_log);
        }

        private static void RunDiagnostics()
        {
            _log?.LogInfo($"[OneJS Diag] Instances: {_instances.Count}");
            foreach (var kv in _instances)
            {
                var inst = kv.Value;
                _log?.LogInfo($"[OneJS Diag] [{kv.Key}] init={inst.IsInitialized} enabled={inst.Enabled} sortOrder={inst.SortingOrder}");
            }
        }

        // ========== 实例管理 API ==========

        /// <summary>动态添加一个新的 UI 实例</summary>
        public static UIInstance AddInstance(string id, string workingDir, string entryFile, int sortingOrder, bool enabled, bool interactive = false)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_instances.ContainsKey(id))
            {
                _log?.LogWarning($"[OneJS] Instance '{id}' already exists");
                return _instances[id];
            }

            UIInstanceConfig.AddInstance(new UIInstanceEntry
            {
                Id = id,
                WorkingDir = workingDir,
                EntryFile = entryFile,
                SortingOrder = sortingOrder,
                Enabled = enabled,
                Interactive = interactive
            });

            if (_initDone)
            {
                var instance = new UIInstance(id, workingDir, entryFile, sortingOrder, enabled, interactive, _log);
                instance.Initialize();
                _instances[id] = instance;
                return instance;
            }
            return null;
        }

        /// <summary>移除一个实例（不允许移除 "default"）</summary>
        public static bool RemoveInstance(string id)
        {
            if (id == "default")
            {
                _log?.LogWarning("[OneJS] Cannot remove the default instance");
                return false;
            }

            if (_instances.TryGetValue(id, out var inst))
            {
                inst.Dispose();
                _instances.Remove(id);
            }
            return UIInstanceConfig.RemoveInstance(id);
        }

        /// <summary>启用或禁用一个实例</summary>
        public static void SetInstanceEnabled(string id, bool enabled)
        {
            if (_instances.TryGetValue(id, out var inst))
                inst.Enabled = enabled;
            UIInstanceConfig.SetEnabled(id, enabled);
        }

        /// <summary>热重载指定实例的 JS 引擎</summary>
        public static void ReloadInstance(string id)
        {
            if (_instances.TryGetValue(id, out var inst))
                inst.Reload();
        }

        public static void Shutdown()
        {
            if (!_initRequested) return;
            _initRequested = false;

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

            // Stop all esbuild watches (in-process Go goroutines, no child processes)
            EsbuildBridge.StopAll();
            _esbuildWatchIds.Clear();

            foreach (var kv in _instances)
            {
                kv.Value.Dispose();
            }
            _instances.Clear();
            ChillJSApi.Instance = null;
        }
    }
}
