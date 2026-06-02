/// Provider for WsClient, reading from the AppState bridge.
/// In the final state, this will manage its own WsClient lifecycle.

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../services/ws_client.dart';
import 'app_state_bridge.dart';

final wsClientProvider = Provider<WsClient>((ref) {
  return ref.watch(appStateProvider).ws;
});
