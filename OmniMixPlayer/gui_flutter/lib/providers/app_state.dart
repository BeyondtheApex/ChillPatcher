import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/node_data.dart';
import '../models/mod_manifest.dart';
import '../services/api_client.dart';
import '../services/ws_client.dart';
import '../services/backend_manager.dart';
import '../services/platform_service.dart';
import '../services/port_file.dart';
import '../services/logger.dart';
import '../services/mod_deployment_service.dart';

enum AppThemeMode { light, dark, system }

class AppState extends ChangeNotifier {
  late ApiClient api;
  late WsClient ws;
  late final BackendManager _backendMgr;

  // Game Integration (Mod Manager)
  final Map<String, String> _gamePaths = {};
  final Map<String, BepInExStatus> _bepinexStatuses = {};
  final Map<String, ModStatus> _modStatuses = {};
  final List<String> _deploymentLogs = [];
  bool _deploymentBusy = false;

  String get gamePath => gamePathFor('chill_with_you');
  BepInExStatus get bepinexStatus => bepinexStatusFor('chill_with_you');
  ModStatus get modStatus => modStatusFor('chill_with_you');
  List<String> get deploymentLogs => _deploymentLogs;
  bool get deploymentBusy => _deploymentBusy;

  String gamePathFor(String gameId) => _gamePaths[gameId] ?? '';
  BepInExStatus bepinexStatusFor(String gameId) =>
      _bepinexStatuses[gameId] ?? BepInExStatus.notInstalled;
  ModStatus modStatusFor(String gameId) =>
      _modStatuses[gameId] ?? ModStatus.notInstalled;

  // Backend
  bool _backendOnline = false;
  bool _backendRunning = false;
  int _backendPort = 17890;
  String _backendBind = '127.0.0.1';
  bool _backendBusy = false; // True when restarting
  bool _autostart = false;
  bool _minimizeToTray = true;
  String _serviceState =
      'unknown'; // 'running', 'installed', 'not_installed', 'unknown'
  bool _serviceBusy = false;
  bool _serviceAutoStart = false;
  String? _serviceResult; // Result message after install/uninstall
  Timer? _serviceResultTimer;

  // Appearance
  AppThemeMode _themeMode = AppThemeMode.system;
  String _language = 'system';

  // Modules
  List<ModuleInfoResponse> _modules = [];
  bool _modulesLoading = false;
  String? _activeModuleId;
  RawNodeData? _moduleUiTree;
  RawNodeData? _overlayUiTree;
  String _overlayMode = ''; // 'about', 'ui'
  String _overlayTitle = '';

  // Navigation
  int _currentTab = 0;

  // Playback
  List<PlaybackInstanceInfo> _playbackInstances = [];
  String? _activeInstanceId;
  List<QueueItemInfo> _activeQueue = [];
  List<QueueItemInfo> _activeHistory = [];
  List<QueueItemInfo> _activePlaylist = [];
  List<PlaylistSourceInfo> _playlistSources = [];
  bool _playbackLoading = false;
  Timer? _playbackPollTimer;

  // Notifications
  String? _lastError;

  // Getters
  String get apiBaseUrl => api.baseUrl;
  bool get backendOnline => _backendOnline;
  bool get backendRunning => _backendRunning;
  bool get backendBusy => _backendBusy;
  String get backendPort => '$_backendPort';
  String get backendBind => _backendBind;
  bool get autostart => _autostart;
  bool get minimizeToTray => _minimizeToTray;
  String get serviceState => _serviceState;
  bool get serviceBusy => _serviceBusy;
  bool get serviceAutoStart => _serviceAutoStart;
  String? get serviceResult => _serviceResult;
  AppThemeMode get themeMode => _themeMode;
  String get language => _language;
  List<ModuleInfoResponse> get modules => _modules;
  bool get modulesLoading => _modulesLoading;
  String? get activeModuleId => _activeModuleId;
  RawNodeData? get moduleUiTree => _moduleUiTree;
  RawNodeData? get overlayUiTree => _overlayUiTree;
  String get overlayMode => _overlayMode;
  String get overlayTitle => _overlayTitle;
  int get currentTab => _currentTab;
  List<PlaybackInstanceInfo> get playbackInstances => _playbackInstances;
  String? get activeInstanceId => _activeInstanceId;
  List<QueueItemInfo> get activeQueue => _activeQueue;
  List<QueueItemInfo> get activeHistory => _activeHistory;
  List<QueueItemInfo> get activePlaylist => _activePlaylist;
  List<PlaylistSourceInfo> get playlistSources => _playlistSources;
  bool get playbackLoading => _playbackLoading;
  int get playbackInstanceCount => _playbackInstances.length;
  int get attachedAudioClientCount => _playbackInstances.where((i) => i.attached).length;

