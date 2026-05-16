using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace ChillPatcher.Module.LocalIntegration
{
    internal static class AIChatSpeakerAdapter
    {
        private const string AiChatGuid = "com.username.chillaimod";

        public static bool TrySpeakText(string voiceText, string subtitleText, string emotionTag, ManualLogSource logger, out string error)
        {
            error = string.Empty;
            var instance = GetAiChatInstance();
            if (instance == null)
            {
                error = "未安装 AIChat";
                return false;
            }

            if (string.IsNullOrWhiteSpace(voiceText))
            {
                error = "text is empty";
                return false;
            }

            if (!(instance is MonoBehaviour monoBehaviour))
            {
                error = "AIChat 实例不是 MonoBehaviour";
                return false;
            }

            var busy = GetPrivateFieldValue(instance, "_isProcessing");
            if (busy is bool isBusy && isBusy)
            {
                error = "AIChat 正忙";
                return false;
            }

            SetPrivateFieldValue(instance, "_isProcessing", true);
            monoBehaviour.StartCoroutine(SpeakTextRoutine(instance, monoBehaviour, voiceText, subtitleText, emotionTag, logger));
            return true;
        }

        private static IEnumerator SpeakTextRoutine(object instance, MonoBehaviour monoBehaviour, string voiceText, string subtitleText, string emotionTag, ManualLogSource logger)
        {
            var assembly = instance.GetType().Assembly;
            var uiHelperType = assembly.GetType("AIChat.Unity.UIHelper");
            var responseParserType = assembly.GetType("AIChat.Core.ResponseParser");
            var ttsClientType = assembly.GetType("AIChat.Services.TTSClient");
            var playNativeAnimation = instance.GetType().GetMethod("PlayNativeAnimation", BindingFlags.Instance | BindingFlags.NonPublic);

            var uiStatusMap = new Dictionary<GameObject, bool>();
            GameObject originalTextObj = null;
            GameObject overlayTextObj = null;

            try
            {
                originalTextObj = GameObject.Find("Canvas/StorySystemUI/MessageWindow/NormalTextParent/NormalTextMessage");
                if (originalTextObj == null)
                {
                    logger?.LogWarning("[AIChatSpeakHandler] 未找到 NormalTextMessage");
                    yield break;
                }

                if (uiHelperType == null || responseParserType == null || ttsClientType == null || playNativeAnimation == null)
                {
                    logger?.LogWarning("[AIChatSpeakHandler] 未找到 AIChat 辅助类型或方法");
                    yield break;
                }

                var forceShowWindow = uiHelperType.GetMethod("ForceShowWindow", BindingFlags.Public | BindingFlags.Static);
                var createOverlayText = uiHelperType.GetMethod("CreateOverlayText", BindingFlags.Public | BindingFlags.Static);
                var restoreUiStatus = uiHelperType.GetMethod("RestoreUiStatus", BindingFlags.Public | BindingFlags.Static);
                var insertLineBreaks = responseParserType.GetMethod("InsertLineBreaks", BindingFlags.Public | BindingFlags.Static);
                var downloadVoice = ttsClientType.GetMethod("DownloadVoiceWithRetry", BindingFlags.Public | BindingFlags.Static);

                if (forceShowWindow == null || createOverlayText == null || restoreUiStatus == null || downloadVoice == null)
                {
                    logger?.LogWarning("[AIChatSpeakHandler] 未找到 AIChat 播放相关方法");
                    yield break;
                }

                forceShowWindow.Invoke(null, new object[] { originalTextObj, uiStatusMap });
                originalTextObj.SetActive(false);

                var parent = originalTextObj.transform.parent != null ? originalTextObj.transform.parent.gameObject : originalTextObj;
                overlayTextObj = createOverlayText.Invoke(null, new object[] { parent }) as GameObject;
                var overlayText = overlayTextObj != null ? overlayTextObj.GetComponent<Text>() : null;
                if (overlayText != null)
                {
                    overlayText.text = "message is sending through cyber space";
                    overlayText.color = Color.yellow;
                }

                var subtitle = string.IsNullOrWhiteSpace(subtitleText) ? voiceText : subtitleText;
                if (insertLineBreaks != null)
                {
                    subtitle = insertLineBreaks.Invoke(null, new object[] { subtitle, 25 }) as string ?? subtitle;
                }

                AudioClip downloadedClip = null;
                Action<AudioClip> onComplete = clip => downloadedClip = clip;

                var ttsUrl = (GetConfigEntryValue(instance, "TTS_Service_URL") ?? "http://127.0.0.1:9880").TrimEnd('/') + "/tts";
                var targetLang = GetConfigEntryValue(instance, "TargetLang") ?? "zh";
                var refPath = GetConfigEntryValue(instance, "Audio_File_Path") ?? "";
                var promptText = GetConfigEntryValue(instance, "Audio_File_Text") ?? "";
                var promptLang = GetConfigEntryValue(instance, "PromptLang") ?? "ja";
                var audioPathCheck = bool.TryParse(GetConfigEntryValue(instance, "AudioPathCheck"), out var check) && check;

                var ttsRoutine = downloadVoice.Invoke(null, new object[]
                {
                    ttsUrl,
                    voiceText,
                    targetLang,
                    refPath,
                    promptText,
                    promptLang,
                    logger,
                    onComplete,
                    3,
                    30f,
                    audioPathCheck
                }) as IEnumerator;

                if (ttsRoutine != null)
                {
                    yield return monoBehaviour.StartCoroutine(ttsRoutine);
                }

                if (downloadedClip != null && !downloadedClip.LoadAudioData())
                {
                    yield return null;
                }

                if (overlayText != null)
                {
                    overlayText.text = subtitle;
                    overlayText.color = Color.white;
                }

                var animationRoutine = playNativeAnimation.Invoke(instance, new object[] { NormalizeEmotion(emotionTag), downloadedClip }) as IEnumerator;
                if (animationRoutine != null)
                {
                    yield return monoBehaviour.StartCoroutine(animationRoutine);
                }

                restoreUiStatus.Invoke(null, new object[] { uiStatusMap, overlayTextObj, originalTextObj });
                overlayTextObj = null;
            }
            finally
            {
                TryRestoreUi(uiHelperType, uiStatusMap, overlayTextObj, originalTextObj, logger);
                SetPrivateFieldValue(instance, "_isProcessing", false);
            }
        }

        private static object GetAiChatInstance()
        {
            if (!Chainloader.PluginInfos.TryGetValue(AiChatGuid, out var pluginInfo)) return null;
            return pluginInfo?.Instance;
        }

        private static string GetConfigEntryValue(object instance, string key)
        {
            var fieldName = ConfigFieldName(key);
            if (string.IsNullOrEmpty(fieldName)) return null;

            var entry = GetPrivateFieldValue(instance, fieldName);
            var valueProp = entry?.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            return valueProp?.GetValue(entry, null)?.ToString();
        }

        private static string ConfigFieldName(string key)
        {
            switch (key)
            {
                case "TTS_Service_URL": return "_sovitsUrlConfig";
                case "Audio_File_Path": return "_refAudioPathConfig";
                case "AudioPathCheck": return "_audioPathCheckConfig";
                case "Audio_File_Text": return "_promptTextConfig";
                case "PromptLang": return "_promptLangConfig";
                case "TargetLang": return "_targetLangConfig";
                default: return null;
            }
        }

        private static object GetPrivateFieldValue(object instance, string fieldName)
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

        private static void SetPrivateFieldValue(object instance, string fieldName, object value)
        {
            var type = instance.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(instance, value);
                    return;
                }
                type = type.BaseType;
            }
        }

        private static string NormalizeEmotion(string value)
        {
            switch ((value ?? "").Trim('[', ']', ' '))
            {
                case "Happy":
                case "Confused":
                case "Sad":
                case "Fun":
                case "Agree":
                case "Drink":
                case "Wave":
                case "Think":
                    return value.Trim('[', ']', ' ');
                default:
                    return "Agree";
            }
        }

        private static void TryRestoreUi(Type uiHelperType, Dictionary<GameObject, bool> uiStatusMap, GameObject overlayTextObj, GameObject originalTextObj, ManualLogSource logger)
        {
            try
            {
                var restoreUiStatus = uiHelperType?.GetMethod("RestoreUiStatus", BindingFlags.Public | BindingFlags.Static);
                restoreUiStatus?.Invoke(null, new object[] { uiStatusMap, overlayTextObj, originalTextObj });
            }
                catch (Exception ex)
                {
                    logger?.LogWarning($"[AIChatSpeakHandler] 恢复 UI 失败：{ex.Message}");
                }
        }
    }
}
