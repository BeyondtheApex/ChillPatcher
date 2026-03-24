using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using FastEnumUtility;
using NestopiSystem.DIContainers;

namespace ChillPatcher.Integration
{
    /// <summary>
    /// 环境控制与查询 API：窗景、音效、昼夜、预设。
    /// 所有标识符使用字符串，不依赖特定枚举值。
    /// </summary>
    public sealed class EnvironmentApiService : IDisposable
    {
        private readonly ManualLogSource _logger;

        /// <summary>
        /// 为 true 时，游戏侧 UI 不可切换环境，但本 API 仍可操作。
        /// </summary>
        public bool Locked { get; set; }

        public event Action<string, object> OnEnvironmentEvent;

        public EnvironmentApiService(ManualLogSource logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有可用环境列表（字符串 id + 是否锁定 + 类型信息）。
        /// </summary>
        public List<Dictionary<string, object>> getEnvironments()
        {
            var list = new List<Dictionary<string, object>>();
            var unlockSvc = ResolveUnlockItemService();

            foreach (var et in FastEnum.GetValues<EnvironmentType>())
            {
                var name = et.ToName<EnvironmentType>();
                bool locked = false;
                if (unlockSvc != null)
                {
                    try { locked = unlockSvc.Environment.GetLockState(et).IsLocked.CurrentValue; }
                    catch { }
                }

                string controllerType = "unknown";
                try { controllerType = et.GetEnvironmentControllerType().ToString(); }
                catch { }

                list.Add(new Dictionary<string, object>
                {
                    ["id"] = name,
                    ["locked"] = locked,
                    ["type"] = controllerType
                });
            }

            return list;
        }

        /// <summary>
        /// 获取指定窗景是否激活。id 为 EnvironmentType 名称中的 WindowView 子集。
        /// </summary>
        public bool isViewActive(string id)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return false;
            if (!FastEnum.TryParse<WindowViewType>(id, out var wv)) return false;
            return svc.IsWindowActive(wv);
        }

        /// <summary>
        /// 设置窗景激活/关闭。
        /// </summary>
        public bool setViewActive(string id, bool active)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return false;
            if (!FastEnum.TryParse<WindowViewType>(id, out var wv)) return false;
            svc.SetViewActive(wv, active);
            try { ResolveAppService()?.ApplyWindow(wv, active); } catch { }
            Emit("viewChanged", new Dictionary<string, object> { ["id"] = id, ["active"] = active });
            return true;
        }

