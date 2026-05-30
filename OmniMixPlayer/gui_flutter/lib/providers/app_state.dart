import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/node_data.dart';
import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';
import '../services/api_client.dart';
import '../services/ws_client.dart';
import '../services/backend_manager.dart'
    if (dart.library.js_interop) '../stubs/backend_manager_web.dart';
import '../services/platform_service.dart'
    if (dart.library.js_interop) '../stubs/platform_service_web.dart';
import '../services/port_file.dart'
    if (dart.library.js_interop) '../stubs/port_file_web.dart';
import '../services/mod_deployment_service.dart'
    if (dart.library.js_interop) '../stubs/mod_deployment_service_web.dart';

enum AppThemeMode { light, dark, system }

class AppState extends ChangeNotifier {
  late ApiClient api;
  late WsClient ws;
  late final BackendManager _backendMgr;

  // Game Integration (Mod Manager)
  final Map<String, String> _gamePaths = {};
  final Map<String, Map<String, dynamic>> _modSettings = {};
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
  Map<String, dynamic> settingsForMod(String modId) =>
      _modSettings[modId] ?? {};

  Future<void> saveModSettings(
    String modId,
    Map<String, dynamic> settings,
  ) async {
    _modSettings[modId] = settings;
    notifyListeners();
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(
        'mod_integration_config_$modId',
        jsonEncode(settings),
      );
    } catch (e) {}
  }

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

  /// Check if an instance exists (online or offline), using backend data as source of truth.
  bool instanceExists(String instanceId) =>
      _playbackInstances.any((i) => i.id == instanceId);

  /// Select an instance. Backend routes all subsequent API calls to it.
  Future<void> selectInstance(String? instanceId) async {
    _activeInstanceId = instanceId;
    if (_backendOnline) {
      api.putConfigRaw({'active_instance': instanceId ?? ''});
      api.saveConfig();
      if (instanceId != null) await loadActiveProfile();
    }
    notifyListeners();
  }

  /// Load active instance profile from backend (online + offline).
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
      notifyListeners();
    } catch (e) {}
  }

  /// Save current state to backend via active profile endpoint.
  Future<void> saveActive() async {
    if (_activeInstanceId == null) return;
    final data = _buildProfileJson();
    try {
      await api.updateActiveProfile(data);
    } catch (e) {}
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

  // Library version — incremented when tags/albums/songs change, pages watch this to reload
  int _libraryGeneration = 0;
  int get libraryGeneration => _libraryGeneration;

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
    // Online server instance (active)
    if (activeInstance?.isServerManaged == true) return true;
    // Selected offline server instance: allow editing profile when backend is running
    if (_activeInstanceId != null && _backendOnline) {
      final inst = _playbackInstances
          .where((i) => i.id == _activeInstanceId)
          .firstOrNull;
      if (inst != null && inst.isServerManaged) return true;
    }
    // No instance explicitly selected: fall back to any available server instance
    if (_activeInstanceId == null && _backendOnline) {
      if (_playbackInstances.any((i) => i.isServerManaged)) return true;
    }
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

  /// Desktop entry: discover backend, manage service, auto-start.
  void init({int? port}) {
    final p = port ?? 17890;
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

  /// Web entry: backend is already running on the same host, connect directly.
  /// Uses same-origin relative URLs so the browser resolves them correctly
  /// whether accessing via localhost or remote IP.
  void initWeb({int? port}) {
    _backendMgr = BackendManager();
    api = ApiClient.forWeb();
    ws = WsClient.forWeb();
    _backendPort = port ?? 17890;
    _setupWs();
    _loadUiPrefs();
    _backendOnline = true;
    _backendRunning = true;
    notifyListeners();
    _connectDirectly();
    _startPlaybackPolling();
  }

  void _createClients(int port) {
    if (_backendMgr.usingSocket) {
      api = ApiClient.withSocket(socketPath: _backendMgr.socketPath);
      ws = WsClient.withSocket(socketPath: _backendMgr.socketPath);
      _backendPort = -1;
    } else {
      final p = _backendMgr.port ?? port;
      api = ApiClient(port: p);
      ws = WsClient(port: p);
      _backendPort = p;
    }
  }

  Future<void> _runInitialDetection() async {
    _serviceBusy = true;
    _backendBusy = true;
    notifyListeners();
    try {
      _serviceState = await PlatformService.getServiceState();
      _serviceAutoStart = await PlatformService.isServiceAutoStart();
      // ═══ Pure service mode: no process fallback ═══

      // 1. Service already running → connect directly
      if (_serviceState == 'running') {
        final healthy = await _backendMgr.checkHealth();
        if (healthy) {
          _applyAliveState(true);
          return;
        }
        // Service stuck → restart it
        await PlatformService.stopService();
        await Future.delayed(const Duration(seconds: 2));
      }

      // 2. Kill any stray process (safety)
      await _backendMgr.forceKillProcess();
      await Future.delayed(const Duration(seconds: 1));

      // 3. Service not installed → auto-install
      if (_serviceState == 'not_installed' || _serviceState == 'unknown') {
        final ok = await PlatformService.installService();
        if (!ok) {
          _lastError = 'Failed to install backend service';
          notifyListeners();
          return;
        }
        _serviceState = 'installed';
        notifyListeners();
        await Future.delayed(const Duration(seconds: 1));
      }

      // 4. Start service and wait for it
      await PlatformService.startService();
      for (var i = 0; i < 20; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
        if (await _backendMgr.checkHealth()) {
          _serviceState = 'running';
          _applyAliveState(true);
          return;
        }
      }
      _lastError = 'Backend service failed to start';
      notifyListeners();
    } finally {
      _backendBusy = false;
      _serviceBusy = false;
      notifyListeners();
    }
  }

  void _onBackendAliveChanged(bool alive) {
    _applyAliveState(alive);
  }

  /// Apply alive/dead state – called both from initial detection & watcher.
  void _applyAliveState(bool alive) {
    if (_backendOnline == alive && _backendRunning == alive) {
      return; // no change
    }

    _backendOnline = alive;
    _backendRunning = alive;

    if (alive) {
      // Backend appeared — connect & load
      _connectAndLoad();
      _startPlaybackPolling();
    } else {
      // Backend disappeared — clean up
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
    try {
      // Re-discover: port file → default port → socket
      final discovered = await _backendMgr.discover();
      if (discovered == null) {
        return;
      }
      // Dispose old clients before recreating
      ws.disconnect();
      api.dispose();
      _createClients(17890);
      _setupWs();

      // Give the backend HTTP server a moment to be ready
      for (var i = 0; i < 10; i++) {
        final ok = await _backendMgr.checkHealth();
        if (ok) break;
        await Future.delayed(const Duration(milliseconds: 300));
      }

      final connected = await ws.connectOnce();
      if (connected) {
        await api.connectController();
        await _loadModules();
        await refreshPlayback();
        await refreshBackendArchives();
        // Auto-select server instance from backend list if none is active yet.
        if (_activeInstanceId == null) {
          final serverInst = _playbackInstances
              .where((i) => i.isServerManaged)
              .firstOrNull;
          if (serverInst != null) {
            _activeInstanceId = serverInst.id;
            api.putConfigRaw({'active_instance': serverInst.id});
            api.saveConfig();
            await loadActiveProfile();
            notifyListeners();
          }
        }
      }
    } catch (e, st) {}
  }

  /// Direct connection without discovery (web mode).
  Future<void> _connectDirectly() async {
    try {
      for (var i = 0; i < 10; i++) {
        final ok = await api.checkHealth();
        if (ok) break;
        await Future.delayed(const Duration(milliseconds: 300));
      }
      final connected = await ws.connectOnce();
      if (connected) {
        await api.connectController();
        await _loadModules();
        await refreshPlayback();
        await refreshBackendArchives();
        if (_activeInstanceId == null) {
          final serverInst = _playbackInstances
              .where((i) => i.isServerManaged)
              .firstOrNull;
          if (serverInst != null) {
            _activeInstanceId = serverInst.id;
            api.putConfigRaw({'active_instance': serverInst.id});
            api.saveConfig();
            await loadActiveProfile();
            notifyListeners();
          }
        }
      }
    } catch (e, st) {
      debugPrint('OmniMix Web: _connectDirectly failed: $e\n$st');
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
          event.type == 'exclude.changed') {
        refreshPlayback();
        refreshBackendArchives(); // archives may change when instances are deleted
      } else if (event.type == 'playlist.updated') {
        refreshPlayback();
        _libraryGeneration++;
        notifyListeners();
      } else if (event.type == 'module.loaded' ||
          event.type == 'module.unloaded') {
        _loadModules();
        _libraryGeneration++;
        notifyListeners();
      } else if (event.type == 'profile.changed') {
        // Profile was updated (e.g. sources/queue changed) — just reload profile data
        loadActiveProfile();
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
      // Kill stray process (safety)
      await _backendMgr.forceKillProcess();
      await Future.delayed(const Duration(seconds: 1));

      // Refresh service state
      _serviceState = await PlatformService.getServiceState();
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
        final ok = await PlatformService.installService();
        if (!ok) {
          _lastError = 'Failed to install backend service';
          notifyListeners();
          return;
        }
        _serviceState = 'installed';
        await Future.delayed(const Duration(seconds: 1));
      }

      // Start service
      await PlatformService.startService();
      for (var i = 0; i < 20; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
        if (await _backendMgr.checkHealth()) {
          _serviceState = 'running';
          _applyAliveState(true);
          return;
        }
      }
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
      // Graceful API stop
      try {
        await api.stopBackend();
      } catch (_) {}

      // Stop service
      _serviceState = await PlatformService.getServiceState();
      if (_serviceState == 'running') {
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
    } catch (e) {}
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
        // Persist auto-selected instance to backend config so profile reads work
        if (nextActiveId != null) {
          api.putConfigRaw({'active_instance': nextActiveId});
          await api.saveConfig();
        }
      }

      // Sync backend online instance IDs for dropdown status
      _backendInstanceIds = instances.map((i) => i.id).toSet();

      _playbackInstances = instances;

      // Load profile data via the unified endpoint (works for both online & offline instances)
      // Only overwrite if the loaded data matches the user's selection (race-condition guard)
      if (_activeInstanceId == nextActiveId && _activeInstanceId != null) {
        await loadActiveProfile();
      }

      notifyListeners();
    } catch (e) {
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

  /// Replace all playlist sources with a single tag source.
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

  /// Replace all playlist sources with a single album source.
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
    notifyListeners();
    saveActive();
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
    // Slow poll as fallback (WebSocket events drive real-time updates)
    _playbackPollTimer = Timer.periodic(const Duration(seconds: 30), (_) {
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
        modId: instance.modId,
        gameName: instance.gameName,
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
    } catch (e) {}
  }

  void _logImageNodes(RawNodeData? node, String context) {
    if (node == null) return;
    if (node.nodeType == 'Image') {}
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
      for (final mod in modCatalog) {
        try {
          final settingsStr = prefs.getString(
            'mod_integration_config_${mod.id}',
          );
          if (settingsStr != null && settingsStr.isNotEmpty) {
            _modSettings[mod.id] = Map<String, dynamic>.from(
              jsonDecode(settingsStr),
            );
          }
        } catch (_) {}
      }
      refreshModStatuses();
      refreshInstances();
    } catch (e) {}
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
    } catch (e) {}
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

  /// Refresh installed instances and archives from local storage + backend.
  void refreshInstances() {
    _instances = ModDeploymentService.loadInstances();
    _archives = ModDeploymentService.listArchives();
    notifyListeners();
  }

  /// Refresh archives from backend API (server is the source of truth).
  Future<void> refreshBackendArchives() async {
    if (!_backendOnline) return;
    try {
      final rawList = await api.getArchives();
      _archives = rawList.map((e) {
        final m = e as Map<String, dynamic>;
        return ArchiveEntry(
          instanceId: m['instanceId'] as String? ?? '',
          modId: m['modId'] as String? ?? '',
          mode: m['mode'] as String? ?? '',
          gameDir: '',
          gameName: '',
          label: m['label'] as String? ?? '',
          archivedAt:
              DateTime.tryParse(m['archivedAt'] as String? ?? '') ??
              DateTime.now(),
        );
      }).toList();
      notifyListeners();
    } catch (e) {}
  }

  /// Delete an instance (online or offline). Also cleans up local registration and port_file_dir.
  Future<bool> deleteInstance(String id) async {
    if (!_backendOnline) return false;
    try {
      await api.deleteInstance(id);
      // Remove local ModDeploymentService registration
      ModDeploymentService.removeInstance(id);
      // If this was the active instance, clear it
      if (_activeInstanceId == id) {
        _activeInstanceId = null;
        _activeQueue = [];
        _activeHistory = [];
        _activePlaylist = [];
        _playlistSources = [];
        api.putConfigRaw({'active_instance': ''});
        api.saveConfig();
      }
      refreshInstances();
      await refreshPlayback();
      return true;
    } catch (e) {
      return false;
    }
  }

  /// Archive an instance with a label.
  Future<bool> archiveInstanceWithLabel(String id, String label) async {
    if (!_backendOnline) return false;
    try {
      await api.archiveInstance(id, label: label);
      await refreshBackendArchives();
      return true;
    } catch (e) {
      return false;
    }
  }

  /// Delete an archive via backend.
  Future<bool> deleteBackendArchive(String id) async {
    if (!_backendOnline) return false;
    try {
      await api.deleteArchive(id);
      await refreshBackendArchives();
      return true;
    } catch (e) {
      return false;
    }
  }

  /// Rename an archive via backend.
  Future<bool> renameBackendArchive(String id, String label) async {
    if (!_backendOnline) return false;
    try {
      await api.renameArchive(id, label);
      await refreshBackendArchives();
      return true;
    } catch (e) {
      return false;
    }
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

  Future<bool> installMod({
    String gameId = 'chill_with_you',
    String? inheritArchiveId,
  }) async {
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
        customSettings: settingsForMod(mod.id),
      );
      if (!success) return false;

      // After deployment: inherit from archive if specified
      if (inheritArchiveId != null &&
          inheritArchiveId.isNotEmpty &&
          _backendOnline) {
        final inst = ModDeploymentService.findInstanceByDir(path);
        if (inst != null) {
          try {
            final result = await api.inheritFromArchive(
              inst.instanceId,
              inheritArchiveId,
            );
            final consumed = result['consumed'] == true;
            addDeploymentLog(
              consumed
                  ? 'Consumed archive "$inheritArchiveId" → instance ${inst.instanceId}'
                  : 'Copied archive "$inheritArchiveId" settings → instance ${inst.instanceId}',
            );
            if (consumed) await refreshBackendArchives();
          } catch (e) {
            addDeploymentLog(
              'Warning: failed to inherit archive settings ($e)',
            );
          }
        }
      }

      // Save instance metadata and create default profile (single source of truth)
      if (_backendOnline) {
        final inst = ModDeploymentService.findInstanceByDir(path);
        if (inst != null) {
          try {
            await api.setInstanceMeta(
              inst.instanceId,
              mod.id,
              mod.name,
              mod.mode,
            );
            // Create default empty profile so the instance appears in backend list
            final defaultProfile = {
              'ActiveQueueId': 'default',
              'Volume': 1.0,
              'Queues': [
                {
                  'Id': 'default',
                  'Name': 'Default',
                  'PlaylistSources': [],
                  'SongUuids': [],
                  'HistoryUuids': [],
                  'Index': -1,
                  'HistoryPosition': -1,
                  'PlaylistPosition': 0,
                  'Shuffle': false,
                  'RepeatMode': 'none',
                },
              ],
            };
            await api.updateInstanceProfile(inst.instanceId, defaultProfile);
          } catch (e) {
            addDeploymentLog('Warning: failed to save instance metadata ($e)');
          }
        }
      }

      refreshModStatuses(gameId);
      refreshInstances();
      await refreshPlayback();
      return true;
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

  Future<bool> redeployModSettingsOnly({required String gameId}) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final game = gameCatalog.firstWhere((g) => g.id == gameId);
      final modId = game.supportedMods.isNotEmpty
          ? game.supportedMods.first
          : '';
      final mod = modCatalog.firstWhere((m) => m.id == modId);

      addDeploymentLog('Re-applying settings for ${mod.name}...');
      await mod.onDeploy(path, addDeploymentLog, settingsForMod(modId));
      addDeploymentLog('Settings re-applied successfully.');
      return true;
    } catch (e) {
      addDeploymentLog('ERROR re-applying settings: $e');
      return false;
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
      final raw = config['port_file_dirs'];
      List<dynamic>? rawList;
      if (raw is String) {
        try {
          rawList = jsonDecode(raw) as List<dynamic>?;
        } catch (_) {}
      } else if (raw is List) {
        rawList = raw;
      }
      final dirs = List<String>.from(rawList?.cast<String>() ?? []);
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
      final raw = config['port_file_dirs'];
      List<dynamic>? rawList;
      if (raw is String) {
        try {
          rawList = jsonDecode(raw) as List<dynamic>?;
        } catch (_) {}
      } else if (raw is List) {
        rawList = raw;
      }
      final dirs = List<String>.from(rawList?.cast<String>() ?? []);
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
    if (_activeInstanceId != null) saveActive();
    _serviceResultTimer?.cancel();
    _stopPlaybackPolling();
    _backendMgr.dispose();
    ws.disconnect();
    api.dispose();
    super.dispose();
  }
}
