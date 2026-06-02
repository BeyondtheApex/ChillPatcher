abstract class ScriptGenerator {
  String get extension; // e.g. '.bat', '.sh', '.ps1'

  String generateInstallScript({
    required String gameDir,
    required String tempDir,
    required String managerModsDir,
    required List<String> addedFiles,
    required List<String> linkFiles,
    required Map<String, bool> isDirectoryMap,
  });

  String generateUninstallScript({
    required String gameDir,
    required String backupDir,
    required List<String> addedFiles,
    required List<String> linkFiles,
    required List<String> backupFiles,
    required Map<String, bool> isDirectoryMap,
    required String gameVersion,
  });
}

class BatchScriptGenerator extends ScriptGenerator {
  @override
  String get extension => '.bat';

  String _path(String path) => path.replaceAll('/', '\\').replaceAll('%', '%%');

  String _text(String text) => text
      .replaceAll('%', '%%')
      .replaceAll('^', '^^')
      .replaceAll('&', '^&')
      .replaceAll('|', '^|')
      .replaceAll('<', '^<')
      .replaceAll('>', '^>');

  @override
  String generateInstallScript({
    required String gameDir,
    required String tempDir,
    required String managerModsDir,
    required List<String> addedFiles,
    required List<String> linkFiles,
    required Map<String, bool> isDirectoryMap,
  }) {
    final buffer = StringBuffer();
    buffer.writeln('@echo off');
    buffer.writeln('setlocal DisableDelayedExpansion');
    buffer.writeln('set "LOG_FILE=%~dp0install.log"');
    buffer.writeln('echo [INFO] Starting installation... > "%LOG_FILE%"');
    buffer.writeln('echo [INFO] Game Path: "${_text(gameDir)}" >> "%LOG_FILE%"');
    buffer.writeln('echo [INFO] Source Path: %~dp0 >> "%LOG_FILE%"');

    // 1. Copy added files
    for (final relPath in addedFiles) {
      final winRelPath = _path(relPath);
      final winGamePath = _path('$gameDir\\$winRelPath');
      final winGameDir = winGamePath.substring(0, winGamePath.lastIndexOf('\\'));

      buffer.writeln('echo [INFO] Ensuring directory exists: "${_text(winGameDir)}" >> "%LOG_FILE%"');
      buffer.writeln('mkdir "$winGameDir" 2>nul');
      buffer.writeln('echo [INFO] Copying added file: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('copy /Y "%~dp0$winRelPath" "$winGamePath" >> "%LOG_FILE%" 2>&1');
      buffer.writeln('if errorlevel 1 (');
      buffer.writeln('  echo [ERROR] Failed to copy: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('  goto ERROR');
      buffer.writeln(')');
    }

    // 2. Create link files
    for (final relPath in linkFiles) {
      final winRelPath = _path(relPath);
      final winGamePath = _path('$gameDir\\$winRelPath');
      final winGameDir = winGamePath.substring(0, winGamePath.lastIndexOf('\\'));
      final isDir = isDirectoryMap[relPath] ?? false;
      final winTarget = _path('$managerModsDir\\$winRelPath');

      buffer.writeln('echo [INFO] Ensuring directory exists: "${_text(winGameDir)}" >> "%LOG_FILE%"');
      buffer.writeln('mkdir "$winGameDir" 2>nul');
      buffer.writeln('echo [INFO] Creating link: "${_text(winRelPath)}" -^> "${_text(winTarget)}" >> "%LOG_FILE%"');

      // Delete existing file/link first
      buffer.writeln('if exist "$winGamePath" (');
      if (isDir) {
        buffer.writeln('  rmdir /S /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
        buffer.writeln('  if exist "$winGamePath" del /F /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
      } else {
        buffer.writeln('  del /F /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
      }
      buffer.writeln(')');
      // Broken symlink cleanup
      if (isDir) {
        buffer.writeln('rmdir "$winGamePath" 2>nul');
      } else {
        buffer.writeln('del /F /Q "$winGamePath" 2>nul');
      }

      if (isDir) {
        buffer.writeln('mklink /D "$winGamePath" "$winTarget" >> "%LOG_FILE%" 2>&1');
      } else {
        buffer.writeln('mklink "$winGamePath" "$winTarget" >> "%LOG_FILE%" 2>&1');
      }

      buffer.writeln('if errorlevel 1 (');
      buffer.writeln('  echo [ERROR] Failed to link: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('  goto ERROR');
      buffer.writeln(')');
    }

    buffer.writeln('echo [INFO] Installation completed successfully. >> "%LOG_FILE%"');
    buffer.writeln('echo SUCCESS >> "%LOG_FILE%"');
    buffer.writeln('exit /b 0');
    buffer.writeln(':ERROR');
    buffer.writeln('echo [ERROR] Installation failed. >> "%LOG_FILE%"');
    buffer.writeln('echo FAILED >> "%LOG_FILE%"');
    buffer.writeln('exit /b 1');

    return buffer.toString();
  }

  @override
  String generateUninstallScript({
    required String gameDir,
    required String backupDir,
    required List<String> addedFiles,
    required List<String> linkFiles,
    required List<String> backupFiles,
    required Map<String, bool> isDirectoryMap,
    required String gameVersion,
  }) {
    final buffer = StringBuffer();
    buffer.writeln('@echo off');
    buffer.writeln('setlocal DisableDelayedExpansion');
    buffer.writeln('set "LOG_FILE=%~dp0uninstall.log"');
    buffer.writeln('echo [INFO] Starting uninstallation... > "%LOG_FILE%"');
    buffer.writeln('echo [INFO] Game Path: "${_text(gameDir)}" >> "%LOG_FILE%"');

    // 1. Delete added files (in reverse order)
    for (final relPath in addedFiles.reversed) {
      final winRelPath = _path(relPath);
      final winGamePath = _path('$gameDir\\$winRelPath');
      final isDir = isDirectoryMap[relPath] ?? false;

      buffer.writeln('echo [INFO] Deleting added: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('if exist "$winGamePath" (');
      if (isDir) {
        buffer.writeln('  rmdir /S /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
      } else {
        buffer.writeln('  del /F /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
      }
      buffer.writeln(')');
    }

    // 2. Delete links (in reverse order)
    for (final relPath in linkFiles.reversed) {
      final winRelPath = _path(relPath);
      final winGamePath = _path('$gameDir\\$winRelPath');
      final isDir = isDirectoryMap[relPath] ?? false;

      buffer.writeln('echo [INFO] Deleting link: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('if exist "$winGamePath" (');
      if (isDir) {
        buffer.writeln('  rmdir /S /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
        buffer.writeln('  if exist "$winGamePath" del /F /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
      } else {
        buffer.writeln('  del /F /Q "$winGamePath" >> "%LOG_FILE%" 2>&1');
      }
      buffer.writeln(')');
      // Broken link cleanup
      if (isDir) {
        buffer.writeln('rmdir "$winGamePath" 2>nul');
      } else {
        buffer.writeln('del /F /Q "$winGamePath" 2>nul');
      }
    }

    // 3. Restore backups
    for (final relPath in backupFiles) {
      final winRelPath = _path(relPath);
      final winBackupPath = _path('$backupDir\\$winRelPath.v$gameVersion.bak');
      final winGamePath = _path('$gameDir\\$winRelPath');
      final winGameDir = winGamePath.substring(0, winGamePath.lastIndexOf('\\'));

      buffer.writeln('echo [INFO] Restoring backup: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('if exist "$winBackupPath" (');
      buffer.writeln('  mkdir "$winGameDir" 2>nul');
      buffer.writeln('  move /Y "$winBackupPath" "$winGamePath" >> "%LOG_FILE%" 2>&1');
      buffer.writeln('  if errorlevel 1 (');
      buffer.writeln('    echo [ERROR] Failed to restore backup: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln('  )');
      buffer.writeln(') else (');
      buffer.writeln('  echo [WARNING] Backup not found: "${_text(winRelPath)}" >> "%LOG_FILE%"');
      buffer.writeln(')');
    }

    buffer.writeln('echo [INFO] Uninstallation completed successfully. >> "%LOG_FILE%"');
    buffer.writeln('echo SUCCESS >> "%LOG_FILE%"');
    buffer.writeln('exit /b 0');

    return buffer.toString();
  }
}
