import 'dart:io';
import 'package:flutter/material.dart';
import 'package:window_manager/window_manager.dart';
import 'providers/app_state.dart';
import 'services/tray_manager.dart';
import 'services/logger.dart';
import 'services/port_file.dart';
import 'services/mod_deployment_service.dart';
import 'app.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  GuiLogger().init();
  final log = GuiLogger();

  // Read IPC port from port file (written by backend)
  final port = PortFile.readPort();
  log.conn('portFile port=$port');

  // Desktop window setup
  await windowManager.ensureInitialized();

  // Prevent close by default — window_manager intercepts the X button
  await windowManager.setPreventClose(true);

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

  runApp(OmniMixApp(state: state));
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
