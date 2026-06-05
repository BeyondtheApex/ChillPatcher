// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/playback.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports
// ignore_for_file: unused_import

import 'dart:convert' as $convert;
import 'dart:core' as $core;
import 'dart:typed_data' as $typed_data;

import '../models/instance.pbjson.dart' as $0;

@$core.Deprecated('Use playRequestDescriptor instead')
const PlayRequest$json = {
  '1': 'PlayRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'uuid', '3': 2, '4': 1, '5': 9, '10': 'uuid'},
  ],
};

/// Descriptor for `PlayRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playRequestDescriptor = $convert.base64Decode(
    'CgtQbGF5UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBISCgR1dWlkGA'
    'IgASgJUgR1dWlk');

@$core.Deprecated('Use playResponseDescriptor instead')
const PlayResponse$json = {
  '1': 'PlayResponse',
};

/// Descriptor for `PlayResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playResponseDescriptor =
    $convert.base64Decode('CgxQbGF5UmVzcG9uc2U=');

@$core.Deprecated('Use pauseRequestDescriptor instead')
const PauseRequest$json = {
  '1': 'PauseRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `PauseRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List pauseRequestDescriptor = $convert.base64Decode(
    'CgxQYXVzZVJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSWQ=');

@$core.Deprecated('Use pauseResponseDescriptor instead')
const PauseResponse$json = {
  '1': 'PauseResponse',
};

/// Descriptor for `PauseResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List pauseResponseDescriptor =
    $convert.base64Decode('Cg1QYXVzZVJlc3BvbnNl');

@$core.Deprecated('Use resumeRequestDescriptor instead')
const ResumeRequest$json = {
  '1': 'ResumeRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `ResumeRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List resumeRequestDescriptor = $convert.base64Decode(
    'Cg1SZXN1bWVSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use resumeResponseDescriptor instead')
const ResumeResponse$json = {
  '1': 'ResumeResponse',
};

/// Descriptor for `ResumeResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List resumeResponseDescriptor =
    $convert.base64Decode('Cg5SZXN1bWVSZXNwb25zZQ==');

@$core.Deprecated('Use toggleRequestDescriptor instead')
const ToggleRequest$json = {
  '1': 'ToggleRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `ToggleRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List toggleRequestDescriptor = $convert.base64Decode(
    'Cg1Ub2dnbGVSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use toggleResponseDescriptor instead')
const ToggleResponse$json = {
  '1': 'ToggleResponse',
};

/// Descriptor for `ToggleResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List toggleResponseDescriptor =
    $convert.base64Decode('Cg5Ub2dnbGVSZXNwb25zZQ==');

@$core.Deprecated('Use nextRequestDescriptor instead')
const NextRequest$json = {
  '1': 'NextRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `NextRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List nextRequestDescriptor = $convert.base64Decode(
    'CgtOZXh0UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZA==');

@$core.Deprecated('Use nextResponseDescriptor instead')
const NextResponse$json = {
  '1': 'NextResponse',
};

/// Descriptor for `NextResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List nextResponseDescriptor =
    $convert.base64Decode('CgxOZXh0UmVzcG9uc2U=');

@$core.Deprecated('Use prevRequestDescriptor instead')
const PrevRequest$json = {
  '1': 'PrevRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `PrevRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List prevRequestDescriptor = $convert.base64Decode(
    'CgtQcmV2UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZA==');

@$core.Deprecated('Use prevResponseDescriptor instead')
const PrevResponse$json = {
  '1': 'PrevResponse',
};

/// Descriptor for `PrevResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List prevResponseDescriptor =
    $convert.base64Decode('CgxQcmV2UmVzcG9uc2U=');

@$core.Deprecated('Use seekRequestDescriptor instead')
const SeekRequest$json = {
  '1': 'SeekRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'position', '3': 2, '4': 1, '5': 2, '10': 'position'},
  ],
};

/// Descriptor for `SeekRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List seekRequestDescriptor = $convert.base64Decode(
    'CgtTZWVrUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBIaCghwb3NpdG'
    'lvbhgCIAEoAlIIcG9zaXRpb24=');

@$core.Deprecated('Use seekResponseDescriptor instead')
const SeekResponse$json = {
  '1': 'SeekResponse',
};

/// Descriptor for `SeekResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List seekResponseDescriptor =
    $convert.base64Decode('CgxTZWVrUmVzcG9uc2U=');

@$core.Deprecated('Use stopRequestDescriptor instead')
const StopRequest$json = {
  '1': 'StopRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `StopRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List stopRequestDescriptor = $convert.base64Decode(
    'CgtTdG9wUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZA==');

@$core.Deprecated('Use stopResponseDescriptor instead')
const StopResponse$json = {
  '1': 'StopResponse',
};

/// Descriptor for `StopResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List stopResponseDescriptor =
    $convert.base64Decode('CgxTdG9wUmVzcG9uc2U=');

@$core.Deprecated('Use setVolumeRequestDescriptor instead')
const SetVolumeRequest$json = {
  '1': 'SetVolumeRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'volume', '3': 2, '4': 1, '5': 2, '10': 'volume'},
  ],
};

/// Descriptor for `SetVolumeRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setVolumeRequestDescriptor = $convert.base64Decode(
    'ChBTZXRWb2x1bWVSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlkEhYKBn'
    'ZvbHVtZRgCIAEoAlIGdm9sdW1l');

@$core.Deprecated('Use setVolumeResponseDescriptor instead')
const SetVolumeResponse$json = {
  '1': 'SetVolumeResponse',
  '2': [
    {'1': 'saved', '3': 1, '4': 1, '5': 8, '10': 'saved'},
  ],
};

/// Descriptor for `SetVolumeResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setVolumeResponseDescriptor = $convert
    .base64Decode('ChFTZXRWb2x1bWVSZXNwb25zZRIUCgVzYXZlZBgBIAEoCFIFc2F2ZWQ=');

@$core.Deprecated('Use getVolumeRequestDescriptor instead')
const GetVolumeRequest$json = {
  '1': 'GetVolumeRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetVolumeRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getVolumeRequestDescriptor = $convert.base64Decode(
    'ChBHZXRWb2x1bWVSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use getVolumeResponseDescriptor instead')
