// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/common.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

/// 音乐源类型
class SourceType extends $pb.ProtobufEnum {
  static const SourceType SOURCE_TYPE_UNSPECIFIED =
      SourceType._(0, _omitEnumNames ? '' : 'SOURCE_TYPE_UNSPECIFIED');
  static const SourceType SOURCE_TYPE_FILE =
      SourceType._(1, _omitEnumNames ? '' : 'SOURCE_TYPE_FILE');
  static const SourceType SOURCE_TYPE_URL =
      SourceType._(2, _omitEnumNames ? '' : 'SOURCE_TYPE_URL');
  static const SourceType SOURCE_TYPE_STREAM =
      SourceType._(3, _omitEnumNames ? '' : 'SOURCE_TYPE_STREAM');

  static const $core.List<SourceType> values = <SourceType>[
    SOURCE_TYPE_UNSPECIFIED,
    SOURCE_TYPE_FILE,
    SOURCE_TYPE_URL,
    SOURCE_TYPE_STREAM,
  ];

  static final $core.List<SourceType?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 3);
  static SourceType? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const SourceType._(super.value, super.name);
}

/// 排序方向
class SortDirection extends $pb.ProtobufEnum {
  static const SortDirection SORT_DIRECTION_UNSPECIFIED =
      SortDirection._(0, _omitEnumNames ? '' : 'SORT_DIRECTION_UNSPECIFIED');
  static const SortDirection SORT_DIRECTION_ASC =
      SortDirection._(1, _omitEnumNames ? '' : 'SORT_DIRECTION_ASC');
  static const SortDirection SORT_DIRECTION_DESC =
      SortDirection._(2, _omitEnumNames ? '' : 'SORT_DIRECTION_DESC');

  static const $core.List<SortDirection> values = <SortDirection>[
    SORT_DIRECTION_UNSPECIFIED,
    SORT_DIRECTION_ASC,
    SORT_DIRECTION_DESC,
  ];

  static final $core.List<SortDirection?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 2);
  static SortDirection? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const SortDirection._(super.value, super.name);
}

