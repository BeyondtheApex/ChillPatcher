import 'dart:async';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../models/mod_manifest.dart';
import '../models/mod_enums.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../services/mod_deployment_service.dart'
    if (dart.library.js_interop) '../stubs/mod_deployment_service_web.dart';

class GameIntegrationPage extends StatefulWidget {
  final AppState state;

  const GameIntegrationPage({super.key, required this.state});

  @override
  State<GameIntegrationPage> createState() => _GameIntegrationPageState();
}

class _GameIntegrationPageState extends State<GameIntegrationPage> {
  String? _activeGameId;

  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    super.dispose();
  }

  void _onStateChanged() {
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    final st = widget.state;

    final busy = st.backendBusy || st.serviceBusy;
    if (busy) {
      return _LoadingPage(
        icon: Icons.settings_input_component_rounded,
        title: l10n.serviceStarting,
        message: l10n.serviceStartingMessage,
      );
    }

    if (!st.backendOnline) {
      return _LoadingPage(
        icon: Icons.cloud_off_rounded,
        title: l10n.serviceNotConnected,
        message: l10n.waitingForBackendMod,
      );
    }

    if (_activeGameId == null) {
      return _buildGameList(context, l10n, cs);
    }

    final game = gameCatalog.firstWhere((g) => g.id == _activeGameId);
    return _buildGameDetail(context, l10n, cs, game);
  }

  // ═══════════════════════════════════════════════════════════
  //  Game List — image cards with fallback icon
  // ═══════════════════════════════════════════════════════════

  Widget _buildGameList(
    BuildContext context,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.gameIntegration,
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.bold,
                color: cs.onSurface,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              l10n.welcomeHint,
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: cs.onSurfaceVariant),
            ),
            const SizedBox(height: 24),
            Expanded(
              child: ListView.builder(
                itemCount: gameCatalog.length,
                itemBuilder: (context, index) {
                  return _buildGameCard(gameCatalog[index], l10n, cs);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildGameCard(
    GameDeclaration game,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    return Card(
      clipBehavior: Clip.antiAlias,
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
      ),
      child: InkWell(
        onTap: () => setState(() => _activeGameId = game.id),
        child: SizedBox(
          height: 120,
          child: Row(
            children: [
              // Cover image — fills the full card height
              SizedBox(
                width: 160,
                child: ClipRRect(
                  borderRadius: const BorderRadius.only(
                    topLeft: Radius.circular(16),
                    bottomLeft: Radius.circular(16),
                  ),
                  child: game.coverAssetPath != null
                      ? Image.asset(
                          game.coverAssetPath!,
                          width: 160,
                          height: 120,
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) =>
                              _coverFallback(game, cs),
                        )
                      : _coverFallback(game, cs),
                ),
              ),
              const SizedBox(width: 20),
              // Content
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        game.name,
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 8),
                      _buildCompactStatusBadge(game, l10n, cs),
                      if (game.websiteUrl != null) ...[
                        const SizedBox(height: 8),
                        InkWell(
                          onTap: () => _openUrl(game.websiteUrl!),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                Icons.open_in_new,
                                size: 14,
                                color: cs.primary,
                              ),
                              const SizedBox(width: 4),
                              Text(
                                l10n.websiteLink,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: cs.primary,
                                  decoration: TextDecoration.underline,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Padding(
                padding: const EdgeInsets.only(right: 20),
                child: Icon(
                  Icons.arrow_forward_ios,
                  size: 16,
                  color: cs.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _coverFallback(GameDeclaration game, ColorScheme cs) {
    return Container(
      width: 160,
      height: 120,
      color: cs.primaryContainer,
      child: Center(
        child: Icon(
          Icons.sports_esports,
          size: 48,
          color: cs.onPrimaryContainer,
        ),
      ),
    );
  }

  void _openUrl(String url) {
    try {
      if (Platform.isWindows) {
        Process.run('cmd', ['/c', 'start', url], runInShell: true);
      } else {
        Process.run('xdg-open', [url], runInShell: true);
      }
    } catch (_) {}
  }

  // ═══════════════════════════════════════════════════════════
  //  Game Detail — scrollable top + fixed log bottom
  // ═══════════════════════════════════════════════════════════

  Widget _buildGameDetail(
    BuildContext context,
    AppLocalizations l10n,
    ColorScheme cs,
    GameDeclaration game,
  ) {
    final st = widget.state;
    final gamePath = st.gamePathFor(game.id);
    final frameworks = frameworksForGame(game);
    final isPathEmpty = gamePath.isEmpty;
    final isValidPath =
        !isPathEmpty &&
        ModDeploymentService.verifyGameDirectory(gamePath, game);
    final hasUnmanagedFramework = frameworks.any(
      (framework) =>
          st.frameworkStatusFor(game.id, framework.id) ==
          BepInExStatus.unmanaged,
    );

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: cs.onSurface),
          onPressed: () => setState(() => _activeGameId = null),
        ),
        title: Text(
          game.name,
          style: TextStyle(fontWeight: FontWeight.bold, color: cs.onSurface),
        ),
      ),
      body: LayoutBuilder(
        builder: (context, constraints) {
          return Column(
            children: [
              Expanded(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(24, 8, 24, 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _buildPathSelector(
                        game,
                        gamePath,
                        isPathEmpty,
                        isValidPath,
                        l10n,
                        cs,
                        st,
                      ),
                      const SizedBox(height: 20),
                      if (isValidPath) ...[
                        if (hasUnmanagedFramework) ...[
                          _unmanagedWarning(cs, l10n),
                          const SizedBox(height: 20),
                        ],
                        _buildInstanceStatusCard(game, st, l10n, cs),
                        const SizedBox(height: 20),
                        _buildComponentCards(game, st, l10n, cs),
                      ],
                    ],
                  ),
                ),
              ),
              if (isValidPath &&
                  (st.deploymentLogs.isNotEmpty || st.deploymentBusy))
                _buildLogArea(st, cs, l10n),
            ],
          );
        },
      ),
    );
  }

  // ── Path selector with open-folder button ──

  Widget _buildPathSelector(
    GameDeclaration game,
    String gamePath,
    bool isPathEmpty,
    bool isValidPath,
    AppLocalizations l10n,
    ColorScheme cs,
    AppState st,
  ) {
    return Card(
      elevation: 0,
      color: cs.surfaceContainerLow,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.selectGameDir,
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      color: cs.surfaceContainerHigh,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                        color: isPathEmpty
                            ? cs.outlineVariant
                            : (isValidPath ? cs.primary : cs.error),
                      ),
                    ),
                    child: Text(
                      isPathEmpty ? l10n.chooseFolder : gamePath,
                      style: TextStyle(
                        fontSize: 13,
                        color: isPathEmpty ? cs.onSurfaceVariant : cs.onSurface,
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                if (!isPathEmpty)
                  SizedBox(
                    width: 40,
                    height: 40,
                    child: IconButton(
                      icon: const Icon(Icons.folder_open_rounded, size: 20),
                      style: IconButton.styleFrom(
                        backgroundColor: cs.surfaceContainerHigh,
                        foregroundColor: cs.onSurfaceVariant,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      onPressed: () => _openFolder(gamePath),
                      tooltip: l10n.openFolder,
                    ),
                  ),
                const SizedBox(width: 8),
                ElevatedButton.icon(
                  onPressed: st.deploymentBusy
                      ? null
                      : () async {
                          final path =
                              await ModDeploymentService.selectDirectory();
                          if (path != null) {
                            await st.setGamePath(path, gameId: game.id);
                          }
                        },
                  icon: const Icon(Icons.folder_open),
                  label: Text(l10n.chooseFolder),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 14,
                    ),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                ),
              ],
            ),
            if (!isPathEmpty && !isValidPath) ...[
              const SizedBox(height: 8),
              Text(
                '${l10n.invalidGameDir} (${game.exeName})',
                style: TextStyle(
                  fontSize: 12,
                  color: cs.error,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  void _openFolder(String path) {
    try {
      if (Platform.isWindows) {
        Process.run('explorer', [path], runInShell: true);
      }
    } catch (_) {}
  }

  // ── Unmanaged warning ──

  Widget _unmanagedWarning(ColorScheme cs, AppLocalizations l10n) {
    return Card(
      elevation: 0,
      color: Colors.orange.withOpacity(0.08),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: Colors.orange.withOpacity(0.4)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Icon(
              Icons.warning_amber_rounded,
              color: Colors.orange,
              size: 24,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                l10n.unmanagedWarning,
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xFFD84315),
                  fontWeight: FontWeight.w500,
                  height: 1.4,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ── Instance status card ──

  Widget _buildInstanceStatusCard(
    GameDeclaration game,
    AppState st,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    final modIds = modsForGame(game).map((m) => m.id).toSet();
    final instances = st.playbackInstances
        .where((i) => modIds.contains(i.modId))
        .toList();
    if (instances.isEmpty) return const SizedBox.shrink();

    return Card(
      elevation: 0,
      color: cs.surfaceContainerLow,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.link, size: 18),
                const SizedBox(width: 8),
                Text(
                  l10n.omnimixInstance,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                    color: cs.onSurface,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ...instances.map((inst) {
              final online = inst.isOnline;
              return Padding(
                padding: const EdgeInsets.only(bottom: 6),
                child: Row(
                  children: [
                    Icon(
                      online ? Icons.circle : Icons.circle_outlined,
                      size: 10,
                      color: online ? Colors.green : Colors.grey,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        online
                            ? l10n.instanceOnline(inst.id)
                            : l10n.instanceOffline(inst.id),
                        style: TextStyle(
                          fontSize: 12,
                          color: online ? Colors.green : cs.outline,
                        ),
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 6,
                        vertical: 2,
                      ),
                      decoration: BoxDecoration(
                        color: st.canControlInstance(inst.id)
                            ? Colors.blue.withAlpha(30)
                            : Colors.orange.withAlpha(30),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        inst.kind.name,
                        style: TextStyle(
                          fontSize: 11,
                          color: st.canControlInstance(inst.id)
                              ? Colors.blue
                              : Colors.orange,
                        ),
                      ),
                    ),
                    const SizedBox(width: 4),
                    IconButton(
                      icon: const Icon(Icons.archive, size: 18),
                      tooltip: l10n.archiveInstanceTooltip,
                      onPressed: () => _showArchiveInstanceDialog(inst),
                    ),
                  ],
                ),
              );
            }),
          ],
        ),
      ),
    );
  }

  // ── Component cards: icon + status + version + icon-only actions ──

  Widget _buildComponentCards(
    GameDeclaration game,
    AppState st,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    final cards = <Widget>[
      for (final framework in frameworksForGame(game))
        _buildResponsiveComponentCard(
          child: _buildComponentCard(
            icon: Icons.build_rounded,
            title: framework.name,
            statusBadge: _buildDetailedStatusBadge(
              st.frameworkStatusFor(game.id, framework.id),
              framework,
              l10n,
              cs,
            ),
            versionInfo: _buildVersionInfo(framework.id, framework.version, cs),
            actions: _buildFrameworkActions(st, l10n, cs, game, framework),
          ),
        ),
      for (final mod in modsForGame(game))
        _buildResponsiveComponentCard(
          child: _buildComponentCard(
            icon: Icons.extension_rounded,
            title: mod.name,
            statusBadge: _buildModDetailedStatusBadge(
              st.modStatusFor(game.id, mod.id),
              l10n,
              cs,
            ),
            versionInfo: _buildVersionInfo(mod.id, mod.version, cs),
            actions: _buildModActions(st, l10n, cs, game, mod),
            onSettingsPressed: mod.hasSettings
                ? () => _showModSettings(context, mod, game.id)
                : null,
          ),
        ),
    ];

    if (cards.isEmpty) return const SizedBox.shrink();

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth >= 760
            ? (constraints.maxWidth - 16) / 2
            : constraints.maxWidth;
        return Wrap(
          spacing: 16,
          runSpacing: 16,
          children: [
            for (final card in cards) SizedBox(width: width, child: card),
          ],
        );
      },
    );
  }

  Widget _buildResponsiveComponentCard({required Widget child}) {
    return child;
  }

  Widget _buildComponentCard({
    required IconData icon,
    required String title,
    required Widget statusBadge,
    required Widget? versionInfo,
    required Widget actions,
    VoidCallback? onSettingsPressed,
  }) {
    final cs = Theme.of(context).colorScheme;
    return Card(
      elevation: 0,
      color: cs.surfaceContainerLow,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, size: 20, color: cs.primary),
                const SizedBox(width: 8),
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const Spacer(),
                if (onSettingsPressed != null)
                  IconButton(
                    icon: const Icon(Icons.settings, size: 18),
                    onPressed: onSettingsPressed,
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    tooltip: AppLocalizations.of(context).settings,
                  ),
              ],
            ),
            const SizedBox(height: 8),
            statusBadge,
            if (versionInfo != null) ...[
              const SizedBox(height: 4),
              versionInfo,
            ],
            const SizedBox(height: 12),
            actions,
          ],
        ),
      ),
    );
  }

  void _showModSettings(
    BuildContext context,
    ModDeclaration mod,
    String gameId,
  ) {
    final currentSettings = widget.state.settingsForMod(mod.id);
    showDialog(
      context: context,
      builder: (context) {
        return mod.buildSettingsWidget(context, currentSettings, (
          newSettings,
        ) async {
          await widget.state.saveModSettings(mod.id, newSettings);
          if (widget.state.modStatusFor(gameId, mod.id) ==
              ModStatus.installed) {
            await widget.state.redeployModSettingsOnly(
              gameId: gameId,
              modId: mod.id,
            );
          }
        });
      },
    );
  }

  Widget? _buildVersionInfo(
    String id,
    String availableVersion,
    ColorScheme cs,
  ) {
    final installed = ModDeploymentService.getInstalledVersion(id);
    final l = AppLocalizations.of(context);

    if (installed == null) {
      return Text(
        l.availableVersion(availableVersion),
        style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
      );
    }

    if (installed == availableVersion) {
      return Text(
        l.installed(installed),
        style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
      );
    } else {
      return Text(
        '${l.installed(installed)} (${l.latestVersion(availableVersion)})',
        style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
      );
    }
  }

  // ── Icon-only action buttons ──

  Widget _buildFrameworkActions(
    AppState st,
    AppLocalizations l10n,
    ColorScheme cs,
    GameDeclaration game,
    FrameworkDeclaration framework,
  ) {
    if (st.deploymentBusy) {
      return const SizedBox(
        width: 20,
        height: 20,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }

    switch (st.frameworkStatusFor(game.id, framework.id)) {
      case BepInExStatus.notInstalled:
        return _iconAction(
          icon: Icons.download_rounded,
          label: 'Install ${framework.name}',
          color: cs.primaryContainer,
          fg: cs.onPrimaryContainer,
          onPressed: () => showDialog(
            context: context,
            barrierDismissible: false,
            builder: (context) => ModInstallWizard(
              state: st,
              gameId: game.id,
              frameworkId: framework.id,
            ),
          ),
        );
      case BepInExStatus.managed:
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            _iconAction(
              icon: Icons.refresh_rounded,
              label: 'Reinstall ${framework.name}',
              color: cs.primaryContainer,
              fg: cs.onPrimaryContainer,
              onPressed: () => showDialog(
                context: context,
                barrierDismissible: false,
                builder: (context) => ModInstallWizard(
                  state: st,
                  gameId: game.id,
                  frameworkId: framework.id,
                ),
              ),
            ),
            const SizedBox(width: 8),
            _iconAction(
              icon: Icons.delete_rounded,
              label: 'Uninstall ${framework.name}',
              color: cs.errorContainer,
              fg: cs.onErrorContainer,
              onPressed: () => st.uninstallFramework(
                gameId: game.id,
                frameworkId: framework.id,
              ),
            ),
          ],
        );
      case BepInExStatus.unmanaged:
        return _iconAction(
          icon: Icons.lock_outline_rounded,
          label: 'Install ${framework.name}',
          color: cs.surfaceContainerHighest,
          fg: cs.onSurfaceVariant,
          onPressed: null,
        );
    }
  }

  Widget _buildModActions(
    AppState st,
    AppLocalizations l10n,
    ColorScheme cs,
    GameDeclaration game,
    ModDeclaration mod,
  ) {
    if (st.deploymentBusy) {
      return const SizedBox(
        width: 20,
        height: 20,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }

    final requiredFramework = mod.targetFramework == null
        ? null
        : frameworkById(mod.targetFramework!);
    final frameworkPresent =
        requiredFramework == null ||
        st.frameworkStatusFor(game.id, requiredFramework.id) !=
            BepInExStatus.notInstalled;

    if (!frameworkPresent) {
      return _iconAction(
        icon: Icons.add_rounded,
        label: l10n.installMod,
        color: cs.surfaceContainerHighest,
        fg: cs.onSurfaceVariant,
        onPressed: null,
      );
    }

    switch (st.modStatusFor(game.id, mod.id)) {
      case ModStatus.notInstalled:
        return _iconAction(
          icon: Icons.download_rounded,
          label: l10n.installMod,
          color: cs.primaryContainer,
          fg: cs.onPrimaryContainer,
          onPressed: () => _installWithArchiveChoice(game.id, mod.id),
        );
      case ModStatus.installed:
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            _iconAction(
              icon: Icons.refresh_rounded,
              label: l10n.reinstallMod,
              color: cs.primaryContainer,
              fg: cs.onPrimaryContainer,
              onPressed: () => _installWithArchiveChoice(game.id, mod.id),
            ),
            const SizedBox(width: 8),
            _iconAction(
              icon: Icons.delete_rounded,
              label: l10n.uninstallMod,
              color: cs.errorContainer,
              fg: cs.onErrorContainer,
              onPressed: () => st.uninstallMod(gameId: game.id, modId: mod.id),
            ),
          ],
        );
    }
  }

  Widget _iconAction({
    required IconData icon,
    required String label,
    required Color color,
    required Color fg,
    required VoidCallback? onPressed,
  }) {
    return SizedBox(
      width: 40,
      height: 40,
      child: IconButton(
        icon: Icon(icon, size: 20),
        style: IconButton.styleFrom(
          backgroundColor: color,
          foregroundColor: fg,
          padding: EdgeInsets.zero,
          minimumSize: const Size(40, 40),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
        onPressed: onPressed,
        tooltip: label,
      ),
    );
  }

  // ── Fixed log area (bottom-anchored, non-scrollable container) ──

  Widget _buildLogArea(AppState st, ColorScheme cs, AppLocalizations l10n) {
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: Colors.black.withOpacity(0.85),
        border: Border(
          top: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
        ),
      ),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (st.deploymentBusy) const LinearProgressIndicator(),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
              child: Row(
                children: [
                  Text(
                    l10n.deploymentLogs,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Colors.white70,
                    ),
                  ),
                  const Spacer(),
                  if (st.deploymentLogs.isNotEmpty)
                    GestureDetector(
                      onTap: () => st.clearDeploymentLogs(),
                      child: Text(
                        l10n.clear,
                        style: TextStyle(fontSize: 11, color: Colors.white38),
                      ),
                    ),
                ],
              ),
            ),
            SizedBox(
              height: 120,
              child: ListView.builder(
                padding: const EdgeInsets.all(12),
                itemCount: st.deploymentLogs.length,
                itemBuilder: (context, index) {
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 3.0),
                    child: Text(
                      st.deploymentLogs[index],
                      style: const TextStyle(
                        color: Colors.lightGreenAccent,
                        fontFamily: 'Consolas',
                        fontSize: 12,
                      ),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ── Badges ──

  Widget _buildCompactStatusBadge(
    GameDeclaration game,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    final st = widget.state;
    final gamePath = st.gamePathFor(game.id);
    if (gamePath.isEmpty) {
      return _badge(l10n.statusNotInstalled, Colors.grey, cs);
    }

    final isValid = ModDeploymentService.verifyGameDirectory(gamePath, game);
    if (!isValid) {
      return _badge(l10n.statusNotInstalled, Colors.grey, cs);
    }

    final list = <Widget>[];

    for (final framework in frameworksForGame(game)) {
      switch (st.frameworkStatusFor(game.id, framework.id)) {
        case BepInExStatus.notInstalled:
          list.add(
            _badge(
              '${framework.name}: ${l10n.statusNotInstalled}',
              Colors.grey,
              cs,
            ),
          );
          break;
        case BepInExStatus.managed:
          list.add(
            _badge(
              '${framework.name}: ${l10n.statusManaged}',
              Colors.green,
              cs,
            ),
          );
          break;
        case BepInExStatus.unmanaged:
          list.add(
            _badge(
              '${framework.name}: ${l10n.statusUnmanaged}',
              Colors.orange,
              cs,
            ),
          );
          break;
      }
      list.add(const SizedBox(width: 8));
    }

    for (final mod in modsForGame(game)) {
      switch (st.modStatusFor(game.id, mod.id)) {
        case ModStatus.notInstalled:
          list.add(
            _badge('${mod.name}: ${l10n.statusNotInstalled}', Colors.grey, cs),
          );
          break;
        case ModStatus.installed:
          list.add(
            _badge('${mod.name}: ${l10n.modInstalled}', Colors.green, cs),
          );
          break;
      }
      list.add(const SizedBox(width: 8));
    }

    if (list.isNotEmpty) list.removeLast();
    return Wrap(spacing: 0, runSpacing: 6, children: list);
  }

  Widget _buildDetailedStatusBadge(
    BepInExStatus status,
    FrameworkDeclaration framework,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    switch (status) {
      case BepInExStatus.notInstalled:
        return _badge(l10n.statusNotInstalled, Colors.grey, cs);
      case BepInExStatus.managed:
        return _badge(
          '${l10n.statusManaged} (${framework.version})',
          Colors.green,
          cs,
        );
      case BepInExStatus.unmanaged:
        return _badge(l10n.statusUnmanaged, Colors.orange, cs);
    }
  }

  Widget _buildModDetailedStatusBadge(
    ModStatus status,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    switch (status) {
      case ModStatus.notInstalled:
        return _badge(l10n.statusNotInstalled, Colors.grey, cs);
      case ModStatus.installed:
        return _badge(l10n.modInstalled, Colors.green, cs);
    }
  }

  void _showArchiveInstanceDialog(InstanceSummary inst) {
    final l10n = AppLocalizations.of(context);
    final ctrl = TextEditingController(
      text: inst.id.isNotEmpty ? inst.id : inst.id,
    );
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l10n.archiveInstance),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(l10n.archiveInstanceHint(inst.id)),
            const SizedBox(height: 12),
            TextField(
              controller: ctrl,
              decoration: InputDecoration(
                hintText: l10n.archiveNameHint,
                border: const OutlineInputBorder(),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(l10n.cancel),
          ),
          FilledButton(
            onPressed: () {
              widget.state.archiveInstanceWithLabel(inst.id, ctrl.text);
              Navigator.pop(ctx);
            },
            child: Text(l10n.archiveAction),
          ),
        ],
      ),
    );
  }

  /// Show archive selection dialog before installing, so user can inherit settings.
  Future<void> _installWithArchiveChoice(String gameId, String modId) async {
    final l10n = AppLocalizations.of(context);
    final st = widget.state;

    // Fetch archives from backend
    await st.refreshBackendArchives();
    final archives = st.archives;

    if (!mounted) return;

    // If no archives, show dialog directly
    if (archives.isEmpty) {
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) =>
            ModInstallWizard(state: st, gameId: gameId, modId: modId),
      );
      return;
    }

    final chosenId = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l10n.inheritArchiveTitle),
        content: SizedBox(
          width: 500,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(l10n.inheritArchiveHint),
              const SizedBox(height: 12),
              Flexible(
                child: ListView.builder(
                  shrinkWrap: true,
                  itemCount: archives.length,
                  itemBuilder: (_, i) {
                    final a = archives[i];
                    final isBound = st.instanceExists(a.instanceId);
                    return Card(
                      child: ListTile(
                        dense: true,
                        leading: Icon(
                          isBound ? Icons.link : Icons.archive,
                          size: 20,
                          color: isBound ? Colors.green : Colors.grey,
                        ),
                        title: Text(
                          a.displayName,
                          style: const TextStyle(fontSize: 13),
                        ),
                        subtitle: Text(
                          isBound
                              ? l10n.archiveBoundWillCopy
                              : l10n.archiveFreeWillConsume,
                          style: const TextStyle(fontSize: 11),
                        ),
                        onTap: () => Navigator.pop(ctx, a.instanceId),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, ''),
            child: Text(l10n.skipInherit),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(l10n.cancel),
          ),
        ],
      ),
    );

    if (!mounted) return;
    if (chosenId == null) return; // cancelled
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => ModInstallWizard(
        state: st,
        gameId: gameId,
        modId: modId,
        inheritArchiveId: chosenId.isEmpty ? null : chosenId,
      ),
    );
  }

  Widget _badge(String text, Color color, ColorScheme cs) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.15),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withOpacity(0.4), width: 1),
      ),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.bold,
          color: color,
        ),
      ),
    );
  }
}

