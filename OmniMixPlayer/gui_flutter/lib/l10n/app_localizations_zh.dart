// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Chinese (`zh`).
class AppLocalizationsZh extends AppLocalizations {
  AppLocalizationsZh([String locale = 'zh']) : super(locale);

  @override
  String get appTitle => 'OmniMixPlayer';

  @override
  String get welcomeMessage => '欢迎使用 OmniMixPlayer';

  @override
  String get welcomeHint => '选择一个标签页或模块开始使用';

  @override
  String get home => '首页';

  @override
  String get playlist => '歌单';

  @override
  String get launchpad => '启动台';

  @override
  String get modules => '模块';

  @override
  String get settings => '设置';

  @override
  String get gameIntegration => '游戏集成';

  @override
  String get selectGameDir => '选择游戏目录';

  @override
  String get gamePath => '游戏路径';

  @override
  String get bepinexStatus => 'BepInEx 状态';

  @override
  String get modStatus => 'Mod 状态';

  @override
  String get installBepInEx => '安装 BepInEx';

  @override
  String get uninstallBepInEx => '卸载 BepInEx';

  @override
  String get installMod => '安装 Mod';

  @override
  String get uninstallMod => '卸载 Mod';

  @override
  String get reinstallBepInEx => '重新安装 BepInEx';

  @override
  String get reinstallMod => '重新安装 Mod';

  @override
  String get statusNotInstalled => '未安装';

  @override
  String get statusManaged => '已管理';

  @override
  String get statusUnmanaged => '外部安装 / 未管理';

  @override
  String get unmanagedWarning =>
      '检测到游戏目录下已存在外部安装的 BepInEx。OmniMix 将仅管理游戏 Mod 插件，不会对 BepInEx 核心文件进行管理或覆盖。';

  @override
  String get invalidGameDir =>
      '无效的游戏目录！请确认选择的是 \'Chill With You\' 的根目录（包含 \'Chill With You.exe\' 和 \'Chill With You_Data\'）。';

  @override
  String get modInstalled => '已安装';

  @override
  String get deploymentLogs => '部署日志';

  @override
  String get chooseFolder => '选择文件夹';

  @override
  String get about => '关于';

  @override
  String get version => 'v1.0.0';

  @override
  String get builtWith => '基于 Flutter + Dart 构建';

  @override
  String get backendDesc => 'C# .NET 8 后端 + Flutter 前端';

  @override
  String get back => '返回';

  @override
  String get backendControl => '后端控制';

  @override
  String get backendService => '服务管理';

  @override
  String get status => '状态';

  @override
  String get running => '运行中';

  @override
  String get stopped => '已停止';

  @override
  String get startBackend => '启动后端';

  @override
  String get stopBackend => '停止后端';

  @override
  String get restartBackend => '重启后端';

  @override
  String get restarting => '重启中...';

  @override
  String get port => '端口';

  @override
  String get bind => '绑定地址';

  @override
  String get guiSettings => 'GUI 设置';

  @override
  String get autoStart => '开机自启';

  @override
  String get minimizeToTray => '最小化到托盘';

  @override
  String get appearance => '外观';

  @override
  String get theme => '主题';

  @override
  String get themeLight => '浅色';

  @override
  String get themeDark => '深色';

  @override
  String get themeSystem => '跟随系统';

  @override
  String get language => '语言';

  @override
  String get saveAndRestart => '保存并重启';

  @override
  String get resetToDefaults => '恢复默认';

  @override
  String get resetConfirm => '确定要恢复所有设置为默认值吗？';

  @override
  String get configReset => '配置已恢复为默认值';

  @override
  String get open => '打开';

  @override
  String get loaded => '已加载';

  @override
  String get disabled => '已禁用';

  @override
  String get manageModules => '管理已安装的模块及其设置';

  @override
  String get noModuleLinks => '暂无模块链接';

  @override
  String get clickIconToOpen => '点击图标打开模块面板';

  @override
  String get connected => '已连接';

  @override
  String get disconnected => '未连接';

  @override
  String get showHideWindow => '显示/隐藏窗口';

  @override
  String get fullyExit => '完全退出';

  @override
  String get serviceManagement => '服务管理（安装/卸载）';

  @override
  String get serviceStatus => '服务状态';

  @override
  String get serviceRunning => '服务运行中';

  @override
  String get serviceInstalled => '服务已安装（未运行）';

  @override
  String get serviceNotInstalled => '服务未安装';

  @override
  String get installService => '安装服务';

  @override
  String get uninstallService => '卸载服务';

  @override
  String get serviceInstallSuccess => '服务安装成功';

  @override
  String get serviceInstallFailed => '服务安装失败';

  @override
  String get serviceUninstallSuccess => '服务已卸载';

  @override
  String get serviceUninstallFailed => '服务卸载失败';

  @override
  String get processMode => '进程';

  @override
  String get serviceMode => '服务';

  @override
  String get serviceAutoStart => '服务自动启动';

  @override
  String get websiteLink => '官网';

  @override
  String get save => '保存';

