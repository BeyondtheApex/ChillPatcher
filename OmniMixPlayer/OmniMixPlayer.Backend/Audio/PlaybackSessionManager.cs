using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{




    public sealed class PlaybackSessionManager : IDisposable
    {
        private const int HeartbeatTimeoutSeconds = 30;
        private const int DetachedTtlSeconds = 120;
        private const int CleanupIntervalSeconds = 10;

        private readonly object _lock = new();
        private readonly Dictionary<string, PlaybackSession> _sessions = new();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly ILibraryRegistry _library;
        private readonly IStreamingService _streamingService;
        private readonly InstanceRegistry _registry;
        private readonly CancellationTokenSource _cleanupCts = new();
        private bool _disposed;

        public event Action<string, Track> OnTrackChanged;
        public event Action<string, PlaybackController> OnStateChanged;
        public event Action<string, float> OnPositionChanged;
        public event Action<string> OnQueueChanged;
        public event Action OnSessionsChanged;

        public PlaybackSessionManager(
            ILoggerFactory loggerFactory,
            IEventBus eventBus,
            ILibraryRegistry library,
            IStreamingService streamingService,
            InstanceRegistry registry)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger("PlaybackSessionManager");
            _eventBus = eventBus;
            _library = library;
            _streamingService = streamingService;
            _registry = registry;
            _ = Task.Run(() => CleanupLoopAsync(_cleanupCts.Token));
        }




        public PlaybackSession Attach(InstanceProfile profile)
        {
            var id = profile.Id;
            lock (_lock)
            {
                if (_sessions.TryGetValue(id, out var existing))
                {
                    existing.DetachedAt = null;
                    existing.LastHeartbeat = DateTime.UtcNow;
                    _logger.LogInformation("Reattached session {Id}", id);
                    return existing;
                }

                var caps = profile.Capabilities ?? new InstanceCapabilities();
                var needsAudio = caps.ServerControlledPlayback;

                SharedMemoryServer sharedMemory = null;
                if (needsAudio)
                {
                    var mapName = $@"Global\OmniMixPlayer_PCM_{id}";
                    sharedMemory = new SharedMemoryServer(_loggerFactory.CreateLogger($"SharedMemory.{id}"), mapName);
                    if (!sharedMemory.Initialize())
                        _logger.LogWarning("Failed to initialize shared memory for {Id}", id);
                }
                else
                {
                    _logger.LogInformation("Skipping SharedMemory for {Id}: no server-controlled playback capability", id);
                }

                var controller = new PlaybackController(
                    _loggerFactory.CreateLogger($"Playback.{id}"),
                    sharedMemory,
                    _eventBus,
                    _library,
                    _streamingService,
                    instanceId: id,
                    mode: profile.Mode);
                controller.ApplyProfile(profile);
                WireController(id, controller);

                var session = new PlaybackSession
                {
                    Id = id,
                    ClientId = id,
                    Mode = profile.Mode,
                    Kind = profile.Kind,
                    CreatedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    SharedMemory = sharedMemory,
                    Controller = controller
                };
                _sessions[id] = session;

                _logger.LogInformation("Created session {Id} kind={Kind} mode={Mode}", id, profile.Kind, profile.Mode);
                OnSessionsChanged?.Invoke();
                return session;
            }
        }

        public bool Heartbeat(string id)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(id, out var s))
                {
                    s.LastHeartbeat = DateTime.UtcNow;
                    s.DetachedAt = null;
                    return true;
                }
                return false;
            }
        }

        public bool Detach(string id)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(id, out var s))
                {
                    s.DetachedAt = DateTime.UtcNow;
                    s.Controller.Stop();
                    OnSessionsChanged?.Invoke();
                    return true;
                }
                return false;
            }
        }

        public bool Remove(string id)
        {
            PlaybackSession s;
            lock (_lock)
            {
                if (!_sessions.Remove(id, out s)) return false;
            }
            s?.Dispose();
            _logger.LogInformation("Removed session {Id}", id);
            OnSessionsChanged?.Invoke();
            return true;
        }

        public PlaybackSession Get(string id)
        {
            lock (_lock) { _sessions.TryGetValue(id, out var s); return s; }
        }

        public PlaybackController GetController(string id) => Get(id)?.Controller;
        public bool IsOnline(string id) { lock (_lock) { return _sessions.TryGetValue(id, out var s) && s.IsAttached; } }

        public string GetCurrentTrackUuid(string id) => GetController(id)?.CurrentTrack?.Uuid ?? "";
        public int GetQueueCount(string id) => GetController(id)?.QueueCount ?? 0;

        public PlaybackStatus GetPlaybackStatus(string id)
        {
            var ctrl = GetController(id);
            if (ctrl == null) return null;
            var track = ctrl.CurrentTrack;
            return new PlaybackStatus
            {
                TrackUuid = track?.Uuid ?? "",
                Title = track?.Title ?? "",
                Artist = track?.Artist ?? "",
                AlbumId = track?.AlbumId ?? "",
                Duration = track?.Duration ?? 0,
                Position = ctrl.Position,
                IsPlaying = ctrl.IsPlaying,
                Shuffle = ctrl.Shuffle,
                RepeatMode = ctrl.RepeatMode,
                Volume = ctrl.Volume
            };
        }

        private void WireController(string id, PlaybackController ctrl)
        {
            ctrl.OnTrackChanged += track => OnTrackChanged?.Invoke(id, track);
            ctrl.OnStateChanged += state => OnStateChanged?.Invoke(id, ctrl);
            ctrl.OnPositionChanged += pos => OnPositionChanged?.Invoke(id, pos);
            ctrl.OnQueueChanged += () =>
            {
                _registry?.SavePlaybackQueue(id, ctrl.CreateQueueSnapshot());
                OnQueueChanged?.Invoke(id);
            };
        }

        private async Task CleanupLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(CleanupIntervalSeconds), ct); }
                catch (OperationCanceledException) { break; }
                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    var timedOut = _sessions.Values
                        .Where(s => s.IsAttached && (now - s.LastHeartbeat).TotalSeconds > HeartbeatTimeoutSeconds).ToList();
                    foreach (var s in timedOut) { s.DetachedAt = now; s.Controller.Stop(); }
                    var expired = _sessions.Values
                        .Where(s => !s.IsAttached && s.DetachedAt.HasValue && (now - s.DetachedAt.Value).TotalSeconds > DetachedTtlSeconds).ToList();
                    foreach (var s in expired) { _sessions.Remove(s.Id); s.Dispose(); }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cleanupCts.Cancel(); _cleanupCts.Dispose();
            lock (_lock) { foreach (var s in _sessions.Values) s.Dispose(); _sessions.Clear(); }
        }
    }
}
