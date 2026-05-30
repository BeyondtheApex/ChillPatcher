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
    required VoidCallback onExitGui,
    String showHideLabel = 'Show/Hide Window',
    String exitGuiLabel = 'Exit GUI',
    String exitLabel = 'Fully Exit',
  }) async {
    if (!Platform.isWindows && !Platform.isLinux && !Platform.isMacOS) return;

    try {
      await _tray.initSystemTray(
        title: 'OmniMixPlayer',
        iconPath: 'assets/tray_icon.ico',
        toolTip: 'OmniMixPlayer',
      );

      final menu = Menu();
      await menu.buildFrom([
        MenuItemLabel(label: showHideLabel, onClicked: (_) => onShowHide()),
        MenuSeparator(),
        MenuItemLabel(label: exitGuiLabel, onClicked: (_) => onExitGui()),
        MenuItemLabel(label: exitLabel, onClicked: (_) => onExit()),
      ]);

      await _tray.setContextMenu(menu);
      _tray.registerSystemTrayEventHandler((event) {
        if (event == 'left-click' || event == 'click') {
          onShowHide();
        } else if (event == 'right-click') {
          _tray.popUpContextMenu();
        }
      });

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
