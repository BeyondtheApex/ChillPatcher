using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using FastEnumUtility;
using NestopiSystem.DIContainers;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 模式/活动 API：Alter Ego、合作活动等特殊模式。
    /// 所有标识符使用字符串，不依赖特定枚举值。
    /// </summary>
    public sealed class ModeApiService : IDisposable
    {
        private readonly ManualLogSource _logger;

        /// <summary>
        /// 为 true 时，游戏侧 UI 不可切换模式，但本 API 仍可操作。
        /// </summary>
        public bool Locked { get; set; }

        public event Action<string, object> OnModeEvent;

        public ModeApiService(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有可用模式标识符。
        /// </summary>
        public List<string> getAvailableModes()
        {
            var list = new List<string>();
            foreach (var ct in FastEnum.GetValues<SpecialService.CollaborationType>())
                list.Add(ct.ToName<SpecialService.CollaborationType>());
            return list;
        }

        /// <summary>
        /// 获取当前激活的模式。
        /// </summary>
        public string getCurrentMode()
        {
            var save = SaveDataManager.Instance;
            if (save == null) return "None";
            return save.CollaborationSaveData.CurrentType.Value
                .ToName<SpecialService.CollaborationType>();
        }

        /// <summary>
        /// 获取当前模式的详细状态。
        /// </summary>
        public Dictionary<string, object> getModeState()
        {
            var save = SaveDataManager.Instance;
            var current = save?.CollaborationSaveData?.CurrentType?.Value
                          ?? SpecialService.CollaborationType.None;
            return new Dictionary<string, object>
            {
                ["current"] = current.ToName<SpecialService.CollaborationType>(),
                ["canChange"] = canChangeMode()
            };
        }

        /// <summary>
        /// 切换到指定模式。id 为 CollaborationType 名称（如 "AlterEgo", "None"）。
        /// </summary>
        public bool setMode(string id)
        {
            if (!FastEnum.TryParse<SpecialService.CollaborationType>(id, out var mode))
                return false;

            var save = SaveDataManager.Instance;
            if (save == null) return false;

            save.CollaborationSaveData.CurrentType.Value = mode;
            save.SaveCollaborationData();
            Emit("modeChanged", new Dictionary<string, object> { ["mode"] = id });
            return true;
        }

        /// <summary>
        /// 是否可以切换模式（游戏可能在某些状态下禁用切换）。
        /// </summary>
        public bool canChangeMode()
        {
            var svc = ResolveSpecialService();
            return svc?.IsPossibleChangeSpecial() ?? false;
        }

        public void Dispose() { }

        private void Emit(string eventName, object data)
        {
            try { OnModeEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _logger?.LogWarning($"[ModeApi] Emit failed ({eventName}): {ex.Message}"); }
        }

        private static SpecialService ResolveSpecialService()
        {
            try { return RoomLifetimeScope.Resolve<SpecialService>(); }
            catch { return null; }
        }
    }
}
