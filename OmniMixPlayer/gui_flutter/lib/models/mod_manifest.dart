import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:archive/archive.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../mods/registry.dart';
import '../games/registry.dart';

class GameDeclaration {
  final String id;
  final String name;
  final String exeName;
  final List<String> signatureFiles;
  final List<String> supportedFrameworks;
  final List<String> supportedMods;
  final String? websiteUrl;
  final String? coverAssetPath;

  const GameDeclaration({
    required this.id,
    required this.name,
    required this.exeName,
    required this.signatureFiles,
    required this.supportedFrameworks,
    required this.supportedMods,
    this.websiteUrl,
    this.coverAssetPath,
  });
}

class FrameworkDeclaration {
  final String id;
  final String name;
  final String version;
  final String archiveName;
  final List<String> filesToLink;
  final List<String> dirsToLink;
  final List<String> dirsToCreate;
  final List<String> statusFiles;
  final List<String> statusDirs;
  final String managedMarkerFile;

  const FrameworkDeclaration({
    required this.id,
    required this.name,
    required this.version,
    required this.archiveName,
    required this.filesToLink,
    required this.dirsToLink,
    required this.dirsToCreate,
    this.statusFiles = const [],
    this.statusDirs = const [],
    this.managedMarkerFile = '',
  });

  List<String> getFilesToLink(String gameDir) {
    return [...filesToLink, ...dirsToLink];
  }

  List<String> getFilesToAdd(String gameDir) {
    return [];
  }

  List<String> getFilesToBackup(String gameDir) {
    return [];
  }

  String getGameVersion(String gameDir) {
    return '1.0.0';
  }

  bool verifyInstallation(String gameDir) {
    final sFiles = statusFiles.isNotEmpty ? statusFiles : filesToLink;
    final sDirs = statusDirs.isNotEmpty ? statusDirs : dirsToLink;

    for (final relFile in sFiles) {
      if (File('$gameDir/$relFile').existsSync()) return true;
    }
    for (final relDir in sDirs) {
      if (Directory('$gameDir/$relDir').existsSync()) return true;
    }
    return false;
  }

  Future<void> prepareStaging(
    String gameDir,
    String tempDir,
    void Function(String) log,
  ) async {
    log('Loading $archiveName from assets...');
    final localAppData = Platform.environment['LOCALAPPDATA'] ?? '';
    final managerDir = localAppData.isEmpty
        ? '${Directory.systemTemp.path}/omnimix_mod_manager'
        : '$localAppData/OmniMixPlayer/mod_manager';
    final localZipPath = '$managerDir/$archiveName';

    try {
      final byteData = await rootBundle.load('assets/$archiveName');
      final localZipFile = File(localZipPath);
      localZipFile.parent.createSync(recursive: true);
      await localZipFile.writeAsBytes(
        byteData.buffer.asUint8List(
          byteData.offsetInBytes,
          byteData.lengthInBytes,
        ),
      );
    } catch (e) {
      log('ERROR loading asset $archiveName: $e');
      rethrow;
    }

    log('Extracting $name zip files to staging...');
    try {
      final bytes = File(localZipPath).readAsBytesSync();
      final archive = ZipDecoder().decodeBytes(bytes);

      final targetExtractDir = Directory(tempDir);
      if (!targetExtractDir.existsSync()) {
        targetExtractDir.createSync(recursive: true);
      }

      for (final file in archive) {
        final outPath = '$tempDir/${file.name}';
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

      for (final relativeDir in dirsToCreate) {
        final d = Directory('$tempDir/$relativeDir');
        if (!d.existsSync()) {
          d.createSync(recursive: true);
        }
      }
    } catch (e) {
      log('ERROR extracting $name: $e');
      rethrow;
    } finally {
      try {
        final f = File(localZipPath);
        if (f.existsSync()) f.deleteSync();
      } catch (_) {}
    }
  }
}

class ModDeclaration {
  final String id;
  final String name;
  final String version;
  final String archiveName;
  final String? targetFramework;
  final String folderName;
  final List<String> rootFilesToLink;
  final List<String> rootDirsToLink;
  final List<String> rootFilesNoBackup;
  final String pluginTargetDir;

  /// "server" = backend-managed playback (supports playlist/queue offline mgmt)
  /// "client" = game-managed playback (minimal profile)
  final String mode;

