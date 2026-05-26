import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'logger.dart';
import 'port_file.dart';
import 'process_runner.dart';

/// Detects and manages the C# backend process lifecycle.
///
/// Discovery chain (3-step):
///   1. Read omni_port.txt → TCP health check on 127.0.0.1:{port}
///   2. Try default port 17890
///   3. Unix socket fallback (PUBLIC/OmniMixPlayer/omnimix.sock or /tmp/omnimix.sock)
class BackendManager {
  Process? _process;
  int? _port;
  bool _usingSocket = false;
  Timer? _watchTimer;
  void Function(bool alive)? onAliveChanged;
  String? _backendExePath;

  BackendManager();

  bool get isRunning => _process != null;
  int? get port => _port;
  bool get usingSocket => _usingSocket;
  String get socketPath => PortFile.resolveSocketPath();

  // ── 3-step discovery ──

  /// Returns: positive port = TCP, -1 = socket mode, null = nothing.
  Future<int?> discover() async {
    final filePort = PortFile.readPort();
    if (filePort != null && await _tcpHealth(filePort)) {
      _port = filePort;
      _usingSocket = false;
      GuiLogger().conn('BackendManager.discover: port file → TCP $filePort');
      return filePort;
    }
    const dp = 17890;
    if (filePort != dp && await _tcpHealth(dp)) {
      _port = dp;
      _usingSocket = false;
      GuiLogger().conn('BackendManager.discover: default port $dp');
      return dp;
    }
    final sp = PortFile.resolveSocketPath();
    if (PortFile.socketExists(sp) && await _socketHealth(sp)) {
      _port = null;
      _usingSocket = true;
      GuiLogger().conn('BackendManager.discover: socket $sp');
      return -1;
    }
    GuiLogger().conn('BackendManager.discover: all failed');
    return null;
  }

  bool detectSync() {
    _port = PortFile.readPort();
    if (_port != null) return true;
    return PortFile.socketExists(PortFile.resolveSocketPath());
  }

  Future<bool> detectAsync() async => await discover() != null;

  void startWatching({Duration interval = const Duration(seconds: 2)}) {
    _watchTimer?.cancel();
    bool last = detectSync();
    GuiLogger().conn(
      'BackendManager.startWatching initial=$last port=$_port sock=$_usingSocket',
    );
    _watchTimer = Timer.periodic(interval, (_) async {
      final alive = await detectAsync();
      if (alive != last) {
        last = alive;
        onAliveChanged?.call(alive);
      }
    });
  }

  void stopWatching() {
    _watchTimer?.cancel();
    _watchTimer = null;
  }

  // ── Health checks ──

  Future<bool> _tcpHealth(int port) async {
    try {
      final s = await Socket.connect(
        InternetAddress('127.0.0.1'),
        port,
        timeout: const Duration(seconds: 3),
      );
      s.write('GET /api/health HTTP/1.0\r\nHost: 127.0.0.1\r\n\r\n');
      await s.flush();
      final ok = (await utf8.decodeStream(s)).contains('200');
      s.destroy();
      return ok;
    } catch (_) {
      return false;
    }
  }

  Future<bool> _socketHealth(String path) async {
    try {
      final s = await Socket.connect(
        InternetAddress(path, type: InternetAddressType.unix),
        0,
        timeout: const Duration(seconds: 3),
      );
      s.write('GET /api/health HTTP/1.0\r\nHost: unix\r\n\r\n');
      await s.flush();
      final ok = (await utf8.decodeStream(s)).contains('200');
      s.destroy();
      return ok;
    } catch (_) {
      return false;
    }
  }

  Future<bool> checkHealth() async {
    if (_usingSocket) return await _socketHealth(PortFile.resolveSocketPath());
    if (_port != null) return await _tcpHealth(_port!);
    return await discover() != null;
  }

  // ── Process lifecycle ──

  Future<bool> start() async {
    if (await discover() != null) {
      GuiLogger().conn(
        'BackendManager.start: already running (port=$_port sock=$_usingSocket)',
      );
      return true;
    }
    _backendExePath = _findBackendExe();
    if (_backendExePath == null) {
      GuiLogger().error('BackendManager.start: exe NOT FOUND');
      return false;
    }

    final guiDir = File(Platform.resolvedExecutable).parent.path;
    GuiLogger().conn(
      'BackendManager.start: spawning $_backendExePath elevated=${Platform.isWindows}',
    );
    await ProcessRunner.spawn(
      exePath: _backendExePath!,
      args: ['--port-file-dir=$guiDir'],
      elevated: Platform.isWindows,
      hidden: true,
    );

    for (var i = 0; i < 40; i++) {
      await Future.delayed(const Duration(milliseconds: 500));
      if (detectSync()) {
        await Future.delayed(const Duration(milliseconds: 300));
        await discover();
        return true;
      }
      if (i % 4 == 3 &&
          !ProcessRunner.isProcessRunning('OmniMixPlayer.Backend.exe') &&
          i > 2) {
        GuiLogger().error('BackendManager.start: process died');
        break;
      }
    }
    GuiLogger().error('BackendManager.start: not detected after 20s');
    return false;
  }

  Future<void> stop() async {
    try {
      Socket s;
      if (_usingSocket) {
        s = await Socket.connect(
          InternetAddress(
            PortFile.resolveSocketPath(),
            type: InternetAddressType.unix,
          ),
          0,
          timeout: const Duration(seconds: 3),
        );
        s.write(
          'POST /api/backend/stop HTTP/1.0\r\nHost: unix\r\nContent-Length: 0\r\n\r\n',
        );
      } else if (_port != null) {
        s = await Socket.connect(
          InternetAddress('127.0.0.1'),
          _port!,
          timeout: const Duration(seconds: 3),
        );
        s.write(
          'POST /api/backend/stop HTTP/1.0\r\nHost: 127.0.0.1\r\nContent-Length: 0\r\n\r\n',
        );
      } else {
        return;
      }
      await s.flush();
      s.destroy();
    } catch (_) {}
    for (var i = 0; i < 20; i++) {
      await Future.delayed(const Duration(milliseconds: 250));
      if (!await checkHealth()) return;
    }
    if (_process != null) {
      _process!.kill(ProcessSignal.sigterm);
      _process = null;
    }
  }

  void dispose() => stopWatching();

  /// Force kill the backend process by name (cross-platform).
  /// Used when service and process coexist — service takes priority.
  Future<bool> forceKillProcess() async {
    const exeName = 'OmniMixPlayer.Backend.exe';
    GuiLogger().conn('BackendManager.forceKillProcess: killing $exeName');
    final ok = await ProcessRunner.killProcess(exeName);
    if (ok) {
      GuiLogger().conn('BackendManager.forceKillProcess: killed');
    } else {
      GuiLogger().conn(
        'BackendManager.forceKillProcess: kill failed or not found',
      );
    }
    _port = null;
    _usingSocket = false;
    return ok;
  }

  String? _findBackendExe() {
    final exeName = Platform.isWindows
        ? 'OmniMixPlayer.Backend.exe'
        : 'OmniMixPlayer.Backend';
    final guiDir = File(Platform.resolvedExecutable).parent.path;
    final sep = Platform.pathSeparator;
    for (final c in [
      '$guiDir$sep$exeName',
      '$guiDir${sep}..${sep}OmniMixPlayer.Backend$sep$exeName',
      '$guiDir${sep}..${sep}bin${sep}Backend$sep$exeName',
    ]) {
      if (File(c).existsSync()) return c;
    }
    return null;
  }
}