class _LoadingPage extends StatelessWidget {
  final IconData icon;
  final String title;
  final String message;

  const _LoadingPage({
    required this.icon,
    required this.title,
    required this.message,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 72, color: cs.primary.withAlpha(180)),
            const SizedBox(height: 16),
            Text(
              title,
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(message, style: TextStyle(color: cs.onSurfaceVariant)),
            const SizedBox(height: 20),
            const SizedBox(
              width: 28,
              height: 28,
              child: CircularProgressIndicator(strokeWidth: 2.5),
            ),
          ],
        ),
      ),
    );
  }
}

class ModInstallWizard extends StatefulWidget {
  final AppState state;
  final String gameId;
  final String? modId;
  final String? frameworkId;
  final String? inheritArchiveId;

  const ModInstallWizard({
    super.key,
    required this.state,
    required this.gameId,
    this.modId,
    this.frameworkId,
    this.inheritArchiveId,
  }) : assert(modId != null || frameworkId != null);

  @override
  State<ModInstallWizard> createState() => _ModInstallWizardState();
}

enum WizardStep { preparing, ready, installing, manualReady, success, failed }

class _ModInstallWizardState extends State<ModInstallWizard> {
  WizardStep _step = WizardStep.preparing;
  String? _tempDir;
  PrepResult? _prepResult;
  final List<String> _prepLogs = [];
  final List<String> _execLogs = [];
  String? _manualErrorMessage;
  Timer? _logTimer;
  int _lastLogLineCount = 0;

