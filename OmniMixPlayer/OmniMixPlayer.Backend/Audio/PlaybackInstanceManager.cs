using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public enum PlaybackClientRole
    {
        Audio,
        Controller,
        Observer
    }

    public sealed class PlaybackClientSession
    {
        public string ClientId { get; init; }
        public PlaybackClientRole Role { get; init; }
        public DateTime ConnectedAt { get; init; }
        public DateTime LastHeartbeat { get; set; }
    }

    public sealed class PlaybackInstance : IDisposable
    {
        public string Id { get; init; }
        public string ClientId { get; init; }
        public PlaybackMode Mode { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime? DetachedAt { get; set; }
        public SharedMemoryServer SharedMemory { get; init; }
        public PlaybackController Controller { get; init; }
        public bool IsAttached => DetachedAt == null;
        public string SharedMemoryName => SharedMemory?.MapName;

        public void Dispose()
        {
            Controller?.Dispose();
            SharedMemory?.Dispose();
        }
    }

    public sealed class PlaybackInstanceManager : IDisposable
    {
        private const int HeartbeatTimeoutSeconds = 30;
        private const int DetachedTtlSeconds = 120;
        private const int CleanupIntervalSeconds = 10;

        private readonly object _lock = new object();
        private readonly Dictionary<string, PlaybackInstance> _instances = new Dictionary<string, PlaybackInstance>();
        private readonly Dictionary<string, PlaybackClientSession> _clients = new Dictionary<string, PlaybackClientSession>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IMusicRegistry _musicRegistry;
        private readonly IStreamingService _streamingService;
        private readonly string _configBaseDir;
        private readonly CancellationTokenSource _cleanupCts = new CancellationTokenSource();
        private bool _disposed;

        public event Action<string, MusicInfo> OnTrackChanged;
        public event Action<string, PlaybackController> OnStateChanged;
        public event Action<string, float> OnPositionChanged;
        public event Action<string> OnQueueChanged;
        public event Action OnInstancesChanged;

        public PlaybackInstanceManager(
            ILoggerFactory loggerFactory,
            IEventBus eventBus,
            IMusicRegistry musicRegistry,
            IStreamingService streamingService,
            string configBaseDir = null)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger("PlaybackInstanceManager");
            _eventBus = eventBus;
            _musicRegistry = musicRegistry;
            _streamingService = streamingService;
            _configBaseDir = configBaseDir;
            _ = Task.Run(() => CleanupLoopAsync(_cleanupCts.Token));
        }

        public object Connect(string clientId, PlaybackClientRole role, PlaybackMode mode)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("clientId is required", nameof(clientId));

            clientId = SanitizeId(clientId);
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                _clients[clientId] = new PlaybackClientSession
                {
                    ClientId = clientId,
                    Role = role,
                    ConnectedAt = now,
                    LastHeartbeat = now
                };

                if (role != PlaybackClientRole.Audio)
                {
                    OnInstancesChanged?.Invoke();
                    return new
                    {
                        clientId,
                        role = role.ToString().ToLowerInvariant(),
                        connectedAt = now.ToString("O")
                    };
                }

                if (!_instances.TryGetValue(clientId, out var instance))
                {
                    var mapName = $@"Global\OmniMixPlayer_PCM_{clientId}";
                    var sharedMemory = new SharedMemoryServer(_loggerFactory.CreateLogger($"SharedMemory.{clientId}"), mapName);
                    if (!sharedMemory.Initialize())
                        _logger.LogWarning("Failed to initialize shared memory for instance {InstanceId}", clientId);

                    var configDir = _configBaseDir != null
                        ? Path.Combine(_configBaseDir, "instances", clientId)
                        : null;
                    if (configDir != null) Directory.CreateDirectory(configDir);

                    var controller = new PlaybackController(
                        _loggerFactory.CreateLogger($"Playback.{clientId}"),
                        sharedMemory,
                        _eventBus,
                        _musicRegistry,
                        _streamingService,
                        configDir: configDir,
                        mode: mode);
                    WireController(clientId, controller);

                    instance = new PlaybackInstance
                    {
                        Id = clientId,
                        ClientId = clientId,
                        Mode = mode,
                        CreatedAt = now,
                        LastHeartbeat = now,
                        SharedMemory = sharedMemory,
                        Controller = controller
                    };
                    _instances[clientId] = instance;
                    SaveInstanceMeta(clientId, mode);
                    _logger.LogInformation("Created playback instance {InstanceId} mode={Mode} sharedMemory={SharedMemory}", clientId, mode, mapName);
                }
                else
                {
                    // Update mode on reattach (e.g. if client changed from ClientManaged to ServerManaged or vice versa)
                    if (instance.Mode != mode)
                    {
                        _logger.LogInformation("Updating instance {InstanceId} mode from {OldMode} to {NewMode}", clientId, instance.Mode, mode);
                        instance.Mode = mode;
                        SaveInstanceMeta(clientId, mode);
                    }
                    instance.DetachedAt = null;
                    instance.LastHeartbeat = now;
                    _logger.LogInformation("Reattached playback instance {InstanceId}", clientId);
                }

                OnInstancesChanged?.Invoke();
                return MapConnectResponse(instance);
            }
        }

        public bool Heartbeat(string id)
        {
            id = SanitizeId(id);
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (_clients.TryGetValue(id, out var client))
                    client.LastHeartbeat = now;
                if (_instances.TryGetValue(id, out var instance))
                {
                    instance.LastHeartbeat = now;
                    instance.DetachedAt = null;
                    return true;
                }
                return _clients.ContainsKey(id);
            }
        }

        public bool Disconnect(string id)
        {
            id = SanitizeId(id);
            lock (_lock)
            {
                _clients.Remove(id);
                if (_instances.TryGetValue(id, out var instance))
                {
                    instance.DetachedAt = DateTime.UtcNow;
                    instance.Controller.Stop();
                    OnInstancesChanged?.Invoke();
                    return true;
                }
                OnInstancesChanged?.Invoke();
                return true;
            }
        }

        public bool Delete(string id)
        {
            id = SanitizeId(id);
            bool deleted = false;
            PlaybackInstance instance = null;
            lock (_lock)
            {
                _clients.Remove(id);
                if (_instances.TryGetValue(id, out instance))
                {
                    _instances.Remove(id);
                    deleted = true;
                }
            }
            instance?.Dispose();

            // Also clean up offline profile directory if it exists
            if (!string.IsNullOrEmpty(_configBaseDir))
            {
                var profileDir = Path.Combine(_configBaseDir, "instances", id);
                if (Directory.Exists(profileDir))
                {
                    try { Directory.Delete(profileDir, true); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete profile dir for {Id}", id); }
                    deleted = true;
                }
            }

            if (deleted)
            {
                _logger.LogInformation("Deleted instance {InstanceId} (was {State})", id,
                    instance != null ? "online" : "offline");
                OnInstancesChanged?.Invoke();
            }
            return deleted;
        }

        public PlaybackInstance Get(string id)
        {
            id = SanitizeId(id);
            lock (_lock)
            {
                _instances.TryGetValue(id, out var instance);
                return instance;
            }
        }

        /// <summary>
        /// Get an instance profile. Works both online (via controller) and offline
        /// (reads from config/instances/{id}/playback_state.json).
        /// </summary>
        public object GetProfile(string id)
        {
            id = SanitizeId(id);
            // Online path
            PlaybackInstance instance;
            lock (_lock) { _instances.TryGetValue(id, out instance); }
            if (instance != null)
                return instance.Controller.GetProfile();

            // Offline path: read from file
            if (string.IsNullOrEmpty(_configBaseDir)) return null;
            try
            {
                var filePath = Path.Combine(_configBaseDir, "instances", id, "playback_state.json");
                if (!File.Exists(filePath)) return null;
                var json = File.ReadAllText(filePath);
                return System.Text.Json.JsonSerializer.Deserialize<object>(json);
            }
            catch { return null; }
        }

        /// <summary>
        /// Update an instance profile — online via controller, offline via file.
        /// </summary>
        public bool UpdateProfile(string id, string json)
        {
            id = SanitizeId(id);
            // Online path: use the controller
            PlaybackInstance instance;
            lock (_lock) { _instances.TryGetValue(id, out instance); }
            if (instance != null)
                return instance.Controller.UpdateProfile(json);

            // Offline path: write directly to config file
            if (string.IsNullOrEmpty(_configBaseDir)) return false;
            try
            {
                var dir = Path.Combine(_configBaseDir, "instances", id);
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, "playback_state.json");
                File.WriteAllText(filePath, json);
                _logger.LogInformation("Updated offline profile for instance {Id} at {Path}", id, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write offline profile for instance {Id}", id);
                return false;
            }
        }

        // ── Archive management ──

        private string ArchiveDir => _configBaseDir != null
            ? Path.Combine(_configBaseDir, "instances", ".archive")
            : null;

        public object ListArchives()
        {
            if (ArchiveDir == null || !Directory.Exists(ArchiveDir)) return Array.Empty<object>();
            var list = new List<object>();
            foreach (var file in Directory.GetFiles(ArchiveDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var entry = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (entry == null) continue;
                    list.Add(new
                    {
                        instanceId = entry.TryGetValue("instanceId", out var v) ? v.GetString() : "",
                        label = entry.TryGetValue("label", out var l) ? l.GetString() : "",
                        archivedAt = entry.TryGetValue("archivedAt", out var d) ? d.GetString() : "",
                        modId = entry.TryGetValue("modId", out var m) ? m.GetString() : "",
                        mode = entry.TryGetValue("mode", out var md) ? md.GetString() : "",
                    });
                }
                catch { }
            }
            return list;
        }

        public bool DeleteArchive(string id)
        {
            if (ArchiveDir == null) return false;
            // Don't delete if the instance is currently online
            lock (_lock)
            {
                if (_instances.ContainsKey(id))
                {
                    _logger.LogWarning("Cannot delete archive {Id}: instance is currently online", id);
                    return false;
                }
            }
            var path = Path.Combine(ArchiveDir, $"{id}.json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            _logger.LogInformation("Deleted archive {Id}", id);
            return true;
        }

        /// <summary>
        /// Inherit profile from an archive to a new instance.
        /// Returns "consumed" if archive is not bound to any existing instance (profile moved).
        /// Returns "copied" if archive is bound to a live instance (profile copied, archive stays).
        /// Returns "not_found" if the archive doesn't exist.
        /// </summary>
        public string InheritFromArchive(string newInstanceId, string archiveId)
        {
            if (string.IsNullOrEmpty(_configBaseDir) || ArchiveDir == null)
                return "error";

            var archivePath = Path.Combine(ArchiveDir, $"{archiveId}.json");
            if (!File.Exists(archivePath)) return "not_found";

            // Check if archive is bound to a live instance (online or has profile dir)
            bool isBound;
            lock (_lock)
            {
                isBound = _instances.ContainsKey(archiveId);
            }
            if (!isBound)
            {
                var existingProfileDir = Path.Combine(_configBaseDir, "instances", archiveId);
                isBound = Directory.Exists(existingProfileDir) &&
                          File.Exists(Path.Combine(existingProfileDir, "playback_state.json"));
            }

            // Ensure target instance profile directory exists
            var targetDir = Path.Combine(_configBaseDir, "instances", newInstanceId);
            Directory.CreateDirectory(targetDir);
            var targetFile = Path.Combine(targetDir, "playback_state.json");

            if (!isBound)
            {
                // Consume: move archive profile to new instance
                // The archive stores metadata, we need to find the actual profile
                // Archive JSON contains instanceId pointing to the original profile
                try
                {
                    var archiveJson = File.ReadAllText(archivePath);
                    using var doc = JsonDocument.Parse(archiveJson);
                    var originalId = archiveId; // The archive ID is the original instance ID

                    // Try to copy from original instance profile if it still exists
                    var originalProfile = Path.Combine(_configBaseDir, "instances", originalId, "playback_state.json");
                    if (File.Exists(originalProfile))
                    {
                        File.Copy(originalProfile, targetFile, true);
                    }
                    else
                    {
                        // No original profile — create empty default
                        var defaultProfile = "{\"ActiveQueueId\":\"default\",\"Volume\":1.0,\"Queues\":[{\"Id\":\"default\",\"Name\":\"Default\",\"PlaylistSources\":[],\"SongUuids\":[],\"HistoryUuids\":[],\"Index\":-1,\"HistoryPosition\":-1,\"PlaylistPosition\":0,\"Shuffle\":false,\"RepeatMode\":\"none\"}]}";
                        File.WriteAllText(targetFile, defaultProfile);
                    }

                    // Delete the archive since it's consumed
                    File.Delete(archivePath);
                    _logger.LogInformation("Consumed archive {ArchiveId} → new instance {NewId}", archiveId, newInstanceId);
                    return "consumed";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to consume archive {ArchiveId}", archiveId);
                    return "error";
                }
            }
            else
            {
                // Copy: archive is bound, just copy the profile
                try
                {
                    var originalProfile = Path.Combine(_configBaseDir, "instances", archiveId, "playback_state.json");
                    if (File.Exists(originalProfile))
                    {
                        File.Copy(originalProfile, targetFile, true);
                    }
                    else
                    {
                        var defaultProfile = "{\"ActiveQueueId\":\"default\",\"Volume\":1.0,\"Queues\":[{\"Id\":\"default\",\"Name\":\"Default\",\"PlaylistSources\":[],\"SongUuids\":[],\"HistoryUuids\":[],\"Index\":-1,\"HistoryPosition\":-1,\"PlaylistPosition\":0,\"Shuffle\":false,\"RepeatMode\":\"none\"}]}";
                        File.WriteAllText(targetFile, defaultProfile);
                    }
                    _logger.LogInformation("Copied archive {ArchiveId} profile → new instance {NewId}", archiveId, newInstanceId);
                    return "copied";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to copy archive {ArchiveId}", archiveId);
                    return "error";
                }
            }
        }

        public bool RenameArchive(string id, string label)
        {
            if (ArchiveDir == null) return false;
            var path = Path.Combine(ArchiveDir, $"{id}.json");
            if (!File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var dict = new Dictionary<string, object>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                    dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText();
                dict["label"] = label;
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(dict));
                return true;
            }
            catch { return false; }
        }

        public bool ArchiveInstance(string id, string label = "", string modId = "", string mode = "")
        {
            if (string.IsNullOrEmpty(_configBaseDir)) return false;
            var profileDir = Path.Combine(_configBaseDir, "instances", id);
            var profileFile = Path.Combine(profileDir, "playback_state.json");
            if (!File.Exists(profileFile)) return false;
            try
            {
                var archiveDir = ArchiveDir;
                Directory.CreateDirectory(archiveDir);
                var archive = new Dictionary<string, object>
                {
                    ["instanceId"] = id,
                    ["archivedAt"] = DateTime.UtcNow.ToString("O"),
                    ["label"] = label ?? "",
                    ["modId"] = modId ?? "",
                    ["mode"] = mode ?? ""
                };
                File.WriteAllText(Path.Combine(archiveDir, $"{id}.json"),
                    System.Text.Json.JsonSerializer.Serialize(archive));
                _logger.LogInformation("Archived instance {Id} with label '{Label}'", id, label);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to archive instance {Id}", id);
                return false;
            }
        }

        public IReadOnlyList<PlaybackInstance> ListInstances()
        {
            lock (_lock)
                return _instances.Values.ToList();
        }

        /// <summary>
        /// Save instance metadata (modId, gameName) alongside the profile.
        /// </summary>
        public bool SetInstanceMeta(string id, string modId, string gameName, string mode = "")
        {
            if (string.IsNullOrEmpty(_configBaseDir)) return false;
            try
            {
                var dir = Path.Combine(_configBaseDir, "instances", id);
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "meta.json");

                // Preserve existing fields if present
                var meta = new Dictionary<string, object>();
                if (File.Exists(path))
                {
                    try
                    {
                        var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
                        if (existing != null)
                        {
                            foreach (var kv in existing) meta[kv.Key] = kv.Value;
                        }
                    }
                    catch { }
                }

                meta["modId"] = modId ?? "";
                meta["gameName"] = gameName ?? "";
                if (!string.IsNullOrWhiteSpace(mode))
                    meta["mode"] = NormalizeMode(mode);
                else if (!meta.ContainsKey("mode"))
                    meta["mode"] = "ServerManaged";

                File.WriteAllText(path, JsonSerializer.Serialize(meta));
                return true;
            }
            catch { return false; }
        }

        private (string modId, string gameName) GetInstanceMeta(string id)
        {
            if (string.IsNullOrEmpty(_configBaseDir)) return ("", "");
            try
            {
                var path = Path.Combine(_configBaseDir, "instances", id, "meta.json");
                if (!File.Exists(path)) return ("", "");
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var modId = doc.RootElement.TryGetProperty("modId", out var m) ? m.GetString() ?? "" : "";
                var gameName = doc.RootElement.TryGetProperty("gameName", out var g) ? g.GetString() ?? "" : "";
                return (modId, gameName);
            }
            catch { return ("", ""); }
        }

        public object ListInstanceDtos()
        {
            var result = new List<object>();
            var seenIds = new HashSet<string>();

            // Online instances first
            lock (_lock)
            {
                foreach (var inst in _instances.Values)
                {
                    seenIds.Add(inst.Id);
                    var (modId, gameName) = GetInstanceMeta(inst.Id);
                    result.Add(MapInstanceWithMeta(inst, modId, gameName));
                }
            }

            // Offline instances: scan config directory for profile files
            if (!string.IsNullOrEmpty(_configBaseDir))
            {
                var instancesDir = Path.Combine(_configBaseDir, "instances");
                if (Directory.Exists(instancesDir))
                {
                    foreach (var dir in Directory.GetDirectories(instancesDir))
                    {
                        var id = Path.GetFileName(dir);
                        if (seenIds.Contains(id)) continue;
                        var profileFile = Path.Combine(dir, "playback_state.json");
                        if (!File.Exists(profileFile)) continue;

                        var (modId, gameName) = GetInstanceMeta(id);

                        int queueCount = 0;
                        try
                        {
                            var json = File.ReadAllText(profileFile);
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("Queues", out var queues) && queues.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var q in queues.EnumerateArray())
                                {
                                    if (q.TryGetProperty("SongUuids", out var su) && su.ValueKind == JsonValueKind.Array)
                                        queueCount += su.GetArrayLength();
                                }
                            }
                        }
                        catch { }

                        var savedMode = ReadInstanceMode(id);

                        result.Add(new
                        {
                            id,
                            clientId = id,
                            role = "audio",
                            mode = savedMode,
                            attached = false,
                            isPlaying = false,
                            position = 0.0,
                            volume = 1.0,
                            queueCount,
                            queueIndex = 0,
                            historyCount = 0,
                            sampleRate = 0,
                            channels = 0,
                            shuffle = false,
                            repeatMode = "none",
                            currentTrack = (object)null,
                            sharedMemoryName = (string)null,
                            modId,
                            gameName
                        });
                    }
                }
            }

            return result;
        }

        public object GetStats()
        {
            lock (_lock)
            {
                var instances = _instances.Values.Select(MapInstance).ToList();
                return new
                {
                    instanceCount = _instances.Count,
                    attachedAudioClients = _instances.Values.Count(i => i.IsAttached),
                    controllerClients = _clients.Values.Count(c => c.Role == PlaybackClientRole.Controller),
                    observerClients = _clients.Values.Count(c => c.Role == PlaybackClientRole.Observer),
                    sharedMemoryBytes = _instances.Values.Sum(i => i.SharedMemory?.TotalSize ?? 0),
                    activeDecoders = _instances.Values.Count(i => i.Controller.IsPlaying),
                    totalQueueItems = _instances.Values.Sum(i => i.Controller.QueueCount),
                    heartbeatTimeoutSeconds = HeartbeatTimeoutSeconds,
                    detachedTtlSeconds = DetachedTtlSeconds,
                    cleanupIntervalSeconds = CleanupIntervalSeconds,
                    instances
                };
            }
        }

        private void WireController(string instanceId, PlaybackController controller)
        {
            controller.OnTrackChanged += track => OnTrackChanged?.Invoke(instanceId, track);
            controller.OnStateChanged += _ => OnStateChanged?.Invoke(instanceId, controller);
            controller.OnPositionChanged += position => OnPositionChanged?.Invoke(instanceId, position);
            controller.OnQueueChanged += () => OnQueueChanged?.Invoke(instanceId);
        }

        private async Task CleanupLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(CleanupIntervalSeconds), ct);
                    CleanupExpired();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Playback instance cleanup failed");
                }
            }
        }

        private void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            var expired = new List<string>();

            lock (_lock)
            {
                foreach (var instance in _instances.Values)
                {
                    if (instance.DetachedAt == null && (now - instance.LastHeartbeat).TotalSeconds > HeartbeatTimeoutSeconds)
                    {
                        instance.DetachedAt = now;
                        instance.Controller.Stop();
                    }

                    if (instance.DetachedAt != null && (now - instance.DetachedAt.Value).TotalSeconds > DetachedTtlSeconds)
                        expired.Add(instance.Id);
                }
            }

            foreach (var id in expired)
                Delete(id);
        }

        private static object MapConnectResponse(PlaybackInstance instance) => new
        {
            instanceId = instance.Id,
            clientId = instance.ClientId,
            role = "audio",
            mode = instance.Mode.ToString(),
            sharedMemoryName = instance.SharedMemoryName,
            cleanupAfterSeconds = DetachedTtlSeconds,
            heartbeatTimeoutSeconds = HeartbeatTimeoutSeconds
        };

        private static object MapInstance(PlaybackInstance instance) => new
        {
            id = instance.Id,
            clientId = instance.ClientId,
            role = "audio",
            mode = instance.Mode.ToString(),
            attached = instance.IsAttached,
            isPlaying = instance.Controller.IsPlaying,
            position = instance.Controller.Position,
            volume = instance.Controller.Volume,
            queueCount = instance.Controller.QueueCount,
            queueIndex = instance.Controller.QueueIndex,
            historyCount = instance.Controller.HistoryCount,
            shuffle = instance.Controller.Shuffle,
            repeatMode = instance.Controller.RepeatMode.ToString().ToLowerInvariant(),
            sampleRate = instance.SharedMemory?.SampleRate ?? 0,
            channels = instance.SharedMemory?.Channels ?? 0,
            sharedMemoryName = instance.SharedMemoryName,
            sharedMemoryBytes = instance.SharedMemory?.TotalSize ?? 0,
            connectedAt = instance.CreatedAt.ToString("O"),
            lastHeartbeat = instance.LastHeartbeat.ToString("O"),
            detachedAt = instance.DetachedAt?.ToString("O"),
            expiresAt = instance.DetachedAt?.AddSeconds(DetachedTtlSeconds).ToString("O"),
            currentTrack = instance.Controller.CurrentTrack == null ? null : new
            {
                uuid = instance.Controller.CurrentTrack.UUID,
                title = instance.Controller.CurrentTrack.Title,
                artist = instance.Controller.CurrentTrack.Artist,
                albumId = instance.Controller.CurrentTrack.AlbumId,
                duration = instance.Controller.CurrentTrack.Duration,
                moduleId = instance.Controller.CurrentTrack.ModuleId
            },
            modId = "",
            gameName = ""
        };

        private static object MapInstanceWithMeta(PlaybackInstance instance, string modId, string gameName) => new
        {
            id = instance.Id,
            clientId = instance.ClientId,
            role = "audio",
            mode = instance.Mode.ToString(),
            attached = instance.IsAttached,
            isPlaying = instance.Controller.IsPlaying,
            position = instance.Controller.Position,
            volume = instance.Controller.Volume,
            queueCount = instance.Controller.QueueCount,
            queueIndex = instance.Controller.QueueIndex,
            historyCount = instance.Controller.HistoryCount,
            shuffle = instance.Controller.Shuffle,
            repeatMode = instance.Controller.RepeatMode.ToString().ToLowerInvariant(),
            sampleRate = instance.SharedMemory?.SampleRate ?? 0,
            channels = instance.SharedMemory?.Channels ?? 0,
            sharedMemoryName = instance.SharedMemoryName,
            sharedMemoryBytes = instance.SharedMemory?.TotalSize ?? 0,
            connectedAt = instance.CreatedAt.ToString("O"),
            lastHeartbeat = instance.LastHeartbeat.ToString("O"),
            detachedAt = instance.DetachedAt?.ToString("O"),
            expiresAt = instance.DetachedAt?.AddSeconds(DetachedTtlSeconds).ToString("O"),
            currentTrack = instance.Controller.CurrentTrack == null ? null : new
            {
                uuid = instance.Controller.CurrentTrack.UUID,
                title = instance.Controller.CurrentTrack.Title,
                artist = instance.Controller.CurrentTrack.Artist,
                albumId = instance.Controller.CurrentTrack.AlbumId,
                duration = instance.Controller.CurrentTrack.Duration,
                moduleId = instance.Controller.CurrentTrack.ModuleId
            },
            modId,
            gameName
        };

        private void SaveInstanceMeta(string id, PlaybackMode mode)
        {
            if (string.IsNullOrEmpty(_configBaseDir)) return;
            try
            {
                var dir = Path.Combine(_configBaseDir, "instances", id);
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "meta.json");

                // Read existing meta (modId, gameName) and merge with mode
                var meta = new Dictionary<string, object>();
                if (File.Exists(path))
                {
                    try
                    {
                        var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
                        if (existing != null)
                        {
                            foreach (var kv in existing) meta[kv.Key] = kv.Value;
                        }
                    }
                    catch { }
                }
                meta["mode"] = mode.ToString();
                File.WriteAllText(path, JsonSerializer.Serialize(meta));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save instance meta for {Id}", id);
            }
        }

        private string ReadInstanceMode(string id)
        {
            if (string.IsNullOrEmpty(_configBaseDir)) return "ServerManaged";
            try
            {
                var path = Path.Combine(_configBaseDir, "instances", id, "meta.json");
                if (!File.Exists(path)) return "ServerManaged";
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("mode", out var modeProp))
                {
                    var m = modeProp.GetString();
                    if (!string.IsNullOrWhiteSpace(m)) return NormalizeMode(m);
                }
            }
            catch { }
            return "ServerManaged";
        }

        /// Normalize mode string to PlaybackMode enum format:
        /// "client"/"ClientManaged" → "ClientManaged", "server"/"ServerManaged" → "ServerManaged"
        private static string NormalizeMode(string mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "client" => "ClientManaged",
                "server" => "ServerManaged",
                _ => mode
            };
        }

        private static string SanitizeId(string id)
        {
            var chars = id.Trim().Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray();
            var sanitized = new string(chars);
            return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cleanupCts.Cancel();
            _cleanupCts.Dispose();
            List<PlaybackInstance> instances;
            lock (_lock)
            {
                instances = _instances.Values.ToList();
                _instances.Clear();
                _clients.Clear();
            }
            foreach (var instance in instances)
                instance.Dispose();
        }
    }

    public sealed class NullPlayQueue : IPlayQueue
    {
        public MusicInfo CurrentTrack => null;
        public bool IsPlaying => false;
        public float Position => 0;
        public float Volume { get; set; } = 1f;
        public bool Shuffle { get; set; }
        public RepeatMode RepeatMode { get; set; }
        public IReadOnlyList<MusicInfo> Queue => Array.Empty<MusicInfo>();
        public int QueueCount => 0;
        public IReadOnlyList<MusicInfo> History => Array.Empty<MusicInfo>();
        public int HistoryCount => 0;
        public int PlaylistPosition => 0;
        public bool CanGoPrevious => false;
        public bool CanGoNext => false;
        public event EventHandler<QueueChangedEventArgs> OnQueueChanged;
        public event EventHandler<MusicInfo> OnTrackChanged;
        public event EventHandler OnStateChanged;
        public event EventHandler<float> OnPositionChanged;
        public void Play(string uuid = null) { }
        public void Pause() { }
        public void Resume() { }
        public void Toggle() { }
        public void Next() { }
        public void Prev() { }
        public void Seek(float position) { }
        public void SetVolume(float volume) => Volume = volume;
        public void SetShuffle(bool enabled) => Shuffle = enabled;
        public void SetRepeatMode(RepeatMode mode) => RepeatMode = mode;
        public void AddToQueue(string uuid) { }
        public void AddToQueueRange(IEnumerable<string> uuids) { }
        public void RemoveFromQueue(int index) { }
        public void MoveInQueue(int fromIndex, int toIndex) { }
        public void ClearQueue() { }
        public void ClearHistory() { }
        public void ImportFromPlaylist(IReadOnlyList<MusicInfo> songs, bool replace = true) { }
        public bool IsFavorite(string uuid) => false;
        public void SetFavorite(string uuid, bool isFavorite) { }
        public bool IsExcluded(string uuid) => false;
        public void SetExcluded(string uuid, bool isExcluded) { }
    }
}
