/// Theme/appearance providers.
/// During Riverpod migration these read from AppState via the bridge.
/// After full migration, ThemeNotifier will own its state directly.
library;

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/app_state_bridge.dart';
import '../app_state.dart'; // for AppThemeMode

// ── Individual read-only providers (rebuild independently) ──

final themeModeProvider = Provider<AppThemeMode>((ref) {
  return ref.watch(appStateProvider.select((s) => s.themeMode));
});

final seedColorProvider = Provider<int>((ref) {
  return ref.watch(appStateProvider.select((s) => s.seedColor));
});

final useSystemColorProvider = Provider<bool>((ref) {
  return ref.watch(appStateProvider.select((s) => s.useSystemColor));
});

final languageProvider = Provider<String>((ref) {
  return ref.watch(appStateProvider.select((s) => s.language));
});

final closeBehaviorProvider = Provider<String>((ref) {
  return ref.watch(appStateProvider.select((s) => s.closeBehavior));
});

// ── Convenience: grouped theme state for app.dart ──

class ThemeSnapshot {
  final AppThemeMode themeMode;
  final int seedColor;
  final bool useSystemColor;
  final String language;
  final String closeBehavior;

  const ThemeSnapshot({
    required this.themeMode,
    required this.seedColor,
    required this.useSystemColor,
    required this.language,
    required this.closeBehavior,
  });
}

final themeSnapshotProvider = Provider<ThemeSnapshot>((ref) {
  return ThemeSnapshot(
    themeMode: ref.watch(themeModeProvider),
    seedColor: ref.watch(seedColorProvider),
    useSystemColor: ref.watch(useSystemColorProvider),
    language: ref.watch(languageProvider),
    closeBehavior: ref.watch(closeBehaviorProvider),
  );
});

// ── Theme write actions (delegates to AppState during transition) ──

class ThemeActions {
  final Ref _ref;
  ThemeActions(this._ref);

  AppState get _state => _ref.read(appStateProvider);

  void setThemeMode(AppThemeMode mode) => _state.setThemeMode(mode);
  void setSeedColor(int color) => _state.setSeedColor(color);
  void setUseSystemColor(bool v) => _state.setUseSystemColor(v);
  void setCloseBehavior(String v) => _state.setCloseBehavior(v);
  void setLanguage(String lang) => _state.setLanguage(lang);
  Future<void> saveAllConfig() => _state.saveAllConfig();
}

final themeActionsProvider = Provider<ThemeActions>((ref) {
  return ThemeActions(ref);
});
