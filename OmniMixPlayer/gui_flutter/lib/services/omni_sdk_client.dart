// High-level Dart wrapper around OmniPcmShared SDK.
// Native only; on web, omni_sdk_client_stub.dart is used instead.
//
// Methods mirror the GrpcServices interface so api_client.dart
// can switch between SDK (native) and gRPC-Web (web) transparently.

import 'dart:async';
import 'dart:convert';
import 'dart:ffi';
import 'dart:isolate';
import 'dart:typed_data';

import 'package:ffi/ffi.dart';
import 'package:fixnum/fixnum.dart' as fixnum;

import '../generated/omni_mix_player/models/common.pb.dart';
import '../generated/omni_mix_player/models/track.pb.dart';
import '../generated/omni_mix_player/models/album.pb.dart';
import '../generated/omni_mix_player/models/tag.pb.dart';
import '../generated/omni_mix_player/models/playlist.pb.dart';
import '../generated/omni_mix_player/models/query.pb.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../generated/omni_mix_player/services/library.pb.dart';
import '../generated/omni_mix_player/services/playback.pb.dart';
import '../generated/omni_mix_player/services/instance.pb.dart';
import 'omni_sdk_bindings.dart';

// Helpers

String _readArray(Array<Uint8> arr, int maxLen) {
  final list = <int>[];
  for (int i = 0; i < maxLen; i++) {
    final c = arr[i];
    if (c == 0) break;
    list.add(c);
  }
  try {
    return utf8.decode(list);
  } catch (_) {
    return String.fromCharCodes(list);
  }
}

Pointer<Utf8> _str(String? s) =>
    s == null ? nullptr : s.toNativeUtf8(allocator: calloc);
void _freeStr(Pointer<Utf8> p) {
  if (p.address != 0) calloc.free(p);
}

void _freeList(List<Pointer<Utf8>> ptrs) {
  for (final p in ptrs) {
    calloc.free(p);
  }
}

bool _ok(int r) => r == 0;

void _check(int r) {
  if (!_ok(r)) throw OmniSdkException('SDK call failed: $r');
}

void _writeFixedString(Array<Uint8> dst, int maxLen, String value) {
  for (int i = 0; i < maxLen; i++) {
    dst[i] = 0;
  }
  final bytes = utf8.encode(value);
  final n = bytes.length < maxLen - 1 ? bytes.length : maxLen - 1;
  for (int i = 0; i < n; i++) {
    dst[i] = bytes[i] & 0xff;
  }
}

int _capFlags(InstanceCapabilities caps) {
  var flags = 0;
  if (caps.serverControlledPlayback) {
    flags |= omniPcmCapServerControlledPlayback;
  }
  if (caps.queueManagement) flags |= omniPcmCapQueueManagement;
  if (caps.playlistManagement) flags |= omniPcmCapPlaylistManagement;
  if (caps.shuffle) flags |= omniPcmCapShuffle;
  if (caps.repeat) flags |= omniPcmCapRepeat;
  if (caps.seek) flags |= omniPcmCapSeek;
  if (caps.volumeControl) flags |= omniPcmCapVolumeControl;
  if (caps.equalizer) flags |= omniPcmCapEqualizer;
  if (caps.multiplePlaylists) flags |= omniPcmCapMultiplePlaylists;
  if (caps.tagFiltering) flags |= omniPcmCapTagFiltering;
  if (caps.unlimitedTags) flags |= omniPcmCapUnlimitedTags;
  if (caps.albumFiltering) flags |= omniPcmCapAlbumFiltering;
  if (caps.audioPlayback) flags |= omniPcmCapAudioPlayback;
  return flags;
}

// Track helpers

Track _cTrackToProto(Pointer<OmniPcmTrackInfo> p) {
  final t = p.ref;
  return Track()
    ..uuid = _readArray(t.uuid, OMNI_PCM_UUID_BYTES)
    ..title = _readArray(t.title, OMNI_PCM_TRACK_TITLE_SZ)
    ..artist = _readArray(t.artist, OMNI_PCM_TRACK_ARTIST_SZ)
    ..albumId = _readArray(t.albumId, 128)
    ..moduleId = _readArray(t.moduleId, 128)
    ..coverUri = _readArray(t.coverUri, OMNI_PCM_RESOURCE_URI_SZ)
    ..duration = t.duration
    ..isExcluded = t.isExcluded != 0
    ..createdAt = _omniTimestamp(t.createdAt)
    ..lastPlayedAt = _omniTimestamp(t.lastPlayedAt);
}

OmniTimestamp _omniTimestamp(int seconds) =>
    OmniTimestamp()..seconds = fixnum.Int64(seconds);

Album _cAlbumToProto(Pointer<OmniPcmAlbumInfo> p) {
  final a = p.ref;
  return Album()
    ..id = _readArray(a.id, 128)
    ..title = _readArray(a.title, OMNI_PCM_ALBUM_TITLE_SZ)
    ..artist = _readArray(a.artist, OMNI_PCM_TRACK_ARTIST_SZ)
    ..moduleId = _readArray(a.moduleId, 128)
    ..coverUri = _readArray(a.coverUri, OMNI_PCM_RESOURCE_URI_SZ);
}

Tag _cTagToProto(Pointer<OmniPcmTagInfo> p) {
  final t = p.ref;
  return Tag()
    ..id = _readArray(t.id, 128)
    ..name = _readArray(t.name, OMNI_PCM_TAG_NAME_SZ)
    ..moduleId = _readArray(t.moduleId, 128)
    ..color = _readArray(t.color, OMNI_PCM_COLOR_SZ);
}

Playlist _cPlaylistToProto(Pointer<OmniPcmPlaylistInfo> p) {
  final pl = p.ref;
  return Playlist()
    ..id = _readArray(pl.id, 128)
    ..name = _readArray(pl.name, OMNI_PCM_PLAYLIST_NAME_SZ)
    ..moduleId = _readArray(pl.moduleId, 128)
    ..coverUri = _readArray(pl.coverUri, OMNI_PCM_RESOURCE_URI_SZ);
}

InstanceSummary _cSummaryToProto(Pointer<OmniPcmInstanceSummaryInfo> p) {
  final s = p.ref;
  return InstanceSummary()
    ..id = _readArray(s.instanceId, 128)
    ..displayName = _readArray(s.displayName, 256)
    ..modId = _readArray(s.modId, 128)
    ..gameName = _readArray(s.gameName, 256)
    ..currentTrackUuid = _readArray(s.currentTrackUuid, OMNI_PCM_UUID_BYTES)
    ..kind =
        InstanceKind.valueOf(s.kind) ?? InstanceKind.INSTANCE_KIND_UNSPECIFIED
    ..isOnline = s.isOnline != 0
    ..queueCount = s.queueCount
    ..connectedAt = _omniTimestamp(s.connectedAt);
}

