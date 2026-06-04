/// gRPC service clients — conditional backend.
/// Native: OmniPcmShared C SDK via FFI (grpc_backend_native.dart)
/// Web:    Hand-written gRPC-Web over HTTP/1 (grpc_backend_web.dart)
library grpc_services;

export 'grpc_backend_native.dart'
    if (dart.library.js_interop) 'grpc_backend_web.dart';

