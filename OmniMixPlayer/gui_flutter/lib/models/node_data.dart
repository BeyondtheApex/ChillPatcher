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

/// WS event wrapper
class WsEvent {
  final String type;
  final dynamic data;

  WsEvent({this.type = '', this.data});

  factory WsEvent.fromJson(Map<String, dynamic> json) {
    return WsEvent(
      type: (json['type'] ?? json['event'] ?? '') as String,
      data: json['data'],
    );
  }
}

/// WS ui_push payload
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
//  Playlist models (matching backend /api/tags, /api/albums, /api/songs)
// ═══════════════════════════════════════════════════════════

class TagInfo {
  final String id;
  final String name;
  final String moduleId;
  final int bitValue;
  final bool isGrowable;

  TagInfo({
    this.id = '',
    this.name = '',
    this.moduleId = '',
    this.bitValue = 0,
    this.isGrowable = false,
  });

  factory TagInfo.fromJson(Map<String, dynamic> json) {
    return TagInfo(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      moduleId: json['moduleId'] ?? '',
      bitValue: json['bitValue'] ?? 0,
      isGrowable: json['isGrowable'] ?? false,
    );
  }
}

class AlbumInfo {
  final String id;
  final String name;
  final String tagId;
  final String moduleId;
  final String coverPath;
  final int songCount;
  final bool isGrowable;

  AlbumInfo({
    this.id = '',
    this.name = '',
    this.tagId = '',
    this.moduleId = '',
    this.coverPath = '',
    this.songCount = 0,
    this.isGrowable = false,
  });

  factory AlbumInfo.fromJson(Map<String, dynamic> json) {
    return AlbumInfo(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      tagId: json['tagId'] ?? '',
      moduleId: json['moduleId'] ?? '',
      coverPath: json['coverPath'] ?? '',
      songCount: json['songCount'] ?? 0,
      isGrowable: json['isGrowable'] ?? false,
    );
  }
}

class SongInfo {
  final String uuid;
  final String title;
  final String artist;
  final String albumId;
  final double duration;
  final String moduleId;
  final bool isFavorite;
  final bool isExcluded;

  SongInfo({
    this.uuid = '',
    this.title = '',
    this.artist = '',
    this.albumId = '',
    this.duration = 0.0,
    this.moduleId = '',
    this.isFavorite = false,
    this.isExcluded = false,
  });

  factory SongInfo.fromJson(Map<String, dynamic> json) {
    return SongInfo(
      uuid: json['uuid'] ?? '',
      title: json['title'] ?? '',
      artist: json['artist'] ?? '',
      albumId: json['albumId'] ?? '',
      duration: (json['duration'] ?? 0.0).toDouble(),
      moduleId: json['moduleId'] ?? '',
      isFavorite: json['isFavorite'] ?? false,
      isExcluded: json['isExcluded'] ?? false,
    );
  }
}

class TrackInfo {
  final String uuid;
  final String title;
  final String artist;
  final String albumId;
  final double duration;
  final String moduleId;

  const TrackInfo({
    this.uuid = '',
    this.title = '',
    this.artist = '',
    this.albumId = '',
    this.duration = 0.0,
    this.moduleId = '',
  });

  factory TrackInfo.fromJson(Map<String, dynamic>? json) {
    if (json == null) return const TrackInfo();
    return TrackInfo(
      uuid: json['uuid'] ?? '',
      title: json['title'] ?? '',
      artist: json['artist'] ?? '',
      albumId: json['albumId'] ?? '',
      duration: (json['duration'] ?? 0.0).toDouble(),
      moduleId: json['moduleId'] ?? '',
    );
  }

  bool get isEmpty => uuid.isEmpty && title.isEmpty;
}

class PlaybackInstanceInfo {
  final String id;
  final String clientId;
  final String role;
  final String mode;
  final bool attached;
  final bool isPlaying;
  final double position;
  final double volume;
  final int queueCount;
  final int queueIndex;
  final int historyCount;
  final int sampleRate;
  final int channels;
  final bool shuffle;
  final String repeatMode;
  final TrackInfo? currentTrack;

