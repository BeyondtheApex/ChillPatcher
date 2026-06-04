import 'dart:async';
import 'dart:ffi';
import 'dart:typed_data';
import 'dart:isolate';

import 'package:ffi/ffi.dart';
import 'package:flutter_sound/flutter_sound.dart';

import 'omni_sdk_bindings.dart';

class FlutterPcmPlaybackService {
  final FlutterSoundPlayer _player = FlutterSoundPlayer();
  bool _opened = false;
  SendPort? _workerControlPort;
  ReceivePort? _audioPort;
  Completer<void>? _stopCompleter;
  Isolate? _workerIsolate;
  final _TaskQueue _queue = _TaskQueue();

  Future<void> startForInstance(String instanceId) async {
    if (instanceId.isEmpty) return;
    await stop();

    _audioPort = ReceivePort();
    final handshakePort = ReceivePort();

    _workerIsolate = await Isolate.spawn(
      _pcmWorkerEntryPoint,
      [handshakePort.sendPort, _audioPort!.sendPort, instanceId],
    );

    _workerControlPort = await handshakePort.first as SendPort;
    handshakePort.close();

    _audioPort!.listen(_handleAudioMessage);
  }

  void _handleAudioMessage(dynamic message) {
    _queue.add(() async {
      if (message is List) {
        final type = message[0] as String;
        if (type == 'format') {
          final sampleRate = message[1] as int;
          final channels = message[2] as int;
          final framesPerChunk = message[3] as int;
          final floatCount = framesPerChunk * channels;

          await _restartPlayer();

          await _player.openPlayer();
          _opened = true;
          await _player.startPlayerFromStream(
            codec: Codec.pcmFloat32,
            interleaved: true,
            numChannels: channels,
            sampleRate: sampleRate,
            bufferSize: floatCount * 4,
          );
        } else if (type == 'data') {
          final bytes = message[1] as Uint8List;
          if (_opened) {
            await _player.feedUint8FromStream(bytes);
          }
        } else if (type == 'stopped') {
          _stopCompleter?.complete();
        }
      }
    });
  }

  Future<void> stop() async {
    final port = _workerControlPort;
    _workerControlPort = null;
    final isolate = _workerIsolate;
    _workerIsolate = null;

    if (isolate != null) {
      if (port != null) {
        _stopCompleter = Completer<void>();
        port.send('stop');
        try {
          await _stopCompleter!.future.timeout(const Duration(seconds: 2));
        } catch (_) {
          isolate.kill(priority: Isolate.beforeNextEvent);
        }
        _stopCompleter = null;
      } else {
        isolate.kill(priority: Isolate.beforeNextEvent);
      }
    }

    _audioPort?.close();
    _audioPort = null;

    await _queue.add(() async {
      await _restartPlayer();
    });
  }

  Future<void> _restartPlayer() async {
    if (!_opened) return;
    _opened = false;
    try {
      await _player.stopPlayer();
    } catch (_) {}
    try {
      await _player.closePlayer();
    } catch (_) {}
  }
}

// ── Background Isolate Entry Point ───────────────────────────────

void _pcmWorkerEntryPoint(List<dynamic> initArgs) {
  final SendPort handshakePort = initArgs[0];
  final SendPort mainSendPort = initArgs[1];
  final String instanceId = initArgs[2];

  final controlPort = ReceivePort();
  handshakePort.send(controlPort.sendPort);

  bool running = true;
  controlPort.listen((message) {
    if (message == 'stop') {
      running = false;
      controlPort.close();
    }
  });

  _runPump(instanceId, mainSendPort, () => running).catchError((Object error, StackTrace stack) {
    print('Error in PCM worker pump: $error\n$stack');
    mainSendPort.send(['stopped']);
  });
}

