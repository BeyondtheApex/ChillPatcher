import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/node_data.dart';
import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../generated/omni_mix_player/models/track.pb.dart';
import '../generated/omni_mix_player/models/album.pb.dart';
import '../generated/omni_mix_player/models/tag.pb.dart';
import '../generated/omni_mix_player/models/playlist.pb.dart';
import '../generated/omni_mix_player/services/playback.pb.dart';
import '../services/api_client.dart';
import '../services/ws_client.dart';
import '../services/floating_window_service.dart';
import '../services/backend_manager.dart'
    if (dart.library.js_interop) '../stubs/backend_manager_web.dart';
import '../services/platform_service.dart'
    if (dart.library.js_interop) '../stubs/platform_service_web.dart';
import 'backend/backend_state_manager.dart';
import 'service/service_state_manager.dart';
import 'playback/playback_state_manager.dart';
import 'modules/modules_state_manager.dart';
import 'game_integration/game_integration_manager.dart';
import 'input/input_event_controller.dart';

enum AppThemeMode { light, dark, system }

/// Application state root — composes domain managers and delegates.
/// During Riverpod migration, this is a pure composition root that owns
/// five sub-managers. All domain logic lives in the managers; AppState
/// only handles wiring and cross-domain coordination.
class AppState extends ChangeNotifier {
  // ── Sub-managers ──
  late final BackendStateManager _backend;
  late final ServiceStateManager _service;
  late final PlaybackStateManager _playback;
  late final ModulesStateManager _modules;
  late final GameIntegrationManager _game;
  late final InputEventController _input;

  // ── Local state (not yet extracted) ──
  AppThemeMode _themeMode = AppThemeMode.system;
  int _seedColor = 0xFF673AB7;
  bool _useSystemColor = true;
  String _language = 'system';
  String _closeBehavior = 'exit';
  bool _mediaControlsEnabled = true;
  bool _floatingPlayerVisible = false;
  int _currentTab = 0;
  String? _lastError;
  int _libraryGeneration = 0;
  int _equalizerGeneration = 0;
  double _lastManualX = 0.0;
  double _lastManualY = 0.0;

  // ── Public accessors ──
  BackendManager get backendMgr => _backend.backendMgr;
  ApiClient get api => _backend.api;
  WsClient get ws => _backend.ws;
  String get apiBaseUrl => _backend.apiBaseUrl;
  InputEventController get input => _input;

  // ── Backend ──
  bool get backendOnline => _backend.online;
  bool get backendRunning => _backend.running;
  bool get backendBusy => _backend.busy;
  String get backendPort => '${_backend.port}';
  String get backendBind => _backend.bind;
  bool get autostart => _backend.autostart;
  bool get minimizeToTray => _backend.minimizeToTray;
  Future<void> startBackend() => _backend.start();
  Future<void> stopBackend() => _backend.stop();
  Future<void> toggleBackend() => _backend.toggle();
  Future<void> restartBackend() => _backend.restart();
  Future<void> fullQuit() => _backend.fullQuit();
  Future<void> setBackendPort(String p) async {
    _backend.portVal = int.tryParse(p) ?? 17890;
    notifyListeners();
    await api.putConfig(AppConfig(backendPort: p));
  }

  Future<void> setBackendBind(String b) async {
    _backend.bindVal = b;
    notifyListeners();
    await api.putConfig(AppConfig(backendBind: b));
  }

  Future<void> setAutostart(bool v) async {
    _backend.autostartVal = v;
    notifyListeners();
    await PlatformService.setGuiAutostart(v);
    try {
      await api.putConfig(AppConfig(autostart: v));
      await api.saveConfig();
    } catch (_) {}
  }

  Future<void> setMinimizeToTray(bool v) async {
    _backend.minimizeToTrayVal = v;
    notifyListeners();
    try {
      await api.putConfig(AppConfig(minimizeToTray: v));
      await api.saveConfig();
    } catch (_) {}
  }

  // ── Service ──
  String get serviceState => _service.state;
  bool get serviceBusy => _service.busy;
  bool get serviceAutoStart => _service.autoStart;
  String? get serviceResult => _service.result;
  Future<String> installService() => _service.install();
  Future<String> uninstallService() => _service.uninstall();
  Future<void> refreshServiceState() => _service.refresh();
  Future<bool> setServiceAutoStart(bool v) => _service.setAutoStart(v);