  @override
  void initState() {
    super.initState();
    _startStaging();
  }

  @override
  void dispose() {
    _logTimer?.cancel();
    if (_tempDir != null) {
      try {
        final d = Directory(_tempDir!);
        if (d.existsSync()) d.deleteSync(recursive: true);
      } catch (_) {}
    }
    super.dispose();
  }

  void _addPrepLog(String msg) {
    if (mounted) {
      setState(() {
        _prepLogs.add(msg);
      });
    }
  }

  Future<void> _startStaging() async {
    try {
      final gameDir = widget.state.gamePathFor(widget.gameId);
      if (widget.modId != null) {
        final mod = modById(widget.modId!);
        if (mod == null) {
          _addPrepLog('ERROR: Mod not found in catalog');
          setState(() => _step = WizardStep.failed);
          return;
        }

        final result = await ModDeploymentService.prepareInstallStaging(
          gameDir,
          mod,
          widget.state.settingsForMod(widget.modId!),
          _addPrepLog,
        );

        setState(() {
          _prepResult = result;
          _tempDir = result.tempDir;
          _step = WizardStep.ready;
        });
      } else if (widget.frameworkId != null) {
        final framework = frameworkById(widget.frameworkId!);
        if (framework == null) {
          _addPrepLog('ERROR: Framework not found');
          setState(() => _step = WizardStep.failed);
          return;
        }

        final result = await ModDeploymentService.prepareFrameworkStaging(
          gameDir,
          framework,
          _addPrepLog,
        );

        setState(() {
          _prepResult = result;
          _tempDir = result.tempDir;
          _step = WizardStep.ready;
        });
      }
    } catch (e) {
      _addPrepLog('ERROR during preparation: $e');
      setState(() => _step = WizardStep.failed);
    }
  }

