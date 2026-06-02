using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.ModuleSystem;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Ipc;
using OmniMixPlayer.SDK.Models;
using QueueChangeType = OmniMixPlayer.SDK.Interfaces.QueueChangeType;

namespace OmniMixPlayer.Backend.Audio
{
    public enum PlaybackMode
    {
        ClientManaged,
        ServerManaged
    }

    public class PlaybackController : IDisposable, IPlayQueue
    {
        private readonly ILogger _logger;
        private readonly SharedMemoryServer _sharedMemory;
        private readonly IEventBus _eventBus;
        private readonly IMusicRegistry _musicRegistry;
        private readonly IStreamingService _streamingService;
        private readonly PlaybackMode _mode;
        private readonly DbService _dbService;
        public string Id { get; }
        private readonly Equalizer _equalizer = new Equalizer();
        public Equalizer Equalizer => _equalizer;

        private readonly Dictionary<string, QueueSlot> _queues = new Dictionary<string, QueueSlot>();
        private string _activeQueueId;
        private const string DefaultQueueId = "default";
        private const int MaxHistoryCount = 50;

        private CancellationTokenSource _playbackCts;
        private Task _playbackTask;
        private int _playbackGeneration;
        private readonly VolumeNode _volumeNode = new VolumeNode();
        public VolumeNode VolumeNode => _volumeNode;
        private volatile int _playState;
        private float _targetLatency = 0.1f;
        private readonly Random _rng = new Random();
        private readonly object _lock = new object();

        private IPcmStreamReader _currentReader;
        private bool _disposed;
        private long _lastHandledSeekGeneration;
        private bool _currentStreamEofSignaled;
        private bool _formatReadySignaled;
        private float _lastLoopProgress;
        private DateTime _lastLoopProgressChangeUtc = DateTime.UtcNow;
        private const int AudibleDrainToleranceMs = 250;
        private const int NearEndStallTimeoutSeconds = 10;
        private string _lastPlayInitiator;
        private string _lastEventSourceModuleId;
        private readonly List<IDisposable> _controlSubscriptions = new List<IDisposable>();

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

        public MusicInfo CurrentTrack => Active.CurrentTrack;
        public bool IsPlaying => _playState == 1;
        public float Position { get; private set; }
        public float Volume { get => _volumeNode.Volume; set => _volumeNode.Volume = value; }
        public float TargetLatency { get => _targetLatency; set => _targetLatency = Math.Clamp(value, 0.03f, 1.0f); }
        public bool Shuffle { get => Active.Shuffle; set => Active.Shuffle = value; }
        public RepeatMode RepeatMode { get => Active.RepeatMode; set => Active.RepeatMode = value; }

        public IReadOnlyList<MusicInfo> Queue => Active.Queue;
        public int QueueCount => Active.QueueCount;
        public int QueueIndex => Active.QueueIndex;
        public IReadOnlyList<MusicInfo> History => Active.History;
        public int HistoryCount => Active.HistoryCount;
        public IReadOnlyList<MusicInfo> Playlist
        {
            get { return Active.Playlist; }
        }
        public IReadOnlyList<PlaylistSourceInfo> PlaylistSources
        {
            get { return Active.PlaylistSources; }
        }
        public int PlaylistCount => Active.PlaylistCount;
        public int PlaylistPosition => Active.PlaylistPosition;
        public bool CanGoPrevious => Active.CanGoPrevious;
        public bool CanGoNext => Active.CanGoNext;
        public string ActiveQueueId => _activeQueueId;
        public IReadOnlyList<string> QueueIds => _queues.Keys.ToList();
        public PlaybackMode CurrentMode => _mode;

        public event Action<MusicInfo> OnTrackChanged;
        public event Action<int> OnStateChanged;
        public event Action<float> OnPositionChanged;
        public event Action OnQueueChanged;

        event EventHandler<QueueChangedEventArgs> IPlayQueue.OnQueueChanged
        {
            add => _onQueueChangedHandler += value;
            remove => _onQueueChangedHandler -= value;
        }
        event EventHandler<MusicInfo> IPlayQueue.OnTrackChanged
        {
            add => _onTrackChangedHandler += value;
            remove => _onTrackChangedHandler -= value;
        }
        event EventHandler IPlayQueue.OnStateChanged
        {
            add => _onStateChangedHandler += value;
            remove => _onStateChangedHandler -= value;
        }
        event EventHandler<float> IPlayQueue.OnPositionChanged
        {
            add => _onPositionChangedHandler += value;
            remove => _onPositionChangedHandler -= value;
        }

        private EventHandler<QueueChangedEventArgs> _onQueueChangedHandler;
        private EventHandler<MusicInfo> _onTrackChangedHandler;
        private EventHandler _onStateChangedHandler;
        private EventHandler<float> _onPositionChangedHandler;

        private void FireStateChanged(int state)
        {
            OnStateChanged?.Invoke(state);
            _onStateChangedHandler?.Invoke(this, EventArgs.Empty);

            // 通过 EventBus 广播状态事件
            var track = Active.CurrentTrack;
            switch (state)
            {
                case 2: // Paused
                    if (track != null)
                    {
                        _eventBus.Publish(new PlayPausedEvent
                        {
                            SourceModuleId = _lastEventSourceModuleId,
                            Music = track,
                            IsPaused = true
                        });
                    }
                    break;
                    // state=0 (Stopped): PlayEndedEvent is fired in the natural end path
                    // state=1 (Playing): PlayStartedEvent is fired in FireTrackChanged
            }
        }
        private void FireTrackChanged(MusicInfo track)
        {
            OnTrackChanged?.Invoke(track);
            _onTrackChangedHandler?.Invoke(this, track);

            // 通过 EventBus 广播曲目变化事件
            if (track != null)
            {
                _eventBus.Publish(new PlayStartedEvent
                {
                    SourceModuleId = _lastEventSourceModuleId,
                    Music = track,
                    Source = PlaySource.UserClick
                });
            }
        }
        private void FirePositionChanged(float pos)
        {
            OnPositionChanged?.Invoke(pos);
            _onPositionChangedHandler?.Invoke(this, pos);

            // 通过 EventBus 广播进度事件
            var track = Active.CurrentTrack;
            if (track != null)
            {
                _eventBus.Publish(new PlayProgressEvent
                {
                    SourceModuleId = _lastEventSourceModuleId,
                    Music = track,
                    CurrentTime = pos,
                    TotalTime = track.Duration,
                    Progress = track.Duration > 0 ? pos / track.Duration : 0
                });
            }
        }
        private void FireQueueChanged(OmniMixPlayer.SDK.Interfaces.QueueChangeType type, string uuid = null)
        {
            OnQueueChanged?.Invoke();
            _onQueueChangedHandler?.Invoke(this, new QueueChangedEventArgs
            {
                ChangeType = type,
                ChangedUuid = uuid ?? ""
            });

            // 通过 EventBus 广播队列事件
            _eventBus.Publish(new QueueChangedEvent
            {
                SourceModuleId = _lastEventSourceModuleId,
                ChangeType = ConvertQueueChangeType(type),
                QueueLength = Active.QueueCount
            });
        }

