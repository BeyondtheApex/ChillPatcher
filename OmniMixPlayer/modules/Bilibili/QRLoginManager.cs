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

        public byte[] QRCodeBytes { get; private set; }
        public bool IsSuccess { get; private set; }

        public event Action OnLoginSuccess;
        public event Action<string> OnStatusChanged;
        public event Action OnQRCodeReady;

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

            try
            {
                OnStatusChanged?.Invoke("请使用B站扫码登录");

                var qrData = await _bridge.GetLoginUrlAsync();
                var imgBytes = await _bridge.GenerateQRBytesAsync(qrData.Url);

                QRCodeBytes = imgBytes;

                OnStatusChanged?.Invoke("请使用B站扫码登录");
                OnQRCodeReady?.Invoke();

                while (!token.IsCancellationRequested)
                {
                    var statusCode = await _bridge.CheckLoginStatusAsync(qrData.Key);

                    if (statusCode == 0)
                    {
                        IsSuccess = true;
                        OnStatusChanged?.Invoke("登录成功！");
                        OnLoginSuccess?.Invoke();
                        break;
                    }
                    else if (statusCode == 86038)
                    {
                        // 二维码已过期，重新获取
                        _logger.LogInformation("[QRLoginManager] B站二维码已过期，重新获取...");
                        OnStatusChanged?.Invoke("二维码已过期，正在刷新...");
                        StartLogin();
                        return; // 新的登录流程已启动
                    }
                    else if (statusCode == 86090)
                    {
                        OnStatusChanged?.Invoke("已扫码，请在手机上确认");
                    }
                    // 86101 = 未扫码，继续轮询

                    await Task.Delay(3000, token);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    OnStatusChanged?.Invoke("错误: " + ex.Message);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }
    }
}