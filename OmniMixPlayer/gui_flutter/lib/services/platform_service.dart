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

  static String? get backendExePath => _backendExePath;

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
      final created = await _runElevated('sc.exe', [
        'create',
        _serviceName,
        'binPath=',
        '"$exe"',
        'start=',
        'demand',
      ]);
      if (created) {
        // Configure DACL to allow Authenticated Users (AU) to start (RP), stop (WP), and query/change config (CCDC) without elevation
        await _runElevated('sc.exe', [
          'sdset',
          _serviceName,
          'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)(A;;CCDCRPWP;;;AU)',
        ]);
      }
      return created;
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

  /// Get the currently registered backend service binary path from OS.
  static Future<String?> getServiceBinaryPath() async {
    try {
      if (Platform.isWindows) {
        final result = await Process.run('sc.exe', ['qc', _serviceName]);
        if (result.exitCode != 0) return null;
        final output = result.stdout as String;
        final lines = output.split('\n');
        for (final line in lines) {
          if (line.toUpperCase().contains('BINARY_PATH_NAME')) {
            final parts = line.split(':');
            if (parts.length >= 2) {
              var path = parts.sublist(1).join(':').trim();
              if (path.startsWith('"') && path.endsWith('"')) {
                path = path.substring(1, path.length - 1);
              }
              return path;
            }
          }
        }
      }
      if (Platform.isLinux) {
        final home = Platform.environment['HOME'] ?? '/tmp';
        final file = File('$home/.config/systemd/user/omnimixplayer-backend.service');
        if (file.existsSync()) {
          final content = file.readAsStringSync();
          final lines = content.split('\n');
          for (final line in lines) {
            final trimmedLine = line.trim();
            if (trimmedLine.startsWith('ExecStart=')) {
              var path = trimmedLine.substring('ExecStart='.length).trim();
              if (path.startsWith('"') && path.endsWith('"')) {
                path = path.substring(1, path.length - 1);
              }
              return path;
            }
          }
        }
      }
      if (Platform.isMacOS) {
        final home = Platform.environment['HOME'] ?? '/tmp';
        final file = File('$home/Library/LaunchAgents/com.omnimixplayer.backend.plist');
        if (file.existsSync()) {
          final content = file.readAsStringSync();
          final match = RegExp(r'<key>ProgramArguments</key>\s*<array>\s*<string>([^<]+)</string>')
              .firstMatch(content);
          if (match != null) {
            return match.group(1);
          }
        }
      }
    } catch (_) {}
    return null;
  }

  /// Normalize and compare two file paths.
  static bool arePathsEqual(String p1, String p2) {
    var n1 = p1.replaceAll('/', '\\').trim();
    var n2 = p2.replaceAll('/', '\\').trim();
    if (Platform.isWindows || Platform.isMacOS) {
      return n1.toLowerCase() == n2.toLowerCase();
    }
    return n1 == n2;
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

  /// Check if the service is configured to start automatically.
  static Future<bool> isServiceAutoStart() async {
    if (Platform.isWindows) {
      final result = await Process.run('sc.exe', ['qc', _serviceName]);
      if (result.exitCode != 0) return false;
      final output = (result.stdout as String).toLowerCase();
      return output.contains('auto_start');
    }
    if (Platform.isLinux) {
      final result = await Process.run('systemctl', [
        '--user',
        'is-enabled',
        'omnimixplayer-backend',
      ]);
      return (result.stdout as String).trim() == 'enabled';
    }
    if (Platform.isMacOS) {
      final home = Platform.environment['HOME'] ?? '/tmp';
      final file = File('$home/Library/LaunchAgents/com.omnimixplayer.backend.plist');
      if (!file.existsSync()) return false;
      final content = file.readAsStringSync();
      return content.contains('<key>RunAtLoad</key><true/>') ||
          content.contains('<key>RunAtLoad</key>\n<true/>') ||
          content.contains('<key>RunAtLoad</key>\n\t<true/>') ||
          content.contains('<key>RunAtLoad</key>\n\t\t<true/>');
    }
    return false;
  }

  /// Configure the service to start automatically or manually.
  static Future<bool> setServiceAutoStart(bool autoStart) async {
    if (Platform.isWindows) {
      final startType = autoStart ? 'auto' : 'demand';
      var result = await Process.run('sc.exe', ['config', _serviceName, 'start=', startType]);
      if (result.exitCode != 0) {
        // Fallback to elevated if normal config fails
        return await _runElevated('sc.exe', ['config', _serviceName, 'start=', startType]);
      }
      return result.exitCode == 0;
    }
    if (Platform.isLinux) {
      final action = autoStart ? 'enable' : 'disable';
      final result = await Process.run('systemctl', [
        '--user',
        action,
        'omnimixplayer-backend',
      ]);
      return result.exitCode == 0;
    }
    if (Platform.isMacOS) {
      final exe = _backendExePath;
      if (exe == null) return false;
      final home = Platform.environment['HOME'] ?? '/tmp';
      final file = File('$home/Library/LaunchAgents/com.omnimixplayer.backend.plist');
      file.writeAsStringSync(
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        '<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" '
        '"http://www.apple.com/DTDs/PropertyList-1.0.dtd">\n'
        '<plist version="1.0"><dict>\n'
        '<key>Label</key><string>com.omnimixplayer.backend</string>\n'
        '<key>ProgramArguments</key><array><string>$exe</string></array>\n'
        '<key>RunAtLoad</key>${autoStart ? '<true/>' : '<false/>'}\n'
        '<key>KeepAlive</key><true/>\n'
        '</dict></plist>\n',
      );
      return true;
    }
    return false;
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
