using BepInEx.Logging;

namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// Handler for a local HTTP integration route.
    /// Implement this interface in a module assembly to expose a trusted local endpoint.
    /// </summary>
    public interface ILocalIntegrationHandler
    {
        /// <summary>
        /// HTTP method handled by this route, for example GET or POST.
        /// </summary>
        string Method { get; }

        /// <summary>
        /// Absolute route path handled by this endpoint, for example /v1/example/action.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Validate the raw request body before the handler is queued onto the Unity main thread.
        /// </summary>
        bool TryValidate(string body, out string error);

        /// <summary>
        /// Execute the request on the Unity main thread.
        /// </summary>
        void Execute(string body, ManualLogSource logger);
    }
}
