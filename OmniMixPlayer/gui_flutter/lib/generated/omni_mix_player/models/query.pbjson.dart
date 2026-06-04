// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/query.proto.

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

@$core.Deprecated('Use trackQueryDescriptor instead')
const TrackQuery$json = {
  '1': 'TrackQuery',
  '2': [
    {'1': 'text', '3': 1, '4': 1, '5': 9, '10': 'text'},
    {'1': 'album_id', '3': 2, '4': 1, '5': 9, '10': 'albumId'},
    {'1': 'tag_ids', '3': 3, '4': 3, '5': 9, '10': 'tagIds'},
    {'1': 'playlist_id', '3': 4, '4': 1, '5': 9, '10': 'playlistId'},
    {'1': 'module_id', '3': 5, '4': 1, '5': 9, '10': 'moduleId'},
    {
      '1': 'is_favorite',
      '3': 6,
      '4': 1,
      '5': 8,
      '9': 0,
      '10': 'isFavorite',
      '17': true
    },
    {
      '1': 'is_excluded',
      '3': 7,
      '4': 1,
      '5': 8,
      '9': 1,
      '10': 'isExcluded',
      '17': true
    },
    {'1': 'offset', '3': 10, '4': 1, '5': 5, '10': 'offset'},
    {'1': 'limit', '3': 11, '4': 1, '5': 5, '10': 'limit'},
    {
      '1': 'sort',
      '3': 12,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.TrackSort',
      '10': 'sort'
    },
  ],
  '8': [
    {'1': '_is_favorite'},
    {'1': '_is_excluded'},
  ],
};

/// Descriptor for `TrackQuery`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List trackQueryDescriptor = $convert.base64Decode(
    'CgpUcmFja1F1ZXJ5EhIKBHRleHQYASABKAlSBHRleHQSGQoIYWxidW1faWQYAiABKAlSB2FsYn'
    'VtSWQSFwoHdGFnX2lkcxgDIAMoCVIGdGFnSWRzEh8KC3BsYXlsaXN0X2lkGAQgASgJUgpwbGF5'
    'bGlzdElkEhsKCW1vZHVsZV9pZBgFIAEoCVIIbW9kdWxlSWQSJAoLaXNfZmF2b3JpdGUYBiABKA'
    'hIAFIKaXNGYXZvcml0ZYgBARIkCgtpc19leGNsdWRlZBgHIAEoCEgBUgppc0V4Y2x1ZGVkiAEB'
    'EhYKBm9mZnNldBgKIAEoBVIGb2Zmc2V0EhQKBWxpbWl0GAsgASgFUgVsaW1pdBIuCgRzb3J0GA'
    'wgASgLMhoub21uaV9taXhfcGxheWVyLlRyYWNrU29ydFIEc29ydEIOCgxfaXNfZmF2b3JpdGVC'
    'DgoMX2lzX2V4Y2x1ZGVk');

@$core.Deprecated('Use trackSortDescriptor instead')
const TrackSort$json = {
  '1': 'TrackSort',
  '2': [
    {
      '1': 'field',
      '3': 1,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.TrackSortField',
      '10': 'field'
    },
    {
      '1': 'direction',
      '3': 2,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.SortDirection',
      '10': 'direction'
    },
  ],
};

/// Descriptor for `TrackSort`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List trackSortDescriptor = $convert.base64Decode(
    'CglUcmFja1NvcnQSNQoFZmllbGQYASABKA4yHy5vbW5pX21peF9wbGF5ZXIuVHJhY2tTb3J0Rm'
    'llbGRSBWZpZWxkEjwKCWRpcmVjdGlvbhgCIAEoDjIeLm9tbmlfbWl4X3BsYXllci5Tb3J0RGly'
    'ZWN0aW9uUglkaXJlY3Rpb24=');

