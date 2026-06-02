import 'dart:io';

import 'input_device_source.dart';
import 'xinput_gamepad_input_source.dart';

InputDeviceSource createGamepadInputSource(
  InputKeyReporter setKeyPressed,
  InputAxisReporter setAxisValue,
) {
  if (Platform.isWindows && XInputGamepadInputSource.isSupported) {
    return XInputGamepadInputSource(setKeyPressed, setAxisValue);
  }
  return NoopInputDeviceSource();
}

class NoopInputDeviceSource implements InputDeviceSource {
  @override
  Future<void> start() async {}

  @override
  Future<void> dispose() async {}
}