Future<void> _runPump(
  String instanceId,
  SendPort mainSendPort,
  bool Function() isRunning,
) async {
  final mapName = 'Global\\OmniMixPlayer_PCM_$instanceId';
  final map = mapName.toNativeUtf8(allocator: calloc);
  PcmHandle pcm = nullptr;
  try {
    pcm = omniPcmOpenUtf8(map);
  } finally {
    calloc.free(map);
  }
  if (pcm.address == 0) {
    mainSendPort.send(['stopped']);
    return;
  }

  final info = calloc<OmniPcmInfo>();
  final snapshot = calloc<OmniPcmSnapshot>();
  Pointer<Float>? buffer;
  int framesPerChunk = 0;
  int channels = 0;
  int sampleRate = 0;
  int boundStreamId = -1;
  int boundFormatGeneration = -1;
  bool opened = false;

  try {
    while (isRunning()) {
      if (omniPcmGetSnapshot(pcm, snapshot) != omniPcmOk) {
        await Future<void>.delayed(const Duration(milliseconds: 100));
        continue;
      }

      final needsRebind = snapshot.ref.streamId != boundStreamId ||
          snapshot.ref.formatGeneration != boundFormatGeneration ||
          !opened;
      if (needsRebind) {
        final ready = await _bindReadyStream(pcm, isRunning);
        if (!ready || !isRunning()) {
          await Future<void>.delayed(const Duration(milliseconds: 50));
          continue;
        }
        if (omniPcmGetInfo(pcm, info) != omniPcmOk ||
            omniPcmGetSnapshot(pcm, snapshot) != omniPcmOk) {
          await Future<void>.delayed(const Duration(milliseconds: 50));
          continue;
        }

        final nextSampleRate =
            info.ref.sampleRate > 0 ? info.ref.sampleRate : 48000;
        final nextChannels = info.ref.channels > 0 ? info.ref.channels : 2;
        final nextFramesPerChunk = (nextSampleRate ~/ 50).clamp(256, 4096);
        final formatChanged = !opened ||
            nextSampleRate != sampleRate ||
            nextChannels != channels ||
            nextFramesPerChunk != framesPerChunk;

        if (formatChanged) {
          final activeBuffer = buffer;
          if (activeBuffer != null) calloc.free(activeBuffer);

          sampleRate = nextSampleRate;
          channels = nextChannels;
          framesPerChunk = nextFramesPerChunk;
          final floatCount = framesPerChunk * channels;
          buffer = calloc<Float>(floatCount);

          mainSendPort.send(['format', sampleRate, channels, framesPerChunk]);
          opened = true;
        }
        boundStreamId = snapshot.ref.streamId;
        boundFormatGeneration = snapshot.ref.formatGeneration;
      }

      if (omniPcmHasError(pcm) != 0) {
        await Future<void>.delayed(const Duration(milliseconds: 100));
        continue;
      }

      final activeBuffer = buffer;
      if (activeBuffer == null || channels <= 0 || framesPerChunk <= 0) {
        await Future<void>.delayed(const Duration(milliseconds: 20));
        continue;
      }

      final frames = omniPcmReadFrames(pcm, activeBuffer, framesPerChunk);
      if (frames > 0) {
        final bytes = activeBuffer
            .cast<Uint8>()
            .asTypedList(frames * channels * 4);
        mainSendPort.send(['data', Uint8List.fromList(bytes)]);
      } else {
        await Future<void>.delayed(const Duration(milliseconds: 20));
      }
    }
  } finally {
    final activeBuffer = buffer;
    if (activeBuffer != null) calloc.free(activeBuffer);
    calloc.free(snapshot);
    calloc.free(info);
    if (pcm.address != 0) {
      omniPcmClose(pcm);
    }
    mainSendPort.send(['stopped']);
  }
}

Future<bool> _bindReadyStream(PcmHandle pcm, bool Function() isRunning) async {
  final emptyUuid = ''.toNativeUtf8(allocator: calloc);
  try {
    for (var i = 0; isRunning() && i < 20; i++) {
      omniPcmBindCurrentStream(pcm);
      if (omniPcmIsFormatReady(pcm) != 0) return true;
      omniPcmWaitForFormatReady(pcm, emptyUuid, 100);
      await Future<void>.delayed(const Duration(milliseconds: 25));
    }
    return false;
  } finally {
    calloc.free(emptyUuid);
  }
}

class _TaskQueue {
  Future<void> _last = Future.value();

  Future<void> add(FutureOr<void> Function() task) {
    final completer = Completer<void>();
    _last.whenComplete(() async {
      try {
        await task();
      } catch (e, st) {
        print('Error in PCM playback task queue: $e\n$st');
      } finally {
        completer.complete();
      }
    });
    _last = completer.future;
    return completer.future;
  }
}
