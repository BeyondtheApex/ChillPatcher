using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;
using QueueChangeType = OmniMixPlayer.SDK.Interfaces.QueueChangeType;

namespace OmniMixPlayer.Backend.Audio
{
    /// <summary>
    /// 播放模式
    /// </summary>
    public enum PlaybackMode
    {
        /// <summary>客户端自行管理队列，服务仅提供音源和解码</summary>
        ClientManaged,
        /// <summary>服务端管理队列和播放决策，客户端被动接收播放事件</summary>
        ServerManaged
    }

    /// <summary>
    /// 已连接客户端状态
    /// </summary>
    public class ClientConnectionState
    {
        public string ClientId { get; set; }
        public PlaybackMode Mode { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public bool IsConnected { get; set; }
    }

    public class PlaybackController : IDisposable, IPlayQueue
    {
        private readonly ILogger _logger;
        private readonly SharedMemoryServer _sharedMemory;
        private readonly IEventBus _eventBus;
        private readonly IMusicRegistry _musicRegistry;
        private readonly IStreamingService _streamingService;
        private readonly string _stateFilePath;

        private readonly Dictionary<string, QueueSlot> _queues = new Dictionary<string, QueueSlot>();
        private string _activeQueueId;
        private const string DefaultQueueId = "default";
        private const int MaxHistoryCount = 50;
        private const int HeartbeatTimeoutSeconds = 30;

        private CancellationTokenSource _playbackCts;
        private Task _playbackTask;
        private float _volume = 1.0f;
        private volatile int _playState;
        private readonly Random _rng = new Random();
        private readonly object _lock = new object();
        private readonly object _clientLock = new object();

        private IPcmStreamReader _currentReader;
        private bool _disposed;

        // 客户端连接状态
        private ClientConnectionState _connectedClient;
        private CancellationTokenSource _heartbeatWatchCts;
        private string _lastPlayInitiator; // "server" 或 clientId

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
        public float Volume { get => _volume; set => _volume = Math.Clamp(value, 0f, 1f); }
        public bool Shuffle { get => Active.Shuffle; set => Active.Shuffle = value; }
        public RepeatMode RepeatMode { get => Active.RepeatMode; set => Active.RepeatMode = value; }

        public IReadOnlyList<MusicInfo> Queue => Active.Queue;
        public int QueueCount => Active.QueueCount;
        public int QueueIndex => Active.QueueIndex;
        public IReadOnlyList<MusicInfo> History => Active.History;
        public int HistoryCount => Active.HistoryCount;
        public int PlaylistPosition => Active.PlaylistPosition;
        public bool CanGoPrevious => Active.CanGoPrevious;
        public bool CanGoNext => Active.CanGoNext;
        public string ActiveQueueId => _activeQueueId;
        public IReadOnlyList<string> QueueIds => _queues.Keys.ToList();

        /// <summary>
        /// 当前连接的客户端信息（null 表示无客户端连接）
        /// </summary>
        public ClientConnectionState ConnectedClient
        {
            get { lock (_clientLock) return _connectedClient; }
        }

        /// <summary>
        /// 当前播放模式
        /// </summary>
        public PlaybackMode CurrentMode => ConnectedClient?.Mode ?? PlaybackMode.ServerManaged;

        /// <summary>
        /// 是否有客户端连接
        /// </summary>
        public bool HasClient => ConnectedClient != null;

