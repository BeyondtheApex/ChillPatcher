using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Bulbul;
using NestopiSystem.DIContainers;
using UnityEngine;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 字幕 API：显示自定义文本字幕、触发游戏内置对话。
    /// 使用游戏的 ScenarioTextMessage 系统显示带打字机动画的文本。
    /// </summary>
    public sealed class SubtitleApiService : IDisposable
    {
        private readonly ManualLogSource _logger;
        private bool _isShowingCustom;
        private float _hideTime;

        /// <summary>
        /// 为 true 时，游戏侧不可自动播放对话/字幕，但本 API 仍可操作。
        /// </summary>
        public bool Locked { get; set; }

        public event Action<string, object> OnSubtitleEvent;

        public SubtitleApiService(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 显示自定义字幕文本（使用游戏的打字机动画）。
        /// duration 为 0 表示不自动消失。
        /// </summary>
        public bool show(string text, float duration = 0f)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var storyUI = FindStorySystemUI();
            if (storyUI == null)
            {
                _logger?.LogWarning("[SubtitleApi] StorySystemUI not found");
                return false;
            }

            // 激活字幕面板（滑入动画 + alpha 渐变）
            storyUI.ActivateNormalText();

            // 获取 _normalTextMessage 并显示文本
            var textMsg = GetNormalTextMessage(storyUI);
            if (textMsg != null)
                textMsg.StartText(text);

            _isShowingCustom = true;

            if (duration > 0f)
                _hideTime = Time.realtimeSinceStartup + duration;
            else
                _hideTime = 0f;

            Emit("show", new Dictionary<string, object>
            {
                ["text"] = text,
                ["duration"] = duration
            });
            return true;
        }

        /// <summary>
        /// 隐藏当前字幕。
        /// </summary>
        public bool hide()
        {
            var storyUI = FindStorySystemUI();
            if (storyUI == null) return false;

            storyUI.DeactivateNormalText(null);
            _isShowingCustom = false;
            _hideTime = 0f;
            Emit("hide", null);
            return true;
        }

        /// <summary>
        /// 是否正在显示自定义字幕。
        /// </summary>
        public bool isShowing()
        {
            return _isShowingCustom;
        }

        /// <summary>
        /// 每帧由外部调用（可选），处理自动隐藏。
        /// </summary>
        public void Tick()
        {
            if (_isShowingCustom && _hideTime > 0f && Time.realtimeSinceStartup >= _hideTime)
            {
                hide();
            }
        }

        /// <summary>
        /// 触发游戏内置的语音文本对话。
        /// scenarioType 和 episodeNumber 为游戏原始参数。
        /// </summary>
        public bool playScenario(string scenarioType, int episodeNumber)
        {
            if (!Enum.TryParse<Bulbul.MasterData.ScenarioType>(scenarioType, out var st)) return false;

            var facility = FindFacilityVoiceText();
            if (facility == null) return false;

            facility.WantPlayVoiceTextScenario(st, episodeNumber);
            Emit("scenarioStarted", new Dictionary<string, object>
            {
                ["scenarioType"] = scenarioType,
                ["episode"] = episodeNumber
            });
            return true;
        }

        /// <summary>
        /// 获取游戏内置对话的状态。
        /// </summary>
        public Dictionary<string, object> getScenarioState()
        {
            var facility = FindFacilityVoiceText();
            if (facility == null) return new Dictionary<string, object> { ["available"] = false };
            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["isReady"] = facility.IsStartReady(),
                ["isEnded"] = facility.IsPlayEnd()
            };
        }

        public void Dispose()
        {
            _isShowingCustom = false;
        }

        private void Emit(string eventName, object data)
        {
            try { OnSubtitleEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _logger?.LogWarning($"[SubtitleApi] Emit failed ({eventName}): {ex.Message}"); }
        }

        private static StorySystemUI FindStorySystemUI()
        {
            try { return UnityEngine.Object.FindObjectOfType<StorySystemUI>(); }
            catch { return null; }
        }

        private static ScenarioTextMessage GetNormalTextMessage(StorySystemUI storyUI)
        {
            try
            {
                var field = typeof(StorySystemUI).GetField("_normalTextMessage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                return field?.GetValue(storyUI) as ScenarioTextMessage;
            }
            catch { return null; }
        }

        private static FacilityVoiceTextScenario FindFacilityVoiceText()
        {
            try
            {
                return RoomLifetimeScope.Resolve<FacilityVoiceTextScenario>();
            }
            catch
            {
                return null;
            }
        }
    }
}
