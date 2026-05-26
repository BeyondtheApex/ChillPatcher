// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'OmniMixPlayer';

  @override
  String get welcomeMessage => 'Welcome to OmniMixPlayer';

  @override
  String get welcomeHint => 'Select a tab or module to get started';

  @override
  String get home => 'Home';

  @override
  String get playlist => 'Playlist';

  @override
  String get launchpad => 'Launchpad';

  @override
  String get modules => 'Modules';

  @override
  String get settings => 'Settings';

  @override
  String get about => 'About';

  @override
  String get version => 'v1.0.0';

  @override
  String get builtWith => 'Built with Flutter + Dart';

  @override
  String get backendDesc => 'C# .NET 8 backend + Flutter frontend';

  @override
  String get back => 'Back';

  @override
  String get backendControl => 'Backend Control';

  @override
  String get backendService => 'Service Management';

  @override
  String get status => 'Status';

  @override
  String get running => 'Running';

  @override
  String get stopped => 'Stopped';

  @override
  String get startBackend => 'Start Backend';

  @override
  String get stopBackend => 'Stop Backend';

  @override
  String get restartBackend => 'Restart Backend';

  @override
  String get restarting => 'Restarting...';

  @override
  String get port => 'Port';

  @override
  String get bind => 'Bind';

  @override
  String get guiSettings => 'GUI Settings';

  @override
  String get autoStart => 'Auto-start';

  @override
  String get minimizeToTray => 'Minimize to tray';

  @override
  String get appearance => 'Appearance';

  @override
  String get theme => 'Theme';

  @override
  String get themeLight => 'Light';

  @override
  String get themeDark => 'Dark';

  @override
  String get themeSystem => 'System';

  @override
  String get language => 'Language';

  @override
  String get saveAndRestart => 'Save & Restart';

  @override
  String get resetToDefaults => 'Reset to Defaults';

  @override
  String get resetConfirm => 'Reset all settings to defaults?';

  @override
  String get configReset => 'Configuration reset to defaults';

  @override
  String get open => 'Open';

  @override
  String get loaded => 'Loaded';

  @override
  String get disabled => 'Disabled';

  @override
  String get manageModules => 'Manage installed modules and their settings';

  @override
  String get noModuleLinks => 'No module links available';

  @override
  String get clickIconToOpen => 'Click an icon to open the module panel';

  @override
  String get connected => 'Connected';

  @override
  String get disconnected => 'Disconnected';

  @override
  String get showHideWindow => 'Show/Hide Window';

  @override
  String get fullyExit => 'Fully Exit';

  @override
  String get serviceStatus => 'Service Status';

  @override
  String get serviceRunning => 'Service is running';

  @override
  String get serviceInstalled => 'Service installed (not running)';

  @override
  String get serviceNotInstalled => 'Service not installed';

  @override
  String get installService => 'Install Service';

  @override
  String get uninstallService => 'Uninstall Service';

  @override
  String get serviceInstallSuccess => 'Service installed successfully';

  @override
  String get serviceInstallFailed => 'Service installation failed';

  @override
  String get serviceUninstallSuccess => 'Service removed successfully';

  @override
  String get serviceUninstallFailed => 'Service removal failed';

  @override
  String get processMode => 'Process';

  @override
  String get serviceMode => 'Service';
}
