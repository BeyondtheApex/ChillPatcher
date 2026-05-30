import '../models/mod_manifest.dart';
import '../services/mod_deployment_service.dart';

class ChillPatcherMod extends ModDeclaration {
  const ChillPatcherMod()
      : super(
          id: 'chill_patcher',
          name: 'ChillPatcher',
          version: '1.0.0',
          archiveName: 'ChillPatcher.zip',
          targetFramework: 'bepinex_5',
          folderName: 'ChillPatcher',
          mode: 'client',
        );

  @override
  String get version => ModDeploymentService.latestModVersion;
}
