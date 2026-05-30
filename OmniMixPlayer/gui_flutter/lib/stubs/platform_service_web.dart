/// Web stub for PlatformService — no-op on web.
/// Service management (install/uninstall/start/stop) is not available in browser.
library platform_service_web;

/// Web stub: always returns 'not_installed' since OS services aren't accessible from browser.
Future<String> getServiceState() async => 'not_installed';

/// Web stub: always returns false.
Future<bool> isServiceAutoStart() async => false;

/// Web stub: always returns false.
Future<bool> installService() async => false;

/// Web stub: always returns false.
Future<bool> uninstallService() async => false;

/// Web stub: always returns false.
Future<bool> startService() async => false;

/// Web stub: always returns false.
Future<bool> stopService() async => false;

/// Web stub: always returns false.
Future<bool> setServiceAutoStart(bool _) async => false;

/// Web stub: always returns false.
Future<bool> setGuiAutostart(bool _) async => false;

/// On web, there's no OS service management — the backend runs independently.
class PlatformService {
  static Future<String> getServiceState() async => 'unknown';
  static Future<bool> isServiceAutoStart() async => false;
  static Future<bool> installService() async => false;
  static Future<bool> startService() async => false;
  static Future<bool> stopService() async => false;
  static Future<bool> uninstallService() async => false;
  static Future<bool> setServiceAutoStart(bool autoStart) async => false;
  static Future<bool> setGuiAutostart(bool v) async => false;
  static String? get backendExePath => null;
  static Future<String?> getServiceBinaryPath() async => null;
  static bool arePathsEqual(String p1, String p2) => false;
}
