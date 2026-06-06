// This is a generated file - do not edit.
//
// Generated from omni_mix_player/events/ws_events.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:fixnum/fixnum.dart' as $fixnum;
import 'package:protobuf/protobuf.dart' as $pb;

import '../models/instance.pb.dart' as $0;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

enum WsEvent_Event {
  trackChanged,
  stateChanged,
  positionChanged,
  queueChanged,
  instancesChanged,
  favoriteChanged,
  excludeChanged,
  playlistUpdated,
  moduleChanged,
  profileChanged,
  backendState,
  volumeChanged,
  latencyChanged,
  eqChanged,
  notSet
}

class WsEvent extends $pb.GeneratedMessage {
  factory WsEvent({
    $core.String? type,
    $fixnum.Int64? timestamp,
    TrackChangedEvent? trackChanged,
    StateChangedEvent? stateChanged,
    PositionChangedEvent? positionChanged,
    QueueChangedEvent? queueChanged,
    InstancesChangedEvent? instancesChanged,
    FavoriteChangedEvent? favoriteChanged,
    ExcludeChangedEvent? excludeChanged,
    PlaylistUpdatedEvent? playlistUpdated,
    ModuleChangedEvent? moduleChanged,
    ProfileChangedEvent? profileChanged,
    BackendStateEvent? backendState,
    VolumeChangedEvent? volumeChanged,
    LatencyChangedEvent? latencyChanged,
    EqualizerChangedEvent? eqChanged,
  }) {
    final result = create();
    if (type != null) result.type = type;
    if (timestamp != null) result.timestamp = timestamp;
    if (trackChanged != null) result.trackChanged = trackChanged;
    if (stateChanged != null) result.stateChanged = stateChanged;
    if (positionChanged != null) result.positionChanged = positionChanged;
    if (queueChanged != null) result.queueChanged = queueChanged;
    if (instancesChanged != null) result.instancesChanged = instancesChanged;
    if (favoriteChanged != null) result.favoriteChanged = favoriteChanged;
    if (excludeChanged != null) result.excludeChanged = excludeChanged;
    if (playlistUpdated != null) result.playlistUpdated = playlistUpdated;
    if (moduleChanged != null) result.moduleChanged = moduleChanged;
    if (profileChanged != null) result.profileChanged = profileChanged;
    if (backendState != null) result.backendState = backendState;
    if (volumeChanged != null) result.volumeChanged = volumeChanged;
    if (latencyChanged != null) result.latencyChanged = latencyChanged;
    if (eqChanged != null) result.eqChanged = eqChanged;
    return result;
  }

  WsEvent._();

