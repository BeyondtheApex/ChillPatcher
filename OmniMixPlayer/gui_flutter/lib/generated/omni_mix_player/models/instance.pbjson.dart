// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/instance.proto.

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

@$core.Deprecated('Use playlistSourceKindDescriptor instead')
const PlaylistSourceKind$json = {
  '1': 'PlaylistSourceKind',
  '2': [
    {'1': 'PLAYLIST_SOURCE_KIND_UNSPECIFIED', '2': 0},
    {'1': 'PLAYLIST_SOURCE_KIND_TAG', '2': 1},
    {'1': 'PLAYLIST_SOURCE_KIND_ALBUM', '2': 2},
    {'1': 'PLAYLIST_SOURCE_KIND_PLAYLIST', '2': 3},
    {'1': 'PLAYLIST_SOURCE_KIND_TRACK', '2': 4},
  ],
};

/// Descriptor for `PlaylistSourceKind`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List playlistSourceKindDescriptor = $convert.base64Decode(
    'ChJQbGF5bGlzdFNvdXJjZUtpbmQSJAogUExBWUxJU1RfU09VUkNFX0tJTkRfVU5TUEVDSUZJRU'
    'QQABIcChhQTEFZTElTVF9TT1VSQ0VfS0lORF9UQUcQARIeChpQTEFZTElTVF9TT1VSQ0VfS0lO'
    'RF9BTEJVTRACEiEKHVBMQVlMSVNUX1NPVVJDRV9LSU5EX1BMQVlMSVNUEAMSHgoaUExBWUxJU1'
    'RfU09VUkNFX0tJTkRfVFJBQ0sQBA==');

@$core.Deprecated('Use instanceCapabilitiesDescriptor instead')
const InstanceCapabilities$json = {
  '1': 'InstanceCapabilities',
  '2': [
    {
      '1': 'server_controlled_playback',
      '3': 1,
      '4': 1,
      '5': 8,
      '10': 'serverControlledPlayback'
    },
    {'1': 'queue_management', '3': 3, '4': 1, '5': 8, '10': 'queueManagement'},
    {
      '1': 'playlist_management',
      '3': 4,
      '4': 1,
      '5': 8,
      '10': 'playlistManagement'
    },
    {
      '1': 'multiple_playlists',
      '3': 5,
      '4': 1,
      '5': 8,
      '10': 'multiplePlaylists'
    },
    {'1': 'tag_filtering', '3': 6, '4': 1, '5': 8, '10': 'tagFiltering'},
    {'1': 'unlimited_tags', '3': 7, '4': 1, '5': 8, '10': 'unlimitedTags'},
    {'1': 'album_filtering', '3': 8, '4': 1, '5': 8, '10': 'albumFiltering'},
    {'1': 'shuffle', '3': 9, '4': 1, '5': 8, '10': 'shuffle'},
    {'1': 'repeat', '3': 10, '4': 1, '5': 8, '10': 'repeat'},
    {'1': 'seek', '3': 11, '4': 1, '5': 8, '10': 'seek'},
    {'1': 'volume_control', '3': 12, '4': 1, '5': 8, '10': 'volumeControl'},
    {'1': 'equalizer', '3': 13, '4': 1, '5': 8, '10': 'equalizer'},
    {'1': 'audio_playback', '3': 14, '4': 1, '5': 8, '10': 'audioPlayback'},
    {
      '1': 'custom_system_media_service',
      '3': 15,
      '4': 1,
      '5': 8,
      '10': 'customSystemMediaService'
    },
    {
      '1': 'max_imported_playlists',
      '3': 20,
      '4': 1,
      '5': 5,
      '9': 0,
      '10': 'maxImportedPlaylists',
      '17': true
    },
    {
      '1': 'max_tags',
      '3': 21,
      '4': 1,
      '5': 5,
      '9': 1,
      '10': 'maxTags',
      '17': true
    },
    {
      '1': 'max_playlist_entries',
      '3': 22,
      '4': 1,
      '5': 5,
      '9': 2,
      '10': 'maxPlaylistEntries',
      '17': true
    },
  ],
  '8': [
    {'1': '_max_imported_playlists'},
    {'1': '_max_tags'},
    {'1': '_max_playlist_entries'},
  ],
  '9': [
    {'1': 2, '2': 3},
  ],
};

