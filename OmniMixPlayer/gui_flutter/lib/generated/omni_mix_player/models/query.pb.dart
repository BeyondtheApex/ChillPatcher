// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/query.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import 'album.pb.dart' as $1;
import 'common.pbenum.dart' as $4;
import 'playlist.pb.dart' as $3;
import 'tag.pb.dart' as $2;
import 'track.pb.dart' as $0;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

/// Track 查询过滤器
class TrackQuery extends $pb.GeneratedMessage {
  factory TrackQuery({
    $core.String? text,
    $core.String? albumId,
    $core.Iterable<$core.String>? tagIds,
    $core.String? playlistId,
    $core.String? moduleId,
    $core.bool? isFavorite,
    $core.bool? isExcluded,
    $core.int? offset,
    $core.int? limit,
    TrackSort? sort,
  }) {
    final result = create();
    if (text != null) result.text = text;
    if (albumId != null) result.albumId = albumId;
    if (tagIds != null) result.tagIds.addAll(tagIds);
    if (playlistId != null) result.playlistId = playlistId;
    if (moduleId != null) result.moduleId = moduleId;
    if (isFavorite != null) result.isFavorite = isFavorite;
    if (isExcluded != null) result.isExcluded = isExcluded;
    if (offset != null) result.offset = offset;
    if (limit != null) result.limit = limit;
    if (sort != null) result.sort = sort;
    return result;
  }

  TrackQuery._();

