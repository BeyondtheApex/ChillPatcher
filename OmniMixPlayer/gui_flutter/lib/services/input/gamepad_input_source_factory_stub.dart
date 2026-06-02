import 'input_device_source.dart';

InputDeviceSource createGamepadInputSource(
  InputKeyReporter setKeyPressed,
  InputAxisReporter setAxisValue,
) {
  return NoopInputDeviceSource();
}

class NoopInputDeviceSource implements InputDeviceSource {
  @override
  Future<void> start() async {}

  @override
  Future<void> dispose() async {}
}