InstanceProfile _cProfileToProto(Pointer<OmniPcmInstanceProfileInfo> p) {
  final prof = p.ref;
  final caps = InstanceCapabilities();
  final f = prof.capabilityFlags;
  caps.serverControlledPlayback = (f & 1) != 0;
  caps.queueManagement = (f & 4) != 0;
  caps.playlistManagement = (f & 8) != 0;
  caps.shuffle = (f & 16) != 0;
  caps.repeat = (f & 32) != 0;
  caps.seek = (f & 64) != 0;
  caps.volumeControl = (f & 128) != 0;
  caps.equalizer = (f & 256) != 0;
  caps.multiplePlaylists = (f & 512) != 0;
  caps.tagFiltering = (f & 1024) != 0;
  caps.unlimitedTags = (f & 2048) != 0;
  caps.albumFiltering = (f & 4096) != 0;
  caps.audioPlayback = (f & omniPcmCapAudioPlayback) != 0;
  caps.customSystemMediaService = (f & omniPcmCapCustomSystemMediaService) != 0;
  caps.maxImportedPlaylists = prof.maxImportedPlaylists;
  caps.maxTags = prof.maxTags;
  caps.maxPlaylistEntries = prof.maxPlaylistEntries;

  return InstanceProfile()
    ..id = _readArray(prof.instanceId, 128)
    ..displayName = _readArray(prof.displayName, 256)
    ..modId = _readArray(prof.modId, 128)
    ..gameName = _readArray(prof.gameName, 256)
    ..kind =
        InstanceKind.valueOf(prof.kind) ??
        InstanceKind.INSTANCE_KIND_UNSPECIFIED
    ..capabilities = caps
    ..volume = prof.volume
    ..targetLatency = prof.targetLatency
    ..createdAt = _omniTimestamp(prof.createdAt)
    ..updatedAt = _omniTimestamp(prof.updatedAt);
}

PlaybackStatus _cStatusToProto(Pointer<OmniPcmPlaybackStatusInfo> p) {
  final s = p.ref;
  return PlaybackStatus()
    ..trackUuid = _readArray(s.trackUuid, OMNI_PCM_UUID_BYTES)
    ..title = _readArray(s.title, 256)
    ..artist = _readArray(s.artist, 256)
    ..albumId = _readArray(s.albumId, 128)
    ..duration = s.duration
    ..position = s.position
    ..isPlaying = s.isPlaying != 0
    ..shuffle = s.shuffle != 0
    ..repeatMode =
        RepeatMode.valueOf(s.repeatMode) ?? RepeatMode.REPEAT_MODE_NONE
    ..volume = s.volume;
}

QueueTrack _cQueueToProto(Pointer<OmniPcmQueueTrackInfo> p) {
  final q = p.ref;
  return QueueTrack()
    ..index = q.index
    ..uuid = _readArray(q.uuid, OMNI_PCM_UUID_BYTES)
    ..title = _readArray(q.title, 256)
    ..artist = _readArray(q.artist, 256)
    ..albumId = _readArray(q.albumId, 128)
    ..moduleId = _readArray(q.moduleId, 128)
    ..coverUri = _readArray(q.coverUri, 512)
    ..duration = q.duration;
}

PlaylistSourceInfo _cPlaylistSourceToProto(
  Pointer<OmniPcmPlaylistSourceInfo> p,
) {
  final s = p.ref;
  return PlaylistSourceInfo()
    ..id = _readArray(s.id, 128)
    ..name = _readArray(s.name, 256)
    ..refId = _readArray(s.refId, 256)
    ..songCount = s.songCount
    ..kind =
        PlaylistSourceKind.valueOf(s.kind) ??
        PlaylistSourceKind.PLAYLIST_SOURCE_KIND_UNSPECIFIED;
}

EqualizerPoint _cEqPointToProto(Pointer<OmniPcmEqualizerPointInfo> p) {
  final e = p.ref;
  return EqualizerPoint()
    ..id = _readArray(e.id, 64)
    ..frequency = e.frequency
    ..gainDb = e.gainDb
    ..q = e.q
    ..type =
        EqualizerFilterType.valueOf(e.type) ??
        EqualizerFilterType.EQ_FILTER_TYPE_PEAKING;
}

// All capability flags for Flutter GUI

const _fullGuiCaps =
    omniPcmCapServerControlledPlayback |
    omniPcmCapQueueManagement |
    omniPcmCapPlaylistManagement |
    omniPcmCapShuffle |
    omniPcmCapRepeat |
    omniPcmCapSeek |
    omniPcmCapVolumeControl |
    omniPcmCapEqualizer |
    omniPcmCapMultiplePlaylists |
    omniPcmCapTagFiltering |
    omniPcmCapUnlimitedTags |
    omniPcmCapAlbumFiltering |
    omniPcmCapAudioPlayback |
    omniPcmCapCustomSystemMediaService;

// SDK Client

class _RawOmniSdkClient {
  ClientHandle? _handle;
  final String _clientId;
  String _instanceId = '';

  _RawOmniSdkClient({this._clientId = 'flutter'}) {
    final config = calloc<OmniPcmClientConfig>();
    config.ref.host = nullptr; // default: 127.0.0.1
    config.ref.port = 0; // discover
    config.ref.timeoutMs = 6000;
    _handle = omniClientCreate(config);
    calloc.free(config);
  }

  ClientHandle get _h => _handle!;

  // Lifecycle

  void dispose() {
    if (_handle != null) {
      omniClientDestroy(_h);
      _handle = null;
    }
  }

  Future<bool> heartbeat(String instanceId) async {
    final alive = calloc<Int32>();
    final iid = _str(instanceId);
    final r = omniClientHeartbeat(_h, iid, alive);
    _freeStr(iid);
    final isAlive = alive.value;
    calloc.free(alive);
    if (_ok(r)) {
      return isAlive != 0;
    }
    return false;
  }

  /// Auto-connect: if instance exists, reuse; otherwise register new.
  Future<InstanceProfile> ensureConnected({
    String modId = '',
    String gameName = 'Flutter GUI',
    String displayName = 'OmniMix GUI',
  }) async {
    if (_instanceId.isNotEmpty) {
      // Already connected; verify still alive
      final alive = calloc<Int32>();
      final iid = _str(_instanceId);
      final r = omniClientHeartbeat(_h, iid, alive);
      _freeStr(iid);
      final isAlive = alive.value;
      calloc.free(alive);
      if (_ok(r) && isAlive != 0) {
        return getProfile(_instanceId);
      }
    }

    // Check if flutter instance already exists
    final count = calloc<Int32>(1);
    omniClientListInstances(_h, nullptr, count);
    final total = count.value;
    calloc.free(count);

    if (total > 0) {
      final buf = calloc<OmniPcmInstanceSummaryInfo>(total);
      final cnt2 = calloc<Int32>(1)..value = total;
      _check(omniClientListInstances(_h, buf, cnt2));
      for (int i = 0; i < total; i++) {
        final id = _readArray(buf[i].instanceId, 128);
        if (id == _clientId) {
          if (buf[i].isOnline != 0) {
            _instanceId = id;
            final prof = await getProfile(id);
            calloc.free(buf);
            calloc.free(cnt2);
            return prof;
          }
          break;
        }
      }
      calloc.free(buf);
      calloc.free(cnt2);
    }

    // Register new instance
    return connect(modId: modId, gameName: gameName, displayName: displayName);
  }

