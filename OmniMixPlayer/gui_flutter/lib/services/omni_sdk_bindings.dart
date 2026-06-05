// SPDX-License-Identifier: MIT
// Native dart:ffi bindings for OmniPcmShared.dll.

import 'dart:ffi';
import 'dart:io';

import 'package:ffi/ffi.dart' show Utf8;

final DynamicLibrary omniDll = () {
  if (Platform.isWindows) {
    return DynamicLibrary.open('OmniPcmShared.dll');
  }
  throw UnsupportedError('OmniPcmShared SDK only supports Windows');
}();

const omniPcmOk = 0;
const omniPcmNotReady = -2;
const OMNI_PCM_UUID_BYTES = 64;
const OMNI_PCM_TRACK_TITLE_SZ = 256;
const OMNI_PCM_TRACK_ARTIST_SZ = 256;
const OMNI_PCM_ALBUM_TITLE_SZ = 256;
const OMNI_PCM_TAG_NAME_SZ = 128;
const OMNI_PCM_PLAYLIST_NAME_SZ = 256;
const OMNI_PCM_RESOURCE_URI_SZ = 512;
const OMNI_PCM_COLOR_SZ = 32;

const omniPcmInstanceKindGameMod = 1;
const omniPcmInstanceKindGui = 2;

const omniPcmCapServerControlledPlayback = 1 << 0;
const omniPcmCapClientManagedPlayback = 1 << 1;
const omniPcmCapQueueManagement = 1 << 2;
const omniPcmCapPlaylistManagement = 1 << 3;
const omniPcmCapShuffle = 1 << 4;
const omniPcmCapRepeat = 1 << 5;
const omniPcmCapSeek = 1 << 6;
const omniPcmCapVolumeControl = 1 << 7;
const omniPcmCapEqualizer = 1 << 8;
const omniPcmCapMultiplePlaylists = 1 << 9;
const omniPcmCapTagFiltering = 1 << 10;
const omniPcmCapUnlimitedTags = 1 << 11;
const omniPcmCapAlbumFiltering = 1 << 12;
const omniPcmCapAudioPlayback = 1 << 13;

final class OmniPcmInfo extends Struct {
  @Int32()
  external int sampleRate;
  @Int32()
  external int channels;
  @Int32()
  external int bytesPerFrame;
  @Int32()
  external int bufferFrames;
  @Int64()
  external int totalFramesHint;
  @Int64()
  external int decodedTotalFrames;
  @Int64()
  external int effectiveTotalFrames;
}

final class OmniPcmSnapshot extends Struct {
  @Uint32()
  external int version;
  @Int32()
  external int sampleRate;
  @Int32()
  external int channels;
  @Int32()
  external int bytesPerFrame;
  @Int32()
  external int bufferFrames;
  @Int32()
  external int legacyPlayState;
  @Uint32()
  external int flags;
  @Int64()
  external int writeCursor;
  @Int64()
  external int readCursor;
  @Int64()
  external int streamId;
  @Int32()
  external int state;
  @Int32()
  external int errorCode;
  @Int64()
  external int totalFramesHint;
  @Int64()
  external int decodedTotalFrames;
  @Int64()
  external int finalWriteCursor;
  @Int64()
  external int audibleCursor;
  @Int64()
  external int seekFrame;
  @Int64()
  external int seekGeneration;
  @Int64()
  external int lastUpdateTick;
  @Int32()
  external int formatGeneration;
  @Array(OMNI_PCM_UUID_BYTES)
  external Array<Uint8> currentUuid;
}

final class OmniPcmClientConfig extends Struct {
  external Pointer<Utf8> host;
  @Int32()
  external int port;
  @Int32()
  external int timeoutMs;
}

final class OmniPcmConnectOptions extends Struct {
  external Pointer<Utf8> clientId;
  external Pointer<Utf8> modId;
  external Pointer<Utf8> gameName;
  external Pointer<Utf8> displayName;
  @Int32()
  external int kind;
  @Uint32()
  external int capabilityFlags;
  @Int32()
  external int noInstance;
}