/// Descriptor for `InstanceCapabilities`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceCapabilitiesDescriptor = $convert.base64Decode(
    'ChRJbnN0YW5jZUNhcGFiaWxpdGllcxI8ChpzZXJ2ZXJfY29udHJvbGxlZF9wbGF5YmFjaxgBIA'
    'EoCFIYc2VydmVyQ29udHJvbGxlZFBsYXliYWNrEikKEHF1ZXVlX21hbmFnZW1lbnQYAyABKAhS'
    'D3F1ZXVlTWFuYWdlbWVudBIvChNwbGF5bGlzdF9tYW5hZ2VtZW50GAQgASgIUhJwbGF5bGlzdE'
    '1hbmFnZW1lbnQSLQoSbXVsdGlwbGVfcGxheWxpc3RzGAUgASgIUhFtdWx0aXBsZVBsYXlsaXN0'
    'cxIjCg10YWdfZmlsdGVyaW5nGAYgASgIUgx0YWdGaWx0ZXJpbmcSJQoOdW5saW1pdGVkX3RhZ3'
    'MYByABKAhSDXVubGltaXRlZFRhZ3MSJwoPYWxidW1fZmlsdGVyaW5nGAggASgIUg5hbGJ1bUZp'
    'bHRlcmluZxIYCgdzaHVmZmxlGAkgASgIUgdzaHVmZmxlEhYKBnJlcGVhdBgKIAEoCFIGcmVwZW'
    'F0EhIKBHNlZWsYCyABKAhSBHNlZWsSJQoOdm9sdW1lX2NvbnRyb2wYDCABKAhSDXZvbHVtZUNv'
    'bnRyb2wSHAoJZXF1YWxpemVyGA0gASgIUgllcXVhbGl6ZXISJQoOYXVkaW9fcGxheWJhY2sYDi'
    'ABKAhSDWF1ZGlvUGxheWJhY2sSPQobY3VzdG9tX3N5c3RlbV9tZWRpYV9zZXJ2aWNlGA8gASgI'
    'UhhjdXN0b21TeXN0ZW1NZWRpYVNlcnZpY2USOQoWbWF4X2ltcG9ydGVkX3BsYXlsaXN0cxgUIA'
    'EoBUgAUhRtYXhJbXBvcnRlZFBsYXlsaXN0c4gBARIeCghtYXhfdGFncxgVIAEoBUgBUgdtYXhU'
    'YWdziAEBEjUKFG1heF9wbGF5bGlzdF9lbnRyaWVzGBYgASgFSAJSEm1heFBsYXlsaXN0RW50cm'
    'llc4gBAUIZChdfbWF4X2ltcG9ydGVkX3BsYXlsaXN0c0ILCglfbWF4X3RhZ3NCFwoVX21heF9w'
    'bGF5bGlzdF9lbnRyaWVzSgQIAhAD');

@$core.Deprecated('Use equalizerPointDescriptor instead')
const EqualizerPoint$json = {
  '1': 'EqualizerPoint',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'frequency', '3': 2, '4': 1, '5': 2, '10': 'frequency'},
    {'1': 'gain_db', '3': 3, '4': 1, '5': 2, '10': 'gainDb'},
    {'1': 'q', '3': 4, '4': 1, '5': 2, '10': 'q'},
    {
      '1': 'type',
      '3': 5,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.EqualizerFilterType',
      '10': 'type'
    },
  ],
};

/// Descriptor for `EqualizerPoint`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List equalizerPointDescriptor = $convert.base64Decode(
    'Cg5FcXVhbGl6ZXJQb2ludBIOCgJpZBgBIAEoCVICaWQSHAoJZnJlcXVlbmN5GAIgASgCUglmcm'
    'VxdWVuY3kSFwoHZ2Fpbl9kYhgDIAEoAlIGZ2FpbkRiEgwKAXEYBCABKAJSAXESOAoEdHlwZRgF'
    'IAEoDjIkLm9tbmlfbWl4X3BsYXllci5FcXVhbGl6ZXJGaWx0ZXJUeXBlUgR0eXBl');

