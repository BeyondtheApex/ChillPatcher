import 'dart:io';

/// Manages the C# backend as a system service + GUI auto-start.
///
/// Architecture:
///   Flutter GUI ──HTTP──► C# Backend ◄──HTTP/SharedMem── 游戏 Mod
///   (管理面板)            (持久服务)                      (ChillPatcher)
///
/// The SERVICE is always the C# backend (OmniMixPlayer.Backend.exe),
/// NOT the Flutter GUI. Flutter is just one of many clients.
///
/// Two modes:
///   "service"  → backend runs as OS service, auto-starts with system.
///                 Game mod can use it even without Flutter running.
///   "process"  → Flutter spawns/kills backend as child process.
///                 Auto-start needs to launch Flutter GUI.
///
/// On Windows, service operations (install/uninstall) use UAC elevation
/// via PowerShell Start-Process -Verb RunAs.
class PlatformService {
  static const _serviceName = 'OmniMixPlayerBackend';

  // ── Find backend exe ──

  static String? get _backendExePath {
    final exeName = Platform.isWindows
        ? 'OmniMixPlayer.Backend.exe'
        : 'OmniMixPlayer.Backend';
    final guiDir = File(Platform.resolvedExecutable).parent.path;
    final sep = Platform.pathSeparator;

    for (final c in [
      '$guiDir$sep$exeName',
      '$guiDir${sep}..${sep}OmniMixPlayer.Backend$sep$exeName',
      '$guiDir${sep}..${sep}..${sep}..${sep}..${sep}OmniMixPlayer.Backend${sep}bin${sep}Release${sep}net8.0$sep$exeName',
    ]) {
      if (File(c).existsSync()) return c;
    }
    return null;
  }

  // ═══════════════════════════════════════════════════════════
  //  Windows: elevated helper
  // ═══════════════════════════════════════════════════════════

  /// Run a command elevated via PowerShell Start-Process -Verb RunAs.
  /// cmd should be like: 'sc.exe', args like: ['create', 'MyService', 'binPath=', '...']
  static Future<bool> _runElevated(String cmd, List<String> args) async {
    if (!Platform.isWindows) return false;

    // Build argument string for sc.exe
    final argStr = args.map((a) => "'$a'").join(',');
    final psCmd =
        'Start-Process -FilePath \'$cmd\' '
        '-ArgumentList $argStr '
        '-Verb RunAs -Wait -WindowStyle Hidden';

    final result = await Process.run('powershell', ['-Command', psCmd]);
    return result.exitCode == 0;
  }

  // ═══════════════════════════════════════════════════════════
  //  Service (C# Backend)
  // ═══════════════════════════════════════════════════════════

  /// Install C# backend as OS service. MANUAL start only.
  /// Elevates via UAC on Windows.
  static Future<bool> installService() async {
    final exe = _backendExePath;
    if (exe == null) return false;

    if (Platform.isWindows) {
      // Clean up old service first (may fail if not exists, that's fine)
      await _runElevated('sc.exe', ['stop', _serviceName]);
      await _runElevated('sc.exe', ['delete', _serviceName]);
      // demand = manual start (not auto)
      return await _runElevated('sc.exe', [
        'create',
        _serviceName,
        'binPath=',
        '"$exe"',
        'start=',
        'demand',
      ]);
    }

    if (Platform.isLinux) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      final dir = Directory('$home/.config/systemd/user');
      if (!dir.existsSync()) dir.createSync(recursive: true);
      File(
        '$home/.config/systemd/user/omnimixplayer-backend.service',
      ).writeAsStringSync(
        '[Unit]\nDescription=OmniMixPlayer Backend\nAfter=network.target\n\n'
        '[Service]\nExecStart="$exe"\nRestart=on-failure\nRestartSec=5\n\n'
        '[Install]\nWantedBy=default.target\n',
      );
      await Process.run('systemctl', ['--user', 'daemon-reload']);
      return true;
    }

