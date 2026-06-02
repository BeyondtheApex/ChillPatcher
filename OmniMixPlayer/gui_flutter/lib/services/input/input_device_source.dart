import '../../models/input/input_event.dart';

typedef InputKeyReporter =
    void Function(InputKeyId key, bool pressed, {DateTime? timestamp});
typedef InputAxisReporter =
    void Function(InputAxisId axis, double value, {DateTime? timestamp});

abstract class InputDeviceSource {
  Future<void> start();
  Future<void> dispose();
}
