import 'dart:async';
import 'dart:convert' hide json;
import '../utils/json_utils.dart';
import 'package:web_socket_channel/web_socket_channel.dart';
import '../generated/omni_mix_player/events/ws_events.pb.dart';
import '../models/node_data.dart';
import 'ws_unix_native.dart'
    if (dart.library.js_interop) '../stubs/ws_unix_web.dart';

/// WebSocket client that talks to the C# backend.
/// Supports TCP (primary) and Unix Domain Socket (fallback).
///
/// Protocol:
///   - Binary frames = protobuf WsEvent (playback/instance events)
///   - Text frames   = JSON ui_push events (module UI)
class WsClient {
  final String _wsUrl;
  final String? _socketPath;
  WebSocketChannel? _channel;
  StreamSubscription? _sub;
  bool _intentionalClose = false;

  /// Protobuf playback/instance events (binary frames).
  void Function(WsEvent event)? onProtoEvent;

  /// JSON UI push events (text frames, module UI).
  void Function(UiPushPayload payload)? onUiPush;

  /// Legacy JSON events (text frames, non-ui).
  void Function(Map<String, dynamic> json)? onJsonEvent;

  void Function()? onDisconnected;

  /// TCP mode.
  WsClient({required int port})
    : _wsUrl = 'ws://127.0.0.1:$port/ws',
      _socketPath = null;

  /// Unix socket mode.
  WsClient.withSocket({required String socketPath})
    : _wsUrl = 'ws://unix/ws',
      _socketPath = socketPath;

  /// Web mode: build full ws:// or wss:// URL from the current page origin.
  /// The web_socket package's BrowserWebSocket requires an absolute URI with
  /// scheme (ws:// or wss://); relative paths like /ws are rejected.
  WsClient.forWeb() : _wsUrl = _buildWebWsUrl(), _socketPath = null;

  /// Construct a full WebSocket URL from the current page's origin.
  static String _buildWebWsUrl() {
    final base = Uri.base;
    final scheme = base.scheme == 'https' ? 'wss' : 'ws';
    final host = base.host.isNotEmpty ? base.host : 'localhost';
    final port = base.port != 0 ? ':${base.port}' : '';
    return '$scheme://$host$port/ws';
  }

  bool get isConnected => _channel != null;

  /// Single connection attempt. Returns true on success.
  /// Uses web_socket_channel which works on both native (dart:io) and web.
  Future<bool> connectOnce() async {
    _intentionalClose = false;
    try {
      if (_socketPath != null) {
        // Unix socket path — only available on native platforms.
        // connectViaUnixSocket is conditionally imported (web stub throws).
        _channel = await connectViaUnixSocket(_socketPath!);
      } else {
        // TCP mode — WebSocketChannel.connect works on both native and web
        _channel = WebSocketChannel.connect(Uri.parse(_wsUrl));
      }
      await _channel!.ready;
      _sub = _channel!.stream.listen(
        (data) => _handleMessage(data),
        onDone: () {
          _cleanup();
          if (!_intentionalClose) onDisconnected?.call();
        },
        onError: (e) {
          _cleanup();
          if (!_intentionalClose) onDisconnected?.call();
        },
      );
      return true;
    } catch (e, st) {
      _cleanup();
      return false;
    }
  }

  void _cleanup() {
    _sub?.cancel();
    _sub = null;
    _channel?.sink.close();
    _channel = null;
  }

  /// Handle incoming messages. Binary = protobuf WsEvent, Text = JSON.
  void _handleMessage(dynamic data) {
    if (data is List<int>) {
      // Binary frame: protobuf WsEvent
      try {
        final event = WsEvent.fromBuffer(data);
        onProtoEvent?.call(event);
      } catch (_) {}
    } else if (data is String) {
      // Text frame: JSON (ui_push or legacy events)
      try {
        final jsonMap = json.decode(data) as Map<String, dynamic>;
        final type = jsonMap['type'] as String? ?? '';

        if (type == 'ui_push') {
          final inner = (jsonMap['data'] as Map<String, dynamic>?) ?? jsonMap;
          final push = UiPushPayload.fromJson(inner);
          onUiPush?.call(push);
        } else {
          onJsonEvent?.call(jsonMap);
        }
      } catch (_) {}
    }
  }

  Future<void> sendUiEvent(
    String moduleId,
    String nodeId,
    String action,
    String value, {
    String uiKind = 'default',
    String linkId = '',
  }) async {
    if (_channel == null) {
      return;
    }
    try {
      final msg = json.encode({
        'type': 'ui_event',
        'moduleId': moduleId,
        'uiKind': uiKind,
        'linkId': linkId,
        'event': {'nodeId': nodeId, 'action': action, 'value': value},
      });
      _channel!.sink.add(msg);
    } catch (e, st) {}
  }

  void disconnect() {
    _intentionalClose = true;
    _cleanup();
  }
}
