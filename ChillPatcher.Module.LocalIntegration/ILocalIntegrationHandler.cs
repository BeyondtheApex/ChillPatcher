using BepInEx.Logging;

namespace ChillPatcher.Module.LocalIntegration
{
    internal interface ILocalIntegrationHandler
    {
        string Method { get; }
        string Path { get; }
        bool TryValidate(string body, out string error);
        void Execute(string body, ManualLogSource logger);
    }
}
