/// Web stub for PortFile — no-op on web platform.
/// On web, the backend is always on the same origin (served by ASP.NET UseStaticFiles).
library;

/// Web stub: no port file needed on web (same-origin).
int? readPort() => null;

/// Web stub: no socket on web.
String resolveSocketPath() => '';

/// Web stub: no socket on web.
bool socketExists(String _) => false;

/// Port file discovery is not needed on web — the backend runs on a known port.
class PortFile {
  static int? readPort({List<String>? extraDirs}) => null;
  static void writePort(String dir, int port) {}
  static String resolveSocketPath() => '';
  static bool socketExists(String socketPath) => false;
  static void clearPortFile() {}
  static void deletePortFile(String dir) {}
}
