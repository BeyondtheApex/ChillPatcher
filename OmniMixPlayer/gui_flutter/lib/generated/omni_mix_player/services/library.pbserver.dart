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

import 'package:protobuf/protobuf.dart' as $pb;

import '../models/album.pb.dart' as $2;
import '../models/playlist.pb.dart' as $4;
import '../models/query.pb.dart' as $1;
import '../models/tag.pb.dart' as $3;
import '../models/track.pb.dart' as $0;
import 'library.pb.dart' as $6;
import 'library.pbjson.dart';

export 'library.pb.dart';

abstract class LibraryServiceBase extends $pb.GeneratedService {
  $async.Future<$0.UpsertTrackResponse> upsertTrack(
      $pb.ServerContext ctx, $0.UpsertTrackRequest request);
  $async.Future<$0.UpsertTracksResponse> upsertTracks(
      $pb.ServerContext ctx, $0.UpsertTracksRequest request);
  $async.Future<$0.Track> getTrack(
      $pb.ServerContext ctx, $6.GetTrackRequest request);
  $async.Future<$1.QueryTracksResponse> queryTracks(
      $pb.ServerContext ctx, $1.TrackQuery request);
  $async.Future<$6.DeleteTrackResponse> deleteTrack(
      $pb.ServerContext ctx, $6.DeleteTrackRequest request);
  $async.Future<$0.SetTrackTagsResponse> setTrackTags(
      $pb.ServerContext ctx, $0.SetTrackTagsRequest request);
  $async.Future<$0.ModifyTrackTagResponse> addTrackTag(
      $pb.ServerContext ctx, $0.ModifyTrackTagRequest request);
  $async.Future<$0.ModifyTrackTagResponse> removeTrackTag(
      $pb.ServerContext ctx, $0.ModifyTrackTagRequest request);
  $async.Future<$6.GetTrackTagsResponse> getTrackTags(
      $pb.ServerContext ctx, $6.GetTrackTagsRequest request);
  $async.Future<$2.UpsertAlbumResponse> upsertAlbum(
      $pb.ServerContext ctx, $2.UpsertAlbumRequest request);
  $async.Future<$2.UpsertAlbumsResponse> upsertAlbums(
      $pb.ServerContext ctx, $2.UpsertAlbumsRequest request);
  $async.Future<$2.Album> getAlbum(
      $pb.ServerContext ctx, $6.GetAlbumRequest request);
  $async.Future<$1.QueryAlbumsResponse> queryAlbums(
      $pb.ServerContext ctx, $1.AlbumQuery request);
  $async.Future<$6.DeleteAlbumResponse> deleteAlbum(
      $pb.ServerContext ctx, $6.DeleteAlbumRequest request);
  $async.Future<$3.UpsertTagResponse> upsertTag(
      $pb.ServerContext ctx, $3.UpsertTagRequest request);
  $async.Future<$3.UpsertTagsResponse> upsertTags(
      $pb.ServerContext ctx, $3.UpsertTagsRequest request);
  $async.Future<$3.Tag> getTag($pb.ServerContext ctx, $6.GetTagRequest request);
  $async.Future<$1.QueryTagsResponse> queryTags(
      $pb.ServerContext ctx, $1.TagQuery request);
  $async.Future<$6.DeleteTagResponse> deleteTag(
      $pb.ServerContext ctx, $6.DeleteTagRequest request);
  $async.Future<$4.UpsertPlaylistResponse> upsertPlaylist(
      $pb.ServerContext ctx, $4.UpsertPlaylistRequest request);
  $async.Future<$4.Playlist> getPlaylist(
      $pb.ServerContext ctx, $6.GetPlaylistRequest request);
  $async.Future<$1.QueryPlaylistsResponse> queryPlaylists(
      $pb.ServerContext ctx, $1.PlaylistQuery request);
  $async.Future<$6.DeletePlaylistResponse> deletePlaylist(
      $pb.ServerContext ctx, $6.DeletePlaylistRequest request);
  $async.Future<$4.ReplacePlaylistEntriesResponse> replacePlaylistEntries(
      $pb.ServerContext ctx, $4.ReplacePlaylistEntriesRequest request);
  $async.Future<$4.InsertPlaylistEntryResponse> insertPlaylistEntry(
      $pb.ServerContext ctx, $4.InsertPlaylistEntryRequest request);
  $async.Future<$4.RemovePlaylistEntryResponse> removePlaylistEntry(
      $pb.ServerContext ctx, $4.RemovePlaylistEntryRequest request);
  $async.Future<$4.MovePlaylistEntryResponse> movePlaylistEntry(
      $pb.ServerContext ctx, $4.MovePlaylistEntryRequest request);
  $async.Future<$4.PlaylistWithEntries> getPlaylistWithEntries(
      $pb.ServerContext ctx, $6.GetPlaylistWithEntriesRequest request);
  $async.Future<$6.UnregisterModuleResponse> unregisterModule(
      $pb.ServerContext ctx, $6.UnregisterModuleRequest request);

