import 'dart:convert';
import 'dart:io';
import 'package:flutter/services.dart' show rootBundle;
import 'package:archive/archive.dart';
import 'package:file_picker/file_picker.dart';
import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';import 'port_file.dart';

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
    if (gameDir.isEmpty) return BepInExStatus.notInstalled;

    final winhttp = File('$gameDir/winhttp.dll');
    final coreDll = File('$gameDir/BepInEx/core/BepInEx.dll');
    final managedMarker = File('$gameDir/BepInEx/.omnimix_managed');

    final exists = winhttp.existsSync() || coreDll.existsSync();
    if (exists) {
      if (managedMarker.existsSync()) {
        return BepInExStatus.managed;
      } else {
        return BepInExStatus.unmanaged;
      }
    }
    return BepInExStatus.notInstalled;
  }

  /// Check Mod status in the game directory.
  static ModStatus checkModStatus(String gameDir, String folderName) {
    if (gameDir.isEmpty) return ModStatus.notInstalled;

    final modDir = Directory('$gameDir/BepInEx/plugins/$folderName');
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

  /// Deploy BepInEx loader.
  static Future<bool> deployBepInEx(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async {
    try {
      log('Starting BepInEx deployment...');
      final managerDirObj = Directory(managerDir);
      if (!managerDirObj.existsSync()) {
        managerDirObj.createSync(recursive: true);
      }

      // 1. Copy BepInEx Zip asset to local cache and extract
      final localZipPath = '$managerDir/${framework.archiveName}';
      log('Loading ${framework.archiveName} from assets...');

      try {
        final byteData = await rootBundle.load(
          'assets/${framework.archiveName}',
        );
        final localZipFile = File(localZipPath);
        await localZipFile.writeAsBytes(
          byteData.buffer.asUint8List(
            byteData.offsetInBytes,
            byteData.lengthInBytes,
          ),
        );
      } catch (e) {
        log('ERROR loading asset ${framework.archiveName}: $e');
        return false;
      }

      final extractPath = bepinexExtractDir;
      log('Extracting loader to local AppData cache...');
      final extractDir = Directory(extractPath);
      if (extractDir.existsSync()) {
        extractDir.deleteSync(recursive: true);
      }
      extractDir.createSync(recursive: true);

      try {
        final bytes = File(localZipPath).readAsBytesSync();
        final archive = ZipDecoder().decodeBytes(bytes);
        _extractArchiveReplacingFiles(archive, extractPath);
      } catch (e) {
        log('ERROR extracting loader: $e');
        return false;
      }

      // Clean up temporary zip
      try {
        File(localZipPath).deleteSync();
      } catch (_) {}

      log('Extraction complete.');

      // 2. Create physical BepInEx directories in game path
      log('Creating directories in game folder...');
      for (final relativeDir in framework.dirsToCreate) {
        final dirPath = '$gameDir/$relativeDir';
        final d = Directory(dirPath);
        if (!d.existsSync()) {
          d.createSync(recursive: true);
          log('  Created: $relativeDir/');
        }
      }

      // 3. Link core directories as symlinks
      log('Linking BepInEx core folders...');
      for (final relativeDir in framework.dirsToLink) {
        final linkPath = '$gameDir/$relativeDir';
        final targetPath = '$extractPath/$relativeDir';

        log('  Directory symlink: $relativeDir -> manager/$relativeDir');
        final success = await _createDirectorySymlink(linkPath, targetPath);
        if (!success) {
          log('ERROR creating directory symlink for $relativeDir');
          return false;
        }
      }

      // 4. Link/copy root files (winhttp.dll, doorstop_config.ini)
      log('Linking core loader files...');
      for (final relativeFile in framework.filesToLink) {
        final linkPath = '$gameDir/$relativeFile';
        final targetPath = '$extractPath/$relativeFile';

        log('  File symlink: $relativeFile -> manager/$relativeFile');
        final success = await _createFileSymlink(linkPath, targetPath);
        if (!success) {
          log('ERROR placing file: $relativeFile');
          return false;
        }
      }

      // 5. Write managed signature
      File(
        '$gameDir/BepInEx/.omnimix_managed',
      ).writeAsStringSync(framework.version);
      recordInstalledVersion(framework.id, framework.version);
      log('BepInEx deployment completed successfully.');
      return true;
    } catch (e, st) {
      log('FATAL ERROR during BepInEx deployment: $e');
      return false;
    }
  }

  /// Deploy Mod.
  static Future<bool> deployMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    int? backendPort,
    Future<void> Function(String, bool)? onPortFileDirChanged,
    Map<String, dynamic> customSettings = const {},
  }) async {
    if (mod.installsToGameRoot) {
      return deployRootMod(
        gameDir,
        mod,
        log,
        backendPort: backendPort,
        onPortFileDirChanged: onPortFileDirChanged,
        customSettings: customSettings,
      );
    }

    try {
      log('Starting ${mod.name} deployment...');
      final managerDirObj = Directory(managerDir);
      if (!managerDirObj.existsSync()) {
        managerDirObj.createSync(recursive: true);
      }

      // 1. Copy Mod Zip asset to local cache and extract
      final localZipPath = '$managerDir/${mod.archiveName}';
      log('Loading ${mod.archiveName} from assets...');

      try {
        final byteData = await rootBundle.load('assets/${mod.archiveName}');
        final localZipFile = File(localZipPath);
        await localZipFile.writeAsBytes(
          byteData.buffer.asUint8List(
            byteData.offsetInBytes,
            byteData.lengthInBytes,
          ),
        );
      } catch (e) {
        log('ERROR loading asset ${mod.archiveName}: $e');
        return false;
      }

      final extractPath = '$modsExtractDir/${mod.folderName}';
      log('Extracting mod to local AppData cache...');
      final extractDir = Directory(extractPath);
      if (extractDir.existsSync()) {
        extractDir.deleteSync(recursive: true);
      }
      extractDir.createSync(recursive: true);

      try {
        final bytes = File(localZipPath).readAsBytesSync();
        final archive = ZipDecoder().decodeBytes(bytes);
        _extractArchiveReplacingFiles(archive, extractPath);
      } catch (e) {
        log('ERROR extracting mod: $e');
        return false;
      }

      // Clean up temporary zip
      try {
        File(localZipPath).deleteSync();
      } catch (_) {}

      log('Extraction complete.');

      // 2. Link mod folder as symlink inside game BepInEx/plugins/
      log('Creating directory symlink inside plugins...');
      final linkPath = '$gameDir/BepInEx/plugins/${mod.folderName}';

      log(
        '  Directory symlink: BepInEx/plugins/${mod.folderName} -> manager/mods/${mod.folderName}',
      );
      final success = await _createDirectorySymlink(linkPath, extractPath);
      if (!success) {
        log('ERROR creating directory symlink');
        return false;
      }

      recordInstalledVersion(mod.id, mod.version);
      // Instance registration — reuse existing ID if reinstalling, generate new otherwise
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

      await mod.onDeploy(gameDir, log, customSettings);

      log(
        '${mod.name} deployment completed successfully. Instance: $instanceId',
      );
      return true;
    } catch (e, st) {
      log('FATAL ERROR during mod deployment: $e');
      return false;
    }
  }

  /// Remove BepInEx files (only if managed) and clean up.
  static Future<bool> undeployBepInEx(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async {
    try {
      log('Starting BepInEx undeployment...');

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
      final marker = File('$gameDir/BepInEx/.omnimix_managed');
      if (marker.existsSync()) marker.deleteSync();

      // 4. Try cleaning up empty directories
      log('Cleaning up empty folders...');
      final pluginsDir = Directory('$gameDir/BepInEx/plugins');
      if (pluginsDir.existsSync() && pluginsDir.listSync().isEmpty) {
        pluginsDir.deleteSync();
        log('  Deleted empty: BepInEx/plugins/');
      }
      final patchersDir = Directory('$gameDir/BepInEx/patchers');
      if (patchersDir.existsSync() && patchersDir.listSync().isEmpty) {
        patchersDir.deleteSync();
        log('  Deleted empty: BepInEx/patchers/');
      }
      final bepinexDir = Directory('$gameDir/BepInEx');
      if (bepinexDir.existsSync() && bepinexDir.listSync().isEmpty) {
        bepinexDir.deleteSync();
        log('  Deleted empty: BepInEx/');
      }

      removeVersionRecord(framework.id);
      log('BepInEx undeployment complete.');
      return true;
    } catch (e) {
      log('ERROR during BepInEx undeployment: $e');
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
      final linkPath = '$gameDir/BepInEx/plugins/${mod.folderName}';

      _deleteLinkSafely(linkPath);
      log('  Removed mod directory symlink: BepInEx/plugins/${mod.folderName}');

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

  /// Deploy a root-level native mod using links and backups.
  static Future<bool> deployRootMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    int? backendPort,
    Future<void> Function(String, bool)? onPortFileDirChanged,
    Map<String, dynamic> customSettings = const {},
  }) async {
    try {
      log('Starting ${mod.name} deployment...');
      final managerDirObj = Directory(managerDir);
      if (!managerDirObj.existsSync()) {
        managerDirObj.createSync(recursive: true);
      }

      final localZipPath = '$managerDir/${mod.archiveName}';
      log('Loading ${mod.archiveName} from assets...');

      try {
        final byteData = await rootBundle.load('assets/${mod.archiveName}');
        final localZipFile = File(localZipPath);
        await localZipFile.writeAsBytes(
          byteData.buffer.asUint8List(
            byteData.offsetInBytes,
            byteData.lengthInBytes,
          ),
        );
      } catch (e) {
        log('ERROR loading asset ${mod.archiveName}: $e');
        return false;
      }

      final extractPath = '$rootModsExtractDir/${mod.id}';
      log('Extracting mod to local AppData link source...');
      final extractDir = Directory(extractPath);
      if (extractDir.existsSync()) {
        extractDir.deleteSync(recursive: true);
      }
      extractDir.createSync(recursive: true);

      try {
        final bytes = File(localZipPath).readAsBytesSync();
        final archive = ZipDecoder().decodeBytes(bytes);
        _extractArchiveReplacingFiles(archive, extractPath);
      } catch (e) {
        log('ERROR extracting mod: $e');
        return false;
      }

      try {
        File(localZipPath).deleteSync();
      } catch (_) {}

      final marker = File('$gameDir/.omnimix_mods/${mod.id}.managed');
      final wasManaged = marker.existsSync();
      final backupDir = Directory('$gameDir/.omnimix_backup/${mod.id}');
      if (!backupDir.existsSync()) {
        backupDir.createSync(recursive: true);
      }

      log('Linking root files...');
      for (final relativeFile in mod.rootFilesToLink) {
        final targetPath = '$extractPath/$relativeFile';
        final linkPath = '$gameDir/$relativeFile';
        if (!File(targetPath).existsSync()) {
          log('ERROR missing packaged file: $relativeFile');
          return false;
        }

        final existing = File(linkPath);
        final backupPath = '${backupDir.path}/$relativeFile';
        final isNoBackup = mod.rootFilesNoBackup.contains(relativeFile);
        if (!wasManaged &&
            existing.existsSync() &&
            !isNoBackup &&
            !File(backupPath).existsSync()) {
          File(backupPath).parent.createSync(recursive: true);
          existing.copySync(backupPath);
          log('  Backed up existing: $relativeFile');
        }

        log(
          '  Symlink: $relativeFile -> manager/root_mods/${mod.id}/$relativeFile',
        );
        final success = await _createFileSymlink(linkPath, targetPath);
        if (!success) {
          final backup = File(backupPath);
          if (backup.existsSync() && !existing.existsSync()) {
            backup.copySync(linkPath);
            log('  Restored backup after link failure: $relativeFile');
          }
          log('ERROR linking file: $relativeFile');
          return false;
        }
      }

      for (final relativeDir in mod.rootDirsToLink) {
        final targetPath = '$extractPath/$relativeDir';
        final linkPath = '$gameDir/$relativeDir';
        if (!Directory(targetPath).existsSync()) {
          log('ERROR missing packaged directory: $relativeDir');
          return false;
        }

        final existing = Directory(linkPath);
        final backupPath = '${backupDir.path}/$relativeDir';
        if (!wasManaged &&
            existing.existsSync() &&
            !Directory(backupPath).existsSync()) {
          Directory(backupPath).parent.createSync(recursive: true);
          existing.renameSync(backupPath);
          log('  Backed up existing directory: $relativeDir');
        }

        final success = await _createDirectorySymlink(linkPath, targetPath);
        if (!success) {
          final backup = Directory(backupPath);
          if (backup.existsSync() && !existing.existsSync()) {
            backup.renameSync(linkPath);
            log('  Restored backup directory after link failure: $relativeDir');
          }
          log('ERROR creating directory symlink for $relativeDir');
          return false;
        }
      }

      marker.parent.createSync(recursive: true);
      marker.writeAsStringSync(mod.version);
      recordInstalledVersion(mod.id, mod.version);
      // Instance registration — reuse existing ID if reinstalling, generate new otherwise
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

      await mod.onDeploy(gameDir, log, customSettings);

      log(
        '${mod.name} deployment completed successfully. Instance: $instanceId',
      );
      return true;
    } catch (e, st) {
      log('FATAL ERROR during root mod deployment: $e');
      return false;
    }
  }

  /// Extract archive files into [destinationPath], replacing existing source files.
  static void _extractArchiveReplacingFiles(
    Archive archive,
    String destinationPath,
  ) {
    for (final file in archive) {
      final outPath = '$destinationPath/${file.name}';
      if (file.isDirectory) {
        Directory(outPath).createSync(recursive: true);
        continue;
      }

      final outFile = File(outPath);
      outFile.parent.createSync(recursive: true);
      if (outFile.existsSync()) {
        outFile.deleteSync();
      }

      outFile.writeAsBytesSync(file.content);
    }
  }

  /// Link a file using a Windows symbolic link (mklink).
  /// If Developer Mode is off, retries through UAC elevation.
  static Future<bool> _createFileSymlink(
    String linkPath,
    String targetPath,
  ) async {
    _deleteLinkSafely(linkPath);

    final winLinkPath = linkPath.replaceAll('/', '\\');
    final winTargetPath = targetPath.replaceAll('/', '\\');
    final res = await Process.run('cmd.exe', [
      '/c',
      'mklink',
      winLinkPath,
      winTargetPath,
    ]);
    if (res.exitCode == 0) {
      return true;
    }

    final script =
        '''
\$ErrorActionPreference = 'Stop'
\$link = '${_escapePowerShellSingleQuoted(winLinkPath)}'
\$target = '${_escapePowerShellSingleQuoted(winTargetPath)}'
if (Test-Path -LiteralPath \$link) {
  Remove-Item -LiteralPath \$link -Force
}
New-Item -ItemType SymbolicLink -Path \$link -Target \$target -Force | Out-Null
''';

    final elevated = await _runPowerShellElevated(script);
    if (!elevated) {
      }
    return elevated;
  }

  /// Link a directory using a Windows symbolic link (mklink /D).
  /// If Developer Mode is off, retries through UAC elevation.
  static Future<bool> _createDirectorySymlink(
    String linkPath,
    String targetPath,
  ) async {
    _deleteLinkSafely(linkPath);

    final winLinkPath = linkPath.replaceAll('/', '\\');
    final winTargetPath = targetPath.replaceAll('/', '\\');
    final res = await Process.run('cmd.exe', [
      '/c',
      'mklink',
      '/D',
      winLinkPath,
      winTargetPath,
    ]);
    if (res.exitCode == 0) {
      return true;
    }

    final script =
        '''
\$ErrorActionPreference = 'Stop'
\$link = '${_escapePowerShellSingleQuoted(winLinkPath)}'
\$target = '${_escapePowerShellSingleQuoted(winTargetPath)}'
if (Test-Path -LiteralPath \$link) {
  Remove-Item -LiteralPath \$link -Force -Recurse
}
New-Item -ItemType SymbolicLink -Path \$link -Target \$target -Force | Out-Null
''';

    final elevated = await _runPowerShellElevated(script);
    if (!elevated) {
      }
    return elevated;
  }

  static Future<bool> _runPowerShellElevated(String script) async {
    if (!Platform.isWindows) return false;

    final scriptFile = File(
      '${Directory.systemTemp.path}/omnimix_elevated_${DateTime.now().microsecondsSinceEpoch}.ps1',
    );
    scriptFile.writeAsStringSync(script);

    try {
      final scriptPath = scriptFile.path.replaceAll('/', '\\');
      final psScriptPath = _escapePowerShellSingleQuoted(scriptPath);
      final command =
          "\$p = Start-Process -FilePath 'powershell.exe' "
          "-ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','$psScriptPath' "
          "-Verb RunAs -Wait -WindowStyle Hidden -PassThru; exit \$p.ExitCode";
      final result = await Process.run('powershell.exe', [
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-Command',
        command,
      ]);
      return result.exitCode == 0;
    } catch (e) {
      return false;
    } finally {
      try {
        if (scriptFile.existsSync()) scriptFile.deleteSync();
      } catch (_) {}
    }
  }

  static String _escapePowerShellSingleQuoted(String value) {
    return value.replaceAll("'", "''");
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
}
