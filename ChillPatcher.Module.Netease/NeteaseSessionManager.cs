using System;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace ChillPatcher.Module.Netease
{
    public enum SessionState
    {
        LoggedOut,
        LoggedIn,
        Expired,
        LoggingIn
    }

    public enum TrialRecoveryResult
    {
        Recovered,
        VipRestricted,
        NetworkError
    }

    /// <summary>
    /// 网易云登录会话管理器。
    /// 负责：验证会话 → 静默刷新 → 试听恢复 → QR 重登录 的完整生命周期。
    /// </summary>
    public class NeteaseSessionManager
    {
        private readonly ManualLogSource _logger;
        private readonly NeteaseBridge _bridge;
        private QRLoginManager _qrLoginManager;

        private TaskCompletionSource<bool> _loginTcs;

        public SessionState State { get; private set; } = SessionState.LoggedOut;
        public NeteaseBridge.UserInfo UserInfo { get; private set; }

        /// <summary>
        /// 试听恢复后拿到的完整 URL，供调用方直接使用。
        /// </summary>
        public NeteaseBridge.SongUrl RecoveredSongUrl { get; private set; }

        public event Action<SessionState> OnStateChanged;

        public NeteaseSessionManager(NeteaseBridge bridge, ManualLogSource logger)
        {
            _bridge = bridge;
            _logger = logger;
        }

        /// <summary>
        /// 设置 QRLoginManager（构造时可能尚未就绪）。
        /// </summary>
        public void SetQRLoginManager(QRLoginManager qrLoginManager)
        {
            _qrLoginManager = qrLoginManager;
            _qrLoginManager.OnLoginSuccess += OnQRLoginSuccess;
        }

        #region Public API

        /// <summary>
        /// 启动时校验会话并刷新用户信息。
        /// </summary>
        public async Task<bool> ValidateAndRefreshAsync()
        {
            _logger.LogInfo("[NeteaseSession] Validating session...");

            var result = await Task.Run(() => CallRefreshLogin());

            switch (result)
            {
                case NeteaseBridge.RefreshLoginResult.Success:
                    UpdateUserInfo();
                    SetState(SessionState.LoggedIn);
                    _logger.LogInfo($"[NeteaseSession] Session valid. User: {UserInfo?.Nickname}, VipType: {UserInfo?.VipType}");
                    return true;

                case NeteaseBridge.RefreshLoginResult.AuthFailed:
                    SetState(SessionState.Expired);
                    _logger.LogWarning("[NeteaseSession] Session expired (auth failed)");
                    return false;

                case NeteaseBridge.RefreshLoginResult.NetworkError:
                    _logger.LogWarning("[NeteaseSession] Network error during validation, keeping current state");
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 检测到试听后触发恢复流程：静默刷新 → QR 重登录。
        /// </summary>
        public async Task<TrialRecoveryResult> HandleTrialAsync(
            long songId,
            NeteaseBridge.Quality quality,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"[NeteaseSession] Trial detected for song {songId}, attempting recovery...");

            var previousState = State;

            // Step 1: 静默刷新
            var refreshResult = await Task.Run(() => CallRefreshLogin(), cancellationToken);

            switch (refreshResult)
            {
                case NeteaseBridge.RefreshLoginResult.NetworkError:
                    SetState(previousState);
                    _logger.LogWarning("[NeteaseSession] Network error during trial recovery, skipping");
                    return TrialRecoveryResult.NetworkError;

                case NeteaseBridge.RefreshLoginResult.Success:
                    UpdateUserInfo();
                    SetState(SessionState.LoggedIn);
                    _logger.LogInfo("[NeteaseSession] Silent refresh succeeded, retrying song URL...");
                    return await RetryGetSongUrl(songId, quality, cancellationToken);

                case NeteaseBridge.RefreshLoginResult.AuthFailed:
                    SetState(SessionState.Expired);
                    _logger.LogWarning("[NeteaseSession] Auth failed, triggering QR login...");

                    // Step 2: QR 重登录
                    if (_qrLoginManager == null)
                    {
                        _logger.LogError("[NeteaseSession] QRLoginManager not available, cannot recover");
                        return TrialRecoveryResult.NetworkError;
                    }

                    TriggerQRLogin();

                    try
                    {
                        var loginSuccess = await WaitWithCancellationAsync(_loginTcs.Task, cancellationToken);
                        if (!loginSuccess)
                        {
                            _logger.LogWarning("[NeteaseSession] QR login was not successful");
                            return TrialRecoveryResult.NetworkError;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInfo("[NeteaseSession] Trial recovery cancelled (song changed?)");
                        return TrialRecoveryResult.NetworkError;
                    }

                    _logger.LogInfo("[NeteaseSession] QR login succeeded, retrying song URL...");
                    return await RetryGetSongUrl(songId, quality, cancellationToken);

                default:
                    return TrialRecoveryResult.NetworkError;
            }
        }

        /// <summary>
        /// 主动触发 QR 登录（UI 按钮等场景调用）。
        /// </summary>
        public void TriggerQRLogin()
        {
            _loginTcs = new TaskCompletionSource<bool>();
            SetState(SessionState.LoggingIn);
            _qrLoginManager?.StartLoginAsync();
            _logger.LogInfo("[NeteaseSession] QR login triggered");
        }

        /// <summary>
        /// 登出。
        /// </summary>
        public void Logout()
        {
            _logger.LogInfo("[NeteaseSession] Logging out...");
            var result = _bridge.Logout();
            UserInfo = null;
            RecoveredSongUrl = null;
            SetState(SessionState.Expired);
            _logger.LogInfo($"[NeteaseSession] Logout completed: {(result ? "success" : "failed")}");
        }

        /// <summary>
        /// 外部通知：QR 登录成功（从 NeteaseModule 现有回调调用）。
        /// </summary>
        public void NotifyLoginSuccess()
        {
            UpdateUserInfo();
            SetState(SessionState.LoggedIn);
            _loginTcs?.TrySetResult(true);
            _logger.LogInfo($"[NeteaseSession] Login success notified. User: {UserInfo?.Nickname}");
        }

        /// <summary>
        /// 外部通知：QR 登录失败。
        /// </summary>
        public void NotifyLoginFailed(string reason)
        {
            _loginTcs?.TrySetResult(false);
            _logger.LogWarning($"[NeteaseSession] Login failed: {reason}");
        }

        #endregion

        #region Private

        private void OnQRLoginSuccess()
        {
            NotifyLoginSuccess();
        }

        private async Task<TrialRecoveryResult> RetryGetSongUrl(
            long songId,
            NeteaseBridge.Quality quality,
            CancellationToken cancellationToken)
        {
            var songUrl = await Task.Run(() => _bridge.GetSongUrl(songId, quality), cancellationToken);

            if (songUrl == null)
            {
                _logger.LogWarning($"[NeteaseSession] Retry GetSongUrl returned null for song {songId}");
                return TrialRecoveryResult.NetworkError;
            }

            if (songUrl.IsTrial)
            {
                _logger.LogWarning($"[NeteaseSession] Song {songId} still trial after recovery — VIP restricted");
                return TrialRecoveryResult.VipRestricted;
            }

            RecoveredSongUrl = songUrl;
            _logger.LogInfo($"[NeteaseSession] Song {songId} recovered successfully");
            return TrialRecoveryResult.Recovered;
        }

        /// <summary>
        /// 调用 bridge 的 RefreshLogin()，直接返回枚举结果。
        /// </summary>
        private NeteaseBridge.RefreshLoginResult CallRefreshLogin()
        {
            return _bridge.RefreshLogin();
        }

        private void UpdateUserInfo()
        {
            try
            {
                UserInfo = _bridge.GetUserInfo();
                _logger.LogDebug($"[NeteaseSession] UserInfo updated: {UserInfo?.Nickname}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[NeteaseSession] Failed to update UserInfo: {ex.Message}");
            }
        }

        private void SetState(SessionState newState)
        {
            if (State == newState) return;
            var oldState = State;
            State = newState;
            _logger.LogInfo($"[NeteaseSession] State: {oldState} -> {newState}");
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// net472 没有 Task.WaitAsync(CancellationToken)，用 CancellationToken.Register 模拟。
        /// </summary>
        private static async Task<T> WaitWithCancellationAsync<T>(Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(task, tcs.Task);
                if (completed == tcs.Task)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                return await task;
            }
        }

        #endregion
    }
}
