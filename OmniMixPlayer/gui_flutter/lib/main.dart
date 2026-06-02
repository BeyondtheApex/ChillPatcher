import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:desktop_multi_window/desktop_multi_window.dart';
import 'package:window_manager/window_manager.dart';
import 'floating/floating_player_window.dart';
import 'providers/app_state.dart';
import 'providers/core/app_state_bridge.dart';
import 'services/tray_manager.dart';
import 'services/port_file.dart';
import 'services/mod_deployment_service.dart';
import 'app.dart';

void main(List<String> args) async {
  WidgetsFlutterBinding.ensureInitialized();

  // Must initialize window_manager BEFORE desktop_multi_window takes over
  // the window, otherwise setPreventClose cannot intercept WM_CLOSE.
  await windowManager.ensureInitialized();
  await windowManager.setPreventClose(true);

  final currentWindow = await WindowController.fromCurrentEngine();
  if (_isFloatingPlayerWindow(currentWindow.arguments)) {
    runApp(
      FloatingPlayerWindowApp(
        controller: currentWindow,
        initialSnapshot: floatingPlayerSnapshotFromArguments(
          currentWindow.arguments,
        ),
      ),
    );
    return;
  }

  // Read IPC port from port file (written by backend)
  final port = PortFile.readPort();

  await windowManager.setTitle('OmniMixPlayer');
  await windowManager.setMinimumSize(const Size(400, 500));
  await windowManager.setSize(const Size(900, 650));
  await windowManager.center();
  await windowManager.show();

  // Pre-load latest mod version from assets or playerbuild/ version_info.json
  await ModDeploymentService.loadLatestModVersion();

  final state = AppState();
  state.init(port: port);

  // Handle window close event — minimize to tray or exit based on setting
  windowManager.addListener(_CloseHandler(state));

  // System tray (desktop only)
  if (Platform.isWindows || Platform.isLinux || Platform.isMacOS) {
    final tray = TrayManager();
    // Detect system language for tray menu labels (before Flutter app is ready)
    final isZh = Platform.localeName.startsWith('zh');
    final showHideLabel = isZh ? '显示/隐藏窗口' : 'Show/Hide Window';
    final exitGuiLabel = isZh ? '退出 GUI' : 'Exit GUI';
    final exitLabel = isZh ? '完全退出' : 'Fully Exit';

    await tray.init(
      showHideLabel: showHideLabel,
      exitGuiLabel: exitGuiLabel,
      exitLabel: exitLabel,
      onShowHide: () async {
        final isVisible = await windowManager.isVisible();
        if (isVisible) {
          await windowManager.hide();
        } else {
          await windowManager.show();
          await windowManager.focus();
        }
      },
      onExitGui: () async {
        await tray.dispose();
        exit(0);
      },
      onExit: () async {
        await state.fullQuit();
        await tray.dispose();
        exit(0);
      },
    );
  }

  runApp(
    ProviderScope(
      overrides: [appStateProvider.overrideWith((ref) => state)],
      child: const OmniMixApp(),
    ),
  );
}

bool _isFloatingPlayerWindow(String arguments) {
  if (arguments.isEmpty) return false;
  return arguments.contains('"type":"player_rectangle"') ||
      arguments.contains('"type": "player_rectangle"');
}

/// WindowListener that minimizes to tray or exits based on AppState setting.
class _CloseHandler with WindowListener {
  final AppState state;
  _CloseHandler(this.state);

  @override
  void onWindowClose() {
    if (state.closeBehavior == 'minimize') {
      windowManager.hide();
    } else {
      // Exit GUI only — keep backend running
      if (Platform.isWindows || Platform.isLinux || Platform.isMacOS) {
        exit(0);
      }
    }
  }
}
