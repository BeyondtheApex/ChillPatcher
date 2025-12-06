using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;

namespace ChillPatcher.ModuleSystem
{
    /// <summary>
    /// 事件总线实现
    /// 用于模块间的事件通信
    /// </summary>
    public class EventBus : IEventBus
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance;

        private readonly ManualLogSource _logger;
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public static void Initialize(ManualLogSource logger)
        {
            if (_instance != null)
            {
                logger.LogWarning("EventBus 已初始化");
                return;
            }
            _instance = new EventBus(logger);
        }

        private EventBus(ManualLogSource logger)
        {
            _logger = logger;
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IModuleEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent);

            lock (_lock)
            {
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<Delegate>();
                }
                _handlers[eventType].Add(handler);
            }

            _logger.LogDebug($"订阅事件: {eventType.Name}");

            return new Subscription<TEvent>(this, handler);
        }

        public void Publish<TEvent>(TEvent eventData) where TEvent : IModuleEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            var eventType = typeof(TEvent);
            List<Delegate> handlers;

            lock (_lock)
            {
                if (!_handlers.TryGetValue(eventType, out handlers))
                {
                    return;
                }
                // 复制一份，避免在迭代时修改
                handlers = new List<Delegate>(handlers);
            }

            _logger.LogDebug($"发布事件: {eventType.Name} (订阅者: {handlers.Count})");

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"事件处理器异常 ({eventType.Name}): {ex.Message}");
                }
            }
        }

        public void UnsubscribeAll()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
            _logger.LogInfo("已取消所有事件订阅");
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        internal void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IModuleEvent
        {
            var eventType = typeof(TEvent);

            lock (_lock)
            {
                if (_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                    {
                        _handlers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// 订阅句柄，用于自动取消订阅
        /// </summary>
        private class Subscription<TEvent> : IDisposable where TEvent : IModuleEvent
        {
            private readonly EventBus _eventBus;
            private readonly Action<TEvent> _handler;
            private bool _disposed;

            public Subscription(EventBus eventBus, Action<TEvent> handler)
            {
                _eventBus = eventBus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _eventBus.Unsubscribe(_handler);
                _disposed = true;
            }
        }
    }
}
