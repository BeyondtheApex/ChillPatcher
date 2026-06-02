import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../models/mod_manifest.dart';

import '../services/mod_deployment_service.dart';

class Fh6OmniBridgeMod extends ModDeclaration {
  const Fh6OmniBridgeMod()
    : super(
        id: 'fh6_omni_bridge',
        name: 'Forza Horizon 6 Omni Bridge',
        version: '1.0.0',
        archiveName: 'FH6OmniBridge.zip',
        folderName: 'fh6-omnimix',
        rootFilesToLink: const ['version.dll', 'OmniPcmShared.dll'],
        rootFilesNoBackup: const ['version.dll', 'OmniPcmShared.dll'],
        mode: 'server',
      );

  @override
  bool get hasSettings => true;

  @override
  List<String> getFilesToAdd(String gameDir) {
    return _mediaRelativePaths;
  }

  @override
  List<String> getFilesToBackup(String gameDir) {
    return _mediaRelativePaths;
  }

  @override
  String getGameVersion(String gameDir) {
    return _getCurrentGameVersion(gameDir);
  }

  @override
  Future<void> prepareStaging(
    String gameDir,
    String tempDir,
    void Function(String) log,
    Map<String, dynamic> settings,
  ) async {
    // 1. Extract the base mod ZIP to staging
    await extractZipToStaging(tempDir, log);

    final version = getGameVersion(gameDir);
    log('Detected game version: $version');

    // 2. Perform backup of specific media assets to manager backups folder if not exists
    final managerBackupDir = '${ModDeploymentService.managerDir}/backups/$id/v$version';
    final backupDirObj = Directory(managerBackupDir);
    if (!backupDirObj.existsSync()) {
      log('Creating original media backup for version $version in manager backups...');
      backupDirObj.createSync(recursive: true);
      _backupMediaFiles(gameDir, managerBackupDir);
      log('Backup created successfully.');
    } else {
      log('Pristine backup for version $version already exists in manager backups.');
    }

    // 3. Run media generator CLI, outputting to tempDir
    log('Preparing media generator configurations...');
    final configMap = _loadConfigJson();
    final omnimix = (configMap['omnimix'] as Map<String, dynamic>?) ?? {};
    final anthemZip = configMap['anthemZip'] as Map<String, dynamic>?;
    final anthemZipEnabled =
        anthemZip?['enabled'] ?? settings['anthemZipEnabled'] ?? true;
    final logoMode = omnimix['logoMode'] ?? settings['logoMode'] ?? 'copy';
    final customPngPath =
        omnimix['customPngPath'] ?? settings['customPngPath'] ?? '';

    final normalizedBackupDir = managerBackupDir.replaceAll('\\', '/');
    final normalizedOutputDir = tempDir.replaceAll('\\', '/');

    final cliPath = _getCliPath();
    final configPath = _getConfigPath();
    log('Running media generator: $cliPath');
    final args = <String>[
      '-c',
      configPath,
      '-g',
      normalizedBackupDir,
      '-o',
      normalizedOutputDir,
    ];
    if (logoMode == 'image' && customPngPath.isNotEmpty) {
      args.addAll(['-r', customPngPath]);
    }
    if (!anthemZipEnabled) {
      args.add('--skip-anthem');
    }

    try {
      final result = await Process.run(cliPath, args);
      if (result.exitCode == 0) {
        log('Media generation completed successfully.');
        log(result.stdout);
      } else {
        log('ERROR: Media generator failed with exit code ${result.exitCode}.');
        log(result.stderr);
        throw Exception('Media generator failed with exit code ${result.exitCode}');
      }
    } catch (e) {
      log('ERROR: Failed to run media generator executable: $e');
      rethrow;
    }
  }