  @override
  String get cancel => '取消';

  @override
  String get pendingSave => '待保存';

  @override
  String get clear => '清除';

  @override
  String get openFolder => '打开目录';

  @override
  String installed(String version) {
    return '已安装 v$version';
  }

  @override
  String get closeBehavior => '关闭行为';

  @override
  String get closeMinimize => '最小化到托盘';

  @override
  String get closeExit => '退出程序';

  @override
  String get themeColor => '主题色';

  @override
  String get customColor => '自定义';

  @override
  String get hue => '色相';

  @override
  String get saturation => '饱和度';

  @override
  String get brightness => '亮度';

  @override
  String get confirm => '确定';

  @override
  String get useSystemColor => '使用系统主题色';

  @override
  String get backendConfig => '后端配置';

  @override
  String get moduleStatusDisabled => '已禁用 — 下次启动将不再加载';

  @override
  String get moduleStatusPending => '已加载但下次启动将禁用';

  @override
  String get moduleStatusActive => '已加载并激活';

  @override
  String get moduleStatusWillLoad => '下次启动时将加载';

  @override
  String get serviceStarting => '服务启动中';

  @override
  String get serviceStartingMessage => '正在连接并初始化播放器服务...';

  @override
  String get serviceNotConnected => '服务未连接';

  @override
  String get waitingForBackend => '请稍候，正在等待后端服务就绪...';

  @override
  String get noSongPlaying => '没有正在播放的歌曲';

  @override
  String get shuffle => '随机';

  @override
  String get previous => '上一首';

  @override
  String get next => '下一首';

  @override
  String get playPause => '播放/暂停';

  @override
  String get repeatOne => '单曲循环';

  @override
  String get serverControlMode => '服务端控制模式';

  @override
  String get clientModeControlsDisabled => '客户端模式：播放控制由客户端管理';

  @override
  String get queue => '队列';

  @override
  String get history => '历史';

  @override
  String get empty => '空';

  @override
  String get byPlaylist => '按歌单';

  @override
  String get byAlbum => '按专辑';

  @override
  String get bySong => '按歌曲';

  @override
  String get addSource => '添加来源';

  @override
  String get searchHint => '搜索歌曲 / 艺术家 / 专辑';

  @override
  String get removeFromLibrary => '从曲库移除';

  @override
  String errorWithMessage(String error) {
    return '错误：$error';
  }

  @override
  String get noActivePlaylist => '当前没有激活歌单，请先添加歌单或专辑来源';

  @override
  String get noSongs => '没有歌曲';

  @override
  String get noAlbumsAdded => '当前没有已添加的专辑';

  @override
  String get noPlaylistsAdded => '当前没有已添加的歌单';

  @override
  String get selectLibrarySource => '选择要加入曲库的来源';

  @override
  String selectedCount(int count) {
    return '已选 $count';
  }

  @override
  String get playlistsTab => '歌单';

  @override
  String get albumsTab => '专辑';

  @override
  String songCountWithModule(int count, String module) {
    return '$count 首 · $module';
  }

  @override
  String failedToLoadLibrary(String error) {
    return '加载曲库失败：$error';
  }

  @override
  String get instanceManagement => '实例管理';

  @override
  String get noInstalledInstances => '暂无已安装实例';

  @override
  String installedInstancesCount(int count) {
    return '已安装实例 ($count)';
  }

  @override
  String archiveCount(int count) {
    return '归档 ($count)';
  }

  @override
  String get instanceAutoRegisterHint => '安装游戏 Mod 后，实例会自动注册';

  @override
  String get archiveManagement => '归档管理';

  @override
  String get noArchivedInstances => '暂无归档实例';

  @override
  String get rename => '重命名';

  @override
  String get delete => '删除';

  @override
  String get close => '关闭';

  @override
  String get renameArchive => '重命名归档';

  @override
  String get archiveNameHint => '输入归档名称';

  @override
  String get deleteArchive => '删除归档';

  @override
  String deleteArchiveConfirm(String name) {
    return '确定要删除\"$name\"的归档吗？此操作不可撤销。';
  }

  @override
  String get deleteInstance => '删除实例';

  @override
  String deleteInstanceConfirm(String id) {
    return '确定要删除实例 \"$id\" 吗？这将同时移除配置文件和游戏注册。';
  }

  @override
  String get archiveInstanceTooltip => '保存为归档';

  @override
  String get archiveInstance => '保存实例归档';

  @override
  String archiveInstanceHint(String id) {
    return '将实例 \"$id\" 的当前设置保存为归档，之后可在安装时继承。';
  }

  @override
  String get archiveAction => '保存归档';

  @override
  String get inheritArchiveTitle => '继承归档设置';

  @override
  String get inheritArchiveHint => '选择一个归档来继承播放列表和设置。未绑定的归档会被消费，已绑定的会复制。';

  @override
  String get archiveBoundWillCopy => '已绑定实例 · 将复制设置';

  @override
  String get archiveFreeWillConsume => '未绑定 · 将直接消费此归档';

