/// Web/WASM gRPC channel factory using gRPC-Web transport (XHR).
/// Only compiled on web platforms.
/// On native, grpc_channel_native.dart is used instead.

import 'package:grpc/grpc.dart';
import 'package:grpc/grpc_web.dart';

dynamic createGrpcChannel(String host, int port) {
  final uri = Uri(scheme: 'http', host: host, port: port);
  return GrpcWebClientChannel.xhr(uri);
}
