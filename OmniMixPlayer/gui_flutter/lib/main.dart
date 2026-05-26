import 'dart:io';
import 'package:flutter/material.dart';
import 'package:window_manager/window_manager.dart';
import 'providers/app_state.dart';
import 'services/tray_manager.dart';
import 'services/logger.dart';
import 'services/port_file.dart';
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
  await WindowHelper.setup();
  await windowManager.setTitle('OmniMixPlayer');
  await windowManager.setMinimumSize(const Size(400, 500));
  await windowManager.setSize(const Size(900, 650));
  await windowManager.center();
  await windowManager.show();

  final state = AppState();
  state.init(port: port);

  // System tray (desktop only)
  if (Platform.isWindows || Platform.isLinux || Platform.isMacOS) {
    final tray = TrayManager();
    await tray.init(
      onShowHide: () async {
        final isVisible = await windowManager.isVisible();
        if (isVisible) {
          await windowManager.hide();
        } else {
          await windowManager.show();
          await windowManager.focus();
        }
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
