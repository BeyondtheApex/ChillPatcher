using System;
using System.Collections.Generic;
using BepInEx.Logging;
using FastEnumUtility;
using UnityEngine;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 角色行为控制 API：状态查询、状态切换、动画与语音。
    /// 所有标识符使用字符串，不依赖特定枚举值。
    /// </summary>
    public sealed class CharacterApiService : IDisposable
    {
        private readonly ManualLogSource _logger;

        /// <summary>
        /// 为 true 时，游戏侧 AI 不可自动切换角色状态，但本 API 仍可操作。
        /// </summary>
        public bool Locked { get; set; }

        public event Action<string, object> OnCharacterEvent;

        public CharacterApiService(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有可用的角色动作状态标识符。
        /// </summary>
        public List<string> getAvailableStates()
        {
            var list = new List<string>();
            foreach (var s in FastEnum.GetValues<HeroineAI.ActionStateType>())
                list.Add(s.ToName<HeroineAI.ActionStateType>());
            return list;
        }

        /// <summary>
        /// 获取当前角色状态。
        /// </summary>
        public Dictionary<string, object> getState()
        {
            var ai = FindHeroineAI();
            if (ai == null)
            {
                return new Dictionary<string, object>
                {
                    ["available"] = false
                };
            }

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["updateState"] = ai.CurrentUpdateState.ToName<HeroineAI.UpdateStateType>(),
                ["isSleeping"] = ai.IsSleeping,
                ["canChange"] = ai.IsPossibleChangeAction()
            };
        }

        /// <summary>
        /// 强制切换角色动作状态（例如 "WorkPC", "BreakTeaTime"）。
        /// </summary>
        public bool setState(string stateId)
        {
            var ai = FindHeroineAI();
            if (ai == null) return false;
            if (!FastEnum.TryParse<HeroineAI.ActionStateType>(stateId, out var state)) return false;
            ai.DebugChangeState(state);
            Emit("stateChanged", new Dictionary<string, object> { ["state"] = stateId });
            return true;
        }

        /// <summary>
        /// 让角色开始工作状态。
        /// </summary>
        public bool startWork()
        {
            var ai = FindHeroineAI();
            if (ai == null) return false;
            ai.StartWork();
            Emit("stateChanged", new Dictionary<string, object> { ["state"] = "Work" });
            return true;
        }

        /// <summary>
        /// 让角色开始休息状态。
        /// </summary>
        public bool startBreak()
        {
            var ai = FindHeroineAI();
            if (ai == null) return false;
            ai.StartBreak();
            Emit("stateChanged", new Dictionary<string, object> { ["state"] = "Break" });
            return true;
        }

        /// <summary>
        /// 取消当前状态转换。
        /// </summary>
        public bool cancelChange()
        {
            var ai = FindHeroineAI();
            if (ai == null) return false;
            ai.CancelChangeAction();
            Emit("changeCancelled", null);
            return true;
        }

        /// <summary>
        /// 自动切换到当前番茄钟阶段匹配的动作。
        /// </summary>
        public bool matchCurrentAction()
        {
            var ai = FindHeroineAI();
            if (ai == null) return false;
            if (!ai.IsPossibleChangeAction()) return false;
            ai.ChangeCurrentMatcheAction(false);
            Emit("stateChanged", new Dictionary<string, object> { ["state"] = "matched" });
            return true;
        }

        /// <summary>
        /// 自动切换并可能触发野生动作（伸懒腰、喝茶等）。
        /// </summary>
        public bool matchCurrentActionWithWild()
        {
            var ai = FindHeroineAI();
            if (ai == null) return false;
            if (!ai.IsPossibleChangeAction()) return false;
            ai.ChangeCurrentMatcheAction(true);
            Emit("stateChanged", new Dictionary<string, object> { ["state"] = "matchedWild" });
            return true;
        }

        public void Dispose() { }

        private void Emit(string eventName, object data)
        {
            try { OnCharacterEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _logger?.LogWarning($"[CharacterApi] Emit failed ({eventName}): {ex.Message}"); }
        }

        private static HeroineAI FindHeroineAI()
        {
            try { return UnityEngine.Object.FindObjectOfType<HeroineAI>(); }
            catch { return null; }
        }
    }
}