  // ── Playback ──
  List<InstanceSummary> get playbackInstances => _playback.instances;
  String? get activeInstanceId => _playback.activeInstanceId;
  List<QueueTrack> get activeQueue => _playback.activeQueue;
  List<QueueTrack> get activeHistory => _playback.activeHistory;
  List<QueueTrack> get activePlaylist => _playback.activePlaylist;
  List<PlaylistSourceState> get playlistSources => _playback.playlistSources;
  bool get playbackLoading => _playback.loading;
  double get lastVolume => _playback.lastVolume;
  double get lastTargetLatency => _playback.lastTargetLatency;
  int get playbackInstanceCount => _playback.instanceCount;
  int get attachedAudioClientCount => _playback.attachedAudioClientCount;
  InstanceSummary? get activeInstance => _playback.activeInstance;
  bool get canControlActiveInstance => _playback.canControlActiveInstance;
  bool get canManageActiveLibrary => _playback.canManageActiveLibrary;
  PlaybackStatus? get playbackStatus => _playback.status;
  String get currentTrackTitle => _playback.currentTitle;
  String get currentTrackArtist => _playback.currentArtist;
  double get currentTrackDuration => _playback.currentDuration;
  double get currentTrackPosition => _playback.currentPosition;
  bool get isPlaying => _playback.isPlaying;
  bool get shuffleEnabled => _playback.shuffle;
  String get repeatModeStr => _playback.repeatMode.name;
  bool isInstanceOnline(String id) => _playback.isInstanceOnline(id);
  bool instanceExists(String id) => _playback.instanceExists(id);
  bool canControlInstance(String id) => _playback.canControlInstance(id);
  bool canManageInstanceLibrary(String id) =>
      _playback.canManageInstanceLibrary(id);
  bool hasCapability(bool Function(InstanceCapabilities c) check) =>
      _playback.hasCapability(check);
  Set<String> get backendInstanceIds => _playback.backendInstanceIds;
  Future<void> selectInstance(String? id) => _playback.selectInstance(id);
  void selectPlaybackInstance(String id) => _playback.selectInstance(id);
  Future<void> loadActiveProfile() => _playback.loadActiveProfile();
  Future<void> saveActive() => _playback.saveActive();
  Future<void> refreshPlayback() => _playback.refreshPlayback();
  Future<void> togglePlayback() => _playback.togglePlayback();
  Future<void> nextTrack() => _playback.nextTrack();
  Future<void> previousTrack() => _playback.previousTrack();
  Future<void> seekActive(double p) => _playback.seekActive(p);
  Future<void> setVolumeActive(double v) => _playback.setVolumeActive(v);
  Future<void> setTargetLatencyActive(double l) =>
      _playback.setTargetLatencyActive(l);
  Future<void> playSongOnActive(String uuid) =>
      _playback.playSongOnActive(uuid);
  Future<void> addSongToActiveQueue(String uuid) =>
      _playback.addSongToActiveQueue(uuid);
  Future<void> removeQueueItem(int i) => _playback.removeQueueItem(i);
  Future<void> clearActiveQueue() => _playback.clearActiveQueue();
  Future<void> clearActiveHistory() => _playback.clearActiveHistory();
  Future<void> moveQueueItem(int f, int t) => _playback.moveQueueItem(f, t);
  Future<void> removeHistoryItem(int i) => _playback.removeHistoryItem(i);
  Future<void> moveHistoryItem(int f, int t) => _playback.moveHistoryItem(f, t);
  Future<void> addSongNextOnActive(String uuid) =>
      _playback.addSongNextOnActive(uuid);
  Future<void> setSongExcluded(String uuid, bool e) =>
      _playback.setSongExcluded(uuid, e);
  Future<void> setShuffle(bool e) => _playback.setShuffle(e);
  Future<void> setRepeatMode(String m) => _playback.setRepeatMode(m);
  Future<void> addTagToActivePlaylist(Tag tag) =>
      _playback.addTagToActivePlaylist(tag);
  Future<void> addAlbumToActivePlaylist(Album a) =>
      _playback.addAlbumToActivePlaylist(a);
  Future<void> removePlaylistSource(String id) =>
      _playback.removePlaylistSource(id);
  Future<void> addPlaylistToActivePlaylist(Playlist p) =>
      _playback.addPlaylistToActivePlaylist(p);
  Future<void> addTrackToActivePlaylist(Track t) =>
      _playback.addTrackToActivePlaylist(t);

