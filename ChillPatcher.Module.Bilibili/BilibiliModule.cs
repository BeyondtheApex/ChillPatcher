using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChillPatcher.SDK;
using ChillPatcher.SDK.Attributes;
using ChillPatcher.SDK.Events;
using ChillPatcher.SDK.Interfaces;
using ChillPatcher.SDK.Models;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;
using BepInEx.Configuration;

namespace ChillPatcher.Module.Bilibili
{
    [MusicModule("com.chillpatcher.bilibili", "Bilibili Music",
        Version = "1.0.0",
        Author = "xgqq",
        Description = "Bilibili video audio streaming")]
    public class BilibiliModule : IMusicModule, IStreamingMusicSourceProvider, ICoverProvider
    {
        public string ModuleId => "com.chillpatcher.bilibili";
        public string DisplayName => "Bilibili Music";
        public string Version => "1.0.0";
        public int Priority => 10;
        public ModuleCapabilities Capabilities => new ModuleCapabilities { CanDelete = false, CanFavorite = false, CanExclude = false, ProvidesCover = true };
        public MusicSourceType SourceType => MusicSourceType.Stream;

        public bool IsReady => true;
        public event Action<bool> OnReadyStateChanged;

        private IModuleContext _context;
        private BilibiliBridge _bridge;
        private QRLoginManager _qrManager;
        private BilibiliSongRegistry _registry;
        private string _currentLoginUuid;

        private Dictionary<string, string> _albumCoverUrls = new Dictionary<string, string>();
        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            string dataPath = Path.Combine(Application.persistentDataPath, "ChillPatcher", ModuleId);
            Directory.CreateDirectory(dataPath);

            // === 读取配置 ===
            int pageDelay = 300;
            try
            {
                string configPath = Path.Combine(Paths.ConfigPath, "com.chillpatcher.plugin.cfg");
                var configFile = new ConfigFile(configPath, true);

                var delayEntry = configFile.Bind(
                    "Module:com.chillpatcher.bilibili",
                    "PageLoadDelay",
                    300,
                    "翻页加载延迟(毫秒)。过低可能导致412错误，建议保持在300以上。"
                );
                configFile.Save();

                pageDelay = delayEntry.Value;
                context.Logger.LogInfo($"[{DisplayName}] 读取配置: 翻页延迟 = {pageDelay}ms");
            }
            catch (Exception ex)
            {
                context.Logger.LogWarning($"[{DisplayName}] 配置文件读写失败，使用默认值 300ms: {ex.Message}");
            }

            _bridge = new BilibiliBridge(context.Logger, dataPath, pageDelay);
            _registry = new BilibiliSongRegistry(context, ModuleId);
            _qrManager = new QRLoginManager(_bridge, context.Logger);

            _qrManager.OnLoginSuccess += async () => {
                context.Logger.LogInfo($"[{DisplayName}] 扫码登录成功！");
                _registry.UpdateLoginSongTitle("登录成功！正在同步...");

                // 清除登录歌曲和专辑
                _context.MusicRegistry.UnregisterMusic(BilibiliSongRegistry.UUID_LOGIN);
                _context.AlbumRegistry.UnregisterAllByModule(ModuleId);
                _currentLoginUuid = null;

                // 加载音乐
                await RefreshAsync();

                // 通知 UI 刷新
                _context.EventBus.Publish(new SDK.Events.PlaylistUpdatedEvent
                {
                    TagId = BilibiliSongRegistry.TAG_LOGIN,
                    UpdateType = SDK.Events.PlaylistUpdateType.FullRefresh
                });
            };
            _qrManager.OnStatusChanged += (msg) => _registry.UpdateLoginSongTitle(msg);
            _qrManager.OnQRCodeReady += () => {
                if (!string.IsNullOrEmpty(_currentLoginUuid))
                    _context.EventBus.Publish(new CoverInvalidatedEvent { MusicUuid = _currentLoginUuid, Reason = "QR" });
            };

            // 信任 cookie 文件，不做额外 API 验证（避免因验证接口问题误删有效 cookie）
            if (_bridge.IsLoggedIn)
            {
                context.Logger.LogInfo($"Bilibili 已登录: {_bridge.CurrentUserId}");
                await RefreshAsync();
            }
            else
            {
                RefreshLoginSong();
            }
            OnReadyStateChanged?.Invoke(true);
        }

