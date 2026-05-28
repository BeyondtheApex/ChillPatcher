class GameDeclaration {
  final String id;
  final String name;
  final String exeName;
  final List<String> signatureFiles;
  final List<String> supportedFrameworks;
  final List<String> supportedMods;

  const GameDeclaration({
    required this.id,
    required this.name,
    required this.exeName,
    required this.signatureFiles,
    required this.supportedFrameworks,
    required this.supportedMods,
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
  final String targetFramework;
  final String folderName;

  const ModDeclaration({
    required this.id,
    required this.name,
    required this.version,
    required this.archiveName,
    required this.targetFramework,
    required this.folderName,
  });
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
  ),
];