  PlaybackInstanceInfo? get activeInstance {
    final id = _activeInstanceId;
    if (id == null) return _playbackInstances.isNotEmpty ? _playbackInstances.first : null;
    for (final instance in _playbackInstances) {
      if (instance.id == id) return instance;
    }
    return _playbackInstances.isNotEmpty ? _playbackInstances.first : null;
  }

  bool get canControlActiveInstance => activeInstance?.isServerManaged == true;

  bool get hasOverlay => _overlayUiTree != null || _overlayMode == 'about';
  bool get hasModuleDetail => _activeModuleId != null && _moduleUiTree != null;

  /// 取出并清除最后一条错误消息（消费后变为 null）
  String? consumeError() {
    final error = _lastError;
    _lastError = null;
    return error;
  }

  // ──────────────────────────────────────────────
  //  Init & detection
  // ──────────────────────────────────────────────

  void init({int? port}) {
    final p = port ?? 17890;
    GuiLogger().conn('AppState.init: port=$p');
    _backendMgr = BackendManager();

    // Try discovering backend first — it may be on socket
    _createClients(p);

    _setupWs();

    // Start watching backend via 3-step detection
    _backendMgr.onAliveChanged = _onBackendAliveChanged;
    _backendMgr.startWatching();

    // Run initial detection → auto-start if not running
    _backendBusy = true;
    _runInitialDetection();
    loadGameIntegrationSettings();
  }

  void _createClients(int port) {
    if (_backendMgr.usingSocket) {
      api = ApiClient.withSocket(socketPath: _backendMgr.socketPath);
      ws = WsClient.withSocket(socketPath: _backendMgr.socketPath);
      _backendPort = -1;
      GuiLogger().conn('AppState._createClients: socket mode');
    } else {
      final p = _backendMgr.port ?? port;
      api = ApiClient(port: p);
      ws = WsClient(port: p);
      _backendPort = p;
      GuiLogger().conn('AppState._createClients: TCP mode port=$p');
    }
  }

  Future<void> _runInitialDetection() async {
    _serviceBusy = true;
    _backendBusy = true;
    notifyListeners();
    try {
      _serviceState = await PlatformService.getServiceState();
      _serviceAutoStart = await PlatformService.isServiceAutoStart();
      GuiLogger().conn(
        'AppState._runInitialDetection: serviceState=$_serviceState, serviceAutoStart=$_serviceAutoStart',
      );

      // ═══ Pure service mode: no process fallback ═══

      // 1. Service already running → connect directly
      if (_serviceState == 'running') {
        final healthy = await _backendMgr.checkHealth();
        if (healthy) {
          GuiLogger().conn(
            'AppState._runInitialDetection: service healthy, connecting...',
          );
          _applyAliveState(true);
          return;
        }
        // Service stuck → restart it
        GuiLogger().conn(
          'AppState._runInitialDetection: service stuck, restarting...',
        );
        await PlatformService.stopService();
        await Future.delayed(const Duration(seconds: 2));
      }

      // 2. Kill any stray process (safety)
      await _backendMgr.forceKillProcess();
      await Future.delayed(const Duration(seconds: 1));

      // 3. Service not installed → auto-install
      if (_serviceState == 'not_installed' || _serviceState == 'unknown') {
        GuiLogger().conn(
          'AppState._runInitialDetection: auto-installing service...',
        );
        final ok = await PlatformService.installService();
        if (!ok) {
          GuiLogger().error(
            'AppState._runInitialDetection: service install FAILED',
          );
          _lastError = 'Failed to install backend service';
          notifyListeners();
          return;
        }
        _serviceState = 'installed';
        notifyListeners();
        await Future.delayed(const Duration(seconds: 1));
      }

      // 4. Start service and wait for it
      GuiLogger().conn('AppState._runInitialDetection: starting service...');
      await PlatformService.startService();
      for (var i = 0; i < 20; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
        if (await _backendMgr.checkHealth()) {
          GuiLogger().conn('AppState._runInitialDetection: service healthy!');
          _serviceState = 'running';
          _applyAliveState(true);
          return;
        }
      }
      GuiLogger().error('AppState._runInitialDetection: service start FAILED');
      _lastError = 'Backend service failed to start';
      notifyListeners();
    } finally {
      _backendBusy = false;
      _serviceBusy = false;
      notifyListeners();
    }
  }

