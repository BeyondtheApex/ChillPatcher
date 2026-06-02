import 'package:audio_service/audio_service.dart';

import '../../models/node_data.dart';
import '../media_control_service.dart';

MediaControlService createMediaControlService() =>
    _AudioServiceMediaControlService();

class _AudioServiceMediaControlService implements MediaControlService {
  _OmniMixAudioHandler? _handler;
  Future<void>? _initFuture;

  @override
  Future<void> ensureInitialized(MediaControlCallbacks callbacks) async {
    if (_handler != null) return;
    _initFuture ??= _init(callbacks);
    await _initFuture;
  }

  Future<void> _init(MediaControlCallbacks callbacks) async {
    try {
      _handler = await AudioService.init(
        builder: () => _OmniMixAudioHandler(callbacks),
        config: const AudioServiceConfig(
          androidNotificationChannelId: 'com.omnimixplayer.media',
          androidNotificationChannelName: 'OmniMixPlayer',
        ),
      );
    } catch (_) {
      _handler = null;
    } finally {
      _initFuture = null;
    }
  }

  @override
  Future<void> update(MediaControlSnapshot? snapshot) async {
    final handler = _handler;
    if (handler == null) return;
    handler.update(snapshot);
  }

  @override
  Future<void> dispose() async {
    await _handler?.stop();
    _handler = null;
  }
}

class _OmniMixAudioHandler extends BaseAudioHandler {
  final MediaControlCallbacks _callbacks;
  String? _instanceId;

  _OmniMixAudioHandler(this._callbacks);

  void update(MediaControlSnapshot? snapshot) {
    if (snapshot == null) {
      _instanceId = null;
      mediaItem.add(null);
      playbackState.add(_state(playing: false, position: Duration.zero));
      return;
    }

    final instance = snapshot.instance;
    final track = instance.currentTrack;
    _instanceId = instance.id;
    mediaItem.add(_mediaItem(instance, snapshot.baseUrl));
    playbackState.add(
      _state(
        playing: instance.isPlaying,
        position: _seconds(instance.position),
        duration: _seconds(track?.duration ?? 0.0),
      ),
    );
  }

  MediaItem _mediaItem(PlaybackInstanceInfo instance, String baseUrl) {
    final track = instance.currentTrack;
    final title = track == null || track.title.isEmpty
        ? 'OmniMixPlayer'
        : track.title;
    final artist = track?.artist.isNotEmpty == true ? track!.artist : null;
    final album = instance.gameName.isNotEmpty
        ? instance.gameName
        : (track?.albumId.isNotEmpty == true ? track!.albumId : null);
    final uuid = track?.uuid ?? '';
    final artUri = uuid.isEmpty
        ? null
        : Uri.parse(
            '${baseUrl.isEmpty ? '' : baseUrl}/api/track/cover?uuid=$uuid',
          );

    return MediaItem(
      id: uuid.isEmpty ? instance.id : uuid,
      title: title,
      artist: artist,
      album: album,
      duration: _seconds(track?.duration ?? 0.0),
      artUri: artUri,
      extras: {'instanceId': instance.id},
    );
  }

  PlaybackState _state({
    required bool playing,
    required Duration position,
    Duration duration = Duration.zero,
  }) {
    return PlaybackState(
      controls: [
        MediaControl.skipToPrevious,
        playing ? MediaControl.pause : MediaControl.play,
        MediaControl.skipToNext,
      ],
      systemActions: const {MediaAction.seek},
      processingState: AudioProcessingState.ready,
      playing: playing,
      updatePosition: position,
      bufferedPosition: duration,
      speed: 1.0,
    );
  }

  Duration _seconds(double value) =>
      Duration(milliseconds: (value * 1000).round().clamp(0, 1 << 53).toInt());

  @override
  Future<void> play() async {
    final id = _instanceId;
    if (id != null) await _callbacks.play(id);
  }

  @override
  Future<void> pause() async {
    final id = _instanceId;
    if (id != null) await _callbacks.pause(id);
  }

  @override
  Future<void> skipToNext() async {
    final id = _instanceId;
    if (id != null) await _callbacks.skipToNext(id);
  }

  @override
  Future<void> skipToPrevious() async {
    final id = _instanceId;
    if (id != null) await _callbacks.skipToPrevious(id);
  }

  @override
  Future<void> seek(Duration position) async {
    final id = _instanceId;
    if (id != null) await _callbacks.seek(id, position);
  }
}
