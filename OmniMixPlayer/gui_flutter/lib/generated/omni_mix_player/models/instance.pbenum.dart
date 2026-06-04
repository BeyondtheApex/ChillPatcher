// This is a generated file - do not edit.
//
// Generated from omni_mix_player/models/instance.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

class PlaylistSourceKind extends $pb.ProtobufEnum {
  static const PlaylistSourceKind PLAYLIST_SOURCE_KIND_UNSPECIFIED =
      PlaylistSourceKind._(
          0, _omitEnumNames ? '' : 'PLAYLIST_SOURCE_KIND_UNSPECIFIED');
  static const PlaylistSourceKind PLAYLIST_SOURCE_KIND_TAG =
      PlaylistSourceKind._(1, _omitEnumNames ? '' : 'PLAYLIST_SOURCE_KIND_TAG');
  static const PlaylistSourceKind PLAYLIST_SOURCE_KIND_ALBUM =
      PlaylistSourceKind._(
          2, _omitEnumNames ? '' : 'PLAYLIST_SOURCE_KIND_ALBUM');
  static const PlaylistSourceKind PLAYLIST_SOURCE_KIND_PLAYLIST =
      PlaylistSourceKind._(
          3, _omitEnumNames ? '' : 'PLAYLIST_SOURCE_KIND_PLAYLIST');
  static const PlaylistSourceKind PLAYLIST_SOURCE_KIND_TRACK =
      PlaylistSourceKind._(
          4, _omitEnumNames ? '' : 'PLAYLIST_SOURCE_KIND_TRACK');

  static const $core.List<PlaylistSourceKind> values = <PlaylistSourceKind>[
    PLAYLIST_SOURCE_KIND_UNSPECIFIED,
    PLAYLIST_SOURCE_KIND_TAG,
    PLAYLIST_SOURCE_KIND_ALBUM,
    PLAYLIST_SOURCE_KIND_PLAYLIST,
    PLAYLIST_SOURCE_KIND_TRACK,
  ];

  static final $core.List<PlaylistSourceKind?> _byValue =
      $pb.ProtobufEnum.$_initByValueList(values, 4);
  static PlaylistSourceKind? valueOf($core.int value) =>
      value < 0 || value >= _byValue.length ? null : _byValue[value];

  const PlaylistSourceKind._(super.value, super.name);
}

const $core.bool _omitEnumNames =
    $core.bool.fromEnvironment('protobuf.omit_enum_names');
