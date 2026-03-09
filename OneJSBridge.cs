using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChillPatcher.JSApi;
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
        /// If Node.js is not available, gracefully skips (pre-built bundle still works).
        /// </summary>
        private static void EnsureBuildSetup()
        {
            var config = UIInstanceConfig.Data;
            if (config?.Instances == null || config.Instances.Count == 0)
            {
                _buildSetupDone = true;
                return;
            }

            // Check if Node.js is available once
            if (!IsNodeAvailable())
            {
                _log.LogInfo("[OneJS] Node.js not found, skipping build setup (using pre-built bundles)");
                _buildSetupDone = true;
                return;
            }

            foreach (var entry in config.Instances)
            {
                var dir = entry.WorkingDir;
                var packageJson = Path.Combine(dir, "package.json");
                if (!File.Exists(packageJson)) continue;

                var nodeModules = Path.Combine(dir, "node_modules");
                var esbuildOutput = Path.Combine(dir, "@outputs", "esbuild", "app.js");

                // npm install
                if (!Directory.Exists(nodeModules))
                {
                    _log.LogInfo($"[OneJS:{entry.Id}] Running npm install...");
                    RunNpmCommand(dir, "install", 60000);
                }

                // esbuild once
                if (!File.Exists(esbuildOutput))
                {
                    _log.LogInfo($"[OneJS:{entry.Id}] Building UI...");
                    RunNpmCommand(dir, "run build", 30000);
                }

                // esbuild watch
                StartEsbuildWatch(dir, entry.Id);
            }

            _buildSetupDone = true;
        }

        private static bool IsNodeAvailable()
        {
            Process proc = null;
            try
            {
                proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                proc.Start();
                if (!proc.WaitForExit(5000))
                {
                    try { KillProcessTree(proc.Id); } catch { }
                    return false;
                }
                return proc.ExitCode == 0;
            }
            catch
            {
                if (proc != null)
                {
                    try { if (!proc.HasExited) KillProcessTree(proc.Id); } catch { }
                }
                return false;
            }
        }

        private static readonly List<Process> _esbuildProcesses = new List<Process>();

        private static void RunNpmCommand(string workingDir, string args, int timeoutMs)
        {
            Process proc = null;
            try
            {
                proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c npm {args}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                proc.Start();
                if (!proc.WaitForExit(timeoutMs))
                {
                    // 超时：终止整个进程树
                    _log.LogWarning($"[OneJS] npm {args} timed out ({timeoutMs}ms), killing...");
                    try { KillProcessTree(proc.Id); } catch { }
                    return;
                }
                if (proc.ExitCode != 0)
                {
                    var err = proc.StandardError.ReadToEnd();
                    _log.LogWarning($"[OneJS] npm {args} failed in {workingDir}: {err}");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[OneJS] npm {args} error in {workingDir}: {ex.Message}");
                if (proc != null)
                {
                    try { if (!proc.HasExited) KillProcessTree(proc.Id); } catch { }
                }
            }
        }

        private static void StartEsbuildWatch(string workingDir, string instanceId)
        {
            var esbuildMjs = Path.Combine(workingDir, "esbuild.mjs");
            if (!File.Exists(esbuildMjs)) return;

            // 先清理上次遗留的 esbuild 进程
            KillOrphanedEsbuild(workingDir, instanceId);

            try
            {
                var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "esbuild.mjs",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                proc.Start();
                _esbuildProcesses.Add(proc);

                // 写入 PID 文件，便于下次启动清理
                WritePidFile(workingDir, proc.Id);

                _log.LogInfo($"[OneJS:{instanceId}] esbuild watch started (pid={proc.Id})");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[OneJS:{instanceId}] Could not start esbuild watch: {ex.Message}");
            }
        }

        private static readonly string PidFileName = ".esbuild.pid";

        /// <summary>
        /// 终止进程及其子进程树。
        /// </summary>
        private static void KillProcessTree(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
                proc.WaitForExit(5000);
            }
            catch (ArgumentException) { } // 进程已退出
            catch { }
        }

        private static void WritePidFile(string workingDir, int pid)
        {
            try
            {
                File.WriteAllText(Path.Combine(workingDir, PidFileName), pid.ToString());
            }
            catch { }
        }

        /// <summary>
        /// 根据 PID 文件清理上一次运行遗留的 esbuild 进程。
        /// </summary>
        private static void KillOrphanedEsbuild(string workingDir, string instanceId)
        {
            var pidFile = Path.Combine(workingDir, PidFileName);
            if (!File.Exists(pidFile)) return;

            try
            {
                var pidStr = File.ReadAllText(pidFile).Trim();
                if (int.TryParse(pidStr, out var pid))
                {
                    KillProcessTree(pid);
                    _log?.LogInfo($"[OneJS:{instanceId}] Killed orphaned esbuild process tree (pid={pid})");
                }
            }
            catch (ArgumentException)
            {
                // 进程已不存在，正常
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[OneJS:{instanceId}] Error cleaning orphaned esbuild: {ex.Message}");
            }
            finally
            {
                try { File.Delete(pidFile); } catch { }
            }
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

            // 终止所有 esbuild 进程并清理 PID 文件
            foreach (var proc in _esbuildProcesses)
            {
                try
                {
                    if (!proc.HasExited)
                        KillProcessTree(proc.Id);
                }
                catch { }
            }
            _esbuildProcesses.Clear();

            // 清理所有实例的 PID 文件
            foreach (var kv in _instances)
            {
                var pidFile = Path.Combine(kv.Value.WorkingDir, PidFileName);
                try { if (File.Exists(pidFile)) File.Delete(pidFile); } catch { }
            }

            foreach (var kv in _instances)
            {
                kv.Value.Dispose();
            }
            _instances.Clear();
            ChillJSApi.Instance = null;
        }
    }
}
