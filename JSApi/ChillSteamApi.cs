using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ChillPatcher.Patches;
using Steamworks;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// Steam 状态与玩家信息 API
    /// JS 端通过 chill.steam 访问
    ///
    /// 方法：
    ///   chill.steam.getStatus()      — 获取连接状态 JSON
    ///   chill.steam.getUserInfo()    — 获取当前登录玩家信息
    ///   chill.steam.isGameOwned()    — 当前玩家是否拥有本游戏
    ///   chill.steam.isGameOwned(appId) — 是否拥有指定AppID的游戏
    ///   chill.steam.initRelayNetwork() — 初始化 Steam Datagram Relay (SDR)
    ///   chill.steam.onConnected(fn)  — 订阅Steam连接成功事件，返回token
    ///   chill.steam.off(token)       — 取消订阅
    /// </summary>
    public class ChillSteamApi
    {
        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, Action<string>> _handlers
            = new Dictionary<string, Action<string>>();

        public ChillSteamApi(ManualLogSource logger)
        {
            _logger = logger;
            SteamConnectionState.OnSteamConnected += OnSteamConnectedInternal;
        }

        private void OnSteamConnectedInternal()
        {
            if (_handlers.Count == 0) return;
            var statusJson = getStatus();
            foreach (var handler in _handlers.Values)
            {
                try { handler(statusJson); }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[SteamApi] onConnected 事件处理器出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取当前 Steam 连接状态。
        /// 返回 JSON 示例：
        /// { state:"connected", isConnected:true, userId:"76561...",
        ///   personaName:"玩家名", isGameOwned:true, silentStartEnabled:true }
        /// </summary>
        public string getStatus()
        {
            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["state"] = SteamConnectionState.CurrentState.ToString().ToLower(),
                ["isConnected"] = SteamConnectionState.IsSteamActuallyInitialized,
                ["userId"] = SteamConnectionState.SteamUserId,
                ["personaName"] = SteamConnectionState.PersonaName,
                ["isGameOwned"] = SteamConnectionState.IsGameOwned,
                ["wallpaperModeEnabled"] = PluginConfig.EnableWallpaperEngineMode.Value
            });
        }

        /// <summary>
        /// 获取当前登录玩家的详细信息。
        /// Steam 未连接时返回 { available:false, reason:"pending" }。
        /// </summary>
        public string getUserInfo()
        {
            if (!SteamConnectionState.IsSteamActuallyInitialized)
            {
                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["available"] = false,
                    ["reason"] = SteamConnectionState.CurrentState.ToString().ToLower()
                });
            }

            try
            {
                var steamId = SteamUser.GetSteamID();
                var personaState = SteamFriends.GetFriendPersonaState(steamId);
                bool isOnline = personaState == EPersonaState.k_EPersonaStateOnline
                             || personaState == EPersonaState.k_EPersonaStateAway
                             || personaState == EPersonaState.k_EPersonaStateBusy;

                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["available"] = true,
                    ["userId"] = steamId.ToString(),
                    ["personaName"] = SteamFriends.GetPersonaName(),
                    ["onlineState"] = personaState.ToString(),
                    ["isOnline"] = isOnline,
                    ["isGameOwned"] = SteamApps.BIsSubscribed(),
                    ["avatarHandle"] = (int)SteamFriends.GetMediumFriendAvatar(steamId)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[SteamApi] 获取玩家信息失败: {ex.Message}");
                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["available"] = false,
                    ["error"] = ex.Message
                });
            }
        }

        /// <summary>
        /// 检查当前玩家是否拥有指定游戏。
        /// appId = 0（默认）表示检查本游戏。
        /// Steam 未连接时返回 false。
        /// </summary>
        public bool isGameOwned(uint appId = 0)
        {
            if (!SteamConnectionState.IsSteamActuallyInitialized) return false;
            try
            {
                return appId == 0
                    ? SteamApps.BIsSubscribed()
                    : SteamApps.BIsSubscribedApp(new AppId_t(appId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[SteamApi] isGameOwned 查询失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 初始化 Steam Datagram Relay (SDR) 网络层。
        /// SDR 用于 P2P 联机时通过 Valve 中继服务器建立连接。
        /// 返回 { success:true } 或 { success:false, error:"..." }。
        /// </summary>
        public string initRelayNetwork()
        {
            if (!SteamConnectionState.IsSteamActuallyInitialized)
            {
                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Steam not connected"
                });
            }
            try
            {
                SteamNetworkingUtils.InitRelayNetworkAccess();
                _logger.LogInfo("[SteamApi] SDR RelayNetworkAccess 已初始化");
                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["success"] = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[SteamApi] SDR 初始化失败: {ex.Message}");
                return JSApiHelper.ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                });
            }
        }

        /// <summary>
        /// 订阅 Steam 连接成功事件。
        /// 回调参数为 getStatus() 返回的 JSON 字符串。
        /// 返回可用于 off() 的 token。
        /// </summary>
        public string onConnected(Action<string> handler)
        {
            if (handler == null) return string.Empty;
            var token = Guid.NewGuid().ToString("N");
            _handlers[token] = handler;
            return token;
        }

        /// <summary>取消指定 token 的事件订阅。</summary>
        public bool off(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            return _handlers.Remove(token);
        }
    }
}
