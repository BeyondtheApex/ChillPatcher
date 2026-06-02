/// Playback state & control manager.
/// Extracted from AppState during Riverpod migration.
///
/// Manages playback instances, active instance selection, queue/history/playlist
/// state, and all playback control operations.

import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../services/api_client.dart';
import '../../services/media_control_service.dart';
import '../../models/node_data.dart';

class PlaybackStateManager extends ChangeNotifier {
  final ApiClient Function() _getApi;
  final MediaControlService _mediaControlService;
  late final MediaControlCallbacks _mediaControlCallbacks;

  PlaybackStateManager(this._getApi)
    : _mediaControlService = createMediaControlService() {
    _mediaControlCallbacks = MediaControlCallbacks(
      play: _mediaPlay,
      pause: _mediaPause,
      skipToNext: _mediaSkipToNext,
      skipToPrevious: _mediaSkipToPrevious,
      seek: _mediaSeek,
    );
  }

  // ── State ──
  List<PlaybackInstanceInfo> _instances = [];
  String? _activeInstanceId;
  List<QueueItemInfo> _activeQueue = [];
  List<QueueItemInfo> _activeHistory = [];
  List<QueueItemInfo> _activePlaylist = [];
  List<PlaylistSourceInfo> _playlistSources = [];
  bool _loading = false;
  Timer? _pollTimer;
  double _lastVolume = 1.0;
  double _lastTargetLatency = 0.05;
  Set<String> _backendInstanceIds = {};
  String? _mediaControlInstanceId;
  bool _mediaControlsEnabled = true;

  // Getters
  List<PlaybackInstanceInfo> get instances => _instances;
  String? get activeInstanceId => _activeInstanceId;
  List<QueueItemInfo> get activeQueue => _activeQueue;
  List<QueueItemInfo> get activeHistory => _activeHistory;
  List<QueueItemInfo> get activePlaylist => _activePlaylist;
  List<PlaylistSourceInfo> get playlistSources => _playlistSources;
  bool get loading => _loading;
  double get lastVolume => _lastVolume;
  double get lastTargetLatency => _lastTargetLatency;
  int get instanceCount => _instances.length;
  int get attachedAudioClientCount =>
      _instances.where((i) => i.attached).length;
  Set<String> get backendInstanceIds => _backendInstanceIds;
  bool get mediaControlsEnabled => _mediaControlsEnabled;

  ApiClient get api => _getApi();

  PlaybackInstanceInfo? get activeInstance {
    final id = _activeInstanceId;
    if (id == null) return _instances.isNotEmpty ? _instances.first : null;
    for (final instance in _instances) {
      if (instance.id == id) return instance;
    }
    return _instances.isNotEmpty ? _instances.first : null;
  }

  bool get canControlActiveInstance {
    if (activeInstance?.isServerManaged == true) return true;
    if (_activeInstanceId != null) {
      final inst = _instances
          .where((i) => i.id == _activeInstanceId)
          .firstOrNull;
      if (inst != null && inst.isServerManaged) return true;
    }
    if (_activeInstanceId == null) {
      if (_instances.any((i) => i.isServerManaged)) return true;
    }
    return false;
  }

  bool isInstanceOnline(String instanceId) =>
      _backendInstanceIds.contains(instanceId);
  bool instanceExists(String instanceId) =>
      _instances.any((i) => i.id == instanceId);

  void setMediaControlsEnabled(bool enabled) {
    if (_mediaControlsEnabled == enabled) return;
    _mediaControlsEnabled = enabled;
    if (enabled) {
      _syncMediaControls();
    } else {
      _mediaControlInstanceId = null;
      _publishMediaControlSnapshot(null);
    }
    notifyListeners();
  }

  // ── Instance selection ──

  Future<void> selectInstance(String? instanceId) async {
    _activeInstanceId = instanceId;
    if (instanceId != null) {
      api.putConfigRaw({'active_instance': instanceId});
      api.saveConfig();
      await loadActiveProfile();
    }
    notifyListeners();
  }

