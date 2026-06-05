// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/playback.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:async' as $async;
import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import '../models/common.pbenum.dart' as $1;
import '../models/instance.pb.dart' as $0;

export 'package:protobuf/protobuf.dart' show GeneratedMessageGenericExtensions;

class PlayRequest extends $pb.GeneratedMessage {
  factory PlayRequest({
    $core.String? instanceId,
    $core.String? uuid,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (uuid != null) result.uuid = uuid;
    return result;
  }

  PlayRequest._();

  factory PlayRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlayRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlayRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOS(2, _omitFieldNames ? '' : 'uuid')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlayRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlayRequest copyWith(void Function(PlayRequest) updates) =>
      super.copyWith((message) => updates(message as PlayRequest))
          as PlayRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlayRequest create() => PlayRequest._();
  @$core.override
  PlayRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlayRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlayRequest>(create);
  static PlayRequest? _defaultInstance;

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
}

class PlayResponse extends $pb.GeneratedMessage {
  factory PlayResponse() => create();

  PlayResponse._();

  factory PlayResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlayResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlayResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlayResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlayResponse copyWith(void Function(PlayResponse) updates) =>
      super.copyWith((message) => updates(message as PlayResponse))
          as PlayResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlayResponse create() => PlayResponse._();
  @$core.override
  PlayResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlayResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlayResponse>(create);
  static PlayResponse? _defaultInstance;
}

class PauseRequest extends $pb.GeneratedMessage {
  factory PauseRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  PauseRequest._();

  factory PauseRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PauseRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PauseRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PauseRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PauseRequest copyWith(void Function(PauseRequest) updates) =>
      super.copyWith((message) => updates(message as PauseRequest))
          as PauseRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PauseRequest create() => PauseRequest._();
  @$core.override
  PauseRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PauseRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PauseRequest>(create);
  static PauseRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class PauseResponse extends $pb.GeneratedMessage {
  factory PauseResponse() => create();

  PauseResponse._();

  factory PauseResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PauseResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PauseResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PauseResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PauseResponse copyWith(void Function(PauseResponse) updates) =>
      super.copyWith((message) => updates(message as PauseResponse))
          as PauseResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PauseResponse create() => PauseResponse._();
  @$core.override
  PauseResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PauseResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PauseResponse>(create);
  static PauseResponse? _defaultInstance;
}

class ResumeRequest extends $pb.GeneratedMessage {
  factory ResumeRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  ResumeRequest._();

  factory ResumeRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ResumeRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ResumeRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ResumeRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ResumeRequest copyWith(void Function(ResumeRequest) updates) =>
      super.copyWith((message) => updates(message as ResumeRequest))
          as ResumeRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ResumeRequest create() => ResumeRequest._();
  @$core.override
  ResumeRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ResumeRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ResumeRequest>(create);
  static ResumeRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class ResumeResponse extends $pb.GeneratedMessage {
  factory ResumeResponse() => create();

  ResumeResponse._();

  factory ResumeResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ResumeResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ResumeResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ResumeResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ResumeResponse copyWith(void Function(ResumeResponse) updates) =>
      super.copyWith((message) => updates(message as ResumeResponse))
          as ResumeResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ResumeResponse create() => ResumeResponse._();
  @$core.override
  ResumeResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ResumeResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ResumeResponse>(create);
  static ResumeResponse? _defaultInstance;
}

class ToggleRequest extends $pb.GeneratedMessage {
  factory ToggleRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  ToggleRequest._();

  factory ToggleRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ToggleRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ToggleRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ToggleRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ToggleRequest copyWith(void Function(ToggleRequest) updates) =>
      super.copyWith((message) => updates(message as ToggleRequest))
          as ToggleRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ToggleRequest create() => ToggleRequest._();
  @$core.override
  ToggleRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ToggleRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ToggleRequest>(create);
  static ToggleRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class ToggleResponse extends $pb.GeneratedMessage {
  factory ToggleResponse() => create();

  ToggleResponse._();

  factory ToggleResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ToggleResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ToggleResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ToggleResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ToggleResponse copyWith(void Function(ToggleResponse) updates) =>
      super.copyWith((message) => updates(message as ToggleResponse))
          as ToggleResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ToggleResponse create() => ToggleResponse._();
  @$core.override
  ToggleResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ToggleResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ToggleResponse>(create);
  static ToggleResponse? _defaultInstance;
}

class NextRequest extends $pb.GeneratedMessage {
  factory NextRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  NextRequest._();

  factory NextRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory NextRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'NextRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  NextRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  NextRequest copyWith(void Function(NextRequest) updates) =>
      super.copyWith((message) => updates(message as NextRequest))
          as NextRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static NextRequest create() => NextRequest._();
  @$core.override
  NextRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static NextRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<NextRequest>(create);
  static NextRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class NextResponse extends $pb.GeneratedMessage {
  factory NextResponse() => create();

  NextResponse._();

  factory NextResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory NextResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'NextResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  NextResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  NextResponse copyWith(void Function(NextResponse) updates) =>
      super.copyWith((message) => updates(message as NextResponse))
          as NextResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static NextResponse create() => NextResponse._();
  @$core.override
  NextResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static NextResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<NextResponse>(create);
  static NextResponse? _defaultInstance;
}

class PrevRequest extends $pb.GeneratedMessage {
  factory PrevRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  PrevRequest._();

