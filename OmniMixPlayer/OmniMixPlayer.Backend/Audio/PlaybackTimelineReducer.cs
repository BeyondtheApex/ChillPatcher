using System;
using System.Collections.Generic;
using System.Linq;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    public enum TimelineAdvanceReason
    {
        InitialPlay,
        ExplicitPlay,
        UserNext,
        UserPrevious,
        NaturalEnd,
        RepeatOne
    }

    public sealed record TimelineAdvanceResult(string CurrentUuid, TimelineAdvanceReason Reason);

    public static class PlaybackTimelineReducer
    {
        private const int MaxHistory = 50;
        private const int MaxForward = 50;

        public static PlaybackTimelineState Ensure(PlaybackTimelineState state)
        {
            state ??= new PlaybackTimelineState();
            state.Version = 2;
            if (string.IsNullOrWhiteSpace(state.CurrentUuid))
                state.CurrentSourceIndex = -1;
            if (state.SourceUuids.Count == 0)
                state.SourceCursor = -1;
            state.SourceCursor = ClampCursor(state.SourceCursor, state.SourceUuids.Count);
            state.CurrentSourceIndex = ClampCursor(state.CurrentSourceIndex, state.SourceUuids.Count);
            return state;
        }

        public static TimelineAdvanceResult PlayExplicit(PlaybackTimelineState state, string uuid)
        {
            Ensure(state);
            if (string.IsNullOrWhiteSpace(uuid))
                return EnsureCurrentOrTakeNext(state, null);

            PushHistory(state, state.CurrentUuid);
            state.NavForwardUuids.Clear();
            RemoveAll(state.ManualQueueUuids, uuid);
            RemoveAll(state.NavForwardUuids, uuid);
            state.CurrentUuid = uuid;
            state.CurrentSourceIndex = FindSourceIndex(state, uuid);
            if (state.CurrentSourceIndex >= 0)
                state.SourceCursor = state.CurrentSourceIndex;
            Touch(state);
            return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.ExplicitPlay);
        }

        public static TimelineAdvanceResult EnsureCurrentOrTakeNext(PlaybackTimelineState state, Random rng)
        {
            Ensure(state);
            if (!string.IsNullOrWhiteSpace(state.CurrentUuid))
                return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.InitialPlay);

            var next = TakeManual(state);
            if (next != null)
            {
                SetManualCurrent(state, next);
                Touch(state);
                return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.InitialPlay);
            }

            var idx = PickInitialSourceIndex(state, rng);
            if (idx < 0)
                return new TimelineAdvanceResult("", TimelineAdvanceReason.InitialPlay);

            SetSourceCurrent(state, idx, commitCursor: true);
            Touch(state);
            return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.InitialPlay);
        }

        public static TimelineAdvanceResult Next(PlaybackTimelineState state, Random rng)
        {
            Ensure(state);
            if (state.NavForwardUuids.Count > 0)
            {
                PushHistory(state, state.CurrentUuid);
                var uuid = state.NavForwardUuids[0];
                state.NavForwardUuids.RemoveAt(0);
                SetCurrentPreservingSourceCursor(state, uuid);
                Touch(state);
                return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.UserNext);
            }

            PushHistory(state, state.CurrentUuid);
            var manual = TakeManual(state);
            if (manual != null)
            {
                SetManualCurrent(state, manual);
                Touch(state);
                return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.UserNext);
            }

            var sourceIndex = PickNextSourceIndex(state, rng, commitBase: false);
            if (sourceIndex < 0)
            {
                ClearCurrent(state);
                Touch(state);
                return new TimelineAdvanceResult("", TimelineAdvanceReason.UserNext);
            }

            SetSourceCurrent(state, sourceIndex, commitCursor: false);
            Touch(state);
            return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.UserNext);
        }

        public static TimelineAdvanceResult Previous(PlaybackTimelineState state)
        {
            Ensure(state);
            if (state.HistoryUuids.Count == 0)
                return new TimelineAdvanceResult(state.CurrentUuid ?? "", TimelineAdvanceReason.UserPrevious);

            PushForward(state, state.CurrentUuid);
            var uuid = state.HistoryUuids[0];
            state.HistoryUuids.RemoveAt(0);
            SetCurrentPreservingSourceCursor(state, uuid);
            Touch(state);
            return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.UserPrevious);
        }

        public static TimelineAdvanceResult NaturalEnd(PlaybackTimelineState state, Random rng)
        {
            Ensure(state);
            if (state.RepeatMode == RepeatMode.One && !string.IsNullOrWhiteSpace(state.CurrentUuid))
            {
                Touch(state);
                return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.RepeatOne);
            }

            var completedSourceIndex = state.CurrentSourceIndex;
            PushHistory(state, state.CurrentUuid);
            state.NavForwardUuids.Clear();

            var manual = TakeManual(state);
            if (manual != null)
            {
                if (completedSourceIndex >= 0)
                    state.SourceCursor = completedSourceIndex;
                SetManualCurrent(state, manual);
                Touch(state);
                return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.NaturalEnd);
            }

            var sourceIndex = PickNextSourceIndex(state, rng, commitBase: true);
            if (sourceIndex < 0)
            {
                ClearCurrent(state);
                Touch(state);
                return new TimelineAdvanceResult("", TimelineAdvanceReason.NaturalEnd);
            }

            SetSourceCurrent(state, sourceIndex, commitCursor: true);
            Touch(state);
            return new TimelineAdvanceResult(state.CurrentUuid, TimelineAdvanceReason.NaturalEnd);
        }

        public static void ClearCurrent(PlaybackTimelineState state)
        {
            Ensure(state);
            state.CurrentUuid = "";
            state.CurrentSourceIndex = -1;
        }

        public static void AddToQueue(PlaybackTimelineState state, string uuid)
        {
            Ensure(state);
            if (!string.IsNullOrWhiteSpace(uuid))
                state.ManualQueueUuids.Add(uuid);
            state.NavForwardUuids.Clear();
            Touch(state);
        }

        public static void InsertIntoQueue(PlaybackTimelineState state, IEnumerable<string> uuids, int index)
        {
            Ensure(state);
            var list = uuids.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
            index = index < 0 || index > state.ManualQueueUuids.Count ? state.ManualQueueUuids.Count : index;
            foreach (var uuid in list)
                state.ManualQueueUuids.Insert(index++, uuid);
            state.NavForwardUuids.Clear();
            Touch(state);
        }

        public static void SetQueue(PlaybackTimelineState state, IEnumerable<string> uuids)
        {
            Ensure(state);
            state.ManualQueueUuids.Clear();
            state.ManualQueueUuids.AddRange(uuids.Where(u => !string.IsNullOrWhiteSpace(u)));
            state.NavForwardUuids.Clear();
            Touch(state);
        }

        public static void RemoveFromQueue(PlaybackTimelineState state, int index)
        {
            Ensure(state);
            if (index >= 0 && index < state.ManualQueueUuids.Count)
                state.ManualQueueUuids.RemoveAt(index);
            Touch(state);
        }

        public static void RemoveFromQueue(PlaybackTimelineState state, string uuid)
        {
            Ensure(state);
            RemoveAll(state.ManualQueueUuids, uuid);
            Touch(state);
        }

        public static void MoveInQueue(PlaybackTimelineState state, int from, int to)
        {
            Ensure(state);
            Move(state.ManualQueueUuids, from, to);
            Touch(state);
        }

        public static void ClearQueue(PlaybackTimelineState state)
        {
            Ensure(state);
            state.ManualQueueUuids.Clear();
            Touch(state);
        }

        public static void RemoveFromHistory(PlaybackTimelineState state, int index)
        {
            Ensure(state);
            if (index >= 0 && index < state.HistoryUuids.Count)
                state.HistoryUuids.RemoveAt(index);
            Touch(state);
        }

        public static void MoveInHistory(PlaybackTimelineState state, int from, int to)
        {
            Ensure(state);
            Move(state.HistoryUuids, from, to);
            Touch(state);
        }

        public static void ClearHistory(PlaybackTimelineState state)
        {
            Ensure(state);
            state.HistoryUuids.Clear();
            state.NavForwardUuids.Clear();
            Touch(state);
        }

        public static void SetShuffle(PlaybackTimelineState state, bool enabled)
        {
            Ensure(state);
            state.Shuffle = enabled;
            state.NavForwardUuids.Clear();
            Touch(state);
        }

        public static void SetRepeatMode(PlaybackTimelineState state, RepeatMode mode)
        {
            Ensure(state);
            state.RepeatMode = mode;
            Touch(state);
        }

        public static void SetSources(PlaybackTimelineState state, IEnumerable<PlaylistSourceState> sources, IEnumerable<string> sourceUuids)
        {
            Ensure(state);
            state.PlaylistSources.Clear();
            state.PlaylistSources.AddRange(sources);
            state.SourceUuids.Clear();
            state.SourceUuids.AddRange(sourceUuids.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct());
            state.SourceCursor = ClampCursor(state.SourceCursor, state.SourceUuids.Count);
            state.CurrentSourceIndex = string.IsNullOrWhiteSpace(state.CurrentUuid)
                ? -1
                : FindSourceIndex(state, state.CurrentUuid);
            Touch(state);
        }

        private static int PickInitialSourceIndex(PlaybackTimelineState state, Random rng)
        {
            if (state.SourceUuids.Count == 0) return -1;
            if (state.Shuffle) return rng?.Next(state.SourceUuids.Count) ?? 0;
            var next = state.SourceCursor >= 0 ? state.SourceCursor : 0;
            return next >= state.SourceUuids.Count ? 0 : next;
        }

        private static int PickNextSourceIndex(PlaybackTimelineState state, Random rng, bool commitBase)
        {
            if (state.SourceUuids.Count == 0) return -1;
            if (state.Shuffle)
            {
                if (state.SourceUuids.Count == 1) return 0;
                var current = state.CurrentUuid;
                for (var i = 0; i < 8; i++)
                {
                    var idx = rng?.Next(state.SourceUuids.Count) ?? 0;
                    if (state.SourceUuids[idx] != current)
                        return idx;
                }
                return Enumerable.Range(0, state.SourceUuids.Count).FirstOrDefault(i => state.SourceUuids[i] != current);
            }

            var baseIndex = state.CurrentSourceIndex >= 0 ? state.CurrentSourceIndex : state.SourceCursor;
            if (commitBase && state.CurrentSourceIndex >= 0)
                state.SourceCursor = state.CurrentSourceIndex;

            var next = baseIndex + 1;
            if (next < state.SourceUuids.Count)
                return next;
            return state.RepeatMode == RepeatMode.All ? 0 : -1;
        }

        private static string TakeManual(PlaybackTimelineState state)
        {
            if (state.ManualQueueUuids.Count == 0) return null;
            var uuid = state.ManualQueueUuids[0];
            state.ManualQueueUuids.RemoveAt(0);
            return uuid;
        }

        private static void SetSourceCurrent(PlaybackTimelineState state, int index, bool commitCursor)
        {
            state.CurrentUuid = index >= 0 && index < state.SourceUuids.Count ? state.SourceUuids[index] : "";
            state.CurrentSourceIndex = string.IsNullOrWhiteSpace(state.CurrentUuid) ? -1 : index;
            if (commitCursor)
                state.SourceCursor = state.CurrentSourceIndex;
        }

        private static void SetManualCurrent(PlaybackTimelineState state, string uuid)
        {
            state.CurrentUuid = uuid ?? "";
            state.CurrentSourceIndex = -1;
        }

        private static void SetCurrentPreservingSourceCursor(PlaybackTimelineState state, string uuid)
        {
            state.CurrentUuid = uuid ?? "";
            state.CurrentSourceIndex = FindSourceIndex(state, state.CurrentUuid);
        }

        private static void PushHistory(PlaybackTimelineState state, string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return;
            state.HistoryUuids.Insert(0, uuid);
            Trim(state.HistoryUuids, MaxHistory);
        }

        private static void PushForward(PlaybackTimelineState state, string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return;
            state.NavForwardUuids.Insert(0, uuid);
            Trim(state.NavForwardUuids, MaxForward);
        }

        private static int FindSourceIndex(PlaybackTimelineState state, string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return -1;
            for (var i = 0; i < state.SourceUuids.Count; i++)
            {
                if (state.SourceUuids[i] == uuid) return i;
            }
            return -1;
        }

        private static int ClampCursor(int value, int count)
        {
            if (count == 0) return -1;
            if (value < -1) return -1;
            return value >= count ? count - 1 : value;
        }

        private static void Move(Google.Protobuf.Collections.RepeatedField<string> list, int from, int to)
        {
            if (from < 0 || from >= list.Count || to < 0 || to >= list.Count || from == to) return;
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }

        private static void RemoveAll(Google.Protobuf.Collections.RepeatedField<string> list, string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return;
            while (list.Remove(uuid)) { }
        }

        private static void Trim(Google.Protobuf.Collections.RepeatedField<string> list, int max)
        {
            while (list.Count > max)
                list.RemoveAt(list.Count - 1);
        }

        private static void Touch(PlaybackTimelineState state)
        {
            state.Version = 2;
            state.Revision++;
        }
    }
}
