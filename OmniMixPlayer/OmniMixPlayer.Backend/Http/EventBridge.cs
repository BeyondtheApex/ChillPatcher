using System;
using System.Collections.Generic;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.Backend.ModuleSystem;
using OmniMixPlayer.SDK.Events;

namespace OmniMixPlayer.Backend.Http
{
    public class EventBridge : IDisposable
    {
        private readonly ApiServer _apiServer;
        private readonly PlaybackController _playback;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly Action<SDK.Models.MusicInfo> _onTrackChanged;
        private readonly Action<int> _onStateChanged;
        private readonly Action<float> _onPositionChanged;
        private readonly Action _onQueueChanged;
        private bool _disposed;

        public EventBridge(ApiServer apiServer, PlaybackController playback)
        {
            _apiServer = apiServer;
            _playback = playback;

            _onTrackChanged = OnTrackChanged;
            _onStateChanged = OnStateChanged;
            _onPositionChanged = OnPositionChanged;
            _onQueueChanged = OnQueueChanged;

            _playback.OnTrackChanged += _onTrackChanged;
            _playback.OnStateChanged += _onStateChanged;
            _playback.OnPositionChanged += _onPositionChanged;
            _playback.OnQueueChanged += _onQueueChanged;

            _subscriptions.Add(EventBus.Instance.Subscribe<PlayStartedEvent>(OnPlayStarted));
            _subscriptions.Add(EventBus.Instance.Subscribe<ModuleLoadedEvent>(OnModuleLoaded));
            _subscriptions.Add(EventBus.Instance.Subscribe<ModuleUnloadedEvent>(OnModuleUnloaded));
            _subscriptions.Add(EventBus.Instance.Subscribe<FavoriteChangedEvent>(OnFavoriteChanged));
            _subscriptions.Add(EventBus.Instance.Subscribe<ExcludeChangedEvent>(OnExcludeChanged));
            _subscriptions.Add(EventBus.Instance.Subscribe<QueueChangedEvent>(OnQueueChangedEvent));
            _subscriptions.Add(EventBus.Instance.Subscribe<CoverInvalidatedEvent>(OnCoverInvalidated));
            _subscriptions.Add(EventBus.Instance.Subscribe<PlaylistUpdatedEvent>(OnPlaylistUpdated));
            _subscriptions.Add(EventBus.Instance.Subscribe<ErrorEvent>(OnError));
            _subscriptions.Add(EventBus.Instance.Subscribe<LyricFetchedEvent>(OnLyricFetched));
            _subscriptions.Add(EventBus.Instance.Subscribe<LyricPositionEvent>(OnLyricPosition));
        }

        private void OnTrackChanged(SDK.Models.MusicInfo track)
        {
            _ = _apiServer.BroadcastEvent("track.changed", new
            {
                uuid = track?.UUID,
                title = track?.Title,
                artist = track?.Artist,
                albumId = track?.AlbumId,
                duration = track?.Duration ?? 0,
                moduleId = track?.ModuleId
            });
        }

        private void OnStateChanged(int state)
        {
            _ = _apiServer.BroadcastEvent("state.changed", new
            {
                isPlaying = _playback.IsPlaying,
                position = _playback.Position,
                volume = _playback.Volume,
                repeatMode = _playback.RepeatMode,
                shuffle = _playback.Shuffle
            });
        }

        private void OnPositionChanged(float position)
        {
            _ = _apiServer.BroadcastEvent("position", new { position });
        }

        private void OnQueueChanged()
        {
            _ = _apiServer.BroadcastEvent("queue.changed", new { });
        }

        private void OnPlayStarted(PlayStartedEvent e)
        {
            var m = e.Music;
            _ = _apiServer.BroadcastEvent("track.changed", new
            {
                uuid = m?.UUID,
                title = m?.Title,
                artist = m?.Artist,
                albumId = m?.AlbumId,
                duration = m?.Duration ?? 0,
                moduleId = m?.ModuleId
            });
        }

        private void OnModuleLoaded(ModuleLoadedEvent e)
        {
            _ = _apiServer.BroadcastEvent("module.loaded", new
            {
                moduleId = e.ModuleId,
                displayName = e.DisplayName
            });
        }

        private void OnModuleUnloaded(ModuleUnloadedEvent e)
        {
            _ = _apiServer.BroadcastEvent("module.unloaded", new { moduleId = e.ModuleId });
        }

        private void OnFavoriteChanged(FavoriteChangedEvent e)
        {
            _ = _apiServer.BroadcastEvent("favorite.changed", new
            {
                uuid = e.UUID,
                isFavorite = e.IsFavorite,
                moduleId = e.ModuleId
            });
        }

        private void OnExcludeChanged(ExcludeChangedEvent e)
        {
            _ = _apiServer.BroadcastEvent("exclude.changed", new
            {
                uuid = e.UUID,
                isExcluded = e.IsExcluded,
                moduleId = e.ModuleId
            });
        }

        private void OnQueueChangedEvent(QueueChangedEvent e)
        {
            _ = _apiServer.BroadcastEvent("queue.changed", new
            {
                changeType = e.ChangeType.ToString(),
                queueLength = e.QueueLength
            });
        }

        private void OnCoverInvalidated(CoverInvalidatedEvent e)
        {
            _ = _apiServer.BroadcastEvent("cover.invalidated", new
            {
                musicUuid = e.MusicUuid,
                albumId = e.AlbumId,
                reason = e.Reason
            });
        }

        private void OnPlaylistUpdated(PlaylistUpdatedEvent e)
        {
            _ = _apiServer.BroadcastEvent("playlist.updated", new
            {
                tagId = e.TagId,
                updateType = e.UpdateType.ToString(),
                changedCount = e.ChangedCount
            });
        }

        private void OnError(ErrorEvent e)
        {
            _ = _apiServer.BroadcastEvent("error", new { code = e.Code, message = e.Message });
        }

        private void OnLyricFetched(LyricFetchedEvent e)
        {
            _ = _apiServer.BroadcastEvent("lyric.fetched", new
            {
                uuid = e.Uuid,
                lrc = e.Lrc,
                tlyric = e.Tlyric,
                rlyric = e.Rlyric
            });
        }

        private void OnLyricPosition(LyricPositionEvent e)
        {
            _ = _apiServer.BroadcastEvent("lyric.position", new
            {
                uuid = e.Uuid,
                lineIndex = e.LineIndex,
                timeMs = e.TimeMs
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _playback.OnTrackChanged -= _onTrackChanged;
            _playback.OnStateChanged -= _onStateChanged;
            _playback.OnPositionChanged -= _onPositionChanged;
            _playback.OnQueueChanged -= _onQueueChanged;

            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