final class OmniPcmConnectionInfo extends Struct {
  @Array(128)
  external Array<Uint8> instanceId;
  @Int32()
  external int isNew;
  @Int32()
  external int noInstance;
}

final class OmniPcmPlaybackStatusInfo extends Struct {
  @Array(OMNI_PCM_UUID_BYTES)
  external Array<Uint8> trackUuid;
  @Array(256)
  external Array<Uint8> title;
  @Array(256)
  external Array<Uint8> artist;
  @Array(128)
  external Array<Uint8> albumId;
  @Float()
  external double duration;
  @Float()
  external double position;
  @Int32()
  external int isPlaying;
  @Int32()
  external int shuffle;
  @Int32()
  external int repeatMode;
  @Float()
  external double volume;
}

final class OmniPcmInstanceSummaryInfo extends Struct {
  @Array(128)
  external Array<Uint8> instanceId;
  @Array(256)
  external Array<Uint8> displayName;
  @Array(128)
  external Array<Uint8> modId;
  @Array(256)
  external Array<Uint8> gameName;
  @Array(OMNI_PCM_UUID_BYTES)
  external Array<Uint8> currentTrackUuid;
  @Int32()
  external int kind;
  @Int32()
  external int isOnline;
  @Int32()
  external int queueCount;
  @Int32()
  external int mode;
  @Int64()
  external int connectedAt;
}

final class OmniPcmInstanceProfileInfo extends Struct {
  @Array(128)
  external Array<Uint8> instanceId;
  @Array(256)
  external Array<Uint8> displayName;
  @Array(128)
  external Array<Uint8> modId;
  @Array(256)
  external Array<Uint8> gameName;
  @Int32()
  external int kind;
  @Uint32()
  external int capabilityFlags;
  @Float()
  external double volume;
  @Float()
  external double targetLatency;
  @Int32()
  external int mode;
  @Int64()
  external int createdAt;
  @Int64()
  external int updatedAt;
}

final class OmniPcmQueueTrackInfo extends Struct {
  @Int32()
  external int index;
  @Array(OMNI_PCM_UUID_BYTES)
  external Array<Uint8> uuid;
  @Array(256)
  external Array<Uint8> title;
  @Array(256)
  external Array<Uint8> artist;
  @Array(128)
  external Array<Uint8> albumId;
  @Array(128)
  external Array<Uint8> moduleId;
  @Array(512)
  external Array<Uint8> coverUri;
  @Float()
  external double duration;
}

final class OmniPcmPlaylistSourceInfo extends Struct {
  @Array(128)
  external Array<Uint8> id;
  @Array(256)
  external Array<Uint8> name;
  @Array(256)
  external Array<Uint8> refId;
  @Int32()
  external int songCount;
  @Int32()
  external int kind;
}

final class OmniPcmPlaylistSourceSpec extends Struct {
  external Pointer<Utf8> id;
  external Pointer<Utf8> name;
  external Pointer<Utf8> refId;
  @Int32()
  external int kind;
  external Pointer<Pointer<Utf8>> uuids;
  @Int32()
  external int uuidCount;
}

final class OmniPcmEqualizerPointInfo extends Struct {
  @Array(64)
  external Array<Uint8> id;
  @Float()
  external double frequency;
  @Float()
  external double gainDb;
  @Float()
  external double q;
  @Int32()
  external int type;
}

final class OmniPcmEqualizerStateInfo extends Struct {
  @Int32()
  external int enabled;
  @Float()
  external double globalGainDb;
  @Int32()
  external int softClipEnabled;
}

final class OmniPcmBackendInfo extends Struct {
  @Array(32)
  external Array<Uint8> status;
  @Array(64)
  external Array<Uint8> version;
  @Array(128)
  external Array<Uint8> name;
  @Int64()
  external int timestamp;
}

