using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Module.Bilibili
{
    public class QRLoginManager
    {
        private readonly BilibiliBridge _bridge;
        private readonly ILogger _logger;
        private CancellationTokenSource _cts;
        private bool _isPolling;
        private string _statusMessage = "未开始";

        public byte[] QRCodeBytes { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool IsPollingActive => _isPolling;
        public string StatusMessage => _statusMessage;

        public event Action OnLoginSuccess;
        public event Action<string> OnStatusChanged;
        public event Action OnQRCodeReady;

        private static readonly TimeSpan POLLING_TIMEOUT = TimeSpan.FromMinutes(2);

        public QRLoginManager(BilibiliBridge bridge, ILogger logger)
        {
            _bridge = bridge;
            _logger = logger;
        }

        public async void StartLogin()
        {
            Stop();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _isPolling = true;

            try
            {
                _statusMessage = "请使用B站扫码登录";
                OnStatusChanged?.Invoke(_statusMessage);

                var qrData = await _bridge.GetLoginUrlAsync();
                var imgBytes = await _bridge.GenerateQRBytesAsync(qrData.Url);

                QRCodeBytes = imgBytes;

                OnStatusChanged?.Invoke(_statusMessage);
                OnQRCodeReady?.Invoke();

                var startTime = DateTime.UtcNow;

                while (!token.IsCancellationRequested)
                {
                    // 检查 2 分钟超时
                    if (DateTime.UtcNow - startTime >= POLLING_TIMEOUT)
                    {
                        _logger.LogInformation("[QRLoginManager] 登录轮询超时，已停止");
                        _statusMessage = "登录超时，请刷新二维码重试";
                        OnStatusChanged?.Invoke(_statusMessage);
                        return;
                    }

                    var statusCode = await _bridge.CheckLoginStatusAsync(qrData.Key);

                    if (statusCode == 0)
                    {
                        IsSuccess = true;
                        _statusMessage = "登录成功！";
                        OnStatusChanged?.Invoke(_statusMessage);
                        OnLoginSuccess?.Invoke();
                        break;
                    }
                    else if (statusCode == 86038)
                    {
                        _logger.LogInformation("[QRLoginManager] B站二维码已过期，停止轮询");
                        _statusMessage = "二维码已过期，请刷新重试";
                        OnStatusChanged?.Invoke(_statusMessage);
                        return;
                    }
                    else if (statusCode == 86090)
                    {
                        _statusMessage = "已扫码，请在手机上确认";
                        OnStatusChanged?.Invoke(_statusMessage);
                    }
                    // 86101 = 未扫码，继续轮询

                    await Task.Delay(3000, token);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    _statusMessage = "错误: " + ex.Message;
                    OnStatusChanged?.Invoke(_statusMessage);
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            _isPolling = false;
        }
    }
}