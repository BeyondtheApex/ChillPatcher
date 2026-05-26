using System;
using System.Collections.Generic;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.SDK.Interfaces
{
    public enum RepeatMode
    {
        None = 0,
        One = 1,
        All = 2
    }

    public class QueueChangedEventArgs : EventArgs
    {
        public QueueChangeType ChangeType { get; set; }
        public int QueueLength { get; set; }
        public string ChangedUuid { get; set; }
    }

    public enum QueueChangeType
    {
        Enqueued,
        Removed,
        Moved,
        Cleared,
        PlaybackStarted
    }

    public interface IPlayQueue
    {
        MusicInfo CurrentTrack { get; }
        bool IsPlaying { get; }
        float Position { get; }
        float Volume { get; set; }
        bool Shuffle { get; set; }
        RepeatMode RepeatMode { get; set; }

        IReadOnlyList<MusicInfo> Queue { get; }
        int QueueCount { get; }
        IReadOnlyList<MusicInfo> History { get; }
        int HistoryCount { get; }
        int PlaylistPosition { get; }

        bool CanGoPrevious { get; }
        bool CanGoNext { get; }

        void Play(string uuid = null);
        void Pause();
        void Resume();
        void Toggle();
        void Next();
        void Prev();
        void Seek(float position);
        void SetVolume(float volume);
        void SetShuffle(bool enabled);
        void SetRepeatMode(RepeatMode mode);

        void AddToQueue(string uuid);
        void AddToQueueRange(IEnumerable<string> uuids);
        void RemoveFromQueue(int index);
        void MoveInQueue(int fromIndex, int toIndex);
        void ClearQueue();
        void ClearHistory();

        void ImportFromPlaylist(IReadOnlyList<MusicInfo> songs, bool replace = true);

        bool IsFavorite(string uuid);
        void SetFavorite(string uuid, bool isFavorite);
        bool IsExcluded(string uuid);
        void SetExcluded(string uuid, bool isExcluded);

        event EventHandler<QueueChangedEventArgs> OnQueueChanged;
        event EventHandler<MusicInfo> OnTrackChanged;
        event EventHandler OnStateChanged;
        event EventHandler<float> OnPositionChanged;
    }
}
