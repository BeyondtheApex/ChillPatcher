using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OmniMixPlayer.Backend.Storage;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.ModuleSystem
{
    public class ModuleConfigManager : IModuleConfigManager
    {
        private readonly string _configPath;
        private Dictionary<string, JsonElement> _values;

        public string ModuleId { get; }

        public ModuleConfigManager(string moduleId, string configDirectory)
        {
            ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _configPath = Path.Combine(configDirectory, $"{moduleId}.json");
            _values = LoadConfig();
        }

        private Dictionary<string, JsonElement> LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    if (!StorageVersion.JsonHasCurrentVersion(_configPath))
                    {
                        File.Delete(_configPath);
                        return new Dictionary<string, JsonElement>();
                    }

                    var json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                        ?? new Dictionary<string, JsonElement>();
                }
            }
            catch { }
            return new Dictionary<string, JsonElement>();
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_values.TryGetValue(key, out var element))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
                catch { }
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            var serialized = JsonSerializer.SerializeToElement(value);
            _values[key] = serialized;
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (_values.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? defaultValue;
            return defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_values.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number)
            {
                if (element.TryGetInt32(out var val)) return val;
            }
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_values.TryGetValue(key, out var element))
            {
                if (element.ValueKind == JsonValueKind.True) return true;
                if (element.ValueKind == JsonValueKind.False) return false;
            }
            return defaultValue;
        }

        public bool HasKey(string key) => _values.ContainsKey(key);

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                SetValue(StorageVersion.JsonKey, StorageVersion.Current);
                var json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }
    }
}
