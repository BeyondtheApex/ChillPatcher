# Local Integration Bus

Local Integration Bus is an optional ChillPatcher module that gives trusted local tools a small HTTP entry point.
It is intended for modules that need to receive commands from local scripts, desktop helpers, macro panels, stream tools, or other software running on the user's machine.

The bus owns only the shared infrastructure:

- HTTP listening on a configurable host and port
- optional bearer-token authentication
- route lookup
- dispatching accepted requests back to the Unity main thread

Feature modules provide the actual behavior by implementing `ChillPatcher.SDK.Interfaces.ILocalIntegrationHandler` in their own assembly.
When modules are loaded, the bus discovers those handlers and registers their routes.

## Handler Example

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
        logger.LogInfo("[Example] local integration request received");
    }
}
```

Requests accepted by a handler return `202 Accepted` and are queued for main-thread execution.

## Health Check

```http
GET http://127.0.0.1:18792/health
```

Response:

```json
{ "ok": true }
```

## Configuration

The module writes its settings under:

```ini
[Module:com.chillpatcher.localintegration]
```

- `Host`: listen address. Defaults to `127.0.0.1`.
- `Port`: listen port. Defaults to `18792`.
- `Token`: optional token. When set, callers must send `Authorization: Bearer <token>` or `X-ChillPatcher-Token: <token>`.

Keep `Host` set to `127.0.0.1` unless LAN access is explicitly required.
