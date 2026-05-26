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

  Future<void> play({String? uuid}) async {
    await _http.post(
      Uri.parse('$_baseUrl/api/play'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'uuid': uuid ?? ''}),
    );
  }
}
