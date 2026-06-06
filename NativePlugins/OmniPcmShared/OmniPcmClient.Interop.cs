using System;
using System.Runtime.InteropServices;

namespace OmniPcmShared.Interop
{
    /// <summary>
    /// Managed wrapper for the native OmniPcmShared control-plane client.
    /// All backend communication (gRPC-Web, HTTP, WebSocket events) goes through OmniPcmShared.dll.
    /// </summary>
    public sealed class OmniPcmClient : IDisposable
    {
        private const string DllName = "OmniPcmShared";

        private IntPtr _handle;
        private bool _disposed;
        private OmniPcmEventCallback _nativeCallbackDelegate;

        // ── Lifecycle ─────────────────────────────────────────────

        public OmniPcmClient(string host = null, int port = 0, int timeoutMs = 3000)
        {
            var config = new OmniPcmClientConfig
            {
                Host = host ?? "127.0.0.1",
                Port = port,
                TimeoutMs = timeoutMs
            };
            _handle = OmniPcmClient_Create(ref config);
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("OmniPcmClient_Create returned null");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopEvents();
            if (_handle != IntPtr.Zero)
            {
                OmniPcmClient_Destroy(_handle);
                _handle = IntPtr.Zero;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed || _handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(OmniPcmClient));
        }

        /// <summary>Optional logger for non-critical failures.  Set once after construction.</summary>
        public Action<string> Log { get; set; }

        public string LastError =>
            _handle != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(OmniPcmClient_GetLastError(_handle)) ?? ""
                : "disposed";

        public int Port =>
            _handle != IntPtr.Zero ? OmniPcmClient_GetPort(_handle) : 0;

        // ── Instance management ───────────────────────────────────

        public OmniPcmConnectionInfo ConnectInstance(
            string clientId,
            OmniPcmCapabilityFlags caps,
            string modId = null,
            string gameName = null,
            string displayName = null)
        {
            ThrowIfDisposed();
            var opts = new OmniPcmConnectOptions
            {
                ClientId = clientId,
                ModId = modId,
                GameName = gameName,
                DisplayName = displayName,
                Kind = (int)OmniPcmInstanceKind.GameMod,
                CapabilityFlags = (uint)caps,
                MaxImportedPlaylists = 27,
                MaxTags = 0,
                MaxPlaylistEntries = 0,
            };
            int r = OmniPcmClient_ConnectInstance(_handle, ref opts, out var info);
            if (r != 0) throw new InvalidOperationException($"ConnectInstance failed: {LastError}");
            return info;
        }

        public bool Heartbeat(string instanceId)
        {
            ThrowIfDisposed();
            int alive = 0;
            int r = OmniPcmClient_Heartbeat(_handle, instanceId, ref alive);
            return r == 0 && alive != 0;
        }

        public void DisconnectInstance(string instanceId)
        {
            ThrowIfDisposed();
            OmniPcmClient_DisconnectInstance(_handle, instanceId);
        }

        public bool DeleteInstance(string instanceId)
        {
            ThrowIfDisposed();
            int deleted = 0;
            int r = OmniPcmClient_DeleteInstance(_handle, instanceId, ref deleted);
            return r == 0 && deleted != 0;
        }

        public OmniPcmInstanceSummaryInfo[] ListInstances()
        {
            ThrowIfDisposed();
            return GetList(
                (arr, ref count) => OmniPcmClient_ListInstances(_handle, arr, ref count),
                () => new OmniPcmInstanceSummaryInfo[0]);
        }

        public OmniPcmInstanceProfileInfo GetProfile(string instanceId)
        {
            ThrowIfDisposed();
            OmniPcmInstanceProfileInfo profile = default;
            Check(OmniPcmClient_GetProfile(_handle, instanceId, ref profile), "GetProfile");
            return profile;
        }

        public bool UpdateProfile(OmniPcmInstanceProfileInfo profile)
        {
            ThrowIfDisposed();
            int saved = 0;
            int r = OmniPcmClient_UpdateProfile(_handle, ref profile, ref saved);
            return r == 0 && saved != 0;
        }

        public OmniPcmInstanceProfileInfo ArchiveInstance(string instanceId, string label = null)
        {
            ThrowIfDisposed();
            OmniPcmInstanceProfileInfo archive = default;
            Check(OmniPcmClient_ArchiveInstance(_handle, instanceId, label, ref archive), "ArchiveInstance");
            return archive;
        }

