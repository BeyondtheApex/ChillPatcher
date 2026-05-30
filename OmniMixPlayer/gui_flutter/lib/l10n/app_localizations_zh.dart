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
}