  Future<void> _startExecution() async {
    setState(() {
      _step = WizardStep.installing;
      _execLogs.clear();
      _execLogs.add('[INFO] Beginning elevated execution...');
    });

    final gameDir = widget.state.gamePathFor(widget.gameId);
    if (_prepResult == null) {
      setState(() => _step = WizardStep.failed);
      return;
    }

    _lastLogLineCount = 0;
    _logTimer = Timer.periodic(const Duration(milliseconds: 200), (timer) {
      _pollInstallLog();
    });

    bool success = false;
    if (widget.modId != null) {
      final mod = modById(widget.modId!);
      if (mod == null) {
        setState(() => _step = WizardStep.failed);
        return;
      }
      success = await ModDeploymentService.executeInstallStaging(
        gameDir,
        mod,
        _prepResult!,
        (msg) {
          if (mounted) {
            setState(() {
              _execLogs.add(msg);
            });
          }
        },
        backendPort: int.tryParse(widget.state.backendPort),
      );
    } else if (widget.frameworkId != null) {
      final framework = frameworkById(widget.frameworkId!);
      if (framework == null) {
        setState(() => _step = WizardStep.failed);
        return;
      }
      success = await ModDeploymentService.executeFrameworkStaging(
        gameDir,
        framework,
        _prepResult!,
        (msg) {
          if (mounted) {
            setState(() {
              _execLogs.add(msg);
            });
          }
        },
      );
    }

    if (!success) {
      _logTimer?.cancel();
      setState(() => _step = WizardStep.failed);
      return;
    }

    final runSuccess = await ModDeploymentService.runInstallScript(
      _prepResult!.tempDir,
      (msg) {
        if (mounted) {
          setState(() {
            _execLogs.add(msg);
          });
        }
      },
    );

    _logTimer?.cancel();
    _pollInstallLog();

    if (runSuccess) {
      if (widget.modId != null) {
        await widget.state.finalizeModInstall(
          gameId: widget.gameId,
          modId: widget.modId!,
          inheritArchiveId: widget.inheritArchiveId,
        );
      } else if (widget.frameworkId != null) {
        await widget.state.finalizeFrameworkInstall(
          gameId: widget.gameId,
          frameworkId: widget.frameworkId!,
        );
      }
      setState(() => _step = WizardStep.success);
    } else {
      setState(() => _step = WizardStep.failed);
    }
  }

