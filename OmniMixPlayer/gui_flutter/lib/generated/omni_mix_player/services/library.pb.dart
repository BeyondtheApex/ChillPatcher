// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/library.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:async' as $async;
import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import '../models/album.pb.dart' as $2;
import '../models/playlist.pb.dart' as $4;
import '../models/query.pb.dart' as $1;
import '../models/tag.pb.dart' as $3;
import '../models/track.pb.dart' as $0;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

class GetTrackRequest extends $pb.GeneratedMessage {
  factory GetTrackRequest({
    $core.String? uuid,
  }) {
    final result = create();
    if (uuid != null) result.uuid = uuid;
    return result;
  }

  GetTrackRequest._();

  factory GetTrackRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetTrackRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetTrackRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'uuid')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTrackRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTrackRequest copyWith(void Function(GetTrackRequest) updates) =>
      super.copyWith((message) => updates(message as GetTrackRequest))
          as GetTrackRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetTrackRequest create() => GetTrackRequest._();
  @$core.override
  GetTrackRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetTrackRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetTrackRequest>(create);
  static GetTrackRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get uuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set uuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearUuid() => $_clearField(1);
}

class DeleteTrackRequest extends $pb.GeneratedMessage {
  factory DeleteTrackRequest({
    $core.String? uuid,
  }) {
    final result = create();
    if (uuid != null) result.uuid = uuid;
    return result;
  }

  DeleteTrackRequest._();

  factory DeleteTrackRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeleteTrackRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeleteTrackRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'uuid')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTrackRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTrackRequest copyWith(void Function(DeleteTrackRequest) updates) =>
      super.copyWith((message) => updates(message as DeleteTrackRequest))
          as DeleteTrackRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeleteTrackRequest create() => DeleteTrackRequest._();
  @$core.override
  DeleteTrackRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeleteTrackRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeleteTrackRequest>(create);
  static DeleteTrackRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get uuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set uuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearUuid() => $_clearField(1);
}

class DeleteTrackResponse extends $pb.GeneratedMessage {
  factory DeleteTrackResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  DeleteTrackResponse._();

  factory DeleteTrackResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeleteTrackResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeleteTrackResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTrackResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTrackResponse copyWith(void Function(DeleteTrackResponse) updates) =>
      super.copyWith((message) => updates(message as DeleteTrackResponse))
          as DeleteTrackResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeleteTrackResponse create() => DeleteTrackResponse._();
  @$core.override
  DeleteTrackResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeleteTrackResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeleteTrackResponse>(create);
  static DeleteTrackResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

class GetTrackTagsRequest extends $pb.GeneratedMessage {
  factory GetTrackTagsRequest({
    $core.String? trackUuid,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    return result;
  }

  GetTrackTagsRequest._();

  factory GetTrackTagsRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetTrackTagsRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetTrackTagsRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTrackTagsRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTrackTagsRequest copyWith(void Function(GetTrackTagsRequest) updates) =>
      super.copyWith((message) => updates(message as GetTrackTagsRequest))
          as GetTrackTagsRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetTrackTagsRequest create() => GetTrackTagsRequest._();
  @$core.override
  GetTrackTagsRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetTrackTagsRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetTrackTagsRequest>(create);
  static GetTrackTagsRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get trackUuid => $_getSZ(0);
  @$pb.TagNumber(1)
  set trackUuid($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasTrackUuid() => $_has(0);
  @$pb.TagNumber(1)
  void clearTrackUuid() => $_clearField(1);
}

class GetTrackTagsResponse extends $pb.GeneratedMessage {
  factory GetTrackTagsResponse({
    $core.String? trackUuid,
    $core.Iterable<$core.String>? tagIds,
  }) {
    final result = create();
    if (trackUuid != null) result.trackUuid = trackUuid;
    if (tagIds != null) result.tagIds.addAll(tagIds);
    return result;
  }

  GetTrackTagsResponse._();

  factory GetTrackTagsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetTrackTagsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetTrackTagsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'trackUuid')
    ..pPS(2, _omitFieldNames ? '' : 'tagIds')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTrackTagsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTrackTagsResponse copyWith(void Function(GetTrackTagsResponse) updates) =>
      super.copyWith((message) => updates(message as GetTrackTagsResponse))
          as GetTrackTagsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetTrackTagsResponse create() => GetTrackTagsResponse._();
  @$core.override
  GetTrackTagsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetTrackTagsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetTrackTagsResponse>(create);
  static GetTrackTagsResponse? _defaultInstance;

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

class GetAlbumRequest extends $pb.GeneratedMessage {
  factory GetAlbumRequest({
    $core.String? id,
  }) {
    final result = create();
    if (id != null) result.id = id;
    return result;
  }

  GetAlbumRequest._();

  factory GetAlbumRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetAlbumRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetAlbumRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetAlbumRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetAlbumRequest copyWith(void Function(GetAlbumRequest) updates) =>
      super.copyWith((message) => updates(message as GetAlbumRequest))
          as GetAlbumRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetAlbumRequest create() => GetAlbumRequest._();
  @$core.override
  GetAlbumRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetAlbumRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetAlbumRequest>(create);
  static GetAlbumRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);
}

class DeleteAlbumRequest extends $pb.GeneratedMessage {
  factory DeleteAlbumRequest({
    $core.String? id,
  }) {
    final result = create();
    if (id != null) result.id = id;
    return result;
  }

