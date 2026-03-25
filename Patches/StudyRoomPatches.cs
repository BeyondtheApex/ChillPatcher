using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using Bulbul.MasterData;
using ChillPatcher.Integration.StudyRoom;
using HarmonyLib;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// 自习室 Harmony Patches
    /// 主机端: 捕获游戏事件 → 广播给客户端
    /// 客户端: 拦截本地保存 → 转发给主机
    /// </summary>
    public static class StudyRoomPatches
    {
        private static ManualLogSource _log;
        private static Harmony _hostHarmony;
        private static Harmony _clientHarmony;

        public static void Initialize(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>
        /// 安装所有通用 Patches (主机+客户端都需要)
        /// </summary>
        public static void PatchCommon()
        {
            // 通用 patches 已由主 Harmony 实例在 Plugin.Awake() 中统一安装
            // 这里的 [HarmonyPatch] 属性类会自动被扫描
        }

        /// <summary>
        /// 安装主机端专用 Patches
        /// </summary>
        public static void PatchHost()
        {
            if (_hostHarmony != null) return;
            _hostHarmony = new Harmony("com.chillpatcher.studyroom.host");
            _hostHarmony.PatchAll(typeof(HostPatches));
            _log?.LogInfo("[StudyRoomPatches] Host patches installed");
        }

        /// <summary>
        /// 安装客户端专用 Patches
        /// </summary>
        public static void PatchClient()
        {
            if (_clientHarmony != null) return;
            _clientHarmony = new Harmony("com.chillpatcher.studyroom.client");
            _clientHarmony.PatchAll(typeof(ClientPatches));
            _log?.LogInfo("[StudyRoomPatches] Client patches installed");
        }

        /// <summary>
        /// 卸载主机端 Patches
        /// </summary>
        public static void UnpatchHost()
        {
            _hostHarmony?.UnpatchSelf();
            _hostHarmony = null;
            _log?.LogInfo("[StudyRoomPatches] Host patches uninstalled");
        }

        /// <summary>
        /// 卸载客户端 Patches
        /// </summary>
        public static void UnpatchClient()
        {
            _clientHarmony?.UnpatchSelf();
            _clientHarmony = null;
            _log?.LogInfo("[StudyRoomPatches] Client patches uninstalled");
        }

        // ═════════════════════════════════════════════
        //  主机端 Patches: 捕获游戏事件 → 广播
        // ═════════════════════════════════════════════

        private static class HostPatches
        {
            /// <summary>
            /// Patch 1: 角色状态变化
            /// 捕获 HeroineAI.ChangeState() → 广播 StateChanged
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HeroineAI), "ChangeState",
                new Type[] { typeof(HeroineAI.ActionStateType) })]
            static void OnStateChanged(HeroineAI.ActionStateType nextAction)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastStateChanged(nextAction.ToString());

                // Fix 7: WantTalk → 广播 StoryReady (主线故事)
                if (nextAction == HeroineAI.ActionStateType.WantTalk)
                {
                    try
                    {
                        var progressData = SaveDataManager.Instance?.ScenarioProgressData;
                        if (progressData != null && progressData.IsPossibleTalkNextMainEpisode())
                        {
                            Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                                .BroadcastStoryReady("MainScenario", (int)progressData.NextEpisodeNumber);
                        }
                    }
                    catch (Exception ex) { _log?.LogWarning($"[StudyRoomPatch] StoryReady error: {ex.Message}"); }
                }
            }

            /// <summary>
            /// Patch 2: 场景对话触发
            /// 捕获 FacilityVoiceTextScenario.WantPlayVoiceTextScenario()
            /// → 广播 ScenarioPlay
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(FacilityVoiceTextScenario),
                "WantPlayVoiceTextScenario")]
            static void OnScenarioPlay(
                ScenarioType scenarioType, int episodeNumber)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastScenarioPlay(scenarioType.ToString(), episodeNumber);
            }

            /// <summary>
            /// Patch 3: 独立语音播放 (非场景触发)
            /// 捕获 HeroineVoiceController.PlayVoice()
            /// → 广播 VoicePlay
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HeroineVoiceController), "PlayVoice")]
            static void OnVoicePlay(
                string voiceName, bool isMoveMouse, bool isStory)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (isStory) return; // 故事语音由 ScenarioPlay 同步
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastVoicePlay(voiceName, isMoveMouse);
            }

            // ─── 存档保存捕获 → 广播给客户端 ───

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveTodoList")]
            static void OnHostTodoSaved(object todoList)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                    SaveDataOpType.TodoItemUpdate, todoList);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveCalenderData", new Type[] { typeof(CalenderMonthlyData) })]
            static void OnHostCalendarSaved(object data)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                    SaveDataOpType.CalendarDiaryEdit, data);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveNoteList")]
            static void OnHostNoteListSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                var save = SaveDataManager.Instance;
                Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                    SaveDataOpType.NotePageReorder, save?.NoteData?.NoteList);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SavePageData")]
            static void OnHostPageSaved(object pageData)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                    SaveDataOpType.NotePageUpdate, pageData);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveHabitHeaders")]
            static void OnHostHabitHeadersSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                var save = SaveDataManager.Instance;
                Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                    SaveDataOpType.HabitUpdate,
                    new { headerData = save?.AllHabitHeaderData });
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveHabitsMonthlyData")]
            static void OnHostHabitMonthlySaved(int year, int month)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                var save = SaveDataManager.Instance;
                Bulbul.HabitAllMonthlyData monthlyData;
                if (save != null && save.TryGetOrLoadHabitsMonthlyData(year, month, out monthlyData))
                {
                    Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                        SaveDataOpType.HabitToggleDay,
                        new { year, month, monthlyData });
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveHabitDeadPeriods")]
            static void OnHostHabitDeadPeriodsSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                var save = SaveDataManager.Instance;
                Integration.StudyRoom.SaveDataSyncManager.BroadcastSaveDataChanged(
                    SaveDataOpType.HabitPause,
                    new { deadPeriodData = save?.AllHabitDeadPeriodData });
            }

            // ─── 装饰/环境/经济保存捕获 ───

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveDecorationThrottled")]
            static void OnHostDecorationSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastDecorationSnapshot();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveEnviromentThrottled")]
            static void OnHostEnvironmentSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastEnvironmentSnapshot();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveAutoTimeWindowChangeData")]
            static void OnHostAutoTimeSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastAutoTimeSnapshot();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveEnvironmentProgressData")]
            static void OnHostPointPurchaseSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastPointPurchaseSync();
            }

            // ─── 经验/等级/经济保存捕获 ───

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SavePlayerData")]
            static void OnHostPlayerDataSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                var hostSync = Integration.StudyRoom.StudyRoomService.Instance?.HostSync;
                hostSync?.BroadcastWorkSecondsSync();
                hostSync?.BroadcastLevelChanged();
                hostSync?.BroadcastExpChanged();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveDataManager), "SavePointPurchaseData")]
            static void OnHostPointPurchaseDataSaved()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    .BroadcastPointPurchaseSync();
            }

            // ─── 互动锁: 主机点击获取锁 / 互动结束释放锁 ───

            /// <summary>
            /// Fix #21: 改为 Prefix + ref bool __runOriginal 模式.
            /// 在原方法执行前检查互动锁，被远程执行时跳过锁检查。
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(FacilityClickHeroine), "ReactionReady")]
            static bool OnHostReactionReadyPrefix(ref bool __result)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return true;
                // 远程交互由 HostSyncManager 直接调用，不检查锁
                if (Integration.StudyRoom.HostSyncManager.IsExecutingRemoteInteraction) return true;
                // 本地点击先尝试获取锁
                if (!(Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                        .TryAcquireHostLock("ClickHeroine") ?? true))
                {
                    __result = false;
                    return false; // 阻止原方法执行
                }
                return true; // 继续执行原方法
            }

            /// <summary>
            /// 主机互动结束时释放锁并广播 InteractionEnd
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(FacilityClickHeroine), "EndReaction")]
            static void OnHostReactionEnd()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    ?.ReleaseHostLock("ClickHeroine");
            }

            // ─── 番茄钟事件捕获 (Fix #2) ───

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PomodoroService), "StartPomodoro")]
            static void OnHostPomodoroStart()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    ?.BroadcastPomodoroEvent("start");
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PomodoroService), "PlayOrPausePomodoroTimer")]
            static void OnHostPomodoroPauseToggle()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                // PlayOrPausePomodoroTimer toggles between Pause and Work/Rest
                // 不判断具体方向，统一广播 toggle 事件 + 下一秒的快照会校正状态
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    ?.BroadcastPomodoroEvent("pauseToggle");
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PomodoroService), "SkipTimer")]
            static void OnHostPomodoroSkip()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    ?.BroadcastPomodoroEvent("skip");
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PomodoroService), "ResetTimer")]
            static void OnHostPomodoroReset()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsHost) return;
                Integration.StudyRoom.StudyRoomService.Instance?.HostSync
                    ?.BroadcastPomodoroEvent("reset");
            }
        }

        // ═════════════════════════════════════════════
        //  客户端 Patches: 拦截本地保存 → 转发给主机
        // ═════════════════════════════════════════════

        private static class ClientPatches
        {
            // ─── 待办事项: 拦截保存 → 转发 ───

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveTodoList")]
            static bool OnClientTodoSave(object todoList)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo("[StudyRoomPatch] Client: TodoList save intercepted → forwarding");
                Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                    SaveDataOpType.TodoItemUpdate, todoList);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveCalenderData", new Type[] { typeof(CalenderMonthlyData) })]
            static bool OnClientCalendarSave(object data)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo("[StudyRoomPatch] Client: Calendar save intercepted → forwarding");
                Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                    SaveDataOpType.CalendarDiaryEdit, data);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveNoteList")]
            static bool OnClientNoteListSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo("[StudyRoomPatch] Client: NoteList save intercepted → forwarding");
                var save = SaveDataManager.Instance;
                Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                    SaveDataOpType.NotePageReorder, save?.NoteData?.NoteList);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SavePageData")]
            static bool OnClientPageSave(object pageData)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo("[StudyRoomPatch] Client: PageData save intercepted → forwarding");
                Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                    SaveDataOpType.NotePageUpdate, pageData);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveHabitHeaders")]
            static bool OnClientHabitHeadersSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo("[StudyRoomPatch] Client: HabitHeaders save intercepted → forwarding");
                var save = SaveDataManager.Instance;
                Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                    SaveDataOpType.HabitUpdate,
                    new { headerData = save?.AllHabitHeaderData });
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveHabitsMonthlyData")]
            static bool OnClientHabitMonthlySave(int year, int month)
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo($"[StudyRoomPatch] Client: HabitMonthly save intercepted ({year}-{month})");
                var save = SaveDataManager.Instance;
                Bulbul.HabitAllMonthlyData monthlyData;
                if (save != null && save.TryGetOrLoadHabitsMonthlyData(year, month, out monthlyData))
                {
                    Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                        SaveDataOpType.HabitToggleDay,
                        new { year, month, monthlyData });
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveHabitDeadPeriods")]
            static bool OnClientHabitDeadPeriodsSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                _log?.LogInfo("[StudyRoomPatch] Client: HabitDeadPeriods save intercepted → forwarding");
                var save = SaveDataManager.Instance;
                Integration.StudyRoom.SaveDataSyncManager.ForwardSaveToHost(
                    SaveDataOpType.HabitPause,
                    new { deadPeriodData = save?.AllHabitDeadPeriodData });
                return false;
            }

            // ─── 装饰/环境写盘拦截 (客户端不直接写) ───

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveDecorationThrottled")]
            static bool OnClientDecorationSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveEnviromentThrottled")]
            static bool OnClientEnvironmentSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveEnviromentPresetThrottled")]
            static bool OnClientEnvironmentPresetSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveAutoTimeWindowChangeData")]
            static bool OnClientAutoTimeSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }

            // Fix #17: 客户端拦截 SaveEnvironmentProgressData (主机端已捕获但客户端缺失)
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SaveEnvironmentProgressData")]
            static bool OnClientEnvironmentProgressSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }

            // ─── 经济/经验拦截 (客户端不直接写) ───

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SavePlayerData")]
            static bool OnClientPlayerDataSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveDataManager), "SavePointPurchaseData")]
            static bool OnClientPointPurchaseDataSave()
            {
                if (!Integration.StudyRoom.StudyRoomService.IsClient) return true;
                if (Integration.StudyRoom.SaveDataSyncManager.IsSyncing) return true;
                return false;
            }
        }

    }
}
