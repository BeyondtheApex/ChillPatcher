using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;

namespace ChillPatcher.Integration
{
    public static class AIChatBridge
    {
        private const string AiChatGuid = "com.username.chillaimod";
        private static readonly Dictionary<Action<Dictionary<string, string>>, Delegate> EventHandlers =
            new Dictionary<Action<Dictionary<string, string>>, Delegate>();

        public static bool IsAvailable => GetAiChatInstance() != null;

        public static string ApiVersion => GetStringProperty("ApiVersion");
        public static bool IsBusy => GetBoolProperty("IsBusy");
        public static bool IsReady => GetBoolProperty("IsReady");

        public static bool TryStartTextConversation(string text, string inputSource, out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(
                instance,
                "TryStartTextConversation",
                new object[] { text, inputSource, null },
                out error);
        }

        public static bool TryStartVoiceConversationFromWav(byte[] wavData, string inputSource, out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(
                instance,
                "TryStartVoiceConversationFromWav",
                new object[] { wavData, inputSource, null },
                out error);
        }

        public static bool TryStartVoiceCapture(out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(
                instance,
                "TryStartVoiceCapture",
                new object[] { null },
                out error);
        }

        public static bool TryStopVoiceCaptureAndSend(string inputSource, out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(
                instance,
                "TryStopVoiceCaptureAndSend",
                new object[] { inputSource, null },
                out error);
        }

        public static Dictionary<string, string> GetAllConfigValues()
        {
            var instance = GetAiChatInstance();
            if (instance == null) return new Dictionary<string, string>();

            try
            {
                var method = instance.GetType().GetMethod("GetAllConfigValues", BindingFlags.Instance | BindingFlags.Public);
                return method?.Invoke(instance, null) as Dictionary<string, string> ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public static Dictionary<string, string> GetAllConfigDefaultValues()
        {
            var instance = GetAiChatInstance();
            if (instance == null) return new Dictionary<string, string>();

            try
            {
                var method = instance.GetType().GetMethod("GetAllConfigDefaultValues", BindingFlags.Instance | BindingFlags.Public);
                return method?.Invoke(instance, null) as Dictionary<string, string> ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public static string GetConfigValue(string key)
        {
            var instance = GetAiChatInstance();
            if (instance == null) return null;

            try
            {
                var method = instance.GetType().GetMethod("GetConfigValue", BindingFlags.Instance | BindingFlags.Public);
                return method?.Invoke(instance, new object[] { key }) as string;
            }
            catch
            {
                return null;
            }
        }

        public static bool TrySetConfigValue(string key, string value, out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(
                instance,
                "TrySetConfigValue",
                new object[] { key, value, null },
                out error);
        }

        public static bool TrySaveConfig(out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(instance, "TrySaveConfig", new object[] { null }, out error);
        }

        public static bool SetConsoleVisible(bool visible, out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(instance, "SetConsoleVisible", new object[] { visible, null }, out error);
        }

        public static bool GetConsoleVisible()
        {
            var instance = GetAiChatInstance();
            if (instance == null) return false;

            try
            {
                var method = instance.GetType().GetMethod("GetConsoleVisible", BindingFlags.Instance | BindingFlags.Public);
                return method?.Invoke(instance, null) is bool visible && visible;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryClearMemory(out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            return TryInvokeBoolWithError(instance, "TryClearMemory", new object[] { null }, out error);
        }

        public static bool TrySubscribeConversationCompleted(Action<Dictionary<string, string>> onCompleted, out string error)
        {
            error = string.Empty;
            if (onCompleted == null)
            {
                error = "callback is null";
                return false;
            }

            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            try
            {
                var eventInfo = instance.GetType().GetEvent("ConversationCompleted", BindingFlags.Instance | BindingFlags.Public);
                if (eventInfo == null)
                {
                    error = "event not found: ConversationCompleted";
                    return false;
                }

                var proxy = new ConversationCompletedProxy(onCompleted);
                var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, proxy, proxy.GetType().GetMethod(nameof(ConversationCompletedProxy.Handle)));
                eventInfo.AddEventHandler(instance, handler);
                EventHandlers[onCompleted] = handler;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryUnsubscribeConversationCompleted(Action<Dictionary<string, string>> onCompleted, out string error)
        {
            error = string.Empty;
            if (onCompleted == null)
            {
                error = "callback is null";
                return false;
            }

            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "AIChat not installed";
                return false;
            }

            if (!EventHandlers.TryGetValue(onCompleted, out var handler))
            {
                error = "callback not subscribed";
                return false;
            }

            try
            {
                var eventInfo = instance.GetType().GetEvent("ConversationCompleted", BindingFlags.Instance | BindingFlags.Public);
                if (eventInfo == null)
                {
                    error = "event not found: ConversationCompleted";
                    return false;
                }

                eventInfo.RemoveEventHandler(instance, handler);
                EventHandlers.Remove(onCompleted);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static object GetAiChatInstance()
        {
            if (!Chainloader.PluginInfos.TryGetValue(AiChatGuid, out var pluginInfo)) return null;
            return pluginInfo?.Instance;
        }

        private static string GetStringProperty(string propertyName)
        {
            var instance = GetAiChatInstance();
            if (instance == null) return string.Empty;

            try
            {
                var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                return prop?.GetValue(instance) as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool GetBoolProperty(string propertyName)
        {
            var instance = GetAiChatInstance();
            if (instance == null) return false;

            try
            {
                var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                return prop?.GetValue(instance) is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeBoolWithError(object instance, string methodName, object[] args, out string error)
        {
            error = string.Empty;
            try
            {
                var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                if (method == null)
                {
                    error = $"method not found: {methodName}";
                    return false;
                }

                var result = method.Invoke(instance, args);
                if (args.Length > 0 && args[args.Length - 1] is string outError && !string.IsNullOrEmpty(outError))
                {
                    error = outError;
                }

                return result is bool ok && ok;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private sealed class ConversationCompletedProxy
        {
            private readonly Action<Dictionary<string, string>> _callback;

            public ConversationCompletedProxy(Action<Dictionary<string, string>> callback)
            {
                _callback = callback;
            }

            public void Handle(object payload)
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (payload != null)
                {
                    var type = payload.GetType();
                    map["Success"] = GetPropertyAsString(type, payload, "Success");
                    map["IsApiError"] = GetPropertyAsString(type, payload, "IsApiError");
                    map["ErrorMessage"] = GetPropertyAsString(type, payload, "ErrorMessage");
                    map["ErrorCode"] = GetPropertyAsString(type, payload, "ErrorCode");
                    map["InputSource"] = GetPropertyAsString(type, payload, "InputSource");
                    map["UserPrompt"] = GetPropertyAsString(type, payload, "UserPrompt");
                    map["EmotionTag"] = GetPropertyAsString(type, payload, "EmotionTag");
                    map["VoiceText"] = GetPropertyAsString(type, payload, "VoiceText");
                    map["SubtitleText"] = GetPropertyAsString(type, payload, "SubtitleText");
                    map["RawResponse"] = GetPropertyAsString(type, payload, "RawResponse");
                    map["TtsAttempted"] = GetPropertyAsString(type, payload, "TtsAttempted");
                    map["TtsSucceeded"] = GetPropertyAsString(type, payload, "TtsSucceeded");
                    map["TimestampUtc"] = GetPropertyAsString(type, payload, "TimestampUtc");
                }

                _callback?.Invoke(map);
            }

            private static string GetPropertyAsString(Type type, object payload, string propertyName)
            {
                try
                {
                    var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                    var value = prop?.GetValue(payload);
                    return value?.ToString() ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
    }
}