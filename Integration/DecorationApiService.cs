using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using FastEnumUtility;
using NestopiSystem.DIContainers;
using UnityEngine;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 物件/装饰 API：马克杯、键盘、眼镜、台灯、桌椅等。
    /// 所有标识符使用字符串，不依赖特定枚举值。
    /// </summary>
    public sealed class DecorationApiService : IDisposable
    {
        private readonly ManualLogSource _logger;

        /// <summary>
        /// 为 true 时，游戏侧 UI 不可切换装饰，但本 API 仍可操作。
        /// </summary>
        public bool Locked { get; set; }

        public event Action<string, object> OnDecorationEvent;

        public DecorationApiService(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有装饰类别。
        /// </summary>
        public List<string> getCategories()
        {
            var list = new List<string>();
            foreach (var cat in FastEnum.GetValues<DecorationService.DecorationCategoryType>())
                list.Add(cat.ToName<DecorationService.DecorationCategoryType>());
            return list;
        }

        /// <summary>
        /// 获取所有装饰皮肤及当前激活状态。
        /// </summary>
        public List<Dictionary<string, object>> getDecorations()
        {
            var list = new List<Dictionary<string, object>>();
            var save = SaveDataManager.Instance;
            if (save == null) return list;

            foreach (var kv in save.DecorationSaveData.DecorationDic)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["id"] = kv.Key.ToName<DecorationService.DecorationSkinType>(),
                    ["active"] = kv.Value.IsActive.Value
                });
            }
            return list;
        }

        /// <summary>
        /// 获取指定装饰皮肤是否激活。
        /// </summary>
        public bool isActive(string skinId)
        {
            var save = SaveDataManager.Instance;
            if (save == null) return false;
            if (!FastEnum.TryParse<DecorationService.DecorationSkinType>(skinId, out var skin)) return false;
            DecorationData data;
            if (!save.DecorationSaveData.DecorationDic.TryGetValue(skin, out data)) return false;
            return data.IsActive.Value;
        }

        /// <summary>
        /// 切换装饰皮肤（同类别中会自动互斥）。
        /// </summary>
        public bool setDecoration(string skinId, bool save = true)
        {
            var svc = FindDecorationService();
            if (svc == null) return false;
            if (!svc.ParseDecorationTypeForString(skinId, out var skin)) return false;
            svc.ChangeDecoration(skin, save);
            Emit("decorationChanged", new Dictionary<string, object> { ["id"] = skinId });
            return true;
        }

        /// <summary>
        /// 关闭指定类别的所有装饰。
        /// </summary>
        public bool deactivateCategory(string categoryId, bool save = true)
        {
            var svc = FindDecorationService();
            if (svc == null) return false;
            if (!FastEnum.TryParse<DecorationService.DecorationCategoryType>(categoryId, out var cat)) return false;
            svc.DeactivateAllModels(cat, save);
            Emit("categoryDeactivated", new Dictionary<string, object> { ["category"] = categoryId });
            return true;
        }

        /// <summary>
        /// 从存档重新应用所有装饰状态。
        /// </summary>
        public bool reloadFromSave()
        {
            var svc = FindDecorationService();
            if (svc == null) return false;
            svc.ApplyDecorationBySavedata();
            Emit("reloaded", null);
            return true;
        }

        /// <summary>
        /// 获取当前马克杯/键盘的模型类型。
        /// </summary>
        public Dictionary<string, string> getCurrentModels()
        {
            var svc = FindDecorationService();
            if (svc == null) return new Dictionary<string, string>();
            return new Dictionary<string, string>
            {
                ["mugCup"] = svc.CurrentMugCupModel?.Value.ToString() ?? "",
                ["keyboard"] = svc.CurrentKeyboardModel?.Value.ToString() ?? ""
            };
        }

        public void Dispose() { }

        private void Emit(string eventName, object data)
        {
            try { OnDecorationEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _logger?.LogWarning($"[DecorationApi] Emit failed ({eventName}): {ex.Message}"); }
        }

        private static DecorationService FindDecorationService()
        {
            try { return UnityEngine.Object.FindObjectOfType<DecorationService>(); }
            catch { return null; }
        }
    }
}
