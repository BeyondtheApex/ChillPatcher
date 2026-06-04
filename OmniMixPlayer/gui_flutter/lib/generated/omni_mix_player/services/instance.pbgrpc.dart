// This is a generated file - do not edit.
//
// Generated from omni_mix_player/services/instance.proto.

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

import '../models/instance.pb.dart' as $0;
import 'instance.pb.dart' as $1;

export 'instance.pb.dart';

/// 实例管理服务 — 平台级别
@$pb.GrpcServiceName('omni_mix_player.InstanceService')
class InstanceServiceClient extends $grpc.Client {
  /// The hostname for this service.
  static const $core.String defaultHost = '';

  /// OAuth scopes needed for the client.
  static const $core.List<$core.String> oauthScopes = [
    '',
  ];

  InstanceServiceClient(super.channel, {super.options, super.interceptors});

  $grpc.ResponseFuture<$0.InstanceConnectResponse> connect(
    $0.InstanceConnectRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$connect, request, options: options);
  }

  $grpc.ResponseFuture<$0.InstanceHeartbeatResponse> heartbeat(
    $0.InstanceHeartbeatRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$heartbeat, request, options: options);
  }

  $grpc.ResponseFuture<$0.InstanceDisconnectResponse> disconnect(
    $0.InstanceDisconnectRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$disconnect, request, options: options);
  }

  $grpc.ResponseFuture<$1.DeleteInstanceResponse> deleteInstance(
    $1.DeleteInstanceRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$deleteInstance, request, options: options);
  }

  $grpc.ResponseFuture<$0.ListInstancesResponse> listInstances(
    $1.ListInstancesRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$listInstances, request, options: options);
  }

  $grpc.ResponseFuture<$0.InstanceProfile> getProfile(
    $1.GetProfileRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getProfile, request, options: options);
  }

  $grpc.ResponseFuture<$1.UpdateProfileResponse> updateProfile(
    $1.UpdateProfileRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$updateProfile, request, options: options);
  }

  $grpc.ResponseFuture<$0.PlaybackStatus> getStatus(
    $1.GetInstanceStatusRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getStatus, request, options: options);
  }

  $grpc.ResponseFuture<$1.ArchiveInstanceResponse> archiveInstance(
    $1.ArchiveInstanceRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$archiveInstance, request, options: options);
  }

  $grpc.ResponseFuture<$1.ListArchivesResponse> listArchives(
    $1.ListArchivesRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$listArchives, request, options: options);
  }

  $grpc.ResponseFuture<$0.InstanceProfile> getArchive(
    $1.GetArchiveRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$getArchive, request, options: options);
  }

  $grpc.ResponseFuture<$1.DeleteArchiveResponse> deleteArchive(
    $1.DeleteArchiveRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$deleteArchive, request, options: options);
  }

  $grpc.ResponseFuture<$1.InheritFromArchiveResponse> inheritFromArchive(
    $1.InheritFromArchiveRequest request, {
    $grpc.CallOptions? options,
  }) {
    return $createUnaryCall(_$inheritFromArchive, request, options: options);
  }

  // method descriptors

  static final _$connect =
      $grpc.ClientMethod<$0.InstanceConnectRequest, $0.InstanceConnectResponse>(
          '/omni_mix_player.InstanceService/Connect',
          ($0.InstanceConnectRequest value) => value.writeToBuffer(),
          $0.InstanceConnectResponse.fromBuffer);
  static final _$heartbeat = $grpc.ClientMethod<$0.InstanceHeartbeatRequest,
          $0.InstanceHeartbeatResponse>(
      '/omni_mix_player.InstanceService/Heartbeat',
      ($0.InstanceHeartbeatRequest value) => value.writeToBuffer(),
      $0.InstanceHeartbeatResponse.fromBuffer);
  static final _$disconnect = $grpc.ClientMethod<$0.InstanceDisconnectRequest,
          $0.InstanceDisconnectResponse>(
      '/omni_mix_player.InstanceService/Disconnect',
      ($0.InstanceDisconnectRequest value) => value.writeToBuffer(),
      $0.InstanceDisconnectResponse.fromBuffer);
  static final _$deleteInstance =
      $grpc.ClientMethod<$1.DeleteInstanceRequest, $1.DeleteInstanceResponse>(
          '/omni_mix_player.InstanceService/DeleteInstance',
          ($1.DeleteInstanceRequest value) => value.writeToBuffer(),
          $1.DeleteInstanceResponse.fromBuffer);
  static final _$listInstances =
      $grpc.ClientMethod<$1.ListInstancesRequest, $0.ListInstancesResponse>(
          '/omni_mix_player.InstanceService/ListInstances',
          ($1.ListInstancesRequest value) => value.writeToBuffer(),
          $0.ListInstancesResponse.fromBuffer);
  static final _$getProfile =
      $grpc.ClientMethod<$1.GetProfileRequest, $0.InstanceProfile>(
          '/omni_mix_player.InstanceService/GetProfile',
          ($1.GetProfileRequest value) => value.writeToBuffer(),
          $0.InstanceProfile.fromBuffer);
  static final _$updateProfile =
      $grpc.ClientMethod<$1.UpdateProfileRequest, $1.UpdateProfileResponse>(
          '/omni_mix_player.InstanceService/UpdateProfile',
          ($1.UpdateProfileRequest value) => value.writeToBuffer(),
          $1.UpdateProfileResponse.fromBuffer);
  static final _$getStatus =
      $grpc.ClientMethod<$1.GetInstanceStatusRequest, $0.PlaybackStatus>(
          '/omni_mix_player.InstanceService/GetStatus',
          ($1.GetInstanceStatusRequest value) => value.writeToBuffer(),
          $0.PlaybackStatus.fromBuffer);
  static final _$archiveInstance =
      $grpc.ClientMethod<$1.ArchiveInstanceRequest, $1.ArchiveInstanceResponse>(
          '/omni_mix_player.InstanceService/ArchiveInstance',
          ($1.ArchiveInstanceRequest value) => value.writeToBuffer(),
          $1.ArchiveInstanceResponse.fromBuffer);
  static final _$listArchives =
      $grpc.ClientMethod<$1.ListArchivesRequest, $1.ListArchivesResponse>(
          '/omni_mix_player.InstanceService/ListArchives',
          ($1.ListArchivesRequest value) => value.writeToBuffer(),
          $1.ListArchivesResponse.fromBuffer);
  static final _$getArchive =
      $grpc.ClientMethod<$1.GetArchiveRequest, $0.InstanceProfile>(
          '/omni_mix_player.InstanceService/GetArchive',
          ($1.GetArchiveRequest value) => value.writeToBuffer(),
          $0.InstanceProfile.fromBuffer);
  static final _$deleteArchive =
      $grpc.ClientMethod<$1.DeleteArchiveRequest, $1.DeleteArchiveResponse>(
          '/omni_mix_player.InstanceService/DeleteArchive',
          ($1.DeleteArchiveRequest value) => value.writeToBuffer(),
          $1.DeleteArchiveResponse.fromBuffer);
  static final _$inheritFromArchive = $grpc.ClientMethod<
          $1.InheritFromArchiveRequest, $1.InheritFromArchiveResponse>(
      '/omni_mix_player.InstanceService/InheritFromArchive',
      ($1.InheritFromArchiveRequest value) => value.writeToBuffer(),
      $1.InheritFromArchiveResponse.fromBuffer);
}