        public event Action<MusicInfo> OnTrackChanged;
        public event Action<int> OnStateChanged;
        public event Action<float> OnPositionChanged;
        public event Action OnQueueChanged;
        public event Action<ClientConnectionState> OnClientConnected;
        public event Action OnClientDisconnected;

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
            string configDir = null)
        {
            _logger = logger;
            _sharedMemory = sharedMemory;
            _eventBus = eventBus;
            _musicRegistry = musicRegistry;
            _streamingService = streamingService;

            lock (_lock)
            {
                _queues[DefaultQueueId] = new QueueSlot(DefaultQueueId, "Default");
                _activeQueueId = DefaultQueueId;
            }

            if (!string.IsNullOrEmpty(configDir))
            {
                _stateFilePath = Path.Combine(configDir, "playback_state.json");
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

        #region IPlayQueue — delegates to Active

        public void Play(string uuid = null)
        {
            if (!string.IsNullOrEmpty(uuid))
            {
                var music = _musicRegistry.GetMusic(uuid);
                if (music == null) { _logger.LogWarning("Track not found: {Uuid}", uuid); return; }
                Active.SetCurrentTrack(music);
            }
            else
            {
                if (Active.CurrentTrack == null && Active.QueueCount > 0)
                    Active.SetQueueIndex(0);
            }

            if (Active.CurrentTrack != null) StartPlayback();
        }

        public void Pause()
        {
            _playState = 2;
            _sharedMemory.SetPlayState(2);
            FireStateChanged(2);
        }

        public void Resume()
        {
            if (Active.CurrentTrack != null)
            {
                _playState = 1;
                _sharedMemory.SetPlayState(1);
                FireStateChanged(1);
            }
        }

        public void Stop()
        {
            StopPlayback();
            _playState = 0;
            _sharedMemory.SetPlayState(0);
            FireStateChanged(0);
        }

        public void Toggle()
        {
            if (_playState == 1) Pause(); else Play(null);
        }

        public void Next()
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

            var next = active.AdvanceNext(Active.Shuffle, _rng);
            if (next != null)
            {
                StartPlayback();
                return;
            }

            // Fill from all songs
            var allSongs = _musicRegistry.GetAllMusic().Where(m => !m.IsExcluded).ToList();
            if (allSongs.Count > 0)
            {
                var pick = allSongs[_rng.Next(allSongs.Count)];
                active.AddToQueue(pick);
                active.SetCurrentTrack(pick);
                StartPlayback();
            }
        }

        public void Prev()
        {
            var prev = Active.GoPreviousInHistory();
            if (prev != null)
                StartPlayback();
        }

        public void Seek(float positionSeconds)
        {
            _sharedMemory.WriteI64(0x1C, (long)(positionSeconds * _sharedMemory.SampleRate));
            if (_currentReader != null && _currentReader.CanSeek)
            {
                ulong targetFrame = (ulong)(positionSeconds * _currentReader.Info.SampleRate);
                if (_currentReader.Seek(targetFrame))
                    Position = positionSeconds;
            }
        }

        public void SetVolume(float volume) => Volume = volume;
        public void SetShuffle(bool enabled) => Shuffle = enabled;
        public void SetRepeatMode(RepeatMode mode) => RepeatMode = mode;

        public void AddToQueue(string uuid)
        {
            var music = _musicRegistry.GetMusic(uuid);
            if (music == null) return;
            Active.AddToQueue(music);
            EmitQueueChanged(QueueChangeType.Enqueued, uuid);
        }

        public void AddToQueueRange(IEnumerable<string> uuids)
        {
            foreach (var uuid in uuids)
            {
                var m = _musicRegistry.GetMusic(uuid);
                if (m != null) Active.AddToQueue(m);
            }
            EmitQueueChanged(QueueChangeType.Enqueued, null);
        }

        public void RemoveFromQueue(int index) { var a = Active; if (index >= 0 && index < a.QueueCount) { a.RemoveFromQueue(index); EmitQueueChanged(QueueChangeType.Removed, null); } }
        public void MoveInQueue(int from, int to) { Active.MoveInQueue(from, to); EmitQueueChanged(QueueChangeType.Moved, null); }
        public void ClearQueue() { Active.ClearQueue(); EmitQueueChanged(QueueChangeType.Cleared, null); }
        public void ClearHistory() { Active.ClearHistory(); }

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
        }

        public bool IsExcluded(string uuid) { var m = _musicRegistry.GetMusic(uuid); return m?.IsExcluded ?? false; }
        public void SetExcluded(string uuid, bool isExcluded)
        {
            var m = _musicRegistry.GetMusic(uuid);
            if (m == null) return;
            m.IsExcluded = isExcluded;
            _musicRegistry.UpdateMusic(m);
        }

        #endregion

        #region Client Connection Management

