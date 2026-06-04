// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/library.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:async' as $async;
import 'dart:core' as $core;

import 'package:grpc/service_api.dart' as $grpc;
import 'package:protobuf/protobuf.dart' as $pb;

import '../models/album.pb.dart' as $3;
import '../models/playlist.pb.dart' as $5;
import '../models/query.pb.dart' as $2;
import '../models/tag.pb.dart' as $4;
import '../models/track.pb.dart' as $0;
import 'library.pb.dart' as $1;

export 'library.pb.dart';

/// 音乐库服务 — 平台级别 upsert + 查询
@$pb.GrpcServiceName('omni_mix_player.LibraryService')
class LibraryServiceClient extends $grpc.Client {
  /// The hostname for this service.
  static const $core.String defaultHost = '';

  /// OAuth scopes needed for the client.
  static const $core.List<$core.String> oauthScopes = [
    '',
  ];

  LibraryServiceClient(super.channel, {super.options, super.interceptors});

  /// ── Track ──
  $grpc.ResponseFuture<$0.UpsertTrackResponse> upsertTrack(
    $0.UpsertTrackRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertTrack, request, options: options);
  }

  $grpc.ResponseFuture<$0.UpsertTracksResponse> upsertTracks(
    $0.UpsertTracksRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertTracks, request, options: options);
  }

  $grpc.ResponseFuture<$0.Track> getTrack(
    $1.GetTrackRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getTrack, request, options: options);
  }

  $grpc.ResponseFuture<$2.QueryTracksResponse> queryTracks(
    $2.TrackQuery request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$queryTracks, request, options: options);
  }

  $grpc.ResponseFuture<$1.DeleteTrackResponse> deleteTrack(
    $1.DeleteTrackRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$deleteTrack, request, options: options);
  }

  /// ── Track Tags (多对多) ──
  $grpc.ResponseFuture<$0.SetTrackTagsResponse> setTrackTags(
    $0.SetTrackTagsRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setTrackTags, request, options: options);
  }

  $grpc.ResponseFuture<$0.ModifyTrackTagResponse> addTrackTag(
    $0.ModifyTrackTagRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$addTrackTag, request, options: options);
  }

  $grpc.ResponseFuture<$0.ModifyTrackTagResponse> removeTrackTag(
    $0.ModifyTrackTagRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$removeTrackTag, request, options: options);
  }

  $grpc.ResponseFuture<$1.GetTrackTagsResponse> getTrackTags(
    $1.GetTrackTagsRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getTrackTags, request, options: options);
  }

  /// ── Album ──
  $grpc.ResponseFuture<$3.UpsertAlbumResponse> upsertAlbum(
    $3.UpsertAlbumRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertAlbum, request, options: options);
  }

  $grpc.ResponseFuture<$3.UpsertAlbumsResponse> upsertAlbums(
    $3.UpsertAlbumsRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertAlbums, request, options: options);
  }

  $grpc.ResponseFuture<$3.Album> getAlbum(
    $1.GetAlbumRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getAlbum, request, options: options);
  }

  $grpc.ResponseFuture<$2.QueryAlbumsResponse> queryAlbums(
    $2.AlbumQuery request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$queryAlbums, request, options: options);
  }

  $grpc.ResponseFuture<$1.DeleteAlbumResponse> deleteAlbum(
    $1.DeleteAlbumRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$deleteAlbum, request, options: options);
  }

  /// ── Tag ──
  $grpc.ResponseFuture<$4.UpsertTagResponse> upsertTag(
    $4.UpsertTagRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertTag, request, options: options);
  }

  $grpc.ResponseFuture<$4.UpsertTagsResponse> upsertTags(
    $4.UpsertTagsRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertTags, request, options: options);
  }

  $grpc.ResponseFuture<$4.Tag> getTag(
    $1.GetTagRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getTag, request, options: options);
  }

  $grpc.ResponseFuture<$2.QueryTagsResponse> queryTags(
    $2.TagQuery request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$queryTags, request, options: options);
  }

  $grpc.ResponseFuture<$1.DeleteTagResponse> deleteTag(
    $1.DeleteTagRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$deleteTag, request, options: options);
  }

  /// ── Playlist ──
  $grpc.ResponseFuture<$5.UpsertPlaylistResponse> upsertPlaylist(
    $5.UpsertPlaylistRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$upsertPlaylist, request, options: options);
  }

  $grpc.ResponseFuture<$5.Playlist> getPlaylist(
    $1.GetPlaylistRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getPlaylist, request, options: options);
  }

  $grpc.ResponseFuture<$2.QueryPlaylistsResponse> queryPlaylists(
    $2.PlaylistQuery request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$queryPlaylists, request, options: options);
  }

  $grpc.ResponseFuture<$1.DeletePlaylistResponse> deletePlaylist(
    $1.DeletePlaylistRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$deletePlaylist, request, options: options);
  }

  /// ── Playlist Entries ──
  $grpc.ResponseFuture<$5.ReplacePlaylistEntriesResponse>
      replacePlaylistEntries(
    $5.ReplacePlaylistEntriesRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$replacePlaylistEntries, request,
        options: options);
  }

  $grpc.ResponseFuture<$5.InsertPlaylistEntryResponse> insertPlaylistEntry(
    $5.InsertPlaylistEntryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$insertPlaylistEntry, request, options: options);
  }

  $grpc.ResponseFuture<$5.RemovePlaylistEntryResponse> removePlaylistEntry(
    $5.RemovePlaylistEntryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$removePlaylistEntry, request, options: options);
  }

  $grpc.ResponseFuture<$5.MovePlaylistEntryResponse> movePlaylistEntry(
    $5.MovePlaylistEntryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$movePlaylistEntry, request, options: options);
  }

  $grpc.ResponseFuture<$5.PlaylistWithEntries> getPlaylistWithEntries(
    $1.GetPlaylistWithEntriesRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getPlaylistWithEntries, request,
        options: options);
  }

  /// ── Module cleanup ──
  $grpc.ResponseFuture<$1.UnregisterModuleResponse> unregisterModule(
    $1.UnregisterModuleRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$unregisterModule, request, options: options);
  }

  // method descriptors

  static final _$upsertTrack =
      $grpc.ClientMethod<$0.UpsertTrackRequest, $0.UpsertTrackResponse>(
          '/omni_mix_player.LibraryService/UpsertTrack',
          ($0.UpsertTrackRequest value) => value.writeToBuffer(),
          $0.UpsertTrackResponse.fromBuffer);
  static final _$upsertTracks =
      $grpc.ClientMethod<$0.UpsertTracksRequest, $0.UpsertTracksResponse>(
          '/omni_mix_player.LibraryService/UpsertTracks',
          ($0.UpsertTracksRequest value) => value.writeToBuffer(),
          $0.UpsertTracksResponse.fromBuffer);
  static final _$getTrack = $grpc.ClientMethod<$1.GetTrackRequest, $0.Track>(
      '/omni_mix_player.LibraryService/GetTrack',
      ($1.GetTrackRequest value) => value.writeToBuffer(),
      $0.Track.fromBuffer);
  static final _$queryTracks =
      $grpc.ClientMethod<$2.TrackQuery, $2.QueryTracksResponse>(
          '/omni_mix_player.LibraryService/QueryTracks',
          ($2.TrackQuery value) => value.writeToBuffer(),
          $2.QueryTracksResponse.fromBuffer);
  static final _$deleteTrack =
      $grpc.ClientMethod<$1.DeleteTrackRequest, $1.DeleteTrackResponse>(
          '/omni_mix_player.LibraryService/DeleteTrack',
          ($1.DeleteTrackRequest value) => value.writeToBuffer(),
          $1.DeleteTrackResponse.fromBuffer);
  static final _$setTrackTags =
      $grpc.ClientMethod<$0.SetTrackTagsRequest, $0.SetTrackTagsResponse>(
          '/omni_mix_player.LibraryService/SetTrackTags',
          ($0.SetTrackTagsRequest value) => value.writeToBuffer(),
          $0.SetTrackTagsResponse.fromBuffer);
  static final _$addTrackTag =
      $grpc.ClientMethod<$0.ModifyTrackTagRequest, $0.ModifyTrackTagResponse>(
          '/omni_mix_player.LibraryService/AddTrackTag',
          ($0.ModifyTrackTagRequest value) => value.writeToBuffer(),
          $0.ModifyTrackTagResponse.fromBuffer);
  static final _$removeTrackTag =
      $grpc.ClientMethod<$0.ModifyTrackTagRequest, $0.ModifyTrackTagResponse>(
          '/omni_mix_player.LibraryService/RemoveTrackTag',
          ($0.ModifyTrackTagRequest value) => value.writeToBuffer(),
          $0.ModifyTrackTagResponse.fromBuffer);
  static final _$getTrackTags =
      $grpc.ClientMethod<$1.GetTrackTagsRequest, $1.GetTrackTagsResponse>(
          '/omni_mix_player.LibraryService/GetTrackTags',
          ($1.GetTrackTagsRequest value) => value.writeToBuffer(),
          $1.GetTrackTagsResponse.fromBuffer);
  static final _$upsertAlbum =
      $grpc.ClientMethod<$3.UpsertAlbumRequest, $3.UpsertAlbumResponse>(
          '/omni_mix_player.LibraryService/UpsertAlbum',
          ($3.UpsertAlbumRequest value) => value.writeToBuffer(),
          $3.UpsertAlbumResponse.fromBuffer);
  static final _$upsertAlbums =
      $grpc.ClientMethod<$3.UpsertAlbumsRequest, $3.UpsertAlbumsResponse>(
          '/omni_mix_player.LibraryService/UpsertAlbums',
          ($3.UpsertAlbumsRequest value) => value.writeToBuffer(),
          $3.UpsertAlbumsResponse.fromBuffer);
  static final _$getAlbum = $grpc.ClientMethod<$1.GetAlbumRequest, $3.Album>(
      '/omni_mix_player.LibraryService/GetAlbum',
      ($1.GetAlbumRequest value) => value.writeToBuffer(),
      $3.Album.fromBuffer);
  static final _$queryAlbums =
      $grpc.ClientMethod<$2.AlbumQuery, $2.QueryAlbumsResponse>(
          '/omni_mix_player.LibraryService/QueryAlbums',
          ($2.AlbumQuery value) => value.writeToBuffer(),
          $2.QueryAlbumsResponse.fromBuffer);
  static final _$deleteAlbum =
      $grpc.ClientMethod<$1.DeleteAlbumRequest, $1.DeleteAlbumResponse>(
          '/omni_mix_player.LibraryService/DeleteAlbum',
          ($1.DeleteAlbumRequest value) => value.writeToBuffer(),
          $1.DeleteAlbumResponse.fromBuffer);
  static final _$upsertTag =
      $grpc.ClientMethod<$4.UpsertTagRequest, $4.UpsertTagResponse>(
          '/omni_mix_player.LibraryService/UpsertTag',
          ($4.UpsertTagRequest value) => value.writeToBuffer(),
          $4.UpsertTagResponse.fromBuffer);
  static final _$upsertTags =
      $grpc.ClientMethod<$4.UpsertTagsRequest, $4.UpsertTagsResponse>(
          '/omni_mix_player.LibraryService/UpsertTags',
          ($4.UpsertTagsRequest value) => value.writeToBuffer(),
          $4.UpsertTagsResponse.fromBuffer);
  static final _$getTag = $grpc.ClientMethod<$1.GetTagRequest, $4.Tag>(
      '/omni_mix_player.LibraryService/GetTag',
      ($1.GetTagRequest value) => value.writeToBuffer(),
      $4.Tag.fromBuffer);
  static final _$queryTags =
      $grpc.ClientMethod<$2.TagQuery, $2.QueryTagsResponse>(
          '/omni_mix_player.LibraryService/QueryTags',
          ($2.TagQuery value) => value.writeToBuffer(),
          $2.QueryTagsResponse.fromBuffer);
  static final _$deleteTag =
      $grpc.ClientMethod<$1.DeleteTagRequest, $1.DeleteTagResponse>(
          '/omni_mix_player.LibraryService/DeleteTag',
          ($1.DeleteTagRequest value) => value.writeToBuffer(),
          $1.DeleteTagResponse.fromBuffer);
  static final _$upsertPlaylist =
      $grpc.ClientMethod<$5.UpsertPlaylistRequest, $5.UpsertPlaylistResponse>(
          '/omni_mix_player.LibraryService/UpsertPlaylist',
          ($5.UpsertPlaylistRequest value) => value.writeToBuffer(),
          $5.UpsertPlaylistResponse.fromBuffer);
  static final _$getPlaylist =
      $grpc.ClientMethod<$1.GetPlaylistRequest, $5.Playlist>(
          '/omni_mix_player.LibraryService/GetPlaylist',
          ($1.GetPlaylistRequest value) => value.writeToBuffer(),
          $5.Playlist.fromBuffer);
  static final _$queryPlaylists =
      $grpc.ClientMethod<$2.PlaylistQuery, $2.QueryPlaylistsResponse>(
          '/omni_mix_player.LibraryService/QueryPlaylists',
          ($2.PlaylistQuery value) => value.writeToBuffer(),
          $2.QueryPlaylistsResponse.fromBuffer);
  static final _$deletePlaylist =
      $grpc.ClientMethod<$1.DeletePlaylistRequest, $1.DeletePlaylistResponse>(
          '/omni_mix_player.LibraryService/DeletePlaylist',
          ($1.DeletePlaylistRequest value) => value.writeToBuffer(),
          $1.DeletePlaylistResponse.fromBuffer);
  static final _$replacePlaylistEntries = $grpc.ClientMethod<
          $5.ReplacePlaylistEntriesRequest, $5.ReplacePlaylistEntriesResponse>(
      '/omni_mix_player.LibraryService/ReplacePlaylistEntries',
      ($5.ReplacePlaylistEntriesRequest value) => value.writeToBuffer(),
      $5.ReplacePlaylistEntriesResponse.fromBuffer);
  static final _$insertPlaylistEntry = $grpc.ClientMethod<
          $5.InsertPlaylistEntryRequest, $5.InsertPlaylistEntryResponse>(
      '/omni_mix_player.LibraryService/InsertPlaylistEntry',
      ($5.InsertPlaylistEntryRequest value) => value.writeToBuffer(),
      $5.InsertPlaylistEntryResponse.fromBuffer);
  static final _$removePlaylistEntry = $grpc.ClientMethod<
          $5.RemovePlaylistEntryRequest, $5.RemovePlaylistEntryResponse>(
      '/omni_mix_player.LibraryService/RemovePlaylistEntry',
      ($5.RemovePlaylistEntryRequest value) => value.writeToBuffer(),
      $5.RemovePlaylistEntryResponse.fromBuffer);
  static final _$movePlaylistEntry = $grpc.ClientMethod<
          $5.MovePlaylistEntryRequest, $5.MovePlaylistEntryResponse>(
      '/omni_mix_player.LibraryService/MovePlaylistEntry',
      ($5.MovePlaylistEntryRequest value) => value.writeToBuffer(),
      $5.MovePlaylistEntryResponse.fromBuffer);
  static final _$getPlaylistWithEntries = $grpc.ClientMethod<
          $1.GetPlaylistWithEntriesRequest, $5.PlaylistWithEntries>(
      '/omni_mix_player.LibraryService/GetPlaylistWithEntries',
      ($1.GetPlaylistWithEntriesRequest value) => value.writeToBuffer(),
      $5.PlaylistWithEntries.fromBuffer);
  static final _$unregisterModule = $grpc.ClientMethod<
          $1.UnregisterModuleRequest, $1.UnregisterModuleResponse>(
      '/omni_mix_player.LibraryService/UnregisterModule',
      ($1.UnregisterModuleRequest value) => value.writeToBuffer(),
      $1.UnregisterModuleResponse.fromBuffer);
}

