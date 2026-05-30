using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using Steamworks;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// Steam Networking Messages P2P 传输层
    /// 负责底层消息收发，不关心业务逻辑
    /// </summary>
    public static class P2PTransport
    {
        private static ManualLogSource _log;
        private static bool _listening;

        /// <summary>
        /// 收到消息时触发: (senderSteamId, rawData)
        /// </summary>
        public static event Action<CSteamID, byte[]> OnMessageReceived;

        /// <summary>
        /// 当前已知的对端列表 (steamId → 最后心跳时间)
        /// </summary>
        private static readonly Dictionary<CSteamID, float> _peers = new Dictionary<CSteamID, float>();

        public static IReadOnlyDictionary<CSteamID, float> Peers => _peers;

        public static void Initialize(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>
        /// 开始监听 P2P 消息
        /// </summary>
        public static void StartListening()
        {
            if (_listening) return;
            _listening = true;
            _log?.LogInfo("[P2P] Started listening");
        }

        /// <summary>
        /// 停止监听并清理
        /// </summary>
        public static void StopListening()
        {
            if (!_listening) return;
            _listening = false;

            // 关闭所有对端会话
            foreach (var peer in _peers.Keys)
            {
                CloseSession(peer);
            }
            _peers.Clear();

            _log?.LogInfo("[P2P] Stopped listening");
        }

        /// <summary>
        /// 注册一个对端
        /// </summary>
        public static void AddPeer(CSteamID steamId)
        {
            _peers[steamId] = UnityEngine.Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 移除一个对端
        /// </summary>
        public static void RemovePeer(CSteamID steamId)
        {
            _peers.Remove(steamId);
            CloseSession(steamId);
        }

        /// <summary>
        /// 更新对端心跳时间
        /// </summary>
        public static void UpdatePeerHeartbeat(CSteamID steamId)
        {
            if (_peers.ContainsKey(steamId))
                _peers[steamId] = UnityEngine.Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 发送 SyncMessage 给指定对端
        /// </summary>
        public static bool SendMessage(CSteamID target, SyncMessage msg)
        {
            if (!_listening) return false;

            var data = SyncProtocol.Serialize(msg);
            var sendFlags = SyncProtocol.IsReliable(msg.Type)
                ? Constants.k_nSteamNetworkingSend_Reliable
                : Constants.k_nSteamNetworkingSend_Unreliable;

            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID(target);

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var result = SteamNetworkingMessages.SendMessageToUser(
                    ref identity, handle.AddrOfPinnedObject(), (uint)data.Length, sendFlags, StudyRoomConfig.P2PChannel);

                if (result != EResult.k_EResultOK)
                {
                    _log?.LogWarning($"[P2P] SendMessage failed to {target}: {result}");
                    return false;
                }
                return true;
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// 发送 SyncMessage 给所有已注册的对端
        /// </summary>
        public static void BroadcastMessage(SyncMessage msg, CSteamID? except = null)
        {
            foreach (var peer in _peers.Keys)
            {
                if (except.HasValue && peer == except.Value) continue;
                SendMessage(peer, msg);
            }
        }

        /// <summary>
        /// 每帧调用，从 Steam 消息队列取出所有待处理消息
        /// </summary>
        public static void PollMessages()
        {
            if (!_listening) return;

            var msgPtrs = new IntPtr[StudyRoomConfig.MaxMessagesPerPoll];
            int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(
                StudyRoomConfig.P2PChannel, msgPtrs, StudyRoomConfig.MaxMessagesPerPoll);

            for (int i = 0; i < count; i++)
            {
                var nativeMsg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msgPtrs[i]);
                try
                {
                    var senderId = nativeMsg.m_identityPeer.GetSteamID();
                    var dataSize = (int)nativeMsg.m_cbSize;

                    if (dataSize <= 0 || dataSize > StudyRoomConfig.MaxMessageSize) continue;

                    var data = new byte[dataSize];
                    Marshal.Copy(nativeMsg.m_pData, data, 0, dataSize);

                    OnMessageReceived?.Invoke(senderId, data);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[P2P] Error processing message: {ex.Message}");
                }
                finally
                {
                    SteamNetworkingMessage_t.Release(msgPtrs[i]);
                }
            }
        }

        /// <summary>
        /// 获取所有超时的对端 (超过 DisconnectTimeoutSeconds 无心跳)
        /// </summary>
        public static List<CSteamID> GetTimedOutPeers()
        {
            var result = new List<CSteamID>();
            var now = UnityEngine.Time.realtimeSinceStartup;
            foreach (var kv in _peers)
            {
                if (now - kv.Value > StudyRoomConfig.DisconnectTimeoutSeconds)
                    result.Add(kv.Key);
            }
            return result;
        }

        private static void CloseSession(CSteamID peer)
        {
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID(peer);
            SteamNetworkingMessages.CloseSessionWithUser(ref identity);
        }

        public static void Reset()
        {
            StopListening();
            OnMessageReceived = null;
        }
    }
}