  const PlaybackInstanceInfo({
    this.id = '',
    this.clientId = '',
    this.role = '',
    this.mode = '',
    this.attached = false,
    this.isPlaying = false,
    this.position = 0.0,
    this.volume = 1.0,
    this.queueCount = 0,
    this.queueIndex = -1,
    this.historyCount = 0,
    this.sampleRate = 0,
    this.channels = 0,
    this.shuffle = false,
    this.repeatMode = 'none',
    this.currentTrack,
  });

  factory PlaybackInstanceInfo.fromJson(Map<String, dynamic> json) {
    return PlaybackInstanceInfo(
      id: json['id'] ?? '',
      clientId: json['clientId'] ?? '',
      role: json['role'] ?? '',
      mode: json['mode'] ?? '',
      attached: json['attached'] ?? false,
      isPlaying: json['isPlaying'] ?? false,
      position: (json['position'] ?? 0.0).toDouble(),
      volume: (json['volume'] ?? 1.0).toDouble(),
      queueCount: json['queueCount'] ?? 0,
      queueIndex: json['queueIndex'] ?? -1,
      historyCount: json['historyCount'] ?? 0,
      sampleRate: json['sampleRate'] ?? 0,
      channels: json['channels'] ?? 0,
      shuffle: json['shuffle'] ?? false,
      repeatMode: (json['repeatMode'] ?? 'none').toString().toLowerCase(),
      currentTrack: json['currentTrack'] == null
          ? null
          : TrackInfo.fromJson(json['currentTrack'] as Map<String, dynamic>),
    );
  }

  bool get isServerManaged => mode.toLowerCase() == 'servermanaged';
  bool get isClientManaged => mode.toLowerCase() == 'clientmanaged';
}

class QueueItemInfo extends TrackInfo {
  final int index;

  const QueueItemInfo({
    this.index = 0,
    super.uuid,
    super.title,
    super.artist,
    super.albumId,
    super.duration,
    super.moduleId,
  });

  factory QueueItemInfo.fromJson(Map<String, dynamic> json) {
    return QueueItemInfo(
      index: json['index'] ?? 0,
      uuid: json['uuid'] ?? '',
      title: json['title'] ?? '',
      artist: json['artist'] ?? '',
      albumId: json['albumId'] ?? '',
      duration: (json['duration'] ?? 0.0).toDouble(),
      moduleId: json['moduleId'] ?? '',
    );
  }
}

class PlaylistSourceInfo {
  final String id;
  final String name;
  final int songCount;
  final List<String> uuids;

  const PlaylistSourceInfo({
    this.id = '',
    this.name = '',
    this.songCount = 0,
    this.uuids = const [],
  });

  factory PlaylistSourceInfo.fromJson(Map<String, dynamic> json) {
    return PlaylistSourceInfo(
      id: json['id'] ?? json['Id'] ?? '',
      name: json['name'] ?? json['Name'] ?? '',
      songCount: json['songCount'] ?? json['SongCount'] ?? 0,
      uuids:
          (json['uuids'] ?? json['SongUuids'] as List?)?.cast<String>() ?? [],
    );
  }
}

/// Full playlist response from GET /api/playlist
class PlaylistData {
  final List<TagInfo> tags;
  final List<AlbumInfo> albums;
  final List<SongInfo> songs;

  PlaylistData({
    this.tags = const [],
    this.albums = const [],
    this.songs = const [],
  });

  factory PlaylistData.fromJson(Map<String, dynamic> json) {
    return PlaylistData(
      tags:
          (json['tags'] as List<dynamic>?)
              ?.map((t) => TagInfo.fromJson(t as Map<String, dynamic>))
              .toList() ??
          [],
      albums:
          (json['albums'] as List<dynamic>?)
              ?.map((a) => AlbumInfo.fromJson(a as Map<String, dynamic>))
              .toList() ??
          [],
      songs:
          (json['songs'] as List<dynamic>?)
              ?.map((s) => SongInfo.fromJson(s as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}
