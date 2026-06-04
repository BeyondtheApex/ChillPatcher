// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/tag.proto.

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

@$core.Deprecated('Use tagDescriptor instead')
const Tag$json = {
  '1': 'Tag',
  '2': [
    {'1': 'id', '3': 1, '4': 1, '5': 9, '10': 'id'},
    {'1': 'name', '3': 2, '4': 1, '5': 9, '10': 'name'},
    {'1': 'color', '3': 3, '4': 1, '5': 9, '10': 'color'},
    {'1': 'module_id', '3': 4, '4': 1, '5': 9, '10': 'moduleId'},
    {
      '1': 'kind',
      '3': 5,
      '4': 1,
      '5': 14,
      '6': '.omni_mix_player.TagKind',
      '10': 'kind'
    },
  ],
};

/// Descriptor for `Tag`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List tagDescriptor = $convert.base64Decode(
    'CgNUYWcSDgoCaWQYASABKAlSAmlkEhIKBG5hbWUYAiABKAlSBG5hbWUSFAoFY29sb3IYAyABKA'
    'lSBWNvbG9yEhsKCW1vZHVsZV9pZBgEIAEoCVIIbW9kdWxlSWQSLAoEa2luZBgFIAEoDjIYLm9t'
    'bmlfbWl4X3BsYXllci5UYWdLaW5kUgRraW5k');

@$core.Deprecated('Use upsertTagRequestDescriptor instead')
const UpsertTagRequest$json = {
  '1': 'UpsertTagRequest',
  '2': [
    {
      '1': 'tag',
      '3': 1,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Tag',
      '10': 'tag'
    },
  ],
};

/// Descriptor for `UpsertTagRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTagRequestDescriptor = $convert.base64Decode(
    'ChBVcHNlcnRUYWdSZXF1ZXN0EiYKA3RhZxgBIAEoCzIULm9tbmlfbWl4X3BsYXllci5UYWdSA3'
    'RhZw==');

@$core.Deprecated('Use upsertTagResponseDescriptor instead')
const UpsertTagResponse$json = {
  '1': 'UpsertTagResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 8, '10': 'created'},
    {
      '1': 'tag',
      '3': 2,
      '4': 1,
      '5': 11,
      '6': '.omni_mix_player.Tag',
      '10': 'tag'
    },
  ],
};

/// Descriptor for `UpsertTagResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTagResponseDescriptor = $convert.base64Decode(
    'ChFVcHNlcnRUYWdSZXNwb25zZRIYCgdjcmVhdGVkGAEgASgIUgdjcmVhdGVkEiYKA3RhZxgCIA'
    'EoCzIULm9tbmlfbWl4X3BsYXllci5UYWdSA3RhZw==');

@$core.Deprecated('Use upsertTagsRequestDescriptor instead')
const UpsertTagsRequest$json = {
  '1': 'UpsertTagsRequest',
  '2': [
    {
      '1': 'tags',
      '3': 1,
      '4': 3,
      '5': 11,
      '6': '.omni_mix_player.Tag',
      '10': 'tags'
    },
  ],
};

/// Descriptor for `UpsertTagsRequest`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTagsRequestDescriptor = $convert.base64Decode(
    'ChFVcHNlcnRUYWdzUmVxdWVzdBIoCgR0YWdzGAEgAygLMhQub21uaV9taXhfcGxheWVyLlRhZ1'
    'IEdGFncw==');

@$core.Deprecated('Use upsertTagsResponseDescriptor instead')
const UpsertTagsResponse$json = {
  '1': 'UpsertTagsResponse',
  '2': [
    {'1': 'created', '3': 1, '4': 1, '5': 5, '10': 'created'},
    {'1': 'updated', '3': 2, '4': 1, '5': 5, '10': 'updated'},
  ],
};

/// Descriptor for `UpsertTagsResponse`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List upsertTagsResponseDescriptor = $convert.base64Decode(
    'ChJVcHNlcnRUYWdzUmVzcG9uc2USGAoHY3JlYXRlZBgBIAEoBVIHY3JlYXRlZBIYCgd1cGRhdG'
    'VkGAIgASgFUgd1cGRhdGVk');
