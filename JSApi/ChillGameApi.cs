using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 游戏控制 API（番茄钟、经验值、等级与进度）。
    /// JS 端通过 chill.game 访问。
    /// </summary>
    public class ChillGameApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly GameApiService _service;

        private readonly Dictionary<string, GameEventHandler> _handlers
            = new Dictionary<string, GameEventHandler>();

        private bool _isSubscribed;

        public ChillGameApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new GameApiService(logger);
        }

        /// <summary>
        /// 获取番茄钟当前状态。
        /// </summary>
        public string getPomodoroState()
        {
            return JSApiHelper.ToJson(_service.getPomodoroStateObject());
        }

        /// <summary>
        /// 获取玩家等级/经验/工时进度。
        /// </summary>
        public string getPlayerProgress()
        {
            return JSApiHelper.ToJson(_service.getPlayerProgressObject());
        }

        /// <summary>
        /// 获取与游戏 UI 同源格式的当前日期和时间。
        /// </summary>
        public string getGameClock()
        {
            return JSApiHelper.ToJson(_service.getGameClockObject());
        }

        /// <summary>
        /// 建立事件桥（幂等）。
        /// </summary>
        public bool ensureEventBridge()
        {
            var ok = _service.ensureEventBridge();
            if (ok && !_isSubscribed)
            {
                _service.OnGameEvent += OnServiceEvent;
                _isSubscribed = true;
            }
            return ok;
        }

        public bool startPomodoro() => _service.startPomodoro();
        public bool togglePomodoroPause() => _service.togglePomodoroPause();
        public bool skipPomodoroPhase() => _service.skipPomodoroPhase();
        public bool resetPomodoro() => _service.resetPomodoro();
        public bool completePomodoroNow() => _service.completePomodoroNow();
        public bool moveAheadPomodoro(float seconds) => _service.moveAheadPomodoro(seconds);

        public bool setWorkMinutes(int minutes) => _service.setWorkMinutes(minutes);
        public bool setBreakMinutes(int minutes) => _service.setBreakMinutes(minutes);
        public bool setLoopCount(int loopCount) => _service.setLoopCount(loopCount);

        public bool addExp(float exp) => _service.addExp(exp);
        public bool setLevel(int level) => _service.setLevel(level);
        public bool setCurrentExp(float exp) => _service.setCurrentExp(exp);
        public bool setCurrentWorkSeconds(double seconds) => _service.setCurrentWorkSeconds(seconds);
        public bool setTotalWorkSeconds(double seconds) => _service.setTotalWorkSeconds(seconds);

        public bool savePomodoroData() => _service.savePomodoroData();
        public bool savePlayerData() => _service.savePlayerData();

        /// <summary>
        /// 订阅游戏事件，返回 token。
        /// eventName 可传 "*" 订阅全部。
        /// handler 参数为 JSON 字符串。
        /// </summary>
        public string on(string eventName, Action<string> handler)
        {
            if (handler == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(eventName)) eventName = "*";

            ensureEventBridge();

            var token = Guid.NewGuid().ToString("N");
            _handlers[token] = new GameEventHandler
            {
                EventName = eventName,
                Handler = handler
            };
            return token;
        }

        public bool off(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            return _handlers.Remove(token);
        }

        public int offAll(string eventName = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                var count = _handlers.Count;
                _handlers.Clear();
                return count;
            }

            var keys = new List<string>();
            foreach (var kv in _handlers)
            {
                if (kv.Value.EventName == eventName)
                {
                    keys.Add(kv.Key);
                }
            }

            foreach (var key in keys)
            {
                _handlers.Remove(key);
            }

            return keys.Count;
        }

        public string getEventNames()
        {
            var names = new[]
            {
                "pomodoroStateChanged",
                "pomodoroStart",
                "pomodoroPlay",
                "pomodoroPause",
                "pomodoroUnpause",
                "pomodoroProgress",
                "pomodoroWorkStart",
                "pomodoroWorkEnd",
                "pomodoroBreakStart",
                "pomodoroBreakEnd",
                "pomodoroComplete",
                "pomodoroWorkHourUpdated",
                "pomodoroPreReward",
                "playerProgressChanged",
                "levelAddExp",
                "levelAddedExp",
                "levelChanged",
                "expChanged",
                "workSecondsChanged",
                "totalWorkSecondsChanged",
                "gameClockTick",
                "gameDateChanged"
            };
            return JSApiHelper.ToJson(names);
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

                try
                {
                    cfg.Handler(packet);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"[JSApi.Game] event handler error ({eventName}): {ex.Message}");
                }
            }
        }

        private sealed class GameEventHandler
        {
            public string EventName;
            public Action<string> Handler;
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                _service.OnGameEvent -= OnServiceEvent;
                _isSubscribed = false;
            }

            _handlers.Clear();
            _service.Dispose();
        }
    }
}