// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/playback.proto.

// @dart = 3.3

// ignore_for_file: annotate_overrides, camel_case_types, comment_references
// ignore_for_file: constant_identifier_names
// ignore_for_file: curly_braces_in_flow_control_structures
// ignore_for_file: deprecated_member_use_from_same_package, library_prefixes
// ignore_for_file: non_constant_identifier_names, prefer_relative_imports

import 'dart:async' as $async;
import 'dart:core' as $core;

import 'package:protobuf/protobuf.dart' as $pb;

import '../models/instance.pb.dart' as $0;
import 'playback.pb.dart' as $2;
import 'playback.pbjson.dart';

export 'playback.pb.dart';

abstract class PlaybackServiceBase extends $pb.GeneratedService {
  $async.Future<$2.PlayResponse> play(
      $pb.ServerContext ctx, $2.PlayRequest request);
  $async.Future<$2.PauseResponse> pause(
      $pb.ServerContext ctx, $2.PauseRequest request);
  $async.Future<$2.ResumeResponse> resume(
      $pb.ServerContext ctx, $2.ResumeRequest request);
  $async.Future<$2.ToggleResponse> toggle(
      $pb.ServerContext ctx, $2.ToggleRequest request);
  $async.Future<$2.NextResponse> next(
      $pb.ServerContext ctx, $2.NextRequest request);
  $async.Future<$2.PrevResponse> prev(
      $pb.ServerContext ctx, $2.PrevRequest request);
  $async.Future<$2.SeekResponse> seek(
      $pb.ServerContext ctx, $2.SeekRequest request);
  $async.Future<$2.StopResponse> stop(
      $pb.ServerContext ctx, $2.StopRequest request);
  $async.Future<$2.SetVolumeResponse> setVolume(
      $pb.ServerContext ctx, $2.SetVolumeRequest request);
  $async.Future<$2.GetVolumeResponse> getVolume(
      $pb.ServerContext ctx, $2.GetVolumeRequest request);
  $async.Future<$2.SetTargetLatencyResponse> setTargetLatency(
      $pb.ServerContext ctx, $2.SetTargetLatencyRequest request);
  $async.Future<$2.GetTargetLatencyResponse> getTargetLatency(
      $pb.ServerContext ctx, $2.GetTargetLatencyRequest request);
  $async.Future<$2.SetShuffleResponse> setShuffle(
      $pb.ServerContext ctx, $2.SetShuffleRequest request);
  $async.Future<$2.SetRepeatModeResponse> setRepeatMode(
      $pb.ServerContext ctx, $2.SetRepeatModeRequest request);
  $async.Future<$2.GetQueueResponse> getQueue(
      $pb.ServerContext ctx, $2.GetQueueRequest request);
  $async.Future<$2.AddToQueueResponse> addToQueue(
      $pb.ServerContext ctx, $2.AddToQueueRequest request);
  $async.Future<$2.InsertIntoQueueResponse> insertIntoQueue(
      $pb.ServerContext ctx, $2.InsertIntoQueueRequest request);
  $async.Future<$2.SetQueueResponse> setQueue(
      $pb.ServerContext ctx, $2.SetQueueRequest request);
  $async.Future<$2.RemoveFromQueueResponse> removeFromQueue(
      $pb.ServerContext ctx, $2.RemoveFromQueueRequest request);
  $async.Future<$2.MoveInQueueResponse> moveInQueue(
      $pb.ServerContext ctx, $2.MoveInQueueRequest request);
  $async.Future<$2.ClearQueueResponse> clearQueue(
      $pb.ServerContext ctx, $2.ClearQueueRequest request);
  $async.Future<$2.GetHistoryResponse> getHistory(
      $pb.ServerContext ctx, $2.GetHistoryRequest request);
  $async.Future<$2.RemoveFromHistoryResponse> removeFromHistory(
      $pb.ServerContext ctx, $2.RemoveFromHistoryRequest request);
  $async.Future<$2.MoveInHistoryResponse> moveInHistory(
      $pb.ServerContext ctx, $2.MoveInHistoryRequest request);
  $async.Future<$2.ClearHistoryResponse> clearHistory(
      $pb.ServerContext ctx, $2.ClearHistoryRequest request);
  $async.Future<$2.GetPlaylistSourcesResponse> getPlaylistSources(
      $pb.ServerContext ctx, $2.GetPlaylistSourcesRequest request);
  $async.Future<$2.SetPlaylistSourcesResponse> setPlaylistSources(
      $pb.ServerContext ctx, $2.SetPlaylistSourcesRequest request);
  $async.Future<$0.PlaybackStatus> getStatus(
      $pb.ServerContext ctx, $2.GetStatusRequest request);
  $async.Future<$0.EqualizerState> getEqualizer(
      $pb.ServerContext ctx, $2.GetEqualizerRequest request);
  $async.Future<$2.SetEqualizerResponse> setEqualizer(
      $pb.ServerContext ctx, $2.SetEqualizerRequest request);

