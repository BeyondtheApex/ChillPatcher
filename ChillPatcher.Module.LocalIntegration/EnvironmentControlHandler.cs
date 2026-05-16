using BepInEx.Logging;
using ChillPatcher.JSApi;

namespace ChillPatcher.Module.LocalIntegration
{
    internal sealed class EnvironmentControlHandler : ILocalIntegrationHandler
    {
        public string Method => "POST";
        public string Path => "/v1/environment/control";

        public bool TryValidate(string body, out string error)
        {
            error = string.Empty;
            var action = JsonBody.StringValue(body, "action");
            if (string.IsNullOrWhiteSpace(action))
            {
                error = "缺少 action";
                return false;
            }

            switch (action)
            {
                case "setViewActive":
                case "setSoundVolume":
                case "setSoundMute":
                case "setAutoTimeEnabled":
                case "setAutoTimeHours":
                case "loadPreset":
                case "savePreset":
                    return true;
                default:
                    error = "未知的环境动作";
                    return false;
            }
        }

        public void Execute(string body, ManualLogSource logger)
        {
            var action = JsonBody.StringValue(body, "action");
            using (var api = new ChillEnvironmentApi(logger))
            {
                switch (action)
                {
                    case "setViewActive":
                        if (JsonBody.TryBoolValue(body, "active", out var active))
                            api.setViewActive(JsonBody.StringValue(body, "id"), active);
                        break;
                    case "setSoundVolume":
                        if (JsonBody.TryFloatValue(body, "volume", out var volume))
                            api.setSoundVolume(JsonBody.StringValue(body, "id"), volume);
                        break;
                    case "setSoundMute":
                        if (JsonBody.TryBoolValue(body, "muted", out var muted))
                            api.setSoundMute(JsonBody.StringValue(body, "id"), muted);
                        break;
                    case "setAutoTimeEnabled":
                        if (JsonBody.TryBoolValue(body, "enabled", out var enabled))
                            api.setAutoTimeEnabled(enabled);
                        break;
                    case "setAutoTimeHours":
                        if (JsonBody.TryFloatValue(body, "dayStart", out var dayStart) &&
                            JsonBody.TryFloatValue(body, "sunsetStart", out var sunsetStart) &&
                            JsonBody.TryFloatValue(body, "nightStart", out var nightStart))
                        {
                            api.setAutoTimeHours(dayStart, sunsetStart, nightStart);
                        }
                        break;
                    case "loadPreset":
                        if (JsonBody.TryIntValue(body, "index", out var loadIndex))
                            api.loadPreset(loadIndex);
                        break;
                    case "savePreset":
                        if (JsonBody.TryIntValue(body, "index", out var saveIndex))
                            api.saveToPreset(saveIndex);
                        break;
                }
            }
        }
    }
}
