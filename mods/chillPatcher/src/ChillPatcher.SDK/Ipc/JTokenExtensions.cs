using System;
using Newtonsoft.Json.Linq;

namespace ChillPatcher.SDK.Ipc
{
    public static class JTokenExtensions
    {
        public static JToken GetIgnoreCase(this JToken token, string propertyName)
        {
            if (token is JObject obj)
            {
                return obj.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        public static string GetStringIgnoreCase(this JToken token, string propertyName, string defaultValue = "")
        {
            var val = token.GetIgnoreCase(propertyName);
            return val?.ToString() ?? defaultValue;
        }

        public static T GetValueIgnoreCase<T>(this JToken token, string propertyName, T defaultValue = default)
        {
            var val = token.GetIgnoreCase(propertyName);
            if (val == null) return defaultValue;
            try
            {
                return val.ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
