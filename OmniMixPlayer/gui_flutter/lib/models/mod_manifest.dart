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
    required this.mode,
  });

  bool get usesFramework =>
      targetFramework != null && targetFramework!.isNotEmpty;
  bool get installsToGameRoot =>
      rootFilesToLink.isNotEmpty || rootDirsToLink.isNotEmpty;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Declarative Catalog Definitions
// ─────────────────────────────────────────────────────────────────────────────

final List<GameDeclaration> gameCatalog = [
  const GameDeclaration(
    id: 'chill_with_you',
    name: 'Chill With You',
    exeName: 'Chill With You.exe',
    signatureFiles: ['Chill With You.exe', 'Chill With You_Data'],
    supportedFrameworks: ['bepinex_5'],
    supportedMods: ['chill_patcher'],
    websiteUrl: 'https://store.steampowered.com/app/3361180',
    coverAssetPath: 'assets/covers/chill_with_you.png',
  ),
  const GameDeclaration(
    id: 'forza_horizon_6',
    name: 'Forza Horizon 6',
    exeName: 'forzahorizon6.exe',
    signatureFiles: ['forzahorizon6.exe'],
    supportedFrameworks: [],
    supportedMods: ['fh6_omni_bridge'],
    websiteUrl: 'https://forza.net',
    coverAssetPath: 'assets/covers/forza_horizon_6.png',
  ),
];

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

final List<ModDeclaration> modCatalog = [
  const ModDeclaration(
    id: 'chill_patcher',
    name: 'ChillPatcher',
    version: '1.0.0',
    archiveName: 'ChillPatcher.zip',
    targetFramework: 'bepinex_5',
    folderName: 'ChillPatcher',
    mode: 'client',
  ),
  const ModDeclaration(
    id: 'fh6_omni_bridge',
    name: 'Forza Horizon 6 Omni Bridge',
    version: '1.0.0',
    archiveName: 'FH6OmniBridge.zip',
    folderName: 'fh6-omnimix',
    rootFilesToLink: ['version.dll', 'OmniPcmShared.dll'],
    mode: 'server',
  ),
];

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