  void _onBackendAliveChanged(bool alive) {
    GuiLogger().conn(
      'AppState._onBackendAliveChanged: alive=$alive (prev online=$_backendOnline running=$_backendRunning)',
    );
    _applyAliveState(alive);
  }

  /// Apply alive/dead state – called both from initial detection & watcher.
  void _applyAliveState(bool alive) {
    if (_backendOnline == alive && _backendRunning == alive) {
      return; // no change
    }

    GuiLogger().conn('AppState._applyAliveState: $alive → updating state');
    _backendOnline = alive;
    _backendRunning = alive;

    if (alive) {
      // Backend appeared — connect & load
      _connectAndLoad();
      _startPlaybackPolling();
    } else {
      // Backend disappeared — clean up
      GuiLogger().conn('AppState: backend gone, cleaning up');
      ws.disconnect();
      _modules.clear();
      _moduleUiTree = null;
      _activeModuleId = null;
      _stopPlaybackPolling();
      _playbackInstances = [];
      _activeInstanceId = null;
      _activeQueue = [];
      _activeHistory = [];
      _activePlaylist = [];
      _playlistSources = [];
    }
    notifyListeners();
  }

  Future<void> _connectAndLoad() async {
    GuiLogger().conn('AppState._connectAndLoad: starting...');
    try {
      // Re-discover: port file → default port → socket
      final discovered = await _backendMgr.discover();
      if (discovered == null) {
        GuiLogger().conn('AppState._connectAndLoad: discovery failed');
        return;
      }
      // Dispose old clients before recreating
      ws.disconnect();
      api.dispose();
      _createClients(17890);
      _setupWs();

      GuiLogger().conn('AppState._connectAndLoad: begin health checks...');
      // Give the backend HTTP server a moment to be ready
      for (var i = 0; i < 10; i++) {
        final ok = await _backendMgr.checkHealth();
        GuiLogger().conn('  health check attempt ${i + 1}: $ok');
        if (ok) break;
        await Future.delayed(const Duration(milliseconds: 300));
      }

      GuiLogger().conn(
        'AppState._connectAndLoad: health checks done, connecting WS...',
      );
      final connected = await ws.connectOnce();
      GuiLogger().conn('AppState._connectAndLoad: WS connected=$connected');
      if (connected) {
        await api.connectController();
        await _loadModules();
        await refreshPlayback();
      }
    } catch (e, st) {
      GuiLogger().error('AppState._connectAndLoad CRASHED', e, st);
    }
  }

  void _setupWs() {
    ws.onEvent = (event) {
      if (event.type == 'backend.state.changed') {
        final data = event.data is Map<String, dynamic>
            ? event.data as Map<String, dynamic>
            : const <String, dynamic>{};
        final running = data['running'] == true;
        if (_backendRunning != running) {
          _backendRunning = running;
          _backendOnline = running;
          notifyListeners();
        }
      } else if (event.type == 'error') {
        final data = event.data is Map<String, dynamic>
            ? event.data as Map<String, dynamic>
            : const <String, dynamic>{};
        final msg = data['message'] as String? ?? 'Unknown error';
        _lastError = msg;
        notifyListeners();
      } else if (event.type == 'instances.changed' ||
          event.type == 'track.changed' ||
          event.type == 'state.changed' ||
          event.type == 'queue.changed' ||
          event.type == 'playlist.updated') {
        refreshPlayback();
      } else if (event.type == 'position') {
        _applyPositionEvent(event.data);
      }
    };
    ws.onUiPush = (push) {
      if (push.replace && push.tree != null) {
        // 只处理当前活跃模块的 UI 推送, 避免后台模块的推送覆盖当前显示
        if (push.moduleId.isNotEmpty && push.moduleId != _activeModuleId) {
          return;
        }
        if (_overlayUiTree != null) {
          _overlayUiTree = push.tree;
        } else {
          _moduleUiTree = push.tree;
        }
        notifyListeners();
      }
    };
    ws.onDisconnected = () {
      // WS dropped – mark offline until detection picks it up again
      if (_backendOnline) {
        _backendOnline = false;
        _backendRunning = false;
        notifyListeners();
      }
    };
  }