/// Track 排序字段
class TrackSortField extends $pb.ProtobufEnum {
  static const TrackSortField TRACK_SORT_FIELD_UNSPECIFIED =
      TrackSortField._(0, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_UNSPECIFIED');
  static const TrackSortField TRACK_SORT_FIELD_TITLE =
      TrackSortField._(1, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_TITLE');
  static const TrackSortField TRACK_SORT_FIELD_ARTIST =
      TrackSortField._(2, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_ARTIST');
  static const TrackSortField TRACK_SORT_FIELD_DURATION =
      TrackSortField._(3, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_DURATION');
  static const TrackSortField TRACK_SORT_FIELD_PLAY_COUNT =
      TrackSortField._(4, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_PLAY_COUNT');
  static const TrackSortField TRACK_SORT_FIELD_LAST_PLAYED =
      TrackSortField._(5, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_LAST_PLAYED');
  static const TrackSortField TRACK_SORT_FIELD_CREATED_AT =
      TrackSortField._(6, _omitEnumNames ? '' : 'TRACK_SORT_FIELD_CREATED_AT');

  static const $core.List<TrackSortField> values = <TrackSortField>[
    TRACK_SORT_FIELD_UNSPECIFIED,
    TRACK_SORT_FIELD_TITLE,
    TRACK_SORT_FIELD_ARTIST,
    TRACK_SORT_FIELD_DURATION,
    TRACK_SORT_FIELD_PLAY_COUNT,
    TRACK_SORT_FIELD_LAST_PLAYED,
    TRACK_SORT_FIELD_CREATED_AT,
  ];

  static final $core.List<TrackSortField?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 6);
  static TrackSortField? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const TrackSortField._(super.value, super.name);
}

/// 重复模式
class RepeatMode extends $pb.ProtobufEnum {
  static const RepeatMode REPEAT_MODE_UNSPECIFIED =
      RepeatMode._(0, _omitEnumNames ? '' : 'REPEAT_MODE_UNSPECIFIED');
  static const RepeatMode REPEAT_MODE_NONE =
      RepeatMode._(1, _omitEnumNames ? '' : 'REPEAT_MODE_NONE');
  static const RepeatMode REPEAT_MODE_ONE =
      RepeatMode._(2, _omitEnumNames ? '' : 'REPEAT_MODE_ONE');
  static const RepeatMode REPEAT_MODE_ALL =
      RepeatMode._(3, _omitEnumNames ? '' : 'REPEAT_MODE_ALL');

  static const $core.List<RepeatMode> values = <RepeatMode>[
    REPEAT_MODE_UNSPECIFIED,
    REPEAT_MODE_NONE,
    REPEAT_MODE_ONE,
    REPEAT_MODE_ALL,
  ];

  static final $core.List<RepeatMode?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 3);
  static RepeatMode? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const RepeatMode._(super.value, super.name);
}

/// 均衡器滤波器类型
class EqualizerFilterType extends $pb.ProtobufEnum {
  static const EqualizerFilterType EQ_FILTER_TYPE_UNSPECIFIED =
      EqualizerFilterType._(
          0, _omitEnumNames ? '' : 'EQ_FILTER_TYPE_UNSPECIFIED');
  static const EqualizerFilterType EQ_FILTER_TYPE_PEAKING =
      EqualizerFilterType._(1, _omitEnumNames ? '' : 'EQ_FILTER_TYPE_PEAKING');
  static const EqualizerFilterType EQ_FILTER_TYPE_LOW_SHELF =
      EqualizerFilterType._(
          2, _omitEnumNames ? '' : 'EQ_FILTER_TYPE_LOW_SHELF');
  static const EqualizerFilterType EQ_FILTER_TYPE_HIGH_SHELF =
      EqualizerFilterType._(
          3, _omitEnumNames ? '' : 'EQ_FILTER_TYPE_HIGH_SHELF');
  static const EqualizerFilterType EQ_FILTER_TYPE_LOW_PASS =
      EqualizerFilterType._(4, _omitEnumNames ? '' : 'EQ_FILTER_TYPE_LOW_PASS');
  static const EqualizerFilterType EQ_FILTER_TYPE_HIGH_PASS =
      EqualizerFilterType._(
          5, _omitEnumNames ? '' : 'EQ_FILTER_TYPE_HIGH_PASS');

  static const $core.List<EqualizerFilterType> values = <EqualizerFilterType>[
    EQ_FILTER_TYPE_UNSPECIFIED,
    EQ_FILTER_TYPE_PEAKING,
    EQ_FILTER_TYPE_LOW_SHELF,
    EQ_FILTER_TYPE_HIGH_SHELF,
    EQ_FILTER_TYPE_LOW_PASS,
    EQ_FILTER_TYPE_HIGH_PASS,
  ];

  static final $core.List<EqualizerFilterType?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 5);
  static EqualizerFilterType? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const EqualizerFilterType._(super.value, super.name);
}

/// 实例类型
class InstanceKind extends $pb.ProtobufEnum {
  static const InstanceKind INSTANCE_KIND_UNSPECIFIED =
      InstanceKind._(0, _omitEnumNames ? '' : 'INSTANCE_KIND_UNSPECIFIED');
  static const InstanceKind INSTANCE_KIND_GAME_MOD =
      InstanceKind._(1, _omitEnumNames ? '' : 'INSTANCE_KIND_GAME_MOD');
  static const InstanceKind INSTANCE_KIND_GUI =
      InstanceKind._(2, _omitEnumNames ? '' : 'INSTANCE_KIND_GUI');
  static const InstanceKind INSTANCE_KIND_EXTERNAL_CLIENT =
      InstanceKind._(3, _omitEnumNames ? '' : 'INSTANCE_KIND_EXTERNAL_CLIENT');
  static const InstanceKind INSTANCE_KIND_OBSERVER =
      InstanceKind._(4, _omitEnumNames ? '' : 'INSTANCE_KIND_OBSERVER');

  static const $core.List<InstanceKind> values = <InstanceKind>[
    INSTANCE_KIND_UNSPECIFIED,
    INSTANCE_KIND_GAME_MOD,
    INSTANCE_KIND_GUI,
    INSTANCE_KIND_EXTERNAL_CLIENT,
    INSTANCE_KIND_OBSERVER,
  ];

  static final $core.List<InstanceKind?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 4);
  static InstanceKind? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const InstanceKind._(super.value, super.name);
}

/// 客户端角色
class ClientRole extends $pb.ProtobufEnum {
  static const ClientRole CLIENT_ROLE_UNSPECIFIED =
      ClientRole._(0, _omitEnumNames ? '' : 'CLIENT_ROLE_UNSPECIFIED');
  static const ClientRole CLIENT_ROLE_AUDIO =
      ClientRole._(1, _omitEnumNames ? '' : 'CLIENT_ROLE_AUDIO');
  static const ClientRole CLIENT_ROLE_CONTROLLER =
      ClientRole._(2, _omitEnumNames ? '' : 'CLIENT_ROLE_CONTROLLER');
  static const ClientRole CLIENT_ROLE_OBSERVER =
      ClientRole._(3, _omitEnumNames ? '' : 'CLIENT_ROLE_OBSERVER');

  static const $core.List<ClientRole> values = <ClientRole>[
    CLIENT_ROLE_UNSPECIFIED,
    CLIENT_ROLE_AUDIO,
    CLIENT_ROLE_CONTROLLER,
    CLIENT_ROLE_OBSERVER,
  ];

  static final $core.List<ClientRole?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 3);
  static ClientRole? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const ClientRole._(super.value, super.name);
}

/// Tag 种类
class TagKind extends $pb.ProtobufEnum {
  static const TagKind TAG_KIND_UNSPECIFIED =
      TagKind._(0, _omitEnumNames ? '' : 'TAG_KIND_UNSPECIFIED');
  static const TagKind TAG_KIND_NORMAL =
      TagKind._(1, _omitEnumNames ? '' : 'TAG_KIND_NORMAL');
  static const TagKind TAG_KIND_GROWABLE =
      TagKind._(2, _omitEnumNames ? '' : 'TAG_KIND_GROWABLE');
  static const TagKind TAG_KIND_SYSTEM =
      TagKind._(3, _omitEnumNames ? '' : 'TAG_KIND_SYSTEM');