  Future<InstanceProfile> connect({
    String modId = '',
    String gameName = 'Flutter GUI',
    String displayName = 'OmniMix GUI',
  }) async {
    final opts = calloc<OmniPcmConnectOptions>();
    opts.ref.clientId = _str(_clientId);
    opts.ref.modId = _str(modId);
    opts.ref.gameName = _str(gameName);
    opts.ref.displayName = _str(displayName);
    opts.ref.kind = omniPcmInstanceKindGui;
    opts.ref.capabilityFlags = _fullGuiCaps;

    final info = calloc<OmniPcmConnectionInfo>();
    _check(omniClientConnect(_h, opts, info));
    _instanceId = _readArray(info.ref.instanceId, 128);

    _freeStr(opts.ref.clientId);
    _freeStr(opts.ref.modId);
    _freeStr(opts.ref.gameName);
    _freeStr(opts.ref.displayName);
    calloc.free(opts);
    calloc.free(info);

    return getProfile(_instanceId);
  }

  // InstanceService

  Future<InstanceProfile> getProfile(String instanceId) async {
    final iid = _str(instanceId);
    final p = calloc<OmniPcmInstanceProfileInfo>();
    _check(omniClientGetProfile(_h, iid, p));
    _freeStr(iid);
    final result = _cProfileToProto(p);
    calloc.free(p);

    try {
      final sources = await getPlaylistSources(instanceId);
      result.playbackTimeline = PlaybackTimelineState()
        ..version = 2
        ..playlistSources.addAll(
          sources.map(
            (s) => PlaylistSourceState()
              ..id = s.id
              ..name = s.name
              ..kind = s.kind
              ..refId = s.refId,
          ),
        );
    } catch (_) {}

    return result;
  }

  Future<void> updateProfile(String instanceId, InstanceProfile profile) async {
    final id = profile.id.isNotEmpty ? profile.id : instanceId;
    final iid = _str(id);
    final p = calloc<OmniPcmInstanceProfileInfo>();
    _check(omniClientGetProfile(_h, iid, p));
    _writeFixedString(p.ref.instanceId, 128, id);
    if (profile.displayName.isNotEmpty) {
      _writeFixedString(p.ref.displayName, 256, profile.displayName);
    }
    if (profile.modId.isNotEmpty) {
      _writeFixedString(p.ref.modId, 128, profile.modId);
    }
    if (profile.gameName.isNotEmpty) {
      _writeFixedString(p.ref.gameName, 256, profile.gameName);
    }
    if (profile.kind != InstanceKind.INSTANCE_KIND_UNSPECIFIED) {
      p.ref.kind = profile.kind.value;
    }
    if (profile.hasCapabilities()) {
      p.ref.capabilityFlags = _capFlags(profile.capabilities);
      final caps = profile.capabilities;
      if (caps.hasMaxImportedPlaylists()) {
        p.ref.maxImportedPlaylists = caps.maxImportedPlaylists;
      }
      if (caps.hasMaxTags()) p.ref.maxTags = caps.maxTags;
      if (caps.hasMaxPlaylistEntries()) {
        p.ref.maxPlaylistEntries = caps.maxPlaylistEntries;
      }
    }
    if (profile.volume != 0) {
      p.ref.volume = profile.volume;
    }
    if (profile.targetLatency != 0) {
      p.ref.targetLatency = profile.targetLatency;
    }
    final saved = calloc<Int32>();
    _check(omniClientUpdateProfile(_h, p, saved));
    calloc.free(saved);
    _freeStr(iid);
    calloc.free(p);
  }

  Future<List<InstanceSummary>> listInstances() async {
    final count = calloc<Int32>(1);
    omniClientListInstances(_h, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) return [];

    final buf = calloc<OmniPcmInstanceSummaryInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientListInstances(_h, buf, cnt2));
    final result = <InstanceSummary>[];
    for (int i = 0; i < total; i++) {
      result.add(_cSummaryToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    return result;
  }

  Future<List<InstanceProfile>> listArchives() async {
    final count = calloc<Int32>(1);
    omniClientListArchives(_h, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) return [];

    final buf = calloc<OmniPcmInstanceProfileInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientListArchives(_h, buf, cnt2));
    final result = <InstanceProfile>[];
    for (int i = 0; i < total; i++) {
      result.add(_cProfileToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    return result;
  }

  Future<InstanceProfile> getArchive(String archiveId) async {
    final aid = _str(archiveId);
    final p = calloc<OmniPcmInstanceProfileInfo>();
    _check(omniClientGetArchive(_h, aid, p));
    _freeStr(aid);
    final result = _cProfileToProto(p);
    calloc.free(p);
    return result;
  }

  Future<void> deleteArchive(String archiveId) async {
    final aid = _str(archiveId);
    final deleted = calloc<Int32>();
    _check(omniClientDeleteArchive(_h, aid, deleted));
    calloc.free(deleted);
    _freeStr(aid);
  }

  Future<void> deleteInstance(String instanceId) async {
    final iid = _str(instanceId);
    final deleted = calloc<Int32>();
    _check(omniClientDeleteInstance(_h, iid, deleted));
    calloc.free(deleted);
    _freeStr(iid);
  }

  Future<void> archiveInstance(String instanceId, String label) async {
    final iid = _str(instanceId);
    final lbl = _str(label);
    final out = calloc<OmniPcmInstanceProfileInfo>();
    _check(omniClientArchiveInstance(_h, iid, lbl, out));
    calloc.free(out);
    _freeStr(iid);
    _freeStr(lbl);
  }

  Future<InstanceProfile> inheritFromArchive(
    String newInstanceId,
    String archiveId,
  ) async {
    final nid = _str(newInstanceId);
    final aid = _str(archiveId);
    final out = calloc<OmniPcmInstanceProfileInfo>();
    _check(omniClientInheritFromArchive(_h, nid, aid, out));
    _freeStr(nid);
    _freeStr(aid);
    final result = _cProfileToProto(out);
    calloc.free(out);
    return result;
  }

  // PlaybackService

  Future<PlaybackStatus> getStatus(String instanceId) async {
    final iid = _str(instanceId);
    final p = calloc<OmniPcmPlaybackStatusInfo>();
    _check(omniClientGetStatus(_h, iid, p));
    _freeStr(iid);
    final result = _cStatusToProto(p);
    calloc.free(p);
    return result;
  }

  Future<void> play(String instanceId, {String uuid = ''}) async {
    final iid = _str(instanceId);
    if (uuid.isEmpty) {
      _check(omniClientPlaybackCommand(_h, iid, 1)); // OMNI_PCM_COMMAND_PLAY
    } else {
      final u = _str(uuid);
      _check(omniClientPlay(_h, iid, u));
      _freeStr(u);
    }
    _freeStr(iid);
  }

  Future<void> pause(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientPlaybackCommand(_h, iid, 2));
    _freeStr(iid);
  }

  Future<void> resume(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientPlaybackCommand(_h, iid, 3));
    _freeStr(iid);
  }

