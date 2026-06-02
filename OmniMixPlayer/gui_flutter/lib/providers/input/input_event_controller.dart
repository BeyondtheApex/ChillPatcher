import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../models/input/input_event.dart';
import '../../services/input/gamepad_input_source_factory.dart';
import '../../services/input/input_device_source.dart';

typedef InputBindingCallback = FutureOr<void> Function(InputEvent event);

class ShortcutAction {
  final String id;
  final String descriptionKey;
  final VoidCallback onTrigger;

  ShortcutAction({
    required this.id,
    required this.descriptionKey,
    required this.onTrigger,
  });
}

class InputEventController extends ChangeNotifier {
  final Set<InputKeyId> _pressedKeys = <InputKeyId>{};
  final Map<InputAxisId, double> _axes = <InputAxisId, double>{};
  final Map<String, _RegisteredInputBinding> _bindings =
      <String, _RegisteredInputBinding>{};
  final Set<String> _activeBindings = <String>{};
  final StreamController<InputEvent> _events =
      StreamController<InputEvent>.broadcast();
  late final InputDeviceSource _gamepads;

  bool _initialized = false;

  // Custom shortcut actions registry
  final Map<String, ShortcutAction> _shortcutActions = {};

  // User's custom bindings: actionId -> CustomShortcutBinding
  final Map<String, CustomShortcutBinding> _customBindings = {};

  // Track active states to detect false -> true transitions
  final Map<String, bool> _customBindingActiveStates = {};

  // Track triggers pressed state for hysteresis
  final Map<String, bool> _triggerPressedStates = {};

  // Flag to suspend executing shortcut actions (e.g. when setting page is open)
  bool _shortcutsSuspended = false;

  InputEventController() {
    _gamepads = createGamepadInputSource(setKeyPressed, setAxisValue);
  }

  Stream<InputEvent> get events => _events.stream;

  InputSnapshot get snapshot => InputSnapshot(
    pressedKeys: Set.unmodifiable(_pressedKeys),
    modifiers: Set.unmodifiable(_currentModifiers),
    axes: Map.unmodifiable(_axes),
  );

  bool get shortcutsSuspended => _shortcutsSuspended;

  void setShortcutsSuspended(bool value) {
    _shortcutsSuspended = value;
    notifyListeners();
  }

  List<ShortcutAction> get registeredActions =>
      _shortcutActions.values.toList();

  CustomShortcutBinding? getBindingForAction(String actionId) =>
      _customBindings[actionId];

  void registerShortcutAction(ShortcutAction action) {
    _shortcutActions[action.id] = action;
    notifyListeners();
  }

  Future<void> init() async {
    if (_initialized) return;
    _initialized = true;
    await loadCustomBindings();
    await _gamepads.start();
  }

  void registerBinding(InputBinding binding, InputBindingCallback callback) {
    _bindings[binding.id] = _RegisteredInputBinding(binding, callback);
    _activeBindings.remove(binding.id);
  }

  void unregisterBinding(String id) {
    _bindings.remove(id);
    _activeBindings.remove(id);
  }

  void clearBindings() {
    _bindings.clear();
    _activeBindings.clear();
  }

  void setKeyPressed(InputKeyId key, bool pressed, {DateTime? timestamp}) {
    final changed = pressed ? _pressedKeys.add(key) : _pressedKeys.remove(key);
    if (!changed) return;

    final event = InputEvent(
      type: pressed ? InputEventType.pressed : InputEventType.released,
      timestamp: timestamp ?? DateTime.now(),
      key: key,
      value: pressed ? 1 : 0,
      pressedKeys: Set.unmodifiable(_pressedKeys),
      modifiers: Set.unmodifiable(_currentModifiers),
    );

    _emit(event);
  }

