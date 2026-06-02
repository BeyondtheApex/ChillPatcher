using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem
{
    public class EventBus : IEventBus
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance;

        private readonly ILogger _logger;
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public static void Initialize(ILogger logger)
        {
            if (_instance != null)
            {
                logger.LogWarning("EventBus already initialized");
                return;
            }
            _instance = new EventBus(logger);
        }

        private EventBus(ILogger logger)
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

            _logger.LogDebug("Subscribe event: {EventType}", eventType.Name);
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
                handlers = new List<Delegate>(handlers);
            }

            _logger.LogDebug("Publish event: {EventType} (source: {Source}, subscribers: {Count})",
                eventType.Name, eventData.SourceModuleId ?? "host", handlers.Count);

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler error ({EventType})", eventType.Name);
                }
            }
        }

        public void Publish<TEvent>(TEvent eventData, string sourceModuleId) where TEvent : IModuleEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            eventData.SourceModuleId = sourceModuleId;
            Publish(eventData);
        }

        public void UnsubscribeAll()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
            _logger.LogInformation("All event subscriptions cleared");
        }

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
