// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/instance.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import 'common.pb.dart' as $0;
import 'instance.pbenum.dart';

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

export 'instance.pbenum.dart';

/// 实例能力声明
class InstanceCapabilities extends $pb.GeneratedMessage {
  factory InstanceCapabilities({
    $core.bool? serverControlledPlayback,
    $core.bool? clientManagedPlayback,
    $core.bool? queueManagement,
    $core.bool? playlistManagement,
    $core.bool? multiplePlaylists,
    $core.bool? tagFiltering,
    $core.bool? unlimitedTags,
    $core.bool? albumFiltering,
    $core.bool? shuffle,
    $core.bool? repeat,
    $core.bool? seek,
    $core.bool? volumeControl,
    $core.bool? equalizer,
    $core.int? maxImportedPlaylists,
    $core.int? maxTags,
    $core.int? maxPlaylistEntries,
  }) {
    final result = create();
    if (serverControlledPlayback != null)
      result.serverControlledPlayback = serverControlledPlayback;
    if (clientManagedPlayback != null)
      result.clientManagedPlayback = clientManagedPlayback;
    if (queueManagement != null) result.queueManagement = queueManagement;
    if (playlistManagement != null)
      result.playlistManagement = playlistManagement;
    if (multiplePlaylists != null) result.multiplePlaylists = multiplePlaylists;
    if (tagFiltering != null) result.tagFiltering = tagFiltering;
    if (unlimitedTags != null) result.unlimitedTags = unlimitedTags;
    if (albumFiltering != null) result.albumFiltering = albumFiltering;
    if (shuffle != null) result.shuffle = shuffle;
    if (repeat != null) result.repeat = repeat;
    if (seek != null) result.seek = seek;
    if (volumeControl != null) result.volumeControl = volumeControl;
    if (equalizer != null) result.equalizer = equalizer;
    if (maxImportedPlaylists != null)
      result.maxImportedPlaylists = maxImportedPlaylists;
    if (maxTags != null) result.maxTags = maxTags;
    if (maxPlaylistEntries != null)
      result.maxPlaylistEntries = maxPlaylistEntries;
    return result;
  }

  InstanceCapabilities._();

