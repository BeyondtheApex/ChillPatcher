using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 字幕/台词 JS API。JS 端通过 chill.game.subtitle 访问。
    /// </summary>
    public class ChillSubtitleApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly SubtitleApiService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillSubtitleApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new SubtitleApiService(logger);
        }

        public bool locked
        {
            get => _service.Locked;
        }

        public bool show(string text, float duration = 5f) => !locked && _service.show(text, duration);
        public bool hide() => !locked && _service.hide();
        public bool isShowing() => _service.isShowing();
        public bool playScenario(string scenarioType, int episodeNumber = 0) => !locked && _service.playScenario(scenarioType, episodeNumber);
        public string getScenarioState() => JSApiHelper.ToJson(_service.getScenarioState());

        public void Tick()
        {
            _service.Tick();
        }

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
            _service.OnSubtitleEvent += OnServiceEvent;
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
                catch (Exception ex) { _logger?.LogWarning($"[JSApi.Subtitle] event handler error ({eventName}): {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed) { _service.OnSubtitleEvent -= OnServiceEvent; _isSubscribed = false; }
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
