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
    /// 主机端同步管理器
    /// 职责: 事件捕获 → 状态快照 → 广播给所有客户端
    /// </summary>
    public class HostSyncManager
    {
        private readonly ManualLogSource _log;
        private readonly InteractionLock _interactionLock;
        private readonly SaveDataSyncManager _saveDataSync;

        /// <summary>暴露 SaveDataSync 供外部订阅编辑事件</summary>
        public SaveDataSyncManager SaveDataSync => _saveDataSync;

        // ─── 序列号 (Fix #4) ───
        private uint _nextSeq = 1;
        /// <summary>消息历史缓冲 (seq → raw message). 用于重连时重放</summary>
        private readonly Dictionary<uint, SyncMessage> _seqHistory
            = new Dictionary<uint, SyncMessage>();
        /// <summary>最大历史缓冲条数</summary>
        private const int MaxSeqHistory = 500;

        /// <summary>已连接的客户端信息</summary>
        private readonly Dictionary<CSteamID, ClientInfo> _clients
            = new Dictionary<CSteamID, ClientInfo>();

        /// <summary>密码挑战等待中的客户端</summary>
        private readonly Dictionary<CSteamID, byte[]> _pendingChallenges
            = new Dictionary<CSteamID, byte[]>();

        /// <summary>房间密码 (null=无密码)</summary>
        public string RoomPassword { get; set; }

        /// <summary>故事等待中的 Ack 集合</summary>
        private readonly HashSet<CSteamID> _storyAcks = new HashSet<CSteamID>();
        private float _storyAckTimer;
        private bool _waitingStoryAck;

        /// <summary>新玩家加入事件</summary>
        public event Action<CSteamID, string> OnPlayerJoined;

        /// <summary>玩家离开事件</summary>
        public event Action<CSteamID, string> OnPlayerLeft;

        public IReadOnlyDictionary<CSteamID, ClientInfo> Clients => _clients;

        public HostSyncManager(ManualLogSource log)
        {
            _log = log;
            _interactionLock = new InteractionLock(log);
            _saveDataSync = new SaveDataSyncManager(log);
        }

        /// <summary>
        /// 处理收到的消息 (主机端消息路由)
        /// </summary>
        public void HandleMessage(CSteamID sender, SyncMessage msg)
        {
            switch (msg.Type)
            {
                case SyncMessageType.JoinRequest:
                    HandleJoinRequest(sender, msg.Payload);
                    break;
                case SyncMessageType.ChallengeResponse:
                    HandleChallengeResponse(sender, msg.Payload);
                    break;
                case SyncMessageType.SyncReady:
                    HandleSyncReady(sender);
                    break;
                case SyncMessageType.ReconnectRequest:
                    HandleReconnectRequest(sender, msg.Payload);
                    break;
                case SyncMessageType.Heartbeat:
                    P2PTransport.UpdatePeerHeartbeat(sender);
                    break;
                case SyncMessageType.InteractionReq:
                    HandleRemoteInteraction(sender, msg.Payload);
                    break;
                case SyncMessageType.SaveDataOp:
                    _saveDataSync.HandleSaveDataOp(sender, msg.Payload);
                    break;
                case SyncMessageType.EditingStart:
                    _saveDataSync.HandleEditingStart(sender, msg.Payload);
                    break;
                case SyncMessageType.EditingEnd:
                    _saveDataSync.HandleEditingEnd(sender, msg.Payload);
                    break;
                case SyncMessageType.StoryAck:
                    HandleStoryAck(sender);
                    break;
                case SyncMessageType.StorySkip:
                    HandleStorySkip(sender);
                    break;
                case SyncMessageType.PlayerLeft:
                    HandlePlayerLeft(sender);
                    break;
                default:
                    _log?.LogWarning($"[HostSync] Unhandled message type: {msg.Type} from {sender}");
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  握手流程: JoinRequest → Challenge → ChallengeResponse → Accept/Reject
        // ─────────────────────────────────────────────

        private void HandleJoinRequest(CSteamID sender, JObject payload)
        {
            _log?.LogInfo($"[HostSync] JoinRequest from {sender}");

            // 验证是否在 Lobby 中
            if (!StudyRoomService.Instance.LobbyManager.IsMemberInLobby(sender))
            {
                var reject = SyncProtocol.Create(SyncMessageType.JoinRejected,
                    new Dictionary<string, object> { ["reason"] = "not_in_lobby" });
                P2PTransport.SendMessage(sender, reject);
                return;
            }

            // 无密码 → 直接接受
            if (string.IsNullOrEmpty(RoomPassword))
            {
                AcceptPlayer(sender);
                return;
            }

            // 有密码 → 发送挑战
            var challenge = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(challenge);

            _pendingChallenges[sender] = challenge;
            var challengeMsg = SyncProtocol.Create(SyncMessageType.Challenge,
                new Dictionary<string, object>
                {
                    ["challenge"] = Convert.ToBase64String(challenge)
                });
            P2PTransport.SendMessage(sender, challengeMsg);
        }

        private void HandleChallengeResponse(CSteamID sender, JObject payload)
        {
            if (!_pendingChallenges.TryGetValue(sender, out var challenge))
            {
                _log?.LogWarning($"[HostSync] Unexpected ChallengeResponse from {sender}");
                return;
            }
            _pendingChallenges.Remove(sender);

            var responseBase64 = payload.Value<string>("response") ?? "";
            var responseBytes = Convert.FromBase64String(responseBase64);

            // HMAC-SHA256 验证
            using (var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(RoomPassword)))
            {
                var expected = hmac.ComputeHash(challenge);
                if (CryptographicEquals(expected, responseBytes))
                {
                    AcceptPlayer(sender);
                }
                else
                {
                    var reject = SyncProtocol.Create(SyncMessageType.JoinRejected,
                        new Dictionary<string, object> { ["reason"] = "wrong_password" });
                    P2PTransport.SendMessage(sender, reject);
                }
            }
        }

        private static bool CryptographicEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }

        private void AcceptPlayer(CSteamID sender)
        {
            P2PTransport.AddPeer(sender);
            var personaName = SteamFriends.GetFriendPersonaName(sender);
            _clients[sender] = new ClientInfo
            {
                SteamId = sender,
                PersonaName = personaName,
                SyncReady = false
            };

            // 发送 JoinAccepted (客户端收到后创建子存档、重载场景)
            var accept = SyncProtocol.Create(SyncMessageType.JoinAccepted,
                new Dictionary<string, object>
                {
                    ["hostSteamId"] = SteamUser.GetSteamID().m_SteamID.ToString()
                });
            P2PTransport.SendMessage(sender, accept);

            // 通知其他人
            var joined = SyncProtocol.Create(SyncMessageType.PlayerJoined,
                new Dictionary<string, object>
                {
                    ["steamId"] = sender.m_SteamID.ToString(),
                    ["personaName"] = personaName
                });
            SeqBroadcast(joined, except: sender);

            StudyRoomService.Instance.LobbyManager.UpdateMemberCount();
            OnPlayerJoined?.Invoke(sender, personaName);
            _log?.LogInfo($"[HostSync] Player accepted: {personaName} ({sender})");
        }

        private void HandleSyncReady(CSteamID sender)
        {
            if (!_clients.ContainsKey(sender)) return;
            _clients[sender].SyncReady = true;

            // 发送完整状态快照
            var snapshot = BuildFullSnapshot();
            P2PTransport.SendMessage(sender, snapshot);

            // 发送完整存档数据 (ZIP 分片)
            SendSaveDataFull(sender);

            _log?.LogInfo($"[HostSync] Sent FullSnapshot + SaveDataFull to {sender}");
        }

        private void HandleReconnectRequest(CSteamID sender, JObject payload)
        {
            _log?.LogInfo($"[HostSync] ReconnectRequest from {sender}");
            if (!_clients.ContainsKey(sender)) return;
            _clients[sender].SyncReady = true;

            var lastSeq = payload?.Value<uint>("lastSeqNumber") ?? 0;

            // 如果客户端提供了 lastSeqNumber, 尝试增量重放
            if (lastSeq > 0 && _seqHistory.Count > 0)
            {
                var replayed = 0;
                // 按序列号升序重放
                var keys = new List<uint>(_seqHistory.Keys);
                keys.Sort();
                foreach (var seq in keys)
                {
                    if (seq > lastSeq)
                    {
                        P2PTransport.SendMessage(sender, _seqHistory[seq]);
                        replayed++;
                    }
                }

                if (replayed > 0)
                {
                    _log?.LogInfo($"[HostSync] Replayed {replayed} messages for {sender} from seq {lastSeq}");
                    return;
                }
            }

            // 无法增量重放 → 发送完整快照
            var snapshot = BuildFullSnapshot();
            P2PTransport.SendMessage(sender, snapshot);
            _log?.LogInfo($"[HostSync] Sent full snapshot to {sender}");
        }

        private void HandlePlayerLeft(CSteamID sender)
        {
            if (!_clients.TryGetValue(sender, out var info)) return;
            _clients.Remove(sender);
            P2PTransport.RemovePeer(sender);
            _interactionLock.ForceRelease(sender);
            _saveDataSync.ClearEditingForPlayer(sender);

            var leftMsg = SyncProtocol.Create(SyncMessageType.PlayerLeft,
                new Dictionary<string, object>
                {
                    ["steamId"] = sender.m_SteamID.ToString(),
                    ["personaName"] = info.PersonaName
                });
            SeqBroadcast(leftMsg);

            StudyRoomService.Instance.LobbyManager.UpdateMemberCount();
            OnPlayerLeft?.Invoke(sender, info.PersonaName);
            _log?.LogInfo($"[HostSync] Player left: {info.PersonaName}");
        }

        // ─────────────────────────────────────────────
        //  带序列号的广播封装
        // ─────────────────────────────────────────────

        /// <summary>
        /// 广播消息: 如果消息类型需要序列号，自动包装 SeqWrapper 并存入历史
        /// </summary>
        private void SeqBroadcast(SyncMessage msg, CSteamID? except = null)
        {
            if (SyncProtocol.NeedsSequenceNumber(msg.Type))
            {
                var seq = _nextSeq++;
                var wrapped = SyncProtocol.WrapWithSequence(msg, seq);

                // 存入历史缓冲 (如果超限则清理最旧的)
                _seqHistory[seq] = wrapped;
                if (_seqHistory.Count > MaxSeqHistory)
                    PruneSeqHistory();

                if (except.HasValue)
                    P2PTransport.BroadcastMessage(wrapped, except: except.Value);
                else
                    P2PTransport.BroadcastMessage(wrapped);
            }
            else
            {
                if (except.HasValue)
                    P2PTransport.BroadcastMessage(msg, except: except.Value);
                else
                    P2PTransport.BroadcastMessage(msg);
            }
        }

        private void PruneSeqHistory()
        {
            // 保留最新的 MaxSeqHistory * 3/4 条
            var keepCount = MaxSeqHistory * 3 / 4;
            var keys = new List<uint>(_seqHistory.Keys);
            keys.Sort();
            var removeCount = keys.Count - keepCount;
            for (int i = 0; i < removeCount; i++)
                _seqHistory.Remove(keys[i]);
        }

        // ─────────────────────────────────────────────
        //  广播方法 (由 Harmony Patches 调用)
        // ─────────────────────────────────────────────

        public void BroadcastStateChanged(string stateId)
        {
            var msg = SyncProtocol.Create(SyncMessageType.StateChanged,
                new Dictionary<string, object> { ["state"] = stateId });
            SeqBroadcast(msg);
        }

        public void BroadcastScenarioPlay(string scenarioType, int episode)
        {
            var msg = SyncProtocol.Create(SyncMessageType.ScenarioPlay,
                new Dictionary<string, object>
                {
                    ["scenarioType"] = scenarioType,
                    ["episode"] = episode
                });
            SeqBroadcast(msg);
        }

        public void BroadcastVoicePlay(string voice, bool moveMouth)
        {
            var msg = SyncProtocol.Create(SyncMessageType.VoicePlay,
                new Dictionary<string, object>
                {
                    ["voice"] = voice,
                    ["moveMouth"] = moveMouth
                });
            SeqBroadcast(msg);
        }

        /// <summary>
        /// 标志: 正在代替远程客户端执行交互 (绕过 Host Postfix 的锁检查)
        /// </summary>
        public static bool IsExecutingRemoteInteraction { get; set; }

        private void HandleRemoteInteraction(CSteamID sender, JObject payload)
        {
            var requestId = payload.Value<string>("requestId") ?? "";
            var type = payload.Value<string>("type") ?? "ClickHeroine";

            if (_interactionLock.TryAcquire(sender, type))
            {
                var grant = SyncProtocol.Create(SyncMessageType.InteractionGrant,
                    new Dictionary<string, object>
                    {
                        ["requestId"] = requestId,
                        ["playerId"] = sender.m_SteamID.ToString()
                    });
                SeqBroadcast(grant);

                // 主机代为执行点击反应
                if (type == "ClickHeroine")
                    ExecuteRemoteClickReaction(type);
            }
            else
            {
                var deny = SyncProtocol.Create(SyncMessageType.InteractionDeny,
                    new Dictionary<string, object>
                    {
                        ["requestId"] = requestId,
                        ["reason"] = "occupied"
                    });
                P2PTransport.SendMessage(sender, deny);
            }
        }

        /// <summary>
        /// 主机代替远程客户端执行点击反应。
        /// 设置 IsExecutingRemoteInteraction 绕过 HostPatches 的锁重复获取。
        /// ReactionReady 成功后，游戏的 UpdateFacility 自然驱动 StartReaction → ScenarioPlay 广播。
        /// </summary>
        private void ExecuteRemoteClickReaction(string lockType)
        {
            try
            {
                var clickHeroine = UnityEngine.Object.FindObjectOfType<FacilityClickHeroine>();
                if (clickHeroine == null)
                {
                    _log?.LogWarning("[HostSync] FacilityClickHeroine not found for remote interaction");
                    ReleaseInteractionLock(lockType);
                    return;
                }

                IsExecutingRemoteInteraction = true;
                bool ready = clickHeroine.ReactionReady(FacilityClickHeroine.ReactionType.Click);
                IsExecutingRemoteInteraction = false;

                if (!ready)
                {
                    _log?.LogInfo("[HostSync] ReactionReady=false for remote interaction, releasing lock");
                    ReleaseInteractionLock(lockType);
                }
                // ready=true → game UpdateFacility → StartReaction → WantPlayVoiceTextScenario Postfix
                //            → broadcasts ScenarioPlay; EndReaction Postfix → releases lock
            }
            catch (Exception ex)
            {
                IsExecutingRemoteInteraction = false;
                _log?.LogError($"[HostSync] Remote click reaction error: {ex.Message}");
                ReleaseInteractionLock(lockType);
            }
        }

        private void ReleaseInteractionLock(string lockType)
        {
            _interactionLock.Release(lockType);
            var endMsg = SyncProtocol.Create(SyncMessageType.InteractionEnd,
                new Dictionary<string, object> { ["type"] = lockType });
            SeqBroadcast(endMsg);
        }

        public void BroadcastPomodoroEvent(string eventType)
        {
            var msg = SyncProtocol.Create(SyncMessageType.PomodoroEvent,
                new Dictionary<string, object> { ["event"] = eventType });
            SeqBroadcast(msg);
        }

        public void BroadcastPomodoroSnapshot()
        {
            var snapshot = BuildPomodoroSnapshot();
            // PomodoroSnapshot 是全量快照，不需要 seq 编号 (NeedsSequenceNumber=false)
            P2PTransport.BroadcastMessage(snapshot);
        }

        public void BroadcastDecorationSnapshot()
        {
            var save = SaveDataManager.Instance;
            if (save == null) return;
            var decoData = save.DecorationSaveData;
            var msg = SyncProtocol.Create(SyncMessageType.DecorationChange,
                new Dictionary<string, object>
                {
                    ["decorationData"] = Newtonsoft.Json.Linq.JToken.FromObject(decoData)
                });
            SeqBroadcast(msg);
        }

        public void BroadcastEnvironmentSnapshot()
        {
            var save = SaveDataManager.Instance;
            if (save == null) return;
            var envData = save.EnviromentData;
            var msg = SyncProtocol.Create(SyncMessageType.EnvironmentViewChange,
                new Dictionary<string, object>
                {
                    ["enviromentData"] = Newtonsoft.Json.Linq.JToken.FromObject(envData)
                });
            SeqBroadcast(msg);
        }

        public void BroadcastAutoTimeSnapshot()
        {
            var save = SaveDataManager.Instance;
            if (save == null) return;
            var autoTimeData = save.AutoTimeWindowChangeData;
            var msg = SyncProtocol.Create(SyncMessageType.EnvironmentAutoTime,
                new Dictionary<string, object>
                {
                    ["autoTimeData"] = Newtonsoft.Json.Linq.JToken.FromObject(autoTimeData)
                });
            SeqBroadcast(msg);
        }

        public void BroadcastPointPurchaseSync()
        {
            var save = SaveDataManager.Instance;
            if (save == null) return;
            var msg = SyncProtocol.Create(SyncMessageType.PointPurchaseSync,
                new Dictionary<string, object>
                {
                    ["pointPurchaseData"] = Newtonsoft.Json.Linq.JToken.FromObject(save.PointPurchaseData)
                });
            SeqBroadcast(msg);
        }

        /// <summary>
        /// 广播经验/等级变更
        /// </summary>
        public void BroadcastExpChanged()
        {
            var levelData = LevelData.GetCurrentLevelData();
            if (levelData == null) return;
            var msg = SyncProtocol.Create(SyncMessageType.ExpChanged,
                new Dictionary<string, object> { ["exp"] = levelData.CurrentExp });
            SeqBroadcast(msg);
        }

        public void BroadcastLevelChanged()
        {
            var levelData = LevelData.GetCurrentLevelData();
            if (levelData == null) return;
            var msg = SyncProtocol.Create(SyncMessageType.LevelChanged,
                new Dictionary<string, object> { ["level"] = levelData.CurrentLevel });
            SeqBroadcast(msg);
        }

        public void BroadcastWorkSecondsSync()
        {
            var save = SaveDataManager.Instance;
            if (save?.PlayerData == null) return;
            var msg = SyncProtocol.Create(SyncMessageType.WorkSecondsSync,
                new Dictionary<string, object>
                {
                    ["current"] = save.PlayerData.CurrentWorkSeconds,
                    ["total"] = save.PlayerData.PomodoroTotalWorkSeconds
                });
            SeqBroadcast(msg);
        }

        /// <summary>
        /// 发送完整存档数据给指定客户端 (ZIP 压缩 + 分片)
        /// </summary>
        public void SendSaveDataFull(CSteamID target)
        {
            try
            {
                var profileSvc = new SaveProfileService(null);
                var saveDir = profileSvc.GetActiveProfileAbsolutePath();
                if (string.IsNullOrEmpty(saveDir) || !System.IO.Directory.Exists(saveDir))
                {
                    _log?.LogWarning("[HostSync] SaveDataFull: save directory not found");
                    return;
                }

                // 读取所有 ES3 文件并打包
                var files = System.IO.Directory.GetFiles(saveDir, "*.es3");
                var archiveData = CompressSaveFiles(files, saveDir);

                // 分片发送 (每片 < 480KB，留 32KB 头部空间)
                const int chunkSize = 480 * 1024;
                int totalChunks = (archiveData.Length + chunkSize - 1) / chunkSize;

                for (int i = 0; i < totalChunks; i++)
                {
                    int offset = i * chunkSize;
                    int len = Math.Min(chunkSize, archiveData.Length - offset);
                    var chunk = new byte[len];
                    Array.Copy(archiveData, offset, chunk, 0, len);

                    var msg = SyncProtocol.Create(SyncMessageType.SaveDataFull,
                        new Dictionary<string, object>
                        {
                            ["chunkIndex"] = i,
                            ["totalChunks"] = totalChunks,
                            ["data"] = Convert.ToBase64String(chunk)
                        });
                    P2PTransport.SendMessage(target, msg);
                }

                _log?.LogInfo($"[HostSync] Sent SaveDataFull to {target}: {archiveData.Length} bytes in {totalChunks} chunks");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[HostSync] SendSaveDataFull error: {ex.Message}");
            }
        }

        private byte[] CompressSaveFiles(string[] files, string baseDir)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                using (var zip = new System.IO.Compression.ZipArchive(ms,
                    System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var relativeName = System.IO.Path.GetFileName(file);
                        var entry = zip.CreateEntry(relativeName,
                            System.IO.Compression.CompressionLevel.Fastest);
                        using (var entryStream = entry.Open())
                        using (var fileStream = System.IO.File.OpenRead(file))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }
                return ms.ToArray();
            }
        }

        // ─────────────────────────────────────────────
        //  故事同步
        // ─────────────────────────────────────────────

        public void BroadcastStoryReady(string scenarioType, int episode)
        {
            _storyAcks.Clear();
            _storyAckTimer = 0;
            _waitingStoryAck = true;

            var msg = SyncProtocol.Create(SyncMessageType.StoryReady,
                new Dictionary<string, object>
                {
                    ["scenarioType"] = scenarioType,
                    ["episode"] = episode
                });
            SeqBroadcast(msg);
        }

        private void HandleStoryAck(CSteamID sender)
        {
            if (!_waitingStoryAck) return;
            _storyAcks.Add(sender);

            // 检查是否所有人都 Ack 了
            if (_storyAcks.Count >= _clients.Count)
            {
                StartStory();
            }
        }

        private void HandleStorySkip(CSteamID sender)
        {
            var msg = SyncProtocol.Create(SyncMessageType.StorySkip,
                new Dictionary<string, object>
                {
                    ["steamId"] = sender.m_SteamID.ToString(),
                    ["personaName"] = _clients.ContainsKey(sender)
                        ? _clients[sender].PersonaName : "Unknown"
                });
            SeqBroadcast(msg, except: sender);
        }

        /// <summary>
        /// Tick 中检查故事 Ack 超时
        /// </summary>
        public void TickStoryAck(float deltaTime)
        {
            if (!_waitingStoryAck) return;
            _storyAckTimer += deltaTime;
            if (_storyAckTimer >= StudyRoomConfig.StoryAckTimeoutSeconds)
            {
                StartStory();
            }
        }

        private void StartStory()
        {
            _waitingStoryAck = false;
            var msg = SyncProtocol.Create(SyncMessageType.StoryStart);
            SeqBroadcast(msg);
        }

        // ─────────────────────────────────────────────
        //  快照构建
        // ─────────────────────────────────────────────

        private SyncMessage BuildFullSnapshot()
        {
            var payload = new JObject
            {
                ["serverTimestampMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // 角色状态
            var charSvc = CharacterApiService.Instance;
            if (charSvc != null)
            {
                var stateInfo = charSvc.getState();
                if (stateInfo.ContainsKey("currentState"))
                    payload["currentState"] = stateInfo["currentState"]?.ToString() ?? "";
                if (stateInfo.ContainsKey("updateState"))
                    payload["updateState"] = stateInfo["updateState"]?.ToString() ?? "";
            }

            // 番茄钟状态
            try
            {
                var gameSvc = GameApiService.Instance;
                if (gameSvc != null)
                {
                    var pomState = gameSvc.getPomodoroStateObject() as Dictionary<string, object>;
                    if (pomState != null)
                    {
                        payload["pomodoro"] = JObject.FromObject(pomState);
                    }
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[HostSync] Pomodoro snapshot error: {ex.Message}"); }

            // 装饰/环境/模式状态
            try
            {
                var save = SaveDataManager.Instance;
                if (save != null)
                {
                    payload["decorationData"] = JToken.FromObject(save.DecorationSaveData);
                    payload["enviromentData"] = JToken.FromObject(save.EnviromentData);
                    payload["autoTimeData"] = JToken.FromObject(save.AutoTimeWindowChangeData);
                    payload["collaborationData"] = JToken.FromObject(save.CollaborationSaveData);
                    payload["progressData"] = JToken.FromObject(save.ScenarioProgressData);
                    payload["pointPurchaseData"] = JToken.FromObject(save.PointPurchaseData);

                    // 经验/等级
                    if (save.PlayerData != null)
                    {
                        payload["playerData"] = new JObject
                        {
                            ["currentWorkSeconds"] = save.PlayerData.CurrentWorkSeconds,
                            ["totalWorkSeconds"] = save.PlayerData.PomodoroTotalWorkSeconds
                        };
                    }
                    var levelData = LevelData.GetCurrentLevelData();
                    if (levelData != null)
                    {
                        payload["levelData"] = new JObject
                        {
                            ["level"] = levelData.CurrentLevel,
                            ["exp"] = levelData.CurrentExp,
                            ["nextLevelExp"] = levelData.NextLevelNecessaryExp
                        };
                    }
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[HostSync] Save data snapshot error: {ex.Message}"); }

            // 成员列表 (供客户端初始化)
            try
            {
                var membersArray = new Newtonsoft.Json.Linq.JArray();
                membersArray.Add(new JObject
                {
                    ["steamId"] = SteamUser.GetSteamID().m_SteamID.ToString(),
                    ["personaName"] = SteamFriends.GetPersonaName(),
                    ["isHost"] = true
                });
                foreach (var kv in _clients)
                {
                    membersArray.Add(new JObject
                    {
                        ["steamId"] = kv.Key.m_SteamID.ToString(),
                        ["personaName"] = kv.Value.PersonaName,
                        ["isHost"] = false
                    });
                }
                payload["members"] = membersArray;
            }
            catch (Exception ex) { _log?.LogWarning($"[HostSync] Members snapshot error: {ex.Message}"); }

            return new SyncMessage(SyncMessageType.FullSnapshot, payload);
        }

        private SyncMessage BuildPomodoroSnapshot()
        {
            var payload = new JObject
            {
                ["serverTimestampMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            try
            {
                var gameSvc = GameApiService.Instance;
                if (gameSvc != null)
                {
                    var pomState = gameSvc.getPomodoroStateObject() as Dictionary<string, object>;
                    if (pomState != null)
                    {
                        foreach (var kv in pomState)
                            payload[kv.Key] = kv.Value != null ? JToken.FromObject(kv.Value) : JValue.CreateNull();
                    }
                }
            }
            catch (Exception ex) { _log?.LogWarning($"[HostSync] PomodoroSnapshot error: {ex.Message}"); }

            return new SyncMessage(SyncMessageType.PomodoroSnapshot, payload);
        }

        /// <summary>
        /// 玩家断线处理
        /// </summary>
        public void HandlePeerTimeout(CSteamID steamId)
        {
            HandlePlayerLeft(steamId);
        }

        /// <summary>
        /// 主机本地点击时获取互动锁 + 广播 InteractionGrant
        /// </summary>
        public bool TryAcquireHostLock(string type)
        {
            var hostId = SteamUser.GetSteamID();
            if (_interactionLock.TryAcquire(hostId, type))
            {
                var grant = SyncProtocol.Create(SyncMessageType.InteractionGrant,
                    new Dictionary<string, object>
                    {
                        ["requestId"] = "",
                        ["playerId"] = hostId.m_SteamID.ToString()
                    });
                SeqBroadcast(grant);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 主机本地互动结束时释放锁 + 广播 InteractionEnd
        /// </summary>
        public void ReleaseHostLock(string type)
        {
            _interactionLock.Release(type);
            var msg = SyncProtocol.Create(SyncMessageType.InteractionEnd,
                new Dictionary<string, object> { ["type"] = type });
            SeqBroadcast(msg);
        }

        public void Reset()
        {
            _clients.Clear();
            _pendingChallenges.Clear();
            _storyAcks.Clear();
            _waitingStoryAck = false;
            _interactionLock.Reset();
            _saveDataSync.Reset();
            _nextSeq = 1;
            _seqHistory.Clear();
        }
    }

    public class ClientInfo
    {
        public CSteamID SteamId;
        public string PersonaName;
        public bool SyncReady;
    }
}

