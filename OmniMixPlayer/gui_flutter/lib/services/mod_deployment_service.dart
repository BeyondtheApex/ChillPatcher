import 'dart:convert';
import 'dart:io';
import 'package:flutter/services.dart' show rootBundle;
import 'package:archive/archive.dart';
import 'package:file_picker/file_picker.dart';
import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';
import 'port_file.dart';
import 'script_generator.dart';

class ModDeploymentService {
  // Manager local storage base path
  static String get managerDir {
    final localAppData = Platform.environment['LOCALAPPDATA'] ?? '';
    if (localAppData.isEmpty) {
      return '${Directory.systemTemp.path}/omnimix_mod_manager';
    }
    return '$localAppData/OmniMixPlayer/mod_manager';
  }

  static String get bepinexExtractDir => '$managerDir/bepinex_core';
  static String frameworkExtractDir(FrameworkDeclaration framework) =>
      '$managerDir/frameworks/${framework.id}';
  static String get modsExtractDir => '$managerDir/mods';
  static String get rootModsExtractDir => '$managerDir/root_mods';

  /// Triggers a native Windows Folder Picker using the file_picker package.
  static Future<String?> selectDirectory() async {
    try {
      final path = await FilePicker.getDirectoryPath(
        dialogTitle: 'Select Game Directory',
      );
      return path;
    } catch (e) {
      }
    return null;
  }

  /// Verifies if the selected folder is a valid game directory based on signature files.
  static bool verifyGameDirectory(String path, GameDeclaration game) {
    try {
      if (path.isEmpty) return false;
      final dir = Directory(path);
      if (!dir.existsSync()) return false;

      for (final sigFile in game.signatureFiles) {
        final fullPath = '$path/$sigFile';
        if (!File(fullPath).existsSync() && !Directory(fullPath).existsSync()) {
          return false;
        }
      }
      return true;
    } catch (_) {
      return false;
    }
  }

  /// Check BepInEx status in the game directory.
  static BepInExStatus checkBepInExStatus(String gameDir) {
    final framework = frameworkById('bepinex_5');
    if (framework == null) return BepInExStatus.notInstalled;
    return checkFrameworkStatus(gameDir, framework);
  }

  static BepInExStatus checkFrameworkStatus(
    String gameDir,
    FrameworkDeclaration framework,
  ) {
    if (gameDir.isEmpty) return BepInExStatus.notInstalled;

    final statusFiles = framework.statusFiles.isNotEmpty
        ? framework.statusFiles
        : framework.filesToLink;
    final statusDirs = framework.statusDirs.isNotEmpty
        ? framework.statusDirs
        : framework.dirsToLink;

    final exists =
        statusFiles.any((relativePath) => File('$gameDir/$relativePath').existsSync()) ||
        statusDirs.any((relativePath) => Directory('$gameDir/$relativePath').existsSync());
    if (!exists) return BepInExStatus.notInstalled;

    if (framework.managedMarkerFile.isEmpty) return BepInExStatus.unmanaged;
    return File('$gameDir/${framework.managedMarkerFile}').existsSync()
        ? BepInExStatus.managed
        : BepInExStatus.unmanaged;
  }

  /// Check Mod status in the game directory.
  static ModStatus checkModStatus(
    String gameDir,
    String folderName, {
    String pluginTargetDir = 'BepInEx/plugins',
  }) {
    if (gameDir.isEmpty) return ModStatus.notInstalled;

    final modDir = Directory('$gameDir/$pluginTargetDir/$folderName');
    if (modDir.existsSync()) {
      return ModStatus.installed;
    }
    return ModStatus.notInstalled;
  }

  /// Check a game-root native mod status.
  static ModStatus checkRootModStatus(String gameDir, ModDeclaration mod) {
    if (gameDir.isEmpty) return ModStatus.notInstalled;
    final marker = File('$gameDir/.omnimix_mods/${mod.id}.managed');
    if (!marker.existsSync()) return ModStatus.notInstalled;

    for (final relativeFile in mod.rootFilesToLink) {
      if (!File('$gameDir/$relativeFile').existsSync()) {
        return ModStatus.notInstalled;
      }
    }
    for (final relativeDir in mod.rootDirsToLink) {
      if (!Directory('$gameDir/$relativeDir').existsSync()) {
        return ModStatus.notInstalled;
      }
    }
    return ModStatus.installed;
  }



