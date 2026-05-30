using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChillPatcher.SDK.Ipc
{
    #region Event Args

    public class TrackChangedEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public float Duration { get; set; }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public bool IsPlaying { get; set; }
        public float Position { get; set; }
        public float Volume { get; set; }
    }

    public class QueueChangedEventArgs : EventArgs
    {
    }

    public class PositionEventArgs : EventArgs
    {
        public float Position { get; set; }
    }

    public class ModuleChangedEventArgs : EventArgs
    {
        public string ModuleId { get; set; }
        public bool Enabled { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class LyricFetchedEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public string Lrc { get; set; }
        public string Tlyric { get; set; }
        public string Rlyric { get; set; }
    }

    public class LyricPositionEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public int LineIndex { get; set; }
        public float TimeMs { get; set; }
    }

    public class PlaylistUpdatedEventArgs : EventArgs
    {
    }

    public class ExcludeChangedEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public bool IsExcluded { get; set; }
    }

    #endregion

    /// <summary>
    /// HTTP + WebSocket client that communicates with the OmniMixPlayer backend.
    /// Supports TCP (primary) and Unix Domain Socket (fallback).
    /// Discovery: port file → default port (17890) → socket.
    /// </summary>
    public class OmniMixPlayerClient : IDisposable
    {
        private const int DefaultPort = 17890;
        private const string DummyHost = "http://unix";
        private const string ApiPath = "/api";
        private const string WsPath = "/ws";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private readonly int _port;
        private readonly string _socketPath;
        private readonly bool _useSocket;
        private readonly HttpClient _http;
        private Socket _wsSocket;
        private ClientWebSocket _ws;
        private CancellationTokenSource _wsCts;
        private Task _wsTask;
        private readonly object _wsLock = new object();
        private string _instanceId;
        private bool _disposed;

        public event EventHandler<TrackChangedEventArgs> OnTrackChanged;
        public event EventHandler<StateChangedEventArgs> OnStateChanged;
        public event EventHandler<PositionEventArgs> OnPosition;
        public event EventHandler<QueueChangedEventArgs> OnQueueChanged;
        public event EventHandler<PlaylistUpdatedEventArgs> OnPlaylistUpdated;
        public event EventHandler<ModuleChangedEventArgs> OnModuleChanged;
        public event EventHandler<ErrorEventArgs> OnError;
        public event EventHandler<LyricFetchedEventArgs> OnLyricFetched;
        public event EventHandler<LyricPositionEventArgs> OnLyricPosition;
        public event EventHandler<ExcludeChangedEventArgs> OnExcludeChanged;

        public bool IsConnected => _ws?.State == WebSocketState.Open;

        /// <summary>TCP mode.</summary>
        public OmniMixPlayerClient(int port)
        {
            _port = port;
            _useSocket = false;
            _http = new HttpClient { Timeout = RequestTimeout };
        }

        /// <summary>Unix socket mode.</summary>
        public OmniMixPlayerClient(string socketPath, bool useSocket)
        {
            _socketPath = socketPath;
            _useSocket = useSocket;
            if (useSocket)
            {
                throw new PlatformNotSupportedException("Unix domain sockets are not supported under .NET Framework.");
            }
            else
            {
                _http = new HttpClient { Timeout = RequestTimeout };
            }
        }

        private string TcpBaseUrl => $"http://127.0.0.1:{_port}/api";
        private string TcpWsUrl => $"ws://127.0.0.1:{_port}/ws";

        private string ApiUrl(string path) => _useSocket
            ? $"{DummyHost}{ApiPath}{path}"
            : $"{TcpBaseUrl}{path}";

        public string InstanceId => _instanceId;

        private string InstancePath(string path)
        {
            if (string.IsNullOrEmpty(_instanceId))
                throw new InvalidOperationException("No playback instance is connected.");
            return $"/instances/{Uri.EscapeDataString(_instanceId)}{path}";
        }


#region Connection

public async Task ConnectAsync()
{
    await ConnectAsync(CancellationToken.None);
}

public async Task ConnectAsync(CancellationToken cancellationToken)
{
    lock (_wsLock)
    {
        if (_ws?.State == WebSocketState.Open)
            return;

        _wsCts?.Cancel();
        _wsCts?.Dispose();
        _ws?.Dispose();
        _wsSocket?.Dispose();

        _wsSocket = null;
        _ws = null;
        _wsCts = new CancellationTokenSource();
    }

    if (_useSocket)
    {
        await ConnectUnixWsAsync(cancellationToken);
    }
    else
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(TcpWsUrl), cancellationToken);
    }
    _wsTask = Task.Run(() => ReceiveLoop(_wsCts.Token));
}

