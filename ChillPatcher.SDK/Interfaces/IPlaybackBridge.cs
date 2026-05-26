using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChillPatcher.SDK.Interfaces
{
    public interface IPlaybackBridge
    {
        #region Events

        event EventHandler<TrackChangedEventArgs> OnTrackChanged;
        event EventHandler<StateChangedEventArgs> OnStateChanged;
        event EventHandler<PositionEventArgs> OnPosition;
        event EventHandler<QueueChangedEventArgs> OnQueueChanged;
        event EventHandler<PlaylistUpdatedEventArgs> OnPlaylistUpdated;
        event EventHandler<ModuleChangedEventArgs> OnModuleChanged;
        event EventHandler<ErrorEventArgs> OnError;
        event EventHandler<LyricFetchedEventArgs> OnLyricFetched;
        event EventHandler<LyricPositionEventArgs> OnLyricPosition;

        #endregion

        #region Playback Control

        Task Play(string uuid);
        Task Pause();
        Task Resume();
        Task Toggle();
        Task Next();
        Task Prev();
        Task Seek(float position);
        Task SetVolume(float volume);
        Task SetShuffle(bool enabled);
        Task SetRepeat(string mode);

        #endregion

        #region Playlist Query

        Task<PlaylistResponse> GetPlaylist();
        Task<List<TagResponse>> GetTags();
        Task<List<AlbumResponse>> GetAlbums(string tagId = null);
        Task<List<SongResponse>> GetSongs(string albumId = null, string tagId = null);
        Task<SongResponse> GetSong(string uuid);
        Task<StatusResponse> GetStatus();

        #endregion

        #region Queue Management

        Task<List<QueueItemResponse>> GetQueue();
        Task AddToQueue(string uuid);
        Task RemoveFromQueue(int index);
        Task MoveInQueue(int from, int to);
        Task ClearQueue();

        #endregion

        #region Cover & Lyrics

        Task<(byte[] data, string mimeType)> GetTrackCover(string uuid);
        Task<LyricData> GetLyric(string uuid);

        #endregion

        #region Modules

        Task<List<ModuleResponse>> GetModules();
        Task SetModuleEnabled(string id, bool enabled);
        Task<VersionResponse> GetVersion();

        #endregion

        #region Connection

        Task ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }

        #endregion
    }

    #region Event Args

    public class TrackChangedEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string AlbumId { get; set; }
        public float Duration { get; set; }
        public string ModuleId { get; set; }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public bool IsPlaying { get; set; }
        public float Position { get; set; }
        public float Volume { get; set; }
        public string RepeatMode { get; set; }
        public bool Shuffle { get; set; }
    }

    public class PositionEventArgs : EventArgs
    {
        public float Position { get; set; }
    }

    public class QueueChangedEventArgs : EventArgs
    {
        public string ChangeType { get; set; }
        public int QueueLength { get; set; }
    }

    public class PlaylistUpdatedEventArgs : EventArgs
    {
        public string TagId { get; set; }
        public string UpdateType { get; set; }
        public int ChangedCount { get; set; }
    }

    public class ModuleChangedEventArgs : EventArgs
    {
        public string ModuleId { get; set; }
        public bool Enabled { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class LyricFetchedEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public string Lrc { get; set; }
        public string Tlyric { get; set; }
        public string Rlyric { get; set; }
    }

    public class LyricPositionEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public int LineIndex { get; set; }
        public float TimeMs { get; set; }
    }

    #endregion

    #region Response Models

    public class PlaylistResponse
    {
        public List<TagResponse> Tags { get; set; }
        public List<AlbumResponse> Albums { get; set; }
        public List<SongResponse> Songs { get; set; }
    }

    public class TagResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ModuleId { get; set; }
    }

    public class AlbumResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TagId { get; set; }
        public string CoverUrl { get; set; }
        public int SongCount { get; set; }
    }

    public class SongResponse
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string AlbumId { get; set; }
        public float Duration { get; set; }
        public string CoverUrl { get; set; }
        public string ModuleId { get; set; }
    }

    public class QueueItemResponse
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public float Duration { get; set; }
        public int Index { get; set; }
    }

    public class StatusResponse
    {
        public bool IsPlaying { get; set; }
        public float Position { get; set; }
        public float Volume { get; set; }
        public bool Shuffle { get; set; }
        public string RepeatMode { get; set; }
        public int QueueLength { get; set; }
        public int QueueIndex { get; set; }
    }

    public class ModuleResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public int Priority { get; set; }
    }

    public class VersionResponse
    {
        public string Version { get; set; }
        public string Name { get; set; }
    }

    public class LyricData
    {
        public string Uuid { get; set; }
        public string Lrc { get; set; }
        public string Tlyric { get; set; }
        public string Rlyric { get; set; }
    }

    #endregion
}