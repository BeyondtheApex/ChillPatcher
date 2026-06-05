// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/common.proto.

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

@$core.Deprecated('Use sourceTypeDescriptor instead')
const SourceType$json = {
  '1': 'SourceType',
  '2': [
    {'1': 'SOURCE_TYPE_UNSPECIFIED', '2': 0},
    {'1': 'SOURCE_TYPE_FILE', '2': 1},
    {'1': 'SOURCE_TYPE_URL', '2': 2},
    {'1': 'SOURCE_TYPE_STREAM', '2': 3},
  ],
};

/// Descriptor for `SourceType`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List sourceTypeDescriptor = $convert.base64Decode(
    'CgpTb3VyY2VUeXBlEhsKF1NPVVJDRV9UWVBFX1VOU1BFQ0lGSUVEEAASFAoQU09VUkNFX1RZUE'
    'VfRklMRRABEhMKD1NPVVJDRV9UWVBFX1VSTBACEhYKElNPVVJDRV9UWVBFX1NUUkVBTRAD');

@$core.Deprecated('Use sortDirectionDescriptor instead')
const SortDirection$json = {
  '1': 'SortDirection',
  '2': [
    {'1': 'SORT_DIRECTION_UNSPECIFIED', '2': 0},
    {'1': 'SORT_DIRECTION_ASC', '2': 1},
    {'1': 'SORT_DIRECTION_DESC', '2': 2},
  ],
};

/// Descriptor for `SortDirection`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List sortDirectionDescriptor = $convert.base64Decode(
    'Cg1Tb3J0RGlyZWN0aW9uEh4KGlNPUlRfRElSRUNUSU9OX1VOU1BFQ0lGSUVEEAASFgoSU09SVF'
    '9ESVJFQ1RJT05fQVNDEAESFwoTU09SVF9ESVJFQ1RJT05fREVTQxAC');

@$core.Deprecated('Use trackSortFieldDescriptor instead')
const TrackSortField$json = {
  '1': 'TrackSortField',
  '2': [
    {'1': 'TRACK_SORT_FIELD_UNSPECIFIED', '2': 0},
    {'1': 'TRACK_SORT_FIELD_TITLE', '2': 1},
    {'1': 'TRACK_SORT_FIELD_ARTIST', '2': 2},
    {'1': 'TRACK_SORT_FIELD_DURATION', '2': 3},
    {'1': 'TRACK_SORT_FIELD_PLAY_COUNT', '2': 4},
    {'1': 'TRACK_SORT_FIELD_LAST_PLAYED', '2': 5},
    {'1': 'TRACK_SORT_FIELD_CREATED_AT', '2': 6},
  ],
};

/// Descriptor for `TrackSortField`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List trackSortFieldDescriptor = $convert.base64Decode(
    'Cg5UcmFja1NvcnRGaWVsZBIgChxUUkFDS19TT1JUX0ZJRUxEX1VOU1BFQ0lGSUVEEAASGgoWVF'
    'JBQ0tfU09SVF9GSUVMRF9USVRMRRABEhsKF1RSQUNLX1NPUlRfRklFTERfQVJUSVNUEAISHQoZ'
    'VFJBQ0tfU09SVF9GSUVMRF9EVVJBVElPThADEh8KG1RSQUNLX1NPUlRfRklFTERfUExBWV9DT1'
    'VOVBAEEiAKHFRSQUNLX1NPUlRfRklFTERfTEFTVF9QTEFZRUQQBRIfChtUUkFDS19TT1JUX0ZJ'
    'RUxEX0NSRUFURURfQVQQBg==');

@$core.Deprecated('Use repeatModeDescriptor instead')
const RepeatMode$json = {
  '1': 'RepeatMode',
  '2': [
    {'1': 'REPEAT_MODE_UNSPECIFIED', '2': 0},
    {'1': 'REPEAT_MODE_NONE', '2': 1},
    {'1': 'REPEAT_MODE_ONE', '2': 2},
    {'1': 'REPEAT_MODE_ALL', '2': 3},
  ],
};

/// Descriptor for `RepeatMode`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List repeatModeDescriptor = $convert.base64Decode(
    'CgpSZXBlYXRNb2RlEhsKF1JFUEVBVF9NT0RFX1VOU1BFQ0lGSUVEEAASFAoQUkVQRUFUX01PRE'
    'VfTk9ORRABEhMKD1JFUEVBVF9NT0RFX09ORRACEhMKD1JFUEVBVF9NT0RFX0FMTBAD');

@$core.Deprecated('Use equalizerFilterTypeDescriptor instead')
const EqualizerFilterType$json = {
  '1': 'EqualizerFilterType',
  '2': [
    {'1': 'EQ_FILTER_TYPE_UNSPECIFIED', '2': 0},
    {'1': 'EQ_FILTER_TYPE_PEAKING', '2': 1},
    {'1': 'EQ_FILTER_TYPE_LOW_SHELF', '2': 2},
    {'1': 'EQ_FILTER_TYPE_HIGH_SHELF', '2': 3},
    {'1': 'EQ_FILTER_TYPE_LOW_PASS', '2': 4},
    {'1': 'EQ_FILTER_TYPE_HIGH_PASS', '2': 5},
  ],
};

