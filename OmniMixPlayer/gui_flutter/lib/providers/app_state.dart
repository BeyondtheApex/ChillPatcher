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
  String _gamePath = '';
  BepInExStatus _bepinexStatus = BepInExStatus.notInstalled;
  ModStatus _modStatus = ModStatus.notInstalled;
  final List<String> _deploymentLogs = [];
  bool _deploymentBusy = false;

  String get gamePath => _gamePath;
  BepInExStatus get bepinexStatus => _bepinexStatus;
  ModStatus get modStatus => _modStatus;
  List<String> get deploymentLogs => _deploymentLogs;
  bool get deploymentBusy => _deploymentBusy;

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
      // Backend appeared – connect & load
      _connectAndLoad();
    } else {
      // Backend disappeared – clean up
      GuiLogger().conn('AppState: backend gone, cleaning up');
      ws.disconnect();
      _modules.clear();
      _moduleUiTree = null;
      _activeModuleId = null;
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
      }
    } catch (e, st) {
      GuiLogger().error('AppState._connectAndLoad CRASHED', e, st);
    }
  }

  void _setupWs() {
    ws.onEvent = (event) {
      if (event.type == 'backend.state.changed') {
        final running = event.data?['running'] == true;
        if (_backendRunning != running) {
          _backendRunning = running;
          _backendOnline = running;
          notifyListeners();
        }
      } else if (event.type == 'error') {
        final msg = event.data?['message'] as String? ?? 'Unknown error';
        _lastError = msg;
        notifyListeners();
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
      _gamePath = prefs.getString('game_integration_path') ?? '';
      refreshModStatuses();
    } catch (e) {
      GuiLogger().error('loadGameIntegrationSettings failed', e);
    }
  }

  Future<void> setGamePath(String path) async {
    _gamePath = path;
    notifyListeners();
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('game_integration_path', path);
      refreshModStatuses();
    } catch (e) {
      GuiLogger().error('setGamePath failed', e);
    }
  }

  void refreshModStatuses() {
    if (_gamePath.isEmpty) {
      _bepinexStatus = BepInExStatus.notInstalled;
      _modStatus = ModStatus.notInstalled;
      notifyListeners();
      return;
    }

    final game = gameCatalog.firstWhere((g) => g.id == 'chill_with_you');
    final isValid = ModDeploymentService.verifyGameDirectory(_gamePath, game);
    if (!isValid) {
      _bepinexStatus = BepInExStatus.notInstalled;
      _modStatus = ModStatus.notInstalled;
      notifyListeners();
      return;
    }

    _bepinexStatus = ModDeploymentService.checkBepInExStatus(_gamePath);
    final mod = modCatalog.firstWhere((m) => m.id == 'chill_patcher');
    _modStatus = ModDeploymentService.checkModStatus(_gamePath, mod.folderName);
    notifyListeners();
  }

  Future<bool> installBepInEx() async {
    if (_gamePath.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final framework = frameworkCatalog.firstWhere((f) => f.id == 'bepinex_5');
      final success = await ModDeploymentService.deployBepInEx(_gamePath, framework, addDeploymentLog);
      refreshModStatuses();
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> uninstallBepInEx() async {
    if (_gamePath.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final framework = frameworkCatalog.firstWhere((f) => f.id == 'bepinex_5');
      final success = await ModDeploymentService.undeployBepInEx(_gamePath, framework, addDeploymentLog);
      refreshModStatuses();
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> installMod() async {
    if (_gamePath.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final mod = modCatalog.firstWhere((m) => m.id == 'chill_patcher');
      final success = await ModDeploymentService.deployMod(_gamePath, mod, addDeploymentLog);
      refreshModStatuses();
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> uninstallMod() async {
    if (_gamePath.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();

    try {
      final mod = modCatalog.firstWhere((m) => m.id == 'chill_patcher');
      final success = await ModDeploymentService.undeployMod(_gamePath, mod, addDeploymentLog);
      refreshModStatuses();
      return success;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _serviceResultTimer?.cancel();
    _backendMgr.dispose();
    ws.disconnect();
    api.dispose();
    super.dispose();
  }
}
