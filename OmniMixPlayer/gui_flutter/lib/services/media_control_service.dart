import '../generated/omni_mix_player/models/instance.pb.dart';
import 'media_control/media_control_service_stub.dart'
    if (dart.library.io) 'media_control/media_control_service_win.dart'
    if (dart.library.js_interop) 'media_control/media_control_service_web.dart'
    as impl;

typedef MediaControlAction = Future<void> Function(String instanceId);
typedef MediaControlSeekAction =
    Future<void> Function(String instanceId, Duration position);

class MediaControlCallbacks {
  final MediaControlAction play;
  final MediaControlAction pause;
  final MediaControlAction skipToNext;
  final MediaControlAction skipToPrevious;
  final MediaControlSeekAction seek;

  const MediaControlCallbacks({
    required this.play,
    required this.pause,
    required this.skipToNext,
    required this.skipToPrevious,
    required this.seek,
  });
}

class MediaControlSnapshot {
  final InstanceSummary instance;
  final String baseUrl;
  final bool canSeek;

  const MediaControlSnapshot({
    required this.instance,
    required this.baseUrl,
    this.canSeek = false,
  });
}

abstract class MediaControlService {
  Future<void> ensureInitialized(MediaControlCallbacks callbacks);
  Future<void> update(MediaControlSnapshot? snapshot);
  Future<void> dispose();
}

class NoopMediaControlService implements MediaControlService {
  @override
  Future<void> ensureInitialized(MediaControlCallbacks callbacks) async {}

  @override
  Future<void> update(MediaControlSnapshot? snapshot) async {}

  @override
  Future<void> dispose() async {}
}

MediaControlService createMediaControlService() =>
    impl.createMediaControlService();
