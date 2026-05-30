import '../models/mod_manifest.dart';
import 'chill_with_you_game.dart';
import 'forza_horizon_6_game.dart';

final List<GameDeclaration> registeredGames = [
  const ChillWithYouGame(),
  const ForzaHorizon6Game(),
];
