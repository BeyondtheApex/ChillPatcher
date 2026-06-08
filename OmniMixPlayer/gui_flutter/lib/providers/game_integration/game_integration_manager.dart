/// Game Integration (Mod Manager) state & deployment manager.
/// Extracted from AppState during Riverpod migration.
library;

import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../services/api_client.dart';
import '../../services/port_file.dart'
    if (dart.library.js_interop) '../../stubs/port_file_web.dart';
import '../../services/mod_deployment_service.dart'
    if (dart.library.js_interop) '../../stubs/mod_deployment_service_web.dart';
import '../../models/mod_manifest.dart';
import '../../models/mod_enums.dart';

class GameIntegrationManager extends ChangeNotifier {
  // ── Injected dependencies ──
  final ApiClient Function() _getApi;
  final bool Function() _isOnline;
  final int Function() _getBackendPort;
  final Future<void> Function() _refreshPlayback;
  final void Function(String? id) _clearActiveInstance;

  GameIntegrationManager({
    required this._getApi,
    required bool Function() isOnline,
    required this._getBackendPort,
    required this._refreshPlayback,
    required this._clearActiveInstance,
  }) : _isOnline = isOnline;

  ApiClient get api => _getApi();
  bool get _backendOnline => _isOnline();

  // ── State ──
  final Map<String, String> _gamePaths = {};
  final Map<String, Map<String, dynamic>> _modSettings = {};
  final Map<String, BepInExStatus> _frameworkStatuses = {};
  final Map<String, ModStatus> _modStatuses = {};
  final List<String> _deploymentLogs = [];
  bool _deploymentBusy = false;

  // Instance management (local)
  List<InstalledInstance> _instances = [];
  List<ArchiveEntry> _archives = [];

  // Getters
  String get gamePath => gamePathFor('chill_with_you');
  BepInExStatus get bepinexStatus => bepinexStatusFor('chill_with_you');
  ModStatus get modStatus => modStatusFor('chill_with_you');
  List<String> get deploymentLogs => _deploymentLogs;
  bool get deploymentBusy => _deploymentBusy;
  List<InstalledInstance> get instances => _instances;
  List<ArchiveEntry> get archives => _archives;

  String gamePathFor(String gameId) => _gamePaths[gameId] ?? '';
  Map<String, dynamic> settingsForMod(String modId) =>
      _modSettings[modId] ?? {};

  String _frameworkStatusKey(String gameId, String frameworkId) =>
      '$gameId::$frameworkId';
  String _modStatusKey(String gameId, String modId) => '$gameId::$modId';

  GameDeclaration? _gameById(String gameId) {
    for (final game in gameCatalog) {
      if (game.id == gameId) return game;
    }
    return null;
  }

  BepInExStatus frameworkStatusFor(String gameId, String frameworkId) =>
      _frameworkStatuses[_frameworkStatusKey(gameId, frameworkId)] ??
      BepInExStatus.notInstalled;

  BepInExStatus bepinexStatusFor(String gameId) {
    final game = _gameById(gameId);
    final framework = game == null
        ? frameworkById('bepinex_5')
        : primaryFrameworkForGame(game);
    if (framework == null) return BepInExStatus.notInstalled;
    return frameworkStatusFor(gameId, framework.id);
  }

  ModStatus modStatusFor(String gameId, [String? modId]) {
    final game = _gameById(gameId);
    final resolvedModId =
        modId ?? (game == null ? null : primaryModForGame(game)?.id);
    if (resolvedModId == null || resolvedModId.isEmpty) {
      return ModStatus.notInstalled;
    }
    return _modStatuses[_modStatusKey(gameId, resolvedModId)] ??
        ModStatus.notInstalled;
  }

  // ── Settings persistence ──

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

  // ── Load ──

