using System;
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;
using UnityEngine;

namespace ChillPatcher.Module.Netease
{
    /// <summary>
    /// 网易云账户 API：通过 chill.custom.get("netease_account") 访问
    /// 前端 500ms 轮询属性获取登录状态、用户信息、二维码；按钮调用方法触发登录/登出/刷新
    /// </summary>
    public class NeteaseAccountApi : ICustomJSApi
    {
        public string Name => "netease_account";

        private readonly NeteaseSessionManager _sessionManager;
        private readonly ManualLogSource _logger;
        private QRLoginManager _qrLoginManager;

        private string _statusMessage = "";
        private string _qrCodeBase64 = "";

        public NeteaseAccountApi(NeteaseSessionManager sessionManager, ManualLogSource logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <summary>
        /// 绑定 QRLoginManager，订阅状态和二维码更新事件
        /// 因为 QRLoginManager 可能在 AccountApi 构造之后才创建，所以用 setter 注入
        /// </summary>
        public void SetQRLoginManager(QRLoginManager qrLoginManager)
        {
            _qrLoginManager = qrLoginManager;
            _qrLoginManager.OnStatusChanged += msg => _statusMessage = msg ?? "";
            _qrLoginManager.OnQRCodeUpdated += _ => SyncQRCode();

            // 主动同步一次当前状态（QR 可能在绑定前已生成）
            SyncQRCode();
        }

        private void SyncQRCode()
        {
            if (_qrLoginManager == null) return;
            var bytes = _qrLoginManager.QRCodeBytes;
            _qrCodeBase64 = bytes != null && bytes.Length > 0
                ? Convert.ToBase64String(bytes)
                : "";
        }

        #region Properties (read by JS UI via polling)

        public string sessionState => _sessionManager.State switch
        {
            SessionState.LoggedIn => "logged_in",
            SessionState.LoggedOut => "logged_out",
            SessionState.Expired => "expired",
            SessionState.LoggingIn => "logging_in",
            _ => "logged_out"
        };

        public string nickname => _sessionManager.UserInfo?.Nickname ?? "";
        public string avatarUrl => _sessionManager.UserInfo?.AvatarUrl ?? "";
        public int vipType => _sessionManager.UserInfo?.VipType ?? 0;
        public string statusMessage => _statusMessage;
        public string qrCodeBase64 => _qrCodeBase64;

        #endregion

        #region Methods (called by JS UI buttons)

        public void login()
        {
            _logger.LogInfo("[NeteaseAccountApi] Manual login triggered from UI");
            _sessionManager.TriggerQRLogin();
        }

        public void logout()
        {
            _logger.LogInfo("[NeteaseAccountApi] Logout triggered from UI");
            _sessionManager.Logout();
            _statusMessage = "";
            _qrCodeBase64 = "";
        }

        public async void refreshLogin()
        {
            _logger.LogInfo("[NeteaseAccountApi] Manual session refresh triggered");
            var result = await _sessionManager.ValidateAndRefreshAsync();
            _logger.LogDebug($"[NeteaseAccountApi] Session refresh result: {(result ? "success" : "failed")}");
        }

        #endregion
    }
}