const GetVolumeResponse$json = {
  '1': 'GetVolumeResponse',
  '2': [
    {'1': 'volume', '3': 1, '4': 1, '5': 2, '10': 'volume'},
  ],
};

/// Descriptor for `GetVolumeResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getVolumeResponseDescriptor = $convert.base64Decode(
    'ChFHZXRWb2x1bWVSZXNwb25zZRIWCgZ2b2x1bWUYASABKAJSBnZvbHVtZQ==');

@$core.Deprecated('Use setTargetLatencyRequestDescriptor instead')
const SetTargetLatencyRequest$json = {
  '1': 'SetTargetLatencyRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'latency', '3': 2, '4': 1, '5': 2, '10': 'latency'},
  ],
};

/// Descriptor for `SetTargetLatencyRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setTargetLatencyRequestDescriptor =
    $convert.base64Decode(
        'ChdTZXRUYXJnZXRMYXRlbmN5UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2'
        'VJZBIYCgdsYXRlbmN5GAIgASgCUgdsYXRlbmN5');

@$core.Deprecated('Use setTargetLatencyResponseDescriptor instead')
const SetTargetLatencyResponse$json = {
  '1': 'SetTargetLatencyResponse',
  '2': [
    {'1': 'saved', '3': 1, '4': 1, '5': 8, '10': 'saved'},
  ],
};

/// Descriptor for `SetTargetLatencyResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setTargetLatencyResponseDescriptor =
    $convert.base64Decode(
        'ChhTZXRUYXJnZXRMYXRlbmN5UmVzcG9uc2USFAoFc2F2ZWQYASABKAhSBXNhdmVk');

@$core.Deprecated('Use getTargetLatencyRequestDescriptor instead')
const GetTargetLatencyRequest$json = {
  '1': 'GetTargetLatencyRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetTargetLatencyRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getTargetLatencyRequestDescriptor =
    $convert.base64Decode(
        'ChdHZXRUYXJnZXRMYXRlbmN5UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2'
        'VJZA==');

@$core.Deprecated('Use getTargetLatencyResponseDescriptor instead')
const GetTargetLatencyResponse$json = {
  '1': 'GetTargetLatencyResponse',
  '2': [
    {'1': 'latency', '3': 1, '4': 1, '5': 2, '10': 'latency'},
  ],
};

/// Descriptor for `GetTargetLatencyResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getTargetLatencyResponseDescriptor =
    $convert.base64Decode(
        'ChhHZXRUYXJnZXRMYXRlbmN5UmVzcG9uc2USGAoHbGF0ZW5jeRgBIAEoAlIHbGF0ZW5jeQ==');

@$core.Deprecated('Use setShuffleRequestDescriptor instead')
const SetShuffleRequest$json = {
  '1': 'SetShuffleRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'enabled', '3': 2, '4': 1, '5': 8, '10': 'enabled'},
  ],
};

/// Descriptor for `SetShuffleRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setShuffleRequestDescriptor = $convert.base64Decode(
    'ChFTZXRTaHVmZmxlUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBIYCg'
    'dlbmFibGVkGAIgASgIUgdlbmFibGVk');

@$core.Deprecated('Use setShuffleResponseDescriptor instead')
const SetShuffleResponse$json = {
  '1': 'SetShuffleResponse',
};

/// Descriptor for `SetShuffleResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setShuffleResponseDescriptor =
    $convert.base64Decode('ChJTZXRTaHVmZmxlUmVzcG9uc2U=');

@$core.Deprecated('Use setRepeatModeRequestDescriptor instead')
const SetRepeatModeRequest$json = {
  '1': 'SetRepeatModeRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {
      '1': 'mode',
      '3': 2,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.RepeatMode',
      '10': 'mode'
    },
  ],
};

/// Descriptor for `SetRepeatModeRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setRepeatModeRequestDescriptor = $convert.base64Decode(
    'ChRTZXRSZXBlYXRNb2RlUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZB'
    'IvCgRtb2RlGAIgASgOMhsub21uaV9taXhfcGxheWVyLlJlcGVhdE1vZGVSBG1vZGU=');

@$core.Deprecated('Use setRepeatModeResponseDescriptor instead')
const SetRepeatModeResponse$json = {
  '1': 'SetRepeatModeResponse',
};

/// Descriptor for `SetRepeatModeResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setRepeatModeResponseDescriptor =
    $convert.base64Decode('ChVTZXRSZXBlYXRNb2RlUmVzcG9uc2U=');

@$core.Deprecated('Use queueTrackDescriptor instead')
const QueueTrack$json = {
  '1': 'QueueTrack',
  '2': [
    {'1': 'index', '3': 1, '4': 1, '5': 5, '10': 'index'},
    {'1': 'uuid', '3': 2, '4': 1, '5': 9, '10': 'uuid'},
    {'1': 'title', '3': 3, '4': 1, '5': 9, '10': 'title'},
    {'1': 'artist', '3': 4, '4': 1, '5': 9, '10': 'artist'},
    {'1': 'album_id', '3': 5, '4': 1, '5': 9, '10': 'albumId'},
    {'1': 'duration', '3': 6, '4': 1, '5': 2, '10': 'duration'},
    {'1': 'module_id', '3': 7, '4': 1, '5': 9, '10': 'moduleId'},
    {'1': 'cover_uri', '3': 8, '4': 1, '5': 9, '10': 'coverUri'},
  ],
};

/// Descriptor for `QueueTrack`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List queueTrackDescriptor = $convert.base64Decode(
    'CgpRdWV1ZVRyYWNrEhQKBWluZGV4GAEgASgFUgVpbmRleBISCgR1dWlkGAIgASgJUgR1dWlkEh'
    'QKBXRpdGxlGAMgASgJUgV0aXRsZRIWCgZhcnRpc3QYBCABKAlSBmFydGlzdBIZCghhbGJ1bV9p'
    'ZBgFIAEoCVIHYWxidW1JZBIaCghkdXJhdGlvbhgGIAEoAlIIZHVyYXRpb24SGwoJbW9kdWxlX2'
    'lkGAcgASgJUghtb2R1bGVJZBIbCgljb3Zlcl91cmkYCCABKAlSCGNvdmVyVXJp');

