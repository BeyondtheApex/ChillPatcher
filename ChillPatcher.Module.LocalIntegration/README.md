# 本地集成总线

本地集成总线是一个可选的 ChillPatcher 模块，用于给可信的本地工具提供一个轻量 HTTP 入口。
它面向需要从本地脚本、桌面助手、快捷键面板、Stream Deck、直播工具或其他本机程序接收命令的第三方模块。

总线本身只负责通用基础设施：

- 在可配置的 Host 和 Port 上监听 HTTP 请求
- 提供可选的 bearer token 鉴权
- 按 HTTP method 和 path 匹配路由
- 将通过校验的请求派发回 Unity 主线程执行

具体功能由其他模块实现。模块只需要在自己的程序集中实现 `ChillPatcher.SDK.Interfaces.ILocalIntegrationHandler`，本地集成总线会在模块加载后自动发现这些 handler 并注册路由。

## Handler 示例

```csharp
using BepInEx.Logging;
using ChillPatcher.SDK.Interfaces;

public sealed class ExampleHandler : ILocalIntegrationHandler
{
    public string Method => "POST";
    public string Path => "/v1/example/ping";

    public bool TryValidate(string body, out string error)
    {
        error = "";
        return true;
    }

    public void Execute(string body, ManualLogSource logger)
    {
        logger.LogInfo("[Example] 收到本地集成请求");
    }
}
```

通过 handler 校验的请求会返回 `202 Accepted`，随后在 Unity 主线程中执行。

## 健康检查

```http
GET http://127.0.0.1:18792/health
```

响应：

```json
{ "ok": true }
```

## 配置

模块配置写入：

```ini
[Module:com.chillpatcher.localintegration]
```

- `Host`：监听地址，默认 `127.0.0.1`。
- `Port`：监听端口，默认 `18792`。
- `Token`：可选 token。设置后，请求必须携带 `Authorization: Bearer <token>` 或 `X-ChillPatcher-Token: <token>`。

除非明确需要局域网访问，否则建议保持 `Host = 127.0.0.1`。