        public OmniPcmInstanceProfileInfo[] ListArchives()
        {
            ThrowIfDisposed();
            return GetList(
                (arr, ref count) => OmniPcmClient_ListArchives(_handle, arr, ref count),
                () => new OmniPcmInstanceProfileInfo[0]);
        }

        public OmniPcmInstanceProfileInfo GetArchive(string archiveId)
        {
            ThrowIfDisposed();
            OmniPcmInstanceProfileInfo archive = default;
            Check(OmniPcmClient_GetArchive(_handle, archiveId, ref archive), "GetArchive");
            return archive;
        }

        public bool DeleteArchive(string archiveId)
        {
            ThrowIfDisposed();
            int deleted = 0;
            int r = OmniPcmClient_DeleteArchive(_handle, archiveId, ref deleted);
            return r == 0 && deleted != 0;
        }

        public OmniPcmInstanceProfileInfo InheritFromArchive(string newInstanceId, string archiveId)
        {
            ThrowIfDisposed();
            OmniPcmInstanceProfileInfo profile = default;
            Check(OmniPcmClient_InheritFromArchive(_handle, newInstanceId, archiveId, ref profile), "InheritFromArchive");
            return profile;
        }

        // ── Playback ──────────────────────────────────────────────

        public OmniPcmPlaybackStatusInfo GetStatus(string instanceId)
        {
            ThrowIfDisposed();
            OmniPcmPlaybackStatusInfo status = default;
            Check(OmniPcmClient_GetStatus(_handle, instanceId, ref status), "GetStatus");
            return status;
        }

        public void PlaybackCommand(string instanceId, OmniPcmPlaybackCommand cmd)
        {
            ThrowIfDisposed();
            int r = OmniPcmClient_PlaybackCommand(_handle, instanceId, (int)cmd);
            if (r != 0) Log?.Invoke($"[OmniPcmClient] PlaybackCommand {cmd} failed: {LastError}");
        }

        public void Play(string instanceId, string uuid = null)
        {
            ThrowIfDisposed();
            int r = OmniPcmClient_Play(_handle, instanceId, uuid);
            if (r != 0) Log?.Invoke($"[OmniPcmClient] Play failed: {LastError}");
        }

        public void Seek(string instanceId, float position)
        {
            ThrowIfDisposed();
            int r = OmniPcmClient_Seek(_handle, instanceId, position);
            if (r != 0) Log?.Invoke($"[OmniPcmClient] Seek failed: {LastError}");
        }

        public void SetVolume(string instanceId, float volume)
        {
            ThrowIfDisposed();
            OmniPcmClient_SetVolume(_handle, instanceId, volume);
        }

        public float GetVolume(string instanceId)
        {
            ThrowIfDisposed();
            float volume = 0;
            Check(OmniPcmClient_GetVolume(_handle, instanceId, ref volume), "GetVolume");
            return volume;
        }

        public void SetTargetLatency(string instanceId, float latency)
        {
            ThrowIfDisposed();
            int r = OmniPcmClient_SetTargetLatency(_handle, instanceId, latency);
            if (r != 0) Log?.Invoke($"[OmniPcmClient] SetTargetLatency failed: {LastError}");
        }

        public float GetTargetLatency(string instanceId)
        {
            ThrowIfDisposed();
            float latency = 0;
            Check(OmniPcmClient_GetTargetLatency(_handle, instanceId, ref latency), "GetTargetLatency");
            return latency;
        }

        public void SetShuffle(string instanceId, bool enabled)
        {
            ThrowIfDisposed();
            OmniPcmClient_SetShuffle(_handle, instanceId, enabled ? 1 : 0);
        }

        public void SetRepeat(string instanceId, int mode)
        {
            ThrowIfDisposed();
            OmniPcmClient_SetRepeatMode(_handle, instanceId, mode);
        }

        // ── Queue ─────────────────────────────────────────────────

        public OmniPcmQueueTrackInfo[] GetQueue(string instanceId)
        {
            ThrowIfDisposed();
            int count = 0;
            OmniPcmClient_GetQueue(_handle, instanceId, null, ref count);
            if (count == 0) return Array.Empty<OmniPcmQueueTrackInfo>();
            var tracks = new OmniPcmQueueTrackInfo[count];
            int r = OmniPcmClient_GetQueue(_handle, instanceId, tracks, ref count);
            return r == 0 ? tracks : Array.Empty<OmniPcmQueueTrackInfo>();
        }

