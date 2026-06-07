/// Web stub for unix_socket_client.dart — no-op on web platform.
/// On web, all HTTP/WS connections use same-origin relative URLs.
library;

import 'package:http/http.dart' as http;

/// Web stub: creates a plain HTTP client (Unix sockets not available on web).
http.Client createUnixHttpClient(String _) => http.Client();

/// Web stub: Unix socket WebSocket is not available on web.
/// WebSocket connections use WsClient.forWeb() with relative paths instead.
Future<dynamic> connectUnixWebSocket(String _, String _) async {
  throw UnsupportedError('Unix socket WebSocket not available on web');
}
