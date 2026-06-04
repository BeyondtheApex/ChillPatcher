// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/playlist.proto.

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

@$core.Deprecated('Use playlistDescriptor instead')
const Playlist$json = {
  '1': 'Playlist',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'name', '3': 2, '4': 1, '5': 9, '10': 'name'},
    {'1': 'module_id', '3': 3, '4': 1, '5': 9, '10': 'moduleId'},
    {
      '1': 'kind',
      '3': 4,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.PlaylistKind',
      '10': 'kind'
    },
    {'1': 'cover_uri', '3': 5, '4': 1, '5': 9, '10': 'coverUri'},
    {'1': 'sort_order', '3': 6, '4': 1, '5': 5, '10': 'sortOrder'},
    {
      '1': 'created_at',
      '3': 7,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'createdAt'
    },
    {
      '1': 'updated_at',
      '3': 8,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'updatedAt'
    },
  ],
};

/// Descriptor for `Playlist`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistDescriptor = $convert.base64Decode(
    'CghQbGF5bGlzdBIOCgJpZBgBIAEoCVICaWQSEgoEbmFtZRgCIAEoCVIEbmFtZRIbCgltb2R1bG'
    'VfaWQYAyABKAlSCG1vZHVsZUlkEjEKBGtpbmQYBCABKA4yHS5vbW5pX21peF9wbGF5ZXIuUGxh'
    'eWxpc3RLaW5kUgRraW5kEhsKCWNvdmVyX3VyaRgFIAEoCVIIY292ZXJVcmkSHQoKc29ydF9vcm'
    'RlchgGIAEoBVIJc29ydE9yZGVyEj0KCmNyZWF0ZWRfYXQYByABKAsyHi5vbW5pX21peF9wbGF5'
    'ZXIuT21uaVRpbWVzdGFtcFIJY3JlYXRlZEF0Ej0KCnVwZGF0ZWRfYXQYCCABKAsyHi5vbW5pX2'
    '1peF9wbGF5ZXIuT21uaVRpbWVzdGFtcFIJdXBkYXRlZEF0');

@$core.Deprecated('Use playlistEntryDescriptor instead')
const PlaylistEntry$json = {
  '1': 'PlaylistEntry',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'playlist_id', '3': 2, '4': 1, '5': 9, '10': 'playlistId'},
    {'1': 'track_uuid', '3': 3, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'position', '3': 4, '4': 1, '5': 5, '10': 'position'},
    {
      '1': 'added_at',
      '3': 5,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.OmniTimestamp',
      '10': 'addedAt'
    },
  ],
};

/// Descriptor for `PlaylistEntry`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistEntryDescriptor = $convert.base64Decode(
    'Cg1QbGF5bGlzdEVudHJ5Eg4KAmlkGAEgASgJUgJpZBIfCgtwbGF5bGlzdF9pZBgCIAEoCVIKcG'
    'xheWxpc3RJZBIdCgp0cmFja191dWlkGAMgASgJUgl0cmFja1V1aWQSGgoIcG9zaXRpb24YBCAB'
    'KAVSCHBvc2l0aW9uEjkKCGFkZGVkX2F0GAUgASgLMh4ub21uaV9taXhfcGxheWVyLk9tbmlUaW'
    '1lc3RhbXBSB2FkZGVkQXQ=');

@$core.Deprecated('Use playlistEntrySpecDescriptor instead')
const PlaylistEntrySpec$json = {
  '1': 'PlaylistEntrySpec',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'position', '3': 2, '4': 1, '5': 5, '10': 'position'},
  ],
};

/// Descriptor for `PlaylistEntrySpec`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistEntrySpecDescriptor = $convert.base64Decode(
    'ChFQbGF5bGlzdEVudHJ5U3BlYxIdCgp0cmFja191dWlkGAEgASgJUgl0cmFja1V1aWQSGgoIcG'
    '9zaXRpb24YAiABKAVSCHBvc2l0aW9u');

@$core.Deprecated('Use upsertPlaylistRequestDescriptor instead')
const UpsertPlaylistRequest$json = {
  '1': 'UpsertPlaylistRequest',
  '2': [
    {
      '1': 'playlist',
      '3': 1,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Playlist',
      '10': 'playlist'
    },
  ],
};

/// Descriptor for `UpsertPlaylistRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertPlaylistRequestDescriptor = $convert.base64Decode(
    'ChVVcHNlcnRQbGF5bGlzdFJlcXVlc3QSNQoIcGxheWxpc3QYASABKAsyGS5vbW5pX21peF9wbG'
    'F5ZXIuUGxheWxpc3RSCHBsYXlsaXN0');

@$core.Deprecated('Use upsertPlaylistResponseDescriptor instead')
const UpsertPlaylistResponse$json = {
  '1': 'UpsertPlaylistResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 8, '10': 'created'},
    {
      '1': 'playlist',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Playlist',
      '10': 'playlist'
    },
  ],
};

