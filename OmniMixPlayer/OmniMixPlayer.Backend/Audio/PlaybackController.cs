using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public class PlaybackController : IDisposable
    {
        private readonly ILogger _logger;
        private readonly SharedMemoryServer _sharedMemory;
        private readonly IEventBus _eventBus;
        private readonly ILibraryRegistry _library;
        private readonly IStreamingService _streamingService;
        public string Id { get; }
        private readonly Equalizer _equalizer = new();
        public Equalizer Equalizer => _equalizer;

        private readonly Dictionary<string, QueueSlot> _queues = new();
        private string _activeQueueId;
        private const string DefaultQueueId = "default";

        private CancellationTokenSource _playbackCts;
        private Task _playbackTask;
        private int _playbackGeneration;
        private readonly VolumeNode _volumeNode = new();
        public VolumeNode VolumeNode => _volumeNode;
        private volatile int _playState;
        private float _targetLatency = 0.1f;
        private readonly Random _rng = new();
        private readonly object _lock = new();

        private IPcmStreamReader _currentReader;
        private bool _disposed;

        public event Action<Track> OnTrackChanged;
        public event Action<int> OnStateChanged;
        public event Action<float> OnPositionChanged;
        public event Action OnQueueChanged;

        private QueueSlot Active
        {
            get
            {
                lock (_lock)
                {
                    if (_activeQueueId == null || !_queues.TryGetValue(_activeQueueId, out var q))
                        return _queues[DefaultQueueId];
                    return q;
                }
            }
        }

        public Track CurrentTrack => Active.CurrentTrack;
        public bool IsPlaying => _playState == 1;
        public float Position { get; private set; }
        public float Volume { get => _volumeNode.Volume; set => _volumeNode.Volume = value; }
        public float TargetLatency { get => _targetLatency; set => _targetLatency = Math.Clamp(value, 0.03f, 1.0f); }
        public bool Shuffle { get => Active.Shuffle; set => Active.Shuffle = value; }
        public SDK.Protos.Models.RepeatMode RepeatMode { get => Active.RepeatMode; set => Active.RepeatMode = value; }
        public IReadOnlyList<Track> Queue => Active.Queue;
        public int QueueCount => Active.QueueCount;
        public IReadOnlyList<Track> History => Active.History;
        public int HistoryCount => Active.HistoryCount;
        public IReadOnlyList<Track> Playlist => Active.Playlist;
        public IReadOnlyList<PlaylistSourceInfo> PlaylistSources => Active.PlaylistSources;
        public IReadOnlyList<PlaylistSourceRequest> PlaylistSourceSpecs => Active.PlaylistSourceSpecs;
        public int PlaylistPosition => Active.PlaylistPosition;
        public bool CanGoPrevious => Active.CanGoPrevious;
        public bool CanGoNext => Active.CanGoNext;
        public string ActiveQueueId => _activeQueueId;
        public IReadOnlyList<string> QueueIds => _queues.Keys.ToList();

        public PlaybackController(
            ILogger logger,
            SharedMemoryServer sharedMemory,  // nullable — null for non-audio clients
            IEventBus eventBus,
            ILibraryRegistry library,
            IStreamingService streamingService,
            string instanceId)
        {
            _logger = logger;
            _sharedMemory = sharedMemory;
            _eventBus = eventBus;
            _library = library;
            _streamingService = streamingService;
            Id = instanceId;

            _queues[DefaultQueueId] = CreateQueueSlot(DefaultQueueId, "Default");
            _activeQueueId = DefaultQueueId;
        }

        private QueueSlot CreateQueueSlot(string id, string name)
        {
            return new QueueSlot(id, name)
            {
                SourceResolver = ResolvePlaylistSource
            };
        }

        public void ApplyProfile(InstanceProfile profile)
        {
            if (profile == null) return;
            lock (_lock)
            {
                SetVolume(profile.Volume);
                SetTargetLatency(profile.TargetLatency);
                if (profile.Equalizer != null)
                    _equalizer.UpdateState(MapEqualizerStateToInternal(profile.Equalizer));

                var queue = profile.PlaybackQueue;
                if (queue == null) return;
                _activeQueueId = string.IsNullOrWhiteSpace(queue.ActiveQueueId) ? DefaultQueueId : queue.ActiveQueueId;
                if (!_queues.ContainsKey(_activeQueueId))
                    _queues[_activeQueueId] = CreateQueueSlot(_activeQueueId, _activeQueueId);

                var slot = Active;
                slot.Shuffle = queue.Shuffle;
                slot.RepeatMode = queue.RepeatMode;
                slot.SetQueue(queue.QueueUuids.Select(u => _library.GetTrack(u)).Where(t => t != null));
                slot.SetHistory(queue.HistoryUuids.Select(u => _library.GetTrack(u)).Where(t => t != null));
                slot.SetPlaylistSources(queue.PlaylistSources.Select(s =>
                    new PlaylistSource(
                        s.Id,
                        s.Name,
                        s.Kind,
                        string.IsNullOrWhiteSpace(s.RefId) ? InferRefId(s.Id, s.Kind) : s.RefId,
                        s.Uuids)));
            }
        }

        public PlaybackQueueState CreateQueueSnapshot()
        {
            lock (_lock)
            {
                var slot = Active;
                var snapshot = new PlaybackQueueState
                {
                    ActiveQueueId = _activeQueueId ?? DefaultQueueId,
                    Shuffle = slot.Shuffle,
                    RepeatMode = slot.RepeatMode
                };
                snapshot.QueueUuids.AddRange(slot.Queue.Select(t => t.Uuid));
                snapshot.HistoryUuids.AddRange(slot.History.Select(t => t.Uuid));
                foreach (var source in slot.PlaylistSourceSpecs)
                {
                    var state = new PlaylistSourceState { Id = source.id ?? "", Name = source.name ?? "" };
                    state.Uuids.AddRange(source.uuids ?? Array.Empty<string>());
                    state.Kind = source.kind;
                    state.RefId = source.refId ?? "";
                    snapshot.PlaylistSources.Add(state);
                }
                return snapshot;
            }
        }

        // ── Playback control ──

        public void Play(string uuid = null)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(uuid))
                {
                    var track = _library.GetTrack(uuid);
                    if (track != null && !track.IsExcluded) PlayTrack(track);
                    else _logger.LogWarning("Track not found: {Uuid}", uuid);
                }
                else if (_playState == 0)
                {
                    var next = DequeueNext();
                    if (next != null) PlayTrack(next);
                }
                else
                {
                    Resume();
                }
            }
        }

        public void Pause() { lock (_lock) { SetPlayState(2); } }
        public void Resume() { lock (_lock) { SetPlayState(1); } }

        public void Toggle()
        {
            lock (_lock)
            {
                if (_playState == 1) SetPlayState(2);
                else if (_playState == 2) SetPlayState(1);
                else Play();
            }
        }

        public void Next()
        {
            lock (_lock)
            {
                var next = DequeueNext();
                if (next != null) PlayTrack(next);
                else Stop();
            }
        }

        public void Prev()
        {
            lock (_lock)
            {
                var slot = Active;
                if (slot.NavigateHistory(-1))
                {
                    var track = slot.GetHistoryTrack();
                    if (track != null) PlayTrack(track);
                }
            }
        }

        public void Seek(float position)
        {
            lock (_lock)
            {
                Position = position;
                OnPositionChanged?.Invoke(position);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                SetPlayState(0);
                var track = Active.CurrentTrack;
                Active.ClearCurrentTrack();
                ReleaseCurrentReader(track);
                Interlocked.Increment(ref _playbackGeneration);
            }
        }

        // ── Volume ──

        public void SetVolume(float volume) { _volumeNode.Volume = Math.Clamp(volume, 0f, 1f); }
        public void SetTargetLatency(float latency) { _targetLatency = Math.Clamp(latency, 0.03f, 1.0f); }

        // ── Shuffle / Repeat ──

        public void SetShuffle(bool enabled) { Active.Shuffle = enabled; }
        public void SetRepeatMode(SDK.Protos.Models.RepeatMode mode) { Active.RepeatMode = mode; }

        // ── Queue ──

        public void AddToQueue(string uuid)
        {
            var track = _library.GetTrack(uuid);
            if (track == null || track.IsExcluded) return;
            lock (_lock) { Active.AddToQueue(track); }
            OnQueueChanged?.Invoke();
        }

        public void InsertIntoQueue(IEnumerable<string> uuids, int index)
        {
            var tracks = uuids.Select(u => _library.GetTrack(u)).Where(t => t != null && !t.IsExcluded).ToList();
            lock (_lock) { Active.InsertIntoQueue(tracks, index); }
            OnQueueChanged?.Invoke();
        }

        public void SetQueue(IEnumerable<string> uuids)
        {
            var tracks = uuids.Select(u => _library.GetTrack(u)).Where(t => t != null && !t.IsExcluded).ToList();
            lock (_lock) { Active.SetQueue(tracks); }
            OnQueueChanged?.Invoke();
        }

        public void RemoveFromQueue(int index) { lock (_lock) { Active.RemoveFromQueue(index); } OnQueueChanged?.Invoke(); }
        public void RemoveFromQueue(string uuid) { lock (_lock) { Active.RemoveFromQueueByUuid(uuid); } OnQueueChanged?.Invoke(); }
        public void MoveInQueue(int from, int to) { lock (_lock) { Active.MoveInQueue(from, to); } OnQueueChanged?.Invoke(); }
        public void ClearQueue() { lock (_lock) { Active.ClearQueue(); } OnQueueChanged?.Invoke(); }
        public void RemoveFromHistory(int index) { lock (_lock) { Active.RemoveFromHistory(index); } OnQueueChanged?.Invoke(); }
        public void MoveInHistory(int from, int to) { lock (_lock) { Active.MoveInHistory(from, to); } OnQueueChanged?.Invoke(); }
        public void ClearHistory() { lock (_lock) { Active.ClearHistory(); } OnQueueChanged?.Invoke(); }

        // ── Playlist sources ──

        public void SetPlaylistSources(IEnumerable<PlaylistSourceRequest> sources)
        {
            lock (_lock)
            {
                var playlistSources = sources.Select(s =>
                {
                    return new PlaylistSource(
                        s.id,
                        s.name,
                        s.kind,
                        string.IsNullOrWhiteSpace(s.refId) ? InferRefId(s.id, s.kind) : s.refId,
                        s.uuids);
                });
                Active.SetPlaylistSources(playlistSources);
            }
            OnQueueChanged?.Invoke();
        }

        private IReadOnlyList<Track> ResolvePlaylistSource(PlaylistSourceKind kind, string refId)
        {
            if (string.IsNullOrWhiteSpace(refId)) return Array.Empty<Track>();
            switch (kind)
            {
                case PlaylistSourceKind.Tag:
                    var tagQuery = new TrackQuery { IsExcluded = false };
                    tagQuery.TagIds.Add(refId);
                    return _library.QueryTracks(tagQuery);
                case PlaylistSourceKind.Album:
                    return _library.QueryTracks(new TrackQuery { AlbumId = refId, IsExcluded = false });
                case PlaylistSourceKind.Playlist:
                    return _library.QueryTracks(new TrackQuery { PlaylistId = refId, IsExcluded = false });
                case PlaylistSourceKind.Track:
                    var track = _library.GetTrack(refId);
                    return track == null || track.IsExcluded ? Array.Empty<Track>() : new[] { track };
                default:
                    return Array.Empty<Track>();
            }
        }

        private static string InferRefId(string id, PlaylistSourceKind kind)
        {
            if (string.IsNullOrWhiteSpace(id)) return "";
            var prefix = kind switch
            {
                PlaylistSourceKind.Tag => "tag_",
                PlaylistSourceKind.Album => "album_",
                PlaylistSourceKind.Playlist => "playlist_",
                PlaylistSourceKind.Track => "track_",
                _ => ""
            };
            return !string.IsNullOrEmpty(prefix) && id.StartsWith(prefix, StringComparison.Ordinal)
                ? id[prefix.Length..]
                : id;
        }

        // ── Playback state snapshot (not instance profile) ──

        // ── Internal ──

        private void PlayTrack(Track track)
        {
            ReleaseCurrentReader(CurrentTrack);

            var slot = Active;
            slot.SetCurrentTrack(track);
            slot.AddToHistory(track);
            SetPlayState(1);
            Position = 0;
            OnTrackChanged?.Invoke(track);
            OnQueueChanged?.Invoke();

            _eventBus.Publish(new PlayStartedEvent { Music = track, Source = PlaySource.Queue });

            // Start PCM streaming
            var gen = Interlocked.Increment(ref _playbackGeneration);
            _playbackCts?.Cancel();
            _playbackCts = new CancellationTokenSource();
            _playbackTask = Task.Run(() => PlaybackLoopAsync(track, gen, _playbackCts.Token));
        }

        private Track DequeueNext()
        {
            var slot = Active;
            var next = slot.DequeueNext(_rng);
            if (next == null && slot.QueueCount > 0)
            {
                next = slot.DequeueNext(_rng);
            }
            return next;
        }

        private async Task PlaybackLoopAsync(Track track, int generation, CancellationToken ct)
        {
            try
            {
                if (track.SourceType == SourceType.Stream || track.SourceType == SourceType.Url)
                {
                    var format = "mp3";
                    var cacheKey = $"{track.ModuleId}_{track.Uuid}";
                    var reader = await _streamingService.CreateStreamAndWaitAsync(
                        track.SourcePath, format, track.Duration, cacheKey,
                        cancellationToken: ct);

                    if (reader == null || ct.IsCancellationRequested) return;

                    lock (_lock) { _currentReader = reader; }

                    float[] buffer = new float[4096];
                    while (!ct.IsCancellationRequested && !reader.IsEndOfStream)
                    {
                        var frames = reader.ReadFrames(buffer, buffer.Length / 2);
                        if (frames <= 0) break;

                        lock (_lock) { Position += (float)frames / 44100f; }
                        OnPositionChanged?.Invoke(Position);

                        _sharedMemory?.WriteFrames(buffer, (int)frames);
                        await Task.Delay((int)(frames * 1000f / 44100f * 0.9f), ct);
                    }

                    reader.Dispose();
                    lock (_lock) { _currentReader = null; }
                    _eventBus.Publish(new MusicResourcesReleasedEvent { Music = track });
                }
                else
                {
                    // File-based — handled by external reader
                    await Task.Delay(100, ct);
                }

                if (!ct.IsCancellationRequested && generation == _playbackGeneration)
                {
                    lock (_lock)
                    {
                        _eventBus.Publish(new PlayEndedEvent { Music = track, Reason = PlayEndReason.Completed });
                        if (RepeatMode == SDK.Protos.Models.RepeatMode.One)
                        {
                            PlayTrack(track);
                        }
                        else
                        {
                            var next = DequeueNext();
                            if (next != null) PlayTrack(next);
                            else if (RepeatMode == SDK.Protos.Models.RepeatMode.All)
                            {
                                // Rebuild playlist
                                Play(null);
                            }
                            else SetPlayState(0);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playback error for {Uuid}", track.Uuid);
            }
        }

        private void SetPlayState(int state)
        {
            _playState = state;
            OnStateChanged?.Invoke(state);
        }

        private void ReleaseCurrentReader(Track track)
        {
            var reader = _currentReader;
            _currentReader = null;
            if (reader == null) return;
            reader.Dispose();
            if (track != null)
                _eventBus.Publish(new MusicResourcesReleasedEvent { Music = track });
        }

        private static Audio.EqualizerState MapEqualizerStateToInternal(SDK.Protos.Models.EqualizerState proto)
        {
            var state = new Audio.EqualizerState
            {
                Enabled = proto.Enabled,
                GlobalGainDb = proto.GlobalGainDb,
                SoftClipEnabled = proto.SoftClipEnabled
            };
            foreach (var pt in proto.Points)
            {
                state.Points.Add(new Audio.EqualizerPoint
                {
                    Id = pt.Id,
                    Frequency = pt.Frequency,
                    GainDb = pt.GainDb,
                    Q = pt.Q,
                    Type = (Audio.EqualizerFilterType)(int)pt.Type
                });
            }
            return state;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
            ReleaseCurrentReader(CurrentTrack);
        }
    }
}