  @override
  Future<void> onDeploy(
    String gameDir,
    void Function(String) log,
    Map<String, dynamic> settings,
  ) async {
    final configMap = _loadConfigJson();
    final omnimix = (configMap['omnimix'] as Map<String, dynamic>?) ?? {};

    // 1. Perform backup of specific media assets
    final version = _getCurrentGameVersion(gameDir);
    log('Detected game version: $version');
    final backupDir = '$gameDir/.omnimix_backup/media_backup_v$version';
    final backupDirObj = Directory(backupDir);
    if (!backupDirObj.existsSync()) {
      log('Creating original media backup for version $version...');
      _backupMediaFiles(gameDir, backupDir);
      log('Backup created successfully.');
    } else {
      log(
        'Pristine backup for version $version already exists. Skipping backup to protect original assets.',
      );
    }

    // 2. Generate generator config & Run media generator CLI
    log('Preparing media generator configurations...');
    final anthemZip = configMap['anthemZip'] as Map<String, dynamic>?;
    final anthemZipEnabled =
        anthemZip?['enabled'] ?? settings['anthemZipEnabled'] ?? true;
    final logoMode = omnimix['logoMode'] ?? settings['logoMode'] ?? 'copy';
    final customPngPath =
        omnimix['customPngPath'] ?? settings['customPngPath'] ?? '';

    final normalizedGameDir = '$gameDir/.omnimix_backup/media_backup_v$version'
        .replaceAll('\\', '/');
    final normalizedOutputDir = '$gameDir/media'.replaceAll('\\', '/');

    final cliPath = _getCliPath();
    final configPath = _getConfigPath();
    log('Running media generator: $cliPath');
    final args = <String>[
      '-c',
      configPath,
      '-g',
      normalizedGameDir,
      '-o',
      normalizedOutputDir,
    ];
    if (logoMode == 'image' && customPngPath.isNotEmpty) {
      args.addAll(['-r', customPngPath]);
    }
    if (!anthemZipEnabled) {
      args.add('--skip-anthem');
    }

    try {
      final result = await Process.run(cliPath, args);
      if (result.exitCode == 0) {
        log('Media generation completed successfully.');
        log(result.stdout);
      } else {
        log('ERROR: Media generator failed with exit code ${result.exitCode}.');
        log(result.stderr);
      }
    } catch (e) {
      log('ERROR: Failed to run media generator executable: $e');
    }
  }

  @override
  Future<void> onUndeploy(String gameDir, void Function(String) log) async {
    // 1. Media files restore cleanup
    final currentVersion = _getCurrentGameVersion(gameDir);
    log('Detected game version: $currentVersion');
    if (_shouldRestore(gameDir, currentVersion)) {
      log('Restoring original media assets from backup...');
      _restoreMediaFiles(gameDir, currentVersion);
      log('Original media assets restored successfully.');
    } else {
      log(
        'Warning: Game version ($currentVersion) is newer than the highest backed-up version. Skipping restore to protect updated game files.',
      );
    }
  }

  @override
  Widget buildSettingsWidget(
    BuildContext context,
    Map<String, dynamic> currentSettings,
    void Function(Map<String, dynamic> newSettings) onSave,
  ) {
    return FH6SettingsDialog(
      currentSettings: currentSettings,
      onSave: onSave,
      loadConfigJson: _loadConfigJson,
      saveConfigJson: _saveConfigJson,
    );
  }

  // ─── Helpers ───

  static const List<String> _mediaRelativePaths = [
    'media/Audio/FMODBanks/R9_Tracks_CU1.bank',
    'media/Audio/FMODBanks/R9_Tracks_CU1.assets.bank',
    'media/Audio/RadioInfo_BR.xml',
    'media/Audio/RadioInfo_CN.xml',
    'media/Audio/RadioInfo_DE.xml',
    'media/Audio/RadioInfo_EN.xml',
    'media/Audio/RadioInfo_ES.xml',
    'media/Audio/RadioInfo_IT.xml',
    'media/Audio/RadioInfo_JP.xml',
    'media/Audio/RadioInfo_KO.xml',
    'media/Audio/RadioInfo_MX.xml',
    'media/Audio/RadioInfo_TW.xml',
    'media/UI/Textures/Anthem.zip',
    'media/UI/Textures/HiRes/Anthem.zip',
  ];