  factory TrackQuery.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory TrackQuery.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'TrackQuery',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'text')
    ..aOS(2, _omitFieldNames ? '' : 'albumId')
    ..pPS(3, _omitFieldNames ? '' : 'tagIds')
    ..aOS(4, _omitFieldNames ? '' : 'playlistId')
    ..aOS(5, _omitFieldNames ? '' : 'moduleId')
    ..aOB(6, _omitFieldNames ? '' : 'isFavorite')
    ..aOB(7, _omitFieldNames ? '' : 'isExcluded')
    ..aI(10, _omitFieldNames ? '' : 'offset')
    ..aI(11, _omitFieldNames ? '' : 'limit')
    ..aOM<TrackSort>(12, _omitFieldNames ? '' : 'sort',
        subBuilder: TrackSort.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackQuery clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackQuery copyWith(void Function(TrackQuery) updates) =>
      super.copyWith((message) => updates(message as TrackQuery)) as TrackQuery;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static TrackQuery create() => TrackQuery._();
  @$core.override
  TrackQuery createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static TrackQuery getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<TrackQuery>(create);
  static TrackQuery? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get text => $_getSZ(0);
  @$pb.TagNumber(1)
  set text($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasText() => $_has(0);
  @$pb.TagNumber(1)
  void clearText() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get albumId => $_getSZ(1);
  @$pb.TagNumber(2)
  set albumId($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasAlbumId() => $_has(1);
  @$pb.TagNumber(2)
  void clearAlbumId() => $_clearField(2);

  @$pb.TagNumber(3)
  $pb.PbList<$core.String> get tagIds => $_getList(2);

  @$pb.TagNumber(4)
  $core.String get playlistId => $_getSZ(3);
  @$pb.TagNumber(4)
  set playlistId($core.String value) => $_setString(3, value);
  @$pb.TagNumber(4)
  $core.bool hasPlaylistId() => $_has(3);
  @$pb.TagNumber(4)
  void clearPlaylistId() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.String get moduleId => $_getSZ(4);
  @$pb.TagNumber(5)
  set moduleId($core.String value) => $_setString(4, value);
  @$pb.TagNumber(5)
  $core.bool hasModuleId() => $_has(4);
  @$pb.TagNumber(5)
  void clearModuleId() => $_clearField(5);

  @$pb.TagNumber(6)
  $core.bool get isFavorite => $_getBF(5);
  @$pb.TagNumber(6)
  set isFavorite($core.bool value) => $_setBool(5, value);
  @$pb.TagNumber(6)
  $core.bool hasIsFavorite() => $_has(5);
  @$pb.TagNumber(6)
  void clearIsFavorite() => $_clearField(6);

  @$pb.TagNumber(7)
  $core.bool get isExcluded => $_getBF(6);
  @$pb.TagNumber(7)
  set isExcluded($core.bool value) => $_setBool(6, value);
  @$pb.TagNumber(7)
  $core.bool hasIsExcluded() => $_has(6);
  @$pb.TagNumber(7)
  void clearIsExcluded() => $_clearField(7);

  @$pb.TagNumber(10)
  $core.int get offset => $_getIZ(7);
  @$pb.TagNumber(10)
  set offset($core.int value) => $_setSignedInt32(7, value);
  @$pb.TagNumber(10)
  $core.bool hasOffset() => $_has(7);
  @$pb.TagNumber(10)
  void clearOffset() => $_clearField(10);

  @$pb.TagNumber(11)
  $core.int get limit => $_getIZ(8);
  @$pb.TagNumber(11)
  set limit($core.int value) => $_setSignedInt32(8, value);
  @$pb.TagNumber(11)
  $core.bool hasLimit() => $_has(8);
  @$pb.TagNumber(11)
  void clearLimit() => $_clearField(11);

  @$pb.TagNumber(12)
  TrackSort get sort => $_getN(9);
  @$pb.TagNumber(12)
  set sort(TrackSort value) => $_setField(12, value);
  @$pb.TagNumber(12)
  $core.bool hasSort() => $_has(9);
  @$pb.TagNumber(12)
  void clearSort() => $_clearField(12);
  @$pb.TagNumber(12)
  TrackSort ensureSort() => $_ensure(9);
}

/// 排序规格
class TrackSort extends $pb.GeneratedMessage {
  factory TrackSort({
    $4.TrackSortField? field_1,
    $4.SortDirection? direction,
  }) {
    final result = create();
    if (field_1 != null) result.field_1 = field_1;
    if (direction != null) result.direction = direction;
    return result;
  }

  TrackSort._();

  factory TrackSort.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory TrackSort.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'TrackSort',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aE<$4.TrackSortField>(1, _omitFieldNames ? '' : 'field',
        enumValues: $4.TrackSortField.values)
    ..aE<$4.SortDirection>(2, _omitFieldNames ? '' : 'direction',
        enumValues: $4.SortDirection.values)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackSort clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TrackSort copyWith(void Function(TrackSort) updates) =>
      super.copyWith((message) => updates(message as TrackSort)) as TrackSort;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static TrackSort create() => TrackSort._();
  @$core.override
  TrackSort createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static TrackSort getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<TrackSort>(create);
  static TrackSort? _defaultInstance;

  @$pb.TagNumber(1)
  $4.TrackSortField get field_1 => $_getN(0);
  @$pb.TagNumber(1)
  set field_1($4.TrackSortField value) => $_setField(1, value);
  @$pb.TagNumber(1)
  $core.bool hasField_1() => $_has(0);
  @$pb.TagNumber(1)
  void clearField_1() => $_clearField(1);

  @$pb.TagNumber(2)
  $4.SortDirection get direction => $_getN(1);
  @$pb.TagNumber(2)
  set direction($4.SortDirection value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasDirection() => $_has(1);
  @$pb.TagNumber(2)
  void clearDirection() => $_clearField(2);
}

/// 分页信息
class Pagination extends $pb.GeneratedMessage {
  factory Pagination({
    $core.int? offset,
    $core.int? limit,
    $core.int? total,
  }) {
    final result = create();
    if (offset != null) result.offset = offset;
    if (limit != null) result.limit = limit;
    if (total != null) result.total = total;
    return result;
  }

  Pagination._();

  factory Pagination.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory Pagination.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'Pagination',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aI(1, _omitFieldNames ? '' : 'offset')
    ..aI(2, _omitFieldNames ? '' : 'limit')
    ..aI(3, _omitFieldNames ? '' : 'total')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Pagination clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Pagination copyWith(void Function(Pagination) updates) =>
      super.copyWith((message) => updates(message as Pagination)) as Pagination;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static Pagination create() => Pagination._();
  @$core.override
  Pagination createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static Pagination getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<Pagination>(create);
  static Pagination? _defaultInstance;

  @$pb.TagNumber(1)
  $core.int get offset => $_getIZ(0);
  @$pb.TagNumber(1)
  set offset($core.int value) => $_setSignedInt32(0, value);
  @$pb.TagNumber(1)
  $core.bool hasOffset() => $_has(0);
  @$pb.TagNumber(1)
  void clearOffset() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get limit => $_getIZ(1);
  @$pb.TagNumber(2)
  set limit($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasLimit() => $_has(1);
  @$pb.TagNumber(2)
  void clearLimit() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.int get total => $_getIZ(2);
  @$pb.TagNumber(3)
  set total($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasTotal() => $_has(2);
  @$pb.TagNumber(3)
  void clearTotal() => $_clearField(3);
}

/// 查询 Track 的响应
class QueryTracksResponse extends $pb.GeneratedMessage {
  factory QueryTracksResponse({
    $core.Iterable<$0.Track>? tracks,
    Pagination? pagination,
  }) {
    final result = create();
    if (tracks != null) result.tracks.addAll(tracks);
    if (pagination != null) result.pagination = pagination;
    return result;
  }

  QueryTracksResponse._();

  factory QueryTracksResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueryTracksResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueryTracksResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<$0.Track>(1, _omitFieldNames ? '' : 'tracks',
        subBuilder: $0.Track.create)
    ..aOM<Pagination>(2, _omitFieldNames ? '' : 'pagination',
        subBuilder: Pagination.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryTracksResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryTracksResponse copyWith(void Function(QueryTracksResponse) updates) =>
      super.copyWith((message) => updates(message as QueryTracksResponse))
          as QueryTracksResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueryTracksResponse create() => QueryTracksResponse._();
  @$core.override
  QueryTracksResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueryTracksResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<QueryTracksResponse>(create);
  static QueryTracksResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<$0.Track> get tracks => $_getList(0);

  @$pb.TagNumber(2)
  Pagination get pagination => $_getN(1);
  @$pb.TagNumber(2)
  set pagination(Pagination value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasPagination() => $_has(1);
  @$pb.TagNumber(2)
  void clearPagination() => $_clearField(2);
  @$pb.TagNumber(2)
  Pagination ensurePagination() => $_ensure(1);
}

/// 查询 Album 的过滤
class AlbumQuery extends $pb.GeneratedMessage {
  factory AlbumQuery({
    $core.String? tagId,
    $core.String? moduleId,
    $core.String? text,
    $core.int? offset,
    $core.int? limit,
  }) {
    final result = create();
    if (tagId != null) result.tagId = tagId;
    if (moduleId != null) result.moduleId = moduleId;
    if (text != null) result.text = text;
    if (offset != null) result.offset = offset;
    if (limit != null) result.limit = limit;
    return result;
  }

  AlbumQuery._();

  factory AlbumQuery.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory AlbumQuery.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'AlbumQuery',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'tagId')
    ..aOS(2, _omitFieldNames ? '' : 'moduleId')
    ..aOS(3, _omitFieldNames ? '' : 'text')
    ..aI(4, _omitFieldNames ? '' : 'offset')
    ..aI(5, _omitFieldNames ? '' : 'limit')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  AlbumQuery clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  AlbumQuery copyWith(void Function(AlbumQuery) updates) =>
      super.copyWith((message) => updates(message as AlbumQuery)) as AlbumQuery;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static AlbumQuery create() => AlbumQuery._();
  @$core.override
  AlbumQuery createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static AlbumQuery getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<AlbumQuery>(create);
  static AlbumQuery? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get tagId => $_getSZ(0);
  @$pb.TagNumber(1)
  set tagId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTagId() => $_has(0);
  @$pb.TagNumber(1)
  void clearTagId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.String get moduleId => $_getSZ(1);
  @$pb.TagNumber(2)
  set moduleId($core.String value) => $_setString(1, value);
  @$pb.TagNumber(2)
  $core.bool hasModuleId() => $_has(1);
  @$pb.TagNumber(2)
  void clearModuleId() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get text => $_getSZ(2);
  @$pb.TagNumber(3)
  set text($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasText() => $_has(2);
  @$pb.TagNumber(3)
  void clearText() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.int get offset => $_getIZ(3);
  @$pb.TagNumber(4)
  set offset($core.int value) => $_setSignedInt32(3, value);
  @$pb.TagNumber(4)
  $core.bool hasOffset() => $_has(3);
  @$pb.TagNumber(4)
  void clearOffset() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.int get limit => $_getIZ(4);
  @$pb.TagNumber(5)
  set limit($core.int value) => $_setSignedInt32(4, value);
  @$pb.TagNumber(5)
  $core.bool hasLimit() => $_has(4);
  @$pb.TagNumber(5)
  void clearLimit() => $_clearField(5);
}

class QueryAlbumsResponse extends $pb.GeneratedMessage {
  factory QueryAlbumsResponse({
    $core.Iterable<$1.Album>? albums,
    Pagination? pagination,
  }) {
    final result = create();
    if (albums != null) result.albums.addAll(albums);
    if (pagination != null) result.pagination = pagination;
    return result;
  }

  QueryAlbumsResponse._();

  factory QueryAlbumsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueryAlbumsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueryAlbumsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<$1.Album>(1, _omitFieldNames ? '' : 'albums',
        subBuilder: $1.Album.create)
    ..aOM<Pagination>(2, _omitFieldNames ? '' : 'pagination',
        subBuilder: Pagination.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryAlbumsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryAlbumsResponse copyWith(void Function(QueryAlbumsResponse) updates) =>
      super.copyWith((message) => updates(message as QueryAlbumsResponse))
          as QueryAlbumsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueryAlbumsResponse create() => QueryAlbumsResponse._();
  @$core.override
  QueryAlbumsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueryAlbumsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<QueryAlbumsResponse>(create);
  static QueryAlbumsResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<$1.Album> get albums => $_getList(0);

  @$pb.TagNumber(2)
  Pagination get pagination => $_getN(1);
  @$pb.TagNumber(2)
  set pagination(Pagination value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasPagination() => $_has(1);
  @$pb.TagNumber(2)
  void clearPagination() => $_clearField(2);
  @$pb.TagNumber(2)
  Pagination ensurePagination() => $_ensure(1);
}

/// 查询 Tag
class TagQuery extends $pb.GeneratedMessage {
  factory TagQuery({
    $core.String? moduleId,
    $core.Iterable<$4.TagKind>? kinds,
    $core.int? offset,
    $core.int? limit,
  }) {
    final result = create();
    if (moduleId != null) result.moduleId = moduleId;
    if (kinds != null) result.kinds.addAll(kinds);
    if (offset != null) result.offset = offset;
    if (limit != null) result.limit = limit;
    return result;
  }

  TagQuery._();

  factory TagQuery.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory TagQuery.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'TagQuery',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'moduleId')
    ..pc<$4.TagKind>(2, _omitFieldNames ? '' : 'kinds', $pb.PbFieldType.KE,
        valueOf: $4.TagKind.valueOf,
        enumValues: $4.TagKind.values,
        defaultEnumValue: $4.TagKind.TAG_KIND_UNSPECIFIED)
    ..aI(3, _omitFieldNames ? '' : 'offset')
    ..aI(4, _omitFieldNames ? '' : 'limit')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TagQuery clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  TagQuery copyWith(void Function(TagQuery) updates) =>
      super.copyWith((message) => updates(message as TagQuery)) as TagQuery;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static TagQuery create() => TagQuery._();
  @$core.override
  TagQuery createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static TagQuery getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<TagQuery>(create);
  static TagQuery? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get moduleId => $_getSZ(0);
  @$pb.TagNumber(1)
  set moduleId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasModuleId() => $_has(0);
  @$pb.TagNumber(1)
  void clearModuleId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<$4.TagKind> get kinds => $_getList(1);

  @$pb.TagNumber(3)
  $core.int get offset => $_getIZ(2);
  @$pb.TagNumber(3)
  set offset($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasOffset() => $_has(2);
  @$pb.TagNumber(3)
  void clearOffset() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.int get limit => $_getIZ(3);
  @$pb.TagNumber(4)
  set limit($core.int value) => $_setSignedInt32(3, value);
  @$pb.TagNumber(4)
  $core.bool hasLimit() => $_has(3);
  @$pb.TagNumber(4)
  void clearLimit() => $_clearField(4);
}

class QueryTagsResponse extends $pb.GeneratedMessage {
  factory QueryTagsResponse({
    $core.Iterable<$2.Tag>? tags,
    Pagination? pagination,
  }) {
    final result = create();
    if (tags != null) result.tags.addAll(tags);
    if (pagination != null) result.pagination = pagination;
    return result;
  }

  QueryTagsResponse._();

  factory QueryTagsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueryTagsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueryTagsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<$2.Tag>(1, _omitFieldNames ? '' : 'tags', subBuilder: $2.Tag.create)
    ..aOM<Pagination>(2, _omitFieldNames ? '' : 'pagination',
        subBuilder: Pagination.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryTagsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryTagsResponse copyWith(void Function(QueryTagsResponse) updates) =>
      super.copyWith((message) => updates(message as QueryTagsResponse))
          as QueryTagsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueryTagsResponse create() => QueryTagsResponse._();
  @$core.override
  QueryTagsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueryTagsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<QueryTagsResponse>(create);
  static QueryTagsResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<$2.Tag> get tags => $_getList(0);

  @$pb.TagNumber(2)
  Pagination get pagination => $_getN(1);
  @$pb.TagNumber(2)
  set pagination(Pagination value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasPagination() => $_has(1);
  @$pb.TagNumber(2)
  void clearPagination() => $_clearField(2);
  @$pb.TagNumber(2)
  Pagination ensurePagination() => $_ensure(1);
}

/// 查询 Playlist
class PlaylistQuery extends $pb.GeneratedMessage {
  factory PlaylistQuery({
    $core.String? moduleId,
    $core.Iterable<$4.PlaylistKind>? kinds,
    $core.int? offset,
    $core.int? limit,
  }) {
    final result = create();
    if (moduleId != null) result.moduleId = moduleId;
    if (kinds != null) result.kinds.addAll(kinds);
    if (offset != null) result.offset = offset;
    if (limit != null) result.limit = limit;
    return result;
  }

  PlaylistQuery._();

  factory PlaylistQuery.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistQuery.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistQuery',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'moduleId')
    ..pc<$4.PlaylistKind>(2, _omitFieldNames ? '' : 'kinds', $pb.PbFieldType.KE,
        valueOf: $4.PlaylistKind.valueOf,
        enumValues: $4.PlaylistKind.values,
        defaultEnumValue: $4.PlaylistKind.PLAYLIST_KIND_UNSPECIFIED)
    ..aI(3, _omitFieldNames ? '' : 'offset')
    ..aI(4, _omitFieldNames ? '' : 'limit')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistQuery clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistQuery copyWith(void Function(PlaylistQuery) updates) =>
      super.copyWith((message) => updates(message as PlaylistQuery))
          as PlaylistQuery;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistQuery create() => PlaylistQuery._();
  @$core.override
  PlaylistQuery createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistQuery getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistQuery>(create);
  static PlaylistQuery? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get moduleId => $_getSZ(0);
  @$pb.TagNumber(1)
  set moduleId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasModuleId() => $_has(0);
  @$pb.TagNumber(1)
  void clearModuleId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<$4.PlaylistKind> get kinds => $_getList(1);

  @$pb.TagNumber(3)
  $core.int get offset => $_getIZ(2);
  @$pb.TagNumber(3)
  set offset($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasOffset() => $_has(2);
  @$pb.TagNumber(3)
  void clearOffset() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.int get limit => $_getIZ(3);
  @$pb.TagNumber(4)
  set limit($core.int value) => $_setSignedInt32(3, value);
  @$pb.TagNumber(4)
  $core.bool hasLimit() => $_has(3);
  @$pb.TagNumber(4)
  void clearLimit() => $_clearField(4);
}

class QueryPlaylistsResponse extends $pb.GeneratedMessage {
  factory QueryPlaylistsResponse({
    $core.Iterable<$3.Playlist>? playlists,
    Pagination? pagination,
  }) {
    final result = create();
    if (playlists != null) result.playlists.addAll(playlists);
    if (pagination != null) result.pagination = pagination;
    return result;
  }

  QueryPlaylistsResponse._();

  factory QueryPlaylistsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueryPlaylistsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueryPlaylistsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<$3.Playlist>(1, _omitFieldNames ? '' : 'playlists',
        subBuilder: $3.Playlist.create)
    ..aOM<Pagination>(2, _omitFieldNames ? '' : 'pagination',
        subBuilder: Pagination.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryPlaylistsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueryPlaylistsResponse copyWith(
          void Function(QueryPlaylistsResponse) updates) =>
      super.copyWith((message) => updates(message as QueryPlaylistsResponse))
          as QueryPlaylistsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueryPlaylistsResponse create() => QueryPlaylistsResponse._();
  @$core.override
  QueryPlaylistsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueryPlaylistsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<QueryPlaylistsResponse>(create);
  static QueryPlaylistsResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<$3.Playlist> get playlists => $_getList(0);

  @$pb.TagNumber(2)
  Pagination get pagination => $_getN(1);
  @$pb.TagNumber(2)
  set pagination(Pagination value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasPagination() => $_has(1);
  @$pb.TagNumber(2)
  void clearPagination() => $_clearField(2);
  @$pb.TagNumber(2)
  Pagination ensurePagination() => $_ensure(1);
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
