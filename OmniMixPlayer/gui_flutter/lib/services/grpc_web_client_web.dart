/// Web-safe gRPC-Web client using package:http (BrowserClient XHR).
/// On web, XHR sends headers+body atomically — no Content-Length race.
/// Mirrors the C++ omni_pcm_control.cpp framing logic.

import 'dart:typed_data';

import 'package:http/http.dart' as http;
import 'package:protobuf/protobuf.dart' as $pb;

class GrpcWebTransport {
  final String _baseUrl;
  final http.Client _http;

  GrpcWebTransport({
    required String host,
    required int port,
    http.Client? client,
  })  : _baseUrl = 'http://$host:$port',
        _http = client ?? http.Client();

  Future<T> unary<T extends $pb.GeneratedMessage>(
    String path,
    $pb.GeneratedMessage request,
    T Function(List<int>) fromBuffer, {
    Duration timeout = const Duration(seconds: 6),
  }) async {
    final frame = _buildFrame(request.writeToBuffer());
    final resp = await _http
        .post(
          Uri.parse('$_baseUrl$path'),
          headers: {
            'content-type': 'application/grpc-web+proto',
            'x-grpc-web': '1',
            'x-user-agent': 'omni-flutter-web',
          },
          body: frame,
        )
        .timeout(timeout);

    if (resp.statusCode < 200 || resp.statusCode >= 300) {
      throw GrpcWebException('HTTP ${resp.statusCode}');
    }
    return _parseResponse(resp.bodyBytes, fromBuffer);
  }

  static Uint8List _buildFrame(List<int> payload) {
    final len = payload.length;
    final frame = Uint8List(5 + len);
    frame[0] = 0;
    frame[1] = (len >> 24) & 0xff;
    frame[2] = (len >> 16) & 0xff;
    frame[3] = (len >> 8) & 0xff;
    frame[4] = len & 0xff;
    frame.setAll(5, payload);
    return frame;
  }

  static T _parseResponse<T extends $pb.GeneratedMessage>(
    List<int> body,
    T Function(List<int>) fromBuffer,
  ) {
    int offset = 0;
    while (offset + 5 <= body.length) {
      final flags = body[offset];
      final len = (body[offset + 1] << 24) |
          (body[offset + 2] << 16) |
          (body[offset + 3] << 8) |
          body[offset + 4];
      offset += 5;
      if (offset + len > body.length) {
        throw GrpcWebException('Malformed gRPC-Web response frame');
      }
      if ((flags & 0x80) == 0) {
        return fromBuffer(body.sublist(offset, offset + len));
      }
      offset += len;
    }
    throw GrpcWebException('No protobuf message in response');
  }

  void close() => _http.close();
}

class GrpcWebException implements Exception {
  final String message;
  GrpcWebException(this.message);
  @override
  String toString() => 'GrpcWebException: $message';
}
