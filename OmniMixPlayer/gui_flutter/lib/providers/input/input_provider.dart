import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/app_state_bridge.dart';
import 'input_event_controller.dart';

final inputEventControllerProvider = Provider<InputEventController>((ref) {
  return ref.watch(appStateProvider.select((state) => state.input));
});