  Future<void> loadActiveProfile() async {
    if (_activeInstanceId == null) return;
    try {
      final profile = await api.getActiveProfile();
      if (profile.isEmpty) return;
      final queues = profile['Queues'] as List?;
      if (queues == null || queues.isEmpty) return;
      final activeId = profile['ActiveQueueId'] as String? ?? '';
      final active = activeId.isNotEmpty
          ? (queues.cast<Map<String, dynamic>>().firstWhere(
              (q) => q['Id'] == activeId,
              orElse: () => queues.first as Map<String, dynamic>,
            ))
          : queues.first as Map<String, dynamic>;
      _activeQueue = ((active['SongUuids'] as List?)?.cast<String>() ?? [])
          .map((u) => QueueItemInfo(uuid: u, title: u))
          .toList();
      _activeHistory = ((active['HistoryUuids'] as List?)?.cast<String>() ?? [])
          .map((u) => QueueItemInfo(uuid: u, title: u))
          .toList();
      _playlistSources = (active['PlaylistSources'] as List? ?? []).map((s) {
        final m = s as Map<String, dynamic>;
        final uuids = (m['SongUuids'] as List?)?.cast<String>() ?? <String>[];
        return PlaylistSourceInfo(
          id: m['Id'] ?? '',
          name: m['Name'] ?? '',
          songCount: uuids.length,
          uuids: uuids,
        );
      }).toList();
      _rebuildActivePlaylistFromSources();
      notifyListeners();
    } catch (e) {
      // silently handle
    }
  }

  void _rebuildActivePlaylistFromSources() {
    final seen = <String>{};
    final merged = <QueueItemInfo>[];
    for (final source in _playlistSources) {
      for (final uuid in source.uuids) {
        if (seen.add(uuid)) {
          merged.add(QueueItemInfo(uuid: uuid, title: uuid));
        }
      }
    }
    _activePlaylist = merged;
  }

  Future<void> saveActive() async {
    if (_activeInstanceId == null) return;
    try {
      await api.updateActiveProfile(_buildProfileJson());
    } catch (e) {
      // silently handle
    }
  }

  Map<String, dynamic> _buildProfileJson() {
    return {
      'ActiveQueueId': 'default',
      'Volume': activeInstance?.volume ?? 1.0,
      'Queues': [
        {
          'Id': 'default',
          'Name': 'Default',
          'PlaylistSources': _playlistSources
              .map((s) => {'Id': s.id, 'Name': s.name, 'SongUuids': s.uuids})
              .toList(),
          'SongUuids': _activeQueue.map((q) => q.uuid).toList(),
          'HistoryUuids': _activeHistory.map((q) => q.uuid).toList(),
          'Index': -1,
          'HistoryPosition': -1,
          'PlaylistPosition': 0,
          'Shuffle': activeInstance?.shuffle ?? false,
          'RepeatMode': activeInstance?.repeatMode ?? 'none',
        },
      ],
    };
  }

  // ── Refresh ──

  Future<void> refreshPlayback() async {
    if (_loading) return;
    _loading = true;
    try {
      final list = await api.getInstances();

      var nextActiveId = _activeInstanceId;
      if (nextActiveId == null || !list.any((i) => i.id == nextActiveId)) {
        final attached = list.where((i) => i.attached).toList();
        final serverManaged = attached.where((i) => i.isServerManaged).toList();
        nextActiveId =
            (serverManaged.isNotEmpty
                    ? serverManaged.first
                    : attached.isNotEmpty
                    ? attached.first
                    : list.isNotEmpty
                    ? list.first
                    : null)
                ?.id;
        if (_activeInstanceId == null) _activeInstanceId = nextActiveId;
        if (nextActiveId != null) {
          api.putConfigRaw({'active_instance': nextActiveId});
          await api.saveConfig();
        }
      }

      _backendInstanceIds = list.map((i) => i.id).toSet();
      _instances = list;
      _syncMediaControls();

      if (_activeInstanceId == nextActiveId && _activeInstanceId != null) {
        await loadActiveProfile();
      }

      final active = activeInstance;
      if (active != null) {
        _lastVolume = active.volume;
        _lastTargetLatency = active.targetLatency;
        SharedPreferences.getInstance()
            .then((prefs) {
              prefs.setDouble('last_volume', _lastVolume);
              prefs.setDouble('last_target_latency', _lastTargetLatency);
            })
            .catchError((_) {});
      }

      notifyListeners();
    } catch (e) {
      // silently handle
    } finally {
      _loading = false;
    }
  }

  void _syncMediaControls() {
    if (!_mediaControlsEnabled) {
      _mediaControlInstanceId = null;
      _publishMediaControlSnapshot(null);
      return;
    }

    final eligible = _instances
        .where((i) => i.attached && i.isServerManaged)
        .toList(growable: false);

    PlaybackInstanceInfo? controlled;
    final currentId = _mediaControlInstanceId;
    if (currentId != null) {
      controlled = eligible.where((i) => i.id == currentId).firstOrNull;
    }

    controlled ??= eligible.firstOrNull;
    _mediaControlInstanceId = controlled?.id;

    final snapshot = controlled == null
        ? null
        : MediaControlSnapshot(instance: controlled, baseUrl: api.baseUrl);
    _publishMediaControlSnapshot(snapshot);
  }

