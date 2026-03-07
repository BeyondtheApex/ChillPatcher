using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.ModuleSystem;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.UIFramework.Music;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 事件 API：订阅游戏/播放事件，JS 端通过 chill.events 访问
    /// 
    /// 支持的事件名：
    /// - "playStarted"  — 播放开始
    /// - "playEnded"    — 播放结束
    /// - "playPaused"   — 播放暂停/恢复
    /// - "playProgress" — 播放进度变化
    /// - "queueChanged" — 播放队列变化
    /// - "tagRegistered"   — Tag 注册
    /// - "tagUnregistered" — Tag 注销
    /// - "musicRegistered" — 歌曲注册
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

        /// <summary>
        /// 初始化事件订阅（在 EventBus 可用后调用）
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var eventBus = EventBus.Instance;
            if (eventBus == null)
            {
                _logger.LogWarning("[JSApi.Events] EventBus not available");
                return;
            }

            // 订阅 SDK 事件并转发到 JS
            _subscriptions.Add(eventBus.Subscribe<PlayStartedEvent>(e =>
                Emit("playStarted", JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["uuid"] = e.Music?.UUID ?? "",
                    ["title"] = e.Music?.Title ?? "",
                    ["artist"] = e.Music?.Artist ?? "",
                    ["source"] = e.Source.ToString()
                }))));

            _subscriptions.Add(eventBus.Subscribe<PlayEndedEvent>(e =>
                Emit("playEnded", JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["uuid"] = e.Music?.UUID ?? "",
                    ["title"] = e.Music?.Title ?? "",
                    ["reason"] = e.Reason.ToString(),
                    ["playedDuration"] = e.PlayedDuration
                }))));

            _subscriptions.Add(eventBus.Subscribe<PlayPausedEvent>(e =>
                Emit("playPaused", JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["uuid"] = e.Music?.UUID ?? "",
                    ["isPaused"] = e.IsPaused
                }))));

            _subscriptions.Add(eventBus.Subscribe<PlayProgressEvent>(e =>
                Emit("playProgress", JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["uuid"] = e.Music?.UUID ?? "",
                    ["currentTime"] = e.CurrentTime,
                    ["totalTime"] = e.TotalTime,
                    ["progress"] = e.Progress
                }))));

            // 订阅队列变化
            var queueMgr = PlayQueueManager.Instance;
            if (queueMgr != null)
            {
                queueMgr.OnQueueChanged += () => Emit("queueChanged", null);
                queueMgr.OnCurrentChanged += (audio) =>
                    Emit("currentChanged", JSApiHelper.ToJson(new Dictionary<string, object>
                    {
                        ["uuid"] = audio?.UUID ?? "",
                        ["title"] = audio?.Title ?? "",
                        ["artist"] = audio?.Credit ?? ""
                    }));
            }

            _logger.LogInfo("[JSApi.Events] Event subscriptions initialized");
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="eventName">事件名</param>
        /// <param name="handler">回调函数，参数为事件数据对象</param>
        /// <returns>取消订阅的函数</returns>
        public Action on(string eventName, Action<object> handler)
        {
            if (!_handlers.TryGetValue(eventName, out var list))
            {
                list = new List<Action<object>>();
                _handlers[eventName] = list;
            }
            list.Add(handler);

            // 返回取消订阅函数
            return () => list.Remove(handler);
        }

        /// <summary>
        /// 一次性订阅（触发一次后自动取消）
        /// </summary>
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

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void off(string eventName, Action<object> handler)
        {
            if (_handlers.TryGetValue(eventName, out var list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>
        /// 取消指定事件的所有订阅
        /// </summary>
        public void offAll(string eventName)
        {
            if (_handlers.ContainsKey(eventName))
            {
                _handlers[eventName].Clear();
            }
        }

        /// <summary>
        /// 触发事件（供内部或自定义模块使用）
        /// </summary>
        public void emit(string eventName, object data)
        {
            Emit(eventName, data);
        }

        private void Emit(string eventName, object data)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;

            // 复制列表以防止迭代时修改
            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                try
                {
                    handler(data);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[JSApi.Events] Handler error for '{eventName}': {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
            _handlers.Clear();
        }
    }
}
