/// Shared enums used by both native and web stubs of ModDeploymentService.
/// Must NOT import dart:io so it's compatible with web compilation.
library;

/// Status of BepInEx framework in a game directory.
enum BepInExStatus { notInstalled, managed, unmanaged }

/// Status of a mod in a game directory.
enum ModStatus { notInstalled, installed, needsUpdate }