final class OmniPcmEventInfo extends Struct {
  @Array(64)
  external Array<Uint8> type;
  @Int64()
  external int timestamp;
  @Array(128)
  external Array<Uint8> instanceId;
  @Array(OMNI_PCM_UUID_BYTES)
  external Array<Uint8> trackUuid;
  @Array(256)
  external Array<Uint8> title;
  @Array(256)
  external Array<Uint8> artist;
  @Array(128)
  external Array<Uint8> albumId;
  @Array(128)
  external Array<Uint8> moduleId;
  @Array(256)
  external Array<Uint8> sourceRefId;
  @Array(64)
  external Array<Uint8> changeType;
  @Array(256)
  external Array<Uint8> displayName;
  @Float()
  external double duration;
  @Float()
  external double position;
  @Int32()
  external int state;
  @Int32()
  external int queueLength;
  @Int32()
  external int backendRunning;
  @Int32()
  external int boolValue;
  @Int32()
  external int songCount;
  @Int32()
  external int instanceCount;
}

final class OmniPcmTrackInfo extends Struct {
  @Array(OMNI_PCM_UUID_BYTES)
  external Array<Uint8> uuid;
  @Array(OMNI_PCM_TRACK_TITLE_SZ)
  external Array<Uint8> title;
  @Array(OMNI_PCM_TRACK_ARTIST_SZ)
  external Array<Uint8> artist;
  @Array(128)
  external Array<Uint8> albumId;
  @Array(128)
  external Array<Uint8> moduleId;
  @Array(OMNI_PCM_RESOURCE_URI_SZ)
  external Array<Uint8> coverUri;
  @Int32()
  external int trackNumber;
  @Float()
  external double duration;
  @Int32()
  external int isExcluded;
  @Int64()
  external int createdAt;
  @Int64()
  external int lastPlayedAt;
}

final class OmniPcmAlbumInfo extends Struct {
  @Array(128)
  external Array<Uint8> id;
  @Array(OMNI_PCM_ALBUM_TITLE_SZ)
  external Array<Uint8> title;
  @Array(OMNI_PCM_TRACK_ARTIST_SZ)
  external Array<Uint8> artist;
  @Array(128)
  external Array<Uint8> moduleId;
  @Array(OMNI_PCM_RESOURCE_URI_SZ)
  external Array<Uint8> coverUri;
  @Int32()
  external int trackCount;
}

final class OmniPcmTagInfo extends Struct {
  @Array(128)
  external Array<Uint8> id;
  @Array(OMNI_PCM_TAG_NAME_SZ)
  external Array<Uint8> name;
  @Array(128)
  external Array<Uint8> moduleId;
  @Array(OMNI_PCM_COLOR_SZ)
  external Array<Uint8> color;
}

final class OmniPcmPlaylistInfo extends Struct {
  @Array(128)
  external Array<Uint8> id;
  @Array(OMNI_PCM_PLAYLIST_NAME_SZ)
  external Array<Uint8> name;
  @Array(128)
  external Array<Uint8> moduleId;
  @Array(OMNI_PCM_RESOURCE_URI_SZ)
  external Array<Uint8> coverUri;
  @Int32()
  external int trackCount;
}

final class OmniPcmTrackQuery extends Struct {
  external Pointer<Utf8> albumId;
  external Pointer<Utf8> tagId;
  external Pointer<Utf8> playlistId;
  external Pointer<Utf8> moduleId;
  @Int32()
  external int isExcluded;
  @Int32()
  external int limit;
  @Int32()
  external int offset;
}

final class OmniPcmLibraryQuery extends Struct {
  external Pointer<Utf8> moduleId;
  @Int32()
  external int limit;
  @Int32()
  external int offset;
}

final class OmniPcmClientHandle extends Opaque {}
final class OmniPcmHandle extends Opaque {}

