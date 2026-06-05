// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/instance.proto.

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

import '../models/common.pbjson.dart' as $1;
import '../models/instance.pbjson.dart' as $0;

@$core.Deprecated('Use deleteInstanceRequestDescriptor instead')
const DeleteInstanceRequest$json = {
  '1': 'DeleteInstanceRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `DeleteInstanceRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteInstanceRequestDescriptor = $convert.base64Decode(
    'ChVEZWxldGVJbnN0YW5jZVJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSW'
    'Q=');

@$core.Deprecated('Use deleteInstanceResponseDescriptor instead')
const DeleteInstanceResponse$json = {
  '1': 'DeleteInstanceResponse',
  '2': [
    {'1': 'deleted', '3': 1, '4': 1, '5': 8, '10': 'deleted'},
  ],
};

/// Descriptor for `DeleteInstanceResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteInstanceResponseDescriptor =
    $convert.base64Decode(
        'ChZEZWxldGVJbnN0YW5jZVJlc3BvbnNlEhgKB2RlbGV0ZWQYASABKAhSB2RlbGV0ZWQ=');

@$core.Deprecated('Use listInstancesRequestDescriptor instead')
const ListInstancesRequest$json = {
  '1': 'ListInstancesRequest',
};

/// Descriptor for `ListInstancesRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List listInstancesRequestDescriptor =
    $convert.base64Decode('ChRMaXN0SW5zdGFuY2VzUmVxdWVzdA==');

@$core.Deprecated('Use getProfileRequestDescriptor instead')
const GetProfileRequest$json = {
  '1': 'GetProfileRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetProfileRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getProfileRequestDescriptor = $convert.base64Decode(
    'ChFHZXRQcm9maWxlUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZA==');

@$core.Deprecated('Use updateProfileRequestDescriptor instead')
const UpdateProfileRequest$json = {
  '1': 'UpdateProfileRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {
      '1': 'profile',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstanceProfile',
      '10': 'profile'
    },
  ],
};

/// Descriptor for `UpdateProfileRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List updateProfileRequestDescriptor = $convert.base64Decode(
    'ChRVcGRhdGVQcm9maWxlUmVxdWVzdBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZB'
    'I6Cgdwcm9maWxlGAIgASgLMiAub21uaV9taXhfcGxheWVyLkluc3RhbmNlUHJvZmlsZVIHcHJv'
    'ZmlsZQ==');

@$core.Deprecated('Use updateProfileResponseDescriptor instead')
const UpdateProfileResponse$json = {
  '1': 'UpdateProfileResponse',
  '2': [
    {'1': 'saved', '3': 1, '4': 1, '5': 8, '10': 'saved'},
  ],
};

/// Descriptor for `UpdateProfileResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List updateProfileResponseDescriptor =
    $convert.base64Decode(
        'ChVVcGRhdGVQcm9maWxlUmVzcG9uc2USFAoFc2F2ZWQYASABKAhSBXNhdmVk');

@$core.Deprecated('Use getInstanceStatusRequestDescriptor instead')
const GetInstanceStatusRequest$json = {
  '1': 'GetInstanceStatusRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `GetInstanceStatusRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getInstanceStatusRequestDescriptor =
    $convert.base64Decode(
        'ChhHZXRJbnN0YW5jZVN0YXR1c1JlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3Rhbm'
        'NlSWQ=');

@$core.Deprecated('Use archiveInstanceRequestDescriptor instead')
const ArchiveInstanceRequest$json = {
  '1': 'ArchiveInstanceRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'label', '3': 2, '4': 1, '5': 9, '10': 'label'},
  ],
};

/// Descriptor for `ArchiveInstanceRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List archiveInstanceRequestDescriptor =
    $convert.base64Decode(
        'ChZBcmNoaXZlSW5zdGFuY2VSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZU'
        'lkEhQKBWxhYmVsGAIgASgJUgVsYWJlbA==');

