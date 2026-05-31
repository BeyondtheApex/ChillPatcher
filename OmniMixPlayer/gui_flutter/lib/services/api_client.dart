import 'dart:convert' hide json;
import '../utils/json_utils.dart';
import 'dart:math' as math;
import 'package:http/http.dart' as http;
import '../models/node_data.dart';
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
  void close() {
    _inner.close();
  }
}

/// HTTP REST client that talks to the C# backend.
/// Supports TCP (primary) and Unix Domain Socket (fallback).
class ApiClient {
  final String _baseUrl;
  final http.Client _http;
  final String clientId;

  /// The backend base URL, used by image widgets to resolve relative paths.
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
       _http = ClientIdClient(
         isSocket ? createUnixHttpClient(socketPath!) : http.Client(),
         cid,
       );

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

  void dispose() => _http.close();

  Future<bool> checkHealth() async {
    try {
      final resp = await _http
          .get(Uri.parse('$_baseUrl/api/health'))
          .timeout(const Duration(seconds: 3));
      return resp.statusCode == 200;
    } catch (e, st) {
      return false;
    }
  }

  Future<List<ModuleInfoResponse>> getModules() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/modules'));
    if (resp.statusCode != 200) {
      throw Exception('HTTP ${resp.statusCode}');
    }
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => ModuleInfoResponse.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<RawNodeData> getModuleUi(String moduleId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/modules/$moduleId/ui'),
    );
    if (resp.statusCode != 200) {
      throw Exception('HTTP ${resp.statusCode}');
    }
    return RawNodeData.fromJson(json.decode(resp.body) as Map<String, dynamic>);
  }

  Future<RawNodeData> getModuleLinkUi(String moduleId, String linkId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/modules/$moduleId/link/$linkId'),
    );
    if (resp.statusCode != 200) {
      throw Exception('HTTP ${resp.statusCode}');
    }
    return RawNodeData.fromJson(json.decode(resp.body) as Map<String, dynamic>);
  }

  Future<void> setModuleEnabled(String moduleId, bool enabled) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/modules/$moduleId'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'enabled': enabled}),
    );
  }

  Future<RawNodeData> getModuleSettingsUi(String moduleId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/modules/$moduleId/settings'),
    );
    if (resp.statusCode != 200) {
      throw Exception('HTTP ${resp.statusCode}');
    }
    return RawNodeData.fromJson(json.decode(resp.body) as Map<String, dynamic>);
  }

  Future<void> putConfig(AppConfig config) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/config'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(config.toJson()),
    );
  }

  /// Send arbitrary key-value pairs to update backend config.
  Future<void> putConfigRaw(Map<String, dynamic> updates) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/config'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(updates),
    );
  }

  Future<Map<String, dynamic>> getConfig() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/config'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> saveConfig() async {
    await _http.post(Uri.parse('$_baseUrl/api/config/save'));
  }

  // ── Active instance endpoints (backend routes to active instance) ──
  Future<Map<String, dynamic>> getActiveProfile() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/active/profile'));
    if (resp.statusCode == 404) return {};
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> updateActiveProfile(Map<String, dynamic> data) async {
    final resp = await _http.put(
      Uri.parse('$_baseUrl/api/active/profile'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(data),
    );
    if (resp.statusCode >= 400) throw Exception('HTTP ${resp.statusCode}');
  }

  Future<List<dynamic>> getArchives() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances/archives'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as List<dynamic>;
  }

  Future<void> deleteArchive(String id) async {
    await _http.delete(Uri.parse('$_baseUrl/api/instances/archives/$id'));
  }

  Future<void> renameArchive(String id, String label) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/instances/archives/$id/rename'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'label': label}),
    );
  }

  Future<void> archiveInstance(String id, {String label = ''}) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$id/archive'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'label': label}),
    );
  }

  Future<void> deleteInstance(String id) async {
    await _http.delete(Uri.parse('$_baseUrl/api/instances/$id'));
  }

  /// Set instance metadata (modId, gameName, mode) on the backend.
  Future<void> setInstanceMeta(
    String instanceId,
    String modId,
    String gameName,
    String mode,
  ) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/instances/$instanceId/meta'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'modId': modId, 'gameName': gameName, 'mode': mode}),
    );
  }

  /// Inherit profile from archive to a new instance.
  /// Returns: {"inherited": true, "consumed": true/false}
  Future<Map<String, dynamic>> inheritFromArchive(
    String instanceId,
    String archiveId,
  ) async {
    final resp = await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/inherit/$archiveId'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getInstanceProfile(String instanceId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/instances/$instanceId/profile'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> updateInstanceProfile(
    String instanceId,
    Map<String, dynamic> data,
  ) async {
    final resp = await _http.put(
      Uri.parse('$_baseUrl/api/instances/$instanceId/profile'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(data),
    );
    if (resp.statusCode == 404) throw Exception('Instance not found');
    if (resp.statusCode >= 400) throw Exception('HTTP ${resp.statusCode}');
  }

  Future<Map<String, dynamic>> getInstanceEqualizer(String instanceId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/instances/$instanceId/equalizer'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> updateInstanceEqualizer(
    String instanceId,
    Map<String, dynamic> data,
  ) async {
    final resp = await _http.put(
      Uri.parse('$_baseUrl/api/instances/$instanceId/equalizer'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(data),
    );
    if (resp.statusCode >= 400) throw Exception('HTTP ${resp.statusCode}');
  }

  Future<Map<String, dynamic>> getInstanceEqualizerPresets(
    String instanceId,
  ) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/instances/$instanceId/equalizer/presets'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> stopBackend() async {
    await _http.post(Uri.parse('$_baseUrl/api/backend/stop'));
  }

  Future<PlaylistData> getPlaylist() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/playlist'));
    if (resp.statusCode != 200) {
      throw Exception('HTTP ${resp.statusCode}');
    }
    return PlaylistData.fromJson(
      json.decode(resp.body) as Map<String, dynamic>,
    );
  }

  Future<List<TagInfo>> getTags() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/tags'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => TagInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<AlbumInfo>> getAlbums({String? tagId}) async {
    final query = tagId != null ? '?tagId=$tagId' : '';
    final resp = await _http.get(Uri.parse('$_baseUrl/api/albums$query'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => AlbumInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<SongInfo>> getSongs({String? albumId, String? tagId}) async {
    final params = <String>[];
    if (albumId != null) params.add('albumId=$albumId');
    if (tagId != null) params.add('tagId=$tagId');
    final query = params.isNotEmpty ? '?${params.join('&')}' : '';
    final resp = await _http.get(Uri.parse('$_baseUrl/api/songs$query'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => SongInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<PlaybackInstanceInfo>> getInstances() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => PlaybackInstanceInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<Map<String, dynamic>> getInstanceStats() async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances/stats'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    return json.decode(resp.body) as Map<String, dynamic>;
  }

  Future<void> connectController({String clientId = 'flutter'}) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/connect'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({
        'clientId': clientId,
        'role': 'controller',
        'mode': 'server',
      }),
    );
  }

  Future<void> play(String instanceId, {String? uuid}) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/play'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'uuid': uuid ?? ''}),
    );
  }

  Future<void> pause(String instanceId) =>
      _post('/api/instances/$instanceId/pause');
  Future<void> resume(String instanceId) =>
      _post('/api/instances/$instanceId/resume');
  Future<void> toggle(String instanceId) =>
      _post('/api/instances/$instanceId/toggle');
  Future<void> next(String instanceId) =>
      _post('/api/instances/$instanceId/next');
  Future<void> previous(String instanceId) =>
      _post('/api/instances/$instanceId/prev');

  Future<void> seek(String instanceId, double position) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/seek'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'position': position}),
    );
  }

  Future<void> setVolume(String instanceId, double volume) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/instances/$instanceId/volume'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'volume': volume}),
    );
  }

  Future<void> setLatency(String instanceId, double latency) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/instances/$instanceId/latency'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'latency': latency}),
    );
  }

  Future<List<QueueItemInfo>> getInstanceQueue(String instanceId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/instances/$instanceId/queue'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => QueueItemInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<QueueItemInfo>> getInstanceHistory(String instanceId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/instances/$instanceId/history'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => QueueItemInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<QueueItemInfo>> getInstancePlaylist(String instanceId) async {
    final resp = await _http.get(
      Uri.parse('$_baseUrl/api/instances/$instanceId/playlist'),
    );
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list
        .map((e) => QueueItemInfo.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> addToQueue(String instanceId, String uuid) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/queue'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'uuid': uuid}),
    );
  }

  Future<void> removeQueueAt(String instanceId, int index) =>
      _delete('/api/instances/$instanceId/queue/$index');
  Future<void> moveQueue(String instanceId, int from, int to) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/queue/move'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'from': from, 'to': to}),
    );
  }

  Future<void> clearQueue(String instanceId) =>
      _post('/api/instances/$instanceId/queue/clear');

  Future<void> removeHistoryAt(String instanceId, int index) =>
      _delete('/api/instances/$instanceId/history/$index');
  Future<void> moveHistory(String instanceId, int from, int to) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/history/move'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'from': from, 'to': to}),
    );
  }

  Future<void> clearHistory(String instanceId) =>
      _post('/api/instances/$instanceId/history/clear');

  Future<void> insertQueueAt(
    String instanceId, {
    required List<String> uuids,
    required int index,
  }) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/queue/insert'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'uuids': uuids, 'index': index}),
    );
  }

  Future<void> setShuffle(String instanceId, bool enabled) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/shuffle'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'enabled': enabled}),
    );
  }

  Future<void> setRepeatMode(String instanceId, String mode) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/repeat'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'mode': mode}),
    );
  }

  Future<void> setExcluded(String uuid, bool excluded) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/exclude'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'uuid': uuid, 'isFavorite': excluded}),
    );
  }

  Future<void> addPlaylistSource(
    String instanceId, {
    required String id,
    required String name,
    required List<String> uuids,
  }) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/playlist/sources'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({
        'source': {'id': id, 'name': name, 'uuids': uuids},
      }),
    );
  }

  Future<void> insertPlaylistSource(
    String instanceId, {
    required String id,
    required String name,
    required List<String> uuids,
    int index = -1,
  }) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/playlist/sources'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({
        'index': index,
        'source': {'id': id, 'name': name, 'uuids': uuids},
      }),
    );
  }

  Future<void> replacePlaylistSources(
    String instanceId, {
    required List<Map<String, dynamic>> sources,
  }) async {
    await _http.put(
      Uri.parse('$_baseUrl/api/instances/$instanceId/playlist/sources'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'sources': sources}),
    );
  }

  Future<void> removePlaylistSource(String instanceId, String sourceId) =>
      _delete('/api/instances/$instanceId/playlist/sources/$sourceId');

  Future<void> _post(String path) async {
    final resp = await _http.post(Uri.parse('$_baseUrl$path'));
    if (resp.statusCode < 200 || resp.statusCode >= 300) {
      throw Exception('HTTP ${resp.statusCode}');
    }
  }

  Future<void> _delete(String path) async {
    final resp = await _http.delete(Uri.parse('$_baseUrl$path'));
    if (resp.statusCode < 200 || resp.statusCode >= 300) {
      throw Exception('HTTP ${resp.statusCode}');
    }
  }
}
