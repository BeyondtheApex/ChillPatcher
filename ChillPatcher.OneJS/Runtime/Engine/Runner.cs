using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OneJS {
    /// <summary>
    /// Executes and optionally live-reloads an entry file, while managing scene-related GameObject cleanups.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(ScriptEngine))] [AddComponentMenu("OneJS/Runner")]
    public class Runner : MonoBehaviour {
        [Tooltip("The entry file to run. Relative to the OneJS WorkingDir.")]
        public string entryFile = "@outputs/esbuild/app.js";
        public bool runOnStart = true;

        [Tooltip("Watch entry file for changes and reload.")]
        public bool liveReload = true;
        [Tooltip("How often to check for changes in the entry file in milliseconds.")]
        public int pollingInterval = 300;
        public bool clearGameObjects = true;
        public bool clearLogs = true;
        [Tooltip("Respawn the Janitor during scene loads so that it doesn't clean up your additively loaded scenes.")]
        public bool respawnJanitorOnSceneLoad;
        [Tooltip("Don't clean up on OnDisable(). (Useful for when your workflow involves disabling ScriptEngine)")]
        public bool stopCleaningOnDisable;
        [Tooltip("Enable Live Reload for Standalone build.")]
        public bool standalone;

        ScriptEngine _engine;
        Janitor _janitor;

        float _lastCheckTime;
        DateTime _lastWriteTime;
        Coroutine _evalCoroutine;

        void Awake() {
            _engine = GetComponent<ScriptEngine>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnEnable() {
            Respawn();
            _engine.OnReload += OnReload;

            var fullpath = _engine.GetFullPath(entryFile);
            if (!File.Exists(fullpath)) {
                Debug.LogError($"Entry file not found: {fullpath}");
                return;
            }
            _lastWriteTime = File.GetLastWriteTime(fullpath); // This needs to be before EvalFile in case EvalFile crashes
            if (runOnStart) {
                // _engine.EvalFile(entryFile);
                _evalCoroutine = StartCoroutine(DelayEvalFile());
            }
        }

        void OnDisable() {
            _engine.OnReload -= OnReload;
        }

        void Update() {
            if (!liveReload) return;
#if !UNITY_EDITOR && (UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID)
            if (!standalone) return;
#endif
            if (Time.time - _lastCheckTime < pollingInterval / 1000f) return;
            _lastCheckTime = Time.time;
            CheckForChanges();
#if UNITY_EDITOR
            CheckForStylesheetChanges();
#endif
        }

        public void Reload() {
            if (_evalCoroutine != null)
                StopCoroutine(_evalCoroutine);
            _engine.Reload();
            // _engine.EvalFile(entryFile);
            _evalCoroutine = StartCoroutine(DelayEvalFile());
        }

        IEnumerator DelayEvalFile() {
            yield return null;
            _engine.EvalFile(entryFile);
        }

        void CheckForChanges() {
            var writeTime = File.GetLastWriteTime(_engine.GetFullPath(entryFile));
            if (_lastWriteTime == writeTime) return; // No change
            _lastWriteTime = writeTime;
            Reload();

            // _engine.OnReload += () => {
            //     _engine.JsEnv.UsingAction<bool>();
            //     // Add more here
            // };
        }

#if UNITY_EDITOR
        Dictionary<StyleSheet, (string, DateTime)> _trackedStylesheets = new Dictionary<StyleSheet, (string, DateTime)>();

        void CheckForStylesheetChanges() {
            if (SameAsTrackedStyleSheet(_engine.styleSheets)) return;
            foreach (var sheet in _engine.styleSheets) {
                var path = UnityEditor.AssetDatabase.GetAssetPath(sheet);
                if (string.IsNullOrEmpty(path)) continue;
                UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
                var lastWriteTime = File.GetLastWriteTime(path);
                if (_trackedStylesheets.TryGetValue(sheet, out var tracked)) {
                    if (tracked.Item1.Equals(path, StringComparison.Ordinal) && tracked.Item2 == lastWriteTime) continue;
                }
                _trackedStylesheets[sheet] = (path, lastWriteTime);
            }
        }

        /// <summary>
        /// Returns true if the provided StyleSheet array matches the tracked stylesheets.
        /// Same SS obj, assetPath, and GetLastWriteTime
        /// </summary>
        bool SameAsTrackedStyleSheet(StyleSheet[] styleSheets) {
            if (styleSheets.Length != _trackedStylesheets.Count) return false;
            for (int i = 0; i < styleSheets.Length; i++) {
                var sheet = styleSheets[i];
                if (!_trackedStylesheets.TryGetValue(sheet, out var tracked)) return false;
                var path = UnityEditor.AssetDatabase.GetAssetPath(sheet);
                if (!path.Equals(tracked.Item1, StringComparison.Ordinal)) return false;
                if (File.GetLastWriteTime(path) != tracked.Item2) return false;
            }
            return true;
        }
#endif

        void Respawn() {
            if (_janitor != null) {
                Destroy(_janitor.gameObject);
            }
            _janitor = new GameObject("Janitor").AddComponent<Janitor>();
            _janitor.clearGameObjects = clearGameObjects;
            _janitor.clearLogs = clearLogs;
        }

        void OnReload() {
            // Because OnDisable() order is non-deterministic, we need to check for gameObject.activeSelf
            // instead of depending on individual components.
            if (stopCleaningOnDisable && !this.gameObject.activeSelf)
                return;
            _janitor.Clean();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (respawnJanitorOnSceneLoad) {
                Respawn();
            }
        }
    }
}