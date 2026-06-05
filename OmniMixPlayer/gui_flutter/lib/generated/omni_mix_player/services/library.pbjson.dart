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

import '../models/album.pbjson.dart' as $2;
import '../models/common.pbjson.dart' as $5;
import '../models/playlist.pbjson.dart' as $4;
import '../models/query.pbjson.dart' as $1;
import '../models/tag.pbjson.dart' as $3;
import '../models/track.pbjson.dart' as $0;

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

const $core.Map<$core.String, $core.dynamic> LibraryServiceBase$json = {
  '1': 'LibraryService',
  '2': [
    {
      '1': 'UpsertTrack',
      '2': '.omni_mix_player.UpsertTrackRequest',
      '3': '.omni_mix_player.UpsertTrackResponse'
    },
    {
      '1': 'UpsertTracks',
      '2': '.omni_mix_player.UpsertTracksRequest',
      '3': '.omni_mix_player.UpsertTracksResponse'
    },
    {
      '1': 'GetTrack',
      '2': '.omni_mix_player.GetTrackRequest',
      '3': '.omni_mix_player.Track'
    },
    {
      '1': 'QueryTracks',
      '2': '.omni_mix_player.TrackQuery',
      '3': '.omni_mix_player.QueryTracksResponse'
    },
    {
      '1': 'DeleteTrack',
      '2': '.omni_mix_player.DeleteTrackRequest',
      '3': '.omni_mix_player.DeleteTrackResponse'
    },
    {
      '1': 'SetTrackTags',
      '2': '.omni_mix_player.SetTrackTagsRequest',
      '3': '.omni_mix_player.SetTrackTagsResponse'
    },
    {
      '1': 'AddTrackTag',
      '2': '.omni_mix_player.ModifyTrackTagRequest',
      '3': '.omni_mix_player.ModifyTrackTagResponse'
    },
    {
      '1': 'RemoveTrackTag',
      '2': '.omni_mix_player.ModifyTrackTagRequest',
      '3': '.omni_mix_player.ModifyTrackTagResponse'
    },
    {
      '1': 'GetTrackTags',
      '2': '.omni_mix_player.GetTrackTagsRequest',
      '3': '.omni_mix_player.GetTrackTagsResponse'
    },
    {
      '1': 'UpsertAlbum',
      '2': '.omni_mix_player.UpsertAlbumRequest',
      '3': '.omni_mix_player.UpsertAlbumResponse'
    },
    {
      '1': 'UpsertAlbums',
      '2': '.omni_mix_player.UpsertAlbumsRequest',
      '3': '.omni_mix_player.UpsertAlbumsResponse'
    },
    {
      '1': 'GetAlbum',
      '2': '.omni_mix_player.GetAlbumRequest',
      '3': '.omni_mix_player.Album'
    },
    {
      '1': 'QueryAlbums',
      '2': '.omni_mix_player.AlbumQuery',
      '3': '.omni_mix_player.QueryAlbumsResponse'
    },
    {
      '1': 'DeleteAlbum',
      '2': '.omni_mix_player.DeleteAlbumRequest',
      '3': '.omni_mix_player.DeleteAlbumResponse'
    },
    {
      '1': 'UpsertTag',
      '2': '.omni_mix_player.UpsertTagRequest',
      '3': '.omni_mix_player.UpsertTagResponse'
    },
    {
      '1': 'UpsertTags',
      '2': '.omni_mix_player.UpsertTagsRequest',
      '3': '.omni_mix_player.UpsertTagsResponse'
    },
    {
      '1': 'GetTag',
      '2': '.omni_mix_player.GetTagRequest',
      '3': '.omni_mix_player.Tag'
    },
    {
      '1': 'QueryTags',
      '2': '.omni_mix_player.TagQuery',
      '3': '.omni_mix_player.QueryTagsResponse'
    },
    {
      '1': 'DeleteTag',
      '2': '.omni_mix_player.DeleteTagRequest',
      '3': '.omni_mix_player.DeleteTagResponse'
    },
    {
      '1': 'UpsertPlaylist',
      '2': '.omni_mix_player.UpsertPlaylistRequest',
      '3': '.omni_mix_player.UpsertPlaylistResponse'
    },
    {
      '1': 'GetPlaylist',
      '2': '.omni_mix_player.GetPlaylistRequest',
      '3': '.omni_mix_player.Playlist'
    },
    {
      '1': 'QueryPlaylists',
      '2': '.omni_mix_player.PlaylistQuery',
      '3': '.omni_mix_player.QueryPlaylistsResponse'
    },
    {
      '1': 'DeletePlaylist',
      '2': '.omni_mix_player.DeletePlaylistRequest',
      '3': '.omni_mix_player.DeletePlaylistResponse'
    },
    {
      '1': 'ReplacePlaylistEntries',
      '2': '.omni_mix_player.ReplacePlaylistEntriesRequest',
      '3': '.omni_mix_player.ReplacePlaylistEntriesResponse'
    },
    {
      '1': 'InsertPlaylistEntry',
      '2': '.omni_mix_player.InsertPlaylistEntryRequest',
      '3': '.omni_mix_player.InsertPlaylistEntryResponse'
    },
    {
      '1': 'RemovePlaylistEntry',
      '2': '.omni_mix_player.RemovePlaylistEntryRequest',
      '3': '.omni_mix_player.RemovePlaylistEntryResponse'
    },
    {
      '1': 'MovePlaylistEntry',
      '2': '.omni_mix_player.MovePlaylistEntryRequest',
      '3': '.omni_mix_player.MovePlaylistEntryResponse'
    },
    {
      '1': 'GetPlaylistWithEntries',
      '2': '.omni_mix_player.GetPlaylistWithEntriesRequest',
      '3': '.omni_mix_player.PlaylistWithEntries'
    },
    {
      '1': 'UnregisterModule',
      '2': '.omni_mix_player.UnregisterModuleRequest',
      '3': '.omni_mix_player.UnregisterModuleResponse'
    },
  ],
};

