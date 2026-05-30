using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneJS {
    /// <summary>
    /// Janitor needs to be created by code (e.g. via JanitorSpawner) because 
    /// root GOs are not guaranteed the same order during runtime.
    /// </summary>
    [AddComponentMenu("")] // Hides it from the Add Component Menu
    public class Janitor : MonoBehaviour {
        public bool clearLogs = true;
        public bool clearGameObjects = true;

        bool _destroyed;

        void OnDestroy() {
            _destroyed = true;
        }

        /// <summary>
        /// Destroys all root sibling GameObjects after this one.
        /// </summary>
        public void ClearGameObjects() {
            if (_destroyed)
                return;
            // NOTE GetSiblingIndex doesn't work on root GOs
            var rootGOs = SceneManager.GetActiveScene().GetRootGameObjects();
            var canDelete = false;
            foreach (var go in rootGOs) {
                // Debug.Log(go.name);
                if (go == this.gameObject) {
                    canDelete = true;
                    // Debug.Log("can delete");
                    continue;
                }
                if (!canDelete)
                    continue;
                Destroy(go);
            }
        }

        [ContextMenu("Clean")]
        public void Clean() {
            if (clearGameObjects)
                ClearGameObjects();
            if (clearLogs)
                ClearLog();
        }

        [ContextMenu("Clear Logs")]
        public void ClearLog() {
#if UNITY_EDITOR
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
#endif
        }
    }
}