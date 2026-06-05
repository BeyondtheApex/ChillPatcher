using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChillPatcher.SDK.Native
{
    #region Enums & Constants

    public enum OmniPcmResult
    {
        Ok = 0,
        Error = -1,
        BadArgument = -2,
        Unsupported = -3,
        NotReady = -4
    }

    public enum OmniPcmInstanceKind
    {
        Unspecified = 0,
        GameMod = 1,
        Gui = 2,
        ExternalClient = 3,
        Observer = 4
    }

    [Flags]
    public enum OmniPcmCapabilityFlags : uint
    {
        ServerControlledPlayback = 1u << 0,
        QueueManagement = 1u << 2,
        PlaylistManagement = 1u << 3,
        Shuffle = 1u << 4,
        Repeat = 1u << 5,
        Seek = 1u << 6,
        VolumeControl = 1u << 7,
        Equalizer = 1u << 8,
        MultiplePlaylists = 1u << 9,
        TagFiltering = 1u << 10,
        UnlimitedTags = 1u << 11,
        AlbumFiltering = 1u << 12,
        AudioPlayback = 1u << 13,
        CustomSystemMediaService = 1u << 14
    }

    public enum OmniPcmCommand
    {
        Pause = 1,
        Resume = 2,
        Toggle = 3,
        Next = 4,
        Prev = 5,
        Stop = 6,
        Play = 7
    }

    #endregion

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmClientConfig
    {
        [MarshalAs(UnmanagedType.LPStr)] public string host;
        public int port;
        public int timeoutMs;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmniPcmConnectOptions
    {
        [MarshalAs(UnmanagedType.LPStr)] public string clientId;
        [MarshalAs(UnmanagedType.LPStr)] public string modId;
        [MarshalAs(UnmanagedType.LPStr)] public string gameName;
        [MarshalAs(UnmanagedType.LPStr)] public string displayName;
        public int kind;
        public uint capabilityFlags;
        public int noInstance;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmConnectionInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string instanceId;
        public int isNew;
        public int noInstance;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmPlaybackStatusInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string trackUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string albumId;
        public float duration;
        public float position;
        public int isPlaying;
        public int shuffle;
        public int repeatMode;
        public float volume;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmQueueTrackInfo
    {
        public int index;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string albumId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string moduleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string coverUri;
        public float duration;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmTrackInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string albumId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string moduleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string coverUri;
        public int trackNumber;
        public float duration;
        public int isExcluded;
        public long createdAt;
        public long lastPlayedAt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmAlbumInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string moduleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string coverUri;
        public int trackCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmTagInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string moduleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string color;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OmniPcmEventInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string type;
        public long timestamp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string instanceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string trackUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string artist;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string albumId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string moduleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string sourceRefId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string changeType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string displayName;
        public float duration;
        public float position;
        public int state;
        public int queueLength;
        public int backendRunning;
        public int boolValue;
        public int songCount;
        public int instanceCount;
    }

    #endregion

    /// <summary>
    /// C# P/Invoke wrapper for the native OmniPcmShared control-plane client.
    /// All backend communication (gRPC-Web, HTTP, WebSocket events) goes through this DLL.
    /// </summary>
    public sealed class OmniPcmClient : IDisposable
    {
        private const string DllName = "OmniPcmShared";

        private IntPtr _handle;
        private bool _disposed;

        #region Lifecycle

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcmClient_Create(ref OmniPcmClientConfig config);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void OmniPcmClient_Destroy(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcmClient_GetLastError(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetPort(IntPtr client);

        public OmniPcmClient(string host = null, int port = 0, int timeoutMs = 3000)
        {
            var config = new OmniPcmClientConfig
            {
                host = host ?? "127.0.0.1",
                port = port,
                timeoutMs = timeoutMs
            };
            _handle = OmniPcmClient_Create(ref config);
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("OmniPcmClient_Create failed");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_handle != IntPtr.Zero) { OmniPcmClient_Destroy(_handle); _handle = IntPtr.Zero; }
        }

        public string LastError => _handle != IntPtr.Zero
            ? Marshal.PtrToStringAnsi(OmniPcmClient_GetLastError(_handle)) ?? ""
            : "disposed";
        public int Port => _handle != IntPtr.Zero ? OmniPcmClient_GetPort(_handle) : 0;

        #endregion

        #region Instance

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ConnectInstance(
            IntPtr client, ref OmniPcmConnectOptions options, out OmniPcmConnectionInfo outInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_Heartbeat(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, out int outAlive);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_DisconnectInstance(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId);

        public OmniPcmConnectionInfo ConnectInstance(string clientId, OmniPcmCapabilityFlags caps,
            string modId = "chillPatcher", string gameName = "Chill With You", string displayName = "ChillPatcher")
        {
            var opts = new OmniPcmConnectOptions
            {
                clientId = clientId,
                modId = modId,
                gameName = gameName,
                displayName = displayName,
                kind = (int)OmniPcmInstanceKind.GameMod,
                capabilityFlags = (uint)caps,
                noInstance = 0
            };
            var r = OmniPcmClient_ConnectInstance(_handle, ref opts, out var info);
            if (r != 0) throw new InvalidOperationException($"ConnectInstance failed: {LastError}");
            return info;
        }

        public bool Heartbeat(string instanceId)
        {
            return OmniPcmClient_Heartbeat(_handle, instanceId, out var alive) == 0 && alive != 0;
        }

        public void DisconnectInstance(string instanceId)
        {
            OmniPcmClient_DisconnectInstance(_handle, instanceId);
        }

        #endregion

        #region Playback

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetStatus(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, out OmniPcmPlaybackStatusInfo outStatus);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_PlaybackCommand(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int command);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_Play(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPStr)] string trackUuid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_Seek(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, float positionSeconds);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetVolume(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, float volume);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetShuffle(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetRepeatMode(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int repeatMode);

        public OmniPcmPlaybackStatusInfo GetStatus(string instanceId)
        {
            var r = OmniPcmClient_GetStatus(_handle, instanceId, out var s);
            if (r != 0) throw new InvalidOperationException($"GetStatus failed: {LastError}");
            return s;
        }

        public void PlaybackCommand(string instanceId, OmniPcmCommand cmd)
        {
            var r = OmniPcmClient_PlaybackCommand(_handle, instanceId, (int)cmd);
            if (r != 0) Debug.WriteLine($"[OmniPcmClient] PlaybackCommand {cmd} failed: {LastError}");
        }

        public void Play(string instanceId, string uuid = null)
        {
            var r = OmniPcmClient_Play(_handle, instanceId, uuid);
            if (r != 0) Debug.WriteLine($"[OmniPcmClient] Play failed: {LastError}");
        }

        public void Seek(string instanceId, float position)
        {
            var r = OmniPcmClient_Seek(_handle, instanceId, position);
            if (r != 0) Debug.WriteLine($"[OmniPcmClient] Seek failed: {LastError}");
        }

        public void SetVolume(string instanceId, float volume)
        {
            OmniPcmClient_SetVolume(_handle, instanceId, volume);
        }

        public void SetShuffle(string instanceId, bool enabled)
        {
            OmniPcmClient_SetShuffle(_handle, instanceId, enabled ? 1 : 0);
        }

        public void SetRepeat(string instanceId, int mode)
        {
            OmniPcmClient_SetRepeatMode(_handle, instanceId, mode);
        }

        #endregion

        #region Queue

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [Out] OmniPcmQueueTrackInfo[] outTracks, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_AddToQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPStr)] string uuid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ClearQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId);

        public OmniPcmQueueTrackInfo[] GetQueue(string instanceId)
        {
            int count = 0;
            int r = OmniPcmClient_GetQueue(_handle, instanceId, null, ref count);
            if (r != (int)OmniPcmResult.NotReady || count == 0) return Array.Empty<OmniPcmQueueTrackInfo>();
            var tracks = new OmniPcmQueueTrackInfo[count];
            r = OmniPcmClient_GetQueue(_handle, instanceId, tracks, ref count);
            return r == 0 ? tracks : Array.Empty<OmniPcmQueueTrackInfo>();
        }

        public void AddToQueue(string instanceId, string uuid)
        {
            OmniPcmClient_AddToQueue(_handle, instanceId, uuid);
        }

        public void ClearQueue(string instanceId)
        {
            OmniPcmClient_ClearQueue(_handle, instanceId);
        }

        #endregion

        #region Events (WebSocket)

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EventCallback(ref OmniPcmEventInfo eventInfo, IntPtr userData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_StartEvents(IntPtr client,
            EventCallback callback, IntPtr userData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void OmniPcmClient_StopEvents(IntPtr client);

        private EventCallback _eventCallback;
        private bool _eventsStarted;

        public void StartEvents(Action<OmniPcmEventInfo> handler)
        {
            if (_eventsStarted) return;
            _eventCallback = (ref OmniPcmEventInfo info, IntPtr _) => handler(info);
            var r = OmniPcmClient_StartEvents(_handle, _eventCallback, IntPtr.Zero);
            if (r != 0) throw new InvalidOperationException($"StartEvents failed: {LastError}");
            _eventsStarted = true;
        }

        public void StopEvents()
        {
            if (!_eventsStarted) return;
            OmniPcmClient_StopEvents(_handle);
            _eventsStarted = false;
        }

        #endregion

        #region Library

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryTracks(IntPtr client,
            ref OmniPcmTrackQuery query, [Out] OmniPcmTrackInfo[] outTracks, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryAlbums(IntPtr client,
            ref OmniPcmLibraryQuery query, [Out] OmniPcmAlbumInfo[] outAlbums, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryTags(IntPtr client,
            ref OmniPcmLibraryQuery query, [Out] OmniPcmTagInfo[] outTags, ref int inoutCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct OmniPcmTrackQuery { /* ... */ }
        [StructLayout(LayoutKind.Sequential)]
        private struct OmniPcmLibraryQuery { /* ... */ }

        // Library queries not needed for core playback; stub for future use.
        // The native SDK supports them via the same DllImport pattern.

        #endregion
    }
}
