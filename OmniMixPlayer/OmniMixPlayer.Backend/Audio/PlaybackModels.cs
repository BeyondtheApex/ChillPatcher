using System.Collections.Generic;

namespace OmniMixPlayer.Backend.Audio
{
    public class PlaylistSourceData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> SongUuids { get; set; } = new();
    }

    public class QueueSlotData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CurrentUuid { get; set; }
        public List<PlaylistSourceData> PlaylistSources { get; set; } = new();
        public List<string> PlaylistUuids { get; set; } = new();
        public List<string> SongUuids { get; set; } = new();
        public int Index { get; set; } = -1;
        public List<string> HistoryUuids { get; set; } = new();
        public int HistoryPosition { get; set; } = -1;
        public int PlaylistPosition { get; set; }
        public bool Shuffle { get; set; }
        public string RepeatMode { get; set; } = "none";
    }

    public class PlaybackStateData
    {
        public string Id { get; set; } // Map this as primary key for LiteDB (InstanceId)
        public string ActiveQueueId { get; set; }
        public float Volume { get; set; } = 1.0f;
        public List<QueueSlotData> Queues { get; set; } = new();
        public EqualizerState Equalizer { get; set; }
        public float TargetLatency { get; set; } = 0.1f;
    }
}
