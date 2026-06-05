class AudioOutputDevice {
  final String id;
  final String name;
  final bool isDefault;

  const AudioOutputDevice({
    required this.id,
    required this.name,
    required this.isDefault,
  });
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

class FlutterPcmPlaybackService {
  String? get selectedDeviceId => null;
  Future<List<AudioOutputDevice>> listOutputDevices() async => const [];
  Future<NativeAudioState> getState() async => NativeAudioState.empty;
  Future<void> setOutputDevice(String? deviceId) async {}
  Future<void> startForInstance(String instanceId) async {}
  Future<void> stop() async {}
  Future<void> restartIfRunning() async {}
}
