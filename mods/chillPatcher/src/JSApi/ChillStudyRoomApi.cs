using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using ChillPatcher.Integration.StudyRoom;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// 自习室 JS API。JS 端通过 chill.studyRoom 访问。
    /// </summary>
    public class ChillStudyRoomApi : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly StudyRoomService _service;
        private readonly Dictionary<string, GameEventHandler> _handlers
            = new Dictionary<string, GameEventHandler>();
        private bool _isSubscribed;

        public ChillStudyRoomApi(ManualLogSource logger)
        {
            _logger = logger;
            _service = new StudyRoomService(logger);
        }

        // ─── 房间管理 ───

        /// <summary>
        /// 创建自习室
        /// options JSON: {password?, maxMembers?, roomName?, inheritSave?}
        /// </summary>
        public string createRoom(string optionsJson)
        {
            try
            {
                var opts = Newtonsoft.Json.Linq.JObject.Parse(optionsJson ?? "{}");
                var roomName = opts.Value<string>("roomName") ?? "Study Room";
                var password = opts.Value<string>("password") ?? "";
                var maxMembers = opts.Value<int?>("maxMembers") ?? StudyRoomConfig.MaxMembers;

                // Fix #15: 支持选择性存档继承
                // inheritSave: true → ["*"], false → null
                // inheritFrom: ["todo","calendar",...] → 选择性继承
                string[] inheritFrom = null;
                if (opts.ContainsKey("inheritFrom"))
                {
                    var arr = opts["inheritFrom"] as Newtonsoft.Json.Linq.JArray;
                    if (arr != null)
                        inheritFrom = arr.Select(t => (string)t).Where(s => s != null).ToArray();
                }
                else
                {
                    var inheritSave = opts.Value<bool?>("inheritSave") ?? true;
                    inheritFrom = inheritSave ? new[] { "*" } : null;
                }

                _service.CreateRoom(roomName, password, maxMembers, inheritFrom);
                return JSApiHelper.ToJson(new { success = true });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[JSApi.StudyRoom] createRoom error: {ex.Message}");
                return JSApiHelper.ToJson(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 加入自习室
        /// </summary>
        public string joinRoom(string lobbyId, string password)
        {
            try
            {
                _service.JoinRoom(lobbyId, password ?? "");
                return JSApiHelper.ToJson(new { success = true });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[JSApi.StudyRoom] joinRoom error: {ex.Message}");
                return JSApiHelper.ToJson(new { success = false, error = ex.Message });
            }
        }

        /// <summary>离开自习室</summary>
        public void leaveRoom() => _service.LeaveRoom();

        /// <summary>主机关闭房间</summary>
        public void closeRoom() => _service.CloseRoom();

        // ─── 大厅浏览 ───

        /// <summary>刷新大厅列表 (异步, 通过 lobbyListUpdated 事件返回)</summary>
        public void refreshLobbyList() => _service.RefreshLobbyList();

        /// <summary>获取缓存的大厅列表 JSON</summary>
        public string getLobbyList()
        {
            var lobbies = _service.GetCachedLobbyList();
            return JSApiHelper.ToJson(lobbies);
        }

        /// <summary>通过邀请码搜索房间</summary>
        public string searchByInviteCode(string code)
        {
            // 邀请码就是 lobbyId
            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["lobbyId"] = code
            });
        }

        // ─── 房间状态 ───

        /// <summary>获取当前房间信息 JSON</summary>
        public string getRoomInfo() => _service.GetRoomInfo();

        /// <summary>获取成员列表 JSON</summary>
        public string getMembers() => JSApiHelper.ToJson(_service.GetMembers());

        /// <summary>获取自己的信息 JSON</summary>
        public string getMyInfo()
        {
            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["steamId"] = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString(),
                ["personaName"] = Steamworks.SteamFriends.GetPersonaName(),
                ["isHost"] = StudyRoomService.IsHost
            });
        }

        /// <summary>是否在房间中</summary>
        public bool isInRoom() => _service.IsActive;

        /// <summary>是否是主机</summary>
        public bool isHost() => StudyRoomService.IsHost;

        /// <summary>获取成员同步状态 JSON: {connected, latencyMs, lastHeartbeat}</summary>
        public string getMemberSyncState(string steamId)
        {
            if (!_service.IsActive || string.IsNullOrEmpty(steamId))
                return JSApiHelper.ToJson(new { connected = false, latencyMs = -1, lastHeartbeat = 0 });

            if (!ulong.TryParse(steamId, out var id))
                return JSApiHelper.ToJson(new { connected = false, latencyMs = -1, lastHeartbeat = 0 });

            var csid = new Steamworks.CSteamID(id);
            var peers = P2PTransport.Peers;
            if (peers.TryGetValue(csid, out var lastTime))
            {
                var elapsed = UnityEngine.Time.realtimeSinceStartup - lastTime;
                return JSApiHelper.ToJson(new
                {
                    connected = elapsed < StudyRoomConfig.DisconnectTimeoutSeconds,
                    latencyMs = (int)(elapsed * 1000),
                    lastHeartbeat = lastTime
                });
            }
            return JSApiHelper.ToJson(new { connected = false, latencyMs = -1, lastHeartbeat = 0 });
        }

        // ─── 故事应答 ───

        /// <summary>响应 StoryReady → 发送 StoryAck</summary>
        public void ackStory()
        {
            if (!_service.IsActive || StudyRoomService.IsHost) return;
            var msg = SyncProtocol.Create(SyncMessageType.StoryAck,
                new Dictionary<string, object>
                {
                    ["playerId"] = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString()
                });
            P2PTransport.SendMessage(_service.ClientSync.HostId, msg);
        }

        /// <summary>跳过故事 → 发送 StorySkip</summary>
        public void skipStory()
        {
            if (!_service.IsActive || StudyRoomService.IsHost) return;
            var msg = SyncProtocol.Create(SyncMessageType.StorySkip,
                new Dictionary<string, object>
                {
                    ["steamId"] = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString()
                });
            P2PTransport.SendMessage(_service.ClientSync.HostId, msg);
        }

        // ─── 编辑状态 ───

        /// <summary>获取所有正在编辑的状态 JSON</summary>
        public string getEditingStatus()
        {
            if (!_service.IsActive) return "[]";
            try
            {
                List<EditingInfo> infos;
                if (StudyRoomService.IsHost)
                    infos = _service.HostSync.SaveDataSync.GetAllEditingStatus();
                else
                    infos = _service.ClientSync.GetAllEditingStatus();

                var result = new List<Dictionary<string, object>>();
                foreach (var info in infos)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["dataType"] = info.DataType,
                        ["itemId"] = info.ItemId,
                        ["steamId"] = info.SteamId.m_SteamID.ToString(),
                        ["personaName"] = info.PersonaName
                    });
                }
                return JSApiHelper.ToJson(result);
            }
            catch
            {
                return "[]";
            }
        }

        // ─── 事件订阅 (遵循现有 JSApi on/off 模式) ───

        public string on(string eventName, Action<string> handler)
        {
            if (handler == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(eventName)) eventName = "*";
            EnsureSubscribed();
            var token = Guid.NewGuid().ToString("N");
            _handlers[token] = new GameEventHandler { EventName = eventName, Handler = handler };
            return token;
        }

        public bool off(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            return _handlers.Remove(token);
        }

        private void EnsureSubscribed()
        {
            if (_isSubscribed) return;
            _service.OnStudyRoomEvent += OnServiceEvent;
            _isSubscribed = true;
        }

        private void OnServiceEvent(string eventName, object payload)
        {
            if (_handlers.Count == 0) return;
            // 具体事件: 只发送 payload JSON
            // 通配 "*": 发送 {name, payload} 方便调试
            string specificJson = null;
            string wildcardJson = null;

            foreach (var kv in _handlers)
            {
                var cfg = kv.Value;
                if (cfg.EventName != "*" && cfg.EventName != eventName) continue;
                try
                {
                    string json;
                    if (cfg.EventName == "*")
                    {
                        if (wildcardJson == null)
                            wildcardJson = JSApiHelper.ToJson(new Dictionary<string, object>
                            {
                                ["name"] = eventName,
                                ["payload"] = payload
                            });
                        json = wildcardJson;
                    }
                    else
                    {
                        if (specificJson == null)
                            specificJson = JSApiHelper.ToJson(payload ?? new Dictionary<string, object>());
                        json = specificJson;
                    }
                    cfg.Handler(json);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(
                        $"[JSApi.StudyRoom] Event handler error ({eventName}): {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                _service.OnStudyRoomEvent -= OnServiceEvent;
                _isSubscribed = false;
            }
            _handlers.Clear();
            _service?.Dispose();
        }

        private sealed class GameEventHandler
        {
            public string EventName;
            public Action<string> Handler;
        }
    }
}