typedef ClientHandle = Pointer<OmniPcmClientHandle>;
typedef PcmHandle = Pointer<OmniPcmHandle>;
typedef OmniPcmEventCallback = Pointer<
    NativeFunction<Void Function(Pointer<OmniPcmEventInfo>, Pointer<Void>)>>;

typedef _CreateClientNative = Pointer<OmniPcmClientHandle> Function(
    Pointer<OmniPcmClientConfig>);
typedef _CreateClientDart = ClientHandle Function(Pointer<OmniPcmClientConfig>);
typedef _DestroyClientNative = Void Function(Pointer<OmniPcmClientHandle>);
typedef _DestroyClientDart = void Function(ClientHandle);
typedef _GetErrorNative = Pointer<Utf8> Function(Pointer<OmniPcmClientHandle>);
typedef _GetErrorDart = Pointer<Utf8> Function(ClientHandle);
typedef _GetPortNative = Int32 Function(Pointer<OmniPcmClientHandle>);
typedef _GetPortDart = int Function(ClientHandle);

typedef _ConnectInstanceNative = Int32 Function(
    Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmConnectOptions>,
    Pointer<OmniPcmConnectionInfo>);
typedef _ConnectInstanceDart = int Function(
    ClientHandle, Pointer<OmniPcmConnectOptions>, Pointer<OmniPcmConnectionInfo>);
typedef _HeartbeatNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<Int32>);
typedef _HeartbeatDart = int Function(ClientHandle, Pointer<Utf8>, Pointer<Int32>);
typedef _DisconnectNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>);
typedef _DisconnectDart = int Function(ClientHandle, Pointer<Utf8>);
typedef _DeleteInstanceNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<Int32>);
typedef _DeleteInstanceDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<Int32>);
typedef _ListInstancesNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmInstanceSummaryInfo>, Pointer<Int32>);
typedef _ListInstancesDart = int Function(
    ClientHandle, Pointer<OmniPcmInstanceSummaryInfo>, Pointer<Int32>);
typedef _GetProfileNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);
typedef _GetProfileDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);
typedef _UpdateProfileNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmInstanceProfileInfo>, Pointer<Int32>);
typedef _UpdateProfileDart = int Function(
    ClientHandle, Pointer<OmniPcmInstanceProfileInfo>, Pointer<Int32>);
typedef _ArchiveInstanceNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);
typedef _ArchiveInstanceDart = int Function(ClientHandle, Pointer<Utf8>,
    Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);
typedef _ListArchivesNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmInstanceProfileInfo>, Pointer<Int32>);
typedef _ListArchivesDart = int Function(
    ClientHandle, Pointer<OmniPcmInstanceProfileInfo>, Pointer<Int32>);
typedef _GetArchiveNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);
typedef _GetArchiveDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);
typedef _DeleteArchiveNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<Int32>);
typedef _DeleteArchiveDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<Int32>);
typedef _InheritFromArchiveNative = Int32 Function(
    Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>,
    Pointer<Utf8>,
    Pointer<OmniPcmInstanceProfileInfo>);
typedef _InheritFromArchiveDart = int Function(ClientHandle, Pointer<Utf8>,
    Pointer<Utf8>, Pointer<OmniPcmInstanceProfileInfo>);

typedef _GetStatusNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<OmniPcmPlaybackStatusInfo>);
typedef _GetStatusDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<OmniPcmPlaybackStatusInfo>);
typedef _PlaybackCommandNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Int32);
typedef _PlaybackCommandDart = int Function(ClientHandle, Pointer<Utf8>, int);
typedef _PlayNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<Utf8>);
typedef _PlayDart = int Function(ClientHandle, Pointer<Utf8>, Pointer<Utf8>);
typedef _SeekNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Float);
typedef _SeekDart = int Function(ClientHandle, Pointer<Utf8>, double);
typedef _SetFloatNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Float);
typedef _SetFloatDart = int Function(ClientHandle, Pointer<Utf8>, double);
typedef _GetFloatNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<Float>);
typedef _GetFloatDart = int Function(ClientHandle, Pointer<Utf8>, Pointer<Float>);
typedef _SetBoolNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Int32);
typedef _SetBoolDart = int Function(ClientHandle, Pointer<Utf8>, int);

