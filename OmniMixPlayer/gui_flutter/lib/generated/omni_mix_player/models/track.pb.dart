// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/track.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import 'common.pb.dart' as $0;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

/// 曲目（歌曲）— 核心实体
class Track extends $pb.GeneratedMessage {
  factory Track({
    $core.String? uuid,
    $core.String? title,
    $core.String? artist,
    $core.String? albumId,
    $core.double? duration,
    $core.String? moduleId,
    $0.SourceType? sourceType,
    $core.String? sourcePath,
    $core.bool? isFavorite,
    $core.bool? isExcluded,
    $core.String? coverUri,
    $core.int? playCount,
    $0.OmniTimestamp? createdAt,
    $0.OmniTimestamp? lastPlayedAt,
    $core.List<$core.int>? extendedData,
  }) {
    final result = create();
    if (uuid != null) result.uuid = uuid;
    if (title != null) result.title = title;
    if (artist != null) result.artist = artist;
    if (albumId != null) result.albumId = albumId;
    if (duration != null) result.duration = duration;
    if (moduleId != null) result.moduleId = moduleId;
    if (sourceType != null) result.sourceType = sourceType;
    if (sourcePath != null) result.sourcePath = sourcePath;
    if (isFavorite != null) result.isFavorite = isFavorite;
    if (isExcluded != null) result.isExcluded = isExcluded;
    if (coverUri != null) result.coverUri = coverUri;
    if (playCount != null) result.playCount = playCount;
    if (createdAt != null) result.createdAt = createdAt;
    if (lastPlayedAt != null) result.lastPlayedAt = lastPlayedAt;
    if (extendedData != null) result.extendedData = extendedData;
    return result;
  }

  Track._();