@$pb.GrpcServiceName('omni_mix_player.InstanceService')
abstract class InstanceServiceBase extends $grpc.Service {
  $core.String get $name => 'omni_mix_player.InstanceService';

  InstanceServiceBase() {
    $addMethod($grpc.ServiceMethod<$0.InstanceConnectRequest,
            $0.InstanceConnectResponse>(
        'Connect',
        connect_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.InstanceConnectRequest.fromBuffer(value),
        ($0.InstanceConnectResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.InstanceHeartbeatRequest,
            $0.InstanceHeartbeatResponse>(
        'Heartbeat',
        heartbeat_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.InstanceHeartbeatRequest.fromBuffer(value),
        ($0.InstanceHeartbeatResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$0.InstanceDisconnectRequest,
            $0.InstanceDisconnectResponse>(
        'Disconnect',
        disconnect_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $0.InstanceDisconnectRequest.fromBuffer(value),
        ($0.InstanceDisconnectResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.DeleteInstanceRequest,
            $1.DeleteInstanceResponse>(
        'DeleteInstance',
        deleteInstance_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.DeleteInstanceRequest.fromBuffer(value),
        ($1.DeleteInstanceResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.ListInstancesRequest, $0.ListInstancesResponse>(
            'ListInstances',
            listInstances_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.ListInstancesRequest.fromBuffer(value),
            ($0.ListInstancesResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetProfileRequest, $0.InstanceProfile>(
        'GetProfile',
        getProfile_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $1.GetProfileRequest.fromBuffer(value),
        ($0.InstanceProfile value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.UpdateProfileRequest, $1.UpdateProfileResponse>(
            'UpdateProfile',
            updateProfile_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.UpdateProfileRequest.fromBuffer(value),
            ($1.UpdateProfileResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.GetInstanceStatusRequest, $0.PlaybackStatus>(
            'GetStatus',
            getStatus_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.GetInstanceStatusRequest.fromBuffer(value),
            ($0.PlaybackStatus value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.ArchiveInstanceRequest,
            $1.ArchiveInstanceResponse>(
        'ArchiveInstance',
        archiveInstance_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.ArchiveInstanceRequest.fromBuffer(value),
        ($1.ArchiveInstanceResponse value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.ListArchivesRequest, $1.ListArchivesResponse>(
            'ListArchives',
            listArchives_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.ListArchivesRequest.fromBuffer(value),
            ($1.ListArchivesResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.GetArchiveRequest, $0.InstanceProfile>(
        'GetArchive',
        getArchive_Pre,
        false,
        false,
        ($core.List<$core.int> value) => $1.GetArchiveRequest.fromBuffer(value),
        ($0.InstanceProfile value) => value.writeToBuffer()));
    $addMethod(
        $grpc.ServiceMethod<$1.DeleteArchiveRequest, $1.DeleteArchiveResponse>(
            'DeleteArchive',
            deleteArchive_Pre,
            false,
            false,
            ($core.List<$core.int> value) =>
                $1.DeleteArchiveRequest.fromBuffer(value),
            ($1.DeleteArchiveResponse value) => value.writeToBuffer()));
    $addMethod($grpc.ServiceMethod<$1.InheritFromArchiveRequest,
            $1.InheritFromArchiveResponse>(
        'InheritFromArchive',
        inheritFromArchive_Pre,
        false,
        false,
        ($core.List<$core.int> value) =>
            $1.InheritFromArchiveRequest.fromBuffer(value),
        ($1.InheritFromArchiveResponse value) => value.writeToBuffer()));
  }

  $async.Future<$0.InstanceConnectResponse> connect_Pre($grpc.ServiceCall $call,
      $async.Future<$0.InstanceConnectRequest> $request) async {
    return connect($call, await $request);
  }

  $async.Future<$0.InstanceConnectResponse> connect(
      $grpc.ServiceCall call, $0.InstanceConnectRequest request);

  $async.Future<$0.InstanceHeartbeatResponse> heartbeat_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.InstanceHeartbeatRequest> $request) async {
    return heartbeat($call, await $request);
  }

  $async.Future<$0.InstanceHeartbeatResponse> heartbeat(
      $grpc.ServiceCall call, $0.InstanceHeartbeatRequest request);

  $async.Future<$0.InstanceDisconnectResponse> disconnect_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$0.InstanceDisconnectRequest> $request) async {
    return disconnect($call, await $request);
  }

  $async.Future<$0.InstanceDisconnectResponse> disconnect(
      $grpc.ServiceCall call, $0.InstanceDisconnectRequest request);

  $async.Future<$1.DeleteInstanceResponse> deleteInstance_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.DeleteInstanceRequest> $request) async {
    return deleteInstance($call, await $request);
  }

  $async.Future<$1.DeleteInstanceResponse> deleteInstance(
      $grpc.ServiceCall call, $1.DeleteInstanceRequest request);

  $async.Future<$0.ListInstancesResponse> listInstances_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.ListInstancesRequest> $request) async {
    return listInstances($call, await $request);
  }

  $async.Future<$0.ListInstancesResponse> listInstances(
      $grpc.ServiceCall call, $1.ListInstancesRequest request);

  $async.Future<$0.InstanceProfile> getProfile_Pre($grpc.ServiceCall $call,
      $async.Future<$1.GetProfileRequest> $request) async {
    return getProfile($call, await $request);
  }

  $async.Future<$0.InstanceProfile> getProfile(
      $grpc.ServiceCall call, $1.GetProfileRequest request);

  $async.Future<$1.UpdateProfileResponse> updateProfile_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.UpdateProfileRequest> $request) async {
    return updateProfile($call, await $request);
  }

  $async.Future<$1.UpdateProfileResponse> updateProfile(
      $grpc.ServiceCall call, $1.UpdateProfileRequest request);

  $async.Future<$0.PlaybackStatus> getStatus_Pre($grpc.ServiceCall $call,
      $async.Future<$1.GetInstanceStatusRequest> $request) async {
    return getStatus($call, await $request);
  }

  $async.Future<$0.PlaybackStatus> getStatus(
      $grpc.ServiceCall call, $1.GetInstanceStatusRequest request);

  $async.Future<$1.ArchiveInstanceResponse> archiveInstance_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.ArchiveInstanceRequest> $request) async {
    return archiveInstance($call, await $request);
  }

  $async.Future<$1.ArchiveInstanceResponse> archiveInstance(
      $grpc.ServiceCall call, $1.ArchiveInstanceRequest request);

  $async.Future<$1.ListArchivesResponse> listArchives_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.ListArchivesRequest> $request) async {
    return listArchives($call, await $request);
  }

  $async.Future<$1.ListArchivesResponse> listArchives(
      $grpc.ServiceCall call, $1.ListArchivesRequest request);

  $async.Future<$0.InstanceProfile> getArchive_Pre($grpc.ServiceCall $call,
      $async.Future<$1.GetArchiveRequest> $request) async {
    return getArchive($call, await $request);
  }

  $async.Future<$0.InstanceProfile> getArchive(
      $grpc.ServiceCall call, $1.GetArchiveRequest request);

  $async.Future<$1.DeleteArchiveResponse> deleteArchive_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.DeleteArchiveRequest> $request) async {
    return deleteArchive($call, await $request);
  }

  $async.Future<$1.DeleteArchiveResponse> deleteArchive(
      $grpc.ServiceCall call, $1.DeleteArchiveRequest request);

  $async.Future<$1.InheritFromArchiveResponse> inheritFromArchive_Pre(
      $grpc.ServiceCall $call,
      $async.Future<$1.InheritFromArchiveRequest> $request) async {
    return inheritFromArchive($call, await $request);
  }

  $async.Future<$1.InheritFromArchiveResponse> inheritFromArchive(
      $grpc.ServiceCall call, $1.InheritFromArchiveRequest request);
}
