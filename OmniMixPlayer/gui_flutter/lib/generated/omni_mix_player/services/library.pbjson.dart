// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/library.proto.

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

@$core.Deprecated('Use getTrackRequestDescriptor instead')
const GetTrackRequest$json = {
  '1': 'GetTrackRequest',
  '2': [
    {'1': 'uuid', '3': 1, '4': 1, '5': 9, '10': 'uuid'},
  ],
};

/// Descriptor for `GetTrackRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getTrackRequestDescriptor = $convert
    .base64Decode('Cg9HZXRUcmFja1JlcXVlc3QSEgoEdXVpZBgBIAEoCVIEdXVpZA==');

@$core.Deprecated('Use deleteTrackRequestDescriptor instead')
const DeleteTrackRequest$json = {
  '1': 'DeleteTrackRequest',
  '2': [
    {'1': 'uuid', '3': 1, '4': 1, '5': 9, '10': 'uuid'},
  ],
};

/// Descriptor for `DeleteTrackRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteTrackRequestDescriptor = $convert
    .base64Decode('ChJEZWxldGVUcmFja1JlcXVlc3QSEgoEdXVpZBgBIAEoCVIEdXVpZA==');

@$core.Deprecated('Use deleteTrackResponseDescriptor instead')
const DeleteTrackResponse$json = {
  '1': 'DeleteTrackResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `DeleteTrackResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteTrackResponseDescriptor =
    $convert.base64Decode(
        'ChNEZWxldGVUcmFja1Jlc3BvbnNlEhgKB3N1Y2Nlc3MYASABKAhSB3N1Y2Nlc3M=');

@$core.Deprecated('Use getTrackTagsRequestDescriptor instead')
const GetTrackTagsRequest$json = {
  '1': 'GetTrackTagsRequest',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
  ],
};

/// Descriptor for `GetTrackTagsRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getTrackTagsRequestDescriptor = $convert.base64Decode(
    'ChNHZXRUcmFja1RhZ3NSZXF1ZXN0Eh0KCnRyYWNrX3V1aWQYASABKAlSCXRyYWNrVXVpZA==');

@$core.Deprecated('Use getTrackTagsResponseDescriptor instead')
const GetTrackTagsResponse$json = {
  '1': 'GetTrackTagsResponse',
  '2': [
    {'1': 'track_uuid', '3': 1, '4': 1, '5': 9, '10': 'trackUuid'},
    {'1': 'tag_ids', '3': 2, '4': 3, '5': 9, '10': 'tagIds'},
  ],
};

/// Descriptor for `GetTrackTagsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getTrackTagsResponseDescriptor = $convert.base64Decode(
    'ChRHZXRUcmFja1RhZ3NSZXNwb25zZRIdCgp0cmFja191dWlkGAEgASgJUgl0cmFja1V1aWQSFw'
    'oHdGFnX2lkcxgCIAMoCVIGdGFnSWRz');

@$core.Deprecated('Use getAlbumRequestDescriptor instead')
const GetAlbumRequest$json = {
  '1': 'GetAlbumRequest',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
  ],
};

/// Descriptor for `GetAlbumRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getAlbumRequestDescriptor =
    $convert.base64Decode('Cg9HZXRBbGJ1bVJlcXVlc3QSDgoCaWQYASABKAlSAmlk');

@$core.Deprecated('Use deleteAlbumRequestDescriptor instead')
const DeleteAlbumRequest$json = {
  '1': 'DeleteAlbumRequest',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
  ],
};

/// Descriptor for `DeleteAlbumRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteAlbumRequestDescriptor =
    $convert.base64Decode('ChJEZWxldGVBbGJ1bVJlcXVlc3QSDgoCaWQYASABKAlSAmlk');

@$core.Deprecated('Use deleteAlbumResponseDescriptor instead')
const DeleteAlbumResponse$json = {
  '1': 'DeleteAlbumResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `DeleteAlbumResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteAlbumResponseDescriptor =
    $convert.base64Decode(
        'ChNEZWxldGVBbGJ1bVJlc3BvbnNlEhgKB3N1Y2Nlc3MYASABKAhSB3N1Y2Nlc3M=');

@$core.Deprecated('Use getTagRequestDescriptor instead')
const GetTagRequest$json = {
  '1': 'GetTagRequest',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
  ],
};

/// Descriptor for `GetTagRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getTagRequestDescriptor =
    $convert.base64Decode('Cg1HZXRUYWdSZXF1ZXN0Eg4KAmlkGAEgASgJUgJpZA==');

@$core.Deprecated('Use deleteTagRequestDescriptor instead')
const DeleteTagRequest$json = {
  '1': 'DeleteTagRequest',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
  ],
};

/// Descriptor for `DeleteTagRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteTagRequestDescriptor =
    $convert.base64Decode('ChBEZWxldGVUYWdSZXF1ZXN0Eg4KAmlkGAEgASgJUgJpZA==');