private Task ConnectUnixWsAsync(CancellationToken ct)
{
    throw new PlatformNotSupportedException("Unix domain sockets are not supported under .NET Framework.");
}

public async Task DisconnectAsync()
{
    lock (_wsLock)
    {
        _wsCts?.Cancel();
    }

    if (_wsTask != null)
    {
        try { await _wsTask; }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }

    lock (_wsLock)
    {
        if (_ws?.State == WebSocketState.Open || _ws?.State == WebSocketState.CloseReceived)
        {
            try
            {
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).GetAwaiter().GetResult();
            }
            catch { }
        }
        _ws?.Dispose();
        _ws = null;
        _wsSocket?.Dispose();
        _wsSocket = null;
        _wsCts?.Dispose();
        _wsCts = null;
    }
}

private async Task ReceiveLoop(CancellationToken cancellationToken)
{
    var buffer = new byte[4096];
    var messageBuffer = new StringBuilder();

    try
    {
        while (!cancellationToken.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            messageBuffer.Clear();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    return;
                }
                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            ProcessMessage(messageBuffer.ToString());
        }
    }
    catch (OperationCanceledException) { }
    catch (WebSocketException) { }
    catch (Exception) { }
}

private void ProcessMessage(string json)
{
    try
    {
        var obj = JObject.Parse(json);
        var eventName = obj["event"]?.ToString();
        var data = obj["data"] as JObject;

        if (eventName == null) return;

        switch (eventName)
        {
            case "track.changed":
                OnTrackChanged?.Invoke(this, new TrackChangedEventArgs
                {
                    Uuid = data?["uuid"]?.ToString(),
                    Title = data?["title"]?.ToString(),
                    Artist = data?["artist"]?.ToString(),
                    Duration = data?["duration"]?.ToObject<float>() ?? 0f
                });
                break;

            case "state.changed":
                OnStateChanged?.Invoke(this, new StateChangedEventArgs
                {
                    IsPlaying = data?["isPlaying"]?.ToObject<bool>() ?? false,
                    Position = data?["position"]?.ToObject<float>() ?? 0f,
                    Volume = data?["volume"]?.ToObject<float>() ?? 0f
                });
                break;

            case "queue.changed":
                OnQueueChanged?.Invoke(this, new QueueChangedEventArgs());
                break;

            case "position":
                OnPosition?.Invoke(this, new PositionEventArgs
                {
                    Position = data?["position"]?.ToObject<float>() ?? 0f
                });
                break;

            case "module.changed":
                OnModuleChanged?.Invoke(this, new ModuleChangedEventArgs
                {
                    ModuleId = data?["moduleId"]?.ToString(),
                    Enabled = data?["enabled"]?.ToObject<bool>() ?? false
                });
                break;

            case "error":
                OnError?.Invoke(this, new ErrorEventArgs
                {
                    Code = data?["code"]?.ToString(),
                    Message = data?["message"]?.ToString()
                });
                break;

            case "lyric.fetched":
                OnLyricFetched?.Invoke(this, new LyricFetchedEventArgs
                {
                    Uuid = data?["uuid"]?.ToString(),
                    Lrc = data?["lrc"]?.ToString(),
                    Tlyric = data?["tlyric"]?.ToString(),
                    Rlyric = data?["rlyric"]?.ToString()
                });
                break;

            case "lyric.position":
                OnLyricPosition?.Invoke(this, new LyricPositionEventArgs
                {
                    Uuid = data?["uuid"]?.ToString(),
                    LineIndex = data?["lineIndex"]?.ToObject<int>() ?? 0,
                    TimeMs = data?["timeMs"]?.ToObject<float>() ?? 0f
                });
                break;

            case "playlist.updated":
                OnPlaylistUpdated?.Invoke(this, new PlaylistUpdatedEventArgs());
                break;

            case "exclude.changed":
                OnExcludeChanged?.Invoke(this, new ExcludeChangedEventArgs
                {
                    Uuid = data?["uuid"]?.ToString(),
                    IsExcluded = data?["isExcluded"]?.ToObject<bool>() ?? false
                });
                break;
        }
    }
    catch { }
}

