import '../generated/omni_mix_player/models/instance.pb.dart';
import '../models/mod_manifest.dart';
import '../services/mod_deployment_service.dart';

class ChillPatcherMod extends ModDeclaration {
  // Not const — InstanceCapabilities() is not a const constructor.
  ChillPatcherMod()
    : super(
        id: 'chill_patcher',
        name: 'ChillPatcher',
        version: '1.0.0',
        archiveName: 'ChillPatcher.zip',
        targetFramework: 'bepinex_5',
        folderName: 'ChillPatcher',
        mode: 'client',
        capabilities: InstanceCapabilities(
          // Game manages its own queue and playback flow.
          // Backend provides: audio streaming, playlist sources, seek, volume, EQ.
          playlistManagement: true,
          multiplePlaylists: true,
          tagFiltering: true,
          albumFiltering: true,
          seek: true,
          volumeControl: true,
          equalizer: true,
          audioPlayback: true,
          customSystemMediaService: true,
          maxImportedPlaylists: 27,
        ),
      );

  @override
  String get version => ModDeploymentService.latestModVersion;
}
