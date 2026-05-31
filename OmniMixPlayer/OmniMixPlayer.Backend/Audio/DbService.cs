using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace OmniMixPlayer.Backend.Audio
{
    public sealed class DbService : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly ILogger _logger;

        public DbService(string configBaseDir, ILogger logger = null)
        {
            _logger = logger;

            // Resolve db path: default to local directory if configBaseDir is null/empty
            string dbDir = string.IsNullOrEmpty(configBaseDir)
                ? AppDomain.CurrentDomain.BaseDirectory
                : configBaseDir;

            try
            {
                if (!Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create directory for database: {Path}", dbDir);
            }

            _dbPath = Path.Combine(dbDir, "omnimix.db");

            try
            {
                // LiteDatabase handles file locking, concurrent reads, and single-writer queueing
                _db = new LiteDatabase(_dbPath);
                _logger?.LogInformation("LiteDB initialized at {Path}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize LiteDB at {Path}", _dbPath);
                throw;
            }
        }

        public PlaybackStateData GetProfile(string instanceId)
        {
            try
            {
                var col = _db.GetCollection<PlaybackStateData>("profiles");
                var profile = col.FindById(instanceId);
                if (profile == null)
                {
                    profile = new PlaybackStateData
                    {
                        Id = instanceId,
                        ActiveQueueId = "default",
                        Volume = 1.0f,
                        Queues = new List<QueueSlotData>()
                    };
                }
                return profile;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get profile for instance {InstanceId}", instanceId);
                return new PlaybackStateData { Id = instanceId };
            }
        }

        public IEnumerable<PlaybackStateData> GetAllProfiles()
        {
            try
            {
                var col = _db.GetCollection<PlaybackStateData>("profiles");
                return col.FindAll();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get all profiles from LiteDB");
                return Array.Empty<PlaybackStateData>();
            }
        }

        public void SaveProfile(PlaybackStateData profile)
        {
            try
            {
                var col = _db.GetCollection<PlaybackStateData>("profiles");
                col.Upsert(profile);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save profile for instance {InstanceId}", profile?.Id);
            }
        }

        public void SaveVolume(string instanceId, float volume)
        {
            try
            {
                var profile = GetProfile(instanceId);
                profile.Volume = volume;
                SaveProfile(profile);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save volume for instance {InstanceId}", instanceId);
            }
        }

        public void SaveEqualizer(string instanceId, EqualizerState equalizer)
        {
            try
            {
                var profile = GetProfile(instanceId);
                profile.Equalizer = equalizer;
                SaveProfile(profile);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save equalizer for instance {InstanceId}", instanceId);
            }
        }

        public bool DeleteProfile(string instanceId)
        {
            try
            {
                var col = _db.GetCollection<PlaybackStateData>("profiles");
                return col.Delete(instanceId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete profile for instance {InstanceId}", instanceId);
                return false;
            }
        }

        public bool UpdateProfileFromJson(string instanceId, string incomingJson)
        {
            try
            {
                var profile = GetProfile(instanceId);
                using var doc = JsonDocument.Parse(incomingJson);
                var root = doc.RootElement;

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                foreach (var prop in root.EnumerateObject())
                {
                    var name = prop.Name.ToLowerInvariant();
                    if (name == "activequeueid")
                    {
                        profile.ActiveQueueId = prop.Value.GetString();
                    }
                    else if (name == "volume")
                    {
                        profile.Volume = prop.Value.GetSingle();
                    }
                    else if (name == "equalizer")
                    {
                        profile.Equalizer = System.Text.Json.JsonSerializer.Deserialize<EqualizerState>(prop.Value.GetRawText(), options);
                    }
                    else if (name == "queues")
                    {
                        profile.Queues = System.Text.Json.JsonSerializer.Deserialize<List<QueueSlotData>>(prop.Value.GetRawText(), options) ?? new();
                    }
                }

                SaveProfile(profile);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update profile from JSON for instance {InstanceId}", instanceId);
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                _db?.Dispose();
                _logger?.LogInformation("LiteDB connection closed.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing LiteDB connection");
            }
        }
    }
}