@$core.Deprecated('Use archiveInstanceResponseDescriptor instead')
const ArchiveInstanceResponse$json = {
  '1': 'ArchiveInstanceResponse',
  '2': [
    {'1': 'archived', '3': 1, '4': 1, '5': 8, '10': 'archived'},
    {
      '1': 'archive',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstanceProfile',
      '10': 'archive'
    },
  ],
};

/// Descriptor for `ArchiveInstanceResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List archiveInstanceResponseDescriptor = $convert.base64Decode(
    'ChdBcmNoaXZlSW5zdGFuY2VSZXNwb25zZRIaCghhcmNoaXZlZBgBIAEoCFIIYXJjaGl2ZWQSOg'
    'oHYXJjaGl2ZRgCIAEoCzIgLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZVByb2ZpbGVSB2FyY2hp'
    'dmU=');

@$core.Deprecated('Use listArchivesRequestDescriptor instead')
const ListArchivesRequest$json = {
  '1': 'ListArchivesRequest',
};

/// Descriptor for `ListArchivesRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List listArchivesRequestDescriptor =
    $convert.base64Decode('ChNMaXN0QXJjaGl2ZXNSZXF1ZXN0');

@$core.Deprecated('Use listArchivesResponseDescriptor instead')
const ListArchivesResponse$json = {
  '1': 'ListArchivesResponse',
  '2': [
    {
      '1': 'archives',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.InstanceProfile',
      '10': 'archives'
    },
  ],
};

/// Descriptor for `ListArchivesResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List listArchivesResponseDescriptor = $convert.base64Decode(
    'ChRMaXN0QXJjaGl2ZXNSZXNwb25zZRI8CghhcmNoaXZlcxgBIAMoCzIgLm9tbmlfbWl4X3BsYX'
    'llci5JbnN0YW5jZVByb2ZpbGVSCGFyY2hpdmVz');

@$core.Deprecated('Use getArchiveRequestDescriptor instead')
const GetArchiveRequest$json = {
  '1': 'GetArchiveRequest',
  '2': [
    {'1': 'archive_id', '3': 1, '4': 1, '5': 9, '10': 'archiveId'},
  ],
};

/// Descriptor for `GetArchiveRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getArchiveRequestDescriptor = $convert.base64Decode(
    'ChFHZXRBcmNoaXZlUmVxdWVzdBIdCgphcmNoaXZlX2lkGAEgASgJUglhcmNoaXZlSWQ=');

@$core.Deprecated('Use deleteArchiveRequestDescriptor instead')
const DeleteArchiveRequest$json = {
  '1': 'DeleteArchiveRequest',
  '2': [
    {'1': 'archive_id', '3': 1, '4': 1, '5': 9, '10': 'archiveId'},
  ],
};

/// Descriptor for `DeleteArchiveRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteArchiveRequestDescriptor = $convert.base64Decode(
    'ChREZWxldGVBcmNoaXZlUmVxdWVzdBIdCgphcmNoaXZlX2lkGAEgASgJUglhcmNoaXZlSWQ=');

@$core.Deprecated('Use deleteArchiveResponseDescriptor instead')
const DeleteArchiveResponse$json = {
  '1': 'DeleteArchiveResponse',
  '2': [
    {'1': 'deleted', '3': 1, '4': 1, '5': 8, '10': 'deleted'},
  ],
};

/// Descriptor for `DeleteArchiveResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteArchiveResponseDescriptor =
    $convert.base64Decode(
        'ChVEZWxldGVBcmNoaXZlUmVzcG9uc2USGAoHZGVsZXRlZBgBIAEoCFIHZGVsZXRlZA==');

