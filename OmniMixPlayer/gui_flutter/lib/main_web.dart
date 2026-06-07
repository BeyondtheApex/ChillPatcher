/// Web entry point for OmniMixPlayer Flutter GUI.
///
/// This is a minimal entry point that skips all desktop-specific
/// initialization (window_manager, system_tray, service management, port file
/// discovery). On web, the backend is assumed to be running on the same host
/// and the Flutter app is served by ASP.NET's UseStaticFiles.
///
/// Build: flutter build web --wasm -t lib/main_web.dart
library;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'providers/app_state.dart';
import 'providers/core/app_state_bridge.dart';
import 'app.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  // Web: connect directly to same-origin backend (served by ASP.NET).
  // The backend listens on 0.0.0.0:17890, and the Flutter web app is
  // served from wwwroot/ via UseStaticFiles, so relative URLs work.
  final state = AppState();
  state.initWeb(port: 17890);

  runApp(
    ProviderScope(
      overrides: [appStateProvider.overrideWith((ref) => state)],
      child: const OmniMixApp(),
    ),
  );
}