        public void AddToQueue(string instanceId, string uuid)
        {
            ThrowIfDisposed();
            OmniPcmClient_AddToQueue(_handle, instanceId, uuid);
        }

        public void InsertIntoQueue(string instanceId, string[] uuids, int index)
        {
            ThrowIfDisposed();
            if (uuids == null) throw new ArgumentNullException(nameof(uuids));
            OmniPcmClient_InsertIntoQueue(_handle, instanceId, uuids, uuids.Length, index);
        }

        public void SetQueue(string instanceId, string[] uuids)
        {
            ThrowIfDisposed();
            if (uuids == null) throw new ArgumentNullException(nameof(uuids));
            OmniPcmClient_SetQueue(_handle, instanceId, uuids, uuids.Length);
        }

        public void RemoveFromQueueIndex(string instanceId, int index)
        {
            ThrowIfDisposed();
            OmniPcmClient_RemoveFromQueueIndex(_handle, instanceId, index);
        }

        public void RemoveFromQueueUuid(string instanceId, string uuid)
        {
            ThrowIfDisposed();
            OmniPcmClient_RemoveFromQueueUuid(_handle, instanceId, uuid);
        }

        public void MoveInQueue(string instanceId, int fromIndex, int toIndex)
        {
            ThrowIfDisposed();
            OmniPcmClient_MoveInQueue(_handle, instanceId, fromIndex, toIndex);
        }

        public void ClearQueue(string instanceId)
        {
            ThrowIfDisposed();
            OmniPcmClient_ClearQueue(_handle, instanceId);
        }

        // ── History ───────────────────────────────────────────────

        public OmniPcmQueueTrackInfo[] GetHistory(string instanceId)
        {
            ThrowIfDisposed();
            int count = 0;
            OmniPcmClient_GetHistory(_handle, instanceId, null, ref count);
            if (count == 0) return Array.Empty<OmniPcmQueueTrackInfo>();
            var tracks = new OmniPcmQueueTrackInfo[count];
            int r = OmniPcmClient_GetHistory(_handle, instanceId, tracks, ref count);
            return r == 0 ? tracks : Array.Empty<OmniPcmQueueTrackInfo>();
        }

        public void RemoveFromHistory(string instanceId, int index)
        {
            ThrowIfDisposed();
            OmniPcmClient_RemoveFromHistory(_handle, instanceId, index);
        }

        public void MoveInHistory(string instanceId, int fromIndex, int toIndex)
        {
            ThrowIfDisposed();
            OmniPcmClient_MoveInHistory(_handle, instanceId, fromIndex, toIndex);
        }

        public void ClearHistory(string instanceId)
        {
            ThrowIfDisposed();
            OmniPcmClient_ClearHistory(_handle, instanceId);
        }

        // ── Playlist sources ──────────────────────────────────────

        public OmniPcmPlaylistSourceInfo[] GetPlaylistSources(string instanceId)
        {
            ThrowIfDisposed();
            int count = 0;
            OmniPcmClient_GetPlaylistSources(_handle, instanceId, null, ref count);
            if (count == 0) return Array.Empty<OmniPcmPlaylistSourceInfo>();
            var sources = new OmniPcmPlaylistSourceInfo[count];
            int r = OmniPcmClient_GetPlaylistSources(_handle, instanceId, sources, ref count);
            return r == 0 ? sources : Array.Empty<OmniPcmPlaylistSourceInfo>();
        }

        public void SetPlaylistSources(string instanceId, OmniPcmPlaylistSourceSpec[] sources)
        {
            ThrowIfDisposed();
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            Check(OmniPcmClient_SetPlaylistSources(_handle, instanceId, sources, sources.Length), "SetPlaylistSources");
        }

        // ── Equalizer ─────────────────────────────────────────────