  Future<void> _loadModules() async {
    _modulesLoading = true;
    notifyListeners();
    try {
      _modules = await api.getModules();
    } catch (_) {}
    _modulesLoading = false;
    notifyListeners();
  }

  // ──────────────────────────────────────────────
  //  Backend actions — pure service mode
  // ──────────────────────────────────────────────

  Future<void> startBackend() async {
    if (_backendBusy) return;
    _backendBusy = true;
    notifyListeners();
    try {
      GuiLogger().conn('startBackend: pure service mode');

      // Kill stray process (safety)
      await _backendMgr.forceKillProcess();
      await Future.delayed(const Duration(seconds: 1));

      // Refresh service state
      _serviceState = await PlatformService.getServiceState();
      GuiLogger().conn('startBackend: serviceState=$_serviceState');

      // If service says running but unreachable, restart it
      if (_serviceState == 'running') {
        final healthy = await _backendMgr.checkHealth();
        if (healthy) {
          _applyAliveState(true);
          return;
        }
        await PlatformService.stopService();
        await Future.delayed(const Duration(seconds: 2));
      }

      // Auto-install if missing
      if (_serviceState != 'installed' && _serviceState != 'running') {
        GuiLogger().conn('startBackend: auto-installing service...');
        final ok = await PlatformService.installService();
        if (!ok) {
          GuiLogger().error('startBackend: service install FAILED');
          _lastError = 'Failed to install backend service';
          notifyListeners();
          return;
        }
        _serviceState = 'installed';
        await Future.delayed(const Duration(seconds: 1));
      }

      // Start service
      GuiLogger().conn('startBackend: starting service...');
      await PlatformService.startService();
      for (var i = 0; i < 20; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
        if (await _backendMgr.checkHealth()) {
          GuiLogger().conn('startBackend: service healthy');
          _serviceState = 'running';
          _applyAliveState(true);
          return;
        }
      }
      GuiLogger().error('startBackend: service start FAILED');
      _lastError = 'Backend service failed to start';
      notifyListeners();
    } finally {
      _backendBusy = false;
      notifyListeners();
    }
  }

  Future<void> stopBackend() async {
    if (_backendBusy) return;
    _backendBusy = true;
    notifyListeners();
    try {
      GuiLogger().conn('stopBackend: stopping...');

      // Graceful API stop
      try {
        await api.stopBackend();
      } catch (_) {}

      // Stop service
      _serviceState = await PlatformService.getServiceState();
      if (_serviceState == 'running') {
        GuiLogger().conn('stopBackend: stopping service...');
        await PlatformService.stopService();
      }

      // Safety: force kill any lingering process
      try {
        await _backendMgr.forceKillProcess();
      } catch (_) {}

      _serviceState = 'installed';
      _backendRunning = false;
      _backendOnline = false;
      _modules.clear();
      _moduleUiTree = null;
      _activeModuleId = null;
      notifyListeners();
    } finally {
      _backendBusy = false;
      notifyListeners();
    }
  }

  /// Toggle: start if stopped, stop if running
  Future<void> toggleBackend() async {
    if (_backendBusy) return;
    if (_backendRunning) {
      await stopBackend();
    } else {
      await startBackend();
    }
  }

  /// Restart backend: stop → start (prefer service)
  Future<void> restartBackend() async {
    if (_backendBusy) return;
    _backendBusy = true;
    notifyListeners();
    try {
      await stopBackend();
      await Future.delayed(const Duration(seconds: 1));
      await startBackend();
    } finally {
      _backendBusy = false;
      notifyListeners();
    }
  }

