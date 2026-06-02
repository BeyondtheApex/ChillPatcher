/// Provider for ApiClient, reading from the AppState bridge.
/// In the final state, this will manage its own ApiClient lifecycle.

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../services/api_client.dart';
import 'app_state_bridge.dart';

final apiClientProvider = Provider<ApiClient>((ref) {
  return ref.watch(appStateProvider).api;
});