  factory Track.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory Track.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'Track',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'uuid')
    ..aOS(2, _omitFieldNames ? '' : 'title')
    ..aOS(3, _omitFieldNames ? '' : 'artist')
    ..aOS(4, _omitFieldNames ? '' : 'albumId')
    ..aD(5, _omitFieldNames ? '' : 'duration', fieldType: $pb.PbFieldType.OF)
    ..aOS(6, _omitFieldNames ? '' : 'moduleId')
    ..aE<$0.SourceType>(7, _omitFieldNames ? '' : 'sourceType',
        enumValues: $0.SourceType.values)
    ..aOS(8, _omitFieldNames ? '' : 'sourcePath')
    ..aOB(9, _omitFieldNames ? '' : 'isFavorite')
    ..aOB(10, _omitFieldNames ? '' : 'isExcluded')
    ..aOS(11, _omitFieldNames ? '' : 'coverUri')
    ..aI(13, _omitFieldNames ? '' : 'playCount')
    ..aOM<$0.OmniTimestamp>(14, _omitFieldNames ? '' : 'createdAt',
        subBuilder: $0.OmniTimestamp.create)
    ..aOM<$0.OmniTimestamp>(15, _omitFieldNames ? '' : 'lastPlayedAt',
        subBuilder: $0.OmniTimestamp.create)
    ..a<$core.List<$core.int>>(
        16, _omitFieldNames ? '' : 'extendedData', $pb.PbFieldType.OY)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Track clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Track copyWith(void Function(Track) updates) =>
      super.copyWith((message) => updates(message as Track)) as Track;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static Track create() => Track._();
  @$core.override
  Track createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static Track getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<Track>(create);
  static Track? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get uuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set uuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearUuid() => $_clearField(1);

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
  $core.String get moduleId => $_getSZ(5);
  @$pb.TagNumber(6)
  set moduleId($core.String value) => $_setString(5, value);
  @$pb.TagNumber(6)
  $core.bool hasModuleId() => $_has(5);
  @$pb.TagNumber(6)
  void clearModuleId() => $_clearField(6);

  @$pb.TagNumber(7)
  $0.SourceType get sourceType => $_getN(6);
  @$pb.TagNumber(7)
  set sourceType($0.SourceType value) => $_setField(7, value);
  @$pb.TagNumber(7)
  $core.bool hasSourceType() => $_has(6);
  @$pb.TagNumber(7)
  void clearSourceType() => $_clearField(7);

  @$pb.TagNumber(8)
  $core.String get sourcePath => $_getSZ(7);
  @$pb.TagNumber(8)
  set sourcePath($core.String value) => $_setString(7, value);
  @$pb.TagNumber(8)
  $core.bool hasSourcePath() => $_has(7);
  @$pb.TagNumber(8)
  void clearSourcePath() => $_clearField(8);

  @$pb.TagNumber(9)
  $core.bool get isFavorite => $_getBF(8);
  @$pb.TagNumber(9)
  set isFavorite($core.bool value) => $_setBool(8, value);
  @$pb.TagNumber(9)
  $core.bool hasIsFavorite() => $_has(8);
  @$pb.TagNumber(9)
  void clearIsFavorite() => $_clearField(9);

  @$pb.TagNumber(10)
  $core.bool get isExcluded => $_getBF(9);
  @$pb.TagNumber(10)
  set isExcluded($core.bool value) => $_setBool(9, value);
  @$pb.TagNumber(10)
  $core.bool hasIsExcluded() => $_has(9);
  @$pb.TagNumber(10)
  void clearIsExcluded() => $_clearField(10);

  @$pb.TagNumber(11)
  $core.String get coverUri => $_getSZ(10);
  @$pb.TagNumber(11)
  set coverUri($core.String value) => $_setString(10, value);
  @$pb.TagNumber(11)
  $core.bool hasCoverUri() => $_has(10);
  @$pb.TagNumber(11)
  void clearCoverUri() => $_clearField(11);

  @$pb.TagNumber(13)
  $core.int get playCount => $_getIZ(11);
  @$pb.TagNumber(13)
  set playCount($core.int value) => $_setSignedInt32(11, value);
  @$pb.TagNumber(13)
  $core.bool hasPlayCount() => $_has(11);
  @$pb.TagNumber(13)
  void clearPlayCount() => $_clearField(13);

  @$pb.TagNumber(14)
  $0.OmniTimestamp get createdAt => $_getN(12);
  @$pb.TagNumber(14)
  set createdAt($0.OmniTimestamp value) => $_setField(14, value);
  @$pb.TagNumber(14)
  $core.bool hasCreatedAt() => $_has(12);
  @$pb.TagNumber(14)
  void clearCreatedAt() => $_clearField(14);
  @$pb.TagNumber(14)
  $0.OmniTimestamp ensureCreatedAt() => $_ensure(12);

  @$pb.TagNumber(15)
  $0.OmniTimestamp get lastPlayedAt => $_getN(13);
  @$pb.TagNumber(15)
  set lastPlayedAt($0.OmniTimestamp value) => $_setField(15, value);
  @$pb.TagNumber(15)
  $core.bool hasLastPlayedAt() => $_has(13);
  @$pb.TagNumber(15)
  void clearLastPlayedAt() => $_clearField(15);
  @$pb.TagNumber(15)
  $0.OmniTimestamp ensureLastPlayedAt() => $_ensure(13);

  @$pb.TagNumber(16)
  $core.List<$core.int> get extendedData => $_getN(14);
  @$pb.TagNumber(16)
  set extendedData($core.List<$core.int> value) => $_setBytes(14, value);
  @$pb.TagNumber(16)
  $core.bool hasExtendedData() => $_has(14);
  @$pb.TagNumber(16)
  void clearExtendedData() => $_clearField(16);
}