  Future<void> loadSettings() async {
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
      await checkModUpdates(); // compare installed vs bundled versions
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

  // ── Mod status ──

  void refreshModStatuses([String? gameId]) {
    final games = gameId == null
        ? gameCatalog
        : gameCatalog.where((g) => g.id == gameId);
    for (final game in games) {
      final path = gamePathFor(game.id);
      final frameworks = frameworksForGame(game);
      final mods = modsForGame(game);
      if (path.isEmpty ||
          !ModDeploymentService.verifyGameDirectory(path, game)) {
        for (final framework in frameworks) {
          _frameworkStatuses[_frameworkStatusKey(game.id, framework.id)] =
              BepInExStatus.notInstalled;
        }
        for (final mod in mods) {
          _modStatuses[_modStatusKey(game.id, mod.id)] = ModStatus.notInstalled;
        }
        continue;
      }

      for (final framework in frameworks) {
        _frameworkStatuses[_frameworkStatusKey(game.id, framework.id)] =
            _checkFrameworkStatus(path, framework);
      }

      for (final mod in mods) {
        _modStatuses[_modStatusKey(game.id, mod.id)] = mod.installsToGameRoot
            ? ModDeploymentService.checkRootModStatus(path, mod)
            : ModDeploymentService.checkModStatus(
                path,
                mod.folderName,
                pluginTargetDir: mod.pluginTargetDir,
              );
      }
    }
    notifyListeners();
  }

  /// After refreshModStatuses, check if any installed mod needs an update
  /// by comparing the .managed marker version against the bundled version.
  Future<void> checkModUpdates([String? gameId]) async {
    final games = gameId == null
        ? gameCatalog
        : gameCatalog.where((g) => g.id == gameId);
    for (final game in games) {
      final path = gamePathFor(game.id);
      if (path.isEmpty) continue;
      for (final mod in modsForGame(game)) {
        final key = _modStatusKey(game.id, mod.id);
        if (_modStatuses[key] == ModStatus.installed) {
          final needs = await ModDeploymentService.needsVersionUpdate(
            path,
            mod.id,
          );
          if (needs) {
            _modStatuses[key] = ModStatus.needsUpdate;
          }
        }
      }
    }
    notifyListeners();
  }

  BepInExStatus _checkFrameworkStatus(
    String gameDir,
    FrameworkDeclaration framework,
  ) {
    return ModDeploymentService.checkFrameworkStatus(gameDir, framework);
  }

  // ── Instances & Archives ──

  void refreshInstances() {
    _instances = ModDeploymentService.loadInstances();
    notifyListeners();
  }

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

  Future<bool> deleteInstance(String id) async {
    if (!_backendOnline) return false;
    try {
      await api.deleteInstance(id);
      ModDeploymentService.removeInstance(id);
      _clearActiveInstance(id);
      refreshInstances();
      await _refreshPlayback();
      return true;
    } catch (e) {
      return false;
    }
  }

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

  // ── Deployment ──

  void addDeploymentLog(String log) {
    final timeStr = DateTime.now().toLocal().toString().substring(11, 19);
    _deploymentLogs.add('[$timeStr] $log');
    notifyListeners();
  }

  void clearDeploymentLogs() {
    _deploymentLogs.clear();
    notifyListeners();
  }

  Future<bool> uninstallFramework({
    String gameId = 'chill_with_you',
    required String frameworkId,
  }) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();
    try {
      final framework = frameworkById(frameworkId);
      if (framework == null) return false;
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

  Future<bool> finalizeInstall({
    required String gameId,
    required String modId,
    String? inheritArchiveId,
  }) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty) return false;

    _deploymentBusy = true;
    notifyListeners();
    try {
      final game = gameCatalog.firstWhere((g) => g.id == gameId);
      final mod = modById(modId);
      if (mod == null) return false;

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
      addDeploymentLog('Backend online: $_backendOnline');
      if (_backendOnline) {
        final inst = ModDeploymentService.findInstanceByDir(path);
        addDeploymentLog(
          'Found instance by dir: ${inst != null ? inst.instanceId : "null"}',
        );
        if (inst != null) {
          try {
            addDeploymentLog(
              'Calling setInstanceMeta(instanceId=${inst.instanceId}, modId=${mod.id}, gameName=${mod.name}, mode=${mod.mode}, caps=${mod.capabilities != null})...',
            );
            await api.setInstanceMeta(
              inst.instanceId,
              mod.id,
              mod.name,
              mod.mode,
              capabilities: mod.capabilities,
            );
            addDeploymentLog('setInstanceMeta succeeded');
            final defaultProfile = {'volume': 1.0};
            await api.updateInstanceProfile(inst.instanceId, defaultProfile);
            addDeploymentLog('updateInstanceProfile succeeded');
          } catch (e) {
            addDeploymentLog('Warning: failed to save instance metadata ($e)');
          }
        } else {
          addDeploymentLog(
            'Warning: findInstanceByDir returned null for path=$path',
          );
        }
      } else {
        addDeploymentLog(
          'Warning: backend not online, skipping instance registration',
        );
      }

      refreshModStatuses(gameId);
      refreshInstances();
      await checkModUpdates(gameId); // verify installed version matches bundled
      addDeploymentLog('Calling refreshPlayback...');
      await _refreshPlayback();
      addDeploymentLog('refreshPlayback completed');
      return true;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> finalizeFrameworkInstall({
    required String gameId,
    required String frameworkId,
  }) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty) return false;

    _deploymentBusy = true;
    notifyListeners();
    try {
      refreshModStatuses(gameId);
      refreshInstances();
      return true;
    } finally {
      _deploymentBusy = false;
      notifyListeners();
    }
  }

  Future<bool> uninstallMod({
    String gameId = 'chill_with_you',
    String? modId,
  }) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();
    try {
      final game = gameCatalog.firstWhere((g) => g.id == gameId);
      final mod = modId == null ? primaryModForGame(game) : modById(modId);
      if (mod == null) return false;
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

  Future<bool> redeployModSettingsOnly({
    required String gameId,
    String? modId,
  }) async {
    final path = gamePathFor(gameId);
    if (path.isEmpty || _deploymentBusy) return false;
    _deploymentBusy = true;
    clearDeploymentLogs();
    notifyListeners();
    try {
      final game = gameCatalog.firstWhere((g) => g.id == gameId);
      final mod = modId == null ? primaryModForGame(game) : modById(modId);
      if (mod == null) return false;
      addDeploymentLog('Re-applying settings for ${mod.name}...');
      await mod.onDeploy(path, addDeploymentLog, settingsForMod(mod.id));
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

  // ── Port file dir sync ──

  Future<void> _onPortFileDirChanged(String gameDir, bool add) async {
    if (add) {
      await _addGameDirToBackendPortFileDirs(gameDir);
    } else {
      await _removeGameDirFromBackendPortFileDirs(gameDir);
    }
  }

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
}
