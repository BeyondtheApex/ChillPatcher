import 'input_device_source.dart';

class XInputGamepadInputSource implements InputDeviceSource {
  XInputGamepadInputSource(
    InputKeyReporter setKeyPressed,
    InputAxisReporter setAxisValue,
  );

  static bool get isSupported => false;

  @override
  Future<void> start() async {}

  @override
  Future<void> dispose() async {}
}