  /// Instance capabilities declared by the mod.
  /// Passed to the backend during registration so the instance menu
  /// can show control buttons immediately, before the mod connects.
  final InstanceCapabilities? capabilities;

  const ModDeclaration({
    required this.id,
    required this.name,
    required this.version,
    required this.archiveName,
    this.targetFramework,
    required this.folderName,
    this.rootFilesToLink = const [],
    this.rootDirsToLink = const [],
    this.rootFilesNoBackup = const [],
    this.pluginTargetDir = 'BepInEx/plugins',
    required this.mode,
    this.capabilities,
  });

  bool get usesFramework =>
      targetFramework != null && targetFramework!.isNotEmpty;
  bool get installsToGameRoot =>
      rootFilesToLink.isNotEmpty || rootDirsToLink.isNotEmpty;

  // Mod-level settings & custom hooks
  bool get hasSettings => false;

  Future<void> onDeploy(
    String gameDir,
    void Function(String) log,
    Map<String, dynamic> settings,
  ) async {}

  Future<void> onUndeploy(String gameDir, void Function(String) log) async {}

  Widget buildSettingsWidget(
    BuildContext context,
    Map<String, dynamic> currentSettings,
    void Function(Map<String, dynamic> newSettings) onSave,
  ) {
    return const SizedBox.shrink();
  }

  bool verifyInstallation(String gameDir) {
    if (installsToGameRoot) {
      final marker = File('$gameDir/.omnimix_mods/$id.managed');
      if (marker.existsSync()) return true;

      for (final relFile in rootFilesToLink) {
        if (File('$gameDir/$relFile').existsSync()) return true;
      }
      for (final relDir in rootDirsToLink) {
        if (Directory('$gameDir/$relDir').existsSync()) return true;
      }
      return false;
    } else {
      final modDir = Directory('$gameDir/$pluginTargetDir/$folderName');
      return modDir.existsSync() || File(modDir.path).existsSync();
    }
  }

  List<String> getFilesToLink(String gameDir) {
    if (installsToGameRoot) {
      return rootFilesToLink;
    }
    return ['$pluginTargetDir/$folderName'];
  }

  List<String> getFilesToAdd(String gameDir) {
    return [];
  }

  List<String> getFilesToBackup(String gameDir) {
    return [];
  }

  String getGameVersion(String gameDir) {
    return '1.0.0';
  }

  Future<void> prepareStaging(
    String gameDir,
    String tempDir,
    void Function(String) log,
    Map<String, dynamic> settings,
  ) async {
    await extractZipToStaging(tempDir, log);
  }