@$core.Deprecated('Use inheritFromArchiveRequestDescriptor instead')
const InheritFromArchiveRequest$json = {
  '1': 'InheritFromArchiveRequest',
  '2': [
    {'1': 'new_instance_id', '3': 1, '4': 1, '5': 9, '10': 'newInstanceId'},
    {'1': 'archive_id', '3': 2, '4': 1, '5': 9, '10': 'archiveId'},
  ],
};

/// Descriptor for `InheritFromArchiveRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List inheritFromArchiveRequestDescriptor =
    $convert.base64Decode(
        'ChlJbmhlcml0RnJvbUFyY2hpdmVSZXF1ZXN0EiYKD25ld19pbnN0YW5jZV9pZBgBIAEoCVINbm'
        'V3SW5zdGFuY2VJZBIdCgphcmNoaXZlX2lkGAIgASgJUglhcmNoaXZlSWQ=');

@$core.Deprecated('Use inheritFromArchiveResponseDescriptor instead')
const InheritFromArchiveResponse$json = {
  '1': 'InheritFromArchiveResponse',
  '2': [
    {'1': 'inherited', '3': 1, '4': 1, '5': 8, '10': 'inherited'},
    {
      '1': 'profile',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstanceProfile',
      '10': 'profile'
    },
  ],
};

/// Descriptor for `InheritFromArchiveResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List inheritFromArchiveResponseDescriptor =
    $convert.base64Decode(
        'ChpJbmhlcml0RnJvbUFyY2hpdmVSZXNwb25zZRIcCglpbmhlcml0ZWQYASABKAhSCWluaGVyaX'
        'RlZBI6Cgdwcm9maWxlGAIgASgLMiAub21uaV9taXhfcGxheWVyLkluc3RhbmNlUHJvZmlsZVIH'
        'cHJvZmlsZQ==');

const $core.Map<$core.String, $core.dynamic> InstanceServiceBase$json = {
  '1': 'InstanceService',
  '2': [
    {
      '1': 'Connect',
      '2': '.omni_mix_player.InstanceConnectRequest',
      '3': '.omni_mix_player.InstanceConnectResponse'
    },
    {
      '1': 'Heartbeat',
      '2': '.omni_mix_player.InstanceHeartbeatRequest',
      '3': '.omni_mix_player.InstanceHeartbeatResponse'
    },
    {
      '1': 'Disconnect',
      '2': '.omni_mix_player.InstanceDisconnectRequest',
      '3': '.omni_mix_player.InstanceDisconnectResponse'
    },
    {
      '1': 'DeleteInstance',
      '2': '.omni_mix_player.DeleteInstanceRequest',
      '3': '.omni_mix_player.DeleteInstanceResponse'
    },
    {
      '1': 'ListInstances',
      '2': '.omni_mix_player.ListInstancesRequest',
      '3': '.omni_mix_player.ListInstancesResponse'
    },
    {
      '1': 'GetProfile',
      '2': '.omni_mix_player.GetProfileRequest',
      '3': '.omni_mix_player.InstanceProfile'
    },
    {
      '1': 'UpdateProfile',
      '2': '.omni_mix_player.UpdateProfileRequest',
      '3': '.omni_mix_player.UpdateProfileResponse'
    },
    {
      '1': 'GetStatus',
      '2': '.omni_mix_player.GetInstanceStatusRequest',
      '3': '.omni_mix_player.PlaybackStatus'
    },
    {
      '1': 'ArchiveInstance',
      '2': '.omni_mix_player.ArchiveInstanceRequest',
      '3': '.omni_mix_player.ArchiveInstanceResponse'
    },
    {
      '1': 'ListArchives',
      '2': '.omni_mix_player.ListArchivesRequest',
      '3': '.omni_mix_player.ListArchivesResponse'
    },
    {
      '1': 'GetArchive',
      '2': '.omni_mix_player.GetArchiveRequest',
      '3': '.omni_mix_player.InstanceProfile'
    },
    {
      '1': 'DeleteArchive',
      '2': '.omni_mix_player.DeleteArchiveRequest',
      '3': '.omni_mix_player.DeleteArchiveResponse'
    },
    {
      '1': 'InheritFromArchive',
      '2': '.omni_mix_player.InheritFromArchiveRequest',
      '3': '.omni_mix_player.InheritFromArchiveResponse'
    },
  ],
};