  /// Save current config to backend config file, then restart
  Future<void> saveAndRestart() async {
    if (_backendBusy) return;
    _backendBusy = true;
    notifyListeners();
    try {
      await api.putConfig(
        AppConfig(
          backendPort: '$_backendPort',
          backendBind: _backendBind,
          autostart: _autostart,
          minimizeToTray: _minimizeToTray,
          theme: _themeMode.name,
          language: _language,
        ),
      );
      await api.saveConfig();
      await stopBackend();
      await Future.delayed(const Duration(seconds: 1));
      await startBackend();
    } finally {
      _backendBusy = false;
      notifyListeners();
    }
  }

  /// Reset config to defaults: delete backend config file and restart
  Future<void> resetToDefaults() async {
    _backendPort = 17890;
    _backendBind = '127.0.0.1';
    _autostart = false;
    _minimizeToTray = true;
    _themeMode = AppThemeMode.system;
    _language = 'system';
    notifyListeners();
    await saveAndRestart();
  }

  /// Install backend as system service.
  Future<String> installService() async {
    _serviceBusy = true;
    _backendBusy = true;
    _serviceResult = null;
    notifyListeners();
    try {
      final ok = await PlatformService.installService();
      if (ok) {
        _serviceState = 'installed';
        _serviceAutoStart = await PlatformService.isServiceAutoStart();
        _serviceResult = 'installed';
      } else {
        _serviceState = await PlatformService.getServiceState();
        _serviceResult = 'failed';
      }
    } catch (e) {
      _serviceResult = 'failed';
      GuiLogger().error('installService failed', e);
    }
    _serviceBusy = false;
    _backendBusy = false;
    notifyListeners();
    _clearServiceResultAfterDelay();
    return _serviceResult!;
  }

  /// Uninstall backend system service.
  Future<String> uninstallService() async {
    _serviceBusy = true;
    _backendBusy = true;
    _serviceResult = null;
    notifyListeners();
    try {
      final ok = await PlatformService.uninstallService();
      if (ok) {
        _serviceState = 'not_installed';
        _serviceAutoStart = false; // Reset when uninstalled
        _serviceResult = 'not_installed';
      } else {
        _serviceState = await PlatformService.getServiceState();
        _serviceResult = 'failed';
      }
    } catch (e) {
      _serviceResult = 'failed';
      GuiLogger().error('uninstallService failed', e);
    }
    _serviceBusy = false;
    _backendBusy = false;
    notifyListeners();
    _clearServiceResultAfterDelay();
    return _serviceResult!;
  }

  void _clearServiceResultAfterDelay() {
    _serviceResultTimer?.cancel();
    _serviceResultTimer = Timer(const Duration(seconds: 5), () {
      _serviceResult = null;
      notifyListeners();
    });
  }

  /// Refresh service state from OS.
  Future<void> refreshServiceState() async {
    _serviceState = await PlatformService.getServiceState();
    _serviceAutoStart = await PlatformService.isServiceAutoStart();
    notifyListeners();
  }

  /// Toggle service startup type between automatic and manual.
  Future<bool> setServiceAutoStart(bool autoStart) async {
    _serviceBusy = true;
    notifyListeners();
    try {
      final ok = await PlatformService.setServiceAutoStart(autoStart);
      if (ok) {
        _serviceAutoStart = autoStart;
      }
      return ok;
    } catch (e) {
      GuiLogger().error('setServiceAutoStart failed', e);
      return false;
    } finally {
      _serviceBusy = false;
      notifyListeners();
    }
  }

  Future<void> setBackendPort(String port) async {
    _backendPort = int.tryParse(port) ?? 17890;
    notifyListeners();
    await api.putConfig(AppConfig(backendPort: port));
  }

  Future<void> setBackendBind(String bind) async {
    _backendBind = bind;
    notifyListeners();
    await api.putConfig(AppConfig(backendBind: bind));
  }

  Future<void> setAutostart(bool v) async {
    _autostart = v;
    notifyListeners();
    await PlatformService.setGuiAutostart(v);
    // Auto-save to backend config
    try {
      await api.putConfig(AppConfig(autostart: v));
      await api.saveConfig();
    } catch (_) {}
  }

  Future<void> setMinimizeToTray(bool v) async {
    _minimizeToTray = v;
    notifyListeners();
    // Auto-save to backend config
    try {
      await api.putConfig(AppConfig(minimizeToTray: v));
      await api.saveConfig();
    } catch (_) {}
  }

