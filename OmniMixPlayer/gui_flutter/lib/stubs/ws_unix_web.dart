/// Web stub for ws_unix_native.dart — never called on web.
/// Unix sockets are not available in the browser; WebSocket connections
/// use WsClient.forWeb() with same-origin relative paths instead.

import 'package:web_socket_channel/web_socket_channel.dart';

/// Web stub: throws UnsupportedError (code path guarded by _socketPath != null,
/// which is always false for web connections).
Future<WebSocketChannel> connectViaUnixSocket(String _) async {
  throw UnsupportedError('Unix socket WebSocket not available on web');
}
