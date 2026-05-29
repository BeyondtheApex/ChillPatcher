import 'dart:io';
import 'package:flutter/services.dart' show rootBundle;
import 'package:archive/archive.dart';
import 'package:file_picker/file_picker.dart';
import '../models/mod_manifest.dart';
import 'logger.dart';

enum BepInExStatus { notInstalled, managed, unmanaged }

enum ModStatus { notInstalled, installed }

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
      GuiLogger().error('selectDirectory failed', e);
    }
    return null;
  }

  /// Verifies if the selected folder is a valid game directory based on signature files.
  static bool verifyGameDirectory(String path, GameDeclaration game) {
    if (path.isEmpty) return false;
    final dir = Directory(path);
    if (!dir.existsSync()) return false;

    for (final sigFile in game.signatureFiles) {
      final fullPath = '$path/$sigFile';
      if (!FileSystemEntity.identicalSync(fullPath, fullPath)) {
        // Fallback check
        if (!File(fullPath).existsSync() && !Directory(fullPath).existsSync()) {
          return false;
        }
      } else {
        if (!File(fullPath).existsSync() && !Directory(fullPath).existsSync()) {
          return false;
        }
      }
    }
    return true;
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
      log('BepInEx deployment completed successfully.');
      return true;
    } catch (e, st) {
      log('FATAL ERROR during BepInEx deployment: $e');
      GuiLogger().error('deployBepInEx failed', e, st);
      return false;
    }
  }

  /// Deploy Mod.
  static Future<bool> deployMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log,
  ) async {
    if (mod.installsToGameRoot) {
      return deployRootMod(gameDir, mod, log);
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

      log('${mod.name} deployment completed successfully.');
      return true;
    } catch (e, st) {
      log('FATAL ERROR during mod deployment: $e');
      GuiLogger().error('deployMod failed', e, st);
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
    void Function(String) log,
  ) async {
    if (mod.installsToGameRoot) {
      return undeployRootMod(gameDir, mod, log);
    }

    try {
      log('Starting ${mod.name} undeployment...');
      final linkPath = '$gameDir/BepInEx/plugins/${mod.folderName}';

      _deleteLinkSafely(linkPath);
      log('  Removed mod directory symlink: BepInEx/plugins/${mod.folderName}');

      log('${mod.name} undeployment complete.');
      return true;
    } catch (e) {
      log('ERROR during mod undeployment: $e');
      return false;
    }
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
    void Function(String) log,
  ) async {
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
    void Function(String) log,
  ) async {
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
        if (!wasManaged &&
            existing.existsSync() &&
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
      log('${mod.name} deployment completed successfully.');
      return true;
    } catch (e, st) {
      log('FATAL ERROR during root mod deployment: $e');
      GuiLogger().error('deployRootMod failed', e, st);
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
      GuiLogger().error('Symlink failed for $linkPath: ${res.stderr}');
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
      GuiLogger().error(
        'Directory symlink failed for $linkPath: ${res.stderr}',
      );
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
      GuiLogger().error('Elevated PowerShell failed', e);
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
}
