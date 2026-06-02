/// Bridge provider that exposes the existing AppState instance to the
/// Riverpod world. During the incremental migration, this is the single
/// channel through which new Riverpod providers can read/write state that
/// still lives in AppState.
///
/// Override this in main.dart with the live AppState instance.
///
/// Uses ChangeNotifierProvider so that when AppState calls notifyListeners(),
/// all dependent Riverpod providers automatically rebuild.

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../app_state.dart';

final appStateProvider = ChangeNotifierProvider<AppState>((ref) {
  throw UnimplementedError(
    'appStateProvider must be overridden in main.dart / main_web.dart '
    'via ProviderScope(overrides: [appStateProvider.overrideWithValue(state)])',
  );
});