typedef _GetQueueNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<OmniPcmQueueTrackInfo>, Pointer<Int32>);
typedef _GetQueueDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<OmniPcmQueueTrackInfo>, Pointer<Int32>);
typedef _AddToQueueNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<Utf8>);
typedef _AddToQueueDart = int Function(ClientHandle, Pointer<Utf8>, Pointer<Utf8>);
typedef _InsertIntoQueueNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<Pointer<Utf8>>, Int32, Int32);
typedef _InsertIntoQueueDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<Pointer<Utf8>>, int, int);
typedef _SetQueueNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<Pointer<Utf8>>, Int32);
typedef _SetQueueDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<Pointer<Utf8>>, int);
typedef _RemoveIndexNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Int32);
typedef _RemoveIndexDart = int Function(ClientHandle, Pointer<Utf8>, int);
typedef _MoveNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Int32, Int32);
typedef _MoveDart = int Function(ClientHandle, Pointer<Utf8>, int, int);
typedef _ClearNative = Int32 Function(Pointer<OmniPcmClientHandle>, Pointer<Utf8>);
typedef _ClearDart = int Function(ClientHandle, Pointer<Utf8>);

typedef _GetPlaylistSourcesNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<OmniPcmPlaylistSourceInfo>, Pointer<Int32>);
typedef _GetPlaylistSourcesDart = int Function(ClientHandle, Pointer<Utf8>,
    Pointer<OmniPcmPlaylistSourceInfo>, Pointer<Int32>);
typedef _SetPlaylistSourcesNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>, Pointer<OmniPcmPlaylistSourceSpec>, Int32);
typedef _SetPlaylistSourcesDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<OmniPcmPlaylistSourceSpec>, int);

typedef _GetEqualizerNative = Int32 Function(
    Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>,
    Pointer<OmniPcmEqualizerStateInfo>,
    Pointer<OmniPcmEqualizerPointInfo>,
    Pointer<Int32>);
typedef _GetEqualizerDart = int Function(
    ClientHandle,
    Pointer<Utf8>,
    Pointer<OmniPcmEqualizerStateInfo>,
    Pointer<OmniPcmEqualizerPointInfo>,
    Pointer<Int32>);
typedef _SetEqualizerNative = Int32 Function(
    Pointer<OmniPcmClientHandle>,
    Pointer<Utf8>,
    Pointer<OmniPcmEqualizerStateInfo>,
    Pointer<OmniPcmEqualizerPointInfo>,
    Int32);
typedef _SetEqualizerDart = int Function(
    ClientHandle,
    Pointer<Utf8>,
    Pointer<OmniPcmEqualizerStateInfo>,
    Pointer<OmniPcmEqualizerPointInfo>,
    int);

typedef _QueryTracksNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmTrackQuery>, Pointer<OmniPcmTrackInfo>, Pointer<Int32>);
typedef _QueryTracksDart = int Function(
    ClientHandle, Pointer<OmniPcmTrackQuery>, Pointer<OmniPcmTrackInfo>, Pointer<Int32>);
typedef _QueryAlbumsNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmLibraryQuery>, Pointer<OmniPcmAlbumInfo>, Pointer<Int32>);
typedef _QueryAlbumsDart = int Function(
    ClientHandle, Pointer<OmniPcmLibraryQuery>, Pointer<OmniPcmAlbumInfo>, Pointer<Int32>);