  String _getCurrentGameVersion(String gameDir) {
    final configFile = File('$gameDir/MicrosoftGame.Config');
    if (!configFile.existsSync()) return '0.0.0.0';
    try {
      final content = configFile.readAsStringSync();
      final regex = RegExp(r'\bVersion="([^"]+)"');
      final match = regex.firstMatch(content);
      if (match != null) {
        return match.group(1)!;
      }
    } catch (_) {}
    return '0.0.0.0';
  }

  void _backupMediaFiles(String gameDir, String backupDir) {
    for (final relPath in _mediaRelativePaths) {
      final srcFile = File('$gameDir/$relPath');
      if (srcFile.existsSync()) {
        final destFile = File('$backupDir/$relPath');
        destFile.parent.createSync(recursive: true);
        srcFile.copySync(destFile.path);
      }
    }
  }

  void _restoreMediaFiles(String gameDir, String currentVersion) {
    final highestVersion = _getHighestBackupVersion(gameDir);
    if (highestVersion == null) return;
    final backupDir = '$gameDir/.omnimix_backup/media_backup_v$highestVersion';
    for (final relPath in _mediaRelativePaths) {
      final srcFile = File('$backupDir/$relPath');
      if (srcFile.existsSync()) {
        final destFile = File('$gameDir/$relPath');
        destFile.parent.createSync(recursive: true);
        srcFile.copySync(destFile.path);
      }
    }
  }

  String? _getHighestBackupVersion(String gameDir) {
    final backupRootDir = Directory('$gameDir/.omnimix_backup');
    if (!backupRootDir.existsSync()) return null;

    String? highestVersion;

    for (final entity in backupRootDir.listSync()) {
      if (entity is Directory) {
        final dirName = entity.uri.pathSegments.lastWhere(
          (s) => s.isNotEmpty,
          orElse: () => '',
        );
        if (dirName.startsWith('media_backup_v')) {
          final verStr = dirName.substring('media_backup_v'.length);
          if (highestVersion == null ||
              _isVersionLower(highestVersion, verStr)) {
            highestVersion = verStr;
          }
        }
      }
    }
    return highestVersion;
  }

  bool _shouldRestore(String gameDir, String currentVersion) {
    final highestVersion = _getHighestBackupVersion(gameDir);
    if (highestVersion == null) return false;
    if (_isVersionLower(highestVersion, currentVersion)) {
      return false;
    }
    return true;
  }

  bool _isVersionLower(String versionA, String versionB) {
    final partsA = versionA
        .split('.')
        .map((e) => int.tryParse(e) ?? 0)
        .toList();
    final partsB = versionB
        .split('.')
        .map((e) => int.tryParse(e) ?? 0)
        .toList();
    for (var i = 0; i < partsA.length && i < partsB.length; i++) {
      if (partsA[i] < partsB[i]) return true;
      if (partsA[i] > partsB[i]) return false;
    }
    return partsA.length < partsB.length;
  }

  String _getCliPath() {
    final guiDir = File(Platform.resolvedExecutable).parent.path;
    final sep = Platform.pathSeparator;

    // List of candidates, order from production to development
    final candidates = [
      // 1. Production: sibling of Flutter executable in playerbuild
      '$guiDir${sep}chill-gen-media.exe',
      // 2. Adjacent folders in release
      '$guiDir${sep}..${sep}Backend${sep}chill-gen-media.exe',
      // 3. Dev: absolute playerbuild path
      r'G:\Csharp\Chill\playerbuild\chill-gen-media.exe',
      // 4. Dev: relative playerbuild path from Flutter debug runner build
      '$guiDir${sep}..${sep}..${sep}..${sep}..${sep}..${sep}playerbuild${sep}chill-gen-media.exe',
      // 5. Dev: absolute MediaGenerator release output path (net10.0)
      r'G:\Csharp\Chill\ChillPatcher.MediaGenerator\bin\Release\net10.0\chill-gen-media.exe',
      // 6. Dev: relative MediaGenerator release output path (net10.0)
      '$guiDir${sep}..${sep}..${sep}..${sep}..${sep}ChillPatcher.MediaGenerator${sep}bin${sep}Release${sep}net10.0${sep}chill-gen-media.exe',
    ];

    for (final c in candidates) {
      if (File(c).existsSync()) {
        return c;
      }
    }

    // Default fallback
    return 'chill-gen-media.exe';
  }