  DeleteAlbumRequest._();

  factory DeleteAlbumRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeleteAlbumRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeleteAlbumRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteAlbumRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteAlbumRequest copyWith(void Function(DeleteAlbumRequest) updates) =>
      super.copyWith((message) => updates(message as DeleteAlbumRequest))
          as DeleteAlbumRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeleteAlbumRequest create() => DeleteAlbumRequest._();
  @$core.override
  DeleteAlbumRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeleteAlbumRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeleteAlbumRequest>(create);
  static DeleteAlbumRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);
}

class DeleteAlbumResponse extends $pb.GeneratedMessage {
  factory DeleteAlbumResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  DeleteAlbumResponse._();

  factory DeleteAlbumResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeleteAlbumResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeleteAlbumResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteAlbumResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteAlbumResponse copyWith(void Function(DeleteAlbumResponse) updates) =>
      super.copyWith((message) => updates(message as DeleteAlbumResponse))
          as DeleteAlbumResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeleteAlbumResponse create() => DeleteAlbumResponse._();
  @$core.override
  DeleteAlbumResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeleteAlbumResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeleteAlbumResponse>(create);
  static DeleteAlbumResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

class GetTagRequest extends $pb.GeneratedMessage {
  factory GetTagRequest({
    $core.String? id,
  }) {
    final result = create();
    if (id != null) result.id = id;
    return result;
  }

  GetTagRequest._();

  factory GetTagRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetTagRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetTagRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTagRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTagRequest copyWith(void Function(GetTagRequest) updates) =>
      super.copyWith((message) => updates(message as GetTagRequest))
          as GetTagRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetTagRequest create() => GetTagRequest._();
  @$core.override
  GetTagRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetTagRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetTagRequest>(create);
  static GetTagRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);
}

class DeleteTagRequest extends $pb.GeneratedMessage {
  factory DeleteTagRequest({
    $core.String? id,
  }) {
    final result = create();
    if (id != null) result.id = id;
    return result;
  }

  DeleteTagRequest._();

  factory DeleteTagRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeleteTagRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeleteTagRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTagRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTagRequest copyWith(void Function(DeleteTagRequest) updates) =>
      super.copyWith((message) => updates(message as DeleteTagRequest))
          as DeleteTagRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeleteTagRequest create() => DeleteTagRequest._();
  @$core.override
  DeleteTagRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeleteTagRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeleteTagRequest>(create);
  static DeleteTagRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);
}

class DeleteTagResponse extends $pb.GeneratedMessage {
  factory DeleteTagResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  DeleteTagResponse._();

  factory DeleteTagResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeleteTagResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeleteTagResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTagResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeleteTagResponse copyWith(void Function(DeleteTagResponse) updates) =>
      super.copyWith((message) => updates(message as DeleteTagResponse))
          as DeleteTagResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeleteTagResponse create() => DeleteTagResponse._();
  @$core.override
  DeleteTagResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeleteTagResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeleteTagResponse>(create);
  static DeleteTagResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

class GetPlaylistRequest extends $pb.GeneratedMessage {
  factory GetPlaylistRequest({
    $core.String? id,
  }) {
    final result = create();
    if (id != null) result.id = id;
    return result;
  }

