import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:http/io_client.dart';/// Creates an [HttpClient] that routes all traffic through a Unix domain socket.
/// The [socketPath] must be an absolute filesystem path.
///
/// The HTTP host/port in Uri are ignored — all traffic goes through the socket.
HttpClient createUnixSocketClient(String socketPath) {
  final client = HttpClient();
  client.connectionFactory = (url, proxyHost, proxyPort) {
    return Socket.startConnect(
      InternetAddress(socketPath, type: InternetAddressType.unix),
      0, // port ignored for Unix sockets
    );
  };
  return client;
}

/// Wraps [createUnixSocketClient] in [IOClient] for convenience with the `http` package.
http.Client createUnixHttpClient(String socketPath) {
  return IOClient(createUnixSocketClient(socketPath));
}

/// Connect a WebSocket through a Unix domain socket.
/// Uses `WebSocket.connect(customClient:)` to perform the full HTTP upgrade
/// over the Unix socket.
Future<WebSocket> connectUnixWebSocket(String socketPath, String path) async {
  try {
    final httpClient = createUnixSocketClient(socketPath);
    final wsUrl = 'ws://unix$path';
    final ws = await WebSocket.connect(
      wsUrl,
      customClient: httpClient,
    ).timeout(const Duration(seconds: 10));
    return ws;
  } catch (e, st) {
    rethrow;
  }
}
