// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/track.proto.

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

@$core.Deprecated('Use trackDescriptor instead')
const Track$json = {
  '1': 'Track',
  '2': [
    {'1': 'uuid', '3': 1, '4': 1, '5': 9, '10': 'uuid'},
    {'1': 'title', '3': 2, '4': 1, '5': 9, '10': 'title'},
    {'1': 'artist', '3': 3, '4': 1, '5': 9, '10': 'artist'},
    {'1': 'album_id', '3': 4, '4': 1, '5': 9, '10': 'albumId'},
    {'1': 'duration', '3': 5, '4': 1, '5': 2, '10': 'duration'},
    {'1': 'module_id', '3': 6, '4': 1, '5': 9, '10': 'moduleId'},
    {
      '1': 'source_type',
      '3': 7,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.SourceType',
      '10': 'sourceType'
    },
    {'1': 'source_path', '3': 8, '4': 1, '5': 9, '10': 'sourcePath'},
    {'1': 'is_favorite', '3': 9, '4': 1, '5': 8, '10': 'isFavorite'},
    {'1': 'is_excluded', '3': 10, '4': 1, '5': 8, '10': 'isExcluded'},
    {'1': 'cover_uri', '3': 11, '4': 1, '5': 9, '10': 'coverUri'},
    {'1': 'play_count', '3': 13, '4': 1, '5': 5, '10': 'playCount'},
    {
      '1': 'created_at',
      '3': 14,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'createdAt'
    },
    {
      '1': 'last_played_at',
      '3': 15,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'lastPlayedAt'
    },
    {'1': 'extended_data', '3': 16, '4': 1, '5': 12, '10': 'extendedData'},
  ],
};

/// Descriptor for `Track`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List trackDescriptor = $convert.base64Decode(
    'CgVUcmFjaxISCgR1dWlkGAEgASgJUgR1dWlkEhQKBXRpdGxlGAIgASgJUgV0aXRsZRIWCgZhcn'
    'Rpc3QYAyABKAlSBmFydGlzdBIZCghhbGJ1bV9pZBgEIAEoCVIHYWxidW1JZBIaCghkdXJhdGlv'
    'bhgFIAEoAlIIZHVyYXRpb24SGwoJbW9kdWxlX2lkGAYgASgJUghtb2R1bGVJZBI8Cgtzb3VyY2'
    'VfdHlwZRgHIAEoDjIbLm9tbmlfbWl4X3BsYXllci5Tb3VyY2VUeXBlUgpzb3VyY2VUeXBlEh8K'
    'C3NvdXJjZV9wYXRoGAggASgJUgpzb3VyY2VQYXRoEh8KC2lzX2Zhdm9yaXRlGAkgASgIUgppc0'
    'Zhdm9yaXRlEh8KC2lzX2V4Y2x1ZGVkGAogASgIUgppc0V4Y2x1ZGVkEhsKCWNvdmVyX3VyaRgL'
    'IAEoCVIIY292ZXJVcmkSHQoKcGxheV9jb3VudBgNIAEoBVIJcGxheUNvdW50Ej0KCmNyZWF0ZW'
    'RfYXQYDiABKAsyHi5vbW5pX21peF9wbGF5ZXIuT21uaVRpbWVzdGFtcFIJY3JlYXRlZEF0EkQK'
    'Dmxhc3RfcGxheWVkX2F0GA8gASgLMh4ub21uaV9taXhfcGxheWVyLk9tbmlUaW1lc3RhbXBSDG'
    'xhc3RQbGF5ZWRBdBIjCg1leHRlbmRlZF9kYXRhGBAgASgMUgxleHRlbmRlZERhdGE=');

@$core.Deprecated('Use trackTagDescriptor instead')
const TrackTag$json = {
  '1': 'TrackTag',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'tag_id', '3': 2, '4': 1, '5': 9, '10': 'tagId'},
  ],
};

/// Descriptor for `TrackTag`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List trackTagDescriptor = $convert.base64Decode(
    'CghUcmFja1RhZxIdCgp0cmFja191dWlkGAEgASgJUgl0cmFja1V1aWQSFQoGdGFnX2lkGAIgAS'
    'gJUgV0YWdJZA==');

@$core.Deprecated('Use upsertTrackRequestDescriptor instead')
const UpsertTrackRequest$json = {
  '1': 'UpsertTrackRequest',
  '2': [
    {
      '1': 'track',
      '3': 1,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Track',
      '10': 'track'
    },
  ],
};

/// Descriptor for `UpsertTrackRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTrackRequestDescriptor = $convert.base64Decode(
    'ChJVcHNlcnRUcmFja1JlcXVlc3QSLAoFdHJhY2sYASABKAsyFi5vbW5pX21peF9wbGF5ZXIuVH'
    'JhY2tSBXRyYWNr');