  String _getConfigPath() {
    final cliPath = _getCliPath();
    final cliFile = File(cliPath);
    if (cliFile.existsSync()) {
      final dir = cliFile.parent.path;
      final configJsonFile = File('$dir${Platform.pathSeparator}config.json');
      if (configJsonFile.existsSync()) {
        return configJsonFile.path;
      }
    }
    // Fallback to sibling of resolved executable
    final guiDir = File(Platform.resolvedExecutable).parent.path;
    return '$guiDir${Platform.pathSeparator}config.json';
  }

  Map<String, dynamic> _loadConfigJson() {
    final path = _getConfigPath();
    final file = File(path);
    if (file.existsSync()) {
      try {
        final content = file.readAsStringSync();
        return jsonDecode(content) as Map<String, dynamic>;
      } catch (_) {}
    }
    return {};
  }

  void _saveConfigJson(Map<String, dynamic> newSettings) {
    final path = _getConfigPath();
    final file = File(path);
    Map<String, dynamic> config = {};
    if (file.existsSync()) {
      try {
        final content = file.readAsStringSync();
        config = jsonDecode(content) as Map<String, dynamic>;
      } catch (_) {}
    }

    // Update keys in config
    // 1. Bank duration
    final bank = (config['bank'] as Map<String, dynamic>?) ?? {};
    bank['sampleDurationSec'] = newSettings['sampleDurationSec'] ?? 300;
    config['bank'] = bank;

    // 2. Radio info
    final radioInfo = (config['radioInfo'] as Map<String, dynamic>?) ?? {};
    radioInfo['stationName'] = newSettings['stationName'] ?? 'Streamer Mode';
    radioInfo['displayName'] = newSettings['displayName'] ?? 'OmniMix Player';
    radioInfo['artist'] = newSettings['artist'] ?? 'ChillPatcher';
    config['radioInfo'] = radioInfo;

    // 3. Anthem zip
    final anthemZip = (config['anthemZip'] as Map<String, dynamic>?) ?? {};
    anthemZip['enabled'] = newSettings['anthemZipEnabled'] ?? true;
    anthemZip['mode'] = newSettings['anthemMode'] ?? 'full';
    config['anthemZip'] = anthemZip;

    // 4. Custom settings (logoMode, customPngPath, serverAddress, serverPort, enableLog)
    final omnimix = (config['omnimix'] as Map<String, dynamic>?) ?? {};
    omnimix['logoMode'] = newSettings['logoMode'] ?? 'copy';
    omnimix['customPngPath'] = newSettings['customPngPath'] ?? '';
    omnimix['serverAddress'] =
        newSettings['serverAddress'] ?? omnimix['serverAddress'] ?? '127.0.0.1';
    omnimix['serverPort'] =
        newSettings['serverPort'] ?? omnimix['serverPort'] ?? 17890;
    omnimix['enableLog'] =
        newSettings['enableLog'] ?? omnimix['enableLog'] ?? true;
    config['omnimix'] = omnimix;

    // Write back
    file.writeAsStringSync(const JsonEncoder.withIndent('  ').convert(config));
  }
}

class FH6SettingsDialog extends StatefulWidget {
  final Map<String, dynamic> currentSettings;
  final void Function(Map<String, dynamic> newSettings) onSave;
  final Map<String, dynamic> Function() loadConfigJson;
  final void Function(Map<String, dynamic>) saveConfigJson;

