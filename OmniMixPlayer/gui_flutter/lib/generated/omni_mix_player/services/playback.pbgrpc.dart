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

import 'package:grpc/service_api.dart' as $grpc;
import 'package:protobuf/protobuf.dart' as $pb;

import '../models/instance.pb.dart' as $1;
import 'playback.pb.dart' as $0;

export 'playback.pb.dart';

/// 播放控制服务 — 实例级别
@$pb.GrpcServiceName('omni_mix_player.PlaybackService')
class PlaybackServiceClient extends $grpc.Client {
  /// The hostname for this service.
  static const $core.String defaultHost = '';

  /// OAuth scopes needed for the client.
  static const $core.List<$core.String> oauthScopes = [
    '',
  ];

  PlaybackServiceClient(super.channel, {super.options, super.interceptors});

  /// 播放控制
  $grpc.ResponseFuture<$0.PlayResponse> play(
    $0.PlayRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$play, request, options: options);
  }

  $grpc.ResponseFuture<$0.PauseResponse> pause(
    $0.PauseRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$pause, request, options: options);
  }

  $grpc.ResponseFuture<$0.ResumeResponse> resume(
    $0.ResumeRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$resume, request, options: options);
  }

  $grpc.ResponseFuture<$0.ToggleResponse> toggle(
    $0.ToggleRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$toggle, request, options: options);
  }

  $grpc.ResponseFuture<$0.NextResponse> next(
    $0.NextRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$next, request, options: options);
  }

  $grpc.ResponseFuture<$0.PrevResponse> prev(
    $0.PrevRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$prev, request, options: options);
  }

  $grpc.ResponseFuture<$0.SeekResponse> seek(
    $0.SeekRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$seek, request, options: options);
  }

  $grpc.ResponseFuture<$0.StopResponse> stop(
    $0.StopRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$stop, request, options: options);
  }

  /// 音量 / 延迟
  $grpc.ResponseFuture<$0.SetVolumeResponse> setVolume(
    $0.SetVolumeRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setVolume, request, options: options);
  }

  $grpc.ResponseFuture<$0.GetVolumeResponse> getVolume(
    $0.GetVolumeRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getVolume, request, options: options);
  }

  $grpc.ResponseFuture<$0.SetTargetLatencyResponse> setTargetLatency(
    $0.SetTargetLatencyRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setTargetLatency, request, options: options);
  }

  $grpc.ResponseFuture<$0.GetTargetLatencyResponse> getTargetLatency(
    $0.GetTargetLatencyRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getTargetLatency, request, options: options);
  }

  /// 随机 / 重复
  $grpc.ResponseFuture<$0.SetShuffleResponse> setShuffle(
    $0.SetShuffleRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setShuffle, request, options: options);
  }

  $grpc.ResponseFuture<$0.SetRepeatModeResponse> setRepeatMode(
    $0.SetRepeatModeRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setRepeatMode, request, options: options);
  }

  /// 队列管理
  $grpc.ResponseFuture<$0.GetQueueResponse> getQueue(
    $0.GetQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getQueue, request, options: options);
  }

  $grpc.ResponseFuture<$0.AddToQueueResponse> addToQueue(
    $0.AddToQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$addToQueue, request, options: options);
  }

  $grpc.ResponseFuture<$0.InsertIntoQueueResponse> insertIntoQueue(
    $0.InsertIntoQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$insertIntoQueue, request, options: options);
  }

  $grpc.ResponseFuture<$0.SetQueueResponse> setQueue(
    $0.SetQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setQueue, request, options: options);
  }

  $grpc.ResponseFuture<$0.RemoveFromQueueResponse> removeFromQueue(
    $0.RemoveFromQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$removeFromQueue, request, options: options);
  }

  $grpc.ResponseFuture<$0.MoveInQueueResponse> moveInQueue(
    $0.MoveInQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$moveInQueue, request, options: options);
  }

  $grpc.ResponseFuture<$0.ClearQueueResponse> clearQueue(
    $0.ClearQueueRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$clearQueue, request, options: options);
  }

  /// 历史
  $grpc.ResponseFuture<$0.GetHistoryResponse> getHistory(
    $0.GetHistoryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getHistory, request, options: options);
  }

  $grpc.ResponseFuture<$0.RemoveFromHistoryResponse> removeFromHistory(
    $0.RemoveFromHistoryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$removeFromHistory, request, options: options);
  }

  $grpc.ResponseFuture<$0.MoveInHistoryResponse> moveInHistory(
    $0.MoveInHistoryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$moveInHistory, request, options: options);
  }

  $grpc.ResponseFuture<$0.ClearHistoryResponse> clearHistory(
    $0.ClearHistoryRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$clearHistory, request, options: options);
  }

  /// 歌单源
  $grpc.ResponseFuture<$0.GetPlaylistSourcesResponse> getPlaylistSources(
    $0.GetPlaylistSourcesRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getPlaylistSources, request, options: options);
  }

  $grpc.ResponseFuture<$0.SetPlaylistSourcesResponse> setPlaylistSources(
    $0.SetPlaylistSourcesRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setPlaylistSources, request, options: options);
  }

  /// 状态
  $grpc.ResponseFuture<$1.PlaybackStatus> getStatus(
    $0.GetStatusRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getStatus, request, options: options);
  }

  /// 均衡器
  $grpc.ResponseFuture<$1.EqualizerState> getEqualizer(
    $0.GetEqualizerRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getEqualizer, request, options: options);
  }

  $grpc.ResponseFuture<$0.SetEqualizerResponse> setEqualizer(
    $0.SetEqualizerRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$setEqualizer, request, options: options);
  }

  // method descriptors

  static final _$play = $grpc.ClientMethod<$0.PlayRequest, $0.PlayResponse>(
      '/omni_mix_player.PlaybackService/Play',
      ($0.PlayRequest value) => value.writeToBuffer(),
      $0.PlayResponse.fromBuffer);
  static final _$pause = $grpc.ClientMethod<$0.PauseRequest, $0.PauseResponse>(
      '/omni_mix_player.PlaybackService/Pause',
      ($0.PauseRequest value) => value.writeToBuffer(),
      $0.PauseResponse.fromBuffer);
  static final _$resume =
      $grpc.ClientMethod<$0.ResumeRequest, $0.ResumeResponse>(
          '/omni_mix_player.PlaybackService/Resume',
          ($0.ResumeRequest value) => value.writeToBuffer(),
          $0.ResumeResponse.fromBuffer);
  static final _$toggle =
      $grpc.ClientMethod<$0.ToggleRequest, $0.ToggleResponse>(
          '/omni_mix_player.PlaybackService/Toggle',
          ($0.ToggleRequest value) => value.writeToBuffer(),
          $0.ToggleResponse.fromBuffer);
  static final _$next = $grpc.ClientMethod<$0.NextRequest, $0.NextResponse>(
      '/omni_mix_player.PlaybackService/Next',
      ($0.NextRequest value) => value.writeToBuffer(),
      $0.NextResponse.fromBuffer);
  static final _$prev = $grpc.ClientMethod<$0.PrevRequest, $0.PrevResponse>(
      '/omni_mix_player.PlaybackService/Prev',
      ($0.PrevRequest value) => value.writeToBuffer(),
      $0.PrevResponse.fromBuffer);
  static final _$seek = $grpc.ClientMethod<$0.SeekRequest, $0.SeekResponse>(
      '/omni_mix_player.PlaybackService/Seek',
      ($0.SeekRequest value) => value.writeToBuffer(),
      $0.SeekResponse.fromBuffer);
  static final _$stop = $grpc.ClientMethod<$0.StopRequest, $0.StopResponse>(
      '/omni_mix_player.PlaybackService/Stop',
      ($0.StopRequest value) => value.writeToBuffer(),
      $0.StopResponse.fromBuffer);
  static final _$setVolume =
      $grpc.ClientMethod<$0.SetVolumeRequest, $0.SetVolumeResponse>(
          '/omni_mix_player.PlaybackService/SetVolume',
          ($0.SetVolumeRequest value) => value.writeToBuffer(),
          $0.SetVolumeResponse.fromBuffer);
  static final _$getVolume =
      $grpc.ClientMethod<$0.GetVolumeRequest, $0.GetVolumeResponse>(
          '/omni_mix_player.PlaybackService/GetVolume',
          ($0.GetVolumeRequest value) => value.writeToBuffer(),
          $0.GetVolumeResponse.fromBuffer);
  static final _$setTargetLatency = $grpc.ClientMethod<
          $0.SetTargetLatencyRequest, $0.SetTargetLatencyResponse>(
      '/omni_mix_player.PlaybackService/SetTargetLatency',
      ($0.SetTargetLatencyRequest value) => value.writeToBuffer(),
      $0.SetTargetLatencyResponse.fromBuffer);
  static final _$getTargetLatency = $grpc.ClientMethod<
          $0.GetTargetLatencyRequest, $0.GetTargetLatencyResponse>(
      '/omni_mix_player.PlaybackService/GetTargetLatency',
      ($0.GetTargetLatencyRequest value) => value.writeToBuffer(),
      $0.GetTargetLatencyResponse.fromBuffer);
  static final _$setShuffle =
      $grpc.ClientMethod<$0.SetShuffleRequest, $0.SetShuffleResponse>(
          '/omni_mix_player.PlaybackService/SetShuffle',
          ($0.SetShuffleRequest value) => value.writeToBuffer(),
          $0.SetShuffleResponse.fromBuffer);
  static final _$setRepeatMode =
      $grpc.ClientMethod<$0.SetRepeatModeRequest, $0.SetRepeatModeResponse>(
          '/omni_mix_player.PlaybackService/SetRepeatMode',
          ($0.SetRepeatModeRequest value) => value.writeToBuffer(),
          $0.SetRepeatModeResponse.fromBuffer);
  static final _$getQueue =
      $grpc.ClientMethod<$0.GetQueueRequest, $0.GetQueueResponse>(
          '/omni_mix_player.PlaybackService/GetQueue',
          ($0.GetQueueRequest value) => value.writeToBuffer(),
          $0.GetQueueResponse.fromBuffer);
  static final _$addToQueue =
      $grpc.ClientMethod<$0.AddToQueueRequest, $0.AddToQueueResponse>(
          '/omni_mix_player.PlaybackService/AddToQueue',
          ($0.AddToQueueRequest value) => value.writeToBuffer(),
          $0.AddToQueueResponse.fromBuffer);
  static final _$insertIntoQueue =
      $grpc.ClientMethod<$0.InsertIntoQueueRequest, $0.InsertIntoQueueResponse>(
          '/omni_mix_player.PlaybackService/InsertIntoQueue',
          ($0.InsertIntoQueueRequest value) => value.writeToBuffer(),
          $0.InsertIntoQueueResponse.fromBuffer);
  static final _$setQueue =
      $grpc.ClientMethod<$0.SetQueueRequest, $0.SetQueueResponse>(
          '/omni_mix_player.PlaybackService/SetQueue',
          ($0.SetQueueRequest value) => value.writeToBuffer(),
          $0.SetQueueResponse.fromBuffer);
  static final _$removeFromQueue =
      $grpc.ClientMethod<$0.RemoveFromQueueRequest, $0.RemoveFromQueueResponse>(
          '/omni_mix_player.PlaybackService/RemoveFromQueue',
          ($0.RemoveFromQueueRequest value) => value.writeToBuffer(),
          $0.RemoveFromQueueResponse.fromBuffer);
  static final _$moveInQueue =
      $grpc.ClientMethod<$0.MoveInQueueRequest, $0.MoveInQueueResponse>(
          '/omni_mix_player.PlaybackService/MoveInQueue',
          ($0.MoveInQueueRequest value) => value.writeToBuffer(),
          $0.MoveInQueueResponse.fromBuffer);
  static final _$clearQueue =
      $grpc.ClientMethod<$0.ClearQueueRequest, $0.ClearQueueResponse>(
          '/omni_mix_player.PlaybackService/ClearQueue',
          ($0.ClearQueueRequest value) => value.writeToBuffer(),
          $0.ClearQueueResponse.fromBuffer);
  static final _$getHistory =
      $grpc.ClientMethod<$0.GetHistoryRequest, $0.GetHistoryResponse>(
          '/omni_mix_player.PlaybackService/GetHistory',
          ($0.GetHistoryRequest value) => value.writeToBuffer(),
          $0.GetHistoryResponse.fromBuffer);
  static final _$removeFromHistory = $grpc.ClientMethod<
          $0.RemoveFromHistoryRequest, $0.RemoveFromHistoryResponse>(
      '/omni_mix_player.PlaybackService/RemoveFromHistory',
      ($0.RemoveFromHistoryRequest value) => value.writeToBuffer(),
      $0.RemoveFromHistoryResponse.fromBuffer);
  static final _$moveInHistory =
      $grpc.ClientMethod<$0.MoveInHistoryRequest, $0.MoveInHistoryResponse>(
          '/omni_mix_player.PlaybackService/MoveInHistory',
          ($0.MoveInHistoryRequest value) => value.writeToBuffer(),
          $0.MoveInHistoryResponse.fromBuffer);
  static final _$clearHistory =
      $grpc.ClientMethod<$0.ClearHistoryRequest, $0.ClearHistoryResponse>(
          '/omni_mix_player.PlaybackService/ClearHistory',
          ($0.ClearHistoryRequest value) => value.writeToBuffer(),
          $0.ClearHistoryResponse.fromBuffer);
  static final _$getPlaylistSources = $grpc.ClientMethod<
          $0.GetPlaylistSourcesRequest, $0.GetPlaylistSourcesResponse>(
      '/omni_mix_player.PlaybackService/GetPlaylistSources',
      ($0.GetPlaylistSourcesRequest value) => value.writeToBuffer(),
      $0.GetPlaylistSourcesResponse.fromBuffer);
  static final _$setPlaylistSources = $grpc.ClientMethod<
          $0.SetPlaylistSourcesRequest, $0.SetPlaylistSourcesResponse>(
      '/omni_mix_player.PlaybackService/SetPlaylistSources',
      ($0.SetPlaylistSourcesRequest value) => value.writeToBuffer(),
      $0.SetPlaylistSourcesResponse.fromBuffer);
  static final _$getStatus =
      $grpc.ClientMethod<$0.GetStatusRequest, $1.PlaybackStatus>(
          '/omni_mix_player.PlaybackService/GetStatus',
          ($0.GetStatusRequest value) => value.writeToBuffer(),
          $1.PlaybackStatus.fromBuffer);
  static final _$getEqualizer =
      $grpc.ClientMethod<$0.GetEqualizerRequest, $1.EqualizerState>(
          '/omni_mix_player.PlaybackService/GetEqualizer',
          ($0.GetEqualizerRequest value) => value.writeToBuffer(),
          $1.EqualizerState.fromBuffer);
  static final _$setEqualizer =
      $grpc.ClientMethod<$0.SetEqualizerRequest, $0.SetEqualizerResponse>(
          '/omni_mix_player.PlaybackService/SetEqualizer',
          ($0.SetEqualizerRequest value) => value.writeToBuffer(),
          $0.SetEqualizerResponse.fromBuffer);
}