  GetPlaylistRequest._();

  factory GetPlaylistRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetPlaylistRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetPlaylistRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistRequest copyWith(void Function(GetPlaylistRequest) updates) =>
      super.copyWith((message) => updates(message as GetPlaylistRequest))
          as GetPlaylistRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetPlaylistRequest create() => GetPlaylistRequest._();
  @$core.override
  GetPlaylistRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetPlaylistRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetPlaylistRequest>(create);
  static GetPlaylistRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);
}

class DeletePlaylistRequest extends $pb.GeneratedMessage {
  factory DeletePlaylistRequest({
    $core.String? id,
  }) {
    final result = create();
    if (id != null) result.id = id;
    return result;
  }

  DeletePlaylistRequest._();

  factory DeletePlaylistRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeletePlaylistRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeletePlaylistRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeletePlaylistRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeletePlaylistRequest copyWith(
          void Function(DeletePlaylistRequest) updates) =>
      super.copyWith((message) => updates(message as DeletePlaylistRequest))
          as DeletePlaylistRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeletePlaylistRequest create() => DeletePlaylistRequest._();
  @$core.override
  DeletePlaylistRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeletePlaylistRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeletePlaylistRequest>(create);
  static DeletePlaylistRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get id => $_getSZ(0);
  @$pb.TagNumber(1)
  set id($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasId() => $_has(0);
  @$pb.TagNumber(1)
  void clearId() => $_clearField(1);
}

class DeletePlaylistResponse extends $pb.GeneratedMessage {
  factory DeletePlaylistResponse({
    $core.bool? success,
  }) {
    final result = create();
    if (success != null) result.success = success;
    return result;
  }

  DeletePlaylistResponse._();

  factory DeletePlaylistResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory DeletePlaylistResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'DeletePlaylistResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeletePlaylistResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  DeletePlaylistResponse copyWith(
          void Function(DeletePlaylistResponse) updates) =>
      super.copyWith((message) => updates(message as DeletePlaylistResponse))
          as DeletePlaylistResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static DeletePlaylistResponse create() => DeletePlaylistResponse._();
  @$core.override
  DeletePlaylistResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static DeletePlaylistResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<DeletePlaylistResponse>(create);
  static DeletePlaylistResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);
}

class GetPlaylistWithEntriesRequest extends $pb.GeneratedMessage {
  factory GetPlaylistWithEntriesRequest({
    $core.String? playlistId,
  }) {
    final result = create();
    if (playlistId != null) result.playlistId = playlistId;
    return result;
  }

  GetPlaylistWithEntriesRequest._();

  factory GetPlaylistWithEntriesRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetPlaylistWithEntriesRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetPlaylistWithEntriesRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'playlistId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistWithEntriesRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistWithEntriesRequest copyWith(
          void Function(GetPlaylistWithEntriesRequest) updates) =>
      super.copyWith(
              (message) => updates(message as GetPlaylistWithEntriesRequest))
          as GetPlaylistWithEntriesRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetPlaylistWithEntriesRequest create() =>
      GetPlaylistWithEntriesRequest._();
  @$core.override
  GetPlaylistWithEntriesRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetPlaylistWithEntriesRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetPlaylistWithEntriesRequest>(create);
  static GetPlaylistWithEntriesRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get playlistId => $_getSZ(0);
  @$pb.TagNumber(1)
  set playlistId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasPlaylistId() => $_has(0);
  @$pb.TagNumber(1)
  void clearPlaylistId() => $_clearField(1);
}

class UnregisterModuleRequest extends $pb.GeneratedMessage {
  factory UnregisterModuleRequest({
    $core.String? moduleId,
  }) {
    final result = create();
    if (moduleId != null) result.moduleId = moduleId;
    return result;
  }

  UnregisterModuleRequest._();

  factory UnregisterModuleRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UnregisterModuleRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UnregisterModuleRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'moduleId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UnregisterModuleRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UnregisterModuleRequest copyWith(
          void Function(UnregisterModuleRequest) updates) =>
      super.copyWith((message) => updates(message as UnregisterModuleRequest))
          as UnregisterModuleRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UnregisterModuleRequest create() => UnregisterModuleRequest._();
  @$core.override
  UnregisterModuleRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UnregisterModuleRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UnregisterModuleRequest>(create);
  static UnregisterModuleRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get moduleId => $_getSZ(0);
  @$pb.TagNumber(1)
  set moduleId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasModuleId() => $_has(0);
  @$pb.TagNumber(1)
  void clearModuleId() => $_clearField(1);
}

