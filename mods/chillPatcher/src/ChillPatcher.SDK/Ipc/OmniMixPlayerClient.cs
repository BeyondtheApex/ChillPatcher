using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChillPatcher.SDK.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChillPatcher.SDK.Ipc
{
    #region Event Args (keep existing signatures)

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

    public class QueueChangedEventArgs : EventArgs { }
    public class PositionEventArgs : EventArgs { public float Position { get; set; } }

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

    public class PlaylistUpdatedEventArgs : EventArgs { }

    public class ExcludeChangedEventArgs : EventArgs
    {
        public string Uuid { get; set; }
        public bool IsExcluded { get; set; }
    }

    #endregion

    /// <summary>
    /// Backend communication client using the native OmniPcmShared DLL via P/Invoke.
    /// All gRPC-Web, HTTP, and WebSocket communication goes through the C++ SDK.
    /// </summary>
    public class OmniMixPlayerClient : IDisposable
    {
        private const int DefaultPort = 17890;

        private readonly OmniPcmClient _native;
        private readonly HttpClient _http; // for library queries (tracks/albums/tags) not yet in native SDK
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

        public bool IsConnected => !_disposed && !string.IsNullOrEmpty(_instanceId);
        public string InstanceId => _instanceId;

        private static readonly OmniPcmCapabilityFlags ChillCaps =
            OmniPcmCapabilityFlags.ServerControlledPlayback |
            OmniPcmCapabilityFlags.QueueManagement |
            OmniPcmCapabilityFlags.PlaylistManagement |
            OmniPcmCapabilityFlags.MultiplePlaylists |
            OmniPcmCapabilityFlags.TagFiltering |
            OmniPcmCapabilityFlags.AlbumFiltering |
            OmniPcmCapabilityFlags.Shuffle |
            OmniPcmCapabilityFlags.Repeat |
            OmniPcmCapabilityFlags.Seek |
            OmniPcmCapabilityFlags.VolumeControl |
            OmniPcmCapabilityFlags.AudioPlayback |
            OmniPcmCapabilityFlags.CustomSystemMediaService;

        public OmniMixPlayerClient(int port)
        {
            _native = new OmniPcmClient(port: port > 0 ? port : 0);
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        public OmniMixPlayerClient(string socketPath, bool useSocket)
        {
            if (useSocket)
                throw new PlatformNotSupportedException("Unix sockets not supported by native SDK; use TCP mode.");
            _native = new OmniPcmClient();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        private string BaseUrl => $"http://127.0.0.1:{_native.Port}/api";

        public async Task ConnectAsync()
        {
            // Native SDK auto-discovers port from omnimix_port.txt
            await Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            _native.StopEvents();
            if (!string.IsNullOrEmpty(_instanceId))
            {
                try { _native.DisconnectInstance(_instanceId); } catch { }
                _instanceId = null;
            }
            await Task.CompletedTask;
        }

        public async Task<JObject> ConnectInstance(string clientId, string role, string mode)
        {
            var info = _native.ConnectInstance(clientId, ChillCaps);
            _instanceId = info.instanceId;

            // Start WebSocket events
            _native.StartEvents(HandleNativeEvent);

            return new JObject
            {
                ["instanceId"] = info.instanceId,
                ["isNew"] = info.isNew != 0,
                ["sharedMemoryName"] = $"Global\\OmniMixPlayer_PCM_{_instanceId}",
                ["noInstance"] = false
            };
        }

        public async Task DisconnectInstance()
        {
            if (!string.IsNullOrEmpty(_instanceId))
            {
                try { _native.DisconnectInstance(_instanceId); } catch { }
                _instanceId = null;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> HeartbeatInstance()
        {
            if (string.IsNullOrEmpty(_instanceId)) return false;
            try { return _native.Heartbeat(_instanceId); }
            catch { return false; }
        }

        #region Playback Control

        public async Task Play(string uuid) => _native.Play(_instanceId, uuid);
        public async Task Pause() => _native.PlaybackCommand(_instanceId, OmniPcmCommand.Pause);
        public async Task Resume() => _native.PlaybackCommand(_instanceId, OmniPcmCommand.Resume);
        public async Task Toggle() => _native.PlaybackCommand(_instanceId, OmniPcmCommand.Toggle);
        public async Task Next() => _native.PlaybackCommand(_instanceId, OmniPcmCommand.Next);
        public async Task Prev() => _native.PlaybackCommand(_instanceId, OmniPcmCommand.Prev);
        public async Task Seek(float position) => _native.Seek(_instanceId, position);
        public async Task SetVolume(float volume) => _native.SetVolume(_instanceId, volume);
        public async Task SetShuffle(bool enabled) => _native.SetShuffle(_instanceId, enabled);
        public async Task SetRepeat(string mode)
        {
            int m = mode switch { "one" => 2, "all" => 3, _ => 1 };
            _native.SetRepeat(_instanceId, m);
        }

        #endregion

        #region Playlist Query (HTTP fallback for library)

        public async Task<JObject> GetStatus()
        {
            var s = _native.GetStatus(_instanceId);
            return JObject.FromObject(new
            {
                isPlaying = s.isPlaying != 0,
                position = s.position,
                volume = s.volume,
                shuffle = s.shuffle != 0,
                repeatMode = s.repeatMode,
                trackUuid = s.trackUuid,
                title = s.title,
                artist = s.artist,
                duration = s.duration
            });
        }

        public async Task<JToken> GetQueue()
        {
            var tracks = _native.GetQueue(_instanceId);
            var arr = new JArray();
            foreach (var t in tracks)
                arr.Add(new JObject { ["uuid"] = t.uuid, ["title"] = t.title, ["artist"] = t.artist, ["duration"] = t.duration });
            return arr;
        }

        public async Task AddToQueue(string uuid) => _native.AddToQueue(_instanceId, uuid);
        public async Task ClearQueue() => _native.ClearQueue(_instanceId);

        public async Task InsertIntoQueue(int index, System.Collections.Generic.IEnumerable<string> uuids)
        {
            await PostAsync($"/instances/{Uri.EscapeDataString(_instanceId)}/queue/insert", new { index, uuids });
        }

        public async Task RemoveFromQueue(int index)
        {
            await _http.DeleteAsync($"{BaseUrl}/instances/{Uri.EscapeDataString(_instanceId)}/queue/{index}");
        }

        public async Task RemoveFromQueue(string uuid)
        {
            await PostAsync($"/instances/{Uri.EscapeDataString(_instanceId)}/queue/remove", new { uuid });
        }

        public async Task MoveInQueue(int from, int to)
        {
            await PostAsync($"/instances/{Uri.EscapeDataString(_instanceId)}/queue/move", new { from, to });
        }

        public async Task<JToken> GetTags()
        {
            var json = await _http.GetStringAsync($"{BaseUrl}/tags");
            return JToken.Parse(json);
        }

        public async Task<JToken> GetAlbums(string tagId = null)
        {
            var url = $"{BaseUrl}/albums";
            if (!string.IsNullOrEmpty(tagId)) url += $"?tagId={Uri.EscapeDataString(tagId)}";
            return JToken.Parse(await _http.GetStringAsync(url));
        }

        public async Task<JToken> GetSongs(string albumId = null, string tagId = null)
        {
            var url = $"{BaseUrl}/songs";
            var sep = "?";
            if (!string.IsNullOrEmpty(albumId)) { url += $"{sep}albumId={Uri.EscapeDataString(albumId)}"; sep = "&"; }
            if (!string.IsNullOrEmpty(tagId)) { url += $"{sep}tagId={Uri.EscapeDataString(tagId)}"; }
            return JToken.Parse(await _http.GetStringAsync(url));
        }

        public async Task<JToken> GetHistory()
        {
            var json = await _http.GetStringAsync($"{BaseUrl}/instances/{Uri.EscapeDataString(_instanceId)}/history");
            return JToken.Parse(json);
        }

        public async Task ClearHistory()
        {
            await _http.PostAsync($"{BaseUrl}/instances/{Uri.EscapeDataString(_instanceId)}/history/clear", null);
        }

        public async Task SetFavorite(string uuid, bool fav)
        {
            await PostAsync("/favorite", new { uuid, isFavorite = fav });
        }

        public async Task SetExcluded(string uuid, bool excluded)
        {
            await PostAsync("/exclude", new { uuid, isFavorite = excluded });
        }

        #endregion

        #region Cover & Lyrics

        public async Task<(byte[] data, string mimeType)> GetTrackCover(string uuid)
        {
            var resp = await _http.GetAsync($"{BaseUrl}/track/cover?uuid={Uri.EscapeDataString(uuid)}");
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadAsByteArrayAsync(), resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
        }

        public async Task<(byte[] data, string mimeType)> GetBytesAsync(string path)
        {
            var resp = await _http.GetAsync($"{BaseUrl}/{path.TrimStart('/')}");
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadAsByteArrayAsync(), resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }

        public async Task<JObject> GetSong(string uuid)
        {
            var json = await _http.GetStringAsync($"{BaseUrl}/song/{Uri.EscapeDataString(uuid)}");
            return JObject.Parse(json);
        }

        public async Task<JObject> GetLyric(string uuid)
        {
            var json = await _http.GetStringAsync($"{BaseUrl}/lyric/{Uri.EscapeDataString(uuid)}");
            return JObject.Parse(json);
        }

        public async Task<string> GetAsync(string path)
        {
            return await _http.GetStringAsync($"{BaseUrl}{path}");
        }

        public async Task<string> PostAsync(string path, object body)
        {
            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{BaseUrl}{path}", content);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<JObject> GetVersion()
        {
            var json = await _http.GetStringAsync($"{BaseUrl}/version");
            return JObject.Parse(json);
        }

        #endregion

        #region Events Dispatch

        private void HandleNativeEvent(OmniPcmEventInfo e)
        {
            try
            {
                switch (e.type)
                {
                    case "track.changed":
                        OnTrackChanged?.Invoke(this, new TrackChangedEventArgs
                        {
                            Uuid = e.trackUuid,
                            Title = e.title,
                            Artist = e.artist,
                            Duration = e.duration
                        });
                        break;
                    case "state.changed":
                        OnStateChanged?.Invoke(this, new StateChangedEventArgs
                        {
                            IsPlaying = e.state == 1,
                            Position = e.position
                        });
                        break;
                    case "position":
                        OnPosition?.Invoke(this, new PositionEventArgs { Position = e.position });
                        break;
                    case "queue.changed":
                        OnQueueChanged?.Invoke(this, new QueueChangedEventArgs());
                        break;
                    case "playlist.updated":
                        OnPlaylistUpdated?.Invoke(this, new PlaylistUpdatedEventArgs());
                        break;
                    case "module.changed":
                        OnModuleChanged?.Invoke(this, new ModuleChangedEventArgs
                        {
                            ModuleId = e.moduleId,
                            Enabled = e.boolValue != 0
                        });
                        break;
                    case "exclude.changed":
                        OnExcludeChanged?.Invoke(this, new ExcludeChangedEventArgs
                        {
                            Uuid = e.trackUuid,
                            IsExcluded = e.boolValue != 0
                        });
                        break;
                    case "error":
                        OnError?.Invoke(this, new ErrorEventArgs { Code = "", Message = e.changeType });
                        break;
                }
            }
            catch { }
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _native?.Dispose();
            _http?.Dispose();
        }
    }
}
