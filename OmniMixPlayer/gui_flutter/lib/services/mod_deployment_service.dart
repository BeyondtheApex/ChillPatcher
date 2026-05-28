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

  /// Triggers a native Windows Folder Picker using the file_picker package.
  static Future<String?> selectDirectory() async {
    try {
      final path = await FilePicker.getDirectoryPath(
        dialogTitle: 'Select Chill With You Game Directory',
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

  /// Deploy BepInEx loader.
  static Future<bool> deployBepInEx(
      String gameDir, FrameworkDeclaration framework, void Function(String) log) async {
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
        final byteData = await rootBundle.load('assets/${framework.archiveName}');
        final localZipFile = File(localZipPath);
        await localZipFile.writeAsBytes(
            byteData.buffer.asUint8List(byteData.offsetInBytes, byteData.lengthInBytes));
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

      // 3. Link core directories as junctions
      log('Linking BepInEx core folders...');
      for (final relativeDir in framework.dirsToLink) {
        final linkPath = '$gameDir/$relativeDir';
        final targetPath = '$extractPath/$relativeDir';

        _deleteLinkSafely(linkPath);

        log('  Junction: $relativeDir -> manager/$relativeDir');
        final winLinkPath = linkPath.replaceAll('/', '\\');
        final winTargetPath = targetPath.replaceAll('/', '\\');
        final linkRes = await Process.run('cmd.exe', ['/c', 'mklink', '/J', winLinkPath, winTargetPath]);
        if (linkRes.exitCode != 0) {
          log('ERROR creating junction for $relativeDir: ${linkRes.stderr}');
          return false;
        }
      }

      // 4. Link/copy root files (winhttp.dll, doorstop_config.ini)
      log('Linking core loader files...');
      for (final relativeFile in framework.filesToLink) {
        final linkPath = '$gameDir/$relativeFile';
        final targetPath = '$extractPath/$relativeFile';

        log('  Linking file: $relativeFile');
        final success = await _createFileLink(linkPath, targetPath);
        if (!success) {
          log('ERROR placing file: $relativeFile');
          return false;
        }
      }

      // 5. Write managed signature
      File('$gameDir/BepInEx/.omnimix_managed').writeAsStringSync(framework.version);
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
      String gameDir, ModDeclaration mod, void Function(String) log) async {
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
            byteData.buffer.asUint8List(byteData.offsetInBytes, byteData.lengthInBytes));
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

      // 2. Link mod folder as junction inside game BepInEx/plugins/
      log('Creating directory junction inside plugins...');
      final linkPath = '$gameDir/BepInEx/plugins/${mod.folderName}';
      
      _deleteLinkSafely(linkPath);

      log('  Junction: BepInEx/plugins/${mod.folderName} -> manager/mods/${mod.folderName}');
      final winLinkPath = linkPath.replaceAll('/', '\\');
      final winExtractPath = extractPath.replaceAll('/', '\\');
      final linkRes = await Process.run('cmd.exe', ['/c', 'mklink', '/J', winLinkPath, winExtractPath]);
      if (linkRes.exitCode != 0) {
        log('ERROR creating junction: ${linkRes.stderr}');
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
      String gameDir, FrameworkDeclaration framework, void Function(String) log) async {
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

      // 2. Remove core junction
      for (final relativeDir in framework.dirsToLink) {
        final dirPath = '$gameDir/$relativeDir';
        _deleteLinkSafely(dirPath);
        log('  Removed junction: $relativeDir');
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

  /// Remove Mod junction link.
  static Future<bool> undeployMod(
      String gameDir, ModDeclaration mod, void Function(String) log) async {
    try {
      log('Starting ${mod.name} undeployment...');
      final linkPath = '$gameDir/BepInEx/plugins/${mod.folderName}';

      _deleteLinkSafely(linkPath);
      log('  Removed mod junction link: BepInEx/plugins/${mod.folderName}');

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

  /// Delete a link/junction or a directory/file safely.
  /// Link(path).deleteSync() removes ONLY the junction point, not target files.
  static void _deleteLinkSafely(String path) {
    if (FileSystemEntity.isLinkSync(path)) {
      Link(path).deleteSync();
    } else if (Directory(path).existsSync()) {
      Directory(path).deleteSync(recursive: true);
    } else if (File(path).existsSync()) {
      File(path).deleteSync();
    }
  }

  /// Extract archive files into [destinationPath], replacing existing source files.
  static void _extractArchiveReplacingFiles(Archive archive, String destinationPath) {
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

  /// Link a file using a Windows Hardlink (mklink /H).
  /// Falls back to a physical File copy if across volumes.
  static Future<bool> _createFileLink(String linkPath, String targetPath) async {
    final f = File(linkPath);
    if (f.existsSync()) {
      f.deleteSync();
    }

    // Try creating a Windows Hardlink
    final winLinkPath = linkPath.replaceAll('/', '\\');
    final winTargetPath = targetPath.replaceAll('/', '\\');
    final res = await Process.run('cmd.exe', ['/c', 'mklink', '/H', winLinkPath, winTargetPath]);
    if (res.exitCode == 0) {
      return true;
    }

    // Fallback: Copy file
    try {
      File(targetPath).copySync(linkPath);
      return true;
    } catch (e) {
      GuiLogger().error('File copy fallback failed for $linkPath', e);
      return false;
    }
  }
}