  void _pollInstallLog() {
    if (_tempDir == null) return;
    final logFile = File('$_tempDir/install.log');
    if (!logFile.existsSync()) return;

    try {
      final lines = logFile.readAsLinesSync();
      if (lines.length > _lastLogLineCount) {
        if (mounted) {
          setState(() {
            for (var i = _lastLogLineCount; i < lines.length; i++) {
              _execLogs.add(lines[i]);
            }
            _lastLogLineCount = lines.length;
          });
        }
      }
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final l10n = AppLocalizations.of(context);

    final String titleText;
    if (widget.modId != null) {
      final modName = modById(widget.modId!)?.name ?? widget.modId;
      titleText = '${l10n.installMod}: $modName';
    } else {
      final frameworkName =
          frameworkById(widget.frameworkId!)?.name ?? widget.frameworkId;
      titleText = 'Install $frameworkName';
    }

    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      title: Row(
        children: [
          Icon(
            _step == WizardStep.success
                ? Icons.check_circle_outline
                : _step == WizardStep.failed
                ? Icons.error_outline
                : Icons.download_rounded,
            color: _step == WizardStep.success
                ? Colors.green
                : _step == WizardStep.failed
                ? Colors.red
                : cs.primary,
          ),
          const SizedBox(width: 10),
          Expanded(child: Text(titleText)),
        ],
      ),
      content: SizedBox(
        width: 600,
        height: 450,
        child: Column(
          children: [
            Expanded(child: _buildBody(cs, l10n)),
            const SizedBox(height: 12),
            _buildActions(cs, l10n),
          ],
        ),
      ),
    );
  }

