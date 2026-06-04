using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OmniMixPlayer.Backend.Storage;

namespace OmniMixPlayer.Backend.Http
{
    public class GlobalConfigManager
    {
        private readonly string _configPath;
        private Dictionary<string, JsonElement> _config;

        /// <summary>
        /// Raised after Save() completes, so callers (e.g. Program) can react
        /// to config changes without restarting the process.
        /// </summary>
        public Action OnConfigSaved;

        public GlobalConfigManager(string configDir)
        {
            _configPath = Path.Combine(configDir, "global_config.json");
            _config = new Dictionary<string, JsonElement>();
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    if (!StorageVersion.JsonHasCurrentVersion(_configPath))
                    {
                        File.Delete(_configPath);
                        _config.Clear();
                        return;
                    }

                    var json = File.ReadAllText(_configPath);
                    var doc = JsonDocument.Parse(json);
                    _config = new Dictionary<string, JsonElement>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        _config[prop.Name] = prop.Value.Clone();
                    }
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                var dict = new Dictionary<string, object>();
                foreach (var kv in _config)
                {
                    dict[kv.Key] = ConvertElement(kv.Value);
                }
                dict[StorageVersion.JsonKey] = StorageVersion.Current;
                var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                var dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(_configPath, json);
                OnConfigSaved?.Invoke();
            }
            catch { }
        }

        private object ConvertElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String: return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var i)) return i;
                    if (element.TryGetDouble(out var d)) return d;
                    return element.GetRawText();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                default: return element.GetRawText();
            }
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_config.TryGetValue(key, out var elem))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(elem.GetRawText());
                }
                catch { }
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            var doc = JsonDocument.Parse(json);
            _config[key] = doc.RootElement.Clone();
        }

        public bool HasKey(string key) => _config.ContainsKey(key);

        public Dictionary<string, object> GetAll()
        {
            var result = new Dictionary<string, object>();
            foreach (var kv in _config)
            {
                result[kv.Key] = ConvertElement(kv.Value);
            }
            return result;
        }
    }
}