@$core.Deprecated('Use getQueueRequestDescriptor instead')
const GetQueueRequest$json = {
  '1': 'GetQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getQueueRequestDescriptor = $convert.base64Decode(
    'Cg9HZXRRdWV1ZVJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSWQ=');

@$core.Deprecated('Use getQueueResponseDescriptor instead')
const GetQueueResponse$json = {
  '1': 'GetQueueResponse',
  '2': [
    {
      '1': 'queue',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.QueueTrack',
      '10': 'queue'
    },
  ],
};

/// Descriptor for `GetQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getQueueResponseDescriptor = $convert.base64Decode(
    'ChBHZXRRdWV1ZVJlc3BvbnNlEjEKBXF1ZXVlGAEgAygLMhsub21uaV9taXhfcGxheWVyLlF1ZX'
    'VlVHJhY2tSBXF1ZXVl');

@$core.Deprecated('Use addToQueueRequestDescriptor instead')
const AddToQueueRequest$json = {
  '1': 'AddToQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'uuid', '3': 2, '4': 1, '5': 9, '10': 'uuid'},
  ],
};

/// Descriptor for `AddToQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List addToQueueRequestDescriptor = $convert.base64Decode(
    'ChFBZGRUb1F1ZXVlUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBISCg'
    'R1dWlkGAIgASgJUgR1dWlk');

@$core.Deprecated('Use addToQueueResponseDescriptor instead')
const AddToQueueResponse$json = {
  '1': 'AddToQueueResponse',
};

/// Descriptor for `AddToQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List addToQueueResponseDescriptor =
    $convert.base64Decode('ChJBZGRUb1F1ZXVlUmVzcG9uc2U=');

@$core.Deprecated('Use insertIntoQueueRequestDescriptor instead')
const InsertIntoQueueRequest$json = {
  '1': 'InsertIntoQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'uuids', '3': 2, '4': 3, '5': 9, '10': 'uuids'},
    {'1': 'index', '3': 3, '4': 1, '5': 5, '10': 'index'},
  ],
};

/// Descriptor for `InsertIntoQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List insertIntoQueueRequestDescriptor =
    $convert.base64Decode(
        'ChZJbnNlcnRJbnRvUXVldWVSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZU'
        'lkEhQKBXV1aWRzGAIgAygJUgV1dWlkcxIUCgVpbmRleBgDIAEoBVIFaW5kZXg=');

@$core.Deprecated('Use insertIntoQueueResponseDescriptor instead')
const InsertIntoQueueResponse$json = {
  '1': 'InsertIntoQueueResponse',
};

/// Descriptor for `InsertIntoQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List insertIntoQueueResponseDescriptor =
    $convert.base64Decode('ChdJbnNlcnRJbnRvUXVldWVSZXNwb25zZQ==');

@$core.Deprecated('Use setQueueRequestDescriptor instead')
const SetQueueRequest$json = {
  '1': 'SetQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'uuids', '3': 2, '4': 3, '5': 9, '10': 'uuids'},
  ],
};

/// Descriptor for `SetQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setQueueRequestDescriptor = $convert.base64Decode(
    'Cg9TZXRRdWV1ZVJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSWQSFAoFdX'
    'VpZHMYAiADKAlSBXV1aWRz');

@$core.Deprecated('Use setQueueResponseDescriptor instead')
const SetQueueResponse$json = {
  '1': 'SetQueueResponse',
};

/// Descriptor for `SetQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setQueueResponseDescriptor =
    $convert.base64Decode('ChBTZXRRdWV1ZVJlc3BvbnNl');

@$core.Deprecated('Use removeFromQueueRequestDescriptor instead')
const RemoveFromQueueRequest$json = {
  '1': 'RemoveFromQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'index', '3': 2, '4': 1, '5': 5, '9': 0, '10': 'index'},
    {'1': 'uuid', '3': 3, '4': 1, '5': 9, '9': 0, '10': 'uuid'},
  ],
  '8': [
    {'1': 'target'},
  ],
};

/// Descriptor for `RemoveFromQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List removeFromQueueRequestDescriptor = $convert.base64Decode(
    'ChZSZW1vdmVGcm9tUXVldWVSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZU'
    'lkEhYKBWluZGV4GAIgASgFSABSBWluZGV4EhQKBHV1aWQYAyABKAlIAFIEdXVpZEIICgZ0YXJn'
    'ZXQ=');

@$core.Deprecated('Use removeFromQueueResponseDescriptor instead')
const RemoveFromQueueResponse$json = {
  '1': 'RemoveFromQueueResponse',
};

/// Descriptor for `RemoveFromQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List removeFromQueueResponseDescriptor =
    $convert.base64Decode('ChdSZW1vdmVGcm9tUXVldWVSZXNwb25zZQ==');

@$core.Deprecated('Use moveInQueueRequestDescriptor instead')
const MoveInQueueRequest$json = {
  '1': 'MoveInQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'from_index', '3': 2, '4': 1, '5': 5, '10': 'fromIndex'},
    {'1': 'to_index', '3': 3, '4': 1, '5': 5, '10': 'toIndex'},
  ],
};

/// Descriptor for `MoveInQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List moveInQueueRequestDescriptor = $convert.base64Decode(
    'ChJNb3ZlSW5RdWV1ZVJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSWQSHQ'
    'oKZnJvbV9pbmRleBgCIAEoBVIJZnJvbUluZGV4EhkKCHRvX2luZGV4GAMgASgFUgd0b0luZGV4');

@$core.Deprecated('Use moveInQueueResponseDescriptor instead')
const MoveInQueueResponse$json = {
  '1': 'MoveInQueueResponse',
};

/// Descriptor for `MoveInQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List moveInQueueResponseDescriptor =
    $convert.base64Decode('ChNNb3ZlSW5RdWV1ZVJlc3BvbnNl');

@$core.Deprecated('Use clearQueueRequestDescriptor instead')
const ClearQueueRequest$json = {
  '1': 'ClearQueueRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `ClearQueueRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List clearQueueRequestDescriptor = $convert.base64Decode(
    'ChFDbGVhclF1ZXVlUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZA==');

