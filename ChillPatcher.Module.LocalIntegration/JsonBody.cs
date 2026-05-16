using System.Text.RegularExpressions;
using System.Globalization;

namespace ChillPatcher.Module.LocalIntegration
{
    internal static class JsonBody
    {
        public static string StringValue(string json, string key)
        {
            var match = Regex.Match(json ?? "", "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"");
            return match.Success ? Regex.Unescape(match.Groups[1].Value) : "";
        }

        public static bool TryBoolValue(string json, string key, out bool value)
        {
            value = false;
            var match = Regex.Match(json ?? "", "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success && bool.TryParse(match.Groups[1].Value, out value);
        }

        public static bool TryIntValue(string json, string key, out int value)
        {
            value = 0;
            var match = Regex.Match(json ?? "", "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryFloatValue(string json, string key, out float value)
        {
            value = 0f;
            var match = Regex.Match(json ?? "", "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)");
            return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
