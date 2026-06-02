/// Web stub for ModDeploymentService — no-op on web.
/// Mod deployment (extract zip, copy to game dir, manage BepInEx) is not
/// available in the browser — it requires filesystem access.
library mod_deployment_service_web;

import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';

/// Web stub: deployment features not available in browser.
class ModDeploymentService {
  static const String managerDir = '';
  static Future<String?> selectDirectory() async => null;

  static bool verifyGameDirectory(String path, GameDeclaration game) => false;

  static BepInExStatus checkBepInExStatus(String gameDir) =>
      BepInExStatus.notInstalled;

  static BepInExStatus checkFrameworkStatus(
    String gameDir,
    FrameworkDeclaration framework,
  ) => BepInExStatus.notInstalled;

  static ModStatus checkModStatus(
    String gameDir,
    String folderName, {
    String pluginTargetDir = 'BepInEx/plugins',
  }) =>
      ModStatus.notInstalled;

  static ModStatus checkRootModStatus(String gameDir, ModDeclaration mod) =>
      ModStatus.notInstalled;

  static Future<bool> undeployBepInEx(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async => false;

  static Future<bool> undeployMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    Future<void> Function(String, bool)? onPortFileDirChanged,
  }) async => false;

  static Future<bool> undeployRootMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    Future<void> Function(String, bool)? onPortFileDirChanged,
  }) async => false;

  static String? getRecordedGameDir(String modId) => null;

  static Future<Map<String, String>?> loadLatestModVersion() async => null;

  static Future<bool> refreshCachedModAssets({String? modArchivePath}) async =>
      false;

  static List<InstalledInstance> loadInstances() => [];

  static List<ArchiveEntry> listArchives() => [];

  static void removeInstance(String id) {}

  static dynamic findInstanceByDir(String path) => null;

  static String? getInstalledVersion(String id) => null;

  static void recordInstalledVersion(String id, String version) {}

  static Future<PrepResult> prepareInstallStaging(
    String gameDir,
    ModDeclaration mod,
    Map<String, dynamic> settings,
    void Function(String) log,
  ) async {
    throw UnimplementedError('Not supported on web');
  }

  static Future<bool> executeInstallStaging(
    String gameDir,
    ModDeclaration mod,
    PrepResult prepResult,
    void Function(String) log, {
    int? backendPort,
    Future<void> Function(String, bool)? onPortFileDirChanged,
  }) async => false;

  static Future<PrepResult> prepareFrameworkStaging(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async {
    throw UnimplementedError('Not supported on web');
  }

  static Future<bool> executeFrameworkStaging(
    String gameDir,
    FrameworkDeclaration framework,
    PrepResult prepResult,
    void Function(String) log,
  ) async => false;

  static Future<bool> runInstallScript(
    String tempDir,
    void Function(String) log,
  ) async => false;
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