@$core.Deprecated('Use equalizerStateDescriptor instead')
const EqualizerState$json = {
  '1': 'EqualizerState',
  '2': [
    {'1': 'enabled', '3': 1, '4': 1, '5': 8, '10': 'enabled'},
    {'1': 'global_gain_db', '3': 2, '4': 1, '5': 2, '10': 'globalGainDb'},
    {'1': 'soft_clip_enabled', '3': 3, '4': 1, '5': 8, '10': 'softClipEnabled'},
    {
      '1': 'points',
      '3': 4,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.EqualizerPoint',
      '10': 'points'
    },
  ],
};

/// Descriptor for `EqualizerState`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List equalizerStateDescriptor = $convert.base64Decode(
    'Cg5FcXVhbGl6ZXJTdGF0ZRIYCgdlbmFibGVkGAEgASgIUgdlbmFibGVkEiQKDmdsb2JhbF9nYW'
    'luX2RiGAIgASgCUgxnbG9iYWxHYWluRGISKgoRc29mdF9jbGlwX2VuYWJsZWQYAyABKAhSD3Nv'
    'ZnRDbGlwRW5hYmxlZBI3CgZwb2ludHMYBCADKAsyHy5vbW5pX21peF9wbGF5ZXIuRXF1YWxpem'
    'VyUG9pbnRSBnBvaW50cw==');

@$core.Deprecated('Use playlistSourceStateDescriptor instead')
const PlaylistSourceState$json = {
  '1': 'PlaylistSourceState',
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

/// Descriptor for `PlaylistSourceState`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistSourceStateDescriptor = $convert.base64Decode(
    'ChNQbGF5bGlzdFNvdXJjZVN0YXRlEg4KAmlkGAEgASgJUgJpZBISCgRuYW1lGAIgASgJUgRuYW'
    '1lEhQKBXV1aWRzGAMgAygJUgV1dWlkcxI3CgRraW5kGAQgASgOMiMub21uaV9taXhfcGxheWVy'
    'LlBsYXlsaXN0U291cmNlS2luZFIEa2luZBIVCgZyZWZfaWQYBSABKAlSBXJlZklk');

@$core.Deprecated('Use playbackTimelineStateDescriptor instead')
const PlaybackTimelineState$json = {
  '1': 'PlaybackTimelineState',
  '2': [
    {'1': 'version', '3': 1, '4': 1, '5': 5, '10': 'version'},
    {'1': 'source_uuids', '3': 2, '4': 3, '5': 9, '10': 'sourceUuids'},
    {'1': 'source_cursor', '3': 3, '4': 1, '5': 5, '10': 'sourceCursor'},
    {'1': 'current_uuid', '3': 4, '4': 1, '5': 9, '10': 'currentUuid'},
    {
      '1': 'current_source_index',
      '3': 5,
      '4': 1,
      '5': 5,
      '10': 'currentSourceIndex'
    },
    {'1': 'history_uuids', '3': 6, '4': 3, '5': 9, '10': 'historyUuids'},
    {'1': 'nav_forward_uuids', '3': 7, '4': 3, '5': 9, '10': 'navForwardUuids'},
    {
      '1': 'manual_queue_uuids',
      '3': 8,
      '4': 3,
      '5': 9,
      '10': 'manualQueueUuids'
    },
    {
      '1': 'playlist_sources',
      '3': 9,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.PlaylistSourceState',
      '10': 'playlistSources'
    },
    {'1': 'shuffle', '3': 10, '4': 1, '5': 8, '10': 'shuffle'},
    {
      '1': 'repeat_mode',
      '3': 11,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.RepeatMode',
      '10': 'repeatMode'
    },
    {'1': 'revision', '3': 12, '4': 1, '5': 3, '10': 'revision'},
  ],
};

