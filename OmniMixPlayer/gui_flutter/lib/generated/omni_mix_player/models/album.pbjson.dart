// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/album.proto.

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

@$core.Deprecated('Use albumDescriptor instead')
const Album$json = {
  '1': 'Album',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'title', '3': 2, '4': 1, '5': 9, '10': 'title'},
    {'1': 'artist', '3': 3, '4': 1, '5': 9, '10': 'artist'},
    {'1': 'cover_uri', '3': 4, '4': 1, '5': 9, '10': 'coverUri'},
    {'1': 'year', '3': 5, '4': 1, '5': 5, '10': 'year'},
    {'1': 'module_id', '3': 6, '4': 1, '5': 9, '10': 'moduleId'},
  ],
};

/// Descriptor for `Album`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List albumDescriptor = $convert.base64Decode(
    'CgVBbGJ1bRIOCgJpZBgBIAEoCVICaWQSFAoFdGl0bGUYAiABKAlSBXRpdGxlEhYKBmFydGlzdB'
    'gDIAEoCVIGYXJ0aXN0EhsKCWNvdmVyX3VyaRgEIAEoCVIIY292ZXJVcmkSEgoEeWVhchgFIAEo'
    'BVIEeWVhchIbCgltb2R1bGVfaWQYBiABKAlSCG1vZHVsZUlk');

@$core.Deprecated('Use upsertAlbumRequestDescriptor instead')
const UpsertAlbumRequest$json = {
  '1': 'UpsertAlbumRequest',
  '2': [
    {
      '1': 'album',
      '3': 1,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Album',
      '10': 'album'
    },
  ],
};

/// Descriptor for `UpsertAlbumRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertAlbumRequestDescriptor = $convert.base64Decode(
    'ChJVcHNlcnRBbGJ1bVJlcXVlc3QSLAoFYWxidW0YASABKAsyFi5vbW5pX21peF9wbGF5ZXIuQW'
    'xidW1SBWFsYnVt');

@$core.Deprecated('Use upsertAlbumResponseDescriptor instead')
const UpsertAlbumResponse$json = {
  '1': 'UpsertAlbumResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 8, '10': 'created'},
    {
      '1': 'album',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Album',
      '10': 'album'
    },
  ],
};

/// Descriptor for `UpsertAlbumResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertAlbumResponseDescriptor = $convert.base64Decode(
    'ChNVcHNlcnRBbGJ1bVJlc3BvbnNlEhgKB2NyZWF0ZWQYASABKAhSB2NyZWF0ZWQSLAoFYWxidW'
    '0YAiABKAsyFi5vbW5pX21peF9wbGF5ZXIuQWxidW1SBWFsYnVt');

@$core.Deprecated('Use upsertAlbumsRequestDescriptor instead')
const UpsertAlbumsRequest$json = {
  '1': 'UpsertAlbumsRequest',
  '2': [
    {
      '1': 'albums',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Album',
      '10': 'albums'
    },
  ],
};

/// Descriptor for `UpsertAlbumsRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertAlbumsRequestDescriptor = $convert.base64Decode(
    'ChNVcHNlcnRBbGJ1bXNSZXF1ZXN0Ei4KBmFsYnVtcxgBIAMoCzIWLm9tbmlfbWl4X3BsYXllci'
    '5BbGJ1bVIGYWxidW1z');

@$core.Deprecated('Use upsertAlbumsResponseDescriptor instead')
const UpsertAlbumsResponse$json = {
  '1': 'UpsertAlbumsResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 5, '10': 'created'},
    {'1': 'updated', '3': 2, '4': 1, '5': 5, '10': 'updated'},
  ],
};

/// Descriptor for `UpsertAlbumsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertAlbumsResponseDescriptor = $convert.base64Decode(
    'ChRVcHNlcnRBbGJ1bXNSZXNwb25zZRIYCgdjcmVhdGVkGAEgASgFUgdjcmVhdGVkEhgKB3VwZG'
    'F0ZWQYAiABKAVSB3VwZGF0ZWQ=');