  /// Remove BepInEx files (only if managed) and clean up.
  static Future<bool> undeployBepInEx(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async {
    final managerFwDir = '$managerDir/frameworks/${framework.id}';
    final uninstallScriptPath = '$managerFwDir/uninstall.bat';
    final uninstallScript = File(uninstallScriptPath);

    if (uninstallScript.existsSync()) {
      try {
        log('Starting ${framework.name} uninstallation using script...');
        final success = await runUninstallScript(uninstallScriptPath, managerFwDir, log);
        if (!success) {
          log('WARNING: Uninstallation script returned non-zero exit code.');
        }

        try {
          final d = Directory(managerFwDir);
          if (d.existsSync()) d.deleteSync(recursive: true);
        } catch (e) {
          log('Note: could not delete framework directory from manager cache ($e)');
        }

        removeVersionRecord(framework.id);
        log('${framework.name} uninstallation complete.');
        return true;
      } catch (e) {
        log('ERROR during script-based uninstallation of framework: $e');
        return false;
      }
    }

    try {
      log('Starting ${framework.name} undeployment...');

      // 1. Remove files
      for (final relativeFile in framework.filesToLink) {
        final filePath = '$gameDir/$relativeFile';
        final f = File(filePath);
        if (f.existsSync()) {
          f.deleteSync();
          log('  Deleted file: $relativeFile');
        }
      }

      // 2. Remove core symlink
      for (final relativeDir in framework.dirsToLink) {
        final dirPath = '$gameDir/$relativeDir';
        _deleteLinkSafely(dirPath);
        log('  Removed directory symlink: $relativeDir');
      }

      // 3. Delete managed marker
      if (framework.managedMarkerFile.isNotEmpty) {
        final marker = File('$gameDir/${framework.managedMarkerFile}');
        if (marker.existsSync()) marker.deleteSync();
      }

      // 4. Try cleaning up empty directories
      log('Cleaning up empty folders...');
      final dirsToClean = framework.dirsToCreate.toList()
        ..sort((a, b) => b.length.compareTo(a.length));
      for (final relativeDir in dirsToClean) {
        final dir = Directory('$gameDir/$relativeDir');
        if (dir.existsSync() && dir.listSync().isEmpty) {
          dir.deleteSync();
          log('  Deleted empty: $relativeDir/');
        }
      }

      removeVersionRecord(framework.id);
      log('${framework.name} undeployment complete.');
      return true;
    } catch (e) {
      log('ERROR during ${framework.name} undeployment: $e');
      return false;
    }
  }

  /// Remove Mod directory symlink.
  static Future<bool> undeployMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    Future<void> Function(String, bool)? onPortFileDirChanged,
  }) async {
    final managerModsDir = '$managerDir/mods/${mod.id}';
    final uninstallScriptPath = '$managerModsDir/uninstall.bat';
    final uninstallScript = File(uninstallScriptPath);

    if (uninstallScript.existsSync()) {
      try {
        log('Starting ${mod.name} uninstallation using script...');
        final success = await runUninstallScript(uninstallScriptPath, managerModsDir, log);
        if (!success) {
          log('WARNING: Uninstallation script returned non-zero exit code.');
        }

        // Clean up mod manager cache for this mod
        try {
          final d = Directory(managerModsDir);
          if (d.existsSync()) d.deleteSync(recursive: true);
        } catch (e) {
          log('Note: could not delete mod directory from manager cache ($e)');
        }

        // Clean up metadata records
        final instanceId = _readInstanceId(gameDir);
        if (instanceId != null && instanceId.isNotEmpty) {
          final existing = findInstanceByDir(gameDir);
          if (existing != null) {
            _archiveInstance(instanceId, gameDir, existing);
            _removeInstance(instanceId);
            log('  Archived instance: $instanceId');
          }
        }
        _deleteInstanceId(gameDir);
        PortFile.deletePortFile(gameDir);
        removeVersionRecord(mod.id);
        await onPortFileDirChanged?.call(gameDir, false);

        await mod.onUndeploy(gameDir, log);

        log('${mod.name} uninstallation complete.');
        return true;
      } catch (e) {
        log('ERROR during script-based uninstallation: $e');
        return false;
      }
    }

    if (mod.installsToGameRoot) {
      return undeployRootMod(
        gameDir,
        mod,
        log,
        onPortFileDirChanged: onPortFileDirChanged,
      );
    }

    try {
      log('Starting ${mod.name} undeployment...');
      final linkPath = '$gameDir/${mod.pluginTargetDir}/${mod.folderName}';

      _deleteLinkSafely(linkPath);
      log(
        '  Removed mod directory symlink: ${mod.pluginTargetDir}/${mod.folderName}',
      );

      // Archive instance + clean up (only if instance ID exists)
      final instanceId = _readInstanceId(gameDir);
      if (instanceId != null && instanceId.isNotEmpty) {
        final existing = findInstanceByDir(gameDir);
        if (existing != null) {
          _archiveInstance(instanceId, gameDir, existing);
          _removeInstance(instanceId);
          log('  Archived instance: $instanceId');
        }
      }
      _deleteInstanceId(gameDir);
      PortFile.deletePortFile(gameDir);
      removeVersionRecord(mod.id);
      await onPortFileDirChanged?.call(gameDir, false);

      await mod.onUndeploy(gameDir, log);

      log('${mod.name} undeployment complete.');
      return true;
    } catch (e) {
      log('ERROR during mod undeployment: $e');
      return false;
    }
  }

  // ═══════════════════════════════════════════════════════════
  //  Version Tracking
  // ═══════════════════════════════════════════════════════════

  /// Path: {managerDir}/installed_versions.json
  /// Stores map of { modId or frameworkId -> installed_version }.
  static String get _versionDbPath => '$managerDir/installed_versions.json';

  static Map<String, String> _readVersions() {
    try {
      final f = File(_versionDbPath);
      if (!f.existsSync()) return {};
      final data = jsonDecode(f.readAsStringSync());
      if (data is Map) return data.cast<String, String>();
    } catch (_) {}
    return {};
  }

  static void _writeVersions(Map<String, String> versions) {
    try {
      final dir = Directory(managerDir);
      if (!dir.existsSync()) dir.createSync(recursive: true);
      File(_versionDbPath).writeAsStringSync(jsonEncode(versions));
    } catch (_) {}
  }

  /// Record that a framework/mod was installed at a specific version.
  static void recordInstalledVersion(String id, String version) {
    final v = _readVersions();
    v[id] = version;
    _writeVersions(v);
  }

  /// Remove version record (after uninstall).
  static void removeVersionRecord(String id) {
    final v = _readVersions();
    v.remove(id);
    _writeVersions(v);
  }

  static String? _latestModVersion;

  static Future<void> loadLatestModVersion() async {
    _latestModVersion = await getLatestModVersion();
  }

  static String get latestModVersion => _latestModVersion ?? '1.0.0';

  /// Get installed version for a framework/mod, or null if not installed.
  static String? getInstalledVersion(String id) {
    return _readVersions()[id];
  }

  /// Get the latest available version from the app's bundled version_info.json.
  /// Looks in the exe directory first, then falls back to assets.
  static Future<String?> getLatestModVersion() async {
    // Try reading from file system (playerbuild/)
    try {
      final exeDir = Directory(Platform.resolvedExecutable).parent.path;
      final verFile = File('$exeDir/version_info.json');
      if (verFile.existsSync()) {
        final data = jsonDecode(verFile.readAsStringSync());
        if (data is Map && data['mod_version'] != null) {
          return data['mod_version'] as String;
        }
      }
    } catch (_) {}
    // Fallback: try bundled asset
    try {
      final data = await rootBundle.loadString('assets/version_info.json');
      final parsed = jsonDecode(data);
      if (parsed is Map && parsed['mod_version'] != null) {
        return parsed['mod_version'] as String;
      }
    } catch (_) {}
    return null;
  }

  // ═══════════════════════════════════════════════════════════
  //  Helpers
  // ═══════════════════════════════════════════════════════════

  /// Delete a link or a directory/file safely.
  /// Link(path).deleteSync() removes ONLY the link point, not target files.
  static void _deleteLinkSafely(String path) {
    if (FileSystemEntity.isLinkSync(path)) {
      Link(path).deleteSync();
    } else if (Directory(path).existsSync()) {
      Directory(path).deleteSync(recursive: true);
    } else if (File(path).existsSync()) {
      File(path).deleteSync();
    }
  }

  /// Remove a root-level native mod and restore any backed-up game files.
  static Future<bool> undeployRootMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    Future<void> Function(String, bool)? onPortFileDirChanged,
  }) async {
    try {
      log('Starting ${mod.name} undeployment...');
      final marker = File('$gameDir/.omnimix_mods/${mod.id}.managed');
      if (!marker.existsSync()) {
        log(
          'ERROR ${mod.name} is not managed by OmniMix; refusing to remove root files.',
        );
        return false;
      }

      final backupDir = Directory('$gameDir/.omnimix_backup/${mod.id}');

      for (final relativeFile in mod.rootFilesToLink) {
        final filePath = '$gameDir/$relativeFile';
        final backupPath = '${backupDir.path}/$relativeFile';
        if (File(filePath).existsSync() ||
            Link(filePath).existsSync() ||
            FileSystemEntity.isLinkSync(filePath)) {
          _deleteLinkSafely(filePath);
          log('  Removed linked file: $relativeFile');
        }

        final backup = File(backupPath);
        if (backup.existsSync()) {
          backup.copySync(filePath);
          backup.deleteSync();
          log('  Restored backup: $relativeFile');
        }
      }

      for (final relativeDir in mod.rootDirsToLink) {
        final dirPath = '$gameDir/$relativeDir';
        final backupPath = '${backupDir.path}/$relativeDir';
        _deleteLinkSafely(dirPath);
        log('  Removed directory symlink: $relativeDir');

        final backup = Directory(backupPath);
        if (backup.existsSync()) {
          backup.renameSync(dirPath);
          log('  Restored backup directory: $relativeDir');
        }
      }

      if (marker.existsSync()) marker.deleteSync();
      final markerDir = Directory('$gameDir/.omnimix_mods');
      if (markerDir.existsSync() && markerDir.listSync().isEmpty) {
        markerDir.deleteSync();
      }
      if (backupDir.existsSync() &&
          backupDir.listSync(recursive: true).isEmpty) {
        backupDir.deleteSync(recursive: true);
      }
      final backupRoot = Directory('$gameDir/.omnimix_backup');
      if (backupRoot.existsSync() && backupRoot.listSync().isEmpty) {
        backupRoot.deleteSync();
      }

      // Archive instance + clean up (only if instance ID exists)
      final instanceId = _readInstanceId(gameDir);
      if (instanceId != null && instanceId.isNotEmpty) {
        final existing = findInstanceByDir(gameDir);
        if (existing != null) {
          _archiveInstance(instanceId, gameDir, existing);
          _removeInstance(instanceId);
          log('  Archived instance: $instanceId');
        }
      }
      _deleteInstanceId(gameDir);
      PortFile.deletePortFile(gameDir);
      removeVersionRecord(mod.id);
      await onPortFileDirChanged?.call(gameDir, false);

      await mod.onUndeploy(gameDir, log);

      log('${mod.name} undeployment complete.');
      return true;
    } catch (e) {
      log('ERROR during root mod undeployment: $e');
      return false;
    }
  }



  // ═══════════════════════════════════════════════════════════
  //  Instance Management (UUID-based, with archive)
  // ═══════════════════════════════════════════════════════════

  /// File marker in game root identifying the instance: .omnimix_instance_id
  static String _instanceIdPath(String gameDir) =>
      '$gameDir${Platform.pathSeparator}.omnimix_instance_id';

  static String? _readInstanceId(String gameDir) {
    try {
      final f = File(_instanceIdPath(gameDir));
      if (f.existsSync()) return f.readAsStringSync().trim();
    } catch (_) {}
    return null;
  }

  static void _writeInstanceId(String gameDir, String instanceId) {
    try {
      File(_instanceIdPath(gameDir)).writeAsStringSync(instanceId);
    } catch (e) {
      }
  }

  static void _deleteInstanceId(String gameDir) {
    try {
      final f = File(_instanceIdPath(gameDir));
      if (f.existsSync()) f.deleteSync();
    } catch (_) {}
  }

  /// Generate a unique instance ID: "inst_{modId}_{8hex}"
  static String _generateInstanceId(String modId) {
    final r = DateTime.now().microsecondsSinceEpoch.toRadixString(16);
    return 'inst_${modId}_${r.substring(r.length > 8 ? r.length - 8 : 0).padLeft(8, '0')}';
  }

  // ── installed_instances.json ──

  static String get _instancesDbPath => '$managerDir/installed_instances.json';

  static Map<String, dynamic> _readInstancesDb() {
    try {
      final f = File(_instancesDbPath);
      if (!f.existsSync()) return {};
      return jsonDecode(f.readAsStringSync()) as Map<String, dynamic>;
    } catch (_) {}
    return {};
  }

  static void _writeInstancesDb(Map<String, dynamic> data) {
    try {
      final d = Directory(managerDir);
      if (!d.existsSync()) d.createSync(recursive: true);
      File(_instancesDbPath).writeAsStringSync(jsonEncode(data));
    } catch (_) {}
  }

  static void _saveInstance(InstalledInstance inst) {
    final db = _readInstancesDb();
    db[inst.instanceId] = inst.toJson();
    _writeInstancesDb(db);
  }

  static void _removeInstance(String instanceId) {
    final db = _readInstancesDb();
    db.remove(instanceId);
    _writeInstancesDb(db);
  }

  /// Public API: remove an instance from the local registration DB.
  static void removeInstance(String instanceId) {
    _removeInstance(instanceId);
  }

  /// Remove any installed instances pointing to the same game directory.
  /// Prevents stale/duplicate entries when re-deploying without proper uninstall.
  static void _cleanupStaleInstances(String gameDir) {
    final db = _readInstancesDb();
    final staleIds = <String>[];
    for (final entry in db.entries) {
      if ((entry.value as Map<String, dynamic>)['gameDir'] == gameDir) {
        staleIds.add(entry.key);
      }
    }
    for (final id in staleIds) {
      db.remove(id);
    }
    if (staleIds.isNotEmpty) _writeInstancesDb(db);
  }

  /// Load all currently-installed instances.
  static List<InstalledInstance> loadInstances() {
    final db = _readInstancesDb();
    return db.values
        .map((e) => InstalledInstance.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Find installed instance by game directory, or null.
  static InstalledInstance? findInstanceByDir(String gameDir) {
    return loadInstances().where((i) => i.gameDir == gameDir).firstOrNull;
  }

  /// Returns the previously-recorded game dir for a mod (for cleanup).
  static String? getRecordedGameDir(String modId) {
    return loadInstances()
        .where((i) => i.modId == modId)
        .map((i) => i.gameDir)
        .firstOrNull;
  }

  // ── Archive ──

  static String get _archiveDbPath => '$managerDir/archived_instances.json';

  static Map<String, dynamic> _readArchiveDb() {
    try {
      final f = File(_archiveDbPath);
      if (!f.existsSync()) return {};
      return jsonDecode(f.readAsStringSync()) as Map<String, dynamic>;
    } catch (_) {}
    return {};
  }

  static void _writeArchiveDb(Map<String, dynamic> data) {
    try {
      final d = Directory(managerDir);
      if (!d.existsSync()) d.createSync(recursive: true);
      File(_archiveDbPath).writeAsStringSync(jsonEncode(data));
    } catch (_) {}
  }

  /// Archive an instance before removal.
  static void _archiveInstance(
    String instanceId,
    String gameDir,
    InstalledInstance inst,
  ) {
    final db = _readArchiveDb();
    db[instanceId] = ArchiveEntry(
      instanceId: instanceId,
      modId: inst.modId,
      mode: inst.mode,
      gameDir: gameDir,
      gameName: inst.gameName,
      archivedAt: DateTime.now(),
    ).toJson();
    _writeArchiveDb(db);
  }

  /// Find an archived instance matching modId and mode (for restore).
  static ArchiveEntry? findArchivedInstance(String modId, String mode) {
    final db = _readArchiveDb();
    for (final entry in db.values) {
      final a = ArchiveEntry.fromJson(entry as Map<String, dynamic>);
      if (a.modId == modId && a.mode == mode) return a;
    }
    return null;
  }

  /// List all archived instances.
  static List<ArchiveEntry> listArchives() {
    final db = _readArchiveDb();
    return db.values
        .map((e) => ArchiveEntry.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Delete an archive entry permanently.
  static void deleteArchive(String instanceId) {
    final db = _readArchiveDb();
    db.remove(instanceId);
    _writeArchiveDb(db);
  }

  /// Rename (set label) an archived instance.
  static void renameArchive(String instanceId, String label) {
    final db = _readArchiveDb();
    if (db.containsKey(instanceId)) {
      final existing = db[instanceId] as Map<String, dynamic>;
      existing['label'] = label;
      db[instanceId] = existing;
      _writeArchiveDb(db);
    }
  }

  static Future<PrepResult> prepareInstallStaging(
    String gameDir,
    ModDeclaration mod,
    Map<String, dynamic> settings,
    void Function(String) log,
  ) async {
    log('Preparing temporary directory for staging...');
    final tempDir = Directory.systemTemp.createTempSync('omnimix_mod_install_').path;
    log('Staging path: $tempDir');

    // 1. Call mod preparation
    await mod.prepareStaging(gameDir, tempDir, log, settings);

    // 2. Prepare backups: Copy files from game or from manager backups to tempDir with .vVERSION.bak suffix
    final backupFiles = mod.getFilesToBackup(gameDir);
    final version = mod.getGameVersion(gameDir);
    
    for (final relPath in backupFiles) {
      final managerBackupPath = '$managerDir/backups/${mod.id}/v$version/$relPath';
      final tempBackupPath = '$tempDir/$relPath.v$version.bak';
      
      final managerBackupFile = File(managerBackupPath);
      final gameFile = File('$gameDir/$relPath');
      
      if (managerBackupFile.existsSync()) {
        log('Staging backup from manager cache: $relPath.v$version.bak');
        File(tempBackupPath).parent.createSync(recursive: true);
        managerBackupFile.copySync(tempBackupPath);
      } else if (gameFile.existsSync()) {
        log('Staging backup from original game path: $relPath.v$version.bak');
        File(tempBackupPath).parent.createSync(recursive: true);
        gameFile.copySync(tempBackupPath);
      } else {
        log('Warning: Original file not found for backup: $relPath');
      }
    }

    // 3. Write managed signature inside tempDir so it is processed as an added file
    final markerFile = File('$tempDir/.omnimix_mods/${mod.id}.managed');
    markerFile.parent.createSync(recursive: true);
    markerFile.writeAsStringSync(mod.version);

    // 4. Scan tempDir to list added, linked, and backup files
    final added = <String>[];
    final links = <String>[];
    final backups = <String>[];
    final isDirectoryMap = <String, bool>{};

    final linksDecl = mod.getFilesToLink(gameDir);
    final backupSuffix = '.v$version.bak';

    bool isUnderLink(String relPath) {
      for (final link in linksDecl) {
        if (relPath == link || relPath.startsWith('$link/')) {
          return true;
        }
      }
      return false;
    }

    final tempDirObj = Directory(tempDir);
    final allEntities = tempDirObj.listSync(recursive: true);
    
    for (final entity in allEntities) {
      final relPath = entity.path
          .substring(tempDir.length + 1)
          .replaceAll('\\', '/');
      
      final isDir = entity is Directory;
      isDirectoryMap[relPath] = isDir;

      if (isUnderLink(relPath)) {
        if (linksDecl.contains(relPath)) {
          links.add(relPath);
        }
        continue;
      }

      if (relPath.endsWith(backupSuffix)) {
        final originalRelPath = relPath.substring(0, relPath.length - backupSuffix.length);
        backups.add(originalRelPath);
        continue;
      }

      if (!isDir) {
        added.add(relPath);
      }
    }

    return PrepResult(
      tempDir: tempDir,
      added: added,
      links: links,
      backups: backups,
      isDirectoryMap: isDirectoryMap,
      gameVersion: version,
    );
  }

  static Future<bool> executeInstallStaging(
    String gameDir,
    ModDeclaration mod,
    PrepResult prepResult,
    void Function(String) log, {
    int? backendPort,
    Future<void> Function(String, bool)? onPortFileDirChanged,
  }) async {
    try {
      final tempDir = prepResult.tempDir;
      final version = prepResult.gameVersion;
      final managerModsDir = '$managerDir/mods/${mod.id}';
      final managerBackupDir = '$managerDir/backups/${mod.id}/v$version';

      // 1. Move link files from tempDir to managerModsDir
      log('Writing link files to mod manager cache...');
      for (final relPath in prepResult.links) {
        final srcEntity = prepResult.isDirectoryMap[relPath] == true
            ? Directory('$tempDir/$relPath')
            : File('$tempDir/$relPath');
        final destEntityPath = '$managerModsDir/$relPath';

        if (srcEntity.existsSync()) {
          if (prepResult.isDirectoryMap[relPath] == true) {
            final d = Directory(destEntityPath);
            if (d.existsSync()) d.deleteSync(recursive: true);
            d.parent.createSync(recursive: true);
            (srcEntity as Directory).renameSync(destEntityPath);
          } else {
            final f = File(destEntityPath);
            if (f.existsSync()) f.deleteSync();
            f.parent.createSync(recursive: true);
            (srcEntity as File).renameSync(destEntityPath);
          }
        }
      }

      // 2. Move backup files to managerBackupDir
      log('Writing backup files to mod manager backups...');
      for (final relPath in prepResult.backups) {
        final tempBackupFile = File('$tempDir/$relPath.v$version.bak');
        final destBackupFile = File('$managerBackupDir/$relPath.v$version.bak');

        if (tempBackupFile.existsSync()) {
          if (!destBackupFile.existsSync()) {
            destBackupFile.parent.createSync(recursive: true);
            tempBackupFile.renameSync(destBackupFile.path);
          } else {
            tempBackupFile.deleteSync();
          }
        }
      }

      // 3. Write batch scripts inside tempDir
      final batchGen = BatchScriptGenerator();
      final installScriptContent = batchGen.generateInstallScript(
        gameDir: gameDir,
        tempDir: tempDir,
        managerModsDir: managerModsDir,
        addedFiles: prepResult.added,
        linkFiles: prepResult.links,
        isDirectoryMap: prepResult.isDirectoryMap,
      );
      final uninstallScriptContent = batchGen.generateUninstallScript(
        gameDir: gameDir,
        backupDir: managerBackupDir,
        addedFiles: prepResult.added,
        linkFiles: prepResult.links,
        backupFiles: prepResult.backups,
        isDirectoryMap: prepResult.isDirectoryMap,
        gameVersion: version,
      );

      final installScriptFile = File('$tempDir/install${batchGen.extension}');
      installScriptFile.writeAsStringSync(installScriptContent);
      log('Generated installation script: ${installScriptFile.path}');

      final uninstallScriptFile = File('$tempDir/uninstall${batchGen.extension}');
      uninstallScriptFile.writeAsStringSync(uninstallScriptContent);
      log('Generated uninstallation script: ${uninstallScriptFile.path}');

      // 4. Save uninstall script to manager folder for future use
      final persistentUninstallFile = File('$managerModsDir/uninstall${batchGen.extension}');
      persistentUninstallFile.parent.createSync(recursive: true);
      persistentUninstallFile.writeAsStringSync(uninstallScriptContent);

      // 5. Save instance info
      recordInstalledVersion(mod.id, mod.version);
      var instanceId = _readInstanceId(gameDir);
      if (instanceId == null || instanceId.isEmpty) {
        _cleanupStaleInstances(gameDir);
        instanceId = _generateInstanceId(mod.id);
      }
      _writeInstanceId(gameDir, instanceId);
      final inst = InstalledInstance(
        instanceId: instanceId,
        modId: mod.id,
        mode: mod.mode,
        gameDir: gameDir,
        gameName: mod.name,
        installedAt: DateTime.now(),
      );
      _saveInstance(inst);

      if (backendPort != null) {
        PortFile.writePort(gameDir, backendPort);
      }
      await onPortFileDirChanged?.call(gameDir, true);

      return true;
    } catch (e, st) {
      log('ERROR executing installation staging: $e\n$st');
      return false;
    }
  }

  static Future<bool> runInstallScript(
    String tempDir,
    void Function(String) log,
  ) async {
    log('Requesting administrator permissions to execute installation script...');
    final scriptFile = File('$tempDir/install.bat');
    if (!scriptFile.existsSync()) {
      log('ERROR: Installation script not found at ${scriptFile.path}');
      return false;
    }

    try {
      final scriptPath = scriptFile.path.replaceAll('/', '\\');
      final psScript =
          "\$p = Start-Process -FilePath 'cmd.exe' "
          "-ArgumentList '/c','\"$scriptPath\"' "
          "-Verb RunAs -Wait -WindowStyle Hidden -PassThru; exit \$p.ExitCode";

      final runFile = File('$tempDir/run_elevated.ps1');
      runFile.writeAsStringSync(psScript);

      final process = await Process.run('powershell.exe', [
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        runFile.path.replaceAll('/', '\\'),
      ]);

      return process.exitCode == 0;
    } catch (e) {
      log('ERROR executing script with elevated permissions: $e');
      return false;
    }
  }

  static Future<bool> runUninstallScript(
    String uninstallScriptPath,
    String logDir,
    void Function(String) log,
  ) async {
    log('Executing uninstallation script with administrator permissions...');
    final logFile = File('$logDir/uninstall.log');
    if (logFile.existsSync()) {
      try {
        logFile.deleteSync();
      } catch (_) {}
    }

    try {
      final scriptPath = uninstallScriptPath.replaceAll('/', '\\');
      final psScript =
          "\$p = Start-Process -FilePath 'cmd.exe' "
          "-ArgumentList '/c','\"$scriptPath\"' "
          "-Verb RunAs -Wait -WindowStyle Hidden -PassThru; exit \$p.ExitCode";

      final tempPsFile = File('${Directory.systemTemp.path}/run_uninstall_${DateTime.now().millisecondsSinceEpoch}.ps1');
      tempPsFile.writeAsStringSync(psScript);

      var running = true;
      var lastReadPos = 0;
      
      void readLog() {
        if (logFile.existsSync()) {
          try {
            final lines = logFile.readAsLinesSync();
            if (lines.length > lastReadPos) {
              for (var i = lastReadPos; i < lines.length; i++) {
                log(lines[i]);
              }
              lastReadPos = lines.length;
            }
          } catch (_) {}
        }
      }

      // Start log reading in background
      Future.doWhile(() async {
        await Future.delayed(const Duration(milliseconds: 200));
        readLog();
        return running;
      });

      final process = await Process.run('powershell.exe', [
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        tempPsFile.path.replaceAll('/', '\\'),
      ]);

      running = false;
      await Future.delayed(const Duration(milliseconds: 200));
      readLog(); // Final read

      try {
        if (tempPsFile.existsSync()) tempPsFile.deleteSync();
      } catch (_) {}

      return process.exitCode == 0;
    } catch (e) {
      log('ERROR running uninstallation script: $e');
      return false;
    }
  }

  static Future<PrepResult> prepareFrameworkStaging(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async {
    log('Preparing temporary directory for framework staging...');
    final tempDir = Directory.systemTemp.createTempSync('omnimix_fw_install_').path;
    log('Staging path: $tempDir');

    // 1. Call framework preparation
    await framework.prepareStaging(gameDir, tempDir, log);

    // 2. Prepare backups
    final backupFiles = framework.getFilesToBackup(gameDir);
    final version = framework.getGameVersion(gameDir);
    
    for (final relPath in backupFiles) {
      final managerBackupPath = '$managerDir/backups/${framework.id}/v$version/$relPath';
      final tempBackupPath = '$tempDir/$relPath.v$version.bak';
      
      final managerBackupFile = File(managerBackupPath);
      final gameFile = File('$gameDir/$relPath');
      
      if (managerBackupFile.existsSync()) {
        log('Staging backup from manager cache: $relPath.v$version.bak');
        File(tempBackupPath).parent.createSync(recursive: true);
        managerBackupFile.copySync(tempBackupPath);
      } else if (gameFile.existsSync()) {
        log('Staging backup from original game path: $relPath.v$version.bak');
        File(tempBackupPath).parent.createSync(recursive: true);
        gameFile.copySync(tempBackupPath);
      }
    }

    // 3. Write managed signature inside tempDir
    if (framework.managedMarkerFile.isNotEmpty) {
      final markerFile = File('$tempDir/${framework.managedMarkerFile}');
      markerFile.parent.createSync(recursive: true);
      markerFile.writeAsStringSync(framework.version);
    }

    // 4. Scan tempDir to list added, linked, and backup files
    final added = <String>[];
    final links = <String>[];
    final backups = <String>[];
    final isDirectoryMap = <String, bool>{};

    final linksDecl = framework.getFilesToLink(gameDir);
    final backupSuffix = '.v$version.bak';

    bool isUnderLink(String relPath) {
      for (final link in linksDecl) {
        if (relPath == link || relPath.startsWith('$link/')) {
          return true;
        }
      }
      return false;
    }

    final tempDirObj = Directory(tempDir);
    final allEntities = tempDirObj.listSync(recursive: true);
    
    for (final entity in allEntities) {
      final relPath = entity.path
          .substring(tempDir.length + 1)
          .replaceAll('\\', '/');
      
      final isDir = entity is Directory;
      isDirectoryMap[relPath] = isDir;

      if (isUnderLink(relPath)) {
        if (linksDecl.contains(relPath)) {
          links.add(relPath);
        }
        continue;
      }

      if (relPath.endsWith(backupSuffix)) {
        final originalRelPath = relPath.substring(0, relPath.length - backupSuffix.length);
        backups.add(originalRelPath);
        continue;
      }

      if (!isDir) {
        added.add(relPath);
      }
    }

    return PrepResult(
      tempDir: tempDir,
      added: added,
      links: links,
      backups: backups,
      isDirectoryMap: isDirectoryMap,
      gameVersion: version,
    );
  }

  static Future<bool> executeFrameworkStaging(
    String gameDir,
    FrameworkDeclaration framework,
    PrepResult prepResult,
    void Function(String) log,
  ) async {
    try {
      final tempDir = prepResult.tempDir;
      final version = prepResult.gameVersion;
      final managerFwDir = frameworkExtractDir(framework);
      final managerBackupDir = '$managerDir/backups/${framework.id}/v$version';

      // 1. Move link files from tempDir to managerFwDir
      log('Writing link files to framework manager cache...');
      for (final relPath in prepResult.links) {
        final srcEntity = prepResult.isDirectoryMap[relPath] == true
            ? Directory('$tempDir/$relPath')
            : File('$tempDir/$relPath');
        final destEntityPath = '$managerFwDir/$relPath';

        if (srcEntity.existsSync()) {
          if (prepResult.isDirectoryMap[relPath] == true) {
            final d = Directory(destEntityPath);
            if (d.existsSync()) d.deleteSync(recursive: true);
            d.parent.createSync(recursive: true);
            (srcEntity as Directory).renameSync(destEntityPath);
          } else {
            final f = File(destEntityPath);
            if (f.existsSync()) f.deleteSync();
            f.parent.createSync(recursive: true);
            (srcEntity as File).renameSync(destEntityPath);
          }
        }
      }

      // 2. Move backup files to managerBackupDir
      log('Writing backup files to framework backups...');
      for (final relPath in prepResult.backups) {
        final tempBackupFile = File('$tempDir/$relPath.v$version.bak');
        final destBackupFile = File('$managerBackupDir/$relPath.v$version.bak');

        if (tempBackupFile.existsSync()) {
          if (!destBackupFile.existsSync()) {
            destBackupFile.parent.createSync(recursive: true);
            tempBackupFile.renameSync(destBackupFile.path);
          } else {
            tempBackupFile.deleteSync();
          }
        }
      }

      // 3. Write batch scripts inside tempDir
      final batchGen = BatchScriptGenerator();
      final installScriptContent = batchGen.generateInstallScript(
        gameDir: gameDir,
        tempDir: tempDir,
        managerModsDir: managerFwDir,
        addedFiles: prepResult.added,
        linkFiles: prepResult.links,
        isDirectoryMap: prepResult.isDirectoryMap,
      );
      final uninstallScriptContent = batchGen.generateUninstallScript(
        gameDir: gameDir,
        backupDir: managerBackupDir,
        addedFiles: prepResult.added,
        linkFiles: prepResult.links,
        backupFiles: prepResult.backups,
        isDirectoryMap: prepResult.isDirectoryMap,
        gameVersion: version,
      );

      final installScriptFile = File('$tempDir/install${batchGen.extension}');
      installScriptFile.writeAsStringSync(installScriptContent);
      log('Generated installation script: ${installScriptFile.path}');

      final uninstallScriptFile = File('$tempDir/uninstall${batchGen.extension}');
      uninstallScriptFile.writeAsStringSync(uninstallScriptContent);
      log('Generated uninstallation script: ${uninstallScriptFile.path}');

      // 4. Save uninstall script to manager folder for future use
      final persistentUninstallFile = File('$managerFwDir/uninstall${batchGen.extension}');
      persistentUninstallFile.parent.createSync(recursive: true);
      persistentUninstallFile.writeAsStringSync(uninstallScriptContent);

      // 5. Save instance info
      recordInstalledVersion(framework.id, framework.version);

      return true;
    } catch (e, st) {
      log('ERROR executing framework staging: $e\n$st');
      return false;
    }
  }
}

class PrepResult {
  final String tempDir;
  final List<String> added;
  final List<String> links;
  final List<String> backups;
  final Map<String, bool> isDirectoryMap;
  final String gameVersion;

  const PrepResult({
    required this.tempDir,
    required this.added,
    required this.links,
    required this.backups,
    required this.isDirectoryMap,
    required this.gameVersion,
  });
}
