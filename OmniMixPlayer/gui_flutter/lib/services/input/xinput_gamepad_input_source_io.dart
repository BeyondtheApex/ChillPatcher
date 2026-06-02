import 'dart:async';
import 'dart:ffi';
import 'dart:io';

import 'package:ffi/ffi.dart';
import 'package:win32/win32.dart';

import '../../models/input/input_event.dart';
import 'input_device_source.dart';

class XInputGamepadInputSource implements InputDeviceSource {
  static const int _controllerCount = 4;
  static const Duration _pollInterval = Duration(milliseconds: 8);
  static const double _axisChangeThreshold = 0.01;

  final InputKeyReporter _setKeyPressed;
  final InputAxisReporter _setAxisValue;
  final List<_XInputSnapshot?> _snapshots = List.filled(_controllerCount, null);
  Timer? _timer;

  XInputGamepadInputSource(this._setKeyPressed, this._setAxisValue);

  static bool get isSupported => Platform.isWindows;

  @override
  Future<void> start() async {
    if (!Platform.isWindows || _timer != null) return;

    _timer = Timer.periodic(_pollInterval, (_) => _poll());
    _poll();
  }

  void _poll() {
    final state = calloc<XINPUT_STATE>();
    try {
      for (var index = 0; index < _controllerCount; index++) {
        final result = XInputGetState(index, state);
        if (result == ERROR_SUCCESS) {
          _handleConnected(index, state.ref.Gamepad);
        } else {
          _handleDisconnected(index);
        }
      }
    } catch (_) {
    } finally {
      calloc.free(state);
    }
  }

  void _handleConnected(int index, XINPUT_GAMEPAD gamepad) {
    final next = _XInputSnapshot.fromGamepad(gamepad);
    final previous = _snapshots[index];
    final deviceId = 'xinput-$index';
    final now = DateTime.now();

    final previousButtons = previous?.buttons ?? 0;
    for (final button in _buttons) {
      final wasPressed = (previousButtons & button.mask) != 0;
      final isPressed = (next.buttons & button.mask) != 0;
      if (wasPressed == isPressed) continue;

      _setKeyPressed(
        InputKeyId.gamepadButton(button.code, deviceId: deviceId),
        isPressed,
        timestamp: now,
      );
    }

    _emitAxisIfChanged(
      deviceId,
      'leftStickX',
      previous?.leftStickX,
      next.leftStickX,
      now,
    );
    _emitAxisIfChanged(
      deviceId,
      'leftStickY',
      previous?.leftStickY,
      next.leftStickY,
      now,
    );
    _emitAxisIfChanged(
      deviceId,
      'rightStickX',
      previous?.rightStickX,
      next.rightStickX,
      now,
    );
    _emitAxisIfChanged(
      deviceId,
      'rightStickY',
      previous?.rightStickY,
      next.rightStickY,
      now,
    );
    _emitAxisIfChanged(
      deviceId,
      'leftTrigger',
      previous?.leftTrigger,
      next.leftTrigger,
      now,
    );
    _emitAxisIfChanged(
      deviceId,
      'rightTrigger',
      previous?.rightTrigger,
      next.rightTrigger,
      now,
    );

    _snapshots[index] = next;
  }

  void _emitAxisIfChanged(
    String deviceId,
    String code,
    double? previous,
    double value,
    DateTime timestamp,
  ) {
    if (previous != null && (previous - value).abs() < _axisChangeThreshold) {
      return;
    }

    _setAxisValue(
      InputAxisId.gamepadAxis(code, deviceId: deviceId),
      value,
      timestamp: timestamp,
    );
  }

  void _handleDisconnected(int index) {
    if (_snapshots[index] == null) return;

    final deviceId = 'xinput-$index';
    final now = DateTime.now();
    for (final button in _buttons) {
      _setKeyPressed(
        InputKeyId.gamepadButton(button.code, deviceId: deviceId),
        false,
        timestamp: now,
      );
    }
    _snapshots[index] = null;
  }

  @override
  Future<void> dispose() async {
    _timer?.cancel();
    _timer = null;
  }
}

class _XInputSnapshot {
  final int buttons;
  final double leftStickX;
  final double leftStickY;
  final double rightStickX;
  final double rightStickY;
  final double leftTrigger;
  final double rightTrigger;

  const _XInputSnapshot({
    required this.buttons,
    required this.leftStickX,
    required this.leftStickY,
    required this.rightStickX,
    required this.rightStickY,
    required this.leftTrigger,
    required this.rightTrigger,
  });

  factory _XInputSnapshot.fromGamepad(XINPUT_GAMEPAD gamepad) {
    return _XInputSnapshot(
      buttons: gamepad.wButtons,
      leftStickX: _normalizeStick(
        gamepad.sThumbLX,
        XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE,
      ),
      leftStickY: _normalizeStick(
        gamepad.sThumbLY,
        XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE,
      ),
      rightStickX: _normalizeStick(
        gamepad.sThumbRX,
        XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE,
      ),
      rightStickY: _normalizeStick(
        gamepad.sThumbRY,
        XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE,
      ),
      leftTrigger: _normalizeTrigger(gamepad.bLeftTrigger),
      rightTrigger: _normalizeTrigger(gamepad.bRightTrigger),
    );
  }

  static double _normalizeStick(int value, int deadzone) {
    if (value.abs() <= deadzone) return 0;
    final normalized = value < 0 ? value / 32768 : value / 32767;
    return normalized.clamp(-1.0, 1.0);
  }

  static double _normalizeTrigger(int value) {
    if (value <= XINPUT_GAMEPAD_TRIGGER_THRESHOLD) return 0;
    return (value / 255).clamp(0.0, 1.0);
  }
}

class _XInputButton {
  final int mask;
  final String code;

  const _XInputButton(this.mask, this.code);
}

const _buttons = <_XInputButton>[
  _XInputButton(XINPUT_GAMEPAD_A, 'a'),
  _XInputButton(XINPUT_GAMEPAD_B, 'b'),
  _XInputButton(XINPUT_GAMEPAD_X, 'x'),
  _XInputButton(XINPUT_GAMEPAD_Y, 'y'),
  _XInputButton(XINPUT_GAMEPAD_DPAD_UP, 'dpadUp'),
  _XInputButton(XINPUT_GAMEPAD_DPAD_DOWN, 'dpadDown'),
  _XInputButton(XINPUT_GAMEPAD_DPAD_LEFT, 'dpadLeft'),
  _XInputButton(XINPUT_GAMEPAD_DPAD_RIGHT, 'dpadRight'),
  _XInputButton(XINPUT_GAMEPAD_START, 'start'),
  _XInputButton(XINPUT_GAMEPAD_BACK, 'back'),
  _XInputButton(XINPUT_GAMEPAD_LEFT_THUMB, 'leftStick'),
  _XInputButton(XINPUT_GAMEPAD_RIGHT_THUMB, 'rightStick'),
  _XInputButton(XINPUT_GAMEPAD_LEFT_SHOULDER, 'leftBumper'),
  _XInputButton(XINPUT_GAMEPAD_RIGHT_SHOULDER, 'rightBumper'),
];
