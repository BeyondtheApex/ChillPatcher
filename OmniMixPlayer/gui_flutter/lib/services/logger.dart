import 'dart:io';

/// Simple file logger for debugging GUI-to-backend connection issues.
/// Logs to %TEMP%/omnimix_gui.log
class GuiLogger {
  static final GuiLogger _instance = GuiLogger._();
  factory GuiLogger() => _instance;
  GuiLogger._();

  late final File _file;
  bool _inited = false;

  void init() {
    if (_inited) return;
    _inited = true;
    final logPath =
        '${Directory.systemTemp.path}${Platform.pathSeparator}omnimix_gui.log';
    _file = File(logPath);
    _log('========== GUI STARTED ==========');
    _log(
      'Platform: ${Platform.operatingSystem} ${Platform.operatingSystemVersion}',
    );
    _log('Executable: ${Platform.resolvedExecutable}');
    _log('PID: ${pid}');
  }

  void info(String msg) {
    _log('[INFO] $msg');
  }

  void warn(String msg) {
    _log('[WARN] $msg');
  }

  void error(String msg, [Object? e, StackTrace? st]) {
    _log('[ERROR] $msg');
    if (e != null) _log('  Exception: $e');
    if (st != null) _log('  Stack: $st');
  }

  void conn(String msg) {
    _log('[CONN] $msg');
  }

  void _log(String line) {
    final ts = DateTime.now().toIso8601String();
    try {
      _file.writeAsStringSync('$ts $line\n', mode: FileMode.append);
    } catch (_) {
      // can't log to file - try stderr
      stderr.writeln('$ts $line');
    }
  }
}