typedef _QueryTagsNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmLibraryQuery>, Pointer<OmniPcmTagInfo>, Pointer<Int32>);
typedef _QueryTagsDart = int Function(
    ClientHandle, Pointer<OmniPcmLibraryQuery>, Pointer<OmniPcmTagInfo>, Pointer<Int32>);
typedef _QueryPlaylistsNative = Int32 Function(Pointer<OmniPcmClientHandle>,
    Pointer<OmniPcmLibraryQuery>, Pointer<OmniPcmPlaylistInfo>, Pointer<Int32>);
typedef _QueryPlaylistsDart = int Function(ClientHandle,
    Pointer<OmniPcmLibraryQuery>, Pointer<OmniPcmPlaylistInfo>, Pointer<Int32>);
typedef _GetTrackNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Pointer<OmniPcmTrackInfo>);
typedef _GetTrackDart = int Function(
    ClientHandle, Pointer<Utf8>, Pointer<OmniPcmTrackInfo>);
typedef _SetTrackExcludedNative = Int32 Function(
    Pointer<OmniPcmClientHandle>, Pointer<Utf8>, Int32);
typedef _SetTrackExcludedDart = int Function(ClientHandle, Pointer<Utf8>, int);

typedef _OpenUtf8Native = Pointer<OmniPcmHandle> Function(Pointer<Utf8>);
typedef _OpenUtf8Dart = PcmHandle Function(Pointer<Utf8>);
typedef _ClosePcmNative = Void Function(Pointer<OmniPcmHandle>);
typedef _ClosePcmDart = void Function(PcmHandle);
typedef _GetInfoNative = Int32 Function(Pointer<OmniPcmHandle>, Pointer<OmniPcmInfo>);
typedef _GetInfoDart = int Function(PcmHandle, Pointer<OmniPcmInfo>);
typedef _GetSnapshotNative = Int32 Function(
    Pointer<OmniPcmHandle>, Pointer<OmniPcmSnapshot>);
typedef _GetSnapshotDart = int Function(
    PcmHandle, Pointer<OmniPcmSnapshot>);
typedef _NoArgPcmNative = Int32 Function(Pointer<OmniPcmHandle>);
typedef _NoArgPcmDart = int Function(PcmHandle);
typedef _WaitFormatNative = Int32 Function(
    Pointer<OmniPcmHandle>, Pointer<Utf8>, Int32);
typedef _WaitFormatDart = int Function(PcmHandle, Pointer<Utf8>, int);
typedef _ReadFramesNative = Int64 Function(
    Pointer<OmniPcmHandle>, Pointer<Float>, Int32);
typedef _ReadFramesDart = int Function(PcmHandle, Pointer<Float>, int);

final omniClientCreate =
    omniDll.lookupFunction<_CreateClientNative, _CreateClientDart>('OmniPcmClient_Create');
final omniClientDestroy =
    omniDll.lookupFunction<_DestroyClientNative, _DestroyClientDart>('OmniPcmClient_Destroy');
final omniClientLastError =
    omniDll.lookupFunction<_GetErrorNative, _GetErrorDart>('OmniPcmClient_GetLastError');
final omniClientGetPort =
    omniDll.lookupFunction<_GetPortNative, _GetPortDart>('OmniPcmClient_GetPort');
final omniClientConnect = omniDll.lookupFunction<_ConnectInstanceNative,
    _ConnectInstanceDart>('OmniPcmClient_ConnectInstance');
final omniClientHeartbeat =
    omniDll.lookupFunction<_HeartbeatNative, _HeartbeatDart>('OmniPcmClient_Heartbeat');
final omniClientDisconnect =
    omniDll.lookupFunction<_DisconnectNative, _DisconnectDart>('OmniPcmClient_DisconnectInstance');
final omniClientDeleteInstance = omniDll.lookupFunction<_DeleteInstanceNative,
    _DeleteInstanceDart>('OmniPcmClient_DeleteInstance');
final omniClientListInstances = omniDll.lookupFunction<_ListInstancesNative,
    _ListInstancesDart>('OmniPcmClient_ListInstances');