  Future<void> extractZipToStaging(
    String tempDir,
    void Function(String) log,
  ) async {
    final localAppData = Platform.environment['LOCALAPPDATA'] ?? '';
    final managerDir = localAppData.isEmpty
        ? '${Directory.systemTemp.path}/omnimix_mod_manager'
        : '$localAppData/OmniMixPlayer/mod_manager';

    final localZipPath = '$managerDir/$archiveName';
    log('Loading $archiveName from assets...');

    try {
      final byteData = await rootBundle.load('assets/$archiveName');
      final localZipFile = File(localZipPath);
      localZipFile.parent.createSync(recursive: true);
      await localZipFile.writeAsBytes(
        byteData.buffer.asUint8List(
          byteData.offsetInBytes,
          byteData.lengthInBytes,
        ),
      );
    } catch (e) {
      log('ERROR loading asset $archiveName: $e');
      rethrow;
    }

    log('Extracting zip files to staging...');
    try {
      final bytes = File(localZipPath).readAsBytesSync();
      final archive = ZipDecoder().decodeBytes(bytes);

      final targetExtractPath = installsToGameRoot
          ? tempDir
          : '$tempDir/$pluginTargetDir/$folderName';

      final targetExtractDir = Directory(targetExtractPath);
      if (targetExtractDir.existsSync()) {
        targetExtractDir.deleteSync(recursive: true);
      }
      targetExtractDir.createSync(recursive: true);

      for (final file in archive) {
        final outPath = '$targetExtractPath/${file.name}';
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
    } catch (e) {
      log('ERROR extracting mod: $e');
      rethrow;
    } finally {
      try {
        final f = File(localZipPath);
        if (f.existsSync()) f.deleteSync();
      } catch (_) {}
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Declarative Catalog Definitions
// ─────────────────────────────────────────────────────────────────────────────

final List<GameDeclaration> gameCatalog = registeredGames;

final List<FrameworkDeclaration> frameworkCatalog = [
  const FrameworkDeclaration(
    id: 'bepinex_5',
    name: 'BepInEx',
    version: '5.4.23.5',
    archiveName: 'BepInEx_win_x64_5.4.23.5.zip',
    filesToLink: ['winhttp.dll', 'doorstop_config.ini'],
    dirsToLink: ['BepInEx/core'],
    dirsToCreate: ['BepInEx', 'BepInEx/plugins', 'BepInEx/patchers'],
    statusFiles: ['winhttp.dll'],
    statusDirs: ['BepInEx/core'],
    managedMarkerFile: 'BepInEx/.omnimix_managed',
  ),
];

final List<ModDeclaration> modCatalog = registeredMods;

FrameworkDeclaration? frameworkById(String id) {
  for (final framework in frameworkCatalog) {
    if (framework.id == id) return framework;
  }
  return null;
}

ModDeclaration? modById(String id) {
  for (final mod in modCatalog) {
    if (mod.id == id) return mod;
  }
  return null;
}

List<FrameworkDeclaration> frameworksForGame(GameDeclaration game) {
  return game.supportedFrameworks
      .map(frameworkById)
      .whereType<FrameworkDeclaration>()
      .toList(growable: false);
}

List<ModDeclaration> modsForGame(GameDeclaration game) {
  return game.supportedMods
      .map(modById)
      .whereType<ModDeclaration>()
      .toList(growable: false);
}

FrameworkDeclaration? primaryFrameworkForGame(GameDeclaration game) {
  final frameworks = frameworksForGame(game);
  return frameworks.isEmpty ? null : frameworks.first;
}

ModDeclaration? primaryModForGame(GameDeclaration game) {
  final mods = modsForGame(game);
  return mods.isEmpty ? null : mods.first;
}

// ═══════════════════════════════════════════════════════════
//  Instance & Archive Models
// ═══════════════════════════════════════════════════════════

class InstalledInstance {
  final String instanceId;
  final String modId;
  final String mode;
  final String gameDir;
  final String gameName;
  final DateTime installedAt;

  const InstalledInstance({
    required this.instanceId,
    required this.modId,
    required this.mode,
    required this.gameDir,
    required this.gameName,
    required this.installedAt,
  });

  factory InstalledInstance.fromJson(Map<String, dynamic> json) {
    return InstalledInstance(
      instanceId: json['instanceId'] as String,
      modId: json['modId'] as String,
      mode: json['mode'] as String,
      gameDir: json['gameDir'] as String,
      gameName: json['gameName'] as String? ?? '',
      installedAt: DateTime.parse(json['installedAt'] as String),
    );
  }

  Map<String, dynamic> toJson() => {
    'instanceId': instanceId,
    'modId': modId,
    'mode': mode,
    'gameDir': gameDir,
    'gameName': gameName,
    'installedAt': installedAt.toIso8601String(),
  };

  bool get isServerMode => mode == 'server';
  bool get isClientMode => mode == 'client';
}

class ArchiveEntry {
  final String instanceId;
  final String modId;
  final String mode;
  final String gameDir;
  final String gameName;
  final String label;
  final DateTime archivedAt;

  const ArchiveEntry({
    required this.instanceId,
    required this.modId,
    required this.mode,
    required this.gameDir,
    required this.gameName,
    this.label = '',
    required this.archivedAt,
  });

  factory ArchiveEntry.fromJson(Map<String, dynamic> json) {
    return ArchiveEntry(
      instanceId: json['instanceId'] as String,
      modId: json['modId'] as String,
      mode: json['mode'] as String,
      gameDir: json['gameDir'] as String,
      gameName: json['gameName'] as String? ?? '',
      label: json['label'] as String? ?? '',
      archivedAt: DateTime.parse(json['archivedAt'] as String),
    );
  }

  Map<String, dynamic> toJson() => {
    'instanceId': instanceId,
    'modId': modId,
    'mode': mode,
    'gameDir': gameDir,
    'gameName': gameName,
    'label': label,
    'archivedAt': archivedAt.toIso8601String(),
  };

  String get displayName =>
      label.isNotEmpty ? label : '$gameName ($instanceId)';
}
