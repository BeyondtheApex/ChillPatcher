/// Web stub for ModDeploymentService — no-op on web.
/// Mod deployment (extract zip, copy to game dir, manage BepInEx) is not
/// available in the browser — it requires filesystem access.
library mod_deployment_service_web;

import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';

/// Web stub: deployment features not available in browser.
class ModDeploymentService {
  static Future<String?> selectDirectory() async => null;

  static bool verifyGameDirectory(String path, GameDeclaration game) => false;

  static BepInExStatus checkBepInExStatus(String gameDir) =>
      BepInExStatus.notInstalled;

  static ModStatus checkModStatus(String gameDir, String folderName) =>
      ModStatus.notInstalled;

  static ModStatus checkRootModStatus(String gameDir, ModDeclaration mod) =>
      ModStatus.notInstalled;

  static Future<bool> deployBepInEx(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async => false;

  static Future<bool> undeployBepInEx(
    String gameDir,
    FrameworkDeclaration framework,
    void Function(String) log,
  ) async => false;

  static Future<bool> deployMod(
    String gameDir,
    ModDeclaration mod,
    void Function(String) log, {
    int? backendPort,
    Future<void> Function(String, bool)? onPortFileDirChanged,
    Map<String, dynamic> customSettings = const {},
  }) async => false;

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
}
