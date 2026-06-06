import '../models/mod_manifest.dart';
import 'chill_patcher_mod.dart';
import 'fh6_omni_bridge_mod.dart';

final List<ModDeclaration> registeredMods = [
  ChillPatcherMod(),
  const Fh6OmniBridgeMod(),
];
