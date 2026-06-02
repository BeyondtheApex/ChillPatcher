import 'dart:io';

import '../media_control_service.dart';
import 'media_control_service_web.dart' as audio_impl;

MediaControlService createMediaControlService() {
  if (!Platform.isWindows) return NoopMediaControlService();
  return audio_impl.createMediaControlService();
}