@$core.Deprecated('Use paginationDescriptor instead')
const Pagination$json = {
  '1': 'Pagination',
  '2': [
    {'1': 'offset', '3': 1, '4': 1, '5': 5, '10': 'offset'},
    {'1': 'limit', '3': 2, '4': 1, '5': 5, '10': 'limit'},
    {'1': 'total', '3': 3, '4': 1, '5': 5, '10': 'total'},
  ],
};

/// Descriptor for `Pagination`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List paginationDescriptor = $convert.base64Decode(
    'CgpQYWdpbmF0aW9uEhYKBm9mZnNldBgBIAEoBVIGb2Zmc2V0EhQKBWxpbWl0GAIgASgFUgVsaW'
    '1pdBIUCgV0b3RhbBgDIAEoBVIFdG90YWw=');

@$core.Deprecated('Use queryTracksResponseDescriptor instead')
const QueryTracksResponse$json = {
  '1': 'QueryTracksResponse',
  '2': [
    {
      '1': 'tracks',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Track',
      '10': 'tracks'
    },
    {
      '1': 'pagination',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Pagination',
      '10': 'pagination'
    },
  ],
};

/// Descriptor for `QueryTracksResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List queryTracksResponseDescriptor = $convert.base64Decode(
    'ChNRdWVyeVRyYWNrc1Jlc3BvbnNlEi4KBnRyYWNrcxgBIAMoCzIWLm9tbmlfbWl4X3BsYXllci'
    '5UcmFja1IGdHJhY2tzEjsKCnBhZ2luYXRpb24YAiABKAsyGy5vbW5pX21peF9wbGF5ZXIuUGFn'
    'aW5hdGlvblIKcGFnaW5hdGlvbg==');

@$core.Deprecated('Use albumQueryDescriptor instead')
const AlbumQuery$json = {
  '1': 'AlbumQuery',
  '2': [
    {'1': 'tag_id', '3': 1, '4': 1, '5': 9, '10': 'tagId'},
    {'1': 'module_id', '3': 2, '4': 1, '5': 9, '10': 'moduleId'},
    {'1': 'text', '3': 3, '4': 1, '5': 9, '10': 'text'},
    {'1': 'offset', '3': 4, '4': 1, '5': 5, '10': 'offset'},
    {'1': 'limit', '3': 5, '4': 1, '5': 5, '10': 'limit'},
  ],
};

/// Descriptor for `AlbumQuery`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List albumQueryDescriptor = $convert.base64Decode(
    'CgpBbGJ1bVF1ZXJ5EhUKBnRhZ19pZBgBIAEoCVIFdGFnSWQSGwoJbW9kdWxlX2lkGAIgASgJUg'
    'htb2R1bGVJZBISCgR0ZXh0GAMgASgJUgR0ZXh0EhYKBm9mZnNldBgEIAEoBVIGb2Zmc2V0EhQK'
    'BWxpbWl0GAUgASgFUgVsaW1pdA==');

@$core.Deprecated('Use queryAlbumsResponseDescriptor instead')
const QueryAlbumsResponse$json = {
  '1': 'QueryAlbumsResponse',
  '2': [
    {
      '1': 'albums',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Album',
      '10': 'albums'
    },
    {
      '1': 'pagination',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Pagination',
      '10': 'pagination'
    },
  ],
};

/// Descriptor for `QueryAlbumsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List queryAlbumsResponseDescriptor = $convert.base64Decode(
    'ChNRdWVyeUFsYnVtc1Jlc3BvbnNlEi4KBmFsYnVtcxgBIAMoCzIWLm9tbmlfbWl4X3BsYXllci'
    '5BbGJ1bVIGYWxidW1zEjsKCnBhZ2luYXRpb24YAiABKAsyGy5vbW5pX21peF9wbGF5ZXIuUGFn'
    'aW5hdGlvblIKcGFnaW5hdGlvbg==');

@$core.Deprecated('Use tagQueryDescriptor instead')
const TagQuery$json = {
  '1': 'TagQuery',
  '2': [
    {'1': 'module_id', '3': 1, '4': 1, '5': 9, '10': 'moduleId'},
    {
      '1': 'kinds',
      '3': 2,
      '4': 3,
      '5': 14,
      '6': '.omni_mix_player.TagKind',
      '10': 'kinds'
    },
    {'1': 'offset', '3': 3, '4': 1, '5': 5, '10': 'offset'},
    {'1': 'limit', '3': 4, '4': 1, '5': 5, '10': 'limit'},
  ],
};

