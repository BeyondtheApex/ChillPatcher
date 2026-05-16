using BepInEx.Logging;
using ChillPatcher.JSApi;
using ChillPatcher.Patches;

namespace ChillPatcher.Module.LocalIntegration
{
    internal sealed class AudioControlHandler : ILocalIntegrationHandler
    {
        public string Method => "POST";
        public string Path => "/v1/audio/control";

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
                case "toggle":
                case "pause":
                case "resume":
                case "next":
                case "previous":
                case "shuffle":
                case "repeatOne":
                case "mute":
                case "progress":
                case "playByIndex":
                case "playByUuid":
                    return true;
                default:
                    error = "未知的音频动作";
                    return false;
            }
        }

        public void Execute(string body, ManualLogSource logger)
        {
            var action = JsonBody.StringValue(body, "action");
            var api = new ChillAudioApi(logger);

            switch (action)
            {
                case "toggle":
                    api.togglePause();
                    break;
                case "pause":
                    api.pause();
                    break;
                case "resume":
                    api.resume();
                    break;
                case "next":
                    api.next();
                    break;
                case "previous":
                    api.previous();
                    break;
                case "shuffle":
                    if (JsonBody.TryBoolValue(body, "enabled", out var shuffle))
                        api.setShuffle(shuffle);
                    break;
                case "repeatOne":
                    if (JsonBody.TryBoolValue(body, "enabled", out var repeat))
                        api.setRepeatOne(repeat);
                    break;
                case "mute":
                    if (JsonBody.TryBoolValue(body, "muted", out var muted))
                        AudioVolumeMultiplier_Patch.SetVolumeMultiplier(muted ? 0f : 1f);
                    break;
                case "progress":
                    if (JsonBody.TryFloatValue(body, "progress", out var progress))
                        api.setProgress(progress);
                    break;
                case "playByIndex":
                    if (JsonBody.TryIntValue(body, "index", out var index))
                        api.playByIndex(index);
                    break;
                case "playByUuid":
                    api.playByUuid(JsonBody.StringValue(body, "uuid"));
                    break;
            }
        }
    }
}