  Future<void> toggle(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientPlaybackCommand(_h, iid, 4));
    _freeStr(iid);
  }

  Future<void> next(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientPlaybackCommand(_h, iid, 5));
    _freeStr(iid);
  }

  Future<void> prev(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientPlaybackCommand(_h, iid, 6));
    _freeStr(iid);
  }

  Future<void> stop(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientPlaybackCommand(_h, iid, 7));
    _freeStr(iid);
  }

  Future<void> seek(String instanceId, double position) async {
    final iid = _str(instanceId);
    _check(omniClientSeek(_h, iid, position));
    _freeStr(iid);
  }

  Future<void> setVolume(String instanceId, double volume) async {
    final iid = _str(instanceId);
    _check(omniClientSetVolume(_h, iid, volume));
    _freeStr(iid);
  }

  Future<double> getVolume(String instanceId) async {
    final iid = _str(instanceId);
    final v = calloc<Float>();
    _check(omniClientGetVolume(_h, iid, v));
    _freeStr(iid);
    final result = v.value;
    calloc.free(v);
    return result;
  }

  Future<void> setTargetLatency(String instanceId, double latency) async {
    final iid = _str(instanceId);
    _check(omniClientSetTargetLatency(_h, iid, latency));
    _freeStr(iid);
  }

  Future<double> getTargetLatency(String instanceId) async {
    final iid = _str(instanceId);
    final v = calloc<Float>();
    _check(omniClientGetTargetLatency(_h, iid, v));
    _freeStr(iid);
    final result = v.value;
    calloc.free(v);
    return result;
  }

  Future<void> setShuffle(String instanceId, bool enabled) async {
    final iid = _str(instanceId);
    _check(omniClientSetShuffle(_h, iid, enabled ? 1 : 0));
    _freeStr(iid);
  }

  Future<void> setRepeatMode(String instanceId, int mode) async {
    final iid = _str(instanceId);
    _check(omniClientSetRepeatMode(_h, iid, mode));
    _freeStr(iid);
  }

  // Queue

  Future<List<QueueTrack>> getQueue(String instanceId) async {
    final iid = _str(instanceId);
    final count = calloc<Int32>(1);
    omniClientGetQueue(_h, iid, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeStr(iid);
      return [];
    }

    final buf = calloc<OmniPcmQueueTrackInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientGetQueue(_h, iid, buf, cnt2));
    final result = <QueueTrack>[];
    for (int i = 0; i < total; i++) {
      result.add(_cQueueToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeStr(iid);
    return result;
  }

  Future<void> addToQueue(String instanceId, String uuid) async {
    final iid = _str(instanceId);
    final u = _str(uuid);
    _check(omniClientAddToQueue(_h, iid, u));
    _freeStr(iid);
    _freeStr(u);
  }

  Future<void> insertIntoQueue(
    String instanceId,
    List<String> uuids,
    int index,
  ) async {
    final iid = _str(instanceId);
    final ptrs = uuids.map((u) => _str(u)).toList();
    final arr = calloc<Pointer<Utf8>>(ptrs.length);
    for (int i = 0; i < ptrs.length; i++) {
      arr[i] = ptrs[i];
    }
    _check(omniClientInsertIntoQueue(_h, iid, arr, ptrs.length, index));
    calloc.free(arr);
    _freeList(ptrs);
    _freeStr(iid);
  }

  Future<void> setQueue(String instanceId, List<String> uuids) async {
    final iid = _str(instanceId);
    final ptrs = uuids.map((u) => _str(u)).toList();
    final arr = calloc<Pointer<Utf8>>(ptrs.length);
    for (int i = 0; i < ptrs.length; i++) {
      arr[i] = ptrs[i];
    }
    _check(omniClientSetQueue(_h, iid, arr, ptrs.length));
    calloc.free(arr);
    _freeList(ptrs);
    _freeStr(iid);
  }

  Future<void> removeFromQueue(String instanceId, int index) async {
    final iid = _str(instanceId);
    _check(omniClientRemoveFromQueueIndex(_h, iid, index));
    _freeStr(iid);
  }

  Future<void> moveInQueue(String instanceId, int from, int to) async {
    final iid = _str(instanceId);
    _check(omniClientMoveInQueue(_h, iid, from, to));
    _freeStr(iid);
  }

  Future<void> clearQueue(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientClearQueue(_h, iid));
    _freeStr(iid);
  }

  // History

  Future<List<QueueTrack>> getHistory(String instanceId) async {
    final iid = _str(instanceId);
    final count = calloc<Int32>(1);
    omniClientGetHistory(_h, iid, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeStr(iid);
      return [];
    }

    final buf = calloc<OmniPcmQueueTrackInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientGetHistory(_h, iid, buf, cnt2));
    final result = <QueueTrack>[];
    for (int i = 0; i < total; i++) {
      result.add(_cQueueToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeStr(iid);
    return result;
  }

  Future<void> removeFromHistory(String instanceId, int index) async {
    final iid = _str(instanceId);
    _check(omniClientRemoveFromHistory(_h, iid, index));
    _freeStr(iid);
  }

  Future<void> moveInHistory(String instanceId, int from, int to) async {
    final iid = _str(instanceId);
    _check(omniClientMoveInHistory(_h, iid, from, to));
    _freeStr(iid);
  }

  Future<void> clearHistory(String instanceId) async {
    final iid = _str(instanceId);
    _check(omniClientClearHistory(_h, iid));
    _freeStr(iid);
  }

  Future<List<PlaylistSourceInfo>> getPlaylistSources(String instanceId) async {
    final iid = _str(instanceId);
    final count = calloc<Int32>(1);
    omniClientGetPlaylistSources(_h, iid, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeStr(iid);
      return [];
    }

    final buf = calloc<OmniPcmPlaylistSourceInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientGetPlaylistSources(_h, iid, buf, cnt2));
    final result = <PlaylistSourceInfo>[];
    for (int i = 0; i < cnt2.value; i++) {
      result.add(_cPlaylistSourceToProto(buf + i));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeStr(iid);
    return result;
  }

  Future<void> setPlaylistSources(
    String instanceId,
    List<PlaylistSourceSpec> sources,
  ) async {
    final iid = _str(instanceId);
    final specs = calloc<OmniPcmPlaylistSourceSpec>(sources.length);
    final ownedStrings = <Pointer<Utf8>>[];
    final ownedArrays = <Pointer<Pointer<Utf8>>>[];
    for (int i = 0; i < sources.length; i++) {
      final src = sources[i];
      final id = _str(src.id);
      final name = _str(src.name);
      final refId = _str(src.refId);
      ownedStrings.addAll([id, name, refId]);
      specs[i].id = id;
      specs[i].name = name;
      specs[i].refId = refId;
      specs[i].kind = src.kind.value;
      specs[i].uuidCount = src.uuids.length;
      if (src.uuids.isNotEmpty) {
        final arr = calloc<Pointer<Utf8>>(src.uuids.length);
        ownedArrays.add(arr);
        for (int j = 0; j < src.uuids.length; j++) {
          final u = _str(src.uuids[j]);
          ownedStrings.add(u);
          arr[j] = u;
        }
        specs[i].uuids = arr;
      } else {
        specs[i].uuids = nullptr;
      }
    }
    _check(omniClientSetPlaylistSources(_h, iid, specs, sources.length));
    for (final arr in ownedArrays) {
      calloc.free(arr);
    }
    _freeList(ownedStrings);
    calloc.free(specs);
    _freeStr(iid);
  }

  Future<EqualizerState> getEqualizer(String instanceId) async {
    final iid = _str(instanceId);
    final state = calloc<OmniPcmEqualizerStateInfo>();
    final count = calloc<Int32>(1);
    omniClientGetEqualizer(_h, iid, state, nullptr, count);
    final total = count.value;
    final result = EqualizerState()
      ..enabled = state.ref.enabled != 0
      ..globalGainDb = state.ref.globalGainDb
      ..softClipEnabled = state.ref.softClipEnabled != 0;
    if (total > 0) {
      final points = calloc<OmniPcmEqualizerPointInfo>(total);
      count.value = total;
      _check(omniClientGetEqualizer(_h, iid, state, points, count));
      result
        ..enabled = state.ref.enabled != 0
        ..globalGainDb = state.ref.globalGainDb
        ..softClipEnabled = state.ref.softClipEnabled != 0;
      for (int i = 0; i < count.value; i++) {
        result.points.add(_cEqPointToProto(points + i));
      }
      calloc.free(points);
    }
    calloc.free(state);
    calloc.free(count);
    _freeStr(iid);
    return result;
  }

  Future<void> setEqualizer(String instanceId, EqualizerState eq) async {
    final iid = _str(instanceId);
    final state = calloc<OmniPcmEqualizerStateInfo>();
    state.ref.enabled = eq.enabled ? 1 : 0;
    state.ref.globalGainDb = eq.globalGainDb;
    state.ref.softClipEnabled = eq.softClipEnabled ? 1 : 0;
    final points = calloc<OmniPcmEqualizerPointInfo>(eq.points.length);
    for (int i = 0; i < eq.points.length; i++) {
      final p = eq.points[i];
      _writeFixedString(points[i].id, 64, p.id);
      points[i].frequency = p.frequency;
      points[i].gainDb = p.gainDb;
      points[i].q = p.q;
      points[i].type = p.type.value;
    }
    _check(omniClientSetEqualizer(_h, iid, state, points, eq.points.length));
    calloc.free(points);
    calloc.free(state);
    _freeStr(iid);
  }

  // Library

  Future<List<Track>> queryTracks({
    String albumId = '',
    String tagId = '',
    String playlistId = '',
    String moduleId = '',
    int isExcluded = -1,
    int limit = 0,
    int offset = 0,
  }) async {
    final q = calloc<OmniPcmTrackQuery>();
    q.ref.albumId = _str(albumId.isEmpty ? null : albumId);
    q.ref.tagId = _str(tagId.isEmpty ? null : tagId);
    q.ref.playlistId = _str(playlistId.isEmpty ? null : playlistId);
    q.ref.moduleId = _str(moduleId.isEmpty ? null : moduleId);
    q.ref.isExcluded = isExcluded;
    q.ref.limit = limit;
    q.ref.offset = offset;

    final count = calloc<Int32>(1);
    omniClientQueryTracks(_h, q, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeQuery(q);
      return [];
    }

    final buf = calloc<OmniPcmTrackInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientQueryTracks(_h, q, buf, cnt2));
    final result = <Track>[];
    for (int i = 0; i < total; i++) {
      result.add(_cTrackToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeQuery(q);
    return result;
  }

  Future<List<Album>> queryAlbums({
    String moduleId = '',
    int limit = 0,
    int offset = 0,
  }) async {
    final q = calloc<OmniPcmLibraryQuery>();
    q.ref.moduleId = _str(moduleId.isEmpty ? null : moduleId);
    q.ref.limit = limit;
    q.ref.offset = offset;

    final count = calloc<Int32>(1);
    omniClientQueryAlbums(_h, q, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeLibQuery(q);
      return [];
    }

    final buf = calloc<OmniPcmAlbumInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientQueryAlbums(_h, q, buf, cnt2));
    final result = <Album>[];
    for (int i = 0; i < total; i++) {
      result.add(_cAlbumToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeLibQuery(q);
    return result;
  }

  Future<List<Tag>> queryTags({
    String moduleId = '',
    int limit = 0,
    int offset = 0,
  }) async {
    final q = calloc<OmniPcmLibraryQuery>();
    q.ref.moduleId = _str(moduleId.isEmpty ? null : moduleId);
    q.ref.limit = limit;
    q.ref.offset = offset;

    final count = calloc<Int32>(1);
    omniClientQueryTags(_h, q, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeLibQuery(q);
      return [];
    }

    final buf = calloc<OmniPcmTagInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientQueryTags(_h, q, buf, cnt2));
    final result = <Tag>[];
    for (int i = 0; i < total; i++) {
      result.add(_cTagToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeLibQuery(q);
    return result;
  }

  Future<List<Playlist>> queryPlaylists({
    String moduleId = '',
    int limit = 0,
    int offset = 0,
  }) async {
    final q = calloc<OmniPcmLibraryQuery>();
    q.ref.moduleId = _str(moduleId.isEmpty ? null : moduleId);
    q.ref.limit = limit;
    q.ref.offset = offset;

    final count = calloc<Int32>(1);
    omniClientQueryPlaylists(_h, q, nullptr, count);
    final total = count.value;
    calloc.free(count);
    if (total == 0) {
      _freeLibQuery(q);
      return [];
    }

    final buf = calloc<OmniPcmPlaylistInfo>(total);
    final cnt2 = calloc<Int32>(1)..value = total;
    _check(omniClientQueryPlaylists(_h, q, buf, cnt2));
    final result = <Playlist>[];
    for (int i = 0; i < total; i++) {
      result.add(_cPlaylistToProto(buf.elementAt(i)));
    }
    calloc.free(buf);
    calloc.free(cnt2);
    _freeLibQuery(q);
    return result;
  }

  Future<Track?> getTrack(String uuid) async {
    final u = _str(uuid);
    final p = calloc<OmniPcmTrackInfo>();
    final r = omniClientGetTrack(_h, u, p);
    _freeStr(u);
    if (!_ok(r)) {
      calloc.free(p);
      return null;
    }
    final result = _cTrackToProto(p);
    calloc.free(p);
    return result;
  }

  Future<void> setTrackExcluded(String uuid, bool excluded) async {
    final u = _str(uuid);
    _check(omniClientSetTrackExcluded(_h, u, excluded ? 1 : 0));
    _freeStr(u);
  }

  // Cleanup

  void _freeQuery(Pointer<OmniPcmTrackQuery> q) {
    _freeStr(q.ref.albumId);
    _freeStr(q.ref.tagId);
    _freeStr(q.ref.playlistId);
    _freeStr(q.ref.moduleId);
    calloc.free(q);
  }

  void _freeLibQuery(Pointer<OmniPcmLibraryQuery> q) {
    _freeStr(q.ref.moduleId);
    calloc.free(q);
  }
}

class OmniSdkException implements Exception {
  final String message;
  OmniSdkException(this.message);
  @override
  String toString() => 'OmniSdkException: $message';
}

// ── Background Isolate Proxy for OmniSdkClient ─────────────────

class OmniSdkClient {
  final String _clientId;
  late final SendPort _workerSendPort;
  late final ReceivePort _responsePort;
  final Map<int, Completer<dynamic>> _pendingRequests = {};
  int _nextRequestId = 0;
  bool _disposed = false;
  late final Future<void> _initFuture;
  Isolate? _workerIsolate;
  Object? _initError;
  StackTrace? _initStackTrace;

  OmniSdkClient({this._clientId = 'flutter'}) {
    _initFuture = _init();
    _initFuture.ignore();
  }

  Future<void> _init() async {
    try {
      _responsePort = ReceivePort();
      _responsePort.listen(_handleResponse);

      final handshakePort = ReceivePort();
      _workerIsolate = await Isolate.spawn(_sdkWorkerEntryPoint, [
        handshakePort.sendPort,
        _responsePort.sendPort,
        _clientId,
      ]);

      final handshakeResult = await handshakePort.first;
      handshakePort.close();
      if (handshakeResult is SendPort) {
        _workerSendPort = handshakeResult;
      } else {
        throw OmniSdkException(
          'Failed to initialize SDK worker: $handshakeResult',
        );
      }
    } catch (e, st) {
      _initError = e;
      _initStackTrace = st;
      rethrow;
    }
  }

  void _handleResponse(dynamic message) {
    if (message is List && message.length == 3) {
      final int id = message[0];
      final bool success = message[1];
      final dynamic result = message[2];

      final completer = _pendingRequests.remove(id);
      if (completer != null) {
        if (success) {
          completer.complete(result);
        } else {
          completer.completeError(OmniSdkException(result.toString()));
        }
      }
    }
  }

  Future<T> _call<T>(String method, List<dynamic> args) async {
    if (_disposed && method != 'dispose') {
      throw OmniSdkException('Client is disposed');
    }
    if (_initError != null) {
      Error.throwWithStackTrace(
        OmniSdkException('SDK Client initialization failed: $_initError'),
        _initStackTrace ?? StackTrace.current,
      );
    }
    await _initFuture;
    final id = _nextRequestId++;
    final completer = Completer<dynamic>();
    _pendingRequests[id] = completer;
    _workerSendPort.send([id, method, args]);
    final result = await completer.future;
    return result as T;
  }

  // Lifecycle

  void dispose() {
    if (!_disposed) {
      _disposed = true;
      final isolate = _workerIsolate;
      _workerIsolate = null;

      _call<void>('dispose', [])
          .timeout(const Duration(seconds: 1))
          .then((_) => _responsePort.close())
          .catchError((_) {
            isolate?.kill(priority: Isolate.beforeNextEvent);
            _responsePort.close();
          });

      final completers = List<Completer<dynamic>>.from(_pendingRequests.values);
      _pendingRequests.clear();
      for (final completer in completers) {
        if (!completer.isCompleted) {
          completer.completeError(OmniSdkException('Client is disposed'));
        }
      }
    }
  }

  Future<InstanceProfile> ensureConnected({
    String modId = '',
    String gameName = 'Flutter GUI',
    String displayName = 'OmniMix GUI',
  }) async {
    final bytes = await _call<Uint8List>('ensureConnected', [
      modId,
      gameName,
      displayName,
    ]);
    return InstanceProfile.fromBuffer(bytes);
  }

  Future<InstanceProfile> connect({
    String modId = '',
    String gameName = 'Flutter GUI',
    String displayName = 'OmniMix GUI',
  }) async {
    final bytes = await _call<Uint8List>('connect', [
      modId,
      gameName,
      displayName,
    ]);
    return InstanceProfile.fromBuffer(bytes);
  }

  Future<bool> heartbeat(String instanceId) =>
      _call<bool>('heartbeat', [instanceId]);

  // InstanceService

  Future<InstanceProfile> getProfile(String instanceId) async {
    final bytes = await _call<Uint8List>('getProfile', [instanceId]);
    return InstanceProfile.fromBuffer(bytes);
  }

  Future<void> updateProfile(String instanceId, InstanceProfile profile) =>
      _call<void>('updateProfile', [instanceId, profile.writeToBuffer()]);

  Future<List<InstanceSummary>> listInstances() async {
    final list = await _call<List<dynamic>>('listInstances', []);
    return list
        .map((bytes) => InstanceSummary.fromBuffer(bytes as Uint8List))
        .toList();
  }

  Future<List<InstanceProfile>> listArchives() async {
    final list = await _call<List<dynamic>>('listArchives', []);
    return list
        .map((bytes) => InstanceProfile.fromBuffer(bytes as Uint8List))
        .toList();
  }

  Future<InstanceProfile> getArchive(String archiveId) async {
    final bytes = await _call<Uint8List>('getArchive', [archiveId]);
    return InstanceProfile.fromBuffer(bytes);
  }

  Future<void> deleteArchive(String archiveId) =>
      _call<void>('deleteArchive', [archiveId]);

  Future<void> deleteInstance(String instanceId) =>
      _call<void>('deleteInstance', [instanceId]);

  Future<void> archiveInstance(String instanceId, String label) =>
      _call<void>('archiveInstance', [instanceId, label]);

  Future<InstanceProfile> inheritFromArchive(
    String newInstanceId,
    String archiveId,
  ) async {
    final bytes = await _call<Uint8List>('inheritFromArchive', [
      newInstanceId,
      archiveId,
    ]);
    return InstanceProfile.fromBuffer(bytes);
  }

  // PlaybackService

  Future<PlaybackStatus> getStatus(String instanceId) async {
    final bytes = await _call<Uint8List>('getStatus', [instanceId]);
    return PlaybackStatus.fromBuffer(bytes);
  }

  Future<void> play(String instanceId, {String uuid = ''}) =>
      _call<void>('play', [instanceId, uuid]);

  Future<void> pause(String instanceId) => _call<void>('pause', [instanceId]);

  Future<void> resume(String instanceId) => _call<void>('resume', [instanceId]);

  Future<void> toggle(String instanceId) => _call<void>('toggle', [instanceId]);

  Future<void> next(String instanceId) => _call<void>('next', [instanceId]);

  Future<void> prev(String instanceId) => _call<void>('prev', [instanceId]);

  Future<void> stop(String instanceId) => _call<void>('stop', [instanceId]);

  Future<void> seek(String instanceId, double position) =>
      _call<void>('seek', [instanceId, position]);

  Future<void> setVolume(String instanceId, double volume) =>
      _call<void>('setVolume', [instanceId, volume]);

  Future<double> getVolume(String instanceId) =>
      _call<double>('getVolume', [instanceId]);

  Future<void> setTargetLatency(String instanceId, double latency) =>
      _call<void>('setTargetLatency', [instanceId, latency]);

  Future<double> getTargetLatency(String instanceId) =>
      _call<double>('getTargetLatency', [instanceId]);

  Future<void> setShuffle(String instanceId, bool enabled) =>
      _call<void>('setShuffle', [instanceId, enabled]);

  Future<void> setRepeatMode(String instanceId, int mode) =>
      _call<void>('setRepeatMode', [instanceId, mode]);

  // Queue

  Future<List<QueueTrack>> getQueue(String instanceId) async {
    final list = await _call<List<dynamic>>('getQueue', [instanceId]);
    return list
        .map((bytes) => QueueTrack.fromBuffer(bytes as Uint8List))
        .toList();
  }

  Future<void> addToQueue(String instanceId, String uuid) =>
      _call<void>('addToQueue', [instanceId, uuid]);

  Future<void> insertIntoQueue(
    String instanceId,
    List<String> uuids,
    int index,
  ) => _call<void>('insertIntoQueue', [instanceId, uuids, index]);

  Future<void> setQueue(String instanceId, List<String> uuids) =>
      _call<void>('setQueue', [instanceId, uuids]);

  Future<void> removeFromQueue(String instanceId, int index) =>
      _call<void>('removeFromQueue', [instanceId, index]);

  Future<void> moveInQueue(String instanceId, int from, int to) =>
      _call<void>('moveInQueue', [instanceId, from, to]);

  Future<void> clearQueue(String instanceId) =>
      _call<void>('clearQueue', [instanceId]);

  // History

  Future<List<QueueTrack>> getHistory(String instanceId) async {
    final list = await _call<List<dynamic>>('getHistory', [instanceId]);
    return list
        .map((bytes) => QueueTrack.fromBuffer(bytes as Uint8List))
        .toList();
  }

  Future<void> removeFromHistory(String instanceId, int index) =>
      _call<void>('removeFromHistory', [instanceId, index]);

  Future<void> moveInHistory(String instanceId, int from, int to) =>
      _call<void>('moveInHistory', [instanceId, from, to]);

  Future<void> clearHistory(String instanceId) =>
      _call<void>('clearHistory', [instanceId]);

  Future<List<PlaylistSourceInfo>> getPlaylistSources(String instanceId) async {
    final list = await _call<List<dynamic>>('getPlaylistSources', [instanceId]);
    return list
        .map((bytes) => PlaylistSourceInfo.fromBuffer(bytes as Uint8List))
        .toList();
  }

  Future<void> setPlaylistSources(
    String instanceId,
    List<PlaylistSourceSpec> sources,
  ) => _call<void>('setPlaylistSources', [
    instanceId,
    sources.map((s) => s.writeToBuffer()).toList(),
  ]);

  Future<EqualizerState> getEqualizer(String instanceId) async {
    final bytes = await _call<Uint8List>('getEqualizer', [instanceId]);
    return EqualizerState.fromBuffer(bytes);
  }

  Future<void> setEqualizer(String instanceId, EqualizerState eq) =>
      _call<void>('setEqualizer', [instanceId, eq.writeToBuffer()]);

  // Library

  Future<List<Track>> queryTracks({
    String albumId = '',
    String tagId = '',
    String playlistId = '',
    String moduleId = '',
    int isExcluded = -1,
    int limit = 0,
    int offset = 0,
  }) async {
    final list = await _call<List<dynamic>>('queryTracks', [
      albumId,
      tagId,
      playlistId,
      moduleId,
      isExcluded,
      limit,
      offset,
    ]);
    return list.map((bytes) => Track.fromBuffer(bytes as Uint8List)).toList();
  }

  Future<List<Album>> queryAlbums({
    String moduleId = '',
    int limit = 0,
    int offset = 0,
  }) async {
    final list = await _call<List<dynamic>>('queryAlbums', [
      moduleId,
      limit,
      offset,
    ]);
    return list.map((bytes) => Album.fromBuffer(bytes as Uint8List)).toList();
  }

  Future<List<Tag>> queryTags({
    String moduleId = '',
    int limit = 0,
    int offset = 0,
  }) async {
    final list = await _call<List<dynamic>>('queryTags', [
      moduleId,
      limit,
      offset,
    ]);
    return list.map((bytes) => Tag.fromBuffer(bytes as Uint8List)).toList();
  }

  Future<List<Playlist>> queryPlaylists({
    String moduleId = '',
    int limit = 0,
    int offset = 0,
  }) async {
    final list = await _call<List<dynamic>>('queryPlaylists', [
      moduleId,
      limit,
      offset,
    ]);
    return list
        .map((bytes) => Playlist.fromBuffer(bytes as Uint8List))
        .toList();
  }

  Future<Track?> getTrack(String uuid) async {
    final bytes = await _call<Uint8List?>('getTrack', [uuid]);
    if (bytes == null) return null;
    return Track.fromBuffer(bytes);
  }

  Future<void> setTrackExcluded(String uuid, bool excluded) =>
      _call<void>('setTrackExcluded', [uuid, excluded]);
}

// ── Background Isolate Entry Point and Handler ──────────────────

void _sdkWorkerEntryPoint(List<dynamic> initArgs) {
  final SendPort handshakePort = initArgs[0];
  final SendPort mainSendPort = initArgs[1];
  final String clientId = initArgs[2];

  final receivePort = ReceivePort();

  _RawOmniSdkClient client;
  try {
    client = _RawOmniSdkClient(clientId: clientId);
  } catch (e) {
    handshakePort.send(e.toString());
    receivePort.close();
    return;
  }

  handshakePort.send(receivePort.sendPort);

  receivePort.listen((message) async {
    if (message is List && message.length == 3) {
      final int id = message[0];
      final String method = message[1];
      final List<dynamic> methodArgs = message[2];

      if (method == 'dispose') {
        try {
          client.dispose();
          mainSendPort.send([id, true, null]);
        } catch (e) {
          mainSendPort.send([id, false, e.toString()]);
        }
        receivePort.close();
        return;
      }

      try {
        final result = await _executeMethod(client, method, methodArgs);
        mainSendPort.send([id, true, result]);
      } catch (e) {
        mainSendPort.send([id, false, e.toString()]);
      }
    }
  });
}

Future<dynamic> _executeMethod(
  _RawOmniSdkClient client,
  String method,
  List<dynamic> args,
) async {
  switch (method) {
    case 'ensureConnected':
      final result = await client.ensureConnected(
        modId: args[0] as String,
        gameName: args[1] as String,
        displayName: args[2] as String,
      );
      return result.writeToBuffer();
    case 'connect':
      final result = await client.connect(
        modId: args[0] as String,
        gameName: args[1] as String,
        displayName: args[2] as String,
      );
      return result.writeToBuffer();
    case 'heartbeat':
      return await client.heartbeat(args[0] as String);
    case 'getProfile':
      final result = await client.getProfile(args[0] as String);
      return result.writeToBuffer();
    case 'updateProfile':
      return await client.updateProfile(
        args[0] as String,
        InstanceProfile.fromBuffer(args[1] as Uint8List),
      );
    case 'listInstances':
      final result = await client.listInstances();
      return result.map((item) => item.writeToBuffer()).toList();
    case 'listArchives':
      final result = await client.listArchives();
      return result.map((item) => item.writeToBuffer()).toList();
    case 'getArchive':
      final result = await client.getArchive(args[0] as String);
      return result.writeToBuffer();
    case 'deleteArchive':
      return await client.deleteArchive(args[0] as String);
    case 'deleteInstance':
      return await client.deleteInstance(args[0] as String);
    case 'archiveInstance':
      return await client.archiveInstance(args[0] as String, args[1] as String);
    case 'inheritFromArchive':
      final result = await client.inheritFromArchive(
        args[0] as String,
        args[1] as String,
      );
      return result.writeToBuffer();
    case 'getStatus':
      final result = await client.getStatus(args[0] as String);
      return result.writeToBuffer();
    case 'play':
      return await client.play(args[0] as String, uuid: args[1] as String);
    case 'pause':
      return await client.pause(args[0] as String);
    case 'resume':
      return await client.resume(args[0] as String);
    case 'toggle':
      return await client.toggle(args[0] as String);
    case 'next':
      return await client.next(args[0] as String);
    case 'prev':
      return await client.prev(args[0] as String);
    case 'stop':
      return await client.stop(args[0] as String);
    case 'seek':
      return await client.seek(args[0] as String, args[1] as double);
    case 'setVolume':
      return await client.setVolume(args[0] as String, args[1] as double);
    case 'getVolume':
      return await client.getVolume(args[0] as String);
    case 'setTargetLatency':
      return await client.setTargetLatency(
        args[0] as String,
        args[1] as double,
      );
    case 'getTargetLatency':
      return await client.getTargetLatency(args[0] as String);
    case 'setShuffle':
      return await client.setShuffle(args[0] as String, args[1] as bool);
    case 'setRepeatMode':
      return await client.setRepeatMode(args[0] as String, args[1] as int);
    case 'getQueue':
      final result = await client.getQueue(args[0] as String);
      return result.map((item) => item.writeToBuffer()).toList();
    case 'addToQueue':
      return await client.addToQueue(args[0] as String, args[1] as String);
    case 'insertIntoQueue':
      return await client.insertIntoQueue(
        args[0] as String,
        List<String>.from(args[1] as List),
        args[2] as int,
      );
    case 'setQueue':
      return await client.setQueue(
        args[0] as String,
        List<String>.from(args[1] as List),
      );
    case 'removeFromQueue':
      return await client.removeFromQueue(args[0] as String, args[1] as int);
    case 'moveInQueue':
      return await client.moveInQueue(
        args[0] as String,
        args[1] as int,
        args[2] as int,
      );
    case 'clearQueue':
      return await client.clearQueue(args[0] as String);
    case 'getHistory':
      final result = await client.getHistory(args[0] as String);
      return result.map((item) => item.writeToBuffer()).toList();
    case 'removeFromHistory':
      return await client.removeFromHistory(args[0] as String, args[1] as int);
    case 'moveInHistory':
      return await client.moveInHistory(
        args[0] as String,
        args[1] as int,
        args[2] as int,
      );
    case 'clearHistory':
      return await client.clearHistory(args[0] as String);
    case 'getPlaylistSources':
      final result = await client.getPlaylistSources(args[0] as String);
      return result.map((item) => item.writeToBuffer()).toList();
    case 'setPlaylistSources':
      final sourcesList = (args[1] as List)
          .map((bytes) => PlaylistSourceSpec.fromBuffer(bytes as Uint8List))
          .toList();
      return await client.setPlaylistSources(args[0] as String, sourcesList);
    case 'getEqualizer':
      final result = await client.getEqualizer(args[0] as String);
      return result.writeToBuffer();
    case 'setEqualizer':
      return await client.setEqualizer(
        args[0] as String,
        EqualizerState.fromBuffer(args[1] as Uint8List),
      );
    case 'queryTracks':
      final result = await client.queryTracks(
        albumId: args[0] as String,
        tagId: args[1] as String,
        playlistId: args[2] as String,
        moduleId: args[3] as String,
        isExcluded: args[4] as int,
        limit: args[5] as int,
        offset: args[6] as int,
      );
      return result.map((item) => item.writeToBuffer()).toList();
    case 'queryAlbums':
      final result = await client.queryAlbums(
        moduleId: args[0] as String,
        limit: args[1] as int,
        offset: args[2] as int,
      );
      return result.map((item) => item.writeToBuffer()).toList();
    case 'queryTags':
      final result = await client.queryTags(
        moduleId: args[0] as String,
        limit: args[1] as int,
        offset: args[2] as int,
      );
      return result.map((item) => item.writeToBuffer()).toList();
    case 'queryPlaylists':
      final result = await client.queryPlaylists(
        moduleId: args[0] as String,
        limit: args[1] as int,
        offset: args[2] as int,
      );
      return result.map((item) => item.writeToBuffer()).toList();
    case 'getTrack':
      final result = await client.getTrack(args[0] as String);
      return result?.writeToBuffer();
    case 'setTrackExcluded':
      return await client.setTrackExcluded(args[0] as String, args[1] as bool);
    default:
      throw UnimplementedError('Method $method not implemented');
  }
}
