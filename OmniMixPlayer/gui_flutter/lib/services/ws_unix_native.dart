/// Native (dart:io) implementation of Unix socket WebSocket connection.
/// This file is only compiled on native platforms (Windows, Linux, macOS).
/// On web, the stub in ws_unix_web.dart is used instead (never called).
library;

import 'package:web_socket_channel/io.dart';
import 'package:web_socket_channel/web_socket_channel.dart';
import 'unix_socket_client.dart';/// Connect to the backend via a Unix domain socket.
/// Returns a [WebSocketChannel] connected to the given [socketPath].
Future<WebSocketChannel> connectViaUnixSocket(String socketPath) async {
  try {
    final ws = await connectUnixWebSocket(socketPath, '/ws');
    final channel = IOWebSocketChannel(ws);
    await channel.ready;
    return channel;
  } catch (e) {
    rethrow;
  }
}
