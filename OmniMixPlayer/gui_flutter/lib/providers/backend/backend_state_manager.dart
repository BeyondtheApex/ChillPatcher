/// Backend connection & lifecycle manager.
/// Extracted from AppState during Riverpod migration.
///
/// Owns ApiClient, WsClient, BackendManager instances and all connection
/// lifecycle logic (start/stop/restart, service coordination, discovery).
///
/// Cross-domain callbacks are set by AppState to coordinate with other managers.

import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../services/api_client.dart';
import '../../services/flutter_pcm_playback_service.dart';
import '../../services/ws_client.dart';
import '../../services/backend_manager.dart'
    if (dart.library.js_interop) '../../stubs/backend_manager_web.dart';
import '../../services/platform_service.dart'
    if (dart.library.js_interop) '../../stubs/platform_service_web.dart';
import '../../generated/omni_mix_player/events/ws_events.pb.dart';

class BackendStateManager extends ChangeNotifier {
  late ApiClient api;
  late WsClient ws;
  late final BackendManager backendMgr;
  final FlutterPcmPlaybackService _pcmPlayback = FlutterPcmPlaybackService();
  Timer? _heartbeatTimer;
  Timer? _reconnectTimer;

  // ── State ──
  bool _online = false;
  bool _running = false;
  bool _isWeb = false;
  int _port = 17890;
  String _bind = '127.0.0.1';
  bool _busy = false;
  bool _connecting = false;
  bool _autostart = false;
  bool _minimizeToTray = true;
  String? _audioOutputDeviceId;
  List<AudioOutputDevice> _audioOutputDevices = const [];
  NativeAudioState _nativeAudioState = NativeAudioState.empty;
  DateTime? _lastNativeAudioStateRefresh;

  // Getters
  bool get online => _online;
  bool get running => _running;
  bool get isWeb => _isWeb;
  int get port => _port;
  String get bind => _bind;
  bool get busy => _busy;
  bool get autostart => _autostart;
  bool get minimizeToTray => _minimizeToTray;
  String get apiBaseUrl => api.baseUrl;
  String? get audioOutputDeviceId => _audioOutputDeviceId;
  List<AudioOutputDevice> get audioOutputDevices => _audioOutputDevices;
  NativeAudioState get nativeAudioState => _nativeAudioState;

  // Setters (used by settings / config)
  set portVal(int v) {
    _port = v;
    notifyListeners();
  }

  set bindVal(String v) {
    _bind = v;
    notifyListeners();
  }

  set autostartVal(bool v) {
    _autostart = v;
    notifyListeners();
  }

  set minimizeToTrayVal(bool v) {
    _minimizeToTray = v;
    notifyListeners();
  }

  // ── Cross-domain callbacks (set by AppState) ──
  Future<void> Function()? onNeedRefreshPlayback;
  Future<void> Function()? onNeedRefreshArchives;
  Future<void> Function()? onNeedLoadModules;
  Future<void> Function()? onNeedLoadActiveProfile;
  void Function(dynamic data)? onPositionEvent;
  void Function(String instanceId, TrackChangedEvent event)? onTrackChanged;
  void Function(String instanceId, int state)? onStateChanged;
  void Function(String instanceId, double position)? onPositionChanged;
  void Function()? onEqualizerChanged;
  void Function()? onPlaylistUpdated;
  void Function()? onModulesChanged;
  void Function()? onProfileChanged;
  void Function(String msg)? onError;
  void Function()? onLibraryBump;
  void Function()? onStopCleanup;
  void Function()? onInitComplete; // called after _connectAndLoad finishes

  // ── Service coordination (set by AppState) ──
  String Function()? getServiceState;
  void Function(String v)? setServiceState;
  void Function(bool v)? setServiceAutoStart;

  // ── Init ──

  void init({int? port}) {
    final p = port ?? 17890;
    backendMgr = BackendManager();
    _createClients(p);
    _setupWs();
    backendMgr.onAliveChanged = _onBackendAliveChanged;
    backendMgr.startWatching();
    unawaited(_loadAudioOutputPrefs());
    unawaited(refreshAudioOutputDevices());
    _busy = true;
    _runInitialDetection();
  }

  void initWeb({int? port}) {
    _isWeb = true;
    backendMgr = BackendManager();
    api = ApiClient.forWeb();
    ws = WsClient.forWeb();
    _port = port ?? 17890;
    _setupWs();
    backendMgr.onAliveChanged = _onBackendAliveChanged;
    backendMgr.startWatching();
    _online = true;
    _running = true;
    notifyListeners();
    _connectDirectly();
  }