@$core.Deprecated('Use instanceServiceDescriptor instead')
const $core.Map<$core.String, $core.Map<$core.String, $core.dynamic>>
    InstanceServiceBase$messageJson = {
  '.omni_mix_player.InstanceConnectRequest': $0.InstanceConnectRequest$json,
  '.omni_mix_player.InstanceCapabilities': $0.InstanceCapabilities$json,
  '.omni_mix_player.InstanceConnectResponse': $0.InstanceConnectResponse$json,
  '.omni_mix_player.InstanceProfile': $0.InstanceProfile$json,
  '.omni_mix_player.EqualizerState': $0.EqualizerState$json,
  '.omni_mix_player.EqualizerPoint': $0.EqualizerPoint$json,
  '.omni_mix_player.OmniTimestamp': $1.OmniTimestamp$json,
  '.omni_mix_player.PlaybackTimelineState': $0.PlaybackTimelineState$json,
  '.omni_mix_player.PlaylistSourceState': $0.PlaylistSourceState$json,
  '.omni_mix_player.InstanceHeartbeatRequest': $0.InstanceHeartbeatRequest$json,
  '.omni_mix_player.InstanceHeartbeatResponse':
      $0.InstanceHeartbeatResponse$json,
  '.omni_mix_player.InstanceDisconnectRequest':
      $0.InstanceDisconnectRequest$json,
  '.omni_mix_player.InstanceDisconnectResponse':
      $0.InstanceDisconnectResponse$json,
  '.omni_mix_player.DeleteInstanceRequest': DeleteInstanceRequest$json,
  '.omni_mix_player.DeleteInstanceResponse': DeleteInstanceResponse$json,
  '.omni_mix_player.ListInstancesRequest': ListInstancesRequest$json,
  '.omni_mix_player.ListInstancesResponse': $0.ListInstancesResponse$json,
  '.omni_mix_player.InstanceSummary': $0.InstanceSummary$json,
  '.omni_mix_player.GetProfileRequest': GetProfileRequest$json,
  '.omni_mix_player.UpdateProfileRequest': UpdateProfileRequest$json,
  '.omni_mix_player.UpdateProfileResponse': UpdateProfileResponse$json,
  '.omni_mix_player.GetInstanceStatusRequest': GetInstanceStatusRequest$json,
  '.omni_mix_player.PlaybackStatus': $0.PlaybackStatus$json,
  '.omni_mix_player.ArchiveInstanceRequest': ArchiveInstanceRequest$json,
  '.omni_mix_player.ArchiveInstanceResponse': ArchiveInstanceResponse$json,
  '.omni_mix_player.ListArchivesRequest': ListArchivesRequest$json,
  '.omni_mix_player.ListArchivesResponse': ListArchivesResponse$json,
  '.omni_mix_player.GetArchiveRequest': GetArchiveRequest$json,
  '.omni_mix_player.DeleteArchiveRequest': DeleteArchiveRequest$json,
  '.omni_mix_player.DeleteArchiveResponse': DeleteArchiveResponse$json,
  '.omni_mix_player.InheritFromArchiveRequest': InheritFromArchiveRequest$json,
  '.omni_mix_player.InheritFromArchiveResponse':
      InheritFromArchiveResponse$json,
};

