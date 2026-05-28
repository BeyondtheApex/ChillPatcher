using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using QRCoder;


namespace OmniMixPlayer.Module.QQMusic
{
    public class QRLoginManager
    {
        private readonly QQMusicBridge _bridge;
        private readonly ILogger _logger;
        private string _loginType;

        private QQMusicBridge.QRLoginState _currentState;
        private byte[] _qrCodeBytes;
        private bool _isPolling;
        private CancellationTokenSource _pollingCts;

        private static readonly TimeSpan POLLING_TIMEOUT = TimeSpan.FromMinutes(2);

        public event Action OnLoginSuccess;
        public event Action<(byte[] data, string mimeType)> OnQRCodeUpdated;
        public event Action<string> OnStatusChanged;
        public event Action<string> OnLoginFailed;

        public byte[] QRCodeBytes => _qrCodeBytes;
        public bool IsWaitingForLogin => _isPolling && _currentState != null;
        public string StatusMessage => _currentState?.Msg ?? "未开始";
        public string LoginType => _loginType;

        public QRLoginManager(QQMusicBridge bridge, ILogger logger)
        {
            _bridge = bridge;
            _logger = logger;
        }

        public async Task<bool> StartLoginAsync(string loginType = "qq")
        {
            try
            {
                _loginType = loginType;
                CancelPolling();
                CleanupQRCodeResources();

                var base64Png = _bridge.GetQRImage(loginType);
                if (string.IsNullOrEmpty(base64Png))
                {
                    _logger.LogError("[QRLoginManager] 获取二维码失败");
                    OnLoginFailed?.Invoke("获取二维码失败: " + (_bridge.GetLastError() ?? "unknown"));
                    return false;
                }

                _qrCodeBytes = Convert.FromBase64String(base64Png);

                _currentState = new QQMusicBridge.QRLoginState { Code = 66, Msg = "等待扫码" };
                var hint = loginType == "wx" ? "请使用微信扫码登录" : "请使用 QQ 扫码登录";
                OnStatusChanged?.Invoke(hint);
                OnQRCodeUpdated?.Invoke((_qrCodeBytes, "image/png"));

                _pollingCts = new CancellationTokenSource();
                _isPolling = true;
                _ = PollLoginStatusAsync(_pollingCts.Token);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[QRLoginManager] StartLoginAsync exception: {ex}");
                OnLoginFailed?.Invoke("启动登录失败: " + ex.Message);
                return false;
            }
        }

        private async Task PollLoginStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // 检查 2 分钟超时
                    if (DateTime.UtcNow - startTime >= POLLING_TIMEOUT)
                    {
                        _logger.LogInformation("[QRLoginManager] 登录轮询超时，已停止");
                        _currentState = new QQMusicBridge.QRLoginState { Code = -1, Msg = "登录超时，请刷新二维码重试" };
                        OnStatusChanged?.Invoke(_currentState.Msg);
                        OnLoginFailed?.Invoke(_currentState.Msg);
                        return;
                    }

                    await Task.Delay(1500, cancellationToken);

                    var status = await Task.Run(() => _bridge.CheckQRStatus(), cancellationToken);
                    if (status == null)
                    {
                        _logger.LogWarning("[QRLoginManager] 检查状态失败");
                        continue;
                    }

                    _currentState = status;
                    OnStatusChanged?.Invoke(status.Msg);

                    if (status.IsSuccess)
                    {
                        _logger.LogInformation("[QRLoginManager] 登录成功！");
                        _isPolling = false;
                        OnLoginSuccess?.Invoke();
                        return;
                    }
                    else if (status.IsExpired)
                    {
                        _logger.LogInformation("[QRLoginManager] 二维码已过期，停止轮询");
                        OnStatusChanged?.Invoke("二维码已过期，请刷新重试");
                        OnLoginFailed?.Invoke("二维码已过期，请刷新重试");
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[QRLoginManager] 轮询已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[QRLoginManager] 轮询异常: {ex}");
                OnLoginFailed?.Invoke("登录过程出错: " + ex.Message);
            }
            finally
            {
                _isPolling = false;
            }
        }

        public void CancelLogin()
        {
            CancelPolling();
            _bridge.CancelQRLogin();
            _currentState = null;
            CleanupQRCodeResources();
        }

        private void CancelPolling()
        {
            if (_pollingCts != null)
            {
                _pollingCts.Cancel();
                _pollingCts.Dispose();
                _pollingCts = null;
            }
            _isPolling = false;
        }

        private void CleanupQRCodeResources()
        {
            _qrCodeBytes = null;
        }
    }
}
