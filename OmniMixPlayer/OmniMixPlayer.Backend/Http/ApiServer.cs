using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.Backend.ModuleSystem;
using OmniMixPlayer.SDK.Interfaces;
using ProtoEvents = OmniMixPlayer.SDK.Protos.Events;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Http
{
    /// <summary>
    /// WebSocket + 模块 UI API 服务器
    /// RESTful CRUD 已迁移到 gRPC，这里只保留 WebSocket 事件推送和模块 UI
    /// </summary>
    public class ApiServer
    {
        private readonly InstanceRegistry _registry;
        private readonly PlaybackSessionManager _sessions;
        private readonly ILibraryRegistry _libraryRegistry;
        private readonly ILogger _logger;
        private readonly List<WebSocket> _wsClients = new();
        private readonly object _wsLock = new();
        private readonly ConcurrentDictionary<WebSocket, SemaphoreSlim> _wsSendLocks = new();
        private ModuleUIHandler _moduleUIHandler;
        private GlobalConfigManager _globalConfig;

        public ApiServer(InstanceRegistry registry, PlaybackSessionManager sessions, ILibraryRegistry libraryRegistry, ILogger logger)
        {
            _registry = registry;
            _sessions = sessions;
            _libraryRegistry = libraryRegistry;
            _logger = logger;

            _sessions.OnTrackChanged += (id, track) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "track.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TrackChanged = new ProtoEvents.TrackChangedEvent { InstanceId = id, Uuid = track?.Uuid ?? "", Title = track?.Title ?? "", Artist = track?.Artist ?? "", AlbumId = track?.AlbumId ?? "", Duration = track?.Duration ?? 0, ModuleId = track?.ModuleId ?? "" }
                });
            _sessions.OnStateChanged += (id, ctrl) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "state.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    StateChanged = new ProtoEvents.StateChangedEvent { InstanceId = id, State = ctrl.IsPlaying ? 1 : (ctrl.CurrentTrack != null ? 2 : 0) }
                });
            _sessions.OnPositionChanged += (id, pos) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "position.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    PositionChanged = new ProtoEvents.PositionChangedEvent { InstanceId = id, Position = pos }
                });
            _sessions.OnQueueChanged += (id) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "queue.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    QueueChanged = new ProtoEvents.QueueChangedEvent { InstanceId = id }
                });

            void OnInstancesOrSessionsChanged()
            {
                var evt = new ProtoEvents.WsEvent { Type = "instances.changed", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                evt.InstancesChanged = new ProtoEvents.InstancesChangedEvent();
                evt.InstancesChanged.Instances.AddRange(_registry.ListSummaries(_sessions));
                _ = BroadcastProtoEvent(evt);
            }
            _sessions.OnSessionsChanged += OnInstancesOrSessionsChanged;
            _registry.OnChanged += OnInstancesOrSessionsChanged;

            _registry.OnVolumeChanged += (id, vol) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "volume.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    VolumeChanged = new ProtoEvents.VolumeChangedEvent { InstanceId = id, Volume = vol }
                });

            _registry.OnLatencyChanged += (id, lat) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "latency.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    LatencyChanged = new ProtoEvents.LatencyChangedEvent { InstanceId = id, Latency = lat }
                });

            _registry.OnEqualizerChanged += (id, eq) =>
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "eq.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    EqChanged = new ProtoEvents.EqualizerChangedEvent { InstanceId = id, State = eq }
                });
        }

        public void SetModuleUIHandler(ModuleUIHandler handler) => _moduleUIHandler = handler;
        public void SetGlobalConfig(GlobalConfigManager config) => _globalConfig = config;

        public void Configure(IEndpointRouteBuilder endpoints)
        {
            // WebSocket
            endpoints.Map("/ws", async (HttpContext ctx) =>
            {
                if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
                var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocket(ws);
            });

            // Module UI event
            endpoints.MapPost("/api/ui/event", async (HttpContext ctx) =>
            {
                if (_moduleUIHandler == null) { ctx.Response.StatusCode = 503; return; }
                using var reader = new StreamReader(ctx.Request.Body);
                var msg = await reader.ReadToEndAsync();
                await _moduleUIHandler.HandleUiEvent(msg);
            });

            // Global config (keep simple REST)
            endpoints.MapGet("/api/config", () =>
            {
                var config = _globalConfig?.GetAll() ?? new Dictionary<string, object>();
                return Results.Json(config);
            });
            endpoints.MapPut("/api/config", async (HttpContext ctx) =>
            {
                try
                {
                    using var reader = new StreamReader(ctx.Request.Body);
                    var json = await reader.ReadToEndAsync();
                    var doc = JsonDocument.Parse(json);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        _globalConfig?.SetValue<object>(prop.Name, ConvertElement(prop.Value));
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse config update"); return Results.BadRequest(new { error = ex.Message }); }
                return Results.Ok();
            });
            endpoints.MapPost("/api/config/save", () => { _globalConfig?.Save(); return Results.Ok(new { message = "Config saved" }); });

            // Health
            endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }));

            // Version
            endpoints.MapGet("/api/version", () => Results.Json(new { version = SDK.SDKInfo.SDK_VERSION, name = SDK.SDKInfo.SDK_NAME }));

            // Backend stop
            endpoints.MapPost("/api/backend/stop", () =>
            {
                _ = BroadcastProtoEvent(new ProtoEvents.WsEvent
                {
                    Type = "backend.state.changed",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    BackendState = new ProtoEvents.BackendStateEvent { Running = false }
                });
                _ = Task.Run(async () => { await Task.Delay(500); Environment.Exit(0); });
                return Results.Ok(new { message = "Shutting down" });
            });

            // ── Module REST endpoints (UI still JSON-based) ──

            // List modules
            endpoints.MapGet("/api/modules", () =>
            {
                var loader = _moduleUIHandler?.ModuleLoader;
                if (loader == null) return Results.Json(Array.Empty<object>());
                var modules = loader.LoadedModules.Select(m => new
                {
                    id = m.Module.ModuleId,
                    name = m.Module.DisplayName,
                    version = m.Module.Version,
                    priority = m.Module.Priority,
                    loadedAt = m.LoadedAt.ToString("o"),
                    enabled = loader.IsModuleEnabled(m.Module.ModuleId),
                    hasSettingsUI = (m.Module as IModuleUIProvider)?.HasSettingsUI ?? false,
                    hasQuickLinks = (m.Module as IModuleUIProvider)?.HasQuickLinks ?? false,
                    linkEntries = (m.Module as IModuleUIProvider)?.GetQuickLinks()
                        ?.Select(l => new
                        {
                            id = l.Id,
                            title = l.Title,
                            icon = l.Icon,
                            svg = l.Svg,
                            backgroundColor = l.BackgroundColor,
                            iconColor = l.IconColor
                        })
                        .Cast<object>()
                        ?? Enumerable.Empty<object>()
                });
                return Results.Json(modules);
            });

            // Get module UI
            endpoints.MapGet("/api/modules/{id}/ui", (string id) =>
            {
                var provider = _moduleUIHandler?.GetUIProvider(id);
                if (provider == null) return Results.NotFound();
                var tree = provider.BuildUI();
                tree?.FinalizeSources();
                return Results.Json(tree);
            });

            // Get module link UI
            endpoints.MapGet("/api/modules/{id}/link/{linkId}", (string id, string linkId) =>
            {
                var provider = _moduleUIHandler?.GetUIProvider(id);
                if (provider == null) return Results.NotFound();
                var tree = provider.BuildLinkUI(linkId);
                tree?.FinalizeSources();
                return tree != null ? Results.Json(tree) : Results.NotFound();
            });

            // Get module settings UI
            endpoints.MapGet("/api/modules/{id}/settings", (string id) =>
            {
                var provider = _moduleUIHandler?.GetUIProvider(id);
                if (provider == null) return Results.NotFound();
                var tree = provider.BuildSettingsUI();
                tree?.FinalizeSources();
                return tree != null ? Results.Json(tree) : Results.NotFound();
            });

            // Enable/disable module
            endpoints.MapPost("/api/modules/{id}", async (string id, HttpContext ctx) =>
            {
                var loader = _moduleUIHandler?.ModuleLoader;
                if (loader == null) return Results.Json(new { error = "Module loader not available" }, statusCode: 503);
                using var reader = new StreamReader(ctx.Request.Body);
                var json = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("enabled", out var enabledProp) && enabledProp.ValueKind == JsonValueKind.True || enabledProp.ValueKind == JsonValueKind.False)
                {
                    loader.SetModuleEnabled(id, enabledProp.GetBoolean());
                    return Results.Ok(new { success = true });
                }
                return Results.BadRequest(new { error = "Missing 'enabled' field" });
            });

            // Track cover proxy
            endpoints.MapGet("/api/track/cover", async (string uuid) =>
            {
                if (string.IsNullOrEmpty(uuid)) return Results.BadRequest();
                var (data, mimeType) = await GetCoverAsync(uuid);
                return data != null ? Results.Bytes(data, mimeType ?? "image/jpeg") : Results.NotFound();
            });

            // Module raw content — modules serve their own binary data (QR codes, etc.)
            endpoints.MapGet("/api/modules/{id}/content/{*path}", async (string id, string path) =>
            {
                var module = ModuleLoader.Instance?.GetModule(id);
                if (module is IModuleUIProvider uiProvider)
                {
                    var contentTask = uiProvider.ServeRawContent(path ?? "");
                    if (contentTask != null)
                    {
                        var content = await contentTask;
                        if (content != null)
                        {
                            var contentType = uiProvider.ServeRawContentType(path ?? "") ?? "application/octet-stream";
                            return Results.Bytes(content, contentType);
                        }
                    }
                }
                _logger.LogWarning("content: module {Id} path {Path} not found", id, path);
                return Results.NotFound();
            });

            // Image proxy — proxies base64-encoded image URLs
            endpoints.MapGet("/api/proxy/image", async (string url) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(url)) return Results.BadRequest();
                    _logger.LogInformation("Image proxy: raw query url={RawUrl}", url);
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(url)));
                    _logger.LogInformation("Image proxy: decoded url={DecodedUrl}", decoded);
                    using var http = new System.Net.Http.HttpClient();
                    var response = await http.GetAsync(decoded);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Image proxy: HTTP {StatusCode} for {Url}", response.StatusCode, decoded);
                        return Results.NotFound();
                    }
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var mime = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    _logger.LogInformation("Image proxy: OK {Mime} {Length} bytes", mime, bytes.Length);
                    return Results.Bytes(bytes, mime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Image proxy: Failed for {Url}", url);
                    return Results.NotFound();
                }
            });
        }

        private async Task HandleWebSocket(WebSocket ws)
        {
            lock (_wsLock) _wsClients.Add(ws);
            _wsSendLocks[ws] = new SemaphoreSlim(1, 1);

            try
            {
                var buffer = new byte[4096];
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (_moduleUIHandler != null)
                            await _moduleUIHandler.HandleUiEvent(msg);
                    }
                }
            }
            catch (WebSocketException) { }
            finally
            {
                lock (_wsLock) _wsClients.Remove(ws);
                if (_wsSendLocks.TryRemove(ws, out var sem)) sem.Dispose();
                if (ws.State == WebSocketState.Open)
                {
                    try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
                    catch { }
                }
                ws.Dispose();
            }
        }

        public async Task BroadcastProtoEvent(ProtoEvents.WsEvent evt)
        {
            var bytes = evt.ToByteArray();

            List<WebSocket> clients;
            lock (_wsLock) clients = _wsClients.ToList();

            foreach (var ws in clients)
            {
                if (ws.State != WebSocketState.Open) continue;
                if (_wsSendLocks.TryGetValue(ws, out var sem))
                {
                    await sem.WaitAsync();
                    try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None); }
                    catch { }
                    finally { sem.Release(); }
                }
            }
        }

        /// <summary>For UI push and other non-proto events, still use JSON text.</summary>
        public async Task BroadcastJsonEvent(string eventType, object data)
        {
            var payload = JsonSerializer.Serialize(new { type = eventType, data, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            var bytes = Encoding.UTF8.GetBytes(payload);

            List<WebSocket> clients;
            lock (_wsLock) clients = _wsClients.ToList();

            foreach (var ws in clients)
            {
                if (ws.State != WebSocketState.Open) continue;
                if (_wsSendLocks.TryGetValue(ws, out var sem))
                {
                    await sem.WaitAsync();
                    try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch { }
                    finally { sem.Release(); }
                }
            }
        }

        private static object ConvertElement(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number when el.TryGetInt32(out var i) => i,
            JsonValueKind.Number when el.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => el.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => ConvertElement(p.Value)),
            _ => null
        };

        internal string GetClientId(HttpContext ctx) => ctx.Connection.Id;

        public async Task<(byte[] data, string mimeType)> GetCoverAsync(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return (null, null);

            var track = _libraryRegistry.GetTrack(uuid);
            if (track == null || string.IsNullOrEmpty(track.ModuleId)) return (null, null);

            var provider = ModuleLoader.Instance?.GetProvider<ICoverProvider>(track.ModuleId);
            if (provider == null) return (null, null);

            try
            {
                return await provider.GetMusicCoverAsync(uuid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cover for uuid {Uuid}", uuid);
                return (null, null);
            }
        }
    }
}