/// Track-Tag 多对多关联
class TrackTag extends $pb.GeneratedMessage {
  factory TrackTag({
    $core.String? trackUuid,
    $core.String? tagId,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (tagId != null) result.tagId = tagId;
    return result;
  }

  TrackTag._();

  factory TrackTag.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory TrackTag.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'TrackTag',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..aOS(2, _omitFieldNames ? '' : 'tagId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackTag clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackTag copyWith(void Function(TrackTag) updates) =>
      super.copyWith((message) => updates(message as TrackTag)) as TrackTag;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static TrackTag create() => TrackTag._();
  @$core.override
  TrackTag createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static TrackTag getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<TrackTag>(create);
  static TrackTag? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get trackUuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set trackUuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTrackUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrackUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get tagId => $_getSZ(1);
  @$pb.TagNumber(2)
  set tagId($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTagId() => $_has(1);
  @$pb.TagNumber(2)
  void clearTagId() => $_clearField(2);
}

/// Track upsert 请求
class UpsertTrackRequest extends $pb.GeneratedMessage {
  factory UpsertTrackRequest({
    Track? track,
  }) {
    final result = create();
    if (track != null) result.track = track;
    return result;
  }

  UpsertTrackRequest._();

  factory UpsertTrackRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTrackRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTrackRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOM<Track>(1, _omitFieldNames ? '' : 'track', subBuilder: Track.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTrackRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTrackRequest copyWith(void Function(UpsertTrackRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertTrackRequest))
          as UpsertTrackRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTrackRequest create() => UpsertTrackRequest._();
  @$core.override
  UpsertTrackRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTrackRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTrackRequest>(create);
  static UpsertTrackRequest? _defaultInstance;

  @$pb.TagNumber(1)
  Track get track => $_getN(0);
  @$pb.TagNumber(1)
  set track(Track value) => $_setField(1, value);
  @$pb.TagNumber(1)
  $core.bool hasTrack() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrack() => $_clearField(1);
  @$pb.TagNumber(1)
  Track ensureTrack() => $_ensure(0);
}

/// Track upsert 响应
class UpsertTrackResponse extends $pb.GeneratedMessage {
  factory UpsertTrackResponse({
    $core.bool? created,
    Track? track,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (track != null) result.track = track;
    return result;
  }

  UpsertTrackResponse._();

  factory UpsertTrackResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTrackResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTrackResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'created')
    ..aOM<Track>(2, _omitFieldNames ? '' : 'track', subBuilder: Track.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTrackResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTrackResponse copyWith(void Function(UpsertTrackResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertTrackResponse))
          as UpsertTrackResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTrackResponse create() => UpsertTrackResponse._();
  @$core.override
  UpsertTrackResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTrackResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTrackResponse>(create);
  static UpsertTrackResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get created => $_getBF(0);
  @$pb.TagNumber(1)
  set created($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasCreated() => $_has(0);
  @$pb.TagNumber(1)
  void clearCreated() => $_clearField(1);

  @$pb.TagNumber(2)
  Track get track => $_getN(1);
  @$pb.TagNumber(2)
  set track(Track value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasTrack() => $_has(1);
  @$pb.TagNumber(2)
  void clearTrack() => $_clearField(2);
  @$pb.TagNumber(2)
  Track ensureTrack() => $_ensure(1);
}

/// 批量 Track upsert
class UpsertTracksRequest extends $pb.GeneratedMessage {
  factory UpsertTracksRequest({
    $core.Iterable<Track>? tracks,
  }) {
    final result = create();
    if (tracks != null) result.tracks.addAll(tracks);
    return result;
  }

  UpsertTracksRequest._();

  factory UpsertTracksRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTracksRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTracksRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<Track>(1, _omitFieldNames ? '' : 'tracks', subBuilder: Track.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTracksRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTracksRequest copyWith(void Function(UpsertTracksRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertTracksRequest))
          as UpsertTracksRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTracksRequest create() => UpsertTracksRequest._();
  @$core.override
  UpsertTracksRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTracksRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTracksRequest>(create);
  static UpsertTracksRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<Track> get tracks => $_getList(0);
}

class UpsertTracksResponse extends $pb.GeneratedMessage {
  factory UpsertTracksResponse({
    $core.int? created,
    $core.int? updated,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (updated != null) result.updated = updated;
    return result;
  }

  UpsertTracksResponse._();

  factory UpsertTracksResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTracksResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTracksResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aI(1, _omitFieldNames ? '' : 'created')
    ..aI(2, _omitFieldNames ? '' : 'updated')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTracksResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTracksResponse copyWith(void Function(UpsertTracksResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertTracksResponse))
          as UpsertTracksResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTracksResponse create() => UpsertTracksResponse._();
  @$core.override
  UpsertTracksResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTracksResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTracksResponse>(create);
  static UpsertTracksResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.int get created => $_getIZ(0);
  @$pb.TagNumber(1)
  set created($core.int value) => $_setSignedInt32(0, value);
  @$pb.TagNumber(1)
  $core.bool hasCreated() => $_has(0);
  @$pb.TagNumber(1)
  void clearCreated() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get updated => $_getIZ(1);
  @$pb.TagNumber(2)
  set updated($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasUpdated() => $_has(1);
  @$pb.TagNumber(2)
  void clearUpdated() => $_clearField(2);
}

/// 设置 Track Tags
class SetTrackTagsRequest extends $pb.GeneratedMessage {
  factory SetTrackTagsRequest({
    $core.String? trackUuid,
    $core.Iterable<$core.String>? tagIds,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (tagIds != null) result.tagIds.addAll(tagIds);
    return result;
  }

  SetTrackTagsRequest._();

  factory SetTrackTagsRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetTrackTagsRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetTrackTagsRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..pPS(2, _omitFieldNames ? '' : 'tagIds')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTrackTagsRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTrackTagsRequest copyWith(void Function(SetTrackTagsRequest) updates) =>
      super.copyWith((message) => updates(message as SetTrackTagsRequest))
          as SetTrackTagsRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetTrackTagsRequest create() => SetTrackTagsRequest._();
  @$core.override
  SetTrackTagsRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetTrackTagsRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetTrackTagsRequest>(create);
  static SetTrackTagsRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get trackUuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set trackUuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTrackUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrackUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<$core.String> get tagIds => $_getList(1);
}

class SetTrackTagsResponse extends $pb.GeneratedMessage {
  factory SetTrackTagsResponse({
    $core.bool? success,
    $core.int? tagCount,
  }) {
    final result = create();
    if (success != null) result.success = success;
    if (tagCount != null) result.tagCount = tagCount;
    return result;
  }

  SetTrackTagsResponse._();

  factory SetTrackTagsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetTrackTagsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetTrackTagsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..aI(2, _omitFieldNames ? '' : 'tagCount')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTrackTagsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTrackTagsResponse copyWith(void Function(SetTrackTagsResponse) updates) =>
      super.copyWith((message) => updates(message as SetTrackTagsResponse))
          as SetTrackTagsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetTrackTagsResponse create() => SetTrackTagsResponse._();
  @$core.override
  SetTrackTagsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetTrackTagsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetTrackTagsResponse>(create);
  static SetTrackTagsResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get tagCount => $_getIZ(1);
  @$pb.TagNumber(2)
  set tagCount($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTagCount() => $_has(1);
  @$pb.TagNumber(2)
  void clearTagCount() => $_clearField(2);
}

/// 添加/移除单个 Track Tag
class ModifyTrackTagRequest extends $pb.GeneratedMessage {
  factory ModifyTrackTagRequest({
    $core.String? trackUuid,
    $core.String? tagId,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (tagId != null) result.tagId = tagId;
    return result;
  }

  ModifyTrackTagRequest._();

  factory ModifyTrackTagRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ModifyTrackTagRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ModifyTrackTagRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..aOS(2, _omitFieldNames ? '' : 'tagId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ModifyTrackTagRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ModifyTrackTagRequest copyWith(
          void Function(ModifyTrackTagRequest) updates) =>
      super.copyWith((message) => updates(message as ModifyTrackTagRequest))
          as ModifyTrackTagRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ModifyTrackTagRequest create() => ModifyTrackTagRequest._();
  @$core.override
  ModifyTrackTagRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ModifyTrackTagRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ModifyTrackTagRequest>(create);
  static ModifyTrackTagRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get trackUuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set trackUuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTrackUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrackUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get tagId => $_getSZ(1);
  @$pb.TagNumber(2)
  set tagId($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTagId() => $_has(1);
  @$pb.TagNumber(2)
  void clearTagId() => $_clearField(2);
}

class ModifyTrackTagResponse extends $pb.GeneratedMessage {
  factory ModifyTrackTagResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  ModifyTrackTagResponse._();

  factory ModifyTrackTagResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ModifyTrackTagResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ModifyTrackTagResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ModifyTrackTagResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ModifyTrackTagResponse copyWith(
          void Function(ModifyTrackTagResponse) updates) =>
      super.copyWith((message) => updates(message as ModifyTrackTagResponse))
          as ModifyTrackTagResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ModifyTrackTagResponse create() => ModifyTrackTagResponse._();
  @$core.override
  ModifyTrackTagResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ModifyTrackTagResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ModifyTrackTagResponse>(create);
  static ModifyTrackTagResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