/// Descriptor for `PlaybackTimelineState`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playbackTimelineStateDescriptor = $convert.base64Decode(
    'ChVQbGF5YmFja1RpbWVsaW5lU3RhdGUSGAoHdmVyc2lvbhgBIAEoBVIHdmVyc2lvbhIhCgxzb3'
    'VyY2VfdXVpZHMYAiADKAlSC3NvdXJjZVV1aWRzEiMKDXNvdXJjZV9jdXJzb3IYAyABKAVSDHNv'
    'dXJjZUN1cnNvchIhCgxjdXJyZW50X3V1aWQYBCABKAlSC2N1cnJlbnRVdWlkEjAKFGN1cnJlbn'
    'Rfc291cmNlX2luZGV4GAUgASgFUhJjdXJyZW50U291cmNlSW5kZXgSIwoNaGlzdG9yeV91dWlk'
    'cxgGIAMoCVIMaGlzdG9yeVV1aWRzEioKEW5hdl9mb3J3YXJkX3V1aWRzGAcgAygJUg9uYXZGb3'
    'J3YXJkVXVpZHMSLAoSbWFudWFsX3F1ZXVlX3V1aWRzGAggAygJUhBtYW51YWxRdWV1ZVV1aWRz'
    'Ek8KEHBsYXlsaXN0X3NvdXJjZXMYCSADKAsyJC5vbW5pX21peF9wbGF5ZXIuUGxheWxpc3RTb3'
    'VyY2VTdGF0ZVIPcGxheWxpc3RTb3VyY2VzEhgKB3NodWZmbGUYCiABKAhSB3NodWZmbGUSPAoL'
    'cmVwZWF0X21vZGUYCyABKA4yGy5vbW5pX21peF9wbGF5ZXIuUmVwZWF0TW9kZVIKcmVwZWF0TW'
    '9kZRIaCghyZXZpc2lvbhgMIAEoA1IIcmV2aXNpb24=');

@$core.Deprecated('Use instanceProfileDescriptor instead')
const InstanceProfile$json = {
  '1': 'InstanceProfile',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'display_name', '3': 2, '4': 1, '5': 9, '10': 'displayName'},
    {
      '1': 'kind',
      '3': 3,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.InstanceKind',
      '10': 'kind'
    },
    {'1': 'mod_id', '3': 4, '4': 1, '5': 9, '10': 'modId'},
    {'1': 'game_name', '3': 5, '4': 1, '5': 9, '10': 'gameName'},
    {
      '1': 'capabilities',
      '3': 7,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstanceCapabilities',
      '10': 'capabilities'
    },
    {'1': 'volume', '3': 8, '4': 1, '5': 2, '10': 'volume'},
    {'1': 'target_latency', '3': 9, '4': 1, '5': 2, '10': 'targetLatency'},
    {
      '1': 'equalizer',
      '3': 10,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.EqualizerState',
      '10': 'equalizer'
    },
    {
      '1': 'imported_playlist_ids',
      '3': 13,
      '4': 3,
      '5': 9,
      '10': 'importedPlaylistIds'
    },
    {'1': 'pinned_tag_ids', '3': 14, '4': 3, '5': 9, '10': 'pinnedTagIds'},
    {
      '1': 'created_at',
      '3': 15,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'createdAt'
    },
    {
      '1': 'updated_at',
      '3': 16,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'updatedAt'
    },
    {
      '1': 'playback_timeline',
      '3': 17,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.PlaybackTimelineState',
      '10': 'playbackTimeline'
    },
  ],
  '9': [
    {'1': 11, '2': 12},
    {'1': 12, '2': 13},
  ],
};