  // ── Modules ──
  List<ModuleInfoResponse> get modules => _modules.modules;
  bool get modulesLoading => _modules.loading;
  String? get activeModuleId => _modules.activeModuleId;
  RawNodeData? get moduleUiTree => _modules.moduleUiTree;
  RawNodeData? get overlayUiTree => _modules.overlayUiTree;
  String get overlayMode => _modules.overlayMode;
  String get overlayTitle => _modules.overlayTitle;
  bool get hasOverlay => _modules.hasOverlay;
  bool get hasModuleDetail => _modules.hasModuleDetail;
  Future<void> refreshModules() => _modules.refreshModules();
  Future<void> setModuleEnabled(String id, bool e) =>
      _modules.setModuleEnabled(id, e);
  Future<void> openModule(String id) => _modules.openModule(id);
  void closeModule() => _modules.closeModule();
  Future<void> openModuleLink(String mid, String lid) =>
      _modules.openModuleLink(mid, lid);
  Future<void> openModuleSettings(String mid) =>
      _modules.openModuleSettings(mid);
  void openAbout() => _modules.openAbout();
  void closeOverlay() => _modules.closeOverlay();
  void dispatchUiEvent(String nodeId, String action, String value) {
    _modules.dispatchUiEvent(
      nodeId,
      action,
      value,
      isWsConnected: ws.isConnected,
      sendFn: (modId, nId, act, val, {required uiKind, required linkId}) {
        ws.sendUiEvent(modId, nId, act, val, uiKind: uiKind, linkId: linkId);
      },
    );
  }

  // ── Game Integration ──
  String get gamePath => _game.gamePath;
  BepInExStatus get bepinexStatus => _game.bepinexStatus;
  ModStatus get modStatus => _game.modStatus;
  List<String> get deploymentLogs => _game.deploymentLogs;
  bool get deploymentBusy => _game.deploymentBusy;
  List<InstalledInstance> get instances => _game.instances;
  List<ArchiveEntry> get archives => _game.archives;
  String gamePathFor(String gameId) => _game.gamePathFor(gameId);
  Map<String, dynamic> settingsForMod(String modId) =>
      _game.settingsForMod(modId);
  BepInExStatus bepinexStatusFor(String gameId) =>
      _game.bepinexStatusFor(gameId);
  BepInExStatus frameworkStatusFor(String gameId, String frameworkId) =>
      _game.frameworkStatusFor(gameId, frameworkId);
  ModStatus modStatusFor(String gameId, [String? modId]) =>
      _game.modStatusFor(gameId, modId);
  Future<void> saveModSettings(String mid, Map<String, dynamic> s) =>
      _game.saveModSettings(mid, s);
  Future<void> loadGameIntegrationSettings() => _game.loadSettings();
  Future<void> setGamePath(String p, {String gameId = 'chill_with_you'}) =>
      _game.setGamePath(p, gameId: gameId);
  void refreshModStatuses([String? gid]) => _game.refreshModStatuses(gid);
  void refreshInstances() => _game.refreshInstances();
  Future<void> refreshBackendArchives() => _game.refreshBackendArchives();
  Future<bool> deleteInstance(String id) => _game.deleteInstance(id);
  Future<bool> archiveInstanceWithLabel(String id, String label) =>
      _game.archiveInstanceWithLabel(id, label);
  Future<bool> deleteBackendArchive(String id) =>
      _game.deleteBackendArchive(id);
  Future<bool> renameBackendArchive(String id, String label) =>
      _game.renameBackendArchive(id, label);
  void addDeploymentLog(String log) => _game.addDeploymentLog(log);
  void clearDeploymentLogs() => _game.clearDeploymentLogs();
  Future<bool> uninstallFramework({
    String gameId = 'chill_with_you',
    required String frameworkId,
  }) => _game.uninstallFramework(gameId: gameId, frameworkId: frameworkId);
  Future<bool> uninstallMod({
    String gameId = 'chill_with_you',
    String? modId,
  }) => _game.uninstallMod(gameId: gameId, modId: modId);
  Future<bool> redeployModSettingsOnly({
    required String gameId,
    String? modId,
  }) => _game.redeployModSettingsOnly(gameId: gameId, modId: modId);