  void _publishMediaControlSnapshot(MediaControlSnapshot? snapshot) {
    unawaited(
      _mediaControlService
          .ensureInitialized(_mediaControlCallbacks)
          .then((_) => _mediaControlService.update(snapshot))
          .catchError((_) {}),
    );
  }

  bool _canMediaControl(String instanceId) {
    if (!_mediaControlsEnabled) return false;
    if (_mediaControlInstanceId != instanceId) return false;
    final instance = _instances.where((i) => i.id == instanceId).firstOrNull;
    return instance != null && instance.attached && instance.isServerManaged;
  }

  Future<void> _mediaPlay(String instanceId) async {
    if (!_canMediaControl(instanceId)) return;
    try {
      await api.resume(instanceId);
    } catch (_) {
      await api.play(instanceId);
    }
    await refreshPlayback();
  }

  Future<void> _mediaPause(String instanceId) async {
    if (!_canMediaControl(instanceId)) return;
    await api.pause(instanceId);
    await refreshPlayback();
  }

  Future<void> _mediaSkipToNext(String instanceId) async {
    if (!_canMediaControl(instanceId)) return;
    await api.next(instanceId);
    await refreshPlayback();
  }

  Future<void> _mediaSkipToPrevious(String instanceId) async {
    if (!_canMediaControl(instanceId)) return;
    await api.previous(instanceId);
    await refreshPlayback();
  }

  Future<void> _mediaSeek(String instanceId, Duration position) async {
    if (!_canMediaControl(instanceId)) return;
    await api.seek(instanceId, position.inMilliseconds / 1000.0);
    await refreshPlayback();
  }

  // ── Playback controls ──