@$core.Deprecated('Use clearQueueResponseDescriptor instead')
const ClearQueueResponse$json = {
  '1': 'ClearQueueResponse',
};

/// Descriptor for `ClearQueueResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List clearQueueResponseDescriptor =
    $convert.base64Decode('ChJDbGVhclF1ZXVlUmVzcG9uc2U=');

@$core.Deprecated('Use getHistoryRequestDescriptor instead')
const GetHistoryRequest$json = {
  '1': 'GetHistoryRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetHistoryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getHistoryRequestDescriptor = $convert.base64Decode(
    'ChFHZXRIaXN0b3J5UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZA==');

@$core.Deprecated('Use getHistoryResponseDescriptor instead')
const GetHistoryResponse$json = {
  '1': 'GetHistoryResponse',
  '2': [
    {
      '1': 'history',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.QueueTrack',
      '10': 'history'
    },
  ],
};

/// Descriptor for `GetHistoryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getHistoryResponseDescriptor = $convert.base64Decode(
    'ChJHZXRIaXN0b3J5UmVzcG9uc2USNQoHaGlzdG9yeRgBIAMoCzIbLm9tbmlfbWl4X3BsYXllci'
    '5RdWV1ZVRyYWNrUgdoaXN0b3J5');

@$core.Deprecated('Use removeFromHistoryRequestDescriptor instead')
const RemoveFromHistoryRequest$json = {
  '1': 'RemoveFromHistoryRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'index', '3': 2, '4': 1, '5': 5, '10': 'index'},
  ],
};

/// Descriptor for `RemoveFromHistoryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List removeFromHistoryRequestDescriptor =
    $convert.base64Decode(
        'ChhSZW1vdmVGcm9tSGlzdG9yeVJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3Rhbm'
        'NlSWQSFAoFaW5kZXgYAiABKAVSBWluZGV4');

@$core.Deprecated('Use removeFromHistoryResponseDescriptor instead')
const RemoveFromHistoryResponse$json = {
  '1': 'RemoveFromHistoryResponse',
};

/// Descriptor for `RemoveFromHistoryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List removeFromHistoryResponseDescriptor =
    $convert.base64Decode('ChlSZW1vdmVGcm9tSGlzdG9yeVJlc3BvbnNl');

@$core.Deprecated('Use moveInHistoryRequestDescriptor instead')
const MoveInHistoryRequest$json = {
  '1': 'MoveInHistoryRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'from_index', '3': 2, '4': 1, '5': 5, '10': 'fromIndex'},
    {'1': 'to_index', '3': 3, '4': 1, '5': 5, '10': 'toIndex'},
  ],
};

/// Descriptor for `MoveInHistoryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List moveInHistoryRequestDescriptor = $convert.base64Decode(
    'ChRNb3ZlSW5IaXN0b3J5UmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZB'
    'IdCgpmcm9tX2luZGV4GAIgASgFUglmcm9tSW5kZXgSGQoIdG9faW5kZXgYAyABKAVSB3RvSW5k'
    'ZXg=');

@$core.Deprecated('Use moveInHistoryResponseDescriptor instead')
const MoveInHistoryResponse$json = {
  '1': 'MoveInHistoryResponse',
};

/// Descriptor for `MoveInHistoryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List moveInHistoryResponseDescriptor =
    $convert.base64Decode('ChVNb3ZlSW5IaXN0b3J5UmVzcG9uc2U=');

@$core.Deprecated('Use clearHistoryRequestDescriptor instead')
const ClearHistoryRequest$json = {
  '1': 'ClearHistoryRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `ClearHistoryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List clearHistoryRequestDescriptor = $convert.base64Decode(
    'ChNDbGVhckhpc3RvcnlSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use clearHistoryResponseDescriptor instead')
const ClearHistoryResponse$json = {
  '1': 'ClearHistoryResponse',
};

/// Descriptor for `ClearHistoryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List clearHistoryResponseDescriptor =
    $convert.base64Decode('ChRDbGVhckhpc3RvcnlSZXNwb25zZQ==');

@$core.Deprecated('Use playlistSourceInfoDescriptor instead')
const PlaylistSourceInfo$json = {
  '1': 'PlaylistSourceInfo',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'name', '3': 2, '4': 1, '5': 9, '10': 'name'},
    {'1': 'song_count', '3': 3, '4': 1, '5': 5, '10': 'songCount'},
    {
      '1': 'kind',
      '3': 4,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.PlaylistSourceKind',
      '10': 'kind'
    },
    {'1': 'ref_id', '3': 5, '4': 1, '5': 9, '10': 'refId'},
  ],
};

/// Descriptor for `PlaylistSourceInfo`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistSourceInfoDescriptor = $convert.base64Decode(
    'ChJQbGF5bGlzdFNvdXJjZUluZm8SDgoCaWQYASABKAlSAmlkEhIKBG5hbWUYAiABKAlSBG5hbW'
    'USHQoKc29uZ19jb3VudBgDIAEoBVIJc29uZ0NvdW50EjcKBGtpbmQYBCABKA4yIy5vbW5pX21p'
    'eF9wbGF5ZXIuUGxheWxpc3RTb3VyY2VLaW5kUgRraW5kEhUKBnJlZl9pZBgFIAEoCVIFcmVmSW'
    'Q=');

@$core.Deprecated('Use playlistSourceSpecDescriptor instead')
const PlaylistSourceSpec$json = {
  '1': 'PlaylistSourceSpec',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'name', '3': 2, '4': 1, '5': 9, '10': 'name'},
    {'1': 'uuids', '3': 3, '4': 3, '5': 9, '10': 'uuids'},
    {
      '1': 'kind',
      '3': 4,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.PlaylistSourceKind',
      '10': 'kind'
    },
    {'1': 'ref_id', '3': 5, '4': 1, '5': 9, '10': 'refId'},
  ],
};