  factory InstanceCapabilities.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceCapabilities.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceCapabilities',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'serverControlledPlayback')
    ..aOB(2, _omitFieldNames ? '' : 'clientManagedPlayback')
    ..aOB(3, _omitFieldNames ? '' : 'queueManagement')
    ..aOB(4, _omitFieldNames ? '' : 'playlistManagement')
    ..aOB(5, _omitFieldNames ? '' : 'multiplePlaylists')
    ..aOB(6, _omitFieldNames ? '' : 'tagFiltering')
    ..aOB(7, _omitFieldNames ? '' : 'unlimitedTags')
    ..aOB(8, _omitFieldNames ? '' : 'albumFiltering')
    ..aOB(9, _omitFieldNames ? '' : 'shuffle')
    ..aOB(10, _omitFieldNames ? '' : 'repeat')
    ..aOB(11, _omitFieldNames ? '' : 'seek')
    ..aOB(12, _omitFieldNames ? '' : 'volumeControl')
    ..aOB(13, _omitFieldNames ? '' : 'equalizer')
    ..aI(20, _omitFieldNames ? '' : 'maxImportedPlaylists')
    ..aI(21, _omitFieldNames ? '' : 'maxTags')
    ..aI(22, _omitFieldNames ? '' : 'maxPlaylistEntries')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceCapabilities clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceCapabilities copyWith(void Function(InstanceCapabilities) updates) =>
      super.copyWith((message) => updates(message as InstanceCapabilities))
          as InstanceCapabilities;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceCapabilities create() => InstanceCapabilities._();
  @$core.override
  InstanceCapabilities createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceCapabilities getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceCapabilities>(create);
  static InstanceCapabilities? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get serverControlledPlayback => $_getBF(0);
  @$pb.TagNumber(1)
  set serverControlledPlayback($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasServerControlledPlayback() => $_has(0);
  @$pb.TagNumber(1)
  void clearServerControlledPlayback() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.bool get clientManagedPlayback => $_getBF(1);
  @$pb.TagNumber(2)
  set clientManagedPlayback($core.bool value) => $_setBool(1, value);
  @$pb.TagNumber(2)
  $core.bool hasClientManagedPlayback() => $_has(1);
  @$pb.TagNumber(2)
  void clearClientManagedPlayback() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.bool get queueManagement => $_getBF(2);
  @$pb.TagNumber(3)
  set queueManagement($core.bool value) => $_setBool(2, value);
  @$pb.TagNumber(3)
  $core.bool hasQueueManagement() => $_has(2);
  @$pb.TagNumber(3)
  void clearQueueManagement() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.bool get playlistManagement => $_getBF(3);
  @$pb.TagNumber(4)
  set playlistManagement($core.bool value) => $_setBool(3, value);
  @$pb.TagNumber(4)
  $core.bool hasPlaylistManagement() => $_has(3);
  @$pb.TagNumber(4)
  void clearPlaylistManagement() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.bool get multiplePlaylists => $_getBF(4);
  @$pb.TagNumber(5)
  set multiplePlaylists($core.bool value) => $_setBool(4, value);
  @$pb.TagNumber(5)
  $core.bool hasMultiplePlaylists() => $_has(4);
  @$pb.TagNumber(5)
  void clearMultiplePlaylists() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.bool get tagFiltering => $_getBF(5);
  @$pb.TagNumber(6)
  set tagFiltering($core.bool value) => $_setBool(5, value);
  @$pb.TagNumber(6)
  $core.bool hasTagFiltering() => $_has(5);
  @$pb.TagNumber(6)
  void clearTagFiltering() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.bool get unlimitedTags => $_getBF(6);
  @$pb.TagNumber(7)
  set unlimitedTags($core.bool value) => $_setBool(6, value);
  @$pb.TagNumber(7)
  $core.bool hasUnlimitedTags() => $_has(6);
  @$pb.TagNumber(7)
  void clearUnlimitedTags() => $_clearField(7);

  @$pb.TagNumber(8)
  $core.bool get albumFiltering => $_getBF(7);
  @$pb.TagNumber(8)
  set albumFiltering($core.bool value) => $_setBool(7, value);
  @$pb.TagNumber(8)
  $core.bool hasAlbumFiltering() => $_has(7);
  @$pb.TagNumber(8)
  void clearAlbumFiltering() => $_clearField(8);

  @$pb.TagNumber(9)
  $core.bool get shuffle => $_getBF(8);
  @$pb.TagNumber(9)
  set shuffle($core.bool value) => $_setBool(8, value);
  @$pb.TagNumber(9)
  $core.bool hasShuffle() => $_has(8);
  @$pb.TagNumber(9)
  void clearShuffle() => $_clearField(9);

  @$pb.TagNumber(10)
  $core.bool get repeat => $_getBF(9);
  @$pb.TagNumber(10)
  set repeat($core.bool value) => $_setBool(9, value);
  @$pb.TagNumber(10)
  $core.bool hasRepeat() => $_has(9);
  @$pb.TagNumber(10)
  void clearRepeat() => $_clearField(10);

  @$pb.TagNumber(11)
  $core.bool get seek => $_getBF(10);
  @$pb.TagNumber(11)
  set seek($core.bool value) => $_setBool(10, value);
  @$pb.TagNumber(11)
  $core.bool hasSeek() => $_has(10);
  @$pb.TagNumber(11)
  void clearSeek() => $_clearField(11);

  @$pb.TagNumber(12)
  $core.bool get volumeControl => $_getBF(11);
  @$pb.TagNumber(12)
  set volumeControl($core.bool value) => $_setBool(11, value);
  @$pb.TagNumber(12)
  $core.bool hasVolumeControl() => $_has(11);
  @$pb.TagNumber(12)
  void clearVolumeControl() => $_clearField(12);

  @$pb.TagNumber(13)
  $core.bool get equalizer => $_getBF(12);
  @$pb.TagNumber(13)
  set equalizer($core.bool value) => $_setBool(12, value);
  @$pb.TagNumber(13)
  $core.bool hasEqualizer() => $_has(12);
  @$pb.TagNumber(13)
  void clearEqualizer() => $_clearField(13);

  /// 限制（null 表示无限制，proto3 用 0 表示未设置 / wrapper types）
  @$pb.TagNumber(20)
  $core.int get maxImportedPlaylists => $_getIZ(13);
  @$pb.TagNumber(20)
  set maxImportedPlaylists($core.int value) => $_setSignedInt32(13, value);
  @$pb.TagNumber(20)
  $core.bool hasMaxImportedPlaylists() => $_has(13);
  @$pb.TagNumber(20)
  void clearMaxImportedPlaylists() => $_clearField(20);

  @$pb.TagNumber(21)
  $core.int get maxTags => $_getIZ(14);
  @$pb.TagNumber(21)
  set maxTags($core.int value) => $_setSignedInt32(14, value);
  @$pb.TagNumber(21)
  $core.bool hasMaxTags() => $_has(14);
  @$pb.TagNumber(21)
  void clearMaxTags() => $_clearField(21);

  @$pb.TagNumber(22)
  $core.int get maxPlaylistEntries => $_getIZ(15);
  @$pb.TagNumber(22)
  set maxPlaylistEntries($core.int value) => $_setSignedInt32(15, value);
  @$pb.TagNumber(22)
  $core.bool hasMaxPlaylistEntries() => $_has(15);
  @$pb.TagNumber(22)
  void clearMaxPlaylistEntries() => $_clearField(22);
}

/// 均衡器点位
class EqualizerPoint extends $pb.GeneratedMessage {
  factory EqualizerPoint({
    $core.String? id,
    $core.double? frequency,
    $core.double? gainDb,
    $core.double? q,
    $0.EqualizerFilterType? type,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (frequency != null) result.frequency = frequency;
    if (gainDb != null) result.gainDb = gainDb;
    if (q != null) result.q = q;
    if (type != null) result.type = type;
    return result;
  }

  EqualizerPoint._();

  factory EqualizerPoint.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory EqualizerPoint.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'EqualizerPoint',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aD(2, _omitFieldNames ? '' : 'frequency', fieldType: $pb.PbFieldType.OF)
    ..aD(3, _omitFieldNames ? '' : 'gainDb', fieldType: $pb.PbFieldType.OF)
    ..aD(4, _omitFieldNames ? '' : 'q', fieldType: $pb.PbFieldType.OF)
    ..aE<$0.EqualizerFilterType>(5, _omitFieldNames ? '' : 'type',
        enumValues: $0.EqualizerFilterType.values)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  EqualizerPoint clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  EqualizerPoint copyWith(void Function(EqualizerPoint) updates) =>
      super.copyWith((message) => updates(message as EqualizerPoint))
          as EqualizerPoint;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static EqualizerPoint create() => EqualizerPoint._();
  @$core.override
  EqualizerPoint createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static EqualizerPoint getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<EqualizerPoint>(create);
  static EqualizerPoint? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.double get frequency => $_getN(1);
  @$pb.TagNumber(2)
  set frequency($core.double value) => $_setFloat(1, value);
  @$pb.TagNumber(2)
  $core.bool hasFrequency() => $_has(1);
  @$pb.TagNumber(2)
  void clearFrequency() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.double get gainDb => $_getN(2);
  @$pb.TagNumber(3)
  set gainDb($core.double value) => $_setFloat(2, value);
  @$pb.TagNumber(3)
  $core.bool hasGainDb() => $_has(2);
  @$pb.TagNumber(3)
  void clearGainDb() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.double get q => $_getN(3);
  @$pb.TagNumber(4)
  set q($core.double value) => $_setFloat(3, value);
  @$pb.TagNumber(4)
  $core.bool hasQ() => $_has(3);
  @$pb.TagNumber(4)
  void clearQ() => $_clearField(4);

  @$pb.TagNumber(5)
  $0.EqualizerFilterType get type => $_getN(4);
  @$pb.TagNumber(5)
  set type($0.EqualizerFilterType value) => $_setField(5, value);
  @$pb.TagNumber(5)
  $core.bool hasType() => $_has(4);
  @$pb.TagNumber(5)
  void clearType() => $_clearField(5);
}

/// 均衡器状态
class EqualizerState extends $pb.GeneratedMessage {
  factory EqualizerState({
    $core.bool? enabled,
    $core.double? globalGainDb,
    $core.bool? softClipEnabled,
    $core.Iterable<EqualizerPoint>? points,
  }) {
    final result = create();
    if (enabled != null) result.enabled = enabled;
    if (globalGainDb != null) result.globalGainDb = globalGainDb;
    if (softClipEnabled != null) result.softClipEnabled = softClipEnabled;
    if (points != null) result.points.addAll(points);
    return result;
  }

