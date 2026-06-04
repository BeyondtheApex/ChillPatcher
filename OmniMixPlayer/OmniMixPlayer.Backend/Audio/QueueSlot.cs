using System;
using System.Collections.Generic;
using System.Linq;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public sealed class PlaylistSourceRequest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string[] uuids { get; set; } = Array.Empty<string>();
        public PlaylistSourceKind kind { get; set; } = PlaylistSourceKind.Unspecified;
        public string refId { get; set; }
    }

    public sealed class PlaylistSourceInfo
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public int SongCount { get; init; }
        public PlaylistSourceKind Kind { get; init; }
        public string RefId { get; init; }
    }

    internal sealed class PlaylistSource
    {
        public string Id { get; }
        public string Name { get; }
        public PlaylistSourceKind Kind { get; }
        public string RefId { get; }
        public List<string> Uuids { get; }

        public PlaylistSource(string id, string name, PlaylistSourceKind kind, string refId, IEnumerable<string> uuids)
        {
            Id = id;
            Name = name;
            Kind = kind;
            RefId = refId;
            Uuids = uuids?.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList() ?? new List<string>();
        }

        public PlaylistSourceInfo ToInfo(Func<PlaylistSource, int> countSongs) => new()
        {
            Id = Id,
            Name = Name,
            SongCount = countSongs(this),
            Kind = Kind,
            RefId = RefId ?? ""
        };
    }

    internal class QueueSlot
    {
        private readonly List<PlaylistSource> _playlistSources = new();
        private readonly List<Track> _playlistCache = new();
        private readonly List<Track> _queue = new();
        private readonly List<Track> _history = new();
        private Track _currentTrack;
        private int _historyPosition = -1;
        private int _playlistPosition;

        public string Id { get; }
        public string Name { get; set; }
        public bool Shuffle { get; set; }
        public SDK.Protos.Models.RepeatMode RepeatMode { get; set; } = SDK.Protos.Models.RepeatMode.None;

        public Track CurrentTrack => _currentTrack;
        public IReadOnlyList<Track> Queue => _queue;
        public int QueueCount => _queue.Count;
        public IReadOnlyList<Track> History => _history;
        public int HistoryCount => _history.Count;
        public IReadOnlyList<PlaylistSourceInfo> PlaylistSources => _playlistSources.Select(s => s.ToInfo(src => ResolveSource(src).Count)).ToList();
        public IReadOnlyList<PlaylistSourceRequest> PlaylistSourceSpecs => _playlistSources
            .Select(s => new PlaylistSourceRequest { id = s.Id, name = s.Name, kind = s.Kind, refId = s.RefId, uuids = s.Uuids.ToArray() })
            .ToList();
        public IReadOnlyList<Track> Playlist => GetPlaylist();
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

        public QueueSlot(string id, string name = "")
        {
            Id = id;
            Name = name;
        }

        public Func<PlaylistSourceKind, string, IReadOnlyList<Track>> SourceResolver { get; set; }

        private List<Track> GetPlaylist()
        {
            _playlistCache.Clear();
            foreach (var source in _playlistSources)
                _playlistCache.AddRange(ResolveSource(source));
            return _playlistCache;
        }

        private IReadOnlyList<Track> ResolveSource(PlaylistSource source)
        {
            IReadOnlyList<Track> tracks = null;
            if (source.Kind != PlaylistSourceKind.Unspecified && !string.IsNullOrWhiteSpace(source.RefId))
                tracks = SourceResolver?.Invoke(source.Kind, source.RefId);
            if (tracks == null || tracks.Count == 0)
                tracks = source.Uuids.Select(u => SourceResolver?.Invoke(PlaylistSourceKind.Track, u)?.FirstOrDefault()).Where(t => t != null).ToList();

            return tracks.Where(t => t != null && !t.IsExcluded).GroupBy(t => t.Uuid).Select(g => g.First()).ToList();
        }

        public void SetPlaylistSources(IEnumerable<PlaylistSource> sources)
        {
            _playlistSources.Clear();
            _playlistSources.AddRange(sources);
        }

        public Track DequeueNext(System.Random rng)
        {
            if (_queue.Count > 0)
            {
                var track = _queue[0];
                _queue.RemoveAt(0);
                return track;
            }

            var playlist = GetPlaylist();
            if (playlist.Count == 0) return null;

            Track next;
            if (Shuffle)
            {
                var idx = rng.Next(playlist.Count);
                next = playlist[idx];
            }
            else
            {
                if (_playlistPosition >= playlist.Count)
                    _playlistPosition = 0;
                next = playlist[_playlistPosition];
                _playlistPosition++;
            }
            return next;
        }

        public void AddToHistory(Track track)
        {
            _history.Insert(0, track);
            _historyPosition = -1;
            // Keep max 50
            while (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
        }

        public bool NavigateHistory(int direction)
        {
            if (direction < 0)
            {
                if (!CanGoPrevious) return false;
                _historyPosition = _historyPosition < 0 ? 1 : _historyPosition + 1;
            }
            else
            {
                if (_historyPosition <= 0) { _historyPosition = -1; return false; }
                _historyPosition--;
            }
            return _historyPosition >= 0 && _historyPosition < _history.Count;
        }

        public Track GetHistoryTrack()
        {
            return _historyPosition >= 0 && _historyPosition < _history.Count ? _history[_historyPosition] : null;
        }

        // ── Queue manipulation ──

        public void AddToQueue(Track track) { _queue.Add(track); }
        public void InsertIntoQueue(IEnumerable<Track> tracks, int index)
        {
            if (index < 0 || index > _queue.Count) index = _queue.Count;
            _queue.InsertRange(index, tracks);
        }
        public void SetQueue(IEnumerable<Track> tracks) { _queue.Clear(); _queue.AddRange(tracks); }
        public void SetHistory(IEnumerable<Track> tracks)
        {
            _history.Clear();
            _history.AddRange(tracks.Where(t => t != null).Take(50));
            _historyPosition = -1;
        }
        public void RemoveFromQueue(int index) { if (index >= 0 && index < _queue.Count) _queue.RemoveAt(index); }
        public void RemoveFromQueueByUuid(string uuid) { _queue.RemoveAll(t => t.Uuid == uuid); }
        public void MoveInQueue(int from, int to)
        {
            if (from < 0 || from >= _queue.Count || to < 0 || to >= _queue.Count) return;
            var item = _queue[from]; _queue.RemoveAt(from); _queue.Insert(to, item);
        }
        public void RemoveFromHistory(int index)
        {
            if (index < 0 || index >= _history.Count) return;
            _history.RemoveAt(index);
            if (_historyPosition >= _history.Count) _historyPosition = _history.Count - 1;
        }
        public void MoveInHistory(int from, int to)
        {
            if (from < 0 || from >= _history.Count || to < 0 || to >= _history.Count) return;
            var item = _history[from]; _history.RemoveAt(from); _history.Insert(to, item);
            if (_historyPosition == from) _historyPosition = to;
        }
        public void ClearQueue() { _queue.Clear(); }
        public void ClearHistory() { _history.Clear(); _historyPosition = -1; }

        /// <summary>Set the currently playing track. Must be called on PlayTrack.</summary>
        public void SetCurrentTrack(Track track) { _currentTrack = track; }
        public void ClearCurrentTrack() { _currentTrack = null; }
    }
}