/// Descriptor for `PlaylistSourceSpec`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistSourceSpecDescriptor = $convert.base64Decode(
    'ChJQbGF5bGlzdFNvdXJjZVNwZWMSDgoCaWQYASABKAlSAmlkEhIKBG5hbWUYAiABKAlSBG5hbW'
    'USFAoFdXVpZHMYAyADKAlSBXV1aWRzEjcKBGtpbmQYBCABKA4yIy5vbW5pX21peF9wbGF5ZXIu'
    'UGxheWxpc3RTb3VyY2VLaW5kUgRraW5kEhUKBnJlZl9pZBgFIAEoCVIFcmVmSWQ=');

@$core.Deprecated('Use getPlaylistSourcesRequestDescriptor instead')
const GetPlaylistSourcesRequest$json = {
  '1': 'GetPlaylistSourcesRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetPlaylistSourcesRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getPlaylistSourcesRequestDescriptor =
    $convert.base64Decode(
        'ChlHZXRQbGF5bGlzdFNvdXJjZXNSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW'
        '5jZUlk');

@$core.Deprecated('Use getPlaylistSourcesResponseDescriptor instead')
const GetPlaylistSourcesResponse$json = {
  '1': 'GetPlaylistSourcesResponse',
  '2': [
    {
      '1': 'sources',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.PlaylistSourceInfo',
      '10': 'sources'
    },
  ],
};

/// Descriptor for `GetPlaylistSourcesResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getPlaylistSourcesResponseDescriptor =
    $convert.base64Decode(
        'ChpHZXRQbGF5bGlzdFNvdXJjZXNSZXNwb25zZRI9Cgdzb3VyY2VzGAEgAygLMiMub21uaV9taX'
        'hfcGxheWVyLlBsYXlsaXN0U291cmNlSW5mb1IHc291cmNlcw==');

@$core.Deprecated('Use setPlaylistSourcesRequestDescriptor instead')
const SetPlaylistSourcesRequest$json = {
  '1': 'SetPlaylistSourcesRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {
      '1': 'sources',
      '3': 2,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.PlaylistSourceSpec',
      '10': 'sources'
    },
  ],
};

/// Descriptor for `SetPlaylistSourcesRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setPlaylistSourcesRequestDescriptor = $convert.base64Decode(
    'ChlTZXRQbGF5bGlzdFNvdXJjZXNSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW'
    '5jZUlkEj0KB3NvdXJjZXMYAiADKAsyIy5vbW5pX21peF9wbGF5ZXIuUGxheWxpc3RTb3VyY2VT'
    'cGVjUgdzb3VyY2Vz');

@$core.Deprecated('Use setPlaylistSourcesResponseDescriptor instead')
const SetPlaylistSourcesResponse$json = {
  '1': 'SetPlaylistSourcesResponse',
};

/// Descriptor for `SetPlaylistSourcesResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setPlaylistSourcesResponseDescriptor =
    $convert.base64Decode('ChpTZXRQbGF5bGlzdFNvdXJjZXNSZXNwb25zZQ==');

@$core.Deprecated('Use getStatusRequestDescriptor instead')
const GetStatusRequest$json = {
  '1': 'GetStatusRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetStatusRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getStatusRequestDescriptor = $convert.base64Decode(
    'ChBHZXRTdGF0dXNSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use getEqualizerRequestDescriptor instead')
const GetEqualizerRequest$json = {
  '1': 'GetEqualizerRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetEqualizerRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getEqualizerRequestDescriptor = $convert.base64Decode(
    'ChNHZXRFcXVhbGl6ZXJSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use setEqualizerRequestDescriptor instead')
const SetEqualizerRequest$json = {
  '1': 'SetEqualizerRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {
      '1': 'state',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.EqualizerState',
      '10': 'state'
    },
  ],
};

/// Descriptor for `SetEqualizerRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setEqualizerRequestDescriptor = $convert.base64Decode(
    'ChNTZXRFcXVhbGl6ZXJSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlkEj'
    'UKBXN0YXRlGAIgASgLMh8ub21uaV9taXhfcGxheWVyLkVxdWFsaXplclN0YXRlUgVzdGF0ZQ==');

@$core.Deprecated('Use setEqualizerResponseDescriptor instead')
const SetEqualizerResponse$json = {
  '1': 'SetEqualizerResponse',
  '2': [
    {'1': 'saved', '3': 1, '4': 1, '5': 8, '10': 'saved'},
  ],
};

/// Descriptor for `SetEqualizerResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setEqualizerResponseDescriptor =
    $convert.base64Decode(
        'ChRTZXRFcXVhbGl6ZXJSZXNwb25zZRIUCgVzYXZlZBgBIAEoCFIFc2F2ZWQ=');