  EqualizerState._();

  factory EqualizerState.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory EqualizerState.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'EqualizerState',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'enabled')
    ..aD(2, _omitFieldNames ? '' : 'globalGainDb',
        fieldType: $pb.PbFieldType.OF)
    ..aOB(3, _omitFieldNames ? '' : 'softClipEnabled')
    ..pPM<EqualizerPoint>(4, _omitFieldNames ? '' : 'points',
        subBuilder: EqualizerPoint.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  EqualizerState clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  EqualizerState copyWith(void Function(EqualizerState) updates) =>
      super.copyWith((message) => updates(message as EqualizerState))
          as EqualizerState;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static EqualizerState create() => EqualizerState._();
  @$core.override
  EqualizerState createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static EqualizerState getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<EqualizerState>(create);
  static EqualizerState? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get enabled => $_getBF(0);
  @$pb.TagNumber(1)
  set enabled($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasEnabled() => $_has(0);
  @$pb.TagNumber(1)
  void clearEnabled() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.double get globalGainDb => $_getN(1);
  @$pb.TagNumber(2)
  set globalGainDb($core.double value) => $_setFloat(1, value);
  @$pb.TagNumber(2)
  $core.bool hasGlobalGainDb() => $_has(1);
  @$pb.TagNumber(2)
  void clearGlobalGainDb() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.bool get softClipEnabled => $_getBF(2);
  @$pb.TagNumber(3)
  set softClipEnabled($core.bool value) => $_setBool(2, value);
  @$pb.TagNumber(3)
  $core.bool hasSoftClipEnabled() => $_has(2);
  @$pb.TagNumber(3)
  void clearSoftClipEnabled() => $_clearField(3);

  @$pb.TagNumber(4)
  $pb.PbList<EqualizerPoint> get points => $_getList(3);
}

/// 队列信息
class QueueInfo extends $pb.GeneratedMessage {
  factory QueueInfo({
    $core.String? id,
    $core.String? name,
    $core.int? songCount,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (name != null) result.name = name;
    if (songCount != null) result.songCount = songCount;
    return result;
  }

  QueueInfo._();

  factory QueueInfo.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueueInfo.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueueInfo',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'name')
    ..aI(3, _omitFieldNames ? '' : 'songCount')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueueInfo clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueueInfo copyWith(void Function(QueueInfo) updates) =>
      super.copyWith((message) => updates(message as QueueInfo)) as QueueInfo;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueueInfo create() => QueueInfo._();
  @$core.override
  QueueInfo createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueueInfo getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<QueueInfo>(create);
  static QueueInfo? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get name => $_getSZ(1);
  @$pb.TagNumber(2)
  set name($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasName() => $_has(1);
  @$pb.TagNumber(2)
  void clearName() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.int get songCount => $_getIZ(2);
  @$pb.TagNumber(3)
  set songCount($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasSongCount() => $_has(2);
  @$pb.TagNumber(3)
  void clearSongCount() => $_clearField(3);
}

/// Persisted playlist source used by an instance queue.
class PlaylistSourceState extends $pb.GeneratedMessage {
  factory PlaylistSourceState({
    $core.String? id,
    $core.String? name,
    $core.Iterable<$core.String>? uuids,
    PlaylistSourceKind? kind,
    $core.String? refId,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (name != null) result.name = name;
    if (uuids != null) result.uuids.addAll(uuids);
    if (kind != null) result.kind = kind;
    if (refId != null) result.refId = refId;
    return result;
  }

  PlaylistSourceState._();

  factory PlaylistSourceState.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistSourceState.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistSourceState',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'name')
    ..pPS(3, _omitFieldNames ? '' : 'uuids')
    ..aE<PlaylistSourceKind>(4, _omitFieldNames ? '' : 'kind',
        enumValues: PlaylistSourceKind.values)
    ..aOS(5, _omitFieldNames ? '' : 'refId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistSourceState clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistSourceState copyWith(void Function(PlaylistSourceState) updates) =>
      super.copyWith((message) => updates(message as PlaylistSourceState))
          as PlaylistSourceState;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistSourceState create() => PlaylistSourceState._();
  @$core.override
  PlaylistSourceState createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistSourceState getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistSourceState>(create);
  static PlaylistSourceState? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get name => $_getSZ(1);
  @$pb.TagNumber(2)
  set name($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasName() => $_has(1);
  @$pb.TagNumber(2)
  void clearName() => $_clearField(2);

  @$pb.TagNumber(3)
  $pb.PbList<$core.String> get uuids => $_getList(2);

  @$pb.TagNumber(4)
  PlaylistSourceKind get kind => $_getN(3);
  @$pb.TagNumber(4)
  set kind(PlaylistSourceKind value) => $_setField(4, value);
  @$pb.TagNumber(4)
  $core.bool hasKind() => $_has(3);
  @$pb.TagNumber(4)
  void clearKind() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.String get refId => $_getSZ(4);
  @$pb.TagNumber(5)
  set refId($core.String value) => $_setString(4, value);
  @$pb.TagNumber(5)
  $core.bool hasRefId() => $_has(4);
  @$pb.TagNumber(5)
  void clearRefId() => $_clearField(5);
}

/// Persisted runtime queue state. Track details remain in LibraryService; this
/// stores only stable IDs so the queue survives reconnects and backend restarts.
class PlaybackQueueState extends $pb.GeneratedMessage {
  factory PlaybackQueueState({
    $core.String? activeQueueId,
    $core.Iterable<$core.String>? queueUuids,
    $core.Iterable<$core.String>? historyUuids,
    $core.Iterable<PlaylistSourceState>? playlistSources,
    $core.bool? shuffle,
    $0.RepeatMode? repeatMode,
  }) {
    final result = create();
    if (activeQueueId != null) result.activeQueueId = activeQueueId;
    if (queueUuids != null) result.queueUuids.addAll(queueUuids);
    if (historyUuids != null) result.historyUuids.addAll(historyUuids);
    if (playlistSources != null) result.playlistSources.addAll(playlistSources);
    if (shuffle != null) result.shuffle = shuffle;
    if (repeatMode != null) result.repeatMode = repeatMode;
    return result;
  }

  PlaybackQueueState._();

  factory PlaybackQueueState.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaybackQueueState.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaybackQueueState',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'activeQueueId')
    ..pPS(2, _omitFieldNames ? '' : 'queueUuids')
    ..pPS(3, _omitFieldNames ? '' : 'historyUuids')
    ..pPM<PlaylistSourceState>(4, _omitFieldNames ? '' : 'playlistSources',
        subBuilder: PlaylistSourceState.create)
    ..aOB(5, _omitFieldNames ? '' : 'shuffle')
    ..aE<$0.RepeatMode>(6, _omitFieldNames ? '' : 'repeatMode',
        enumValues: $0.RepeatMode.values)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaybackQueueState clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaybackQueueState copyWith(void Function(PlaybackQueueState) updates) =>
      super.copyWith((message) => updates(message as PlaybackQueueState))
          as PlaybackQueueState;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaybackQueueState create() => PlaybackQueueState._();
  @$core.override
  PlaybackQueueState createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaybackQueueState getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaybackQueueState>(create);
  static PlaybackQueueState? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get activeQueueId => $_getSZ(0);
  @$pb.TagNumber(1)
  set activeQueueId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasActiveQueueId() => $_has(0);
  @$pb.TagNumber(1)
  void clearActiveQueueId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<$core.String> get queueUuids => $_getList(1);

  @$pb.TagNumber(3)
  $pb.PbList<$core.String> get historyUuids => $_getList(2);

  @$pb.TagNumber(4)
  $pb.PbList<PlaylistSourceState> get playlistSources => $_getList(3);

  @$pb.TagNumber(5)
  $core.bool get shuffle => $_getBF(4);
  @$pb.TagNumber(5)
  set shuffle($core.bool value) => $_setBool(4, value);
  @$pb.TagNumber(5)
  $core.bool hasShuffle() => $_has(4);
  @$pb.TagNumber(5)
  void clearShuffle() => $_clearField(5);

  @$pb.TagNumber(6)
  $0.RepeatMode get repeatMode => $_getN(5);
  @$pb.TagNumber(6)
  set repeatMode($0.RepeatMode value) => $_setField(6, value);
  @$pb.TagNumber(6)
  $core.bool hasRepeatMode() => $_has(5);
  @$pb.TagNumber(6)
  void clearRepeatMode() => $_clearField(6);
}

/// 实例配置 — 统一持久化
class InstanceProfile extends $pb.GeneratedMessage {
  factory InstanceProfile({
    $core.String? id,
    $core.String? displayName,
    $0.InstanceKind? kind,
    $core.String? modId,
    $core.String? gameName,
    $0.PlaybackModeType? mode,
    InstanceCapabilities? capabilities,
    $core.double? volume,
    $core.double? targetLatency,
    EqualizerState? equalizer,
    $core.String? activeQueueId,
    $core.Iterable<QueueInfo>? queues,
    $core.Iterable<$core.String>? importedPlaylistIds,
    $core.Iterable<$core.String>? pinnedTagIds,
    $0.OmniTimestamp? createdAt,
    $0.OmniTimestamp? updatedAt,
    PlaybackQueueState? playbackQueue,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (displayName != null) result.displayName = displayName;
    if (kind != null) result.kind = kind;
    if (modId != null) result.modId = modId;
    if (gameName != null) result.gameName = gameName;
    if (mode != null) result.mode = mode;
    if (capabilities != null) result.capabilities = capabilities;
    if (volume != null) result.volume = volume;
    if (targetLatency != null) result.targetLatency = targetLatency;
    if (equalizer != null) result.equalizer = equalizer;
    if (activeQueueId != null) result.activeQueueId = activeQueueId;
    if (queues != null) result.queues.addAll(queues);
    if (importedPlaylistIds != null)
      result.importedPlaylistIds.addAll(importedPlaylistIds);
    if (pinnedTagIds != null) result.pinnedTagIds.addAll(pinnedTagIds);
    if (createdAt != null) result.createdAt = createdAt;
    if (updatedAt != null) result.updatedAt = updatedAt;
    if (playbackQueue != null) result.playbackQueue = playbackQueue;
    return result;
  }

  InstanceProfile._();

  factory InstanceProfile.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceProfile.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceProfile',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'displayName')
    ..aE<$0.InstanceKind>(3, _omitFieldNames ? '' : 'kind',
        enumValues: $0.InstanceKind.values)
    ..aOS(4, _omitFieldNames ? '' : 'modId')
    ..aOS(5, _omitFieldNames ? '' : 'gameName')
    ..aE<$0.PlaybackModeType>(6, _omitFieldNames ? '' : 'mode',
        enumValues: $0.PlaybackModeType.values)
    ..aOM<InstanceCapabilities>(7, _omitFieldNames ? '' : 'capabilities',
        subBuilder: InstanceCapabilities.create)
    ..aD(8, _omitFieldNames ? '' : 'volume', fieldType: $pb.PbFieldType.OF)
    ..aD(9, _omitFieldNames ? '' : 'targetLatency',
        fieldType: $pb.PbFieldType.OF)
    ..aOM<EqualizerState>(10, _omitFieldNames ? '' : 'equalizer',
        subBuilder: EqualizerState.create)
    ..aOS(11, _omitFieldNames ? '' : 'activeQueueId')
    ..pPM<QueueInfo>(12, _omitFieldNames ? '' : 'queues',
        subBuilder: QueueInfo.create)
    ..pPS(13, _omitFieldNames ? '' : 'importedPlaylistIds')
    ..pPS(14, _omitFieldNames ? '' : 'pinnedTagIds')
    ..aOM<$0.OmniTimestamp>(15, _omitFieldNames ? '' : 'createdAt',
        subBuilder: $0.OmniTimestamp.create)
    ..aOM<$0.OmniTimestamp>(16, _omitFieldNames ? '' : 'updatedAt',
        subBuilder: $0.OmniTimestamp.create)
    ..aOM<PlaybackQueueState>(17, _omitFieldNames ? '' : 'playbackQueue',
        subBuilder: PlaybackQueueState.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceProfile clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceProfile copyWith(void Function(InstanceProfile) updates) =>
      super.copyWith((message) => updates(message as InstanceProfile))
          as InstanceProfile;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceProfile create() => InstanceProfile._();
  @$core.override
  InstanceProfile createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceProfile getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceProfile>(create);
  static InstanceProfile? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get displayName => $_getSZ(1);
  @$pb.TagNumber(2)
  set displayName($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasDisplayName() => $_has(1);
  @$pb.TagNumber(2)
  void clearDisplayName() => $_clearField(2);

  @$pb.TagNumber(3)
  $0.InstanceKind get kind => $_getN(2);
  @$pb.TagNumber(3)
  set kind($0.InstanceKind value) => $_setField(3, value);
  @$pb.TagNumber(3)
  $core.bool hasKind() => $_has(2);
  @$pb.TagNumber(3)
  void clearKind() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.String get modId => $_getSZ(3);
  @$pb.TagNumber(4)
  set modId($core.String value) => $_setString(3, value);
  @$pb.TagNumber(4)
  $core.bool hasModId() => $_has(3);
  @$pb.TagNumber(4)
  void clearModId() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.String get gameName => $_getSZ(4);
  @$pb.TagNumber(5)
  set gameName($core.String value) => $_setString(4, value);
  @$pb.TagNumber(5)
  $core.bool hasGameName() => $_has(4);
  @$pb.TagNumber(5)
  void clearGameName() => $_clearField(5);

  @$pb.TagNumber(6)
  $0.PlaybackModeType get mode => $_getN(5);
  @$pb.TagNumber(6)
  set mode($0.PlaybackModeType value) => $_setField(6, value);
  @$pb.TagNumber(6)
  $core.bool hasMode() => $_has(5);
  @$pb.TagNumber(6)
  void clearMode() => $_clearField(6);

  @$pb.TagNumber(7)
  InstanceCapabilities get capabilities => $_getN(6);
  @$pb.TagNumber(7)
  set capabilities(InstanceCapabilities value) => $_setField(7, value);
  @$pb.TagNumber(7)
  $core.bool hasCapabilities() => $_has(6);
  @$pb.TagNumber(7)
  void clearCapabilities() => $_clearField(7);
  @$pb.TagNumber(7)
  InstanceCapabilities ensureCapabilities() => $_ensure(6);

  @$pb.TagNumber(8)
  $core.double get volume => $_getN(7);
  @$pb.TagNumber(8)
  set volume($core.double value) => $_setFloat(7, value);
  @$pb.TagNumber(8)
  $core.bool hasVolume() => $_has(7);
  @$pb.TagNumber(8)
  void clearVolume() => $_clearField(8);

  @$pb.TagNumber(9)
  $core.double get targetLatency => $_getN(8);
  @$pb.TagNumber(9)
  set targetLatency($core.double value) => $_setFloat(8, value);
  @$pb.TagNumber(9)
  $core.bool hasTargetLatency() => $_has(8);
  @$pb.TagNumber(9)
  void clearTargetLatency() => $_clearField(9);

  @$pb.TagNumber(10)
  EqualizerState get equalizer => $_getN(9);
  @$pb.TagNumber(10)
  set equalizer(EqualizerState value) => $_setField(10, value);
  @$pb.TagNumber(10)
  $core.bool hasEqualizer() => $_has(9);
  @$pb.TagNumber(10)
  void clearEqualizer() => $_clearField(10);
  @$pb.TagNumber(10)
  EqualizerState ensureEqualizer() => $_ensure(9);

  @$pb.TagNumber(11)
  $core.String get activeQueueId => $_getSZ(10);
  @$pb.TagNumber(11)
  set activeQueueId($core.String value) => $_setString(10, value);
  @$pb.TagNumber(11)
  $core.bool hasActiveQueueId() => $_has(10);
  @$pb.TagNumber(11)
  void clearActiveQueueId() => $_clearField(11);

  @$pb.TagNumber(12)
  $pb.PbList<QueueInfo> get queues => $_getList(11);

  @$pb.TagNumber(13)
  $pb.PbList<$core.String> get importedPlaylistIds => $_getList(12);

  @$pb.TagNumber(14)
  $pb.PbList<$core.String> get pinnedTagIds => $_getList(13);

  @$pb.TagNumber(15)
  $0.OmniTimestamp get createdAt => $_getN(14);
  @$pb.TagNumber(15)
  set createdAt($0.OmniTimestamp value) => $_setField(15, value);
  @$pb.TagNumber(15)
  $core.bool hasCreatedAt() => $_has(14);
  @$pb.TagNumber(15)
  void clearCreatedAt() => $_clearField(15);
  @$pb.TagNumber(15)
  $0.OmniTimestamp ensureCreatedAt() => $_ensure(14);

  @$pb.TagNumber(16)
  $0.OmniTimestamp get updatedAt => $_getN(15);
  @$pb.TagNumber(16)
  set updatedAt($0.OmniTimestamp value) => $_setField(16, value);
  @$pb.TagNumber(16)
  $core.bool hasUpdatedAt() => $_has(15);
  @$pb.TagNumber(16)
  void clearUpdatedAt() => $_clearField(16);
  @$pb.TagNumber(16)
  $0.OmniTimestamp ensureUpdatedAt() => $_ensure(15);

  @$pb.TagNumber(17)
  PlaybackQueueState get playbackQueue => $_getN(16);
  @$pb.TagNumber(17)
  set playbackQueue(PlaybackQueueState value) => $_setField(17, value);
  @$pb.TagNumber(17)
  $core.bool hasPlaybackQueue() => $_has(16);
  @$pb.TagNumber(17)
  void clearPlaybackQueue() => $_clearField(17);
  @$pb.TagNumber(17)
  PlaybackQueueState ensurePlaybackQueue() => $_ensure(16);
}

/// 连接请求
class InstanceConnectRequest extends $pb.GeneratedMessage {
  factory InstanceConnectRequest({
    $core.String? clientId,
    $0.InstanceKind? kind,
    $0.PlaybackModeType? mode,
    InstanceCapabilities? capabilities,
    $core.String? modId,
    $core.String? gameName,
    $core.String? displayName,
  }) {
    final result = create();
    if (clientId != null) result.clientId = clientId;
    if (kind != null) result.kind = kind;
    if (mode != null) result.mode = mode;
    if (capabilities != null) result.capabilities = capabilities;
    if (modId != null) result.modId = modId;
    if (gameName != null) result.gameName = gameName;
    if (displayName != null) result.displayName = displayName;
    return result;
  }

  InstanceConnectRequest._();

  factory InstanceConnectRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceConnectRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceConnectRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'clientId')
    ..aE<$0.InstanceKind>(2, _omitFieldNames ? '' : 'kind',
        enumValues: $0.InstanceKind.values)
    ..aE<$0.PlaybackModeType>(3, _omitFieldNames ? '' : 'mode',
        enumValues: $0.PlaybackModeType.values)
    ..aOM<InstanceCapabilities>(4, _omitFieldNames ? '' : 'capabilities',
        subBuilder: InstanceCapabilities.create)
    ..aOS(5, _omitFieldNames ? '' : 'modId')
    ..aOS(6, _omitFieldNames ? '' : 'gameName')
    ..aOS(7, _omitFieldNames ? '' : 'displayName')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceConnectRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceConnectRequest copyWith(
          void Function(InstanceConnectRequest) updates) =>
      super.copyWith((message) => updates(message as InstanceConnectRequest))
          as InstanceConnectRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceConnectRequest create() => InstanceConnectRequest._();
  @$core.override
  InstanceConnectRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceConnectRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceConnectRequest>(create);
  static InstanceConnectRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get clientId => $_getSZ(0);
  @$pb.TagNumber(1)
  set clientId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasClientId() => $_has(0);
  @$pb.TagNumber(1)
  void clearClientId() => $_clearField(1);

  @$pb.TagNumber(2)
  $0.InstanceKind get kind => $_getN(1);
  @$pb.TagNumber(2)
  set kind($0.InstanceKind value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasKind() => $_has(1);
  @$pb.TagNumber(2)
  void clearKind() => $_clearField(2);

  @$pb.TagNumber(3)
  $0.PlaybackModeType get mode => $_getN(2);
  @$pb.TagNumber(3)
  set mode($0.PlaybackModeType value) => $_setField(3, value);
  @$pb.TagNumber(3)
  $core.bool hasMode() => $_has(2);
  @$pb.TagNumber(3)
  void clearMode() => $_clearField(3);

  @$pb.TagNumber(4)
  InstanceCapabilities get capabilities => $_getN(3);
  @$pb.TagNumber(4)
  set capabilities(InstanceCapabilities value) => $_setField(4, value);
  @$pb.TagNumber(4)
  $core.bool hasCapabilities() => $_has(3);
  @$pb.TagNumber(4)
  void clearCapabilities() => $_clearField(4);
  @$pb.TagNumber(4)
  InstanceCapabilities ensureCapabilities() => $_ensure(3);

  @$pb.TagNumber(5)
  $core.String get modId => $_getSZ(4);
  @$pb.TagNumber(5)
  set modId($core.String value) => $_setString(4, value);
  @$pb.TagNumber(5)
  $core.bool hasModId() => $_has(4);
  @$pb.TagNumber(5)
  void clearModId() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.String get gameName => $_getSZ(5);
  @$pb.TagNumber(6)
  set gameName($core.String value) => $_setString(5, value);
  @$pb.TagNumber(6)
  $core.bool hasGameName() => $_has(5);
  @$pb.TagNumber(6)
  void clearGameName() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.String get displayName => $_getSZ(6);
  @$pb.TagNumber(7)
  set displayName($core.String value) => $_setString(6, value);
  @$pb.TagNumber(7)
  $core.bool hasDisplayName() => $_has(6);
  @$pb.TagNumber(7)
  void clearDisplayName() => $_clearField(7);
}

/// 连接响应
class InstanceConnectResponse extends $pb.GeneratedMessage {
  factory InstanceConnectResponse({
    $core.String? instanceId,
    $core.bool? isNew,
    InstanceProfile? profile,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (isNew != null) result.isNew = isNew;
    if (profile != null) result.profile = profile;
    return result;
  }

  InstanceConnectResponse._();

  factory InstanceConnectResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceConnectResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceConnectResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOB(2, _omitFieldNames ? '' : 'isNew')
    ..aOM<InstanceProfile>(3, _omitFieldNames ? '' : 'profile',
        subBuilder: InstanceProfile.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceConnectResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceConnectResponse copyWith(
          void Function(InstanceConnectResponse) updates) =>
      super.copyWith((message) => updates(message as InstanceConnectResponse))
          as InstanceConnectResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceConnectResponse create() => InstanceConnectResponse._();
  @$core.override
  InstanceConnectResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceConnectResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceConnectResponse>(create);
  static InstanceConnectResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.bool get isNew => $_getBF(1);
  @$pb.TagNumber(2)
  set isNew($core.bool value) => $_setBool(1, value);
  @$pb.TagNumber(2)
  $core.bool hasIsNew() => $_has(1);
  @$pb.TagNumber(2)
  void clearIsNew() => $_clearField(2);

  @$pb.TagNumber(3)
  InstanceProfile get profile => $_getN(2);
  @$pb.TagNumber(3)
  set profile(InstanceProfile value) => $_setField(3, value);
  @$pb.TagNumber(3)
  $core.bool hasProfile() => $_has(2);
  @$pb.TagNumber(3)
  void clearProfile() => $_clearField(3);
  @$pb.TagNumber(3)
  InstanceProfile ensureProfile() => $_ensure(2);
}

/// 心跳请求
class InstanceHeartbeatRequest extends $pb.GeneratedMessage {
  factory InstanceHeartbeatRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  InstanceHeartbeatRequest._();

  factory InstanceHeartbeatRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceHeartbeatRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceHeartbeatRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceHeartbeatRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceHeartbeatRequest copyWith(
          void Function(InstanceHeartbeatRequest) updates) =>
      super.copyWith((message) => updates(message as InstanceHeartbeatRequest))
          as InstanceHeartbeatRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceHeartbeatRequest create() => InstanceHeartbeatRequest._();
  @$core.override
  InstanceHeartbeatRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceHeartbeatRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceHeartbeatRequest>(create);
  static InstanceHeartbeatRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class InstanceHeartbeatResponse extends $pb.GeneratedMessage {
  factory InstanceHeartbeatResponse({
    $core.bool? alive,
  }) {
    final result = create();
    if (alive != null) result.alive = alive;
    return result;
  }

  InstanceHeartbeatResponse._();

  factory InstanceHeartbeatResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceHeartbeatResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceHeartbeatResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'alive')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceHeartbeatResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceHeartbeatResponse copyWith(
          void Function(InstanceHeartbeatResponse) updates) =>
      super.copyWith((message) => updates(message as InstanceHeartbeatResponse))
          as InstanceHeartbeatResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceHeartbeatResponse create() => InstanceHeartbeatResponse._();
  @$core.override
  InstanceHeartbeatResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceHeartbeatResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceHeartbeatResponse>(create);
  static InstanceHeartbeatResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get alive => $_getBF(0);
  @$pb.TagNumber(1)
  set alive($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasAlive() => $_has(0);
  @$pb.TagNumber(1)
  void clearAlive() => $_clearField(1);
}

/// 断开请求
class InstanceDisconnectRequest extends $pb.GeneratedMessage {
  factory InstanceDisconnectRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  InstanceDisconnectRequest._();

  factory InstanceDisconnectRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceDisconnectRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceDisconnectRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceDisconnectRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceDisconnectRequest copyWith(
          void Function(InstanceDisconnectRequest) updates) =>
      super.copyWith((message) => updates(message as InstanceDisconnectRequest))
          as InstanceDisconnectRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceDisconnectRequest create() => InstanceDisconnectRequest._();
  @$core.override
  InstanceDisconnectRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceDisconnectRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceDisconnectRequest>(create);
  static InstanceDisconnectRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class InstanceDisconnectResponse extends $pb.GeneratedMessage {
  factory InstanceDisconnectResponse({
    $core.bool? disconnected,
  }) {
    final result = create();
    if (disconnected != null) result.disconnected = disconnected;
    return result;
  }

  InstanceDisconnectResponse._();

  factory InstanceDisconnectResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceDisconnectResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceDisconnectResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'disconnected')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceDisconnectResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceDisconnectResponse copyWith(
          void Function(InstanceDisconnectResponse) updates) =>
      super.copyWith(
              (message) => updates(message as InstanceDisconnectResponse))
          as InstanceDisconnectResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceDisconnectResponse create() => InstanceDisconnectResponse._();
  @$core.override
  InstanceDisconnectResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceDisconnectResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceDisconnectResponse>(create);
  static InstanceDisconnectResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get disconnected => $_getBF(0);
  @$pb.TagNumber(1)
  set disconnected($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasDisconnected() => $_has(0);
  @$pb.TagNumber(1)
  void clearDisconnected() => $_clearField(1);
}

/// 实例摘要
class InstanceSummary extends $pb.GeneratedMessage {
  factory InstanceSummary({
    $core.String? id,
    $core.String? displayName,
    $0.InstanceKind? kind,
    $0.PlaybackModeType? mode,
    $core.bool? isOnline,
    $core.String? currentTrackUuid,
    $core.int? queueCount,
    $0.OmniTimestamp? connectedAt,
    $core.String? modId,
    $core.String? gameName,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (displayName != null) result.displayName = displayName;
    if (kind != null) result.kind = kind;
    if (mode != null) result.mode = mode;
    if (isOnline != null) result.isOnline = isOnline;
    if (currentTrackUuid != null) result.currentTrackUuid = currentTrackUuid;
    if (queueCount != null) result.queueCount = queueCount;
    if (connectedAt != null) result.connectedAt = connectedAt;
    if (modId != null) result.modId = modId;
    if (gameName != null) result.gameName = gameName;
    return result;
  }

  InstanceSummary._();

  factory InstanceSummary.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InstanceSummary.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InstanceSummary',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'displayName')
    ..aE<$0.InstanceKind>(3, _omitFieldNames ? '' : 'kind',
        enumValues: $0.InstanceKind.values)
    ..aE<$0.PlaybackModeType>(4, _omitFieldNames ? '' : 'mode',
        enumValues: $0.PlaybackModeType.values)
    ..aOB(5, _omitFieldNames ? '' : 'isOnline')
    ..aOS(6, _omitFieldNames ? '' : 'currentTrackUuid')
    ..aI(7, _omitFieldNames ? '' : 'queueCount')
    ..aOM<$0.OmniTimestamp>(8, _omitFieldNames ? '' : 'connectedAt',
        subBuilder: $0.OmniTimestamp.create)
    ..aOS(9, _omitFieldNames ? '' : 'modId')
    ..aOS(10, _omitFieldNames ? '' : 'gameName')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceSummary clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InstanceSummary copyWith(void Function(InstanceSummary) updates) =>
      super.copyWith((message) => updates(message as InstanceSummary))
          as InstanceSummary;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InstanceSummary create() => InstanceSummary._();
  @$core.override
  InstanceSummary createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InstanceSummary getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InstanceSummary>(create);
  static InstanceSummary? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get displayName => $_getSZ(1);
  @$pb.TagNumber(2)
  set displayName($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasDisplayName() => $_has(1);
  @$pb.TagNumber(2)
  void clearDisplayName() => $_clearField(2);

  @$pb.TagNumber(3)
  $0.InstanceKind get kind => $_getN(2);
  @$pb.TagNumber(3)
  set kind($0.InstanceKind value) => $_setField(3, value);
  @$pb.TagNumber(3)
  $core.bool hasKind() => $_has(2);
  @$pb.TagNumber(3)
  void clearKind() => $_clearField(3);

  @$pb.TagNumber(4)
  $0.PlaybackModeType get mode => $_getN(3);
  @$pb.TagNumber(4)
  set mode($0.PlaybackModeType value) => $_setField(4, value);
  @$pb.TagNumber(4)
  $core.bool hasMode() => $_has(3);
  @$pb.TagNumber(4)
  void clearMode() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.bool get isOnline => $_getBF(4);
  @$pb.TagNumber(5)
  set isOnline($core.bool value) => $_setBool(4, value);
  @$pb.TagNumber(5)
  $core.bool hasIsOnline() => $_has(4);
  @$pb.TagNumber(5)
  void clearIsOnline() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.String get currentTrackUuid => $_getSZ(5);
  @$pb.TagNumber(6)
  set currentTrackUuid($core.String value) => $_setString(5, value);
  @$pb.TagNumber(6)
  $core.bool hasCurrentTrackUuid() => $_has(5);
  @$pb.TagNumber(6)
  void clearCurrentTrackUuid() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.int get queueCount => $_getIZ(6);
  @$pb.TagNumber(7)
  set queueCount($core.int value) => $_setSignedInt32(6, value);
  @$pb.TagNumber(7)
  $core.bool hasQueueCount() => $_has(6);
  @$pb.TagNumber(7)
  void clearQueueCount() => $_clearField(7);

  @$pb.TagNumber(8)
  $0.OmniTimestamp get connectedAt => $_getN(7);
  @$pb.TagNumber(8)
  set connectedAt($0.OmniTimestamp value) => $_setField(8, value);
  @$pb.TagNumber(8)
  $core.bool hasConnectedAt() => $_has(7);
  @$pb.TagNumber(8)
  void clearConnectedAt() => $_clearField(8);
  @$pb.TagNumber(8)
  $0.OmniTimestamp ensureConnectedAt() => $_ensure(7);

  @$pb.TagNumber(9)
  $core.String get modId => $_getSZ(8);
  @$pb.TagNumber(9)
  set modId($core.String value) => $_setString(8, value);
  @$pb.TagNumber(9)
  $core.bool hasModId() => $_has(8);
  @$pb.TagNumber(9)
  void clearModId() => $_clearField(9);

  @$pb.TagNumber(10)
  $core.String get gameName => $_getSZ(9);
  @$pb.TagNumber(10)
  set gameName($core.String value) => $_setString(9, value);
  @$pb.TagNumber(10)
  $core.bool hasGameName() => $_has(9);
  @$pb.TagNumber(10)
  void clearGameName() => $_clearField(10);
}

class ListInstancesResponse extends $pb.GeneratedMessage {
  factory ListInstancesResponse({
    $core.Iterable<InstanceSummary>? instances,
  }) {
    final result = create();
    if (instances != null) result.instances.addAll(instances);
    return result;
  }

  ListInstancesResponse._();

  factory ListInstancesResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ListInstancesResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ListInstancesResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<InstanceSummary>(1, _omitFieldNames ? '' : 'instances',
        subBuilder: InstanceSummary.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ListInstancesResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ListInstancesResponse copyWith(
          void Function(ListInstancesResponse) updates) =>
      super.copyWith((message) => updates(message as ListInstancesResponse))
          as ListInstancesResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ListInstancesResponse create() => ListInstancesResponse._();
  @$core.override
  ListInstancesResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ListInstancesResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ListInstancesResponse>(create);
  static ListInstancesResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<InstanceSummary> get instances => $_getList(0);
}

/// 播放状态
class PlaybackStatus extends $pb.GeneratedMessage {
  factory PlaybackStatus({
    $core.String? trackUuid,
    $core.String? title,
    $core.String? artist,
    $core.String? albumId,
    $core.double? duration,
    $core.double? position,
    $core.bool? isPlaying,
    $core.bool? shuffle,
    $0.RepeatMode? repeatMode,
    $core.double? volume,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (title != null) result.title = title;
    if (artist != null) result.artist = artist;
    if (albumId != null) result.albumId = albumId;
    if (duration != null) result.duration = duration;
    if (position != null) result.position = position;
    if (isPlaying != null) result.isPlaying = isPlaying;
    if (shuffle != null) result.shuffle = shuffle;
    if (repeatMode != null) result.repeatMode = repeatMode;
    if (volume != null) result.volume = volume;
    return result;
  }

  PlaybackStatus._();

  factory PlaybackStatus.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaybackStatus.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaybackStatus',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..aOS(2, _omitFieldNames ? '' : 'title')
    ..aOS(3, _omitFieldNames ? '' : 'artist')
    ..aOS(4, _omitFieldNames ? '' : 'albumId')
    ..aD(5, _omitFieldNames ? '' : 'duration', fieldType: $pb.PbFieldType.OF)
    ..aD(6, _omitFieldNames ? '' : 'position', fieldType: $pb.PbFieldType.OF)
    ..aOB(7, _omitFieldNames ? '' : 'isPlaying')
    ..aOB(8, _omitFieldNames ? '' : 'shuffle')
    ..aE<$0.RepeatMode>(9, _omitFieldNames ? '' : 'repeatMode',
        enumValues: $0.RepeatMode.values)
    ..aD(10, _omitFieldNames ? '' : 'volume', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaybackStatus clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaybackStatus copyWith(void Function(PlaybackStatus) updates) =>
      super.copyWith((message) => updates(message as PlaybackStatus))
          as PlaybackStatus;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaybackStatus create() => PlaybackStatus._();
  @$core.override
  PlaybackStatus createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaybackStatus getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaybackStatus>(create);
  static PlaybackStatus? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get trackUuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set trackUuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTrackUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrackUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get title => $_getSZ(1);
  @$pb.TagNumber(2)
  set title($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTitle() => $_has(1);
  @$pb.TagNumber(2)
  void clearTitle() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get artist => $_getSZ(2);
  @$pb.TagNumber(3)
  set artist($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasArtist() => $_has(2);
  @$pb.TagNumber(3)
  void clearArtist() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.String get albumId => $_getSZ(3);
  @$pb.TagNumber(4)
  set albumId($core.String value) => $_setString(3, value);
  @$pb.TagNumber(4)
  $core.bool hasAlbumId() => $_has(3);
  @$pb.TagNumber(4)
  void clearAlbumId() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.double get duration => $_getN(4);
  @$pb.TagNumber(5)
  set duration($core.double value) => $_setFloat(4, value);
  @$pb.TagNumber(5)
  $core.bool hasDuration() => $_has(4);
  @$pb.TagNumber(5)
  void clearDuration() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.double get position => $_getN(5);
  @$pb.TagNumber(6)
  set position($core.double value) => $_setFloat(5, value);
  @$pb.TagNumber(6)
  $core.bool hasPosition() => $_has(5);
  @$pb.TagNumber(6)
  void clearPosition() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.bool get isPlaying => $_getBF(6);
  @$pb.TagNumber(7)
  set isPlaying($core.bool value) => $_setBool(6, value);
  @$pb.TagNumber(7)
  $core.bool hasIsPlaying() => $_has(6);
  @$pb.TagNumber(7)
  void clearIsPlaying() => $_clearField(7);

  @$pb.TagNumber(8)
  $core.bool get shuffle => $_getBF(7);
  @$pb.TagNumber(8)
  set shuffle($core.bool value) => $_setBool(7, value);
  @$pb.TagNumber(8)
  $core.bool hasShuffle() => $_has(7);
  @$pb.TagNumber(8)
  void clearShuffle() => $_clearField(8);

  @$pb.TagNumber(9)
  $0.RepeatMode get repeatMode => $_getN(8);
  @$pb.TagNumber(9)
  set repeatMode($0.RepeatMode value) => $_setField(9, value);
  @$pb.TagNumber(9)
  $core.bool hasRepeatMode() => $_has(8);
  @$pb.TagNumber(9)
  void clearRepeatMode() => $_clearField(9);

  @$pb.TagNumber(10)
  $core.double get volume => $_getN(9);
  @$pb.TagNumber(10)
  set volume($core.double value) => $_setFloat(9, value);
  @$pb.TagNumber(10)
  $core.bool hasVolume() => $_has(9);
  @$pb.TagNumber(10)
  void clearVolume() => $_clearField(10);
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