        public (OmniPcmEqualizerStateInfo state, OmniPcmEqualizerPointInfo[] points) GetEqualizer(string instanceId)
        {
            ThrowIfDisposed();
            OmniPcmEqualizerStateInfo state = default;
            int pointCount = 0;
            OmniPcmClient_GetEqualizer(_handle, instanceId, ref state, null, ref pointCount);

            OmniPcmEqualizerPointInfo[] points;
            if (pointCount <= 0)
            {
                points = Array.Empty<OmniPcmEqualizerPointInfo>();
            }
            else
            {
                points = new OmniPcmEqualizerPointInfo[pointCount];
                int capacity = pointCount;
                Check(OmniPcmClient_GetEqualizer(_handle, instanceId, ref state, points, ref capacity), "GetEqualizer");
                if (capacity != pointCount)
                    Array.Resize(ref points, capacity);
            }
            return (state, points);
        }

        public void SetEqualizer(string instanceId, OmniPcmEqualizerStateInfo state, OmniPcmEqualizerPointInfo[] points)
        {
            ThrowIfDisposed();
            int pointCount = points?.Length ?? 0;
            Check(OmniPcmClient_SetEqualizer(_handle, instanceId, ref state, points, pointCount), "SetEqualizer");
        }

        // ── Backend ───────────────────────────────────────────────

        public OmniPcmBackendInfo GetBackendInfo()
        {
            ThrowIfDisposed();
            OmniPcmBackendInfo info = default;
            Check(OmniPcmClient_GetBackendInfo(_handle, ref info), "GetBackendInfo");
            return info;
        }

        public void StopBackend()
        {
            ThrowIfDisposed();
            Check(OmniPcmClient_StopBackend(_handle), "StopBackend");
        }

        // ── WebSocket events ──────────────────────────────────────

        public void StartEvents(Action<OmniPcmEventInfo> handler)
        {
            ThrowIfDisposed();
            if (_nativeCallbackDelegate != null) return;

            _nativeCallbackDelegate = (ref OmniPcmEventInfo info, IntPtr _) => handler(info);
            Check(OmniPcmClient_StartEvents(_handle, _nativeCallbackDelegate, IntPtr.Zero), "StartEvents");
        }

        public void StopEvents()
        {
            if (_nativeCallbackDelegate == null) return;
            OmniPcmClient_StopEvents(_handle);
            _nativeCallbackDelegate = null;
        }

        // ── Library queries ───────────────────────────────────────

        public OmniPcmTrackInfo[] QueryTracks(OmniPcmTrackQuery query)
        {
            ThrowIfDisposed();
            return GetList(
                (arr, ref count) => OmniPcmClient_QueryTracks(_handle, ref query, arr, ref count),
                () => new OmniPcmTrackInfo[0]);
        }

        public OmniPcmAlbumInfo[] QueryAlbums(OmniPcmLibraryQuery query)
        {
            ThrowIfDisposed();
            return GetList(
                (arr, ref count) => OmniPcmClient_QueryAlbums(_handle, ref query, arr, ref count),
                () => new OmniPcmAlbumInfo[0]);
        }

        public OmniPcmTagInfo[] QueryTags(OmniPcmLibraryQuery query)
        {
            ThrowIfDisposed();
            return GetList(
                (arr, ref count) => OmniPcmClient_QueryTags(_handle, ref query, arr, ref count),
                () => new OmniPcmTagInfo[0]);
        }

        public OmniPcmPlaylistInfo[] QueryPlaylists(OmniPcmLibraryQuery query)
        {
            ThrowIfDisposed();
            return GetList(
                (arr, ref count) => OmniPcmClient_QueryPlaylists(_handle, ref query, arr, ref count),
                () => new OmniPcmPlaylistInfo[0]);
        }

        public OmniPcmTrackInfo GetTrack(string uuid)
        {
            ThrowIfDisposed();
            OmniPcmTrackInfo track = default;
            Check(OmniPcmClient_GetTrack(_handle, uuid, ref track), "GetTrack");
            return track;
        }

        public void SetTrackExcluded(string uuid, bool excluded)
        {
            ThrowIfDisposed();
            int r = OmniPcmClient_SetTrackExcluded(_handle, uuid, excluded ? 1 : 0);
            if (r != 0) Log?.Invoke($"[OmniPcmClient] SetTrackExcluded failed: {LastError}");
        }

        // ── Helpers ───────────────────────────────────────────────

        private delegate int ListDelegate<T>(T[] arr, ref int count);

        private T[] GetList<T>(ListDelegate<T> fetch, Func<T[]> empty)
        {
            int count = 0;
            fetch(null, ref count);
            if (count == 0) return empty();
            var arr = new T[count];
            int r = fetch(arr, ref count);
            return r == 0 ? arr : empty();
        }

