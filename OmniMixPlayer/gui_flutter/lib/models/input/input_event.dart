enum InputSource { keyboard, gamepad }

enum InputModifier { shift, control, alt, meta }

enum InputEventType { pressed, released, axisChanged }

enum InputBindingTrigger { press, release }

class InputKeyId {
  final InputSource source;
  final String code;
  final String? deviceId;

  const InputKeyId({required this.source, required this.code, this.deviceId});

  const InputKeyId.keyboard(String code)
    : this(source: InputSource.keyboard, code: code);

  const InputKeyId.gamepadButton(String code, {String? deviceId})
    : this(source: InputSource.gamepad, code: code, deviceId: deviceId);

  InputKeyId forAnyDevice() => InputKeyId(source: source, code: code);

  bool matches(InputKeyId other) {
    if (source != other.source || code != other.code) return false;
    return deviceId == null ||
        other.deviceId == null ||
        deviceId == other.deviceId;
  }

  @override
  bool operator ==(Object other) {
    return other is InputKeyId &&
        source == other.source &&
        code == other.code &&
        deviceId == other.deviceId;
  }

  @override
  int get hashCode => Object.hash(source, code, deviceId);

  @override
  String toString() {
    final prefix = source.name;
    return deviceId == null ? '$prefix:$code' : '$prefix:$deviceId:$code';
  }

  Map<String, dynamic> toJson() => {
    'source': source.name,
    'code': code,
    'deviceId': deviceId,
  };

  factory InputKeyId.fromJson(Map<String, dynamic> json) {
    return InputKeyId(
      source: InputSource.values.firstWhere((e) => e.name == json['source']),
      code: json['code'] as String,
      deviceId: json['deviceId'] as String?,
    );
  }
}

class InputAxisId {
  final InputSource source;
  final String code;
  final String? deviceId;

  const InputAxisId({required this.source, required this.code, this.deviceId});

  const InputAxisId.gamepadAxis(String code, {String? deviceId})
    : this(source: InputSource.gamepad, code: code, deviceId: deviceId);

  @override
  bool operator ==(Object other) {
    return other is InputAxisId &&
        source == other.source &&
        code == other.code &&
        deviceId == other.deviceId;
  }

  @override
  int get hashCode => Object.hash(source, code, deviceId);

  @override
  String toString() {
    final prefix = source.name;
    return deviceId == null ? '$prefix:$code' : '$prefix:$deviceId:$code';
  }
}

class InputEvent {
  final InputEventType type;
  final DateTime timestamp;
  final InputKeyId? key;
  final InputAxisId? axis;
  final double value;
  final Set<InputKeyId> pressedKeys;
  final Set<InputModifier> modifiers;

  const InputEvent({
    required this.type,
    required this.timestamp,
    required this.value,
    required this.pressedKeys,
    required this.modifiers,
    this.key,
    this.axis,
  }) : assert(key != null || axis != null);

  bool get isPressed => type == InputEventType.pressed;
  bool get isReleased => type == InputEventType.released;
  bool get isAxisChanged => type == InputEventType.axisChanged;

  Map<String, dynamic> toJson() => {
    'type': type.name,
    'timestamp': timestamp.toIso8601String(),
    'key': key?.toJson(),
    'axis': axis != null ? {
      'source': axis!.source.name,
      'code': axis!.code,
      'deviceId': axis!.deviceId,
    } : null,
    'value': value,
    'pressedKeys': pressedKeys.map((k) => k.toJson()).toList(),
    'modifiers': modifiers.map((m) => m.name).toList(),
  };

  factory InputEvent.fromJson(Map<String, dynamic> json) {
    final keyJson = json['key'];
    final axisJson = json['axis'];
    final pressedKeysJson = json['pressedKeys'] as List?;
    final modifiersJson = json['modifiers'] as List?;

    return InputEvent(
      type: InputEventType.values.firstWhere((e) => e.name == json['type']),
      timestamp: DateTime.parse(json['timestamp'] as String),
      value: (json['value'] as num).toDouble(),
      key: keyJson != null ? InputKeyId.fromJson(Map<String, dynamic>.from(keyJson)) : null,
      axis: axisJson != null ? InputAxisId(
        source: InputSource.values.firstWhere((e) => e.name == axisJson['source']),
        code: axisJson['code'] as String,
        deviceId: axisJson['deviceId'] as String?,
      ) : null,
      pressedKeys: pressedKeysJson != null
          ? pressedKeysJson.map((k) => InputKeyId.fromJson(Map<String, dynamic>.from(k))).toSet()
          : {},
      modifiers: modifiersJson != null
          ? modifiersJson.map((m) => InputModifier.values.firstWhere((e) => e.name == m)).toSet()
          : {},
    );
  }
}

