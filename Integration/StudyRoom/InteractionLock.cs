using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using Steamworks;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// 点击互动独占锁 (分布式互斥)
    /// 主机端管理，确保同一时间只有一个玩家与角色互动
    /// </summary>
    public class InteractionLock
    {
        private readonly ManualLogSource _log;

        /// <summary>当前持有锁的玩家 (null = 空闲)</summary>
        private CSteamID? _holder;

        /// <summary>当前锁的交互类型</summary>
        private string _lockType;

        /// <summary>锁获取时间 (用于超时自动释放)</summary>
        private float _lockTime;

        /// <summary>当前是否有人占用</summary>
        public bool IsLocked => _holder.HasValue;

        /// <summary>当前持有者</summary>
        public CSteamID? Holder => _holder;

        public InteractionLock(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>
        /// 尝试获取互动锁 (主机端调用)
        /// </summary>
        /// <returns>true=获取成功</returns>
        public bool TryAcquire(CSteamID requester, string type)
        {
            // 检查超时自动释放
            CheckTimeout();

            if (_holder.HasValue)
            {
                _log?.LogInfo($"[InteractionLock] Denied {requester} ({type}): held by {_holder.Value}");
                return false;
            }

            _holder = requester;
            _lockType = type;
            _lockTime = UnityEngine.Time.realtimeSinceStartup;
            _log?.LogInfo($"[InteractionLock] Granted to {requester} ({type})");
            return true;
        }

        /// <summary>
        /// 释放互动锁 (主机端调用)
        /// </summary>
        public void Release(string type)
        {
            if (!_holder.HasValue) return;
            _log?.LogInfo($"[InteractionLock] Released ({type})");
            _holder = null;
            _lockType = null;
        }

        /// <summary>
        /// 强制释放 (玩家断线时)
        /// </summary>
        public void ForceRelease(CSteamID steamId)
        {
            if (_holder.HasValue && _holder.Value == steamId)
            {
                _log?.LogInfo($"[InteractionLock] Force released (player {steamId} disconnected)");
                _holder = null;
                _lockType = null;
            }
        }

        /// <summary>
        /// 检查超时自动释放
        /// </summary>
        private void CheckTimeout()
        {
            if (!_holder.HasValue) return;
            var elapsed = UnityEngine.Time.realtimeSinceStartup - _lockTime;
            if (elapsed > StudyRoomConfig.InteractionLockTimeoutSeconds)
            {
                _log?.LogWarning($"[InteractionLock] Timeout, force releasing from {_holder.Value}");
                _holder = null;
                _lockType = null;
            }
        }

        /// <summary>
        /// 处理客户端的 InteractionRequest (主机端调用)
        /// </summary>
        public void HandleRequest(CSteamID sender, JObject payload)
        {
            var requestId = payload.Value<string>("requestId") ?? "";
            var type = payload.Value<string>("type") ?? "ClickHeroine";

            if (TryAcquire(sender, type))
            {
                // 授权
                var grant = SyncProtocol.Create(SyncMessageType.InteractionGrant,
                    new Dictionary<string, object>
                    {
                        ["requestId"] = requestId,
                        ["playerId"] = sender.m_SteamID.ToString()
                    });
                P2PTransport.BroadcastMessage(grant);
            }
            else
            {
                // 拒绝
                var deny = SyncProtocol.Create(SyncMessageType.InteractionDeny,
                    new Dictionary<string, object>
                    {
                        ["requestId"] = requestId,
                        ["reason"] = "occupied"
                    });
                P2PTransport.SendMessage(sender, deny);
            }
        }

        public void Reset()
        {
            _holder = null;
            _lockType = null;
        }
    }
}