const $core.Map<$core.String, $core.dynamic> PlaybackServiceBase$json = {
  '1': 'PlaybackService',
  '2': [
    {
      '1': 'Play',
      '2': '.omni_mix_player.PlayRequest',
      '3': '.omni_mix_player.PlayResponse'
    },
    {
      '1': 'Pause',
      '2': '.omni_mix_player.PauseRequest',
      '3': '.omni_mix_player.PauseResponse'
    },
    {
      '1': 'Resume',
      '2': '.omni_mix_player.ResumeRequest',
      '3': '.omni_mix_player.ResumeResponse'
    },
    {
      '1': 'Toggle',
      '2': '.omni_mix_player.ToggleRequest',
      '3': '.omni_mix_player.ToggleResponse'
    },
    {
      '1': 'Next',
      '2': '.omni_mix_player.NextRequest',
      '3': '.omni_mix_player.NextResponse'
    },
    {
      '1': 'Prev',
      '2': '.omni_mix_player.PrevRequest',
      '3': '.omni_mix_player.PrevResponse'
    },
    {
      '1': 'Seek',
      '2': '.omni_mix_player.SeekRequest',
      '3': '.omni_mix_player.SeekResponse'
    },
    {
      '1': 'Stop',
      '2': '.omni_mix_player.StopRequest',
      '3': '.omni_mix_player.StopResponse'
    },
    {
      '1': 'SetVolume',
      '2': '.omni_mix_player.SetVolumeRequest',
      '3': '.omni_mix_player.SetVolumeResponse'
    },
    {
      '1': 'GetVolume',
      '2': '.omni_mix_player.GetVolumeRequest',
      '3': '.omni_mix_player.GetVolumeResponse'
    },
    {
      '1': 'SetTargetLatency',
      '2': '.omni_mix_player.SetTargetLatencyRequest',
      '3': '.omni_mix_player.SetTargetLatencyResponse'
    },
    {
      '1': 'GetTargetLatency',
      '2': '.omni_mix_player.GetTargetLatencyRequest',
      '3': '.omni_mix_player.GetTargetLatencyResponse'
    },
    {
      '1': 'SetShuffle',
      '2': '.omni_mix_player.SetShuffleRequest',
      '3': '.omni_mix_player.SetShuffleResponse'
    },
    {
      '1': 'SetRepeatMode',
      '2': '.omni_mix_player.SetRepeatModeRequest',
      '3': '.omni_mix_player.SetRepeatModeResponse'
    },
    {
      '1': 'GetQueue',
      '2': '.omni_mix_player.GetQueueRequest',
      '3': '.omni_mix_player.GetQueueResponse'
    },
    {
      '1': 'AddToQueue',
      '2': '.omni_mix_player.AddToQueueRequest',
      '3': '.omni_mix_player.AddToQueueResponse'
    },
    {
      '1': 'InsertIntoQueue',
      '2': '.omni_mix_player.InsertIntoQueueRequest',
      '3': '.omni_mix_player.InsertIntoQueueResponse'
    },
    {
      '1': 'SetQueue',
      '2': '.omni_mix_player.SetQueueRequest',
      '3': '.omni_mix_player.SetQueueResponse'
    },
    {
      '1': 'RemoveFromQueue',
      '2': '.omni_mix_player.RemoveFromQueueRequest',
      '3': '.omni_mix_player.RemoveFromQueueResponse'
    },
    {
      '1': 'MoveInQueue',
      '2': '.omni_mix_player.MoveInQueueRequest',
      '3': '.omni_mix_player.MoveInQueueResponse'
    },
    {
      '1': 'ClearQueue',
      '2': '.omni_mix_player.ClearQueueRequest',
      '3': '.omni_mix_player.ClearQueueResponse'
    },
    {
      '1': 'GetHistory',
      '2': '.omni_mix_player.GetHistoryRequest',
      '3': '.omni_mix_player.GetHistoryResponse'
    },
    {
      '1': 'RemoveFromHistory',
      '2': '.omni_mix_player.RemoveFromHistoryRequest',
      '3': '.omni_mix_player.RemoveFromHistoryResponse'
    },
    {
      '1': 'MoveInHistory',
      '2': '.omni_mix_player.MoveInHistoryRequest',
      '3': '.omni_mix_player.MoveInHistoryResponse'
    },
    {
      '1': 'ClearHistory',
      '2': '.omni_mix_player.ClearHistoryRequest',
      '3': '.omni_mix_player.ClearHistoryResponse'
    },
    {
      '1': 'GetPlaylistSources',
      '2': '.omni_mix_player.GetPlaylistSourcesRequest',
      '3': '.omni_mix_player.GetPlaylistSourcesResponse'
    },
    {
      '1': 'SetPlaylistSources',
      '2': '.omni_mix_player.SetPlaylistSourcesRequest',
      '3': '.omni_mix_player.SetPlaylistSourcesResponse'
    },
    {
      '1': 'GetStatus',
      '2': '.omni_mix_player.GetStatusRequest',
      '3': '.omni_mix_player.PlaybackStatus'
    },
    {
      '1': 'GetEqualizer',
      '2': '.omni_mix_player.GetEqualizerRequest',
      '3': '.omni_mix_player.EqualizerState'
    },
    {
      '1': 'SetEqualizer',
      '2': '.omni_mix_player.SetEqualizerRequest',
      '3': '.omni_mix_player.SetEqualizerResponse'
    },
  ],
};