        /// <summary>
        /// 客户端请求连接。同一时间只允许一个客户端连接。
        /// </summary>
        /// <param name="clientId">客户端标识</param>
        /// <param name="mode">播放模式：ClientManaged=客户端自管队列, ServerManaged=服务端管队列</param>
        /// <returns>连接成功返回 true，已有其他客户端连接返回 false</returns>
        public bool ConnectClient(string clientId, PlaybackMode mode)
        {
            lock (_clientLock)
            {
                // 如果已有连接，检查是否超时
                if (_connectedClient != null && _connectedClient.IsConnected)
                {
                    var elapsed = DateTime.UtcNow - _connectedClient.LastHeartbeat;
                    if (elapsed.TotalSeconds < HeartbeatTimeoutSeconds)
                    {
                        _logger.LogWarning("Client connection rejected: {ClientId} already connected", _connectedClient.ClientId);
                        return false;
                    }
                    // 旧连接已超时，强制断开
                    _logger.LogWarning("Old client {OldId} heartbeat timeout ({Seconds:F0}s), force disconnect",
                        _connectedClient.ClientId, elapsed.TotalSeconds);
                    DisconnectClientInternal();
                }

                _connectedClient = new ClientConnectionState
                {
                    ClientId = clientId,
                    Mode = mode,
                    ConnectedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    IsConnected = true
                };

                _logger.LogInformation("Client connected: {ClientId} mode={Mode}", clientId, mode);

                // 启动心跳监控
                StartHeartbeatWatch();

                OnClientConnected?.Invoke(_connectedClient);
                return true;
            }
        }

        /// <summary>
        /// 客户端心跳
        /// </summary>
        public bool Heartbeat(string clientId)
        {
            lock (_clientLock)
            {
                if (_connectedClient == null || !_connectedClient.IsConnected)
                    return false;
                if (_connectedClient.ClientId != clientId)
                    return false;

                _connectedClient.LastHeartbeat = DateTime.UtcNow;
                return true;
            }
        }

        /// <summary>
        /// 断开客户端连接
        /// </summary>
        public bool DisconnectClient(string clientId)
        {
            lock (_clientLock)
            {
                if (_connectedClient == null || _connectedClient.ClientId != clientId)
                    return false;

                DisconnectClientInternal();
                return true;
            }
        }

        private void DisconnectClientInternal()
        {
            if (_connectedClient == null) return;

            _logger.LogInformation("Client disconnected: {ClientId}", _connectedClient.ClientId);

            // 停止当前播放
            if (_playState == 1)
            {
                Stop();
            }

            _connectedClient = null;
            StopHeartbeatWatch();
            OnClientDisconnected?.Invoke();
        }

        private void StartHeartbeatWatch()
        {
            StopHeartbeatWatch();
            _heartbeatWatchCts = new CancellationTokenSource();
            var ct = _heartbeatWatchCts.Token;

            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);
                    if (ct.IsCancellationRequested) break;

