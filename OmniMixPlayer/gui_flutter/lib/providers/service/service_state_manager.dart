/// Service installation & management (Windows service).
/// Extracted from AppState during Riverpod migration.

import 'dart:async';
import 'package:flutter/foundation.dart';
import '../../services/platform_service.dart'
    if (dart.library.js_interop) '../../stubs/platform_service_web.dart';

class ServiceStateManager extends ChangeNotifier {
  String _state =
      'unknown'; // 'running', 'installed', 'not_installed', 'unknown'
  bool _busy = false;
  bool _autoStart = false;
  String? _result;
  Timer? _resultTimer;

  String get state => _state;
  bool get busy => _busy;
  bool get autoStart => _autoStart;
  String? get result => _result;

  set stateVal(String v) {
    _state = v;
    notifyListeners();
  }

  set autoStartVal(bool v) {
    _autoStart = v;
    notifyListeners();
  }

  Future<String> install() async {
    _busy = true;
    _result = null;
    notifyListeners();
    try {
      final ok = await PlatformService.installService();
      if (ok) {
        _state = 'installed';
        _autoStart = await PlatformService.isServiceAutoStart();
        _result = 'installed';
      } else {
        _state = await PlatformService.getServiceState();
        _result = 'failed';
      }
    } catch (e) {
      _result = 'failed';
    }
    _busy = false;
    notifyListeners();
    _clearResultAfterDelay();
    return _result!;
  }

  Future<String> uninstall() async {
    _busy = true;
    _result = null;
    notifyListeners();
    try {
      final ok = await PlatformService.uninstallService();
      if (ok) {
        _state = 'not_installed';
        _autoStart = false;
        _result = 'not_installed';
      } else {
        _state = await PlatformService.getServiceState();
        _result = 'failed';
      }
    } catch (e) {
      _result = 'failed';
    }
    _busy = false;
    notifyListeners();
    _clearResultAfterDelay();
    return _result!;
  }

  void _clearResultAfterDelay() {
    _resultTimer?.cancel();
    _resultTimer = Timer(const Duration(seconds: 5), () {
      _result = null;
      notifyListeners();
    });
  }

  Future<void> refresh() async {
    _state = await PlatformService.getServiceState();
    _autoStart = await PlatformService.isServiceAutoStart();
    notifyListeners();
  }

  Future<bool> setAutoStart(bool autoStart) async {
    _busy = true;
    notifyListeners();
    try {
      final ok = await PlatformService.setServiceAutoStart(autoStart);
      if (ok) {
        _autoStart = autoStart;
      }
      return ok;
    } catch (e) {
      return false;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  void disposeManager() {
    _resultTimer?.cancel();
  }
}
