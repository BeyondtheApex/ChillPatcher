import '../models/mod_manifest.dart';

class ForzaHorizon6Game extends GameDeclaration {
  const ForzaHorizon6Game()
      : super(
          id: 'forza_horizon_6',
          name: 'Forza Horizon 6',
          exeName: 'forzahorizon6.exe',
          signatureFiles: const ['forzahorizon6.exe'],
          supportedFrameworks: const [],
          supportedMods: const ['fh6_omni_bridge'],
          websiteUrl: 'https://forza.net',
          coverAssetPath: 'assets/covers/forza_horizon_6.png',
        );
}
