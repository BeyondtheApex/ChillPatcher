import 'dart:io';/// Reads the IPC port from omni_port.txt files in known directories.
///
/// The backend writes `omnimix_port.txt` (containing just the port number)
/// to configured directories. GUI and game clients read this file to discover
/// the backend.
class PortFile {
  /// Try to read the IPC port from known locations.
  /// Returns the port number, or null if no port file found.
  static int? readPort({List<String>? extraDirs}) {
    final dirs = <String>[
      // 1. GUI's own directory (highest priority)
      _guiDir(),
      // 2. PUBLIC/OmniMixPlayer (shared between admin/non-admin)
      _publicDir(),
      // 3. System temp
      Directory.systemTemp.path,
    ];

    if (extraDirs != null) {
      for (final d in extraDirs) {
        if (!dirs.contains(d)) dirs.add(d);
      }
    }

    for (final dir in dirs) {
      if (dir.isEmpty) continue;
      try {
        final file = File('$dir${Platform.pathSeparator}omnimix_port.txt');
        if (file.existsSync()) {
          final content = file.readAsStringSync().trim();
          final port = int.tryParse(content);
          if (port != null && port > 0 && port < 65536) {
            return port;
          }
        }
      } catch (e) {
        }
    }
    return null;
  }

  /// Write the port file to the given directory.
  static void writePort(String dir, int port) {
    try {
      final d = Directory(dir);
      if (!d.existsSync()) d.createSync(recursive: true);
      final file = File('$dir${Platform.pathSeparator}omnimix_port.txt');
      file.writeAsStringSync('$port');
      } catch (e) {
      }
  }

  static String _guiDir() {
    try {
      return File(Platform.resolvedExecutable).parent.path;
    } catch (_) {
      return '';
    }
  }

  static String _publicDir() {
    final public = Platform.environment['PUBLIC'];
    if (public != null && public.isNotEmpty) {
      return '$public${Platform.pathSeparator}OmniMixPlayer';
    }
    return '';
  }

  /// Resolve the unified Unix Domain Socket path (fallback IPC).
  /// Windows: %PUBLIC%/OmniMixPlayer/omnimix.sock
  /// Others:  /tmp/omnimix.sock
  static String resolveSocketPath() {
    if (Platform.isWindows) {
      final public =
          Platform.environment['PUBLIC'] ?? Directory.systemTemp.path;
      return '$public${Platform.pathSeparator}OmniMixPlayer${Platform.pathSeparator}omnimix.sock';
    }
    return '${Platform.pathSeparator}tmp${Platform.pathSeparator}omnimix.sock';
  }

  /// Check if socket file exists (even as a reparse point on Windows).
  static bool socketExists(String socketPath) {
    try {
      final stat = FileStat.statSync(socketPath);
      return stat.type != FileSystemEntityType.notFound;
    } catch (_) {
      return false;
    }
  }

  /// Delete stale port files from all known locations.
  static void clearPortFile() {
    final dirs = <String>[_guiDir(), _publicDir(), Directory.systemTemp.path];
    for (final dir in dirs) {
      if (dir.isEmpty) continue;
      try {
        final file = File('$dir${Platform.pathSeparator}omnimix_port.txt');
        if (file.existsSync()) {
          file.deleteSync();
          }
      } catch (_) {}
    }
  }

  /// Delete the port file from a specific directory.
  static void deletePortFile(String dir) {
    try {
      final file = File('$dir${Platform.pathSeparator}omnimix_port.txt');
      if (file.existsSync()) {
        file.deleteSync();
        }
    } catch (e) {
      }
  }
}
