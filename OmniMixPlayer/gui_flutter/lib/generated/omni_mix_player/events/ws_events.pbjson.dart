// This is a generated file - do not edit.
//
// Generated from omni_mix_player/events/ws_events.proto.

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

@$core.Deprecated('Use wsEventDescriptor instead')
const WsEvent$json = {
  '1': 'WsEvent',
  '2': [
    {'1': 'type', '3': 1, '4': 1, '5': 9, '10': 'type'},
    {'1': 'timestamp', '3': 2, '4': 1, '5': 3, '10': 'timestamp'},
    {
      '1': 'track_changed',
      '3': 10,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.TrackChangedEvent',
      '9': 0,
      '10': 'trackChanged'
    },
    {
      '1': 'state_changed',
      '3': 11,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.StateChangedEvent',
      '9': 0,
      '10': 'stateChanged'
    },
    {
      '1': 'position_changed',
      '3': 12,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.PositionChangedEvent',
      '9': 0,
      '10': 'positionChanged'
    },
    {
      '1': 'queue_changed',
      '3': 13,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.QueueChangedEvent',
      '9': 0,
      '10': 'queueChanged'
    },
    {
      '1': 'instances_changed',
      '3': 14,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstancesChangedEvent',
      '9': 0,
      '10': 'instancesChanged'
    },
    {
      '1': 'favorite_changed',
      '3': 15,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.FavoriteChangedEvent',
      '9': 0,
      '10': 'favoriteChanged'
    },
    {
      '1': 'exclude_changed',
      '3': 16,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.ExcludeChangedEvent',
      '9': 0,
      '10': 'excludeChanged'
    },
    {
      '1': 'playlist_updated',
      '3': 17,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.PlaylistUpdatedEvent',
      '9': 0,
      '10': 'playlistUpdated'
    },
    {
      '1': 'module_changed',
      '3': 18,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.ModuleChangedEvent',
      '9': 0,
      '10': 'moduleChanged'
    },
    {
      '1': 'profile_changed',
      '3': 19,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.ProfileChangedEvent',
      '9': 0,
      '10': 'profileChanged'
    },
    {
      '1': 'backend_state',
      '3': 20,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.BackendStateEvent',
      '9': 0,
      '10': 'backendState'
    },
    {
      '1': 'volume_changed',
      '3': 21,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.VolumeChangedEvent',
      '9': 0,
      '10': 'volumeChanged'
    },
    {
      '1': 'latency_changed',
      '3': 22,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.LatencyChangedEvent',
      '9': 0,
      '10': 'latencyChanged'
    },
    {
      '1': 'eq_changed',
      '3': 23,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.EqualizerChangedEvent',
      '9': 0,
      '10': 'eqChanged'
    },
  ],
  '8': [
    {'1': 'event'},
  ],
};

