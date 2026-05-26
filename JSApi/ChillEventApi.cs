using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using ChillPatcher.UIFramework.Music;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 事件 API (IPC 版本)
    /// EventBus subscriptions removed. Game events from PlayQueueManager still work.
    /// </summary>
    public class ChillEventApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly Dictionary<string, List<Action<object>>> _handlers
            = new Dictionary<string, List<Action<object>>>();
        private bool _initialized;

        public ChillEventApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Subscribe to queue changes (still works with PlayQueueManager)
            var queueMgr = PlayQueueManager.Instance;
            if (queueMgr != null)
            {
                var qAction = new Action(() => Emit("queueChanged", null));
                queueMgr.OnQueueChanged += qAction;

                Action<Bulbul.GameAudioInfo> cAction = (audio) =>
                    Emit("currentChanged", JSApiHelper.ToJson(new Dictionary<string, object>
                    {
                        ["uuid"] = audio?.UUID ?? "",
                        ["title"] = audio?.Title ?? "",
                        ["artist"] = audio?.Credit ?? ""
                    }));
                queueMgr.OnCurrentChanged += cAction;

                _subscriptions.Add(new ActionDisposable(() => queueMgr.OnQueueChanged -= qAction));
                _subscriptions.Add(new ActionDisposable(() => queueMgr.OnCurrentChanged -= cAction));
            }

            // Subscribe to IME context changes
            UIToolkitInputDispatcher.OnImeContextChanged += (ctx, rect) =>
                Emit("imeContextChanged", JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["context"] = ctx,
                    ["inputRect"] = rect,
                }));

            // Subscribe to input mode changes
            UIToolkitInputDispatcher.OnInputModeChanged += (isGameMode) =>
                Emit("inputModeChanged", JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["isGameMode"] = isGameMode,
                }));

            _logger.LogInfo("[JSApi.Events] Event subscriptions initialized (IPC mode)");
        }

        public Action on(string eventName, Action<object> handler)
        {
            if (!_handlers.TryGetValue(eventName, out var list))
            {
                list = new List<Action<object>>();
                _handlers[eventName] = list;
            }
            list.Add(handler);
            return () => list.Remove(handler);
        }

        public Action once(string eventName, Action<object> handler)
        {
            Action<object> wrapper = null;
            wrapper = (data) =>
            {
                handler(data);
                off(eventName, wrapper);
            };
            return on(eventName, wrapper);
        }

        public void off(string eventName, Action<object> handler)
        {
            if (_handlers.TryGetValue(eventName, out var list))
                list.Remove(handler);
        }

        public void offAll(string eventName)
        {
            if (_handlers.ContainsKey(eventName))
                _handlers[eventName].Clear();
        }

        public void emit(string eventName, object data)
        {
            Emit(eventName, data);
        }

        private void Emit(string eventName, object data)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                try { handler(data); }
                catch (Exception ex) { _logger.LogWarning($"[JSApi.Events] Handler error for '{eventName}': {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
            _handlers.Clear();
        }

        private class ActionDisposable : IDisposable
        {
            private readonly Action _action;
            public ActionDisposable(Action action) => _action = action;
            public void Dispose() => _action?.Invoke();
        }
    }
}