  void setAxisValue(InputAxisId axis, double value, {DateTime? timestamp}) {
    // Convert leftStickX to virtual button presses
    if (axis.code == 'leftStickX') {
      final leftKey = InputKeyId.gamepadButton(
        'leftStickLeft',
        deviceId: axis.deviceId,
      );
      final rightKey = InputKeyId.gamepadButton(
        'leftStickRight',
        deviceId: axis.deviceId,
      );

      final wasLeftPressed = _pressedKeys.contains(leftKey);
      final wasRightPressed = _pressedKeys.contains(rightKey);

      if (value < -0.5) {
        if (!wasLeftPressed) {
          setKeyPressed(leftKey, true, timestamp: timestamp);
        }
      } else if (value > -0.2) {
        if (wasLeftPressed) {
          setKeyPressed(leftKey, false, timestamp: timestamp);
        }
      }

      if (value > 0.5) {
        if (!wasRightPressed) {
          setKeyPressed(rightKey, true, timestamp: timestamp);
        }
      } else if (value < 0.2) {
        if (wasRightPressed) {
          setKeyPressed(rightKey, false, timestamp: timestamp);
        }
      }
      return;
    }

    // 1. Ignore joystick axis events (不要摇杆轴事件)
    if (axis.code == 'leftStickY' ||
        axis.code == 'rightStickX' ||
        axis.code == 'rightStickY') {
      return;
    }

    final previous = _axes[axis];
    if (previous == value) return;
    _axes[axis] = value;

    // 2. Map LT / RT trigger axes to keys with a hysteresis threshold (press > 0.6, release < 0.3)
    if (axis.code == 'leftTrigger' || axis.code == 'rightTrigger') {
      final keyId = InputKeyId.gamepadButton(
        axis.code,
        deviceId: axis.deviceId,
      );
      final wasPressed = _triggerPressedStates[axis.code] ?? false;

      bool isPressed = wasPressed;
      if (!wasPressed && value > 0.6) {
        isPressed = true;
      } else if (wasPressed && value < 0.3) {
        isPressed = false;
      }

      if (wasPressed != isPressed) {
        _triggerPressedStates[axis.code] = isPressed;
        final changed = isPressed
            ? _pressedKeys.add(keyId)
            : _pressedKeys.remove(keyId);
        if (changed) {
          final keyEvent = InputEvent(
            type: isPressed ? InputEventType.pressed : InputEventType.released,
            timestamp: timestamp ?? DateTime.now(),
            key: keyId,
            value: isPressed ? 1 : 0,
            pressedKeys: Set.unmodifiable(_pressedKeys),
            modifiers: Set.unmodifiable(_currentModifiers),
          );
          _emit(keyEvent);
        }
      }
    }

    final event = InputEvent(
      type: InputEventType.axisChanged,
      timestamp: timestamp ?? DateTime.now(),
      axis: axis,
      value: value,
      pressedKeys: Set.unmodifiable(_pressedKeys),
      modifiers: Set.unmodifiable(_currentModifiers),
    );

    _emit(event);
  }

  void clearPressedKeysForDevice(InputSource source, String deviceId) {
    final removed = _pressedKeys
        .where((key) => key.source == source && key.deviceId == deviceId)
        .toList();
    for (final key in removed) {
      setKeyPressed(key, false);
    }
  }

  Set<InputModifier> get _currentModifiers {
    final modifiers = <InputModifier>{};
    for (final key in _pressedKeys) {
      final modifier = _modifierForKey(key);
      if (modifier != null) modifiers.add(modifier);
    }
    return modifiers;
  }

  InputModifier? _modifierForKey(InputKeyId key) {
    if (key.source != InputSource.keyboard) return null;
    switch (key.code) {
      case 'ShiftLeft':
      case 'ShiftRight':
        return InputModifier.shift;
      case 'ControlLeft':
      case 'ControlRight':
        return InputModifier.control;
      case 'AltLeft':
      case 'AltRight':
        return InputModifier.alt;
      case 'MetaLeft':
      case 'MetaRight':
        return InputModifier.meta;
    }
    return null;
  }

  void _emit(InputEvent event) {
    _events.add(event);
    _dispatchBindings(event);
    _dispatchCustomShortcuts(event);
    notifyListeners();
  }

  void _dispatchBindings(InputEvent event) {
    final snapshot = InputSnapshot(
      pressedKeys: event.pressedKeys,
      modifiers: event.modifiers,
      axes: Map.unmodifiable(_axes),
    );

    for (final registered in _bindings.values) {
      final binding = registered.binding;
      final isMatch = _matches(binding, snapshot);
      final wasActive = _activeBindings.contains(binding.id);

      if (isMatch && !wasActive) {
        _activeBindings.add(binding.id);
        if (binding.trigger == InputBindingTrigger.press) {
          registered.callback(event);
        }
      } else if (!isMatch && wasActive) {
        _activeBindings.remove(binding.id);
        if (binding.trigger == InputBindingTrigger.release) {
          registered.callback(event);
        }
      }
    }
  }