/// Descriptor for `InstanceProfile`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceProfileDescriptor = $convert.base64Decode(
    'Cg9JbnN0YW5jZVByb2ZpbGUSDgoCaWQYASABKAlSAmlkEiEKDGRpc3BsYXlfbmFtZRgCIAEoCV'
    'ILZGlzcGxheU5hbWUSMQoEa2luZBgDIAEoDjIdLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZUtp'
    'bmRSBGtpbmQSFQoGbW9kX2lkGAQgASgJUgVtb2RJZBIbCglnYW1lX25hbWUYBSABKAlSCGdhbW'
    'VOYW1lEkkKDGNhcGFiaWxpdGllcxgHIAEoCzIlLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZUNh'
    'cGFiaWxpdGllc1IMY2FwYWJpbGl0aWVzEhYKBnZvbHVtZRgIIAEoAlIGdm9sdW1lEiUKDnRhcm'
    'dldF9sYXRlbmN5GAkgASgCUg10YXJnZXRMYXRlbmN5Ej0KCWVxdWFsaXplchgKIAEoCzIfLm9t'
    'bmlfbWl4X3BsYXllci5FcXVhbGl6ZXJTdGF0ZVIJZXF1YWxpemVyEjIKFWltcG9ydGVkX3BsYX'
    'lsaXN0X2lkcxgNIAMoCVITaW1wb3J0ZWRQbGF5bGlzdElkcxIkCg5waW5uZWRfdGFnX2lkcxgO'
    'IAMoCVIMcGlubmVkVGFnSWRzEj0KCmNyZWF0ZWRfYXQYDyABKAsyHi5vbW5pX21peF9wbGF5ZX'
    'IuT21uaVRpbWVzdGFtcFIJY3JlYXRlZEF0Ej0KCnVwZGF0ZWRfYXQYECABKAsyHi5vbW5pX21p'
    'eF9wbGF5ZXIuT21uaVRpbWVzdGFtcFIJdXBkYXRlZEF0ElMKEXBsYXliYWNrX3RpbWVsaW5lGB'
    'EgASgLMiYub21uaV9taXhfcGxheWVyLlBsYXliYWNrVGltZWxpbmVTdGF0ZVIQcGxheWJhY2tU'
    'aW1lbGluZUoECAsQDEoECAwQDQ==');

@$core.Deprecated('Use instanceConnectRequestDescriptor instead')
const InstanceConnectRequest$json = {
  '1': 'InstanceConnectRequest',
  '2': [
    {'1': 'client_id', '3': 1, '4': 1, '5': 9, '10': 'clientId'},
    {
      '1': 'kind',
      '3': 2,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.InstanceKind',
      '10': 'kind'
    },
    {
      '1': 'capabilities',
      '3': 4,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstanceCapabilities',
      '10': 'capabilities'
    },
    {'1': 'mod_id', '3': 5, '4': 1, '5': 9, '10': 'modId'},
    {'1': 'game_name', '3': 6, '4': 1, '5': 9, '10': 'gameName'},
    {'1': 'display_name', '3': 7, '4': 1, '5': 9, '10': 'displayName'},
    {'1': 'no_instance', '3': 8, '4': 1, '5': 8, '10': 'noInstance'},
  ],
};

/// Descriptor for `InstanceConnectRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceConnectRequestDescriptor = $convert.base64Decode(
    'ChZJbnN0YW5jZUNvbm5lY3RSZXF1ZXN0EhsKCWNsaWVudF9pZBgBIAEoCVIIY2xpZW50SWQSMQ'
    'oEa2luZBgCIAEoDjIdLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZUtpbmRSBGtpbmQSSQoMY2Fw'
    'YWJpbGl0aWVzGAQgASgLMiUub21uaV9taXhfcGxheWVyLkluc3RhbmNlQ2FwYWJpbGl0aWVzUg'
    'xjYXBhYmlsaXRpZXMSFQoGbW9kX2lkGAUgASgJUgVtb2RJZBIbCglnYW1lX25hbWUYBiABKAlS'
    'CGdhbWVOYW1lEiEKDGRpc3BsYXlfbmFtZRgHIAEoCVILZGlzcGxheU5hbWUSHwoLbm9faW5zdG'
    'FuY2UYCCABKAhSCm5vSW5zdGFuY2U=');

@$core.Deprecated('Use instanceConnectResponseDescriptor instead')
const InstanceConnectResponse$json = {
  '1': 'InstanceConnectResponse',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
    {'1': 'is_new', '3': 2, '4': 1, '5': 8, '10': 'isNew'},
    {
      '1': 'profile',
      '3': 3,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.InstanceProfile',
      '10': 'profile'
    },
    {'1': 'no_instance', '3': 4, '4': 1, '5': 8, '10': 'noInstance'},
  ],
};