  static const $core.List<TagKind> values = <TagKind>[
    TAG_KIND_UNSPECIFIED,
    TAG_KIND_NORMAL,
    TAG_KIND_GROWABLE,
    TAG_KIND_SYSTEM,
  ];

  static final $core.List<TagKind?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 3);
  static TagKind? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const TagKind._(super.value, super.name);
}

/// Playlist 种类
class PlaylistKind extends $pb.ProtobufEnum {
  static const PlaylistKind PLAYLIST_KIND_UNSPECIFIED =
      PlaylistKind._(0, _omitEnumNames ? '' : 'PLAYLIST_KIND_UNSPECIFIED');
  static const PlaylistKind PLAYLIST_KIND_USER =
      PlaylistKind._(1, _omitEnumNames ? '' : 'PLAYLIST_KIND_USER');
  static const PlaylistKind PLAYLIST_KIND_SYSTEM =
      PlaylistKind._(2, _omitEnumNames ? '' : 'PLAYLIST_KIND_SYSTEM');
  static const PlaylistKind PLAYLIST_KIND_IMPORTED =
      PlaylistKind._(3, _omitEnumNames ? '' : 'PLAYLIST_KIND_IMPORTED');

  static const $core.List<PlaylistKind> values = <PlaylistKind>[
    PLAYLIST_KIND_UNSPECIFIED,
    PLAYLIST_KIND_USER,
    PLAYLIST_KIND_SYSTEM,
    PLAYLIST_KIND_IMPORTED,
  ];

  static final $core.List<PlaylistKind?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 3);
  static PlaylistKind? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const PlaylistKind._(super.value, super.name);
}

const $core.bool _omitEnumNames =
    $core.bool.fromEnvironment('protobuf.omit_enum_names');
