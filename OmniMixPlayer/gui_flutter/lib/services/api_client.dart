/// API client that combines gRPC (library, playback, instance) with
/// REST (config, health, modules). All data operations now go through gRPC;
/// only module UI and config remain REST.
///
/// Public API is kept compatible with existing providers, migrating from old
/// JSON models to generated protobuf types where possible.
library;

import '../utils/json_utils.dart';
import 'dart:math' as math;
import 'package:http/http.dart' as http;
import '../models/node_data.dart';
import '../generated/omni_mix_player/models/track.pb.dart';
import '../generated/omni_mix_player/models/album.pb.dart';
import '../generated/omni_mix_player/models/tag.pb.dart';
import '../generated/omni_mix_player/models/playlist.pb.dart';
import '../generated/omni_mix_player/models/query.pb.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../generated/omni_mix_player/services/library.pb.dart';
import '../generated/omni_mix_player/services/playback.pb.dart';
import '../generated/omni_mix_player/services/instance.pb.dart';
import '../generated/omni_mix_player/models/common.pbenum.dart';
import 'grpc_services.dart';
import 'unix_socket_client.dart'
    if (dart.library.js_interop) '../stubs/unix_socket_client_web.dart';

class ClientIdClient extends http.BaseClient {
  final http.Client _inner;
  final String clientId;

  ClientIdClient(this._inner, this.clientId);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) {
    request.headers['X-Client-Id'] = clientId;
    return _inner.send(request);
  }

  @override
  void close() => _inner.close();
}

/// Unified API client: gRPC for data, REST for config/modules.
class ApiClient {
  final String _baseUrl;
  final http.Client _http;
  late final GrpcServices _grpc;
  final String clientId;
  final bool _isWeb;
  static const _libraryTimeout = Duration(seconds: 6);

  String get baseUrl => _baseUrl;

  static String _generateClientId() {
    final random = math.Random();
    const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
    return List.generate(16, (i) => chars[random.nextInt(chars.length)]).join();
  }

  ApiClient._internal({
    int? port,
    String? socketPath,
    required bool isSocket,
    required bool isWeb,
    required String cid,
  }) : clientId = cid,
       _baseUrl = isWeb
           ? ''
           : (isSocket ? 'http://unix' : 'http://127.0.0.1:$port'),
       _isWeb = isWeb,
       _http = ClientIdClient(
         isSocket ? createUnixHttpClient(socketPath!) : http.Client(),
         cid,
       ) {
    _grpc = GrpcServices(host: '127.0.0.1', port: port ?? 17890);
  }

  factory ApiClient({required int port}) {
    final cid = _generateClientId();
    return ApiClient._internal(
      port: port,
      isSocket: false,
      isWeb: false,
      cid: cid,
    );
  }

  factory ApiClient.withSocket({required String socketPath}) {
    final cid = _generateClientId();
    return ApiClient._internal(
      socketPath: socketPath,
      isSocket: true,
      isWeb: false,
      cid: cid,
    );
  }

  factory ApiClient.forWeb() {
    final cid = _generateClientId();
    return ApiClient._internal(isSocket: false, isWeb: true, cid: cid);
  }

  void dispose() {
    _http.close();
    _grpc.dispose();
  }

  void reconnectGrpc() => _grpc.reconnect();

  // ── Health & Config (REST) ──

  Future<bool> checkHealth() async {
    try {
      final resp = await _http
          .get(Uri.parse('$_baseUrl/api/health'))
          .timeout(const Duration(seconds: 3));
      return resp.statusCode == 200;
    } catch (_) {
      return false;
    }
  }

  Future<Map<String, dynamic>> getConfig() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/config'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> putConfig(AppConfig config) => putConfigRaw(config.toJson());

