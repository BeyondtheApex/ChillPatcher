import 'dart:async';
import 'dart:convert';
import 'dart:io';
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
  String _closeBehavior = 'exit'; // 'minimize' or 'exit'
  String _serviceState =
      'unknown'; // 'running', 'installed', 'not_installed', 'unknown'

  // Instance management
  List<InstalledInstance> _instances = [];
  List<ArchiveEntry> _archives = [];
  Set<String> _backendInstanceIds =
      {}; // Live backend instance IDs for online check

  List<InstalledInstance> get instances => _instances;
  List<ArchiveEntry> get archives => _archives;

  /// Check if an instance is currently online (connected to backend).
  bool isInstanceOnline(String instanceId) =>
      _backendInstanceIds.contains(instanceId);

  /// Select an instance for context. If null, switches to global (no instance).
  /// For server-mode instances, loads the instance profile.
  /// For online instances, also selects the playback instance on the backend.
  /// Auto-saves the previously selected instance before switching.
  void selectInstance(String? instanceId) {
    // Save previous instance before switching
    final prev = _activeInstanceId;
    if (prev != null && prev != instanceId) {
      saveInstanceProfile(prev);
    }
    _activeInstanceId = instanceId;
    if (instanceId != null && _backendInstanceIds.contains(instanceId)) {
      refreshPlayback();
    } else if (instanceId != null) {
      // Offline: try loading profile from file
      _loadOfflineProfile(instanceId);
    }
    notifyListeners();
  }

  /// Compute the file path for an instance's playback_state.json.
  String? _instanceProfilePath(String instanceId) {
    final base = _backendMgr.backendBaseDir;
    if (base == null) return null;
    return '$base${Platform.pathSeparator}config${Platform.pathSeparator}instances${Platform.pathSeparator}$instanceId${Platform.pathSeparator}playback_state.json';
  }

  /// Load instance profile directly from file (offline fallback).
  void _loadOfflineProfile(String instanceId) {
    final path = _instanceProfilePath(instanceId);
    if (path == null) return;
    try {
      final f = File(path);
      if (!f.existsSync()) return;
      final json = jsonDecode(f.readAsStringSync()) as Map<String, dynamic>;
      _activeQueue = _parseQueueItems(json, 'SongUuids');
      _activeHistory = _parseQueueItems(json, 'HistoryUuids');
      _playlistSources = _parsePlaylistSources(json);
      // Rebuild _activePlaylist from playlist sources' persisted UUIDs
      _activePlaylist = _buildPlaylistFromSources(_playlistSources);
      notifyListeners();
    } catch (e) {
      GuiLogger().error('_loadOfflineProfile failed', e);
    }
  }

  /// Build _activePlaylist entries from playlist sources' UUIDs.
  List<QueueItemInfo> _buildPlaylistFromSources(
    List<PlaylistSourceInfo> sources,
  ) {
    final seen = <String>{};
    final result = <QueueItemInfo>[];
    for (final src in sources) {
      for (final uuid in src.uuids) {
        if (seen.add(uuid)) {
          result.add(QueueItemInfo(uuid: uuid, title: uuid));
        }
      }
    }
    return result;
  }

  List<QueueItemInfo> _parseQueueItems(Map<String, dynamic> json, String key) {
    final queues = json['Queues'] as List?;
    if (queues == null || queues.isEmpty) return [];
    final activeId = json['ActiveQueueId'] as String? ?? '';
    final active = activeId.isNotEmpty
        ? (queues.cast<Map<String, dynamic>>().firstWhere(
            (q) => q['Id'] == activeId,
            orElse: () => queues.first as Map<String, dynamic>,
          ))
        : queues.first as Map<String, dynamic>;
    final uuids = (active[key] as List?)?.cast<String>() ?? [];
    return uuids.map((u) => QueueItemInfo(uuid: u, title: u)).toList();
  }

  List<PlaylistSourceInfo> _parsePlaylistSources(Map<String, dynamic> json) {
    final queues = json['Queues'] as List?;
    if (queues == null || queues.isEmpty) return [];
    final activeId = json['ActiveQueueId'] as String? ?? '';
    final active = activeId.isNotEmpty
        ? (queues.cast<Map<String, dynamic>>().firstWhere(
            (q) => q['Id'] == activeId,
            orElse: () => queues.first as Map<String, dynamic>,
          ))
        : queues.first as Map<String, dynamic>;
    final sources = active['PlaylistSources'] as List? ?? [];
    return sources.map((s) {
      final m = s as Map<String, dynamic>;
      final uuids = (m['SongUuids'] as List?)?.cast<String>() ?? <String>[];
      return PlaylistSourceInfo(
        id: m['Id'] ?? '',
        name: m['Name'] ?? '',
        songCount: uuids.length,
        uuids: uuids,
      );
    }).toList();
  }

  /// Save instance profile — writes current queue/history/sources.
  Future<void> saveInstanceProfile(String instanceId) async {
    final data = _buildProfileJson();
    if (_backendOnline) {
      try {
        await api.updateInstanceProfile(instanceId, data);
      } catch (e) {
        GuiLogger().error('saveInstanceProfile API failed', e);
      }
    } else {
      final path = _instanceProfilePath(instanceId);
      if (path == null) return;
      try {
        final dir = File(path).parent;
        if (!dir.existsSync()) dir.createSync(recursive: true);
        File(path).writeAsStringSync(jsonEncode(data));
      } catch (e) {
        GuiLogger().error('saveInstanceProfile file failed', e);
      }
    }
  }

  Map<String, dynamic> _buildProfileJson() {
    return {
      'ActiveQueueId': 'default',
      'Volume': 1.0,
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
          'Shuffle': false,
          'RepeatMode': 'none',
        },
      ],
    };
  }

  bool _serviceBusy = false;
  bool _serviceAutoStart = false;
  String? _serviceResult; // Result message after install/uninstall
  Timer? _serviceResultTimer;

  // Appearance
  AppThemeMode _themeMode = AppThemeMode.system;
  int _seedColor = 0xFF673AB7; // deepPurple
  bool _useSystemColor = true;
  String _language = 'system';

  // Modules
  List<ModuleInfoResponse> _modules = [];
  bool _modulesLoading = false;
  String? _activeModuleId;
  String _activeUiKind = 'default'; // 'default', 'link', 'settings'
  String _activeLinkId = '';
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
  String get closeBehavior => _closeBehavior;
  String get serviceState => _serviceState;
  bool get serviceBusy => _serviceBusy;
  bool get serviceAutoStart => _serviceAutoStart;
  String? get serviceResult => _serviceResult;
  AppThemeMode get themeMode => _themeMode;
  int get seedColor => _seedColor;
  bool get useSystemColor => _useSystemColor;
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
  int get attachedAudioClientCount =>
      _playbackInstances.where((i) => i.attached).length;

  PlaybackInstanceInfo? get activeInstance {
    final id = _activeInstanceId;
    if (id == null)
      return _playbackInstances.isNotEmpty ? _playbackInstances.first : null;
    for (final instance in _playbackInstances) {
      if (instance.id == id) return instance;
    }
    return _playbackInstances.isNotEmpty ? _playbackInstances.first : null;
  }

  bool get canControlActiveInstance {
    // Online server instance (active or any online)
    if (activeInstance?.isServerManaged == true) return true;
    // Any online server instance available (even before refreshPlayback completes)
    if (_backendOnline && _playbackInstances.any((i) => i.isServerManaged))
      return true;
    // Offline server instance: allow editing profile when backend is running
    if (_activeInstanceId != null && _backendOnline) {
      final inst = _instances
          .where((i) => i.instanceId == _activeInstanceId)
          .firstOrNull;
      if (inst != null && inst.isServerMode) return true;
    }
    // Backend online and server instances exist in local storage (even if none active yet)
    if (_backendOnline && _instances.any((i) => i.isServerMode)) return true;
    return false;
  }

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

    // Load UI preferences from SharedPreferences
    _loadUiPrefs();

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
      _backendInstanceIds.clear();
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
        // Auto-select server instance from local storage if none is active yet.
        // This ensures offline server instances show controls on startup.
        if (_activeInstanceId == null) {
          final serverInst = _instances
              .where((i) => i.isServerMode)
              .firstOrNull;
          if (serverInst != null) {
            _activeInstanceId = serverInst.instanceId;
            _loadOfflineProfile(serverInst.instanceId);
            notifyListeners();
          }
        }
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
          event.type == 'playlist.updated' ||
          event.type == 'exclude.changed') {
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
          seedColor: _seedColor,
          useSystemColor: _useSystemColor,
          closeBehavior: _closeBehavior,
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
    _seedColor = 0xFF673AB7;
    _useSystemColor = true;
    _closeBehavior = 'exit';
    _saveUiPrefs();
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

  void setSeedColor(int color) {
    _seedColor = color;
    _useSystemColor = false;
    _saveUiPrefs();
    notifyListeners();
  }

  void setUseSystemColor(bool v) {
    _useSystemColor = v;
    _saveUiPrefs();
    notifyListeners();
  }

  void setCloseBehavior(String v) {
    if (v != 'minimize' && v != 'exit') return;
    _closeBehavior = v;
    _saveUiPrefs();
    notifyListeners();
  }

  void _saveUiPrefs() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setInt('ui_seed_color', _seedColor);
      await prefs.setBool('ui_use_system_color', _useSystemColor);
      await prefs.setString('ui_close_behavior', _closeBehavior);
    } catch (_) {}
  }

  void _loadUiPrefs() {
    try {
      SharedPreferences.getInstance().then((prefs) {
        _seedColor = prefs.getInt('ui_seed_color') ?? 0xFF673AB7;
        _useSystemColor = prefs.getBool('ui_use_system_color') ?? true;
        _closeBehavior = prefs.getString('ui_close_behavior') ?? 'exit';
        notifyListeners();
      });
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

  Future<void> setModuleEnabled(String moduleId, bool enabled) async {
    try {
      await api.setModuleEnabled(moduleId, enabled);
      // Update local state immediately
      for (final m in _modules) {
        if (m.id == moduleId) {
          final idx = _modules.indexOf(m);
          _modules[idx] = ModuleInfoResponse(
            id: m.id,
            name: m.name,
            version: m.version,
            priority: m.priority,
            loadedAt: m.loadedAt,
            enabled: enabled,
            hasSettingsUi: m.hasSettingsUi,
            hasQuickLinks: m.hasQuickLinks,
            linkEntries: m.linkEntries,
          );
          break;
        }
      }
      notifyListeners();
    } catch (e) {
      GuiLogger().error('setModuleEnabled failed', e);
    }
  }

  Future<void> refreshPlayback() async {
    if (!_backendOnline) return;
    if (_playbackLoading) return;
    _playbackLoading = true;
    try {
      final instances = await api.getInstances();

      // Keep user-selected instance; only auto-select for display if null
      var nextActiveId = _activeInstanceId;
      if (nextActiveId == null || !instances.any((i) => i.id == nextActiveId)) {
        final attached = instances.where((i) => i.attached).toList();
        final serverManaged = attached.where((i) => i.isServerManaged).toList();
        nextActiveId =
            (serverManaged.isNotEmpty
                    ? serverManaged.first
                    : attached.isNotEmpty
                    ? attached.first
                    : instances.isNotEmpty
                    ? instances.first
                    : null)
                ?.id;
        if (_activeInstanceId == null) _activeInstanceId = nextActiveId;
      }

      // Sync backend online instance IDs for dropdown status
      _backendInstanceIds = instances.map((i) => i.id).toSet();

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
      // Only overwrite queue/history if the loaded data matches the user's selection
      if (_activeInstanceId == null || _activeInstanceId == nextActiveId) {
        _activeQueue = queue;
        _activeHistory = history;
        _activePlaylist = playlist;
        _playlistSources = sources;
      }
      notifyListeners();
    } catch (e) {
      GuiLogger().error('refreshPlayback failed', e);
    } finally {
      _playbackLoading = false;
    }
  }

  void selectPlaybackInstance(String id) {
    selectInstance(id);
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
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    _activeQueue.add(QueueItemInfo(uuid: uuid, title: uuid));
    notifyListeners();
    if (_backendInstanceIds.contains(instanceId)) {
      await api.addToQueue(instanceId, uuid);
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
  }

  Future<void> removeQueueItem(int index) async {
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    if (index >= 0 && index < _activeQueue.length) {
      _activeQueue.removeAt(index);
      notifyListeners();
    }
    if (_backendInstanceIds.contains(instanceId)) {
      await api.removeQueueAt(instanceId, index);
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
  }

  Future<void> clearActiveQueue() async {
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    _activeQueue.clear();
    notifyListeners();
    if (_backendInstanceIds.contains(instanceId)) {
      await api.clearQueue(instanceId);
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
  }

  Future<void> clearActiveHistory() async {
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    _activeHistory.clear();
    notifyListeners();
    if (_backendInstanceIds.contains(instanceId)) {
      await api.clearHistory(instanceId);
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
  }

  Future<void> moveQueueItem(int from, int to) async {
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    if (from >= 0 &&
        from < _activeQueue.length &&
        to >= 0 &&
        to < _activeQueue.length) {
      final item = _activeQueue.removeAt(from);
      _activeQueue.insert(to, item);
      notifyListeners();
    }
    if (_backendInstanceIds.contains(instanceId)) {
      await api.moveQueue(instanceId, from, to);
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
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
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    final songs = await api.getSongs(tagId: tag.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _addSourceInMemory('tag_${tag.id}', tag.name, uuids);
    // Also populate _activePlaylist so the library panel shows songs
    _mergeSongsToPlaylist(songs);
    if (_backendInstanceIds.contains(instanceId)) {
      try {
        await api.removePlaylistSource(instanceId, 'tag_${tag.id}');
      } catch (_) {}
      await api.insertPlaylistSource(
        instanceId,
        id: 'tag_${tag.id}',
        name: tag.name,
        uuids: uuids,
        index: _playlistSources.where((s) => s.id != 'all').length,
      );
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
  }

  /// Replace all playlist sources with a single album source.
  Future<void> addAlbumToActivePlaylist(AlbumInfo album) async {
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    final songs = await api.getSongs(albumId: album.id);
    final uuids = songs.map((s) => s.uuid).toList();
    _addSourceInMemory('album_${album.id}', album.name, uuids);
    // Also populate _activePlaylist so the library panel shows songs
    _mergeSongsToPlaylist(songs);
    if (_backendInstanceIds.contains(instanceId)) {
      try {
        await api.removePlaylistSource(instanceId, 'album_${album.id}');
      } catch (_) {}
      await api.insertPlaylistSource(
        instanceId,
        id: 'album_${album.id}',
        name: album.name,
        uuids: uuids,
        index: _playlistSources.where((s) => s.id != 'all').length,
      );
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
  }

  Future<void> removePlaylistSource(String sourceId) async {
    final instanceId = _resolveWriteTarget();
    if (instanceId == null) return;
    _playlistSources.removeWhere((s) => s.id == sourceId);
    // Rebuild _activePlaylist from remaining sources
    _activePlaylist = _buildPlaylistFromSources(_playlistSources);
    if (_backendInstanceIds.contains(instanceId)) {
      await api.removePlaylistSource(instanceId, sourceId);
      await refreshPlayback();
    } else {
      saveInstanceProfile(instanceId);
    }
    notifyListeners();
  }

  /// Resolve the target instance for writing operations.
  /// Returns the online active instance, or the offline selected instance ID,
  /// or null if neither is available.
  String? _resolveWriteTarget() {
    final online = activeInstance;
    if (online != null && online.isServerManaged) return online.id;
    if (_activeInstanceId != null && _backendOnline) {
      final inst = _instances
          .where((i) => i.instanceId == _activeInstanceId)
          .firstOrNull;
      if (inst != null && inst.isServerMode) return inst.instanceId;
    }
    return null;
  }

  void _addSourceInMemory(String id, String name, List<String> uuids) {
    _playlistSources.removeWhere((s) => s.id == id);
    _playlistSources.add(
      PlaylistSourceInfo(
        id: id,
        name: name,
        songCount: uuids.length,
        uuids: uuids,
      ),
    );
    notifyListeners();
  }

  /// Merge fetched songs into _activePlaylist, dedup by UUID.
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
        shuffle: instance.shuffle,
        repeatMode: instance.repeatMode,
        currentTrack: instance.currentTrack,
      );
    }).toList();
    if (changed) notifyListeners();
  }

  Future<void> openModule(String moduleId) async {
    _activeModuleId = moduleId;
    _activeUiKind = 'default';
    _activeLinkId = '';
    _moduleUiTree = null;
    notifyListeners();
    try {
      _moduleUiTree = await api.getModuleUi(moduleId);
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
    _activeUiKind = 'default';
    _activeLinkId = '';
    _moduleUiTree = null;
    notifyListeners();
  }

  Future<void> openModuleLink(String moduleId, String linkId) async {
    try {
      final tree = await api.getModuleLinkUi(moduleId, linkId);
      _moduleUiTree = tree;
      _overlayMode = 'ui';
      _overlayTitle = '$moduleId / $linkId';
      _activeModuleId = moduleId;
      _activeUiKind = 'link';
      _activeLinkId = linkId;
      _currentTab = 3; // Switch to modules tab
      notifyListeners();
    } catch (_) {}
  }

  Future<void> openModuleSettings(String moduleId) async {
    try {
      final tree = await api.getModuleSettingsUi(moduleId);
      _moduleUiTree = tree;
      _overlayMode = 'ui';
      _overlayTitle = '$moduleId / Settings';
      _activeModuleId = moduleId;
      _activeUiKind = 'settings';
      _activeLinkId = '';
      _currentTab = 3; // Switch to modules tab
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
    _activeUiKind = 'default';
    _activeLinkId = '';
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
    ws.sendUiEvent(
      _activeModuleId ?? '',
      nodeId,
      action,
      value,
      uiKind: _activeUiKind,
      linkId: _activeLinkId,
    );
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
      refreshInstances();
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

  /// Refresh installed instances and archives from local storage.
  void refreshInstances() {
    _instances = ModDeploymentService.loadInstances();
    _archives = ModDeploymentService.listArchives();
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
      refreshInstances();
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
      refreshInstances();
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

      // Check archive for matching modId+mode (restore support)
      final archived = ModDeploymentService.findArchivedInstance(
        mod.id,
        mod.mode,
      );
      if (archived != null) {
        addDeploymentLog(
          'Found archived instance "${archived.instanceId}" from ${archived.gameDir} — '
          'archived at ${archived.archivedAt.toLocal()}',
        );
      }

      // If a previous installation exists at a different path, clean up its
      // backend port_file_dirs entry before adding the new one.
      final oldDir = ModDeploymentService.getRecordedGameDir(mod.id);
      if (oldDir != null && oldDir != path) {
        addDeploymentLog('Cleaning up previous installation at $oldDir...');
        await _removeGameDirFromBackendPortFileDirs(oldDir);
        PortFile.deletePortFile(oldDir);
      }

      final success = await ModDeploymentService.deployMod(
        path,
        mod,
        addDeploymentLog,
        backendPort: _backendPort,
        onPortFileDirChanged: _onPortFileDirChanged,
      );
      refreshModStatuses(gameId);
      refreshInstances();
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
        onPortFileDirChanged: _onPortFileDirChanged,
      );
      refreshModStatuses(gameId);
      refreshInstances();
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  /// Callback passed to ModDeploymentService for backend port_file_dirs sync.
  /// Called with (gameDir, true=add / false=remove).
  Future<void> _onPortFileDirChanged(String gameDir, bool add) async {
    if (add) {
      await _addGameDirToBackendPortFileDirs(gameDir);
    } else {
      await _removeGameDirFromBackendPortFileDirs(gameDir);
    }
  }

  /// Add a game directory to the backend's port_file_dirs config.
  Future<void> _addGameDirToBackendPortFileDirs(String gameDir) async {
    if (!_backendOnline) return;
    try {
      final config = await api.getConfig();
      final dirs = List<String>.from(
        (config['port_file_dirs'] as List?)?.cast<String>() ?? [],
      );
      if (!dirs.contains(gameDir)) {
        dirs.add(gameDir);
        await api.putConfigRaw({'port_file_dirs': dirs});
        await api.saveConfig();
        addDeploymentLog('Backend port_file_dirs updated with: $gameDir');
      }
    } catch (e) {
      addDeploymentLog('Note: could not update backend port_file_dirs ($e)');
    }
  }

  /// Remove a game directory from the backend's port_file_dirs config.
  Future<void> _removeGameDirFromBackendPortFileDirs(String gameDir) async {
    if (!_backendOnline) return;
    try {
      final config = await api.getConfig();
      final dirs = List<String>.from(
        (config['port_file_dirs'] as List?)?.cast<String>() ?? [],
      );
      if (dirs.contains(gameDir)) {
        dirs.remove(gameDir);
        await api.putConfigRaw({'port_file_dirs': dirs});
        await api.saveConfig();
        addDeploymentLog('Backend port_file_dirs removed: $gameDir');
      }
    } catch (e) {
      addDeploymentLog('Note: could not update backend port_file_dirs ($e)');
    }
  }

  @override
  void dispose() {
    // Save active instance profile before shutdown
    if (_activeInstanceId != null) {
      saveInstanceProfile(_activeInstanceId!);
    }
    _serviceResultTimer?.cancel();
    _stopPlaybackPolling();
    _backendMgr.dispose();
    ws.disconnect();
    api.dispose();
    super.dispose();
  }
}
