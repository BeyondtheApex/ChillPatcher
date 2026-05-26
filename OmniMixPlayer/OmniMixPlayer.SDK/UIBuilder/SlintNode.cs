using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniMixPlayer.SDK.Interfaces
{
    public class SlintNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("node-type")]
        public string NodeType { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("font-size")]
        public float FontSize { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; }

        [JsonPropertyName("spacing")]
        public float Spacing { get; set; }

        [JsonPropertyName("padding")]
        public float Padding { get; set; }

        [JsonPropertyName("children")]
        public List<SlintNode> Children { get; set; } = new();

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("input-type")]
        public string InputType { get; set; }

        [JsonPropertyName("button-variant")]
        public string ButtonVariant { get; set; }

        [JsonPropertyName("checked")]
        public bool Checked { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("image-width")]
        public float ImageWidth { get; set; }

        [JsonPropertyName("image-height")]
        public float ImageHeight { get; set; }

        [JsonPropertyName("image-fit")]
        public string ImageFit { get; set; }

        [JsonPropertyName("selected-value")]
        public string SelectedValue { get; set; }

        [JsonPropertyName("options")]
        public List<SlintOption> Options { get; set; }

        [JsonPropertyName("items")]
        public List<SlintNode> Items { get; set; }

        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = null,
                WriteIndented = false
            };

            return JsonSerializer.Serialize(this, options);
        }

        public void FinalizeSources()
        {
            if (!string.IsNullOrEmpty(Source))
            {
                Source = RewriteImageUrl(Source);
            }

            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.FinalizeSources();
                }
            }

            if (Items != null)
            {
                foreach (var item in Items)
                {
                    item.FinalizeSources();
                }
            }
        }

        private static string RewriteImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            if (url.StartsWith("data:") || url.StartsWith("/"))
            {
                Console.Error.WriteLine($"[SlintNode] RewriteImageUrl: keeping as-is '{url}'");
                return url;
            }

            var encoded = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(url)));
            var result = $"/api/proxy/image?url={encoded}";
            Console.Error.WriteLine($"[SlintNode] RewriteImageUrl: '{url}' -> '{result}'");
            return result;
        }
    }

    public class SlintOption
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        public SlintOption() { }

        public SlintOption(string value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
