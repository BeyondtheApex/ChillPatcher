/// JSON models matching the C# backend NodeData tree structure.
class RawNodeData {
  final String id;
  final String nodeType;
  final String text;
  final double fontSize;
  final String color;
  final String direction;
  final String crossAxisAlignment;
  final double spacing;
  final double padding;
  final List<RawNodeData> children;
  final String value;
  final String inputType;
  final String buttonVariant;
  final bool checked;
  final String source;
  final double imageWidth;
  final double imageHeight;
  final String imageFit;
  final String selectedValue;
  final List<RawOptionData> options;
  final List<RawNodeData> items;

  RawNodeData({
    this.id = '',
    this.nodeType = '',
    this.text = '',
    this.fontSize = 14.0,
    this.color = '',
    this.crossAxisAlignment = '',
    this.direction = '',
    this.spacing = 8.0,
    this.padding = 0.0,
    this.children = const [],
    this.value = '',
    this.inputType = 'text',
    this.buttonVariant = 'primary',
    this.checked = false,
    this.source = '',
    this.imageWidth = 200.0,
    this.imageHeight = 200.0,
    this.imageFit = 'contain',
    this.selectedValue = '',
    this.options = const [],
    this.items = const [],
  });

  factory RawNodeData.fromJson(Map<String, dynamic> json) {
    return RawNodeData(
      id: json['id'] ?? '',
      nodeType: json['node-type'] ?? '',
      text: json['text'] ?? '',
      fontSize: (json['font-size'] ?? 14.0).toDouble(),
      color: json['color'] ?? '',
      crossAxisAlignment: json['cross-axis-align'] ?? '',
      direction: json['direction'] ?? '',
      spacing: (json['spacing'] ?? 8.0).toDouble(),
      padding: (json['padding'] ?? 0.0).toDouble(),
      children:
          (json['children'] as List<dynamic>?)
              ?.map((c) => RawNodeData.fromJson(c as Map<String, dynamic>))
              .toList() ??
          [],
      value: json['value'] ?? '',
      inputType: json['input-type'] ?? 'text',
      buttonVariant: json['button-variant'] ?? 'primary',
      checked: json['checked'] ?? false,
      source: json['source'] ?? '',
      imageWidth: (json['image-width'] ?? 200.0).toDouble(),
      imageHeight: (json['image-height'] ?? 200.0).toDouble(),
      imageFit: json['image-fit'] ?? 'contain',
      selectedValue: json['selected-value'] ?? '',
      options:
          (json['options'] as List<dynamic>?)
              ?.map((o) => RawOptionData.fromJson(o as Map<String, dynamic>))
              .toList() ??
          [],
      items:
          (json['items'] as List<dynamic>?)
              ?.map((i) => RawNodeData.fromJson(i as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}

class RawOptionData {
  final String value;
  final String label;

  RawOptionData({this.value = '', this.label = ''});

  factory RawOptionData.fromJson(Map<String, dynamic> json) {
    return RawOptionData(
      value: json['value'] ?? '',
      label: json['label'] ?? '',
    );
  }
}

class ModuleInfoResponse {
  final String id;
  final String name;
  final String version;
  final int priority;
  final String loadedAt;
  final bool enabled;
  final bool hasSettingsUi;
  final bool hasQuickLinks;
  final List<ModuleLinkEntryResponse> linkEntries;

  ModuleInfoResponse({
    this.id = '',
    this.name = '',
    this.version = '',
    this.priority = 0,
    this.loadedAt = '',
    this.enabled = true,
    this.hasSettingsUi = false,
    this.hasQuickLinks = false,
    this.linkEntries = const [],
  });

  factory ModuleInfoResponse.fromJson(Map<String, dynamic> json) {
    return ModuleInfoResponse(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      version: json['version'] ?? '',
      priority: json['priority'] ?? 0,
      loadedAt: json['loadedAt'] ?? '',
      enabled: json['enabled'] ?? true,
      hasSettingsUi: json['hasSettingsUI'] ?? false,
      hasQuickLinks: json['hasQuickLinks'] ?? false,
      linkEntries:
          (json['linkEntries'] as List<dynamic>?)
              ?.map(
                (e) =>
                    ModuleLinkEntryResponse.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          [],
    );
  }
}

class ModuleLinkEntryResponse {
  final String id;
  final String title;
  final String icon;
  final String svg;
  final String backgroundColor;
  final String iconColor;

  ModuleLinkEntryResponse({
    this.id = '',
    this.title = '',
    this.icon = '',
    this.svg = '',
    this.backgroundColor = '',
    this.iconColor = '',
  });

  factory ModuleLinkEntryResponse.fromJson(Map<String, dynamic> json) {
    return ModuleLinkEntryResponse(
      id: json['id'] ?? '',
      title: json['title'] ?? '',
      icon: json['icon'] ?? '',
      svg: json['svg'] ?? '',
      backgroundColor: json['backgroundColor'] ?? '',
      iconColor: json['iconColor'] ?? '',
    );
  }
}

/// WS ui_push payload (JSON, module UI)
class UiPushPayload {
  final String moduleId;
  final bool replace;
  final RawNodeData? tree;
  final List<Map<String, dynamic>>? updates;

  UiPushPayload({
    this.moduleId = '',
    this.replace = true,
    this.tree,
    this.updates,
  });

  factory UiPushPayload.fromJson(Map<String, dynamic> json) {
    return UiPushPayload(
      moduleId: json['moduleId'] ?? '',
      replace: json['replace'] ?? true,
      tree: json['tree'] != null
          ? RawNodeData.fromJson(json['tree'] as Map<String, dynamic>)
          : null,
      updates: (json['updates'] as List<dynamic>?)
          ?.map((u) => u as Map<String, dynamic>)
          .toList(),
    );
  }
}

/// Config data to send via PUT /api/config
class AppConfig {
  String backendPort;
  String backendBind;
  bool autostart;
  bool minimizeToTray;
  String theme;
  String language;
  int seedColor;
  bool useSystemColor;
  String closeBehavior;

  AppConfig({
    this.backendPort = '17890',
    this.backendBind = '127.0.0.1',
    this.autostart = false,
    this.minimizeToTray = true,
    this.theme = 'system',
    this.language = 'system',
    this.seedColor = 0xFF673AB7,
    this.useSystemColor = true,
    this.closeBehavior = 'exit',
  });

  Map<String, dynamic> toJson() => {
    'backend_port': backendPort,
    'backend_bind': backendBind,
    'autostart': autostart,
    'minimize_to_tray': minimizeToTray,
    'theme': theme,
    'language': language,
    'seed_color': seedColor,
    'use_system_color': useSystemColor,
    'close_behavior': closeBehavior,
  };
}

// ═══════════════════════════════════════════════════════════
//  Re-exports: data models now come from generated protobuf types.
//  Import from proto files directly:
//    import '../generated/omni_mix_player/models/track.pb.dart';   → Track
//    import '../generated/omni_mix_player/models/album.pb.dart';   → Album
//    import '../generated/omni_mix_player/models/tag.pb.dart';     → Tag
//    import '../generated/omni_mix_player/models/instance.pb.dart';→ InstanceSummary, InstanceProfile, PlaybackQueueState, PlaylistSourceState
//    import '../generated/omni_mix_player/services/playback.pb.dart'; → QueueTrack, PlaybackStatus, RepeatMode
//    import '../generated/omni_mix_player/events/ws_events.pb.dart'; → WsEvent (protobuf binary)
// ═══════════════════════════════════════════════════════════