/// Descriptor for `WsEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List wsEventDescriptor = $convert.base64Decode(
    'CgdXc0V2ZW50EhIKBHR5cGUYASABKAlSBHR5cGUSHAoJdGltZXN0YW1wGAIgASgDUgl0aW1lc3'
    'RhbXASSQoNdHJhY2tfY2hhbmdlZBgKIAEoCzIiLm9tbmlfbWl4X3BsYXllci5UcmFja0NoYW5n'
    'ZWRFdmVudEgAUgx0cmFja0NoYW5nZWQSSQoNc3RhdGVfY2hhbmdlZBgLIAEoCzIiLm9tbmlfbW'
    'l4X3BsYXllci5TdGF0ZUNoYW5nZWRFdmVudEgAUgxzdGF0ZUNoYW5nZWQSUgoQcG9zaXRpb25f'
    'Y2hhbmdlZBgMIAEoCzIlLm9tbmlfbWl4X3BsYXllci5Qb3NpdGlvbkNoYW5nZWRFdmVudEgAUg'
    '9wb3NpdGlvbkNoYW5nZWQSSQoNcXVldWVfY2hhbmdlZBgNIAEoCzIiLm9tbmlfbWl4X3BsYXll'
    'ci5RdWV1ZUNoYW5nZWRFdmVudEgAUgxxdWV1ZUNoYW5nZWQSVQoRaW5zdGFuY2VzX2NoYW5nZW'
    'QYDiABKAsyJi5vbW5pX21peF9wbGF5ZXIuSW5zdGFuY2VzQ2hhbmdlZEV2ZW50SABSEGluc3Rh'
    'bmNlc0NoYW5nZWQSUgoQZmF2b3JpdGVfY2hhbmdlZBgPIAEoCzIlLm9tbmlfbWl4X3BsYXllci'
    '5GYXZvcml0ZUNoYW5nZWRFdmVudEgAUg9mYXZvcml0ZUNoYW5nZWQSTwoPZXhjbHVkZV9jaGFu'
    'Z2VkGBAgASgLMiQub21uaV9taXhfcGxheWVyLkV4Y2x1ZGVDaGFuZ2VkRXZlbnRIAFIOZXhjbH'
    'VkZUNoYW5nZWQSUgoQcGxheWxpc3RfdXBkYXRlZBgRIAEoCzIlLm9tbmlfbWl4X3BsYXllci5Q'
    'bGF5bGlzdFVwZGF0ZWRFdmVudEgAUg9wbGF5bGlzdFVwZGF0ZWQSTAoObW9kdWxlX2NoYW5nZW'
    'QYEiABKAsyIy5vbW5pX21peF9wbGF5ZXIuTW9kdWxlQ2hhbmdlZEV2ZW50SABSDW1vZHVsZUNo'
    'YW5nZWQSTwoPcHJvZmlsZV9jaGFuZ2VkGBMgASgLMiQub21uaV9taXhfcGxheWVyLlByb2ZpbG'
    'VDaGFuZ2VkRXZlbnRIAFIOcHJvZmlsZUNoYW5nZWQSSQoNYmFja2VuZF9zdGF0ZRgUIAEoCzIi'
    'Lm9tbmlfbWl4X3BsYXllci5CYWNrZW5kU3RhdGVFdmVudEgAUgxiYWNrZW5kU3RhdGUSTAoOdm'
    '9sdW1lX2NoYW5nZWQYFSABKAsyIy5vbW5pX21peF9wbGF5ZXIuVm9sdW1lQ2hhbmdlZEV2ZW50'
    'SABSDXZvbHVtZUNoYW5nZWQSTwoPbGF0ZW5jeV9jaGFuZ2VkGBYgASgLMiQub21uaV9taXhfcG'
    'xheWVyLkxhdGVuY3lDaGFuZ2VkRXZlbnRIAFIObGF0ZW5jeUNoYW5nZWQSRwoKZXFfY2hhbmdl'
    'ZBgXIAEoCzImLm9tbmlfbWl4X3BsYXllci5FcXVhbGl6ZXJDaGFuZ2VkRXZlbnRIAFIJZXFDaG'
    'FuZ2VkQgcKBWV2ZW50');

@$core.Deprecated('Use trackChangedEventDescriptor instead')
const TrackChangedEvent$json = {
  '1': 'TrackChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'uuid', '3': 2, '4': 1, '5': 9, '10': 'uuid'},
    {'1': 'title', '3': 3, '4': 1, '5': 9, '10': 'title'},
    {'1': 'artist', '3': 4, '4': 1, '5': 9, '10': 'artist'},
    {'1': 'album_id', '3': 5, '4': 1, '5': 9, '10': 'albumId'},
    {'1': 'duration', '3': 6, '4': 1, '5': 2, '10': 'duration'},
    {'1': 'module_id', '3': 7, '4': 1, '5': 9, '10': 'moduleId'},
  ],
};

/// Descriptor for `TrackChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List trackChangedEventDescriptor = $convert.base64Decode(
    'ChFUcmFja0NoYW5nZWRFdmVudBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBISCg'
    'R1dWlkGAIgASgJUgR1dWlkEhQKBXRpdGxlGAMgASgJUgV0aXRsZRIWCgZhcnRpc3QYBCABKAlS'
    'BmFydGlzdBIZCghhbGJ1bV9pZBgFIAEoCVIHYWxidW1JZBIaCghkdXJhdGlvbhgGIAEoAlIIZH'
    'VyYXRpb24SGwoJbW9kdWxlX2lkGAcgASgJUghtb2R1bGVJZA==');

@$core.Deprecated('Use stateChangedEventDescriptor instead')
const StateChangedEvent$json = {
  '1': 'StateChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'state', '3': 2, '4': 1, '5': 5, '10': 'state'},
  ],
};