class UnregisterModuleResponse extends $pb.GeneratedMessage {
  factory UnregisterModuleResponse({
    $core.bool? success,
    $core.int? tracksRemoved,
    $core.int? albumsRemoved,
    $core.int? tagsRemoved,
    $core.int? playlistsRemoved,
  }) {
    final result = create();
    if (success != null) result.success = success;
    if (tracksRemoved != null) result.tracksRemoved = tracksRemoved;
    if (albumsRemoved != null) result.albumsRemoved = albumsRemoved;
    if (tagsRemoved != null) result.tagsRemoved = tagsRemoved;
    if (playlistsRemoved != null) result.playlistsRemoved = playlistsRemoved;
    return result;
  }

  UnregisterModuleResponse._();

  factory UnregisterModuleResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UnregisterModuleResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UnregisterModuleResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'success')
    ..aI(2, _omitFieldNames ? '' : 'tracksRemoved')
    ..aI(3, _omitFieldNames ? '' : 'albumsRemoved')
    ..aI(4, _omitFieldNames ? '' : 'tagsRemoved')
    ..aI(5, _omitFieldNames ? '' : 'playlistsRemoved')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UnregisterModuleResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UnregisterModuleResponse copyWith(
          void Function(UnregisterModuleResponse) updates) =>
      super.copyWith((message) => updates(message as UnregisterModuleResponse))
          as UnregisterModuleResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UnregisterModuleResponse create() => UnregisterModuleResponse._();
  @$core.override
  UnregisterModuleResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UnregisterModuleResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UnregisterModuleResponse>(create);
  static UnregisterModuleResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get success => $_getBF(0);
  @$pb.TagNumber(1)
  set success($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSuccess() => $_has(0);
  @$pb.TagNumber(1)
  void clearSuccess() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get tracksRemoved => $_getIZ(1);
  @$pb.TagNumber(2)
  set tracksRemoved($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasTracksRemoved() => $_has(1);
  @$pb.TagNumber(2)
  void clearTracksRemoved() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.int get albumsRemoved => $_getIZ(2);
  @$pb.TagNumber(3)
  set albumsRemoved($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasAlbumsRemoved() => $_has(2);
  @$pb.TagNumber(3)
  void clearAlbumsRemoved() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.int get tagsRemoved => $_getIZ(3);
  @$pb.TagNumber(4)
  set tagsRemoved($core.int value) => $_setSignedInt32(3, value);
  @$pb.TagNumber(4)
  $core.bool hasTagsRemoved() => $_has(3);
  @$pb.TagNumber(4)
  void clearTagsRemoved() => $_clearField(4);

  @$pb.TagNumber(5)
  $core.int get playlistsRemoved => $_getIZ(4);
  @$pb.TagNumber(5)
  set playlistsRemoved($core.int value) => $_setSignedInt32(4, value);
  @$pb.TagNumber(5)
  $core.bool hasPlaylistsRemoved() => $_has(4);
  @$pb.TagNumber(5)
  void clearPlaylistsRemoved() => $_clearField(5);
}

/// 音乐库服务 — 平台级别 upsert + 查询
class LibraryServiceApi {
  final $pb.RpcClient _client;

  LibraryServiceApi(this._client);

  /// ── Track ──
  $async.Future<$0.UpsertTrackResponse> upsertTrack(
          $pb.ClientContext? ctx, $0.UpsertTrackRequest request) =>
      _client.invoke<$0.UpsertTrackResponse>(ctx, 'LibraryService',
          'UpsertTrack', request, $0.UpsertTrackResponse());
  $async.Future<$0.UpsertTracksResponse> upsertTracks(
          $pb.ClientContext? ctx, $0.UpsertTracksRequest request) =>
      _client.invoke<$0.UpsertTracksResponse>(ctx, 'LibraryService',
          'UpsertTracks', request, $0.UpsertTracksResponse());
  $async.Future<$0.Track> getTrack(
          $pb.ClientContext? ctx, GetTrackRequest request) =>
      _client.invoke<$0.Track>(
          ctx, 'LibraryService', 'GetTrack', request, $0.Track());
  $async.Future<$1.QueryTracksResponse> queryTracks(
          $pb.ClientContext? ctx, $1.TrackQuery request) =>
      _client.invoke<$1.QueryTracksResponse>(ctx, 'LibraryService',
          'QueryTracks', request, $1.QueryTracksResponse());
  $async.Future<DeleteTrackResponse> deleteTrack(
          $pb.ClientContext? ctx, DeleteTrackRequest request) =>
      _client.invoke<DeleteTrackResponse>(
          ctx, 'LibraryService', 'DeleteTrack', request, DeleteTrackResponse());

  /// ── Track Tags (多对多) ──
  $async.Future<$0.SetTrackTagsResponse> setTrackTags(
          $pb.ClientContext? ctx, $0.SetTrackTagsRequest request) =>
      _client.invoke<$0.SetTrackTagsResponse>(ctx, 'LibraryService',
          'SetTrackTags', request, $0.SetTrackTagsResponse());
  $async.Future<$0.ModifyTrackTagResponse> addTrackTag(
          $pb.ClientContext? ctx, $0.ModifyTrackTagRequest request) =>
      _client.invoke<$0.ModifyTrackTagResponse>(ctx, 'LibraryService',
          'AddTrackTag', request, $0.ModifyTrackTagResponse());
  $async.Future<$0.ModifyTrackTagResponse> removeTrackTag(
          $pb.ClientContext? ctx, $0.ModifyTrackTagRequest request) =>
      _client.invoke<$0.ModifyTrackTagResponse>(ctx, 'LibraryService',
          'RemoveTrackTag', request, $0.ModifyTrackTagResponse());
  $async.Future<GetTrackTagsResponse> getTrackTags(
          $pb.ClientContext? ctx, GetTrackTagsRequest request) =>
      _client.invoke<GetTrackTagsResponse>(ctx, 'LibraryService',
          'GetTrackTags', request, GetTrackTagsResponse());

  /// ── Album ──
  $async.Future<$2.UpsertAlbumResponse> upsertAlbum(
          $pb.ClientContext? ctx, $2.UpsertAlbumRequest request) =>
      _client.invoke<$2.UpsertAlbumResponse>(ctx, 'LibraryService',
          'UpsertAlbum', request, $2.UpsertAlbumResponse());
  $async.Future<$2.UpsertAlbumsResponse> upsertAlbums(
          $pb.ClientContext? ctx, $2.UpsertAlbumsRequest request) =>
      _client.invoke<$2.UpsertAlbumsResponse>(ctx, 'LibraryService',
          'UpsertAlbums', request, $2.UpsertAlbumsResponse());
  $async.Future<$2.Album> getAlbum(
          $pb.ClientContext? ctx, GetAlbumRequest request) =>
      _client.invoke<$2.Album>(
          ctx, 'LibraryService', 'GetAlbum', request, $2.Album());
  $async.Future<$1.QueryAlbumsResponse> queryAlbums(
          $pb.ClientContext? ctx, $1.AlbumQuery request) =>
      _client.invoke<$1.QueryAlbumsResponse>(ctx, 'LibraryService',
          'QueryAlbums', request, $1.QueryAlbumsResponse());
  $async.Future<DeleteAlbumResponse> deleteAlbum(
          $pb.ClientContext? ctx, DeleteAlbumRequest request) =>
      _client.invoke<DeleteAlbumResponse>(
          ctx, 'LibraryService', 'DeleteAlbum', request, DeleteAlbumResponse());

  /// ── Tag ──
  $async.Future<$3.UpsertTagResponse> upsertTag(
          $pb.ClientContext? ctx, $3.UpsertTagRequest request) =>
      _client.invoke<$3.UpsertTagResponse>(
          ctx, 'LibraryService', 'UpsertTag', request, $3.UpsertTagResponse());
  $async.Future<$3.UpsertTagsResponse> upsertTags(
          $pb.ClientContext? ctx, $3.UpsertTagsRequest request) =>
      _client.invoke<$3.UpsertTagsResponse>(ctx, 'LibraryService', 'UpsertTags',
          request, $3.UpsertTagsResponse());
  $async.Future<$3.Tag> getTag($pb.ClientContext? ctx, GetTagRequest request) =>
      _client.invoke<$3.Tag>(
          ctx, 'LibraryService', 'GetTag', request, $3.Tag());
  $async.Future<$1.QueryTagsResponse> queryTags(
          $pb.ClientContext? ctx, $1.TagQuery request) =>
      _client.invoke<$1.QueryTagsResponse>(
          ctx, 'LibraryService', 'QueryTags', request, $1.QueryTagsResponse());
  $async.Future<DeleteTagResponse> deleteTag(
          $pb.ClientContext? ctx, DeleteTagRequest request) =>
      _client.invoke<DeleteTagResponse>(
          ctx, 'LibraryService', 'DeleteTag', request, DeleteTagResponse());

  /// ── Playlist ──
  $async.Future<$4.UpsertPlaylistResponse> upsertPlaylist(
          $pb.ClientContext? ctx, $4.UpsertPlaylistRequest request) =>
      _client.invoke<$4.UpsertPlaylistResponse>(ctx, 'LibraryService',
          'UpsertPlaylist', request, $4.UpsertPlaylistResponse());
  $async.Future<$4.Playlist> getPlaylist(
          $pb.ClientContext? ctx, GetPlaylistRequest request) =>
      _client.invoke<$4.Playlist>(
          ctx, 'LibraryService', 'GetPlaylist', request, $4.Playlist());
  $async.Future<$1.QueryPlaylistsResponse> queryPlaylists(
          $pb.ClientContext? ctx, $1.PlaylistQuery request) =>
      _client.invoke<$1.QueryPlaylistsResponse>(ctx, 'LibraryService',
          'QueryPlaylists', request, $1.QueryPlaylistsResponse());
  $async.Future<DeletePlaylistResponse> deletePlaylist(
          $pb.ClientContext? ctx, DeletePlaylistRequest request) =>
      _client.invoke<DeletePlaylistResponse>(ctx, 'LibraryService',
          'DeletePlaylist', request, DeletePlaylistResponse());

  /// ── Playlist Entries ──
  $async.Future<$4.ReplacePlaylistEntriesResponse> replacePlaylistEntries(
          $pb.ClientContext? ctx, $4.ReplacePlaylistEntriesRequest request) =>
      _client.invoke<$4.ReplacePlaylistEntriesResponse>(
          ctx,
          'LibraryService',
          'ReplacePlaylistEntries',
          request,
          $4.ReplacePlaylistEntriesResponse());
  $async.Future<$4.InsertPlaylistEntryResponse> insertPlaylistEntry(
          $pb.ClientContext? ctx, $4.InsertPlaylistEntryRequest request) =>
      _client.invoke<$4.InsertPlaylistEntryResponse>(ctx, 'LibraryService',
          'InsertPlaylistEntry', request, $4.InsertPlaylistEntryResponse());
  $async.Future<$4.RemovePlaylistEntryResponse> removePlaylistEntry(
          $pb.ClientContext? ctx, $4.RemovePlaylistEntryRequest request) =>
      _client.invoke<$4.RemovePlaylistEntryResponse>(ctx, 'LibraryService',
          'RemovePlaylistEntry', request, $4.RemovePlaylistEntryResponse());
  $async.Future<$4.MovePlaylistEntryResponse> movePlaylistEntry(
          $pb.ClientContext? ctx, $4.MovePlaylistEntryRequest request) =>
      _client.invoke<$4.MovePlaylistEntryResponse>(ctx, 'LibraryService',
          'MovePlaylistEntry', request, $4.MovePlaylistEntryResponse());
  $async.Future<$4.PlaylistWithEntries> getPlaylistWithEntries(
          $pb.ClientContext? ctx, GetPlaylistWithEntriesRequest request) =>
      _client.invoke<$4.PlaylistWithEntries>(ctx, 'LibraryService',
          'GetPlaylistWithEntries', request, $4.PlaylistWithEntries());

  /// ── Module cleanup ──
  $async.Future<UnregisterModuleResponse> unregisterModule(
          $pb.ClientContext? ctx, UnregisterModuleRequest request) =>
      _client.invoke<UnregisterModuleResponse>(ctx, 'LibraryService',
          'UnregisterModule', request, UnregisterModuleResponse());
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
