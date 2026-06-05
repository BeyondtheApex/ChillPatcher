using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Storage;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    /// <summary>
    /// 实例配置持久化 — 统一 LiteDB 表
    /// </summary>
    public sealed class InstanceProfileStore : IDisposable
    {
        private const int InstanceDbVersion = 2;
        private readonly LiteDatabase _db;
        private readonly ILogger _logger;

        private sealed class ProfileDoc
        {
            [BsonId] public string Id { get; set; }
            public string DisplayName { get; set; }
            public int Kind { get; set; }
            public string ModId { get; set; }
            public string GameName { get; set; }
            public int Mode { get; set; }
            public string CapabilitiesJson { get; set; }
            public float Volume { get; set; } = 1.0f;
            public float TargetLatency { get; set; } = 0.1f;
            public string EqualizerJson { get; set; }
            public string ActiveQueueId { get; set; }
            public string QueuesJson { get; set; }
            public string PlaybackTimelineJson { get; set; }
            public List<string> ImportedPlaylistIds { get; set; } = new();
            public List<string> PinnedTagIds { get; set; } = new();
            public long CreatedAt { get; set; }
            public long UpdatedAt { get; set; }
        }

        public InstanceProfileStore(string configBaseDir, ILogger logger = null)
        {
            _logger = logger;
            var dbDir = string.IsNullOrEmpty(configBaseDir)
                ? AppDomain.CurrentDomain.BaseDirectory
                : configBaseDir;

            if (!Directory.Exists(dbDir))
                Directory.CreateDirectory(dbDir);

            var dbPath = Path.Combine(dbDir, "omnimix_instances.db");
            EnsureDatabaseVersion(dbPath);
            _db = new LiteDatabase(dbPath);
            WriteDatabaseVersion();
            _logger?.LogInformation("InstanceProfileStore initialized at {Path}", dbPath);

            _db.GetCollection<ProfileDoc>("profiles").EnsureIndex(x => x.Kind);
        }

        private void EnsureDatabaseVersion(string dbPath)
        {
            if (!File.Exists(dbPath)) return;

            try
            {
                using var db = new LiteDatabase(dbPath);
                var meta = db.GetCollection<BsonDocument>(StorageVersion.LiteDbCollection);
                var doc = meta.FindById(StorageVersion.LiteDbDocumentId);
                if (doc != null &&
                    doc.ContainsKey("version") &&
                    doc["version"].AsInt32 == InstanceDbVersion)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Instance database version check failed; rebuilding {Path}", dbPath);
            }

            DeleteDatabaseFiles(dbPath);
            _logger?.LogInformation("Deleted incompatible instance database; it will be rebuilt at {Path}", dbPath);
        }

        private void WriteDatabaseVersion()
        {
            var meta = _db.GetCollection<BsonDocument>(StorageVersion.LiteDbCollection);
            meta.Upsert(new BsonDocument
            {
                ["_id"] = StorageVersion.LiteDbDocumentId,
                ["version"] = InstanceDbVersion
            });
        }

        private static void DeleteDatabaseFiles(string dbPath)
        {
            foreach (var path in new[] { dbPath, $"{dbPath}-log", $"{dbPath}-shm", $"{dbPath}-wal" })
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public InstanceProfile Get(string id)
        {
            return TryGet(id) ?? CreateDefault(id);
        }

        public InstanceProfile TryGet(string id)
        {
            var col = _db.GetCollection<ProfileDoc>("profiles");
            var doc = col.FindById(id);
            return doc != null ? ToProto(doc) : null;
        }

        public bool Exists(string id)
        {
            return _db.GetCollection<ProfileDoc>("profiles").Exists(x => x.Id == id);
        }

        public List<InstanceProfile> GetAll()
        {
            var col = _db.GetCollection<ProfileDoc>("profiles");
            return col.FindAll().Select(ToProto).ToList();
        }

        public void Upsert(InstanceProfile profile)
        {
            var col = _db.GetCollection<ProfileDoc>("profiles");
            profile.UpdatedAt = new OmniTimestamp { Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            col.Upsert(ToDoc(profile));
        }

        public bool Delete(string id)
        {
            return _db.GetCollection<ProfileDoc>("profiles").Delete(id);
        }

        // ── Archive ──

        public bool Archive(string id)
        {
            var col = _db.GetCollection<ProfileDoc>("profiles");
            var doc = col.FindById(id);
            if (doc == null) return false;
            var archiveCol = _db.GetCollection<ProfileDoc>("archives");
            doc.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            archiveCol.Upsert(doc);
            col.Delete(id);
            return true;
        }

        public InstanceProfile SaveArchiveCopy(string id, string label)
        {
            var col = _db.GetCollection<ProfileDoc>("profiles");
            var doc = col.FindById(id);
            if (doc == null) return null;

            var profile = ToProto(doc);
            if (!string.IsNullOrWhiteSpace(label))
                profile.DisplayName = label;
            profile.UpdatedAt = new OmniTimestamp { Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

            var archiveCol = _db.GetCollection<ProfileDoc>("archives");
            archiveCol.Upsert(ToDoc(profile));
            return profile;
        }

        public List<InstanceProfile> ListArchives()
        {
            return _db.GetCollection<ProfileDoc>("archives").FindAll().Select(ToProto).ToList();
        }

        public bool DeleteArchive(string id)
        {
            return _db.GetCollection<ProfileDoc>("archives").Delete(id);
        }

        public InstanceProfile GetArchive(string id)
        {
            var doc = _db.GetCollection<ProfileDoc>("archives").FindById(id);
            return doc != null ? ToProto(doc) : null;
        }

        public InstanceProfile InheritFromArchive(string newId, string archiveId)
        {
            var archiveCol = _db.GetCollection<ProfileDoc>("archives");
            var archived = archiveCol.FindById(archiveId);
            if (archived == null) return null;

            var col = _db.GetCollection<ProfileDoc>("profiles");
            archived.Id = newId;
            archived.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            archived.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            col.Upsert(archived);

            // If archive is not bound to any existing instance (only in archives, not in profiles), consume it
            if (_db.GetCollection<ProfileDoc>("profiles").FindById(archiveId) == null)
                archiveCol.Delete(archiveId);

            return ToProto(archived);
        }

        public void SaveVolume(string instanceId, float volume)
        {
            var profile = Get(instanceId);
            profile.Volume = volume;
            Upsert(profile);
        }

        public void SaveTargetLatency(string instanceId, float latency)
        {
            var profile = Get(instanceId);
            profile.TargetLatency = latency;
            Upsert(profile);
        }

        public void SaveEqualizer(string instanceId, SDK.Protos.Models.EqualizerState eq)
        {
            var profile = Get(instanceId);
            profile.Equalizer = eq;
            Upsert(profile);
        }

        // ── Conversion ──

        private static ProfileDoc ToDoc(InstanceProfile p) => new ProfileDoc
        {
            Id = p.Id,
            DisplayName = p.DisplayName,
            Kind = (int)p.Kind,
            ModId = p.ModId,
            GameName = p.GameName,
            Mode = 0,
            CapabilitiesJson = Google.Protobuf.JsonFormatter.Default.Format(p.Capabilities),
            Volume = p.Volume,
            TargetLatency = p.TargetLatency,
            EqualizerJson = Google.Protobuf.JsonFormatter.Default.Format(p.Equalizer),
            ActiveQueueId = p.ActiveQueueId,
            QueuesJson = SerializeQueues(p.Queues),
            PlaybackTimelineJson = Google.Protobuf.JsonFormatter.Default.Format(p.PlaybackTimeline ?? CreateDefaultTimeline()),
            ImportedPlaylistIds = p.ImportedPlaylistIds?.ToList() ?? new(),
            PinnedTagIds = p.PinnedTagIds?.ToList() ?? new(),
            CreatedAt = p.CreatedAt?.Seconds ?? 0,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        private static InstanceProfile ToProto(ProfileDoc doc)
        {
            var profile = new InstanceProfile
            {
                Id = doc.Id ?? "",
                DisplayName = doc.DisplayName ?? "",
                Kind = (InstanceKind)doc.Kind,
                ModId = doc.ModId ?? "",
                GameName = doc.GameName ?? "",
                Volume = doc.Volume,
                TargetLatency = doc.TargetLatency,
                ActiveQueueId = doc.ActiveQueueId ?? "default",
                CreatedAt = new OmniTimestamp { Seconds = doc.CreatedAt },
                UpdatedAt = new OmniTimestamp { Seconds = doc.UpdatedAt }
            };

            if (doc.ImportedPlaylistIds != null)
                profile.ImportedPlaylistIds.AddRange(doc.ImportedPlaylistIds);
            if (doc.PinnedTagIds != null)
                profile.PinnedTagIds.AddRange(doc.PinnedTagIds);

            // Deserialize capabilities
            if (!string.IsNullOrEmpty(doc.CapabilitiesJson))
            {
                try
                {
                    profile.Capabilities = Google.Protobuf.JsonParser.Default.Parse<SDK.Protos.Models.InstanceCapabilities>(doc.CapabilitiesJson);
                }
                catch { profile.Capabilities = new SDK.Protos.Models.InstanceCapabilities(); }
            }

            // Deserialize equalizer
            if (!string.IsNullOrEmpty(doc.EqualizerJson))
            {
                try
                {
                    profile.Equalizer = Google.Protobuf.JsonParser.Default.Parse<SDK.Protos.Models.EqualizerState>(doc.EqualizerJson);
                }
                catch { profile.Equalizer = new SDK.Protos.Models.EqualizerState(); }
            }

            // Deserialize queues
            if (!string.IsNullOrEmpty(doc.QueuesJson) && doc.QueuesJson != "[]")
            {
                try
                {
                    var queueList = System.Text.Json.JsonSerializer.Deserialize<List<QueueInfo>>(doc.QueuesJson);
                    if (queueList != null)
                        profile.Queues.AddRange(queueList);
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(doc.PlaybackTimelineJson))
            {
                try
                {
                    var timeline = Google.Protobuf.JsonParser.Default.Parse<PlaybackTimelineState>(doc.PlaybackTimelineJson);
                    profile.PlaybackTimeline = timeline.Version == 2 ? timeline : CreateDefaultTimeline();
                }
                catch { profile.PlaybackTimeline = CreateDefaultTimeline(); }
            }
            else
            {
                profile.PlaybackTimeline = CreateDefaultTimeline();
            }

            return profile;
        }

        private static string SerializeQueues(IEnumerable<QueueInfo> queues)
        {
            if (queues == null) return "[]";
            var list = queues.ToList();
            if (list.Count == 0) return "[]";
            return System.Text.Json.JsonSerializer.Serialize(list);
        }

        private static InstanceProfile CreateDefault(string id) => new InstanceProfile
        {
            Id = id,
            DisplayName = id,
            Kind = InstanceKind.GameMod,
            Volume = 1.0f,
            TargetLatency = 0.1f,
            ActiveQueueId = "default",
            Capabilities = new InstanceCapabilities(),
            Equalizer = new SDK.Protos.Models.EqualizerState { SoftClipEnabled = true },
            PlaybackTimeline = CreateDefaultTimeline(),
            CreatedAt = new OmniTimestamp { Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

        private static PlaybackTimelineState CreateDefaultTimeline() => new()
        {
            Version = 2,
            SourceCursor = -1,
            CurrentSourceIndex = -1
        };

        public void Dispose()
        {
            try { _db?.Dispose(); }
            catch (Exception ex) { _logger?.LogError(ex, "Error disposing InstanceProfileStore"); }
        }
    }
}
