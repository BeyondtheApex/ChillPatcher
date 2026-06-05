using Bulbul;
using ChillPatcher.Patches.UIFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 播放队列管理器（后端代理模式）
    /// 
    /// 核心变更：
    /// - 后端 OmniMixPlayer Profile 维护队列/历史/播放状态的权威数据
    /// - 游戏侧只维护本地镜像用于 UI 显示
    /// - 所有播放控制（Next/Previous/Advance）由后端决定
    /// - Enqueue/Remove/Clear 操作转发到后端 API
    /// - 收到后端的 WebSocket 事件时更新本地镜像
    /// </summary>
    public class PlayQueueManager
    {
        private static PlayQueueManager _instance;
        public static PlayQueueManager Instance => _instance ??= new PlayQueueManager();

        private List<GameAudioInfo> _queue = new List<GameAudioInfo>();
        private List<GameAudioInfo> _history = new List<GameAudioInfo>();
        private GameAudioInfo _currentPlaying;

        public IReadOnlyList<GameAudioInfo> Queue => _queue;
        public IReadOnlyList<GameAudioInfo> History => _history;
        public GameAudioInfo CurrentPlaying => _currentPlaying ?? (_queue.Count > 0 ? _queue[0] : null);
        public bool IsQueueEmpty => _queue.Count <= 1;
        public int PendingCount => Math.Max(0, _queue.Count - 1);
        public int PlaylistPosition { get; private set; } = 0;
        public int HistoryPosition => -1;
        public int ExtendedSteps => 0;
        public bool IsInHistoryMode => false;
        public bool IsInExtendedMode => false;
        public bool CanGoPrevious => _history.Count >= 2;
        public int HistoryCount => _history.Count;

        public event Action OnQueueChanged;
        public event Action OnHistoryChanged;
        public event Action<GameAudioInfo> OnCurrentChanged;
        public event Action<int> OnPlaylistPositionChanged;

        private PlayQueueManager()
        {
            MusicService_Excluded_Patch.OnSongExcludedChanged += HandleSongExcludedChanged;
        }

        private void HandleSongExcludedChanged(string uuid, bool isExcluded)
        {
            if (isExcluded) OnSongExcluded(uuid);
        }

        #region 后端事件驱动更新

        public void UpdateFromBackendQueue(List<string> queueUuids, List<string> historyUuids, int playlistPosition)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var allMusic = musicService?.AllMusicList;

            if (queueUuids != null && allMusic != null)
            {
                _queue.Clear();
                if (_currentPlaying != null) _queue.Add(_currentPlaying);
                foreach (var uuid in queueUuids)
                {
                    var audio = allMusic.FirstOrDefault(m => m?.UUID == uuid);
                    if (audio != null && audio.UUID != _currentPlaying?.UUID) _queue.Add(audio);
                }
            }

            if (historyUuids != null && allMusic != null)
            {
                _history.Clear();
                foreach (var uuid in historyUuids)
                {
                    var audio = allMusic.FirstOrDefault(m => m?.UUID == uuid);
                    if (audio != null) _history.Add(audio);
                }
            }

            PlaylistPosition = playlistPosition;

            OnQueueChanged?.Invoke();
            OnHistoryChanged?.Invoke();
            if (CurrentPlaying != null) OnCurrentChanged?.Invoke(CurrentPlaying);
            OnPlaylistPositionChanged?.Invoke(PlaylistPosition);
        }

        public void UpdateCurrentTrack(string uuid)
        {
            var musicService = MusicService_RemoveLimit_Patch.CurrentInstance;
            var allMusic = musicService?.AllMusicList;
            if (allMusic == null) return;
            var audio = allMusic.FirstOrDefault(m => m?.UUID == uuid);
            if (audio == null) return;

            _currentPlaying = audio;
            if (_queue.Count > 0) _queue[0] = audio;
            else _queue.Add(audio);

            OnCurrentChanged?.Invoke(audio);
            OnQueueChanged?.Invoke();
        }

        #endregion

        #region 队列操作（转发到后端 API）

        public void Enqueue(GameAudioInfo audio)
        {
            if (audio == null || string.IsNullOrEmpty(audio.UUID)) return;
            _queue.Add(audio);
            OnQueueChanged?.Invoke();
            _ = OmniMixIntegration.Instance.AddToQueue(audio.UUID);
        }

        public void EnqueueRange(IEnumerable<GameAudioInfo> audios)
        {
            if (audios == null) return;
            var list = audios.ToList();
            _queue.AddRange(list);
            OnQueueChanged?.Invoke();
        }

        public void InsertNext(GameAudioInfo audio)
        {
            if (audio == null || string.IsNullOrEmpty(audio.UUID)) return;
            Insert(1, audio);
        }

        public void Insert(int index, GameAudioInfo audio)
        {
            if (audio == null) return;
            index = Math.Max(0, Math.Min(index, _queue.Count));
            _queue.Insert(index, audio);
            OnQueueChanged?.Invoke();
            if (!string.IsNullOrEmpty(audio.UUID))
                _ = OmniMixIntegration.Instance.InsertIntoQueue(Math.Max(0, index - 1), new[] { audio.UUID });
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _queue.Count) return false;
            var uuid = _queue[index]?.UUID;
            _queue.RemoveAt(index);
            OnQueueChanged?.Invoke();
            if (index == 0)
                _ = OmniMixIntegration.Instance.Next();
            else
                _ = OmniMixIntegration.Instance.RemoveFromQueue(uuid);
            return true;
        }

        public bool Remove(GameAudioInfo audio)
        {
            if (audio == null) return false;
            var index = _queue.IndexOf(audio);
            bool removed = _queue.Remove(audio);
            if (removed)
            {
                OnQueueChanged?.Invoke();
                if (index == 0)
                    _ = OmniMixIntegration.Instance.Next();
                else
                    _ = OmniMixIntegration.Instance.RemoveFromQueue(audio.UUID);
            }
            return removed;
        }

        public bool RemoveByUUID(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return false;
            var audio = _queue.FirstOrDefault(a => a?.UUID == uuid);
            if (audio != null) return Remove(audio);
            return false;
        }

        public void Move(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _queue.Count) return;
            if (toIndex < 0 || toIndex >= _queue.Count) return;
            if (fromIndex == toIndex) return;
            var item = _queue[fromIndex];
            _queue.RemoveAt(fromIndex);
            _queue.Insert(toIndex, item);
            OnQueueChanged?.Invoke();
            if (fromIndex > 0 && toIndex > 0)
                _ = OmniMixIntegration.Instance.MoveInQueue(fromIndex - 1, toIndex - 1);
        }

        public void ClearPending()
        {
            if (_queue.Count > 1)
            {
                var current = _queue[0];
                _queue.Clear();
                _queue.Add(current);
                OnQueueChanged?.Invoke();
            }
            _ = OmniMixIntegration.Instance.ClearQueueAsync();
        }

        public void Clear()
        {
            _queue.Clear();
            _history.Clear();
            OnQueueChanged?.Invoke();
            OnHistoryChanged?.Invoke();
        }

        public void ClearHistory()
        {
            _history.Clear();
            OnHistoryChanged?.Invoke();
            _ = OmniMixIntegration.Instance.ClearHistoryAsync();
        }

        public void SetCurrentPlaying(GameAudioInfo audio, bool updatePosition = false, int newPosition = 0, bool addToHistory = true)
        {
            if (audio == null || string.IsNullOrEmpty(audio.UUID)) return;
            _currentPlaying = audio;
            if (_queue.Count > 0) _queue[0] = audio;
            else _queue.Add(audio);
            if (updatePosition) { PlaylistPosition = newPosition; OnPlaylistPositionChanged?.Invoke(PlaylistPosition); }
            OnCurrentChanged?.Invoke(audio);
            OnQueueChanged?.Invoke();
            _ = OmniMixIntegration.Instance.Play(audio.UUID);
        }

        public void NotifyCurrentChanged(GameAudioInfo audio)
        {
            OnCurrentChanged?.Invoke(audio);
            OnQueueChanged?.Invoke();
        }

        #endregion

        #region 已废弃（后端自动管理，保留空壳以兼容调用方）

        public GameAudioInfo AdvanceToNext(IReadOnlyList<GameAudioInfo> currentPlaylist, bool isShuffle, Func<GameAudioInfo, bool> isExcludedFunc = null) => null;
        public System.Threading.Tasks.Task<GameAudioInfo> AdvanceToNextAsync(
            IReadOnlyList<GameAudioInfo> currentPlaylist, bool isShuffle,
            Func<GameAudioInfo, bool> isExcludedFunc = null,
            Func<IReadOnlyList<GameAudioInfo>> getUpdatedPlaylistFunc = null)
            => System.Threading.Tasks.Task.FromResult<GameAudioInfo>(null);
        public GameAudioInfo GoPrevious() => null;
        public GameAudioInfo GoNext() => null;
        public GameAudioInfo GoPreviousExtended(IReadOnlyList<GameAudioInfo> cp, GameAudioInfo ca, Func<GameAudioInfo, bool> ef = null) => null;
        public GameAudioInfo GoNextExtended(IReadOnlyList<GameAudioInfo> cp, GameAudioInfo ca, Func<GameAudioInfo, bool> ef = null) => null;
        public void ResetHistoryPosition() { }
        public void ResetExtendedSteps() { }

        #endregion

        #region 辅助方法

        public void OnSongExcluded(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return;
            for (int i = _queue.Count - 1; i >= 1; i--)
                if (_queue[i]?.UUID == uuid) _queue.RemoveAt(i);
            _history.RemoveAll(a => a?.UUID == uuid);
            OnQueueChanged?.Invoke();
            OnHistoryChanged?.Invoke();
        }

        public int IndexOf(GameAudioInfo audio) => _queue.IndexOf(audio);
        public bool Contains(string uuid) => !string.IsNullOrEmpty(uuid) && _queue.Any(a => a?.UUID == uuid);
        public bool Contains(GameAudioInfo audio) => _queue.Contains(audio);
        public List<GameAudioInfo> ToList() => new List<GameAudioInfo>(_queue);
        public List<string> GetQueueUUIDs() => _queue.Where(a => a != null).Select(a => a.UUID).ToList();
        public List<string> GetHistoryUUIDs() => _history.Where(a => a != null).Select(a => a.UUID).ToList();

        public void RestoreFromUUIDs(List<string> uuids, IReadOnlyList<GameAudioInfo> allMusic)
        {
            _queue.Clear();
            if (uuids != null && allMusic != null)
                foreach (var uuid in uuids)
                { var a = allMusic.FirstOrDefault(m => m?.UUID == uuid); if (a != null) _queue.Add(a); }
            OnQueueChanged?.Invoke();
        }

        public void RestoreHistoryFromUUIDs(List<string> uuids, IReadOnlyList<GameAudioInfo> allMusic)
        {
            _history.Clear();
            if (uuids != null && allMusic != null)
                foreach (var uuid in uuids)
                { var a = allMusic.FirstOrDefault(m => m?.UUID == uuid); if (a != null) _history.Add(a); }
            OnHistoryChanged?.Invoke();
        }

        public void RestoreFullState(List<string> queueUuids, List<string> historyUuids,
            int playlistPosition, int historyPosition, int extendedSteps, IReadOnlyList<GameAudioInfo> allMusic)
        {
            RestoreFromUUIDs(queueUuids, allMusic);
            RestoreHistoryFromUUIDs(historyUuids, allMusic);
            PlaylistPosition = playlistPosition;
        }

        public void AddToHistory(GameAudioInfo audio)
        {
            if (audio == null) return;
            _history.RemoveAll(a => a?.UUID == audio.UUID);
            _history.Insert(0, audio);
            while (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
            OnHistoryChanged?.Invoke();
        }

        #endregion
    }
}