@$pb.GrpcServiceName('omni_mix_player.PlaybackService')
abstract class PlaybackServiceBase extends $grpc.Service {
  $core.String get $name => 'omni_mix_player.PlaybackService';

  PlaybackServiceBase() {
    $addMethod($grpc.ServiceMethod<$0.PlayRequest, $0.PlayResponse>(
        'Play',
        play_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.PlayRequest.fromBuffer(value),
        ($0.PlayResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.PauseRequest, $0.PauseResponse>(
        'Pause',
        pause_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.PauseRequest.fromBuffer(value),
        ($0.PauseResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.ResumeRequest, $0.ResumeResponse>(
        'Resume',
        resume_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.ResumeRequest.fromBuffer(value),
        ($0.ResumeResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.ToggleRequest, $0.ToggleResponse>(
        'Toggle',
        toggle_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.ToggleRequest.fromBuffer(value),
        ($0.ToggleResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.NextRequest, $0.NextResponse>(
        'Next',
        next_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.NextRequest.fromBuffer(value),
        ($0.NextResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.PrevRequest, $0.PrevResponse>(
        'Prev',
        prev_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.PrevRequest.fromBuffer(value),
        ($0.PrevResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.SeekRequest, $0.SeekResponse>(
        'Seek',
        seek_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.SeekRequest.fromBuffer(value),
        ($0.SeekResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.StopRequest, $0.StopResponse>(
        'Stop',
        stop_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.StopRequest.fromBuffer(value),
        ($0.StopResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.SetVolumeRequest, $0.SetVolumeResponse>(
        'SetVolume',
        setVolume_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.SetVolumeRequest.fromBuffer(value),
        ($0.SetVolumeResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetVolumeRequest, $0.GetVolumeResponse>(
        'GetVolume',
        getVolume_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.GetVolumeRequest.fromBuffer(value),
        ($0.GetVolumeResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.SetTargetLatencyRequest,
            $0.SetTargetLatencyResponse>(
        'SetTargetLatency',
        setTargetLatency_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.SetTargetLatencyRequest.fromBuffer(value),
        ($0.SetTargetLatencyResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetTargetLatencyRequest,
            $0.GetTargetLatencyResponse>(
        'GetTargetLatency',
        getTargetLatency_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.GetTargetLatencyRequest.fromBuffer(value),
        ($0.GetTargetLatencyResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.SetShuffleRequest, $0.SetShuffleResponse>(
        'SetShuffle',
        setShuffle_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.SetShuffleRequest.fromBuffer(value),
        ($0.SetShuffleResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.SetRepeatModeRequest, $0.SetRepeatModeResponse>(
            'SetRepeatMode',
            setRepeatMode_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.SetRepeatModeRequest.fromBuffer(value),
            ($0.SetRepeatModeResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetQueueRequest, $0.GetQueueResponse>(
        'GetQueue',
        getQueue_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.GetQueueRequest.fromBuffer(value),
        ($0.GetQueueResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.AddToQueueRequest, $0.AddToQueueResponse>(
        'AddToQueue',
        addToQueue_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.AddToQueueRequest.fromBuffer(value),
        ($0.AddToQueueResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.InsertIntoQueueRequest,
            $0.InsertIntoQueueResponse>(
        'InsertIntoQueue',
        insertIntoQueue_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.InsertIntoQueueRequest.fromBuffer(value),
        ($0.InsertIntoQueueResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.SetQueueRequest, $0.SetQueueResponse>(
        'SetQueue',
        setQueue_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.SetQueueRequest.fromBuffer(value),
        ($0.SetQueueResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.RemoveFromQueueRequest,
            $0.RemoveFromQueueResponse>(
        'RemoveFromQueue',
        removeFromQueue_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.RemoveFromQueueRequest.fromBuffer(value),
        ($0.RemoveFromQueueResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.MoveInQueueRequest, $0.MoveInQueueResponse>(
            'MoveInQueue',
            moveInQueue_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.MoveInQueueRequest.fromBuffer(value),
            ($0.MoveInQueueResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.ClearQueueRequest, $0.ClearQueueResponse>(
        'ClearQueue',
        clearQueue_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.ClearQueueRequest.fromBuffer(value),
        ($0.ClearQueueResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetHistoryRequest, $0.GetHistoryResponse>(
        'GetHistory',
        getHistory_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.GetHistoryRequest.fromBuffer(value),
        ($0.GetHistoryResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.RemoveFromHistoryRequest,
            $0.RemoveFromHistoryResponse>(
        'RemoveFromHistory',
        removeFromHistory_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.RemoveFromHistoryRequest.fromBuffer(value),
        ($0.RemoveFromHistoryResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.MoveInHistoryRequest, $0.MoveInHistoryResponse>(
            'MoveInHistory',
            moveInHistory_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.MoveInHistoryRequest.fromBuffer(value),
            ($0.MoveInHistoryResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.ClearHistoryRequest, $0.ClearHistoryResponse>(
            'ClearHistory',
            clearHistory_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.ClearHistoryRequest.fromBuffer(value),
            ($0.ClearHistoryResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetPlaylistSourcesRequest,
            $0.GetPlaylistSourcesResponse>(
        'GetPlaylistSources',
        getPlaylistSources_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.GetPlaylistSourcesRequest.fromBuffer(value),
        ($0.GetPlaylistSourcesResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.SetPlaylistSourcesRequest,
            $0.SetPlaylistSourcesResponse>(
        'SetPlaylistSources',
        setPlaylistSources_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.SetPlaylistSourcesRequest.fromBuffer(value),
        ($0.SetPlaylistSourcesResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetStatusRequest, $1.PlaybackStatus>(
        'GetStatus',
        getStatus_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $0.GetStatusRequest.fromBuffer(value),
        ($1.PlaybackStatus value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.GetEqualizerRequest, $1.EqualizerState>(
        'GetEqualizer',
        getEqualizer_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.GetEqualizerRequest.fromBuffer(value),
        ($1.EqualizerState value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$0.SetEqualizerRequest, $0.SetEqualizerResponse>(
            'SetEqualizer',
            setEqualizer_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $0.SetEqualizerRequest.fromBuffer(value),
            ($0.SetEqualizerResponse value) => value.writeToBuffer()));
  }

  $async.Future<$0.PlayResponse> play_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.PlayRequest> $request) async {
    return play($call, await $request);
  }

  $async.Future<$0.PlayResponse> play(
      $grpc.ServiceCall call, $0.PlayRequest request);

  $async.Future<$0.PauseResponse> pause_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.PauseRequest> $request) async {
    return pause($call, await $request);
  }

  $async.Future<$0.PauseResponse> pause(
      $grpc.ServiceCall call, $0.PauseRequest request);

  $async.Future<$0.ResumeResponse> resume_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.ResumeRequest> $request) async {
    return resume($call, await $request);
  }

  $async.Future<$0.ResumeResponse> resume(
      $grpc.ServiceCall call, $0.ResumeRequest request);

  $async.Future<$0.ToggleResponse> toggle_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.ToggleRequest> $request) async {
    return toggle($call, await $request);
  }

  $async.Future<$0.ToggleResponse> toggle(
      $grpc.ServiceCall call, $0.ToggleRequest request);

  $async.Future<$0.NextResponse> next_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.NextRequest> $request) async {
    return next($call, await $request);
  }

  $async.Future<$0.NextResponse> next(
      $grpc.ServiceCall call, $0.NextRequest request);

  $async.Future<$0.PrevResponse> prev_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.PrevRequest> $request) async {
    return prev($call, await $request);
  }

  $async.Future<$0.PrevResponse> prev(
      $grpc.ServiceCall call, $0.PrevRequest request);

  $async.Future<$0.SeekResponse> seek_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.SeekRequest> $request) async {
    return seek($call, await $request);
  }

  $async.Future<$0.SeekResponse> seek(
      $grpc.ServiceCall call, $0.SeekRequest request);

  $async.Future<$0.StopResponse> stop_Pre(
      $grpc.ServiceCall $call, $async.Future<$0.StopRequest> $request) async {
    return stop($call, await $request);
  }

  $async.Future<$0.StopResponse> stop(
      $grpc.ServiceCall call, $0.StopRequest request);

  $async.Future<$0.SetVolumeResponse> setVolume_Pre($grpc.ServiceCall $call,
      $async.Future<$0.SetVolumeRequest> $request) async {
    return setVolume($call, await $request);
  }

  $async.Future<$0.SetVolumeResponse> setVolume(
      $grpc.ServiceCall call, $0.SetVolumeRequest request);

  $async.Future<$0.GetVolumeResponse> getVolume_Pre($grpc.ServiceCall $call,
      $async.Future<$0.GetVolumeRequest> $request) async {
    return getVolume($call, await $request);
  }

  $async.Future<$0.GetVolumeResponse> getVolume(
      $grpc.ServiceCall call, $0.GetVolumeRequest request);

  $async.Future<$0.SetTargetLatencyResponse> setTargetLatency_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.SetTargetLatencyRequest> $request) async {
    return setTargetLatency($call, await $request);
  }

  $async.Future<$0.SetTargetLatencyResponse> setTargetLatency(
      $grpc.ServiceCall call, $0.SetTargetLatencyRequest request);

  $async.Future<$0.GetTargetLatencyResponse> getTargetLatency_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.GetTargetLatencyRequest> $request) async {
    return getTargetLatency($call, await $request);
  }

  $async.Future<$0.GetTargetLatencyResponse> getTargetLatency(
      $grpc.ServiceCall call, $0.GetTargetLatencyRequest request);

  $async.Future<$0.SetShuffleResponse> setShuffle_Pre($grpc.ServiceCall $call,
      $async.Future<$0.SetShuffleRequest> $request) async {
    return setShuffle($call, await $request);
  }

  $async.Future<$0.SetShuffleResponse> setShuffle(
      $grpc.ServiceCall call, $0.SetShuffleRequest request);

  $async.Future<$0.SetRepeatModeResponse> setRepeatMode_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.SetRepeatModeRequest> $request) async {
    return setRepeatMode($call, await $request);
  }

  $async.Future<$0.SetRepeatModeResponse> setRepeatMode(
      $grpc.ServiceCall call, $0.SetRepeatModeRequest request);

  $async.Future<$0.GetQueueResponse> getQueue_Pre($grpc.ServiceCall $call,
      $async.Future<$0.GetQueueRequest> $request) async {
    return getQueue($call, await $request);
  }

  $async.Future<$0.GetQueueResponse> getQueue(
      $grpc.ServiceCall call, $0.GetQueueRequest request);

  $async.Future<$0.AddToQueueResponse> addToQueue_Pre($grpc.ServiceCall $call,
      $async.Future<$0.AddToQueueRequest> $request) async {
    return addToQueue($call, await $request);
  }

  $async.Future<$0.AddToQueueResponse> addToQueue(
      $grpc.ServiceCall call, $0.AddToQueueRequest request);

  $async.Future<$0.InsertIntoQueueResponse> insertIntoQueue_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.InsertIntoQueueRequest> $request) async {
    return insertIntoQueue($call, await $request);
  }

  $async.Future<$0.InsertIntoQueueResponse> insertIntoQueue(
      $grpc.ServiceCall call, $0.InsertIntoQueueRequest request);

  $async.Future<$0.SetQueueResponse> setQueue_Pre($grpc.ServiceCall $call,
      $async.Future<$0.SetQueueRequest> $request) async {
    return setQueue($call, await $request);
  }

  $async.Future<$0.SetQueueResponse> setQueue(
      $grpc.ServiceCall call, $0.SetQueueRequest request);

  $async.Future<$0.RemoveFromQueueResponse> removeFromQueue_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.RemoveFromQueueRequest> $request) async {
    return removeFromQueue($call, await $request);
  }

  $async.Future<$0.RemoveFromQueueResponse> removeFromQueue(
      $grpc.ServiceCall call, $0.RemoveFromQueueRequest request);

  $async.Future<$0.MoveInQueueResponse> moveInQueue_Pre($grpc.ServiceCall $call,
      $async.Future<$0.MoveInQueueRequest> $request) async {
    return moveInQueue($call, await $request);
  }

  $async.Future<$0.MoveInQueueResponse> moveInQueue(
      $grpc.ServiceCall call, $0.MoveInQueueRequest request);

  $async.Future<$0.ClearQueueResponse> clearQueue_Pre($grpc.ServiceCall $call,
      $async.Future<$0.ClearQueueRequest> $request) async {
    return clearQueue($call, await $request);
  }

  $async.Future<$0.ClearQueueResponse> clearQueue(
      $grpc.ServiceCall call, $0.ClearQueueRequest request);

  $async.Future<$0.GetHistoryResponse> getHistory_Pre($grpc.ServiceCall $call,
      $async.Future<$0.GetHistoryRequest> $request) async {
    return getHistory($call, await $request);
  }

  $async.Future<$0.GetHistoryResponse> getHistory(
      $grpc.ServiceCall call, $0.GetHistoryRequest request);

  $async.Future<$0.RemoveFromHistoryResponse> removeFromHistory_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.RemoveFromHistoryRequest> $request) async {
    return removeFromHistory($call, await $request);
  }

  $async.Future<$0.RemoveFromHistoryResponse> removeFromHistory(
      $grpc.ServiceCall call, $0.RemoveFromHistoryRequest request);

  $async.Future<$0.MoveInHistoryResponse> moveInHistory_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.MoveInHistoryRequest> $request) async {
    return moveInHistory($call, await $request);
  }

  $async.Future<$0.MoveInHistoryResponse> moveInHistory(
      $grpc.ServiceCall call, $0.MoveInHistoryRequest request);

  $async.Future<$0.ClearHistoryResponse> clearHistory_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.ClearHistoryRequest> $request) async {
    return clearHistory($call, await $request);
  }

  $async.Future<$0.ClearHistoryResponse> clearHistory(
      $grpc.ServiceCall call, $0.ClearHistoryRequest request);

  $async.Future<$0.GetPlaylistSourcesResponse> getPlaylistSources_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.GetPlaylistSourcesRequest> $request) async {
    return getPlaylistSources($call, await $request);
  }

  $async.Future<$0.GetPlaylistSourcesResponse> getPlaylistSources(
      $grpc.ServiceCall call, $0.GetPlaylistSourcesRequest request);

  $async.Future<$0.SetPlaylistSourcesResponse> setPlaylistSources_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.SetPlaylistSourcesRequest> $request) async {
    return setPlaylistSources($call, await $request);
  }

  $async.Future<$0.SetPlaylistSourcesResponse> setPlaylistSources(
      $grpc.ServiceCall call, $0.SetPlaylistSourcesRequest request);

  $async.Future<$1.PlaybackStatus> getStatus_Pre($grpc.ServiceCall $call,
      $async.Future<$0.GetStatusRequest> $request) async {
    return getStatus($call, await $request);
  }

  $async.Future<$1.PlaybackStatus> getStatus(
      $grpc.ServiceCall call, $0.GetStatusRequest request);

  $async.Future<$1.EqualizerState> getEqualizer_Pre($grpc.ServiceCall $call,
      $async.Future<$0.GetEqualizerRequest> $request) async {
    return getEqualizer($call, await $request);
  }

  $async.Future<$1.EqualizerState> getEqualizer(
      $grpc.ServiceCall call, $0.GetEqualizerRequest request);

  $async.Future<$0.SetEqualizerResponse> setEqualizer_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.SetEqualizerRequest> $request) async {
    return setEqualizer($call, await $request);
  }

  $async.Future<$0.SetEqualizerResponse> setEqualizer(
      $grpc.ServiceCall call, $0.SetEqualizerRequest request);
}
