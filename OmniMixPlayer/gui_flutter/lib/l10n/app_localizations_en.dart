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
  String get gameIntegration => 'Game Integration';

  @override
  String get selectGameDir => 'Select Game Directory';

  @override
  String get gamePath => 'Game Path';

  @override
  String get bepinexStatus => 'BepInEx Status';

  @override
  String get modStatus => 'Mod Status';

  @override
  String get installBepInEx => 'Install BepInEx';

  @override
  String get uninstallBepInEx => 'Uninstall BepInEx';

  @override
  String get installMod => 'Install Mod';

  @override
  String get uninstallMod => 'Uninstall Mod';

  @override
  String get reinstallBepInEx => 'Reinstall BepInEx';

  @override
  String get reinstallMod => 'Reinstall Mod';

  @override
  String get statusNotInstalled => 'Not Installed';

  @override
  String get statusManaged => 'Managed';

  @override
  String get statusUnmanaged => 'Pre-installed / Unmanaged';

  @override
  String get unmanagedWarning =>
      'BepInEx is already pre-installed. OmniMix will only manage Mod plugins. Core loader will not be managed or overwritten.';

  @override
  String get invalidGameDir =>
      'Invalid game directory! Please select the root folder of \'Chill With You\' containing \'Chill With You.exe\' and \'Chill With You_Data\'.';

  @override
  String get modInstalled => 'Installed';

  @override
  String get deploymentLogs => 'Deployment Logs';

  @override
  String get chooseFolder => 'Choose Folder';

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

  @override
  String get serviceAutoStart => 'Service Auto-Start';

  @override
  String get websiteLink => 'Website';

  @override
  String get save => 'Save';

  @override
  String get cancel => 'Cancel';

  @override
  String get pendingSave => 'Pending Save';

  @override
  String get clear => 'Clear';

  @override
  String get openFolder => 'Open Folder';

  @override
  String installed(String version) {
    return 'Installed v$version';
  }

  @override
  String get closeBehavior => 'Close Behavior';

  @override
  String get closeMinimize => 'Minimize to Tray';

  @override
  String get closeExit => 'Exit App';

  @override
  String get themeColor => 'Theme Color';

  @override
  String get customColor => 'Custom';

  @override
  String get hue => 'Hue';

  @override
  String get saturation => 'Saturation';

  @override
  String get brightness => 'Brightness';

  @override
  String get confirm => 'OK';

  @override
  String get useSystemColor => 'Use system accent color';

  @override
  String get backendConfig => 'Backend Config';

  @override
  String get moduleStatusDisabled => 'Disabled — will not load on next start';

  @override
  String get moduleStatusPending => 'Loaded but will be disabled on next start';

  @override
  String get moduleStatusActive => 'Loaded and active';

  @override
  String get moduleStatusWillLoad => 'Will load on next start';

  @override
  String get serviceStarting => 'Service Starting';

  @override
  String get serviceStartingMessage =>
      'Connecting and initializing player service...';

  @override
  String get serviceNotConnected => 'Service Not Connected';

  @override
  String get waitingForBackend => 'Please wait, waiting for backend service...';

  @override
  String get noSongPlaying => 'No song playing';

  @override
  String get shuffle => 'Shuffle';

  @override
  String get previous => 'Previous';

  @override
  String get next => 'Next';

  @override
  String get playPause => 'Play/Pause';

  @override
  String get repeatOne => 'Repeat One';

  @override
  String get serverControlMode => 'Server control mode';

  @override
  String get clientModeControlsDisabled => 'Client mode: controls disabled';

  @override
  String get queue => 'Queue';

  @override
  String get history => 'History';

  @override
  String get empty => 'Empty';

  @override
  String get byPlaylist => 'By Playlist';

  @override
  String get byAlbum => 'By Album';

  @override
  String get bySong => 'By Song';

  @override
  String get addSource => 'Add Source';

  @override
  String get searchHint => 'Search songs / artist / album';

  @override
  String get removeFromLibrary => 'Remove from Library';

  @override
  String errorWithMessage(String error) {
    return 'Error: $error';
  }

  @override
  String get noActivePlaylist =>
      'No active playlist, add a playlist or album source first';

  @override
  String get noSongs => 'No songs';

  @override
  String get noAlbumsAdded => 'No albums added';

  @override
  String get noPlaylistsAdded => 'No playlists added';

  @override
  String get selectLibrarySource => 'Select sources to add to library';

  @override
  String selectedCount(int count) {
    return 'Selected $count';
  }

  @override
  String get playlistsTab => 'Playlists';

  @override
  String get albumsTab => 'Albums';

  @override
  String songCountWithModule(int count, String module) {
    return '$count songs · $module';
  }

  @override
  String failedToLoadLibrary(String error) {
    return 'Failed to load library: $error';
  }

  @override
  String get instanceManagement => 'Instance Management';

  @override
  String get noInstalledInstances => 'No installed instances';

  @override
  String installedInstancesCount(int count) {
    return 'Installed Instances ($count)';
  }

  @override
  String archiveCount(int count) {
    return 'Archive ($count)';
  }

  @override
  String get instanceAutoRegisterHint =>
      'Instances will be auto-registered after installing game Mod';

  @override
  String get archiveManagement => 'Archive Management';

  @override
  String get noArchivedInstances => 'No archived instances';

  @override
  String get rename => 'Rename';

  @override
  String get delete => 'Delete';

  @override
  String get close => 'Close';

  @override
  String get renameArchive => 'Rename Archive';

  @override
  String get archiveNameHint => 'Enter archive name';

  @override
  String get deleteArchive => 'Delete Archive';

  @override
  String deleteArchiveConfirm(String name) {
    return 'Are you sure you want to delete the archive of \"$name\"? This action cannot be undone.';
  }

  @override
  String get deleteInstance => 'Delete Instance';

  @override
  String deleteInstanceConfirm(String id) {
    return 'Delete instance \"$id\"? This will also remove its config and game registration.';
  }

  @override
  String get archiveInstanceTooltip => 'Save as archive';

  @override
  String get archiveInstance => 'Archive Instance';

  @override
  String archiveInstanceHint(String id) {
    return 'Save instance \"$id\" settings as an archive for later reuse during installation.';
  }

  @override
  String get archiveAction => 'Archive';

  @override
  String get inheritArchiveTitle => 'Inherit Archive Settings';

  @override
  String get inheritArchiveHint =>
      'Choose an archive to inherit playlist and settings from. Unbound archives will be consumed, bound ones will be copied.';

  @override
  String get archiveBoundWillCopy => 'Bound to instance · Will copy settings';

  @override
  String get archiveFreeWillConsume => 'Unbound · Will consume this archive';

  @override
  String get skipInherit => 'Skip, fresh install';

  @override
  String get uninstallServiceConfirm =>
      'This will stop the backend service and remove it from the system.';

  @override
  String get serviceAutoStartUpdated => 'Service auto-start updated';

  @override
  String get serviceAutoStartFailed => 'Failed to update service auto-start';

  @override
  String get autoStartSuccess => 'Auto-start updated successfully';

  @override
  String get autoStartFailed => 'Failed to update auto-start';

  @override
  String get waitingForBackendMod =>
      'Please wait, waiting for backend service for Mod installation and management...';

  @override
  String get omnimixInstance => 'OmniMix Instance';

  @override
  String instanceOnline(String id) {
    return 'Online — $id';
  }

  @override
  String instanceOffline(String id) {
    return 'Offline — $id';
  }

  @override
  String availableVersion(String version) {
    return 'Available: v$version';
  }

  @override
  String latestVersion(String version) {
    return 'Latest: v$version';
  }

  @override
  String get refresh => 'Refresh';

  @override
  String get noInstances => 'No instances';

  @override
  String get exitGui => 'Exit GUI';

  @override
  String get languageChinese => '中文';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSystem => 'System';

  @override
  String get backendNotConnected => 'Backend Not Connected';

  @override
  String get libraryBrowser => 'Library Browser';

  @override
  String playlistStats(int tags, int songs) {
    return '$tags playlists · $songs songs';
  }

  @override
  String get libraryEmpty => 'Library is empty';

  @override
  String loadLibraryFailed(String error) {
    return 'Failed to load library: $error';
  }

  @override
  String fromModule(String module) {
    return 'from $module';
  }

  @override
  String albumCountLabel(int count) {
    return '$count albums';
  }

  @override
  String songCountLabel(int count) {
    return '$count songs';
  }

  @override
  String get unknownArtist => 'Unknown artist';

  @override
  String sourceChipLabel(String name, int count) {
    return '$name ($count)';
  }

  @override
  String get playNext => 'Play next';

  @override
  String get addToQueueTail => 'Add to queue tail';

  @override
  String get exclude => 'Exclude';

  @override
  String get removeExclusion => 'Remove exclusion';

  @override
  String get removeShort => 'Remove';

  @override
  String get playTooltip => 'Play';

  @override
  String get fh6SettingsTitle => 'Forza Horizon 6 Integration Settings';

  @override
  String get mediaOverlaySettings => 'Media Overlay Generator Settings';

  @override
  String get stationNameLabel => 'Station Name in XML';

  @override
  String get stationNameHint => 'e.g. Streamer Mode';

  @override
  String get sampleDurationLabel => 'Sample Duration (s)';

  @override
  String get sampleDurationHint => 'e.g. 300';

  @override
  String get displayNameLabel => 'Game Display Name';

  @override
  String get displayNameHint => 'e.g. OmniMix Player';

  @override
  String get artistNameLabel => 'Artist Name';

  @override
  String get artistNameHint => 'e.g. ChillPatcher';

  @override
  String get enableAnthemZip => 'Enable Anthem.zip Processing';

  @override
  String get anthemModeLabel => 'Anthem Mode';

  @override
  String get modeFull => 'Full (Keep all original entries)';

  @override
  String get modePartial => 'Partial (Remove empty directories)';

  @override
  String get logoOptionLabel => 'Logo Option';

  @override
  String get copyDefaultLogo => 'Copy default Horizon Pulse logo';

  @override
  String get injectCustomPng => 'Inject custom PNG image';

  @override
  String get selectLogoPng => 'Select Logo PNG';

  @override
  String get browse => 'Browse';

  @override
  String get saveAndApply => 'Save & Apply';
}