@$pb.GrpcServiceName('omni_mix_player.LibraryService')
abstract class LibraryServiceBase extends $grpc.Service {
  $core.String get $name => 'omni_mix_player.LibraryService';

  LibraryServiceBase() {
    $addMethod(
        $grpc.ServiceMethod<$0.UpsertTrackRequest, $0.UpsertTrackResponse>(
            'UpsertTrack',
            upsertTrack_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.UpsertTrackRequest.fromBuffer(value),
            ($0.UpsertTrackResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.UpsertTracksRequest, $0.UpsertTracksResponse>(
            'UpsertTracks',
            upsertTracks_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.UpsertTracksRequest.fromBuffer(value),
            ($0.UpsertTracksResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetTrackRequest, $0.Track>(
        'GetTrack',
        getTrack_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $1.GetTrackRequest.fromBuffer(value),
        ($0.Track value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$2.TrackQuery, $2.QueryTracksResponse>(
        'QueryTracks',
        queryTracks_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $2.TrackQuery.fromBuffer(value),
        ($2.QueryTracksResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.DeleteTrackRequest, $1.DeleteTrackResponse>(
            'DeleteTrack',
            deleteTrack_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.DeleteTrackRequest.fromBuffer(value),
            ($1.DeleteTrackResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.SetTrackTagsRequest, $0.SetTrackTagsResponse>(
            'SetTrackTags',
            setTrackTags_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.SetTrackTagsRequest.fromBuffer(value),
            ($0.SetTrackTagsResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.ModifyTrackTagRequest,
            $0.ModifyTrackTagResponse>(
        'AddTrackTag',
        addTrackTag_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.ModifyTrackTagRequest.fromBuffer(value),
        ($0.ModifyTrackTagResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.ModifyTrackTagRequest,
            $0.ModifyTrackTagResponse>(
        'RemoveTrackTag',
        removeTrackTag_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.ModifyTrackTagRequest.fromBuffer(value),
        ($0.ModifyTrackTagResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.GetTrackTagsRequest, $1.GetTrackTagsResponse>(
            'GetTrackTags',
            getTrackTags_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.GetTrackTagsRequest.fromBuffer(value),
            ($1.GetTrackTagsResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$3.UpsertAlbumRequest, $3.UpsertAlbumResponse>(
            'UpsertAlbum',
            upsertAlbum_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $3.UpsertAlbumRequest.fromBuffer(value),
            ($3.UpsertAlbumResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$3.UpsertAlbumsRequest, $3.UpsertAlbumsResponse>(
            'UpsertAlbums',
            upsertAlbums_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $3.UpsertAlbumsRequest.fromBuffer(value),
            ($3.UpsertAlbumsResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetAlbumRequest, $3.Album>(
        'GetAlbum',
        getAlbum_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $1.GetAlbumRequest.fromBuffer(value),
        ($3.Album value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$2.AlbumQuery, $2.QueryAlbumsResponse>(
        'QueryAlbums',
        queryAlbums_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $2.AlbumQuery.fromBuffer(value),
        ($2.QueryAlbumsResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.DeleteAlbumRequest, $1.DeleteAlbumResponse>(
            'DeleteAlbum',
            deleteAlbum_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.DeleteAlbumRequest.fromBuffer(value),
            ($1.DeleteAlbumResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$4.UpsertTagRequest, $4.UpsertTagResponse>(
        'UpsertTag',
        upsertTag_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $4.UpsertTagRequest.fromBuffer(value),
        ($4.UpsertTagResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$4.UpsertTagsRequest, $4.UpsertTagsResponse>(
        'UpsertTags',
        upsertTags_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $4.UpsertTagsRequest.fromBuffer(value),
        ($4.UpsertTagsResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetTagRequest, $4.Tag>(
        'GetTag',
        getTag_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $1.GetTagRequest.fromBuffer(value),
        ($4.Tag value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$2.TagQuery, $2.QueryTagsResponse>(
        'QueryTags',
        queryTags_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $2.TagQuery.fromBuffer(value),
        ($2.QueryTagsResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.DeleteTagRequest, $1.DeleteTagResponse>(
        'DeleteTag',
        deleteTag_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $1.DeleteTagRequest.fromBuffer(value),
        ($1.DeleteTagResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$5.UpsertPlaylistRequest,
            $5.UpsertPlaylistResponse>(
        'UpsertPlaylist',
        upsertPlaylist_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $5.UpsertPlaylistRequest.fromBuffer(value),
        ($5.UpsertPlaylistResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetPlaylistRequest, $5.Playlist>(
        'GetPlaylist',
        getPlaylist_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.GetPlaylistRequest.fromBuffer(value),
        ($5.Playlist value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$2.PlaylistQuery, $2.QueryPlaylistsResponse>(
        'QueryPlaylists',
        queryPlaylists_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $2.PlaylistQuery.fromBuffer(value),
        ($2.QueryPlaylistsResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.DeletePlaylistRequest,
            $1.DeletePlaylistResponse>(
        'DeletePlaylist',
        deletePlaylist_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.DeletePlaylistRequest.fromBuffer(value),
        ($1.DeletePlaylistResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$5.ReplacePlaylistEntriesRequest,
            $5.ReplacePlaylistEntriesResponse>(
        'ReplacePlaylistEntries',
        replacePlaylistEntries_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $5.ReplacePlaylistEntriesRequest.fromBuffer(value),
        ($5.ReplacePlaylistEntriesResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$5.InsertPlaylistEntryRequest,
            $5.InsertPlaylistEntryResponse>(
        'InsertPlaylistEntry',
        insertPlaylistEntry_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $5.InsertPlaylistEntryRequest.fromBuffer(value),
        ($5.InsertPlaylistEntryResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$5.RemovePlaylistEntryRequest,
            $5.RemovePlaylistEntryResponse>(
        'RemovePlaylistEntry',
        removePlaylistEntry_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $5.RemovePlaylistEntryRequest.fromBuffer(value),
        ($5.RemovePlaylistEntryResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$5.MovePlaylistEntryRequest,
            $5.MovePlaylistEntryResponse>(
        'MovePlaylistEntry',
        movePlaylistEntry_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $5.MovePlaylistEntryRequest.fromBuffer(value),
        ($5.MovePlaylistEntryResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetPlaylistWithEntriesRequest,
            $5.PlaylistWithEntries>(
        'GetPlaylistWithEntries',
        getPlaylistWithEntries_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.GetPlaylistWithEntriesRequest.fromBuffer(value),
        ($5.PlaylistWithEntries value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.UnregisterModuleRequest,
            $1.UnregisterModuleResponse>(
        'UnregisterModule',
        unregisterModule_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.UnregisterModuleRequest.fromBuffer(value),
        ($1.UnregisterModuleResponse value) => value.writeToBuffer()));
  }

  $async.Future<$0.UpsertTrackResponse> upsertTrack_Pre($grpc.ServiceCall $call,
      $async.Future<$0.UpsertTrackRequest> $request) async {
    return upsertTrack($call, await $request);
  }

  $async.Future<$0.UpsertTrackResponse> upsertTrack(
      $grpc.ServiceCall call, $0.UpsertTrackRequest request);

  $async.Future<$0.UpsertTracksResponse> upsertTracks_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.UpsertTracksRequest> $request) async {
    return upsertTracks($call, await $request);
  }

  $async.Future<$0.UpsertTracksResponse> upsertTracks(
      $grpc.ServiceCall call, $0.UpsertTracksRequest request);

  $async.Future<$0.Track> getTrack_Pre($grpc.ServiceCall $call,
      $async.Future<$1.GetTrackRequest> $request) async {
    return getTrack($call, await $request);
  }

  $async.Future<$0.Track> getTrack(
      $grpc.ServiceCall call, $1.GetTrackRequest request);

  $async.Future<$2.QueryTracksResponse> queryTracks_Pre(
      $grpc.ServiceCall $call, $async.Future<$2.TrackQuery> $request) async {
    return queryTracks($call, await $request);
  }

  $async.Future<$2.QueryTracksResponse> queryTracks(
      $grpc.ServiceCall call, $2.TrackQuery request);

  $async.Future<$1.DeleteTrackResponse> deleteTrack_Pre($grpc.ServiceCall $call,
      $async.Future<$1.DeleteTrackRequest> $request) async {
    return deleteTrack($call, await $request);
  }

  $async.Future<$1.DeleteTrackResponse> deleteTrack(
      $grpc.ServiceCall call, $1.DeleteTrackRequest request);

  $async.Future<$0.SetTrackTagsResponse> setTrackTags_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.SetTrackTagsRequest> $request) async {
    return setTrackTags($call, await $request);
  }

  $async.Future<$0.SetTrackTagsResponse> setTrackTags(
      $grpc.ServiceCall call, $0.SetTrackTagsRequest request);

  $async.Future<$0.ModifyTrackTagResponse> addTrackTag_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.ModifyTrackTagRequest> $request) async {
    return addTrackTag($call, await $request);
  }

  $async.Future<$0.ModifyTrackTagResponse> addTrackTag(
      $grpc.ServiceCall call, $0.ModifyTrackTagRequest request);

  $async.Future<$0.ModifyTrackTagResponse> removeTrackTag_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.ModifyTrackTagRequest> $request) async {
    return removeTrackTag($call, await $request);
  }

  $async.Future<$0.ModifyTrackTagResponse> removeTrackTag(
      $grpc.ServiceCall call, $0.ModifyTrackTagRequest request);

  $async.Future<$1.GetTrackTagsResponse> getTrackTags_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.GetTrackTagsRequest> $request) async {
    return getTrackTags($call, await $request);
  }

  $async.Future<$1.GetTrackTagsResponse> getTrackTags(
      $grpc.ServiceCall call, $1.GetTrackTagsRequest request);

  $async.Future<$3.UpsertAlbumResponse> upsertAlbum_Pre($grpc.ServiceCall $call,
      $async.Future<$3.UpsertAlbumRequest> $request) async {
    return upsertAlbum($call, await $request);
  }

  $async.Future<$3.UpsertAlbumResponse> upsertAlbum(
      $grpc.ServiceCall call, $3.UpsertAlbumRequest request);

  $async.Future<$3.UpsertAlbumsResponse> upsertAlbums_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$3.UpsertAlbumsRequest> $request) async {
    return upsertAlbums($call, await $request);
  }

  $async.Future<$3.UpsertAlbumsResponse> upsertAlbums(
      $grpc.ServiceCall call, $3.UpsertAlbumsRequest request);

  $async.Future<$3.Album> getAlbum_Pre($grpc.ServiceCall $call,
      $async.Future<$1.GetAlbumRequest> $request) async {
    return getAlbum($call, await $request);
  }

  $async.Future<$3.Album> getAlbum(
      $grpc.ServiceCall call, $1.GetAlbumRequest request);

  $async.Future<$2.QueryAlbumsResponse> queryAlbums_Pre(
      $grpc.ServiceCall $call, $async.Future<$2.AlbumQuery> $request) async {
    return queryAlbums($call, await $request);
  }

  $async.Future<$2.QueryAlbumsResponse> queryAlbums(
      $grpc.ServiceCall call, $2.AlbumQuery request);

  $async.Future<$1.DeleteAlbumResponse> deleteAlbum_Pre($grpc.ServiceCall $call,
      $async.Future<$1.DeleteAlbumRequest> $request) async {
    return deleteAlbum($call, await $request);
  }

  $async.Future<$1.DeleteAlbumResponse> deleteAlbum(
      $grpc.ServiceCall call, $1.DeleteAlbumRequest request);

  $async.Future<$4.UpsertTagResponse> upsertTag_Pre($grpc.ServiceCall $call,
      $async.Future<$4.UpsertTagRequest> $request) async {
    return upsertTag($call, await $request);
  }

  $async.Future<$4.UpsertTagResponse> upsertTag(
      $grpc.ServiceCall call, $4.UpsertTagRequest request);

  $async.Future<$4.UpsertTagsResponse> upsertTags_Pre($grpc.ServiceCall $call,
      $async.Future<$4.UpsertTagsRequest> $request) async {
    return upsertTags($call, await $request);
  }

  $async.Future<$4.UpsertTagsResponse> upsertTags(
      $grpc.ServiceCall call, $4.UpsertTagsRequest request);

  $async.Future<$4.Tag> getTag_Pre(
      $grpc.ServiceCall $call, $async.Future<$1.GetTagRequest> $request) async {
    return getTag($call, await $request);
  }

  $async.Future<$4.Tag> getTag(
      $grpc.ServiceCall call, $1.GetTagRequest request);

  $async.Future<$2.QueryTagsResponse> queryTags_Pre(
      $grpc.ServiceCall $call, $async.Future<$2.TagQuery> $request) async {
    return queryTags($call, await $request);
  }

  $async.Future<$2.QueryTagsResponse> queryTags(
      $grpc.ServiceCall call, $2.TagQuery request);

  $async.Future<$1.DeleteTagResponse> deleteTag_Pre($grpc.ServiceCall $call,
      $async.Future<$1.DeleteTagRequest> $request) async {
    return deleteTag($call, await $request);
  }

  $async.Future<$1.DeleteTagResponse> deleteTag(
      $grpc.ServiceCall call, $1.DeleteTagRequest request);

  $async.Future<$5.UpsertPlaylistResponse> upsertPlaylist_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$5.UpsertPlaylistRequest> $request) async {
    return upsertPlaylist($call, await $request);
  }

  $async.Future<$5.UpsertPlaylistResponse> upsertPlaylist(
      $grpc.ServiceCall call, $5.UpsertPlaylistRequest request);

  $async.Future<$5.Playlist> getPlaylist_Pre($grpc.ServiceCall $call,
      $async.Future<$1.GetPlaylistRequest> $request) async {
    return getPlaylist($call, await $request);
  }

  $async.Future<$5.Playlist> getPlaylist(
      $grpc.ServiceCall call, $1.GetPlaylistRequest request);

  $async.Future<$2.QueryPlaylistsResponse> queryPlaylists_Pre(
      $grpc.ServiceCall $call, $async.Future<$2.PlaylistQuery> $request) async {
    return queryPlaylists($call, await $request);
  }

  $async.Future<$2.QueryPlaylistsResponse> queryPlaylists(
      $grpc.ServiceCall call, $2.PlaylistQuery request);

  $async.Future<$1.DeletePlaylistResponse> deletePlaylist_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.DeletePlaylistRequest> $request) async {
    return deletePlaylist($call, await $request);
  }

  $async.Future<$1.DeletePlaylistResponse> deletePlaylist(
      $grpc.ServiceCall call, $1.DeletePlaylistRequest request);

  $async.Future<$5.ReplacePlaylistEntriesResponse> replacePlaylistEntries_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$5.ReplacePlaylistEntriesRequest> $request) async {
    return replacePlaylistEntries($call, await $request);
  }

  $async.Future<$5.ReplacePlaylistEntriesResponse> replacePlaylistEntries(
      $grpc.ServiceCall call, $5.ReplacePlaylistEntriesRequest request);

  $async.Future<$5.InsertPlaylistEntryResponse> insertPlaylistEntry_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$5.InsertPlaylistEntryRequest> $request) async {
    return insertPlaylistEntry($call, await $request);
  }

  $async.Future<$5.InsertPlaylistEntryResponse> insertPlaylistEntry(
      $grpc.ServiceCall call, $5.InsertPlaylistEntryRequest request);

  $async.Future<$5.RemovePlaylistEntryResponse> removePlaylistEntry_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$5.RemovePlaylistEntryRequest> $request) async {
    return removePlaylistEntry($call, await $request);
  }

  $async.Future<$5.RemovePlaylistEntryResponse> removePlaylistEntry(
      $grpc.ServiceCall call, $5.RemovePlaylistEntryRequest request);

  $async.Future<$5.MovePlaylistEntryResponse> movePlaylistEntry_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$5.MovePlaylistEntryRequest> $request) async {
    return movePlaylistEntry($call, await $request);
  }

  $async.Future<$5.MovePlaylistEntryResponse> movePlaylistEntry(
      $grpc.ServiceCall call, $5.MovePlaylistEntryRequest request);

  $async.Future<$5.PlaylistWithEntries> getPlaylistWithEntries_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.GetPlaylistWithEntriesRequest> $request) async {
    return getPlaylistWithEntries($call, await $request);
  }

  $async.Future<$5.PlaylistWithEntries> getPlaylistWithEntries(
      $grpc.ServiceCall call, $1.GetPlaylistWithEntriesRequest request);

  $async.Future<$1.UnregisterModuleResponse> unregisterModule_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.UnregisterModuleRequest> $request) async {
    return unregisterModule($call, await $request);
  }

  $async.Future<$1.UnregisterModuleResponse> unregisterModule(
      $grpc.ServiceCall call, $1.UnregisterModuleRequest request);
}