/// Descriptor for `InstanceConnectResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceConnectResponseDescriptor = $convert.base64Decode(
    'ChdJbnN0YW5jZUNvbm5lY3RSZXNwb25zZRIfCgtpbnN0YW5jZV9pZBgBIAEoCVIKaW5zdGFuY2'
    'VJZBIVCgZpc19uZXcYAiABKAhSBWlzTmV3EjoKB3Byb2ZpbGUYAyABKAsyIC5vbW5pX21peF9w'
    'bGF5ZXIuSW5zdGFuY2VQcm9maWxlUgdwcm9maWxlEh8KC25vX2luc3RhbmNlGAQgASgIUgpub0'
    'luc3RhbmNl');

@$core.Deprecated('Use instanceHeartbeatRequestDescriptor instead')
const InstanceHeartbeatRequest$json = {
  '1': 'InstanceHeartbeatRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `InstanceHeartbeatRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceHeartbeatRequestDescriptor =
    $convert.base64Decode(
        'ChhJbnN0YW5jZUhlYXJ0YmVhdFJlcXVlc3QSHwoLaW5zdGFuY2VfaWQYASABKAlSCmluc3Rhbm'
        'NlSWQ=');

@$core.Deprecated('Use instanceHeartbeatResponseDescriptor instead')
const InstanceHeartbeatResponse$json = {
  '1': 'InstanceHeartbeatResponse',
  '2': [
    {'1': 'alive', '3': 1, '4': 1, '5': 8, '10': 'alive'},
  ],
};

/// Descriptor for `InstanceHeartbeatResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceHeartbeatResponseDescriptor =
    $convert.base64Decode(
        'ChlJbnN0YW5jZUhlYXJ0YmVhdFJlc3BvbnNlEhQKBWFsaXZlGAEgASgIUgVhbGl2ZQ==');

@$core.Deprecated('Use instanceDisconnectRequestDescriptor instead')
const InstanceDisconnectRequest$json = {
  '1': 'InstanceDisconnectRequest',
  '2': [
    {'1': 'instance_id', '3': 1, '4': 1, '5': 9, '10': 'instanceId'},
  ],
};

/// Descriptor for `InstanceDisconnectRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceDisconnectRequestDescriptor =
    $convert.base64Decode(
        'ChlJbnN0YW5jZURpc2Nvbm5lY3RSZXF1ZXN0Eh8KC2luc3RhbmNlX2lkGAEgASgJUgppbnN0YW'
        '5jZUlk');

@$core.Deprecated('Use instanceDisconnectResponseDescriptor instead')
const InstanceDisconnectResponse$json = {
  '1': 'InstanceDisconnectResponse',
  '2': [
    {'1': 'disconnected', '3': 1, '4': 1, '5': 8, '10': 'disconnected'},
  ],
};

/// Descriptor for `InstanceDisconnectResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceDisconnectResponseDescriptor =
    $convert.base64Decode(
        'ChpJbnN0YW5jZURpc2Nvbm5lY3RSZXNwb25zZRIiCgxkaXNjb25uZWN0ZWQYASABKAhSDGRpc2'
        'Nvbm5lY3RlZA==');

@$core.Deprecated('Use instanceSummaryDescriptor instead')
const InstanceSummary$json = {
  '1': 'InstanceSummary',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'display_name', '3': 2, '4': 1, '5': 9, '10': 'displayName'},
    {
      '1': 'kind',
      '3': 3,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.InstanceKind',
      '10': 'kind'
    },
    {'1': 'is_online', '3': 5, '4': 1, '5': 8, '10': 'isOnline'},
    {
      '1': 'current_track_uuid',
      '3': 6,
      '4': 1,
      '5': 9,
      '10': 'currentTrackUuid'
    },
    {'1': 'queue_count', '3': 7, '4': 1, '5': 5, '10': 'queueCount'},
    {
      '1': 'connected_at',
      '3': 8,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'connectedAt'
    },
    {'1': 'mod_id', '3': 9, '4': 1, '5': 9, '10': 'modId'},
    {'1': 'game_name', '3': 10, '4': 1, '5': 9, '10': 'gameName'},
  ],
};

