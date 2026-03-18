using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// AIChat 联动 API（OneJS）
    ///
    /// JS 端示例：
    ///   const status = JSON.parse(chill.aichat.getStatus())
    ///   const cfg = JSON.parse(chill.aichat.getAllConfig())
    ///   const ret = JSON.parse(chill.aichat.startTextConversation("你好", "onejs"))
    ///   const token = chill.aichat.onConversationCompleted((json) => {
    ///     const data = JSON.parse(json)
    ///     console.log(data)
    ///   })
    ///   chill.aichat.offConversationCompleted(token)
    /// </summary>
    public class ChillAIChatApi
    {
        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, Action<Dictionary<string, string>>> _eventHandlers
            = new Dictionary<string, Action<Dictionary<string, string>>>();

        public ChillAIChatApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        public string getStatus()
        {
            var payload = new Dictionary<string, object>
            {
                ["available"] = AIChatBridge.IsAvailable,
                ["apiVersion"] = AIChatBridge.ApiVersion,
                ["isBusy"] = AIChatBridge.IsBusy,
                ["isReady"] = AIChatBridge.IsReady,
                ["consoleVisible"] = AIChatBridge.GetConsoleVisible()
            };

            return JSApiHelper.ToJson(payload);
        }

        public bool isAvailable()
        {
            return AIChatBridge.IsAvailable;
        }

        public bool isBusy()
        {
            return AIChatBridge.IsBusy;
        }

        public bool isReady()
        {
            return AIChatBridge.IsReady;
        }

        public string apiVersion()
        {
            return AIChatBridge.ApiVersion;
        }

        public bool getConsoleVisible()
        {
            return AIChatBridge.GetConsoleVisible();
        }

        public string getAllConfig()
        {
            if (!AIChatBridge.IsAvailable)
            {
                return "{}";
            }

            return JSApiHelper.ToJson(AIChatBridge.GetAllConfigValues());
        }

        public string getAllConfigDefaults()
        {
            if (!AIChatBridge.IsAvailable)
            {
                return "{}";
            }

            return JSApiHelper.ToJson(AIChatBridge.GetAllConfigDefaultValues());
        }

        public string getConfig(string key)
        {
            if (!AIChatBridge.IsAvailable)
            {
                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["ok"] = false,
                    ["key"] = key,
                    ["value"] = null,
                    ["error"] = "AIChat not installed"
                });
            }

            var value = AIChatBridge.GetConfigValue(key);
            var payload = new Dictionary<string, object>
            {
                ["ok"] = value != null,
                ["key"] = key,
                ["value"] = value,
                ["error"] = value != null ? string.Empty : "config key not found"
            };
            return JSApiHelper.ToJson(payload);
        }

        public string setConfig(string key, string value)
        {
            var ok = AIChatBridge.TrySetConfigValue(key, value, out var error);
            return ToResult(ok, error);
        }

        public string saveConfig()
        {
            var ok = AIChatBridge.TrySaveConfig(out var error);
            return ToResult(ok, error);
        }

        public string setConsoleVisible(bool visible)
        {
            var ok = AIChatBridge.SetConsoleVisible(visible, out var error);
            return ToResult(ok, error);
        }

        public string clearMemory()
        {
            var ok = AIChatBridge.TryClearMemory(out var error);
            return ToResult(ok, error);
        }

        public string startTextConversation(string text, string inputSource = "onejs")
        {
            var ok = AIChatBridge.TryStartTextConversation(text, inputSource, out var error);
            return ToResult(ok, error);
        }

        public string startVoiceConversationFromWavBase64(string wavBase64, string inputSource = "onejs")
        {
            if (string.IsNullOrWhiteSpace(wavBase64))
            {
                return ToResult(false, "wavBase64 is empty");
            }

            try
            {
                var wavData = Convert.FromBase64String(wavBase64);
                var ok = AIChatBridge.TryStartVoiceConversationFromWav(wavData, inputSource, out var error);
                return ToResult(ok, error);
            }
            catch (Exception ex)
            {
                return ToResult(false, ex.Message);
            }
        }

        public string startVoiceConversationFromWavBytes(byte[] wavData, string inputSource = "onejs")
        {
            if (wavData == null || wavData.Length == 0)
            {
                return ToResult(false, "wavData is empty");
            }

            var ok = AIChatBridge.TryStartVoiceConversationFromWav(wavData, inputSource, out var error);
            return ToResult(ok, error);
        }

        public string startVoiceCapture()
        {
            var ok = AIChatBridge.TryStartVoiceCapture(out var error);
            return ToResult(ok, error);
        }

        public string stopVoiceCaptureAndSend(string inputSource = "onejs")
        {
            var ok = AIChatBridge.TryStopVoiceCaptureAndSend(inputSource, out var error);
            return ToResult(ok, error);
        }

        public string onConversationCompleted(Action<string> handler)
        {
            if (handler == null) return string.Empty;

            var token = Guid.NewGuid().ToString("N");
            Action<Dictionary<string, string>> callback = map =>
            {
                try
                {
                    handler(JSApiHelper.ToJson(map));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[AIChatApi] onConversationCompleted handler error: {ex.Message}");
                }
            };

            if (!AIChatBridge.TrySubscribeConversationCompleted(callback, out var error))
            {
                _logger.LogWarning($"[AIChatApi] subscribe failed: {error}");
                return string.Empty;
            }

            _eventHandlers[token] = callback;
            return token;
        }

        public bool offConversationCompleted(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            if (!_eventHandlers.TryGetValue(token, out var callback)) return false;

            var ok = AIChatBridge.TryUnsubscribeConversationCompleted(callback, out var error);
            if (!ok)
            {
                _logger.LogWarning($"[AIChatApi] unsubscribe failed: {error}");
                return false;
            }

            _eventHandlers.Remove(token);
            return true;
        }

        public int offAllConversationCompleted()
        {
            var removed = 0;
            var tokens = new List<string>(_eventHandlers.Keys);
            foreach (var token in tokens)
            {
                if (offConversationCompleted(token)) removed++;
            }
            return removed;
        }

        private static string ToResult(bool ok, string error)
        {
            var payload = new Dictionary<string, object>
            {
                ["ok"] = ok,
                ["error"] = ok ? string.Empty : (error ?? string.Empty)
            };
            return JSApiHelper.ToJson(payload);
        }
    }
}