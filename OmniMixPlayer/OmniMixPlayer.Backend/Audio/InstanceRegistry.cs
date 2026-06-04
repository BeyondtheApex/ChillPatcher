using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    /// <summary>
    /// 实例注册表 — 管理 InstanceProfile 的持久化和 CRUD
    /// 实例 = 长期存在的 profile (capabilities, settings, imported playlists)
    /// 不管理在线会话 (那是 PlaybackSessionManager 的职责)
    /// </summary>
    public sealed class InstanceRegistry : IDisposable
    {
        private readonly InstanceProfileStore _store;
        private readonly ILogger _logger;

        public InstanceProfileStore ProfileStore => _store;

        public event Action OnChanged;

        public InstanceRegistry(InstanceProfileStore store, ILogger logger)
        {
            _store = store;
            _logger = logger;
        }

        /// <summary>
        /// ConnectOrCreate: 合并 profile，不覆盖用户设置
        /// </summary>
        public InstanceProfile ConnectOrCreate(InstanceConnectRequest req, out bool isNew)
        {
            var id = SanitizeId(req.ClientId);
            var existing = _store.TryGet(id);
            isNew = existing == null;
            existing ??= _store.Get(id);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var merged = new InstanceProfile
            {
                Id = id,
                DisplayName = req.DisplayName ?? existing.DisplayName,
                Kind = req.Kind,
                ModId = req.ModId ?? existing.ModId,
                GameName = req.GameName ?? existing.GameName,
                Mode = req.Mode,
                Capabilities = req.Capabilities ?? existing.Capabilities ?? new InstanceCapabilities(),
                Volume = existing.Volume,
                TargetLatency = existing.TargetLatency,
                Equalizer = existing.Equalizer ?? new SDK.Protos.Models.EqualizerState { SoftClipEnabled = true },
                ActiveQueueId = existing.ActiveQueueId ?? "default",
                PlaybackQueue = existing.PlaybackQueue ?? new PlaybackQueueState { ActiveQueueId = existing.ActiveQueueId ?? "default" },
                CreatedAt = existing.CreatedAt ?? new OmniTimestamp { Seconds = now }
            };
            merged.ImportedPlaylistIds.AddRange(existing.ImportedPlaylistIds);
            merged.PinnedTagIds.AddRange(existing.PinnedTagIds);
            merged.Queues.AddRange(existing.Queues);

            _store.Upsert(merged);
            OnChanged?.Invoke();
            return merged;
        }

        public InstanceProfile Get(string id) => _store.TryGet(SanitizeId(id));
        public InstanceProfile GetOrDefault(string id) => _store.Get(SanitizeId(id));
        public List<InstanceProfile> GetAll() => _store.GetAll();

        public void Update(InstanceProfile profile)
        {
            profile.Id = SanitizeId(profile.Id);
            _store.Upsert(profile);
            OnChanged?.Invoke();
        }

        public bool Delete(string id)
        {
            id = SanitizeId(id);
            var archived = _store.Archive(id);
            if (archived && !_store.Exists(id))
            {
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        // ── Archive ──

        public List<InstanceProfile> ListArchives() => _store.ListArchives();
        public InstanceProfile SaveArchiveCopy(string id, string label)
        {
            var profile = _store.SaveArchiveCopy(SanitizeId(id), label);
            if (profile != null) OnChanged?.Invoke();
            return profile;
        }
        public bool DeleteArchive(string id) => _store.DeleteArchive(SanitizeId(id));
        public InstanceProfile GetArchive(string id) => _store.GetArchive(SanitizeId(id));
        public InstanceProfile InheritFromArchive(string newId, string archiveId)
        {
            var profile = _store.InheritFromArchive(SanitizeId(newId), SanitizeId(archiveId));
            if (profile != null) OnChanged?.Invoke();
            return profile;
        }

        public void SaveVolume(string id, float volume) { _store.SaveVolume(SanitizeId(id), volume); }
        public void SaveTargetLatency(string id, float latency) { _store.SaveTargetLatency(SanitizeId(id), latency); }
        public void SaveEqualizer(string id, SDK.Protos.Models.EqualizerState eq) { _store.SaveEqualizer(SanitizeId(id), eq); }
        public void SavePlaybackQueue(string id, PlaybackQueueState queue)
        {
            var profile = _store.Get(SanitizeId(id));
            profile.PlaybackQueue = queue ?? new PlaybackQueueState { ActiveQueueId = profile.ActiveQueueId ?? "default" };
            profile.ActiveQueueId = profile.PlaybackQueue.ActiveQueueId;
            profile.ImportedPlaylistIds.Clear();
            profile.ImportedPlaylistIds.AddRange(profile.PlaybackQueue.PlaylistSources.Select(s => s.Id));
            profile.Queues.Clear();
            profile.Queues.Add(new QueueInfo
            {
                Id = profile.PlaybackQueue.ActiveQueueId ?? "default",
                Name = "Default",
                SongCount = profile.PlaybackQueue.QueueUuids.Count
            });
            _store.Upsert(profile);
            OnChanged?.Invoke();
        }

        public List<InstanceSummary> ListSummaries(PlaybackSessionManager sessions = null)
        {
            var summaries = new List<InstanceSummary>();
            var allProfiles = _store.GetAll();
            foreach (var p in allProfiles)
            {
                var online = sessions?.IsOnline(p.Id) ?? false;
                summaries.Add(new InstanceSummary
                {
                    Id = p.Id,
                    DisplayName = p.DisplayName,
                    Kind = p.Kind,
                    Mode = p.Mode,
                    IsOnline = online,
                    CurrentTrackUuid = sessions?.GetCurrentTrackUuid(p.Id) ?? "",
                    QueueCount = sessions?.GetQueueCount(p.Id) ?? 0
                });
            }
            return summaries;
        }

        private static string SanitizeId(string id) => (id ?? "").Replace("..", "").Replace("/", "").Replace("\\", "").Trim();

        public void Dispose() => _store?.Dispose();
    }
}