/// Descriptor for `InstanceService`. Decode as a `google.protobuf.ServiceDescriptorProto`.
final $typed_data.Uint8List instanceServiceDescriptor = $convert.base64Decode(
    'Cg9JbnN0YW5jZVNlcnZpY2USXAoHQ29ubmVjdBInLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZU'
    'Nvbm5lY3RSZXF1ZXN0Gigub21uaV9taXhfcGxheWVyLkluc3RhbmNlQ29ubmVjdFJlc3BvbnNl'
    'EmIKCUhlYXJ0YmVhdBIpLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZUhlYXJ0YmVhdFJlcXVlc3'
    'QaKi5vbW5pX21peF9wbGF5ZXIuSW5zdGFuY2VIZWFydGJlYXRSZXNwb25zZRJlCgpEaXNjb25u'
    'ZWN0Eioub21uaV9taXhfcGxheWVyLkluc3RhbmNlRGlzY29ubmVjdFJlcXVlc3QaKy5vbW5pX2'
    '1peF9wbGF5ZXIuSW5zdGFuY2VEaXNjb25uZWN0UmVzcG9uc2USYQoORGVsZXRlSW5zdGFuY2US'
    'Ji5vbW5pX21peF9wbGF5ZXIuRGVsZXRlSW5zdGFuY2VSZXF1ZXN0Gicub21uaV9taXhfcGxheW'
    'VyLkRlbGV0ZUluc3RhbmNlUmVzcG9uc2USXgoNTGlzdEluc3RhbmNlcxIlLm9tbmlfbWl4X3Bs'
    'YXllci5MaXN0SW5zdGFuY2VzUmVxdWVzdBomLm9tbmlfbWl4X3BsYXllci5MaXN0SW5zdGFuY2'
    'VzUmVzcG9uc2USUgoKR2V0UHJvZmlsZRIiLm9tbmlfbWl4X3BsYXllci5HZXRQcm9maWxlUmVx'
    'dWVzdBogLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZVByb2ZpbGUSXgoNVXBkYXRlUHJvZmlsZR'
    'IlLm9tbmlfbWl4X3BsYXllci5VcGRhdGVQcm9maWxlUmVxdWVzdBomLm9tbmlfbWl4X3BsYXll'
    'ci5VcGRhdGVQcm9maWxlUmVzcG9uc2USVwoJR2V0U3RhdHVzEikub21uaV9taXhfcGxheWVyLk'
    'dldEluc3RhbmNlU3RhdHVzUmVxdWVzdBofLm9tbmlfbWl4X3BsYXllci5QbGF5YmFja1N0YXR1'
    'cxJkCg9BcmNoaXZlSW5zdGFuY2USJy5vbW5pX21peF9wbGF5ZXIuQXJjaGl2ZUluc3RhbmNlUm'
    'VxdWVzdBooLm9tbmlfbWl4X3BsYXllci5BcmNoaXZlSW5zdGFuY2VSZXNwb25zZRJbCgxMaXN0'
    'QXJjaGl2ZXMSJC5vbW5pX21peF9wbGF5ZXIuTGlzdEFyY2hpdmVzUmVxdWVzdBolLm9tbmlfbW'
    'l4X3BsYXllci5MaXN0QXJjaGl2ZXNSZXNwb25zZRJSCgpHZXRBcmNoaXZlEiIub21uaV9taXhf'
    'cGxheWVyLkdldEFyY2hpdmVSZXF1ZXN0GiAub21uaV9taXhfcGxheWVyLkluc3RhbmNlUHJvZm'
    'lsZRJeCg1EZWxldGVBcmNoaXZlEiUub21uaV9taXhfcGxheWVyLkRlbGV0ZUFyY2hpdmVSZXF1'
    'ZXN0GiYub21uaV9taXhfcGxheWVyLkRlbGV0ZUFyY2hpdmVSZXNwb25zZRJtChJJbmhlcml0Rn'
    'JvbUFyY2hpdmUSKi5vbW5pX21peF9wbGF5ZXIuSW5oZXJpdEZyb21BcmNoaXZlUmVxdWVzdBor'
    'Lm9tbmlfbWl4X3BsYXllci5Jbmhlcml0RnJvbUFyY2hpdmVSZXNwb25zZQ==');