  void _dispatchCustomShortcuts(InputEvent event) {
    if (_shortcutsSuspended) return;

    final snapshot = InputSnapshot(
      pressedKeys: event.pressedKeys,
      modifiers: event.modifiers,
      axes: Map.unmodifiable(_axes),
    );

    for (final entry in _customBindings.entries) {
      final actionId = entry.key;
      final binding = entry.value;
      final action = _shortcutActions[actionId];
      if (action == null) continue;

      final isMatch = _evaluateCustomBinding(binding, snapshot);
      final wasActive = _customBindingActiveStates[actionId] ?? false;

      if (isMatch && !wasActive) {
        _customBindingActiveStates[actionId] = true;
        try {
          action.onTrigger();
        } catch (e) {
          debugPrint('Error triggering shortcut action $actionId: $e');
        }
      } else if (!isMatch && wasActive) {
        _customBindingActiveStates[actionId] = false;
      }
    }
  }

  bool _evaluateCustomBinding(
    CustomShortcutBinding binding,
    InputSnapshot snapshot,
  ) {
    final activeSlots = <MapEntry<int, InputKeyId>>[];
    for (int i = 0; i < 4; i++) {
      final key = binding.regularKeys[i];
      if (key != null) {
        activeSlots.add(MapEntry(i, key));
      }
    }

    if (activeSlots.isEmpty) return false;

    if (binding.prefixKey != null) {
      final prefixPressed = snapshot.isPressed(binding.prefixKey!);
      if (binding.prefixNegated) {
        if (prefixPressed) return false;
      } else {
        if (!prefixPressed) return false;
      }
    }

    bool result = snapshot.isPressed(activeSlots[0].value);
    for (int j = 1; j < activeSlots.length; j++) {
      final leftIdx = activeSlots[j - 1].key;
      final op = binding.operators[leftIdx];
      final rightVal = snapshot.isPressed(activeSlots[j].value);
      if (op == 'or') {
        result = result || rightVal;
      } else {
        result = result && rightVal;
      }
    }

    return result;
  }

  bool _matches(InputBinding binding, InputSnapshot snapshot) {
    if (!snapshot.modifiers.containsAll(binding.modifiers)) return false;
    if (!snapshot.containsAll(binding.keys)) return false;
    if (!binding.exactKeys) return true;

    final nonModifierKeys = snapshot.pressedKeys.where(
      (key) => _modifierForKey(key) == null,
    );
    return nonModifierKeys.length == binding.keys.length;
  }

  Future<void> loadCustomBindings() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final keys = prefs.getKeys().where(
        (k) => k.startsWith('shortcut_binding_'),
      );
      for (final key in keys) {
        final jsonStr = prefs.getString(key);
        if (jsonStr != null) {
          final map = json.decode(jsonStr);
          final binding = CustomShortcutBinding.fromJson(map);
          _customBindings[binding.actionId] = binding;
        }
      }
      notifyListeners();
    } catch (e) {
      debugPrint('Error loading custom shortcut bindings: $e');
    }
  }

  Future<void> saveCustomBinding(CustomShortcutBinding binding) async {
    _customBindings[binding.actionId] = binding;
    notifyListeners();
    try {
      final prefs = await SharedPreferences.getInstance();
      final jsonStr = json.encode(binding.toJson());
      await prefs.setString('shortcut_binding_${binding.actionId}', jsonStr);
    } catch (e) {
      debugPrint('Error saving custom shortcut binding: $e');
    }
  }

  Future<void> clearCustomBinding(String actionId) async {
    _customBindings.remove(actionId);
    _customBindingActiveStates.remove(actionId);
    notifyListeners();
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove('shortcut_binding_$actionId');
    } catch (e) {
      debugPrint('Error clearing custom shortcut binding: $e');
    }
  }

  @override
  void dispose() {
    _gamepads.dispose();
    _events.close();
    super.dispose();
  }
}

class _RegisteredInputBinding {
  final InputBinding binding;
  final InputBindingCallback callback;

  const _RegisteredInputBinding(this.binding, this.callback);
}