        private void Check(int result, string operation)
        {
            if (result < 0)
                throw new InvalidOperationException($"{operation} failed: {LastError}");
        }

        // ══════════════════════════════════════════════════════════
        //  P/Invoke declarations
        // ══════════════════════════════════════════════════════════

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcmClient_Create(ref OmniPcmClientConfig config);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void OmniPcmClient_Destroy(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OmniPcmClient_GetLastError(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetPort(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ConnectInstance(
            IntPtr client, ref OmniPcmConnectOptions options, out OmniPcmConnectionInfo outInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_Heartbeat(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, ref int outAlive);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_DisconnectInstance(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_DeleteInstance(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, ref int outDeleted);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ListInstances(IntPtr client,
            [Out] OmniPcmInstanceSummaryInfo[] outInstances, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetProfile(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, ref OmniPcmInstanceProfileInfo outProfile);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_UpdateProfile(IntPtr client,
            ref OmniPcmInstanceProfileInfo profile, ref int outSaved);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ArchiveInstance(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPStr)] string label,
            ref OmniPcmInstanceProfileInfo outArchive);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ListArchives(IntPtr client,
            [Out] OmniPcmInstanceProfileInfo[] outArchives, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetArchive(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string archiveId, ref OmniPcmInstanceProfileInfo outArchive);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_DeleteArchive(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string archiveId, ref int outDeleted);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_InheritFromArchive(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string newInstanceId,
            [MarshalAs(UnmanagedType.LPStr)] string archiveId,
            ref OmniPcmInstanceProfileInfo outProfile);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetStatus(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, ref OmniPcmPlaybackStatusInfo outStatus);

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
        private static extern int OmniPcmClient_GetVolume(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, ref float outVolume);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetTargetLatency(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, float latency);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetTargetLatency(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, ref float outLatency);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetShuffle(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int enabled);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetRepeatMode(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int repeatMode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [Out] OmniPcmQueueTrackInfo[] outTracks, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_AddToQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPStr)] string uuid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_InsertIntoQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] uuids,
            int uuidCount, int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] uuids,
            int uuidCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_RemoveFromQueueIndex(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_RemoveFromQueueUuid(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPStr)] string uuid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_MoveInQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int fromIndex, int toIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ClearQueue(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetHistory(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [Out] OmniPcmQueueTrackInfo[] outTracks, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_RemoveFromHistory(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_MoveInHistory(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId, int fromIndex, int toIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_ClearHistory(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetPlaylistSources(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [Out] OmniPcmPlaylistSourceInfo[] outSources, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetPlaylistSources(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            [MarshalAs(UnmanagedType.LPArray)] OmniPcmPlaylistSourceSpec[] sources, int sourceCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetEqualizer(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            ref OmniPcmEqualizerStateInfo outState,
            [Out] OmniPcmEqualizerPointInfo[] outPoints, ref int inoutPointCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetEqualizer(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string instanceId,
            ref OmniPcmEqualizerStateInfo state,
            [MarshalAs(UnmanagedType.LPArray)] OmniPcmEqualizerPointInfo[] points, int pointCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetBackendInfo(IntPtr client,
            ref OmniPcmBackendInfo outInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_StopBackend(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_StartEvents(IntPtr client,
            [MarshalAs(UnmanagedType.FunctionPtr)] OmniPcmEventCallback callback, IntPtr userData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void OmniPcmClient_StopEvents(IntPtr client);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryTracks(IntPtr client,
            ref OmniPcmTrackQuery query,
            [Out] OmniPcmTrackInfo[] outTracks, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryAlbums(IntPtr client,
            ref OmniPcmLibraryQuery query,
            [Out] OmniPcmAlbumInfo[] outAlbums, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryTags(IntPtr client,
            ref OmniPcmLibraryQuery query,
            [Out] OmniPcmTagInfo[] outTags, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_QueryPlaylists(IntPtr client,
            ref OmniPcmLibraryQuery query,
            [Out] OmniPcmPlaylistInfo[] outPlaylists, ref int inoutCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_GetTrack(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string uuid, ref OmniPcmTrackInfo outTrack);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OmniPcmClient_SetTrackExcluded(IntPtr client,
            [MarshalAs(UnmanagedType.LPStr)] string uuid, int excluded);
    }
}