@$core.Deprecated('Use libraryServiceDescriptor instead')
const $core.Map<$core.String, $core.Map<$core.String, $core.dynamic>>
    LibraryServiceBase$messageJson = {
  '.omni_mix_player.UpsertTrackRequest': $0.UpsertTrackRequest$json,
  '.omni_mix_player.Track': $0.Track$json,
  '.omni_mix_player.OmniTimestamp': $5.OmniTimestamp$json,
  '.omni_mix_player.UpsertTrackResponse': $0.UpsertTrackResponse$json,
  '.omni_mix_player.UpsertTracksRequest': $0.UpsertTracksRequest$json,
  '.omni_mix_player.UpsertTracksResponse': $0.UpsertTracksResponse$json,
  '.omni_mix_player.GetTrackRequest': GetTrackRequest$json,
  '.omni_mix_player.TrackQuery': $1.TrackQuery$json,
  '.omni_mix_player.TrackSort': $1.TrackSort$json,
  '.omni_mix_player.QueryTracksResponse': $1.QueryTracksResponse$json,
  '.omni_mix_player.Pagination': $1.Pagination$json,
  '.omni_mix_player.DeleteTrackRequest': DeleteTrackRequest$json,
  '.omni_mix_player.DeleteTrackResponse': DeleteTrackResponse$json,
  '.omni_mix_player.SetTrackTagsRequest': $0.SetTrackTagsRequest$json,
  '.omni_mix_player.SetTrackTagsResponse': $0.SetTrackTagsResponse$json,
  '.omni_mix_player.ModifyTrackTagRequest': $0.ModifyTrackTagRequest$json,
  '.omni_mix_player.ModifyTrackTagResponse': $0.ModifyTrackTagResponse$json,
  '.omni_mix_player.GetTrackTagsRequest': GetTrackTagsRequest$json,
  '.omni_mix_player.GetTrackTagsResponse': GetTrackTagsResponse$json,
  '.omni_mix_player.UpsertAlbumRequest': $2.UpsertAlbumRequest$json,
  '.omni_mix_player.Album': $2.Album$json,
  '.omni_mix_player.UpsertAlbumResponse': $2.UpsertAlbumResponse$json,
  '.omni_mix_player.UpsertAlbumsRequest': $2.UpsertAlbumsRequest$json,
  '.omni_mix_player.UpsertAlbumsResponse': $2.UpsertAlbumsResponse$json,
  '.omni_mix_player.GetAlbumRequest': GetAlbumRequest$json,
  '.omni_mix_player.AlbumQuery': $1.AlbumQuery$json,
  '.omni_mix_player.QueryAlbumsResponse': $1.QueryAlbumsResponse$json,
  '.omni_mix_player.DeleteAlbumRequest': DeleteAlbumRequest$json,
  '.omni_mix_player.DeleteAlbumResponse': DeleteAlbumResponse$json,
  '.omni_mix_player.UpsertTagRequest': $3.UpsertTagRequest$json,
  '.omni_mix_player.Tag': $3.Tag$json,
  '.omni_mix_player.UpsertTagResponse': $3.UpsertTagResponse$json,
  '.omni_mix_player.UpsertTagsRequest': $3.UpsertTagsRequest$json,
  '.omni_mix_player.UpsertTagsResponse': $3.UpsertTagsResponse$json,
  '.omni_mix_player.GetTagRequest': GetTagRequest$json,
  '.omni_mix_player.TagQuery': $1.TagQuery$json,
  '.omni_mix_player.QueryTagsResponse': $1.QueryTagsResponse$json,
  '.omni_mix_player.DeleteTagRequest': DeleteTagRequest$json,
  '.omni_mix_player.DeleteTagResponse': DeleteTagResponse$json,
  '.omni_mix_player.UpsertPlaylistRequest': $4.UpsertPlaylistRequest$json,
  '.omni_mix_player.Playlist': $4.Playlist$json,
  '.omni_mix_player.UpsertPlaylistResponse': $4.UpsertPlaylistResponse$json,
  '.omni_mix_player.GetPlaylistRequest': GetPlaylistRequest$json,
  '.omni_mix_player.PlaylistQuery': $1.PlaylistQuery$json,
  '.omni_mix_player.QueryPlaylistsResponse': $1.QueryPlaylistsResponse$json,
  '.omni_mix_player.DeletePlaylistRequest': DeletePlaylistRequest$json,
  '.omni_mix_player.DeletePlaylistResponse': DeletePlaylistResponse$json,
  '.omni_mix_player.ReplacePlaylistEntriesRequest':
      $4.ReplacePlaylistEntriesRequest$json,
  '.omni_mix_player.PlaylistEntrySpec': $4.PlaylistEntrySpec$json,
  '.omni_mix_player.ReplacePlaylistEntriesResponse':
      $4.ReplacePlaylistEntriesResponse$json,
  '.omni_mix_player.InsertPlaylistEntryRequest':
      $4.InsertPlaylistEntryRequest$json,
  '.omni_mix_player.InsertPlaylistEntryResponse':
      $4.InsertPlaylistEntryResponse$json,
  '.omni_mix_player.PlaylistEntry': $4.PlaylistEntry$json,
  '.omni_mix_player.RemovePlaylistEntryRequest':
      $4.RemovePlaylistEntryRequest$json,
  '.omni_mix_player.RemovePlaylistEntryResponse':
      $4.RemovePlaylistEntryResponse$json,
  '.omni_mix_player.MovePlaylistEntryRequest': $4.MovePlaylistEntryRequest$json,
  '.omni_mix_player.MovePlaylistEntryResponse':
      $4.MovePlaylistEntryResponse$json,
  '.omni_mix_player.GetPlaylistWithEntriesRequest':
      GetPlaylistWithEntriesRequest$json,
  '.omni_mix_player.PlaylistWithEntries': $4.PlaylistWithEntries$json,
  '.omni_mix_player.PlaylistEntryWithTrack': $4.PlaylistEntryWithTrack$json,
  '.omni_mix_player.UnregisterModuleRequest': UnregisterModuleRequest$json,
  '.omni_mix_player.UnregisterModuleResponse': UnregisterModuleResponse$json,
};

