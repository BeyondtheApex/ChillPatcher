import 'package:flutter/material.dart';
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

  const FrameworkDeclaration({
    required this.id,
    required this.name,
    required this.version,
    required this.archiveName,
    required this.filesToLink,
    required this.dirsToLink,
    required this.dirsToCreate,
  });
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

  /// "server" = backend-managed playback (supports playlist/queue offline mgmt)
  /// "client" = game-managed playback (minimal profile)
  final String mode;

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
    required this.mode,
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

  Future<void> onUndeploy(
    String gameDir,
    void Function(String) log,
  ) async {}

  Widget buildSettingsWidget(
    BuildContext context,
    Map<String, dynamic> currentSettings,
    void Function(Map<String, dynamic> newSettings) onSave,
  ) {
    return const SizedBox.shrink();
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
  ),
];

final List<ModDeclaration> modCatalog = registeredMods;


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