/// Descriptor for `EqualizerFilterType`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List equalizerFilterTypeDescriptor = $convert.base64Decode(
    'ChNFcXVhbGl6ZXJGaWx0ZXJUeXBlEh4KGkVRX0ZJTFRFUl9UWVBFX1VOU1BFQ0lGSUVEEAASGg'
    'oWRVFfRklMVEVSX1RZUEVfUEVBS0lORxABEhwKGEVRX0ZJTFRFUl9UWVBFX0xPV19TSEVMRhAC'
    'Eh0KGUVRX0ZJTFRFUl9UWVBFX0hJR0hfU0hFTEYQAxIbChdFUV9GSUxURVJfVFlQRV9MT1dfUE'
    'FTUxAEEhwKGEVRX0ZJTFRFUl9UWVBFX0hJR0hfUEFTUxAF');

@$core.Deprecated('Use instanceKindDescriptor instead')
const InstanceKind$json = {
  '1': 'InstanceKind',
  '2': [
    {'1': 'INSTANCE_KIND_UNSPECIFIED', '2': 0},
    {'1': 'INSTANCE_KIND_GAME_MOD', '2': 1},
    {'1': 'INSTANCE_KIND_GUI', '2': 2},
    {'1': 'INSTANCE_KIND_EXTERNAL_CLIENT', '2': 3},
    {'1': 'INSTANCE_KIND_OBSERVER', '2': 4},
  ],
};

/// Descriptor for `InstanceKind`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List instanceKindDescriptor = $convert.base64Decode(
    'CgxJbnN0YW5jZUtpbmQSHQoZSU5TVEFOQ0VfS0lORF9VTlNQRUNJRklFRBAAEhoKFklOU1RBTk'
    'NFX0tJTkRfR0FNRV9NT0QQARIVChFJTlNUQU5DRV9LSU5EX0dVSRACEiEKHUlOU1RBTkNFX0tJ'
    'TkRfRVhURVJOQUxfQ0xJRU5UEAMSGgoWSU5TVEFOQ0VfS0lORF9PQlNFUlZFUhAE');

@$core.Deprecated('Use clientRoleDescriptor instead')
const ClientRole$json = {
  '1': 'ClientRole',
  '2': [
    {'1': 'CLIENT_ROLE_UNSPECIFIED', '2': 0},
    {'1': 'CLIENT_ROLE_AUDIO', '2': 1},
    {'1': 'CLIENT_ROLE_CONTROLLER', '2': 2},
    {'1': 'CLIENT_ROLE_OBSERVER', '2': 3},
  ],
};

/// Descriptor for `ClientRole`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List clientRoleDescriptor = $convert.base64Decode(
    'CgpDbGllbnRSb2xlEhsKF0NMSUVOVF9ST0xFX1VOU1BFQ0lGSUVEEAASFQoRQ0xJRU5UX1JPTE'
    'VfQVVESU8QARIaChZDTElFTlRfUk9MRV9DT05UUk9MTEVSEAISGAoUQ0xJRU5UX1JPTEVfT0JT'
    'RVJWRVIQAw==');

@$core.Deprecated('Use tagKindDescriptor instead')
const TagKind$json = {
  '1': 'TagKind',
  '2': [
    {'1': 'TAG_KIND_UNSPECIFIED', '2': 0},
    {'1': 'TAG_KIND_NORMAL', '2': 1},
    {'1': 'TAG_KIND_GROWABLE', '2': 2},
    {'1': 'TAG_KIND_SYSTEM', '2': 3},
  ],
};

/// Descriptor for `TagKind`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List tagKindDescriptor = $convert.base64Decode(
    'CgdUYWdLaW5kEhgKFFRBR19LSU5EX1VOU1BFQ0lGSUVEEAASEwoPVEFHX0tJTkRfTk9STUFMEA'
    'ESFQoRVEFHX0tJTkRfR1JPV0FCTEUQAhITCg9UQUdfS0lORF9TWVNURU0QAw==');

@$core.Deprecated('Use playlistKindDescriptor instead')
const PlaylistKind$json = {
  '1': 'PlaylistKind',
  '2': [
    {'1': 'PLAYLIST_KIND_UNSPECIFIED', '2': 0},
    {'1': 'PLAYLIST_KIND_USER', '2': 1},
    {'1': 'PLAYLIST_KIND_SYSTEM', '2': 2},
    {'1': 'PLAYLIST_KIND_IMPORTED', '2': 3},
  ],
};

/// Descriptor for `PlaylistKind`. Decode as a `google.protobuf.EnumDescriptorProto`.
final $typed_data.Uint8List playlistKindDescriptor = $convert.base64Decode(
    'CgxQbGF5bGlzdEtpbmQSHQoZUExBWUxJU1RfS0lORF9VTlNQRUNJRklFRBAAEhYKElBMQVlMSV'
    'NUX0tJTkRfVVNFUhABEhgKFFBMQVlMSVNUX0tJTkRfU1lTVEVNEAISGgoWUExBWUxJU1RfS0lO'
    'RF9JTVBPUlRFRBAD');

@$core.Deprecated('Use omniTimestampDescriptor instead')
const OmniTimestamp$json = {
  '1': 'OmniTimestamp',
  '2': [
    {'1': 'seconds', '3': 1, '4': 1, '5': 3, '10': 'seconds'},
    {'1': 'nanos', '3': 2, '4': 1, '5': 5, '10': 'nanos'},
  ],
};

/// Descriptor for `OmniTimestamp`. Decode as a `google.protobuf.DescriptorProto`.
final $typed_data.Uint8List omniTimestampDescriptor = $convert.base64Decode(
    'Cg1PbW5pVGltZXN0YW1wEhgKB3NlY29uZHMYASABKANSB3NlY29uZHMSFAoFbmFub3MYAiABKA'
    'VSBW5hbm9z');