/// Descriptor for `LibraryService`. Decode as a `google.protobuf.ServiceDescriptorProto`.
final $typed_data.Uint8List libraryServiceDescriptor = $convert.base64Decode(
    'Cg5MaWJyYXJ5U2VydmljZRJYCgtVcHNlcnRUcmFjaxIjLm9tbmlfbWl4X3BsYXllci5VcHNlcn'
    'RUcmFja1JlcXVlc3QaJC5vbW5pX21peF9wbGF5ZXIuVXBzZXJ0VHJhY2tSZXNwb25zZRJbCgxV'
    'cHNlcnRUcmFja3MSJC5vbW5pX21peF9wbGF5ZXIuVXBzZXJ0VHJhY2tzUmVxdWVzdBolLm9tbm'
    'lfbWl4X3BsYXllci5VcHNlcnRUcmFja3NSZXNwb25zZRJECghHZXRUcmFjaxIgLm9tbmlfbWl4'
    'X3BsYXllci5HZXRUcmFja1JlcXVlc3QaFi5vbW5pX21peF9wbGF5ZXIuVHJhY2sSUAoLUXVlcn'
    'lUcmFja3MSGy5vbW5pX21peF9wbGF5ZXIuVHJhY2tRdWVyeRokLm9tbmlfbWl4X3BsYXllci5R'
    'dWVyeVRyYWNrc1Jlc3BvbnNlElgKC0RlbGV0ZVRyYWNrEiMub21uaV9taXhfcGxheWVyLkRlbG'
    'V0ZVRyYWNrUmVxdWVzdBokLm9tbmlfbWl4X3BsYXllci5EZWxldGVUcmFja1Jlc3BvbnNlElsK'
    'DFNldFRyYWNrVGFncxIkLm9tbmlfbWl4X3BsYXllci5TZXRUcmFja1RhZ3NSZXF1ZXN0GiUub2'
    '1uaV9taXhfcGxheWVyLlNldFRyYWNrVGFnc1Jlc3BvbnNlEl4KC0FkZFRyYWNrVGFnEiYub21u'
    'aV9taXhfcGxheWVyLk1vZGlmeVRyYWNrVGFnUmVxdWVzdBonLm9tbmlfbWl4X3BsYXllci5Nb2'
    'RpZnlUcmFja1RhZ1Jlc3BvbnNlEmEKDlJlbW92ZVRyYWNrVGFnEiYub21uaV9taXhfcGxheWVy'
    'Lk1vZGlmeVRyYWNrVGFnUmVxdWVzdBonLm9tbmlfbWl4X3BsYXllci5Nb2RpZnlUcmFja1RhZ1'
    'Jlc3BvbnNlElsKDEdldFRyYWNrVGFncxIkLm9tbmlfbWl4X3BsYXllci5HZXRUcmFja1RhZ3NS'
    'ZXF1ZXN0GiUub21uaV9taXhfcGxheWVyLkdldFRyYWNrVGFnc1Jlc3BvbnNlElgKC1Vwc2VydE'
    'FsYnVtEiMub21uaV9taXhfcGxheWVyLlVwc2VydEFsYnVtUmVxdWVzdBokLm9tbmlfbWl4X3Bs'
    'YXllci5VcHNlcnRBbGJ1bVJlc3BvbnNlElsKDFVwc2VydEFsYnVtcxIkLm9tbmlfbWl4X3BsYX'
    'llci5VcHNlcnRBbGJ1bXNSZXF1ZXN0GiUub21uaV9taXhfcGxheWVyLlVwc2VydEFsYnVtc1Jl'
    'c3BvbnNlEkQKCEdldEFsYnVtEiAub21uaV9taXhfcGxheWVyLkdldEFsYnVtUmVxdWVzdBoWLm'
    '9tbmlfbWl4X3BsYXllci5BbGJ1bRJQCgtRdWVyeUFsYnVtcxIbLm9tbmlfbWl4X3BsYXllci5B'
    'bGJ1bVF1ZXJ5GiQub21uaV9taXhfcGxheWVyLlF1ZXJ5QWxidW1zUmVzcG9uc2USWAoLRGVsZX'
    'RlQWxidW0SIy5vbW5pX21peF9wbGF5ZXIuRGVsZXRlQWxidW1SZXF1ZXN0GiQub21uaV9taXhf'
    'cGxheWVyLkRlbGV0ZUFsYnVtUmVzcG9uc2USUgoJVXBzZXJ0VGFnEiEub21uaV9taXhfcGxheW'
    'VyLlVwc2VydFRhZ1JlcXVlc3QaIi5vbW5pX21peF9wbGF5ZXIuVXBzZXJ0VGFnUmVzcG9uc2US'
    'VQoKVXBzZXJ0VGFncxIiLm9tbmlfbWl4X3BsYXllci5VcHNlcnRUYWdzUmVxdWVzdBojLm9tbm'
    'lfbWl4X3BsYXllci5VcHNlcnRUYWdzUmVzcG9uc2USPgoGR2V0VGFnEh4ub21uaV9taXhfcGxh'
    'eWVyLkdldFRhZ1JlcXVlc3QaFC5vbW5pX21peF9wbGF5ZXIuVGFnEkoKCVF1ZXJ5VGFncxIZLm'
    '9tbmlfbWl4X3BsYXllci5UYWdRdWVyeRoiLm9tbmlfbWl4X3BsYXllci5RdWVyeVRhZ3NSZXNw'
    'b25zZRJSCglEZWxldGVUYWcSIS5vbW5pX21peF9wbGF5ZXIuRGVsZXRlVGFnUmVxdWVzdBoiLm'
    '9tbmlfbWl4X3BsYXllci5EZWxldGVUYWdSZXNwb25zZRJhCg5VcHNlcnRQbGF5bGlzdBImLm9t'
    'bmlfbWl4X3BsYXllci5VcHNlcnRQbGF5bGlzdFJlcXVlc3QaJy5vbW5pX21peF9wbGF5ZXIuVX'
    'BzZXJ0UGxheWxpc3RSZXNwb25zZRJNCgtHZXRQbGF5bGlzdBIjLm9tbmlfbWl4X3BsYXllci5H'
    'ZXRQbGF5bGlzdFJlcXVlc3QaGS5vbW5pX21peF9wbGF5ZXIuUGxheWxpc3QSWQoOUXVlcnlQbG'
    'F5bGlzdHMSHi5vbW5pX21peF9wbGF5ZXIuUGxheWxpc3RRdWVyeRonLm9tbmlfbWl4X3BsYXll'
    'ci5RdWVyeVBsYXlsaXN0c1Jlc3BvbnNlEmEKDkRlbGV0ZVBsYXlsaXN0EiYub21uaV9taXhfcG'
    'xheWVyLkRlbGV0ZVBsYXlsaXN0UmVxdWVzdBonLm9tbmlfbWl4X3BsYXllci5EZWxldGVQbGF5'
    'bGlzdFJlc3BvbnNlEnkKFlJlcGxhY2VQbGF5bGlzdEVudHJpZXMSLi5vbW5pX21peF9wbGF5ZX'
    'IuUmVwbGFjZVBsYXlsaXN0RW50cmllc1JlcXVlc3QaLy5vbW5pX21peF9wbGF5ZXIuUmVwbGFj'
    'ZVBsYXlsaXN0RW50cmllc1Jlc3BvbnNlEnAKE0luc2VydFBsYXlsaXN0RW50cnkSKy5vbW5pX2'
    '1peF9wbGF5ZXIuSW5zZXJ0UGxheWxpc3RFbnRyeVJlcXVlc3QaLC5vbW5pX21peF9wbGF5ZXIu'
    'SW5zZXJ0UGxheWxpc3RFbnRyeVJlc3BvbnNlEnAKE1JlbW92ZVBsYXlsaXN0RW50cnkSKy5vbW'
    '5pX21peF9wbGF5ZXIuUmVtb3ZlUGxheWxpc3RFbnRyeVJlcXVlc3QaLC5vbW5pX21peF9wbGF5'
    'ZXIuUmVtb3ZlUGxheWxpc3RFbnRyeVJlc3BvbnNlEmoKEU1vdmVQbGF5bGlzdEVudHJ5Eikub2'
    '1uaV9taXhfcGxheWVyLk1vdmVQbGF5bGlzdEVudHJ5UmVxdWVzdBoqLm9tbmlfbWl4X3BsYXll'
    'ci5Nb3ZlUGxheWxpc3RFbnRyeVJlc3BvbnNlEm4KFkdldFBsYXlsaXN0V2l0aEVudHJpZXMSLi'
    '5vbW5pX21peF9wbGF5ZXIuR2V0UGxheWxpc3RXaXRoRW50cmllc1JlcXVlc3QaJC5vbW5pX21p'
    'eF9wbGF5ZXIuUGxheWxpc3RXaXRoRW50cmllcxJnChBVbnJlZ2lzdGVyTW9kdWxlEigub21uaV'
    '9taXhfcGxheWVyLlVucmVnaXN0ZXJNb2R1bGVSZXF1ZXN0Gikub21uaV9taXhfcGxheWVyLlVu'
    'cmVnaXN0ZXJNb2R1bGVSZXNwb25zZQ==');