final omniClientGetProfile =
    omniDll.lookupFunction<_GetProfileNative, _GetProfileDart>('OmniPcmClient_GetProfile');
final omniClientUpdateProfile =
    omniDll.lookupFunction<_UpdateProfileNative, _UpdateProfileDart>('OmniPcmClient_UpdateProfile');
final omniClientArchiveInstance = omniDll.lookupFunction<_ArchiveInstanceNative,
    _ArchiveInstanceDart>('OmniPcmClient_ArchiveInstance');
final omniClientListArchives =
    omniDll.lookupFunction<_ListArchivesNative, _ListArchivesDart>('OmniPcmClient_ListArchives');
final omniClientGetArchive =
    omniDll.lookupFunction<_GetArchiveNative, _GetArchiveDart>('OmniPcmClient_GetArchive');
final omniClientDeleteArchive =
    omniDll.lookupFunction<_DeleteArchiveNative, _DeleteArchiveDart>('OmniPcmClient_DeleteArchive');
final omniClientInheritFromArchive = omniDll.lookupFunction<
    _InheritFromArchiveNative,
    _InheritFromArchiveDart>('OmniPcmClient_InheritFromArchive');
final omniClientGetStatus =
    omniDll.lookupFunction<_GetStatusNative, _GetStatusDart>('OmniPcmClient_GetStatus');
final omniClientPlaybackCommand = omniDll.lookupFunction<_PlaybackCommandNative,
    _PlaybackCommandDart>('OmniPcmClient_PlaybackCommand');
final omniClientPlay = omniDll.lookupFunction<_PlayNative, _PlayDart>('OmniPcmClient_Play');
final omniClientSeek = omniDll.lookupFunction<_SeekNative, _SeekDart>('OmniPcmClient_Seek');
final omniClientSetVolume =
    omniDll.lookupFunction<_SetFloatNative, _SetFloatDart>('OmniPcmClient_SetVolume');
final omniClientGetVolume =
    omniDll.lookupFunction<_GetFloatNative, _GetFloatDart>('OmniPcmClient_GetVolume');
final omniClientSetTargetLatency = omniDll.lookupFunction<_SetFloatNative,
    _SetFloatDart>('OmniPcmClient_SetTargetLatency');
final omniClientGetTargetLatency = omniDll.lookupFunction<_GetFloatNative,
    _GetFloatDart>('OmniPcmClient_GetTargetLatency');
final omniClientSetShuffle =
    omniDll.lookupFunction<_SetBoolNative, _SetBoolDart>('OmniPcmClient_SetShuffle');
final omniClientSetRepeatMode =
    omniDll.lookupFunction<_SetBoolNative, _SetBoolDart>('OmniPcmClient_SetRepeatMode');
final omniClientGetQueue =
    omniDll.lookupFunction<_GetQueueNative, _GetQueueDart>('OmniPcmClient_GetQueue');
final omniClientAddToQueue =
    omniDll.lookupFunction<_AddToQueueNative, _AddToQueueDart>('OmniPcmClient_AddToQueue');
final omniClientInsertIntoQueue = omniDll.lookupFunction<_InsertIntoQueueNative,
    _InsertIntoQueueDart>('OmniPcmClient_InsertIntoQueue');
final omniClientSetQueue =
    omniDll.lookupFunction<_SetQueueNative, _SetQueueDart>('OmniPcmClient_SetQueue');
final omniClientRemoveFromQueueIndex = omniDll.lookupFunction<_RemoveIndexNative,
    _RemoveIndexDart>('OmniPcmClient_RemoveFromQueueIndex');
final omniClientMoveInQueue =
    omniDll.lookupFunction<_MoveNative, _MoveDart>('OmniPcmClient_MoveInQueue');
final omniClientClearQueue =
    omniDll.lookupFunction<_ClearNative, _ClearDart>('OmniPcmClient_ClearQueue');
