using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.Http
{
    public class ModuleUIHandler
    {
        private readonly ModuleSystem.ModuleLoader _moduleLoader;
        private readonly ApiServer _apiServer;
        private readonly ILogger _logger;

        /// <summary>Exposed for REST endpoint access.</summary>
        public ModuleSystem.ModuleLoader ModuleLoader => _moduleLoader;

        public ModuleUIHandler(ModuleSystem.ModuleLoader moduleLoader, ApiServer apiServer, ILogger logger)
        {
            _moduleLoader = moduleLoader;
            _apiServer = apiServer;
            _logger = logger;
        }

        /// <summary>Get the IModuleUIProvider for a module, if any.</summary>
        public IModuleUIProvider GetUIProvider(string moduleId)
        {
            var module = _moduleLoader.GetModule(moduleId);
            return module as IModuleUIProvider;
        }

        public async Task HandleUiEvent(string message)
        {
            try
            {
                var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "ui_event")
                    return;

                if (!root.TryGetProperty("moduleId", out var moduleIdProp))
                    return;

                var moduleId = moduleIdProp.GetString();
                if (string.IsNullOrEmpty(moduleId))
                    return;

                if (!root.TryGetProperty("event", out var eventProp))
                    return;

                var nodeId = "";
                var action = "";
                var value = "";
                var uiKind = "default";
                var linkId = "";

                if (eventProp.TryGetProperty("nodeId", out var nid))
                    nodeId = nid.GetString() ?? "";
                if (eventProp.TryGetProperty("action", out var act))
                    action = act.GetString() ?? "";
                if (eventProp.TryGetProperty("value", out var val))
                    value = val.GetString() ?? "";
                if (root.TryGetProperty("uiKind", out var kind))
                    uiKind = kind.GetString() ?? "default";
                if (root.TryGetProperty("linkId", out var lid))
                    linkId = lid.GetString() ?? "";

                _logger.LogInformation("UI event: module={ModuleId}, node={NodeId}, action={Action}, value={Value}, kind={UIKind}",
                    moduleId, nodeId, action, value, uiKind);

                var module = _moduleLoader.GetModule(moduleId);
                if (module is IModuleUIProvider uiProvider)
                {
                    switch (uiKind)
                    {
                        case "link":
                            uiProvider.HandleLinkUIEvent(linkId, nodeId, action, value);
                            break;
                        case "settings":
                            uiProvider.HandleSettingsUIEvent(nodeId, action, value);
                            break;
                        default:
                            uiProvider.HandleUIEvent(nodeId, action, value);
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning("Module {ModuleId} does not implement IModuleUIProvider", moduleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling UI event");
            }
        }

        public async Task PushUI(string moduleId, SlintNode tree, bool replace = true)
        {
            try
            {
                tree?.FinalizeSources();

                var payload = new
                {
                    type = "ui_push",
                    moduleId,
                    replace,
                    tree
                };

                await _apiServer.BroadcastJsonEvent("ui_push", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing UI for module {ModuleId}", moduleId);
            }
        }

        public void RegisterPushUICallback(string moduleId, IModuleUIProvider provider)
        {
            provider.PushUI = (tree) =>
            {
                _ = PushUI(moduleId, tree, true);
            };
        }
    }
}
