import '../models/mod_manifest.dart';

class ChillWithYouGame extends GameDeclaration {
  const ChillWithYouGame()
      : super(
          id: 'chill_with_you',
          name: 'Chill With You',
          exeName: 'Chill With You.exe',
          signatureFiles: const ['Chill With You.exe', 'Chill With You_Data'],
          supportedFrameworks: const ['bepinex_5'],
          supportedMods: const ['chill_patcher'],
          websiteUrl: 'https://store.steampowered.com/app/3361180',
          coverAssetPath: 'assets/covers/chill_with_you.png',
        );
}
