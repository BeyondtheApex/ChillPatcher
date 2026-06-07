/// Provider for BackendManager, reading from the AppState bridge.
library;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../services/backend_manager.dart';
import 'app_state_bridge.dart';

final backendManagerProvider = Provider<BackendManager>((ref) {
  return ref.watch(appStateProvider.select((s) => s.backendMgr));
});
