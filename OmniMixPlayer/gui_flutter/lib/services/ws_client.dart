import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:web_socket_channel/web_socket_channel.dart';
import 'package:web_socket_channel/io.dart';
import '../models/node_data.dart';
import 'unix_socket_client.dart';
import 'logger.dart';

/// WebSocket client that talks to the C# backend.
/// Supports TCP (primary) and Unix Domain Socket (fallback).
class WsClient {
  final String _wsUrl;
  final String? _socketPath;
  WebSocketChannel? _channel;
  StreamSubscription? _sub;
  bool _intentionalClose = false;

  void Function(WsEvent)? onEvent;
  void Function(UiPushPayload)? onUiPush;
  void Function()? onDisconnected;

  /// TCP mode.
  WsClient({required int port})
    : _wsUrl = 'ws://127.0.0.1:$port/ws',
      _socketPath = null;

  /// Unix socket mode.
  WsClient.withSocket({required String socketPath})
    : _wsUrl = 'ws://unix/ws',
      _socketPath = socketPath;

  bool get isConnected => _channel != null;

  /// Single connection attempt. Returns true on success.
  Future<bool> connectOnce() async {
    _intentionalClose = false;
    GuiLogger().conn('WsClient.connectOnce: $_wsUrl socket=$_socketPath');
    try {
      WebSocket ws;
      if (_socketPath != null) {
        final httpClient = createUnixSocketClient(_socketPath!);
        ws = await WebSocket.connect(
          _wsUrl,
          customClient: httpClient,
        ).timeout(const Duration(seconds: 10));
      } else {
        ws = await WebSocket.connect(
          _wsUrl,
        ).timeout(const Duration(seconds: 10));
      }
      _channel = IOWebSocketChannel(ws);
      await _channel!.ready;
      GuiLogger().conn('WsClient.connectOnce: WebSocket ready, listening...');
      _sub = _channel!.stream.listen(
        (data) => _handleMessage(data as String),
        onDone: () {
          GuiLogger().conn('WsClient stream onDone');
          _cleanup();
          if (!_intentionalClose) onDisconnected?.call();
        },
        onError: (e) {
          GuiLogger().error('WsClient stream onError', e);
          _cleanup();
          if (!_intentionalClose) onDisconnected?.call();
        },
      );
      return true;
    } catch (e, st) {
      GuiLogger().error('WsClient.connectOnce FAILED', e, st);
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

  void _handleMessage(String text) {
    try {
      final jsonMap = json.decode(text) as Map<String, dynamic>;
      final type = jsonMap['type'] as String? ?? '';

      if (type == 'ui_push') {
        // BroadcastEvent 包裹在 data 字段中，需要解包
        final inner = (jsonMap['data'] as Map<String, dynamic>?) ?? jsonMap;
        final push = UiPushPayload.fromJson(inner);
        onUiPush?.call(push);
      } else {
        final event = WsEvent.fromJson(jsonMap);
        onEvent?.call(event);
      }
    } catch (_) {}
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
      GuiLogger().warn(
        'sendUiEvent: channel is null, dropped event nodeId=$nodeId action=$action',
      );
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
      GuiLogger().info('sendUiEvent: $msg');
      _channel!.sink.add(msg);
    } catch (e, st) {
      GuiLogger().error('sendUiEvent FAILED', e, st);
    }
  }

  void disconnect() {
    _intentionalClose = true;
    _cleanup();
  }
}