    if (Platform.isMacOS) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      File(
        '$home/Library/LaunchAgents/com.omnimixplayer.backend.plist',
      ).writeAsStringSync(
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        '<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" '
        '"http://www.apple.com/DTDs/PropertyList-1.0.dtd">\n'
        '<plist version="1.0"><dict>\n'
        '<key>Label</key><string>com.omnimixplayer.backend</string>\n'
        '<key>ProgramArguments</key><array><string>$exe</string></array>\n'
        '<key>RunAtLoad</key><false/>\n'
        '<key>KeepAlive</key><true/>\n'
        '</dict></plist>\n',
      );
      return true;
    }

    return false;
  }

  static Future<bool> uninstallService() async {
    if (Platform.isWindows) {
      await _runElevated('sc.exe', ['stop', _serviceName]);
      return await _runElevated('sc.exe', ['delete', _serviceName]);
    }
    if (Platform.isLinux) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      await Process.run('systemctl', [
        '--user',
        'stop',
        'omnimixplayer-backend',
      ]);
      await Process.run('systemctl', [
        '--user',
        'disable',
        'omnimixplayer-backend',
      ]);
      final f = File(
        '$home/.config/systemd/user/omnimixplayer-backend.service',
      );
      if (f.existsSync()) f.deleteSync();
      await Process.run('systemctl', ['--user', 'daemon-reload']);
      return true;
    }
    if (Platform.isMacOS) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      final uid = await _getMacUid();
      await Process.run('launchctl', [
        'bootout',
        'gui/$uid/com.omnimixplayer.backend',
      ]);
      final f = File(
        '$home/Library/LaunchAgents/com.omnimixplayer.backend.plist',
      );
      if (f.existsSync()) f.deleteSync();
      return true;
    }
    return false;
  }

  static Future<bool> startService() async {
    if (Platform.isWindows) {
      return await _runElevated('sc.exe', ['start', _serviceName]);
    }
    if (Platform.isLinux) {
      return (await Process.run('systemctl', [
            '--user',
            'start',
            'omnimixplayer-backend',
          ])).exitCode ==
          0;
    }
    if (Platform.isMacOS) {
      final uid = await _getMacUid();
      return (await Process.run('launchctl', [
            'kickstart',
            'gui/$uid/com.omnimixplayer.backend',
          ])).exitCode ==
          0;
    }
    return false;
  }

  static Future<bool> stopService() async {
    if (Platform.isWindows) {
      return await _runElevated('sc.exe', ['stop', _serviceName]);
    }
    if (Platform.isLinux) {
      return (await Process.run('systemctl', [
            '--user',
            'stop',
            'omnimixplayer-backend',
          ])).exitCode ==
          0;
    }
    if (Platform.isMacOS) {
      final uid = await _getMacUid();
      return (await Process.run('launchctl', [
            'bootout',
            'gui/$uid/com.omnimixplayer.backend',
          ])).exitCode ==
          0;
    }
    return false;
  }

  /// Get macOS current user UID (for launchctl domain).
  static Future<String> _getMacUid() async {
    final result = await Process.run('id', ['-u']);
    return (result.stdout as String).trim();
  }

  // ═══════════════════════════════════════════════════════════
  //  Service status checks
  // ═══════════════════════════════════════════════════════════

  /// Check if the backend service is installed.
  static Future<bool> isServiceInstalled() async {
    if (Platform.isWindows) {
      final result = await Process.run('sc.exe', ['query', _serviceName]);
      return result.exitCode == 0;
    }
    if (Platform.isLinux) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      return File(
        '$home/.config/systemd/user/omnimixplayer-backend.service',
      ).existsSync();
    }
    if (Platform.isMacOS) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      return File(
        '$home/Library/LaunchAgents/com.omnimixplayer.backend.plist',
      ).existsSync();
    }
    return false;
  }

  /// Check if the backend service is currently running.
  static Future<bool> isServiceRunning() async {
    if (Platform.isWindows) {
      final result = await Process.run('sc.exe', ['query', _serviceName]);
      if (result.exitCode != 0) return false;
      final output = (result.stdout as String).toLowerCase();
      return output.contains('running');
    }
    if (Platform.isLinux) {
      final result = await Process.run('systemctl', [
        '--user',
        'is-active',
        'omnimixplayer-backend',
      ]);
      return (result.stdout as String).trim() == 'active';
    }
    if (Platform.isMacOS) {
      final uid = await _getMacUid();
      final result = await Process.run('launchctl', [
        'print',
        'gui/$uid/com.omnimixplayer.backend',
      ]);
      return result.exitCode == 0;
    }
    return false;
  }

  /// Get service state as a string: 'running', 'installed', 'not_installed', 'unknown'
  static Future<String> getServiceState() async {
    final running = await isServiceRunning();
    if (running) return 'running';
    final installed = await isServiceInstalled();
    if (installed) return 'installed';
    return 'not_installed';
  }

  // ═══════════════════════════════════════════════════════════
  //  Auto-start (Flutter GUI)
  // ═══════════════════════════════════════════════════════════

  /// Make the Flutter GUI auto-start with the OS.
  /// Only needed when backend mode = "process" (Flutter spawns it).
  /// When mode = "service", the backend starts on its own.
  static Future<bool> setGuiAutostart(bool enabled) async {
    final guiExe = Platform.resolvedExecutable;

    if (Platform.isWindows) {
      if (enabled) {
        return (await Process.run('reg', [
              'add',
              r'HKCU\Software\Microsoft\Windows\CurrentVersion\Run',
              '/v',
              'OmniMixPlayer',
              '/t',
              'REG_SZ',
              '/d',
              '"$guiExe"',
              '/f',
            ])).exitCode ==
            0;
      } else {
        return (await Process.run('reg', [
              'delete',
              r'HKCU\Software\Microsoft\Windows\CurrentVersion\Run',
              '/v',
              'OmniMixPlayer',
              '/f',
            ])).exitCode ==
            0;
      }
    }

    if (Platform.isLinux) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      final dir = Directory('$home/.config/autostart');
      if (!dir.existsSync()) dir.createSync(recursive: true);
      final f = File('$home/.config/autostart/omnimixplayer.desktop');
      if (enabled) {
        f.writeAsStringSync(
          '[Desktop Entry]\nType=Application\nName=OmniMixPlayer\n'
          'Exec=$guiExe\nHidden=false\nX-GNOME-Autostart-enabled=true\n',
        );
      } else {
        if (f.existsSync()) f.deleteSync();
      }
      return true;
    }

    return false;
  }
}