#endregion

#region Playback Control

public async Task Play(string uuid)
{
    var content = JsonContent(new { uuid });
    await _http.PostAsync(ApiUrl(InstancePath("/play")), content);
}

public async Task Pause()
{
    await _http.PostAsync(ApiUrl(InstancePath("/pause")), null);
}

public async Task Resume()
{
    await _http.PostAsync(ApiUrl(InstancePath("/resume")), null);
}

public async Task Toggle()
{
    await _http.PostAsync(ApiUrl(InstancePath("/toggle")), null);
}

public async Task Next()
{
    await _http.PostAsync(ApiUrl(InstancePath("/next")), null);
}

public async Task Prev()
{
    await _http.PostAsync(ApiUrl(InstancePath("/prev")), null);
}

public async Task Seek(float position)
{
    var content = JsonContent(new { position });
    await _http.PostAsync(ApiUrl(InstancePath("/seek")), content);
}

public async Task SetVolume(float volume)
{
    var content = JsonContent(new { volume });
    var request = new HttpRequestMessage(HttpMethod.Put, ApiUrl(InstancePath("/volume")))
    {
        Content = content
    };
    await _http.SendAsync(request);
}

public async Task SetShuffle(bool enabled)
{
    var content = JsonContent(new { enabled });
    await _http.PostAsync(ApiUrl(InstancePath("/shuffle")), content);
}

public async Task SetRepeat(string mode)
{
    var content = JsonContent(new { mode });
    await _http.PostAsync(ApiUrl(InstancePath("/repeat")), content);
}

#endregion

#region Playlist Query

public async Task<JObject> GetStatus()
{
    var json = await _http.GetStringAsync(ApiUrl(InstancePath("/status")));
    return JObject.Parse(json);
}

public async Task<JObject> GetPlaylist()
{
    var json = await _http.GetStringAsync(ApiUrl("/playlist"));
    return JObject.Parse(json);
}

public async Task<JToken> GetTags()
{
    var json = await _http.GetStringAsync(ApiUrl("/tags"));
    return JToken.Parse(json);
}

public async Task<JToken> GetAlbums(string tagId = null)
{
    var url = ApiUrl("/albums");
    if (!string.IsNullOrEmpty(tagId))
        url += $"?tagId={Uri.EscapeDataString(tagId)}";
    var json = await _http.GetStringAsync(url);
    return JToken.Parse(json);
}

public async Task<JToken> GetSongs(string albumId = null, string tagId = null)
{
    var url = ApiUrl("/songs");
    var separator = "?";
    if (!string.IsNullOrEmpty(albumId))
    {
        url += $"{separator}albumId={Uri.EscapeDataString(albumId)}";
        separator = "&";
    }
    if (!string.IsNullOrEmpty(tagId))
    {
        url += $"{separator}tagId={Uri.EscapeDataString(tagId)}";
    }
    var json = await _http.GetStringAsync(url);
    return JToken.Parse(json);
}

#endregion

#region Queue Management

public async Task<JToken> GetQueue()
{
    var json = await _http.GetStringAsync(ApiUrl(InstancePath("/queue")));
    return JToken.Parse(json);
}

