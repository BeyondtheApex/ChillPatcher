using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// 自习室系统主服务: 房间生命周期管理 + Tick 驱动
    /// </summary>
    public class StudyRoomService : IDisposable
    {
        private readonly ManualLogSource _log;

        // ─── 单例 ───
        public static StudyRoomService Instance { get; private set; }

        // ─── 子系统 ───
        public SteamLobbyManager LobbyManager { get; private set; }
        public HostSyncManager HostSync { get; private set; }
        public ClientSyncManager ClientSync { get; private set; }
        private readonly SaveProfileService _profileService;

        // ─── 子存档记忆 ───
        private string _previousProfileName;
        private string _pendingClientProfileName;

        /// <summary>保存创建房间时的继承选项</summary>
        private string[] _inheritFromPatterns;

        // ─── SyncReady 延迟发送 (等待场景重载完成) ───
        private bool _pendingSyncReady;
        private float _syncReadyDelayTimer;

        // ─── 断线重连 Lobby ───
        private CSteamID _reconnectLobbyId = CSteamID.Nil;
        private bool _lobbyRejoinPending;

        // ─── 隐藏的 UI 元素引用 (用于退出时恢复) ───
        private readonly List<GameObject> _hiddenUIObjects = new List<GameObject>();

        // ─── 状态 ───
        public bool IsActive { get; private set; }
        public static bool IsHost { get; private set; }
        public static bool IsClient => Instance != null && Instance.IsActive && !IsHost;

        private float _heartbeatTimer;
        private float _pomodoroTimer;

        /// <summary>缓存的大厅列表 (供 JSApi 同步查询)</summary>
        private List<LobbyInfo> _cachedLobbyList = new List<LobbyInfo>();

        // ─── 事件 (供 JSApi 订阅) ───
        public event Action<string, object> OnStudyRoomEvent;

        public StudyRoomService(ManualLogSource log)
        {
            _log = log;
            Instance = this;
            P2PTransport.Initialize(log);
            LobbyManager = new SteamLobbyManager(log);
            HostSync = new HostSyncManager(log);
            ClientSync = new ClientSyncManager(log);
            _profileService = new SaveProfileService(log);

            // 注册网络消息处理
            P2PTransport.OnMessageReceived += OnRawMessageReceived;

            // 注册 Lobby 事件
            LobbyManager.OnLobbyCreated += OnLobbyCreated;
            LobbyManager.OnLobbyJoined += OnLobbyJoined;
            LobbyManager.OnLobbyListReceived += OnLobbyListReceived;
            LobbyManager.OnMemberJoined += OnMemberJoinedLobby;
            LobbyManager.OnMemberLeft += OnMemberLeftLobby;

            // 注册客户端事件传递到 StudyRoomEvent
            ClientSync.OnSyncReady += () => Emit("syncReady", null);
            ClientSync.OnKicked += reason => Emit("kicked", new { reason });
            ClientSync.OnRoomClosed += () =>
            {
                Emit("roomClosed", new { reason = "host_closed" });
                LeaveRoom();
            };
            ClientSync.OnPlayerJoined += (id, name) =>
                Emit("playerJoined", new { steamId = id.m_SteamID.ToString(), personaName = name });
            ClientSync.OnPlayerLeft += (id, name) =>
                Emit("playerLeft", new { steamId = id.m_SteamID.ToString(), personaName = name });
            ClientSync.OnConnectionLost += () => Emit("connectionLost", null);
            ClientSync.OnReconnecting += attempt => Emit("reconnecting", new { attempt });
            ClientSync.OnReconnected += () => Emit("reconnected", null);
            ClientSync.OnStoryReady += (type, p) =>
                Emit("storyReady", new { scenarioType = type, episode = p.Value<int>("episode") });
            ClientSync.OnStoryStarted += () => Emit("storyStarted", null);
            ClientSync.OnStorySkipped += (id, name) =>
                Emit("storySkipped", new { steamId = id.m_SteamID.ToString(), personaName = name });
            ClientSync.OnPomodoroSync += p => Emit("pomodoroSync", p);
            ClientSync.OnJoinFailed += reason => Emit("joinFailed", new { reason });
            ClientSync.OnJoinAccepted += () => OnClientJoinAccepted();

            // 注册客户端编辑状态事件
            ClientSync.OnEditingChanged += (key, info, isStart) =>
            {
                if (isStart)
                    Emit("editingStarted", new
                    {
                        dataType = info.DataType,
                        itemId = info.ItemId,
                        steamId = info.SteamId.m_SteamID.ToString(),
                        personaName = info.PersonaName
                    });
                else
                    Emit("editingEnded", new
                    {
                        dataType = info.DataType,
                        itemId = info.ItemId,
                        steamId = info.SteamId.m_SteamID.ToString()
                    });
            };

            // 注册主机事件传递
            HostSync.OnPlayerJoined += (id, name) =>
                Emit("playerJoined", new { steamId = id.m_SteamID.ToString(), personaName = name });
            HostSync.OnPlayerLeft += (id, name) =>
                Emit("playerLeft", new { steamId = id.m_SteamID.ToString(), personaName = name });

            // 注册主机编辑状态事件
            HostSync.SaveDataSync.OnEditingChanged += (key, info, isStart) =>
            {
                if (isStart)
                    Emit("editingStarted", new
                    {
                        dataType = info.DataType,
                        itemId = info.ItemId,
                        steamId = info.SteamId.m_SteamID.ToString(),
                        personaName = info.PersonaName
                    });
                else
                    Emit("editingEnded", new
                    {
                        dataType = info.DataType,
                        itemId = info.ItemId,
                        steamId = info.SteamId.m_SteamID.ToString()
                    });
            };
        }

        // ─────────────────────────────────────────────
        //  房间生命周期: 创建 / 加入 / 离开 / 关闭
        // ─────────────────────────────────────────────

        /// <summary>
        /// 主机: 创建自习室
        /// inheritFrom: null=空白存档, ["*"]=继承全部, ["todo","calendar",...]=选择性继承
        /// </summary>
        public void CreateRoom(string roomName, string password, int maxMembers, string[] inheritFrom)
        {
            if (IsActive)
            {
                _log?.LogWarning("[StudyRoom] Already in a room");
                return;
            }

            _log?.LogInfo($"[StudyRoom] Creating room: {roomName}");
            IsHost = true;
            _inheritFromPatterns = inheritFrom;
            HostSync.RoomPassword = password;

            // 安装主机 Patches
            Patches.StudyRoomPatches.PatchHost();

            // 创建 Lobby → 成功后在 OnLobbyCreated 中创建子存档
            LobbyManager.CreateLobby(roomName, !string.IsNullOrEmpty(password), maxMembers);
        }

        /// <summary>
        /// 客户端: 加入自习室
        /// </summary>
        public void JoinRoom(string lobbyIdStr, string password)
        {
            if (IsActive)
            {
                _log?.LogWarning("[StudyRoom] Already in a room");
                return;
            }

            if (!ulong.TryParse(lobbyIdStr, out var lobbyIdVal))
            {
                Emit("joinFailed", new { reason = "invalid_lobby_id" });
                return;
            }

            _log?.LogInfo($"[StudyRoom] Joining room: {lobbyIdStr}");
            IsHost = false;
            ClientSync.JoinPassword = password;

            // 安装客户端 Patches
            Patches.StudyRoomPatches.PatchClient();

            // 锁定客户端服务
            LockClientServices();

            var lobbyId = new CSteamID(lobbyIdVal);
            LobbyManager.JoinLobby(lobbyId);
        }

        /// <summary>
        /// 离开自习室 (主机或客户端)
        /// </summary>
        public void LeaveRoom()
        {
            if (!IsActive) return;
            _log?.LogInfo("[StudyRoom] Leaving room");

            if (IsHost)
            {
                // 通知所有客户端房间关闭
                var msg = SyncProtocol.Create(SyncMessageType.RoomClosed);
                P2PTransport.BroadcastMessage(msg);
            }
            else
            {
                // 通知主机自己离开
                ClientSync.SendPlayerLeft();
            }

            Cleanup();
            Emit("roomLeft", null);
        }

        /// <summary>
        /// 主机端: 关闭房间 (与 LeaveRoom 相同但语义不同)
        /// </summary>
        public void CloseRoom()
        {
            if (!IsHost) return;
            LeaveRoom();
        }

        // ─────────────────────────────────────────────
        //  Tick (每帧由 PlayerLoopInjector 调用)
        // ─────────────────────────────────────────────

        public static void Tick()
        {
            if (Instance == null || !Instance.IsActive) return;
            Instance.TickInternal();
        }

        private void TickInternal()
        {
            // 1. 接收网络消息
            P2PTransport.PollMessages();

            var dt = Time.deltaTime;

            // 1.5 客户端: 检查延迟 SyncReady (等待场景重载完成)
            if (_pendingSyncReady && IsClient)
            {
                if (Bulbul.SaveDataManager.HasInstance)
                {
                    _syncReadyDelayTimer += dt;
                    if (_syncReadyDelayTimer >= 2.0f)
                    {
                        _pendingSyncReady = false;
                        _syncReadyDelayTimer = 0;
                        ClientSync.SendSyncReady();
                        _log?.LogInfo("[StudyRoom] SyncReady sent after scene reload");
                    }
                }
            }

            // 2. 心跳
            _heartbeatTimer += dt;
            if (_heartbeatTimer >= StudyRoomConfig.HeartbeatIntervalSeconds)
            {
                _heartbeatTimer = 0;
                SendHeartbeat();
                CheckPeerTimeouts();
            }

            // 3. 主机端: 番茄钟快照
            if (IsHost)
            {
                _pomodoroTimer += dt;
                if (_pomodoroTimer >= StudyRoomConfig.PomodoroSnapshotIntervalSeconds)
                {
                    _pomodoroTimer = 0;
                    HostSync.BroadcastPomodoroSnapshot();
                }
                HostSync.TickStoryAck(dt);
            }

            // 4. 客户端: 断线检测 & 重连
            if (IsClient)
            {
                ClientSync.CheckConnection();

                // 断线时: 如果已不在 Lobby 中，先尝试重新加入
                if (ClientSync.Disconnected
                    && !LobbyManager.InLobby
                    && _reconnectLobbyId.IsValid()
                    && !_lobbyRejoinPending)
                {
                    _lobbyRejoinPending = true;
                    _log?.LogInfo($"[StudyRoom] Rejoining lobby for reconnect: {_reconnectLobbyId}");
                    LobbyManager.JoinLobby(_reconnectLobbyId);
                }

                ClientSync.TickReconnect(dt);
            }
        }

        private void SendHeartbeat()
        {
            var msg = SyncProtocol.Create(SyncMessageType.Heartbeat,
                new Dictionary<string, object>
                {
                    ["timestampMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            P2PTransport.BroadcastMessage(msg);
        }

        private void CheckPeerTimeouts()
        {
            var timedOut = P2PTransport.GetTimedOutPeers();
            foreach (var peer in timedOut)
            {
                if (IsHost)
                {
                    HostSync.HandlePeerTimeout(peer);
                }
            }

            // Fix #13: 定期发出 memberSyncUpdate 事件
            foreach (var kv in P2PTransport.Peers)
            {
                var elapsed = UnityEngine.Time.realtimeSinceStartup - kv.Value;
                var connected = elapsed < StudyRoomConfig.DisconnectTimeoutSeconds;
                Emit("memberSyncUpdate", new
                {
                    steamId = kv.Key.m_SteamID.ToString(),
                    latencyMs = (int)(elapsed * 1000),
                    connected
                });
            }
        }

        // ─────────────────────────────────────────────
        //  网络消息路由
        // ─────────────────────────────────────────────

        private void OnRawMessageReceived(CSteamID sender, byte[] data)
        {
            if (!SyncProtocol.TryDeserialize(data, 0, data.Length, out var msg))
            {
                _log?.LogWarning($"[StudyRoom] Failed to deserialize message from {sender}");
                return;
            }

            if (IsHost)
                HostSync.HandleMessage(sender, msg);
            else
                ClientSync.HandleMessage(sender, msg);
        }

        // ─────────────────────────────────────────────
        //  Lobby 事件回调
        // ─────────────────────────────────────────────

        private void OnLobbyCreated(CSteamID lobbyId)
        {
            try
            {
                IsActive = true;
                P2PTransport.StartListening();

                // 创建主机子存档
                _previousProfileName = SaveProfileService.ActiveProfileName;
                var profileName = StudyRoomConfig.HostProfilePrefix
                    + System.Guid.NewGuid().ToString("N").Substring(0, 8);
                _profileService.createProfile(profileName, _inheritFromPatterns);
                _profileService.switchProfile(profileName);

                Emit("roomCreated", new
                {
                    lobbyId = lobbyId.m_SteamID.ToString(),
                    inviteCode = lobbyId.m_SteamID.ToString()
                });
                _log?.LogInfo($"[StudyRoom] Room created, lobby={lobbyId}");
            }
            catch (System.Exception ex)
            {
                _log?.LogError($"[StudyRoom] OnLobbyCreated exception: {ex}");
            }
        }

        private void OnLobbyJoined(CSteamID lobbyId, bool success)
        {
            // 断线重连的 lobby rejoin
            if (_lobbyRejoinPending)
            {
                _lobbyRejoinPending = false;
                if (success)
                    _log?.LogInfo("[StudyRoom] Lobby rejoin successful for reconnect");
                else
                    _log?.LogWarning("[StudyRoom] Lobby rejoin failed, continuing reconnect via P2P");
                return;
            }

            if (!success)
            {
                // 加入失败: 回滚 patches 和服务锁
                Patches.StudyRoomPatches.UnpatchClient();
                UnlockClientServices();
                Emit("joinFailed", new { reason = "lobby_join_failed" });
                return;
            }

            IsActive = true;
            P2PTransport.StartListening();
            _reconnectLobbyId = lobbyId;

            // 获取主机 SteamID (Lobby Owner)
            var hostId = LobbyManager.GetLobbyOwner();
            ClientSync.HostId = hostId;
            P2PTransport.AddPeer(hostId);

            // 记住当前存档名，准备后续切换
            _previousProfileName = SaveProfileService.ActiveProfileName;

            // 发送 JoinRequest → 等待 Challenge/JoinAccepted 再创建子存档
            var joinReq = SyncProtocol.Create(SyncMessageType.JoinRequest,
                new Dictionary<string, object>
                {
                    ["steamId"] = SteamUser.GetSteamID().m_SteamID.ToString()
                });
            P2PTransport.SendMessage(hostId, joinReq);
        }

        /// <summary>
        /// 客户端收到 JoinAccepted 后: 创建子存档 → 切换 → 发送 SyncReady
        /// </summary>
        private void OnClientJoinAccepted()
        {
            var hostId = ClientSync.HostId;
            var profileName = StudyRoomConfig.ClientProfilePrefix
                + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            _pendingClientProfileName = profileName;
            _profileService.createProfile(profileName, new[] { "*" });
            _profileService.switchProfile(profileName);

            // switchProfile 触发场景重载，SyncReady 需要在重载完成后发送
            _pendingSyncReady = true;
            _syncReadyDelayTimer = 0;

            Emit("roomJoined", new
            {
                lobbyId = LobbyManager.LobbyId.m_SteamID.ToString(),
                hostName = SteamFriends.GetFriendPersonaName(hostId),
                isHost = false
            });
        }

        private void OnLobbyListReceived(List<LobbyInfo> lobbies)
        {
            _cachedLobbyList = lobbies ?? new List<LobbyInfo>();
            Emit("lobbyListUpdated", new { lobbies });
        }

        private void OnMemberJoinedLobby(CSteamID steamId)
        {
            _log?.LogInfo($"[StudyRoom] Lobby member joined: {steamId}");
        }

        private void OnMemberLeftLobby(CSteamID steamId)
        {
            _log?.LogInfo($"[StudyRoom] Lobby member left: {steamId}");
        }

        // ─────────────────────────────────────────────
        //  查询方法
        // ─────────────────────────────────────────────

        public string GetRoomInfo()
        {
            if (!IsActive) return "{}";
            return JSApi.JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["lobbyId"] = LobbyManager.LobbyId.m_SteamID.ToString(),
                ["isHost"] = IsHost,
                ["hostSteamId"] = IsHost
                    ? SteamUser.GetSteamID().m_SteamID.ToString()
                    : ClientSync.HostId.m_SteamID.ToString(),
                ["inviteCode"] = LobbyManager.LobbyId.m_SteamID.ToString(),
                ["password"] = IsHost ? (HostSync.RoomPassword ?? "") : null,
            });
        }

        public List<Dictionary<string, object>> GetMembers()
        {
            var members = new List<Dictionary<string, object>>();
            if (IsHost)
            {
                // 添加主机自己
                members.Add(new Dictionary<string, object>
                {
                    ["steamId"] = SteamUser.GetSteamID().m_SteamID.ToString(),
                    ["personaName"] = SteamFriends.GetPersonaName(),
                    ["isHost"] = true,
                    ["syncState"] = "ready"
                });
                foreach (var kv in HostSync.Clients)
                {
                    members.Add(new Dictionary<string, object>
                    {
                        ["steamId"] = kv.Key.m_SteamID.ToString(),
                        ["personaName"] = kv.Value.PersonaName,
                        ["isHost"] = false,
                        ["syncState"] = kv.Value.SyncReady ? "ready" : "syncing"
                    });
                }
            }
            else if (IsClient)
            {
                return ClientSync.GetTrackedMembers();
            }
            return members;
        }

        public void RefreshLobbyList()
        {
            LobbyManager.RequestLobbyList();
        }

        public List<LobbyInfo> GetCachedLobbyList()
        {
            return _cachedLobbyList;
        }

        // ─────────────────────────────────────────────
        //  服务锁定 (客户端加入时锁定交互，离开时解锁)
        // ─────────────────────────────────────────────

        private void LockClientServices()
        {
            try
            {
                // Fix #5: 提前禁用 AI，避免加入房间到收到快照之间的窗口期产生随机行为
                var charSvc = CharacterApiService.Instance;
                if (charSvc != null)
                {
                    charSvc.Locked = true;
                    charSvc.setAIEnabled(false);
                }

                // Fix #3: 重置本地番茄钟计时器，避免与主机快照冲突
                try
                {
                    var gameSvc = GameApiService.Instance;
                    gameSvc?.resetPomodoro();
                }
                catch (Exception ex) { _log?.LogWarning($"[StudyRoom] Pomodoro reset error: {ex.Message}"); }

                if (DecorationApiService.Instance != null) DecorationApiService.Instance.Locked = true;
                if (EnvironmentApiService.Instance != null) EnvironmentApiService.Instance.Locked = true;
                if (ModeApiService.Instance != null) ModeApiService.Instance.Locked = true;
                if (VoiceApiService.Instance != null) VoiceApiService.Instance.Locked = true;
                if (SubtitleApiService.Instance != null) SubtitleApiService.Instance.Locked = true;
                HideUIElements();
                _log?.LogInfo("[StudyRoom] Client services locked, AI disabled, pomodoro reset");
            }
            catch (Exception ex) { _log?.LogWarning($"[StudyRoom] Lock error: {ex.Message}"); }
        }

        private void UnlockClientServices()
        {
            try
            {
                if (CharacterApiService.Instance != null)
                {
                    CharacterApiService.Instance.Locked = false;
                    CharacterApiService.Instance.setAIEnabled(true);
                }
                if (DecorationApiService.Instance != null) DecorationApiService.Instance.Locked = false;
                if (EnvironmentApiService.Instance != null) EnvironmentApiService.Instance.Locked = false;
                if (ModeApiService.Instance != null) ModeApiService.Instance.Locked = false;
                if (VoiceApiService.Instance != null) VoiceApiService.Instance.Locked = false;
                if (SubtitleApiService.Instance != null) SubtitleApiService.Instance.Locked = false;
                RestoreUIElements();
                _log?.LogInfo("[StudyRoom] Client services unlocked, AI restored");
            }
            catch (Exception ex) { _log?.LogWarning($"[StudyRoom] Unlock error: {ex.Message}"); }
        }

        private void HideUIElements()
        {
            foreach (var path in StudyRoomConfig.HiddenUIElements)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var go = GameObject.Find(path);
                if (go != null && go.activeSelf)
                {
                    go.SetActive(false);
                    _hiddenUIObjects.Add(go);
                    _log?.LogInfo($"[StudyRoom] Hidden UI: {path}");
                }
            }
        }

        private void RestoreUIElements()
        {
            foreach (var go in _hiddenUIObjects)
            {
                if (go != null) go.SetActive(true);
            }
            if (_hiddenUIObjects.Count > 0)
                _log?.LogInfo($"[StudyRoom] Restored {_hiddenUIObjects.Count} hidden UI elements");
            _hiddenUIObjects.Clear();
        }

        // ─────────────────────────────────────────────
        //  清理
        // ─────────────────────────────────────────────

        private void Cleanup()
        {
            var wasHost = IsHost;
            var wasActive = IsActive;
            IsActive = false;
            IsHost = false;
            _heartbeatTimer = 0;
            _pomodoroTimer = 0;
            _reconnectLobbyId = CSteamID.Nil;
            _lobbyRejoinPending = false;

            // 卸载 Patches
            if (wasHost)
                Patches.StudyRoomPatches.UnpatchHost();
            else
                Patches.StudyRoomPatches.UnpatchClient();

            // 解锁客户端服务
            if (!wasHost)
                UnlockClientServices();

            P2PTransport.StopListening();
            LobbyManager.LeaveLobby();
            HostSync.Reset();
            ClientSync.Reset();

            // 切回原存档并删除临时子存档
            if (wasActive)
            {
                var currentProfile = SaveProfileService.ActiveProfileName;
                _profileService.switchProfile(_previousProfileName);
                if (!string.IsNullOrEmpty(currentProfile))
                {
                    try { _profileService.deleteProfile(currentProfile); }
                    catch (Exception ex)
                    {
                        _log?.LogWarning($"[StudyRoom] Failed to delete temp profile: {ex.Message}");
                    }
                }
                _previousProfileName = null;
            }
        }

        private void Emit(string eventName, object data)
        {
            try { OnStudyRoomEvent?.Invoke(eventName, data); }
            catch (Exception ex) { _log?.LogWarning($"[StudyRoom] Event error ({eventName}): {ex.Message}"); }
        }

        public void Dispose()
        {
            if (IsActive) LeaveRoom();
            P2PTransport.OnMessageReceived -= OnRawMessageReceived;
            LobbyManager?.Dispose();
            P2PTransport.Reset();
            Instance = null;
        }
    }
}