  @override
  String get skipInherit => '跳过，全新安装';

  @override
  String get uninstallServiceConfirm => '这将停止后端服务并将其从系统中移除。';

  @override
  String get serviceAutoStartUpdated => '服务开机自启已更新';

  @override
  String get serviceAutoStartFailed => '服务开机自启更新失败';

  @override
  String get autoStartSuccess => '开机自启设置成功';

  @override
  String get autoStartFailed => '开机自启设置失败';

  @override
  String get waitingForBackendMod => '请稍候，正在等待后端服务就绪以进行 Mod 安装和管理...';

  @override
  String get omnimixInstance => 'OmniMix 实例';

  @override
  String instanceOnline(String id) {
    return '在线 — $id';
  }

  @override
  String instanceOffline(String id) {
    return '离线 — $id';
  }

  @override
  String availableVersion(String version) {
    return '可安装版本: v$version';
  }

  @override
  String latestVersion(String version) {
    return '最新: v$version';
  }

  @override
  String get refresh => '刷新';

  @override
  String get noInstances => '没有实例';

  @override
  String get exitGui => '退出 GUI';

  @override
  String get languageChinese => '中文';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSystem => '跟随系统';

  @override
  String get backendNotConnected => '后端未连接';

  @override
  String get libraryBrowser => '曲库浏览器';

  @override
  String playlistStats(int tags, int songs) {
    return '$tags 个歌单 · $songs 首曲目';
  }

  @override
  String get libraryEmpty => '曲库为空';

  @override
  String loadLibraryFailed(String error) {
    return '加载曲库失败：$error';
  }

  @override
  String fromModule(String module) {
    return '来自 $module';
  }

  @override
  String albumCountLabel(int count) {
    return '$count 个专辑';
  }

  @override
  String songCountLabel(int count) {
    return '$count 首';
  }

  @override
  String get unknownArtist => '未知艺术家';

  @override
  String sourceChipLabel(String name, int count) {
    return '$name（$count）';
  }

  @override
  String get playNext => '下一首播放';

  @override
  String get addToQueueTail => '添加到队尾';

  @override
  String get exclude => '排除';

  @override
  String get removeExclusion => '取消排除';

  @override
  String get removeShort => '移除';

  @override
  String get playTooltip => '播放';

  @override
  String get fh6SettingsTitle => 'Forza Horizon 6 集成设置';

  @override
  String get mediaOverlaySettings => '媒体覆盖生成器设置';

  @override
  String get stationNameLabel => 'XML 中的电台名称';

  @override
  String get stationNameHint => '例如：Streamer Mode';

  @override
  String get sampleDurationLabel => '采样时长（秒）';

  @override
  String get sampleDurationHint => '例如：300';

  @override
  String get displayNameLabel => '游戏显示名称';

  @override
  String get displayNameHint => '例如：OmniMix Player';

  @override
  String get artistNameLabel => '艺术家名称';

  @override
  String get artistNameHint => '例如：ChillPatcher';

  @override
  String get enableAnthemZip => '启用 Anthem.zip 处理';

  @override
  String get anthemModeLabel => '国歌模式';

  @override
  String get modeFull => '完整（保留所有原始条目）';

  @override
  String get modePartial => '精简（清除空目录）';

  @override
  String get logoOptionLabel => 'Logo 选项';

  @override
  String get copyDefaultLogo => '复制默认 Horizon Pulse Logo';

  @override
  String get injectCustomPng => '注入自定义 PNG 图片';

  @override
  String get selectLogoPng => '选择 Logo PNG';

  @override
  String get browse => '浏览';

  @override
  String get saveAndApply => '保存并应用';

  @override
  String get equalizer => '均衡器';

  @override
  String get noSelectedInstance => '没有选中的音频实例';

  @override
  String get equalizerControl => '均衡器控制';

  @override
  String get enabled => '已启用';

  @override
  String get selectPreset => '选择预设';

  @override
  String get reset => '重置';

  @override
  String get globalGainPreamp => '整体增益 (Preamp):';

  @override
  String get softClip => '防爆音(软剪切)';

  @override
  String controlPointSettingsActive(int frequency) {
    return '控制点设置 ($frequency Hz)';
  }

  @override
  String get controlPointSettingsNone => '控制点设置 (未选择)';

  @override
  String get typeLabel => '类型: ';

  @override
  String get filterTypeBell => '钟形 (Bell)';

  @override
  String get filterTypeLowShelf => '低架 (Low Shelf)';

  @override
  String get filterTypeHighShelf => '高架 (High Shelf)';

  @override
  String get filterTypeLowPass => '低通 (Low Pass)';

  @override
  String get filterTypeHighPass => '高通 (High Pass)';

  @override
  String get qFactorLabel => '带宽 (Q值): ';

  @override
  String get equalizerTip => '提示: 在画布上双击可新建控制点，单选控制点可以拖动调节频率与增益';

  @override
  String get audioBufferLatencyTip => '物理缓冲区限流延迟（越小响应越快，默认0.05秒）';
}
