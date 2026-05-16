using BepInEx.Logging;
using ChillPatcher.JSApi;

namespace ChillPatcher.Module.LocalIntegration
{
    internal sealed class PomodoroControlHandler : ILocalIntegrationHandler
    {
        public string Method => "POST";
        public string Path => "/v1/pomodoro/control";

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
                case "start":
                case "togglePause":
                case "skip":
                case "reset":
                case "completeNow":
                case "moveAhead":
                case "setWorkMinutes":
                case "setBreakMinutes":
                case "setLoopCount":
                    return true;
                default:
                    error = "未知的番茄钟动作";
                    return false;
            }
        }

        public void Execute(string body, ManualLogSource logger)
        {
            var action = JsonBody.StringValue(body, "action");
            using (var api = new ChillGameApi(logger))
            {
                switch (action)
                {
                    case "start":
                        api.startPomodoro();
                        break;
                    case "togglePause":
                        api.togglePomodoroPause();
                        break;
                    case "skip":
                        api.skipPomodoroPhase();
                        break;
                    case "reset":
                        api.resetPomodoro();
                        break;
                    case "completeNow":
                        api.completePomodoroNow();
                        break;
                    case "moveAhead":
                        if (JsonBody.TryFloatValue(body, "seconds", out var seconds))
                            api.moveAheadPomodoro(seconds);
                        break;
                    case "setWorkMinutes":
                        if (JsonBody.TryIntValue(body, "minutes", out var workMinutes))
                            api.setWorkMinutes(workMinutes);
                        break;
                    case "setBreakMinutes":
                        if (JsonBody.TryIntValue(body, "minutes", out var breakMinutes))
                            api.setBreakMinutes(breakMinutes);
                        break;
                    case "setLoopCount":
                        if (JsonBody.TryIntValue(body, "loopCount", out var loopCount))
                            api.setLoopCount(loopCount);
                        break;
                }
            }
        }
    }
}
