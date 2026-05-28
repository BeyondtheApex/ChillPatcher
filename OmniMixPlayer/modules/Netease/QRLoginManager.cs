using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using QRCoder;


namespace OmniMixPlayer.Module.Netease
{
    /// <summary>
    /// 二维码登录管理器
    /// 负责生成登录二维码、轮询登录状态
    /// </summary>
    public class QRLoginManager
    {
        private readonly NeteaseBridge _bridge;
        private readonly ILogger _logger;

        private NeteaseBridge.QRLoginState _currentState;
        private byte[] _qrCodeBytes;
        private bool _isPolling;
        private CancellationTokenSource _pollingCts;

        private static readonly TimeSpan POLLING_TIMEOUT = TimeSpan.FromMinutes(2);

        /// <summary>
        /// 登录成功事件
        /// </summary>
        public event Action OnLoginSuccess;

        /// <summary>
        /// 二维码更新事件 (新二维码生成时触发)
        /// </summary>
        public event Action<byte[]> OnQRCodeUpdated;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event Action<string> OnStatusChanged;

        /// <summary>
        /// 登录失败/取消事件
        /// </summary>
        public event Action<string> OnLoginFailed;

        /// <summary>
        /// 当前二维码字节数据 (用于 SMTC / UI)
        /// </summary>
        public byte[] QRCodeBytes => _qrCodeBytes;

        /// <summary>
        /// 是否正在等待登录
        /// </summary>
        public bool IsWaitingForLogin => _isPolling && _currentState != null;

        /// <summary>
        /// 当前状态消息
        /// </summary>
        public string StatusMessage => _currentState?.StatusMsg ?? "未开始";

        public QRLoginManager(NeteaseBridge bridge, ILogger logger)
        {
            _bridge = bridge;
            _logger = logger;
        }

        /// <summary>
        /// 开始二维码登录流程
        /// </summary>
        public async Task<bool> StartLoginAsync()
        {
            try
            {
                // 取消之前的轮询
                CancelPolling();

                // 清理旧的二维码资源（每次重新开始时都清理）
                CleanupQRCodeResources();

                // 获取二维码（同步调用 Go DLL）
                _currentState = _bridge.StartQRLogin();
                if (_currentState == null || string.IsNullOrEmpty(_currentState.QRCodeURL))
                {
                    _logger.LogError("[QRLoginManager] 获取二维码失败");
                    OnLoginFailed?.Invoke("获取二维码失败");
                    return false;
                }

                // 生成二维码图片
                GenerateQRCodeBytes(_currentState.QRCodeURL);

                OnStatusChanged?.Invoke("请使用网易云音乐 APP 扫码");
                OnQRCodeUpdated?.Invoke(_qrCodeBytes);

                // 开始轮询
                _pollingCts = new CancellationTokenSource();
                _isPolling = true;

                // 启动轮询任务
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

        /// <summary>
        /// 轮询登录状态
        /// </summary>
        private async Task PollLoginStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                int failCount = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // 检查 2 分钟超时
                    if (DateTime.UtcNow - startTime >= POLLING_TIMEOUT)
                    {
                        _logger.LogInformation("[QRLoginManager] 登录轮询超时，已停止");
                        _currentState = new NeteaseBridge.QRLoginState { StatusCode = -1, StatusMsg = "登录超时，请刷新二维码重试" };
                        OnStatusChanged?.Invoke(_currentState.StatusMsg);
                        OnLoginFailed?.Invoke(_currentState.StatusMsg);
                        return;
                    }

                    await Task.Delay(1500, cancellationToken); // 每 1.5 秒检查一次

                    var status = await Task.Run(() => _bridge.CheckQRLoginStatus(), cancellationToken);
                    if (status == null)
                    {
                        failCount++;
                        _logger.LogWarning($"[QRLoginManager] 检查状态失败 ({failCount})");
                        if (failCount >= 60)
                        {
                            _logger.LogInformation("[QRLoginManager] 连续失败过多，停止轮询");
                            OnLoginFailed?.Invoke("检查登录状态失败，请刷新重试");
                            return;
                        }
                        continue;
                    }
                    failCount = 0;

                    _currentState = status;
                    OnStatusChanged?.Invoke(status.StatusMsg);

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
                    // IsWaitingScan 和 IsWaitingConfirm 继续轮询
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

        /// <summary>
        /// 取消登录
        /// </summary>
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

        /// <summary>
        /// 清理二维码资源
        /// </summary>
        private void CleanupQRCodeResources()
        {
            _qrCodeBytes = null;
        }

        /// <summary>
        /// 生成二维码字节数据
        /// </summary>
        private void GenerateQRCodeBytes(string url)
        {
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        _qrCodeBytes = qrCode.GetGraphic(10); // 10 像素每模块
                    }
                }

                _logger.LogInformation($"[QRLoginManager] 二维码生成成功: {_qrCodeBytes.Length} bytes");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[QRLoginManager] 生成二维码失败: {ex}");
            }
        }
    }
}
