using System;
using System.Collections.Generic;
using System.Linq;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public sealed class PlaybackTimelineStore
    {
        private readonly object _lock = new();
        private readonly InstanceRegistry _registry;
        private readonly ILibraryRegistry _library;
        private readonly IEventBus _eventBus;
        private readonly Random _rng = new();

        public event Action<string> OnTimelineChanged;

        public PlaybackTimelineStore(InstanceRegistry registry, ILibraryRegistry library, IEventBus eventBus)
        {
            _registry = registry;
            _library = library;
            _eventBus = eventBus;
        }

        public PlaybackTimelineState Get(string instanceId)
        {
            lock (_lock)
            {
                return Clone(GetProfileTimeline(instanceId));
            }
        }

        public Track GetCurrentTrack(string instanceId)
        {
            var timeline = Get(instanceId);
            return ResolveTrack(timeline.CurrentUuid);
        }

        public int GetManualQueueCount(string instanceId) => Get(instanceId).ManualQueueUuids.Count;

        public TimelineAdvanceResult PlayExplicit(string instanceId, string uuid)
        {
            return Mutate(instanceId, timeline =>
            {
                if (!string.IsNullOrWhiteSpace(uuid) && !IsPlayable(uuid))
                    return new TimelineAdvanceResult(timeline.CurrentUuid ?? "", TimelineAdvanceReason.ExplicitPlay);
                return PlaybackTimelineReducer.PlayExplicit(timeline, uuid);
            });
        }

        public TimelineAdvanceResult EnsureCurrentOrTakeNext(string instanceId)
        {
            return Mutate(instanceId, timeline => PlaybackTimelineReducer.EnsureCurrentOrTakeNext(timeline, _rng));
        }

        public TimelineAdvanceResult Next(string instanceId)
        {
            return Mutate(instanceId, timeline => PlaybackTimelineReducer.Next(timeline, _rng));
        }

        public TimelineAdvanceResult Previous(string instanceId)
        {
            return Mutate(instanceId, PlaybackTimelineReducer.Previous);
        }

        public TimelineAdvanceResult NaturalEnd(string instanceId)
        {
            return Mutate(instanceId, timeline => PlaybackTimelineReducer.NaturalEnd(timeline, _rng));
        }

        public void ClearCurrent(string instanceId)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.ClearCurrent(timeline);
                return null;
            });
        }

        public void SetShuffle(string instanceId, bool enabled)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.SetShuffle(timeline, enabled);
                return null;
            });
        }

        public void SetRepeatMode(string instanceId, RepeatMode mode)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.SetRepeatMode(timeline, mode);
                return null;
            });
        }

        public void AddToQueue(string instanceId, string uuid)
        {
            Mutate(instanceId, timeline =>
            {
                if (IsPlayable(uuid))
                    PlaybackTimelineReducer.AddToQueue(timeline, uuid);
                return null;
            });
        }

        public void InsertIntoQueue(string instanceId, IEnumerable<string> uuids, int index)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.InsertIntoQueue(timeline, FilterPlayable(uuids), index);
                return null;
            });
        }

        public void SetQueue(string instanceId, IEnumerable<string> uuids)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.SetQueue(timeline, FilterPlayable(uuids));
                return null;
            });
        }

        public void RemoveFromQueue(string instanceId, int index)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.RemoveFromQueue(timeline, index);
                return null;
            });
        }

        public void RemoveFromQueue(string instanceId, string uuid)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.RemoveFromQueue(timeline, uuid);
                return null;
            });
        }

        public void MoveInQueue(string instanceId, int from, int to)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.MoveInQueue(timeline, from, to);
                return null;
            });
        }

        public void ClearQueue(string instanceId)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.ClearQueue(timeline);
                return null;
            });
        }

        public void RemoveFromHistory(string instanceId, int index)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.RemoveFromHistory(timeline, index);
                return null;
            });
        }

        public void MoveInHistory(string instanceId, int from, int to)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.MoveInHistory(timeline, from, to);
                return null;
            });
        }

        public void ClearHistory(string instanceId)
        {
            Mutate(instanceId, timeline =>
            {
                PlaybackTimelineReducer.ClearHistory(timeline);
                return null;
            });
        }

        public void SetPlaylistSources(string instanceId, IEnumerable<PlaylistSourceRequest> sources)
        {
            Mutate(instanceId, timeline =>
            {
                var states = sources.Select(ToState).ToList();
                var sourceUuids = ExpandSources(states);
                PlaybackTimelineReducer.SetSources(timeline, states, sourceUuids);
                return null;
            });
        }

        public IReadOnlyList<PlaylistSourceInfo> GetPlaylistSources(string instanceId)
        {
            var timeline = Get(instanceId);
            return timeline.PlaylistSources.Select(s => new PlaylistSourceInfo
            {
                Id = s.Id,
                Name = s.Name,
                Kind = s.Kind,
                RefId = s.RefId ?? "",
                SongCount = ResolveSource(s).Count
            }).ToList();
        }

        private TimelineAdvanceResult Mutate(string instanceId, Func<PlaybackTimelineState, TimelineAdvanceResult> mutation)
        {
            TimelineAdvanceResult result;
            PlaybackTimelineState changed;
            lock (_lock)
            {
                var profile = _registry.GetOrDefault(instanceId);
                var timeline = PlaybackTimelineReducer.Ensure(profile.PlaybackTimeline);
                result = mutation(timeline);
                profile.PlaybackTimeline = timeline;
                _registry.SavePlaybackTimeline(instanceId, timeline);
                changed = timeline.Clone();
            }

            _eventBus?.Publish(new PlaybackTimelineChangedEvent
            {
                InstanceId = instanceId,
                CurrentUuid = changed.CurrentUuid ?? "",
                ManualQueueLength = changed.ManualQueueUuids.Count,
                HistoryLength = changed.HistoryUuids.Count,
                SourceLength = changed.SourceUuids.Count,
                Revision = changed.Revision
            });
            OnTimelineChanged?.Invoke(instanceId);
            return result;
        }

        private PlaybackTimelineState GetProfileTimeline(string instanceId)
        {
            var profile = _registry.GetOrDefault(instanceId);
            var timeline = PlaybackTimelineReducer.Ensure(profile.PlaybackTimeline);
            if (profile.PlaybackTimeline == null || profile.PlaybackTimeline.Version != 2)
            {
                profile.PlaybackTimeline = timeline;
                _registry.SavePlaybackTimeline(instanceId, timeline);
            }
            return timeline;
        }

        private static PlaybackTimelineState Clone(PlaybackTimelineState timeline) => timeline.Clone();

        private bool IsPlayable(string uuid)
        {
            var track = ResolveTrack(uuid);
            return track != null && !track.IsExcluded;
        }

        private IEnumerable<string> FilterPlayable(IEnumerable<string> uuids)
        {
            return (uuids ?? Enumerable.Empty<string>())
                .Where(IsPlayable)
                .ToList();
        }

        private Track ResolveTrack(string uuid)
        {
            return string.IsNullOrWhiteSpace(uuid) ? null : _library.GetTrack(uuid);
        }

        private PlaylistSourceState ToState(PlaylistSourceRequest source)
        {
            var state = new PlaylistSourceState
            {
                Id = source?.id ?? "",
                Name = source?.name ?? "",
                Kind = source?.kind ?? PlaylistSourceKind.Unspecified,
                RefId = string.IsNullOrWhiteSpace(source?.refId)
                    ? InferRefId(source?.id, source?.kind ?? PlaylistSourceKind.Unspecified)
                    : source.refId
            };
            state.Uuids.AddRange(source?.uuids?.Where(IsPlayable) ?? Array.Empty<string>());
            return state;
        }

        private IReadOnlyList<string> ExpandSources(IEnumerable<PlaylistSourceState> sources)
        {
            return sources.SelectMany(ResolveSource)
                .Select(t => t.Uuid)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList();
        }

        private IReadOnlyList<Track> ResolveSource(PlaylistSourceState source)
        {
            IReadOnlyList<Track> tracks = null;
            if (source.Kind != PlaylistSourceKind.Unspecified && !string.IsNullOrWhiteSpace(source.RefId))
                tracks = ResolveByKind(source.Kind, source.RefId);
            if (tracks == null || tracks.Count == 0)
                tracks = source.Uuids.Select(ResolveTrack).Where(t => t != null).ToList();

            return tracks.Where(t => t != null && !t.IsExcluded)
                .GroupBy(t => t.Uuid)
                .Select(g => g.First())
                .ToList();
        }

        private IReadOnlyList<Track> ResolveByKind(PlaylistSourceKind kind, string refId)
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
                    var track = ResolveTrack(refId);
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
    }
}
