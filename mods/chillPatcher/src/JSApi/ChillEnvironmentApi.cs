using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 环境控制 JS API。JS 端通过 chill.game.environment 访问。
    /// </summary>
    public class ChillEnvironmentApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly EnvironmentApiService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillEnvironmentApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new EnvironmentApiService(logger);
        }

        public bool locked
        {
            get => _service.Locked;
        }

        public string getEnvironments() => JSApiHelper.ToJson(_service.getEnvironments());
        public bool isViewActive(string id) => _service.isViewActive(id);
        public bool setViewActive(string id, bool active) => !locked && _service.setViewActive(id, active);
        public string getSoundState(string id) => JSApiHelper.ToJson(_service.getSoundState(id));
        public bool setSoundVolume(string id, float volume) => !locked && _service.setSoundVolume(id, volume);
        public bool setSoundMute(string id, bool mute) => !locked && _service.setSoundMute(id, mute);
        public string getAutoTimeSettings() => JSApiHelper.ToJson(_service.getAutoTimeSettings());

        public bool setAutoTimeEnabled(bool enabled)
            => !locked && _service.setAutoTimeSettings(enabled, null, null, null);

        public bool setAutoTimeHours(float dayStart, float sunsetStart, float nightStart)
            => !locked && _service.setAutoTimeSettings(null, dayStart, sunsetStart, nightStart);

        public bool loadPreset(int index) => !locked && _service.loadPreset(index);
        public bool saveToPreset(int index) => !locked && _service.saveToPreset(index);
        public int getCurrentPresetIndex() => _service.getCurrentPresetIndex();

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
            _service.OnEnvironmentEvent += OnServiceEvent;
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
                catch (Exception ex) { _logger?.LogWarning($"[JSApi.Environment] event handler error ({eventName}): {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed) { _service.OnEnvironmentEvent -= OnServiceEvent; _isSubscribed = false; }
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
