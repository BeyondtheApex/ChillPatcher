using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace OneJS {
    /// <summary>
    /// Watch for screen size changes and apply media classes to the root element.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(UIDocument))] [AddComponentMenu("OneJS/Screen Monitor")]
    public class ScreenMonitor : MonoBehaviour {
        static string[] screenClasses = new[] {
            "onejs-media-sm", "onejs-media-md", "onejs-media-lg", "onejs-media-xl", "onejs-media-xxl"
        };

        [Tooltip("Screen breakpoints for responsive design.")]
        public int[] breakpoints = new[] { 640, 768, 1024, 1280, 1536 };
        [Tooltip("Enable for standalone player.")]
        public bool standalone;

        UIDocument _uiDocument;
        float _lastScreenWidth;

        void Awake() {
            _uiDocument = GetComponent<UIDocument>();
        }

        void Start() {
            PollScreenChange();
        }

        void Update() {
#if !UNITY_EDITOR && (UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID)
            if (standalone) {
                PollScreenChange();
            }
#else
            PollScreenChange();
#endif
        }

        void PollScreenChange() {
            var width = _uiDocument.rootVisualElement.resolvedStyle.width;
            if (!Mathf.Approximately(_lastScreenWidth, width)) {
                SetRootMediaClass(width);
                _lastScreenWidth = width;
            }
        }

        void SetRootMediaClass(float width) {
            foreach (var sc in screenClasses) {
                _uiDocument.rootVisualElement.RemoveFromClassList(sc);
            }
            for (int i = 0; i < breakpoints.Length; i++) {
                if (screenClasses.Length <= i) break;
                if (width >= breakpoints[i]) {
                    _uiDocument.rootVisualElement.AddToClassList(screenClasses[i]);
                }
            }
        }
    }
}