  Future<bool> finalizeModInstall({
    required String gameId,
    required String modId,
    String? inheritArchiveId,
  }) => _game.finalizeInstall(
    gameId: gameId,
    modId: modId,
    inheritArchiveId: inheritArchiveId,
  );

  Future<bool> finalizeFrameworkInstall({
    required String gameId,
    required String frameworkId,
  }) =>
      _game.finalizeFrameworkInstall(gameId: gameId, frameworkId: frameworkId);

  // ── Theme ──
  AppThemeMode get themeMode => _themeMode;
  int get seedColor => _seedColor;
  bool get useSystemColor => _useSystemColor;
  String get language => _language;
  String get closeBehavior => _closeBehavior;
  bool get mediaControlsEnabled => _mediaControlsEnabled;
  bool get floatingPlayerVisible => _floatingPlayerVisible;
  void setThemeMode(AppThemeMode mode) {
    _themeMode = mode;
    notifyListeners();
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

  void setMediaControlsEnabled(bool v) {
    _mediaControlsEnabled = v;
    _playback.setMediaControlsEnabled(v);
    _saveUiPrefs();
    notifyListeners();
  }

  void setFloatingPlayerVisible(bool v) {
    if (_floatingPlayerVisible == v) return;
    _floatingPlayerVisible = v;
    _saveUiPrefs();
    unawaited(FloatingWindowService.instance.setPlayerVisible(v));
    notifyListeners();
  }

  void setLanguage(String lang) {
    _language = lang;
    notifyListeners();
    try {
      api.putConfig(AppConfig(language: lang));
      api.saveConfig();
    } catch (_) {}
  }

  Future<void> saveAllConfig() => api.saveConfig();

  // ── Navigation ──
  int get currentTab => _currentTab;
  void selectTab(int tab) {
    _currentTab = tab;
    _modules.onTabChanged();
    notifyListeners();
    if (tab == 3 && backendOnline) _modules.loadModules();
  }

  // ── Error ──
  String? consumeError() {
    final e = _lastError;
    _lastError = null;
    return e;
  }

  // ── Generations ──
  int get libraryGeneration => _libraryGeneration;
  int get equalizerGeneration => _equalizerGeneration;

  // ── Config ──
  Future<void> saveAndRestart() async {
    if (_backend.busy) return;
    notifyListeners();
    try {
      await api.putConfig(
        AppConfig(
          backendPort: backendPort,
          backendBind: _backend.bind,
          autostart: _backend.autostart,
          minimizeToTray: _backend.minimizeToTray,
          theme: _themeMode.name,
          language: _language,
          seedColor: _seedColor,
          useSystemColor: _useSystemColor,
          closeBehavior: _closeBehavior,
        ),
      );
      await api.saveConfig();
      await _backend.stop();
      await Future.delayed(const Duration(seconds: 1));
      await _backend.start();
    } finally {
      notifyListeners();
    }
  }

  Future<void> resetToDefaults() async {
    _backend.portVal = 17890;
    _backend.bindVal = '127.0.0.1';
    _backend.autostartVal = false;
    _backend.minimizeToTrayVal = true;
    _themeMode = AppThemeMode.system;
    _language = 'system';
    _seedColor = 0xFF673AB7;
    _useSystemColor = true;
    _closeBehavior = 'exit';
    _mediaControlsEnabled = true;
    _floatingPlayerVisible = false;
    _playback.setMediaControlsEnabled(true);
    unawaited(FloatingWindowService.instance.setPlayerVisible(false));
    _saveUiPrefs();
    notifyListeners();
    await saveAndRestart();
  }

  // ── Init ──

  void init({int? port}) {
    _createManagers();
    _wireManagers();
    _initFloatingWindows();
    _loadUiPrefs();
    _backend.init(port: port);
    _game.loadSettings();
    _input.init();
    _input.events.listen((event) {
      FloatingWindowService.instance.handleInputEvent(event);
    });
  }

  void initWeb({int? port}) {
    _createManagers();
    _wireManagers();
    _loadUiPrefs();
    _backend.initWeb(port: port);
    _playback.startPolling();
    _input.init();
    _input.events.listen((event) {
      FloatingWindowService.instance.handleInputEvent(event);
    });
  }

  void _createManagers() {
    _backend = BackendStateManager();
    _service = ServiceStateManager();
    _playback = PlaybackStateManager(() => _backend.api);
    _modules = ModulesStateManager(() => _backend.api);
    _input = InputEventController();

    // Register default playback shortcut actions
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'playback_play_pause',
        descriptionKey: 'shortcutPlayPause',
        onTrigger: () => togglePlayback(),
      ),
    );
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'playback_next',
        descriptionKey: 'shortcutNext',
        onTrigger: () => nextTrack(),
      ),
    );
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'playback_prev',
        descriptionKey: 'shortcutPrev',
        onTrigger: () => previousTrack(),
      ),
    );
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'playback_vol_up',
        descriptionKey: 'shortcutVolUp',
        onTrigger: () => setVolumeActive((lastVolume + 0.05).clamp(0.0, 1.0)),
      ),
    );
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'playback_vol_down',
        descriptionKey: 'shortcutVolDown',
        onTrigger: () => setVolumeActive((lastVolume - 0.05).clamp(0.0, 1.0)),
      ),
    );
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'floating_window_toggle',
        descriptionKey: 'shortcutToggleFloatingPlayer',
        onTrigger: () => setFloatingPlayerVisible(!floatingPlayerVisible),
      ),
    );
    _input.registerShortcutAction(
      ShortcutAction(
        id: 'floating_window_center_left_quad',
        descriptionKey: 'shortcutCenterLeftQuad',
        onTrigger: () =>
            unawaited(FloatingWindowService.instance.moveToCenterLeftQuad()),
      ),
    );

    _game = GameIntegrationManager(
      getApi: () => _backend.api,
      isOnline: () => _backend.online,
      getBackendPort: () => _backend.port,
      refreshPlayback: () => _playback.refreshPlayback(),
      clearActiveInstance: (id) {
        if (_playback.activeInstanceId == id) {
          _playback.selectInstance(null);
          _backend.api.putConfigRaw({'active_instance': ''});
          _backend.api.saveConfig();
        }
      },
    );
  }

  void _wireManagers() {
    for (final m in <ChangeNotifier>[
      _backend,
      _service,
      _playback,
      _modules,
      _game,
      _input,
    ]) {
      m.addListener(() => notifyListeners());
    }

    _backend.onNeedRefreshPlayback = () => _playback.refreshPlayback();
    _backend.onNeedRefreshArchives = () => _game.refreshBackendArchives();
    _backend.onNeedLoadModules = () => _modules.loadModules();
    _backend.onNeedLoadActiveProfile = () => _playback.loadActiveProfile();
    _backend.onPositionEvent = (d) {
      if (d is Map<String, dynamic>) {
        final instanceId = d['instanceId'] as String? ?? '';
        final position = (d['position'] as num?)?.toDouble() ?? 0.0;
        _playback.applyPositionChanged(instanceId, position);
      }
    };
    _backend.onTrackChanged = (id, e) => _playback.applyTrackChanged(id, e);
    _backend.onStateChanged = (id, state) =>
        _playback.applyStateChanged(id, state);
    _backend.onPositionChanged = (id, position) =>
        _playback.applyPositionChanged(id, position);
    _backend.onEqualizerChanged = () {
      _equalizerGeneration++;
      notifyListeners();
    };
    _backend.onPlaylistUpdated = () {
      _playback.refreshPlayback();
      _libraryGeneration++;
      notifyListeners();
    };
    _backend.onModulesChanged = () {
      _modules.loadModules();
      _libraryGeneration++;
      notifyListeners();
    };
    _backend.onProfileChanged = () => _playback.loadActiveProfile();
    _backend.onError = (msg) {
      _lastError = msg;
      notifyListeners();
    };
    _backend.onLibraryBump = () {
      _libraryGeneration++;
      notifyListeners();
    };
    _backend.onStopCleanup = () {
      _modules.clearOnDisconnect();
      _playback.stopPolling();
      _playback.clearOnDisconnect();
    };
    _backend.onInitComplete = () {
      _playback.startPolling();
      if (_playback.activeInstanceId == null) {
        final si = _playback.instances
            .where((i) => _playback.canControlInstance(i.id))
            .firstOrNull;
        if (si != null) _playback.selectInstance(si.id);
      }
      _libraryGeneration++;
      notifyListeners();
    };
    _backend.getServiceState = () => _service.state;
    _backend.setServiceState = (v) => _service.stateVal = v;
    _backend.setServiceAutoStart = (v) => _service.autoStartVal = v;
    _backend.onUiPushCallback = (push) => _modules.handleUiPush(push);
  }

  void _initFloatingWindows() {
    unawaited(
      FloatingWindowService.instance.init(
        onTogglePlayback: togglePlayback,
        onPreviousTrack: previousTrack,
        onNextTrack: nextTrack,
        onSeek: seekActive,
        onUpdateManualPosition: (x, y) async {
          _lastManualX = x;
          _lastManualY = y;
          _saveUiPrefs();
        },
      ),
    );
    addListener(_syncFloatingPlayer);
  }

  void _syncFloatingPlayer() {
    final instance = activeInstance;
    final trackUuid = instance?.currentTrackUuid ?? '';
    unawaited(
      FloatingWindowService.instance.updatePlayer(
        FloatingPlayerSnapshot(
          baseUrl: apiBaseUrl,
          seedColor: _seedColor,
          useSystemColor: _useSystemColor,
          themeMode: _themeMode.name,
          canControl: canControlActiveInstance,
          canSeek: canControlActiveInstance,
          hasTrack: trackUuid.isNotEmpty,
          isPlaying: isPlaying,
          uuid: trackUuid,
          title: currentTrackTitle,
          artist: currentTrackArtist,
          position: currentTrackPosition,
          duration: currentTrackDuration,
          lastManualX: _lastManualX,
          lastManualY: _lastManualY,
        ),
      ),
    );
  }

  void _saveUiPrefs() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setInt('ui_seed_color', _seedColor);
      await prefs.setBool('ui_use_system_color', _useSystemColor);
      await prefs.setString('ui_close_behavior', _closeBehavior);
      await prefs.setBool('ui_media_controls_enabled', _mediaControlsEnabled);
      await prefs.setBool('ui_floating_player_visible', _floatingPlayerVisible);
      await prefs.setDouble('floating_player_last_manual_x', _lastManualX);
      await prefs.setDouble('floating_player_last_manual_y', _lastManualY);
    } catch (_) {}
  }

  void _loadUiPrefs() {
    try {
      SharedPreferences.getInstance().then((prefs) {
        _seedColor = prefs.getInt('ui_seed_color') ?? 0xFF673AB7;
        _useSystemColor = prefs.getBool('ui_use_system_color') ?? true;
        _closeBehavior = prefs.getString('ui_close_behavior') ?? 'exit';
        _mediaControlsEnabled =
            prefs.getBool('ui_media_controls_enabled') ?? true;
        _floatingPlayerVisible =
            prefs.getBool('ui_floating_player_visible') ?? false;
        _lastManualX = prefs.getDouble('floating_player_last_manual_x') ?? 0.0;
        _lastManualY = prefs.getDouble('floating_player_last_manual_y') ?? 0.0;
        _playback.setMediaControlsEnabled(_mediaControlsEnabled);

        _syncFloatingPlayer();

        if (_floatingPlayerVisible) {
          unawaited(FloatingWindowService.instance.setPlayerVisible(true));
        }
        notifyListeners();
      });
    } catch (_) {}
  }

  @override
  void dispose() {
    removeListener(_syncFloatingPlayer);
    if (_playback.activeInstanceId != null) _playback.saveActive();
    unawaited(FloatingWindowService.instance.dispose());
    _service.disposeManager();
    _playback.disposeManager();
    _input.dispose();
    _backend.disposeManager();
    super.dispose();
  }
}
