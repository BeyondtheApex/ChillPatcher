using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.Backend.ModuleSystem;
using OmniMixPlayer.Backend.ModuleSystem.Registry;
using OmniMixPlayer.SDK.Events;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.Backend.Http
{
    public class ApiServer
    {
        private readonly PlaybackInstanceManager _instances;
        private readonly ModuleLoader _moduleLoader;
        private readonly TagRegistry _tagRegistry;
        private readonly AlbumRegistry _albumRegistry;
        private readonly MusicRegistry _musicRegistry;
        private readonly ILogger _logger;
        private readonly List<WebSocket> _wsClients = new List<WebSocket>();
        private readonly object _wsLock = new object();
        private readonly ConcurrentDictionary<WebSocket, SemaphoreSlim> _wsSendLocks = new ConcurrentDictionary<WebSocket, SemaphoreSlim>();
        private ModuleUIHandler _moduleUIHandler;
        private GlobalConfigManager _globalConfig;

        public ApiServer(PlaybackInstanceManager instances, ModuleLoader moduleLoader,
            TagRegistry tagRegistry, AlbumRegistry albumRegistry, MusicRegistry musicRegistry,
            ILogger logger)
        {
            _instances = instances;
            _moduleLoader = moduleLoader;
            _tagRegistry = tagRegistry;
            _albumRegistry = albumRegistry;
            _musicRegistry = musicRegistry;
            _logger = logger;

            _instances.OnTrackChanged += OnInstanceTrackChanged;
            _instances.OnStateChanged += OnInstanceStateChanged;
            _instances.OnPositionChanged += OnInstancePositionChanged;
            _instances.OnQueueChanged += OnInstanceQueueChanged;
            _instances.OnInstancesChanged += OnInstancesChanged;
        }

        public void SetModuleUIHandler(ModuleUIHandler handler)
        {
            _moduleUIHandler = handler;
        }

        public void SetGlobalConfig(GlobalConfigManager config)
        {
            _globalConfig = config;
        }

        public void Configure(IEndpointRouteBuilder endpoints)
        {
            // Playback instances
            endpoints.MapGet("/api/instances", () => Results.Json(_instances.ListInstanceDtos()));
            endpoints.MapGet("/api/instances/stats", () => Results.Json(_instances.GetStats()));
            endpoints.MapPost("/api/instances/connect", (InstanceConnectRequest req) =>
            {
                if (string.IsNullOrWhiteSpace(req.clientId))
                    return Results.BadRequest(new { error = "clientId is required" });
                var role = ParseRole(req.role);
                var mode = req.mode?.ToLowerInvariant() == "client"
                    ? PlaybackMode.ClientManaged : PlaybackMode.ServerManaged;
                return Results.Json(_instances.Connect(req.clientId, role, mode));
            });
            endpoints.MapPost("/api/instances/{id}/heartbeat", (string id) =>
                _instances.Heartbeat(id) ? Results.Ok(new { alive = true }) : Results.NotFound(new { alive = false }));
            endpoints.MapPost("/api/instances/{id}/disconnect", (string id) =>
                _instances.Disconnect(id) ? Results.Ok(new { disconnected = true }) : Results.NotFound());
            endpoints.MapDelete("/api/instances/{id}", (string id) =>
                _instances.Delete(id) ? Results.Ok(new { deleted = true }) : Results.NotFound());
            endpoints.MapGet("/api/instances/{id}/status", (string id) =>
            {
                var instance = _instances.Get(id);
                return instance != null ? Results.Json(instance.Controller.GetStatus()) : Results.NotFound();
            });
            endpoints.MapPost("/api/instances/{id}/play", (string id, PlayRequest req) =>
            {
                var instance = _instances.Get(id);
                if (instance == null) return Results.NotFound();
                instance.Controller.Play(req.uuid);
                return Results.Ok();
            });
            endpoints.MapPost("/api/instances/{id}/pause", (string id) => WithInstance(id, p => p.Pause()));
            endpoints.MapPost("/api/instances/{id}/resume", (string id) => WithInstance(id, p => p.Resume()));
            endpoints.MapPost("/api/instances/{id}/toggle", (string id) => WithInstance(id, p => p.Toggle()));
            endpoints.MapPost("/api/instances/{id}/next", (string id) => WithInstance(id, p => p.Next()));
            endpoints.MapPost("/api/instances/{id}/prev", (string id) => WithInstance(id, p => p.Prev()));
            endpoints.MapPost("/api/instances/{id}/seek", (string id, SeekRequest req) => WithInstance(id, p => p.Seek(req.position)));
            endpoints.MapPut("/api/instances/{id}/volume", (string id, VolumeRequest req) => WithInstance(id, p => p.SetVolume(req.volume)));
            endpoints.MapPost("/api/instances/{id}/shuffle", (string id, ShuffleRequest req) => WithInstance(id, p => p.SetShuffle(req.enabled)));
            endpoints.MapPost("/api/instances/{id}/repeat", (string id, RepeatRequest req) =>
            {
                if (!Enum.TryParse<SDK.Interfaces.RepeatMode>(req.mode, true, out var rm))
                    return Results.BadRequest(new { error = "Invalid repeat mode" });
                return WithInstance(id, p => p.SetRepeatMode(rm));
            });

            // Playlist
            endpoints.MapGet("/api/playlist", GetPlaylist);
            endpoints.MapGet("/api/tags", () => Results.Json(_tagRegistry.GetAllTags().Select(t => new { id = t.TagId ?? "", name = t.DisplayName ?? "", moduleId = t.ModuleId ?? "", bitValue = t.BitValue, isGrowable = t.IsGrowableList })));
            endpoints.MapGet("/api/albums", (string? tagId) => Results.Json(
                string.IsNullOrEmpty(tagId)
                    ? _albumRegistry.GetAllAlbums().Select(a => new { id = a.AlbumId ?? "", name = a.DisplayName ?? "", tagId = (a.TagIds?.FirstOrDefault() ?? ""), moduleId = a.ModuleId ?? "", coverPath = a.CoverPath ?? "", songCount = a.SongCount, isGrowable = a.IsGrowableAlbum })
                    : _albumRegistry.GetAlbumsByTag(tagId).Select(a => new { id = a.AlbumId ?? "", name = a.DisplayName ?? "", tagId = (a.TagIds?.FirstOrDefault() ?? ""), moduleId = a.ModuleId ?? "", coverPath = a.CoverPath ?? "", songCount = a.SongCount, isGrowable = a.IsGrowableAlbum })
            ));
            endpoints.MapGet("/api/songs", GetSongs);
            endpoints.MapGet("/api/song/{uuid}", (string uuid) => { var m = _musicRegistry.GetMusic(uuid); return m != null ? Results.Json(MapSong(m)) : Results.NotFound(); });

            // Instance queue
            endpoints.MapGet("/api/instances/{id}/queue", (string id) =>
            {
                var instance = _instances.Get(id);
                return instance != null
                    ? Results.Json(instance.Controller.Queue.Select((m, i) => new
                    {
                        index = i,
                        uuid = m.UUID,
                        title = m.Title,
                        artist = m.Artist,
                        albumId = m.AlbumId,
                        duration = m.Duration,
                        moduleId = m.ModuleId
                    }))
                    : Results.NotFound();
            });
            endpoints.MapGet("/api/instances/{id}/playlist", (string id) =>
            {
                var instance = _instances.Get(id);
                return instance != null
                    ? Results.Json(instance.Controller.Playlist.Select((m, i) => new { index = i, uuid = m.UUID, title = m.Title, artist = m.Artist, albumId = m.AlbumId, moduleId = m.ModuleId }))
                    : Results.NotFound();
            });
            endpoints.MapGet("/api/instances/{id}/playlist/sources", (string id) =>
            {
                var instance = _instances.Get(id);
                return instance != null ? Results.Json(instance.Controller.PlaylistSources) : Results.NotFound();
            });
            endpoints.MapPut("/api/instances/{id}/playlist", (string id, QueueReplaceRequest req) => WithInstance(id, p => p.SetPlaylist(req.uuids ?? Array.Empty<string>())));
            endpoints.MapPut("/api/instances/{id}/playlist/sources", (string id, PlaylistSourcesReplaceRequest req) => WithInstance(id, p => p.SetPlaylistSources(req.sources ?? Array.Empty<PlaylistSourceRequest>())));
            endpoints.MapPost("/api/instances/{id}/playlist/sources", (string id, PlaylistSourceInsertRequest req) => WithInstance(id, p => p.InsertPlaylistSource(req.source, req.index)));
            endpoints.MapDelete("/api/instances/{id}/playlist/sources/{sourceId}", (string id, string sourceId) => WithInstance(id, p => p.RemovePlaylistSource(sourceId)));
            endpoints.MapPost("/api/instances/{id}/queue", (string id, PlayRequest req) => WithInstance(id, p => p.AddToQueue(req.uuid)));
            endpoints.MapPost("/api/instances/{id}/queue/insert", (string id, QueueInsertRequest req) => WithInstance(id, p => p.InsertIntoQueue(req.uuids ?? Array.Empty<string>(), req.index)));
            endpoints.MapPut("/api/instances/{id}/queue", (string id, QueueReplaceRequest req) => WithInstance(id, p => p.SetQueue(req.uuids ?? Array.Empty<string>())));
            endpoints.MapPost("/api/instances/{id}/queue/replace", (string id, QueueReplaceRequest req) => WithInstance(id, p => p.SetQueue(req.uuids ?? Array.Empty<string>())));
            endpoints.MapDelete("/api/instances/{id}/queue/{index}", (string id, int index) => WithInstance(id, p => p.RemoveFromQueue(index)));
            endpoints.MapDelete("/api/instances/{id}/queue/by-uuid/{uuid}", (string id, string uuid) => WithInstance(id, p => p.RemoveFromQueue(uuid)));
            endpoints.MapPost("/api/instances/{id}/queue/move", (string id, MoveRequest req) => WithInstance(id, p => p.MoveInQueue(req.from, req.to)));
            endpoints.MapPost("/api/instances/{id}/queue/clear", (string id) => WithInstance(id, p => p.ClearQueue()));
            endpoints.MapGet("/api/instances/{id}/history", (string id) =>
            {
                var instance = _instances.Get(id);
                return instance != null
                    ? Results.Json(instance.Controller.History.Select((m, i) => new
                    {
                        index = i,
                        uuid = m.UUID,
                        title = m.Title,
                        artist = m.Artist,
                        albumId = m.AlbumId,
                        duration = m.Duration,
                        moduleId = m.ModuleId
                    }))
                    : Results.NotFound();
            });
            endpoints.MapPost("/api/instances/{id}/history/insert", (string id, QueueInsertRequest req) => WithInstance(id, p => p.InsertIntoHistory(req.uuids ?? Array.Empty<string>(), req.index)));
            endpoints.MapDelete("/api/instances/{id}/history/{index}", (string id, int index) => WithInstance(id, p => p.RemoveFromHistory(index)));
            endpoints.MapDelete("/api/instances/{id}/history/by-uuid/{uuid}", (string id, string uuid) => WithInstance(id, p => p.RemoveFromHistory(uuid)));
            endpoints.MapPost("/api/instances/{id}/history/move", (string id, MoveRequest req) => WithInstance(id, p => p.MoveInHistory(req.from, req.to)));
            endpoints.MapPost("/api/instances/{id}/history/clear", (string id) => WithInstance(id, p => p.ClearHistory()));

            // Growable tags (paginated loading)
            endpoints.MapGet("/api/tags/growable", () =>
            {
                var tags = _tagRegistry.GetGrowableTags();
                return Results.Json(tags.Select(t => new { tagId = t.TagId, name = t.DisplayName, moduleId = t.ModuleId, bitValue = t.BitValue, isGrowable = t.IsGrowableList }));
            });
            endpoints.MapPost("/api/tags/{tagId}/load-more", async (string tagId) =>
            {
                var tag = _tagRegistry.GetTag(tagId);
                if (tag == null || tag.LoadMoreCallback == null)
                    return Results.NotFound();
                try
                {
                    var loadedCount = await tag.LoadMoreCallback();
                    _ = BroadcastEvent("playlist.updated", new { tagId, updateType = "growable", changedCount = loadedCount });
                    return Results.Json(new { tagId, loadedCount });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoadMore failed for tag {TagId}", tagId);
                    return Results.Json(new { tagId, loadedCount = 0 });
                }
            });
            endpoints.MapPost("/api/tags/{tagId}/activate", (string tagId) =>
            {
                if (!string.IsNullOrEmpty(tagId))
                    _tagRegistry.SetCurrentGrowableTag(tagId);
                else
                    _tagRegistry.SetCurrentGrowableTag(null);
                return Results.Ok();
            });

            // Favorites
            endpoints.MapPost("/api/favorite", (FavoriteRequest req) => SetFavorite(req.uuid, req.isFavorite) ? Results.Ok() : Results.NotFound());

            // Exclude
            endpoints.MapPost("/api/exclude", (FavoriteRequest req) =>
            {
                var ok = SetExcluded(req.uuid, req.isFavorite);
                if (ok) _ = BroadcastEvent("exclude.changed", new { uuid = req.uuid, isExcluded = req.isFavorite });
                return ok ? Results.Ok() : Results.NotFound();
            });

            // Modules
            endpoints.MapGet("/api/modules", () => Results.Json(_moduleLoader.LoadedModules.Select(m =>
            {
                var uiProvider = m.Module as IModuleUIProvider;
                var hasSettingsUI = uiProvider != null;
                var hasQuickLinks = uiProvider?.HasQuickLinks ?? false;
                var linkEntries = new List<object>();
                if (hasQuickLinks)
                {
                    linkEntries = uiProvider.GetQuickLinks().Select(le => (object)new
                    {
                        id = le.Id,
                        title = le.Title,
                        icon = le.Icon,
                        svg = le.Svg ?? "",
                        backgroundColor = le.BackgroundColor,
                        iconColor = le.IconColor
                    }).ToList();
                }
                var isEnabled = _moduleLoader.IsModuleEnabled(m.Module.ModuleId);
                return new
                {
                    id = m.Module.ModuleId,
                    name = m.Module.DisplayName,
                    version = m.Module.Version,
                    priority = m.Module.Priority,
                    loadedAt = m.LoadedAt.ToString("O"),
                    enabled = isEnabled,
                    hasSettingsUI,
                    hasQuickLinks,
                    linkEntries
                };
            })));
            endpoints.MapPost("/api/modules/{id}", (string id, ModuleToggleRequest req) =>
            {
                SetModuleEnabled(id, req.enabled);
                _ = BroadcastEvent("module.changed", new { moduleId = id, enabled = req.enabled });
                return Results.Ok();
            });
            endpoints.MapGet("/api/version", () => Results.Json(new { version = SDK.SDKInfo.SDK_VERSION, name = SDK.SDKInfo.SDK_NAME }));

            // Health
            endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }));

            // Backend control
            endpoints.MapPost("/api/backend/stop", () =>
            {
                _logger.LogInformation("Backend stop requested via API");
                _ = BroadcastEvent("backend.state.changed", new { running = false });
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    Environment.Exit(0);
                });
                return Results.Ok(new { message = "Shutting down" });
            });

            // Global config
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
                    {
                        _globalConfig?.SetValue<object>(prop.Name, ConvertElement(prop.Value));
                    }
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse config update");
                    return Results.BadRequest(new { error = ex.Message });
                }
            });
            endpoints.MapPost("/api/config/save", () =>
            {
                _globalConfig?.Save();
                return Results.Ok(new { message = "Config saved" });
            });

            // Instance profile (persisted per-instance config for offline management)
            endpoints.MapGet("/api/instances/{id}/profile", (string id) =>
            {
                var instance = _instances.Get(id);
                if (instance == null) return Results.NotFound();
                var profile = instance.Controller.GetProfile();
                return Results.Json(profile);
            });
            endpoints.MapPut("/api/instances/{id}/profile", async (string id, HttpContext ctx) =>
            {
                var instance = _instances.Get(id);
                if (instance == null) return Results.NotFound();
                try
                {
                    using var reader = new StreamReader(ctx.Request.Body);
                    var json = await reader.ReadToEndAsync();
                    var ok = instance.Controller.UpdateProfile(json);
                    return ok ? Results.Ok() : Results.Problem("Failed to persist profile", statusCode: 500);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update profile for instance {Id}", id);
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            // Image proxy (CORS / anti-hotlinking)
            endpoints.MapGet("/api/proxy/image", async (string url) =>
            {
                try
                {
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
                    _logger.LogWarning(ex, "Image proxy failed for url={RawUrl}", url);
                    return Results.NotFound();
                }
            });

            // Module Link UI
            endpoints.MapGet("/api/modules/{id}/link/{linkId}", (string id, string linkId) =>
            {
                var module = _moduleLoader.GetModule(id);
                if (module is IModuleUIProvider uiProvider)
                {
                    try
                    {
                        var tree = uiProvider.BuildLinkUI(linkId);
                        if (tree == null)
                            return Results.Json(new { id = "root", nodeType = "Text", text = "No UI available for this link" });
                        tree.FinalizeSources();
                        return Results.Json(tree);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to build link UI for module {ModuleId}, link {LinkId}", id, linkId);
                        return Results.Json(new { id = "root", nodeType = "Text", text = "Error building link UI" });
                    }
                }
                return Results.Json(new { id = "root", nodeType = "Text", text = "This module has no UI panel" });
            });

            // Module Settings UI
            endpoints.MapGet("/api/modules/{id}/settings", (string id) =>
            {
                var module = _moduleLoader.GetModule(id);
                if (module is IModuleUIProvider uiProvider && uiProvider.HasSettingsUI)
                {
                    try
                    {
                        var tree = uiProvider.BuildSettingsUI();
                        if (tree == null)
                            return Results.Json(new { id = "root", nodeType = "Text", text = "No settings UI available" });
                        tree.FinalizeSources();
                        return Results.Json(tree);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to build settings UI for module {ModuleId}", id);
                        return Results.Json(new { id = "root", nodeType = "Text", text = "Error building settings UI" });
                    }
                }
                return Results.NotFound();
            });

            // Module UI
            endpoints.MapGet("/api/modules/{id}/ui", (string id) =>
            {
                var module = _moduleLoader.GetModule(id);
                if (module is IModuleUIProvider uiProvider)
                {
                    var tree = uiProvider.BuildUI();
                    if (tree == null)
                        return Results.Json(new { id = "root", nodeType = "Text", text = "No UI available" });
                    tree.FinalizeSources();
                    return Results.Json(tree);
                }
                return Results.Json(new { id = "root", nodeType = "Text", text = "This module has no UI panel" });
            });

            // Module raw content 鈥?modules serve their own binary data (QR codes, etc.)
            endpoints.MapGet("/api/modules/{id}/content/{*path}", async (string id, string path) =>
            {
                var module = _moduleLoader.GetModule(id);
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

            // Track cover proxy
            endpoints.MapGet("/api/track/cover", async (string uuid) =>
            {
                if (string.IsNullOrEmpty(uuid)) return Results.BadRequest();
                var (data, mimeType) = await GetCoverAsync(uuid);
                return data != null ? Results.Bytes(data, mimeType ?? "image/jpeg") : Results.NotFound();
            });

            // Lyrics
            endpoints.MapGet("/api/lyric/{uuid}", (string uuid) =>
            {
                if (string.IsNullOrEmpty(uuid)) return Results.BadRequest();
                var lyricData = GetLyricAsync(uuid);
                if (lyricData == null) return Results.NotFound();
                _ = BroadcastEvent("lyric.fetched", new { uuid });
                return Results.Json(lyricData);
            });

            // WebSocket
            endpoints.Map("/ws", async (HttpContext ctx) => await HandleWebSocket(ctx));
        }

        private IResult GetPlaylist()
        {
            var tags = _tagRegistry.GetAllTags().Select(t => new { id = t.TagId ?? "", name = t.DisplayName ?? "", moduleId = t.ModuleId ?? "" });
            var albums = _albumRegistry.GetAllAlbums().Select(a => new { id = a.AlbumId ?? "", name = a.DisplayName ?? "", tagId = (a.TagIds?.FirstOrDefault() ?? ""), moduleId = a.ModuleId ?? "", coverPath = a.CoverPath ?? "", songCount = a.SongCount });
            var songs = _musicRegistry.GetAllMusic().Select(MapSong);
            return Results.Json(new { tags, albums, songs });
        }

        private IResult GetSongs(string albumId, string tagId)
        {
            var songs = !string.IsNullOrEmpty(albumId) ? _musicRegistry.GetMusicByAlbum(albumId)
                : !string.IsNullOrEmpty(tagId) ? _musicRegistry.GetMusicByTag(tagId)
                : _musicRegistry.GetAllMusic();
            return Results.Json(songs.Select(MapSong));
        }

        private static object MapSong(SDK.Models.MusicInfo m)
        {
            return new
            {
                uuid = m.UUID,
                title = m.Title ?? "",
                artist = m.Artist ?? "",
                albumId = m.AlbumId ?? "",
                tagIds = m.TagIds?.ToArray() ?? Array.Empty<string>(),
                duration = m.Duration,
                moduleId = m.ModuleId ?? "",
                coverUrl = m.CoverUrl ?? "",
                sourceType = m.SourceType.ToString(),
                isFavorite = m.IsFavorite,
                isExcluded = m.IsExcluded,
                playCount = m.PlayCount,
                isUnlocked = m.IsUnlocked,
                sourcePath = m.SourcePath ?? ""
            };
        }

        public async Task<(byte[] data, string mimeType)> GetCoverAsync(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return (null, null);

            var music = _musicRegistry.GetMusic(uuid);
            if (music == null || string.IsNullOrEmpty(music.ModuleId)) return (null, null);

            var provider = _moduleLoader.GetProvider<ICoverProvider>(music.ModuleId);
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

        public object GetLyricAsync(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            var music = _musicRegistry.GetMusic(uuid);
            if (music == null || string.IsNullOrEmpty(music.ModuleId)) return null;

            var provider = _moduleLoader.GetProvider<ILyricProvider>(music.ModuleId);
            if (provider == null) return null;

            try
            {
                var lyricText = provider.GetLyric(uuid);
                if (lyricText != null)
                {
                    return new { uuid, lrc = lyricText, tlyric = (string)null, rlyric = (string)null };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get lyric for uuid {Uuid}", uuid);
            }

            return null;
        }

        private void SetModuleEnabled(string moduleId, bool enabled)
        {
            try
            {
                _moduleLoader.SetModuleEnabled(moduleId, enabled);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set enabled state for module {ModuleId}", moduleId);
            }
        }

        private IResult WithInstance(string id, Action<PlaybackController> action)
        {
            var instance = _instances.Get(id);
            if (instance == null) return Results.NotFound();
            action(instance.Controller);
            return Results.Ok();
        }

        private static PlaybackClientRole ParseRole(string role)
        {
            return role?.ToLowerInvariant() switch
            {
                "controller" => PlaybackClientRole.Controller,
                "observer" => PlaybackClientRole.Observer,
                _ => PlaybackClientRole.Audio
            };
        }

        private bool SetFavorite(string uuid, bool isFavorite)
        {
            var m = _musicRegistry.GetMusic(uuid);
            if (m == null) return false;
            m.IsFavorite = isFavorite;
            _musicRegistry.UpdateMusic(m);

            if (!string.IsNullOrEmpty(m.ModuleId))
            {
                var module = _moduleLoader.GetModule(m.ModuleId);
                if (module != null && module.Capabilities.CanFavorite)
                {
                    var handler = _moduleLoader.GetProvider<IFavoriteExcludeHandler>(m.ModuleId);
                    handler?.SetFavorite(uuid, isFavorite);
                }
            }
            return true;
        }

        private bool SetExcluded(string uuid, bool isExcluded)
        {
            var m = _musicRegistry.GetMusic(uuid);
            if (m == null) return false;
            m.IsExcluded = isExcluded;
            _musicRegistry.UpdateMusic(m);

            if (!string.IsNullOrEmpty(m.ModuleId))
            {
                var module = _moduleLoader.GetModule(m.ModuleId);
                if (module != null && module.Capabilities.CanExclude)
                {
                    var handler = _moduleLoader.GetProvider<IFavoriteExcludeHandler>(m.ModuleId);
                    handler?.SetExcluded(uuid, isExcluded);
                }
            }
            return true;
        }

        private void OnInstanceTrackChanged(string instanceId, SDK.Models.MusicInfo track)
        {
            _ = BroadcastEvent("track.changed", new
            {
                instanceId,
                uuid = track?.UUID,
                title = track?.Title,
                artist = track?.Artist,
                albumId = track?.AlbumId,
                duration = track?.Duration ?? 0,
                moduleId = track?.ModuleId
            });
        }

        private void OnInstanceStateChanged(string instanceId, PlaybackController playback)
        {
            _ = BroadcastEvent("state.changed", new
            {
                instanceId,
                isPlaying = playback.IsPlaying,
                position = playback.Position,
                volume = playback.Volume,
                repeatMode = playback.RepeatMode,
                shuffle = playback.Shuffle
            });
        }

        private void OnInstancePositionChanged(string instanceId, float position)
        {
            _ = BroadcastEvent("position", new { instanceId, position });
        }

        private void OnInstanceQueueChanged(string instanceId)
        {
            _ = BroadcastEvent("queue.changed", new { instanceId });
        }

        private void OnInstancesChanged()
        {
            _ = BroadcastEvent("instances.changed", _instances.ListInstanceDtos());
        }

        private async Task HandleWebSocket(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
            var ws = await context.WebSockets.AcceptWebSocketAsync();
            lock (_wsLock) { _wsClients.Add(ws); _wsSendLocks[ws] = new SemaphoreSlim(1, 1); }

            try
            {
                var buffer = new byte[4096];
                var messageBuffer = new StringBuilder();
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        if (result.EndOfMessage)
                        {
                            var message = messageBuffer.ToString();
                            messageBuffer.Clear();

                            try
                            {
                                if (message.Contains("\"type\":\"ui_event\""))
                                {
                                    await _moduleUIHandler?.HandleUiEvent(message);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                lock (_wsLock) _wsClients.Remove(ws);
                _wsSendLocks.TryRemove(ws, out var sendLock);
                sendLock?.Dispose();
            }
        }

        public async Task BroadcastEvent(string eventType, object data)
        {
            var msg = JsonSerializer.Serialize(new { type = eventType, @event = eventType, data, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            var bytes = Encoding.UTF8.GetBytes(msg);
            var segment = new ArraySegment<byte>(bytes);

            List<WebSocket> sockets;
            lock (_wsLock) { sockets = new List<WebSocket>(_wsClients); }

            var dead = new List<WebSocket>();
            foreach (var ws in sockets)
            {
                if (!_wsSendLocks.TryGetValue(ws, out var sendLock)) continue;
                await sendLock.WaitAsync();
                try
                {
                    if (ws.State == WebSocketState.Open)
                        await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { dead.Add(ws); }
                finally
                {
                    sendLock.Release();
                }
            }
            if (dead.Count > 0)
            {
                lock (_wsLock)
                {
                    foreach (var ws in dead)
                    {
                        _wsClients.Remove(ws);
                        if (_wsSendLocks.TryRemove(ws, out var deadLock))
                            deadLock.Dispose();
                    }
                }
            }
        }

        private static object ConvertElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String: return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var i)) return i;
                    if (element.TryGetDouble(out var d)) return d;
                    return element.GetRawText();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                default: return element.GetRawText();
            }
        }
    }

    // Request DTOs
    public record PlayRequest(string uuid);
    public record SeekRequest(float position);
    public record VolumeRequest(float volume);
    public record ShuffleRequest(bool enabled);
    public record RepeatRequest(string mode);
    public record MoveRequest(int from, int to);
    public record QueueReplaceRequest(string[] uuids);
    public record QueueInsertRequest(string[] uuids, int index);
    public record PlaylistSourcesReplaceRequest(PlaylistSourceRequest[] sources);
    public record PlaylistSourceInsertRequest(PlaylistSourceRequest source, int index);
    public record FavoriteRequest(string uuid, bool isFavorite);
    public record ModuleToggleRequest(bool enabled);
    public record InstanceConnectRequest(string clientId, string role, string mode);
}