@$core.Deprecated('Use playbackServiceDescriptor instead')
const $core.Map<$core.String, $core.Map<$core.String, $core.dynamic>>
    PlaybackServiceBase$messageJson = {
  '.omni_mix_player.PlayRequest': PlayRequest$json,
  '.omni_mix_player.PlayResponse': PlayResponse$json,
  '.omni_mix_player.PauseRequest': PauseRequest$json,
  '.omni_mix_player.PauseResponse': PauseResponse$json,
  '.omni_mix_player.ResumeRequest': ResumeRequest$json,
  '.omni_mix_player.ResumeResponse': ResumeResponse$json,
  '.omni_mix_player.ToggleRequest': ToggleRequest$json,
  '.omni_mix_player.ToggleResponse': ToggleResponse$json,
  '.omni_mix_player.NextRequest': NextRequest$json,
  '.omni_mix_player.NextResponse': NextResponse$json,
  '.omni_mix_player.PrevRequest': PrevRequest$json,
  '.omni_mix_player.PrevResponse': PrevResponse$json,
  '.omni_mix_player.SeekRequest': SeekRequest$json,
  '.omni_mix_player.SeekResponse': SeekResponse$json,
  '.omni_mix_player.StopRequest': StopRequest$json,
  '.omni_mix_player.StopResponse': StopResponse$json,
  '.omni_mix_player.SetVolumeRequest': SetVolumeRequest$json,
  '.omni_mix_player.SetVolumeResponse': SetVolumeResponse$json,
  '.omni_mix_player.GetVolumeRequest': GetVolumeRequest$json,
  '.omni_mix_player.GetVolumeResponse': GetVolumeResponse$json,
  '.omni_mix_player.SetTargetLatencyRequest': SetTargetLatencyRequest$json,
  '.omni_mix_player.SetTargetLatencyResponse': SetTargetLatencyResponse$json,
  '.omni_mix_player.GetTargetLatencyRequest': GetTargetLatencyRequest$json,
  '.omni_mix_player.GetTargetLatencyResponse': GetTargetLatencyResponse$json,
  '.omni_mix_player.SetShuffleRequest': SetShuffleRequest$json,
  '.omni_mix_player.SetShuffleResponse': SetShuffleResponse$json,
  '.omni_mix_player.SetRepeatModeRequest': SetRepeatModeRequest$json,
  '.omni_mix_player.SetRepeatModeResponse': SetRepeatModeResponse$json,
  '.omni_mix_player.GetQueueRequest': GetQueueRequest$json,
  '.omni_mix_player.GetQueueResponse': GetQueueResponse$json,
  '.omni_mix_player.QueueTrack': QueueTrack$json,
  '.omni_mix_player.AddToQueueRequest': AddToQueueRequest$json,
  '.omni_mix_player.AddToQueueResponse': AddToQueueResponse$json,
  '.omni_mix_player.InsertIntoQueueRequest': InsertIntoQueueRequest$json,
  '.omni_mix_player.InsertIntoQueueResponse': InsertIntoQueueResponse$json,
  '.omni_mix_player.SetQueueRequest': SetQueueRequest$json,
  '.omni_mix_player.SetQueueResponse': SetQueueResponse$json,
  '.omni_mix_player.RemoveFromQueueRequest': RemoveFromQueueRequest$json,
  '.omni_mix_player.RemoveFromQueueResponse': RemoveFromQueueResponse$json,
  '.omni_mix_player.MoveInQueueRequest': MoveInQueueRequest$json,
  '.omni_mix_player.MoveInQueueResponse': MoveInQueueResponse$json,
  '.omni_mix_player.ClearQueueRequest': ClearQueueRequest$json,
  '.omni_mix_player.ClearQueueResponse': ClearQueueResponse$json,
  '.omni_mix_player.GetHistoryRequest': GetHistoryRequest$json,
  '.omni_mix_player.GetHistoryResponse': GetHistoryResponse$json,
  '.omni_mix_player.RemoveFromHistoryRequest': RemoveFromHistoryRequest$json,
  '.omni_mix_player.RemoveFromHistoryResponse': RemoveFromHistoryResponse$json,
  '.omni_mix_player.MoveInHistoryRequest': MoveInHistoryRequest$json,
  '.omni_mix_player.MoveInHistoryResponse': MoveInHistoryResponse$json,
  '.omni_mix_player.ClearHistoryRequest': ClearHistoryRequest$json,
  '.omni_mix_player.ClearHistoryResponse': ClearHistoryResponse$json,
  '.omni_mix_player.GetPlaylistSourcesRequest': GetPlaylistSourcesRequest$json,
  '.omni_mix_player.GetPlaylistSourcesResponse':
      GetPlaylistSourcesResponse$json,
  '.omni_mix_player.PlaylistSourceInfo': PlaylistSourceInfo$json,
  '.omni_mix_player.SetPlaylistSourcesRequest': SetPlaylistSourcesRequest$json,
  '.omni_mix_player.PlaylistSourceSpec': PlaylistSourceSpec$json,
  '.omni_mix_player.SetPlaylistSourcesResponse':
      SetPlaylistSourcesResponse$json,
  '.omni_mix_player.GetStatusRequest': GetStatusRequest$json,
  '.omni_mix_player.PlaybackStatus': $0.PlaybackStatus$json,
  '.omni_mix_player.GetEqualizerRequest': GetEqualizerRequest$json,
  '.omni_mix_player.EqualizerState': $0.EqualizerState$json,
  '.omni_mix_player.EqualizerPoint': $0.EqualizerPoint$json,
  '.omni_mix_player.SetEqualizerRequest': SetEqualizerRequest$json,
  '.omni_mix_player.SetEqualizerResponse': SetEqualizerResponse$json,
};

