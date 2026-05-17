using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Bootstrap;

namespace ChillPatcher.Integration
{
    public static class AIChatBridge
    {
        private const string AiChatGuid = "com.username.chillaimod";
        private static readonly Dictionary<Action<Dictionary<string, string>>, Delegate> EventHandlers =
            new Dictionary<Action<Dictionary<string, string>>, Delegate>();

        private static readonly Dictionary<string, string> ConfigFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Use_Ollama_API"] = "_useOllama",
                ["ThinkMode"] = "_thinkModeConfig",
                ["API_URL"] = "_chatApiUrlConfig",
                ["API_Key"] = "_apiKeyConfig",
                ["ModelName"] = "_modelConfig",
                ["LogApiRequestBody"] = "_logApiRequestBodyConfig",
                ["FixApiPathForThinkMode"] = "_fixApiPathForThinkModeConfig",
                ["TTS_Service_URL"] = "_sovitsUrlConfig",
                ["TTS_Service_Script_Path"] = "_TTSServicePathConfig",
                ["LaunchTTSService"] = "_LaunchTTSServiceConfig",
                ["QuitTTSServiceOnQuit"] = "_quitTTSServiceOnQuitConfig",
                ["Audio_File_Path"] = "_refAudioPathConfig",
                ["AudioPathCheck"] = "_audioPathCheckConfig",
                ["Audio_File_Text"] = "_promptTextConfig",
                ["PromptLang"] = "_promptLangConfig",
                ["TargetLang"] = "_targetLangConfig",
                ["JapaneseCheck"] = "_japaneseCheckConfig",
                ["VoiceVolume"] = "_voiceVolumeConfig",
                ["WindowWidth"] = "_windowWidthConfig",
                ["WindowHeightBase"] = "_windowHeightConfig",
                ["ReverseEnterBehavior"] = "_reverseEnterBehaviorConfig",
                ["ExperimentalMemory"] = "_experimentalMemoryConfig",
                ["SystemPrompt"] = "_personaConfig",
            };

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
                var values = method?.Invoke(instance, null) as Dictionary<string, string>;
                if (values != null) return values;
            }
            catch
            {
            }

            return GetConfigEntryValues(instance, defaultValues: false);
        }

        public static Dictionary<string, string> GetAllConfigDefaultValues()
        {
            var instance = GetAiChatInstance();
            if (instance == null) return new Dictionary<string, string>();

            try
            {
                var method = instance.GetType().GetMethod("GetAllConfigDefaultValues", BindingFlags.Instance | BindingFlags.Public);
                var values = method?.Invoke(instance, null) as Dictionary<string, string>;
                if (values != null) return values;
            }
            catch
            {
            }

            return GetConfigEntryValues(instance, defaultValues: true);
        }

        public static string GetConfigValue(string key)
        {
            var instance = GetAiChatInstance();
            if (instance == null) return null;

            try
            {
                var method = instance.GetType().GetMethod("GetConfigValue", BindingFlags.Instance | BindingFlags.Public);
                var value = method?.Invoke(instance, new object[] { key }) as string;
                if (value != null) return value;
            }
            catch
            {
            }

            return TryGetConfigEntry(instance, key, out var entry)
                ? GetConfigEntryString(entry, defaultValue: false)
                : null;
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

            var ok = TryInvokeBoolWithError(
                instance,
                "TrySetConfigValue",
                new object[] { key, value, null },
                out error);
            if (ok) return true;

            return TrySetConfigEntryValue(instance, key, value, out error);
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

            var ok = TryInvokeBoolWithError(instance, "TrySaveConfig", new object[] { null }, out error);
            if (ok) return true;

            return TrySaveBepInExConfig(instance, out error);
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

        private static Dictionary<string, string> GetConfigEntryValues(object instance, bool defaultValues)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in ConfigFields)
            {
                if (TryGetConfigEntry(instance, pair.Key, out var entry))
                {
                    values[pair.Key] = GetConfigEntryString(entry, defaultValues) ?? string.Empty;
                }
            }
            return values;
        }

        private static bool TryGetConfigEntry(object instance, string key, out object entry)
        {
            entry = null;
            if (instance == null || string.IsNullOrWhiteSpace(key)) return false;
            if (!ConfigFields.TryGetValue(key, out var fieldName)) return false;

            entry = GetFieldValue(instance, fieldName);
            return entry != null;
        }

        private static string GetConfigEntryString(object entry, bool defaultValue)
        {
            if (entry == null) return null;

            var propertyName = defaultValue ? "DefaultValue" : "Value";
            var prop = entry.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var value = prop?.GetValue(entry, null);
            return value?.ToString();
        }

        private static bool TrySetConfigEntryValue(object instance, string key, string value, out string error)
        {
            error = string.Empty;
            if (!TryGetConfigEntry(instance, key, out var entry))
            {
                error = $"config key not found: {key}";
                return false;
            }

            try
            {
                var valueProp = entry.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (valueProp == null || !valueProp.CanWrite)
                {
                    error = $"config entry is readonly: {key}";
                    return false;
                }

                var converted = ConvertConfigValue(value, valueProp.PropertyType);
                valueProp.SetValue(entry, converted, null);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static object ConvertConfigValue(string value, Type targetType)
        {
            if (targetType == typeof(string)) return value ?? string.Empty;
            if (targetType == typeof(bool)) return bool.Parse(value ?? "false");
            if (targetType == typeof(float)) return float.Parse(value ?? "0", CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(value ?? "0", CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return int.Parse(value ?? "0", CultureInfo.InvariantCulture);
            if (targetType.IsEnum) return Enum.Parse(targetType, value ?? string.Empty, ignoreCase: true);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static bool TrySaveBepInExConfig(object instance, out string error)
        {
            error = string.Empty;
            try
            {
                var config = instance.GetType().GetProperty("Config", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(instance, null);
                var save = config?.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public);
                if (save == null)
                {
                    error = "BepInEx Config.Save not found";
                    return false;
                }

                save.Invoke(config, null);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static object GetFieldValue(object instance, string fieldName)
        {
            var type = instance.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null) return field.GetValue(instance);
                type = type.BaseType;
            }
            return null;
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
