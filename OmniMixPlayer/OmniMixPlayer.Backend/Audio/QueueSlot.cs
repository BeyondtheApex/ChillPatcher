using System;
using System.Collections.Generic;
using System.Linq;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public sealed class PlaylistSourceRequest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string[] uuids { get; set; } = Array.Empty<string>();
    }

    public sealed class PlaylistSourceInfo
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public int SongCount { get; init; }
    }

    internal sealed class PlaylistSource
    {
        public string Id { get; }
        public string Name { get; }
        public List<MusicInfo> Songs { get; }

        public PlaylistSource(string id, string name, IEnumerable<MusicInfo> songs)
        {
            Id = id;
            Name = name;
            Songs = songs?.Where(s => s != null).ToList() ?? new List<MusicInfo>();
        }

        public PlaylistSourceInfo ToInfo() => new()
        {
            Id = Id,
            Name = Name,
            SongCount = Songs.Count
        };

        public PlaylistSourceData Serialize() => new()
        {
            Id = Id,
            Name = Name,
            SongUuids = Songs.Select(s => s.UUID).ToList()
        };
    }

    internal class QueueSlot
    {
        private readonly List<PlaylistSource> _playlistSources = new();
        private readonly List<MusicInfo> _playlistCache = new();
        private readonly List<MusicInfo> _queue = new();
        private readonly List<MusicInfo> _history = new();
        private MusicInfo _currentTrack;
        private int _historyPosition = -1;
        private int _playlistPosition;
        private bool _playlistDirty = true;

        public string Id { get; }
        public string Name { get; set; }
        public bool Shuffle { get; set; }
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;

        public MusicInfo CurrentTrack => _currentTrack;
        public IReadOnlyList<MusicInfo> Queue => _queue;
        public int QueueCount => _queue.Count;
        public int QueueIndex => -1;
        public IReadOnlyList<MusicInfo> History => _history;
        public int HistoryCount => _history.Count;
        public IReadOnlyList<PlaylistSourceInfo> PlaylistSources => _playlistSources.Select(s => s.ToInfo()).ToList();
        public IReadOnlyList<MusicInfo> Playlist => GetPlaylist();
        public int PlaylistCount => Playlist.Count;
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

        public bool CanGoNext => IsInHistoryMode || _queue.Count > 0 || PlaylistCount > 0 || CurrentTrack != null;

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
            _currentTrack = m;
            _historyPosition = -1;
        }

        public void SetQueueIndex(int idx)
        {
            if (idx >= 0 && idx < _queue.Count)
            {
                _currentTrack = _queue[idx];
                _queue.RemoveAt(idx);
                _historyPosition = -1;
            }
        }

        public void InsertQueue(IEnumerable<MusicInfo> songs, int index) => InsertUnique(_queue, songs, index);
        public void InsertHistory(IEnumerable<MusicInfo> songs, int index)
        {
            InsertUnique(_history, songs, index);
            if (_historyPosition >= _history.Count) _historyPosition = -1;
        }
        public bool RemoveFromQueue(int idx) => RemoveAt(_queue, idx);
        public bool RemoveFromQueue(string uuid) => RemoveByUuid(_queue, uuid);
        public bool RemoveFromHistory(int idx)
        {
            var ok = RemoveAt(_history, idx);
            if (ok && _historyPosition >= _history.Count) _historyPosition = -1;
            return ok;
        }
        public bool RemoveFromHistory(string uuid)
        {
            var ok = RemoveByUuid(_history, uuid);
            if (ok && _historyPosition >= _history.Count) _historyPosition = -1;
            return ok;
        }
        public bool MoveInQueue(int f, int t)
        {
            return MoveInList(_queue, f, t);
        }
        public bool MoveInHistory(int f, int t) => MoveInList(_history, f, t);
        public void ClearQueue() { _queue.Clear(); }
        public void ReplacePlaylistSources(IEnumerable<PlaylistSource> sources)
        {
            _playlistSources.Clear();
            foreach (var source in sources ?? Array.Empty<PlaylistSource>())
                if (source != null) _playlistSources.Add(source);
            _playlistDirty = true;
            if (_playlistPosition >= PlaylistCount) _playlistPosition = 0;
        }
        public void InsertPlaylistSource(PlaylistSource source, int index)
        {
            if (source == null) return;
            RemovePlaylistSource(source.Id);
            index = Math.Clamp(index, 0, _playlistSources.Count);
            _playlistSources.Insert(index, source);
            _playlistDirty = true;
            if (_playlistPosition >= PlaylistCount) _playlistPosition = 0;
        }
        public bool RemovePlaylistSource(string id)
        {
            var idx = _playlistSources.FindIndex(s => s.Id == id);
            if (idx < 0) return false;
            _playlistSources.RemoveAt(idx);
            _playlistDirty = true;
            if (_playlistPosition >= PlaylistCount) _playlistPosition = 0;
            return true;
        }
        public void ReplaceQueue(IEnumerable<MusicInfo> songs)
        {
            _queue.Clear();
            InsertUnique(_queue, songs, 0);
        }

        public void MarkCurrentStarted()
        {
            var current = CurrentTrack;
            if (current == null) return;
            if (_historyPosition >= 0) return;
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
            _currentTrack = _history[_historyPosition];
            return _currentTrack;
        }

        public MusicInfo GoNextInHistory()
        {
            if (_historyPosition <= 0) { _historyPosition = -1; return null; }
            _historyPosition--;
            _currentTrack = _history[_historyPosition];
            return _currentTrack;
        }

        public MusicInfo SelectNext(IReadOnlyList<MusicInfo> playlist, bool shuffle, Random rng)
        {
            _historyPosition = -1;

            while (_queue.Count > 0)
            {
                var queued = _queue[0];
                _queue.RemoveAt(0);
                if (queued == null || queued.IsExcluded) continue;
                _currentTrack = queued;
                return _currentTrack;
            }

            if (playlist == null || playlist.Count == 0)
            {
                _currentTrack = null;
                return null;
            }

            if (shuffle)
            {
                var candidates = playlist.Where(m => m != null && !m.IsExcluded).ToList();
                if (candidates.Count == 0) candidates = playlist.Where(m => m != null).ToList();
                if (candidates.Count == 0)
                {
                    _currentTrack = null;
                    return null;
                }
                var pick = candidates[rng.Next(candidates.Count)];
                _playlistPosition = (playlist.ToList().FindIndex(m => m.UUID == pick.UUID) + 1) % playlist.Count;
                _currentTrack = pick;
                return _currentTrack;
            }

            int start = _playlistPosition;
            bool reachedEnd = false;

            if (_currentTrack != null)
            {
                int currentIdx = -1;
                for (int i = 0; i < playlist.Count; i++)
                {
                    if (playlist[i]?.UUID == _currentTrack.UUID)
                    {
                        currentIdx = i;
                        break;
                    }
                }
                if (currentIdx >= 0)
                {
                    start = currentIdx + 1;
                    if (start >= playlist.Count)
                    {
                        if (RepeatMode == RepeatMode.None)
                        {
                            reachedEnd = true;
                        }
                        else
                        {
                            start = 0;
                        }
                    }
                }
            }

            if (reachedEnd)
            {
                _currentTrack = null;
                return null;
            }

            if (start >= playlist.Count)
            {
                start = 0;
            }

            int nextIdx = -1;
            for (int i = start; i < playlist.Count; i++)
            {
                var candidate = playlist[i];
                if (candidate != null && !candidate.IsExcluded)
                {
                    nextIdx = i;
                    break;
                }
            }

            if (nextIdx == -1 && RepeatMode == RepeatMode.All)
            {
                for (int i = 0; i < start; i++)
                {
                    var candidate = playlist[i];
                    if (candidate != null && !candidate.IsExcluded)
                    {
                        nextIdx = i;
                        break;
                    }
                }
            }

            if (nextIdx != -1)
            {
                _playlistPosition = (nextIdx + 1) % playlist.Count;
                _currentTrack = playlist[nextIdx];
                return _currentTrack;
            }

            _currentTrack = null;
            return null;
        }

        public void ImportFromPlaylist(IReadOnlyList<MusicInfo> songs, bool replace)
        {
            if (replace) _queue.Clear();
            InsertUnique(_queue, songs, _queue.Count);
        }

        public QueueSlotData Serialize() => new()
        {
            Id = Id,
            Name = Name,
            CurrentUuid = _currentTrack?.UUID,
            PlaylistSources = _playlistSources.Select(s => s.Serialize()).ToList(),
            SongUuids = _queue.Select(m => m.UUID).ToList(),
            Index = -1,
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
                _historyPosition = data.HistoryPosition,
                _playlistPosition = data.PlaylistPosition,
                Shuffle = data.Shuffle
            };
            if (Enum.TryParse<RepeatMode>(data.RepeatMode, out var rm)) slot.RepeatMode = rm;

            if (!string.IsNullOrEmpty(data.CurrentUuid))
                slot._currentTrack = registry.GetMusic(data.CurrentUuid);
            if (data.PlaylistSources != null)
            {
                foreach (var source in data.PlaylistSources)
                {
                    var songs = ResolveSerializedSongs(source.SongUuids, registry);
                    slot._playlistSources.Add(new PlaylistSource(source.Id, source.Name, songs));
                }
            }
            if (data.PlaylistUuids != null)
            {
                var songs = ResolveSerializedSongs(data.PlaylistUuids, registry);
                if (songs.Count > 0)
                    slot._playlistSources.Add(new PlaylistSource("legacy", "Legacy", songs));
            }
            if (data.SongUuids != null)
                foreach (var u in data.SongUuids) { var m = registry.GetMusic(u); if (m != null) slot._queue.Add(m); }
            if (data.HistoryUuids != null)
                foreach (var u in data.HistoryUuids) { var m = registry.GetMusic(u); if (m != null) slot._history.Add(m); }

            if (slot._currentTrack == null && data.Index >= 0 && data.Index < slot._queue.Count)
            {
                slot._currentTrack = slot._queue[data.Index];
                slot._queue.RemoveAt(data.Index);
            }

            return slot;
        }

        private IReadOnlyList<MusicInfo> GetPlaylist()
        {
            if (_playlistDirty) RebuildPlaylistCache();
            return _playlistCache;
        }

        private void RebuildPlaylistCache()
        {
            _playlistCache.Clear();
            var seen = new HashSet<string>();
            foreach (var source in _playlistSources)
            {
                foreach (var song in source.Songs)
                {
                    if (song == null || string.IsNullOrEmpty(song.UUID)) continue;
                    if (seen.Add(song.UUID)) _playlistCache.Add(song);
                }
            }
            _playlistDirty = false;
        }

        private static void InsertUnique(List<MusicInfo> target, IEnumerable<MusicInfo> songs, int index)
        {
            if (songs == null) return;
            index = Math.Clamp(index, 0, target.Count);
            foreach (var song in songs.Where(s => s != null && !string.IsNullOrEmpty(s.UUID)))
            {
                var existing = target.FindIndex(m => m?.UUID == song.UUID);
                if (existing >= 0)
                {
                    target.RemoveAt(existing);
                    if (existing < index) index--;
                }
                index = Math.Clamp(index, 0, target.Count);
                target.Insert(index++, song);
            }
        }

        private static bool RemoveAt(List<MusicInfo> target, int index)
        {
            if (index < 0 || index >= target.Count) return false;
            target.RemoveAt(index);
            return true;
        }

        private static bool RemoveByUuid(List<MusicInfo> target, string uuid)
        {
            var index = target.FindIndex(m => m?.UUID == uuid);
            return RemoveAt(target, index);
        }

        private static bool MoveInList(List<MusicInfo> target, int from, int to)
        {
            if (from < 0 || from >= target.Count || to < 0 || to >= target.Count) return false;
            var item = target[from];
            target.RemoveAt(from);
            target.Insert(to, item);
            return true;
        }

        private static List<MusicInfo> ResolveSerializedSongs(IEnumerable<string> uuids, IMusicRegistry registry)
        {
            var songs = new List<MusicInfo>();
            foreach (var u in uuids ?? Array.Empty<string>())
            {
                var m = registry.GetMusic(u);
                if (m != null) songs.Add(m);
            }
            return songs;
        }
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
}
