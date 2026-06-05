import 'dart:convert';
import 'dart:ffi';
import 'dart:io';

import 'package:ffi/ffi.dart';

class AudioOutputDevice {
  final String id;
  final String name;
  final bool isDefault;

  const AudioOutputDevice({
    required this.id,
    required this.name,
    required this.isDefault,
  });

  factory AudioOutputDevice.fromJson(Map<String, dynamic> json) {
    return AudioOutputDevice(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? 'Unknown output device',
      isDefault: json['is_default'] as bool? ?? false,
    );
  }
}

class NativeAudioState {
  final bool running;
  final String selectedDeviceId;
  final String instanceId;
  final int inputSampleRate;
  final int inputChannels;
  final int outputSampleRate;
  final int outputChannels;
  final int streamId;
  final int formatGeneration;
  final String lastError;

  const NativeAudioState({
    required this.running,
    required this.selectedDeviceId,
    required this.instanceId,
    required this.inputSampleRate,
    required this.inputChannels,
    required this.outputSampleRate,
    required this.outputChannels,
    required this.streamId,
    required this.formatGeneration,
    required this.lastError,
  });

  factory NativeAudioState.fromJson(Map<String, dynamic> json) {
    return NativeAudioState(
      running: json['running'] as bool? ?? false,
      selectedDeviceId: json['selected_device_id'] as String? ?? '',
      instanceId: json['instance_id'] as String? ?? '',
      inputSampleRate: json['input_sample_rate'] as int? ?? 0,
      inputChannels: json['input_channels'] as int? ?? 0,
      outputSampleRate: json['output_sample_rate'] as int? ?? 0,
      outputChannels: json['output_channels'] as int? ?? 0,
      streamId: json['stream_id'] as int? ?? 0,
      formatGeneration: json['format_generation'] as int? ?? 0,
      lastError: json['last_error'] as String? ?? '',
    );
  }

  static const empty = NativeAudioState(
    running: false,
    selectedDeviceId: '',
    instanceId: '',
    inputSampleRate: 0,
    inputChannels: 0,
    outputSampleRate: 0,
    outputChannels: 0,
    streamId: 0,
    formatGeneration: 0,
    lastError: '',
  );
}

typedef _Str0Native = Pointer<Utf8> Function();
typedef _Str0Dart = Pointer<Utf8> Function();
typedef _StartNative = Int32 Function(Pointer<Utf8>, Pointer<Utf8>);
typedef _StartDart = int Function(Pointer<Utf8>, Pointer<Utf8>);
typedef _SetDeviceNative = Int32 Function(Pointer<Utf8>);
typedef _SetDeviceDart = int Function(Pointer<Utf8>);
typedef _Int0Native = Int32 Function();
typedef _Int0Dart = int Function();
typedef _FreeNative = Void Function(Pointer<Utf8>);
typedef _FreeDart = void Function(Pointer<Utf8>);

class NativeAudioEngine {
  NativeAudioEngine._() {
    if (!Platform.isWindows) {
      throw UnsupportedError('Native audio engine currently supports Windows');
    }
    _dll = DynamicLibrary.open('omnimix_audio.dll');
    _listDevices = _dll.lookupFunction<_Str0Native, _Str0Dart>(
      'omnimix_audio_list_output_devices_json',
    );
    _start = _dll.lookupFunction<_StartNative, _StartDart>(
      'omnimix_audio_start',
    );
    _stop = _dll.lookupFunction<_Int0Native, _Int0Dart>('omnimix_audio_stop');
    _setDevice = _dll.lookupFunction<_SetDeviceNative, _SetDeviceDart>(
      'omnimix_audio_set_device',
    );
    _getState = _dll.lookupFunction<_Str0Native, _Str0Dart>(
      'omnimix_audio_get_state_json',
    );
    _freeString = _dll.lookupFunction<_FreeNative, _FreeDart>(
      'omnimix_audio_free_string',
    );
  }

  static final NativeAudioEngine instance = NativeAudioEngine._();

  late final DynamicLibrary _dll;
  late final _Str0Dart _listDevices;
  late final _StartDart _start;
  late final _Int0Dart _stop;
  late final _SetDeviceDart _setDevice;
  late final _Str0Dart _getState;
  late final _FreeDart _freeString;

  List<AudioOutputDevice> listOutputDevices() {
    final json = _takeString(_listDevices());
    final decoded = jsonDecode(json) as Map<String, dynamic>;
    final devices = decoded['devices'] as List<dynamic>? ?? const [];
    return devices
        .map((item) => AudioOutputDevice.fromJson(item as Map<String, dynamic>))
        .where((d) => d.id.isNotEmpty)
        .toList();
  }

  void start(String instanceId, {String? deviceId}) {
    final iid = instanceId.toNativeUtf8();
    final dev = (deviceId ?? '').toNativeUtf8();
    try {
      _check(_start(iid, dev));
    } finally {
      calloc.free(iid);
      calloc.free(dev);
    }
  }

  void stop() {
    _check(_stop());
  }

  void setDevice(String? deviceId) {
    final dev = (deviceId ?? '').toNativeUtf8();
    try {
      _check(_setDevice(dev));
    } finally {
      calloc.free(dev);
    }
  }

  NativeAudioState state() {
    final json = _takeString(_getState());
    return NativeAudioState.fromJson(jsonDecode(json) as Map<String, dynamic>);
  }

  String _takeString(Pointer<Utf8> ptr) {
    if (ptr == nullptr) return '{}';
    try {
      return ptr.toDartString();
    } finally {
      _freeString(ptr);
    }
  }

  void _check(int result) {
    if (result == 0) return;
    final state = this.state();
    final message = state.lastError.isNotEmpty
        ? state.lastError
        : 'native audio operation failed';
    throw StateError(message);
  }
}