class InputSnapshot {
  final Set<InputKeyId> pressedKeys;
  final Set<InputModifier> modifiers;
  final Map<InputAxisId, double> axes;

  const InputSnapshot({
    required this.pressedKeys,
    required this.modifiers,
    required this.axes,
  });

  bool isPressed(InputKeyId key) => pressedKeys.any(key.matches);

  bool containsAll(Iterable<InputKeyId> keys) {
    return keys.every(isPressed);
  }
}

class InputBinding {
  final String id;
  final Set<InputKeyId> keys;
  final Set<InputModifier> modifiers;
  final bool exactKeys;
  final InputBindingTrigger trigger;

  const InputBinding({
    required this.id,
    required this.keys,
    this.modifiers = const {},
    this.exactKeys = false,
    this.trigger = InputBindingTrigger.press,
  });
}

class CustomShortcutBinding {
  final String actionId;
  final InputKeyId? prefixKey;
  final bool prefixNegated;
  final List<InputKeyId?> regularKeys; // Length 4
  final List<String> operators; // Length 3 ('and' or 'or')

  CustomShortcutBinding({
    required this.actionId,
    this.prefixKey,
    this.prefixNegated = false,
    List<InputKeyId?>? regularKeys,
    List<String>? operators,
  }) : regularKeys = regularKeys ?? List.filled(4, null),
       operators = operators ?? List.filled(3, 'and');

  Map<String, dynamic> toJson() {
    return {
      'actionId': actionId,
      'prefixKey': prefixKey != null
          ? {
              'source': prefixKey!.source.name,
              'code': prefixKey!.code,
              'deviceId': prefixKey!.deviceId
            }
          : null,
      'prefixNegated': prefixNegated,
      'regularKeys': regularKeys
          .map((k) => k != null
              ? {
                  'source': k.source.name,
                  'code': k.code,
                  'deviceId': k.deviceId
                }
              : null)
          .toList(),
      'operators': operators,
    };
  }

  factory CustomShortcutBinding.fromJson(Map<String, dynamic> json) {
    final prefixKeyJson = json['prefixKey'];
    InputKeyId? prefixKey;
    if (prefixKeyJson != null) {
      prefixKey = InputKeyId(
        source: InputSource.values
            .firstWhere((e) => e.name == prefixKeyJson['source']),
        code: prefixKeyJson['code'],
        deviceId: prefixKeyJson['deviceId'],
      );
    }

    final regularKeysList = json['regularKeys'] as List;
    final List<InputKeyId?> regularKeys = regularKeysList.map((kJson) {
      if (kJson == null) return null;
      return InputKeyId(
        source: InputSource.values
            .firstWhere((e) => e.name == kJson['source']),
        code: kJson['code'],
        deviceId: kJson['deviceId'],
      );
    }).toList();

    while (regularKeys.length < 4) {
      regularKeys.add(null);
    }

    final operatorsList = List<String>.from(json['operators'] ?? ['and', 'and', 'and']);
    while (operatorsList.length < 3) {
      operatorsList.add('and');
    }

    return CustomShortcutBinding(
      actionId: json['actionId'],
      prefixKey: prefixKey,
      prefixNegated: json['prefixNegated'] ?? false,
      regularKeys: regularKeys,
      operators: operatorsList,
    );
  }

  CustomShortcutBinding copyWith({
    InputKeyId? prefixKey,
    bool? prefixNegated,
    List<InputKeyId?>? regularKeys,
    List<String>? operators,
  }) {
    return CustomShortcutBinding(
      actionId: actionId,
      prefixKey: prefixKey ?? this.prefixKey,
      prefixNegated: prefixNegated ?? this.prefixNegated,
      regularKeys: regularKeys ?? List.from(this.regularKeys),
      operators: operators ?? List.from(this.operators),
    );
  }
}