  $pb.GeneratedMessage createRequest($core.String methodName) {
    switch (methodName) {
      case 'Play':
        return $2.PlayRequest();
      case 'Pause':
        return $2.PauseRequest();
      case 'Resume':
        return $2.ResumeRequest();
      case 'Toggle':
        return $2.ToggleRequest();
      case 'Next':
        return $2.NextRequest();
      case 'Prev':
        return $2.PrevRequest();
      case 'Seek':
        return $2.SeekRequest();
      case 'Stop':
        return $2.StopRequest();
      case 'SetVolume':
        return $2.SetVolumeRequest();
      case 'GetVolume':
        return $2.GetVolumeRequest();
      case 'SetTargetLatency':
        return $2.SetTargetLatencyRequest();
      case 'GetTargetLatency':
        return $2.GetTargetLatencyRequest();
      case 'SetShuffle':
        return $2.SetShuffleRequest();
      case 'SetRepeatMode':
        return $2.SetRepeatModeRequest();
      case 'GetQueue':
        return $2.GetQueueRequest();
      case 'AddToQueue':
        return $2.AddToQueueRequest();
      case 'InsertIntoQueue':
        return $2.InsertIntoQueueRequest();
      case 'SetQueue':
        return $2.SetQueueRequest();
      case 'RemoveFromQueue':
        return $2.RemoveFromQueueRequest();
      case 'MoveInQueue':
        return $2.MoveInQueueRequest();
      case 'ClearQueue':
        return $2.ClearQueueRequest();
      case 'GetHistory':
        return $2.GetHistoryRequest();
      case 'RemoveFromHistory':
        return $2.RemoveFromHistoryRequest();
      case 'MoveInHistory':
        return $2.MoveInHistoryRequest();
      case 'ClearHistory':
        return $2.ClearHistoryRequest();
      case 'GetPlaylistSources':
        return $2.GetPlaylistSourcesRequest();
      case 'SetPlaylistSources':
        return $2.SetPlaylistSourcesRequest();
      case 'GetStatus':
        return $2.GetStatusRequest();
      case 'GetEqualizer':
        return $2.GetEqualizerRequest();
      case 'SetEqualizer':
        return $2.SetEqualizerRequest();
      default:
        throw $core.ArgumentError('Unknown method: $methodName');
    }
  }

  $async.Future<$pb.GeneratedMessage> handleCall($pb.ServerContext ctx,
      $core.String methodName, $pb.GeneratedMessage request) {
    switch (methodName) {
      case 'Play':
        return play(ctx, request as $2.PlayRequest);
      case 'Pause':
        return pause(ctx, request as $2.PauseRequest);
      case 'Resume':
        return resume(ctx, request as $2.ResumeRequest);
      case 'Toggle':
        return toggle(ctx, request as $2.ToggleRequest);
      case 'Next':
        return next(ctx, request as $2.NextRequest);
      case 'Prev':
        return prev(ctx, request as $2.PrevRequest);
      case 'Seek':
        return seek(ctx, request as $2.SeekRequest);
      case 'Stop':
        return stop(ctx, request as $2.StopRequest);
      case 'SetVolume':
        return setVolume(ctx, request as $2.SetVolumeRequest);
      case 'GetVolume':
        return getVolume(ctx, request as $2.GetVolumeRequest);
      case 'SetTargetLatency':
        return setTargetLatency(ctx, request as $2.SetTargetLatencyRequest);
      case 'GetTargetLatency':
        return getTargetLatency(ctx, request as $2.GetTargetLatencyRequest);
      case 'SetShuffle':
        return setShuffle(ctx, request as $2.SetShuffleRequest);
      case 'SetRepeatMode':
        return setRepeatMode(ctx, request as $2.SetRepeatModeRequest);
      case 'GetQueue':
        return getQueue(ctx, request as $2.GetQueueRequest);
      case 'AddToQueue':
        return addToQueue(ctx, request as $2.AddToQueueRequest);
      case 'InsertIntoQueue':
        return insertIntoQueue(ctx, request as $2.InsertIntoQueueRequest);
      case 'SetQueue':
        return setQueue(ctx, request as $2.SetQueueRequest);
      case 'RemoveFromQueue':
        return removeFromQueue(ctx, request as $2.RemoveFromQueueRequest);
      case 'MoveInQueue':
        return moveInQueue(ctx, request as $2.MoveInQueueRequest);
      case 'ClearQueue':
        return clearQueue(ctx, request as $2.ClearQueueRequest);
      case 'GetHistory':
        return getHistory(ctx, request as $2.GetHistoryRequest);
      case 'RemoveFromHistory':
        return removeFromHistory(ctx, request as $2.RemoveFromHistoryRequest);
      case 'MoveInHistory':
        return moveInHistory(ctx, request as $2.MoveInHistoryRequest);
      case 'ClearHistory':
        return clearHistory(ctx, request as $2.ClearHistoryRequest);
      case 'GetPlaylistSources':
        return getPlaylistSources(ctx, request as $2.GetPlaylistSourcesRequest);
      case 'SetPlaylistSources':
        return setPlaylistSources(ctx, request as $2.SetPlaylistSourcesRequest);
      case 'GetStatus':
        return getStatus(ctx, request as $2.GetStatusRequest);
      case 'GetEqualizer':
        return getEqualizer(ctx, request as $2.GetEqualizerRequest);
      case 'SetEqualizer':
        return setEqualizer(ctx, request as $2.SetEqualizerRequest);
      default:
        throw $core.ArgumentError('Unknown method: $methodName');
    }
  }

  $core.Map<$core.String, $core.dynamic> get $json => PlaybackServiceBase$json;
  $core.Map<$core.String, $core.Map<$core.String, $core.dynamic>>
      get $messageJson => PlaybackServiceBase$messageJson;
}
