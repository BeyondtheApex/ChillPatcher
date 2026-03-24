using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 角色行为 JS API。JS 端通过 chill.game.character 访问。
    /// </summary>
    public class ChillCharacterApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly CharacterApiService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillCharacterApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new CharacterApiService(logger);
        }

        public bool locked
        {
            get => _service.Locked;
        }

        public string getAvailableStates() => JSApiHelper.ToJson(_service.getAvailableStates());
        public string getState() => JSApiHelper.ToJson(_service.getState());
        public bool setState(string stateId) => !locked && _service.setState(stateId);
        public bool startWork() => !locked && _service.startWork();
        public bool startBreak() => !locked && _service.startBreak();
        public bool cancelChange() => !locked && _service.cancelChange();
        public bool matchCurrentAction() => !locked && _service.matchCurrentAction();
        public bool matchCurrentActionWithWild() => !locked && _service.matchCurrentActionWithWild();

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
            _service.OnCharacterEvent += OnServiceEvent;
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
                catch (Exception ex) { _logger?.LogWarning($"[JSApi.Character] event handler error ({eventName}): {ex.Message}"); }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed) { _service.OnCharacterEvent -= OnServiceEvent; _isSubscribed = false; }
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
