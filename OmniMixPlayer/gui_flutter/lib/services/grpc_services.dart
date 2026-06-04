/// gRPC service client aggregator.

import 'dart:async';
import 'package:grpc/grpc.dart';
import '../generated/omni_mix_player/services/library.pbgrpc.dart';
import '../generated/omni_mix_player/services/playback.pbgrpc.dart';
import '../generated/omni_mix_player/services/instance.pbgrpc.dart';
import '../generated/omni_mix_player/services/instance.pb.dart';
import 'grpc_channel_native.dart'
    if (dart.library.js_interop) 'grpc_channel_web.dart';

class GrpcServices {
  final String _host;
  final int _port;
  dynamic _channel;
  bool _disposed = false;

  late final LibraryServiceClient library;
  late final PlaybackServiceClient playback;
  late final InstanceServiceClient instance;

  GrpcServices({required String host, required int port})
    : _host = host,
      _port = port {
    _connect();
  }

  String get host => _host;
  int get port => _port;

  void _connect() {
    _channel?.shutdown();
    _channel = createGrpcChannel(_host, _port) as ClientChannel;
    final ch = _channel!;
    library = LibraryServiceClient(ch);
    playback = PlaybackServiceClient(ch);
    instance = InstanceServiceClient(ch);
  }

  Future<bool> checkHealth({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    if (_disposed) return false;
    try {
      await instance
          .listInstances(
            ListInstancesRequest(),
            options: CallOptions(timeout: timeout),
          )
          .timeout(timeout);
      return true;
    } catch (_) {
      return false;
    }
  }

  void reconnect() {
    if (_disposed) return;
    _connect();
  }

  void dispose() {
    _disposed = true;
    _channel?.shutdown();
  }
}