  $pb.GeneratedMessage createRequest($core.String methodName) {
    switch (methodName) {
      case 'UpsertTrack':
        return $0.UpsertTrackRequest();
      case 'UpsertTracks':
        return $0.UpsertTracksRequest();
      case 'GetTrack':
        return $6.GetTrackRequest();
      case 'QueryTracks':
        return $1.TrackQuery();
      case 'DeleteTrack':
        return $6.DeleteTrackRequest();
      case 'SetTrackTags':
        return $0.SetTrackTagsRequest();
      case 'AddTrackTag':
        return $0.ModifyTrackTagRequest();
      case 'RemoveTrackTag':
        return $0.ModifyTrackTagRequest();
      case 'GetTrackTags':
        return $6.GetTrackTagsRequest();
      case 'UpsertAlbum':
        return $2.UpsertAlbumRequest();
      case 'UpsertAlbums':
        return $2.UpsertAlbumsRequest();
      case 'GetAlbum':
        return $6.GetAlbumRequest();
      case 'QueryAlbums':
        return $1.AlbumQuery();
      case 'DeleteAlbum':
        return $6.DeleteAlbumRequest();
      case 'UpsertTag':
        return $3.UpsertTagRequest();
      case 'UpsertTags':
        return $3.UpsertTagsRequest();
      case 'GetTag':
        return $6.GetTagRequest();
      case 'QueryTags':
        return $1.TagQuery();
      case 'DeleteTag':
        return $6.DeleteTagRequest();
      case 'UpsertPlaylist':
        return $4.UpsertPlaylistRequest();
      case 'GetPlaylist':
        return $6.GetPlaylistRequest();
      case 'QueryPlaylists':
        return $1.PlaylistQuery();
      case 'DeletePlaylist':
        return $6.DeletePlaylistRequest();
      case 'ReplacePlaylistEntries':
        return $4.ReplacePlaylistEntriesRequest();
      case 'InsertPlaylistEntry':
        return $4.InsertPlaylistEntryRequest();
      case 'RemovePlaylistEntry':
        return $4.RemovePlaylistEntryRequest();
      case 'MovePlaylistEntry':
        return $4.MovePlaylistEntryRequest();
      case 'GetPlaylistWithEntries':
        return $6.GetPlaylistWithEntriesRequest();
      case 'UnregisterModule':
        return $6.UnregisterModuleRequest();
      default:
        throw $core.ArgumentError('Unknown method: $methodName');
    }
  }

  $async.Future<$pb.GeneratedMessage> handleCall($pb.ServerContext ctx,
      $core.String methodName, $pb.GeneratedMessage request) {
    switch (methodName) {
      case 'UpsertTrack':
        return upsertTrack(ctx, request as $0.UpsertTrackRequest);
      case 'UpsertTracks':
        return upsertTracks(ctx, request as $0.UpsertTracksRequest);
      case 'GetTrack':
        return getTrack(ctx, request as $6.GetTrackRequest);
      case 'QueryTracks':
        return queryTracks(ctx, request as $1.TrackQuery);
      case 'DeleteTrack':
        return deleteTrack(ctx, request as $6.DeleteTrackRequest);
      case 'SetTrackTags':
        return setTrackTags(ctx, request as $0.SetTrackTagsRequest);
      case 'AddTrackTag':
        return addTrackTag(ctx, request as $0.ModifyTrackTagRequest);
      case 'RemoveTrackTag':
        return removeTrackTag(ctx, request as $0.ModifyTrackTagRequest);
      case 'GetTrackTags':
        return getTrackTags(ctx, request as $6.GetTrackTagsRequest);
      case 'UpsertAlbum':
        return upsertAlbum(ctx, request as $2.UpsertAlbumRequest);
      case 'UpsertAlbums':
        return upsertAlbums(ctx, request as $2.UpsertAlbumsRequest);
      case 'GetAlbum':
        return getAlbum(ctx, request as $6.GetAlbumRequest);
      case 'QueryAlbums':
        return queryAlbums(ctx, request as $1.AlbumQuery);
      case 'DeleteAlbum':
        return deleteAlbum(ctx, request as $6.DeleteAlbumRequest);
      case 'UpsertTag':
        return upsertTag(ctx, request as $3.UpsertTagRequest);
      case 'UpsertTags':
        return upsertTags(ctx, request as $3.UpsertTagsRequest);
      case 'GetTag':
        return getTag(ctx, request as $6.GetTagRequest);
      case 'QueryTags':
        return queryTags(ctx, request as $1.TagQuery);
      case 'DeleteTag':
        return deleteTag(ctx, request as $6.DeleteTagRequest);
      case 'UpsertPlaylist':
        return upsertPlaylist(ctx, request as $4.UpsertPlaylistRequest);
      case 'GetPlaylist':
        return getPlaylist(ctx, request as $6.GetPlaylistRequest);
      case 'QueryPlaylists':
        return queryPlaylists(ctx, request as $1.PlaylistQuery);
      case 'DeletePlaylist':
        return deletePlaylist(ctx, request as $6.DeletePlaylistRequest);
      case 'ReplacePlaylistEntries':
        return replacePlaylistEntries(
            ctx, request as $4.ReplacePlaylistEntriesRequest);
      case 'InsertPlaylistEntry':
        return insertPlaylistEntry(
            ctx, request as $4.InsertPlaylistEntryRequest);
      case 'RemovePlaylistEntry':
        return removePlaylistEntry(
            ctx, request as $4.RemovePlaylistEntryRequest);
      case 'MovePlaylistEntry':
        return movePlaylistEntry(ctx, request as $4.MovePlaylistEntryRequest);
      case 'GetPlaylistWithEntries':
        return getPlaylistWithEntries(
            ctx, request as $6.GetPlaylistWithEntriesRequest);
      case 'UnregisterModule':
        return unregisterModule(ctx, request as $6.UnregisterModuleRequest);
      default:
        throw $core.ArgumentError('Unknown method: $methodName');
    }
  }

  $core.Map<$core.String, $core.dynamic> get $json => LibraryServiceBase$json;
  $core.Map<$core.String, $core.Map<$core.String, $core.dynamic>>
      get $messageJson => LibraryServiceBase$messageJson;
}