  Future<void> togglePlayback() async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.toggle(instance.id);
    await refreshPlayback();
  }

  Future<void> nextTrack() async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.next(instance.id);
    await refreshPlayback();
  }

  Future<void> previousTrack() async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.previous(instance.id);
    await refreshPlayback();
  }

  Future<void> seekActive(double position) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.seek(instance.id, position);
    await refreshPlayback();
  }

  Future<void> setVolumeActive(double volume) async {
    final instance = activeInstance;
    _lastVolume = volume;
    notifyListeners();
    if (instance == null) return;
    await api.setVolume(instance.id, volume);
    await refreshPlayback();
  }

  Future<void> setTargetLatencyActive(double latency) async {
    final instance = activeInstance;
    _lastTargetLatency = latency;
    notifyListeners();
    if (instance == null) return;
    await api.setLatency(instance.id, latency);
    await refreshPlayback();
  }

  Future<void> playSongOnActive(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.play(instance.id, uuid: uuid);
    await refreshPlayback();
  }

  // ── Queue management ──

  Future<void> addSongToActiveQueue(String uuid) async {
    if (_activeInstanceId == null) return;
    _activeQueue.add(QueueItemInfo(uuid: uuid, title: uuid));
    notifyListeners();
    saveActive();
  }

  Future<void> removeQueueItem(int index) async {
    if (_activeInstanceId == null || index < 0 || index >= _activeQueue.length)
      return;
    _activeQueue.removeAt(index);
    notifyListeners();
    saveActive();
  }

  Future<void> clearActiveQueue() async {
    if (_activeInstanceId == null) return;
    _activeQueue.clear();
    notifyListeners();
    saveActive();
  }

  Future<void> clearActiveHistory() async {
    if (_activeInstanceId == null) return;
    _activeHistory.clear();
    notifyListeners();
    saveActive();
  }

  Future<void> moveQueueItem(int from, int to) async {
    if (_activeInstanceId == null) return;
    if (from >= 0 &&
        from < _activeQueue.length &&
        to >= 0 &&
        to < _activeQueue.length) {
      final item = _activeQueue.removeAt(from);
      _activeQueue.insert(to, item);
      notifyListeners();
      saveActive();
    }
  }

  Future<void> removeHistoryItem(int index) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.removeHistoryAt(instance.id, index);
    await refreshPlayback();
  }

  Future<void> moveHistoryItem(int from, int to) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.moveHistory(instance.id, from, to);
    await refreshPlayback();
  }

  Future<void> addSongNextOnActive(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.insertQueueAt(instance.id, uuids: [uuid], index: 0);
    await refreshPlayback();
  }

  Future<void> setSongExcluded(String uuid, bool excluded) async {
    if (!canControlActiveInstance) return;
    await api.setExcluded(uuid, excluded);
    await refreshPlayback();
  }

  Future<void> setShuffle(bool enabled) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.setShuffle(instance.id, enabled);
    await refreshPlayback();
  }

  Future<void> setRepeatMode(String mode) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.setRepeatMode(instance.id, mode);
    await refreshPlayback();
  }

  // ── Playlist sources ──

  Future<void> addTagToActivePlaylist(TagInfo tag) async {
    if (_activeInstanceId == null) return;
    final songs = await api.getSongs(tagId: tag.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _playlistSources.removeWhere((s) => s.id == 'tag_${tag.id}');
    _playlistSources.add(
      PlaylistSourceInfo(
        id: 'tag_${tag.id}',
        name: tag.name,
        songCount: uuids.length,
        uuids: uuids,
      ),
    );
    _mergeSongsToPlaylist(songs);
    notifyListeners();
    saveActive();
  }

  Future<void> addAlbumToActivePlaylist(AlbumInfo album) async {
    if (_activeInstanceId == null) return;
    final songs = await api.getSongs(albumId: album.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _playlistSources.removeWhere((s) => s.id == 'album_${album.id}');
    _playlistSources.add(
      PlaylistSourceInfo(
        id: 'album_${album.id}',
        name: album.name,
        songCount: uuids.length,
        uuids: uuids,
      ),
    );
    _mergeSongsToPlaylist(songs);
    notifyListeners();
    saveActive();
  }

  Future<void> removePlaylistSource(String sourceId) async {
    if (_activeInstanceId == null) return;
    _playlistSources.removeWhere((s) => s.id == sourceId);
    _rebuildActivePlaylistFromSources();
    notifyListeners();
    saveActive();
  }

  void _mergeSongsToPlaylist(List<SongInfo> songs) {
    final existingUuids = _activePlaylist.map((q) => q.uuid).toSet();
    for (final song in songs) {
      if (existingUuids.add(song.uuid)) {
        _activePlaylist.add(
          QueueItemInfo(
            uuid: song.uuid,
            title: song.title,
            artist: song.artist,
            albumId: song.albumId,
            duration: song.duration,
            moduleId: song.moduleId,
          ),
        );
      }
    }
    notifyListeners();
  }

  // ── Polling ──

  void startPolling() {
    _pollTimer?.cancel();
    _pollTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      refreshPlayback();
    });
  }

  void stopPolling() {
    _pollTimer?.cancel();
    _pollTimer = null;
  }

  // ── Position event ──

  void applyPositionEvent(dynamic data) {
    if (data is! Map<String, dynamic>) return;
    final instanceId = data['instanceId'] as String?;
    final position = (data['position'] ?? 0.0).toDouble();
    var changed = false;
    _instances = _instances.map((instance) {
      if (instance.id != instanceId) return instance;
      changed = true;
      return PlaybackInstanceInfo(
        id: instance.id,
        clientId: instance.clientId,
        role: instance.role,
        mode: instance.mode,
        attached: instance.attached,
        isPlaying: instance.isPlaying,
        position: position,
        volume: instance.volume,
        targetLatency: instance.targetLatency,
        queueCount: instance.queueCount,
        queueIndex: instance.queueIndex,
        historyCount: instance.historyCount,
        sampleRate: instance.sampleRate,
        channels: instance.channels,
        shuffle: instance.shuffle,
        repeatMode: instance.repeatMode,
        currentTrack: instance.currentTrack,
        modId: instance.modId,
        gameName: instance.gameName,
      );
    }).toList();
    if (changed) {
      _syncMediaControls();
      notifyListeners();
    }
  }

  // ── Clear state (on disconnect) ──

  void clearOnDisconnect() {
    _loading = false;
    _instances = [];
    _activeInstanceId = null;
    _activeQueue = [];
    _activeHistory = [];
    _activePlaylist = [];
    _playlistSources = [];
    _backendInstanceIds.clear();
    _mediaControlInstanceId = null;
    _publishMediaControlSnapshot(null);
  }

  // ── Dispose ──

  void disposeManager() {
    stopPolling();
    unawaited(_mediaControlService.dispose());
  }
}
