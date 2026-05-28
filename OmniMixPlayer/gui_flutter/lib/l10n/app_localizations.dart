import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_zh.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('zh'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'OmniMixPlayer'**
  String get appTitle;

  /// No description provided for @welcomeMessage.
  ///
  /// In en, this message translates to:
  /// **'Welcome to OmniMixPlayer'**
  String get welcomeMessage;

  /// No description provided for @welcomeHint.
  ///
  /// In en, this message translates to:
  /// **'Select a tab or module to get started'**
  String get welcomeHint;

  /// No description provided for @home.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get home;

  /// No description provided for @playlist.
  ///
  /// In en, this message translates to:
  /// **'Playlist'**
  String get playlist;

  /// No description provided for @launchpad.
  ///
  /// In en, this message translates to:
  /// **'Launchpad'**
  String get launchpad;

  /// No description provided for @modules.
  ///
  /// In en, this message translates to:
  /// **'Modules'**
  String get modules;

  /// No description provided for @settings.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get settings;

  /// No description provided for @gameIntegration.
  ///
  /// In en, this message translates to:
  /// **'Game Integration'**
  String get gameIntegration;

  /// No description provided for @selectGameDir.
  ///
  /// In en, this message translates to:
  /// **'Select Game Directory'**
  String get selectGameDir;

  /// No description provided for @gamePath.
  ///
  /// In en, this message translates to:
  /// **'Game Path'**
  String get gamePath;

  /// No description provided for @bepinexStatus.
  ///
  /// In en, this message translates to:
  /// **'BepInEx Status'**
  String get bepinexStatus;

  /// No description provided for @modStatus.
  ///
  /// In en, this message translates to:
  /// **'Mod Status'**
  String get modStatus;

  /// No description provided for @installBepInEx.
  ///
  /// In en, this message translates to:
  /// **'Install BepInEx'**
  String get installBepInEx;

  /// No description provided for @uninstallBepInEx.
  ///
  /// In en, this message translates to:
  /// **'Uninstall BepInEx'**
  String get uninstallBepInEx;

  /// No description provided for @installMod.
  ///
  /// In en, this message translates to:
  /// **'Install Mod'**
  String get installMod;

  /// No description provided for @uninstallMod.
  ///
  /// In en, this message translates to:
  /// **'Uninstall Mod'**
  String get uninstallMod;

  /// No description provided for @reinstallBepInEx.
  ///
  /// In en, this message translates to:
  /// **'Reinstall BepInEx'**
  String get reinstallBepInEx;

  /// No description provided for @reinstallMod.
  ///
  /// In en, this message translates to:
  /// **'Reinstall Mod'**
  String get reinstallMod;

  /// No description provided for @statusNotInstalled.
  ///
  /// In en, this message translates to:
  /// **'Not Installed'**
  String get statusNotInstalled;

  /// No description provided for @statusManaged.
  ///
  /// In en, this message translates to:
  /// **'Managed'**
  String get statusManaged;

  /// No description provided for @statusUnmanaged.
  ///
  /// In en, this message translates to:
  /// **'Pre-installed / Unmanaged'**
  String get statusUnmanaged;

  /// No description provided for @unmanagedWarning.
  ///
  /// In en, this message translates to:
  /// **'BepInEx is already pre-installed. OmniMix will only manage Mod plugins. Core loader will not be managed or overwritten.'**
  String get unmanagedWarning;

  /// No description provided for @invalidGameDir.
  ///
  /// In en, this message translates to:
  /// **'Invalid game directory! Please select the root folder of \'Chill With You\' containing \'Chill With You.exe\' and \'Chill With You_Data\'.'**
  String get invalidGameDir;

  /// No description provided for @modInstalled.
  ///
  /// In en, this message translates to:
  /// **'Installed'**
  String get modInstalled;

  /// No description provided for @deploymentLogs.
  ///
  /// In en, this message translates to:
  /// **'Deployment Logs'**
  String get deploymentLogs;

  /// No description provided for @chooseFolder.
  ///
  /// In en, this message translates to:
  /// **'Choose Folder'**
  String get chooseFolder;

  /// No description provided for @about.
  ///
  /// In en, this message translates to:
  /// **'About'**
  String get about;

  /// No description provided for @version.
  ///
  /// In en, this message translates to:
  /// **'v1.0.0'**
  String get version;

  /// No description provided for @builtWith.
  ///
  /// In en, this message translates to:
  /// **'Built with Flutter + Dart'**
  String get builtWith;

  /// No description provided for @backendDesc.
  ///
  /// In en, this message translates to:
  /// **'C# .NET 8 backend + Flutter frontend'**
  String get backendDesc;

  /// No description provided for @back.
  ///
  /// In en, this message translates to:
  /// **'Back'**
  String get back;

  /// No description provided for @backendControl.
  ///
  /// In en, this message translates to:
  /// **'Backend Control'**
  String get backendControl;

  /// No description provided for @backendService.
  ///
  /// In en, this message translates to:
  /// **'Service Management'**
  String get backendService;

  /// No description provided for @status.
  ///
  /// In en, this message translates to:
  /// **'Status'**
  String get status;

  /// No description provided for @running.
  ///
  /// In en, this message translates to:
  /// **'Running'**
  String get running;

  /// No description provided for @stopped.
  ///
  /// In en, this message translates to:
  /// **'Stopped'**
  String get stopped;

  /// No description provided for @startBackend.
  ///
  /// In en, this message translates to:
  /// **'Start Backend'**
  String get startBackend;

  /// No description provided for @stopBackend.
  ///
  /// In en, this message translates to:
  /// **'Stop Backend'**
  String get stopBackend;

  /// No description provided for @restartBackend.
  ///
  /// In en, this message translates to:
  /// **'Restart Backend'**
  String get restartBackend;

  /// No description provided for @restarting.
  ///
  /// In en, this message translates to:
  /// **'Restarting...'**
  String get restarting;

  /// No description provided for @port.
  ///
  /// In en, this message translates to:
  /// **'Port'**
  String get port;

  /// No description provided for @bind.
  ///
  /// In en, this message translates to:
  /// **'Bind'**
  String get bind;

  /// No description provided for @guiSettings.
  ///
  /// In en, this message translates to:
  /// **'GUI Settings'**
  String get guiSettings;

  /// No description provided for @autoStart.
  ///
  /// In en, this message translates to:
  /// **'Auto-start'**
  String get autoStart;

  /// No description provided for @minimizeToTray.
  ///
  /// In en, this message translates to:
  /// **'Minimize to tray'**
  String get minimizeToTray;

  /// No description provided for @appearance.
  ///
  /// In en, this message translates to:
  /// **'Appearance'**
  String get appearance;

  /// No description provided for @theme.
  ///
  /// In en, this message translates to:
  /// **'Theme'**
  String get theme;

  /// No description provided for @themeLight.
  ///
  /// In en, this message translates to:
  /// **'Light'**
  String get themeLight;

  /// No description provided for @themeDark.
  ///
  /// In en, this message translates to:
  /// **'Dark'**
  String get themeDark;

  /// No description provided for @themeSystem.
  ///
  /// In en, this message translates to:
  /// **'System'**
  String get themeSystem;

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get language;

  /// No description provided for @saveAndRestart.
  ///
  /// In en, this message translates to:
  /// **'Save & Restart'**
  String get saveAndRestart;

  /// No description provided for @resetToDefaults.
  ///
  /// In en, this message translates to:
  /// **'Reset to Defaults'**
  String get resetToDefaults;

  /// No description provided for @resetConfirm.
  ///
  /// In en, this message translates to:
  /// **'Reset all settings to defaults?'**
  String get resetConfirm;

  /// No description provided for @configReset.
  ///
  /// In en, this message translates to:
  /// **'Configuration reset to defaults'**
  String get configReset;

  /// No description provided for @open.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get open;

  /// No description provided for @loaded.
  ///
  /// In en, this message translates to:
  /// **'Loaded'**
  String get loaded;

  /// No description provided for @disabled.
  ///
  /// In en, this message translates to:
  /// **'Disabled'**
  String get disabled;

  /// No description provided for @manageModules.
  ///
  /// In en, this message translates to:
  /// **'Manage installed modules and their settings'**
  String get manageModules;

  /// No description provided for @noModuleLinks.
  ///
  /// In en, this message translates to:
  /// **'No module links available'**
  String get noModuleLinks;

  /// No description provided for @clickIconToOpen.
  ///
  /// In en, this message translates to:
  /// **'Click an icon to open the module panel'**
  String get clickIconToOpen;

  /// No description provided for @connected.
  ///
  /// In en, this message translates to:
  /// **'Connected'**
  String get connected;

  /// No description provided for @disconnected.
  ///
  /// In en, this message translates to:
  /// **'Disconnected'**
  String get disconnected;

  /// No description provided for @showHideWindow.
  ///
  /// In en, this message translates to:
  /// **'Show/Hide Window'**
  String get showHideWindow;

  /// No description provided for @fullyExit.
  ///
  /// In en, this message translates to:
  /// **'Fully Exit'**
  String get fullyExit;

  /// No description provided for @serviceStatus.
  ///
  /// In en, this message translates to:
  /// **'Service Status'**
  String get serviceStatus;

  /// No description provided for @serviceRunning.
  ///
  /// In en, this message translates to:
  /// **'Service is running'**
  String get serviceRunning;

  /// No description provided for @serviceInstalled.
  ///
  /// In en, this message translates to:
  /// **'Service installed (not running)'**
  String get serviceInstalled;

  /// No description provided for @serviceNotInstalled.
  ///
  /// In en, this message translates to:
  /// **'Service not installed'**
  String get serviceNotInstalled;

  /// No description provided for @installService.
  ///
  /// In en, this message translates to:
  /// **'Install Service'**
  String get installService;

  /// No description provided for @uninstallService.
  ///
  /// In en, this message translates to:
  /// **'Uninstall Service'**
  String get uninstallService;

  /// No description provided for @serviceInstallSuccess.
  ///
  /// In en, this message translates to:
  /// **'Service installed successfully'**
  String get serviceInstallSuccess;

  /// No description provided for @serviceInstallFailed.
  ///
  /// In en, this message translates to:
  /// **'Service installation failed'**
  String get serviceInstallFailed;

  /// No description provided for @serviceUninstallSuccess.
  ///
  /// In en, this message translates to:
  /// **'Service removed successfully'**
  String get serviceUninstallSuccess;

  /// No description provided for @serviceUninstallFailed.
  ///
  /// In en, this message translates to:
  /// **'Service removal failed'**
  String get serviceUninstallFailed;

  /// No description provided for @processMode.
  ///
  /// In en, this message translates to:
  /// **'Process'**
  String get processMode;

  /// No description provided for @serviceMode.
  ///
  /// In en, this message translates to:
  /// **'Service'**
  String get serviceMode;

  /// No description provided for @serviceAutoStart.
  ///
  /// In en, this message translates to:
  /// **'Service Auto-Start'**
  String get serviceAutoStart;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'zh'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'zh':
      return AppLocalizationsZh();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