/// Descriptor for `StateChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List stateChangedEventDescriptor = $convert.base64Decode(
    'ChFTdGF0ZUNoYW5nZWRFdmVudBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBIUCg'
    'VzdGF0ZRgCIAEoBVIFc3RhdGU=');

@$core.Deprecated('Use positionChangedEventDescriptor instead')
const PositionChangedEvent$json = {
  '1': 'PositionChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'position', '3': 2, '4': 1, '5': 2, '10': 'position'},
  ],
};

/// Descriptor for `PositionChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List positionChangedEventDescriptor = $convert.base64Decode(
    'ChRQb3NpdGlvbkNoYW5nZWRFdmVudBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZB'
    'IaCghwb3NpdGlvbhgCIAEoAlIIcG9zaXRpb24=');

@$core.Deprecated('Use queueChangedEventDescriptor instead')
const QueueChangedEvent$json = {
  '1': 'QueueChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'change_type', '3': 2, '4': 1, '5': 9, '10': 'changeType'},
    {'1': 'queue_length', '3': 3, '4': 1, '5': 5, '10': 'queueLength'},
  ],
};

/// Descriptor for `QueueChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List queueChangedEventDescriptor = $convert.base64Decode(
    'ChFRdWV1ZUNoYW5nZWRFdmVudBIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2VJZBIfCg'
    'tjaGFuZ2VfdHlwZRgCIAEoCVIKY2hhbmdlVHlwZRIhCgxxdWV1ZV9sZW5ndGgYAyABKAVSC3F1'
    'ZXVlTGVuZ3Ro');

@$core.Deprecated('Use instancesChangedEventDescriptor instead')
const InstancesChangedEvent$json = {
  '1': 'InstancesChangedEvent',
  '2': [
    {
      '1': 'instances',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.InstanceSummary',
      '10': 'instances'
    },
  ],
};

/// Descriptor for `InstancesChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instancesChangedEventDescriptor = $convert.base64Decode(
    'ChVJbnN0YW5jZXNDaGFuZ2VkRXZlbnQSPgoJaW5zdGFuY2VzGAEgAygLMiAub21uaV9taXhfcG'
    'xheWVyLkluc3RhbmNlU3VtbWFyeVIJaW5zdGFuY2Vz');

@$core.Deprecated('Use favoriteChangedEventDescriptor instead')
const FavoriteChangedEvent$json = {
  '1': 'FavoriteChangedEvent',
  '2': [
    {'1': 'uuid', '3': 1, '4': 1, '5': 9, '10': 'uuid'},
    {'1': 'is_favorite', '3': 2, '4': 1, '5': 8, '10': 'isFavorite'},
    {'1': 'module_id', '3': 3, '4': 1, '5': 9, '10': 'moduleId'},
  ],
};

/// Descriptor for `FavoriteChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List favoriteChangedEventDescriptor = $convert.base64Decode(
    'ChRGYXZvcml0ZUNoYW5nZWRFdmVudBISCgR1dWlkGAEgASgJUgR1dWlkEh8KC2lzX2Zhdm9yaX'
    'RlGAIgASgIUgppc0Zhdm9yaXRlEhsKCW1vZHVsZV9pZBgDIAEoCVIIbW9kdWxlSWQ=');

@$core.Deprecated('Use excludeChangedEventDescriptor instead')
const ExcludeChangedEvent$json = {
  '1': 'ExcludeChangedEvent',
  '2': [
    {'1': 'uuid', '3': 1, '4': 1, '5': 9, '10': 'uuid'},
    {'1': 'is_excluded', '3': 2, '4': 1, '5': 8, '10': 'isExcluded'},
    {'1': 'module_id', '3': 3, '4': 1, '5': 9, '10': 'moduleId'},
  ],
};

/// Descriptor for `ExcludeChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List excludeChangedEventDescriptor = $convert.base64Decode(
    'ChNFeGNsdWRlQ2hhbmdlZEV2ZW50EhIKBHV1aWQYASABKAlSBHV1aWQSHwoLaXNfZXhjbHVkZW'
    'QYAiABKAhSCmlzRXhjbHVkZWQSGwoJbW9kdWxlX2lkGAMgASgJUghtb2R1bGVJZA==');