        public async Task<PlayableSource> ResolveAsync(string uuid, AudioQuality quality, CancellationToken token = default)
        {
            if (uuid == _currentLoginUuid || uuid.Contains("bili_login_action"))
            {
                // 如果二维码已经获取到了，直接返回静音流
                if (_qrManager.QRCodeSprite != null)
                {
                    _context.Logger.LogInfo("登录流程已启动，二维码已就绪");
                }
                else
                {
                    _context.Logger.LogInfo("触发登录流程...");
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    Action onReady = () => tcs.TrySetResult(true);
                    _qrManager.OnQRCodeReady += onReady;
                    _qrManager.StartLogin();
                    await Task.WhenAny(tcs.Task, Task.Delay(15000, token));
                    _qrManager.OnQRCodeReady -= onReady;
                }

                // 强制刷新封面缓存
                _context.EventBus.Publish(new SDK.Events.CoverInvalidatedEvent { MusicUuid = uuid, Reason = "login song played" });
                return PlayableSource.FromPcmStream(uuid, new SilentPcmReader(), AudioFormat.Mp3);
            }

            if (!_context.StreamingService.IsAvailable)
            {
                _context.Logger.LogError($"[{DisplayName}] 流式服务不可用 (原生解码器未加载)");
                return null;
            }

            var music = _context.MusicRegistry.GetMusic(uuid);
            if (music == null) return null;

            const int maxRetries = 3;
            const int readyTimeoutMs = 20000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var url = await _bridge.GetPlayUrlAsync(music.SourcePath);
                if (string.IsNullOrEmpty(url))
                {
                    _context.Logger.LogWarning($"[{DisplayName}] 获取播放 URL 失败: {music.Title} (尝试 {attempt}/{maxRetries})");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000, token);
                        continue;
                    }
                    return null;
                }

                _context.Logger.LogInfo($"[Stream] 启动流: {music.Title} (尝试 {attempt}/{maxRetries})");

                var reader = await _context.StreamingService.CreateStreamAndWaitAsync(
                    url,
                    "aac",
                    music.Duration,
                    $"bili_{music.SourcePath}",
                    readyTimeoutMs,
                    new Dictionary<string, string>
                    {
                        ["Referer"] = "https://www.bilibili.com",
                        ["User-Agent"] = BilibiliBridge.UserAgent
                    },
                    token);

                if (reader != null)
                {
                    _context.Logger.LogInfo($"[{DisplayName}] PCM 流已就绪: {music.Title} [{reader.Info.SampleRate}Hz, {reader.Info.Channels}ch]");
                    return PlayableSource.FromPcmStream(uuid, reader, AudioFormat.Aac);
                }

                _context.Logger.LogWarning($"[{DisplayName}] PCM 流创建/准备失败: {music.Title} (尝试 {attempt}/{maxRetries})");
                if (attempt < maxRetries)
                    await Task.Delay(1000, token);
            }

            return null;
        }

        public Task<PlayableSource> RefreshUrlAsync(string u, AudioQuality q, CancellationToken t) => ResolveAsync(u, q, t);
        private void RefreshLoginSong()
        {
            _registry.RegisterLoginSong("B站扫码登录");
            _currentLoginUuid = BilibiliSongRegistry.UUID_LOGIN;

            // 预先获取二维码，这样歌曲列表显示时封面就能看到二维码
            _qrManager.StartLogin();
        }

        public async Task<List<MusicInfo>> GetMusicListAsync()
        {
            var list = new List<MusicInfo>();
            if (!_bridge.IsLoggedIn) return list;

            _albumCoverUrls.Clear();
            _spriteCache.Clear();

            var folders = await _bridge.GetMyFoldersAsync();
            foreach (var f in folders)
            {
                var videos = await _bridge.GetFolderVideosAsync(f.Id);

                if (videos.Count > 0 && !string.IsNullOrEmpty(videos[0].CoverUrl))
                {
                    string albumId = $"bili_album_{f.Id}";
                    string coverUrl = videos[0].CoverUrl;
                    if (coverUrl.StartsWith("http://")) coverUrl = coverUrl.Replace("http://", "https://");
                    _albumCoverUrls[albumId] = coverUrl;

                    _context.EventBus.Publish(new CoverInvalidatedEvent { AlbumId = albumId, Reason = "FolderLoaded" });
                }

                _registry.RegisterFolder(f, videos);

                foreach (var v in videos)
                {
                    string uuid = MusicInfo.GenerateUUID("bili_" + v.Bvid);
                    var registeredMusic = _context.MusicRegistry.GetMusic(uuid);
                    if (registeredMusic != null) list.Add(registeredMusic);
                }
            }
            return list;
        }

        public async Task<Sprite> GetAlbumCoverAsync(string albumId)
        {
            if (_albumCoverUrls.TryGetValue(albumId, out string url))
                return await DownloadSpriteAsync(url);
            return _context.DefaultCover.DefaultAlbumCover;
        }

        public async Task<Sprite> GetMusicCoverAsync(string uuid)
        {
            if (uuid == _currentLoginUuid) return _qrManager?.QRCodeSprite ?? _context.DefaultCover.DefaultMusicCover;
            var music = _context.MusicRegistry.GetMusic(uuid);
            if (music?.ExtendedData is string url && !string.IsNullOrEmpty(url))
                return await DownloadSpriteAsync(url);
            return _context.DefaultCover.DefaultMusicCover;
        }

        private async Task<Sprite> DownloadSpriteAsync(string url)
        {
            if (_spriteCache.TryGetValue(url, out var cachedSprite) && cachedSprite != null)
                return cachedSprite;

            try
            {
                if (url.StartsWith("http://")) url = url.Replace("http://", "https://");
                using (var req = UnityWebRequestTexture.GetTexture(url))
                {
                    req.SendWebRequest();
                    while (!req.isDone) await Task.Delay(10);
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        var tex = DownloadHandlerTexture.GetContent(req);
                        if (tex != null)
                        {
                            // === [核心修复] 中心裁切逻辑 ===
                            // 1. 取宽和高中的最小值，作为正方形的边长
                            int size = Math.Min(tex.width, tex.height);

                            // 2. 计算居中偏移量
                            int offsetX = (tex.width - size) / 2;
                            int offsetY = (tex.height - size) / 2;

                            // 3. 创建裁切区域
                            Rect cropRect = new Rect(offsetX, offsetY, size, size);

                            // 4. 创建 Sprite
                            var sprite = Sprite.Create(tex, cropRect, new Vector2(0.5f, 0.5f));
                            _spriteCache[url] = sprite;
                            return sprite;
                        }
                    }
                }
            }
            catch { }
            return _context.DefaultCover.DefaultMusicCover;
        }

        public void OnEnable() { }
        public void OnDisable() { }
        public void OnUnload() { _qrManager?.Stop(); _spriteCache.Clear(); }
        public void RemoveMusicCoverCache(string u) { }
        public void RemoveAlbumCoverCache(string a) { }
        public async Task<(byte[], string)> GetMusicCoverBytesAsync(string uuid)
        {
            var music = _context.MusicRegistry.GetMusic(uuid);
            if (music?.ExtendedData is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    if (url.StartsWith("http://")) url = url.Replace("http://", "https://");
                    using (var req = UnityWebRequest.Get(url))
                    {
                        req.SendWebRequest();
                        while (!req.isDone) await Task.Delay(10);
                        if (req.result == UnityWebRequest.Result.Success)
                        {
                            var data = req.downloadHandler.data;
                            if (data != null && data.Length > 0)
                                return (data, "image/jpeg");
                        }
                    }
                }
                catch { }
            }
            return (null, null);
        }
        public void ClearCache() { _spriteCache.Clear(); }
        public Task<AudioClip> LoadAudioAsync(string u) => Task.FromResult<AudioClip>(null);
        public Task<AudioClip> LoadAudioAsync(string u, CancellationToken c) => Task.FromResult<AudioClip>(null);
        public void UnloadAudio(string u) { }
        public async Task RefreshAsync() => await GetMusicListAsync();
    }
}