        /// <summary>
        /// 获取环境音量和静音状态。
        /// </summary>
        public Dictionary<string, object> getSoundState(string id)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return null;
            if (!FastEnum.TryParse<AmbientSoundType>(id, out var st)) return null;
            var (volume, isMute) = svc.GetVolume(st);
            return new Dictionary<string, object>
            {
                ["id"] = id,
                ["volume"] = volume,
                ["muted"] = isMute
            };
        }

        /// <summary>
        /// 设置环境音量（0.0 ~ 1.0）。
        /// </summary>
        public bool setSoundVolume(string id, float volume)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return false;
            if (!FastEnum.TryParse<AmbientSoundType>(id, out var st)) return false;
            volume = Math.Max(0f, Math.Min(1f, volume));
            svc.SetVolume(st, volume);
            try
            {
                var (_, isMute) = svc.GetVolume(st);
                ResolveAppService()?.ApplySound(st, !isMute, volume);
            }
            catch { }
            Emit("soundVolumeChanged", new Dictionary<string, object> { ["id"] = id, ["volume"] = volume });
            return true;
        }

        /// <summary>
        /// 设置环境音静音。
        /// </summary>
        public bool setSoundMute(string id, bool mute)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return false;
            if (!FastEnum.TryParse<AmbientSoundType>(id, out var st)) return false;
            svc.SetMute(st, mute);
            try
            {
                var (vol, _) = svc.GetVolume(st);
                ResolveAppService()?.ApplySound(st, !mute, vol);
            }
            catch { }
            Emit("soundMuteChanged", new Dictionary<string, object> { ["id"] = id, ["muted"] = mute });
            return true;
        }

        /// <summary>
        /// 获取自动昼夜切换设置。
        /// </summary>
        public Dictionary<string, object> getAutoTimeSettings()
        {
            var save = SaveDataManager.Instance;
            if (save == null) return null;
            var data = save.AutoTimeWindowChangeData;
            return new Dictionary<string, object>
            {
                ["enabled"] = data.IsActiveAuto.Value,
                ["dayStartHour"] = data.TimeDayStart,
                ["sunsetStartHour"] = data.TimeSunsetStart,
                ["nightStartHour"] = data.TimeNightStart
            };
        }

        /// <summary>
        /// 设置自动昼夜切换。
        /// </summary>
        public bool setAutoTimeSettings(bool? enabled, float? dayStart, float? sunsetStart, float? nightStart)
        {
            var save = SaveDataManager.Instance;
            if (save == null) return false;
            var data = save.AutoTimeWindowChangeData;
            if (enabled.HasValue) data.IsActiveAuto.Value = enabled.Value;
            if (dayStart.HasValue) data.TimeDayStart = dayStart.Value;
            if (sunsetStart.HasValue) data.TimeSunsetStart = sunsetStart.Value;
            if (nightStart.HasValue) data.TimeNightStart = nightStart.Value;
            save.SaveAutoTimeWindowChangeData();
            Emit("autoTimeSettingsChanged", getAutoTimeSettings());
            return true;
        }

        /// <summary>
        /// 加载环境预设。
        /// </summary>
        public bool loadPreset(int index)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return false;
            svc.LoadPreset(index);
            // 刷新视觉状态（窗景 + 音效）
            try { ResolveApplyController()?.ApplyWindowBySavedata(); } catch { }
            try { ApplyAllSounds(svc); } catch { }
            Emit("presetLoaded", new Dictionary<string, object> { ["index"] = index });
            return true;
        }

        /// <summary>
        /// 保存当前配置到预设。
        /// </summary>
        public bool saveToPreset(int index)
        {
            var svc = ResolveEnvironmentDataService();
            if (svc == null) return false;
            svc.SaveCurrentToPreset(index, true);
            Emit("presetSaved", new Dictionary<string, object> { ["index"] = index });
            return true;
        }

        /// <summary>
        /// 获取当前激活的预设索引。
        /// </summary>
        public int getCurrentPresetIndex()
        {
            var svc = ResolveEnvironmentDataService();
            return svc?.GetCurrentPresetIndex() ?? -1;
        }

        public void Dispose() { }

        private void Emit(string eventName, object data)
        {
            try { OnEnvironmentEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _logger?.LogWarning($"[EnvironmentApi] Emit failed ({eventName}): {ex.Message}"); }
        }

        private static EnvironmentDataService ResolveEnvironmentDataService()
        {
            try { return RoomLifetimeScope.Resolve<EnvironmentDataService>(); }
            catch { return null; }
        }

        private static UnlockItemService ResolveUnlockItemService()
        {
            try { return RoomLifetimeScope.Resolve<UnlockItemService>(); }
            catch { return null; }
        }

        private static Bulbul.EnvironmentApplicationService ResolveAppService()
        {
            try { return RoomLifetimeScope.Resolve<Bulbul.EnvironmentApplicationService>(); }
            catch { return null; }
        }

        private static Bulbul.IApplyEnvironmentWindowController ResolveApplyController()
        {
            try { return RoomLifetimeScope.Resolve<Bulbul.IApplyEnvironmentWindowController>(); }
            catch { return null; }
        }

        /// <summary>
        /// 读取 save data 后重新应用所有环境音的实际音量。
        /// </summary>
        private void ApplyAllSounds(EnvironmentDataService dataSvc)
        {
            var appSvc = ResolveAppService();
            if (appSvc == null) return;
            foreach (var st in FastEnum.GetValues<AmbientSoundType>())
            {
                try
                {
                    var (vol, isMute) = dataSvc.GetVolume(st);
                    appSvc.ApplySound(st, !isMute, vol);
                }
                catch { }
            }
        }
    }
}
