using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 模式/活动 JS API。JS 端通过 chill.game.mode 访问。
    /// </summary>
    public class ChillModeApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly ModeApiService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillModeApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new ModeApiService(logger);
        }

        public bool locked
        {
            get => _service.Locked;
        }

        public string getAvailableModes() => JSApiHelper.ToJson(_service.getAvailableModes());
        public string getCurrentMode() => _service.getCurrentMode();
        public string getModeState() => JSApiHelper.ToJson(_service.getModeState());
        public bool setMode(string id) => !locked && _service.setMode(id);
        public bool canChangeMode() => _service.canChangeMode();

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
            _service.OnModeEvent += OnServiceEvent;
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
                catch (Exception ex) { _logger?.LogWarning($"[JSApi.Mode] event handler error ({eventName}): {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed) { _service.OnModeEvent -= OnServiceEvent; _isSubscribed = false; }
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
