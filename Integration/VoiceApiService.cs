using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Bulbul;
using Bulbul.MasterData;
using FastEnumUtility;
using UnityEngine;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 语音控制 API：角色语音播放、取消、场景语音触发。
    /// 所有标识符使用字符串，不依赖特定枚举值。
    /// </summary>
    public sealed class VoiceApiService : IDisposable
    {
        private readonly ManualLogSource _logger;

        /// <summary>
        /// 为 true 时，游戏侧不可自动播放语音，但本 API 仍可操作。
        /// </summary>
        public bool Locked { get; set; }

        public event Action<string, object> OnVoiceEvent;

        public VoiceApiService(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有可用的场景语音类型。
        /// </summary>
        public List<string> getScenarioTypes()
        {
            var list = new List<string>();
            foreach (var st in FastEnum.GetValues<ScenarioType>())
                list.Add(st.ToName<ScenarioType>());
            return list;
        }

        /// <summary>
        /// 直接播放指定语音资源。voiceName 为游戏内语音文件名。
        /// moveMouth 控制是否同步嘴巴动画。
        /// </summary>
        public bool playVoice(string voiceName, bool moveMouth = true)
        {
            if (string.IsNullOrEmpty(voiceName)) return false;
            var ctrl = FindVoiceController();
            if (ctrl == null) return false;

            ctrl.PlayVoice(voiceName, moveMouth, false).Forget();
            Emit("voicePlayed", new Dictionary<string, object>
            {
                ["voiceName"] = voiceName,
                ["moveMouth"] = moveMouth
            });
            return true;
        }

        /// <summary>
        /// 停止当前语音播放。
        /// </summary>
        public bool cancelVoice()
        {
            var ctrl = FindVoiceController();
            if (ctrl == null) return false;
            ctrl.CancelVoice();
            Emit("voiceCancelled", null);
            return true;
        }

        /// <summary>
        /// 语音是否播放完毕。
        /// </summary>
        public bool isFinished()
        {
            var ctrl = FindVoiceController();
            return ctrl?.IsFinishedVoice ?? true;
        }

        /// <summary>
        /// 是否正在播嘴巴动画。
        /// </summary>
        public bool isMouthMoving()
        {
            var ctrl = FindVoiceController();
            return ctrl?.IsPlayingMouthTalkMotion() ?? false;
        }

        /// <summary>
        /// 触发场景语音对话。scenarioType 为 ScenarioType 名称。
        /// </summary>
        public bool playScenarioVoice(string scenarioType, int episodeNumber = 0)
        {
            if (!FastEnum.TryParse<ScenarioType>(scenarioType, out var st)) return false;
            var facility = FindFacilityVoiceText();
            if (facility == null) return false;
            facility.WantPlayVoiceTextScenario(st, episodeNumber);
            Emit("scenarioVoicePlayed", new Dictionary<string, object>
            {
                ["scenarioType"] = scenarioType,
                ["episode"] = episodeNumber
            });
            return true;
        }

        /// <summary>
        /// 获取当前语音状态。
        /// </summary>
        public Dictionary<string, object> getState()
        {
            var ctrl = FindVoiceController();
            if (ctrl == null) return new Dictionary<string, object> { ["available"] = false };
            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["isFinished"] = ctrl.IsFinishedVoice,
                ["isMouthMoving"] = ctrl.IsPlayingMouthTalkMotion(),
            };
        }

        public void Dispose() { }

        private void Emit(string eventName, object data)
        {
            try { OnVoiceEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _logger?.LogWarning($"[VoiceApi] Emit failed ({eventName}): {ex.Message}"); }
        }

        private static HeroineVoiceController FindVoiceController()
        {
            try
            {
                var ai = UnityEngine.Object.FindObjectOfType<HeroineAI>();
                if (ai == null) return null;
                var field = typeof(HeroineAI).GetField("_heroineVoiceController",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                return field?.GetValue(ai) as HeroineVoiceController;
            }
            catch { return null; }
        }

        private static FacilityVoiceTextScenario FindFacilityVoiceText()
        {
            try { return RoomLifetimeScope.Resolve<FacilityVoiceTextScenario>(); }
            catch { return null; }
        }
    }
}