/// Descriptor for `PlaybackService`. Decode as a `google.protobuf.ServiceDescriptorProto`.
final $typed_data.Uint8List playbackServiceDescriptor = $convert.base64Decode(
    'Cg9QbGF5YmFja1NlcnZpY2USQwoEUGxheRIcLm9tbmlfbWl4X3BsYXllci5QbGF5UmVxdWVzdB'
    'odLm9tbmlfbWl4X3BsYXllci5QbGF5UmVzcG9uc2USRgoFUGF1c2USHS5vbW5pX21peF9wbGF5'
    'ZXIuUGF1c2VSZXF1ZXN0Gh4ub21uaV9taXhfcGxheWVyLlBhdXNlUmVzcG9uc2USSQoGUmVzdW'
    '1lEh4ub21uaV9taXhfcGxheWVyLlJlc3VtZVJlcXVlc3QaHy5vbW5pX21peF9wbGF5ZXIuUmVz'
    'dW1lUmVzcG9uc2USSQoGVG9nZ2xlEh4ub21uaV9taXhfcGxheWVyLlRvZ2dsZVJlcXVlc3QaHy'
    '5vbW5pX21peF9wbGF5ZXIuVG9nZ2xlUmVzcG9uc2USQwoETmV4dBIcLm9tbmlfbWl4X3BsYXll'
    'ci5OZXh0UmVxdWVzdBodLm9tbmlfbWl4X3BsYXllci5OZXh0UmVzcG9uc2USQwoEUHJldhIcLm'
    '9tbmlfbWl4X3BsYXllci5QcmV2UmVxdWVzdBodLm9tbmlfbWl4X3BsYXllci5QcmV2UmVzcG9u'
    'c2USQwoEU2VlaxIcLm9tbmlfbWl4X3BsYXllci5TZWVrUmVxdWVzdBodLm9tbmlfbWl4X3BsYX'
    'llci5TZWVrUmVzcG9uc2USQwoEU3RvcBIcLm9tbmlfbWl4X3BsYXllci5TdG9wUmVxdWVzdBod'
    'Lm9tbmlfbWl4X3BsYXllci5TdG9wUmVzcG9uc2USUgoJU2V0Vm9sdW1lEiEub21uaV9taXhfcG'
    'xheWVyLlNldFZvbHVtZVJlcXVlc3QaIi5vbW5pX21peF9wbGF5ZXIuU2V0Vm9sdW1lUmVzcG9u'
    'c2USUgoJR2V0Vm9sdW1lEiEub21uaV9taXhfcGxheWVyLkdldFZvbHVtZVJlcXVlc3QaIi5vbW'
    '5pX21peF9wbGF5ZXIuR2V0Vm9sdW1lUmVzcG9uc2USZwoQU2V0VGFyZ2V0TGF0ZW5jeRIoLm9t'
    'bmlfbWl4X3BsYXllci5TZXRUYXJnZXRMYXRlbmN5UmVxdWVzdBopLm9tbmlfbWl4X3BsYXllci'
    '5TZXRUYXJnZXRMYXRlbmN5UmVzcG9uc2USZwoQR2V0VGFyZ2V0TGF0ZW5jeRIoLm9tbmlfbWl4'
    'X3BsYXllci5HZXRUYXJnZXRMYXRlbmN5UmVxdWVzdBopLm9tbmlfbWl4X3BsYXllci5HZXRUYX'
    'JnZXRMYXRlbmN5UmVzcG9uc2USVQoKU2V0U2h1ZmZsZRIiLm9tbmlfbWl4X3BsYXllci5TZXRT'
    'aHVmZmxlUmVxdWVzdBojLm9tbmlfbWl4X3BsYXllci5TZXRTaHVmZmxlUmVzcG9uc2USXgoNU2'
    'V0UmVwZWF0TW9kZRIlLm9tbmlfbWl4X3BsYXllci5TZXRSZXBlYXRNb2RlUmVxdWVzdBomLm9t'
    'bmlfbWl4X3BsYXllci5TZXRSZXBlYXRNb2RlUmVzcG9uc2USTwoIR2V0UXVldWUSIC5vbW5pX2'
    '1peF9wbGF5ZXIuR2V0UXVldWVSZXF1ZXN0GiEub21uaV9taXhfcGxheWVyLkdldFF1ZXVlUmVz'
    'cG9uc2USVQoKQWRkVG9RdWV1ZRIiLm9tbmlfbWl4X3BsYXllci5BZGRUb1F1ZXVlUmVxdWVzdB'
    'ojLm9tbmlfbWl4X3BsYXllci5BZGRUb1F1ZXVlUmVzcG9uc2USZAoPSW5zZXJ0SW50b1F1ZXVl'
    'Eicub21uaV9taXhfcGxheWVyLkluc2VydEludG9RdWV1ZVJlcXVlc3QaKC5vbW5pX21peF9wbG'
    'F5ZXIuSW5zZXJ0SW50b1F1ZXVlUmVzcG9uc2USTwoIU2V0UXVldWUSIC5vbW5pX21peF9wbGF5'
    'ZXIuU2V0UXVldWVSZXF1ZXN0GiEub21uaV9taXhfcGxheWVyLlNldFF1ZXVlUmVzcG9uc2USZA'
    'oPUmVtb3ZlRnJvbVF1ZXVlEicub21uaV9taXhfcGxheWVyLlJlbW92ZUZyb21RdWV1ZVJlcXVl'
    'c3QaKC5vbW5pX21peF9wbGF5ZXIuUmVtb3ZlRnJvbVF1ZXVlUmVzcG9uc2USWAoLTW92ZUluUX'
    'VldWUSIy5vbW5pX21peF9wbGF5ZXIuTW92ZUluUXVldWVSZXF1ZXN0GiQub21uaV9taXhfcGxh'
    'eWVyLk1vdmVJblF1ZXVlUmVzcG9uc2USVQoKQ2xlYXJRdWV1ZRIiLm9tbmlfbWl4X3BsYXllci'
    '5DbGVhclF1ZXVlUmVxdWVzdBojLm9tbmlfbWl4X3BsYXllci5DbGVhclF1ZXVlUmVzcG9uc2US'
    'VQoKR2V0SGlzdG9yeRIiLm9tbmlfbWl4X3BsYXllci5HZXRIaXN0b3J5UmVxdWVzdBojLm9tbm'
    'lfbWl4X3BsYXllci5HZXRIaXN0b3J5UmVzcG9uc2USagoRUmVtb3ZlRnJvbUhpc3RvcnkSKS5v'
    'bW5pX21peF9wbGF5ZXIuUmVtb3ZlRnJvbUhpc3RvcnlSZXF1ZXN0Gioub21uaV9taXhfcGxheW'
    'VyLlJlbW92ZUZyb21IaXN0b3J5UmVzcG9uc2USXgoNTW92ZUluSGlzdG9yeRIlLm9tbmlfbWl4'
    'X3BsYXllci5Nb3ZlSW5IaXN0b3J5UmVxdWVzdBomLm9tbmlfbWl4X3BsYXllci5Nb3ZlSW5IaX'
    'N0b3J5UmVzcG9uc2USWwoMQ2xlYXJIaXN0b3J5EiQub21uaV9taXhfcGxheWVyLkNsZWFySGlz'
    'dG9yeVJlcXVlc3QaJS5vbW5pX21peF9wbGF5ZXIuQ2xlYXJIaXN0b3J5UmVzcG9uc2USbQoSR2'
    'V0UGxheWxpc3RTb3VyY2VzEioub21uaV9taXhfcGxheWVyLkdldFBsYXlsaXN0U291cmNlc1Jl'
    'cXVlc3QaKy5vbW5pX21peF9wbGF5ZXIuR2V0UGxheWxpc3RTb3VyY2VzUmVzcG9uc2USbQoSU2'
    'V0UGxheWxpc3RTb3VyY2VzEioub21uaV9taXhfcGxheWVyLlNldFBsYXlsaXN0U291cmNlc1Jl'
    'cXVlc3QaKy5vbW5pX21peF9wbGF5ZXIuU2V0UGxheWxpc3RTb3VyY2VzUmVzcG9uc2USTwoJR2'
    'V0U3RhdHVzEiEub21uaV9taXhfcGxheWVyLkdldFN0YXR1c1JlcXVlc3QaHy5vbW5pX21peF9w'
    'bGF5ZXIuUGxheWJhY2tTdGF0dXMSVQoMR2V0RXF1YWxpemVyEiQub21uaV9taXhfcGxheWVyLk'
    'dldEVxdWFsaXplclJlcXVlc3QaHy5vbW5pX21peF9wbGF5ZXIuRXF1YWxpemVyU3RhdGUSWwoM'
    'U2V0RXF1YWxpemVyEiQub21uaV9taXhfcGxheWVyLlNldEVxdWFsaXplclJlcXVlc3QaJS5vbW'
    '5pX21peF9wbGF5ZXIuU2V0RXF1YWxpemVyUmVzcG9uc2U=');
