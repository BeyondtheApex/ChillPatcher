using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 语音 JS API。JS 端通过 chill.game.voice 访问。
    /// </summary>
    public class ChillVoiceApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly VoiceApiService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillVoiceApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new VoiceApiService(logger);
        }

        public bool locked
        {
            get => _service.Locked;
        }

        public string getScenarioTypes() => JSApiHelper.ToJson(_service.getScenarioTypes());
        public bool playVoice(string voiceName, bool moveMouth = true) => !locked && _service.playVoice(voiceName, moveMouth);
        public bool cancelVoice() => !locked && _service.cancelVoice();
        public bool isFinished() => _service.isFinished();
        public bool isMouthMoving() => _service.isMouthMoving();
        public bool playScenarioVoice(string scenarioType, int episodeNumber = 0) => !locked && _service.playScenarioVoice(scenarioType, episodeNumber);
        public string getState() => JSApiHelper.ToJson(_service.getState());

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
            _service.OnVoiceEvent += OnServiceEvent;
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
                catch (Exception ex) { _logger?.LogWarning($"[JSApi.Voice] event handler error ({eventName}): {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed) { _service.OnVoiceEvent -= OnServiceEvent; _isSubscribed = false; }
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
