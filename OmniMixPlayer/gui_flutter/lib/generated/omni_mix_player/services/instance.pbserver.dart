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

import 'package:protobuf/protobuf.dart' as $pb;

import '../models/instance.pb.dart' as $0;
import 'instance.pb.dart' as $2;
import 'instance.pbjson.dart';

export 'instance.pb.dart';

abstract class InstanceServiceBase extends $pb.GeneratedService {
  $async.Future<$0.InstanceConnectResponse> connect(
      $pb.ServerContext ctx, $0.InstanceConnectRequest request);
  $async.Future<$0.InstanceHeartbeatResponse> heartbeat(
      $pb.ServerContext ctx, $0.InstanceHeartbeatRequest request);
  $async.Future<$0.InstanceDisconnectResponse> disconnect(
      $pb.ServerContext ctx, $0.InstanceDisconnectRequest request);
  $async.Future<$2.DeleteInstanceResponse> deleteInstance(
      $pb.ServerContext ctx, $2.DeleteInstanceRequest request);
  $async.Future<$0.ListInstancesResponse> listInstances(
      $pb.ServerContext ctx, $2.ListInstancesRequest request);
  $async.Future<$0.InstanceProfile> getProfile(
      $pb.ServerContext ctx, $2.GetProfileRequest request);
  $async.Future<$2.UpdateProfileResponse> updateProfile(
      $pb.ServerContext ctx, $2.UpdateProfileRequest request);
  $async.Future<$0.PlaybackStatus> getStatus(
      $pb.ServerContext ctx, $2.GetInstanceStatusRequest request);
  $async.Future<$2.ArchiveInstanceResponse> archiveInstance(
      $pb.ServerContext ctx, $2.ArchiveInstanceRequest request);
  $async.Future<$2.ListArchivesResponse> listArchives(
      $pb.ServerContext ctx, $2.ListArchivesRequest request);
  $async.Future<$0.InstanceProfile> getArchive(
      $pb.ServerContext ctx, $2.GetArchiveRequest request);
  $async.Future<$2.DeleteArchiveResponse> deleteArchive(
      $pb.ServerContext ctx, $2.DeleteArchiveRequest request);
  $async.Future<$2.InheritFromArchiveResponse> inheritFromArchive(
      $pb.ServerContext ctx, $2.InheritFromArchiveRequest request);

  $pb.GeneratedMessage createRequest($core.String methodName) {
    switch (methodName) {
      case 'Connect':
        return $0.InstanceConnectRequest();
      case 'Heartbeat':
        return $0.InstanceHeartbeatRequest();
      case 'Disconnect':
        return $0.InstanceDisconnectRequest();
      case 'DeleteInstance':
        return $2.DeleteInstanceRequest();
      case 'ListInstances':
        return $2.ListInstancesRequest();
      case 'GetProfile':
        return $2.GetProfileRequest();
      case 'UpdateProfile':
        return $2.UpdateProfileRequest();
      case 'GetStatus':
        return $2.GetInstanceStatusRequest();
      case 'ArchiveInstance':
        return $2.ArchiveInstanceRequest();
      case 'ListArchives':
        return $2.ListArchivesRequest();
      case 'GetArchive':
        return $2.GetArchiveRequest();
      case 'DeleteArchive':
        return $2.DeleteArchiveRequest();
      case 'InheritFromArchive':
        return $2.InheritFromArchiveRequest();
      default:
        throw $core.ArgumentError('Unknown method: $methodName');
    }
  }

  $async.Future<$pb.GeneratedMessage> handleCall($pb.ServerContext ctx,
      $core.String methodName, $pb.GeneratedMessage request) {
    switch (methodName) {
      case 'Connect':
        return connect(ctx, request as $0.InstanceConnectRequest);
      case 'Heartbeat':
        return heartbeat(ctx, request as $0.InstanceHeartbeatRequest);
      case 'Disconnect':
        return disconnect(ctx, request as $0.InstanceDisconnectRequest);
      case 'DeleteInstance':
        return deleteInstance(ctx, request as $2.DeleteInstanceRequest);
      case 'ListInstances':
        return listInstances(ctx, request as $2.ListInstancesRequest);
      case 'GetProfile':
        return getProfile(ctx, request as $2.GetProfileRequest);
      case 'UpdateProfile':
        return updateProfile(ctx, request as $2.UpdateProfileRequest);
      case 'GetStatus':
        return getStatus(ctx, request as $2.GetInstanceStatusRequest);
      case 'ArchiveInstance':
        return archiveInstance(ctx, request as $2.ArchiveInstanceRequest);
      case 'ListArchives':
        return listArchives(ctx, request as $2.ListArchivesRequest);
      case 'GetArchive':
        return getArchive(ctx, request as $2.GetArchiveRequest);
      case 'DeleteArchive':
        return deleteArchive(ctx, request as $2.DeleteArchiveRequest);
      case 'InheritFromArchive':
        return inheritFromArchive(ctx, request as $2.InheritFromArchiveRequest);
      default:
        throw $core.ArgumentError('Unknown method: $methodName');
    }
  }

  $core.Map<$core.String, $core.dynamic> get $json => InstanceServiceBase$json;
  $core.Map<$core.String, $core.Map<$core.String, $core.dynamic>>
      get $messageJson => InstanceServiceBase$messageJson;
}
