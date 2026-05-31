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
        private string _lastPlayInitiator; // "server" 鎴?clientId

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
        public bool Shuffle { get => Active.Shuffle; set => Active.Shuffle = value; }
        public RepeatMode RepeatMode { get => Active.RepeatMode; set => Active.RepeatMode = value; }

        public IReadOnlyList<MusicInfo> Queue => Active.Queue;
        public int QueueCount => Active.QueueCount;
        public int QueueIndex => Active.QueueIndex;
        public IReadOnlyList<MusicInfo> History => Active.History;
        public int HistoryCount => Active.HistoryCount;
        public IReadOnlyList<MusicInfo> Playlist
        {
            get
            {
                return Active.Playlist;
            }
        }
        public IReadOnlyList<PlaylistSourceInfo> PlaylistSources
        {
            get
            {
                return Active.PlaylistSources;
            }
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

        // Bridge helpers: fire both public Action events and IPlayQueue EventHandler events
        private void FireStateChanged(int state)
        {
            OnStateChanged?.Invoke(state);
            _onStateChangedHandler?.Invoke(this, EventArgs.Empty);
        }
        private void FireTrackChanged(MusicInfo track)
        {
            OnTrackChanged?.Invoke(track);
            _onTrackChangedHandler?.Invoke(this, track);
        }
        private void FirePositionChanged(float pos)
        {
            OnPositionChanged?.Invoke(pos);
            _onPositionChangedHandler?.Invoke(this, pos);
        }
        private void FireQueueChanged(OmniMixPlayer.SDK.Interfaces.QueueChangeType type, string uuid = null)
        {
            OnQueueChanged?.Invoke();
            _onQueueChangedHandler?.Invoke(this, new QueueChangedEventArgs
            {
                ChangeType = type,
                ChangedUuid = uuid ?? ""
            });
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
        }

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

        #region IPlayQueue 鈥?delegates to Active

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

        public void Seek(float positionSeconds)
        {
            long shmTargetFrame = (long)(positionSeconds * _sharedMemory.SampleRate);
            var seekGeneration = _sharedMemory.RequestSeek(shmTargetFrame);
            _sharedMemory.DiscardUnreadFrames();
            if (_currentReader != null && _currentReader.CanSeek)
            {
                ulong targetFrame = (ulong)(positionSeconds * _currentReader.Info.SampleRate);
                if (_currentReader.Seek(targetFrame))
                {
                    Position = positionSeconds;

                    // Align the shared memory cursors to the seek target frame
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
                    SaveState();
                }
            }
        }

        public void SetVolume(float volume)
        {
            Volume = volume;
            _dbService.SaveVolume(Id, volume);
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

        private static int GetHistoryPosition(QueueSlot a) => -1; // internal only

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

        /// <summary>
        /// Re-read the profile from disk and re-resolve all song references.
        /// Call this after modules finish loading, when the music registry is
        /// fully populated. Without this, RestoreState() during construction
        /// may have dropped songs because the registry was empty.
        /// </summary>
        public void RefreshFromDisk()
        {
            RestoreState();
        }

        /// <summary>
        /// Return the full profile as a PlaybackStateData object.
        /// </summary>
        public PlaybackStateData GetProfile()
        {
            if (_dbService != null)
            {
                return _dbService.GetProfile(Id);
            }
            return new PlaybackStateData();
        }

        /// <summary>
        /// Update the profile from JSON and restore internal state.
        /// Merges with existing data so fields not present in the new JSON
        /// (e.g. Equalizer, Volume) are preserved from the previous file.
        /// Returns true on success.
        /// </summary>
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

        private void StartPlayback()
        {
            StopPlayback();
            var track = Active.CurrentTrack;
            if (track == null) return;

            Active.MarkCurrentStarted();
            // 璁板綍鎾斁鍙戣捣鑰咃細ClientManaged 妯″紡涓嬫潵鑷鎴风锛屽惁鍒欐潵鑷湇鍔＄
            _lastPlayInitiator = CurrentMode == PlaybackMode.ClientManaged
                ? "client"
                : "server";
            FireTrackChanged(track);
            long totalFramesHint = track.Duration > 0 ? (long)(track.Duration * _sharedMemory.SampleRate) : 0;
            _sharedMemory.BeginStream(track.UUID, totalFramesHint);
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

                    // Module tracks store provider-specific identifiers in SourcePath (for example Netease song ids),
                    // so falling back to CoreStreaming would create a bogus URL stream instead of skipping.
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
                case PlayableSourceType.PcmStream:
                    if (source.PcmReader == null)
                    {
                        _logger.LogWarning("Module returned a PCM source without reader: {Uuid}", music.UUID);
                        return null;
                    }
                    return source.PcmReader;

                case PlayableSourceType.Remote:
                    if (string.IsNullOrEmpty(source.Url))
                        return null;
                    return _streamingService.CreateStream(
                        source.Url,
                        FormatToString(source.Format, DetectFormat(music)),
                        music.Duration > 0 ? music.Duration : 240f,
                        music.UUID ?? Guid.NewGuid().ToString("N"));

                case PlayableSourceType.Local:
                case PlayableSourceType.Cached:
                    if (string.IsNullOrEmpty(source.LocalPath))
                        return null;
                    return _streamingService.CreateStream(
                        source.LocalPath,
                        FormatToString(source.Format, DetectFormat(music)),
                        music.Duration > 0 ? music.Duration : 240f,
                        music.UUID ?? Guid.NewGuid().ToString("N"));

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

        private async Task PlaybackLoopAsync(CancellationToken ct, int playbackGeneration)
        {
            float[] pcmBuffer = null;
            int sampleRate = _sharedMemory.SampleRate;
            int channels = _sharedMemory.Channels;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (playbackGeneration != Volatile.Read(ref _playbackGeneration))
                        return;

                    if (_playState != 1) { await Task.Delay(50, ct); continue; }

                    // Update Position based on the audible cursor when the client reports it.
                    var audibleCursor = _sharedMemory.GetAudibleCursor();
                    var readCursor = _sharedMemory.GetReadCursor();
                    var progressCursor = audibleCursor > 0 ? audibleCursor : readCursor;
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

                    // 限流机制：只在缓冲区空余空间不足 2048 帧时睡眠，防止因 Windows 定时器精度不足导致播放卡顿
                    while (_sharedMemory.GetReadableFrames() > _sharedMemory.BufferFrames - 2048)
                    {
                        await Task.Delay(10, ct);
                        if (_playState != 1 || ct.IsCancellationRequested)
                            break;
                    }
                    if (_playState != 1 || ct.IsCancellationRequested) continue;

                    var reader = _currentReader;
                    if (reader == null || !reader.IsReady) { await Task.Delay(100, ct); continue; }

                    if (channels != reader.Info.Channels || sampleRate != reader.Info.SampleRate)
                    {
                        sampleRate = reader.Info.SampleRate;
                        channels = reader.Info.Channels;
                        _sharedMemory.UpdateFormat(sampleRate, channels);
                        _logger.LogInformation("Dynamic audio format updated: {SampleRate} Hz, {Channels} channels", sampleRate, channels);
                    }

                    if (!_formatReadySignaled)
                    {
                        var totalFramesHint = reader.Info.TotalFrames > 0
                            ? (long)Math.Min(reader.Info.TotalFrames, (ulong)long.MaxValue)
                            : (CurrentTrack?.Duration > 0 ? (long)(CurrentTrack.Duration * sampleRate) : 0);
                        _sharedMemory.MarkFormatReady(sampleRate, channels, totalFramesHint);
                        _formatReadySignaled = true;
                    }

                    HandlePendingSharedMemorySeek(reader, sampleRate);

                    int framesPerRead = 1024; // 每次读取的音频帧数 (大约 23ms，能平稳控制占用)
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
                                _logger.LogInformation("Decoder EOF signaled: finalWrite={FinalWrite}, decodedFrames={DecodedFrames}",
                                    _sharedMemory.GetWriteCursor(), decodedFrames);
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
                            // ClientManaged mode leaves the next-track decision to the client.
                            if (CurrentMode == PlaybackMode.ClientManaged)
                            {
                                _playState = 0;
                                _sharedMemory.MarkEnded();
                                FireStateChanged(0);
                                continue;
                            }
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
                        await Task.Delay(10, ct);
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

        private void HandlePendingSharedMemorySeek(IPcmStreamReader reader, int sampleRate)
        {
            if (reader == null || !reader.CanSeek)
                return;

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
        }

        #endregion
    }
}

