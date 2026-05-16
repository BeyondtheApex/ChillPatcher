using BepInEx.Logging;
using ChillPatcher.Integration;

namespace ChillPatcher.Module.LocalIntegration
{
    internal sealed class AIChatConversationHandler : ILocalIntegrationHandler
    {
        public string Method => "POST";
        public string Path => "/v1/aichat/chat";

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
            var text = JsonBody.StringValue(body, "text");
            var inputSource = JsonBody.StringValue(body, "inputSource");
            if (string.IsNullOrWhiteSpace(inputSource))
            {
                inputSource = "localintegration";
            }

            var ok = AIChatBridge.TryStartTextConversation(text, inputSource, out var error);
            if (!ok)
            {
                logger?.LogWarning($"[本地集成总线] aichat.chat 执行失败：{error}");
            }
        }
    }
}
