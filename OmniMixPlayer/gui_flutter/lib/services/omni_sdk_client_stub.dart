// Web stub — OmniPcmShared SDK is native-only.
// On web, gRPC-Web transport is used instead.

class OmniSdkClient {
  OmniSdkClient({String clientId = 'flutter'}) {
    throw UnsupportedError('OmniSdkClient is native-only');
  }
  void dispose() {}
}

class OmniSdkException implements Exception {
  final String message;
  OmniSdkException(this.message);
}