  factory WsEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory WsEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static const $core.Map<$core.int, WsEvent_Event> _WsEvent_EventByTag = {
    10: WsEvent_Event.trackChanged,
    11: WsEvent_Event.stateChanged,
    12: WsEvent_Event.positionChanged,
    13: WsEvent_Event.queueChanged,
    14: WsEvent_Event.instancesChanged,
    15: WsEvent_Event.favoriteChanged,
    16: WsEvent_Event.excludeChanged,
    17: WsEvent_Event.playlistUpdated,
    18: WsEvent_Event.moduleChanged,
    19: WsEvent_Event.profileChanged,
    20: WsEvent_Event.backendState,
    21: WsEvent_Event.volumeChanged,
    22: WsEvent_Event.latencyChanged,
    23: WsEvent_Event.eqChanged,
    0: WsEvent_Event.notSet
  };
  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'WsEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..oo(0, [10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23])
    ..aOS(1, _omitFieldNames ? '' : 'type')
    ..aInt64(2, _omitFieldNames ? '' : 'timestamp')
    ..aOM<TrackChangedEvent>(10, _omitFieldNames ? '' : 'trackChanged',
        subBuilder: TrackChangedEvent.create)
    ..aOM<StateChangedEvent>(11, _omitFieldNames ? '' : 'stateChanged',
        subBuilder: StateChangedEvent.create)
    ..aOM<PositionChangedEvent>(12, _omitFieldNames ? '' : 'positionChanged',
        subBuilder: PositionChangedEvent.create)
    ..aOM<QueueChangedEvent>(13, _omitFieldNames ? '' : 'queueChanged',
        subBuilder: QueueChangedEvent.create)
    ..aOM<InstancesChangedEvent>(14, _omitFieldNames ? '' : 'instancesChanged',
        subBuilder: InstancesChangedEvent.create)
    ..aOM<FavoriteChangedEvent>(15, _omitFieldNames ? '' : 'favoriteChanged',
        subBuilder: FavoriteChangedEvent.create)
    ..aOM<ExcludeChangedEvent>(16, _omitFieldNames ? '' : 'excludeChanged',
        subBuilder: ExcludeChangedEvent.create)
    ..aOM<PlaylistUpdatedEvent>(17, _omitFieldNames ? '' : 'playlistUpdated',
        subBuilder: PlaylistUpdatedEvent.create)
    ..aOM<ModuleChangedEvent>(18, _omitFieldNames ? '' : 'moduleChanged',
        subBuilder: ModuleChangedEvent.create)
    ..aOM<ProfileChangedEvent>(19, _omitFieldNames ? '' : 'profileChanged',
        subBuilder: ProfileChangedEvent.create)
    ..aOM<BackendStateEvent>(20, _omitFieldNames ? '' : 'backendState',
        subBuilder: BackendStateEvent.create)
    ..aOM<VolumeChangedEvent>(21, _omitFieldNames ? '' : 'volumeChanged',
        subBuilder: VolumeChangedEvent.create)
    ..aOM<LatencyChangedEvent>(22, _omitFieldNames ? '' : 'latencyChanged',
        subBuilder: LatencyChangedEvent.create)
    ..aOM<EqualizerChangedEvent>(23, _omitFieldNames ? '' : 'eqChanged',
        subBuilder: EqualizerChangedEvent.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  WsEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  WsEvent copyWith(void Function(WsEvent) updates) =>
      super.copyWith((message) => updates(message as WsEvent)) as WsEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static WsEvent create() => WsEvent._();
  @$core.override
  WsEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static WsEvent getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<WsEvent>(create);
  static WsEvent? _defaultInstance;

  @$pb.TagNumber(10)
  @$pb.TagNumber(11)
  @$pb.TagNumber(12)
  @$pb.TagNumber(13)
  @$pb.TagNumber(14)
  @$pb.TagNumber(15)
  @$pb.TagNumber(16)
  @$pb.TagNumber(17)
  @$pb.TagNumber(18)
  @$pb.TagNumber(19)
  @$pb.TagNumber(20)
  @$pb.TagNumber(21)
  @$pb.TagNumber(22)
  @$pb.TagNumber(23)
  WsEvent_Event whichEvent() => _WsEvent_EventByTag[$_whichOneof(0)]!;
  @$pb.TagNumber(10)
  @$pb.TagNumber(11)
  @$pb.TagNumber(12)
  @$pb.TagNumber(13)
  @$pb.TagNumber(14)
  @$pb.TagNumber(15)
  @$pb.TagNumber(16)
  @$pb.TagNumber(17)
  @$pb.TagNumber(18)
  @$pb.TagNumber(19)
  @$pb.TagNumber(20)
  @$pb.TagNumber(21)
  @$pb.TagNumber(22)
  @$pb.TagNumber(23)
  void clearEvent() => $_clearField($_whichOneof(0));

  @$pb.TagNumber(1)
  $core.String get type => $_getSZ(0);
  @$pb.TagNumber(1)
  set type($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasType() => $_has(0);
  @$pb.TagNumber(1)
  void clearType() => $_clearField(1);

  @$pb.TagNumber(2)
  $fixnum.Int64 get timestamp => $_getI64(1);
  @$pb.TagNumber(2)
  set timestamp($fixnum.Int64 value) => $_setInt64(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTimestamp() => $_has(1);
  @$pb.TagNumber(2)
  void clearTimestamp() => $_clearField(2);

  @$pb.TagNumber(10)
  TrackChangedEvent get trackChanged => $_getN(2);
  @$pb.TagNumber(10)
  set trackChanged(TrackChangedEvent value) => $_setField(10, value);
  @$pb.TagNumber(10)
  $core.bool hasTrackChanged() => $_has(2);
  @$pb.TagNumber(10)
  void clearTrackChanged() => $_clearField(10);
  @$pb.TagNumber(10)
  TrackChangedEvent ensureTrackChanged() => $_ensure(2);

  @$pb.TagNumber(11)
  StateChangedEvent get stateChanged => $_getN(3);
  @$pb.TagNumber(11)
  set stateChanged(StateChangedEvent value) => $_setField(11, value);
  @$pb.TagNumber(11)
  $core.bool hasStateChanged() => $_has(3);
  @$pb.TagNumber(11)
  void clearStateChanged() => $_clearField(11);
  @$pb.TagNumber(11)
  StateChangedEvent ensureStateChanged() => $_ensure(3);

  @$pb.TagNumber(12)
  PositionChangedEvent get positionChanged => $_getN(4);
  @$pb.TagNumber(12)
  set positionChanged(PositionChangedEvent value) => $_setField(12, value);
  @$pb.TagNumber(12)
  $core.bool hasPositionChanged() => $_has(4);
  @$pb.TagNumber(12)
  void clearPositionChanged() => $_clearField(12);
  @$pb.TagNumber(12)
  PositionChangedEvent ensurePositionChanged() => $_ensure(4);

  @$pb.TagNumber(13)
  QueueChangedEvent get queueChanged => $_getN(5);
  @$pb.TagNumber(13)
  set queueChanged(QueueChangedEvent value) => $_setField(13, value);
  @$pb.TagNumber(13)
  $core.bool hasQueueChanged() => $_has(5);
  @$pb.TagNumber(13)
  void clearQueueChanged() => $_clearField(13);
  @$pb.TagNumber(13)
  QueueChangedEvent ensureQueueChanged() => $_ensure(5);

  @$pb.TagNumber(14)
  InstancesChangedEvent get instancesChanged => $_getN(6);
  @$pb.TagNumber(14)
  set instancesChanged(InstancesChangedEvent value) => $_setField(14, value);
  @$pb.TagNumber(14)
  $core.bool hasInstancesChanged() => $_has(6);
  @$pb.TagNumber(14)
  void clearInstancesChanged() => $_clearField(14);
  @$pb.TagNumber(14)
  InstancesChangedEvent ensureInstancesChanged() => $_ensure(6);

  @$pb.TagNumber(15)
  FavoriteChangedEvent get favoriteChanged => $_getN(7);
  @$pb.TagNumber(15)
  set favoriteChanged(FavoriteChangedEvent value) => $_setField(15, value);
  @$pb.TagNumber(15)
  $core.bool hasFavoriteChanged() => $_has(7);
  @$pb.TagNumber(15)
  void clearFavoriteChanged() => $_clearField(15);
  @$pb.TagNumber(15)
  FavoriteChangedEvent ensureFavoriteChanged() => $_ensure(7);

  @$pb.TagNumber(16)
  ExcludeChangedEvent get excludeChanged => $_getN(8);
  @$pb.TagNumber(16)
  set excludeChanged(ExcludeChangedEvent value) => $_setField(16, value);
  @$pb.TagNumber(16)
  $core.bool hasExcludeChanged() => $_has(8);
  @$pb.TagNumber(16)
  void clearExcludeChanged() => $_clearField(16);
  @$pb.TagNumber(16)
  ExcludeChangedEvent ensureExcludeChanged() => $_ensure(8);

  @$pb.TagNumber(17)
  PlaylistUpdatedEvent get playlistUpdated => $_getN(9);
  @$pb.TagNumber(17)
  set playlistUpdated(PlaylistUpdatedEvent value) => $_setField(17, value);
  @$pb.TagNumber(17)
  $core.bool hasPlaylistUpdated() => $_has(9);
  @$pb.TagNumber(17)
  void clearPlaylistUpdated() => $_clearField(17);
  @$pb.TagNumber(17)
  PlaylistUpdatedEvent ensurePlaylistUpdated() => $_ensure(9);

  @$pb.TagNumber(18)
  ModuleChangedEvent get moduleChanged => $_getN(10);
  @$pb.TagNumber(18)
  set moduleChanged(ModuleChangedEvent value) => $_setField(18, value);
  @$pb.TagNumber(18)
  $core.bool hasModuleChanged() => $_has(10);
  @$pb.TagNumber(18)
  void clearModuleChanged() => $_clearField(18);
  @$pb.TagNumber(18)
  ModuleChangedEvent ensureModuleChanged() => $_ensure(10);

  @$pb.TagNumber(19)
  ProfileChangedEvent get profileChanged => $_getN(11);
  @$pb.TagNumber(19)
  set profileChanged(ProfileChangedEvent value) => $_setField(19, value);
  @$pb.TagNumber(19)
  $core.bool hasProfileChanged() => $_has(11);
  @$pb.TagNumber(19)
  void clearProfileChanged() => $_clearField(19);
  @$pb.TagNumber(19)
  ProfileChangedEvent ensureProfileChanged() => $_ensure(11);

  @$pb.TagNumber(20)
  BackendStateEvent get backendState => $_getN(12);
  @$pb.TagNumber(20)
  set backendState(BackendStateEvent value) => $_setField(20, value);
  @$pb.TagNumber(20)
  $core.bool hasBackendState() => $_has(12);
  @$pb.TagNumber(20)
  void clearBackendState() => $_clearField(20);
  @$pb.TagNumber(20)
  BackendStateEvent ensureBackendState() => $_ensure(12);

  @$pb.TagNumber(21)
  VolumeChangedEvent get volumeChanged => $_getN(13);
  @$pb.TagNumber(21)
  set volumeChanged(VolumeChangedEvent value) => $_setField(21, value);
  @$pb.TagNumber(21)
  $core.bool hasVolumeChanged() => $_has(13);
  @$pb.TagNumber(21)
  void clearVolumeChanged() => $_clearField(21);
  @$pb.TagNumber(21)
  VolumeChangedEvent ensureVolumeChanged() => $_ensure(13);

  @$pb.TagNumber(22)
  LatencyChangedEvent get latencyChanged => $_getN(14);
  @$pb.TagNumber(22)
  set latencyChanged(LatencyChangedEvent value) => $_setField(22, value);
  @$pb.TagNumber(22)
  $core.bool hasLatencyChanged() => $_has(14);
  @$pb.TagNumber(22)
  void clearLatencyChanged() => $_clearField(22);
  @$pb.TagNumber(22)
  LatencyChangedEvent ensureLatencyChanged() => $_ensure(14);

  @$pb.TagNumber(23)
  EqualizerChangedEvent get eqChanged => $_getN(15);
  @$pb.TagNumber(23)
  set eqChanged(EqualizerChangedEvent value) => $_setField(23, value);
  @$pb.TagNumber(23)
  $core.bool hasEqChanged() => $_has(15);
  @$pb.TagNumber(23)
  void clearEqChanged() => $_clearField(23);
  @$pb.TagNumber(23)
  EqualizerChangedEvent ensureEqChanged() => $_ensure(15);
}

/// 歌曲切换
class TrackChangedEvent extends $pb.GeneratedMessage {
  factory TrackChangedEvent({
    $core.String? instanceId,
    $core.String? uuid,
    $core.String? title,
    $core.String? artist,
    $core.String? albumId,
    $core.double? duration,
    $core.String? moduleId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (uuid != null) result.uuid = uuid;
    if (title != null) result.title = title;
    if (artist != null) result.artist = artist;
    if (albumId != null) result.albumId = albumId;
    if (duration != null) result.duration = duration;
    if (moduleId != null) result.moduleId = moduleId;
    return result;
  }

  TrackChangedEvent._();

  factory TrackChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory TrackChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'TrackChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOS(2, _omitFieldNames ? '' : 'uuid')
    ..aOS(3, _omitFieldNames ? '' : 'title')
    ..aOS(4, _omitFieldNames ? '' : 'artist')
    ..aOS(5, _omitFieldNames ? '' : 'albumId')
    ..aD(6, _omitFieldNames ? '' : 'duration', fieldType: $pb.PbFieldType.OF)
    ..aOS(7, _omitFieldNames ? '' : 'moduleId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackChangedEvent copyWith(void Function(TrackChangedEvent) updates) =>
      super.copyWith((message) => updates(message as TrackChangedEvent))
          as TrackChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static TrackChangedEvent create() => TrackChangedEvent._();
  @$core.override
  TrackChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static TrackChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<TrackChangedEvent>(create);
  static TrackChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get uuid => $_getSZ(1);
  @$pb.TagNumber(2)
  set uuid($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasUuid() => $_has(1);
  @$pb.TagNumber(2)
  void clearUuid() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get title => $_getSZ(2);
  @$pb.TagNumber(3)
  set title($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasTitle() => $_has(2);
  @$pb.TagNumber(3)
  void clearTitle() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.String get artist => $_getSZ(3);
  @$pb.TagNumber(4)
  set artist($core.String value) => $_setString(3, value);
  @$pb.TagNumber(4)
  $core.bool hasArtist() => $_has(3);
  @$pb.TagNumber(4)
  void clearArtist() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.String get albumId => $_getSZ(4);
  @$pb.TagNumber(5)
  set albumId($core.String value) => $_setString(4, value);
  @$pb.TagNumber(5)
  $core.bool hasAlbumId() => $_has(4);
  @$pb.TagNumber(5)
  void clearAlbumId() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.double get duration => $_getN(5);
  @$pb.TagNumber(6)
  set duration($core.double value) => $_setFloat(5, value);
  @$pb.TagNumber(6)
  $core.bool hasDuration() => $_has(5);
  @$pb.TagNumber(6)
  void clearDuration() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.String get moduleId => $_getSZ(6);
  @$pb.TagNumber(7)
  set moduleId($core.String value) => $_setString(6, value);
  @$pb.TagNumber(7)
  $core.bool hasModuleId() => $_has(6);
  @$pb.TagNumber(7)
  void clearModuleId() => $_clearField(7);
}

/// 播放状态变化
class StateChangedEvent extends $pb.GeneratedMessage {
  factory StateChangedEvent({
    $core.String? instanceId,
    $core.int? state,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (state != null) result.state = state;
    return result;
  }

  StateChangedEvent._();

  factory StateChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory StateChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'StateChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aI(2, _omitFieldNames ? '' : 'state')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  StateChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  StateChangedEvent copyWith(void Function(StateChangedEvent) updates) =>
      super.copyWith((message) => updates(message as StateChangedEvent))
          as StateChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static StateChangedEvent create() => StateChangedEvent._();
  @$core.override
  StateChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static StateChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<StateChangedEvent>(create);
  static StateChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get state => $_getIZ(1);
  @$pb.TagNumber(2)
  set state($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasState() => $_has(1);
  @$pb.TagNumber(2)
  void clearState() => $_clearField(2);
}

/// 进度变化
class PositionChangedEvent extends $pb.GeneratedMessage {
  factory PositionChangedEvent({
    $core.String? instanceId,
    $core.double? position,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (position != null) result.position = position;
    return result;
  }

  PositionChangedEvent._();

  factory PositionChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PositionChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PositionChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aD(2, _omitFieldNames ? '' : 'position', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PositionChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PositionChangedEvent copyWith(void Function(PositionChangedEvent) updates) =>
      super.copyWith((message) => updates(message as PositionChangedEvent))
          as PositionChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PositionChangedEvent create() => PositionChangedEvent._();
  @$core.override
  PositionChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PositionChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PositionChangedEvent>(create);
  static PositionChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.double get position => $_getN(1);
  @$pb.TagNumber(2)
  set position($core.double value) => $_setFloat(1, value);
  @$pb.TagNumber(2)
  $core.bool hasPosition() => $_has(1);
  @$pb.TagNumber(2)
  void clearPosition() => $_clearField(2);
}

/// 队列变化
class QueueChangedEvent extends $pb.GeneratedMessage {
  factory QueueChangedEvent({
    $core.String? instanceId,
    $core.String? changeType,
    $core.int? queueLength,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (changeType != null) result.changeType = changeType;
    if (queueLength != null) result.queueLength = queueLength;
    return result;
  }

  QueueChangedEvent._();

  factory QueueChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueueChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueueChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOS(2, _omitFieldNames ? '' : 'changeType')
    ..aI(3, _omitFieldNames ? '' : 'queueLength')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueueChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueueChangedEvent copyWith(void Function(QueueChangedEvent) updates) =>
      super.copyWith((message) => updates(message as QueueChangedEvent))
          as QueueChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueueChangedEvent create() => QueueChangedEvent._();
  @$core.override
  QueueChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueueChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<QueueChangedEvent>(create);
  static QueueChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get changeType => $_getSZ(1);
  @$pb.TagNumber(2)
  set changeType($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasChangeType() => $_has(1);
  @$pb.TagNumber(2)
  void clearChangeType() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.int get queueLength => $_getIZ(2);
  @$pb.TagNumber(3)
  set queueLength($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasQueueLength() => $_has(2);
  @$pb.TagNumber(3)
  void clearQueueLength() => $_clearField(3);
}

/// 实例列表变化
class InstancesChangedEvent extends $pb.GeneratedMessage {
  factory InstancesChangedEvent({
    $core.Iterable<$0.InstanceSummary>? instances,
  }) {
    final result = create();
    if (instances != null) result.instances.addAll(instances);
    return result;
  }

  InstancesChangedEvent._();

  factory InstancesChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstancesChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstancesChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<$0.InstanceSummary>(1, _omitFieldNames ? '' : 'instances',
        subBuilder: $0.InstanceSummary.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstancesChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstancesChangedEvent copyWith(
          void Function(InstancesChangedEvent) updates) =>
      super.copyWith((message) => updates(message as InstancesChangedEvent))
          as InstancesChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstancesChangedEvent create() => InstancesChangedEvent._();
  @$core.override
  InstancesChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstancesChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstancesChangedEvent>(create);
  static InstancesChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<$0.InstanceSummary> get instances => $_getList(0);
}

/// 收藏变化
class FavoriteChangedEvent extends $pb.GeneratedMessage {
  factory FavoriteChangedEvent({
    $core.String? uuid,
    $core.bool? isFavorite,
    $core.String? moduleId,
  }) {
    final result = create();
    if (uuid != null) result.uuid = uuid;
    if (isFavorite != null) result.isFavorite = isFavorite;
    if (moduleId != null) result.moduleId = moduleId;
    return result;
  }

  FavoriteChangedEvent._();

  factory FavoriteChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory FavoriteChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'FavoriteChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'uuid')
    ..aOB(2, _omitFieldNames ? '' : 'isFavorite')
    ..aOS(3, _omitFieldNames ? '' : 'moduleId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  FavoriteChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  FavoriteChangedEvent copyWith(void Function(FavoriteChangedEvent) updates) =>
      super.copyWith((message) => updates(message as FavoriteChangedEvent))
          as FavoriteChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static FavoriteChangedEvent create() => FavoriteChangedEvent._();
  @$core.override
  FavoriteChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static FavoriteChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<FavoriteChangedEvent>(create);
  static FavoriteChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get uuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set uuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.bool get isFavorite => $_getBF(1);
  @$pb.TagNumber(2)
  set isFavorite($core.bool value) => $_setBool(1, value);
  @$pb.TagNumber(2)
  $core.bool hasIsFavorite() => $_has(1);
  @$pb.TagNumber(2)
  void clearIsFavorite() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get moduleId => $_getSZ(2);
  @$pb.TagNumber(3)
  set moduleId($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasModuleId() => $_has(2);
  @$pb.TagNumber(3)
  void clearModuleId() => $_clearField(3);
}

/// 排除变化
class ExcludeChangedEvent extends $pb.GeneratedMessage {
  factory ExcludeChangedEvent({
    $core.String? uuid,
    $core.bool? isExcluded,
    $core.String? moduleId,
  }) {
    final result = create();
    if (uuid != null) result.uuid = uuid;
    if (isExcluded != null) result.isExcluded = isExcluded;
    if (moduleId != null) result.moduleId = moduleId;
    return result;
  }

  ExcludeChangedEvent._();

  factory ExcludeChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ExcludeChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ExcludeChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'uuid')
    ..aOB(2, _omitFieldNames ? '' : 'isExcluded')
    ..aOS(3, _omitFieldNames ? '' : 'moduleId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ExcludeChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ExcludeChangedEvent copyWith(void Function(ExcludeChangedEvent) updates) =>
      super.copyWith((message) => updates(message as ExcludeChangedEvent))
          as ExcludeChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ExcludeChangedEvent create() => ExcludeChangedEvent._();
  @$core.override
  ExcludeChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ExcludeChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ExcludeChangedEvent>(create);
  static ExcludeChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get uuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set uuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.bool get isExcluded => $_getBF(1);
  @$pb.TagNumber(2)
  set isExcluded($core.bool value) => $_setBool(1, value);
  @$pb.TagNumber(2)
  $core.bool hasIsExcluded() => $_has(1);
  @$pb.TagNumber(2)
  void clearIsExcluded() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get moduleId => $_getSZ(2);
  @$pb.TagNumber(3)
  set moduleId($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasModuleId() => $_has(2);
  @$pb.TagNumber(3)
  void clearModuleId() => $_clearField(3);
}

/// 歌单更新
class PlaylistUpdatedEvent extends $pb.GeneratedMessage {
  factory PlaylistUpdatedEvent({
    $core.String? sourceRefId,
    $core.int? songCount,
    $core.String? updateType,
  }) {
    final result = create();
    if (sourceRefId != null) result.sourceRefId = sourceRefId;
    if (songCount != null) result.songCount = songCount;
    if (updateType != null) result.updateType = updateType;
    return result;
  }

  PlaylistUpdatedEvent._();

  factory PlaylistUpdatedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistUpdatedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistUpdatedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'sourceRefId')
    ..aI(2, _omitFieldNames ? '' : 'songCount')
    ..aOS(3, _omitFieldNames ? '' : 'updateType')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistUpdatedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistUpdatedEvent copyWith(void Function(PlaylistUpdatedEvent) updates) =>
      super.copyWith((message) => updates(message as PlaylistUpdatedEvent))
          as PlaylistUpdatedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistUpdatedEvent create() => PlaylistUpdatedEvent._();
  @$core.override
  PlaylistUpdatedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistUpdatedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistUpdatedEvent>(create);
  static PlaylistUpdatedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get sourceRefId => $_getSZ(0);
  @$pb.TagNumber(1)
  set sourceRefId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSourceRefId() => $_has(0);
  @$pb.TagNumber(1)
  void clearSourceRefId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get songCount => $_getIZ(1);
  @$pb.TagNumber(2)
  set songCount($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasSongCount() => $_has(1);
  @$pb.TagNumber(2)
  void clearSongCount() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get updateType => $_getSZ(2);
  @$pb.TagNumber(3)
  set updateType($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasUpdateType() => $_has(2);
  @$pb.TagNumber(3)
  void clearUpdateType() => $_clearField(3);
}

/// 模块状态变化
class ModuleChangedEvent extends $pb.GeneratedMessage {
  factory ModuleChangedEvent({
    $core.String? moduleId,
    $core.bool? enabled,
    $core.String? displayName,
  }) {
    final result = create();
    if (moduleId != null) result.moduleId = moduleId;
    if (enabled != null) result.enabled = enabled;
    if (displayName != null) result.displayName = displayName;
    return result;
  }

  ModuleChangedEvent._();

  factory ModuleChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ModuleChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ModuleChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'moduleId')
    ..aOB(2, _omitFieldNames ? '' : 'enabled')
    ..aOS(3, _omitFieldNames ? '' : 'displayName')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ModuleChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ModuleChangedEvent copyWith(void Function(ModuleChangedEvent) updates) =>
      super.copyWith((message) => updates(message as ModuleChangedEvent))
          as ModuleChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ModuleChangedEvent create() => ModuleChangedEvent._();
  @$core.override
  ModuleChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ModuleChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ModuleChangedEvent>(create);
  static ModuleChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get moduleId => $_getSZ(0);
  @$pb.TagNumber(1)
  set moduleId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasModuleId() => $_has(0);
  @$pb.TagNumber(1)
  void clearModuleId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.bool get enabled => $_getBF(1);
  @$pb.TagNumber(2)
  set enabled($core.bool value) => $_setBool(1, value);
  @$pb.TagNumber(2)
  $core.bool hasEnabled() => $_has(1);
  @$pb.TagNumber(2)
  void clearEnabled() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get displayName => $_getSZ(2);
  @$pb.TagNumber(3)
  set displayName($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasDisplayName() => $_has(2);
  @$pb.TagNumber(3)
  void clearDisplayName() => $_clearField(3);
}

/// Profile 变化
class ProfileChangedEvent extends $pb.GeneratedMessage {
  factory ProfileChangedEvent({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  ProfileChangedEvent._();

  factory ProfileChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ProfileChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ProfileChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ProfileChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ProfileChangedEvent copyWith(void Function(ProfileChangedEvent) updates) =>
      super.copyWith((message) => updates(message as ProfileChangedEvent))
          as ProfileChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ProfileChangedEvent create() => ProfileChangedEvent._();
  @$core.override
  ProfileChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ProfileChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ProfileChangedEvent>(create);
  static ProfileChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

/// 后端状态
class BackendStateEvent extends $pb.GeneratedMessage {
  factory BackendStateEvent({
    $core.bool? running,
  }) {
    final result = create();
    if (running != null) result.running = running;
    return result;
  }

  BackendStateEvent._();

  factory BackendStateEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory BackendStateEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'BackendStateEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'running')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  BackendStateEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  BackendStateEvent copyWith(void Function(BackendStateEvent) updates) =>
      super.copyWith((message) => updates(message as BackendStateEvent))
          as BackendStateEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static BackendStateEvent create() => BackendStateEvent._();
  @$core.override
  BackendStateEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static BackendStateEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<BackendStateEvent>(create);
  static BackendStateEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get running => $_getBF(0);
  @$pb.TagNumber(1)
  set running($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasRunning() => $_has(0);
  @$pb.TagNumber(1)
  void clearRunning() => $_clearField(1);
}

/// 音量变化（轻量推送，不触发全量刷新）
class VolumeChangedEvent extends $pb.GeneratedMessage {
  factory VolumeChangedEvent({
    $core.String? instanceId,
    $core.double? volume,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (volume != null) result.volume = volume;
    return result;
  }

  VolumeChangedEvent._();

  factory VolumeChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory VolumeChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'VolumeChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aD(2, _omitFieldNames ? '' : 'volume', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  VolumeChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  VolumeChangedEvent copyWith(void Function(VolumeChangedEvent) updates) =>
      super.copyWith((message) => updates(message as VolumeChangedEvent))
          as VolumeChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static VolumeChangedEvent create() => VolumeChangedEvent._();
  @$core.override
  VolumeChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static VolumeChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<VolumeChangedEvent>(create);
  static VolumeChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.double get volume => $_getN(1);
  @$pb.TagNumber(2)
  set volume($core.double value) => $_setFloat(1, value);
  @$pb.TagNumber(2)
  $core.bool hasVolume() => $_has(1);
  @$pb.TagNumber(2)
  void clearVolume() => $_clearField(2);
}

/// 延迟变化（轻量推送，不触发全量刷新）
class LatencyChangedEvent extends $pb.GeneratedMessage {
  factory LatencyChangedEvent({
    $core.String? instanceId,
    $core.double? latency,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (latency != null) result.latency = latency;
    return result;
  }

  LatencyChangedEvent._();

  factory LatencyChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory LatencyChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'LatencyChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aD(2, _omitFieldNames ? '' : 'latency', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  LatencyChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  LatencyChangedEvent copyWith(void Function(LatencyChangedEvent) updates) =>
      super.copyWith((message) => updates(message as LatencyChangedEvent))
          as LatencyChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static LatencyChangedEvent create() => LatencyChangedEvent._();
  @$core.override
  LatencyChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static LatencyChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<LatencyChangedEvent>(create);
  static LatencyChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.double get latency => $_getN(1);
  @$pb.TagNumber(2)
  set latency($core.double value) => $_setFloat(1, value);
  @$pb.TagNumber(2)
  $core.bool hasLatency() => $_has(1);
  @$pb.TagNumber(2)
  void clearLatency() => $_clearField(2);
}

/// 均衡器变化（轻量推送，不触发全量刷新）
class EqualizerChangedEvent extends $pb.GeneratedMessage {
  factory EqualizerChangedEvent({
    $core.String? instanceId,
    $0.EqualizerState? state,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (state != null) result.state = state;
    return result;
  }

  EqualizerChangedEvent._();

  factory EqualizerChangedEvent.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory EqualizerChangedEvent.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'EqualizerChangedEvent',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOM<$0.EqualizerState>(2, _omitFieldNames ? '' : 'state',
        subBuilder: $0.EqualizerState.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  EqualizerChangedEvent clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  EqualizerChangedEvent copyWith(
          void Function(EqualizerChangedEvent) updates) =>
      super.copyWith((message) => updates(message as EqualizerChangedEvent))
          as EqualizerChangedEvent;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static EqualizerChangedEvent create() => EqualizerChangedEvent._();
  @$core.override
  EqualizerChangedEvent createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static EqualizerChangedEvent getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<EqualizerChangedEvent>(create);
  static EqualizerChangedEvent? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $0.EqualizerState get state => $_getN(1);
  @$pb.TagNumber(2)
  set state($0.EqualizerState value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasState() => $_has(1);
  @$pb.TagNumber(2)
  void clearState() => $_clearField(2);
  @$pb.TagNumber(2)
  $0.EqualizerState ensureState() => $_ensure(1);
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
