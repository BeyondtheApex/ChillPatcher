import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/node_data.dart';
import 'unix_socket_client.dart';
import 'logger.dart';

/// HTTP REST client that talks to the C# backend.
/// Supports TCP (primary) and Unix Domain Socket (fallback).
class ApiClient {
  final String _baseUrl;
  final http.Client _http;

  /// The backend base URL, used by image widgets to resolve relative paths.
  String get baseUrl => _baseUrl;

  /// TCP mode.
  ApiClient({required int port})
    : _baseUrl = 'http://127.0.0.1:$port',
      _http = http.Client() {
    GuiLogger().conn('ApiClient: TCP mode, baseUrl=$_baseUrl');
  }

  /// Unix socket mode.
  ApiClient.withSocket({required String socketPath})
    : _baseUrl = 'http://unix',
      _http = createUnixHttpClient(socketPath) {
    GuiLogger().conn('ApiClient: socket mode, path=$socketPath');
  }

  void dispose() => _http.close();

  Future<bool> checkHealth() async {
    try {
      GuiLogger().conn('ApiClient.checkHealth: GET $_baseUrl/api/health');
      final resp = await _http
          .get(Uri.parse('$_baseUrl/api/health'))
          .timeout(const Duration(seconds: 3));
      GuiLogger().conn('ApiClient.checkHealth: statusCode=${resp.statusCode}');
      return resp.statusCode == 200;
    } catch (e, st) {
      GuiLogger().error('ApiClient.checkHealth FAILED', e, st);
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

  Future<void> saveConfig() async {
    await _http.post(Uri.parse('$_baseUrl/api/config/save'));
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

  Future<void> pause(String instanceId) => _post('/api/instances/$instanceId/pause');
  Future<void> resume(String instanceId) => _post('/api/instances/$instanceId/resume');
  Future<void> toggle(String instanceId) => _post('/api/instances/$instanceId/toggle');
  Future<void> next(String instanceId) => _post('/api/instances/$instanceId/next');
  Future<void> previous(String instanceId) => _post('/api/instances/$instanceId/prev');

  Future<void> seek(String instanceId, double position) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/instances/$instanceId/seek'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'position': position}),
    );
  }

  Future<List<QueueItemInfo>> getInstanceQueue(String instanceId) async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances/$instanceId/queue'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list.map((e) => QueueItemInfo.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<QueueItemInfo>> getInstanceHistory(String instanceId) async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances/$instanceId/history'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list.map((e) => QueueItemInfo.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<QueueItemInfo>> getInstancePlaylist(String instanceId) async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances/$instanceId/playlist'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list.map((e) => QueueItemInfo.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<PlaylistSourceInfo>> getPlaylistSources(String instanceId) async {
    final resp = await _http.get(Uri.parse('$_baseUrl/api/instances/$instanceId/playlist/sources'));
    if (resp.statusCode != 200) throw Exception('HTTP ${resp.statusCode}');
    final list = json.decode(resp.body) as List<dynamic>;
    return list.map((e) => PlaylistSourceInfo.fromJson(e as Map<String, dynamic>)).toList();
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

  Future<void> clearQueue(String instanceId) => _post('/api/instances/$instanceId/queue/clear');

  Future<void> removeHistoryAt(String instanceId, int index) =>
      _delete('/api/instances/$instanceId/history/$index');

  Future<void> clearHistory(String instanceId) => _post('/api/instances/$instanceId/history/clear');

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
