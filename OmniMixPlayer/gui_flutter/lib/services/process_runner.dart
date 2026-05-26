import 'dart:ffi';
import 'dart:io';
import 'package:ffi/ffi.dart';

/// Cross-platform process runner with elevation support.
class ProcessRunner {
  /// Spawn a process, optionally with admin elevation.
  /// Returns true if the spawn was initiated successfully.
  static Future<bool> spawn({
    required String exePath,
    List<String> args = const [],
    String? workingDir,
    bool elevated = false,
    bool hidden = true,
  }) async {
    if (Platform.isWindows) {
      return _spawnWindows(
        exePath: exePath,
        args: args,
        elevated: elevated,
        hidden: hidden,
      );
    } else {
      return _spawnUnix(exePath: exePath, args: args, workingDir: workingDir);
    }
  }

  /// Check if a process with the given name is running.
  static bool isProcessRunning(String processName) {
    if (Platform.isWindows) {
      return _isProcessRunningWindows(processName);
    } else {
      return _isProcessRunningUnix(processName);
    }
  }

  /// Force kill a process by name. Returns true if successful.
  static Future<bool> killProcess(String processName) async {
    if (Platform.isWindows) {
      return _killProcessWindows(processName);
    } else {
      return _killProcessUnix(processName);
    }
  }

  // ── Windows ──

  static Future<bool> _spawnWindows({
    required String exePath,
    required List<String> args,
    required bool elevated,
    required bool hidden,
  }) async {
    if (elevated) {
      // Use ShellExecuteW("runas") for UAC elevation
      return _shellExecuteElevated(exePath, args, hidden);
    } else {
      final flags = hidden
          ? ProcessStartMode.detached
          : ProcessStartMode.normal;
      final proc = await Process.start(exePath, args, mode: flags);
      proc.stdout.drain();
      proc.stderr.drain();
      return true;
    }
  }

  /// Call ShellExecuteW with "runas" verb for UAC elevation.
  /// Returns: true if ShellExecuteW returned > 32 (success).
  static bool _shellExecuteElevated(
    String exePath,
    List<String> args,
    bool hidden,
  ) {
    final argLine = args.map((a) => _quoteArg(a)).join(' ');
    final arena = Arena();

    try {
      final exePtr = exePath.toNativeUtf16(allocator: arena);
      final argPtr = argLine.toNativeUtf16(allocator: arena);
      final verbPtr = 'runas'.toNativeUtf16(allocator: arena);

      final result = ShellExecuteW(
        0,
        verbPtr,
        exePtr,
        argPtr,
        Pointer.fromAddress(0), // lpDirectory = NULL
        hidden ? SW_HIDE : SW_SHOWNORMAL,
      );
      return result > 32;
    } finally {
      arena.releaseAll();
    }
  }

  static String _quoteArg(String arg) {
    if (arg.contains(' ') || arg.contains('"')) {
      return '"${arg.replaceAll('"', '\\"')}"';
    }
    return arg;
  }

  static bool _isProcessRunningWindows(String processName) {
    try {
      final result = Process.runSync('tasklist', [
        '/FI',
        'IMAGENAME eq $processName',
        '/NH',
      ]);
      return result.stdout.toString().contains(processName);
    } catch (_) {
      return false;
    }
  }

  // ── Unix (Linux / macOS) ──

  static Future<bool> _spawnUnix({
    required String exePath,
    required List<String> args,
    String? workingDir,
  }) async {
    try {
      final proc = await Process.start(
        exePath,
        args,
        workingDirectory: workingDir,
      );
      proc.stdout.drain();
      proc.stderr.drain();
      return true;
    } catch (_) {
      return false;
    }
  }

  static bool _isProcessRunningUnix(String processName) {
    try {
      final result = Process.runSync('pgrep', [processName]);
      return result.exitCode == 0;
    } catch (_) {
      return false;
    }
  }

  static Future<bool> _killProcessWindows(String processName) async {
    try {
      final result = await Process.run('taskkill', ['/F', '/IM', processName]);
      return result.exitCode == 0;
    } catch (_) {
      return false;
    }
  }

  static Future<bool> _killProcessUnix(String processName) async {
    try {
      final result = await Process.run('pkill', ['-f', processName]);
      return result.exitCode == 0;
    } catch (_) {
      return false;
    }
  }
}

// ── Win32 FFI bindings ──

final DynamicLibrary _shell32 = DynamicLibrary.open('shell32.dll');

typedef ShellExecuteWNative =
    IntPtr Function(
      IntPtr hwnd,
      Pointer<Utf16> lpOperation,
      Pointer<Utf16> lpFile,
      Pointer<Utf16> lpParameters,
      Pointer<Utf16> lpDirectory,
      Int32 nShowCmd,
    );

typedef ShellExecuteWDart =
    int Function(
      int hwnd,
      Pointer<Utf16> lpOperation,
      Pointer<Utf16> lpFile,
      Pointer<Utf16> lpParameters,
      Pointer<Utf16> lpDirectory,
      int nShowCmd,
    );

final ShellExecuteW = _shell32
    .lookupFunction<ShellExecuteWNative, ShellExecuteWDart>('ShellExecuteW');

const int SW_HIDE = 0;
const int SW_SHOWNORMAL = 1;
