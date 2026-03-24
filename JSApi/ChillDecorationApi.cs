using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 物件/装饰 JS API。JS 端通过 chill.game.decoration 访问。
    /// </summary>
    public class ChillDecorationApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly DecorationApiService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillDecorationApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new DecorationApiService(logger);
        }

        public bool locked
        {
            get => _service.Locked;
        }

        public string getCategories() => JSApiHelper.ToJson(_service.getCategories());
        public string getDecorations() => JSApiHelper.ToJson(_service.getDecorations());
        public bool isActive(string skinId) => _service.isActive(skinId);
        public bool setDecoration(string skinId) => !locked && _service.setDecoration(skinId, true);
        public bool deactivateCategory(string categoryId) => !locked && _service.deactivateCategory(categoryId, true);
        public bool reloadFromSave() => !locked && _service.reloadFromSave();
        public string getCurrentModels() => JSApiHelper.ToJson(_service.getCurrentModels());

        public string on(string eventName, Action<string> handler)
        {
            if (handler == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(eventName)) eventName = "*";
            EnsureSubscribed();
            var token = Guid.NewGuid().ToString("N");
            _handlers[token] = new GameEventHandler { EventName = eventName, Handler = handler };
            return token;
        }

        public bool off(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            return _handlers.Remove(token);
        }

        private void EnsureSubscribed()
        {
            if (_isSubscribed) return;
            _service.OnDecorationEvent += OnServiceEvent;
            _isSubscribed = true;
        }

        private void OnServiceEvent(string eventName, object payload)
        {
            if (_handlers.Count == 0) return;
            var packet = JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["name"] = eventName,
                ["payload"] = payload
            });
            foreach (var kv in _handlers)
            {
                var cfg = kv.Value;
                if (cfg.EventName != "*" && cfg.EventName != eventName) continue;
                try { cfg.Handler(packet); }
                catch (Exception ex) { _logger?.LogWarning($"[JSApi.Decoration] event handler error ({eventName}): {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed) { _service.OnDecorationEvent -= OnServiceEvent; _isSubscribed = false; }
            _handlers.Clear();
            _service.Dispose();
        }

        private sealed class GameEventHandler
        {
            public string EventName;
            public Action<string> Handler;
        }
    }
}
