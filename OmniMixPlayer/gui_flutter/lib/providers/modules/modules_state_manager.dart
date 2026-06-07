/// Modules (plugin) state & navigation manager.
/// Extracted from AppState during Riverpod migration.
library;

import 'package:flutter/foundation.dart';
import '../../services/api_client.dart';
import '../../models/node_data.dart';

class ModulesStateManager extends ChangeNotifier {
  final ApiClient Function() _getApi;

  ModulesStateManager(this._getApi);

  ApiClient get api => _getApi();

  // ── State ──
  List<ModuleInfoResponse> _modules = [];
  bool _loading = false;
  String? _activeModuleId;
  String _activeUiKind = 'default';
  String _activeLinkId = '';
  RawNodeData? _moduleUiTree;
  RawNodeData? _overlayUiTree;
  String _overlayMode = '';
  String _overlayTitle = '';

  // Getters
  List<ModuleInfoResponse> get modules => _modules;
  bool get loading => _loading;
  String? get activeModuleId => _activeModuleId;
  RawNodeData? get moduleUiTree => _moduleUiTree;
  RawNodeData? get overlayUiTree => _overlayUiTree;
  String get overlayMode => _overlayMode;
  String get overlayTitle => _overlayTitle;
  String get activeUiKind => _activeUiKind;
  String get activeLinkId => _activeLinkId;

  bool get hasOverlay => _overlayUiTree != null || _overlayMode == 'about';
  bool get hasModuleDetail => _activeModuleId != null && _moduleUiTree != null;

  // ── Load ──

  Future<void> loadModules() async {
    _loading = true;
    notifyListeners();
    try {
      _modules = await api.getModules();
    } catch (_) {}
    _loading = false;
    notifyListeners();
  }

  Future<void> refreshModules() async {
    await loadModules();
  }

  Future<void> setModuleEnabled(String moduleId, bool enabled) async {
    try {
      await api.setModuleEnabled(moduleId, enabled);
      for (final m in _modules) {
        if (m.id == moduleId) {
          final idx = _modules.indexOf(m);
          _modules[idx] = ModuleInfoResponse(
            id: m.id,
            name: m.name,
            version: m.version,
            priority: m.priority,
            loadedAt: m.loadedAt,
            enabled: enabled,
            hasSettingsUi: m.hasSettingsUi,
            hasQuickLinks: m.hasQuickLinks,
            linkEntries: m.linkEntries,
          );
          break;
        }
      }
      notifyListeners();
    } catch (e) {}
  }

  // ── Navigation ──

  Future<void> openModule(String moduleId) async {
    _activeModuleId = moduleId;
    _activeUiKind = 'default';
    _activeLinkId = '';
    _moduleUiTree = null;
    notifyListeners();
    try {
      _moduleUiTree = await api.getModuleUi(moduleId);
      notifyListeners();
    } catch (e) {}
  }

  void closeModule() {
    _activeModuleId = null;
    _activeUiKind = 'default';
    _activeLinkId = '';
    _moduleUiTree = null;
    notifyListeners();
  }

  Future<void> openModuleLink(String moduleId, String linkId) async {
    try {
      final tree = await api.getModuleLinkUi(moduleId, linkId);
      _overlayUiTree = tree;
      _overlayMode = 'ui';
      _overlayTitle = '$moduleId / $linkId';
      _activeModuleId = moduleId;
      _activeUiKind = 'link';
      _activeLinkId = linkId;
      notifyListeners();
    } catch (_) {}
  }

  Future<void> openModuleSettings(String moduleId) async {
    try {
      final tree = await api.getModuleSettingsUi(moduleId);
      _moduleUiTree = tree;
      _overlayMode = 'ui';
      _overlayTitle = '$moduleId / Settings';
      _activeModuleId = moduleId;
      _activeUiKind = 'settings';
      _activeLinkId = '';
      notifyListeners();
    } catch (_) {}
  }

  void openAbout() {
    _overlayUiTree = null;
    _overlayMode = 'about';
    _overlayTitle = 'About';
    notifyListeners();
  }

  void closeOverlay() {
    _overlayUiTree = null;
    _overlayMode = '';
    _overlayTitle = '';
    _activeUiKind = 'default';
    _activeLinkId = '';
    if (_activeModuleId != null && _moduleUiTree == null) {
      _activeModuleId = null;
    }
    notifyListeners();
  }

  // ── UI dispatch ──

  void dispatchUiEvent(
    String nodeId,
    String action,
    String value, {
    required bool isWsConnected,
    required void Function(
      String,
      String,
      String,
      String, {
      required String uiKind,
      required String linkId,
    })
    sendFn,
  }) {
    if (!isWsConnected) return;
    sendFn(
      _activeModuleId ?? '',
      nodeId,
      action,
      value,
      uiKind: _activeUiKind,
      linkId: _activeLinkId,
    );
  }

  // ── UI push from WS ──

  void handleUiPush(dynamic push) {
    if (push.replace && push.tree != null) {
      if (push.moduleId.isNotEmpty && push.moduleId != _activeModuleId) return;
      if (_overlayUiTree != null) {
        _overlayUiTree = push.tree;
      } else {
        _moduleUiTree = push.tree;
      }
      notifyListeners();
    }
  }

  // ── Clear on disconnect ──

  void clearOnDisconnect() {
    _modules.clear();
    _moduleUiTree = null;
    _activeModuleId = null;
  }

  // ── Tab change (clear overlay) ──

  void onTabChanged() {
    _overlayUiTree = null;
    _overlayMode = '';
    _overlayTitle = '';
  }

  void _logImageNodes(RawNodeData? node, String context) {
    if (node == null) return;
    if (node.nodeType == 'Image') {}
    for (final child in node.children) {
      _logImageNodes(child, context);
    }
    for (final item in node.items) {
      _logImageNodes(item, context);
    }
  }
}