@$core.Deprecated('Use deleteTagResponseDescriptor instead')
const DeleteTagResponse$json = {
  '1': 'DeleteTagResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `DeleteTagResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deleteTagResponseDescriptor = $convert.base64Decode(
    'ChFEZWxldGVUYWdSZXNwb25zZRIYCgdzdWNjZXNzGAEgASgIUgdzdWNjZXNz');

@$core.Deprecated('Use getPlaylistRequestDescriptor instead')
const GetPlaylistRequest$json = {
  '1': 'GetPlaylistRequest',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
  ],
};

/// Descriptor for `GetPlaylistRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getPlaylistRequestDescriptor =
    $convert.base64Decode('ChJHZXRQbGF5bGlzdFJlcXVlc3QSDgoCaWQYASABKAlSAmlk');

@$core.Deprecated('Use deletePlaylistRequestDescriptor instead')
const DeletePlaylistRequest$json = {
  '1': 'DeletePlaylistRequest',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
  ],
};

/// Descriptor for `DeletePlaylistRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deletePlaylistRequestDescriptor = $convert
    .base64Decode('ChVEZWxldGVQbGF5bGlzdFJlcXVlc3QSDgoCaWQYASABKAlSAmlk');

@$core.Deprecated('Use deletePlaylistResponseDescriptor instead')
const DeletePlaylistResponse$json = {
  '1': 'DeletePlaylistResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
  ],
};

/// Descriptor for `DeletePlaylistResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List deletePlaylistResponseDescriptor =
    $convert.base64Decode(
        'ChZEZWxldGVQbGF5bGlzdFJlc3BvbnNlEhgKB3N1Y2Nlc3MYASABKAhSB3N1Y2Nlc3M=');

@$core.Deprecated('Use getPlaylistWithEntriesRequestDescriptor instead')
const GetPlaylistWithEntriesRequest$json = {
  '1': 'GetPlaylistWithEntriesRequest',
  '2': [
    {'1': 'playlist_id', '3': 1, '4': 1, '5': 9, '10': 'playlistId'},
  ],
};

/// Descriptor for `GetPlaylistWithEntriesRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List getPlaylistWithEntriesRequestDescriptor =
    $convert.base64Decode(
        'Ch1HZXRQbGF5bGlzdFdpdGhFbnRyaWVzUmVxdWVzdBIfCgtwbGF5bGlzdF9pZBgBIAEoCVIKcG'
        'xheWxpc3RJZA==');

@$core.Deprecated('Use unregisterModuleRequestDescriptor instead')
const UnregisterModuleRequest$json = {
  '1': 'UnregisterModuleRequest',
  '2': [
    {'1': 'module_id', '3': 1, '4': 1, '5': 9, '10': 'moduleId'},
  ],
};

/// Descriptor for `UnregisterModuleRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List unregisterModuleRequestDescriptor =
    $convert.base64Decode(
        'ChdVbnJlZ2lzdGVyTW9kdWxlUmVxdWVzdBIbCgltb2R1bGVfaWQYASABKAlSCG1vZHVsZUlk');

@$core.Deprecated('Use unregisterModuleResponseDescriptor instead')
const UnregisterModuleResponse$json = {
  '1': 'UnregisterModuleResponse',
  '2': [
    {'1': 'success', '3': 1, '4': 1, '5': 8, '10': 'success'},
    {'1': 'tracks_removed', '3': 2, '4': 1, '5': 5, '10': 'tracksRemoved'},
    {'1': 'albums_removed', '3': 3, '4': 1, '5': 5, '10': 'albumsRemoved'},
    {'1': 'tags_removed', '3': 4, '4': 1, '5': 5, '10': 'tagsRemoved'},
    {
      '1': 'playlists_removed',
      '3': 5,
      '4': 1,
      '5': 5,
      '10': 'playlistsRemoved'
    },
  ],
};

/// Descriptor for `UnregisterModuleResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List unregisterModuleResponseDescriptor = $convert.base64Decode(
    'ChhVbnJlZ2lzdGVyTW9kdWxlUmVzcG9uc2USGAoHc3VjY2VzcxgBIAEoCFIHc3VjY2VzcxIlCg'
    '50cmFja3NfcmVtb3ZlZBgCIAEoBVINdHJhY2tzUmVtb3ZlZBIlCg5hbGJ1bXNfcmVtb3ZlZBgD'
    'IAEoBVINYWxidW1zUmVtb3ZlZBIhCgx0YWdzX3JlbW92ZWQYBCABKAVSC3RhZ3NSZW1vdmVkEi'
    'sKEXBsYXlsaXN0c19yZW1vdmVkGAUgASgFUhBwbGF5bGlzdHNSZW1vdmVk');