                    lock (_clientLock)
                    {
                        if (_connectedClient == null || !_connectedClient.IsConnected) break;

                        var elapsed = DateTime.UtcNow - _connectedClient.LastHeartbeat;
                        if (elapsed.TotalSeconds > HeartbeatTimeoutSeconds)
                        {
                            _logger.LogWarning("Client {ClientId} heartbeat timeout, disconnecting",
                                _connectedClient.ClientId);
                            DisconnectClientInternal();
                            break;
                        }
                    }
                }
            }, ct);
        }

        private void StopHeartbeatWatch()
        {
            _heartbeatWatchCts?.Cancel();
            _heartbeatWatchCts?.Dispose();
            _heartbeatWatchCts = null;
        }

        #endregion

        #region Public misc

        public void SetQueue(List<string> uuids) { Active.SetQueue(uuids); EmitQueueChanged(QueueChangeType.Enqueued, null); }
        public bool Fav(string uuid, bool isFav) { SetFavorite(uuid, isFav); return true; }

        public object GetStatus()
        {
            var a = Active;
            var client = ConnectedClient;
            return new
            {
                IsPlaying = _playState == 1,
                Position,
                Volume = _volume,
                Shuffle = a.Shuffle,
                RepeatMode = a.RepeatMode.ToString().ToLowerInvariant(),
                QueueLength = a.QueueCount,
                QueueIndex = a.QueueIndex,
                PlaylistPosition = a.PlaylistPosition,
                HistoryCount = a.HistoryCount,
                HistoryPosition = GetHistoryPosition(a),
                ActiveQueueId = _activeQueueId,
                PlaybackMode = CurrentMode.ToString(),
                HasClient = client != null,
                ClientId = client?.ClientId,
                PlayInitiator = _lastPlayInitiator,
                CurrentTrack = a.CurrentTrack != null ? TrackMap(a.CurrentTrack) : null
            };
        }

        /// <summary>
        /// 获取客户端连接状态（供 API 返回）
        /// </summary>
        public object GetClientStatus()
        {
            var client = ConnectedClient;
            if (client == null)
                return new { connected = false };
            return new
            {
                connected = true,
                clientId = client.ClientId,
                mode = client.Mode.ToString(),
                connectedAt = client.ConnectedAt.ToString("O"),
                lastHeartbeat = client.LastHeartbeat.ToString("O"),
                playbackMode = CurrentMode.ToString()
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
                if (string.IsNullOrEmpty(_stateFilePath)) return;
                var data = new PlaybackStateData
                {
                    ActiveQueueId = _activeQueueId,
                    Volume = _volume,
                    Queues = _queues.Values.Select(q => q.Serialize()).ToList()
                };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var dir = Path.GetDirectoryName(_stateFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to save playback state"); }
        }

        private void RestoreState()
        {
            try
            {
                if (string.IsNullOrEmpty(_stateFilePath) || !File.Exists(_stateFilePath)) return;
                var json = File.ReadAllText(_stateFilePath);
                var data = JsonSerializer.Deserialize<PlaybackStateData>(json);
                if (data == null) return;

                _volume = data.Volume;
                _activeQueueId = data.ActiveQueueId ?? DefaultQueueId;

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

                _logger.LogInformation("Restored {Count} queues, active: {Id}", _queues.Count, _activeQueueId);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to restore playback state"); }
        }

        private class PlaybackStateData
        {
            public string ActiveQueueId { get; set; }
            public float Volume { get; set; } = 1.0f;
            public List<QueueSlotData> Queues { get; set; } = new();
        }

        #endregion

        #region Internals

        private void StartPlayback()
        {
            StopPlayback();
            var track = Active.CurrentTrack;
            if (track == null) return;

            Active.PushHistory();
            // 记录播放发起者：ClientManaged 模式下来自客户端，否则来自服务端
            _lastPlayInitiator = CurrentMode == PlaybackMode.ClientManaged
                ? ConnectedClient?.ClientId ?? "client"
                : "server";
            FireTrackChanged(track);
            _sharedMemory.SetCurrentUuid(track.UUID);
            SaveState();

            _currentReader = CreateStreamReader(track);
            _playbackCts = new CancellationTokenSource();
            _playState = 1;
            _sharedMemory.SetPlayState(1);
            Position = 0;
            FireStateChanged(1);
            _playbackTask = Task.Run(() => PlaybackLoopAsync(_playbackCts.Token));
        }

        private IPcmStreamReader CreateStreamReader(MusicInfo music)
        {
            try
            {
                string url = music.SourcePath;
                string format = DetectFormat(music);
                float duration = music.Duration > 0 ? music.Duration : 240f;
                string cacheKey = music.UUID ?? Guid.NewGuid().ToString("N");
                if (!string.IsNullOrEmpty(url))
                    return _streamingService.CreateStream(url, format, duration, cacheKey);
            }
            catch (Exception ex) { _logger.LogError(ex, "CreateStreamReader failed: {Uuid}", music.UUID); }
            return new NullPcmReader();
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
            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
            _playbackCts = null;
            _playState = 0;
            _sharedMemory.SetPlayState(0);
            var old = _currentReader;
            _currentReader = null;
            old?.Dispose();
        }

        private async Task PlaybackLoopAsync(CancellationToken ct)
        {
            var pcmBuffer = new float[4096];
            int sampleRate = _sharedMemory.SampleRate;
            int channels = _sharedMemory.Channels;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_playState != 1) { await Task.Delay(50, ct); continue; }

                    var reader = _currentReader;
                    if (reader == null || !reader.IsReady) { await Task.Delay(100, ct); continue; }

                    int framesPerRead = pcmBuffer.Length / channels;
                    long framesRead = reader.ReadFrames(pcmBuffer, framesPerRead);

                    if (framesRead <= 0)
                    {
                        if (reader.IsEndOfStream)
                        {
                            if (Active.RepeatMode == RepeatMode.One) { reader.Seek(0); Position = 0; continue; }
                            // ClientManaged 模式下不自动切歌，由客户端决定下一首
                            if (CurrentMode == PlaybackMode.ClientManaged)
                            {
                                _playState = 0;
                                _sharedMemory.SetPlayState(0);
                                FireStateChanged(0);
                                continue;
                            }
                            await Task.Delay(50, ct);
                            Next();
                            continue;
                        }
                        await Task.Delay(10, ct);
                        continue;
                    }

                    _sharedMemory.WriteFrames(pcmBuffer, (int)framesRead);
                    Position += (float)framesRead / sampleRate;
                    FirePositionChanged(Position);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogError(ex, "Playback loop error"); }
        }

        private void EmitQueueChanged(OmniMixPlayer.SDK.Interfaces.QueueChangeType type, string uuid)
        {
            FireQueueChanged(type, uuid);
            SaveState();
        }

        private static string SanitizeId(string name) =>
            string.Join("_", name.Split(Path.GetInvalidFileNameChars())).ToLowerInvariant();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            SaveState();
            StopPlayback();
            StopHeartbeatWatch();
        }

        private sealed class NullPcmReader : IPcmStreamReader
        {
            public PcmStreamInfo Info => new() { SampleRate = 44100, Channels = 2 };
            public ulong CurrentFrame => 0;
            public bool IsEndOfStream => true;
            public bool IsReady => true;
            public bool CanSeek => false;
            public bool HasPendingSeek => false;
            public long PendingSeekFrame => -1;
            public double CacheProgress => -1;
            public bool IsCacheComplete => false;
            public long ReadFrames(float[] buffer, int framesToRead) { Array.Clear(buffer, 0, framesToRead * 2); return 0; }
            public bool Seek(ulong frameIndex) => false;
            public void CancelPendingSeek() { }
            public void Dispose() { }
        }

        #endregion
    }

    #region QueueSlot

    internal class QueueSlot
    {
        private readonly List<MusicInfo> _queue = new();
        private int _queueIndex = -1;
        private readonly List<MusicInfo> _history = new();
        private int _historyPosition = -1;
        private int _playlistPosition;

        public string Id { get; }
        public string Name { get; set; }
        public bool Shuffle { get; set; }
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;

        public MusicInfo CurrentTrack => _queue.Count > 0 && _queueIndex >= 0 && _queueIndex < _queue.Count ? _queue[_queueIndex] : null;
        public IReadOnlyList<MusicInfo> Queue => _queue;
        public int QueueCount => _queue.Count;
        public int QueueIndex => _queueIndex;
        public IReadOnlyList<MusicInfo> History => _history;
        public int HistoryCount => _history.Count;
        public int PlaylistPosition => _playlistPosition;
        public bool IsInHistoryMode => _historyPosition >= 0;

        public bool CanGoPrevious
        {
            get
            {
                if (_historyPosition < 0) return _history.Count >= 2;
                return _historyPosition + 1 < _history.Count;
            }
        }

        public bool CanGoNext => _queue.Count > 0;

        public QueueSlot(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public QueueInfo GetInfo() => new()
        {
            Id = Id,
            Name = Name,
            SongCount = _queue.Count,
            IsActive = false,
            HistoryCount = _history.Count,
            Shuffle = Shuffle,
            RepeatMode = RepeatMode
        };

        public void SetCurrentTrack(MusicInfo m)
        {
            var idx = _queue.FindIndex(q => q.UUID == m.UUID);
            if (idx >= 0) _queueIndex = idx;
            else { _queue.Insert(0, m); _queueIndex = 0; }
            _historyPosition = -1;
        }

        public void SetQueueIndex(int idx) { _queueIndex = idx; _historyPosition = -1; }

        public void AddToQueue(MusicInfo m) { _queue.Add(m); }
        public void RemoveFromQueue(int idx) { if (idx >= 0 && idx < _queue.Count) _queue.RemoveAt(idx); }
        public void MoveInQueue(int f, int t) { var item = _queue[f]; _queue.RemoveAt(f); _queue.Insert(t, item); }
        public void ClearQueue() { _queue.Clear(); _queueIndex = -1; }

        public void PushHistory()
        {
            var current = CurrentTrack;
            if (current == null) return;
            if (_historyPosition > 0) _history.RemoveRange(0, _historyPosition);
            if (_history.Count > 0 && _history[0]?.UUID == current.UUID) return;
            _history.Insert(0, current);
            while (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
            _historyPosition = -1;
        }

        public void ClearHistory() { _history.Clear(); _historyPosition = -1; }

        public MusicInfo GoPreviousInHistory()
        {
            if (_historyPosition < 0) _historyPosition = 1;
            else _historyPosition++;
            if (_historyPosition >= _history.Count) return null;
            return _history[_historyPosition];
        }

        public MusicInfo GoNextInHistory()
        {
            if (_historyPosition <= 0) { _historyPosition = -1; return null; }
            _historyPosition--;
            return _historyPosition >= 0 ? _history[_historyPosition] : null;
        }

        public MusicInfo AdvanceNext(bool shuffle, Random rng)
        {
            if (_queue.Count == 0) return null;

            if (shuffle)
            {
                var candidates = _queue.Where(m => !m.IsExcluded).ToList();
                if (candidates.Count == 0) candidates = _queue;
                var pick = candidates[rng.Next(candidates.Count)];
                _queueIndex = _queue.IndexOf(pick);
                _playlistPosition = (_queueIndex + 1) % _queue.Count;
                return pick;
            }
            else
            {
                for (int i = 1; i <= _queue.Count; i++)
                {
                    int idx = (_queueIndex + i) % _queue.Count;
                    if (!_queue[idx].IsExcluded)
                    {
                        _queueIndex = idx;
                        _playlistPosition = (idx + 1) % _queue.Count;
                        return _queue[idx];
                    }
                }
                // All excluded, just play next regardless
                _queueIndex = (_queueIndex + 1) % _queue.Count;
                _playlistPosition = (_queueIndex + 1) % _queue.Count;
                return _queue[_queueIndex];
            }
        }

        public void ImportFromPlaylist(IReadOnlyList<MusicInfo> songs, bool replace)
        {
            if (replace) _queue.Clear();
            foreach (var s in songs)
                if (!_queue.Any(q => q.UUID == s.UUID)) _queue.Add(s);
        }

        public void SetQueue(List<string> uuids)
        {
            _queue.Clear();
            foreach (var u in uuids) { /* filled externally */ }
            _queueIndex = -1;
        }

        public QueueSlotData Serialize() => new()
        {
            Id = Id,
            Name = Name,
            SongUuids = _queue.Select(m => m.UUID).ToList(),
            Index = _queueIndex,
            HistoryUuids = _history.Select(m => m.UUID).ToList(),
            HistoryPosition = _historyPosition,
            PlaylistPosition = _playlistPosition,
            Shuffle = Shuffle,
            RepeatMode = RepeatMode.ToString()
        };

        public static QueueSlot Deserialize(QueueSlotData data, IMusicRegistry registry)
        {
            var slot = new QueueSlot(data.Id, data.Name)
            {
                _queueIndex = data.Index,
                _historyPosition = data.HistoryPosition,
                _playlistPosition = data.PlaylistPosition,
                Shuffle = data.Shuffle
            };
            if (Enum.TryParse<RepeatMode>(data.RepeatMode, out var rm)) slot.RepeatMode = rm;

            if (data.SongUuids != null)
                foreach (var u in data.SongUuids) { var m = registry.GetMusic(u); if (m != null) slot._queue.Add(m); }
            if (data.HistoryUuids != null)
                foreach (var u in data.HistoryUuids) { var m = registry.GetMusic(u); if (m != null) slot._history.Add(m); }

            return slot;
        }
    }

    public class QueueSlotData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> SongUuids { get; set; } = new();
        public int Index { get; set; } = -1;
        public List<string> HistoryUuids { get; set; } = new();
        public int HistoryPosition { get; set; } = -1;
        public int PlaylistPosition { get; set; }
        public bool Shuffle { get; set; }
        public string RepeatMode { get; set; } = "none";
    }

    public class QueueInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SongCount { get; set; }
        public bool IsActive { get; set; }
        public int HistoryCount { get; set; }
        public bool Shuffle { get; set; }
        public RepeatMode RepeatMode { get; set; }
    }

    #endregion
}