  // ── Client creation ──

  void _createClients(int fallbackPort) {
    if (backendMgr.usingSocket) {
      api = ApiClient.withSocket(socketPath: backendMgr.socketPath);
      ws = WsClient.withSocket(socketPath: backendMgr.socketPath);
      _port = -1;
    } else {
      final p = backendMgr.port ?? fallbackPort;
      api = ApiClient(port: p);
      ws = WsClient(port: p);
      _port = p;
    }
  }

  // ── Initial detection ──

  Future<void> _runInitialDetection() async {
    notifyListeners();
    try {
      final svcState = await PlatformService.getServiceState();
      setServiceState?.call(svcState);
      setServiceAutoStart?.call(await PlatformService.isServiceAutoStart());

      if (svcState == 'running') {
        final healthy = await backendMgr.checkHealth();
        if (healthy) {
          _applyAliveState(true);
          return;
        }
        await PlatformService.stopService();
        await Future.delayed(const Duration(seconds: 2));
      }

      await backendMgr.forceKillProcess();
      await Future.delayed(const Duration(seconds: 1));

      if (svcState == 'not_installed' || svcState == 'unknown') {
        final ok = await PlatformService.installService();
        if (!ok) {
          onError?.call('Failed to install backend service');
          notifyListeners();
          _busy = false;
          notifyListeners();
          return;
        }
        setServiceState?.call('installed');
        notifyListeners();
        await Future.delayed(const Duration(seconds: 1));
      }

      await PlatformService.startService();
      for (var i = 0; i < 20; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
        if (await backendMgr.checkHealth()) {
          setServiceState?.call('running');
          _applyAliveState(true);
          _busy = false;
          notifyListeners();
          return;
        }
      }
      onError?.call('Backend service failed to start');
      notifyListeners();
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  void _onBackendAliveChanged(bool alive) {
    _applyAliveState(alive);
  }

  Future<void> refreshAudioOutputDevices() async {
    if (_isWeb) return;
    try {
      _audioOutputDevices = await _pcmPlayback.listOutputDevices();
      notifyListeners();
    } catch (e) {
      onError?.call('Failed to list audio output devices: $e');
    }
  }

  Future<void> setAudioOutputDevice(String? deviceId) async {
    final normalized = deviceId == null || deviceId.isEmpty ? null : deviceId;
    _audioOutputDeviceId = normalized;
    notifyListeners();
    try {
      final prefs = await SharedPreferences.getInstance();
      if (normalized == null) {
        await prefs.remove('audio_output_device_id');
      } else {
        await prefs.setString('audio_output_device_id', normalized);
      }
      await _pcmPlayback.setOutputDevice(normalized);
      _nativeAudioState = await _pcmPlayback.getState();
      notifyListeners();
    } catch (e) {
      onError?.call('Failed to switch audio output device: $e');
    }
  }

  Future<void> _loadAudioOutputPrefs() async {
    if (_isWeb) return;
    try {
      final prefs = await SharedPreferences.getInstance();
      _audioOutputDeviceId = prefs.getString('audio_output_device_id');
      await _pcmPlayback.setOutputDevice(_audioOutputDeviceId);
      notifyListeners();
    } catch (_) {}
  }

  void _applyAliveState(bool alive) {
    if (_online == alive && _running == alive) return;

    _online = alive;
    _running = alive;

    if (alive) {
      _reconnectTimer?.cancel();
      _reconnectTimer = null;
      if (_isWeb) {
        _connectDirectly();
      } else {
        _connectAndLoad();
      }
    } else {
      _stopHeartbeat();
      _reconnectTimer?.cancel();
      _reconnectTimer = null;
      unawaited(_pcmPlayback.stop());
      ws.disconnect();
      onStopCleanup?.call();
    }
    notifyListeners();
  }

  Future<void> _connectAndLoad() async {
    if (_connecting) return;
    _connecting = true;
    var completed = false;
    try {
      final discovered = await backendMgr.discover();
      if (discovered == null) return;

      ws.disconnect();
      api.dispose();
      _createClients(17890);
      _setupWs();

      for (var i = 0; i < 10; i++) {
        final ok = await backendMgr.checkHealth();
        if (ok) break;
        await Future.delayed(const Duration(milliseconds: 300));
      }

      final connected = await ws.connectOnce();
      if (connected) {
        final profile = await api.connectController();
        _startHeartbeat(profile.id);
        await _startNativeAudio(profile.id);
        await onNeedRefreshPlayback?.call();
        await onNeedLoadModules?.call();
        await onNeedRefreshArchives?.call();
        onLibraryBump?.call();
        onInitComplete?.call();
        completed = true;
        notifyListeners();
      }
    } catch (e, st) {
      debugPrint('OmniMix: _connectAndLoad failed: $e\n$st');
    } finally {
      _connecting = false;
      if (!completed && !_busy) {
        _online = false;
        _running = false;
        notifyListeners();
        _scheduleReconnect();
      }
    }
  }

  void _scheduleReconnect() {
    if (_reconnectTimer != null || _connecting || _busy) return;
    _reconnectTimer = Timer(const Duration(seconds: 1), () async {
      _reconnectTimer = null;
      if (_online || _connecting) return;
      try {
        final alive = await backendMgr.discover() != null;
        if (alive) {
          _applyAliveState(true);
        }
      } catch (_) {}
    });
  }

  Future<void> _connectDirectly() async {
    if (_connecting) return;
    _connecting = true;
    var completed = false;
    try {
      for (var i = 0; i < 10; i++) {
        final ok = await api.checkHealth();
        if (ok) break;
        await Future.delayed(const Duration(milliseconds: 300));
      }
      final connected = await ws.connectOnce();
      if (connected) {
        final profile = await api.connectController();
        _startHeartbeat(profile.id);
        if (!_isWeb) {
          await _startNativeAudio(profile.id);
        }
        await onNeedRefreshPlayback?.call();
        await onNeedLoadModules?.call();
        await onNeedRefreshArchives?.call();
        onLibraryBump?.call();
        onInitComplete?.call();
        completed = true;
        notifyListeners();
      }
    } catch (e, st) {
      debugPrint('OmniMix Web: _connectDirectly failed: $e\n$st');
    } finally {
      _connecting = false;
      if (!completed && !_busy) {
        _online = false;
        _running = false;
        notifyListeners();
        _scheduleReconnect();
      }
    }
  }

  Future<void> _startNativeAudio(String instanceId) async {
    try {
      await _pcmPlayback.startForInstance(instanceId);
      _nativeAudioState = await _pcmPlayback.getState();
      _lastNativeAudioStateRefresh = DateTime.now();
      notifyListeners();
    } catch (e) {
      onError?.call('Failed to start native PCM playback: $e');
    }
  }

  Future<void> _refreshNativeAudioStateThrottled() async {
    if (_isWeb) return;
    final now = DateTime.now();
    final last = _lastNativeAudioStateRefresh;
    if (last != null && now.difference(last) < const Duration(seconds: 1)) {
      return;
    }
    _lastNativeAudioStateRefresh = now;
    try {
      _nativeAudioState = await _pcmPlayback.getState();
      notifyListeners();
    } catch (_) {}
  }

  // ── WebSocket setup ──

  void _setupWs() {
    ws.onProtoEvent = (event) {
      switch (event.type) {
        case 'backend.state.changed':
          if (event.hasBackendState()) {
            final r = event.backendState.running;
            if (_running != r) {
              _running = r;
              _online = r;
              notifyListeners();
            }
          }
          break;
        case 'instances.changed':
        case 'queue.changed':
          onNeedRefreshPlayback?.call();
          onNeedRefreshArchives?.call();
          break;
        case 'track.changed':
          if (event.hasTrackChanged()) {
            onTrackChanged?.call(
              event.trackChanged.instanceId,
              event.trackChanged,
            );
          }
          onNeedRefreshPlayback?.call();
          break;
        case 'state.changed':
          if (event.hasStateChanged()) {
            onStateChanged?.call(
              event.stateChanged.instanceId,
              event.stateChanged.state,
            );
          }
          break;
        case 'position.changed':
          if (event.hasPositionChanged()) {
            onPositionChanged?.call(
              event.positionChanged.instanceId,
              event.positionChanged.position,
            );
            unawaited(_refreshNativeAudioStateThrottled());
          }
          break;
        case 'profile.changed':
          onProfileChanged?.call();
          break;
        case 'favorite.changed':
        case 'exclude.changed':
        case 'playlist.updated':
          onNeedRefreshPlayback?.call();
          onLibraryBump?.call();
          notifyListeners();
          break;
        case 'module.loaded':
        case 'module.unloaded':
          onNeedLoadModules?.call();
          onLibraryBump?.call();
          notifyListeners();
          break;
        default:
          break;
      }
    };

    // JSON text frames are only used for ui_push (handled by onUiPush below).
    // All other events now go through protobuf binary → onProtoEvent above.

    ws.onUiPush = (push) {
      if (push.replace && push.tree != null) {
        onUiPushCallback?.call(push);
      }
    };

    ws.onDisconnected = () {
      if (_online) {
        _online = false;
        _running = false;
        _stopHeartbeat();
        unawaited(_pcmPlayback.stop());
        onStopCleanup?.call();
        onLibraryBump?.call();
        _scheduleReconnect();
        notifyListeners();
      }
    };
  }

  /// AppState sets this after creating the manager to handle module UI pushes.
  void Function(dynamic)? onUiPushCallback;

  // ── Backend lifecycle ──

  Future<void> start() async {
    if (_busy) return;
    _busy = true;
    notifyListeners();
    try {
      await backendMgr.forceKillProcess();
      await Future.delayed(const Duration(seconds: 1));

      final svcState = await PlatformService.getServiceState();
      setServiceState?.call(svcState);

      if (svcState == 'installed' || svcState == 'running') {
        final currentExe = PlatformService.backendExePath;
        final registeredExe = await PlatformService.getServiceBinaryPath();
        if (currentExe != null &&
            registeredExe != null &&
            !PlatformService.arePathsEqual(currentExe, registeredExe)) {
          final ok = await PlatformService.installService();
          if (ok) {
            setServiceState?.call('installed');
            await Future.delayed(const Duration(seconds: 1));
          }
        }
      }

      if (svcState == 'running') {
        final healthy = await backendMgr.checkHealth();
        if (healthy) {
          _applyAliveState(true);
          _busy = false;
          notifyListeners();
          return;
        }
        await PlatformService.stopService();
        await Future.delayed(const Duration(seconds: 2));
      }

      if (svcState != 'installed' && svcState != 'running') {
        final ok = await PlatformService.installService();
        if (!ok) {
          onError?.call('Failed to install backend service');
          notifyListeners();
          _busy = false;
          notifyListeners();
          return;
        }
        setServiceState?.call('installed');
        await Future.delayed(const Duration(seconds: 1));
      }

      await PlatformService.startService();
      for (var i = 0; i < 20; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
        if (await backendMgr.checkHealth()) {
          setServiceState?.call('running');
          _applyAliveState(true);
          _busy = false;
          notifyListeners();
          return;
        }
      }
      onError?.call('Backend service failed to start');
      notifyListeners();
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  Future<void> stop() async {
    if (_busy) return;
    _busy = true;
    notifyListeners();
    try {
      await _pcmPlayback.stop();
      try {
        await api.stopBackend();
      } catch (_) {}

      final svcState = await PlatformService.getServiceState();
      if (svcState == 'running') {
        await PlatformService.stopService();
      }

      try {
        await backendMgr.forceKillProcess();
      } catch (_) {}

      setServiceState?.call('installed');
      _running = false;
      _online = false;
      notifyListeners();
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  Future<void> toggle() async {
    if (_busy) return;
    if (_running) {
      await stop();
    } else {
      await start();
    }
  }

  Future<void> restart() async {
    if (_busy) return;
    _busy = true;
    notifyListeners();
    try {
      await stop();
      await Future.delayed(const Duration(seconds: 1));
      await start();
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  /// Full quit: stop watcher, stop backend.
  Future<void> fullQuit() async {
    backendMgr.stopWatching();
    await stop();
  }

  // ── Check health ──

  Future<bool> checkHealth() => backendMgr.checkHealth();

  // ── Dispose ──

  void disposeManager() {
    _stopHeartbeat();
    _reconnectTimer?.cancel();
    _reconnectTimer = null;
    unawaited(_pcmPlayback.stop());
    ws.disconnect();
    api.dispose();
    backendMgr.dispose();
  }

  void _startHeartbeat(String instanceId) {
    if (instanceId.isEmpty) return;
    _heartbeatTimer?.cancel();
    _heartbeatTimer = Timer.periodic(const Duration(seconds: 15), (
      timer,
    ) async {
      if (!_online) {
        timer.cancel();
        return;
      }
      try {
        final alive = await api.heartbeat(instanceId);
        if (!alive && _online) {
          timer.cancel();
          _heartbeatTimer = null;
          await _connectAndLoad();
        }
      } catch (e) {
        debugPrint('OmniMix: Heartbeat failed: $e');
      }
    });
  }

  void _stopHeartbeat() {
    _heartbeatTimer?.cancel();
    _heartbeatTimer = null;
  }
}
