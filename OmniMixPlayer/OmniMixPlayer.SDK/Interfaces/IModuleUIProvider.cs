using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OmniMixPlayer.SDK.Interfaces
{
    public struct ModuleLinkEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("svg")]
        public string Svg { get; set; }

        [JsonPropertyName("backgroundColor")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("iconColor")]
        public string IconColor { get; set; }

        public ModuleLinkEntry(string id, string title, string icon, string backgroundColor, string iconColor, string svg = null)
        {
            Id = id;
            Title = title;
            Icon = icon;
            Svg = svg ?? "";
            BackgroundColor = backgroundColor;
            IconColor = iconColor;
        }
    }

    public interface IModuleUIProvider
    {
        SlintNode BuildUI();
        void HandleUIEvent(string nodeId, string action, string value);
        Action<SlintNode> PushUI { get; set; }

        bool HasQuickLinks => false;
        IReadOnlyList<ModuleLinkEntry> GetQuickLinks() => Array.Empty<ModuleLinkEntry>();

        SlintNode BuildLinkUI(string linkId) => null;
        void HandleLinkUIEvent(string linkId, string nodeId, string action, string value) { }

        bool HasSettingsUI => false;
        SlintNode BuildSettingsUI() => null;
        void HandleSettingsUIEvent(string nodeId, string action, string value) { }

        /// <summary>Serve raw binary content for paths under /api/modules/{id}/content/{path}.</summary>
        Task<byte[]> ServeRawContent(string path) => null;
        string ServeRawContentType(string path) => null;
    }
}