public async Task AddToQueue(string uuid)
{
    var content = JsonContent(new { uuid });
    await _http.PostAsync(ApiUrl(InstancePath("/queue")), content);
}

public async Task RemoveFromQueue(int index)
{
    await _http.DeleteAsync(ApiUrl(InstancePath($"/queue/{index}")));
}

public async Task MoveInQueue(int from, int to)
{
    var content = JsonContent(new { from, to });
    await _http.PostAsync(ApiUrl(InstancePath("/queue/move")), content);
}

public async Task ClearQueue()
{
    await _http.PostAsync(ApiUrl(InstancePath("/queue/clear")), null);
}

#endregion

#region Client Connection

public async Task<JObject> ConnectInstance(string clientId, string role, string mode)
{
    var content = JsonContent(new { clientId, role, mode });
    var response = await _http.PostAsync(ApiUrl("/instances/connect"), content);
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    var obj = JObject.Parse(json);
    _instanceId = obj["instanceId"]?.ToString();
    return obj;
}

public async Task DisconnectInstance()
{
    try
    {
        if (!string.IsNullOrEmpty(_instanceId))
            await _http.PostAsync(ApiUrl(InstancePath("/disconnect")), null);
        _instanceId = null;
    }
    catch { }
}

public async Task<bool> HeartbeatInstance()
{
    try
    {
        var response = await _http.PostAsync(ApiUrl(InstancePath("/heartbeat")), null);
        return response.IsSuccessStatusCode;
    }
    catch { return false; }
}

public async Task<JArray> GetInstances()
{
    var json = await _http.GetStringAsync(ApiUrl("/instances"));
    return JArray.Parse(json);
}

public async Task<JObject> GetInstanceStats()
{
    var json = await _http.GetStringAsync(ApiUrl("/instances/stats"));
    return JObject.Parse(json);
}

#endregion

#region Song

public async Task<JObject> GetSong(string uuid)
{
    var json = await _http.GetStringAsync(ApiUrl("/song/" + Uri.EscapeDataString(uuid)));
    return JObject.Parse(json);
}

public async Task<JObject> GetLyric(string uuid)
{
    var json = await _http.GetStringAsync(ApiUrl("/lyric/" + Uri.EscapeDataString(uuid)));
    return JObject.Parse(json);
}

public async Task<(byte[] data, string mimeType)> GetTrackCover(string uuid)
{
    var response = await _http.GetAsync(ApiUrl("/track/cover?uuid=" + Uri.EscapeDataString(uuid)));
    response.EnsureSuccessStatusCode();
    var data = await response.Content.ReadAsByteArrayAsync();
    var mimeType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
    return (data, mimeType);
}

#endregion

#region Modules

public async Task<JArray> GetModules()
{
    var json = await _http.GetStringAsync(ApiUrl("/modules"));
    return JArray.Parse(json);
}

public async Task SetModuleEnabled(string id, bool enabled)
{
    var content = JsonContent(new { enabled });
    await _http.PostAsync(ApiUrl("/modules/" + Uri.EscapeDataString(id)), content);
}

#endregion

#region Version

public async Task<JObject> GetVersion()
{
    var json = await _http.GetStringAsync(ApiUrl("/version"));
    return JObject.Parse(json);
}

#endregion

#region General

public async Task<string> PostAsync(string path, object body)
{
    var content = JsonContent(body);
    var response = await _http.PostAsync(ApiUrl(path), content);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}

public async Task<string> GetAsync(string path)
{
    return await _http.GetStringAsync(ApiUrl(path));
}

#endregion

#region Helpers

private static StringContent JsonContent(object obj)
{
    var json = JsonConvert.SerializeObject(obj);
    return new StringContent(json, Encoding.UTF8, "application/json");
}

#endregion

#region IDisposable

public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
    _wsCts?.Cancel();
    _wsCts?.Dispose();
    _ws?.Dispose();
    _wsSocket?.Dispose();
    _http?.Dispose();
    _wsTask?.Dispose();
}

        #endregion
    }
}
