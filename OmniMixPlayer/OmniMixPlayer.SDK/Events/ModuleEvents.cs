using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.SDK.Events
{
    /// <summary>
    /// 模块事件基础接口
    /// </summary>
    public interface IModuleEvent
    {
        long Timestamp { get; }
        string SourceModuleId { get; set; }
    }

    /// <summary>
    /// 事件基类
    /// </summary>
    public abstract class ModuleEventBase : IModuleEvent
    {
        public long Timestamp { get; } = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string SourceModuleId { get; set; }
    }

    #region 播放事件

    public class PlayStartedEvent : ModuleEventBase
    {
        public Track Music { get; set; }
        public PlaySource Source { get; set; }
    }

    public class PlayEndedEvent : ModuleEventBase
    {
        public Track Music { get; set; }
        public PlayEndReason Reason { get; set; }
        public float PlayedDuration { get; set; }
    }

    public class MusicResourcesReleasedEvent : ModuleEventBase
    {
        public Track Music { get; set; }
    }

    public class PlayPausedEvent : ModuleEventBase
    {
        public Track Music { get; set; }
        public bool IsPaused { get; set; }
    }

    public class PlayProgressEvent : ModuleEventBase
    {
        public Track Music { get; set; }
        public float CurrentTime { get; set; }
        public float TotalTime { get; set; }
        public float Progress { get; set; }
    }

    public enum PlaySource
    {
        UserClick,
        Queue,
        Shuffle,
        AutoNext,
        Previous
    }

    public enum PlayEndReason
    {
        Completed,
        Skipped,
        Stopped,
        Failed,
        Replaced
    }

    #endregion

    #region Tag 和专辑事件

    public class TagChangedEvent : ModuleEventBase
    {
        public string OldTagId { get; set; }
        public string NewTagId { get; set; }
        public Tag Tag { get; set; }
    }

    public class AlbumChangedEvent : ModuleEventBase
    {
        public string OldAlbumId { get; set; }
        public string NewAlbumId { get; set; }
        public Album Album { get; set; }
    }

    #endregion

    #region 歌单事件

    public class PlaylistUpdatedEvent : ModuleEventBase
    {
        public string SourceRefId { get; set; }
        public PlaylistUpdateType UpdateType { get; set; }
        public int ChangedCount { get; set; }
    }

    public enum PlaylistUpdateType
    {
        FullRefresh,
        Added,
        SongAdded = Added,
        Removed,
        SongRemoved = Removed,
        Reordered
    }

    public class PlaylistOrderChangedEvent : ModuleEventBase
    {
        public string TagId { get; set; }
        public PlaylistUpdateType UpdateType { get; set; }
        public string[] AffectedUUIDs { get; set; }
        public string[] AffectedSongUUIDs
        {
            get => AffectedUUIDs;
            set => AffectedUUIDs = value;
        }
        public string ModuleId { get; set; }
    }

    #endregion

    #region 收藏和排除事件

    public class FavoriteChangedEvent : ModuleEventBase
    {
        public string UUID { get; set; }
        public bool IsFavorite { get; set; }
        public Track Music { get; set; }
        public string ModuleId { get; set; }
    }

    public class ExcludeChangedEvent : ModuleEventBase
    {
        public string UUID { get; set; }
        public bool IsExcluded { get; set; }
        public Track Music { get; set; }
        public string ModuleId { get; set; }
    }

    #endregion

    #region 模块生命周期事件

    public class ModuleLoadedEvent : ModuleEventBase
    {
        public string ModuleId { get; set; }
        public string DisplayName { get; set; }
    }

    public class ModuleUnloadedEvent : ModuleEventBase
    {
        public string ModuleId { get; set; }
    }

    public class AllModulesLoadedEvent : ModuleEventBase
    {
        public int ModuleCount { get; set; }
    }

    #endregion

    #region 队列事件

    public class QueueChangedEvent : ModuleEventBase
    {
        public QueueChangeType ChangeType { get; set; }
        public int QueueLength { get; set; }
    }

    public enum QueueChangeType
    {
        Added,
        Removed,
        Cleared,
        Reordered
    }

    #endregion

    #region 增长列表事件

    public class GrowableListBottomOutEvent : ModuleEventBase
    {
        public string TagId { get; set; }
        public Tag TagInfo { get; set; }
        public int CurrentSongCount { get; set; }
        public System.Action<int> ReportLoaded { get; set; }
    }

    public class GrowableListLoadedEvent : ModuleEventBase
    {
        public string TagId { get; set; }
        public int LoadedCount { get; set; }
        public bool HasMore { get; set; }
    }

    #endregion

    #region 封面事件

    public class CoverInvalidatedEvent : ModuleEventBase
    {
        public string MusicUuid { get; set; }
        public string AlbumId { get; set; }
        public string Reason { get; set; }
    }

    #endregion

    #region Seek 事件

    public class PlaySeekEvent : ModuleEventBase
    {
        public Track Music { get; set; }
        public float Progress { get; set; }
        public float TargetTime { get; set; }
        public bool IsPending { get; set; }
        public bool IsCompleted { get; set; }
    }

    #endregion

    #region 歌词事件

    public class LyricFetchedEvent : ModuleEventBase
    {
        public string Uuid { get; set; }
        public string Lrc { get; set; }
        public string Tlyric { get; set; }
        public string Rlyric { get; set; }
        public string ModuleId { get; set; }
    }

    public class LyricPositionEvent : ModuleEventBase
    {
        public string Uuid { get; set; }
        public int LineIndex { get; set; }
        public float TimeMs { get; set; }
    }

    #endregion

    #region 错误事件

    public class ErrorEvent : ModuleEventBase
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string ModuleId { get; set; }
    }

    #endregion

    #region 控制请求事件 (模块 → 主机)

    public class PlayRequestedEvent : ModuleEventBase
    {
        public string MusicUuid { get; set; }
    }

    public class PauseRequestedEvent : ModuleEventBase { }

    public class ResumeRequestedEvent : ModuleEventBase { }

    public class StopRequestedEvent : ModuleEventBase { }

    public class TogglePlayRequestedEvent : ModuleEventBase { }

    public class NextTrackRequestedEvent : ModuleEventBase { }

    public class PreviousTrackRequestedEvent : ModuleEventBase { }

    public class SeekRequestedEvent : ModuleEventBase
    {
        public float PositionSeconds { get; set; }
    }

    public class VolumeChangeRequestedEvent : ModuleEventBase
    {
        public float Volume { get; set; }
    }

    public class ToggleShuffleRequestedEvent : ModuleEventBase
    {
        public bool Enabled { get; set; }
    }

    public class SetRepeatModeRequestedEvent : ModuleEventBase
    {
        public Protos.Models.RepeatMode Mode { get; set; }
    }

    #endregion
}