  /// Full quit: stop backend, dispose tray, exit app.
  Future<void> fullQuit() async {
    _backendMgr.stopWatching();
    await stopBackend();
  }

  // ── Theme / Language ──
  void setThemeMode(AppThemeMode mode) {
    _themeMode = mode;
    notifyListeners();
    // Auto-save
    try {
      api.putConfig(AppConfig(theme: mode.name));
      api.saveConfig();
    } catch (_) {}
  }

  void setLanguage(String lang) {
    _language = lang;
    notifyListeners();
    // Auto-save
    try {
      api.putConfig(AppConfig(language: lang));
      api.saveConfig();
    } catch (_) {}
  }

  Future<void> saveAllConfig() => api.saveConfig();

  // ── Tab ──
  void selectTab(int tab) {
    _currentTab = tab;
    _overlayUiTree = null;
    _overlayMode = '';
    _overlayTitle = '';
    notifyListeners();
    // Refresh modules when switching to modules tab (tab 3)
    if (tab == 3 && _backendOnline) {
      _loadModules();
    }
  }

  // ── Module navigation ──
  Future<void> refreshModules() async {
    if (_backendOnline) {
      await _loadModules();
    }
  }

  Future<void> refreshPlayback() async {
    if (!_backendOnline) return;
    if (_playbackLoading) return;
    _playbackLoading = true;
    try {
      final instances = await api.getInstances();
      var nextActiveId = _activeInstanceId;
      if (nextActiveId == null || !instances.any((i) => i.id == nextActiveId)) {
        final attached = instances.where((i) => i.attached).toList();
        final serverManaged = attached.where((i) => i.isServerManaged).toList();
        nextActiveId = (serverManaged.isNotEmpty
                ? serverManaged.first
                : attached.isNotEmpty
                ? attached.first
                : instances.isNotEmpty
                ? instances.first
                : null)
            ?.id;
      }

      List<QueueItemInfo> queue = [];
      List<QueueItemInfo> history = [];
      List<QueueItemInfo> playlist = [];
      List<PlaylistSourceInfo> sources = [];
      if (nextActiveId != null && nextActiveId.isNotEmpty) {
        queue = await api.getInstanceQueue(nextActiveId);
        history = await api.getInstanceHistory(nextActiveId);
        playlist = await api.getInstancePlaylist(nextActiveId);
        sources = await api.getPlaylistSources(nextActiveId);
      }

      _playbackInstances = instances;
      _activeInstanceId = nextActiveId;
      _activeQueue = queue;
      _activeHistory = history;
      _activePlaylist = playlist;
      _playlistSources = sources;
      notifyListeners();
    } catch (e) {
      GuiLogger().error('refreshPlayback failed', e);
    } finally {
      _playbackLoading = false;
    }
  }

  void selectPlaybackInstance(String id) {
    _activeInstanceId = id;
    notifyListeners();
    refreshPlayback();
  }

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

  Future<void> playSongOnActive(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.play(instance.id, uuid: uuid);
    await refreshPlayback();
  }

