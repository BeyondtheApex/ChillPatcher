namespace OmniMixPlayer.SDK.Interfaces
{
    public interface IModuleConfigManager
    {
        string ModuleId { get; }
        T GetValue<T>(string key, T defaultValue = default);
        void SetValue<T>(string key, T value);
        string GetString(string key, string defaultValue = "");
        int GetInt(string key, int defaultValue = 0);
        bool GetBool(string key, bool defaultValue = false);
        bool HasKey(string key);
        void Save();
    }
}
