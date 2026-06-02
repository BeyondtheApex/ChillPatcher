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

  /// No description provided for @mediaControls.
  ///
  /// In en, this message translates to:
  /// **'Media controls'**
  String get mediaControls;

  /// No description provided for @floatingPlayer.
  ///
  /// In en, this message translates to:
  /// **'Floating player'**
  String get floatingPlayer;

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

  /// No description provided for @serviceManagement.
  ///
  /// In en, this message translates to:
  /// **'Service Management (Install/Uninstall)'**
  String get serviceManagement;

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

  /// No description provided for @websiteLink.
  ///
  /// In en, this message translates to:
  /// **'Website'**
  String get websiteLink;

  /// No description provided for @save.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get save;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @pendingSave.
  ///
  /// In en, this message translates to:
  /// **'Pending Save'**
  String get pendingSave;

  /// No description provided for @clear.
  ///
  /// In en, this message translates to:
  /// **'Clear'**
  String get clear;

  /// No description provided for @openFolder.
  ///
  /// In en, this message translates to:
  /// **'Open Folder'**
  String get openFolder;

  /// Shows installed version
  ///
  /// In en, this message translates to:
  /// **'Installed v{version}'**
  String installed(String version);

  /// No description provided for @closeBehavior.
  ///
  /// In en, this message translates to:
  /// **'Close Behavior'**
  String get closeBehavior;

  /// No description provided for @closeMinimize.
  ///
  /// In en, this message translates to:
  /// **'Minimize to Tray'**
  String get closeMinimize;

  /// No description provided for @closeExit.
  ///
  /// In en, this message translates to:
  /// **'Exit App'**
  String get closeExit;

  /// No description provided for @themeColor.
  ///
  /// In en, this message translates to:
  /// **'Theme Color'**
  String get themeColor;

  /// No description provided for @customColor.
  ///
  /// In en, this message translates to:
  /// **'Custom'**
  String get customColor;

  /// No description provided for @hue.
  ///
  /// In en, this message translates to:
  /// **'Hue'**
  String get hue;

  /// No description provided for @saturation.
  ///
  /// In en, this message translates to:
  /// **'Saturation'**
  String get saturation;

  /// No description provided for @brightness.
  ///
  /// In en, this message translates to:
  /// **'Brightness'**
  String get brightness;

  /// No description provided for @confirm.
  ///
  /// In en, this message translates to:
  /// **'OK'**
  String get confirm;

  /// No description provided for @useSystemColor.
  ///
  /// In en, this message translates to:
  /// **'Use system accent color'**
  String get useSystemColor;

  /// No description provided for @backendConfig.
  ///
  /// In en, this message translates to:
  /// **'Backend Config'**
  String get backendConfig;

  /// No description provided for @moduleStatusDisabled.
  ///
  /// In en, this message translates to:
  /// **'Disabled — will not load on next start'**
  String get moduleStatusDisabled;

  /// No description provided for @moduleStatusPending.
  ///
  /// In en, this message translates to:
  /// **'Loaded but will be disabled on next start'**
  String get moduleStatusPending;

  /// No description provided for @moduleStatusActive.
  ///
  /// In en, this message translates to:
  /// **'Loaded and active'**
  String get moduleStatusActive;

  /// No description provided for @moduleStatusWillLoad.
  ///
  /// In en, this message translates to:
  /// **'Will load on next start'**
  String get moduleStatusWillLoad;

  /// No description provided for @serviceStarting.
  ///
  /// In en, this message translates to:
  /// **'Service Starting'**
  String get serviceStarting;

  /// No description provided for @serviceStartingMessage.
  ///
  /// In en, this message translates to:
  /// **'Connecting and initializing player service...'**
  String get serviceStartingMessage;

  /// No description provided for @serviceNotConnected.
  ///
  /// In en, this message translates to:
  /// **'Service Not Connected'**
  String get serviceNotConnected;

  /// No description provided for @waitingForBackend.
  ///
  /// In en, this message translates to:
  /// **'Please wait, waiting for backend service...'**
  String get waitingForBackend;

  /// No description provided for @noSongPlaying.
  ///
  /// In en, this message translates to:
  /// **'No song playing'**
  String get noSongPlaying;

  /// No description provided for @shuffle.
  ///
  /// In en, this message translates to:
  /// **'Shuffle'**
  String get shuffle;

  /// No description provided for @previous.
  ///
  /// In en, this message translates to:
  /// **'Previous'**
  String get previous;

  /// No description provided for @next.
  ///
  /// In en, this message translates to:
  /// **'Next'**
  String get next;

  /// No description provided for @playPause.
  ///
  /// In en, this message translates to:
  /// **'Play/Pause'**
  String get playPause;

  /// No description provided for @repeatOne.
  ///
  /// In en, this message translates to:
  /// **'Repeat One'**
  String get repeatOne;

  /// No description provided for @serverControlMode.
  ///
  /// In en, this message translates to:
  /// **'Server control mode'**
  String get serverControlMode;

  /// No description provided for @clientModeControlsDisabled.
  ///
  /// In en, this message translates to:
  /// **'Client mode: playback controlled by client'**
  String get clientModeControlsDisabled;

  /// No description provided for @queue.
  ///
  /// In en, this message translates to:
  /// **'Queue'**
  String get queue;

  /// No description provided for @history.
  ///
  /// In en, this message translates to:
  /// **'History'**
  String get history;

  /// No description provided for @empty.
  ///
  /// In en, this message translates to:
  /// **'Empty'**
  String get empty;

  /// No description provided for @byPlaylist.
  ///
  /// In en, this message translates to:
  /// **'By Playlist'**
  String get byPlaylist;

  /// No description provided for @byAlbum.
  ///
  /// In en, this message translates to:
  /// **'By Album'**
  String get byAlbum;

  /// No description provided for @bySong.
  ///
  /// In en, this message translates to:
  /// **'By Song'**
  String get bySong;

  /// No description provided for @addSource.
  ///
  /// In en, this message translates to:
  /// **'Add Source'**
  String get addSource;

  /// No description provided for @searchHint.
  ///
  /// In en, this message translates to:
  /// **'Search songs / artist / album'**
  String get searchHint;

  /// No description provided for @removeFromLibrary.
  ///
  /// In en, this message translates to:
  /// **'Remove from Library'**
  String get removeFromLibrary;

  /// Error message with details
  ///
  /// In en, this message translates to:
  /// **'Error: {error}'**
  String errorWithMessage(String error);

  /// No description provided for @noActivePlaylist.
  ///
  /// In en, this message translates to:
  /// **'No active playlist, add a playlist or album source first'**
  String get noActivePlaylist;

  /// No description provided for @noSongs.
  ///
  /// In en, this message translates to:
  /// **'No songs'**
  String get noSongs;

  /// No description provided for @noAlbumsAdded.
  ///
  /// In en, this message translates to:
  /// **'No albums added'**
  String get noAlbumsAdded;

  /// No description provided for @noPlaylistsAdded.
  ///
  /// In en, this message translates to:
  /// **'No playlists added'**
  String get noPlaylistsAdded;

  /// No description provided for @selectLibrarySource.
  ///
  /// In en, this message translates to:
  /// **'Select sources to add to library'**
  String get selectLibrarySource;

  /// Count of selected items
  ///
  /// In en, this message translates to:
  /// **'Selected {count}'**
  String selectedCount(int count);

  /// No description provided for @playlistsTab.
  ///
  /// In en, this message translates to:
  /// **'Playlists'**
  String get playlistsTab;

  /// No description provided for @albumsTab.
  ///
  /// In en, this message translates to:
  /// **'Albums'**
  String get albumsTab;

  /// Song count with module name
  ///
  /// In en, this message translates to:
  /// **'{count} songs · {module}'**
  String songCountWithModule(int count, String module);

  /// Failed to load library with error details
  ///
  /// In en, this message translates to:
  /// **'Failed to load library: {error}'**
  String failedToLoadLibrary(String error);

  /// No description provided for @instanceManagement.
  ///
  /// In en, this message translates to:
  /// **'Instance Management'**
  String get instanceManagement;

  /// No description provided for @noInstalledInstances.
  ///
  /// In en, this message translates to:
  /// **'No installed instances'**
  String get noInstalledInstances;

  /// Installed instances count
  ///
  /// In en, this message translates to:
  /// **'Installed Instances ({count})'**
  String installedInstancesCount(int count);

  /// Archive count
  ///
  /// In en, this message translates to:
  /// **'Archive ({count})'**
  String archiveCount(int count);

  /// No description provided for @instanceAutoRegisterHint.
  ///
  /// In en, this message translates to:
  /// **'Instances will be auto-registered after installing game Mod'**
  String get instanceAutoRegisterHint;

  /// No description provided for @archiveManagement.
  ///
  /// In en, this message translates to:
  /// **'Archive Management'**
  String get archiveManagement;

  /// No description provided for @noArchivedInstances.
  ///
  /// In en, this message translates to:
  /// **'No archived instances'**
  String get noArchivedInstances;

  /// No description provided for @rename.
  ///
  /// In en, this message translates to:
  /// **'Rename'**
  String get rename;

  /// No description provided for @delete.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get delete;

  /// No description provided for @close.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get close;

  /// No description provided for @renameArchive.
  ///
  /// In en, this message translates to:
  /// **'Rename Archive'**
  String get renameArchive;

  /// No description provided for @archiveNameHint.
  ///
  /// In en, this message translates to:
  /// **'Enter archive name'**
  String get archiveNameHint;

  /// No description provided for @deleteArchive.
  ///
  /// In en, this message translates to:
  /// **'Delete Archive'**
  String get deleteArchive;

  /// Delete archive confirmation dialog
  ///
  /// In en, this message translates to:
  /// **'Are you sure you want to delete the archive of \"{name}\"? This action cannot be undone.'**
  String deleteArchiveConfirm(String name);

  /// No description provided for @deleteInstance.
  ///
  /// In en, this message translates to:
  /// **'Delete Instance'**
  String get deleteInstance;

  /// Delete instance confirmation dialog
  ///
  /// In en, this message translates to:
  /// **'Delete instance \"{id}\"? This will also remove its config and game registration.'**
  String deleteInstanceConfirm(String id);

  /// No description provided for @archiveInstanceTooltip.
  ///
  /// In en, this message translates to:
  /// **'Save as archive'**
  String get archiveInstanceTooltip;

  /// No description provided for @archiveInstance.
  ///
  /// In en, this message translates to:
  /// **'Archive Instance'**
  String get archiveInstance;

  /// Archive instance hint dialog
  ///
  /// In en, this message translates to:
  /// **'Save instance \"{id}\" settings as an archive for later reuse during installation.'**
  String archiveInstanceHint(String id);

  /// No description provided for @archiveAction.
  ///
  /// In en, this message translates to:
  /// **'Archive'**
  String get archiveAction;

  /// No description provided for @inheritArchiveTitle.
  ///
  /// In en, this message translates to:
  /// **'Inherit Archive Settings'**
  String get inheritArchiveTitle;

  /// No description provided for @inheritArchiveHint.
  ///
  /// In en, this message translates to:
  /// **'Choose an archive to inherit playlist and settings from. Unbound archives will be consumed, bound ones will be copied.'**
  String get inheritArchiveHint;

  /// No description provided for @archiveBoundWillCopy.
  ///
  /// In en, this message translates to:
  /// **'Bound to instance · Will copy settings'**
  String get archiveBoundWillCopy;

  /// No description provided for @archiveFreeWillConsume.
  ///
  /// In en, this message translates to:
  /// **'Unbound · Will consume this archive'**
  String get archiveFreeWillConsume;

  /// No description provided for @skipInherit.
  ///
  /// In en, this message translates to:
  /// **'Skip, fresh install'**
  String get skipInherit;

  /// No description provided for @uninstallServiceConfirm.
  ///
  /// In en, this message translates to:
  /// **'This will stop the backend service and remove it from the system.'**
  String get uninstallServiceConfirm;

  /// No description provided for @serviceAutoStartUpdated.
  ///
  /// In en, this message translates to:
  /// **'Service auto-start updated'**
  String get serviceAutoStartUpdated;

  /// No description provided for @serviceAutoStartFailed.
  ///
  /// In en, this message translates to:
  /// **'Failed to update service auto-start'**
  String get serviceAutoStartFailed;

  /// No description provided for @autoStartSuccess.
  ///
  /// In en, this message translates to:
  /// **'Auto-start updated successfully'**
  String get autoStartSuccess;

  /// No description provided for @autoStartFailed.
  ///
  /// In en, this message translates to:
  /// **'Failed to update auto-start'**
  String get autoStartFailed;

  /// No description provided for @waitingForBackendMod.
  ///
  /// In en, this message translates to:
  /// **'Please wait, waiting for backend service for Mod installation and management...'**
  String get waitingForBackendMod;

  /// No description provided for @omnimixInstance.
  ///
  /// In en, this message translates to:
  /// **'OmniMix Instance'**
  String get omnimixInstance;

  /// Instance online status
  ///
  /// In en, this message translates to:
  /// **'Online — {id}'**
  String instanceOnline(String id);

  /// Instance offline status
  ///
  /// In en, this message translates to:
  /// **'Offline — {id}'**
  String instanceOffline(String id);

  /// Available version
  ///
  /// In en, this message translates to:
  /// **'Available: v{version}'**
  String availableVersion(String version);

  /// Latest version
  ///
  /// In en, this message translates to:
  /// **'Latest: v{version}'**
  String latestVersion(String version);

  /// No description provided for @refresh.
  ///
  /// In en, this message translates to:
  /// **'Refresh'**
  String get refresh;

  /// No description provided for @noInstances.
  ///
  /// In en, this message translates to:
  /// **'No instances'**
  String get noInstances;

  /// No description provided for @exitGui.
  ///
  /// In en, this message translates to:
  /// **'Exit GUI'**
  String get exitGui;

  /// No description provided for @languageChinese.
  ///
  /// In en, this message translates to:
  /// **'中文'**
  String get languageChinese;

  /// No description provided for @languageEnglish.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get languageEnglish;

  /// No description provided for @languageSystem.
  ///
  /// In en, this message translates to:
  /// **'System'**
  String get languageSystem;

  /// No description provided for @backendNotConnected.
  ///
  /// In en, this message translates to:
  /// **'Backend Not Connected'**
  String get backendNotConnected;

  /// No description provided for @libraryBrowser.
  ///
  /// In en, this message translates to:
  /// **'Library Browser'**
  String get libraryBrowser;

  /// Playlist statistics
  ///
  /// In en, this message translates to:
  /// **'{tags} playlists · {songs} songs'**
  String playlistStats(int tags, int songs);

  /// No description provided for @libraryEmpty.
  ///
  /// In en, this message translates to:
  /// **'Library is empty'**
  String get libraryEmpty;

  /// Failed to load library with error
  ///
  /// In en, this message translates to:
  /// **'Failed to load library: {error}'**
  String loadLibraryFailed(String error);

  /// From module name
  ///
  /// In en, this message translates to:
  /// **'from {module}'**
  String fromModule(String module);

  /// Album count
  ///
  /// In en, this message translates to:
  /// **'{count} albums'**
  String albumCountLabel(int count);

  /// Song count label
  ///
  /// In en, this message translates to:
  /// **'{count} songs'**
  String songCountLabel(int count);

  /// No description provided for @unknownArtist.
  ///
  /// In en, this message translates to:
  /// **'Unknown artist'**
  String get unknownArtist;

  /// Source chip label with name and count
  ///
  /// In en, this message translates to:
  /// **'{name} ({count})'**
  String sourceChipLabel(String name, int count);

  /// No description provided for @playNext.
  ///
  /// In en, this message translates to:
  /// **'Play next'**
  String get playNext;

  /// No description provided for @addToQueueTail.
  ///
  /// In en, this message translates to:
  /// **'Add to queue tail'**
  String get addToQueueTail;

  /// No description provided for @exclude.
  ///
  /// In en, this message translates to:
  /// **'Exclude'**
  String get exclude;

  /// No description provided for @removeExclusion.
  ///
  /// In en, this message translates to:
  /// **'Remove exclusion'**
  String get removeExclusion;

  /// No description provided for @removeShort.
  ///
  /// In en, this message translates to:
  /// **'Remove'**
  String get removeShort;

  /// No description provided for @playTooltip.
  ///
  /// In en, this message translates to:
  /// **'Play'**
  String get playTooltip;

  /// No description provided for @fh6SettingsTitle.
  ///
  /// In en, this message translates to:
  /// **'Forza Horizon 6 Integration Settings'**
  String get fh6SettingsTitle;

  /// No description provided for @mediaOverlaySettings.
  ///
  /// In en, this message translates to:
  /// **'Media Overlay Generator Settings'**
  String get mediaOverlaySettings;

  /// No description provided for @stationNameLabel.
  ///
  /// In en, this message translates to:
  /// **'Station Name in XML'**
  String get stationNameLabel;

  /// No description provided for @stationNameHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. Streamer Mode'**
  String get stationNameHint;

  /// No description provided for @sampleDurationLabel.
  ///
  /// In en, this message translates to:
  /// **'Sample Duration (s)'**
  String get sampleDurationLabel;

  /// No description provided for @sampleDurationHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. 300'**
  String get sampleDurationHint;

  /// No description provided for @displayNameLabel.
  ///
  /// In en, this message translates to:
  /// **'Game Display Name'**
  String get displayNameLabel;

  /// No description provided for @displayNameHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. OmniMix Player'**
  String get displayNameHint;

  /// No description provided for @artistNameLabel.
  ///
  /// In en, this message translates to:
  /// **'Artist Name'**
  String get artistNameLabel;

  /// No description provided for @artistNameHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. ChillPatcher'**
  String get artistNameHint;

  /// No description provided for @enableAnthemZip.
  ///
  /// In en, this message translates to:
  /// **'Enable Anthem.zip Processing'**
  String get enableAnthemZip;

  /// No description provided for @anthemModeLabel.
  ///
  /// In en, this message translates to:
  /// **'Anthem Mode'**
  String get anthemModeLabel;

  /// No description provided for @modeFull.
  ///
  /// In en, this message translates to:
  /// **'Full (Keep all original entries)'**
  String get modeFull;

  /// No description provided for @modePartial.
  ///
  /// In en, this message translates to:
  /// **'Partial (Remove empty directories)'**
  String get modePartial;

  /// No description provided for @logoOptionLabel.
  ///
  /// In en, this message translates to:
  /// **'Logo Option'**
  String get logoOptionLabel;

  /// No description provided for @copyDefaultLogo.
  ///
  /// In en, this message translates to:
  /// **'Copy default Horizon Pulse logo'**
  String get copyDefaultLogo;

  /// No description provided for @injectCustomPng.
  ///
  /// In en, this message translates to:
  /// **'Inject custom PNG image'**
  String get injectCustomPng;

  /// No description provided for @selectLogoPng.
  ///
  /// In en, this message translates to:
  /// **'Select Logo PNG'**
  String get selectLogoPng;

  /// No description provided for @browse.
  ///
  /// In en, this message translates to:
  /// **'Browse'**
  String get browse;

  /// No description provided for @saveAndApply.
  ///
  /// In en, this message translates to:
  /// **'Save & Apply'**
  String get saveAndApply;

  /// No description provided for @equalizer.
  ///
  /// In en, this message translates to:
  /// **'Equalizer'**
  String get equalizer;

  /// No description provided for @noSelectedInstance.
  ///
  /// In en, this message translates to:
  /// **'No audio instance selected'**
  String get noSelectedInstance;

  /// No description provided for @equalizerControl.
  ///
  /// In en, this message translates to:
  /// **'Equalizer Control'**
  String get equalizerControl;

  /// No description provided for @enabled.
  ///
  /// In en, this message translates to:
  /// **'Enabled'**
  String get enabled;

  /// No description provided for @selectPreset.
  ///
  /// In en, this message translates to:
  /// **'Select Preset'**
  String get selectPreset;

  /// No description provided for @reset.
  ///
  /// In en, this message translates to:
  /// **'Reset'**
  String get reset;

  /// No description provided for @globalGainPreamp.
  ///
  /// In en, this message translates to:
  /// **'Global Gain (Preamp):'**
  String get globalGainPreamp;

  /// No description provided for @softClip.
  ///
  /// In en, this message translates to:
  /// **'Soft Clip (Prevent Clipping)'**
  String get softClip;

  /// Active control point settings with frequency
  ///
  /// In en, this message translates to:
  /// **'Control Point Settings ({frequency} Hz)'**
  String controlPointSettingsActive(int frequency);

  /// No description provided for @controlPointSettingsNone.
  ///
  /// In en, this message translates to:
  /// **'Control Point Settings (None selected)'**
  String get controlPointSettingsNone;

  /// No description provided for @typeLabel.
  ///
  /// In en, this message translates to:
  /// **'Type: '**
  String get typeLabel;

  /// No description provided for @filterTypeBell.
  ///
  /// In en, this message translates to:
  /// **'Bell'**
  String get filterTypeBell;

  /// No description provided for @filterTypeLowShelf.
  ///
  /// In en, this message translates to:
  /// **'Low Shelf'**
  String get filterTypeLowShelf;

  /// No description provided for @filterTypeHighShelf.
  ///
  /// In en, this message translates to:
  /// **'High Shelf'**
  String get filterTypeHighShelf;

  /// No description provided for @filterTypeLowPass.
  ///
  /// In en, this message translates to:
  /// **'Low Pass'**
  String get filterTypeLowPass;

  /// No description provided for @filterTypeHighPass.
  ///
  /// In en, this message translates to:
  /// **'High Pass'**
  String get filterTypeHighPass;

  /// No description provided for @qFactorLabel.
  ///
  /// In en, this message translates to:
  /// **'Q Factor: '**
  String get qFactorLabel;

  /// No description provided for @equalizerTip.
  ///
  /// In en, this message translates to:
  /// **'Tip: Double-click on the canvas to create a control point. Select and drag a point to adjust frequency and gain.'**
  String get equalizerTip;

  /// No description provided for @audioBufferLatencyTip.
  ///
  /// In en, this message translates to:
  /// **'Audio buffer latency (smaller is faster, default 0.05s)'**
  String get audioBufferLatencyTip;

  /// No description provided for @manualInstall.
  ///
  /// In en, this message translates to:
  /// **'Manual Install'**
  String get manualInstall;

  /// No description provided for @checkInstallation.
  ///
  /// In en, this message translates to:
  /// **'Check Installation'**
  String get checkInstallation;

  /// No description provided for @manualInstallTitle.
  ///
  /// In en, this message translates to:
  /// **'Manual Installation Mode'**
  String get manualInstallTitle;

  /// No description provided for @manualInstallHint.
  ///
  /// In en, this message translates to:
  /// **'Please copy all files and folders from the staging folder (Source) and paste them directly into your game folder (Target) manually.'**
  String get manualInstallHint;

  /// No description provided for @manualInstallSource.
  ///
  /// In en, this message translates to:
  /// **'Source (Staging folder containing prepared files):'**
  String get manualInstallSource;

  /// No description provided for @manualInstallTarget.
  ///
  /// In en, this message translates to:
  /// **'Target (Game folder):'**
  String get manualInstallTarget;

  /// No description provided for @manualInstallCheckHint.
  ///
  /// In en, this message translates to:
  /// **'After copying the files manually, click \"Check Installation\" to verify and register.'**
  String get manualInstallCheckHint;

  /// No description provided for @manualInstallErrDb.
  ///
  /// In en, this message translates to:
  /// **'Failed to register the manual install in the local database.'**
  String get manualInstallErrDb;

  /// No description provided for @manualInstallErrFwDb.
  ///
  /// In en, this message translates to:
  /// **'Failed to register BepInEx in the database.'**
  String get manualInstallErrFwDb;

  /// No description provided for @manualInstallErrVerify.
  ///
  /// In en, this message translates to:
  /// **'Verification failed. Necessary files/directories are still missing in your game folder. Please check if you dragged/copied all files correctly.'**
  String get manualInstallErrVerify;

  /// No description provided for @confirmInstall.
  ///
  /// In en, this message translates to:
  /// **'Confirm & Install'**
  String get confirmInstall;

  /// No description provided for @manualInstallErrRegFailed.
  ///
  /// In en, this message translates to:
  /// **'Verification succeeded, but registration failed: {error}'**
  String manualInstallErrRegFailed(String error);

  /// No description provided for @manualInstallGuideTitle.
  ///
  /// In en, this message translates to:
  /// **'Manual Installation Steps'**
  String get manualInstallGuideTitle;

  /// No description provided for @manualInstallGuideSteps.
  ///
  /// In en, this message translates to:
  /// **'1. Click \'Open\' next to \'Source\' to access the staging folder.\n2. Click \'Open\' next to \'Target\' to access the game folder.\n3. Select and copy ALL items inside the \'Source\' folder.\n4. Paste them directly into the \'Target\' folder. Choose \'Replace\' if prompted.\n5. Click \'Check Installation\' below to verify and finish.'**
  String get manualInstallGuideSteps;

  /// No description provided for @shortcutSettings.
  ///
  /// In en, this message translates to:
  /// **'Gamepad Shortcut Settings'**
  String get shortcutSettings;

  /// No description provided for @shortcutSettingsDesc.
  ///
  /// In en, this message translates to:
  /// **'Configure gamepad shortcut keys to control playback, volume, etc.'**
  String get shortcutSettingsDesc;

  /// No description provided for @configureShortcutsButton.
  ///
  /// In en, this message translates to:
  /// **'Configure Shortcuts'**
  String get configureShortcutsButton;

  /// No description provided for @registeredEvents.
  ///
  /// In en, this message translates to:
  /// **'Registered Shortcut Events'**
  String get registeredEvents;

  /// No description provided for @prefixKey.
  ///
  /// In en, this message translates to:
  /// **'Prefix'**
  String get prefixKey;

  /// No description provided for @negationPrefix.
  ///
  /// In en, this message translates to:
  /// **'Negate Prefix'**
  String get negationPrefix;

  /// Label for regular key slot
  ///
  /// In en, this message translates to:
  /// **'Key {index}'**
  String regularKeySlot(int index);

  /// No description provided for @pressGamepadButton.
  ///
  /// In en, this message translates to:
  /// **'Press Gamepad Button...'**
  String get pressGamepadButton;

  /// No description provided for @clearShortcut.
  ///
  /// In en, this message translates to:
  /// **'Clear Shortcut'**
  String get clearShortcut;

  /// No description provided for @clearAll.
  ///
  /// In en, this message translates to:
  /// **'Clear All'**
  String get clearAll;

  /// No description provided for @shortcutEvaluationError.
  ///
  /// In en, this message translates to:
  /// **'Prefix cannot be configured alone. Please bind at least one regular key.'**
  String get shortcutEvaluationError;

  /// No description provided for @shortcutPlayPause.
  ///
  /// In en, this message translates to:
  /// **'Play / Pause'**
  String get shortcutPlayPause;

  /// No description provided for @shortcutNext.
  ///
  /// In en, this message translates to:
  /// **'Next Track'**
  String get shortcutNext;

  /// No description provided for @shortcutPrev.
  ///
  /// In en, this message translates to:
  /// **'Previous Track'**
  String get shortcutPrev;

  /// No description provided for @shortcutVolUp.
  ///
  /// In en, this message translates to:
  /// **'Volume Up +5%'**
  String get shortcutVolUp;

  /// No description provided for @shortcutVolDown.
  ///
  /// In en, this message translates to:
  /// **'Volume Down -5%'**
  String get shortcutVolDown;

  /// No description provided for @shortcutToggleFloatingPlayer.
  ///
  /// In en, this message translates to:
  /// **'Toggle Floating Player Window'**
  String get shortcutToggleFloatingPlayer;

  /// No description provided for @shortcutCenterLeftQuad.
  ///
  /// In en, this message translates to:
  /// **'Move Floating Player to Left 1/4'**
  String get shortcutCenterLeftQuad;
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