/// Descriptor for `InstanceSummary`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List instanceSummaryDescriptor = $convert.base64Decode(
    'Cg9JbnN0YW5jZVN1bW1hcnkSDgoCaWQYASABKAlSAmlkEiEKDGRpc3BsYXlfbmFtZRgCIAEoCV'
    'ILZGlzcGxheU5hbWUSMQoEa2luZBgDIAEoDjIdLm9tbmlfbWl4X3BsYXllci5JbnN0YW5jZUtp'
    'bmRSBGtpbmQSGwoJaXNfb25saW5lGAUgASgIUghpc09ubGluZRIsChJjdXJyZW50X3RyYWNrX3'
    'V1aWQYBiABKAlSEGN1cnJlbnRUcmFja1V1aWQSHwoLcXVldWVfY291bnQYByABKAVSCnF1ZXVl'
    'Q291bnQSQQoMY29ubmVjdGVkX2F0GAggASgLMh4ub21uaV9taXhfcGxheWVyLk9tbmlUaW1lc3'
    'RhbXBSC2Nvbm5lY3RlZEF0EhUKBm1vZF9pZBgJIAEoCVIFbW9kSWQSGwoJZ2FtZV9uYW1lGAog'
    'ASgJUghnYW1lTmFtZQ==');

@$core.Deprecated('Use listInstancesResponseDescriptor instead')
const ListInstancesResponse$json = {
  '1': 'ListInstancesResponse',
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

/// Descriptor for `ListInstancesResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List listInstancesResponseDescriptor = $convert.base64Decode(
    'ChVMaXN0SW5zdGFuY2VzUmVzcG9uc2USPgoJaW5zdGFuY2VzGAEgAygLMiAub21uaV9taXhfcG'
    'xheWVyLkluc3RhbmNlU3VtbWFyeVIJaW5zdGFuY2Vz');

@$core.Deprecated('Use playbackStatusDescriptor instead')
const PlaybackStatus$json = {
  '1': 'PlaybackStatus',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'title', '3': 2, '4': 1, '5': 9, '10': 'title'},
    {'1': 'artist', '3': 3, '4': 1, '5': 9, '10': 'artist'},
    {'1': 'album_id', '3': 4, '4': 1, '5': 9, '10': 'albumId'},
    {'1': 'duration', '3': 5, '4': 1, '5': 2, '10': 'duration'},
    {'1': 'position', '3': 6, '4': 1, '5': 2, '10': 'position'},
    {'1': 'is_playing', '3': 7, '4': 1, '5': 8, '10': 'isPlaying'},
    {'1': 'shuffle', '3': 8, '4': 1, '5': 8, '10': 'shuffle'},
    {
      '1': 'repeat_mode',
      '3': 9,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.RepeatMode',
      '10': 'repeatMode'
    },
    {'1': 'volume', '3': 10, '4': 1, '5': 2, '10': 'volume'},
  ],
};

/// Descriptor for `PlaybackStatus`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playbackStatusDescriptor = $convert.base64Decode(
    'Cg5QbGF5YmFja1N0YXR1cxIdCgp0cmFja191dWlkGAEgASgJUgl0cmFja1V1aWQSFAoFdGl0bG'
    'UYAiABKAlSBXRpdGxlEhYKBmFydGlzdBgDIAEoCVIGYXJ0aXN0EhkKCGFsYnVtX2lkGAQgASgJ'
    'UgdhbGJ1bUlkEhoKCGR1cmF0aW9uGAUgASgCUghkdXJhdGlvbhIaCghwb3NpdGlvbhgGIAEoAl'
    'IIcG9zaXRpb24SHQoKaXNfcGxheWluZxgHIAEoCFIJaXNQbGF5aW5nEhgKB3NodWZmbGUYCCAB'
    'KAhSB3NodWZmbGUSPAoLcmVwZWF0X21vZGUYCSABKA4yGy5vbW5pX21peF9wbGF5ZXIuUmVwZW'
    'F0TW9kZVIKcmVwZWF0TW9kZRIWCgZ2b2x1bWUYCiABKAJSBnZvbHVtZQ==');
