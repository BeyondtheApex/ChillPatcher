import 'dart:async';
import 'dart:convert';

import 'package:desktop_multi_window/desktop_multi_window.dart';

import '../models/input/input_event.dart';

class FloatingPlayerSnapshot {
  final String baseUrl;
  final int seedColor;
  final bool useSystemColor;
  final String themeMode;
  final bool canControl;
  final bool hasTrack;
  final bool isPlaying;
  final String uuid;
  final String title;
  final String artist;
  final double position;
  final double duration;
  final double lastManualX;
  final double lastManualY;

  const FloatingPlayerSnapshot({
    this.baseUrl = '',
    this.seedColor = 0xFF673AB7,
    this.useSystemColor = true,
    this.themeMode = 'system',
    this.canControl = false,
    this.hasTrack = false,
    this.isPlaying = false,
    this.uuid = '',
    this.title = '',
    this.artist = '',
    this.position = 0,
    this.duration = 0,
    this.lastManualX = 0.0,
    this.lastManualY = 0.0,
  });

  factory FloatingPlayerSnapshot.fromJson(Map<String, dynamic> json) {
    return FloatingPlayerSnapshot(
      baseUrl: json['baseUrl'] as String? ?? '',
      seedColor: json['seedColor'] as int? ?? 0xFF673AB7,
      useSystemColor: json['useSystemColor'] as bool? ?? true,
      themeMode: json['themeMode'] as String? ?? 'system',
      canControl: json['canControl'] as bool? ?? false,
      hasTrack: json['hasTrack'] as bool? ?? false,
      isPlaying: json['isPlaying'] as bool? ?? false,
      uuid: json['uuid'] as String? ?? '',
      title: json['title'] as String? ?? '',
      artist: json['artist'] as String? ?? '',
      position: (json['position'] as num?)?.toDouble() ?? 0,
      duration: (json['duration'] as num?)?.toDouble() ?? 0,
      lastManualX: (json['lastManualX'] as num?)?.toDouble() ?? 0.0,
      lastManualY: (json['lastManualY'] as num?)?.toDouble() ?? 0.0,
    );
  }

  Map<String, dynamic> toJson() => {
    'baseUrl': baseUrl,
    'seedColor': seedColor,
    'useSystemColor': useSystemColor,
    'themeMode': themeMode,
    'canControl': canControl,
    'hasTrack': hasTrack,
    'isPlaying': isPlaying,
    'uuid': uuid,
    'title': title,
    'artist': artist,
    'position': position,
    'duration': duration,
    'lastManualX': lastManualX,
    'lastManualY': lastManualY,
  };
}

typedef FloatingPlayerSeekCallback = Future<void> Function(double position);
typedef FloatingPlayerManualPositionCallback =
    Future<void> Function(double x, double y);

class FloatingWindowService {
  FloatingWindowService._();

  static final FloatingWindowService instance = FloatingWindowService._();
  static const playerWindowKey = 'player_rectangle';
  static const controlChannelName = 'omnimix/floating_player/control';

  final _controlChannel = const WindowMethodChannel(
    controlChannelName,
    mode: ChannelMode.unidirectional,
  );

  WindowController? _playerWindow;
  FloatingPlayerSnapshot _latestSnapshot = const FloatingPlayerSnapshot();
  bool _playerVisible = false;
  bool _controlChannelReady = false;
  Future<void>? _pendingCreate;

  Future<void> init({
    required Future<void> Function() onTogglePlayback,
    required Future<void> Function() onPreviousTrack,
    required Future<void> Function() onNextTrack,
    required FloatingPlayerSeekCallback onSeek,
    required FloatingPlayerManualPositionCallback onUpdateManualPosition,
  }) async {
    if (_controlChannelReady) return;
    _controlChannelReady = true;
    await _controlChannel.setMethodCallHandler((call) async {
      switch (call.method) {
        case 'toggle':
          await onTogglePlayback();
          return true;
        case 'previous':
          await onPreviousTrack();
          return true;
        case 'next':
          await onNextTrack();
          return true;
        case 'seek':
          final value = (call.arguments as num?)?.toDouble();
          if (value != null) await onSeek(value);
          return true;
        case 'update_manual_position':
          final args = call.arguments as Map?;
          if (args != null) {
            final x = (args['x'] as num?)?.toDouble() ?? 0.0;
            final y = (args['y'] as num?)?.toDouble() ?? 0.0;
            await onUpdateManualPosition(x, y);
          }
          return true;
        case 'ready':
          await _sendPlayerSnapshot();
          return true;
      }
      return false;
    });
  }

  Future<void> setPlayerVisible(bool visible) async {
    _playerVisible = visible;
    if (visible) {
      await _ensurePlayerWindow();
      await _playerWindow?.show();
      await _sendPlayerSnapshot();
      try {
        await _playerWindow?.invokeMethod('visibility_changed', true);
      } catch (_) {}
    } else {
      try {
        await _playerWindow?.invokeMethod('visibility_changed', false);
      } catch (_) {}
      await _playerWindow?.hide();
    }
  }

  void handleInputEvent(InputEvent event) {
    if (!_playerVisible || _playerWindow == null) return;
    try {
      _playerWindow?.invokeMethod('input_event', event.toJson());
    } catch (_) {}
  }

  Future<void> updatePlayer(FloatingPlayerSnapshot snapshot) async {
    _latestSnapshot = snapshot;
    if (_playerVisible) {
      await _ensurePlayerWindow();
      await _sendPlayerSnapshot();
    }
  }

  Future<void> moveToCenterLeftQuad() async {
    final window = _playerWindow;
    if (window == null || !_playerVisible) return;
    try {
      await window.invokeMethod('move_to_center_left_quad');
    } catch (_) {}
  }

  Future<void> dispose() async {
    await _controlChannel.setMethodCallHandler(null);
    _controlChannelReady = false;
  }

  Future<void> _ensurePlayerWindow() {
    if (_playerWindow != null) return Future.value();
    return _pendingCreate ??= _createPlayerWindow().whenComplete(() {
      _pendingCreate = null;
    });
  }

  Future<void> _createPlayerWindow() async {
    final args = jsonEncode({
      'type': playerWindowKey,
      'snapshot': _latestSnapshot.toJson(),
    });
    _playerWindow = await WindowController.create(
      WindowConfiguration(arguments: args, hiddenAtLaunch: true),
    );
  }

  Future<void> _sendPlayerSnapshot() async {
    final window = _playerWindow;
    if (window == null) return;
    try {
      await window.invokeMethod('update_player', _latestSnapshot.toJson());
    } catch (_) {
      // The child engine may still be starting. The next state tick or
      // explicit ready ping will deliver the latest snapshot.
    }
  }
}