/// Descriptor for `TagQuery`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List tagQueryDescriptor = $convert.base64Decode(
    'CghUYWdRdWVyeRIbCgltb2R1bGVfaWQYASABKAlSCG1vZHVsZUlkEi4KBWtpbmRzGAIgAygOMh'
    'gub21uaV9taXhfcGxheWVyLlRhZ0tpbmRSBWtpbmRzEhYKBm9mZnNldBgDIAEoBVIGb2Zmc2V0'
    'EhQKBWxpbWl0GAQgASgFUgVsaW1pdA==');

@$core.Deprecated('Use queryTagsResponseDescriptor instead')
const QueryTagsResponse$json = {
  '1': 'QueryTagsResponse',
  '2': [
    {
      '1': 'tags',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Tag',
      '10': 'tags'
    },
    {
      '1': 'pagination',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Pagination',
      '10': 'pagination'
    },
  ],
};

/// Descriptor for `QueryTagsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List queryTagsResponseDescriptor = $convert.base64Decode(
    'ChFRdWVyeVRhZ3NSZXNwb25zZRIoCgR0YWdzGAEgAygLMhQub21uaV9taXhfcGxheWVyLlRhZ1'
    'IEdGFncxI7CgpwYWdpbmF0aW9uGAIgASgLMhsub21uaV9taXhfcGxheWVyLlBhZ2luYXRpb25S'
    'CnBhZ2luYXRpb24=');

@$core.Deprecated('Use playlistQueryDescriptor instead')
const PlaylistQuery$json = {
  '1': 'PlaylistQuery',
  '2': [
    {'1': 'module_id', '3': 1, '4': 1, '5': 9, '10': 'moduleId'},
    {
      '1': 'kinds',
      '3': 2,
      '4': 3,
      '5': 14,
      '6': '.omni_mix_player.PlaylistKind',
      '10': 'kinds'
    },
    {'1': 'offset', '3': 3, '4': 1, '5': 5, '10': 'offset'},
    {'1': 'limit', '3': 4, '4': 1, '5': 5, '10': 'limit'},
  ],
};

/// Descriptor for `PlaylistQuery`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List playlistQueryDescriptor = $convert.base64Decode(
    'Cg1QbGF5bGlzdFF1ZXJ5EhsKCW1vZHVsZV9pZBgBIAEoCVIIbW9kdWxlSWQSMwoFa2luZHMYAi'
    'ADKA4yHS5vbW5pX21peF9wbGF5ZXIuUGxheWxpc3RLaW5kUgVraW5kcxIWCgZvZmZzZXQYAyAB'
    'KAVSBm9mZnNldBIUCgVsaW1pdBgEIAEoBVIFbGltaXQ=');

@$core.Deprecated('Use queryPlaylistsResponseDescriptor instead')
const QueryPlaylistsResponse$json = {
  '1': 'QueryPlaylistsResponse',
  '2': [
    {
      '1': 'playlists',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Playlist',
      '10': 'playlists'
    },
    {
      '1': 'pagination',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Pagination',
      '10': 'pagination'
    },
  ],
};

/// Descriptor for `QueryPlaylistsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List queryPlaylistsResponseDescriptor = $convert.base64Decode(
    'ChZRdWVyeVBsYXlsaXN0c1Jlc3BvbnNlEjcKCXBsYXlsaXN0cxgBIAMoCzIZLm9tbmlfbWl4X3'
    'BsYXllci5QbGF5bGlzdFIJcGxheWxpc3RzEjsKCnBhZ2luYXRpb24YAiABKAsyGy5vbW5pX21p'
    'eF9wbGF5ZXIuUGFnaW5hdGlvblIKcGFnaW5hdGlvbg==');