  const FH6SettingsDialog({
    Key? key,
    required this.currentSettings,
    required this.onSave,
    required this.loadConfigJson,
    required this.saveConfigJson,
  }) : super(key: key);

  @override
  _FH6SettingsDialogState createState() => _FH6SettingsDialogState();
}

class _FH6SettingsDialogState extends State<FH6SettingsDialog> {
  late final TextEditingController serverController;
  late final TextEditingController portController;
  late bool enableLog;

  late final TextEditingController durationController;
  late final TextEditingController displayNameController;
  late final TextEditingController artistController;
  late final TextEditingController stationNameController;
  late bool anthemZipEnabled;
  late String anthemMode;
  late String logoMode;
  late final TextEditingController customPngPathController;

  @override
  void initState() {
    super.initState();
    final configMap = widget.loadConfigJson();
    final bank = (configMap['bank'] as Map<String, dynamic>?) ?? {};
    final radioInfo = (configMap['radioInfo'] as Map<String, dynamic>?) ?? {};
    final anthemZip = (configMap['anthemZip'] as Map<String, dynamic>?) ?? {};
    final omnimix = (configMap['omnimix'] as Map<String, dynamic>?) ?? {};

    // Core config defaults
    final serverAddress =
        omnimix['serverAddress'] ??
        widget.currentSettings['serverAddress'] ??
        '127.0.0.1';
    final serverPort =
        omnimix['serverPort'] ?? widget.currentSettings['serverPort'] ?? 17890;
    enableLog =
        omnimix['enableLog'] ?? widget.currentSettings['enableLog'] ?? true;

    serverController = TextEditingController(text: serverAddress);
    portController = TextEditingController(text: serverPort.toString());

    // Media config
    durationController = TextEditingController(
      text:
          (bank['sampleDurationSec'] ??
                  widget.currentSettings['sampleDurationSec'] ??
                  300)
              .toString(),
    );
    displayNameController = TextEditingController(
      text:
          radioInfo['displayName'] ??
          widget.currentSettings['displayName'] ??
          'OmniMix Player',
    );
    artistController = TextEditingController(
      text:
          radioInfo['artist'] ??
          widget.currentSettings['artist'] ??
          'ChillPatcher',
    );
    stationNameController = TextEditingController(
      text:
          radioInfo['stationName'] ??
          widget.currentSettings['stationName'] ??
          'Streamer Mode',
    );
    anthemZipEnabled =
        anthemZip['enabled'] ??
        widget.currentSettings['anthemZipEnabled'] ??
        true;
    anthemMode =
        anthemZip['mode'] ?? widget.currentSettings['anthemMode'] ?? 'full';
    logoMode =
        omnimix['logoMode'] ?? widget.currentSettings['logoMode'] ?? 'copy';
    customPngPathController = TextEditingController(
      text:
          omnimix['customPngPath'] ??
          widget.currentSettings['customPngPath'] ??
          '',
    );
  }

