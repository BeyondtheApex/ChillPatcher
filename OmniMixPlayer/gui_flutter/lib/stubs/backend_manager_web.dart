/// Web stub for BackendManager — simplified for web.
/// On web, the backend is assumed to be running on the same origin.
/// No process management, no file-based discovery.
library backend_manager_web;

import 'dart:async';
import 'package:http/http.dart' as http;

/// Simplified backend manager for web — no process lifecycle.
class BackendManager {
  bool _alive = false;
  Timer? _watchTimer;
  void Function(bool alive)? onAliveChanged;

  BackendManager();

  bool get usingSocket => false;
  String get socketPath => '';

  /// Start watching for backend health (HTTP-based, same-origin).
  void startWatching({Duration interval = const Duration(seconds: 3)}) {
    _watchTimer?.cancel();
    _watchTimer = Timer.periodic(interval, (_) async {
      final wasAlive = _alive;
      _alive = await checkHealth();
      if (_alive != wasAlive) {
        onAliveChanged?.call(_alive);
      }
    });
  }

  void stopWatching() {
    _watchTimer?.cancel();
    _watchTimer = null;
  }

  /// Check backend health via same-origin HTTP.
  Future<bool> checkHealth() async {
    try {
      final resp = await http
          .get(Uri.parse('/api/health'))
          .timeout(const Duration(seconds: 3));
      return resp.statusCode == 200;
    } catch (_) {
      return false;
    }
  }

  /// Discover is not needed on web — always use same origin.
  Future<int?> discover() async {
    final ok = await checkHealth();
    return ok ? 17890 : null;
  }

  bool detectSync() => _alive;

  Future<bool> detectAsync() async => await checkHealth();

  /// Port is always null on web (not relevant — same-origin).
  int? get port => null;

  /// No-op on web.
  void dispose() {}

  // start/stop/forceKill are no-ops on web
  Future<bool> start() async => false;
  Future<void> stop() async {}
  Future<void> forceKillProcess() async {}
}
