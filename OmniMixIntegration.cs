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
using ChillPatcher.SDK.Native;
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

        private OmniMixIntegration()
        {
            _clientId = ReadInstanceId() ?? "chillpatcher-" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Read the instance ID from .omnimix_instance_id in the game root.
        /// Falls back to a random GUID if the file doesn't exist.
        /// </summary>
        private static string ReadInstanceId()
        {
            try
            {
                var dataPath = Application.dataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    var gameRoot = Path.GetDirectoryName(dataPath);
                    if (!string.IsNullOrEmpty(gameRoot))
                    {
                        var idFile = Path.Combine(gameRoot, ".omnimix_instance_id");
                        if (File.Exists(idFile))
                        {
                            var id = File.ReadAllText(idFile).Trim();
                            if (!string.IsNullOrEmpty(id))
                            {
                                Plugin.Log?.LogInfo($"[OmniMix] Using instance ID from file: {id}");
                                return id;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Failed to read instance ID: {ex.Message}");
            }
            return null;
        }

        private OmniMixPlayerClient _client;
        private OmniPcmShared _shmReader;
        private CancellationTokenSource _heartbeatCts;
        private readonly string _clientId;
        private bool _connected;
        private bool _disposed;
        private readonly SemaphoreSlim _importSemaphore = new SemaphoreSlim(1, 1);

        // Tag state (synced from backend)
        private List<TagInfo> _allTags = new List<TagInfo>();
        private List<TagInfo> _growableTags = new List<TagInfo>();
        private TagInfo _currentGrowableTag;

        public IReadOnlyList<TagInfo> GetAllTags() => _allTags;

        // In-memory exclusions (synced from backend)
        private readonly HashSet<string> _excludedUuids = new HashSet<string>();

        public bool IsExcluded(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return false;
            lock (_excludedUuids)
            {
                return _excludedUuids.Contains(uuid);
            }
        }

        private float _currentTrackDuration;
        private float _currentTrackPosition;
        private bool _currentIsPlaying;
        private float _lastPositionUpdateTime;

        public float CurrentTrackDuration => _currentTrackDuration;
        public float CurrentTrackPosition
        {
            get
            {
                if (!_currentIsPlaying)
                    return _currentTrackPosition;
                float elapsed = Time.time - _lastPositionUpdateTime;
                return Mathf.Min(_currentTrackPosition + elapsed, _currentTrackDuration);
            }
        }
        public bool CurrentIsPlaying => _currentIsPlaying;

        public async UniTask Seek(float positionSeconds)
        {
            if (_client != null)
            {
                try
                {
                    await _client.Seek(positionSeconds);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[OmniMix] Seek request failed: {ex.Message}");
                }
            }
        }

        public bool IsConnected => _connected && _client?.IsConnected == true;
        public OmniPcmShared SharedMemory => _shmReader;

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
            var dirs = new List<string>
            {
                Path.GetDirectoryName(typeof(OmniMixIntegration).Assembly.Location) ?? "",
                Path.Combine(Environment.GetEnvironmentVariable("PUBLIC") ?? Path.GetTempPath(), "OmniMixPlayer"),
                Path.GetTempPath(),
            };

            // Also check the game root directory (where Flutter writes the port file)
            try
            {
                var dataPath = Application.dataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    var gameRoot = Path.GetDirectoryName(dataPath);
                    if (!string.IsNullOrEmpty(gameRoot))
                        dirs.Insert(0, gameRoot);
                }
            }
            catch { /* Unity may not be ready yet */ }

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

        private CancellationTokenSource _connectionLoopCts;
        private bool _isConnectionLoopRunning;

        private void StartConnectionLoop()
        {
            if (_isConnectionLoopRunning) return;
            _isConnectionLoopRunning = true;

            // Try starting Windows Service in the background at startup
            _ = System.Threading.Tasks.Task.Run(() => TryStartWindowsService());

            _connectionLoopCts = new CancellationTokenSource();
            ConnectionLoop(_connectionLoopCts.Token).Forget();
        }

        private void StopConnectionLoop()
        {
            _connectionLoopCts?.Cancel();
            _connectionLoopCts?.Dispose();
            _connectionLoopCts = null;
            _isConnectionLoopRunning = false;
        }

        private async UniTaskVoid ConnectionLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                bool alive = false;
                try
                {
                    var port = ReadPortFile();
                    alive = (port.HasValue && QuickTcpProbe(port.Value))
                            || QuickTcpProbe(17890)
                            || SocketFileExists(ResolveSocketPath());
                }
                catch { }

                if (!alive)
                {
                    if (_connected)
                    {
                        Plugin.Log?.LogWarning("[OmniMix] Backend connection lost, cleaning up...");
                        await DisconnectInternalAsync();
                    }
                }
                else
                {
                    if (!_connected)
                    {
                        Plugin.Log?.LogInfo("[OmniMix] Backend detected, attempting to connect...");
                        bool success = await ConnectInternalAsync();
                        if (success)
                        {
                            Plugin.Log?.LogInfo("[OmniMix] Reconnected to backend. Syncing songs...");
                            try
                            {
                                await ImportSongsToGame(replace: true);
                            }
                            catch (Exception ex)
                            {
                                Plugin.Log?.LogWarning($"[OmniMix] Reconnect song sync failed: {ex.Message}");
                            }
                        }
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: ct);
            }
        }

        public async UniTask<bool> ConnectAsync()
        {
            StartConnectionLoop();
            if (_connected) return true;

            return await ConnectInternalAsync();
        }

        private async UniTask<bool> ConnectInternalAsync()
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

                _connected = false;
                return false;

            connect:
                await _client.ConnectAsync();

                var connectResult = await _client.ConnectInstance(_clientId, "audio", "client");
                var sharedMemoryName = connectResult["sharedMemoryName"]?.ToString();

                _connected = true;

                _client.OnTrackChanged += OnTrackChanged;
                _client.OnStateChanged += OnStateChanged;
                _client.OnQueueChanged += OnQueueChanged;
                _client.OnPosition += OnPosition;
                _client.OnPlaylistUpdated += OnPlaylistUpdated;
                _client.OnExcludeChanged += OnExcludeChanged;

                // 3. Open this instance's shared memory for PCM audio
                _shmReader = new OmniPcmShared(sharedMemoryName);
                StartHeartbeatLoop();

                // Sync growable tags
                await RefreshGrowableTags();

                Plugin.Log?.LogInfo($"[OmniMix] Connected to OmniMixPlayer backend instance {_client.InstanceId} (ClientManaged mode)");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Failed to connect internally: {ex.Message}");
                await DisconnectInternalAsync();
                return false;
            }
        }

        public async UniTask DisconnectAsync()
        {
            StopConnectionLoop();
            await DisconnectInternalAsync();
        }

        private async UniTask DisconnectInternalAsync()
        {
            try
            {
                if (_client != null)
                {
                    _client.OnTrackChanged -= OnTrackChanged;
                    _client.OnStateChanged -= OnStateChanged;
                    _client.OnQueueChanged -= OnQueueChanged;
                    _client.OnPosition -= OnPosition;
                    _client.OnPlaylistUpdated -= OnPlaylistUpdated;
                    _client.OnExcludeChanged -= OnExcludeChanged;

                    StopHeartbeatLoop();
                    try { await _client.DisconnectInstance(); } catch { }
                    await _client.DisconnectAsync();
                }
            }
            catch { }
            finally
            {
                _shmReader?.Dispose();
                _shmReader = null;
                _connected = false;

                // Clean up game audio resources and cover cache
                try
                {
                    CleanupStreamAudioClips();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[OmniMix] Failed to clean up stream audio clips: {ex.Message}");
                }

                try
                {
                    UIFramework.Music.CoverService.Instance.ClearCache();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[OmniMix] Failed to clear cover cache: {ex.Message}");
                }

                Plugin.Log?.LogInfo("[OmniMix] Disconnected from OmniMixPlayer backend and cleared cached resources");
            }
        }

        private void StartHeartbeatLoop()
        {
            StopHeartbeatLoop();
            _heartbeatCts = new CancellationTokenSource();
            var token = _heartbeatCts.Token;
            UniTask.Void(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: token);
                        if (_client == null || !_connected) continue;
                        var ok = await _client.HeartbeatInstance();
                        if (!ok)
                        {
                            Plugin.Log?.LogWarning("[OmniMix] Instance heartbeat rejected; disconnecting local integration");
                            await DisconnectInternalAsync();
                            break;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogWarning($"[OmniMix] Instance heartbeat failed: {ex.Message}");
                    }
                }
            });
        }

        private void StopHeartbeatLoop()
        {
            try { _heartbeatCts?.Cancel(); } catch { }
            try { _heartbeatCts?.Dispose(); } catch { }
            _heartbeatCts = null;
        }

        private static void TryStartWindowsService()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;

            try
            {
                // Check if the service is installed
                var queryPsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "query OmniMixPlayerBackend",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var queryProcess = System.Diagnostics.Process.Start(queryPsi);
                if (queryProcess == null) { TryDirectExeLaunch(); return; }

                string output = queryProcess.StandardOutput.ReadToEnd();
                queryProcess.WaitForExit();

                if (queryProcess.ExitCode == 0) // Service is installed
                {
                    if (!output.ToLower().Contains("running"))
                    {
                        Plugin.Log?.LogInfo("[OmniMix] Service 'OmniMixPlayerBackend' is installed but not running. Attempting to start it...");

                        var startPsi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = "start OmniMixPlayerBackend",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var startProcess = System.Diagnostics.Process.Start(startPsi);
                        startProcess?.WaitForExit();
                    }
                    else
                    {
                        Plugin.Log?.LogInfo("[OmniMix] Service 'OmniMixPlayerBackend' is already running.");
                    }
                }
                else
                {
                    Plugin.Log?.LogDebug("[OmniMix] Service 'OmniMixPlayerBackend' is not installed. Trying direct exe launch...");
                    TryDirectExeLaunch();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Failed to check/start Windows service: {ex.Message}");
                TryDirectExeLaunch();
            }
        }

        /// <summary>
        /// Try to launch OmniMixPlayer.Backend.exe directly as a fallback
        /// when the Windows Service is not installed.
        /// </summary>
        private static void TryDirectExeLaunch()
        {
            try
            {
                // Look for the backend exe relative to the mod assembly
                var modDir = Path.GetDirectoryName(typeof(OmniMixIntegration).Assembly.Location);
                if (string.IsNullOrEmpty(modDir)) return;

                // Walk up to find OmniMixPlayer.Backend.exe
                var candidateDirs = new List<string>
                {
                    modDir,
                    Path.Combine(modDir, ".."),
                    Path.Combine(modDir, "..", ".."),
                    Path.Combine(modDir, "OmniMixPlayer"),
                };

                foreach (var dir in candidateDirs)
                {
                    try
                    {
                        var exePath = Path.GetFullPath(Path.Combine(dir, "OmniMixPlayer.Backend.exe"));
                        if (File.Exists(exePath))
                        {
                            Plugin.Log?.LogInfo($"[OmniMix] Launching backend: {exePath}");
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            };
                            System.Diagnostics.Process.Start(psi);
                            return;
                        }
                    }
                    catch { /* try next candidate */ }
                }

                Plugin.Log?.LogDebug("[OmniMix] OmniMixPlayer.Backend.exe not found in any candidate directory.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[OmniMix] Direct exe launch failed: {ex.Message}");
            }
        }

        private static void CleanupStreamAudioClips()
        {
            var musicService = Patches.UIFramework.MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService != null)
            {
                var allMusicList = Traverse.Create(musicService)
                    .Field("_allMusicList")
                    .GetValue<List<GameAudioInfo>>();
                if (allMusicList != null)
                {
                    foreach (var ga in allMusicList)
                    {
                        if (ga != null && UIFramework.Audio.StreamingAudioLoader.IsStreamingSource(ga))
                        {
                            if (ga.AudioClip != null)
                            {
                                UnityEngine.Object.Destroy(ga.AudioClip);
                                ga.AudioClip = null;
                            }
                        }
                    }
                }
            }
        }

        #region Import songs into game MusicService

        public async UniTask<int> ImportSongsToGame(bool replace = false)
        {
            await _importSemaphore.WaitAsync();
            try
            {
                var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
                if (musicService == null)
                {
                    Plugin.Log?.LogWarning("[OmniMix] MusicService not available, cannot import songs");
                    return 0;
                }

                // Pull active queue from OmniMixPlayer
                var queueJson = await _client.GetQueue();
                var songsJson = await _client.GetSongs();
                var tagsJson = await _client.GetTags();

                // Refresh custom tags and growable tags
                await RefreshGrowableTags();

                // Switch to main thread for modifying game collections
                await UniTask.SwitchToMainThread();

                var allGameAudios = new List<GameAudioInfo>();
                var moduleSongs = new List<MusicInfo>();

                // Parse tags to map tagId -> bitValue
                var tagBitValues = new Dictionary<string, ulong>();
                if (tagsJson is JArray tagsArr)
                {
                    foreach (var tag in tagsArr)
                    {
                        var tagId = tag["id"]?.ToString();
                        var bitValue = tag["bitValue"]?.ToObject<ulong>() ?? 0;
                        if (!string.IsNullOrEmpty(tagId) && bitValue > 0)
                        {
                            tagBitValues[tagId] = bitValue;
                        }
                    }
                }

                // Parse all songs
                var songDict = new Dictionary<string, MusicInfo>();
                if (songsJson is JArray songsArr)
                {
                    foreach (var s in songsArr)
                    {
                        var uuid = s["uuid"]?.ToString();
                        if (string.IsNullOrEmpty(uuid)) continue;

                        bool isExcluded = s["isExcluded"]?.ToObject<bool>() ?? false;
                        lock (_excludedUuids)
                        {
                            if (isExcluded)
                                _excludedUuids.Add(uuid);
                            else
                                _excludedUuids.Remove(uuid);
                        }

                        var songTagIds = s["tagIds"]?.ToObject<List<string>>() ?? new List<string>();

                        var song = new MusicInfo
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
                            IsExcluded = isExcluded,
                            TagIds = songTagIds
                        };
                        songDict[uuid] = song;
                    }
                }

                // Parse queue items
                if (queueJson is JArray queueArr)
                {
                    foreach (var item in queueArr)
                    {
                        var uuid = item["uuid"]?.ToString();
                        if (string.IsNullOrEmpty(uuid)) continue;

                        MusicInfo song;
                        if (songDict.TryGetValue(uuid, out var existingSong))
                        {
                            song = existingSong;
                        }
                        else
                        {
                            var title = item["title"]?.ToString() ?? "";
                            var artist = item["artist"]?.ToString() ?? "";
                            var moduleId = item["moduleId"]?.ToString() ?? "";
                            song = new MusicInfo
                            {
                                UUID = uuid,
                                Title = title,
                                Artist = artist,
                                ModuleId = moduleId,
                                SourceType = MusicSourceType.Stream,
                                IsUnlocked = true
                            };
                        }

                        if (!moduleSongs.Any(m => m.UUID == uuid))
                        {
                            moduleSongs.Add(song);
                        }
                        allGameAudios.Add(ConvertToGameAudio(song, tagBitValues));
                    }
                }

                // Add remaining songs from songDict to moduleSongs
                foreach (var kvp in songDict)
                {
                    if (!moduleSongs.Any(m => m.UUID == kvp.Key))
                    {
                        moduleSongs.Add(kvp.Value);
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
                        var ga = ConvertToGameAudio(ms, tagBitValues);
                        allMusicList.Add(ga);
                    }
                }

                // Sync to current playlist
                var currentPlayList = musicService.CurrentPlayList;
                if (currentPlayList != null && allGameAudios.Count > 0)
                {
                    if (replace)
                    {
                        for (int i = currentPlayList.Count - 1; i >= 0; i--)
                        {
                            if (string.IsNullOrEmpty(currentPlayList[i].LocalPath))
                            {
                                currentPlayList.RemoveAt(i);
                            }
                        }
                    }

                    foreach (var ga in allGameAudios)
                    {
                        if (!currentPlayList.Any(a => a.UUID == ga.UUID))
                            currentPlayList.Add(ga);
                    }
                }

                Plugin.Log?.LogInfo($"[OmniMix] Imported {moduleSongs.Count} songs to game MusicService");

                // Trigger UI playlist refresh
                try
                {
                    MusicUI_VirtualScroll_Patch.RefreshPlaylistDisplay();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[OmniMix] Failed to refresh playlist display: {ex.Message}");
                }

                // Trigger UI tag buttons refresh
                try
                {
                    MusicTagListUI_Patches.RefreshCustomTagButtons();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[OmniMix] Failed to refresh custom tag buttons: {ex.Message}");
                }

                return moduleSongs.Count;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[OmniMix] Failed to import songs: {ex}");
                return 0;
            }
            finally
            {
                _importSemaphore.Release();
            }
        }

        private static GameAudioInfo ConvertToGameAudio(MusicInfo mi, Dictionary<string, ulong> tagBitValues)
        {
            ulong tagValue = 0;
            if (mi.TagIds != null && tagBitValues != null)
            {
                foreach (var tagId in mi.TagIds)
                {
                    if (tagBitValues.TryGetValue(tagId, out var bv))
                    {
                        tagValue |= bv;
                    }
                }
            }
            if (mi.IsFavorite)
            {
                tagValue |= (ulong)AudioTag.Favorite;
            }

            return new GameAudioInfo
            {
                UUID = mi.UUID,
                Title = mi.Title ?? "",
                Credit = mi.Artist ?? "",
                Tag = (AudioTag)tagValue,
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
            try { await _client.PostAsync($"/favorite", new { uuid, isFavorite = fav }); } catch { }
        }
        public async UniTask SetExcluded(string uuid, bool excluded)
        {
            try { await _client.PostAsync($"/exclude", new { uuid, isFavorite = excluded }); } catch { }
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
                try { await _client.PostAsync($"/tags/{Uri.EscapeDataString(tagId)}/activate", new { }); } catch { }
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
                var json = await _client.PostAsync($"/tags/{Uri.EscapeDataString(tagId)}/load-more", new { });
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
        /// 从后端刷新 Tag 缓存（包括所有自定义 Tag 和增长型 Tag）
        /// </summary>
        public async UniTask RefreshGrowableTags()
        {
            try
            {
                var json = await _client.GetAsync("/tags");
                var arr = JArray.Parse(json);
                _allTags = arr.Select(j => new TagInfo
                {
                    TagId = j["id"]?.ToString() ?? "",
                    DisplayName = j["name"]?.ToString() ?? "",
                    ModuleId = j["moduleId"]?.ToString() ?? "",
                    BitValue = (ulong)(j["bitValue"]?.ToObject<long>() ?? 0),
                    IsGrowableList = j["isGrowable"]?.ToObject<bool>() ?? false
                }).ToList();

                _growableTags = _allTags.Where(t => t.IsGrowableList).ToList();
                Plugin.Log?.LogInfo($"[OmniMix] Refreshed {_allTags.Count} custom tags ({_growableTags.Count} growable)");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[OmniMix] Failed to refresh tags: {ex.Message}");
            }
        }

        #endregion

        #region Events

        public event Action<string, string, string, float> OnTrackUpdated;
        public event Action<bool, float, float> OnStateUpdated;
        public event Action OnQueueUpdated;

        private void OnTrackChanged(object sender, TrackChangedEventArgs e)
        {
            _currentTrackDuration = e.Duration;
            _currentTrackPosition = 0f;
            _lastPositionUpdateTime = Time.time;
            OnTrackUpdated?.Invoke(e.Uuid, e.Title, e.Artist, e.Duration);

            // 同步播放状态到游戏内，传入事件参数以防未导入时动态生成虚拟轨道
            SyncTrackToGameAsync(e.Uuid, e.Title, e.Artist).Forget();
        }

        private async UniTaskVoid SyncTrackToGameAsync(string uuid, string title, string artist)
        {
            if (string.IsNullOrEmpty(uuid)) return;

            await UniTask.SwitchToMainThread();

            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            if (musicService == null) return;

            // 如果已经是当前播放的歌曲，无需重复触发
            if (musicService.PlayingMusic != null && musicService.PlayingMusic.UUID == uuid)
                return;

            var targetAudio = musicService.AllMusicList?.FirstOrDefault(m => m != null && m.UUID == uuid);
            if (targetAudio == null)
            {
                // 1. 如果找不到，说明歌单可能落后，尝试从后端增量同步
                Plugin.Log?.LogInfo($"[OmniMix] Track {uuid} not found in game list, forcing a playlist refresh...");
                await ImportSongsToGame(replace: false);
                targetAudio = musicService.AllMusicList?.FirstOrDefault(m => m != null && m.UUID == uuid);
            }

            if (targetAudio == null)
            {
                // 2. 如果依然找不到（例如是通过外部临时播放的单曲/搜索结果），动态生成一个虚拟歌曲信息注入游戏，确保 UI 能够正确显示
                Plugin.Log?.LogInfo($"[OmniMix] Track {uuid} still not found after refresh, dynamically creating temporary track info...");

                var virtualMusic = new MusicInfo
                {
                    UUID = uuid,
                    Title = string.IsNullOrEmpty(title) ? "OmniMix Track" : title,
                    Artist = string.IsNullOrEmpty(artist) ? "" : artist,
                    Duration = _currentTrackDuration,
                    SourceType = MusicSourceType.Stream,
                    IsUnlocked = true
                };

                var allMusicList = Traverse.Create(musicService)
                    .Field("_allMusicList")
                    .GetValue<List<GameAudioInfo>>();

                if (allMusicList != null)
                {
                    targetAudio = ConvertToGameAudio(virtualMusic, new Dictionary<string, ulong>());
                    allMusicList.Add(targetAudio);
                }
            }

            if (targetAudio != null)
            {
                Plugin.Log?.LogInfo($"[OmniMix] Syncing track change from backend: {targetAudio.AudioClipName}");
                musicService.PlayArugumentMusic(targetAudio, MusicChangeKind.Manual);
            }
        }

        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            _currentIsPlaying = e.IsPlaying;
            _currentTrackPosition = e.Position;
            _lastPositionUpdateTime = Time.time;
            OnStateUpdated?.Invoke(e.IsPlaying, e.Position, e.Volume);
        }

        private void OnQueueChanged(object sender, QueueChangedEventArgs e)
        {
            OnQueueUpdated?.Invoke();
        }

        private void OnPosition(object sender, PositionEventArgs e)
        {
            _currentTrackPosition = e.Position;
            _lastPositionUpdateTime = Time.time;
        }

        private void OnPlaylistUpdated(object sender, PlaylistUpdatedEventArgs e)
        {
            // When backend pushes new songs (e.g. from growable list load-more),
            // re-import them into the game's MusicService
            Plugin.Log?.LogInfo("[OmniMix] Playlist updated, re-importing songs...");
            ImportSongsToGame(false).Forget();
        }

        private void OnExcludeChanged(object sender, ExcludeChangedEventArgs e)
        {
            lock (_excludedUuids)
            {
                if (e.IsExcluded)
                    _excludedUuids.Add(e.Uuid);
                else
                    _excludedUuids.Remove(e.Uuid);
            }

            try
            {
                MusicService_Excluded_Patch.RaiseOnSongExcludedChanged(e.Uuid, e.IsExcluded);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[OmniMix] Failed to raise UI exclusion change event: {ex}");
            }
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