        private static SDK.Events.QueueChangeType ConvertQueueChangeType(SDK.Interfaces.QueueChangeType type)
        {
            return type switch
            {
                SDK.Interfaces.QueueChangeType.Enqueued => SDK.Events.QueueChangeType.Added,
                SDK.Interfaces.QueueChangeType.Removed => SDK.Events.QueueChangeType.Removed,
                SDK.Interfaces.QueueChangeType.Moved => SDK.Events.QueueChangeType.Reordered,
                SDK.Interfaces.QueueChangeType.Cleared => SDK.Events.QueueChangeType.Cleared,
                SDK.Interfaces.QueueChangeType.PlaybackStarted => SDK.Events.QueueChangeType.Added,
                _ => SDK.Events.QueueChangeType.Added
            };
        }

        public PlaybackController(ILogger logger, SharedMemoryServer sharedMemory,
            IEventBus eventBus, IMusicRegistry musicRegistry, IStreamingService streamingService,
            DbService dbService = null, string instanceId = null, string configDir = null, PlaybackMode mode = PlaybackMode.ServerManaged)
        {
            _logger = logger;
            _sharedMemory = sharedMemory;
            _eventBus = eventBus;
            _musicRegistry = musicRegistry;
            _streamingService = streamingService;
            _dbService = dbService;
            Id = instanceId ?? (configDir != null ? Path.GetFileName(configDir) : "default");
            _mode = mode;

            lock (_lock)
            {
                _queues[DefaultQueueId] = new QueueSlot(DefaultQueueId, "Default");
                _activeQueueId = DefaultQueueId;
            }

            if (_dbService != null)
            {
                RestoreState();
            }

            SubscribeControlEvents();
        }

