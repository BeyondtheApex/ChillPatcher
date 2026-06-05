import 'dart:io';

export 'native_audio_engine.dart' show AudioOutputDevice, NativeAudioState;

import 'native_audio_engine.dart';

class FlutterPcmPlaybackService {
  NativeAudioEngine? _engine;
  String? _instanceId;
  String? _deviceId;

  String? get selectedDeviceId => _deviceId;

  NativeAudioEngine? get _safeEngine {
    if (!Platform.isWindows) return null;
    return _engine ??= NativeAudioEngine.instance;
  }

  Future<List<AudioOutputDevice>> listOutputDevices() async {
    return _safeEngine?.listOutputDevices() ?? const [];
  }

  Future<NativeAudioState> getState() async {
    return _safeEngine?.state() ?? NativeAudioState.empty;
  }

  Future<void> setOutputDevice(String? deviceId) async {
    _deviceId = deviceId == null || deviceId.isEmpty ? null : deviceId;
    _safeEngine?.setDevice(_deviceId);
  }

  Future<void> startForInstance(String instanceId) async {
    if (instanceId.isEmpty) return;
    _instanceId = instanceId;
    _safeEngine?.start(instanceId, deviceId: _deviceId);
  }

  Future<void> stop() async {
    _instanceId = null;
    _engine?.stop();
  }

  Future<void> restartIfRunning() async {
    final id = _instanceId;
    if (id == null || id.isEmpty) return;
    _safeEngine?.start(id, deviceId: _deviceId);
  }
}
