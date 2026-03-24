using System;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;

namespace ChillPatcher.Module.QQMusic
{
    /// <summary>
    /// QQ音乐二维码登录管理器
    /// 参照网易云模块的 QRLoginManager 实现
    /// </summary>
    public class QRLoginManager
    {
        private readonly QQMusicBridge _bridge;
        private readonly ManualLogSource _logger;
        private string _loginType; // "qq" 或 "wx"

        private QQMusicBridge.QRLoginState _currentState;
        private Sprite _qrCodeSprite;
        private byte[] _qrCodeBytes;
        private bool _isPolling;
        private CancellationTokenSource _pollingCts;

        public event Action OnLoginSuccess;
        public event Action<Sprite> OnQRCodeUpdated;
        public event Action<string> OnStatusChanged;
        public event Action<string> OnLoginFailed;

        public Sprite QRCodeSprite => _qrCodeSprite;
        public byte[] QRCodeBytes => _qrCodeBytes;
        public bool IsWaitingForLogin => _isPolling && _currentState != null;
        public string StatusMessage => _currentState?.Msg ?? "未开始";
        public string LoginType => _loginType;

        public QRLoginManager(QQMusicBridge bridge, ManualLogSource logger)
        {
            _bridge = bridge;
            _logger = logger;
        }

        /// <summary>
        /// 开始二维码登录流程
        /// </summary>
        /// <param name="loginType">登录类型: "qq" 或 "wx"</param>
        public async Task<bool> StartLoginAsync(string loginType = "qq")
        {
            try
            {
                _loginType = loginType;
                CancelPolling();
                CleanupQRCodeResources();

                // 同步调用 Go DLL 获取 QR 码（必须在主线程，因为后续创建 Texture2D/Sprite 需要主线程）
                // 切换 QQ/微信扫码时会有短暂卡顿，这是正常的
                var base64Png = _bridge.GetQRImage(loginType);
                if (string.IsNullOrEmpty(base64Png))
                {
                    _logger.LogError("[QRLoginManager] 获取二维码失败");
                    OnLoginFailed?.Invoke("获取二维码失败: " + (_bridge.GetLastError() ?? "unknown"));
                    return false;
                }

                // 在当前线程创建 Texture2D + Sprite（必须在主线程）
                LoadQRCodeFromBase64(base64Png);

                _currentState = new QQMusicBridge.QRLoginState { Code = 66, Msg = "等待扫码" };
                var hint = loginType == "wx" ? "请使用微信扫码登录" : "请使用 QQ 扫码登录";
                OnStatusChanged?.Invoke(hint);
                OnQRCodeUpdated?.Invoke(_qrCodeSprite);

                // 开始轮询
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
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1500, cancellationToken);

                    // 在后台线程执行（微信长轮询可能耗时40秒）
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
                        _logger.LogInfo("[QRLoginManager] 登录成功！");
                        _isPolling = false;
                        OnLoginSuccess?.Invoke();
                        return;
                    }
                    else if (status.IsExpired)
                    {
                        _logger.LogInfo("[QRLoginManager] 二维码已失效，重新生成...");
                        await StartLoginAsync(_loginType);
                        return; // 新的轮询任务已启动
                    }
                    // IsWaitingScan 和 IsWaitingConfirm 继续轮询
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("[QRLoginManager] 轮询已取消");
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
            if (_qrCodeSprite != null && _qrCodeSprite.texture != null)
            {
                UnityEngine.Object.Destroy(_qrCodeSprite.texture);
                UnityEngine.Object.Destroy(_qrCodeSprite);
                _qrCodeSprite = null;
            }
            _qrCodeBytes = null;
        }

        /// <summary>
        /// 从 base64 PNG 加载二维码
        /// </summary>
        private void LoadQRCodeFromBase64(string base64Png)
        {
            try
            {
                _qrCodeBytes = System.Convert.FromBase64String(base64Png);

                var texture = new Texture2D(2, 2);
                texture.LoadImage(_qrCodeBytes);
                texture.filterMode = FilterMode.Point;

                _qrCodeSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));

                _logger.LogInfo($"[QRLoginManager] 二维码加载成功: {texture.width}x{texture.height}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[QRLoginManager] 加载二维码失败: {ex}");
            }
        }
    }
}