  Future<void> putConfigRaw(Map<String, dynamic> updates) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/config'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(updates),
    );
  }

  Future<void> saveConfig() async {
    await _http.post(Uri.parse('$_baseUrl/api/config/save'));
  }

  Future<void> stopBackend() async {
    await _http.post(Uri.parse('$_baseUrl/api/backend/stop'));
  }

  // ── Modules (REST) ──

  Future<List<ModuleInfoResponse>> getModules() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/modules'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => ModuleInfoResponse.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<RawNodeData> getModuleUi(String moduleId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/modules/$moduleId/ui'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return RawNodeData.fromJson(json.decode(resp.body) as Map<String, dynamic>);
  }

  Future<RawNodeData> getModuleLinkUi(String moduleId, String linkId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/modules/$moduleId/link/$linkId'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return RawNodeData.fromJson(json.decode(resp.body) as Map<String, dynamic>);
  }

  Future<RawNodeData> getModuleSettingsUi(String moduleId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/modules/$moduleId/settings'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return RawNodeData.fromJson(json.decode(resp.body) as Map<String, dynamic>);
  }

  Future<void> setModuleEnabled(String moduleId, bool enabled) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/modules/$moduleId'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'enabled': enabled}),
    );
  }

  // ── Library: Tags, Albums, Tracks (gRPC) ──

  Future<List<Tag>> getTags({String? moduleId}) async {
    final resp = await _grpc.library
        .queryTags(TagQuery(moduleId: moduleId ?? ''))
        .timeout(_libraryTimeout);
    return resp.tags;
  }

  Future<List<Album>> getAlbums({String? tagId}) async {
    final resp = await _grpc.library
        .queryAlbums(AlbumQuery(tagId: tagId ?? ''))
        .timeout(_libraryTimeout);
    return resp.albums;
  }

  Future<List<Playlist>> getPlaylists({String? moduleId}) async {
    final resp = await _grpc.library
        .queryPlaylists(PlaylistQuery(moduleId: moduleId ?? ''))
        .timeout(_libraryTimeout);
    return resp.playlists;
  }

  Future<List<Track>> getSongs({
    String? albumId,
    String? tagId,
    String? playlistId,
  }) async {
    final resp = await _grpc.library
        .queryTracks(
          TrackQuery(
            albumId: albumId ?? '',
            playlistId: playlistId ?? '',
            tagIds: tagId != null ? [tagId] : [],
            isExcluded: false,
          ),
        )
        .timeout(_libraryTimeout);
    return resp.tracks;
  }

  Future<Track?> getSong(String uuid) async {
    try {
      return await _grpc.library.getTrack(GetTrackRequest(uuid: uuid));
    } catch (_) {
      return null;
    }
  }

  Future<void> setSongExcluded(String uuid, bool excluded) async {
    try {
      final track = await _grpc.library.getTrack(GetTrackRequest(uuid: uuid));
      track.isExcluded = excluded;
      await _grpc.library.upsertTrack(UpsertTrackRequest(track: track));
    } catch (_) {}
  }

  // ── Instances (gRPC) ──

  Future<List<InstanceSummary>> getInstances() async {
    final resp = await _grpc.instance.listInstances(ListInstancesRequest());
    return resp.instances;
  }

  Future<Map<String, dynamic>> getInstanceStats() async {
    final instances = await getInstances();
    return {
      'total': instances.length,
      'online': instances.where((i) => i.isOnline).length,
    };
  }

  Future<InstanceProfile?> getInstanceProfile(String instanceId) async {
    try {
      return await _grpc.instance.getProfile(
        GetProfileRequest(instanceId: instanceId),
      );
    } catch (_) {
      return null;
    }
  }

  Future<void> updateInstanceProfile(
    String instanceId,
    Map<String, dynamic> data,
  ) async {
    final profile = await _loadProfileForUpdate(instanceId);
    if (data['displayName'] != null) {
      profile.displayName = data['displayName'] as String? ?? '';
    }
    if (data['volume'] != null) {
      profile.volume = (data['volume'] as num).toDouble();
    }
    if (data['targetLatency'] != null) {
      profile.targetLatency = (data['targetLatency'] as num).toDouble();
    }
    await _grpc.instance.updateProfile(
      UpdateProfileRequest(instanceId: instanceId, profile: profile),
    );
  }

  Future<List<InstanceProfile>> getArchives() async {
    final resp = await _grpc.instance.listArchives(ListArchivesRequest());
    return resp.archives;
  }

  Future<void> deleteArchive(String archiveId) async {
    await _grpc.instance.deleteArchive(
      DeleteArchiveRequest(archiveId: archiveId),
    );
  }

  Future<void> renameArchive(String id, String label) async {
    final profile = InstanceProfile()..id = id;
    try {
      final existing = await _grpc.instance.getArchive(
        GetArchiveRequest(archiveId: id),
      );
      profile.mergeFromMessage(existing);
    } catch (_) {}
    profile.displayName = label;
    await _grpc.instance.updateProfile(
      UpdateProfileRequest(instanceId: id, profile: profile),
    );
  }

  Future<void> deleteInstance(String instanceId) async {
    await _grpc.instance.deleteInstance(
      DeleteInstanceRequest(instanceId: instanceId),
    );
  }

  /// Archive an instance (move to archive list).
  Future<void> archiveInstance(String id, {String label = ''}) async {
    await _grpc.instance.archiveInstance(
      ArchiveInstanceRequest(instanceId: id, label: label),
    );
  }

  Future<void> setInstanceMeta(
    String instanceId,
    String modId,
    String gameName,
    String mode, {
    InstanceCapabilities? capabilities,
  }) async {
    final profile = await _loadProfileForUpdate(instanceId)
      ..kind = InstanceKind.INSTANCE_KIND_GAME_MOD
      ..modId = modId
      ..gameName = gameName
      ..displayName = gameName;
    // Use mod-declared capabilities if provided, otherwise ensure non-null
    profile.capabilities =
        capabilities ??
        (profile.hasCapabilities()
            ? profile.capabilities
            : InstanceCapabilities());
    await _grpc.instance.updateProfile(
      UpdateProfileRequest(instanceId: instanceId, profile: profile),
    );
  }

  Future<InstanceProfile> _loadProfileForUpdate(String instanceId) async {
    try {
      final existing = await _grpc.instance.getProfile(
        GetProfileRequest(instanceId: instanceId),
      );
      return existing.deepCopy()..id = instanceId;
    } catch (_) {
      return InstanceProfile()..id = instanceId;
    }
  }

  Future<Map<String, dynamic>> inheritFromArchive(
    String instanceId,
    String archiveId,
  ) async {
    final resp = await _grpc.instance.inheritFromArchive(
      InheritFromArchiveRequest(
        newInstanceId: instanceId,
        archiveId: archiveId,
      ),
    );
    return {'inherited': resp.inherited, 'consumed': true};
  }

  Future<InstanceProfile> connectController({
    String clientId = 'flutter',
  }) async {
    final resp = await _grpc.instance.connect(
      InstanceConnectRequest(
        clientId: clientId,
        kind: InstanceKind.INSTANCE_KIND_GUI,
        displayName: 'OmniMix GUI',
        gameName: 'Flutter GUI',
        capabilities: _fullGuiCapabilities(),
        noInstance: _isWeb,
      ),
    );
    if (resp.noInstance) return InstanceProfile();
    return resp.profile;
  }

  Future<bool> heartbeat(String instanceId) async {
    try {
      final resp = await _grpc.instance.heartbeat(
        InstanceHeartbeatRequest(instanceId: instanceId),
      );
      return resp.alive;
    } catch (_) {
      return false;
    }
  }

  InstanceCapabilities _fullGuiCapabilities() => InstanceCapabilities()
    ..serverControlledPlayback = true
    ..queueManagement = true
    ..playlistManagement = true
    ..multiplePlaylists = true
    ..tagFiltering = true
    ..unlimitedTags = true
    ..albumFiltering = true
    ..shuffle = true
    ..repeat = true
    ..seek = true
    ..volumeControl = true
    ..equalizer = true
    ..audioPlayback = true;

  // ── Playback Control (gRPC) ──

  Future<void> play(String instanceId, {String? uuid}) async {
    await _grpc.playback.play(
      PlayRequest(instanceId: instanceId, uuid: uuid ?? ''),
    );
  }

  Future<void> pause(String instanceId) async {
    await _grpc.playback.pause(PauseRequest(instanceId: instanceId));
  }

  Future<void> resume(String instanceId) async {
    await _grpc.playback.resume(ResumeRequest(instanceId: instanceId));
  }

  Future<void> toggle(String instanceId) async {
    await _grpc.playback.toggle(ToggleRequest(instanceId: instanceId));
  }

  Future<void> next(String instanceId) async {
    await _grpc.playback.next(NextRequest(instanceId: instanceId));
  }

  Future<void> previous(String instanceId) async {
    await _grpc.playback.prev(PrevRequest(instanceId: instanceId));
  }

  Future<void> seek(String instanceId, double position) async {
    await _grpc.playback.seek(
      SeekRequest(instanceId: instanceId, position: position),
    );
  }

  Future<void> setVolume(String instanceId, double volume) async {
    await _grpc.playback.setVolume(
      SetVolumeRequest(instanceId: instanceId, volume: volume),
    );
  }

  Future<double> getVolume(String instanceId) async {
    final resp = await _grpc.playback.getVolume(
      GetVolumeRequest(instanceId: instanceId),
    );
    return resp.volume;
  }

  Future<void> setLatency(String instanceId, double latency) async {
    await _grpc.playback.setTargetLatency(
      SetTargetLatencyRequest(instanceId: instanceId, latency: latency),
    );
  }

  Future<PlaybackStatus> getPlaybackStatus(String instanceId) async {
    return _grpc.instance.getStatus(
      GetInstanceStatusRequest(instanceId: instanceId),
    );
  }

  // ── Queue (gRPC) ──

  Future<List<QueueTrack>> getInstanceQueue(String instanceId) async {
    final resp = await _grpc.playback.getQueue(
      GetQueueRequest(instanceId: instanceId),
    );
    return resp.queue;
  }

  Future<List<QueueTrack>> getInstanceHistory(String instanceId) async {
    final resp = await _grpc.playback.getHistory(
      GetHistoryRequest(instanceId: instanceId),
    );
    return resp.history;
  }

  Future<void> addToQueue(String instanceId, String uuid) async {
    await _grpc.playback.addToQueue(
      AddToQueueRequest(instanceId: instanceId, uuid: uuid),
    );
  }

  Future<void> removeQueueAt(String instanceId, int index) async {
    await _grpc.playback.removeFromQueue(
      RemoveFromQueueRequest(instanceId: instanceId, index: index),
    );
  }

  Future<void> moveQueue(String instanceId, int from, int to) async {
    await _grpc.playback.moveInQueue(
      MoveInQueueRequest(instanceId: instanceId, fromIndex: from, toIndex: to),
    );
  }

  Future<void> clearQueue(String instanceId) async {
    await _grpc.playback.clearQueue(ClearQueueRequest(instanceId: instanceId));
  }

  Future<void> setQueue(String instanceId, List<String> uuids) async {
    await _grpc.playback.setQueue(
      SetQueueRequest(instanceId: instanceId, uuids: uuids),
    );
  }

  Future<void> insertQueueAt(
    String instanceId, {
    required List<String> uuids,
    required int index,
  }) async {
    await _grpc.playback.insertIntoQueue(
      InsertIntoQueueRequest(
        instanceId: instanceId,
        uuids: uuids,
        index: index,
      ),
    );
  }

  Future<void> removeHistoryAt(String instanceId, int index) async {
    await _grpc.playback.removeFromHistory(
      RemoveFromHistoryRequest(instanceId: instanceId, index: index),
    );
  }

  Future<void> moveHistory(String instanceId, int from, int to) async {
    await _grpc.playback.moveInHistory(
      MoveInHistoryRequest(
        instanceId: instanceId,
        fromIndex: from,
        toIndex: to,
      ),
    );
  }

  Future<void> clearHistory(String instanceId) async {
    await _grpc.playback.clearHistory(
      ClearHistoryRequest(instanceId: instanceId),
    );
  }

  // ── Shuffle / Repeat (gRPC) ──

  Future<void> setShuffle(String instanceId, bool enabled) async {
    await _grpc.playback.setShuffle(
      SetShuffleRequest(instanceId: instanceId, enabled: enabled),
    );
  }

  Future<void> setRepeatMode(String instanceId, String mode) async {
    final repeatMode = _parseRepeatMode(mode);
    await _grpc.playback.setRepeatMode(
      SetRepeatModeRequest(instanceId: instanceId, mode: repeatMode),
    );
  }

  RepeatMode _parseRepeatMode(String mode) {
    switch (mode.toLowerCase()) {
      case 'one':
        return RepeatMode.REPEAT_MODE_ONE;
      case 'all':
        return RepeatMode.REPEAT_MODE_ALL;
      default:
        return RepeatMode.REPEAT_MODE_NONE;
    }
  }

  // ── Playlist Sources (gRPC) ──

  Future<List<PlaylistSourceState>> getPlaylistSources(
    String instanceId,
  ) async {
    final resp = await _grpc.playback.getPlaylistSources(
      GetPlaylistSourcesRequest(instanceId: instanceId),
    );
    return resp.sources
        .map(
          (s) => PlaylistSourceState()
            ..id = s.id
            ..name = s.name
            ..kind = s.kind
            ..refId = s.refId,
        )
        .toList();
  }

  Future<void> setPlaylistSources(
    String instanceId,
    List<PlaylistSourceState> sources,
  ) async {
    final specs = sources.map(
      (s) => PlaylistSourceSpec()
        ..id = s.id
        ..name = s.name
        ..kind = s.kind
        ..refId = s.refId
        ..uuids.addAll(s.uuids),
    );
    await _grpc.playback.setPlaylistSources(
      SetPlaylistSourcesRequest(instanceId: instanceId, sources: specs),
    );
  }

  // ── Equalizer (gRPC) ──

  Future<EqualizerState> getInstanceEqualizer(String instanceId) async {
    return _grpc.playback.getEqualizer(
      GetEqualizerRequest(instanceId: instanceId),
    );
  }

  Future<void> updateInstanceEqualizer(
    String instanceId,
    Map<String, dynamic> data,
  ) async {
    final eq = EqualizerState()
      ..enabled = data['enabled'] as bool? ?? false
      ..globalGainDb = (data['globalGainDb'] as num?)?.toDouble() ?? 0.0
      ..softClipEnabled = data['softClipEnabled'] as bool? ?? false;
    final points = data['points'] as List?;
    if (points != null) {
      for (final p in points) {
        final m = p as Map<String, dynamic>;
        eq.points.add(
          EqualizerPoint()
            ..id = m['id'] as String? ?? ''
            ..frequency = (m['frequency'] as num).toDouble()
            ..gainDb = (m['gainDb'] as num).toDouble()
            ..q = (m['q'] as num?)?.toDouble() ?? 1.0
            ..type = _parseEqFilterType(m['type'] as String? ?? 'peak'),
        );
      }
    }
    await _grpc.playback.setEqualizer(
      SetEqualizerRequest(instanceId: instanceId, state: eq),
    );
  }

  EqualizerFilterType _parseEqFilterType(String type) {
    switch (type.toLowerCase()) {
      case 'lowpass':
        return EqualizerFilterType.EQ_FILTER_TYPE_LOW_PASS;
      case 'highpass':
        return EqualizerFilterType.EQ_FILTER_TYPE_HIGH_PASS;
      case 'lowshelf':
        return EqualizerFilterType.EQ_FILTER_TYPE_LOW_SHELF;
      case 'highshelf':
        return EqualizerFilterType.EQ_FILTER_TYPE_HIGH_SHELF;
      case 'notch':
        return EqualizerFilterType.EQ_FILTER_TYPE_PEAKING;
      default:
        return EqualizerFilterType.EQ_FILTER_TYPE_PEAKING;
    }
  }
}
