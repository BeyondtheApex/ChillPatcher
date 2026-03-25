using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using BepInEx.Logging;
using Bulbul;
using Newtonsoft.Json.Linq;
using Steamworks;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// 客户端同步管理器
    /// 职责: 接收主机的同步消息 → 应用到本地状态 → 触发事件
    /// </summary>
    public class ClientSyncManager
    {
        private readonly ManualLogSource _log;

        /// <summary>主机 SteamID</summary>
        public CSteamID HostId { get; set; }

        /// <summary>是否已完成初始同步</summary>
        public bool SyncReady { get; private set; }

        /// <summary>断线状态</summary>
        public bool Disconnected { get; private set; }

        /// <summary>重连计时</summary>
        private float _reconnectTimer;
        private int _reconnectAttempt;

        /// <summary>SaveDataFull 分片缓冲</summary>
        private Dictionary<int, byte[]> _saveDataChunks;
        private int _saveDataTotalChunks;

        // ─── 事件 ───
        public event Action OnSyncReady;
        public event Action<string> OnKicked;
        public event Action OnRoomClosed;
        public event Action<CSteamID, string> OnPlayerJoined;
        public event Action<CSteamID, string> OnPlayerLeft;
        public event Action OnConnectionLost;
        public event Action<int> OnReconnecting;
        public event Action OnReconnected;
        public event Action<string, JObject> OnStoryReady;
        public event Action OnStoryStarted;
        public event Action<CSteamID, string> OnStorySkipped;
        public event Action<JObject> OnPomodoroSync;
        public event Action<string> OnJoinFailed;
        public event Action OnJoinAccepted;

        /// <summary>编辑状态事件 (key, info, isStart)</summary>
        public event Action<string, EditingInfo, bool> OnEditingChanged;

        /// <summary>客户端本地编辑状态跟踪</summary>
        private readonly Dictionary<string, EditingInfo> _editingState
            = new Dictionary<string, EditingInfo>();

        /// <summary>客户端追踪的房间成员列表 (由 FullSnapshot + PlayerJoined/Left 维护)</summary>
        private readonly List<Dictionary<string, object>> _trackedMembers
            = new List<Dictionary<string, object>>();

        /// <summary>最后收到的序列号 (用于重连时增量同步)</summary>
        private uint _lastSeqNumber;

        public ClientSyncManager(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>
        /// 处理收到的消息 (客户端消息路由)
        /// </summary>
        public void HandleMessage(CSteamID sender, SyncMessage msg)
        {
            switch (msg.Type)
            {
                // === 握手 ===
                case SyncMessageType.Challenge:
                    HandleChallenge(sender, msg.Payload);
                    break;
                case SyncMessageType.JoinAccepted:
                    HandleJoinAccepted(sender, msg.Payload);
                    break;
                case SyncMessageType.JoinRejected:
                    HandleJoinRejected(msg.Payload);
                    break;

                // === 状态同步 ===
                case SyncMessageType.FullSnapshot:
                    HandleFullSnapshot(msg.Payload);
                    break;
                case SyncMessageType.StateChanged:
                    HandleStateChanged(msg.Payload);
                    break;
                case SyncMessageType.ScenarioPlay:
                    HandleScenarioPlay(msg.Payload);
                    break;
                case SyncMessageType.VoicePlay:
                    HandleVoicePlay(msg.Payload);
                    break;
                case SyncMessageType.VoiceCancel:
                    HandleVoiceCancel();
                    break;
                case SyncMessageType.SubtitleShow:
                    HandleSubtitleShow(msg.Payload);
                    break;
                case SyncMessageType.SubtitleHide:
                    HandleSubtitleHide();
                    break;

                // === 番茄钟 ===
                case SyncMessageType.PomodoroSnapshot:
                    ApplyPomodoroFromSnapshot(msg.Payload);
                    OnPomodoroSync?.Invoke(msg.Payload);
                    break;
                case SyncMessageType.PomodoroEvent:
                    HandlePomodoroEvent(msg.Payload);
                    break;

                // === 互动 ===
                case SyncMessageType.InteractionGrant:
                case SyncMessageType.InteractionDeny:
                case SyncMessageType.InteractionEnd:
                    // 转发给 StudyRoomService 的事件系统
                    break;

                // === 故事 ===
                case SyncMessageType.StoryReady:
                    OnStoryReady?.Invoke(
                        msg.Payload.Value<string>("scenarioType"),
                        msg.Payload);
                    break;
                case SyncMessageType.StoryStart:
                    OnStoryStarted?.Invoke();
                    break;
                case SyncMessageType.StorySkip:
                    HandleStorySkip(msg.Payload);
                    break;

                // === 存档 ===
                case SyncMessageType.SaveDataChanged:
                    HandleSaveDataChanged(msg.Payload);
                    break;
                case SyncMessageType.SaveDataFull:
                    HandleSaveDataFullChunk(msg.Payload);
                    break;
                case SyncMessageType.EditingStart:
                    HandleEditingStart(msg.Payload);
                    break;
                case SyncMessageType.EditingEnd:
                    HandleEditingEnd(msg.Payload);
                    break;

                // === 环境/装饰/模式 ===
                case SyncMessageType.EnvironmentViewChange:
                    HandleEnvironmentViewChange(msg.Payload);
                    break;
                case SyncMessageType.EnvironmentSoundChange:
                    HandleEnvironmentSoundChange(msg.Payload);
                    break;
                case SyncMessageType.EnvironmentPresetLoad:
                    HandleEnvironmentPresetLoad(msg.Payload);
                    break;
                case SyncMessageType.EnvironmentAutoTime:
                    HandleEnvironmentAutoTime(msg.Payload);
                    break;
                case SyncMessageType.DecorationChange:
                    HandleDecorationChange(msg.Payload);
                    break;
                case SyncMessageType.DecorationPresetLoad:
                    HandleDecorationPresetLoad(msg.Payload);
                    break;
                case SyncMessageType.ModeChange:
                    HandleModeChange(msg.Payload);
                    break;
                case SyncMessageType.PointPurchaseSync:
                    HandlePointPurchaseSync(msg.Payload);
                    break;

                // === 进度同步 ===
                case SyncMessageType.ExpChanged:
                case SyncMessageType.LevelChanged:
                case SyncMessageType.WorkSecondsSync:
                    HandleProgressSync(msg.Type, msg.Payload);
                    break;

                // === 控制 ===
                case SyncMessageType.Heartbeat:
                    HandleHeartbeat(sender);
                    break;
                case SyncMessageType.Kick:
                    OnKicked?.Invoke(msg.Payload.Value<string>("reason") ?? "");
                    break;
                case SyncMessageType.RoomClosed:
                    OnRoomClosed?.Invoke();
                    break;
                case SyncMessageType.PlayerJoined:
                    HandlePlayerJoinedBroadcast(msg.Payload);
                    break;
                case SyncMessageType.PlayerLeft:
                    HandlePlayerLeftBroadcast(msg.Payload);
                    break;

                // === SeqWrapper ===
                case SyncMessageType.SeqWrapper:
                    if (SyncProtocol.TryUnwrapSequence(msg.Payload, out var seq, out var inner))
                    {
                        _lastSeqNumber = seq;
                        HandleMessage(sender, inner); // 递归处理内部消息
                    }
                    break;

                default:
                    _log?.LogWarning($"[ClientSync] Unhandled: {msg.Type}");
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  握手处理
        // ─────────────────────────────────────────────

        /// <summary>保存密码用于挑战应答</summary>
        public string JoinPassword { get; set; }

        private void HandleChallenge(CSteamID sender, JObject payload)
        {
            var challengeBase64 = payload.Value<string>("challenge") ?? "";
            var challengeBytes = Convert.FromBase64String(challengeBase64);

            using (var hmac = new HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(JoinPassword ?? "")))
            {
                var response = hmac.ComputeHash(challengeBytes);
                var msg = SyncProtocol.Create(SyncMessageType.ChallengeResponse,
                    new Dictionary<string, object>
                    {
                        ["response"] = Convert.ToBase64String(response)
                    });
                P2PTransport.SendMessage(sender, msg);
            }
        }

        private void HandleJoinAccepted(CSteamID sender, JObject payload)
        {
            HostId = sender;
            P2PTransport.AddPeer(sender);

            // 初始化成员列表 (主机 + 自己)
            _trackedMembers.Clear();
            _trackedMembers.Add(new Dictionary<string, object>
            {
                ["steamId"] = sender.m_SteamID.ToString(),
                ["personaName"] = SteamFriends.GetFriendPersonaName(sender),
                ["isHost"] = true,
                ["syncState"] = "ready"
            });
            _trackedMembers.Add(new Dictionary<string, object>
            {
                ["steamId"] = SteamUser.GetSteamID().m_SteamID.ToString(),
                ["personaName"] = SteamFriends.GetPersonaName(),
                ["isHost"] = false,
                ["syncState"] = "syncing"
            });

            _log?.LogInfo($"[ClientSync] Join accepted by {sender}");
            OnJoinAccepted?.Invoke();
        }

        private void HandleJoinRejected(JObject payload)
        {
            var reason = payload.Value<string>("reason") ?? "unknown";
            _log?.LogInfo($"[ClientSync] Join rejected: {reason}");
            OnJoinFailed?.Invoke(reason);
        }

        // ─────────────────────────────────────────────
        //  状态同步处理
        // ─────────────────────────────────────────────

        private void HandleFullSnapshot(JObject payload)
        {
            _log?.LogInfo("[ClientSync] Received FullSnapshot");

            // 禁用 AI 并应用角色状态
            var charSvc = CharacterApiService.Instance;
            if (charSvc != null)
            {
                charSvc.setAIEnabled(false);
                if (payload.ContainsKey("currentState"))
                {
                    var stateId = payload.Value<string>("currentState") ?? "";
                    charSvc.setState(stateId);
                }
            }

            // 应用装饰数据
            try
            {
                if (payload.ContainsKey("decorationData"))
                {
                    var decoToken = payload["decorationData"];
                    ApplyDecorationFromSnapshot(decoToken);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Decoration apply error: {ex.Message}"); }

            // 应用环境数据
            try
            {
                if (payload.ContainsKey("enviromentData"))
                {
                    ApplyEnvironmentFromSnapshot(payload["enviromentData"]);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Environment apply error: {ex.Message}"); }

            // 应用自动昼夜数据
            try
            {
                if (payload.ContainsKey("autoTimeData"))
                {
                    ApplyAutoTimeFromSnapshot(payload["autoTimeData"]);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] AutoTime apply error: {ex.Message}"); }

            // 应用模式数据
            try
            {
                if (payload.ContainsKey("collaborationData"))
                {
                    ApplyModeFromSnapshot(payload["collaborationData"]);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Mode apply error: {ex.Message}"); }

            // 应用故事进度 (ScenarioProgressData)
            try
            {
                if (payload.ContainsKey("progressData"))
                {
                    var save = SaveDataManager.Instance;
                    if (save?.ScenarioProgressData != null)
                        Newtonsoft.Json.JsonConvert.PopulateObject(
                            payload["progressData"].ToString(), save.ScenarioProgressData);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] ProgressData apply error: {ex.Message}"); }

            // 应用经济数据 (PointPurchaseData)
            try
            {
                if (payload.ContainsKey("pointPurchaseData"))
                {
                    var save = SaveDataManager.Instance;
                    if (save != null)
                        Newtonsoft.Json.JsonConvert.PopulateObject(
                            payload["pointPurchaseData"].ToString(), save.PointPurchaseData);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] PointPurchase apply error: {ex.Message}"); }

            // 应用经验/等级数据
            try
            {
                if (payload.ContainsKey("playerData"))
                {
                    var save = SaveDataManager.Instance;
                    if (save?.PlayerData != null)
                    {
                        var pd = payload["playerData"];
                        save.PlayerData.CurrentWorkSeconds = pd.Value<double>("currentWorkSeconds");
                        save.PlayerData.PomodoroTotalWorkSeconds = pd.Value<double>("totalWorkSeconds");
                    }
                }
                if (payload.ContainsKey("levelData"))
                {
                    var levelData = LevelData.GetCurrentLevelData();
                    if (levelData != null)
                    {
                        var ld = payload["levelData"];
                        levelData.SetLevel(ld.Value<int>("level"));
                    }
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Level/Exp apply error: {ex.Message}"); }

            // 应用番茄钟状态
            try
            {
                if (payload.ContainsKey("pomodoro"))
                {
                    ApplyPomodoroFromSnapshot(payload["pomodoro"] as JObject);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Pomodoro apply error: {ex.Message}"); }

            // 解析成员列表 (覆盖初始的简洁列表)
            try
            {
                if (payload.ContainsKey("members"))
                {
                    _trackedMembers.Clear();
                    var membersArr = payload["members"] as JArray;
                    if (membersArr != null)
                    {
                        foreach (var m in membersArr)
                        {
                            _trackedMembers.Add(new Dictionary<string, object>
                            {
                                ["steamId"] = m.Value<string>("steamId") ?? "",
                                ["personaName"] = m.Value<string>("personaName") ?? "",
                                ["isHost"] = m.Value<bool>("isHost"),
                                ["syncState"] = "ready"
                            });
                        }
                    }
                    // 更新自己的同步状态
                    var myId = SteamUser.GetSteamID().m_SteamID.ToString();
                    var myEntry = _trackedMembers.Find(m => m["steamId"].ToString() == myId);
                    if (myEntry != null) myEntry["syncState"] = "ready";
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Members parse error: {ex.Message}"); }

            SyncReady = true;
            Disconnected = false;
            OnSyncReady?.Invoke();
        }

        private void HandleStateChanged(JObject payload)
        {
            var stateId = payload.Value<string>("state") ?? "";
            CharacterApiService.Instance?.setState(stateId);
        }

        private void HandleScenarioPlay(JObject payload)
        {
            var scenarioType = payload.Value<string>("scenarioType") ?? "";
            var episode = payload.Value<int>("episode");
            _log?.LogInfo($"[ClientSync] ScenarioPlay: {scenarioType} ep={episode}");

            try
            {
                var voiceSvc = VoiceApiService.Instance;
                if (voiceSvc == null) return;
                bool played = voiceSvc.playScenarioVoice(scenarioType, episode);
                if (!played)
                {
                    _log?.LogWarning($"[ClientSync] Failed to play scenario: {scenarioType} ep={episode}, notifying host");
                    var skipMsg = SyncProtocol.Create(SyncMessageType.StorySkip,
                        new Dictionary<string, object>
                        {
                            ["steamId"] = SteamUser.GetSteamID().m_SteamID.ToString()
                        });
                    P2PTransport.SendMessage(HostId, skipMsg);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] ScenarioPlay error: {ex.Message}"); }
        }

        private void HandlePomodoroEvent(JObject payload)
        {
            var eventType = payload.Value<string>("event") ?? "";
            _log?.LogInfo($"[ClientSync] PomodoroEvent: {eventType}");
            // 番茄钟事件用于 UI 通知，实际状态由 PomodoroSnapshot 驱动
            OnPomodoroSync?.Invoke(payload);
        }

        private void HandleVoicePlay(JObject payload)
        {
            var voice = payload.Value<string>("voice") ?? "";
            var moveMouth = payload.Value<bool>("moveMouth");
            _log?.LogInfo($"[ClientSync] VoicePlay: {voice}");

            try
            {
                VoiceApiService.Instance?.playVoice(voice, moveMouth);
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] VoicePlay error: {ex.Message}"); }
        }

        private void HandleVoiceCancel()
        {
            try
            {
                VoiceApiService.Instance?.cancelVoice();
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] VoiceCancel error: {ex.Message}"); }
        }

        private void HandleSubtitleShow(JObject payload)
        {
            var text = payload.Value<string>("text") ?? "";
            var duration = payload.Value<float>("duration");

            try
            {
                SubtitleApiService.Instance?.show(text, duration);
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] SubtitleShow error: {ex.Message}"); }
        }

        private void HandleSubtitleHide()
        {
            try
            {
                SubtitleApiService.Instance?.hide();
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] SubtitleHide error: {ex.Message}"); }
        }

        private void HandleStorySkip(JObject payload)
        {
            var steamId = new CSteamID(ulong.Parse(payload.Value<string>("steamId") ?? "0"));
            var name = payload.Value<string>("personaName") ?? "";
            OnStorySkipped?.Invoke(steamId, name);
        }

        // ─── 编辑状态 ───

        private void HandleEditingStart(JObject payload)
        {
            var dataType = payload.Value<string>("dataType") ?? "";
            var itemId = payload.Value<string>("itemId") ?? "";
            var steamIdStr = payload.Value<string>("steamId") ?? "0";
            var personaName = payload.Value<string>("personaName") ?? "";
            var key = $"{dataType}:{itemId}";
            var info = new EditingInfo
            {
                SteamId = new CSteamID(ulong.Parse(steamIdStr)),
                PersonaName = personaName,
                DataType = dataType,
                ItemId = itemId
            };
            _editingState[key] = info;
            OnEditingChanged?.Invoke(key, info, true);
        }

        private void HandleEditingEnd(JObject payload)
        {
            var dataType = payload.Value<string>("dataType") ?? "";
            var itemId = payload.Value<string>("itemId") ?? "";
            var key = $"{dataType}:{itemId}";
            if (_editingState.TryGetValue(key, out var info))
            {
                _editingState.Remove(key);
                OnEditingChanged?.Invoke(key, info, false);
            }
        }

        /// <summary>获取所有正在编辑的状态 (客户端本地追踪)</summary>
        public List<EditingInfo> GetAllEditingStatus()
        {
            return new List<EditingInfo>(_editingState.Values);
        }

        /// <summary>获取客户端追踪的成员列表</summary>
        public List<Dictionary<string, object>> GetTrackedMembers()
        {
            return new List<Dictionary<string, object>>(_trackedMembers);
        }

        private void HandleSaveDataChanged(JObject payload)
        {
            var opType = (SaveDataOpType)payload.Value<byte>("opType");
            var data = payload["data"] as JObject ?? new JObject();
            _log?.LogInfo($"[ClientSync] SaveDataChanged: {opType}");

            // 应用远程变更到本地 SaveDataManager 内存  
            // IsSyncing 标志确保客户端 Prefix 允许保存通过
            SaveDataSyncManager.ApplyRemoteChangeToMemory(opType, data, _log);
        }

        private void HandleSaveDataFullChunk(JObject payload)
        {
            var chunkIndex = payload.Value<int>("chunkIndex");
            var totalChunks = payload.Value<int>("totalChunks");
            var dataBase64 = payload.Value<string>("data") ?? "";

            if (_saveDataChunks == null || _saveDataTotalChunks != totalChunks)
            {
                _saveDataChunks = new Dictionary<int, byte[]>();
                _saveDataTotalChunks = totalChunks;
            }

            _saveDataChunks[chunkIndex] = Convert.FromBase64String(dataBase64);
            _log?.LogInfo($"[ClientSync] SaveDataFull chunk {chunkIndex + 1}/{totalChunks}");

            // 所有分片到齐 → 组装并解压
            if (_saveDataChunks.Count >= totalChunks)
            {
                try
                {
                    ApplySaveDataFull();
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ClientSync] SaveDataFull apply error: {ex.Message}");
                }
                finally
                {
                    _saveDataChunks = null;
                }
            }
        }

        private void ApplySaveDataFull()
        {
            // 拼接所有分片
            int totalLen = 0;
            for (int i = 0; i < _saveDataTotalChunks; i++)
                totalLen += _saveDataChunks[i].Length;

            var fullData = new byte[totalLen];
            int offset = 0;
            for (int i = 0; i < _saveDataTotalChunks; i++)
            {
                var chunk = _saveDataChunks[i];
                Array.Copy(chunk, 0, fullData, offset, chunk.Length);
                offset += chunk.Length;
            }

            // 解压到客户端子存档目录
            var profileSvc = new SaveProfileService(null);
            var saveDir = profileSvc.GetActiveProfileAbsolutePath();
            if (string.IsNullOrEmpty(saveDir))
            {
                _log?.LogWarning("[ClientSync] No active save directory for SaveDataFull");
                return;
            }

            using (var ms = new MemoryStream(fullData))
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    var destPath = Path.Combine(saveDir, entry.Name);
                    using (var entryStream = entry.Open())
                    using (var fileStream = File.Create(destPath))
                    {
                        entryStream.CopyTo(fileStream);
                    }
                }
            }

            _log?.LogInfo($"[ClientSync] SaveDataFull applied: {fullData.Length} bytes to {saveDir}");
        }

        private void HandleHeartbeat(CSteamID sender)
        {
            P2PTransport.UpdatePeerHeartbeat(sender);
            if (Disconnected)
            {
                Disconnected = false;
                _reconnectAttempt = 0;
                OnReconnected?.Invoke();
            }
        }

        private void HandlePlayerJoinedBroadcast(JObject payload)
        {
            var steamId = new CSteamID(ulong.Parse(payload.Value<string>("steamId") ?? "0"));
            var name = payload.Value<string>("personaName") ?? "";
            _trackedMembers.Add(new Dictionary<string, object>
            {
                ["steamId"] = steamId.m_SteamID.ToString(),
                ["personaName"] = name,
                ["isHost"] = false,
                ["syncState"] = "syncing"
            });
            OnPlayerJoined?.Invoke(steamId, name);
        }

        private void HandlePlayerLeftBroadcast(JObject payload)
        {
            var steamId = new CSteamID(ulong.Parse(payload.Value<string>("steamId") ?? "0"));
            var name = payload.Value<string>("personaName") ?? "";
            _trackedMembers.RemoveAll(m => m["steamId"].ToString() == steamId.m_SteamID.ToString());
            OnPlayerLeft?.Invoke(steamId, name);
        }

        // ─────────────────────────────────────────────
        //  环境 / 装饰 / 模式 处理
        // ─────────────────────────────────────────────

        private void HandleEnvironmentViewChange(JObject payload)
        {
            _log?.LogInfo("[ClientSync] EnvironmentViewChange");
            try { ApplyEnvironmentFromSnapshot(payload["enviromentData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] EnvView error: {ex.Message}"); }
        }

        private void HandleEnvironmentSoundChange(JObject payload)
        {
            _log?.LogInfo("[ClientSync] EnvironmentSoundChange");
            try { ApplyEnvironmentFromSnapshot(payload["enviromentData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] EnvSound error: {ex.Message}"); }
        }

        private void HandleEnvironmentPresetLoad(JObject payload)
        {
            _log?.LogInfo("[ClientSync] EnvironmentPresetLoad");
            try { ApplyEnvironmentFromSnapshot(payload["enviromentData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] EnvPreset error: {ex.Message}"); }
        }

        private void HandleEnvironmentAutoTime(JObject payload)
        {
            _log?.LogInfo("[ClientSync] EnvironmentAutoTime");
            try { ApplyAutoTimeFromSnapshot(payload["autoTimeData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] AutoTime error: {ex.Message}"); }
        }

        private void HandleDecorationChange(JObject payload)
        {
            _log?.LogInfo("[ClientSync] DecorationChange");
            try { ApplyDecorationFromSnapshot(payload["decorationData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Deco error: {ex.Message}"); }
        }

        private void HandleDecorationPresetLoad(JObject payload)
        {
            _log?.LogInfo("[ClientSync] DecorationPresetLoad");
            try { ApplyDecorationFromSnapshot(payload["decorationData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] DecoPreset error: {ex.Message}"); }
        }

        private void HandleModeChange(JObject payload)
        {
            _log?.LogInfo("[ClientSync] ModeChange");
            try { ApplyModeFromSnapshot(payload["collaborationData"]); }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] Mode error: {ex.Message}"); }
        }

        private void HandlePointPurchaseSync(JObject payload)
        {
            _log?.LogInfo("[ClientSync] PointPurchaseSync");
            try
            {
                var save = SaveDataManager.Instance;
                if (save != null && payload.ContainsKey("pointPurchaseData"))
                {
                    Newtonsoft.Json.JsonConvert.PopulateObject(
                        payload["pointPurchaseData"].ToString(), save.PointPurchaseData);
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] PointPurchase error: {ex.Message}"); }
        }

        private void HandleProgressSync(SyncMessageType type, JObject payload)
        {
            _log?.LogInfo($"[ClientSync] ProgressSync: {type}");
            try
            {
                var save = SaveDataManager.Instance;
                if (save == null) return;

                switch (type)
                {
                    case SyncMessageType.ExpChanged:
                        var levelData = LevelData.GetCurrentLevelData();
                        if (levelData != null && payload.ContainsKey("exp"))
                            levelData._currentExp = payload.Value<float>("exp");
                        break;
                    case SyncMessageType.LevelChanged:
                        var ld = LevelData.GetCurrentLevelData();
                        if (ld != null && payload.ContainsKey("level"))
                            ld.SetLevel(payload.Value<int>("level"));
                        break;
                    case SyncMessageType.WorkSecondsSync:
                        if (save.PlayerData != null)
                        {
                            if (payload.ContainsKey("current"))
                                save.PlayerData.CurrentWorkSeconds = payload.Value<double>("current");
                            if (payload.ContainsKey("total"))
                                save.PlayerData.PomodoroTotalWorkSeconds = payload.Value<double>("total");
                        }
                        break;
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[ClientSync] ProgressSync error: {ex.Message}"); }
        }

        // ─────────────────────────────────────────────
        //  断线检测 & 重连
        // ─────────────────────────────────────────────

        /// <summary>
        /// 检查是否断线 (在 Tick 中调用)
        /// </summary>
        public void CheckConnection()
        {
            if (!SyncReady || Disconnected) return;

            var timedOut = P2PTransport.GetTimedOutPeers();
            if (timedOut.Contains(HostId))
            {
                Disconnected = true;
                _reconnectTimer = 0;
                _reconnectAttempt = 0;
                _log?.LogWarning("[ClientSync] Connection lost to host");
                OnConnectionLost?.Invoke();
            }
        }

        /// <summary>
        /// 重连逻辑 (在 Tick 中调用)
        /// </summary>
        public void TickReconnect(float deltaTime)
        {
            if (!Disconnected) return;

            _reconnectTimer += deltaTime;
            if (_reconnectTimer >= StudyRoomConfig.DisconnectTimeoutSeconds)
            {
                _reconnectTimer = 0;
                _reconnectAttempt++;

                if (_reconnectAttempt * StudyRoomConfig.DisconnectTimeoutSeconds
                    >= StudyRoomConfig.ReconnectTimeoutSeconds)
                {
                    // 超时，放弃重连
                    _log?.LogWarning("[ClientSync] Reconnect timeout, giving up");
                    OnRoomClosed?.Invoke();
                    return;
                }

                OnReconnecting?.Invoke(_reconnectAttempt);
                var msg = SyncProtocol.Create(SyncMessageType.ReconnectRequest,
                    new Dictionary<string, object>
                    {
                        ["attempt"] = _reconnectAttempt,
                        ["lastSeqNumber"] = _lastSeqNumber
                    });
                P2PTransport.SendMessage(HostId, msg);
            }
        }

        /// <summary>
        /// 发送 SyncReady 给主机
        /// </summary>
        public void SendSyncReady()
        {
            var msg = SyncProtocol.Create(SyncMessageType.SyncReady);
            P2PTransport.SendMessage(HostId, msg);
        }

        /// <summary>
        /// 发送心跳给主机
        /// </summary>
        public void SendHeartbeat()
        {
            var msg = SyncProtocol.Create(SyncMessageType.Heartbeat,
                new Dictionary<string, object>
                {
                    ["timestampMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            P2PTransport.SendMessage(HostId, msg);
        }

        /// <summary>
        /// 通知主机: 本机离开
        /// </summary>
        public void SendPlayerLeft()
        {
            var msg = SyncProtocol.Create(SyncMessageType.PlayerLeft);
            P2PTransport.SendMessage(HostId, msg);
        }

        public void Reset()
        {
            HostId = CSteamID.Nil;
            SyncReady = false;
            Disconnected = false;
            _reconnectTimer = 0;
            _reconnectAttempt = 0;
            _saveDataChunks = null;
            _editingState.Clear();
            _trackedMembers.Clear();
            _lastSeqNumber = 0;
        }

        // ─────────────────────────────────────────────
        //  快照应用辅助方法
        // ─────────────────────────────────────────────

        private void ApplyDecorationFromSnapshot(JToken decoToken)
        {
            if (decoToken == null) return;
            var save = SaveDataManager.Instance;
            if (save == null) return;

            // 将主机装饰数据覆写到本地 SaveData 内存
            Newtonsoft.Json.JsonConvert.PopulateObject(
                decoToken.ToString(), save.DecorationSaveData);

            // 通过 Integration 服务应用到视觉
            DecorationApiService.Instance?.reloadFromSave();
        }

        private void ApplyEnvironmentFromSnapshot(JToken envToken)
        {
            if (envToken == null) return;
            var save = SaveDataManager.Instance;
            if (save == null) return;

            Newtonsoft.Json.JsonConvert.PopulateObject(
                envToken.ToString(), save.EnviromentData);

            // 通过 Integration 服务刷新视觉
            EnvironmentApiService.Instance?.reloadFromSave();
        }

        private void ApplyAutoTimeFromSnapshot(JToken autoTimeToken)
        {
            if (autoTimeToken == null) return;
            var save = SaveDataManager.Instance;
            if (save == null) return;

            Newtonsoft.Json.JsonConvert.PopulateObject(
                autoTimeToken.ToString(), save.AutoTimeWindowChangeData);
        }

        private void ApplyModeFromSnapshot(JToken modeToken)
        {
            if (modeToken == null) return;
            var save = SaveDataManager.Instance;
            if (save == null) return;

            Newtonsoft.Json.JsonConvert.PopulateObject(
                modeToken.ToString(), save.CollaborationSaveData);
        }

        /// <summary>
        /// 应用番茄钟快照到本地 (客户端不运行本地计时器，完全由快照驱动)
        /// 更新 PomodoroData + PlayerData + 触发 UI 刷新
        /// 包含网络延迟补偿: 若番茄钟正在运行，将已过秒数补偿单程延迟估算
        /// </summary>
        private void ApplyPomodoroFromSnapshot(JObject payload)
        {
            if (payload == null) return;
            try
            {
                var save = SaveDataManager.Instance;
                if (save == null) return;

                // 计算网络延迟补偿 (单程延迟 ≈ RTT/2)
                double delayCompensationSec = 0;
                bool isRunning = payload.Value<bool>("isRunning");
                if (isRunning && payload.ContainsKey("serverTimestampMs"))
                {
                    long serverMs = payload.Value<long>("serverTimestampMs");
                    long localMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long rttMs = localMs - serverMs;
                    if (rttMs > 0 && rttMs < 5000) // 合理范围内才补偿 (防止时钟偏差)
                        delayCompensationSec = rttMs / 2000.0;
                }

                // 更新工作时间统计 (加上延迟补偿)
                if (payload.ContainsKey("currentWorkSeconds") && save.PlayerData != null)
                    save.PlayerData.CurrentWorkSeconds = payload.Value<double>("currentWorkSeconds") + delayCompensationSec;
                if (payload.ContainsKey("totalWorkSeconds") && save.PlayerData != null)
                    save.PlayerData.PomodoroTotalWorkSeconds = payload.Value<double>("totalWorkSeconds") + delayCompensationSec;

                // 更新番茄钟配置参数 (使游戏原生 UI 显示正确值)
                if (save.PomodoroData != null)
                {
                    if (payload.ContainsKey("workMinutes"))
                        save.PomodoroData.WorkMinutes.Value = payload.Value<int>("workMinutes");
                    if (payload.ContainsKey("breakMinutes"))
                        save.PomodoroData.BreakMinutes.Value = payload.Value<int>("breakMinutes");
                    if (payload.ContainsKey("loopTotal"))
                        save.PomodoroData.LoopCount.Value = payload.Value<int>("loopTotal");
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[ClientSync] ApplyPomodoro error: {ex.Message}");
            }
        }
    }
}