  @override
  void dispose() {
    serverController.dispose();
    portController.dispose();
    durationController.dispose();
    displayNameController.dispose();
    artistController.dispose();
    stationNameController.dispose();
    customPngPathController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final l10n = AppLocalizations.of(context)!;
    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      title: Row(
        children: [
          Icon(Icons.settings, color: cs.primary),
          const SizedBox(width: 10),
          Text(l10n.fh6SettingsTitle),
        ],
      ),
      content: SizedBox(
        width: 500,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Core Config fields removed (always default to local server/port)
              const SizedBox(height: 8),
              Text(
                l10n.mediaOverlaySettings,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: cs.primary,
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: stationNameController,
                      decoration: InputDecoration(
                        labelText: l10n.stationNameLabel,
                        hintText: l10n.stationNameHint,
                        border: const OutlineInputBorder(),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextField(
                      controller: durationController,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: l10n.sampleDurationLabel,
                        hintText: l10n.sampleDurationHint,
                        border: const OutlineInputBorder(),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: displayNameController,
                      decoration: InputDecoration(
                        labelText: l10n.displayNameLabel,
                        hintText: l10n.displayNameHint,
                        border: const OutlineInputBorder(),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextField(
                      controller: artistController,
                      decoration: InputDecoration(
                        labelText: l10n.artistNameLabel,
                        hintText: l10n.artistNameHint,
                        border: const OutlineInputBorder(),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              SwitchListTile(
                title: Text(l10n.enableAnthemZip),
                value: anthemZipEnabled,
                onChanged: (val) {
                  setState(() {
                    anthemZipEnabled = val;
                  });
                },
                contentPadding: EdgeInsets.zero,
              ),
              if (anthemZipEnabled) ...[
                const SizedBox(height: 8),
                DropdownButtonFormField<String>(
                  value: anthemMode,
                  decoration: InputDecoration(
                    labelText: l10n.anthemModeLabel,
                    border: const OutlineInputBorder(),
                  ),
                  items: [
                    DropdownMenuItem(value: 'full', child: Text(l10n.modeFull)),
                    DropdownMenuItem(
                      value: 'partial',
                      child: Text(l10n.modePartial),
                    ),
                  ],
                  onChanged: (val) {
                    if (val != null) {
                      setState(() {
                        anthemMode = val;
                      });
                    }
                  },
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  value: logoMode,
                  decoration: InputDecoration(
                    labelText: l10n.logoOptionLabel,
                    border: const OutlineInputBorder(),
                    prefixIcon: const Icon(Icons.image, size: 20),
                  ),
                  items: [
                    DropdownMenuItem(
                      value: 'copy',
                      child: Text(l10n.copyDefaultLogo),
                    ),
                    DropdownMenuItem(
                      value: 'image',
                      child: Text(l10n.injectCustomPng),
                    ),
                  ],
                  onChanged: (val) {
                    if (val != null) {
                      setState(() {
                        logoMode = val;
                      });
                    }
                  },
                ),
                if (logoMode == 'image') ...[
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: customPngPathController,
                          readOnly: true,
                          decoration: InputDecoration(
                            labelText: l10n.logoOptionLabel,
                            border: const OutlineInputBorder(),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      ElevatedButton(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: cs.secondaryContainer,
                          foregroundColor: cs.onSecondaryContainer,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 20,
                          ),
                        ),
                        onPressed: () async {
                          final result = await FilePicker.pickFiles(
                            type: FileType.custom,
                            allowedExtensions: ['png'],
                            dialogTitle: l10n.selectLogoPng,
                          );
                          if (result != null &&
                              result.files.single.path != null) {
                            setState(() {
                              customPngPathController.text =
                                  result.files.single.path!;
                            });
                          }
                        },
                        child: Text(l10n.browse),
                      ),
                    ],
                  ),
                ],
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(l10n.cancel),
        ),
        ElevatedButton(
          style: ElevatedButton.styleFrom(
            backgroundColor: cs.primary,
            foregroundColor: cs.onPrimary,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
          onPressed: () {
            final durationVal = int.tryParse(durationController.text) ?? 300;
            final newSettings = {
              'serverAddress': serverController.text,
              'serverPort': int.tryParse(portController.text) ?? 17890,
              'enableLog': enableLog,
              'sampleDurationSec': durationVal,
              'displayName': displayNameController.text,
              'artist': artistController.text,
              'stationName': stationNameController.text,
              'anthemZipEnabled': anthemZipEnabled,
              'anthemMode': anthemMode,
              'logoMode': logoMode,
              'customPngPath': customPngPathController.text,
            };
            widget.saveConfigJson(newSettings);
            widget.onSave(newSettings);
            Navigator.of(context).pop();
          },
          child: Text(l10n.saveAndApply),
        ),
      ],
    );
  }
}
