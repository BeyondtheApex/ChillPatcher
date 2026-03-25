using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Steamworks;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// Steam Lobby 管理: 创建/搜索/加入/离开/元数据
    /// </summary>
    public class SteamLobbyManager
    {
        private readonly ManualLogSource _log;

        /// <summary>当前 Lobby ID (无效=未在房间中)</summary>
        public CSteamID LobbyId { get; private set; }

        /// <summary>是否已在 Lobby 中</summary>
        public bool InLobby => LobbyId.IsValid();

        // ─── 事件 ───
        public event Action<CSteamID> OnLobbyCreated;
        public event Action<CSteamID, bool> OnLobbyJoined;         // lobbyId, success
        public event Action<List<LobbyInfo>> OnLobbyListReceived;
        public event Action<CSteamID> OnMemberJoined;
        public event Action<CSteamID> OnMemberLeft;

        // ─── Steam Callbacks ───
        private CallResult<LobbyCreated_t> _lobbyCreatedResult;
        private CallResult<LobbyMatchList_t> _lobbyMatchListResult;
        private CallResult<LobbyEnter_t> _lobbyEnterResult;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdateCallback;

        public SteamLobbyManager(ManualLogSource log)
        {
            _log = log;
            _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        }

        /// <summary>
        /// 创建公开 Lobby
        /// </summary>
        public void CreateLobby(string roomName, bool hasPassword, int maxMembers)
        {
            _log?.LogInfo($"[Lobby] Creating lobby: {roomName}, max={maxMembers}");
            var call = SteamMatchmaking.CreateLobby(
                ELobbyType.k_ELobbyTypePublic, maxMembers);
            _lobbyCreatedResult = CallResult<LobbyCreated_t>.Create((result, failure) =>
            {
                if (failure || result.m_eResult != EResult.k_EResultOK)
                {
                    _log?.LogError($"[Lobby] CreateLobby failed: {result.m_eResult}");
                    return;
                }

                LobbyId = new CSteamID(result.m_ulSteamIDLobby);
                SetLobbyMetadata(roomName, hasPassword, maxMembers);
                _log?.LogInfo($"[Lobby] Created lobby: {LobbyId}");
                OnLobbyCreated?.Invoke(LobbyId);
            });
            _lobbyCreatedResult.Set(call);
        }

        /// <summary>
        /// 加入已有 Lobby
        /// </summary>
        public void JoinLobby(CSteamID lobbyId)
        {
            _log?.LogInfo($"[Lobby] Joining lobby: {lobbyId}");
            var call = SteamMatchmaking.JoinLobby(lobbyId);
            _lobbyEnterResult = CallResult<LobbyEnter_t>.Create((result, failure) =>
            {
                if (failure)
                {
                    _log?.LogError("[Lobby] JoinLobby failed (IO failure)");
                    OnLobbyJoined?.Invoke(lobbyId, false);
                    return;
                }

                var response = (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;
                if (response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
                {
                    _log?.LogError($"[Lobby] JoinLobby failed: {response}");
                    OnLobbyJoined?.Invoke(lobbyId, false);
                    return;
                }

                LobbyId = new CSteamID(result.m_ulSteamIDLobby);
                _log?.LogInfo($"[Lobby] Joined lobby: {LobbyId}");
                OnLobbyJoined?.Invoke(LobbyId, true);
            });
            _lobbyEnterResult.Set(call);
        }

        /// <summary>
        /// 离开当前 Lobby
        /// </summary>
        public void LeaveLobby()
        {
            if (!InLobby) return;
            _log?.LogInfo($"[Lobby] Leaving lobby: {LobbyId}");
            SteamMatchmaking.LeaveLobby(LobbyId);
            LobbyId = CSteamID.Nil;
        }

        /// <summary>
        /// 搜索公开自习室列表
        /// </summary>
        public void RequestLobbyList()
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                StudyRoomConfig.MetaKey_Filter, "1",
                ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                StudyRoomConfig.MetaKey_ProtocolVersion,
                StudyRoomConfig.ProtocolVersion,
                ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(
                ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

            var call = SteamMatchmaking.RequestLobbyList();
            _lobbyMatchListResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            _lobbyMatchListResult.Set(call);
        }

        private void OnLobbyMatchList(LobbyMatchList_t result, bool failure)
        {
            var lobbies = new List<LobbyInfo>();
            if (failure)
            {
                _log?.LogWarning("[Lobby] RequestLobbyList failed");
                OnLobbyListReceived?.Invoke(lobbies);
                return;
            }

            for (int i = 0; i < (int)result.m_nLobbiesMatching; i++)
            {
                var id = SteamMatchmaking.GetLobbyByIndex(i);
                lobbies.Add(ReadLobbyInfo(id));
            }

            _log?.LogInfo($"[Lobby] Found {lobbies.Count} lobbies");
            OnLobbyListReceived?.Invoke(lobbies);
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t data)
        {
            if (new CSteamID(data.m_ulSteamIDLobby) != LobbyId) return;

            var userId = new CSteamID(data.m_ulSteamIDUserChanged);
            var flags = (EChatMemberStateChange)data.m_rgfChatMemberStateChange;

            if ((flags & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                OnMemberJoined?.Invoke(userId);
            }
            if ((flags & (EChatMemberStateChange.k_EChatMemberStateChangeLeft |
                          EChatMemberStateChange.k_EChatMemberStateChangeDisconnected |
                          EChatMemberStateChange.k_EChatMemberStateChangeKicked)) != 0)
            {
                OnMemberLeft?.Invoke(userId);
            }
        }

        /// <summary>
        /// 设置 Lobby 元数据
        /// </summary>
        private void SetLobbyMetadata(string roomName, bool hasPassword, int maxMembers)
        {
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_Filter, "1");
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_RoomName, roomName);
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_HostName,
                SteamFriends.GetPersonaName());
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_PasswordRequired,
                hasPassword ? "1" : "0");
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_ProtocolVersion,
                StudyRoomConfig.ProtocolVersion);
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_MemberCount, "1");
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_MaxMembers,
                maxMembers.ToString());
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_PomodoroState, "idle");
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_Mode, "");
        }

        /// <summary>
        /// 更新成员计数元数据
        /// </summary>
        public void UpdateMemberCount()
        {
            if (!InLobby) return;
            var count = SteamMatchmaking.GetNumLobbyMembers(LobbyId);
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_MemberCount,
                count.ToString());
        }

        /// <summary>
        /// 更新番茄钟状态元数据
        /// </summary>
        public void UpdatePomodoroState(string state)
        {
            if (!InLobby) return;
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_PomodoroState, state);
        }

        /// <summary>
        /// 更新模式元数据
        /// </summary>
        public void UpdateMode(string mode)
        {
            if (!InLobby) return;
            SteamMatchmaking.SetLobbyData(LobbyId, StudyRoomConfig.MetaKey_Mode, mode ?? "");
        }

        /// <summary>
        /// 读取指定 Lobby 的信息
        /// </summary>
        public static LobbyInfo ReadLobbyInfo(CSteamID lobbyId)
        {
            return new LobbyInfo
            {
                LobbyId = lobbyId.m_SteamID.ToString(),
                RoomName = SteamMatchmaking.GetLobbyData(lobbyId, StudyRoomConfig.MetaKey_RoomName),
                HostName = SteamMatchmaking.GetLobbyData(lobbyId, StudyRoomConfig.MetaKey_HostName),
                HasPassword = SteamMatchmaking.GetLobbyData(lobbyId,
                    StudyRoomConfig.MetaKey_PasswordRequired) == "1",
                MemberCount = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId,
                    StudyRoomConfig.MetaKey_MemberCount), out var mc) ? mc : 0,
                MaxMembers = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId,
                    StudyRoomConfig.MetaKey_MaxMembers), out var mm) ? mm : StudyRoomConfig.MaxMembers,
                PomodoroState = SteamMatchmaking.GetLobbyData(lobbyId,
                    StudyRoomConfig.MetaKey_PomodoroState),
                Mode = SteamMatchmaking.GetLobbyData(lobbyId,
                    StudyRoomConfig.MetaKey_Mode),
            };
        }

        /// <summary>
        /// 验证某个 SteamID 是否在当前 Lobby 中
        /// </summary>
        public bool IsMemberInLobby(CSteamID steamId)
        {
            if (!InLobby) return false;
            int count = SteamMatchmaking.GetNumLobbyMembers(LobbyId);
            for (int i = 0; i < count; i++)
            {
                if (SteamMatchmaking.GetLobbyMemberByIndex(LobbyId, i) == steamId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取 Lobby 所有者 (主机)
        /// </summary>
        public CSteamID GetLobbyOwner()
        {
            if (!InLobby) return CSteamID.Nil;
            return SteamMatchmaking.GetLobbyOwner(LobbyId);
        }

        public void Dispose()
        {
            LeaveLobby();
            _lobbyChatUpdateCallback?.Dispose();
        }
    }

    /// <summary>
    /// Lobby 信息 DTO
    /// </summary>
    public class LobbyInfo
    {
        public string LobbyId;
        public string RoomName;
        public string HostName;
        public bool HasPassword;
        public int MemberCount;
        public int MaxMembers;
        public string PomodoroState;
        public string Mode;
    }
}