/// Descriptor for `UpsertPlaylistResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertPlaylistResponseDescriptor =
    $convert.base64Decode(
        'ChZVcHNlcnRQbGF5bGlzdFJlc3BvbnNlEhgKB2NyZWF0ZWQYASABKAhSB2NyZWF0ZWQSNQoIcG'
        'xheWxpc3QYAiABKAsyGS5vbW5pX21peF9wbGF5ZXIuUGxheWxpc3RSCHBsYXlsaXN0');

@$core.Deprecated('Use replacePlaylistEntriesRequestDescriptor instead')
const ReplacePlaylistEntriesRequest$json = {
  '1': 'ReplacePlaylistEntriesRequest',
  '2': [
    {'1': 'playlist_id', '3': 1, '4': 1, '5': 9, '10': 'playlistId'},
    {
      '1': 'entries',
      '3': 2,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.PlaylistEntrySpec',
      '10': 'entries'
    },
  ],
};

/// Descriptor for `ReplacePlaylistEntriesRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List replacePlaylistEntriesRequestDescriptor =
    $convert.base64Decode(
        'Ch1SZXBsYWNlUGxheWxpc3RFbnRyaWVzUmVxdWVzdBIfCgtwbGF5bGlzdF9pZBgBIAEoCVIKcG'
        'xheWxpc3RJZBI8CgdlbnRyaWVzGAIgAygLMiIub21uaV9taXhfcGxheWVyLlBsYXlsaXN0RW50'
        'cnlTcGVjUgdlbnRyaWVz');

@$core.Deprecated('Use replacePlaylistEntriesResponseDescriptor instead')
const ReplacePlaylistEntriesResponse$json = {
  '1': 'ReplacePlaylistEntriesResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
    {'1': 'entry_count', '3': 2, '4': 1, '5': 5, '10': 'entryCount'},
  ],
};

/// Descriptor for `ReplacePlaylistEntriesResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List replacePlaylistEntriesResponseDescriptor =
    $convert.base64Decode(
        'Ch5SZXBsYWNlUGxheWxpc3RFbnRyaWVzUmVzcG9uc2USGAoHc3VjY2VzcxgBIAEoCFIHc3VjY2'
        'VzcxIfCgtlbnRyeV9jb3VudBgCIAEoBVIKZW50cnlDb3VudA==');

@$core.Deprecated('Use insertPlaylistEntryRequestDescriptor instead')
const InsertPlaylistEntryRequest$json = {
  '1': 'InsertPlaylistEntryRequest',
  '2': [
    {'1': 'playlist_id', '3': 1, '4': 1, '5': 9, '10': 'playlistId'},
    {
      '1': 'entry',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.PlaylistEntrySpec',
      '10': 'entry'
    },
    {'1': 'index', '3': 3, '4': 1, '5': 5, '10': 'index'},
  ],
};

/// Descriptor for `InsertPlaylistEntryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List insertPlaylistEntryRequestDescriptor =
    $convert.base64Decode(
        'ChpJbnNlcnRQbGF5bGlzdEVudHJ5UmVxdWVzdBIfCgtwbGF5bGlzdF9pZBgBIAEoCVIKcGxheW'
        'xpc3RJZBI4CgVlbnRyeRgCIAEoCzIiLm9tbmlfbWl4X3BsYXllci5QbGF5bGlzdEVudHJ5U3Bl'
        'Y1IFZW50cnkSFAoFaW5kZXgYAyABKAVSBWluZGV4');

@$core.Deprecated('Use insertPlaylistEntryResponseDescriptor instead')
const InsertPlaylistEntryResponse$json = {
  '1': 'InsertPlaylistEntryResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
    {
      '1': 'entry',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.PlaylistEntry',
      '10': 'entry'
    },
  ],
};

/// Descriptor for `InsertPlaylistEntryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List insertPlaylistEntryResponseDescriptor =
    $convert.base64Decode(
        'ChtJbnNlcnRQbGF5bGlzdEVudHJ5UmVzcG9uc2USGAoHc3VjY2VzcxgBIAEoCFIHc3VjY2Vzcx'
        'I0CgVlbnRyeRgCIAEoCzIeLm9tbmlfbWl4X3BsYXllci5QbGF5bGlzdEVudHJ5UgVlbnRyeQ==');

@$core.Deprecated('Use removePlaylistEntryRequestDescriptor instead')
const RemovePlaylistEntryRequest$json = {
  '1': 'RemovePlaylistEntryRequest',
  '2': [
    {'1': 'entry_id', '3': 1, '4': 1, '5': 9, '10': 'entryId'},
  ],
};

/// Descriptor for `RemovePlaylistEntryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List removePlaylistEntryRequestDescriptor =
    $convert.base64Decode(
        'ChpSZW1vdmVQbGF5bGlzdEVudHJ5UmVxdWVzdBIZCghlbnRyeV9pZBgBIAEoCVIHZW50cnlJZA'
        '==');