  Widget _buildBody(ColorScheme cs, AppLocalizations l10n) {
    switch (_step) {
      case WizardStep.preparing:
        return _buildLoading('Preparing files...', _prepLogs, cs);
      case WizardStep.ready:
        return _buildReviewList(cs);
      case WizardStep.installing:
        return _buildLoading(
          'Installing (Requires Administrator Privilege)...',
          _execLogs,
          cs,
        );
      case WizardStep.manualReady:
        return _buildManualReady(cs, l10n);
      case WizardStep.success:
        return _buildSuccess(cs);
      case WizardStep.failed:
        return _buildFailure(cs);
    }
  }

  Widget _buildLoading(String title, List<String> logs, ColorScheme cs) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
            const SizedBox(width: 10),
            Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        const SizedBox(height: 12),
        Expanded(
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.black.withOpacity(0.85),
              borderRadius: BorderRadius.circular(8),
            ),
            child: ListView.builder(
              itemCount: logs.length,
              itemBuilder: (context, idx) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 4.0),
                  child: Text(
                    logs[idx],
                    style: const TextStyle(
                      color: Colors.lightGreenAccent,
                      fontFamily: 'Consolas',
                      fontSize: 12,
                    ),
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildReviewList(ColorScheme cs) {
    final pr = _prepResult;
    if (pr == null) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Review the files to be processed:',
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: cs.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: 10),
        Expanded(
          child: ListView(
            children: [
              if (pr.links.isNotEmpty) ...[
                _buildGroupHeader(
                  'Links to be Created (Symlinks)',
                  Colors.blue,
                  cs,
                ),
                ...pr.links.map(
                  (f) => _buildFileRow(f, Icons.link, Colors.blue),
                ),
              ],
              if (pr.added.isNotEmpty) ...[
                _buildGroupHeader(
                  'Files to be Added/Overwritten',
                  Colors.green,
                  cs,
                ),
                ...pr.added.map(
                  (f) => _buildFileRow(f, Icons.add_box_outlined, Colors.green),
                ),
              ],
              if (pr.backups.isNotEmpty) ...[
                _buildGroupHeader(
                  'Original Files to be Backed Up',
                  Colors.orange,
                  cs,
                ),
                ...pr.backups.map(
                  (f) => _buildFileRow(f, Icons.backup_outlined, Colors.orange),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildGroupHeader(String title, Color color, ColorScheme cs) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8.0),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(
          color: color.withOpacity(0.1),
          borderRadius: BorderRadius.circular(4),
          border: Border.all(color: color.withOpacity(0.3)),
        ),
        child: Text(
          title,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: color,
            fontSize: 13,
          ),
        ),
      ),
    );
  }

  Widget _buildFileRow(String file, IconData icon, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0, horizontal: 8.0),
      child: Row(
        children: [
          Icon(icon, size: 16, color: color),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              file,
              style: const TextStyle(fontFamily: 'Consolas', fontSize: 12),
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSuccess(ColorScheme cs) {
    final isMod = widget.modId != null;
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.check_circle_rounded, size: 72, color: Colors.green),
          const SizedBox(height: 16),
          const Text(
            'Installation Completed!',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            isMod
                ? 'The mod files and symbolic links have been successfully deployed.'
                : 'The framework files and symbolic links have been successfully deployed.',
            style: TextStyle(color: cs.onSurfaceVariant),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  Widget _buildFailure(ColorScheme cs) {
    return Column(
      children: [
        const Icon(Icons.error_rounded, size: 56, color: Colors.red),
        const SizedBox(height: 12),
        const Text(
          'Installation Failed!',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: Colors.red,
          ),
        ),
        const SizedBox(height: 12),
        Expanded(
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.black.withOpacity(0.85),
              borderRadius: BorderRadius.circular(8),
            ),
            child: ListView.builder(
              itemCount: _execLogs.isNotEmpty
                  ? _execLogs.length
                  : _prepLogs.length,
              itemBuilder: (context, idx) {
                final logs = _execLogs.isNotEmpty ? _execLogs : _prepLogs;
                return Text(
                  logs[idx],
                  style: const TextStyle(
                    color: Colors.redAccent,
                    fontFamily: 'Consolas',
                    fontSize: 11,
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildActions(ColorScheme cs, AppLocalizations l10n) {
    switch (_step) {
      case WizardStep.preparing:
        return Row(
          mainAxisAlignment: MainAxisAlignment.end,
          children: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text(l10n.cancel),
            ),
          ],
        );
      case WizardStep.ready:
        return Row(
          children: [
            TextButton(
              style: TextButton.styleFrom(foregroundColor: Colors.orange),
              onPressed: () {
                _prepareManualInstallFolder();
                setState(() {
                  _step = WizardStep.manualReady;
                  _manualErrorMessage = null;
                });
              },
              child: Text(l10n.manualInstall),
            ),
            const Spacer(),
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text(l10n.cancel),
            ),
            const SizedBox(width: 8),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: cs.primary,
                foregroundColor: cs.onPrimary,
              ),
              onPressed: _startExecution,
              child: Text(l10n.confirmInstall),
            ),
          ],
        );
      case WizardStep.manualReady:
        return Row(
          children: [
            TextButton(
              onPressed: () {
                _restoreManualInstallFolder();
                setState(() => _step = WizardStep.ready);
              },
              child: Text(l10n.back),
            ),
            const Spacer(),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: cs.primary,
                foregroundColor: cs.onPrimary,
              ),
              onPressed: _checkManualInstallation,
              child: Text(l10n.checkInstallation),
            ),
          ],
        );
      case WizardStep.installing:
        return const SizedBox.shrink();
      case WizardStep.success:
      case WizardStep.failed:
        return Row(
          mainAxisAlignment: MainAxisAlignment.end,
          children: [
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: cs.primary,
                foregroundColor: cs.onPrimary,
              ),
              onPressed: () => Navigator.pop(context),
              child: Text(l10n.close),
            ),
          ],
        );
    }
  }

  Widget _buildManualReady(ColorScheme cs, AppLocalizations l10n) {
    final gameDir = widget.state.gamePathFor(widget.gameId);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          l10n.manualInstallTitle,
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        const SizedBox(height: 8),
        Text(
          l10n.manualInstallHint,
          style: TextStyle(color: cs.onSurfaceVariant, fontSize: 13),
        ),
        const SizedBox(height: 16),
        _buildPathRow(l10n.manualInstallSource, _tempDir ?? '', cs, l10n),
        const SizedBox(height: 16),
        _buildPathRow(l10n.manualInstallTarget, gameDir, cs, l10n),
        const SizedBox(height: 16),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: cs.surfaceContainerLow,
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: cs.outlineVariant.withOpacity(0.5)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(Icons.help_outline, size: 16, color: cs.primary),
                  const SizedBox(width: 8),
                  Text(
                    l10n.manualInstallGuideTitle,
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                      color: cs.primary,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                l10n.manualInstallGuideSteps,
                style: TextStyle(
                  fontSize: 12,
                  height: 1.5,
                  color: cs.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        if (_manualErrorMessage != null) ...[
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: Colors.red.withOpacity(0.15),
              borderRadius: BorderRadius.circular(6),
              border: Border.all(color: Colors.red.withOpacity(0.4)),
            ),
            child: Row(
              children: [
                const Icon(Icons.error_outline, color: Colors.red, size: 18),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _manualErrorMessage!,
                    style: const TextStyle(color: Colors.red, fontSize: 12),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
        ],
        Center(
          child: Column(
            children: [
              Icon(Icons.info_outline, size: 28, color: cs.primary),
              const SizedBox(height: 6),
              Text(
                l10n.manualInstallCheckHint,
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w500,
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPathRow(
    String label,
    String path,
    ColorScheme cs,
    AppLocalizations l10n,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12),
        ),
        const SizedBox(height: 4),
        Row(
          children: [
            Expanded(
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
                decoration: BoxDecoration(
                  color: cs.surfaceContainerLow,
                  borderRadius: BorderRadius.circular(6),
                  border: Border.all(color: cs.outlineVariant),
                ),
                child: Text(
                  path,
                  style: const TextStyle(fontFamily: 'Consolas', fontSize: 11),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ),
            const SizedBox(width: 8),
            ElevatedButton.icon(
              onPressed: () => _openFolder(path),
              icon: const Icon(Icons.folder_open_outlined, size: 14),
              label: Text(l10n.open),
            ),
          ],
        ),
      ],
    );
  }

  Future<void> _openFolder(String path) async {
    if (path.isEmpty) return;
    try {
      final winPath = path.replaceAll('/', '\\');
      await Process.run('explorer.exe', [winPath]);
    } catch (_) {}
  }

  Future<void> _checkManualInstallation() async {
    final l10n = AppLocalizations.of(context)!;
    final gameDir = widget.state.gamePathFor(widget.gameId);
    bool verified = false;

    if (widget.modId != null) {
      final mod = modById(widget.modId!);
      if (mod != null) {
        verified = mod.verifyInstallation(gameDir);
      }
    } else if (widget.frameworkId != null) {
      final framework = frameworkById(widget.frameworkId!);
      if (framework != null) {
        verified = framework.verifyInstallation(gameDir);
      }
    }

    if (verified) {
      try {
        if (widget.modId != null) {
          final mod = modById(widget.modId!);
          if (mod != null) {
            final markerFile = File('$gameDir/.omnimix_mods/${mod.id}.managed');
            markerFile.parent.createSync(recursive: true);
            markerFile.writeAsStringSync(mod.version);

            final success = await ModDeploymentService.executeInstallStaging(
              gameDir,
              mod,
              _prepResult!,
              (_) {}, // no-op logger for manual install background setup
              backendPort: int.tryParse(widget.state.backendPort),
            );

            if (success) {
              await widget.state.finalizeModInstall(
                gameId: widget.gameId,
                modId: widget.modId!,
                inheritArchiveId: widget.inheritArchiveId,
              );
              setState(() => _step = WizardStep.success);
            } else {
              setState(() {
                _manualErrorMessage = l10n.manualInstallErrDb;
              });
            }
          }
        } else if (widget.frameworkId != null) {
          final framework = frameworkById(widget.frameworkId!);
          if (framework != null) {
            final markerFile = File('$gameDir/${framework.managedMarkerFile}');
            if (framework.managedMarkerFile.isNotEmpty) {
              markerFile.parent.createSync(recursive: true);
              markerFile.writeAsStringSync(framework.version);
            }

            final success = await ModDeploymentService.executeFrameworkStaging(
              gameDir,
              framework,
              _prepResult!,
              (_) {}, // no-op logger
            );

            if (success) {
              await widget.state.finalizeFrameworkInstall(
                gameId: widget.gameId,
                frameworkId: widget.frameworkId!,
              );
              setState(() => _step = WizardStep.success);
            } else {
              setState(() {
                _manualErrorMessage = l10n.manualInstallErrFwDb;
              });
            }
          }
        }
      } catch (e) {
        setState(() {
          _manualErrorMessage = l10n.manualInstallErrRegFailed(e.toString());
        });
      }
    } else {
      setState(() {
        _manualErrorMessage = l10n.manualInstallErrVerify;
      });
    }
  }

  void _prepareManualInstallFolder() {
    final pr = _prepResult;
    if (pr == null) return;

    final tempDir = pr.tempDir;
    final version = pr.gameVersion;
    final id = widget.modId ?? widget.frameworkId;
    if (id == null) return;

    final managerBackupDir =
        '${ModDeploymentService.managerDir}/backups/$id/v$version';

    for (final relPath in pr.backups) {
      final tempBackupFile = File('$tempDir/$relPath.v$version.bak');
      final destBackupFile = File('$managerBackupDir/$relPath.v$version.bak');

      if (tempBackupFile.existsSync()) {
        if (!destBackupFile.existsSync()) {
          destBackupFile.parent.createSync(recursive: true);
          try {
            tempBackupFile.copySync(destBackupFile.path);
          } catch (_) {}
        }
        try {
          tempBackupFile.deleteSync();
        } catch (_) {}
      }
    }
  }

  void _restoreManualInstallFolder() {
    final pr = _prepResult;
    if (pr == null) return;

    final tempDir = pr.tempDir;
    final version = pr.gameVersion;
    final id = widget.modId ?? widget.frameworkId;
    if (id == null) return;

    final managerBackupDir =
        '${ModDeploymentService.managerDir}/backups/$id/v$version';

    for (final relPath in pr.backups) {
      final tempBackupFile = File('$tempDir/$relPath.v$version.bak');
      final destBackupFile = File('$managerBackupDir/$relPath.v$version.bak');

      if (destBackupFile.existsSync() && !tempBackupFile.existsSync()) {
        tempBackupFile.parent.createSync(recursive: true);
        try {
          destBackupFile.copySync(tempBackupFile.path);
        } catch (_) {}
      }
    }
  }
}