        private void SubscribeControlEvents()
        {
            _controlSubscriptions.Add(_eventBus.Subscribe<PlayRequestedEvent>(OnPlayRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<PauseRequestedEvent>(OnPauseRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<ResumeRequestedEvent>(OnResumeRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<StopRequestedEvent>(OnStopRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<TogglePlayRequestedEvent>(OnTogglePlayRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<NextTrackRequestedEvent>(OnNextTrackRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<PreviousTrackRequestedEvent>(OnPreviousTrackRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<SeekRequestedEvent>(OnSeekRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<PlaySeekEvent>(OnPlaySeekEvent));
            _controlSubscriptions.Add(_eventBus.Subscribe<VolumeChangeRequestedEvent>(OnVolumeChangeRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<ToggleShuffleRequestedEvent>(OnToggleShuffleRequested));
            _controlSubscriptions.Add(_eventBus.Subscribe<SetRepeatModeRequestedEvent>(OnSetRepeatModeRequested));
        }

        #region Control Event Handlers

        private void OnPlayRequested(PlayRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Play requested by {Source}", e.SourceModuleId ?? "host");
            Play(e.MusicUuid);
        }

        private void OnPauseRequested(PauseRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Pause requested by {Source}", e.SourceModuleId ?? "host");
            Pause();
        }

        private void OnResumeRequested(ResumeRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Resume requested by {Source}", e.SourceModuleId ?? "host");
            Resume();
        }

        private void OnStopRequested(StopRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Stop requested by {Source}", e.SourceModuleId ?? "host");
            Stop();
        }

        private void OnTogglePlayRequested(TogglePlayRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Toggle play requested by {Source}", e.SourceModuleId ?? "host");
            Toggle();
        }

        private void OnNextTrackRequested(NextTrackRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Next track requested by {Source}", e.SourceModuleId ?? "host");
            Next();
        }

        private void OnPreviousTrackRequested(PreviousTrackRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Previous track requested by {Source}", e.SourceModuleId ?? "host");
            Prev();
        }

        private void OnSeekRequested(SeekRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Seek to {Pos}s requested by {Source}", e.PositionSeconds, e.SourceModuleId ?? "host");
            Seek(e.PositionSeconds);
        }

        private void OnPlaySeekEvent(PlaySeekEvent e)
        {
            if (e == null || !e.IsCompleted || e.IsPending || _currentReader == null || _currentReader.CanSeek)
                return;

            var track = Active.CurrentTrack;
            if (track == null || e.Music == null || track.UUID != e.Music.UUID)
                return;

            if (string.IsNullOrEmpty(e.SourceModuleId) || e.SourceModuleId != track.ModuleId)
                return;

            ConfirmExternalSeek(e.TargetTime);
        }

        private void OnVolumeChangeRequested(VolumeChangeRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Volume {Vol} requested by {Source}", e.Volume, e.SourceModuleId ?? "host");
            SetVolume(e.Volume);
        }

        private void OnToggleShuffleRequested(ToggleShuffleRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Shuffle {Enabled} requested by {Source}", e.Enabled, e.SourceModuleId ?? "host");
            SetShuffle(e.Enabled);
        }

        private void OnSetRepeatModeRequested(SetRepeatModeRequestedEvent e)
        {
            _lastEventSourceModuleId = e.SourceModuleId;
            _logger.LogInformation("Repeat mode {Mode} requested by {Source}", e.Mode, e.SourceModuleId ?? "host");
            SetRepeatMode(e.Mode);
        }

        #endregion

        #region Multi-Queue Management

        public QueueInfo CreateQueue(string name)
        {
            lock (_lock)
            {
                var id = SanitizeId(name);
                if (_queues.TryGetValue(id, out var existing))
                    return existing.GetInfo();

                var slot = new QueueSlot(id, name);
                _queues[id] = slot;
                _logger.LogInformation("Created queue: {Id} ({Name})", id, name);
                SaveState();
                return slot.GetInfo();
            }
        }

        public bool DeleteQueue(string queueId)
        {
            lock (_lock)
            {
                if (queueId == DefaultQueueId) return false;
                if (!_queues.Remove(queueId)) return false;

                if (_activeQueueId == queueId)
                    _activeQueueId = DefaultQueueId;

                _logger.LogInformation("Deleted queue: {Id}", queueId);
                SaveState();
                return true;
            }
        }

        public bool SwitchQueue(string queueId)
        {
            lock (_lock)
            {
                if (!_queues.ContainsKey(queueId)) return false;
                _activeQueueId = queueId;
                _logger.LogInformation("Switched to queue: {Id}", queueId);
                EmitQueueChanged(QueueChangeType.Cleared, null);
                SaveState();
                return true;
            }
        }

        public QueueInfo GetActiveQueueInfo() => Active.GetInfo();
        public IReadOnlyList<QueueInfo> ListQueues() => _queues.Values.Select(q => q.GetInfo()).ToList();

        #endregion

        #region IPlayQueue — delegates to Active

        public void Play(string uuid = null)
        {
            lock (_lock)
            {
                var active = Active;
                if (!string.IsNullOrEmpty(uuid))
                {
                    var music = _musicRegistry.GetMusic(uuid);
                    if (music == null) { _logger.LogWarning("Track not found: {Uuid}", uuid); return; }
                    active.SetCurrentTrack(music);
                }
                else
                {
                    if (active.CurrentTrack == null)
                        active.SelectNext(GetPlayablePlaylist(), active.Shuffle, _rng);
                }

                if (active.CurrentTrack != null) StartPlayback();
            }
        }

        public void Pause()
        {
            _playState = 2;
            _sharedMemory.SetPlayState(2);
            _sharedMemory.SetStreamState(SharedMemoryStreamState.Paused);
            FireStateChanged(2);
        }

        public void Resume()
        {
            if (Active.CurrentTrack != null)
            {
                _playState = 1;
                _sharedMemory.SetPlayState(1);
                _sharedMemory.SetStreamState(SharedMemoryStreamState.Playing);
                FireStateChanged(1);
            }
        }

        public void Stop()
        {
            StopPlayback();
            _playState = 0;
            _sharedMemory.SetPlayState(0);
            _sharedMemory.SetStreamState(SharedMemoryStreamState.Stopped);
            FireStateChanged(0);
        }

        public void Toggle()
        {
            if (_playState == 1) Pause();
            else if (_playState == 2) Resume();
            else Play(null);
        }

        public void Next()
        {
            lock (_lock)
            {
                var active = Active;

                if (Active.RepeatMode == RepeatMode.One && active.CurrentTrack != null)
                {
                    StartPlayback();
                    return;
                }

                if (active.IsInHistoryMode)
                {
                    var prev = active.GoNextInHistory();
                    if (prev != null)
                    {
                        StartPlayback();
                        return;
                    }
                }

                var next = active.SelectNext(GetPlayablePlaylist(), active.Shuffle, _rng);
                if (next != null)
                {
                    StartPlayback();
                    return;
                }
            }
        }

        public void Prev()
        {
            lock (_lock)
            {
                var prev = Active.GoPreviousInHistory();
                if (prev != null)
                    StartPlayback();
            }
        }

        // ── [FIXED] Seek: CanSeek 始终为 true，移除条件分支 ──
        public void Seek(float positionSeconds)
        {
            if (_currentReader == null) return;

            // 使用 reader 的实际采样率（而非 _sharedMemory.SampleRate）
            int sampleRate = _currentReader.Info.SampleRate > 0
                ? _currentReader.Info.SampleRate
                : _sharedMemory.SampleRate;

            if (!_currentReader.CanSeek)
            {
                PublishSeekEvent(positionSeconds, isPending: true, isCompleted: false);
                return;
            }

            long shmTargetFrame = (long)(positionSeconds * sampleRate);
            var seekGeneration = _sharedMemory.RequestSeek(shmTargetFrame);
            _sharedMemory.DiscardUnreadFrames();

            ulong targetFrame = (ulong)(positionSeconds * sampleRate);
            if (_currentReader.Seek(targetFrame))
            {
                Position = positionSeconds;

                _sharedMemory.WriteI64(SharedMemoryProtocol.WriteCursor, shmTargetFrame);
                _sharedMemory.WriteI64(SharedMemoryProtocol.ReadCursor, shmTargetFrame);
                _sharedMemory.WriteI64(SharedMemoryProtocol.AudibleCursor, shmTargetFrame);
                _sharedMemory.WriteI64(SharedMemoryProtocol.FinalWriteCursor, 0);
                _sharedMemory.SetFlag(SharedMemoryStreamFlags.DecoderEof, false);
                _sharedMemory.SetFlag(SharedMemoryStreamFlags.SyntheticEof, false);
                _sharedMemory.SetFlag(SharedMemoryStreamFlags.ClientDrained, false);
                _sharedMemory.SetFlag(SharedMemoryStreamFlags.SeekPending, false);
                _sharedMemory.SetFlag(SharedMemoryStreamFlags.Discontinuity, false);
                _currentStreamEofSignaled = false;
                _lastHandledSeekGeneration = seekGeneration;

                FirePositionChanged(Position);
                PublishSeekEvent(positionSeconds, isPending: false, isCompleted: true);
                SaveState();
            }
        }

        private void ConfirmExternalSeek(float positionSeconds)
        {
            if (_currentReader == null) return;

            var sampleRate = _currentReader.Info.SampleRate > 0
                ? _currentReader.Info.SampleRate
                : _sharedMemory.SampleRate;
            if (sampleRate <= 0) sampleRate = 44100;

            var targetFrame = (long)(Math.Max(0, positionSeconds) * sampleRate);
            _sharedMemory.ResetCursors(targetFrame);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.SeekPending, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.Discontinuity, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.DecoderEof, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.SyntheticEof, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.ClientDrained, false);
            _sharedMemory.SetStreamState(_playState == 1 ? SharedMemoryStreamState.Playing : SharedMemoryStreamState.Paused);

            Position = Math.Max(0, positionSeconds);
            _currentStreamEofSignaled = false;
            _lastLoopProgress = Position;
            _lastLoopProgressChangeUtc = DateTime.UtcNow;
            FirePositionChanged(Position);
            SaveState();
        }

        private void PublishSeekEvent(float positionSeconds, bool isPending, bool isCompleted)
        {
            var track = Active.CurrentTrack;
            if (track == null) return;

            _eventBus.Publish(new PlaySeekEvent
            {
                SourceModuleId = _lastEventSourceModuleId,
                Music = track,
                Progress = track.Duration > 0 ? positionSeconds / track.Duration : 0,
                TargetTime = positionSeconds,
                IsPending = isPending,
                IsCompleted = isCompleted
            });
        }

        public void SetVolume(float volume)
        {
            Volume = volume;
            _dbService.SaveVolume(Id, volume);
            FireStateChanged(_playState);
        }

        public void SetTargetLatency(float latency)
        {
            TargetLatency = latency;
            _dbService.SaveTargetLatency(Id, latency);
            FireStateChanged(_playState);
        }
        public void SetShuffle(bool enabled) => Shuffle = enabled;
        public void SetRepeatMode(RepeatMode mode) => RepeatMode = mode;

        public void UpdateEqualizer(EqualizerState state)
        {
            _equalizer.UpdateState(state);
            _dbService.SaveEqualizer(Id, state);
        }

        public void AddToQueue(string uuid)
        {
            lock (_lock)
            {
                var music = _musicRegistry.GetMusic(uuid);
                if (music == null) return;
                Active.InsertQueue(new[] { music }, Active.QueueCount);
                EmitQueueChanged(QueueChangeType.Enqueued, uuid);
            }
        }

        public void AddToQueueRange(IEnumerable<string> uuids)
        {
            lock (_lock)
            {
                Active.InsertQueue(ResolveSongs(uuids), Active.QueueCount);
                EmitQueueChanged(QueueChangeType.Enqueued, null);
            }
        }

        public void InsertIntoQueue(IEnumerable<string> uuids, int index)
        {
            lock (_lock)
            {
                Active.InsertQueue(ResolveSongs(uuids), index);
                EmitQueueChanged(QueueChangeType.Enqueued, null);
            }
        }

        public void InsertIntoHistory(IEnumerable<string> uuids, int index)
        {
            lock (_lock)
            {
                Active.InsertHistory(ResolveSongs(uuids), index);
                EmitQueueChanged(QueueChangeType.Enqueued, null);
            }
        }

        public void RemoveFromQueue(int index) { lock (_lock) { if (Active.RemoveFromQueue(index)) EmitQueueChanged(QueueChangeType.Removed, null); } }
        public void RemoveFromQueue(string uuid) { lock (_lock) { if (Active.RemoveFromQueue(uuid)) EmitQueueChanged(QueueChangeType.Removed, uuid); } }
        public void RemoveFromHistory(int index) { lock (_lock) { if (Active.RemoveFromHistory(index)) EmitQueueChanged(QueueChangeType.Removed, null); } }
        public void RemoveFromHistory(string uuid) { lock (_lock) { if (Active.RemoveFromHistory(uuid)) EmitQueueChanged(QueueChangeType.Removed, uuid); } }
        public void MoveInQueue(int from, int to) { lock (_lock) { if (Active.MoveInQueue(from, to)) EmitQueueChanged(QueueChangeType.Moved, null); } }
        public void MoveInHistory(int from, int to) { lock (_lock) { if (Active.MoveInHistory(from, to)) EmitQueueChanged(QueueChangeType.Moved, null); } }
        public void ClearQueue() { lock (_lock) { Active.ClearQueue(); EmitQueueChanged(QueueChangeType.Cleared, null); } }
        public void ClearHistory() { lock (_lock) { Active.ClearHistory(); EmitQueueChanged(QueueChangeType.Cleared, null); } }

        public void SetPlaylist(IEnumerable<string> uuids)
        {
            lock (_lock)
            {
                var songs = ResolveSongs(uuids);
                Active.ReplacePlaylistSources(new[]
                {
                    new PlaylistSource("custom", "Custom", songs)
                });
                EmitQueueChanged(QueueChangeType.Cleared, null);
            }
        }

        public void SetPlaylistSources(IEnumerable<PlaylistSourceRequest> sources)
        {
            lock (_lock)
            {
                Active.ReplacePlaylistSources(ResolvePlaylistSources(sources));
                EmitQueueChanged(QueueChangeType.Cleared, null);
            }
        }

        public void InsertPlaylistSource(PlaylistSourceRequest source, int index)
        {
            lock (_lock)
            {
                var resolved = ResolvePlaylistSource(source);
                if (resolved == null) return;
                Active.InsertPlaylistSource(resolved, index);
                EmitQueueChanged(QueueChangeType.Enqueued, null);
            }
        }

        public void RemovePlaylistSource(string id)
        {
            lock (_lock)
            {
                if (Active.RemovePlaylistSource(id))
                    EmitQueueChanged(QueueChangeType.Removed, id);
            }
        }

        public void SetQueue(IEnumerable<string> uuids)
        {
            lock (_lock)
            {
                var songs = ResolveSongs(uuids);
                Active.ReplaceQueue(songs);
                EmitQueueChanged(QueueChangeType.Cleared, null);
            }
        }

        public void ImportFromPlaylist(IReadOnlyList<MusicInfo> songs, bool replace = true)
        {
            Active.ImportFromPlaylist(songs, replace);
            EmitQueueChanged(QueueChangeType.Enqueued, null);
        }

        public bool IsFavorite(string uuid) { var m = _musicRegistry.GetMusic(uuid); return m?.IsFavorite ?? false; }
        public void SetFavorite(string uuid, bool isFavorite)
        {
            var m = _musicRegistry.GetMusic(uuid);
            if (m == null) return;
            m.IsFavorite = isFavorite;
            _musicRegistry.UpdateMusic(m);

            if (!string.IsNullOrEmpty(m.ModuleId))
            {
                var loader = OmniMixPlayer.Backend.ModuleSystem.ModuleLoader.Instance;
                if (loader != null)
                {
                    var module = loader.GetModule(m.ModuleId);
                    if (module != null && module.Capabilities.CanFavorite)
                    {
                        var handler = loader.GetProvider<IFavoriteExcludeHandler>(m.ModuleId);
                        handler?.SetFavorite(uuid, isFavorite);
                    }
                }
            }
        }

        public bool IsExcluded(string uuid) { var m = _musicRegistry.GetMusic(uuid); return m?.IsExcluded ?? false; }
        public void SetExcluded(string uuid, bool isExcluded)
        {
            var m = _musicRegistry.GetMusic(uuid);
            if (m == null) return;
            m.IsExcluded = isExcluded;
            _musicRegistry.UpdateMusic(m);

            if (!string.IsNullOrEmpty(m.ModuleId))
            {
                var loader = OmniMixPlayer.Backend.ModuleSystem.ModuleLoader.Instance;
                if (loader != null)
                {
                    var module = loader.GetModule(m.ModuleId);
                    if (module != null && module.Capabilities.CanExclude)
                    {
                        var handler = loader.GetProvider<IFavoriteExcludeHandler>(m.ModuleId);
                        handler?.SetExcluded(uuid, isExcluded);
                    }
                }
            }
        }

        #endregion

        #region Public misc

        public void SetQueue(List<string> uuids) => SetQueue((IEnumerable<string>)uuids);
        public bool Fav(string uuid, bool isFav) { SetFavorite(uuid, isFav); return true; }

        public object GetStatus()
        {
            var a = Active;
            var reader = _currentReader;
            return new
            {
                IsPlaying = _playState == 1,
                Position,
                Volume = Volume,
                Shuffle = a.Shuffle,
                RepeatMode = a.RepeatMode.ToString().ToLowerInvariant(),
                QueueLength = a.QueueCount,
                QueueIndex = a.QueueIndex,
                PlaylistLength = a.PlaylistCount,
                PlaylistSourceCount = a.PlaylistSources.Count,
                PlaylistPosition = a.PlaylistPosition,
                HistoryCount = a.HistoryCount,
                HistoryPosition = GetHistoryPosition(a),
                ActiveQueueId = _activeQueueId,
                PlaybackMode = CurrentMode.ToString(),
                PlayInitiator = _lastPlayInitiator,
                IsBuffering = reader != null && reader.HasPendingSeek,
                CacheProgress = reader?.CacheProgress ?? -1,
                CurrentTrack = a.CurrentTrack != null ? TrackMap(a.CurrentTrack) : null
            };
        }
        private static object TrackMap(MusicInfo m) => new
        {
            uuid = m.UUID,
            title = m.Title,
            artist = m.Artist,
            albumId = m.AlbumId,
            duration = m.Duration,
            moduleId = m.ModuleId
        };

        private static int GetHistoryPosition(QueueSlot a) => -1;

        #endregion

        #region Persistence

        private void SaveState()
        {
            try
            {
                if (_dbService == null) return;
                var profile = _dbService.GetProfile(Id);
                profile.ActiveQueueId = _activeQueueId;
                profile.Volume = Volume;
                profile.TargetLatency = TargetLatency;
                lock (_lock)
                {
                    profile.Queues = _queues.Values.Select(q => q.Serialize()).ToList();
                }
                profile.Equalizer = _equalizer.CurrentState;
                _dbService.SaveProfile(profile);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to save playback state"); }
        }

        private void RestoreState()
        {
            try
            {
                if (_dbService == null) return;
                var data = _dbService.GetProfile(Id);
                Volume = data.Volume;
                TargetLatency = data.TargetLatency > 0 ? data.TargetLatency : 0.1f;
                _activeQueueId = data.ActiveQueueId ?? DefaultQueueId;
                if (data.Equalizer != null)
                {
                    _equalizer.UpdateState(data.Equalizer);
                }

                lock (_lock)
                {
                    _queues.Clear();
                    if (data.Queues != null)
                    {
                        foreach (var qd in data.Queues)
                        {
                            var slot = QueueSlot.Deserialize(qd, _musicRegistry);
                            _queues[qd.Id] = slot;
                        }
                    }
                    if (!_queues.ContainsKey(DefaultQueueId))
                        _queues[DefaultQueueId] = new QueueSlot(DefaultQueueId, "Default");
                    if (!_queues.ContainsKey(_activeQueueId))
                        _activeQueueId = DefaultQueueId;
                }

                _logger.LogInformation("Restored {Count} queues from LiteDB, active: {Id}", _queues.Count, _activeQueueId);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to restore playback state"); }
        }

        public void RefreshFromDisk()
        {
            RestoreState();
        }

        public PlaybackStateData GetProfile()
        {
            if (_dbService != null)
            {
                return _dbService.GetProfile(Id);
            }
            return new PlaybackStateData();
        }

        public bool UpdateProfile(string json)
        {
            if (_dbService != null)
            {
                var ok = _dbService.UpdateProfileFromJson(Id, json);
                if (ok)
                {
                    RestoreState();
                }
                return ok;
            }
            return false;
        }

        #endregion

        #region Internals

        // ── [FIXED] StartPlayback: 不传错误的 TotalFramesHint ──
        private void StartPlayback()
        {
            StopPlayback();
            var track = Active.CurrentTrack;
            if (track == null) return;

            Active.MarkCurrentStarted();
            _lastPlayInitiator = CurrentMode == PlaybackMode.ClientManaged
                ? "client"
                : "server";
            FireTrackChanged(track);

            // 修复: 传 0，由 MarkFormatReady 在获取到实际格式后填写正确的值
            _sharedMemory.BeginStream(track.UUID, totalFramesHint: 0);
            SaveState();

            _playbackCts = new CancellationTokenSource();
            var playbackToken = _playbackCts.Token;
            _playState = 1;
            _sharedMemory.SetPlayState(1);
            _sharedMemory.SetStreamState(SharedMemoryStreamState.Preparing);
            Position = 0;
            _lastHandledSeekGeneration = _sharedMemory.GetSeekGeneration();
            _currentStreamEofSignaled = false;
            _formatReadySignaled = false;
            _lastLoopProgress = 0;
            _lastLoopProgressChangeUtc = DateTime.UtcNow;
            FireStateChanged(1);
            var playbackGeneration = Volatile.Read(ref _playbackGeneration);
            _playbackTask = Task.Run(async () =>
            {
                var reader = await CreateStreamReaderAsync(track, playbackToken).ConfigureAwait(false);
                if (playbackToken.IsCancellationRequested ||
                    playbackGeneration != Volatile.Read(ref _playbackGeneration))
                {
                    reader?.Dispose();
                    return;
                }

                if (reader == null)
                {
                    _logger.LogWarning("No playable stream for {Uuid}; publishing synthetic EOF", track.UUID);
                    _sharedMemory.MarkFormatReady(_sharedMemory.SampleRate, _sharedMemory.Channels, 0);
                    _sharedMemory.MarkError(SharedMemoryStreamError.DecoderFailed, syntheticEof: true);
                    _playState = 0;
                    FireStateChanged(0);

                    if (CurrentMode != PlaybackMode.ClientManaged && !playbackToken.IsCancellationRequested)
                    {
                        await Task.Delay(50, playbackToken).ConfigureAwait(false);
                        Next();
                    }
                    return;
                }

                _currentReader = reader;
                await PlaybackLoopAsync(playbackToken, playbackGeneration).ConfigureAwait(false);
            }, playbackToken);
        }

        private async Task<IPcmStreamReader> CreateStreamReaderAsync(MusicInfo music, CancellationToken cancellationToken)
        {
            try
            {
                if (IsModuleResolvedSource(music))
                {
                    var moduleReader = await TryCreateModuleDecoderAsync(music, cancellationToken).ConfigureAwait(false);
                    if (moduleReader != null)
                        return moduleReader;

                    moduleReader = await TryResolveModuleStreamAsync(music, cancellationToken).ConfigureAwait(false);
                    if (moduleReader != null)
                        return moduleReader;

                    return null;
                }

                string url = music.SourcePath;
                string format = DetectFormat(music);
                float duration = music.Duration > 0 ? music.Duration : 240f;
                string cacheKey = music.UUID ?? Guid.NewGuid().ToString("N");
                if (!string.IsNullOrEmpty(url))
                    return _streamingService.CreateStream(url, format, duration, cacheKey);
            }
            catch (Exception ex) { _logger.LogError(ex, "CreateStreamReader failed: {Uuid}", music.UUID); }
            return null;
        }

        private bool IsModuleResolvedSource(MusicInfo music)
        {
            if (music == null || string.IsNullOrEmpty(music.ModuleId))
                return false;
            if (music.SourceType != MusicSourceType.Stream && music.SourceType != MusicSourceType.Url)
                return false;
            return ModuleLoader.Instance?.GetProvider<IModuleAudioDecoderProvider>(music.ModuleId) != null
                || ModuleLoader.Instance?.GetProvider<IPlayableSourceResolver>(music.ModuleId) != null;
        }

        private async Task<IPcmStreamReader> TryCreateModuleDecoderAsync(MusicInfo music, CancellationToken cancellationToken)
        {
            if (music == null || string.IsNullOrEmpty(music.ModuleId))
                return null;
            if (music.SourceType != MusicSourceType.Stream && music.SourceType != MusicSourceType.Url)
                return null;

            var provider = ModuleLoader.Instance?.GetProvider<IModuleAudioDecoderProvider>(music.ModuleId);
            if (provider == null || !provider.CanDecode(music.UUID))
                return null;

            _logger.LogInformation("Creating module decoder via {ModuleId}: {Uuid}", music.ModuleId, music.UUID);
            var reader = await provider.CreateDecoderAsync(music.UUID, AudioQuality.ExHigh, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return reader;
        }

        private async Task<IPcmStreamReader> TryResolveModuleStreamAsync(MusicInfo music, CancellationToken cancellationToken)
        {
            if (music == null || string.IsNullOrEmpty(music.ModuleId))
                return null;
            if (music.SourceType != MusicSourceType.Stream && music.SourceType != MusicSourceType.Url)
                return null;

            var resolver = ModuleLoader.Instance?.GetProvider<IPlayableSourceResolver>(music.ModuleId);
            if (resolver == null)
                return null;

            _logger.LogInformation("Resolving playable source via module {ModuleId}: {Uuid}", music.ModuleId, music.UUID);
            var source = await resolver.ResolveAsync(music.UUID, AudioQuality.ExHigh, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (source == null)
            {
                _logger.LogWarning("Module {ModuleId} returned no playable source: {Uuid}", music.ModuleId, music.UUID);
                return null;
            }

            if (source.IsRemote && source.IsExpired)
            {
                _logger.LogInformation("Playable URL expired, refreshing via module {ModuleId}: {Uuid}", music.ModuleId, music.UUID);
                source = await resolver.RefreshUrlAsync(music.UUID, AudioQuality.ExHigh, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return CreateReaderFromPlayableSource(music, source);
        }

        private IPcmStreamReader CreateReaderFromPlayableSource(MusicInfo music, PlayableSource source)
        {
            if (source == null)
                return null;

            switch (source.SourceType)
            {
                case PlayableSourceType.Remote:
                    // UseCachePath 优先：缓存命中时 Url 可能为空，直接使用缓存路径
                    if (source.UseCachePath)
                    {
                        return _streamingService.CreateStream(
                            source.Url,
                            FormatToString(source.Format, DetectFormat(music)),
                            music.Duration > 0 ? music.Duration : 240f,
                            !string.IsNullOrEmpty(source.CachePath) ? source.CachePath : (source.CacheKey ?? music.UUID ?? Guid.NewGuid().ToString("N")),
                            source.Headers,
                            useCachePath: true);
                    }
                    if (string.IsNullOrEmpty(source.Url))
                        return null;
                    return _streamingService.CreateStream(
                        source.Url,
                        FormatToString(source.Format, DetectFormat(music)),
                        music.Duration > 0 ? music.Duration : 240f,
                        source.CacheKey ?? music.UUID ?? Guid.NewGuid().ToString("N"),
                        source.Headers);

                case PlayableSourceType.Local:
                case PlayableSourceType.Cached:
                    if (string.IsNullOrEmpty(source.LocalPath))
                        return null;
                    if (source.UseCachePath)
                    {
                        return _streamingService.CreateStream(
                            source.LocalPath,
                            FormatToString(source.Format, DetectFormat(music)),
                            music.Duration > 0 ? music.Duration : 240f,
                            !string.IsNullOrEmpty(source.CachePath) ? source.CachePath : source.LocalPath,
                            source.Headers,
                            useCachePath: true);
                    }
                    return _streamingService.CreateStream(
                        source.LocalPath,
                        FormatToString(source.Format, DetectFormat(music)),
                        music.Duration > 0 ? music.Duration : 240f,
                        source.CacheKey ?? music.UUID ?? Guid.NewGuid().ToString("N"),
                        source.Headers);

                default:
                    _logger.LogWarning("Unsupported playable source type {SourceType}: {Uuid}", source.SourceType, music.UUID);
                    return null;
            }
        }

        private static string FormatToString(AudioFormat format, string fallback)
        {
            return format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Ogg => "ogg",
                AudioFormat.Wav => "wav",
                AudioFormat.Flac => "flac",
                AudioFormat.Aac => "aac",
                _ => fallback
            };
        }

        private static string DetectFormat(MusicInfo music)
        {
            var path = music.SourcePath ?? "";
            var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && (ext == "mp3" || ext == "flac" || ext == "wav")) return ext;
            if (music.SourceType == MusicSourceType.Stream || music.SourceType == MusicSourceType.Url) return "mp3";
            if (music.SourceType == MusicSourceType.File) return ext.Length > 0 ? ext : "mp3";
            return "mp3";
        }

        private void StopPlayback()
        {
            Interlocked.Increment(ref _playbackGeneration);
            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
            _playbackCts = null;
            _playState = 0;
            _sharedMemory.SetPlayState(0);
            _sharedMemory.SetStreamState(SharedMemoryStreamState.Stopped);
            _sharedMemory.ResetCursors();
            var old = _currentReader;
            _currentReader = null;
            old?.Dispose();
        }

        // ── [FIXED] PlaybackLoopAsync: 格式就绪从 reader 获取，不再用 _sharedMemory 默认值 ──
        private async Task PlaybackLoopAsync(CancellationToken ct, int playbackGeneration)
        {
            float[] pcmBuffer = null;
            // 初始值从共享内存获取（客户端可能在连接时已设置），但会尽快从 reader 更新
            int sampleRate = _sharedMemory.SampleRate > 0 ? _sharedMemory.SampleRate : 44100;
            int channels = _sharedMemory.Channels > 0 ? _sharedMemory.Channels : 2;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (playbackGeneration != Volatile.Read(ref _playbackGeneration))
                        return;

                    if (_playState != 1) { await Task.Delay(50, ct); continue; }

                    // Position from client's audible cursor
                    var audibleCursor = _sharedMemory.GetAudibleCursor();
                    var readCursor = _sharedMemory.GetReadCursor();
                    var progressCursor = audibleCursor > 0 ? audibleCursor : readCursor;
                    if (sampleRate > 0)
                    {
                        var newPos = (float)progressCursor / sampleRate;
                        if (Math.Abs(newPos - Position) > 0.05f)
                        {
                            Position = newPos;
                            FirePositionChanged(Position);
                        }

                        if (Math.Abs(newPos - _lastLoopProgress) > 0.1f)
                        {
                            _lastLoopProgress = newPos;
                            _lastLoopProgressChangeUtc = DateTime.UtcNow;
                        }
                    }

                    // Latency control
                    long audible = _sharedMemory.GetAudibleCursor();
                    long read = _sharedMemory.GetReadCursor();
                    bool supportsAudible = audible > 0 || read < 4096;
                    long progress = supportsAudible ? audible : read;
                    long write = _sharedMemory.GetWriteCursor();
                    long inFlight = write - progress;
                    int targetBacklogFrames = (int)(sampleRate * TargetLatency);
                    if (targetBacklogFrames < 1024) targetBacklogFrames = 1024;

                    while (inFlight > targetBacklogFrames)
                    {
                        await Task.Delay(10, ct);
                        if (_playState != 1 || ct.IsCancellationRequested)
                            break;

                        audible = _sharedMemory.GetAudibleCursor();
                        read = _sharedMemory.GetReadCursor();
                        supportsAudible = audible > 0 || read < 4096;
                        progress = supportsAudible ? audible : read;
                        write = _sharedMemory.GetWriteCursor();
                        inFlight = write - progress;
                        targetBacklogFrames = (int)(sampleRate * TargetLatency);
                        if (targetBacklogFrames < 1024) targetBacklogFrames = 1024;
                    }
                    if (_playState != 1 || ct.IsCancellationRequested) continue;

                    var reader = _currentReader;
                    if (reader == null || !reader.IsReady) { await Task.Delay(100, ct); continue; }

                    // Format update: use reader's actual format
                    if (reader.Info.SampleRate > 0 && reader.Info.Channels > 0)
                    {
                        if (channels != reader.Info.Channels || sampleRate != reader.Info.SampleRate)
                        {
                            sampleRate = reader.Info.SampleRate;
                            channels = reader.Info.Channels;
                            // 格式首次变化时重置 cursor（避免用旧速率解读 cursor）
                            if (_sharedMemory.GetWriteCursor() < 4096)
                            {
                                _sharedMemory.UpdateFormat(sampleRate, channels);
                            }
                            else
                            {
                                _sharedMemory.UpdateFormat(sampleRate, channels);
                            }
                            _logger.LogInformation("Audio format: {SampleRate} Hz, {Channels} channels", sampleRate, channels);
                        }
                    }

                    if (!_formatReadySignaled && reader.Info.SampleRate > 0)
                    {
                        var totalFramesHint = reader.Info.TotalFrames > 0
                            ? (long)Math.Min(reader.Info.TotalFrames, (ulong)long.MaxValue)
                            : (CurrentTrack?.Duration > 0 ? (long)(CurrentTrack.Duration * sampleRate) : 0);
                        _sharedMemory.MarkFormatReady(sampleRate, channels, totalFramesHint);
                        _formatReadySignaled = true;
                    }

                    HandlePendingSharedMemorySeek(reader, sampleRate);

                    int framesPerRead = 1024;
                    int requiredLength = framesPerRead * channels;
                    if (pcmBuffer == null || pcmBuffer.Length < requiredLength)
                    {
                        pcmBuffer = new float[requiredLength];
                    }

                    long framesRead = reader.ReadFrames(pcmBuffer, framesPerRead);

                    if (framesRead <= 0)
                    {
                        if (reader.IsEndOfStream)
                        {
                            if (!_currentStreamEofSignaled)
                            {
                                var decodedFrames = reader.CurrentFrame > 0
                                    ? (long)Math.Min(reader.CurrentFrame, (ulong)long.MaxValue)
                                    : _sharedMemory.GetWriteCursor();
                                _sharedMemory.MarkDecoderEof(decodedFrames);
                                _currentStreamEofSignaled = true;
                            }

                            var toleranceFrames = Math.Max(1, sampleRate * AudibleDrainToleranceMs / 1000);
                            if (!_sharedMemory.IsClientDrained(toleranceFrames))
                            {
                                Position = (float)Math.Max(_sharedMemory.GetAudibleCursor(), _sharedMemory.GetReadCursor()) / sampleRate;
                                FirePositionChanged(Position);
                                await Task.Delay(25, ct);
                                continue;
                            }

                            if (Active.RepeatMode == RepeatMode.One)
                            {
                                reader.Seek(0);
                                _sharedMemory.WriteI64(SharedMemoryProtocol.WriteCursor, 0);
                                _sharedMemory.WriteI64(SharedMemoryProtocol.ReadCursor, 0);
                                _sharedMemory.WriteI64(SharedMemoryProtocol.AudibleCursor, 0);
                                _sharedMemory.WriteI64(SharedMemoryProtocol.FinalWriteCursor, 0);
                                _sharedMemory.SetFlag(SharedMemoryStreamFlags.DecoderEof, false);
                                _sharedMemory.SetFlag(SharedMemoryStreamFlags.SyntheticEof, false);
                                _sharedMemory.SetFlag(SharedMemoryStreamFlags.ClientDrained, false);
                                _sharedMemory.SetStreamState(SharedMemoryStreamState.Playing);
                                _currentStreamEofSignaled = false;
                                Position = 0;
                                continue;
                            }
                            if (CurrentMode == PlaybackMode.ClientManaged)
                            {
                                _playState = 0;
                                _sharedMemory.MarkEnded();
                                _eventBus.Publish(new PlayEndedEvent
                                {
                                    SourceModuleId = _lastEventSourceModuleId,
                                    Music = CurrentTrack,
                                    Reason = PlayEndReason.Completed,
                                    PlayedDuration = Position
                                });
                                FireStateChanged(0);
                                continue;
                            }
                            _eventBus.Publish(new PlayEndedEvent
                            {
                                SourceModuleId = _lastEventSourceModuleId,
                                Music = CurrentTrack,
                                Reason = PlayEndReason.Completed,
                                PlayedDuration = Position
                            });
                            await Task.Delay(50, ct);
                            Next();
                            continue;
                        }
                        if (ShouldTreatStallAsEof(reader, sampleRate))
                        {
                            _logger.LogWarning("Playback stalled near end; marking synthetic EOF");
                            _sharedMemory.MarkError(SharedMemoryStreamError.StalledNearEnd, syntheticEof: true);
                            _currentStreamEofSignaled = true;
                            continue;
                        }
                        // Waiting for data: longer delay if cache still downloading
                        int waitMs = reader.IsCacheComplete ? 10 : 100;
                        await Task.Delay(waitMs, ct);
                        continue;
                    }

                    if (playbackGeneration != Volatile.Read(ref _playbackGeneration))
                        return;

                    _equalizer.Process(pcmBuffer, (int)framesRead, channels, sampleRate);
                    _volumeNode.Process(pcmBuffer, (int)framesRead, channels);
                    _sharedMemory.WriteFrames(pcmBuffer, (int)framesRead);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogError(ex, "Playback loop error"); }
        }

        // ── [FIXED] HandlePendingSharedMemorySeek: CanSeek 始终 true，移除检查 ──
        private void HandlePendingSharedMemorySeek(IPcmStreamReader reader, int sampleRate)
        {
            if (reader == null) return;

            var seekGeneration = _sharedMemory.GetSeekGeneration();
            if (seekGeneration == _lastHandledSeekGeneration)
                return;

            _lastHandledSeekGeneration = seekGeneration;
            var targetFrame = _sharedMemory.GetSeekFrame();
            if (targetFrame < 0) targetFrame = 0;

            if (!reader.Seek((ulong)targetFrame))
                return;

            _sharedMemory.WriteI64(SharedMemoryProtocol.WriteCursor, targetFrame);
            _sharedMemory.WriteI64(SharedMemoryProtocol.ReadCursor, targetFrame);
            _sharedMemory.WriteI64(SharedMemoryProtocol.AudibleCursor, targetFrame);
            _sharedMemory.WriteI64(SharedMemoryProtocol.FinalWriteCursor, 0);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.DecoderEof, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.SyntheticEof, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.ClientDrained, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.SeekPending, false);
            _sharedMemory.SetFlag(SharedMemoryStreamFlags.Discontinuity, false);
            _sharedMemory.SetStreamState(SharedMemoryStreamState.Playing);
            Position = (float)targetFrame / sampleRate;
            _currentStreamEofSignaled = false;
            _lastLoopProgress = Position;
            _lastLoopProgressChangeUtc = DateTime.UtcNow;
            FirePositionChanged(Position);
        }

        private bool ShouldTreatStallAsEof(IPcmStreamReader reader, int sampleRate)
        {
            if (reader == null || reader.HasPendingSeek || _currentStreamEofSignaled)
                return false;

            // Still downloading — not a real stall, just waiting for data
            if (!reader.IsCacheComplete)
                return false;

            var totalFrames = reader.Info.TotalFrames;
            if (totalFrames == 0)
                return false;

            var secondsFromEnd = ((double)totalFrames - reader.CurrentFrame) / Math.Max(1, sampleRate);
            if (secondsFromEnd > 1.0)
                return false;

            return (DateTime.UtcNow - _lastLoopProgressChangeUtc).TotalSeconds >= NearEndStallTimeoutSeconds;
        }

        private void EmitQueueChanged(OmniMixPlayer.SDK.Interfaces.QueueChangeType type, string uuid)
        {
            FireQueueChanged(type, uuid);
            SaveState();
        }

        private IReadOnlyList<MusicInfo> GetPlayablePlaylist()
        {
            var activePlaylist = Active.Playlist;
            var allSongs = activePlaylist;
            var playable = allSongs.Where(m => m != null && !m.IsExcluded).ToList();
            return playable.Count > 0 ? playable : allSongs.Where(m => m != null).ToList();
        }

        private List<MusicInfo> ResolveSongs(IEnumerable<string> uuids)
        {
            var songs = new List<MusicInfo>();
            foreach (var uuid in uuids ?? Array.Empty<string>())
            {
                var music = _musicRegistry.GetMusic(uuid);
                if (music != null) songs.Add(music);
            }
            return songs;
        }

        private List<PlaylistSource> ResolvePlaylistSources(IEnumerable<PlaylistSourceRequest> sources)
        {
            var resolved = new List<PlaylistSource>();
            foreach (var source in sources ?? Array.Empty<PlaylistSourceRequest>())
            {
                var item = ResolvePlaylistSource(source);
                if (item != null) resolved.Add(item);
            }
            return resolved;
        }

        private PlaylistSource ResolvePlaylistSource(PlaylistSourceRequest source)
        {
            if (source == null) return null;
            var id = string.IsNullOrWhiteSpace(source.id) ? Guid.NewGuid().ToString("N") : source.id.Trim();
            var name = string.IsNullOrWhiteSpace(source.name) ? id : source.name.Trim();
            return new PlaylistSource(id, name, ResolveSongs(source.uuids));
        }

        private static string SanitizeId(string name) =>
            string.Join("_", name.Split(Path.GetInvalidFileNameChars())).ToLowerInvariant();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            SaveState();
            StopPlayback();

            foreach (var sub in _controlSubscriptions)
                sub?.Dispose();
            _controlSubscriptions.Clear();
        }

        #endregion
    }
}