@$core.Deprecated('Use playlistUpdatedEventDescriptor instead')
const PlaylistUpdatedEvent$json = {
  '1': 'PlaylistUpdatedEvent',
  '2': [
    {'1': 'source_ref_id', '3': 1, '4': 1, '5': 9, '10': 'sourceRefId'},
    {'1': 'song_count', '3': 2, '4': 1, '5': 5, '10': 'songCount'},
    {'1': 'update_type', '3': 3, '4': 1, '5': 9, '10': 'updateType'},
  ],
};

/// Descriptor for `PlaylistUpdatedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistUpdatedEventDescriptor = $convert.base64Decode(
    'ChRQbGF5bGlzdFVwZGF0ZWRFdmVudBIiCg1zb3VyY2VfcmVmX2lkGAEgASgJUgtzb3VyY2VSZW'
    'ZJZBIdCgpzb25nX2NvdW50GAIgASgFUglzb25nQ291bnQSHwoLdXBkYXRlX3R5cGUYAyABKAlS'
    'CnVwZGF0ZVR5cGU=');

@$core.Deprecated('Use moduleChangedEventDescriptor instead')
const ModuleChangedEvent$json = {
  '1': 'ModuleChangedEvent',
  '2': [
    {'1': 'module_id', '3': 1, '4': 1, '5': 9, '10': 'moduleId'},
    {'1': 'enabled', '3': 2, '4': 1, '5': 8, '10': 'enabled'},
    {'1': 'display_name', '3': 3, '4': 1, '5': 9, '10': 'displayName'},
  ],
};

/// Descriptor for `ModuleChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List moduleChangedEventDescriptor = $convert.base64Decode(
    'ChJNb2R1bGVDaGFuZ2VkRXZlbnQSGwoJbW9kdWxlX2lkGAEgASgJUghtb2R1bGVJZBIYCgdlbm'
    'FibGVkGAIgASgIUgdlbmFibGVkEiEKDGRpc3BsYXlfbmFtZRgDIAEoCVILZGlzcGxheU5hbWU=');

@$core.Deprecated('Use profileChangedEventDescriptor instead')
const ProfileChangedEvent$json = {
  '1': 'ProfileChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `ProfileChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List profileChangedEventDescriptor = $convert.base64Decode(
    'ChNQcm9maWxlQ2hhbmdlZEV2ZW50Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlk');

@$core.Deprecated('Use backendStateEventDescriptor instead')
const BackendStateEvent$json = {
  '1': 'BackendStateEvent',
  '2': [
    {'1': 'running', '3': 1, '4': 1, '5': 8, '10': 'running'},
  ],
};

/// Descriptor for `BackendStateEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List backendStateEventDescriptor = $convert.base64Decode(
    'ChFCYWNrZW5kU3RhdGVFdmVudBIYCgdydW5uaW5nGAEgASgIUgdydW5uaW5n');

@$core.Deprecated('Use volumeChangedEventDescriptor instead')
const VolumeChangedEvent$json = {
  '1': 'VolumeChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'volume', '3': 2, '4': 1, '5': 2, '10': 'volume'},
  ],
};

/// Descriptor for `VolumeChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List volumeChangedEventDescriptor = $convert.base64Decode(
    'ChJWb2x1bWVDaGFuZ2VkRXZlbnQSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSWQSFg'
    'oGdm9sdW1lGAIgASgCUgZ2b2x1bWU=');

@$core.Deprecated('Use latencyChangedEventDescriptor instead')
const LatencyChangedEvent$json = {
  '1': 'LatencyChangedEvent',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'latency', '3': 2, '4': 1, '5': 2, '10': 'latency'},
  ],
};

/// Descriptor for `LatencyChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List latencyChangedEventDescriptor = $convert.base64Decode(
    'ChNMYXRlbmN5Q2hhbmdlZEV2ZW50Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW5jZUlkEh'
    'gKB2xhdGVuY3kYAiABKAJSB2xhdGVuY3k=');

@$core.Deprecated('Use equalizerChangedEventDescriptor instead')
const EqualizerChangedEvent$json = {
  '1': 'EqualizerChangedEvent',
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

/// Descriptor for `EqualizerChangedEvent`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List equalizerChangedEventDescriptor = $convert.base64Decode(
    'ChVFcXVhbGl6ZXJDaGFuZ2VkRXZlbnQSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3RhbmNlSW'
    'QSNQoFc3RhdGUYAiABKAsyHy5vbW5pX21peF9wbGF5ZXIuRXF1YWxpemVyU3RhdGVSBXN0YXRl');
