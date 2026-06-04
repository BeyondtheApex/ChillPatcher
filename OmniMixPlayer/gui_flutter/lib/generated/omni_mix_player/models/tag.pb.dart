// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/tag.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import 'common.pbenum.dart' as $0;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

/// 标签 — 独立实体
class Tag extends $pb.GeneratedMessage {
  factory Tag({
    $core.String? id,
    $core.String? name,
    $core.String? color,
    $core.String? moduleId,
    $0.TagKind? kind,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (name != null) result.name = name;
    if (color != null) result.color = color;
    if (moduleId != null) result.moduleId = moduleId;
    if (kind != null) result.kind = kind;
    return result;
  }

  Tag._();

  factory Tag.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory Tag.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'Tag',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'name')
    ..aOS(3, _omitFieldNames ? '' : 'color')
    ..aOS(4, _omitFieldNames ? '' : 'moduleId')
    ..aE<$0.TagKind>(5, _omitFieldNames ? '' : 'kind',
        enumValues: $0.TagKind.values)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Tag clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  Tag copyWith(void Function(Tag) updates) =>
      super.copyWith((message) => updates(message as Tag)) as Tag;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static Tag create() => Tag._();
  @$core.override
  Tag createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static Tag getDefault() =>
      _defaultInstance ??= $pb.GeneratedMessage.$_defaultFor<Tag>(create);
  static Tag? _defaultInstance;

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
  $core.String get color => $_getSZ(2);
  @$pb.TagNumber(3)
  set color($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasColor() => $_has(2);
  @$pb.TagNumber(3)
  void clearColor() => $_clearField(3);

  @$pb.TagNumber(4)
  $core.String get moduleId => $_getSZ(3);
  @$pb.TagNumber(4)
  set moduleId($core.String value) => $_setString(3, value);
  @$pb.TagNumber(4)
  $core.bool hasModuleId() => $_has(3);
  @$pb.TagNumber(4)
  void clearModuleId() => $_clearField(4);

  @$pb.TagNumber(5)
  $0.TagKind get kind => $_getN(4);
  @$pb.TagNumber(5)
  set kind($0.TagKind value) => $_setField(5, value);
  @$pb.TagNumber(5)
  $core.bool hasKind() => $_has(4);
  @$pb.TagNumber(5)
  void clearKind() => $_clearField(5);
}

/// Tag upsert
class UpsertTagRequest extends $pb.GeneratedMessage {
  factory UpsertTagRequest({
    Tag? tag,
  }) {
    final result = create();
    if (tag != null) result.tag = tag;
    return result;
  }

  UpsertTagRequest._();

  factory UpsertTagRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTagRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTagRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOM<Tag>(1, _omitFieldNames ? '' : 'tag', subBuilder: Tag.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagRequest copyWith(void Function(UpsertTagRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertTagRequest))
          as UpsertTagRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTagRequest create() => UpsertTagRequest._();
  @$core.override
  UpsertTagRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTagRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTagRequest>(create);
  static UpsertTagRequest? _defaultInstance;

  @$pb.TagNumber(1)
  Tag get tag => $_getN(0);
  @$pb.TagNumber(1)
  set tag(Tag value) => $_setField(1, value);
  @$pb.TagNumber(1)
  $core.bool hasTag() => $_has(0);
  @$pb.TagNumber(1)
  void clearTag() => $_clearField(1);
  @$pb.TagNumber(1)
  Tag ensureTag() => $_ensure(0);
}

class UpsertTagResponse extends $pb.GeneratedMessage {
  factory UpsertTagResponse({
    $core.bool? created,
    Tag? tag,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (tag != null) result.tag = tag;
    return result;
  }

  UpsertTagResponse._();

  factory UpsertTagResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTagResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTagResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'created')
    ..aOM<Tag>(2, _omitFieldNames ? '' : 'tag', subBuilder: Tag.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagResponse copyWith(void Function(UpsertTagResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertTagResponse))
          as UpsertTagResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTagResponse create() => UpsertTagResponse._();
  @$core.override
  UpsertTagResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTagResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTagResponse>(create);
  static UpsertTagResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get created => $_getBF(0);
  @$pb.TagNumber(1)
  set created($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasCreated() => $_has(0);
  @$pb.TagNumber(1)
  void clearCreated() => $_clearField(1);

  @$pb.TagNumber(2)
  Tag get tag => $_getN(1);
  @$pb.TagNumber(2)
  set tag(Tag value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasTag() => $_has(1);
  @$pb.TagNumber(2)
  void clearTag() => $_clearField(2);
  @$pb.TagNumber(2)
  Tag ensureTag() => $_ensure(1);
}

/// 批量 Tag upsert
class UpsertTagsRequest extends $pb.GeneratedMessage {
  factory UpsertTagsRequest({
    $core.Iterable<Tag>? tags,
  }) {
    final result = create();
    if (tags != null) result.tags.addAll(tags);
    return result;
  }

  UpsertTagsRequest._();

  factory UpsertTagsRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTagsRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTagsRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<Tag>(1, _omitFieldNames ? '' : 'tags', subBuilder: Tag.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagsRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagsRequest copyWith(void Function(UpsertTagsRequest) updates) =>
      super.copyWith((message) => updates(message as UpsertTagsRequest))
          as UpsertTagsRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTagsRequest create() => UpsertTagsRequest._();
  @$core.override
  UpsertTagsRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTagsRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTagsRequest>(create);
  static UpsertTagsRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<Tag> get tags => $_getList(0);
}

class UpsertTagsResponse extends $pb.GeneratedMessage {
  factory UpsertTagsResponse({
    $core.int? created,
    $core.int? updated,
  }) {
    final result = create();
    if (created != null) result.created = created;
    if (updated != null) result.updated = updated;
    return result;
  }

  UpsertTagsResponse._();

  factory UpsertTagsResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory UpsertTagsResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'UpsertTagsResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aI(1, _omitFieldNames ? '' : 'created')
    ..aI(2, _omitFieldNames ? '' : 'updated')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagsResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  UpsertTagsResponse copyWith(void Function(UpsertTagsResponse) updates) =>
      super.copyWith((message) => updates(message as UpsertTagsResponse))
          as UpsertTagsResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static UpsertTagsResponse create() => UpsertTagsResponse._();
  @$core.override
  UpsertTagsResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static UpsertTagsResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<UpsertTagsResponse>(create);
  static UpsertTagsResponse? _defaultInstance;

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