  factory PrevRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PrevRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PrevRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PrevRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PrevRequest copyWith(void Function(PrevRequest) updates) =>
      super.copyWith((message) => updates(message as PrevRequest))
          as PrevRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PrevRequest create() => PrevRequest._();
  @$core.override
  PrevRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PrevRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PrevRequest>(create);
  static PrevRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class PrevResponse extends $pb.GeneratedMessage {
  factory PrevResponse() => create();

  PrevResponse._();

  factory PrevResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PrevResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PrevResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PrevResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PrevResponse copyWith(void Function(PrevResponse) updates) =>
      super.copyWith((message) => updates(message as PrevResponse))
          as PrevResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PrevResponse create() => PrevResponse._();
  @$core.override
  PrevResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PrevResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PrevResponse>(create);
  static PrevResponse? _defaultInstance;
}

class SeekRequest extends $pb.GeneratedMessage {
  factory SeekRequest({
    $core.String? instanceId,
    $core.double? position,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (position != null) result.position = position;
    return result;
  }

  SeekRequest._();

  factory SeekRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SeekRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SeekRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aD(2, _omitFieldNames ? '' : 'position', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SeekRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SeekRequest copyWith(void Function(SeekRequest) updates) =>
      super.copyWith((message) => updates(message as SeekRequest))
          as SeekRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SeekRequest create() => SeekRequest._();
  @$core.override
  SeekRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SeekRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SeekRequest>(create);
  static SeekRequest? _defaultInstance;

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

class SeekResponse extends $pb.GeneratedMessage {
  factory SeekResponse() => create();

  SeekResponse._();

  factory SeekResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SeekResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SeekResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SeekResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SeekResponse copyWith(void Function(SeekResponse) updates) =>
      super.copyWith((message) => updates(message as SeekResponse))
          as SeekResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SeekResponse create() => SeekResponse._();
  @$core.override
  SeekResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SeekResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SeekResponse>(create);
  static SeekResponse? _defaultInstance;
}

class StopRequest extends $pb.GeneratedMessage {
  factory StopRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  StopRequest._();

  factory StopRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory StopRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'StopRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  StopRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  StopRequest copyWith(void Function(StopRequest) updates) =>
      super.copyWith((message) => updates(message as StopRequest))
          as StopRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static StopRequest create() => StopRequest._();
  @$core.override
  StopRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static StopRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<StopRequest>(create);
  static StopRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class StopResponse extends $pb.GeneratedMessage {
  factory StopResponse() => create();

  StopResponse._();

  factory StopResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory StopResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'StopResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  StopResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  StopResponse copyWith(void Function(StopResponse) updates) =>
      super.copyWith((message) => updates(message as StopResponse))
          as StopResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static StopResponse create() => StopResponse._();
  @$core.override
  StopResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static StopResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<StopResponse>(create);
  static StopResponse? _defaultInstance;
}

class SetVolumeRequest extends $pb.GeneratedMessage {
  factory SetVolumeRequest({
    $core.String? instanceId,
    $core.double? volume,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (volume != null) result.volume = volume;
    return result;
  }

  SetVolumeRequest._();

  factory SetVolumeRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetVolumeRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetVolumeRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aD(2, _omitFieldNames ? '' : 'volume', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetVolumeRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetVolumeRequest copyWith(void Function(SetVolumeRequest) updates) =>
      super.copyWith((message) => updates(message as SetVolumeRequest))
          as SetVolumeRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetVolumeRequest create() => SetVolumeRequest._();
  @$core.override
  SetVolumeRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetVolumeRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetVolumeRequest>(create);
  static SetVolumeRequest? _defaultInstance;

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

class SetVolumeResponse extends $pb.GeneratedMessage {
  factory SetVolumeResponse({
    $core.bool? saved,
  }) {
    final result = create();
    if (saved != null) result.saved = saved;
    return result;
  }

  SetVolumeResponse._();

  factory SetVolumeResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetVolumeResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetVolumeResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'saved')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetVolumeResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetVolumeResponse copyWith(void Function(SetVolumeResponse) updates) =>
      super.copyWith((message) => updates(message as SetVolumeResponse))
          as SetVolumeResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetVolumeResponse create() => SetVolumeResponse._();
  @$core.override
  SetVolumeResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetVolumeResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetVolumeResponse>(create);
  static SetVolumeResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get saved => $_getBF(0);
  @$pb.TagNumber(1)
  set saved($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSaved() => $_has(0);
  @$pb.TagNumber(1)
  void clearSaved() => $_clearField(1);
}

class GetVolumeRequest extends $pb.GeneratedMessage {
  factory GetVolumeRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetVolumeRequest._();

  factory GetVolumeRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetVolumeRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetVolumeRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetVolumeRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetVolumeRequest copyWith(void Function(GetVolumeRequest) updates) =>
      super.copyWith((message) => updates(message as GetVolumeRequest))
          as GetVolumeRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetVolumeRequest create() => GetVolumeRequest._();
  @$core.override
  GetVolumeRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetVolumeRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetVolumeRequest>(create);
  static GetVolumeRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class GetVolumeResponse extends $pb.GeneratedMessage {
  factory GetVolumeResponse({
    $core.double? volume,
  }) {
    final result = create();
    if (volume != null) result.volume = volume;
    return result;
  }

  GetVolumeResponse._();

  factory GetVolumeResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetVolumeResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetVolumeResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aD(1, _omitFieldNames ? '' : 'volume', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetVolumeResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetVolumeResponse copyWith(void Function(GetVolumeResponse) updates) =>
      super.copyWith((message) => updates(message as GetVolumeResponse))
          as GetVolumeResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetVolumeResponse create() => GetVolumeResponse._();
  @$core.override
  GetVolumeResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetVolumeResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetVolumeResponse>(create);
  static GetVolumeResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.double get volume => $_getN(0);
  @$pb.TagNumber(1)
  set volume($core.double value) => $_setFloat(0, value);
  @$pb.TagNumber(1)
  $core.bool hasVolume() => $_has(0);
  @$pb.TagNumber(1)
  void clearVolume() => $_clearField(1);
}

class SetTargetLatencyRequest extends $pb.GeneratedMessage {
  factory SetTargetLatencyRequest({
    $core.String? instanceId,
    $core.double? latency,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (latency != null) result.latency = latency;
    return result;
  }

  SetTargetLatencyRequest._();

  factory SetTargetLatencyRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetTargetLatencyRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetTargetLatencyRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aD(2, _omitFieldNames ? '' : 'latency', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTargetLatencyRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTargetLatencyRequest copyWith(
          void Function(SetTargetLatencyRequest) updates) =>
      super.copyWith((message) => updates(message as SetTargetLatencyRequest))
          as SetTargetLatencyRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetTargetLatencyRequest create() => SetTargetLatencyRequest._();
  @$core.override
  SetTargetLatencyRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetTargetLatencyRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetTargetLatencyRequest>(create);
  static SetTargetLatencyRequest? _defaultInstance;

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

class SetTargetLatencyResponse extends $pb.GeneratedMessage {
  factory SetTargetLatencyResponse({
    $core.bool? saved,
  }) {
    final result = create();
    if (saved != null) result.saved = saved;
    return result;
  }

  SetTargetLatencyResponse._();

  factory SetTargetLatencyResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetTargetLatencyResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetTargetLatencyResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'saved')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTargetLatencyResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetTargetLatencyResponse copyWith(
          void Function(SetTargetLatencyResponse) updates) =>
      super.copyWith((message) => updates(message as SetTargetLatencyResponse))
          as SetTargetLatencyResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetTargetLatencyResponse create() => SetTargetLatencyResponse._();
  @$core.override
  SetTargetLatencyResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetTargetLatencyResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetTargetLatencyResponse>(create);
  static SetTargetLatencyResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get saved => $_getBF(0);
  @$pb.TagNumber(1)
  set saved($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSaved() => $_has(0);
  @$pb.TagNumber(1)
  void clearSaved() => $_clearField(1);
}

class GetTargetLatencyRequest extends $pb.GeneratedMessage {
  factory GetTargetLatencyRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetTargetLatencyRequest._();

  factory GetTargetLatencyRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetTargetLatencyRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetTargetLatencyRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTargetLatencyRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTargetLatencyRequest copyWith(
          void Function(GetTargetLatencyRequest) updates) =>
      super.copyWith((message) => updates(message as GetTargetLatencyRequest))
          as GetTargetLatencyRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetTargetLatencyRequest create() => GetTargetLatencyRequest._();
  @$core.override
  GetTargetLatencyRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetTargetLatencyRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetTargetLatencyRequest>(create);
  static GetTargetLatencyRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class GetTargetLatencyResponse extends $pb.GeneratedMessage {
  factory GetTargetLatencyResponse({
    $core.double? latency,
  }) {
    final result = create();
    if (latency != null) result.latency = latency;
    return result;
  }

  GetTargetLatencyResponse._();

  factory GetTargetLatencyResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetTargetLatencyResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetTargetLatencyResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aD(1, _omitFieldNames ? '' : 'latency', fieldType: $pb.PbFieldType.OF)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTargetLatencyResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetTargetLatencyResponse copyWith(
          void Function(GetTargetLatencyResponse) updates) =>
      super.copyWith((message) => updates(message as GetTargetLatencyResponse))
          as GetTargetLatencyResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetTargetLatencyResponse create() => GetTargetLatencyResponse._();
  @$core.override
  GetTargetLatencyResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetTargetLatencyResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetTargetLatencyResponse>(create);
  static GetTargetLatencyResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.double get latency => $_getN(0);
  @$pb.TagNumber(1)
  set latency($core.double value) => $_setFloat(0, value);
  @$pb.TagNumber(1)
  $core.bool hasLatency() => $_has(0);
  @$pb.TagNumber(1)
  void clearLatency() => $_clearField(1);
}

class SetShuffleRequest extends $pb.GeneratedMessage {
  factory SetShuffleRequest({
    $core.String? instanceId,
    $core.bool? enabled,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (enabled != null) result.enabled = enabled;
    return result;
  }

  SetShuffleRequest._();

  factory SetShuffleRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetShuffleRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetShuffleRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOB(2, _omitFieldNames ? '' : 'enabled')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetShuffleRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetShuffleRequest copyWith(void Function(SetShuffleRequest) updates) =>
      super.copyWith((message) => updates(message as SetShuffleRequest))
          as SetShuffleRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetShuffleRequest create() => SetShuffleRequest._();
  @$core.override
  SetShuffleRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetShuffleRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetShuffleRequest>(create);
  static SetShuffleRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.bool get enabled => $_getBF(1);
  @$pb.TagNumber(2)
  set enabled($core.bool value) => $_setBool(1, value);
  @$pb.TagNumber(2)
  $core.bool hasEnabled() => $_has(1);
  @$pb.TagNumber(2)
  void clearEnabled() => $_clearField(2);
}

class SetShuffleResponse extends $pb.GeneratedMessage {
  factory SetShuffleResponse() => create();

  SetShuffleResponse._();

  factory SetShuffleResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetShuffleResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetShuffleResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetShuffleResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetShuffleResponse copyWith(void Function(SetShuffleResponse) updates) =>
      super.copyWith((message) => updates(message as SetShuffleResponse))
          as SetShuffleResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetShuffleResponse create() => SetShuffleResponse._();
  @$core.override
  SetShuffleResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetShuffleResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetShuffleResponse>(create);
  static SetShuffleResponse? _defaultInstance;
}

class SetRepeatModeRequest extends $pb.GeneratedMessage {
  factory SetRepeatModeRequest({
    $core.String? instanceId,
    $1.RepeatMode? mode,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (mode != null) result.mode = mode;
    return result;
  }

  SetRepeatModeRequest._();

  factory SetRepeatModeRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetRepeatModeRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetRepeatModeRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aE<$1.RepeatMode>(2, _omitFieldNames ? '' : 'mode',
        enumValues: $1.RepeatMode.values)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetRepeatModeRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetRepeatModeRequest copyWith(void Function(SetRepeatModeRequest) updates) =>
      super.copyWith((message) => updates(message as SetRepeatModeRequest))
          as SetRepeatModeRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetRepeatModeRequest create() => SetRepeatModeRequest._();
  @$core.override
  SetRepeatModeRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetRepeatModeRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetRepeatModeRequest>(create);
  static SetRepeatModeRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $1.RepeatMode get mode => $_getN(1);
  @$pb.TagNumber(2)
  set mode($1.RepeatMode value) => $_setField(2, value);
  @$pb.TagNumber(2)
  $core.bool hasMode() => $_has(1);
  @$pb.TagNumber(2)
  void clearMode() => $_clearField(2);
}

class SetRepeatModeResponse extends $pb.GeneratedMessage {
  factory SetRepeatModeResponse() => create();

  SetRepeatModeResponse._();

  factory SetRepeatModeResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetRepeatModeResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetRepeatModeResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetRepeatModeResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetRepeatModeResponse copyWith(
          void Function(SetRepeatModeResponse) updates) =>
      super.copyWith((message) => updates(message as SetRepeatModeResponse))
          as SetRepeatModeResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetRepeatModeResponse create() => SetRepeatModeResponse._();
  @$core.override
  SetRepeatModeResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetRepeatModeResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetRepeatModeResponse>(create);
  static SetRepeatModeResponse? _defaultInstance;
}

class QueueTrack extends $pb.GeneratedMessage {
  factory QueueTrack({
    $core.int? index,
    $core.String? uuid,
    $core.String? title,
    $core.String? artist,
    $core.String? albumId,
    $core.double? duration,
    $core.String? moduleId,
    $core.String? coverUri,
  }) {
    final result = create();
    if (index != null) result.index = index;
    if (uuid != null) result.uuid = uuid;
    if (title != null) result.title = title;
    if (artist != null) result.artist = artist;
    if (albumId != null) result.albumId = albumId;
    if (duration != null) result.duration = duration;
    if (moduleId != null) result.moduleId = moduleId;
    if (coverUri != null) result.coverUri = coverUri;
    return result;
  }

  QueueTrack._();

  factory QueueTrack.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory QueueTrack.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'QueueTrack',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aI(1, _omitFieldNames ? '' : 'index')
    ..aOS(2, _omitFieldNames ? '' : 'uuid')
    ..aOS(3, _omitFieldNames ? '' : 'title')
    ..aOS(4, _omitFieldNames ? '' : 'artist')
    ..aOS(5, _omitFieldNames ? '' : 'albumId')
    ..aD(6, _omitFieldNames ? '' : 'duration', fieldType: $pb.PbFieldType.OF)
    ..aOS(7, _omitFieldNames ? '' : 'moduleId')
    ..aOS(8, _omitFieldNames ? '' : 'coverUri')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueueTrack clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  QueueTrack copyWith(void Function(QueueTrack) updates) =>
      super.copyWith((message) => updates(message as QueueTrack)) as QueueTrack;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static QueueTrack create() => QueueTrack._();
  @$core.override
  QueueTrack createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static QueueTrack getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<QueueTrack>(create);
  static QueueTrack? _defaultInstance;

  @$pb.TagNumber(1)
  $core.int get index => $_getIZ(0);
  @$pb.TagNumber(1)
  set index($core.int value) => $_setSignedInt32(0, value);
  @$pb.TagNumber(1)
  $core.bool hasIndex() => $_has(0);
  @$pb.TagNumber(1)
  void clearIndex() => $_clearField(1);

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

  @$pb.TagNumber(8)
  $core.String get coverUri => $_getSZ(7);
  @$pb.TagNumber(8)
  set coverUri($core.String value) => $_setString(7, value);
  @$pb.TagNumber(8)
  $core.bool hasCoverUri() => $_has(7);
  @$pb.TagNumber(8)
  void clearCoverUri() => $_clearField(8);
}

class GetQueueRequest extends $pb.GeneratedMessage {
  factory GetQueueRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetQueueRequest._();

  factory GetQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetQueueRequest copyWith(void Function(GetQueueRequest) updates) =>
      super.copyWith((message) => updates(message as GetQueueRequest))
          as GetQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetQueueRequest create() => GetQueueRequest._();
  @$core.override
  GetQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetQueueRequest>(create);
  static GetQueueRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class GetQueueResponse extends $pb.GeneratedMessage {
  factory GetQueueResponse({
    $core.Iterable<QueueTrack>? queue,
  }) {
    final result = create();
    if (queue != null) result.queue.addAll(queue);
    return result;
  }

  GetQueueResponse._();

  factory GetQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<QueueTrack>(1, _omitFieldNames ? '' : 'queue',
        subBuilder: QueueTrack.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetQueueResponse copyWith(void Function(GetQueueResponse) updates) =>
      super.copyWith((message) => updates(message as GetQueueResponse))
          as GetQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetQueueResponse create() => GetQueueResponse._();
  @$core.override
  GetQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetQueueResponse>(create);
  static GetQueueResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<QueueTrack> get queue => $_getList(0);
}

class AddToQueueRequest extends $pb.GeneratedMessage {
  factory AddToQueueRequest({
    $core.String? instanceId,
    $core.String? uuid,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (uuid != null) result.uuid = uuid;
    return result;
  }

  AddToQueueRequest._();

  factory AddToQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory AddToQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'AddToQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOS(2, _omitFieldNames ? '' : 'uuid')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  AddToQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  AddToQueueRequest copyWith(void Function(AddToQueueRequest) updates) =>
      super.copyWith((message) => updates(message as AddToQueueRequest))
          as AddToQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static AddToQueueRequest create() => AddToQueueRequest._();
  @$core.override
  AddToQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static AddToQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<AddToQueueRequest>(create);
  static AddToQueueRequest? _defaultInstance;

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
}

class AddToQueueResponse extends $pb.GeneratedMessage {
  factory AddToQueueResponse() => create();

  AddToQueueResponse._();

  factory AddToQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory AddToQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'AddToQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  AddToQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  AddToQueueResponse copyWith(void Function(AddToQueueResponse) updates) =>
      super.copyWith((message) => updates(message as AddToQueueResponse))
          as AddToQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static AddToQueueResponse create() => AddToQueueResponse._();
  @$core.override
  AddToQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static AddToQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<AddToQueueResponse>(create);
  static AddToQueueResponse? _defaultInstance;
}

class InsertIntoQueueRequest extends $pb.GeneratedMessage {
  factory InsertIntoQueueRequest({
    $core.String? instanceId,
    $core.Iterable<$core.String>? uuids,
    $core.int? index,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (uuids != null) result.uuids.addAll(uuids);
    if (index != null) result.index = index;
    return result;
  }

  InsertIntoQueueRequest._();

  factory InsertIntoQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InsertIntoQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InsertIntoQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..pPS(2, _omitFieldNames ? '' : 'uuids')
    ..aI(3, _omitFieldNames ? '' : 'index')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertIntoQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertIntoQueueRequest copyWith(
          void Function(InsertIntoQueueRequest) updates) =>
      super.copyWith((message) => updates(message as InsertIntoQueueRequest))
          as InsertIntoQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InsertIntoQueueRequest create() => InsertIntoQueueRequest._();
  @$core.override
  InsertIntoQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InsertIntoQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InsertIntoQueueRequest>(create);
  static InsertIntoQueueRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<$core.String> get uuids => $_getList(1);

  @$pb.TagNumber(3)
  $core.int get index => $_getIZ(2);
  @$pb.TagNumber(3)
  set index($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasIndex() => $_has(2);
  @$pb.TagNumber(3)
  void clearIndex() => $_clearField(3);
}

class InsertIntoQueueResponse extends $pb.GeneratedMessage {
  factory InsertIntoQueueResponse() => create();

  InsertIntoQueueResponse._();

  factory InsertIntoQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory InsertIntoQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'InsertIntoQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertIntoQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  InsertIntoQueueResponse copyWith(
          void Function(InsertIntoQueueResponse) updates) =>
      super.copyWith((message) => updates(message as InsertIntoQueueResponse))
          as InsertIntoQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static InsertIntoQueueResponse create() => InsertIntoQueueResponse._();
  @$core.override
  InsertIntoQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static InsertIntoQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<InsertIntoQueueResponse>(create);
  static InsertIntoQueueResponse? _defaultInstance;
}

class SetQueueRequest extends $pb.GeneratedMessage {
  factory SetQueueRequest({
    $core.String? instanceId,
    $core.Iterable<$core.String>? uuids,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (uuids != null) result.uuids.addAll(uuids);
    return result;
  }

  SetQueueRequest._();

  factory SetQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..pPS(2, _omitFieldNames ? '' : 'uuids')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetQueueRequest copyWith(void Function(SetQueueRequest) updates) =>
      super.copyWith((message) => updates(message as SetQueueRequest))
          as SetQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetQueueRequest create() => SetQueueRequest._();
  @$core.override
  SetQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetQueueRequest>(create);
  static SetQueueRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<$core.String> get uuids => $_getList(1);
}

class SetQueueResponse extends $pb.GeneratedMessage {
  factory SetQueueResponse() => create();

  SetQueueResponse._();

  factory SetQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetQueueResponse copyWith(void Function(SetQueueResponse) updates) =>
      super.copyWith((message) => updates(message as SetQueueResponse))
          as SetQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetQueueResponse create() => SetQueueResponse._();
  @$core.override
  SetQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetQueueResponse>(create);
  static SetQueueResponse? _defaultInstance;
}

enum RemoveFromQueueRequest_Target { index_, uuid, notSet }

class RemoveFromQueueRequest extends $pb.GeneratedMessage {
  factory RemoveFromQueueRequest({
    $core.String? instanceId,
    $core.int? index,
    $core.String? uuid,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (index != null) result.index = index;
    if (uuid != null) result.uuid = uuid;
    return result;
  }

  RemoveFromQueueRequest._();

  factory RemoveFromQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory RemoveFromQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static const $core.Map<$core.int, RemoveFromQueueRequest_Target>
      _RemoveFromQueueRequest_TargetByTag = {
    2: RemoveFromQueueRequest_Target.index_,
    3: RemoveFromQueueRequest_Target.uuid,
    0: RemoveFromQueueRequest_Target.notSet
  };
  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'RemoveFromQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..oo(0, [2, 3])
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aI(2, _omitFieldNames ? '' : 'index')
    ..aOS(3, _omitFieldNames ? '' : 'uuid')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromQueueRequest copyWith(
          void Function(RemoveFromQueueRequest) updates) =>
      super.copyWith((message) => updates(message as RemoveFromQueueRequest))
          as RemoveFromQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static RemoveFromQueueRequest create() => RemoveFromQueueRequest._();
  @$core.override
  RemoveFromQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static RemoveFromQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<RemoveFromQueueRequest>(create);
  static RemoveFromQueueRequest? _defaultInstance;

  @$pb.TagNumber(2)
  @$pb.TagNumber(3)
  RemoveFromQueueRequest_Target whichTarget() =>
      _RemoveFromQueueRequest_TargetByTag[$_whichOneof(0)]!;
  @$pb.TagNumber(2)
  @$pb.TagNumber(3)
  void clearTarget() => $_clearField($_whichOneof(0));

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get index => $_getIZ(1);
  @$pb.TagNumber(2)
  set index($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasIndex() => $_has(1);
  @$pb.TagNumber(2)
  void clearIndex() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.String get uuid => $_getSZ(2);
  @$pb.TagNumber(3)
  set uuid($core.String value) => $_setString(2, value);
  @$pb.TagNumber(3)
  $core.bool hasUuid() => $_has(2);
  @$pb.TagNumber(3)
  void clearUuid() => $_clearField(3);
}

class RemoveFromQueueResponse extends $pb.GeneratedMessage {
  factory RemoveFromQueueResponse() => create();

  RemoveFromQueueResponse._();

  factory RemoveFromQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory RemoveFromQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'RemoveFromQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromQueueResponse copyWith(
          void Function(RemoveFromQueueResponse) updates) =>
      super.copyWith((message) => updates(message as RemoveFromQueueResponse))
          as RemoveFromQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static RemoveFromQueueResponse create() => RemoveFromQueueResponse._();
  @$core.override
  RemoveFromQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static RemoveFromQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<RemoveFromQueueResponse>(create);
  static RemoveFromQueueResponse? _defaultInstance;
}

class MoveInQueueRequest extends $pb.GeneratedMessage {
  factory MoveInQueueRequest({
    $core.String? instanceId,
    $core.int? fromIndex,
    $core.int? toIndex,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (fromIndex != null) result.fromIndex = fromIndex;
    if (toIndex != null) result.toIndex = toIndex;
    return result;
  }

  MoveInQueueRequest._();

  factory MoveInQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory MoveInQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'MoveInQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aI(2, _omitFieldNames ? '' : 'fromIndex')
    ..aI(3, _omitFieldNames ? '' : 'toIndex')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInQueueRequest copyWith(void Function(MoveInQueueRequest) updates) =>
      super.copyWith((message) => updates(message as MoveInQueueRequest))
          as MoveInQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static MoveInQueueRequest create() => MoveInQueueRequest._();
  @$core.override
  MoveInQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static MoveInQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<MoveInQueueRequest>(create);
  static MoveInQueueRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get fromIndex => $_getIZ(1);
  @$pb.TagNumber(2)
  set fromIndex($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasFromIndex() => $_has(1);
  @$pb.TagNumber(2)
  void clearFromIndex() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.int get toIndex => $_getIZ(2);
  @$pb.TagNumber(3)
  set toIndex($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasToIndex() => $_has(2);
  @$pb.TagNumber(3)
  void clearToIndex() => $_clearField(3);
}

class MoveInQueueResponse extends $pb.GeneratedMessage {
  factory MoveInQueueResponse() => create();

  MoveInQueueResponse._();

  factory MoveInQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory MoveInQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'MoveInQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInQueueResponse copyWith(void Function(MoveInQueueResponse) updates) =>
      super.copyWith((message) => updates(message as MoveInQueueResponse))
          as MoveInQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static MoveInQueueResponse create() => MoveInQueueResponse._();
  @$core.override
  MoveInQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static MoveInQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<MoveInQueueResponse>(create);
  static MoveInQueueResponse? _defaultInstance;
}

class ClearQueueRequest extends $pb.GeneratedMessage {
  factory ClearQueueRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  ClearQueueRequest._();

  factory ClearQueueRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ClearQueueRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ClearQueueRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearQueueRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearQueueRequest copyWith(void Function(ClearQueueRequest) updates) =>
      super.copyWith((message) => updates(message as ClearQueueRequest))
          as ClearQueueRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ClearQueueRequest create() => ClearQueueRequest._();
  @$core.override
  ClearQueueRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ClearQueueRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ClearQueueRequest>(create);
  static ClearQueueRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class ClearQueueResponse extends $pb.GeneratedMessage {
  factory ClearQueueResponse() => create();

  ClearQueueResponse._();

  factory ClearQueueResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ClearQueueResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ClearQueueResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearQueueResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearQueueResponse copyWith(void Function(ClearQueueResponse) updates) =>
      super.copyWith((message) => updates(message as ClearQueueResponse))
          as ClearQueueResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ClearQueueResponse create() => ClearQueueResponse._();
  @$core.override
  ClearQueueResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ClearQueueResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ClearQueueResponse>(create);
  static ClearQueueResponse? _defaultInstance;
}

class GetHistoryRequest extends $pb.GeneratedMessage {
  factory GetHistoryRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetHistoryRequest._();

  factory GetHistoryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetHistoryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetHistoryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetHistoryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetHistoryRequest copyWith(void Function(GetHistoryRequest) updates) =>
      super.copyWith((message) => updates(message as GetHistoryRequest))
          as GetHistoryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetHistoryRequest create() => GetHistoryRequest._();
  @$core.override
  GetHistoryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetHistoryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetHistoryRequest>(create);
  static GetHistoryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class GetHistoryResponse extends $pb.GeneratedMessage {
  factory GetHistoryResponse({
    $core.Iterable<QueueTrack>? history,
  }) {
    final result = create();
    if (history != null) result.history.addAll(history);
    return result;
  }

  GetHistoryResponse._();

  factory GetHistoryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetHistoryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetHistoryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<QueueTrack>(1, _omitFieldNames ? '' : 'history',
        subBuilder: QueueTrack.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetHistoryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetHistoryResponse copyWith(void Function(GetHistoryResponse) updates) =>
      super.copyWith((message) => updates(message as GetHistoryResponse))
          as GetHistoryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetHistoryResponse create() => GetHistoryResponse._();
  @$core.override
  GetHistoryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetHistoryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetHistoryResponse>(create);
  static GetHistoryResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<QueueTrack> get history => $_getList(0);
}

class RemoveFromHistoryRequest extends $pb.GeneratedMessage {
  factory RemoveFromHistoryRequest({
    $core.String? instanceId,
    $core.int? index,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (index != null) result.index = index;
    return result;
  }

  RemoveFromHistoryRequest._();

  factory RemoveFromHistoryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory RemoveFromHistoryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'RemoveFromHistoryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aI(2, _omitFieldNames ? '' : 'index')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromHistoryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromHistoryRequest copyWith(
          void Function(RemoveFromHistoryRequest) updates) =>
      super.copyWith((message) => updates(message as RemoveFromHistoryRequest))
          as RemoveFromHistoryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static RemoveFromHistoryRequest create() => RemoveFromHistoryRequest._();
  @$core.override
  RemoveFromHistoryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static RemoveFromHistoryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<RemoveFromHistoryRequest>(create);
  static RemoveFromHistoryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get index => $_getIZ(1);
  @$pb.TagNumber(2)
  set index($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasIndex() => $_has(1);
  @$pb.TagNumber(2)
  void clearIndex() => $_clearField(2);
}

class RemoveFromHistoryResponse extends $pb.GeneratedMessage {
  factory RemoveFromHistoryResponse() => create();

  RemoveFromHistoryResponse._();

  factory RemoveFromHistoryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory RemoveFromHistoryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'RemoveFromHistoryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromHistoryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  RemoveFromHistoryResponse copyWith(
          void Function(RemoveFromHistoryResponse) updates) =>
      super.copyWith((message) => updates(message as RemoveFromHistoryResponse))
          as RemoveFromHistoryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static RemoveFromHistoryResponse create() => RemoveFromHistoryResponse._();
  @$core.override
  RemoveFromHistoryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static RemoveFromHistoryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<RemoveFromHistoryResponse>(create);
  static RemoveFromHistoryResponse? _defaultInstance;
}

class MoveInHistoryRequest extends $pb.GeneratedMessage {
  factory MoveInHistoryRequest({
    $core.String? instanceId,
    $core.int? fromIndex,
    $core.int? toIndex,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (fromIndex != null) result.fromIndex = fromIndex;
    if (toIndex != null) result.toIndex = toIndex;
    return result;
  }

  MoveInHistoryRequest._();

  factory MoveInHistoryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory MoveInHistoryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'MoveInHistoryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aI(2, _omitFieldNames ? '' : 'fromIndex')
    ..aI(3, _omitFieldNames ? '' : 'toIndex')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInHistoryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInHistoryRequest copyWith(void Function(MoveInHistoryRequest) updates) =>
      super.copyWith((message) => updates(message as MoveInHistoryRequest))
          as MoveInHistoryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static MoveInHistoryRequest create() => MoveInHistoryRequest._();
  @$core.override
  MoveInHistoryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static MoveInHistoryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<MoveInHistoryRequest>(create);
  static MoveInHistoryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $core.int get fromIndex => $_getIZ(1);
  @$pb.TagNumber(2)
  set fromIndex($core.int value) => $_setSignedInt32(1, value);
  @$pb.TagNumber(2)
  $core.bool hasFromIndex() => $_has(1);
  @$pb.TagNumber(2)
  void clearFromIndex() => $_clearField(2);

  @$pb.TagNumber(3)
  $core.int get toIndex => $_getIZ(2);
  @$pb.TagNumber(3)
  set toIndex($core.int value) => $_setSignedInt32(2, value);
  @$pb.TagNumber(3)
  $core.bool hasToIndex() => $_has(2);
  @$pb.TagNumber(3)
  void clearToIndex() => $_clearField(3);
}

class MoveInHistoryResponse extends $pb.GeneratedMessage {
  factory MoveInHistoryResponse() => create();

  MoveInHistoryResponse._();

  factory MoveInHistoryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory MoveInHistoryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'MoveInHistoryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInHistoryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  MoveInHistoryResponse copyWith(
          void Function(MoveInHistoryResponse) updates) =>
      super.copyWith((message) => updates(message as MoveInHistoryResponse))
          as MoveInHistoryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static MoveInHistoryResponse create() => MoveInHistoryResponse._();
  @$core.override
  MoveInHistoryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static MoveInHistoryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<MoveInHistoryResponse>(create);
  static MoveInHistoryResponse? _defaultInstance;
}

class ClearHistoryRequest extends $pb.GeneratedMessage {
  factory ClearHistoryRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  ClearHistoryRequest._();

  factory ClearHistoryRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ClearHistoryRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ClearHistoryRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearHistoryRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearHistoryRequest copyWith(void Function(ClearHistoryRequest) updates) =>
      super.copyWith((message) => updates(message as ClearHistoryRequest))
          as ClearHistoryRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ClearHistoryRequest create() => ClearHistoryRequest._();
  @$core.override
  ClearHistoryRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ClearHistoryRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ClearHistoryRequest>(create);
  static ClearHistoryRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class ClearHistoryResponse extends $pb.GeneratedMessage {
  factory ClearHistoryResponse() => create();

  ClearHistoryResponse._();

  factory ClearHistoryResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory ClearHistoryResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'ClearHistoryResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearHistoryResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  ClearHistoryResponse copyWith(void Function(ClearHistoryResponse) updates) =>
      super.copyWith((message) => updates(message as ClearHistoryResponse))
          as ClearHistoryResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static ClearHistoryResponse create() => ClearHistoryResponse._();
  @$core.override
  ClearHistoryResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static ClearHistoryResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<ClearHistoryResponse>(create);
  static ClearHistoryResponse? _defaultInstance;
}

class PlaylistSourceInfo extends $pb.GeneratedMessage {
  factory PlaylistSourceInfo({
    $core.String? id,
    $core.String? name,
    $core.int? songCount,
    $0.PlaylistSourceKind? kind,
    $core.String? refId,
  }) {
    final result = create();
    if (id != null) result.id = id;
    if (name != null) result.name = name;
    if (songCount != null) result.songCount = songCount;
    if (kind != null) result.kind = kind;
    if (refId != null) result.refId = refId;
    return result;
  }

  PlaylistSourceInfo._();

  factory PlaylistSourceInfo.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistSourceInfo.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistSourceInfo',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'name')
    ..aI(3, _omitFieldNames ? '' : 'songCount')
    ..aE<$0.PlaylistSourceKind>(4, _omitFieldNames ? '' : 'kind',
        enumValues: $0.PlaylistSourceKind.values)
    ..aOS(5, _omitFieldNames ? '' : 'refId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistSourceInfo clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistSourceInfo copyWith(void Function(PlaylistSourceInfo) updates) =>
      super.copyWith((message) => updates(message as PlaylistSourceInfo))
          as PlaylistSourceInfo;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistSourceInfo create() => PlaylistSourceInfo._();
  @$core.override
  PlaylistSourceInfo createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistSourceInfo getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistSourceInfo>(create);
  static PlaylistSourceInfo? _defaultInstance;

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

  @$pb.TagNumber(4)
  $0.PlaylistSourceKind get kind => $_getN(3);
  @$pb.TagNumber(4)
  set kind($0.PlaylistSourceKind value) => $_setField(4, value);
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

class PlaylistSourceSpec extends $pb.GeneratedMessage {
  factory PlaylistSourceSpec({
    $core.String? id,
    $core.String? name,
    $core.Iterable<$core.String>? uuids,
    $0.PlaylistSourceKind? kind,
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

  PlaylistSourceSpec._();

  factory PlaylistSourceSpec.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory PlaylistSourceSpec.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'PlaylistSourceSpec',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'id')
    ..aOS(2, _omitFieldNames ? '' : 'name')
    ..pPS(3, _omitFieldNames ? '' : 'uuids')
    ..aE<$0.PlaylistSourceKind>(4, _omitFieldNames ? '' : 'kind',
        enumValues: $0.PlaylistSourceKind.values)
    ..aOS(5, _omitFieldNames ? '' : 'refId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistSourceSpec clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  PlaylistSourceSpec copyWith(void Function(PlaylistSourceSpec) updates) =>
      super.copyWith((message) => updates(message as PlaylistSourceSpec))
          as PlaylistSourceSpec;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static PlaylistSourceSpec create() => PlaylistSourceSpec._();
  @$core.override
  PlaylistSourceSpec createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static PlaylistSourceSpec getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<PlaylistSourceSpec>(create);
  static PlaylistSourceSpec? _defaultInstance;

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
  $0.PlaylistSourceKind get kind => $_getN(3);
  @$pb.TagNumber(4)
  set kind($0.PlaylistSourceKind value) => $_setField(4, value);
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

class GetPlaylistSourcesRequest extends $pb.GeneratedMessage {
  factory GetPlaylistSourcesRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetPlaylistSourcesRequest._();

  factory GetPlaylistSourcesRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetPlaylistSourcesRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetPlaylistSourcesRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistSourcesRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistSourcesRequest copyWith(
          void Function(GetPlaylistSourcesRequest) updates) =>
      super.copyWith((message) => updates(message as GetPlaylistSourcesRequest))
          as GetPlaylistSourcesRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetPlaylistSourcesRequest create() => GetPlaylistSourcesRequest._();
  @$core.override
  GetPlaylistSourcesRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetPlaylistSourcesRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetPlaylistSourcesRequest>(create);
  static GetPlaylistSourcesRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class GetPlaylistSourcesResponse extends $pb.GeneratedMessage {
  factory GetPlaylistSourcesResponse({
    $core.Iterable<PlaylistSourceInfo>? sources,
  }) {
    final result = create();
    if (sources != null) result.sources.addAll(sources);
    return result;
  }

  GetPlaylistSourcesResponse._();

  factory GetPlaylistSourcesResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetPlaylistSourcesResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetPlaylistSourcesResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..pPM<PlaylistSourceInfo>(1, _omitFieldNames ? '' : 'sources',
        subBuilder: PlaylistSourceInfo.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistSourcesResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetPlaylistSourcesResponse copyWith(
          void Function(GetPlaylistSourcesResponse) updates) =>
      super.copyWith(
              (message) => updates(message as GetPlaylistSourcesResponse))
          as GetPlaylistSourcesResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetPlaylistSourcesResponse create() => GetPlaylistSourcesResponse._();
  @$core.override
  GetPlaylistSourcesResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetPlaylistSourcesResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetPlaylistSourcesResponse>(create);
  static GetPlaylistSourcesResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $pb.PbList<PlaylistSourceInfo> get sources => $_getList(0);
}

class SetPlaylistSourcesRequest extends $pb.GeneratedMessage {
  factory SetPlaylistSourcesRequest({
    $core.String? instanceId,
    $core.Iterable<PlaylistSourceSpec>? sources,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (sources != null) result.sources.addAll(sources);
    return result;
  }

  SetPlaylistSourcesRequest._();

  factory SetPlaylistSourcesRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetPlaylistSourcesRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetPlaylistSourcesRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..pPM<PlaylistSourceSpec>(2, _omitFieldNames ? '' : 'sources',
        subBuilder: PlaylistSourceSpec.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetPlaylistSourcesRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetPlaylistSourcesRequest copyWith(
          void Function(SetPlaylistSourcesRequest) updates) =>
      super.copyWith((message) => updates(message as SetPlaylistSourcesRequest))
          as SetPlaylistSourcesRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetPlaylistSourcesRequest create() => SetPlaylistSourcesRequest._();
  @$core.override
  SetPlaylistSourcesRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetPlaylistSourcesRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetPlaylistSourcesRequest>(create);
  static SetPlaylistSourcesRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);

  @$pb.TagNumber(2)
  $pb.PbList<PlaylistSourceSpec> get sources => $_getList(1);
}

class SetPlaylistSourcesResponse extends $pb.GeneratedMessage {
  factory SetPlaylistSourcesResponse() => create();

  SetPlaylistSourcesResponse._();

  factory SetPlaylistSourcesResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetPlaylistSourcesResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetPlaylistSourcesResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetPlaylistSourcesResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetPlaylistSourcesResponse copyWith(
          void Function(SetPlaylistSourcesResponse) updates) =>
      super.copyWith(
              (message) => updates(message as SetPlaylistSourcesResponse))
          as SetPlaylistSourcesResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetPlaylistSourcesResponse create() => SetPlaylistSourcesResponse._();
  @$core.override
  SetPlaylistSourcesResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetPlaylistSourcesResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetPlaylistSourcesResponse>(create);
  static SetPlaylistSourcesResponse? _defaultInstance;
}

class GetStatusRequest extends $pb.GeneratedMessage {
  factory GetStatusRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetStatusRequest._();

  factory GetStatusRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetStatusRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetStatusRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetStatusRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetStatusRequest copyWith(void Function(GetStatusRequest) updates) =>
      super.copyWith((message) => updates(message as GetStatusRequest))
          as GetStatusRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetStatusRequest create() => GetStatusRequest._();
  @$core.override
  GetStatusRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetStatusRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetStatusRequest>(create);
  static GetStatusRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class GetEqualizerRequest extends $pb.GeneratedMessage {
  factory GetEqualizerRequest({
    $core.String? instanceId,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    return result;
  }

  GetEqualizerRequest._();

  factory GetEqualizerRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory GetEqualizerRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'GetEqualizerRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetEqualizerRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  GetEqualizerRequest copyWith(void Function(GetEqualizerRequest) updates) =>
      super.copyWith((message) => updates(message as GetEqualizerRequest))
          as GetEqualizerRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static GetEqualizerRequest create() => GetEqualizerRequest._();
  @$core.override
  GetEqualizerRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static GetEqualizerRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<GetEqualizerRequest>(create);
  static GetEqualizerRequest? _defaultInstance;

  @$pb.TagNumber(1)
  $core.String get instanceId => $_getSZ(0);
  @$pb.TagNumber(1)
  set instanceId($core.String value) => $_setString(0, value);
  @$pb.TagNumber(1)
  $core.bool hasInstanceId() => $_has(0);
  @$pb.TagNumber(1)
  void clearInstanceId() => $_clearField(1);
}

class SetEqualizerRequest extends $pb.GeneratedMessage {
  factory SetEqualizerRequest({
    $core.String? instanceId,
    $0.EqualizerState? state,
  }) {
    final result = create();
    if (instanceId != null) result.instanceId = instanceId;
    if (state != null) result.state = state;
    return result;
  }

  SetEqualizerRequest._();

  factory SetEqualizerRequest.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetEqualizerRequest.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetEqualizerRequest',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOS(1, _omitFieldNames ? '' : 'instanceId')
    ..aOM<$0.EqualizerState>(2, _omitFieldNames ? '' : 'state',
        subBuilder: $0.EqualizerState.create)
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetEqualizerRequest clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetEqualizerRequest copyWith(void Function(SetEqualizerRequest) updates) =>
      super.copyWith((message) => updates(message as SetEqualizerRequest))
          as SetEqualizerRequest;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetEqualizerRequest create() => SetEqualizerRequest._();
  @$core.override
  SetEqualizerRequest createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetEqualizerRequest getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetEqualizerRequest>(create);
  static SetEqualizerRequest? _defaultInstance;

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

class SetEqualizerResponse extends $pb.GeneratedMessage {
  factory SetEqualizerResponse({
    $core.bool? saved,
  }) {
    final result = create();
    if (saved != null) result.saved = saved;
    return result;
  }

  SetEqualizerResponse._();

  factory SetEqualizerResponse.fromBuffer($core.List<$core.int> data,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromBuffer(data, registry);
  factory SetEqualizerResponse.fromJson($core.String json,
          [$pb.ExtensionRegistry registry = $pb.ExtensionRegistry.EMPTY]) =>
      create()..mergeFromJson(json, registry);

  static final $pb.BuilderInfo _i = $pb.BuilderInfo(
      _omitMessageNames ? '' : 'SetEqualizerResponse',
      package:
          const $pb.PackageName(_omitMessageNames ? '' : 'omni_mix_player'),
      createEmptyInstance: create)
    ..aOB(1, _omitFieldNames ? '' : 'saved')
    ..hasRequiredFields = false;

  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetEqualizerResponse clone() => deepCopy();
  @$core.Deprecated('See https://github.com/google/protobuf.dart/issues/998.')
  SetEqualizerResponse copyWith(void Function(SetEqualizerResponse) updates) =>
      super.copyWith((message) => updates(message as SetEqualizerResponse))
          as SetEqualizerResponse;

  @$core.override
  $pb.BuilderInfo get info_ => _i;

  @$core.pragma('dart2js:noInline')
  static SetEqualizerResponse create() => SetEqualizerResponse._();
  @$core.override
  SetEqualizerResponse createEmptyInstance() => create();
  @$core.pragma('dart2js:noInline')
  static SetEqualizerResponse getDefault() => _defaultInstance ??=
      $pb.GeneratedMessage.$_defaultFor<SetEqualizerResponse>(create);
  static SetEqualizerResponse? _defaultInstance;

  @$pb.TagNumber(1)
  $core.bool get saved => $_getBF(0);
  @$pb.TagNumber(1)
  set saved($core.bool value) => $_setBool(0, value);
  @$pb.TagNumber(1)
  $core.bool hasSaved() => $_has(0);
  @$pb.TagNumber(1)
  void clearSaved() => $_clearField(1);
}

/// 播放控制服务 — 实例级别
class PlaybackServiceApi {
  final $pb.RpcClient _client;

  PlaybackServiceApi(this._client);

  /// 播放控制
  $async.Future<PlayResponse> play(
          $pb.ClientContext? ctx, PlayRequest request) =>
      _client.invoke<PlayResponse>(
          ctx, 'PlaybackService', 'Play', request, PlayResponse());
  $async.Future<PauseResponse> pause(
          $pb.ClientContext? ctx, PauseRequest request) =>
      _client.invoke<PauseResponse>(
          ctx, 'PlaybackService', 'Pause', request, PauseResponse());
  $async.Future<ResumeResponse> resume(
          $pb.ClientContext? ctx, ResumeRequest request) =>
      _client.invoke<ResumeResponse>(
          ctx, 'PlaybackService', 'Resume', request, ResumeResponse());
  $async.Future<ToggleResponse> toggle(
          $pb.ClientContext? ctx, ToggleRequest request) =>
      _client.invoke<ToggleResponse>(
          ctx, 'PlaybackService', 'Toggle', request, ToggleResponse());
  $async.Future<NextResponse> next(
          $pb.ClientContext? ctx, NextRequest request) =>
      _client.invoke<NextResponse>(
          ctx, 'PlaybackService', 'Next', request, NextResponse());
  $async.Future<PrevResponse> prev(
          $pb.ClientContext? ctx, PrevRequest request) =>
      _client.invoke<PrevResponse>(
          ctx, 'PlaybackService', 'Prev', request, PrevResponse());
  $async.Future<SeekResponse> seek(
          $pb.ClientContext? ctx, SeekRequest request) =>
      _client.invoke<SeekResponse>(
          ctx, 'PlaybackService', 'Seek', request, SeekResponse());
  $async.Future<StopResponse> stop(
          $pb.ClientContext? ctx, StopRequest request) =>
      _client.invoke<StopResponse>(
          ctx, 'PlaybackService', 'Stop', request, StopResponse());

  /// 音量 / 延迟
  $async.Future<SetVolumeResponse> setVolume(
          $pb.ClientContext? ctx, SetVolumeRequest request) =>
      _client.invoke<SetVolumeResponse>(
          ctx, 'PlaybackService', 'SetVolume', request, SetVolumeResponse());
  $async.Future<GetVolumeResponse> getVolume(
          $pb.ClientContext? ctx, GetVolumeRequest request) =>
      _client.invoke<GetVolumeResponse>(
          ctx, 'PlaybackService', 'GetVolume', request, GetVolumeResponse());
  $async.Future<SetTargetLatencyResponse> setTargetLatency(
          $pb.ClientContext? ctx, SetTargetLatencyRequest request) =>
      _client.invoke<SetTargetLatencyResponse>(ctx, 'PlaybackService',
          'SetTargetLatency', request, SetTargetLatencyResponse());
  $async.Future<GetTargetLatencyResponse> getTargetLatency(
          $pb.ClientContext? ctx, GetTargetLatencyRequest request) =>
      _client.invoke<GetTargetLatencyResponse>(ctx, 'PlaybackService',
          'GetTargetLatency', request, GetTargetLatencyResponse());

  /// 随机 / 重复
  $async.Future<SetShuffleResponse> setShuffle(
          $pb.ClientContext? ctx, SetShuffleRequest request) =>
      _client.invoke<SetShuffleResponse>(
          ctx, 'PlaybackService', 'SetShuffle', request, SetShuffleResponse());
  $async.Future<SetRepeatModeResponse> setRepeatMode(
          $pb.ClientContext? ctx, SetRepeatModeRequest request) =>
      _client.invoke<SetRepeatModeResponse>(ctx, 'PlaybackService',
          'SetRepeatMode', request, SetRepeatModeResponse());

  /// 队列管理
  $async.Future<GetQueueResponse> getQueue(
          $pb.ClientContext? ctx, GetQueueRequest request) =>
      _client.invoke<GetQueueResponse>(
          ctx, 'PlaybackService', 'GetQueue', request, GetQueueResponse());
  $async.Future<AddToQueueResponse> addToQueue(
          $pb.ClientContext? ctx, AddToQueueRequest request) =>
      _client.invoke<AddToQueueResponse>(
          ctx, 'PlaybackService', 'AddToQueue', request, AddToQueueResponse());
  $async.Future<InsertIntoQueueResponse> insertIntoQueue(
          $pb.ClientContext? ctx, InsertIntoQueueRequest request) =>
      _client.invoke<InsertIntoQueueResponse>(ctx, 'PlaybackService',
          'InsertIntoQueue', request, InsertIntoQueueResponse());
  $async.Future<SetQueueResponse> setQueue(
          $pb.ClientContext? ctx, SetQueueRequest request) =>
      _client.invoke<SetQueueResponse>(
          ctx, 'PlaybackService', 'SetQueue', request, SetQueueResponse());
  $async.Future<RemoveFromQueueResponse> removeFromQueue(
          $pb.ClientContext? ctx, RemoveFromQueueRequest request) =>
      _client.invoke<RemoveFromQueueResponse>(ctx, 'PlaybackService',
          'RemoveFromQueue', request, RemoveFromQueueResponse());
  $async.Future<MoveInQueueResponse> moveInQueue(
          $pb.ClientContext? ctx, MoveInQueueRequest request) =>
      _client.invoke<MoveInQueueResponse>(ctx, 'PlaybackService', 'MoveInQueue',
          request, MoveInQueueResponse());
  $async.Future<ClearQueueResponse> clearQueue(
          $pb.ClientContext? ctx, ClearQueueRequest request) =>
      _client.invoke<ClearQueueResponse>(
          ctx, 'PlaybackService', 'ClearQueue', request, ClearQueueResponse());

  /// 历史
  $async.Future<GetHistoryResponse> getHistory(
          $pb.ClientContext? ctx, GetHistoryRequest request) =>
      _client.invoke<GetHistoryResponse>(
          ctx, 'PlaybackService', 'GetHistory', request, GetHistoryResponse());
  $async.Future<RemoveFromHistoryResponse> removeFromHistory(
          $pb.ClientContext? ctx, RemoveFromHistoryRequest request) =>
      _client.invoke<RemoveFromHistoryResponse>(ctx, 'PlaybackService',
          'RemoveFromHistory', request, RemoveFromHistoryResponse());
  $async.Future<MoveInHistoryResponse> moveInHistory(
          $pb.ClientContext? ctx, MoveInHistoryRequest request) =>
      _client.invoke<MoveInHistoryResponse>(ctx, 'PlaybackService',
          'MoveInHistory', request, MoveInHistoryResponse());
  $async.Future<ClearHistoryResponse> clearHistory(
          $pb.ClientContext? ctx, ClearHistoryRequest request) =>
      _client.invoke<ClearHistoryResponse>(ctx, 'PlaybackService',
          'ClearHistory', request, ClearHistoryResponse());

  /// 歌单源
  $async.Future<GetPlaylistSourcesResponse> getPlaylistSources(
          $pb.ClientContext? ctx, GetPlaylistSourcesRequest request) =>
      _client.invoke<GetPlaylistSourcesResponse>(ctx, 'PlaybackService',
          'GetPlaylistSources', request, GetPlaylistSourcesResponse());
  $async.Future<SetPlaylistSourcesResponse> setPlaylistSources(
          $pb.ClientContext? ctx, SetPlaylistSourcesRequest request) =>
      _client.invoke<SetPlaylistSourcesResponse>(ctx, 'PlaybackService',
          'SetPlaylistSources', request, SetPlaylistSourcesResponse());

  /// 状态
  $async.Future<$0.PlaybackStatus> getStatus(
          $pb.ClientContext? ctx, GetStatusRequest request) =>
      _client.invoke<$0.PlaybackStatus>(
          ctx, 'PlaybackService', 'GetStatus', request, $0.PlaybackStatus());

  /// 均衡器
  $async.Future<$0.EqualizerState> getEqualizer(
          $pb.ClientContext? ctx, GetEqualizerRequest request) =>
      _client.invoke<$0.EqualizerState>(
          ctx, 'PlaybackService', 'GetEqualizer', request, $0.EqualizerState());
  $async.Future<SetEqualizerResponse> setEqualizer(
          $pb.ClientContext? ctx, SetEqualizerRequest request) =>
      _client.invoke<SetEqualizerResponse>(ctx, 'PlaybackService',
          'SetEqualizer', request, SetEqualizerResponse());
}

const $core.bool _omitFieldNames =
    $core.bool.fromEnvironment('protobuf.omit_field_names');
const $core.bool _omitMessageNames =
    $core.bool.fromEnvironment('protobuf.omit_message_names');
