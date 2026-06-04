using System;
using System.Collections.Generic;
using OmniMixPlayer.Backend.ModuleSystem;
using OmniMixPlayer.SDK.Events;
using ProtoEvents = OmniMixPlayer.SDK.Protos.Events;

namespace OmniMixPlayer.Backend.Http
{
    public class EventBridge : IDisposable
    {
        private readonly ApiServer _apiServer;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        public EventBridge(ApiServer apiServer)
        {
            _apiServer = apiServer;

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

        private static ProtoEvents.WsEvent MakeEvent(string type) => new ProtoEvents.WsEvent { Type = type, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

        private void OnModuleLoaded(ModuleLoadedEvent e)
        {
            var evt = MakeEvent("module.loaded");
            evt.ModuleChanged = new ProtoEvents.ModuleChangedEvent { ModuleId = e.ModuleId, Enabled = true, DisplayName = e.DisplayName };
            _ = _apiServer.BroadcastProtoEvent(evt);
        }

        private void OnModuleUnloaded(ModuleUnloadedEvent e)
        {
            var evt = MakeEvent("module.unloaded");
            evt.ModuleChanged = new ProtoEvents.ModuleChangedEvent { ModuleId = e.ModuleId, Enabled = false };
            _ = _apiServer.BroadcastProtoEvent(evt);
        }

        private void OnFavoriteChanged(FavoriteChangedEvent e)
        {
            var evt = MakeEvent("favorite.changed");
            evt.FavoriteChanged = new ProtoEvents.FavoriteChangedEvent { Uuid = e.UUID, IsFavorite = e.IsFavorite, ModuleId = e.ModuleId };
            _ = _apiServer.BroadcastProtoEvent(evt);
        }

        private void OnExcludeChanged(ExcludeChangedEvent e)
        {
            var evt = MakeEvent("exclude.changed");
            evt.ExcludeChanged = new ProtoEvents.ExcludeChangedEvent { Uuid = e.UUID, IsExcluded = e.IsExcluded, ModuleId = e.ModuleId };
            _ = _apiServer.BroadcastProtoEvent(evt);
        }

        private void OnQueueChangedEvent(QueueChangedEvent e)
        {
            var evt = MakeEvent("queue.changed");
            evt.QueueChanged = new ProtoEvents.QueueChangedEvent { ChangeType = e.ChangeType.ToString(), QueueLength = e.QueueLength };
            _ = _apiServer.BroadcastProtoEvent(evt);
        }

        private void OnCoverInvalidated(CoverInvalidatedEvent e) { /* skip */ }

        private void OnPlaylistUpdated(PlaylistUpdatedEvent e)
        {
            var evt = MakeEvent("playlist.updated");
            evt.PlaylistUpdated = new ProtoEvents.PlaylistUpdatedEvent { SourceRefId = e.SourceRefId ?? "", SongCount = e.ChangedCount, UpdateType = e.UpdateType.ToString() };
            _ = _apiServer.BroadcastProtoEvent(evt);
        }

        private void OnError(ErrorEvent e) { }
        private void OnLyricFetched(LyricFetchedEvent e) { }
        private void OnLyricPosition(LyricPositionEvent e) { }

        public void Dispose()
        {
            if (!_disposed) { _disposed = true; foreach (var s in _subscriptions) s.Dispose(); _subscriptions.Clear(); }
        }
    }
}
