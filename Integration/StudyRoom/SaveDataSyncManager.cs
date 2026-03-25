using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bulbul;
using Newtonsoft.Json.Linq;
using Steamworks;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// 存档数据协同编辑管理器
    /// 主机端: 接收客户端操作请求 → 应用到本地 → 广播给所有人
    /// 客户端: 拦截本地保存 → 转发给主机
    /// </summary>
    public class SaveDataSyncManager
    {
        private readonly ManualLogSource _log;
        private uint _version;

        /// <summary>
        /// 正在编辑的状态追踪: (dataType+itemId) → (steamId, personaName)
        /// </summary>
        private readonly Dictionary<string, EditingInfo> _editingState
            = new Dictionary<string, EditingInfo>();

        /// <summary>编辑状态变更事件</summary>
        public event Action<string, EditingInfo, bool> OnEditingChanged; // key, info, isStart

        /// <summary>当正在应用远程同步数据时为 true，用于绕过 Harmony Patches 的拦截/广播</summary>
        public static bool IsSyncing { get; private set; }

        public SaveDataSyncManager(ManualLogSource log)
        {
            _log = log;
        }

        // ─────────────────────────────────────────────
        //  主机端: 处理来自客户端的存档操作请求
        // ─────────────────────────────────────────────

        /// <summary>
        /// 主机端: 处理 SaveDataOp 消息
        /// </summary>
        public void HandleSaveDataOp(CSteamID sender, JObject payload)
        {
            var opType = (SaveDataOpType)payload.Value<byte>("opType");
            var data = payload["data"] as JObject ?? new JObject();

            _log?.LogInfo($"[SaveDataSync] Host received op {opType} from {sender}");

            // 应用到本地 SaveDataManager (通过反射或直接调用)
            bool success = ApplyOperation(opType, data);

            if (success)
            {
                // 广播变更给所有客户端
                _version++;
                var changed = SyncProtocol.Create(SyncMessageType.SaveDataChanged,
                    new Dictionary<string, object>
                    {
                        ["opType"] = (byte)opType,
                        ["data"] = data,
                        ["version"] = _version
                    });
                P2PTransport.BroadcastMessage(changed);
            }
        }

        /// <summary>
        /// 应用存档操作到本地 SaveDataManager
        /// </summary>
        private bool ApplyOperation(SaveDataOpType opType, JObject data)
        {
            try
            {
                switch (opType)
                {
                    // === 待办事项 ===
                    case SaveDataOpType.TodoListCreate:
                    case SaveDataOpType.TodoListDelete:
                    case SaveDataOpType.TodoListRename:
                    case SaveDataOpType.TodoItemAdd:
                    case SaveDataOpType.TodoItemDelete:
                    case SaveDataOpType.TodoItemUpdate:
                    case SaveDataOpType.TodoItemReorder:
                        return ApplyTodoOperation(opType, data);

                    // === 日历/日记 ===
                    case SaveDataOpType.CalendarDiaryEdit:
                    case SaveDataOpType.CalendarWorkTime:
                        return ApplyCalendarOperation(opType, data);

                    // === 备忘录 ===
                    case SaveDataOpType.NotePageCreate:
                    case SaveDataOpType.NotePageDelete:
                    case SaveDataOpType.NotePageUpdate:
                    case SaveDataOpType.NotePageRename:
                    case SaveDataOpType.NotePageReorder:
                        return ApplyNoteOperation(opType, data);

                    // === 习惯追踪 ===
                    case SaveDataOpType.HabitCreate:
                    case SaveDataOpType.HabitDelete:
                    case SaveDataOpType.HabitUpdate:
                    case SaveDataOpType.HabitToggleDay:
                    case SaveDataOpType.HabitHideOnCal:
                    case SaveDataOpType.HabitPause:
                    case SaveDataOpType.HabitResume:
                        return ApplyHabitOperation(opType, data);

                    default:
                        _log?.LogWarning($"[SaveDataSync] Unknown op type: {opType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[SaveDataSync] ApplyOperation error ({opType}): {ex.Message}");
                return false;
            }
        }

        // ─────────────────────────────────────────────
        //  各数据系统的操作适配 (应用到本地 SaveDataManager 内存)
        // ─────────────────────────────────────────────

        private bool ApplyTodoOperation(SaveDataOpType opType, JObject data)
        {
            _log?.LogInfo($"[SaveDataSync] Applying todo op: {opType}");
            var save = SaveDataManager.Instance;
            if (save == null) return false;

            try
            {
                var todoData = data.ToObject<TodoListData>();
                if (todoData != null)
                {
                    save.TodoAllData.TodoListDic[todoData.UniqueID] = todoData;
                    IsSyncing = true;
                    try { save.SaveTodoList(todoData); }
                    finally { IsSyncing = false; }
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[SaveDataSync] Todo apply error: {ex.Message}");
            }
            return true;
        }

        private bool ApplyCalendarOperation(SaveDataOpType opType, JObject data)
        {
            _log?.LogInfo($"[SaveDataSync] Applying calendar op: {opType}");
            var save = SaveDataManager.Instance;
            if (save == null) return false;

            try
            {
                var calData = data.ToObject<CalenderMonthlyData>();
                if (calData != null)
                {
                    IsSyncing = true;
                    try { save.SaveCalenderData(calData); }
                    finally { IsSyncing = false; }
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[SaveDataSync] Calendar apply error: {ex.Message}");
            }
            return true;
        }

        private bool ApplyNoteOperation(SaveDataOpType opType, JObject data)
        {
            _log?.LogInfo($"[SaveDataSync] Applying note op: {opType}");
            var save = SaveDataManager.Instance;
            if (save == null) return false;

            IsSyncing = true;
            try
            {
                if (opType == SaveDataOpType.NotePageUpdate && data.ContainsKey("UniqueID"))
                {
                    var pageData = data.ToObject<PageDataV2>();
                    if (pageData != null)
                        save.SavePageData(pageData);
                }
                else if (data.ContainsKey("Titles") || data.ContainsKey("PageOrderList"))
                {
                    Newtonsoft.Json.JsonConvert.PopulateObject(data.ToString(), save.NoteData.NoteList);
                    save.SaveNoteList();
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[SaveDataSync] Note apply error: {ex.Message}");
            }
            finally { IsSyncing = false; }
            return true;
        }

        private bool ApplyHabitOperation(SaveDataOpType opType, JObject data)
        {
            _log?.LogInfo($"[SaveDataSync] Applying habit op: {opType}");
            var save = SaveDataManager.Instance;
            if (save == null) return false;

            IsSyncing = true;
            try
            {
                switch (opType)
                {
                    case SaveDataOpType.HabitToggleDay:
                    case SaveDataOpType.HabitHideOnCal:
                        var year = data.Value<int>("year");
                        var month = data.Value<int>("month");
                        if (data.ContainsKey("monthlyData"))
                        {
                            var monthlyObj = data["monthlyData"]?.ToObject<Bulbul.HabitAllMonthlyData>();
                            if (monthlyObj != null)
                                save.AddHabitsMonthlyData(year, month, monthlyObj);
                        }
                        save.SaveHabitsMonthlyData(year, month);
                        break;
                    case SaveDataOpType.HabitPause:
                    case SaveDataOpType.HabitResume:
                        if (data.ContainsKey("deadPeriodData"))
                            Newtonsoft.Json.JsonConvert.PopulateObject(
                                data["deadPeriodData"].ToString(), save.AllHabitDeadPeriodData);
                        save.SaveHabitDeadPeriods();
                        break;
                    default:
                        if (data.ContainsKey("headerData"))
                            Newtonsoft.Json.JsonConvert.PopulateObject(
                                data["headerData"].ToString(), save.AllHabitHeaderData);
                        save.SaveHabitHeaders();
                        break;
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[SaveDataSync] Habit apply error: {ex.Message}");
            }
            finally { IsSyncing = false; }
            return true;
        }

        // ─────────────────────────────────────────────
        //  静态方法: 供 Harmony Patches 调用
        // ─────────────────────────────────────────────

        /// <summary>
        /// 主机端: 广播存档变更给所有客户端 (由 HostPatches Postfix 调用)
        /// </summary>
        public static void BroadcastSaveDataChanged(SaveDataOpType opType, object rawData)
        {
            var svc = StudyRoomService.Instance;
            if (svc == null || !StudyRoomService.IsHost) return;

            JObject data;
            if (rawData is JObject jo) data = jo;
            else if (rawData != null)
                data = JObject.FromObject(rawData);
            else
                data = new JObject();

            var msg = SyncProtocol.Create(SyncMessageType.SaveDataChanged,
                new Dictionary<string, object>
                {
                    ["opType"] = (byte)opType,
                    ["data"] = data
                });
            P2PTransport.BroadcastMessage(msg);
        }

        /// <summary>
        /// 客户端: 将本地保存操作转发给主机 (由 ClientPatches Prefix 调用)
        /// </summary>
        public static void ForwardSaveToHost(SaveDataOpType opType, object rawData)
        {
            var svc = StudyRoomService.Instance;
            if (svc == null || !StudyRoomService.IsClient) return;

            JObject data;
            if (rawData is JObject jo) data = jo;
            else if (rawData != null)
                data = JObject.FromObject(rawData);
            else
                data = new JObject();

            var hostId = svc.ClientSync.HostId;
            SendOperationToHost(hostId, opType, data);
        }

        // ─────────────────────────────────────────────
        //  客户端端: 将本地保存操作转发给主机
        // ─────────────────────────────────────────────

        /// <summary>
        /// 客户端端: 发送存档操作请求给主机
        /// </summary>
        public static void SendOperationToHost(CSteamID hostId, SaveDataOpType opType, JObject data)
        {
            var msg = SyncProtocol.Create(SyncMessageType.SaveDataOp,
                new Dictionary<string, object>
                {
                    ["opType"] = (byte)opType,
                    ["data"] = data
                });
            P2PTransport.SendMessage(hostId, msg);
        }

        // ─────────────────────────────────────────────
        //  编辑状态追踪 (UI 提示)
        // ─────────────────────────────────────────────

        /// <summary>
        /// 处理 EditingStart 消息
        /// </summary>
        public void HandleEditingStart(CSteamID sender, JObject payload)
        {
            var dataType = payload.Value<string>("dataType") ?? "";
            var itemId = payload.Value<string>("itemId") ?? "";
            var key = $"{dataType}:{itemId}";
            var personaName = payload.Value<string>("personaName") ?? "";

            var info = new EditingInfo
            {
                SteamId = sender,
                PersonaName = personaName,
                DataType = dataType,
                ItemId = itemId
            };

            _editingState[key] = info;
            OnEditingChanged?.Invoke(key, info, true);

            // 主机端: 广播给其他人
            if (StudyRoomService.IsHost)
            {
                var broadcast = SyncProtocol.Create(SyncMessageType.EditingStart,
                    new Dictionary<string, object>
                    {
                        ["dataType"] = dataType,
                        ["itemId"] = itemId,
                        ["steamId"] = sender.m_SteamID.ToString(),
                        ["personaName"] = personaName
                    });
                P2PTransport.BroadcastMessage(broadcast, except: sender);
            }
        }

        /// <summary>
        /// 处理 EditingEnd 消息
        /// </summary>
        public void HandleEditingEnd(CSteamID sender, JObject payload)
        {
            var dataType = payload.Value<string>("dataType") ?? "";
            var itemId = payload.Value<string>("itemId") ?? "";
            var key = $"{dataType}:{itemId}";

            if (_editingState.TryGetValue(key, out var info))
            {
                _editingState.Remove(key);
                OnEditingChanged?.Invoke(key, info, false);
            }

            if (StudyRoomService.IsHost)
            {
                var broadcast = SyncProtocol.Create(SyncMessageType.EditingEnd,
                    new Dictionary<string, object>
                    {
                        ["dataType"] = dataType,
                        ["itemId"] = itemId,
                        ["steamId"] = sender.m_SteamID.ToString()
                    });
                P2PTransport.BroadcastMessage(broadcast, except: sender);
            }
        }

        /// <summary>
        /// 玩家离开时清理其编辑状态
        /// </summary>
        public void ClearEditingForPlayer(CSteamID steamId)
        {
            var toRemove = new List<string>();
            foreach (var kv in _editingState)
            {
                if (kv.Value.SteamId == steamId) toRemove.Add(kv.Key);
            }
            foreach (var key in toRemove)
            {
                var info = _editingState[key];
                _editingState.Remove(key);
                OnEditingChanged?.Invoke(key, info, false);
            }
        }

        /// <summary>
        /// 获取所有正在编辑的状态
        /// </summary>
        public List<EditingInfo> GetAllEditingStatus()
        {
            return new List<EditingInfo>(_editingState.Values);
        }

        public void Reset()
        {
            _editingState.Clear();
            _version = 0;
        }

        /// <summary>
        /// 客户端: 应用远程存档变更到本地内存 (由 ClientSyncManager 调用)
        /// 设置 IsSyncing 标志以绕过客户端 Prefix 拦截
        /// </summary>
        public static void ApplyRemoteChangeToMemory(SaveDataOpType opType, JObject data, ManualLogSource log = null)
        {
            var save = SaveDataManager.Instance;
            if (save == null) return;

            IsSyncing = true;
            try
            {
                switch (opType)
                {
                    // === 待办事项 ===
                    case SaveDataOpType.TodoListCreate:
                    case SaveDataOpType.TodoListDelete:
                    case SaveDataOpType.TodoListRename:
                    case SaveDataOpType.TodoItemAdd:
                    case SaveDataOpType.TodoItemDelete:
                    case SaveDataOpType.TodoItemUpdate:
                    case SaveDataOpType.TodoItemReorder:
                        var todoData = data.ToObject<TodoListData>();
                        if (todoData != null)
                        {
                            save.TodoAllData.TodoListDic[todoData.UniqueID] = todoData;
                            save.SaveTodoList(todoData);
                        }
                        break;

                    // === 日历/日记 ===
                    case SaveDataOpType.CalendarDiaryEdit:
                    case SaveDataOpType.CalendarWorkTime:
                        var calData = data.ToObject<CalenderMonthlyData>();
                        if (calData != null)
                            save.SaveCalenderData(calData);
                        break;

                    // === 备忘录 ===
                    case SaveDataOpType.NotePageUpdate:
                        if (data.ContainsKey("UniqueID"))
                        {
                            var pageData = data.ToObject<PageDataV2>();
                            if (pageData != null)
                                save.SavePageData(pageData);
                        }
                        else if (data.ContainsKey("Titles") || data.ContainsKey("PageOrderList"))
                        {
                            Newtonsoft.Json.JsonConvert.PopulateObject(
                                data.ToString(), save.NoteData.NoteList);
                            save.SaveNoteList();
                        }
                        break;
                    case SaveDataOpType.NotePageCreate:
                    case SaveDataOpType.NotePageDelete:
                    case SaveDataOpType.NotePageRename:
                    case SaveDataOpType.NotePageReorder:
                        if (data.HasValues)
                        {
                            Newtonsoft.Json.JsonConvert.PopulateObject(
                                data.ToString(), save.NoteData.NoteList);
                            save.SaveNoteList();
                        }
                        break;

                    // === 习惯追踪 ===
                    case SaveDataOpType.HabitToggleDay:
                    case SaveDataOpType.HabitHideOnCal:
                        if (data.ContainsKey("year") && data.ContainsKey("month"))
                        {
                            var hYear = data.Value<int>("year");
                            var hMonth = data.Value<int>("month");
                            if (data.ContainsKey("monthlyData"))
                            {
                                var monthlyObj = data["monthlyData"]?.ToObject<Bulbul.HabitAllMonthlyData>();
                                if (monthlyObj != null)
                                    save.AddHabitsMonthlyData(hYear, hMonth, monthlyObj);
                            }
                            save.SaveHabitsMonthlyData(hYear, hMonth);
                        }
                        break;
                    case SaveDataOpType.HabitPause:
                    case SaveDataOpType.HabitResume:
                        if (data.ContainsKey("deadPeriodData"))
                            Newtonsoft.Json.JsonConvert.PopulateObject(
                                data["deadPeriodData"].ToString(), save.AllHabitDeadPeriodData);
                        save.SaveHabitDeadPeriods();
                        break;
                    case SaveDataOpType.HabitCreate:
                    case SaveDataOpType.HabitDelete:
                    case SaveDataOpType.HabitUpdate:
                        if (data.ContainsKey("headerData"))
                            Newtonsoft.Json.JsonConvert.PopulateObject(
                                data["headerData"].ToString(), save.AllHabitHeaderData);
                        save.SaveHabitHeaders();
                        break;
                }
            }
            catch (Exception ex)
            {
                log?.LogWarning($"[SaveDataSync] ApplyRemoteChange error ({opType}): {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }

    public class EditingInfo
    {
        public CSteamID SteamId;
        public string PersonaName;
        public string DataType;
        public string ItemId;
    }
}
