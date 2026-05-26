using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bulbul;
using ChillPatcher.Patches.UIFramework;
using ChillPatcher.SDK.Ipc;
using ChillPatcher.SDK.Models;
using ChillPatcher.UIFramework.Music;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ChillPatcher
{
    public class OmniMixIntegration : IDisposable
    {
        private static OmniMixIntegration _instance;
        public static OmniMixIntegration Instance => _instance ??= new OmniMixIntegration();

        private readonly OmniMixPlayerClient _client;
        private SharedMemoryReader _shmReader;
        private bool _connected;
        private bool _disposed;

        // Growable list state (synced from backend)
        private List<TagInfo> _growableTags = new List<TagInfo>();
        private TagInfo _currentGrowableTag;

        public bool IsConnected => _connected && _client?.IsConnected == true;
        public SharedMemoryReader SharedMemory => _shmReader;

        /// <summary>
        /// Unified Unix socket path (fallback IPC).
        /// Windows: %PUBLIC%/OmniMixPlayer/omnimix.sock | Others: /tmp/omnimix.sock
        /// </summary>
        private static string ResolveSocketPath()
        {
            var publicDir = Environment.GetEnvironmentVariable("PUBLIC") ?? Path.GetTempPath();
            var dir = Path.Combine(publicDir, "OmniMixPlayer");
            return Path.Combine(dir, "omnimix.sock");
        }

        /// <summary>
        /// Read IPC port from omni_port.txt in known directories.
        /// </summary>
        private static int? ReadPortFile()
        {
            string[] dirs = {
                Path.GetDirectoryName(typeof(OmniMixIntegration).Assembly.Location) ?? "",
                Path.Combine(Environment.GetEnvironmentVariable("PUBLIC") ?? Path.GetTempPath(), "OmniMixPlayer"),
                Path.GetTempPath(),
            };
            foreach (var dir in dirs)
            {
                try
                {
                    var filePath = Path.Combine(dir, "omnimix_port.txt");
                    if (File.Exists(filePath))
                    {
                        var text = File.ReadAllText(filePath).Trim();
                        if (int.TryParse(text, out var port) && port > 0 && port < 65536)
                            return port;
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// 3-step discovery: port file → default port → socket.
        /// Returns a configured OmniMixPlayerClient, or null.
        /// </summary>
        private static OmniMixPlayerClient DiscoverClient()
        {
            // Step 1: port file
            var port = ReadPortFile();
            if (port.HasValue)
                return new OmniMixPlayerClient(port.Value);

            // Step 2: default port
            return new OmniMixPlayerClient(17890);
        }

        /// <summary>
        /// Try to do a quick TCP health check to see if backend is reachable.
        /// </summary>
        private static bool QuickTcpProbe(int port)
        {
            try
            {
                using var sock = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork,
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Tcp);
                sock.Connect("127.0.0.1", port);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Check if a Unix Domain Socket file exists.
        /// On Windows they are IO_REPARSE_TAG_AF_UNIX reparse points —
        /// File.Exists may return false. Use FileInfo as fallback.
        /// </summary>
        private static bool SocketFileExists(string path)
        {
            try
            {
                if (File.Exists(path)) return true;
                // Fallback for reparse points on Windows
                var fi = new FileInfo(path);
                return fi.Exists;
            }
            catch { return false; }
        }

        public async UniTask<bool> ConnectAsync()
        {
            try
            {
                // Step 1: Try port file → TCP
                var port = ReadPortFile();
                if (port.HasValue && QuickTcpProbe(port.Value))
                {
                    Plugin.Log?.LogInfo($"[OmniMix] Backend detected via port file (port {port.Value}), connecting TCP...");
                    _client = new OmniMixPlayerClient(port.Value);
                    goto connect;
                }

                // Step 2: Try default port
                if (QuickTcpProbe(17890))
                {
                    Plugin.Log?.LogInfo("[OmniMix] Backend detected via default port 17890, connecting TCP...");
                    _client = new OmniMixPlayerClient(17890);
                    goto connect;
                }

                // Step 3: Try Unix socket fallback
                var socketPath = ResolveSocketPath();
                if (SocketFileExists(socketPath))
                {
                    Plugin.Log?.LogInfo("[OmniMix] Backend detected via Unix socket, connecting...");
                    _client = new OmniMixPlayerClient(socketPath, useSocket: true);
                    goto connect;
                }

                Plugin.Log?.LogWarning("[OmniMix] Backend not detected (no port file, no TCP, no socket)");
                _connected = false;
                return false;

            connect:
                await _client.ConnectAsync();

                // 以 ClientManaged 模式连接（此 Mod 自行管理队列）
                var connectResult = await _client.ConnectClient("chillpatcher", "client");
                if (!connectResult)
                {
                    Plugin.Log?.LogWarning("[OmniMix] Another client is already connected, retrying as observer...");
                }

                _connected = true;

                _client.OnTrackChanged += OnTrackChanged;
                _client.OnStateChanged += OnStateChanged;
                _client.OnQueueChanged += OnQueueChanged;
                _client.OnPosition += OnPosition;
                _client.OnPlaylistUpdated += OnPlaylistUpdated;

                // 3. Open shared memory for PCM audio (separate from connection detection)
                _shmReader = new SharedMemoryReader();
                _shmReader.Initialize();

                // Sync growable tags
                await RefreshGrowableTags();

                // 启动心跳 (每10秒) — socket 文件为主，HTTP 为辅
                StartHeartbeat();

                Plugin.Log?.LogInfo("[OmniMix] Connected to OmniMixPlayer backend (ClientManaged mode)");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Failed to connect: {ex.Message}");
                _connected = false;
                _shmReader?.Dispose();
                _shmReader = null;
                return false;
            }
        }

        public async UniTask DisconnectAsync()
        {
            try
            {
                StopHeartbeat();

                _client.OnTrackChanged -= OnTrackChanged;
                _client.OnStateChanged -= OnStateChanged;
                _client.OnQueueChanged -= OnQueueChanged;
                _client.OnPosition -= OnPosition;
                _client.OnPlaylistUpdated -= OnPlaylistUpdated;

                try { await _client.DisconnectClient("chillpatcher"); } catch { }
                await _client.DisconnectAsync();
                _shmReader?.Dispose();
                _shmReader = null;
                _connected = false;
                Plugin.Log?.LogInfo("[OmniMix] Disconnected from OmniMixPlayer backend");
            }
            catch { }
        }

        #region Heartbeat

        private CancellationTokenSource _heartbeatCts;

        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatCts = new CancellationTokenSource();
            HeartbeatLoop(_heartbeatCts.Token).Forget();
        }

        private void StopHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        private async UniTaskVoid HeartbeatLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: ct);
                if (ct.IsCancellationRequested) break;
                try
                {
                    // Primary: port file or default port reachable via TCP?
                    var port = ReadPortFile();
                    bool alive = (port.HasValue && QuickTcpProbe(port.Value))
                              || QuickTcpProbe(17890)
                              || SocketFileExists(ResolveSocketPath());

                    if (!alive)
                    {
                        Plugin.Log?.LogWarning("[OmniMix] Backend appears down, reconnecting...");
                        _connected = false;
                        await ConnectAsync();
                        break;
                    }
                }
                catch
                {
                    Plugin.Log?.LogWarning("[OmniMix] Heartbeat error, may be disconnected");
                    _connected = false;
                    break;
                }
            }
        }

        #endregion

        #region Import songs into game MusicService

        public async UniTask<int> ImportSongsToGame(bool replace = false)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService == null)
            {
                Plugin.Log?.LogWarning("[OmniMix] MusicService not available, cannot import songs");
                return 0;
            }

            try
            {
                // Pull active queue from OmniMixPlayer
                var queueJson = await _client.GetQueue();
                var songsJson = await _client.GetSongs();
                var tagsJson = await _client.GetTags();

                var allGameAudios = new List<GameAudioInfo>();
                var moduleSongs = new List<MusicInfo>();

                // Parse queue items
                if (queueJson is JArray queueArr)
                {
                    foreach (var item in queueArr)
                    {
                        var uuid = item["uuid"]?.ToString();
                        var title = item["title"]?.ToString() ?? "";
                        var artist = item["artist"]?.ToString() ?? "";
                        var moduleId = item["moduleId"]?.ToString() ?? "";
                        var song = new MusicInfo
                        {
                            UUID = uuid,
                            Title = title,
                            Artist = artist,
                            ModuleId = moduleId,
                            SourceType = MusicSourceType.Stream,
                            IsUnlocked = true
                        };
                        moduleSongs.Add(song);
                        allGameAudios.Add(ConvertToGameAudio(song));
                    }
                }

                // Parse all songs
                if (songsJson is JArray songsArr)
                {
                    foreach (var s in songsArr)
                    {
                        var uuid = s["uuid"]?.ToString();
                        if (moduleSongs.Any(m => m.UUID == uuid)) continue;

                        moduleSongs.Add(new MusicInfo
                        {
                            UUID = uuid,
                            Title = s["title"]?.ToString() ?? "",
                            Artist = s["artist"]?.ToString() ?? "",
                            AlbumId = s["albumId"]?.ToString() ?? "",
                            Duration = s["duration"]?.ToObject<float>() ?? 0,
                            ModuleId = s["moduleId"]?.ToString() ?? "",
                            SourceType = MusicSourceType.Stream,
                            IsUnlocked = true,
                            IsFavorite = s["isFavorite"]?.ToObject<bool>() ?? false,
                            IsExcluded = s["isExcluded"]?.ToObject<bool>() ?? false
                        });
                    }
                }

                // Inject into game MusicService
                var allMusicList = Traverse.Create(musicService)
                    .Field("_allMusicList")
                    .GetValue<List<GameAudioInfo>>();

                if (allMusicList == null)
                {
                    Plugin.Log?.LogWarning("[OmniMix] Cannot access _allMusicList");
                    return 0;
                }

                if (replace)
                {
                    // Remove previously synced module songs (those without LocalPath)
                    allMusicList.RemoveAll(a => string.IsNullOrEmpty(a.LocalPath));
                }

                foreach (var ms in moduleSongs)
                {
                    if (!allMusicList.Any(a => a.UUID == ms.UUID))
                    {
                        var ga = ConvertToGameAudio(ms);
                        allMusicList.Add(ga);
                    }
                }

                // Sync to current playlist
                var currentPlayList = musicService.CurrentPlayList;
                if (currentPlayList != null && allGameAudios.Count > 0)
                {
                    if (replace) currentPlayList.RemoveAll(a => string.IsNullOrEmpty(a.LocalPath));

                    foreach (var ga in allGameAudios)
                    {
                        if (!currentPlayList.Any(a => a.UUID == ga.UUID))
                            currentPlayList.Add(ga);
                    }
                }

                Plugin.Log?.LogInfo($"[OmniMix] Imported {moduleSongs.Count} songs to game MusicService");
                return moduleSongs.Count;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[OmniMix] Failed to import songs: {ex}");
                return 0;
            }
        }

        private static GameAudioInfo ConvertToGameAudio(MusicInfo mi)
        {
            return new GameAudioInfo
            {
                UUID = mi.UUID,
                Title = mi.Title ?? "",
                Credit = mi.Artist ?? "",
                Tag = AudioTag.Custom1,
                IsUnlocked = true,
                PathType = AudioMode.LocalPc,
                LocalPath = "",
                AudioClip = null
            };
        }

        #endregion

        #region Playlist query (for JSApi)

        public async UniTask<string> GetTagsJson()
        {
            try { var r = await _client.GetTags(); return r.ToString(); } catch { return "[]"; }
        }

        public async UniTask<string> GetAlbumsJson(string tagId = null)
        {
            try { var r = await _client.GetAlbums(tagId); return r.ToString(); } catch { return "[]"; }
        }

        public async UniTask<string> GetSongsJson(string albumId = null, string tagId = null)
        {
            try { var r = await _client.GetSongs(albumId, tagId); return r.ToString(); } catch { return "[]"; }
        }

        public async UniTask<(byte[] data, string mime)> GetCoverAsync(string uuid)
        {
            try { return await _client.GetTrackCover(uuid); } catch { return (null, null); }
        }

        #endregion

        #region Playback control (for JSApi)

        public async UniTask Play(string uuid) { try { await _client.Play(uuid); } catch { } }
        public async UniTask Pause() { try { await _client.Pause(); } catch { } }
        public async UniTask Resume() { try { await _client.Resume(); } catch { } }
        public async UniTask Toggle() { try { await _client.Toggle(); } catch { } }
        public async UniTask Next() { try { await _client.Next(); } catch { } }
        public async UniTask Prev() { try { await _client.Prev(); } catch { } }

        public async UniTask AddToQueue(string uuid) { try { await _client.AddToQueue(uuid); } catch { } }
        public async UniTask SetFavorite(string uuid, bool fav)
        {
            try { await _client.PostAsync($"/api/favorite", new { uuid, isFavorite = fav }); } catch { }
        }
        public async UniTask SetExcluded(string uuid, bool excluded)
        {
            try { await _client.PostAsync($"/api/exclude", new { uuid, isFavorite = excluded }); } catch { }
        }

        #endregion

        #region Growable List (IPC bridge for paginated loading)

        /// <summary>
        /// 获取所有增长列表 Tag（从后端缓存）
        /// </summary>
        public IReadOnlyList<TagInfo> GetGrowableTags()
        {
            return _growableTags;
        }

        /// <summary>
        /// 获取当前选中的增长列表 Tag
        /// </summary>
        public TagInfo GetCurrentGrowableTag()
        {
            return _currentGrowableTag;
        }

        /// <summary>
        /// 设置当前选中的增长列表 Tag（同步到后端）
        /// </summary>
        public async UniTask SetCurrentGrowableTag(string tagId)
        {
            if (!string.IsNullOrEmpty(tagId))
            {
                _currentGrowableTag = _growableTags.FirstOrDefault(t => t.TagId == tagId);
                try { await _client.PostAsync($"/api/tags/{Uri.EscapeDataString(tagId)}/activate", new { }); } catch { }
            }
            else
            {
                _currentGrowableTag = null;
            }
        }

        /// <summary>
        /// 触发增长列表加载更多（调用后端模块的 LoadMoreCallback）
        /// </summary>
        public async UniTask<int> TriggerGrowableLoadMore(string tagId)
        {
            try
            {
                var json = await _client.GetAsync($"/api/tags/{Uri.EscapeDataString(tagId)}/load-more");
                var obj = JObject.Parse(json);
                return obj["loadedCount"]?.ToObject<int>() ?? 0;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Growable load-more failed for {tagId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 从后端刷新增长列表 Tag 缓存
        /// </summary>
        public async UniTask RefreshGrowableTags()
        {
            try
            {
                var json = await _client.GetAsync("/api/tags/growable");
                var arr = JArray.Parse(json);
                _growableTags = arr.Select(j => new TagInfo
                {
                    TagId = j["tagId"]?.ToString() ?? "",
                    DisplayName = j["name"]?.ToString() ?? "",
                    ModuleId = j["moduleId"]?.ToString() ?? "",
                    BitValue = (ulong)(j["bitValue"]?.ToObject<long>() ?? 0),
                    IsGrowableList = j["isGrowable"]?.ToObject<bool>() ?? false
                }).ToList();
                Plugin.Log?.LogInfo($"[OmniMix] Refreshed {_growableTags.Count} growable tags");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Failed to refresh growable tags: {ex.Message}");
            }
        }

        #endregion

        #region Events

        public event Action<string, string, string, float> OnTrackUpdated;
        public event Action<bool, float, float> OnStateUpdated;
        public event Action OnQueueUpdated;

        private void OnTrackChanged(object sender, TrackChangedEventArgs e)
        {
            OnTrackUpdated?.Invoke(e.Uuid, e.Title, e.Artist, e.Duration);
        }

        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            OnStateUpdated?.Invoke(e.IsPlaying, e.Position, e.Volume);
        }

        private void OnQueueChanged(object sender, QueueChangedEventArgs e)
        {
            OnQueueUpdated?.Invoke();
        }

        private void OnPosition(object sender, PositionEventArgs e) { }

        private void OnPlaylistUpdated(object sender, PlaylistUpdatedEventArgs e)
        {
            // When backend pushes new songs (e.g. from growable list load-more),
            // re-import them into the game's MusicService
            Plugin.Log?.LogInfo("[OmniMix] Playlist updated, re-importing songs...");
            ImportSongsToGame(false).Forget();
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _ = DisconnectAsync();
        }
    }
}