@$core.Deprecated('Use upsertTrackResponseDescriptor instead')
const UpsertTrackResponse$json = {
  '1': 'UpsertTrackResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 8, '10': 'created'},
    {
      '1': 'track',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Track',
      '10': 'track'
    },
  ],
};

/// Descriptor for `UpsertTrackResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTrackResponseDescriptor = $convert.base64Decode(
    'ChNVcHNlcnRUcmFja1Jlc3BvbnNlEhgKB2NyZWF0ZWQYASABKAhSB2NyZWF0ZWQSLAoFdHJhY2'
    'sYAiABKAsyFi5vbW5pX21peF9wbGF5ZXIuVHJhY2tSBXRyYWNr');

@$core.Deprecated('Use upsertTracksRequestDescriptor instead')
const UpsertTracksRequest$json = {
  '1': 'UpsertTracksRequest',
  '2': [
    {
      '1': 'tracks',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Track',
      '10': 'tracks'
    },
  ],
};

/// Descriptor for `UpsertTracksRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTracksRequestDescriptor = $convert.base64Decode(
    'ChNVcHNlcnRUcmFja3NSZXF1ZXN0Ei4KBnRyYWNrcxgBIAMoCzIWLm9tbmlfbWl4X3BsYXllci'
    '5UcmFja1IGdHJhY2tz');

@$core.Deprecated('Use upsertTracksResponseDescriptor instead')
const UpsertTracksResponse$json = {
  '1': 'UpsertTracksResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 5, '10': 'created'},
    {'1': 'updated', '3': 2, '4': 1, '5': 5, '10': 'updated'},
  ],
};

/// Descriptor for `UpsertTracksResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTracksResponseDescriptor = $convert.base64Decode(
    'ChRVcHNlcnRUcmFja3NSZXNwb25zZRIYCgdjcmVhdGVkGAEgASgFUgdjcmVhdGVkEhgKB3VwZG'
    'F0ZWQYAiABKAVSB3VwZGF0ZWQ=');

@$core.Deprecated('Use setTrackTagsRequestDescriptor instead')
const SetTrackTagsRequest$json = {
  '1': 'SetTrackTagsRequest',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'tag_ids', '3': 2, '4': 3, '5': 9, '10': 'tagIds'},
  ],
};

/// Descriptor for `SetTrackTagsRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setTrackTagsRequestDescriptor = $convert.base64Decode(
    'ChNTZXRUcmFja1RhZ3NSZXF1ZXN0Eh0KCnRyYWNrX3V1aWQYASABKAlSCXRyYWNrVXVpZBIXCg'
    'd0YWdfaWRzGAIgAygJUgZ0YWdJZHM=');

@$core.Deprecated('Use setTrackTagsResponseDescriptor instead')
const SetTrackTagsResponse$json = {
  '1': 'SetTrackTagsResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
    {'1': 'tag_count', '3': 2, '4': 1, '5': 5, '10': 'tagCount'},
  ],
};

/// Descriptor for `SetTrackTagsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List setTrackTagsResponseDescriptor = $convert.base64Decode(
    'ChRTZXRUcmFja1RhZ3NSZXNwb25zZRIYCgdzdWNjZXNzGAEgASgIUgdzdWNjZXNzEhsKCXRhZ1'
    '9jb3VudBgCIAEoBVIIdGFnQ291bnQ=');

@$core.Deprecated('Use modifyTrackTagRequestDescriptor instead')
const ModifyTrackTagRequest$json = {
  '1': 'ModifyTrackTagRequest',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'tag_id', '3': 2, '4': 1, '5': 9, '10': 'tagId'},
  ],
};

/// Descriptor for `ModifyTrackTagRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List modifyTrackTagRequestDescriptor = $convert.base64Decode(
    'ChVNb2RpZnlUcmFja1RhZ1JlcXVlc3QSHQoKdHJhY2tfdXVpZBgBIAEoCVIJdHJhY2tVdWlkEh'
    'UKBnRhZ19pZBgCIAEoCVIFdGFnSWQ=');

@$core.Deprecated('Use modifyTrackTagResponseDescriptor instead')
const ModifyTrackTagResponse$json = {
  '1': 'ModifyTrackTagResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `ModifyTrackTagResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List modifyTrackTagResponseDescriptor =
    $convert.base64Decode(
        'ChZNb2RpZnlUcmFja1RhZ1Jlc3BvbnNlEhgKB3N1Y2Nlc3MYASABKAhSB3N1Y2Nlc3M=');
