/// Playback state & control manager.
/// Extracted from AppState during Riverpod migration.
///
/// Manages playback instances, active instance selection, queue/history/playlist
/// state, and all playback control operations.
/// Now uses proto-generated types from gRPC.

import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../services/api_client.dart';
import '../../services/media_control_service.dart';
import '../../generated/omni_mix_player/models/instance.pb.dart';
import '../../generated/omni_mix_player/models/track.pb.dart';
import '../../generated/omni_mix_player/models/album.pb.dart';
import '../../generated/omni_mix_player/models/tag.pb.dart';
import '../../generated/omni_mix_player/models/playlist.pb.dart';
import '../../generated/omni_mix_player/services/playback.pb.dart';
import '../../generated/omni_mix_player/events/ws_events.pb.dart';
import '../../generated/omni_mix_player/models/common.pbenum.dart';

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
  List<InstanceSummary> _instances = [];
  String? _activeInstanceId;
  List<QueueTrack> _activeQueue = [];
  List<QueueTrack> _activeHistory = [];
  List<QueueTrack> _activePlaylist = [];
  List<PlaylistSourceState> _playlistSources = [];
  bool _loading = false;
  Timer? _pollTimer;
  double _lastVolume = 1.0;
  double _lastTargetLatency = 0.05;
  Set<String> _backendInstanceIds = {};
  String? _mediaControlInstanceId;
  bool _mediaControlsEnabled = true;

  /// Cached instance capabilities, refreshed in refreshPlayback/loadActiveProfile.
  final Map<String, InstanceCapabilities> _capabilities = {};

  /// Cached playback status for the active instance.
  PlaybackStatus? _status;

  // ── Status getters (delegate to cached PlaybackStatus) ──

  PlaybackStatus? get status => _status;
  String get currentTitle => _status?.title ?? '';
  String get currentArtist => _status?.artist ?? '';
  String get currentAlbumId => _status?.albumId ?? '';
  double get currentDuration => _status?.duration ?? 0.0;
  double get currentPosition => _status?.position ?? 0.0;
  bool get isPlaying => _status?.isPlaying ?? false;
  bool get shuffle => _status?.shuffle ?? false;
  RepeatMode get repeatMode =>
      _status?.repeatMode ?? RepeatMode.REPEAT_MODE_NONE;
  double get statusVolume => _status?.volume ?? 1.0;

  // ── Capability helpers ──

  /// Whether the active instance supports server-controlled playback.
  bool get canControlActiveInstance {
    final id = _activeInstanceId;
    if (id == null) return false;
    return isInstanceOnline(id) &&
        _capabilities[id]?.serverControlledPlayback == true;
  }

  /// Check if a specific instance can be controlled (playback, queue, etc.).
  bool canControlInstance(String instanceId) {
    return isInstanceOnline(instanceId) &&
        _capabilities[instanceId]?.serverControlledPlayback == true;
  }

  bool get canManageActiveLibrary {
    final id = _activeInstanceId;
    if (id == null) return false;
    return canManageInstanceLibrary(id);
  }

  bool canManageInstanceLibrary(String instanceId) {
    final caps = _capabilities[instanceId];
    return caps?.serverControlledPlayback == true ||
        caps?.playlistManagement == true;
  }

  bool canManageInstanceQueue(String instanceId) {
    return _capabilities[instanceId]?.queueManagement == true;
  }

  /// Whether the active instance supports a specific capability.
  bool hasCapability(bool Function(InstanceCapabilities c) check) {
    final id = _activeInstanceId;
    if (id == null) return false;
    final caps = _capabilities[id];
    if (caps == null) return false;
    return check(caps);
  }

  /// Whether the instance at [instanceId] can be used for media controls.
  bool _canMediaControl(String instanceId) {
    if (!_mediaControlsEnabled) return false;
    if (_mediaControlInstanceId != instanceId) return false;
    final instance = _instances.where((i) => i.id == instanceId).firstOrNull;
    return instance != null &&
        instance.isOnline &&
        canControlInstance(instanceId);
  }

  // Getters
  List<InstanceSummary> get instances => _instances;
  String? get activeInstanceId => _activeInstanceId;
  List<QueueTrack> get activeQueue => _activeQueue;
  List<QueueTrack> get activeHistory => _activeHistory;
  List<QueueTrack> get activePlaylist => _activePlaylist;
  List<PlaylistSourceState> get playlistSources => _playlistSources;
  bool get loading => _loading;
  double get lastVolume => _lastVolume;
  double get lastTargetLatency => _lastTargetLatency;
  int get instanceCount => _instances.length;
  int get attachedAudioClientCount =>
      _instances.where((i) => i.isOnline).length;

  Set<String> get backendInstanceIds => _backendInstanceIds;
  bool get mediaControlsEnabled => _mediaControlsEnabled;

  ApiClient get api => _getApi();

  InstanceSummary? get activeInstance {
    final id = _activeInstanceId;
    if (id == null) return _instances.isNotEmpty ? _instances.first : null;
    for (final instance in _instances) {
      if (instance.id == id) return instance;
    }
    return _instances.isNotEmpty ? _instances.first : null;
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
      final profile = await api.getInstanceProfile(_activeInstanceId!);
      if (profile == null) return;
      // Cache capabilities
      if (profile.hasCapabilities()) {
        _capabilities[_activeInstanceId!] = profile.capabilities;
      }
      _lastVolume = profile.volume;
      _lastTargetLatency = profile.targetLatency;
      final timeline = profile.playbackTimeline;
      try {
        _activeQueue = await api.getInstanceQueue(_activeInstanceId!);
      } catch (_) {
        _activeQueue = timeline.manualQueueUuids
            .map((u) => QueueTrack()..uuid = u)
            .toList();
      }
      try {
        _activeHistory = await api.getInstanceHistory(_activeInstanceId!);
      } catch (_) {
        _activeHistory = timeline.historyUuids
            .map((u) => QueueTrack()..uuid = u)
            .toList();
      }
      _playlistSources = timeline.playlistSources.toList();
      await _rebuildActivePlaylistFromSources();
      notifyListeners();
    } catch (e) {
      // silently handle
    }
  }

  Future<void> _rebuildActivePlaylistFromSources() async {
    final seen = <String>{};
    final merged = <QueueTrack>[];
    for (final source in _playlistSources) {
      final songs = await _resolveSourceSongs(source);
      for (final song in songs) {
        if (song.isExcluded || !seen.add(song.uuid)) continue;
        merged.add(
          QueueTrack()
            ..uuid = song.uuid
            ..title = song.title
            ..artist = song.artist
            ..albumId = song.albumId
            ..duration = song.duration
            ..moduleId = song.moduleId
            ..coverUri = song.coverUri,
        );
      }
    }
    _activePlaylist = merged;
  }

  Future<List<Track>> _resolveSourceSongs(PlaylistSourceState source) async {
    switch (source.kind) {
      case PlaylistSourceKind.PLAYLIST_SOURCE_KIND_TAG:
        return api.getSongs(tagId: source.refId);
      case PlaylistSourceKind.PLAYLIST_SOURCE_KIND_ALBUM:
        return api.getSongs(albumId: source.refId);
      case PlaylistSourceKind.PLAYLIST_SOURCE_KIND_PLAYLIST:
        return api.getSongs(playlistId: source.refId);
      case PlaylistSourceKind.PLAYLIST_SOURCE_KIND_TRACK:
        final track = await api.getSong(source.refId);
        return track == null || track.isExcluded ? const [] : [track];
      default:
        final songs = <Track>[];
        for (final uuid in source.uuids) {
          final track = await api.getSong(uuid);
          if (track != null && !track.isExcluded) songs.add(track);
        }
        return songs;
    }
  }

  Future<void> saveActive() async {
    if (_activeInstanceId == null) return;
    try {
      await api.setPlaylistSources(_activeInstanceId!, _playlistSources);
    } catch (e) {
      // silently handle
    }
  }

  // ── Refresh ──

  Future<void> refreshPlayback() async {
    if (_loading) return;
    _loading = true;
    try {
      final list = await api.getInstances();

      var nextActiveId = _activeInstanceId;
      if (nextActiveId == null || !list.any((i) => i.id == nextActiveId)) {
        final online = list.where((i) => i.isOnline).toList();
        nextActiveId =
            (online.isNotEmpty
                    ? online.first
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

      _backendInstanceIds = list
          .where((i) => i.isOnline)
          .map((i) => i.id)
          .toSet();
      _instances = list;
      _syncMediaControls();

      final active = activeInstance;
      if (_activeInstanceId == nextActiveId && _activeInstanceId != null) {
        await loadActiveProfile();
      }

      if (active != null) {
        try {
          final prof = await api.getInstanceProfile(active.id);
          if (prof != null) {
            _lastVolume = prof.volume;
            _lastTargetLatency = prof.targetLatency;
            if (prof.hasCapabilities()) {
              _capabilities[active.id] = prof.capabilities;
            }
          }
          _status = await api.getPlaybackStatus(active.id);
          if (_status != null) {
            _lastVolume = _status!.volume;
          }
        } catch (_) {}
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
        .where((i) => i.isOnline && canControlInstance(i.id))
        .toList(growable: false);

    InstanceSummary? controlled;
    final currentId = _mediaControlInstanceId;
    if (currentId != null) {
      controlled = eligible.where((i) => i.id == currentId).firstOrNull;
    }

    controlled ??= eligible.firstOrNull;
    _mediaControlInstanceId = controlled?.id;

    final snapshot = controlled == null
        ? null
        : MediaControlSnapshot(
            instance: controlled,
            baseUrl: api.baseUrl,
            canSeek: canControlInstance(controlled.id),
          );
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
    if (!canControlInstance(instanceId)) return;
    await api.seek(instanceId, position.inMilliseconds / 1000.0);
    await refreshPlayback();
  }

  // ── Playback controls ──

  Future<void> togglePlayback() async {
    final instance = activeInstance;
    if (instance == null || !canControlInstance(instance.id)) return;
    await api.toggle(instance.id);
    await refreshPlayback();
  }

  Future<void> nextTrack() async {
    final instance = activeInstance;
    if (instance == null || !canControlInstance(instance.id)) return;
    await api.next(instance.id);
    await refreshPlayback();
  }

  Future<void> previousTrack() async {
    final instance = activeInstance;
    if (instance == null || !canControlInstance(instance.id)) return;
    await api.previous(instance.id);
    await refreshPlayback();
  }

  Future<void> seekActive(double position) async {
    final instance = activeInstance;
    if (instance == null || !canControlInstance(instance.id)) return;
    await api.seek(instance.id, position);
    await refreshPlayback();
  }

  Future<void> setVolumeActive(double volume) async {
    final instance = activeInstance;
    if (instance == null || !hasCapability((c) => c.volumeControl)) return;
    _lastVolume = volume;
    notifyListeners();
    await api.setVolume(instance.id, volume);
    await refreshPlayback();
  }

  Future<void> setTargetLatencyActive(double latency) async {
    final instance = activeInstance;
    if (instance == null || !hasCapability((c) => c.audioPlayback)) return;
    _lastTargetLatency = latency;
    notifyListeners();
    await api.setLatency(instance.id, latency);
    await refreshPlayback();
  }

  Future<void> playSongOnActive(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !canControlInstance(instance.id)) return;
    await api.play(instance.id, uuid: uuid);
    await refreshPlayback();
  }

  // ── Queue management ──

  Future<void> addSongToActiveQueue(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    await api.addToQueue(instance.id, uuid);
    await refreshPlayback();
  }

  Future<void> removeQueueItem(int index) async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    if (index < 0 || index >= _activeQueue.length) return;
    await api.removeQueueAt(instance.id, index);
    await refreshPlayback();
  }

  Future<void> clearActiveQueue() async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    await api.clearQueue(instance.id);
    await refreshPlayback();
  }

  Future<void> clearActiveHistory() async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    await api.clearHistory(instance.id);
    await refreshPlayback();
  }

  Future<void> moveQueueItem(int from, int to) async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    if (from < 0 || from >= _activeQueue.length) return;
    if (to < 0 || to >= _activeQueue.length) return;
    await api.moveQueue(instance.id, from, to);
    await refreshPlayback();
  }

  Future<void> removeHistoryItem(int index) async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    await api.removeHistoryAt(instance.id, index);
    await refreshPlayback();
  }

  Future<void> moveHistoryItem(int from, int to) async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    await api.moveHistory(instance.id, from, to);
    await refreshPlayback();
  }

  Future<void> addSongNextOnActive(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !canManageInstanceQueue(instance.id)) return;
    await api.insertQueueAt(instance.id, uuids: [uuid], index: 0);
    await refreshPlayback();
  }

  Future<void> setSongExcluded(String uuid, bool excluded) async {
    if (!canManageActiveLibrary) return;
    await api.setSongExcluded(uuid, excluded);
    await refreshPlayback();
  }

  Future<void> setShuffle(bool enabled) async {
    final instance = activeInstance;
    if (instance == null || !hasCapability((c) => c.shuffle)) return;
    await api.setShuffle(instance.id, enabled);
    await refreshPlayback();
  }

  Future<void> setRepeatMode(String mode) async {
    final instance = activeInstance;
    if (instance == null || !hasCapability((c) => c.repeat)) return;
    await api.setRepeatMode(instance.id, mode);
    await refreshPlayback();
  }

  // ── Playlist sources ──

  Future<void> addTagToActivePlaylist(Tag tag) async {
    if (_activeInstanceId == null || !canManageActiveLibrary) return;
    final songs = await api.getSongs(tagId: tag.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _playlistSources.removeWhere((s) => s.id == 'tag_${tag.id}');
    _playlistSources.add(
      PlaylistSourceState()
        ..id = 'tag_${tag.id}'
        ..name = tag.name
        ..kind = PlaylistSourceKind.PLAYLIST_SOURCE_KIND_TAG
        ..refId = tag.id
        ..uuids.addAll(uuids),
    );
    _mergeSongsToPlaylist(songs);
    notifyListeners();
    saveActive();
  }

  Future<void> addAlbumToActivePlaylist(Album album) async {
    if (_activeInstanceId == null || !canManageActiveLibrary) return;
    final songs = await api.getSongs(albumId: album.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _playlistSources.removeWhere((s) => s.id == 'album_${album.id}');
    _playlistSources.add(
      PlaylistSourceState()
        ..id = 'album_${album.id}'
        ..name = album.title
        ..kind = PlaylistSourceKind.PLAYLIST_SOURCE_KIND_ALBUM
        ..refId = album.id
        ..uuids.addAll(uuids),
    );
    _mergeSongsToPlaylist(songs);
    notifyListeners();
    saveActive();
  }

  Future<void> addPlaylistToActivePlaylist(Playlist playlist) async {
    if (_activeInstanceId == null || !canManageActiveLibrary) return;
    final songs = await api.getSongs(playlistId: playlist.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _playlistSources.removeWhere((s) => s.id == 'playlist_${playlist.id}');
    _playlistSources.add(
      PlaylistSourceState()
        ..id = 'playlist_${playlist.id}'
        ..name = playlist.name
        ..kind = PlaylistSourceKind.PLAYLIST_SOURCE_KIND_PLAYLIST
        ..refId = playlist.id
        ..uuids.addAll(uuids),
    );
    _mergeSongsToPlaylist(songs);
    notifyListeners();
    saveActive();
  }

  Future<void> addTrackToActivePlaylist(Track track) async {
    if (_activeInstanceId == null || !canManageActiveLibrary) return;
    _playlistSources.removeWhere((s) => s.id == 'track_${track.uuid}');
    _playlistSources.add(
      PlaylistSourceState()
        ..id = 'track_${track.uuid}'
        ..name = track.title
        ..kind = PlaylistSourceKind.PLAYLIST_SOURCE_KIND_TRACK
        ..refId = track.uuid
        ..uuids.add(track.uuid),
    );
    _mergeSongsToPlaylist([track]);
    notifyListeners();
    saveActive();
  }

  Future<void> removePlaylistSource(String sourceId) async {
    if (_activeInstanceId == null || !canManageActiveLibrary) return;
    _playlistSources.removeWhere((s) => s.id == sourceId);
    await _rebuildActivePlaylistFromSources();
    notifyListeners();
    saveActive();
  }

  void _mergeSongsToPlaylist(List<Track> songs) {
    final existingUuids = _activePlaylist.map((q) => q.uuid).toSet();
    for (final song in songs) {
      if (!song.isExcluded && existingUuids.add(song.uuid)) {
        _activePlaylist.add(
          QueueTrack()
            ..uuid = song.uuid
            ..title = song.title
            ..artist = song.artist
            ..albumId = song.albumId
            ..duration = song.duration
            ..moduleId = song.moduleId,
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

  // ── Proto event handlers ──

  void applyTrackChanged(String instanceId, TrackChangedEvent e) {
    if (_activeInstanceId != instanceId) return;
    final next = (_status ?? PlaybackStatus()).deepCopy()
      ..trackUuid = e.uuid
      ..title = e.title
      ..artist = e.artist
      ..albumId = e.albumId
      ..duration = e.duration
      ..position = 0;
    _status = next;
    _syncMediaControls();
    notifyListeners();
  }

  void applyStateChanged(String instanceId, int state) {
    if (_activeInstanceId != instanceId) return;
    final next = (_status ?? PlaybackStatus()).deepCopy()
      ..isPlaying = state == 1;
    _status = next;
    _syncMediaControls();
    notifyListeners();
  }

  void applyPositionChanged(String instanceId, double position) {
    if (_activeInstanceId != instanceId) return;
    final next = (_status ?? PlaybackStatus()).deepCopy()..position = position;
    _status = next;
    _syncMediaControls();
    notifyListeners();
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
    _status = null;
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
