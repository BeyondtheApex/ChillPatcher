using BepInEx.Logging;

namespace ChillPatcher.Module.LocalIntegration
{
    internal sealed class AIChatSpeakHandler : ILocalIntegrationHandler
    {
        public string Method => "POST";
        public string Path => "/v1/aichat/speak";

        public bool TryValidate(string body, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(JsonBody.StringValue(body, "text")))
            {
                error = "缺少 text";
                return false;
            }
            return true;
        }

        public void Execute(string body, ManualLogSource logger)
        {
            var request = SpeakRequest.Parse(body);
            var ok = AIChatSpeakerAdapter.TrySpeakText(request.Text, request.Subtitle, request.Emotion, logger, out var error);
            if (!ok)
            {
                logger?.LogWarning($"[本地集成总线] aichat.speak 执行失败：{error}");
            }
        }

        private sealed class SpeakRequest
        {
            public string Text { get; private set; }
            public string Subtitle { get; private set; }
            public string Emotion { get; private set; }

            public static SpeakRequest Parse(string json)
            {
                var text = JsonBody.StringValue(json, "text");
                return new SpeakRequest
                {
                    Text = text,
                    Subtitle = JsonBody.StringValue(json, "subtitle"),
                    Emotion = NormalizeEmotion(JsonBody.StringValue(json, "emotion"))
                };
            }

            private static string NormalizeEmotion(string emotion)
            {
                switch ((emotion ?? "").Trim('[', ']', ' '))
                {
                    case "Happy":
                    case "Confused":
                    case "Sad":
                    case "Fun":
                    case "Agree":
                    case "Drink":
                    case "Wave":
                    case "Think":
                        return emotion.Trim('[', ']', ' ');
                    default:
                        return "Agree";
                }
            }
        }
    }
}
