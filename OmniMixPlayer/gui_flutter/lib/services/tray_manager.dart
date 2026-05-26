import 'dart:io';
import 'package:flutter/material.dart';
import 'package:system_tray/system_tray.dart';
import 'package:window_manager/window_manager.dart';

class TrayManager {
  final SystemTray _tray = SystemTray();
  bool _initialized = false;

  Future<void> init({
    required VoidCallback onShowHide,
    required VoidCallback onExit,
  }) async {
    if (!Platform.isWindows && !Platform.isLinux && !Platform.isMacOS) return;

    try {
      await _tray.initSystemTray(
        title: 'OmniMixPlayer',
        iconPath: '', // Will use app icon
        toolTip: 'OmniMixPlayer',
      );

      final menu = Menu();
      await menu.buildFrom([
        MenuItemLabel(
          label: Platform.isMacOS ? 'Show/Hide Window' : '显示/隐藏窗口',
          onClicked: (_) => onShowHide(),
        ),
        MenuSeparator(),
        MenuItemLabel(
          label: Platform.isMacOS ? 'Fully Exit' : '完全退出',
          onClicked: (_) => onExit(),
        ),
      ]);

      await _tray.setContextMenu(menu);
      _tray.registerSystemTrayEventHandler((event) {});

      _initialized = true;
    } catch (e) {
      debugPrint('Tray init failed: $e');
    }
  }

  Future<void> dispose() async {
    if (_initialized) {
      await _tray.destroy();
    }
  }
}

/// Windows-specific: minimize to tray instead of closing.
class WindowHelper {
  static Future<void> setup() async {
    // Prevent actual window close; user must use tray menu to fully exit
    await windowManager.setPreventClose(true);
  }
}
