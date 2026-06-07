/// Navigation tab state provider.
/// During transition reads from AppState; after migration becomes standalone.
library;

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/app_state_bridge.dart';

final currentTabProvider = Provider<int>((ref) {
  return ref.watch(appStateProvider.select((s) => s.currentTab));
});

/// Select a tab, delegating to AppState during transition.
void selectTab(Ref ref, int tab) {
  ref.read(appStateProvider).selectTab(tab);
}
