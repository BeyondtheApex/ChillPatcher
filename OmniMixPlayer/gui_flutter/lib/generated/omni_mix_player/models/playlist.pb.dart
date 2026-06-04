// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/playlist.proto.

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

/// 歌单
class Playlist extends $pb.GeneratedMessage {
  factory Playlist({
    $core.String? id,
    $core.String? name,
    $core.String? moduleId,
    $0.PlaylistKind? kind,
    $core.String? coverUri,
    $core.int? sortOrder,
    $0.OmniTimestamp? createdAt,
    $0.OmniTimestamp? updatedAt,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (name != null) result.name = name;
    if (moduleId != null) result.moduleId = moduleId;
    if (kind != null) result.kind = kind;
    if (coverUri != null) result.coverUri = coverUri;
    if (sortOrder != null) result.sortOrder = sortOrder;
    if (createdAt != null) result.createdAt = createdAt;
    if (updatedAt != null) result.updatedAt = updatedAt;
    return result;
  }

  Playlist._();

  factory Playlist.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory Playlist.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'Playlist',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'name')
    ..aOS(3, _omitFieldNames ? '' : 'moduleId')
    ..aE<$0.PlaylistKind>(4, _omitFieldNames ? '' : 'kind',
        enumValues: $0.PlaylistKind.values)
    ..aOS(5, _omitFieldNames ? '' : 'coverUri')
    ..aI(6, _omitFieldNames ? '' : 'sortOrder')
    ..aOM<$0.OmniTimestamp>(7, _omitFieldNames ? '' : 'createdAt',
        subBuilder: $0.OmniTimestamp.create)
    ..aOM<$0.OmniTimestamp>(8, _omitFieldNames ? '' : 'updatedAt',
        subBuilder: $0.OmniTimestamp.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Playlist clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Playlist copyWith(void Function(Playlist) updates) =>
      super.copyWith((message) => updates(message as Playlist)) as Playlist;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static Playlist create() => Playlist._();
  @$core.override
  Playlist createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static Playlist getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<Playlist>(create);
  static Playlist? _defaultInstance;

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
  $core.String get moduleId => $_getSZ(2);
  @$pb.TagNumber(3)
  set moduleId($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasModuleId() => $_has(2);
  @$pb.TagNumber(3)
  void clearModuleId() => $_clearField(3);

  @$pb.TagNumber(4)
  $0.PlaylistKind get kind => $_getN(3);
  @$pb.TagNumber(4)
  set kind($0.PlaylistKind value) => $_setField(4, value);
  @$pb.TagNumber(4)
  $core.bool hasKind() => $_has(3);
  @$pb.TagNumber(4)
  void clearKind() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.String get coverUri => $_getSZ(4);
  @$pb.TagNumber(5)
  set coverUri($core.String value) => $_setString(4, value);
  @$pb.TagNumber(5)
  $core.bool hasCoverUri() => $_has(4);
  @$pb.TagNumber(5)
  void clearCoverUri() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.int get sortOrder => $_getIZ(5);
  @$pb.TagNumber(6)
  set sortOrder($core.int value) => $_setSignedInt32(5, value);
  @$pb.TagNumber(6)
  $core.bool hasSortOrder() => $_has(5);
  @$pb.TagNumber(6)
  void clearSortOrder() => $_clearField(6);

  @$pb.TagNumber(7)
  $0.OmniTimestamp get createdAt => $_getN(6);
  @$pb.TagNumber(7)
  set createdAt($0.OmniTimestamp value) => $_setField(7, value);
  @$pb.TagNumber(7)
  $core.bool hasCreatedAt() => $_has(6);
  @$pb.TagNumber(7)
  void clearCreatedAt() => $_clearField(7);
  @$pb.TagNumber(7)
  $0.OmniTimestamp ensureCreatedAt() => $_ensure(6);

  @$pb.TagNumber(8)
  $0.OmniTimestamp get updatedAt => $_getN(7);
  @$pb.TagNumber(8)
  set updatedAt($0.OmniTimestamp value) => $_setField(8, value);
  @$pb.TagNumber(8)
  $core.bool hasUpdatedAt() => $_has(7);
  @$pb.TagNumber(8)
  void clearUpdatedAt() => $_clearField(8);
  @$pb.TagNumber(8)
  $0.OmniTimestamp ensureUpdatedAt() => $_ensure(7);
}

/// 歌单条目 — 歌单中的歌曲位置
class PlaylistEntry extends $pb.GeneratedMessage {
  factory PlaylistEntry({
    $core.String? id,
    $core.String? playlistId,
    $core.String? trackUuid,
    $core.int? position,
    $0.OmniTimestamp? addedAt,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (playlistId != null) result.playlistId = playlistId;
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (position != null) result.position = position;
    if (addedAt != null) result.addedAt = addedAt;
    return result;
  }

  PlaylistEntry._();

  factory PlaylistEntry.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistEntry.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistEntry',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'playlistId')
    ..aOS(3, _omitFieldNames ? '' : 'trackUuid')
    ..aI(4, _omitFieldNames ? '' : 'position')
    ..aOM<$0.OmniTimestamp>(5, _omitFieldNames ? '' : 'addedAt',
        subBuilder: $0.OmniTimestamp.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistEntry clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistEntry copyWith(void Function(PlaylistEntry) updates) =>
      super.copyWith((message) => updates(message as PlaylistEntry))
          as PlaylistEntry;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistEntry create() => PlaylistEntry._();
  @$core.override
  PlaylistEntry createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistEntry getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistEntry>(create);
  static PlaylistEntry? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get playlistId => $_getSZ(1);
  @$pb.TagNumber(2)
  set playlistId($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasPlaylistId() => $_has(1);
  @$pb.TagNumber(2)
  void clearPlaylistId() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get trackUuid => $_getSZ(2);
  @$pb.TagNumber(3)
  set trackUuid($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasTrackUuid() => $_has(2);
  @$pb.TagNumber(3)
  void clearTrackUuid() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.int get position => $_getIZ(3);
  @$pb.TagNumber(4)
  set position($core.int value) => $_setSignedInt32(3, value);
  @$pb.TagNumber(4)
  $core.bool hasPosition() => $_has(3);
  @$pb.TagNumber(4)
  void clearPosition() => $_clearField(4);

  @$pb.TagNumber(5)
  $0.OmniTimestamp get addedAt => $_getN(4);
  @$pb.TagNumber(5)
  set addedAt($0.OmniTimestamp value) => $_setField(5, value);
  @$pb.TagNumber(5)
  $core.bool hasAddedAt() => $_has(4);
  @$pb.TagNumber(5)
  void clearAddedAt() => $_clearField(5);
  @$pb.TagNumber(5)
  $0.OmniTimestamp ensureAddedAt() => $_ensure(4);
}

/// 歌单条目规格 (用于插入时指定)
class PlaylistEntrySpec extends $pb.GeneratedMessage {
  factory PlaylistEntrySpec({
    $core.String? trackUuid,
    $core.int? position,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (position != null) result.position = position;
    return result;
  }

  PlaylistEntrySpec._();

  factory PlaylistEntrySpec.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistEntrySpec.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistEntrySpec',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..aI(2, _omitFieldNames ? '' : 'position')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistEntrySpec clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistEntrySpec copyWith(void Function(PlaylistEntrySpec) updates) =>
      super.copyWith((message) => updates(message as PlaylistEntrySpec))
          as PlaylistEntrySpec;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistEntrySpec create() => PlaylistEntrySpec._();
  @$core.override
  PlaylistEntrySpec createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistEntrySpec getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistEntrySpec>(create);
  static PlaylistEntrySpec? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get trackUuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set trackUuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTrackUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrackUuid() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get position => $_getIZ(1);
  @$pb.TagNumber(2)
  set position($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasPosition() => $_has(1);
  @$pb.TagNumber(2)
  void clearPosition() => $_clearField(2);
}

/// Playlist upsert
class UpsertPlaylistRequest extends $pb.GeneratedMessage {
  factory UpsertPlaylistRequest({
    Playlist? playlist,
  }) {
    final result = create();
    if (playlist != null) result.playlist = playlist;
    return result;
  }

  UpsertPlaylistRequest._();

  factory UpsertPlaylistRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertPlaylistRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertPlaylistRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOM<Playlist>(1, _omitFieldNames ? '' : 'playlist',
        subBuilder: Playlist.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertPlaylistRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertPlaylistRequest copyWith(
          void Function(UpsertPlaylistRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertPlaylistRequest))
          as UpsertPlaylistRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertPlaylistRequest create() => UpsertPlaylistRequest._();
  @$core.override
  UpsertPlaylistRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertPlaylistRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertPlaylistRequest>(create);
  static UpsertPlaylistRequest? _defaultInstance;

  @$pb.TagNumber(1)
  Playlist get playlist => $_getN(0);
  @$pb.TagNumber(1)
  set playlist(Playlist value) => $_setField(1, value);
  @$pb.TagNumber(1)
  $core.bool hasPlaylist() => $_has(0);
  @$pb.TagNumber(1)
  void clearPlaylist() => $_clearField(1);
  @$pb.TagNumber(1)
  Playlist ensurePlaylist() => $_ensure(0);
}

class UpsertPlaylistResponse extends $pb.GeneratedMessage {
  factory UpsertPlaylistResponse({
    $core.bool? created,
    Playlist? playlist,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (playlist != null) result.playlist = playlist;
    return result;
  }

  UpsertPlaylistResponse._();

  factory UpsertPlaylistResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertPlaylistResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertPlaylistResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'created')
    ..aOM<Playlist>(2, _omitFieldNames ? '' : 'playlist',
        subBuilder: Playlist.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertPlaylistResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertPlaylistResponse copyWith(
          void Function(UpsertPlaylistResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertPlaylistResponse))
          as UpsertPlaylistResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertPlaylistResponse create() => UpsertPlaylistResponse._();
  @$core.override
  UpsertPlaylistResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertPlaylistResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertPlaylistResponse>(create);
  static UpsertPlaylistResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get created => $_getBF(0);
  @$pb.TagNumber(1)
  set created($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasCreated() => $_has(0);
  @$pb.TagNumber(1)
  void clearCreated() => $_clearField(1);

  @$pb.TagNumber(2)
  Playlist get playlist => $_getN(1);
  @$pb.TagNumber(2)
  set playlist(Playlist value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasPlaylist() => $_has(1);
  @$pb.TagNumber(2)
  void clearPlaylist() => $_clearField(2);
  @$pb.TagNumber(2)
  Playlist ensurePlaylist() => $_ensure(1);
}

/// 替换整个歌单的条目
class ReplacePlaylistEntriesRequest extends $pb.GeneratedMessage {
  factory ReplacePlaylistEntriesRequest({
    $core.String? playlistId,
    $core.Iterable<PlaylistEntrySpec>? entries,
  }) {
    final result = create();
    if (playlistId != null) result.playlistId = playlistId;
    if (entries != null) result.entries.addAll(entries);
    return result;
  }

  ReplacePlaylistEntriesRequest._();

  factory ReplacePlaylistEntriesRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ReplacePlaylistEntriesRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ReplacePlaylistEntriesRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'playlistId')
    ..pPM<PlaylistEntrySpec>(2, _omitFieldNames ? '' : 'entries',
        subBuilder: PlaylistEntrySpec.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ReplacePlaylistEntriesRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ReplacePlaylistEntriesRequest copyWith(
          void Function(ReplacePlaylistEntriesRequest) updates) =>
      super.copyWith(
              (message) => updates(message as ReplacePlaylistEntriesRequest))
          as ReplacePlaylistEntriesRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ReplacePlaylistEntriesRequest create() =>
      ReplacePlaylistEntriesRequest._();
  @$core.override
  ReplacePlaylistEntriesRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ReplacePlaylistEntriesRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ReplacePlaylistEntriesRequest>(create);
  static ReplacePlaylistEntriesRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get playlistId => $_getSZ(0);
  @$pb.TagNumber(1)
  set playlistId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasPlaylistId() => $_has(0);
  @$pb.TagNumber(1)
  void clearPlaylistId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<PlaylistEntrySpec> get entries => $_getList(1);
}

class ReplacePlaylistEntriesResponse extends $pb.GeneratedMessage {
  factory ReplacePlaylistEntriesResponse({
    $core.bool? success,
    $core.int? entryCount,
  }) {
    final result = create();
    if (success != null) result.success = success;
    if (entryCount != null) result.entryCount = entryCount;
    return result;
  }

  ReplacePlaylistEntriesResponse._();

  factory ReplacePlaylistEntriesResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ReplacePlaylistEntriesResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ReplacePlaylistEntriesResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..aI(2, _omitFieldNames ? '' : 'entryCount')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ReplacePlaylistEntriesResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ReplacePlaylistEntriesResponse copyWith(
          void Function(ReplacePlaylistEntriesResponse) updates) =>
      super.copyWith(
              (message) => updates(message as ReplacePlaylistEntriesResponse))
          as ReplacePlaylistEntriesResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ReplacePlaylistEntriesResponse create() =>
      ReplacePlaylistEntriesResponse._();
  @$core.override
  ReplacePlaylistEntriesResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ReplacePlaylistEntriesResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ReplacePlaylistEntriesResponse>(create);
  static ReplacePlaylistEntriesResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get entryCount => $_getIZ(1);
  @$pb.TagNumber(2)
  set entryCount($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasEntryCount() => $_has(1);
  @$pb.TagNumber(2)
  void clearEntryCount() => $_clearField(2);
}

/// 在指定位置插入条目
class InsertPlaylistEntryRequest extends $pb.GeneratedMessage {
  factory InsertPlaylistEntryRequest({
    $core.String? playlistId,
    PlaylistEntrySpec? entry,
    $core.int? index,
  }) {
    final result = create();
    if (playlistId != null) result.playlistId = playlistId;
    if (entry != null) result.entry = entry;
    if (index != null) result.index = index;
    return result;
  }

  InsertPlaylistEntryRequest._();

  factory InsertPlaylistEntryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InsertPlaylistEntryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InsertPlaylistEntryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'playlistId')
    ..aOM<PlaylistEntrySpec>(2, _omitFieldNames ? '' : 'entry',
        subBuilder: PlaylistEntrySpec.create)
    ..aI(3, _omitFieldNames ? '' : 'index')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertPlaylistEntryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertPlaylistEntryRequest copyWith(
          void Function(InsertPlaylistEntryRequest) updates) =>
      super.copyWith(
              (message) => updates(message as InsertPlaylistEntryRequest))
          as InsertPlaylistEntryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InsertPlaylistEntryRequest create() => InsertPlaylistEntryRequest._();
  @$core.override
  InsertPlaylistEntryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InsertPlaylistEntryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InsertPlaylistEntryRequest>(create);
  static InsertPlaylistEntryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get playlistId => $_getSZ(0);
  @$pb.TagNumber(1)
  set playlistId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasPlaylistId() => $_has(0);
  @$pb.TagNumber(1)
  void clearPlaylistId() => $_clearField(1);

  @$pb.TagNumber(2)
  PlaylistEntrySpec get entry => $_getN(1);
  @$pb.TagNumber(2)
  set entry(PlaylistEntrySpec value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasEntry() => $_has(1);
  @$pb.TagNumber(2)
  void clearEntry() => $_clearField(2);
  @$pb.TagNumber(2)
  PlaylistEntrySpec ensureEntry() => $_ensure(1);

  @$pb.TagNumber(3)
  $core.int get index => $_getIZ(2);
  @$pb.TagNumber(3)
  set index($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasIndex() => $_has(2);
  @$pb.TagNumber(3)
  void clearIndex() => $_clearField(3);
}

class InsertPlaylistEntryResponse extends $pb.GeneratedMessage {
  factory InsertPlaylistEntryResponse({
    $core.bool? success,
    PlaylistEntry? entry,
  }) {
    final result = create();
    if (success != null) result.success = success;
    if (entry != null) result.entry = entry;
    return result;
  }

  InsertPlaylistEntryResponse._();

  factory InsertPlaylistEntryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InsertPlaylistEntryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InsertPlaylistEntryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..aOM<PlaylistEntry>(2, _omitFieldNames ? '' : 'entry',
        subBuilder: PlaylistEntry.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertPlaylistEntryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertPlaylistEntryResponse copyWith(
          void Function(InsertPlaylistEntryResponse) updates) =>
      super.copyWith(
              (message) => updates(message as InsertPlaylistEntryResponse))
          as InsertPlaylistEntryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InsertPlaylistEntryResponse create() =>
      InsertPlaylistEntryResponse._();
  @$core.override
  InsertPlaylistEntryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InsertPlaylistEntryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InsertPlaylistEntryResponse>(create);
  static InsertPlaylistEntryResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);

  @$pb.TagNumber(2)
  PlaylistEntry get entry => $_getN(1);
  @$pb.TagNumber(2)
  set entry(PlaylistEntry value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasEntry() => $_has(1);
  @$pb.TagNumber(2)
  void clearEntry() => $_clearField(2);
  @$pb.TagNumber(2)
  PlaylistEntry ensureEntry() => $_ensure(1);
}

/// 删除条目
class RemovePlaylistEntryRequest extends $pb.GeneratedMessage {
  factory RemovePlaylistEntryRequest({
    $core.String? entryId,
  }) {
    final result = create();
    if (entryId != null) result.entryId = entryId;
    return result;
  }

  RemovePlaylistEntryRequest._();

  factory RemovePlaylistEntryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory RemovePlaylistEntryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'RemovePlaylistEntryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'entryId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemovePlaylistEntryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemovePlaylistEntryRequest copyWith(
          void Function(RemovePlaylistEntryRequest) updates) =>
      super.copyWith(
              (message) => updates(message as RemovePlaylistEntryRequest))
          as RemovePlaylistEntryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static RemovePlaylistEntryRequest create() => RemovePlaylistEntryRequest._();
  @$core.override
  RemovePlaylistEntryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static RemovePlaylistEntryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<RemovePlaylistEntryRequest>(create);
  static RemovePlaylistEntryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get entryId => $_getSZ(0);
  @$pb.TagNumber(1)
  set entryId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasEntryId() => $_has(0);
  @$pb.TagNumber(1)
  void clearEntryId() => $_clearField(1);
}

class RemovePlaylistEntryResponse extends $pb.GeneratedMessage {
  factory RemovePlaylistEntryResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  RemovePlaylistEntryResponse._();

  factory RemovePlaylistEntryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory RemovePlaylistEntryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'RemovePlaylistEntryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemovePlaylistEntryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemovePlaylistEntryResponse copyWith(
          void Function(RemovePlaylistEntryResponse) updates) =>
      super.copyWith(
              (message) => updates(message as RemovePlaylistEntryResponse))
          as RemovePlaylistEntryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static RemovePlaylistEntryResponse create() =>
      RemovePlaylistEntryResponse._();
  @$core.override
  RemovePlaylistEntryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static RemovePlaylistEntryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<RemovePlaylistEntryResponse>(create);
  static RemovePlaylistEntryResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

/// 移动条目
class MovePlaylistEntryRequest extends $pb.GeneratedMessage {
  factory MovePlaylistEntryRequest({
    $core.String? entryId,
    $core.int? newIndex,
  }) {
    final result = create();
    if (entryId != null) result.entryId = entryId;
    if (newIndex != null) result.newIndex = newIndex;
    return result;
  }

  MovePlaylistEntryRequest._();

  factory MovePlaylistEntryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory MovePlaylistEntryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'MovePlaylistEntryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'entryId')
    ..aI(2, _omitFieldNames ? '' : 'newIndex')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MovePlaylistEntryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MovePlaylistEntryRequest copyWith(
          void Function(MovePlaylistEntryRequest) updates) =>
      super.copyWith((message) => updates(message as MovePlaylistEntryRequest))
          as MovePlaylistEntryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static MovePlaylistEntryRequest create() => MovePlaylistEntryRequest._();
  @$core.override
  MovePlaylistEntryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static MovePlaylistEntryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<MovePlaylistEntryRequest>(create);
  static MovePlaylistEntryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get entryId => $_getSZ(0);
  @$pb.TagNumber(1)
  set entryId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasEntryId() => $_has(0);
  @$pb.TagNumber(1)
  void clearEntryId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get newIndex => $_getIZ(1);
  @$pb.TagNumber(2)
  set newIndex($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasNewIndex() => $_has(1);
  @$pb.TagNumber(2)
  void clearNewIndex() => $_clearField(2);
}

class MovePlaylistEntryResponse extends $pb.GeneratedMessage {
  factory MovePlaylistEntryResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  MovePlaylistEntryResponse._();

  factory MovePlaylistEntryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory MovePlaylistEntryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'MovePlaylistEntryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MovePlaylistEntryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MovePlaylistEntryResponse copyWith(
          void Function(MovePlaylistEntryResponse) updates) =>
      super.copyWith((message) => updates(message as MovePlaylistEntryResponse))
          as MovePlaylistEntryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static MovePlaylistEntryResponse create() => MovePlaylistEntryResponse._();
  @$core.override
  MovePlaylistEntryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static MovePlaylistEntryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<MovePlaylistEntryResponse>(create);
  static MovePlaylistEntryResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

/// 歌单查询结果（包含 entries 以支持重复歌曲）
class PlaylistWithEntries extends $pb.GeneratedMessage {
  factory PlaylistWithEntries({
    $core.String? playlistId,
    $core.String? playlistName,
    $core.Iterable<PlaylistEntryWithTrack>? entries,
  }) {
    final result = create();
    if (playlistId != null) result.playlistId = playlistId;
    if (playlistName != null) result.playlistName = playlistName;
    if (entries != null) result.entries.addAll(entries);
    return result;
  }

  PlaylistWithEntries._();

  factory PlaylistWithEntries.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistWithEntries.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistWithEntries',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'playlistId')
    ..aOS(2, _omitFieldNames ? '' : 'playlistName')
    ..pPM<PlaylistEntryWithTrack>(3, _omitFieldNames ? '' : 'entries',
        subBuilder: PlaylistEntryWithTrack.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistWithEntries clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistWithEntries copyWith(void Function(PlaylistWithEntries) updates) =>
      super.copyWith((message) => updates(message as PlaylistWithEntries))
          as PlaylistWithEntries;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistWithEntries create() => PlaylistWithEntries._();
  @$core.override
  PlaylistWithEntries createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistWithEntries getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistWithEntries>(create);
  static PlaylistWithEntries? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get playlistId => $_getSZ(0);
  @$pb.TagNumber(1)
  set playlistId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasPlaylistId() => $_has(0);
  @$pb.TagNumber(1)
  void clearPlaylistId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get playlistName => $_getSZ(1);
  @$pb.TagNumber(2)
  set playlistName($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasPlaylistName() => $_has(1);
  @$pb.TagNumber(2)
  void clearPlaylistName() => $_clearField(2);

  @$pb.TagNumber(3)
  $pb.PbList<PlaylistEntryWithTrack> get entries => $_getList(2);
}

/// 条目 + 关联 Track 信息
class PlaylistEntryWithTrack extends $pb.GeneratedMessage {
  factory PlaylistEntryWithTrack({
    $core.String? entryId,
    $core.String? trackUuid,
    $core.String? title,
    $core.String? artist,
    $core.double? duration,
    $core.String? albumId,
    $core.String? coverUri,
    $core.int? position,
  }) {
    final result = create();
    if (entryId != null) result.entryId = entryId;
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (title != null) result.title = title;
    if (artist != null) result.artist = artist;
    if (duration != null) result.duration = duration;
    if (albumId != null) result.albumId = albumId;
    if (coverUri != null) result.coverUri = coverUri;
    if (position != null) result.position = position;
    return result;
  }

  PlaylistEntryWithTrack._();

  factory PlaylistEntryWithTrack.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistEntryWithTrack.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistEntryWithTrack',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'entryId')
    ..aOS(2, _omitFieldNames ? '' : 'trackUuid')
    ..aOS(3, _omitFieldNames ? '' : 'title')
    ..aOS(4, _omitFieldNames ? '' : 'artist')
    ..aD(5, _omitFieldNames ? '' : 'duration', fieldType: $pb.PbFieldType.OF)
    ..aOS(6, _omitFieldNames ? '' : 'albumId')
    ..aOS(7, _omitFieldNames ? '' : 'coverUri')
    ..aI(8, _omitFieldNames ? '' : 'position')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistEntryWithTrack clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistEntryWithTrack copyWith(
          void Function(PlaylistEntryWithTrack) updates) =>
      super.copyWith((message) => updates(message as PlaylistEntryWithTrack))
          as PlaylistEntryWithTrack;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistEntryWithTrack create() => PlaylistEntryWithTrack._();
  @$core.override
  PlaylistEntryWithTrack createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistEntryWithTrack getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistEntryWithTrack>(create);
  static PlaylistEntryWithTrack? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get entryId => $_getSZ(0);
  @$pb.TagNumber(1)
  set entryId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasEntryId() => $_has(0);
  @$pb.TagNumber(1)
  void clearEntryId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get trackUuid => $_getSZ(1);
  @$pb.TagNumber(2)
  set trackUuid($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTrackUuid() => $_has(1);
  @$pb.TagNumber(2)
  void clearTrackUuid() => $_clearField(2);

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
  $core.double get duration => $_getN(4);
  @$pb.TagNumber(5)
  set duration($core.double value) => $_setFloat(4, value);
  @$pb.TagNumber(5)
  $core.bool hasDuration() => $_has(4);
  @$pb.TagNumber(5)
  void clearDuration() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.String get albumId => $_getSZ(5);
  @$pb.TagNumber(6)
  set albumId($core.String value) => $_setString(5, value);
  @$pb.TagNumber(6)
  $core.bool hasAlbumId() => $_has(5);
  @$pb.TagNumber(6)
  void clearAlbumId() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.String get coverUri => $_getSZ(6);
  @$pb.TagNumber(7)
  set coverUri($core.String value) => $_setString(6, value);
  @$pb.TagNumber(7)
  $core.bool hasCoverUri() => $_has(6);
  @$pb.TagNumber(7)
  void clearCoverUri() => $_clearField(7);

  @$pb.TagNumber(8)
  $core.int get position => $_getIZ(7);
  @$pb.TagNumber(8)
  set position($core.int value) => $_setSignedInt32(7, value);
  @$pb.TagNumber(8)
  $core.bool hasPosition() => $_has(7);
  @$pb.TagNumber(8)
  void clearPosition() => $_clearField(8);
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