  Future<void> addSongToActiveQueue(String uuid) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.addToQueue(instance.id, uuid);
    await refreshPlayback();
  }

  Future<void> removeQueueItem(int index) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.removeQueueAt(instance.id, index);
    await refreshPlayback();
  }

  Future<void> clearActiveQueue() async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.clearQueue(instance.id);
    await refreshPlayback();
  }

  Future<void> removeHistoryItem(int index) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.removeHistoryAt(instance.id, index);
    await refreshPlayback();
  }

  Future<void> clearActiveHistory() async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.clearHistory(instance.id);
    await refreshPlayback();
  }

  /// Replace all playlist sources with the given list. Empty list = reset to "全部".
  Future<void> replacePlaylistSources(
    String instanceId, {
    required List<Map<String, dynamic>> sources,
  }) async {
    await api.replacePlaylistSources(instanceId, sources: sources);
    await refreshPlayback();
  }

  /// Replace all playlist sources with a single tag source.
  Future<void> addTagToActivePlaylist(TagInfo tag) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    final songs = await api.getSongs(tagId: tag.id);
    await api.replacePlaylistSources(
      instance.id,
      sources: [
        {
          'id': 'tag_${tag.id}',
          'name': tag.name,
          'uuids': songs.map((s) => s.uuid).toList(),
        },
      ],
    );
    await refreshPlayback();
  }

  /// Replace all playlist sources with a single album source.
  Future<void> addAlbumToActivePlaylist(AlbumInfo album) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    final songs = await api.getSongs(albumId: album.id);
    await api.replacePlaylistSources(
      instance.id,
      sources: [
        {
          'id': 'album_${album.id}',
          'name': album.name,
          'uuids': songs.map((s) => s.uuid).toList(),
        },
      ],
    );
    await refreshPlayback();
  }

  Future<void> removePlaylistSource(String sourceId) async {
    final instance = activeInstance;
    if (instance == null || !instance.isServerManaged) return;
    await api.removePlaylistSource(instance.id, sourceId);
    await refreshPlayback();
  }

  void _startPlaybackPolling() {
    _playbackPollTimer?.cancel();
    _playbackPollTimer = Timer.periodic(const Duration(seconds: 2), (_) {
      refreshPlayback();
    });
  }

  void _stopPlaybackPolling() {
    _playbackPollTimer?.cancel();
    _playbackPollTimer = null;
  }

  void _applyPositionEvent(dynamic data) {
    if (data is! Map<String, dynamic>) return;
    final instanceId = data['instanceId'] as String?;
    final position = (data['position'] ?? 0.0).toDouble();
    var changed = false;
    _playbackInstances = _playbackInstances.map((instance) {
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
        queueCount: instance.queueCount,
        queueIndex: instance.queueIndex,
        historyCount: instance.historyCount,
        sampleRate: instance.sampleRate,
        channels: instance.channels,
        currentTrack: instance.currentTrack,
      );
    }).toList();
    if (changed) notifyListeners();
  }

  Future<void> openModule(String moduleId) async {
    _activeModuleId = moduleId;
    _moduleUiTree = null;
    notifyListeners();
    try {
      _moduleUiTree = await api.getModuleUi(moduleId);
      _logImageNodes(_moduleUiTree, moduleId);
      notifyListeners();
    } catch (e) {
      GuiLogger().error('openModule failed: moduleId=$moduleId', e);
    }
  }

  void _logImageNodes(RawNodeData? node, String context) {
    if (node == null) return;
    if (node.nodeType == 'Image') {
      GuiLogger().info(
        'UI Tree Image: id=${node.id} source="${node.source}" '
        'width=${node.imageWidth} height=${node.imageHeight} '
        'context=$context baseUrl=$apiBaseUrl',
      );
    }
    for (final child in node.children) {
      _logImageNodes(child, context);
    }
    for (final item in node.items) {
      _logImageNodes(item, context);
    }
  }

  void closeModule() {
    _activeModuleId = null;
    _moduleUiTree = null;
    notifyListeners();
  }

  Future<void> openModuleLink(String moduleId, String linkId) async {
    try {
      final tree = await api.getModuleLinkUi(moduleId, linkId);
      _overlayUiTree = tree;
      _overlayMode = 'ui';
      _overlayTitle = '$moduleId / $linkId';
      _activeModuleId = moduleId;
      notifyListeners();
    } catch (_) {}
  }

  Future<void> openModuleSettings(String moduleId) async {
    try {
      final tree = await api.getModuleSettingsUi(moduleId);
      _overlayUiTree = tree;
      _overlayMode = 'ui';
      _overlayTitle = '$moduleId / Settings';
      _activeModuleId = moduleId;
      notifyListeners();
    } catch (_) {}
  }

  void openAbout() {
    _overlayUiTree = null;
    _overlayMode = 'about';
    _overlayTitle = 'About';
    notifyListeners();
  }

  void closeOverlay() {
    _overlayUiTree = null;
    _overlayMode = '';
    _overlayTitle = '';
    if (_activeModuleId != null && _moduleUiTree == null) {
      _activeModuleId = null;
    }
    notifyListeners();
  }

  // ── UI dispatch ──
  void dispatchUiEvent(String nodeId, String action, String value) {
    if (!ws.isConnected) {
      GuiLogger().warn(
        'dispatchUiEvent: ws not connected, dropped $nodeId/$action',
      );
      return;
    }
    ws.sendUiEvent(_activeModuleId ?? '', nodeId, action, value);
  }

  // ═══════════════════════════════════════════════════════════
  //  Game Integration (Mod Manager) Methods
  // ═══════════════════════════════════════════════════════════

  void addDeploymentLog(String log) {
    final timeStr = DateTime.now().toLocal().toString().substring(11, 19);
    _deploymentLogs.add('[$timeStr] $log');
    notifyListeners();
  }

  void clearDeploymentLogs() {
    _deploymentLogs.clear();
    notifyListeners();
  }

  Future<void> loadGameIntegrationSettings() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      for (final game in gameCatalog) {
        _gamePaths[game.id] =
            prefs.getString('game_integration_path_${game.id}') ??
            (game.id == 'chill_with_you'
                ? prefs.getString('game_integration_path')
                : null) ??
            '';
      }
      refreshModStatuses();
    } catch (e) {
      GuiLogger().error('loadGameIntegrationSettings failed', e);
    }
  }

  Future<void> setGamePath(
    String path, {
    String gameId = 'chill_with_you',
  }) async {
    _gamePaths[gameId] = path;
    notifyListeners();
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('game_integration_path_$gameId', path);
      if (gameId == 'chill_with_you') {
        await prefs.setString('game_integration_path', path);
      }
      refreshModStatuses(gameId);
    } catch (e) {
      GuiLogger().error('setGamePath failed', e);
    }
  }

  void refreshModStatuses([String? gameId]) {
    final games = gameId == null
        ? gameCatalog
        : gameCatalog.where((g) => g.id == gameId);

    for (final game in games) {
      final path = gamePathFor(game.id);
      if (path.isEmpty ||
          !ModDeploymentService.verifyGameDirectory(path, game)) {
        _bepinexStatuses[game.id] = BepInExStatus.notInstalled;
        _modStatuses[game.id] = ModStatus.notInstalled;
        continue;
      }

      _bepinexStatuses[game.id] = game.supportedFrameworks.contains('bepinex_5')
          ? ModDeploymentService.checkBepInExStatus(path)
          : BepInExStatus.notInstalled;

      if (game.supportedMods.isEmpty) {
        _modStatuses[game.id] = ModStatus.notInstalled;
        continue;
      }

      final mod = modCatalog.firstWhere(
        (m) => m.id == game.supportedMods.first,
      );
      _modStatuses[game.id] = mod.installsToGameRoot
          ? ModDeploymentService.checkRootModStatus(path, mod)
          : ModDeploymentService.checkModStatus(path, mod.folderName);
    }
    notifyListeners();
  }

  Future<bool> installBepInEx({String gameId = 'chill_with_you'}) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final framework = frameworkCatalog.firstWhere((f) => f.id == 'bepinex_5');
      final success = await ModDeploymentService.deployBepInEx(
        path,
        framework,
        addDeploymentLog,
      );
      refreshModStatuses(gameId);
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> uninstallBepInEx({String gameId = 'chill_with_you'}) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final framework = frameworkCatalog.firstWhere((f) => f.id == 'bepinex_5');
      final success = await ModDeploymentService.undeployBepInEx(
        path,
        framework,
        addDeploymentLog,
      );
      refreshModStatuses(gameId);
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> installMod({String gameId = 'chill_with_you'}) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final game = gameCatalog.firstWhere((g) => g.id == gameId);
      final mod = modCatalog.firstWhere(
        (m) => m.id == game.supportedMods.first,
      );
      final success = await ModDeploymentService.deployMod(
        path,
        mod,
        addDeploymentLog,
      );
      refreshModStatuses(gameId);
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> uninstallMod({String gameId = 'chill_with_you'}) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final game = gameCatalog.firstWhere((g) => g.id == gameId);
      final mod = modCatalog.firstWhere(
        (m) => m.id == game.supportedMods.first,
      );
      final success = await ModDeploymentService.undeployMod(
        path,
        mod,
        addDeploymentLog,
      );
      refreshModStatuses(gameId);
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _serviceResultTimer?.cancel();
    _stopPlaybackPolling();
    _backendMgr.dispose();
    ws.disconnect();
    api.dispose();
    super.dispose();
  }
}
