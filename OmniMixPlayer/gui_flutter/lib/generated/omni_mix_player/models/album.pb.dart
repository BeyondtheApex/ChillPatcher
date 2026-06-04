// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/album.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

/// 专辑 — 独立实体
class Album extends $pb.GeneratedMessage {
  factory Album({
    $core.String? id,
    $core.String? title,
    $core.String? artist,
    $core.String? coverUri,
    $core.int? year,
    $core.String? moduleId,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (title != null) result.title = title;
    if (artist != null) result.artist = artist;
    if (coverUri != null) result.coverUri = coverUri;
    if (year != null) result.year = year;
    if (moduleId != null) result.moduleId = moduleId;
    return result;
  }

  Album._();

  factory Album.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory Album.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'Album',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'title')
    ..aOS(3, _omitFieldNames ? '' : 'artist')
    ..aOS(4, _omitFieldNames ? '' : 'coverUri')
    ..aI(5, _omitFieldNames ? '' : 'year')
    ..aOS(6, _omitFieldNames ? '' : 'moduleId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Album clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Album copyWith(void Function(Album) updates) =>
      super.copyWith((message) => updates(message as Album)) as Album;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static Album create() => Album._();
  @$core.override
  Album createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static Album getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<Album>(create);
  static Album? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);

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
  $core.String get coverUri => $_getSZ(3);
  @$pb.TagNumber(4)
  set coverUri($core.String value) => $_setString(3, value);
  @$pb.TagNumber(4)
  $core.bool hasCoverUri() => $_has(3);
  @$pb.TagNumber(4)
  void clearCoverUri() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.int get year => $_getIZ(4);
  @$pb.TagNumber(5)
  set year($core.int value) => $_setSignedInt32(4, value);
  @$pb.TagNumber(5)
  $core.bool hasYear() => $_has(4);
  @$pb.TagNumber(5)
  void clearYear() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.String get moduleId => $_getSZ(5);
  @$pb.TagNumber(6)
  set moduleId($core.String value) => $_setString(5, value);
  @$pb.TagNumber(6)
  $core.bool hasModuleId() => $_has(5);
  @$pb.TagNumber(6)
  void clearModuleId() => $_clearField(6);
}

/// Album upsert
class UpsertAlbumRequest extends $pb.GeneratedMessage {
  factory UpsertAlbumRequest({
    Album? album,
  }) {
    final result = create();
    if (album != null) result.album = album;
    return result;
  }

  UpsertAlbumRequest._();

  factory UpsertAlbumRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertAlbumRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertAlbumRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOM<Album>(1, _omitFieldNames ? '' : 'album', subBuilder: Album.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumRequest copyWith(void Function(UpsertAlbumRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertAlbumRequest))
          as UpsertAlbumRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertAlbumRequest create() => UpsertAlbumRequest._();
  @$core.override
  UpsertAlbumRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertAlbumRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertAlbumRequest>(create);
  static UpsertAlbumRequest? _defaultInstance;

  @$pb.TagNumber(1)
  Album get album => $_getN(0);
  @$pb.TagNumber(1)
  set album(Album value) => $_setField(1, value);
  @$pb.TagNumber(1)
  $core.bool hasAlbum() => $_has(0);
  @$pb.TagNumber(1)
  void clearAlbum() => $_clearField(1);
  @$pb.TagNumber(1)
  Album ensureAlbum() => $_ensure(0);
}

class UpsertAlbumResponse extends $pb.GeneratedMessage {
  factory UpsertAlbumResponse({
    $core.bool? created,
    Album? album,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (album != null) result.album = album;
    return result;
  }

  UpsertAlbumResponse._();

  factory UpsertAlbumResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertAlbumResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertAlbumResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'created')
    ..aOM<Album>(2, _omitFieldNames ? '' : 'album', subBuilder: Album.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumResponse copyWith(void Function(UpsertAlbumResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertAlbumResponse))
          as UpsertAlbumResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertAlbumResponse create() => UpsertAlbumResponse._();
  @$core.override
  UpsertAlbumResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertAlbumResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertAlbumResponse>(create);
  static UpsertAlbumResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get created => $_getBF(0);
  @$pb.TagNumber(1)
  set created($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasCreated() => $_has(0);
  @$pb.TagNumber(1)
  void clearCreated() => $_clearField(1);

  @$pb.TagNumber(2)
  Album get album => $_getN(1);
  @$pb.TagNumber(2)
  set album(Album value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasAlbum() => $_has(1);
  @$pb.TagNumber(2)
  void clearAlbum() => $_clearField(2);
  @$pb.TagNumber(2)
  Album ensureAlbum() => $_ensure(1);
}

/// 批量 Album upsert
class UpsertAlbumsRequest extends $pb.GeneratedMessage {
  factory UpsertAlbumsRequest({
    $core.Iterable<Album>? albums,
  }) {
    final result = create();
    if (albums != null) result.albums.addAll(albums);
    return result;
  }

  UpsertAlbumsRequest._();

  factory UpsertAlbumsRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertAlbumsRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertAlbumsRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<Album>(1, _omitFieldNames ? '' : 'albums', subBuilder: Album.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumsRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumsRequest copyWith(void Function(UpsertAlbumsRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertAlbumsRequest))
          as UpsertAlbumsRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertAlbumsRequest create() => UpsertAlbumsRequest._();
  @$core.override
  UpsertAlbumsRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertAlbumsRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertAlbumsRequest>(create);
  static UpsertAlbumsRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<Album> get albums => $_getList(0);
}

class UpsertAlbumsResponse extends $pb.GeneratedMessage {
  factory UpsertAlbumsResponse({
    $core.int? created,
    $core.int? updated,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (updated != null) result.updated = updated;
    return result;
  }

  UpsertAlbumsResponse._();

  factory UpsertAlbumsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertAlbumsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertAlbumsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aI(1, _omitFieldNames ? '' : 'created')
    ..aI(2, _omitFieldNames ? '' : 'updated')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertAlbumsResponse copyWith(void Function(UpsertAlbumsResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertAlbumsResponse))
          as UpsertAlbumsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertAlbumsResponse create() => UpsertAlbumsResponse._();
  @$core.override
  UpsertAlbumsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertAlbumsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertAlbumsResponse>(create);
  static UpsertAlbumsResponse? _defaultInstance;

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

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
