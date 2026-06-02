/// Error state utilities.
/// During transition, error display is still handled by app.dart's listener.
/// These utilities are available for future ConsumerWidget migration.

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/app_state_bridge.dart';

/// Consume (read-and-clear) the last error from AppState.
/// Returns the error message or null if no error was pending.
String? consumeLastError(Ref ref) {
  return ref.read(appStateProvider).consumeError();
}