final omniClientGetHistory =
    omniDll.lookupFunction<_GetQueueNative, _GetQueueDart>('OmniPcmClient_GetHistory');
final omniClientRemoveFromHistory =
    omniDll.lookupFunction<_RemoveIndexNative, _RemoveIndexDart>('OmniPcmClient_RemoveFromHistory');
final omniClientMoveInHistory =
    omniDll.lookupFunction<_MoveNative, _MoveDart>('OmniPcmClient_MoveInHistory');
final omniClientClearHistory =
    omniDll.lookupFunction<_ClearNative, _ClearDart>('OmniPcmClient_ClearHistory');
final omniClientGetPlaylistSources = omniDll.lookupFunction<
    _GetPlaylistSourcesNative,
    _GetPlaylistSourcesDart>('OmniPcmClient_GetPlaylistSources');
final omniClientSetPlaylistSources = omniDll.lookupFunction<
    _SetPlaylistSourcesNative,
    _SetPlaylistSourcesDart>('OmniPcmClient_SetPlaylistSources');
final omniClientGetEqualizer =
    omniDll.lookupFunction<_GetEqualizerNative, _GetEqualizerDart>('OmniPcmClient_GetEqualizer');
final omniClientSetEqualizer =
    omniDll.lookupFunction<_SetEqualizerNative, _SetEqualizerDart>('OmniPcmClient_SetEqualizer');
final omniClientQueryTracks =
    omniDll.lookupFunction<_QueryTracksNative, _QueryTracksDart>('OmniPcmClient_QueryTracks');
final omniClientQueryAlbums =
    omniDll.lookupFunction<_QueryAlbumsNative, _QueryAlbumsDart>('OmniPcmClient_QueryAlbums');
final omniClientQueryTags =
    omniDll.lookupFunction<_QueryTagsNative, _QueryTagsDart>('OmniPcmClient_QueryTags');
final omniClientQueryPlaylists = omniDll.lookupFunction<_QueryPlaylistsNative,
    _QueryPlaylistsDart>('OmniPcmClient_QueryPlaylists');
final omniClientGetTrack =
    omniDll.lookupFunction<_GetTrackNative, _GetTrackDart>('OmniPcmClient_GetTrack');
final omniClientSetTrackExcluded = omniDll.lookupFunction<_SetTrackExcludedNative,
    _SetTrackExcludedDart>('OmniPcmClient_SetTrackExcluded');

final omniPcmOpenUtf8 =
    omniDll.lookupFunction<_OpenUtf8Native, _OpenUtf8Dart>('OmniPcm_OpenUtf8');
final omniPcmClose =
    omniDll.lookupFunction<_ClosePcmNative, _ClosePcmDart>('OmniPcm_Close');
final omniPcmGetInfo =
    omniDll.lookupFunction<_GetInfoNative, _GetInfoDart>('OmniPcm_GetInfo');
final omniPcmGetSnapshot = omniDll.lookupFunction<_GetSnapshotNative,
    _GetSnapshotDart>('OmniPcm_GetSnapshot');
final omniPcmBindCurrentStream =
    omniDll.lookupFunction<_NoArgPcmNative, _NoArgPcmDart>('OmniPcm_BindCurrentStream');
final omniPcmIsFormatReady =
    omniDll.lookupFunction<_NoArgPcmNative, _NoArgPcmDart>('OmniPcm_IsFormatReady');
final omniPcmHasError =
    omniDll.lookupFunction<_NoArgPcmNative, _NoArgPcmDart>('OmniPcm_HasError');
final omniPcmWaitForFormatReady =
    omniDll.lookupFunction<_WaitFormatNative, _WaitFormatDart>('OmniPcm_WaitForFormatReady');
final omniPcmReadFrames =
    omniDll.lookupFunction<_ReadFramesNative, _ReadFramesDart>('OmniPcm_ReadFrames');