@$core.Deprecated('Use removePlaylistEntryResponseDescriptor instead')
const RemovePlaylistEntryResponse$json = {
  '1': 'RemovePlaylistEntryResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `RemovePlaylistEntryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List removePlaylistEntryResponseDescriptor =
    $convert.base64Decode(
        'ChtSZW1vdmVQbGF5bGlzdEVudHJ5UmVzcG9uc2USGAoHc3VjY2VzcxgBIAEoCFIHc3VjY2Vzcw'
        '==');

@$core.Deprecated('Use movePlaylistEntryRequestDescriptor instead')
const MovePlaylistEntryRequest$json = {
  '1': 'MovePlaylistEntryRequest',
  '2': [
    {'1': 'entry_id', '3': 1, '4': 1, '5': 9, '10': 'entryId'},
    {'1': 'new_index', '3': 2, '4': 1, '5': 5, '10': 'newIndex'},
  ],
};

/// Descriptor for `MovePlaylistEntryRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List movePlaylistEntryRequestDescriptor =
    $convert.base64Decode(
        'ChhNb3ZlUGxheWxpc3RFbnRyeVJlcXVlc3QSGQoIZW50cnlfaWQYASABKAlSB2VudHJ5SWQSGw'
        'oJbmV3X2luZGV4GAIgASgFUghuZXdJbmRleA==');

@$core.Deprecated('Use movePlaylistEntryResponseDescriptor instead')
const MovePlaylistEntryResponse$json = {
  '1': 'MovePlaylistEntryResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `MovePlaylistEntryResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List movePlaylistEntryResponseDescriptor =
    $convert.base64Decode(
        'ChlNb3ZlUGxheWxpc3RFbnRyeVJlc3BvbnNlEhgKB3N1Y2Nlc3MYASABKAhSB3N1Y2Nlc3M=');

@$core.Deprecated('Use playlistWithEntriesDescriptor instead')
const PlaylistWithEntries$json = {
  '1': 'PlaylistWithEntries',
  '2': [
    {'1': 'playlist_id', '3': 1, '4': 1, '5': 9, '10': 'playlistId'},
    {'1': 'playlist_name', '3': 2, '4': 1, '5': 9, '10': 'playlistName'},
    {
      '1': 'entries',
      '3': 3,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.PlaylistEntryWithTrack',
      '10': 'entries'
    },
  ],
};

/// Descriptor for `PlaylistWithEntries`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistWithEntriesDescriptor = $convert.base64Decode(
    'ChNQbGF5bGlzdFdpdGhFbnRyaWVzEh8KC3BsYXlsaXN0X2lkGAEgASgJUgpwbGF5bGlzdElkEi'
    'MKDXBsYXlsaXN0X25hbWUYAiABKAlSDHBsYXlsaXN0TmFtZRJBCgdlbnRyaWVzGAMgAygLMicu'
    'b21uaV9taXhfcGxheWVyLlBsYXlsaXN0RW50cnlXaXRoVHJhY2tSB2VudHJpZXM=');

@$core.Deprecated('Use playlistEntryWithTrackDescriptor instead')
const PlaylistEntryWithTrack$json = {
  '1': 'PlaylistEntryWithTrack',
  '2': [
    {'1': 'entry_id', '3': 1, '4': 1, '5': 9, '10': 'entryId'},
    {'1': 'track_uuid', '3': 2, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'title', '3': 3, '4': 1, '5': 9, '10': 'title'},
    {'1': 'artist', '3': 4, '4': 1, '5': 9, '10': 'artist'},
    {'1': 'duration', '3': 5, '4': 1, '5': 2, '10': 'duration'},
    {'1': 'album_id', '3': 6, '4': 1, '5': 9, '10': 'albumId'},
    {'1': 'cover_uri', '3': 7, '4': 1, '5': 9, '10': 'coverUri'},
    {'1': 'position', '3': 8, '4': 1, '5': 5, '10': 'position'},
  ],
};

/// Descriptor for `PlaylistEntryWithTrack`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistEntryWithTrackDescriptor = $convert.base64Decode(
    'ChZQbGF5bGlzdEVudHJ5V2l0aFRyYWNrEhkKCGVudHJ5X2lkGAEgASgJUgdlbnRyeUlkEh0KCn'
    'RyYWNrX3V1aWQYAiABKAlSCXRyYWNrVXVpZBIUCgV0aXRsZRgDIAEoCVIFdGl0bGUSFgoGYXJ0'
    'aXN0GAQgASgJUgZhcnRpc3QSGgoIZHVyYXRpb24YBSABKAJSCGR1cmF0aW9uEhkKCGFsYnVtX2'
    'lkGAYgASgJUgdhbGJ1bUlkEhsKCWNvdmVyX3VyaRgHIAEoCVIIY292ZXJVcmkSGgoIcG9zaXRp'
    'b24YCCABKAVSCHBvc2l0aW9u');
