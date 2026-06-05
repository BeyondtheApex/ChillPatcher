using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniMixPlayer.SDK;
using OmniMixPlayer.SDK.Attributes;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Module.Bilibili
{
    [MusicModule("com.chillpatcher.bilibili", "Bilibili Music",
        Version = "1.0.0",
        Author = "xgqq",
        Description = "Bilibili video audio streaming")]
    public class BilibiliModule : IMusicModule, IStreamingMusicSourceProvider, ICoverProvider, IModuleUIProvider
    {
        public string ModuleId => "com.chillpatcher.bilibili";
        public string DisplayName => "Bilibili Music";
        public string Version => "1.0.0";
        public int Priority => 10;
        public ModuleCapabilities Capabilities => new ModuleCapabilities { CanDelete = false, CanFavorite = false, CanExclude = false, ProvidesCover = true };
        public SourceType SourceType => SourceType.Stream;

        public bool IsReady => true;
        public event Action<bool> OnReadyStateChanged;

        private IModuleContext _context;
        private BilibiliBridge _bridge;
        private QRLoginManager _qrManager;
        private BilibiliSongRegistry _registry;

        private Dictionary<string, (byte[] data, string mimeType)> _spriteCache = new Dictionary<string, (byte[] data, string mimeType)>();

        // UI 相关
        private long _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            string dataPath = context.GetModuleDataPath(ModuleId);
            Directory.CreateDirectory(dataPath);

            int pageDelay = context.ConfigManager.GetValue<int>("PageLoadDelay", 300);
            context.Logger.LogInformation($"[{DisplayName}] 读取配置: 翻页延迟 = {pageDelay}ms");

            _bridge = new BilibiliBridge(context.Logger, dataPath, pageDelay);
            _registry = new BilibiliSongRegistry(context, ModuleId);
            _qrManager = new QRLoginManager(_bridge, context.Logger);

            _qrManager.OnLoginSuccess += async () =>
            {
                context.Logger.LogInformation($"[{DisplayName}] 扫码登录成功！");

                // 加载音乐
                await RefreshAsync();

                // 推送 UI 更新
                PushUI?.Invoke(BuildUI());
            };
            _qrManager.OnStatusChanged += (msg) =>
            {
                PushUI?.Invoke(BuildUI());
            };
            _qrManager.OnQRCodeReady += () =>
            {
                PushUI?.Invoke(BuildUI());
            };

            // 信任 cookie 文件，不做额外 API 验证（避免因验证接口问题误删有效 cookie）
            if (_bridge.IsLoggedIn)
            {
                context.Logger.LogInformation($"Bilibili 已登录: {_bridge.CurrentUserId}");
                await RefreshAsync();
            }
            else
            {
                context.Logger.LogInformation("Bilibili 未登录, 等待用户扫码");
            }
            OnReadyStateChanged?.Invoke(true);
        }

        public async Task<PlayableSource> ResolveAsync(string uuid, AudioQuality quality, CancellationToken token = default)
        {
            var track = _context.Library.GetTrack(uuid);
            if (track == null) return null;

            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var url = await _bridge.GetPlayUrlAsync(track.SourcePath);
                if (string.IsNullOrEmpty(url))
                {
                    _context.Logger.LogWarning($"[{DisplayName}] 获取播放 URL 失败: {track.Title} (尝试 {attempt}/{maxRetries})");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000, token);
                        continue;
                    }
                    return null;
                }

                _context.Logger.LogInformation($"[Stream] 启动流: {track.Title} (尝试 {attempt}/{maxRetries})");

                return new PlayableSource
                {
                    UUID = uuid,
                    SourceType = PlayableSourceType.Remote,
                    Url = url,
                    Format = AudioFormat.Aac,
                    Headers = new Dictionary<string, string>
                    {
                        ["Referer"] = "https://www.bilibili.com",
                        ["User-Agent"] = BilibiliBridge.UserAgent
                    },
                    CacheKey = $"bili_{track.SourcePath}"
                };
            }

            return null;
        }

        public Task<PlayableSource> RefreshUrlAsync(string u, AudioQuality q, CancellationToken t) => ResolveAsync(u, q, t);

        public async Task<List<Track>> GetMusicListAsync()
        {
            return _context.Library.QueryTracks(new TrackQuery { ModuleId = ModuleId, Limit = 0 }).ToList();
        }

        public async Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId)
        {
            var album = _context.Library.GetAlbum(albumId);
            if (album != null && !string.IsNullOrWhiteSpace(album.CoverUri))
                return await DownloadSpriteAsync(album.CoverUri);
            return (_context.DefaultCover.DefaultAlbumCover, "image/png");
        }

        public async Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid)
        {
            var track = _context.Library.GetTrack(uuid);
            if (track != null && !string.IsNullOrWhiteSpace(track.CoverUri))
                return await DownloadSpriteAsync(track.CoverUri);
            return (_context.DefaultCover.DefaultMusicCover, "image/png");
        }

        private async Task<(byte[] data, string mimeType)> DownloadSpriteAsync(string url)
        {
            if (_spriteCache.TryGetValue(url, out var cached) && cached.data != null)
                return cached;

            try
            {
                if (url.StartsWith("http://")) url = url.Replace("http://", "https://");
                using (var client = new System.Net.Http.HttpClient())
                {
                    var data = await client.GetByteArrayAsync(url);
                    if (data != null && data.Length > 0)
                    {
                        var result = (data, "image/jpeg");
                        _spriteCache[url] = result;
                        return result;
                    }
                }
            }
            catch { }
            return (null, null);
        }

        public void OnEnable() { }
        public void OnDisable() { }
        public void OnUnload() { _qrManager?.Stop(); _spriteCache.Clear(); }
        public void ClearCache() { _spriteCache.Clear(); }
        public void RemoveMusicCoverCache(string u) { }
        public void RemoveAlbumCoverCache(string a) { }
        public async Task<(byte[], string)> GetMusicCoverBytesAsync(string uuid)
        {
            var track = _context.Library.GetTrack(uuid);
            if (track != null && !string.IsNullOrWhiteSpace(track.CoverUri))
            {
                try
                {
                    var url = track.CoverUri;
                    if (url.StartsWith("http://")) url = url.Replace("http://", "https://");
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var data = await client.GetByteArrayAsync(url);
                        if (data != null && data.Length > 0)
                            return (data, "image/jpeg");
                    }
                }
                catch { }
            }
            return (null, null);
        }
        public async Task RefreshAsync()
        {
            await RefreshMusicListAsync();
            PublishLibraryRefresh();
        }

        private async Task RefreshMusicListAsync()
        {
            if (!_bridge.IsLoggedIn) return;

            var folderResult = await _bridge.GetMyFoldersAsync();
            if (!folderResult.Success)
            {
                _context.Logger.LogWarning("[{DisplayName}] 收藏夹列表获取失败，保留现有 Bilibili 曲库: {Message}",
                    DisplayName, folderResult.ErrorMessage);
                return;
            }

            var folders = ApplyFolderFilter(folderResult.Folders);
            _context.Logger.LogInformation("[{DisplayName}] 准备同步收藏夹: {Filtered}/{Total}",
                DisplayName, folders.Count, folderResult.Folders.Count);

            _context.Library.UnregisterModule(ModuleId);
            _spriteCache.Clear();

            int registeredFolders = 0;
            int registeredTracks = 0;
            foreach (var f in folders)
            {
                var videoResult = await _bridge.GetFolderVideosAsync(f.Id);
                if (!videoResult.Success)
                {
                    _context.Logger.LogWarning("[{DisplayName}] 跳过收藏夹 {FolderTitle} ({FolderId})，内容获取失败: {Message}",
                        DisplayName, f.Title, f.Id, videoResult.ErrorMessage);
                    continue;
                }

                _registry.RegisterFolder(f, videoResult.Videos);
                registeredFolders++;
                registeredTracks += videoResult.Videos.Count;
            }

            _context.Logger.LogInformation("[{DisplayName}] 同步完成: {FolderCount} 个收藏夹，{TrackCount} 首",
                DisplayName, registeredFolders, registeredTracks);
        }

        private List<BiliFolder> ApplyFolderFilter(List<BiliFolder> folders)
        {
            folders ??= new List<BiliFolder>();

            var enabled = _context.ConfigManager.GetValue<bool>("ImportFilterEnabled", false);
            if (!enabled) return folders;

            var mode = (_context.ConfigManager.GetValue<string>("ImportFilterMode", "allow") ?? "allow").Trim().ToLowerInvariant();
            var ids = ParseFolderIds(_context.ConfigManager.GetValue<string>("ImportFolderIds", ""));

            if (ids.Count == 0)
            {
                _context.Logger.LogWarning("[{DisplayName}] 已启用收藏夹名单，但没有填写有效 fid", DisplayName);
                return mode == "deny" ? folders : new List<BiliFolder>();
            }

            return mode == "deny"
                ? folders.Where(f => !ids.Contains(f.Id)).ToList()
                : folders.Where(f => ids.Contains(f.Id)).ToList();
        }

        private static HashSet<long> ParseFolderIds(string raw)
        {
            var set = new HashSet<long>();
            if (string.IsNullOrWhiteSpace(raw)) return set;

            foreach (var part in raw.Split(new[] { ',', '，', ';', '；', '\n', '\r', '\t', ' ' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (long.TryParse(part.Trim(), out var id))
                    set.Add(id);
            }

            return set;
        }

        private void PublishLibraryRefresh()
        {
            _context?.EventBus?.Publish(new PlaylistUpdatedEvent
            {
                SourceRefId = "bili_all",
                UpdateType = PlaylistUpdateType.FullRefresh
            });
        }

        #region IModuleUIProvider

        public bool HasSettingsUI => true;

        public Action<SlintNode> PushUI { get; set; }

        public SlintNode BuildUI()
        {
            var isLoggedIn = _bridge?.IsLoggedIn ?? false;

            if (isLoggedIn)
            {
                var userId = _bridge?.CurrentUserId ?? "";
                var pageDelay = _context?.ConfigManager?.GetValue<int>("PageLoadDelay", 300) ?? 300;
                var filterEnabled = _context?.ConfigManager?.GetValue<bool>("ImportFilterEnabled", false) ?? false;
                var filterMode = _context?.ConfigManager?.GetValue<string>("ImportFilterMode", "allow") ?? "allow";
                var filterIds = _context?.ConfigManager?.GetValue<string>("ImportFolderIds", "") ?? "";

                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(
                        SlintUi.Row(spacing: 12)
                            .AddChild(
                                SlintUi.Column(spacing: 4)
                                    .AddChild(SlintUi.Text("Bilibili Music", fontSize: 18))
                                    .AddChild(SlintUi.Text("已登录", fontSize: 12, color: "#4caf50"))
                            )
                    )
                    .AddChild(
                        SlintUi.Row(spacing: 8)
                            .AddChild(SlintUi.Text("用户 ID:", fontSize: 12))
                            .AddChild(SlintUi.Text(userId, fontSize: 12, color: "#94a3b8"))
                    )
                    .AddChild(SlintUi.Text("播放设置", fontSize: 16))
                    .AddChild(
                        SlintUi.Select("page_delay", "翻页延迟 (ms)", pageDelay.ToString(), new List<SlintOption>
                        {
                            new SlintOption("100", "100ms"),
                            new SlintOption("200", "200ms"),
                            new SlintOption("300", "300ms"),
                            new SlintOption("500", "500ms"),
                            new SlintOption("1000", "1000ms"),
                        })
                    )
                    .AddChild(SlintUi.Text("收藏夹导入", fontSize: 16))
                    .AddChild(
                        SlintUi.Switch("folder_filter_enabled", "启用收藏夹名单", filterEnabled)
                    )
                    .AddChild(
                        SlintUi.Select("folder_filter_mode", "名单模式", filterMode, new List<SlintOption>
                        {
                            new SlintOption("allow", "白名单"),
                            new SlintOption("deny", "黑名单"),
                        })
                    )
                    .AddChild(
                        SlintUi.Column(spacing: 4)
                            .AddChild(SlintUi.Text("收藏夹 fid (逗号分隔)", fontSize: 12, color: "#94a3b8"))
                            .AddChild(
                                SlintUi.Input("folder_filter_ids", "例如: 3945472809,123456789", filterIds)
                            )
                    )
                    .AddChild(
                        SlintUi.Button("logout_btn", "退出登录", variant: "danger")
                    );
            }
            else
            {
                var qrReady = _qrManager?.QRCodeBytes != null;
                var qrStatusText = qrReady
                    ? (_qrManager?.StatusMessage ?? "请使用 B站 扫码登录")
                    : "二维码加载中...";

                return SlintUi.Column(spacing: 16, padding: 20)
                    .AddChild(SlintUi.Text("未登录", fontSize: 16))
                    .AddChild(SlintUi.Text("请使用哔哩哔哩 App 扫描二维码登录", fontSize: 12))
                    .AddChild(
                        SlintUi.Image("qr_image",
                            "/api/modules/" + ModuleId + "/content/qr-image?v=" + _qrVersion,
                            width: 200, height: 200))
                    .AddChild(SlintUi.Text(qrStatusText, fontSize: 12))
                    .AddChild(
                        SlintUi.Button("qr_refresh", qrReady ? "刷新二维码" : "获取二维码"));
            }
        }

        public void HandleUIEvent(string nodeId, string action, string value)
        {
            _context?.Logger.LogInformation(
                "[{DisplayName}] UI Event: node={NodeId}, action={Action}, value={Value}",
                DisplayName, nodeId, action, value);

            switch (nodeId)
            {
                case "logout_btn":
                    LogoutBilibili();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "page_delay":
                    if (int.TryParse(value, out var delay))
                    {
                        _context?.ConfigManager?.SetValue("PageLoadDelay", delay);
                        _context?.ConfigManager?.Save();
                        if (_bridge != null) _bridge.PageDelay = delay;
                        _context?.Logger.LogInformation("[{DisplayName}] Page delay set to {Delay}ms", DisplayName, delay);
                    }
                    PushUI?.Invoke(BuildUI());
                    break;

                case "folder_filter_enabled":
                    var enabled = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    _context?.ConfigManager?.SetValue("ImportFilterEnabled", enabled);
                    _context?.ConfigManager?.Save();
                    _context?.Logger.LogInformation("[{DisplayName}] Import folder filter enabled: {Enabled}", DisplayName, enabled);
                    _ = RefreshAsync();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "folder_filter_mode":
                    var mode = string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase) ? "deny" : "allow";
                    _context?.ConfigManager?.SetValue("ImportFilterMode", mode);
                    _context?.ConfigManager?.Save();
                    _context?.Logger.LogInformation("[{DisplayName}] Import folder filter mode: {Mode}", DisplayName, mode);
                    _ = RefreshAsync();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "folder_filter_ids":
                    _context?.ConfigManager?.SetValue("ImportFolderIds", value ?? "");
                    _context?.ConfigManager?.Save();
                    _context?.Logger.LogInformation("[{DisplayName}] Import folder filter IDs set", DisplayName);
                    _ = RefreshAsync();
                    PushUI?.Invoke(BuildUI());
                    break;

                case "qr_refresh":
                    _ = RefreshQRCodeAsync();
                    break;
            }
        }

        private async Task RefreshQRCodeAsync()
        {
            if (_qrManager == null) return;
            _context?.Logger.LogInformation("[{DisplayName}] 刷新二维码...", DisplayName);
            _qrVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 等待二维码就绪再推 UI
            var qrReady = false;
            Action onReady = () => qrReady = true;
            _qrManager.OnQRCodeReady += onReady;
            _qrManager.StartLogin();

            for (int i = 0; i < 100 && !qrReady; i++)
                await Task.Delay(100);

            _qrManager.OnQRCodeReady -= onReady;
            PushUI?.Invoke(BuildUI());
        }

        public async Task<byte[]> ServeRawContent(string path)
        {
            if (path == "qr-image")
            {
                if (_qrManager != null)
                {
                    // 二维码不存在或轮询已停止（超时/过期），重新开始
                    if (_qrManager.QRCodeBytes == null || !_qrManager.IsPollingActive)
                    {
                        _qrManager.StartLogin();
                        // 等待一小段时间让二维码生成
                        for (int i = 0; i < 50; i++)
                        {
                            if (_qrManager.QRCodeBytes != null) break;
                            await Task.Delay(100);
                        }
                    }
                    return _qrManager.QRCodeBytes;
                }
            }
            return null;
        }

        public string ServeRawContentType(string path)
        {
            if (path == "qr-image") return "image/png";
            return null;
        }

        private void LogoutBilibili()
        {
            try
            {
                _qrManager?.Stop();
                _bridge?.ClearSession();
                _spriteCache.Clear();

                _context?.Library?.UnregisterModule(ModuleId);

                // 重新初始化 QR 登录
                _qrManager = new QRLoginManager(_bridge, _context?.Logger);
                _qrManager.OnLoginSuccess += async () =>
                {
                    _context?.Logger.LogInformation("[{DisplayName}] 扫码登录成功！", DisplayName);
                    await RefreshAsync();
                    PushUI?.Invoke(BuildUI());
                };
                _qrManager.OnStatusChanged += (msg) =>
                {
                    PushUI?.Invoke(BuildUI());
                };
                _qrManager.OnQRCodeReady += () =>
                {
                    PushUI?.Invoke(BuildUI());
                };

                PushUI?.Invoke(BuildUI());

                _context?.Logger?.LogInformation("[{DisplayName}] 已退出登录", DisplayName);
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogError(ex, "[{DisplayName}] 退出登录失败", DisplayName);
            }
        }

        #endregion
    }
}
