/// Native gRPC channel factory using HTTP/2 transport.
/// Only compiled on native platforms (Windows, Linux, macOS).
/// On web, grpc_channel_web.dart is used instead.

import 'package:grpc/grpc.dart';

dynamic createGrpcChannel(String host, int port) {
  return ClientChannel(
    host,
    port: port,
    options: const ChannelOptions(
      credentials: ChannelCredentials.insecure(),
    ),
  );
}
