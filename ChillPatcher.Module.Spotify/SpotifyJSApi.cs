using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;
using Newtonsoft.Json;

namespace ChillPatcher.Module.Spotify
{
    /// <summary>
    /// Spotify 模块的 JS API，供 OneJS 前端访问。
    /// JS 端通过 chill.custom.get("spotify") 获取此实例。
    /// </summary>
    public class SpotifyJSApi : ICustomJSApi
    {
        public string Name => "spotify";

        private readonly ManualLogSource _logger;

        // --- 状态由 SpotifyModule 设置 ---

        /// <summary>当前登录状态文本（JS 端轮询显示）</summary>
        public string loginStatus { get; set; } = "";

        /// <summary>是否已登录</summary>
        public bool isLoggedIn { get; set; }

        /// <summary>是否需要配置 Client ID</summary>
        public bool needsClientId { get; set; }

        /// <summary>是否正在登录流程中</summary>
        public bool isLoggingIn { get; set; }

        /// <summary>当前用户名</summary>
        public string userName { get; set; } = "";

        /// <summary>账户类型 (premium/free)</summary>
        public string accountType { get; set; } = "";

        /// <summary>当前选中设备名称</summary>
        public string activeDeviceName { get; set; } = "";

        /// <summary>当前选中设备 ID</summary>
        public string activeDeviceId { get; set; } = "";

        /// <summary>是否正在加载设备列表</summary>
        public bool isLoadingDevices { get; set; }

        /// <summary>最新的设备列表 JSON</summary>
        public string devicesJson { get; set; } = "[]";

        /// <summary>是否显示配置面板</summary>
        public bool showConfigPanel { get; set; }

        /// <summary>是否显示设备选择面板</summary>
        public bool showDevicePanel { get; set; }

        // --- 回调委托，由 SpotifyModule 注册 ---

        public event Action<string> OnClientIdSubmitted;
        public event Action OnConfigCancelled;
        public event Action<string> OnDeviceSelected;
        public event Action OnDevicePanelCancelled;
        public event Action OnLoginRequested;
        public event Action OnLogoutRequested;
        public event Action OnRefreshDevicesRequested;

        public SpotifyJSApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        // =====================================================================
        // JS 调用的方法
        // =====================================================================

        /// <summary>提交 Client ID（从配置面板）</summary>
        public void submitClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return;
            _logger.LogInfo($"[SpotifyJSApi] Client ID submitted");
            showConfigPanel = false;
            OnClientIdSubmitted?.Invoke(clientId.Trim());
        }

        /// <summary>取消配置</summary>
        public void cancelConfig()
        {
            _logger.LogInfo("[SpotifyJSApi] Config cancelled");
            showConfigPanel = false;
            OnConfigCancelled?.Invoke();
        }

        /// <summary>选择设备</summary>
        public void selectDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            _logger.LogInfo($"[SpotifyJSApi] Device selected: {deviceId}");
            showDevicePanel = false;
            OnDeviceSelected?.Invoke(deviceId);
        }

        /// <summary>取消设备选择</summary>
        public void cancelDeviceSelection()
        {
            _logger.LogInfo("[SpotifyJSApi] Device selection cancelled");
            showDevicePanel = false;
            OnDevicePanelCancelled?.Invoke();
        }

        /// <summary>请求启动 OAuth 登录</summary>
        public void requestLogin()
        {
            _logger.LogInfo("[SpotifyJSApi] Login requested");
            OnLoginRequested?.Invoke();
        }

        /// <summary>请求登出</summary>
        public void requestLogout()
        {
            _logger.LogInfo("[SpotifyJSApi] Logout requested");
            OnLogoutRequested?.Invoke();
        }

        /// <summary>请求刷新设备列表</summary>
        public void refreshDevices()
        {
            _logger.LogInfo("[SpotifyJSApi] Refresh devices requested");
            OnRefreshDevicesRequested?.Invoke();
        }

        /// <summary>打开设备选择面板</summary>
        public void openDevicePanel()
        {
            showDevicePanel = true;
        }

        /// <summary>打开配置面板</summary>
        public void openConfigPanel()
        {
            showConfigPanel = true;
        }
    }
}
