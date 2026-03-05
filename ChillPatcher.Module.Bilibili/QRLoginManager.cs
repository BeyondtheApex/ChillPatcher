using System;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;

namespace ChillPatcher.Module.Bilibili
{
    public class QRLoginManager
    {
        private readonly BilibiliBridge _bridge;
        private readonly ManualLogSource _logger;
        private CancellationTokenSource _cts;

        public Sprite QRCodeSprite { get; private set; }
        public bool IsSuccess { get; private set; }

        public event Action OnLoginSuccess;
        public event Action<string> OnStatusChanged;
        public event Action OnQRCodeReady;

        public QRLoginManager(BilibiliBridge bridge, ManualLogSource logger)
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
                OnStatusChanged?.Invoke("正在获取二维码...");

                var qrData = await _bridge.GetLoginUrlAsync();
                var imgBytes = await _bridge.GenerateQRBytesAsync(qrData.Url);

                var tex = new Texture2D(2, 2);
                tex.LoadImage(imgBytes);
                QRCodeSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                OnStatusChanged?.Invoke("请使用 Bilibili 扫码");
                OnQRCodeReady?.Invoke();

                while (!token.IsCancellationRequested)
                {
                    if (await _bridge.CheckLoginStatusAsync(qrData.Key))
                    {
                        IsSuccess = true;
                        OnStatusChanged?.Invoke("登录成功！");
                        OnLoginSuccess?.Invoke();
                        break;
                    }
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