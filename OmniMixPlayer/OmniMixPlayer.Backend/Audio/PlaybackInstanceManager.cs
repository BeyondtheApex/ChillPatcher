using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public PlaybackMode Mode { get; init; }
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
                    _logger.LogInformation("Created playback instance {InstanceId} sharedMemory={SharedMemory}", clientId, mapName);
                }
                else
                {
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
            PlaybackInstance instance = null;
            lock (_lock)
            {
                _clients.Remove(id);
                if (_instances.TryGetValue(id, out instance))
                    _instances.Remove(id);
            }
            instance?.Dispose();
            if (instance != null)
            {
                _logger.LogInformation("Deleted playback instance {InstanceId}", id);
                OnInstancesChanged?.Invoke();
            }
            return instance != null;
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

        public IReadOnlyList<PlaybackInstance> ListInstances()
        {
            lock (_lock)
                return _instances.Values.ToList();
        }

        public object ListInstanceDtos()
        {
            lock (_lock)
                return _instances.Values.Select(MapInstance).ToList();
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
            }
        };

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
