import 'dart:io';
import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../models/mod_manifest.dart';
import '../services/mod_deployment_service.dart';

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
    final bepinexStatus = st.bepinexStatusFor(game.id);
    final modStatus = st.modStatusFor(game.id);
    final hasBepInEx = game.supportedFrameworks.contains('bepinex_5');
    final isPathEmpty = gamePath.isEmpty;
    final isValidPath =
        !isPathEmpty &&
        ModDeploymentService.verifyGameDirectory(gamePath, game);

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
                        if (hasBepInEx &&
                            bepinexStatus == BepInExStatus.unmanaged) ...[
                          _unmanagedWarning(cs, l10n),
                          const SizedBox(height: 20),
                        ],
                        _buildInstanceStatusCard(game, st, l10n, cs),
                        const SizedBox(height: 20),
                        _buildComponentCards(
                          game,
                          st,
                          l10n,
                          cs,
                          hasBepInEx,
                          bepinexStatus,
                          modStatus,
                        ),
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
    final mod = modCatalog.firstWhere((m) => m.id == game.supportedMods.first);
    final instances = st.instances.where((i) => i.modId == mod.id).toList();
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
                  'OmniMix 实例',
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
              final online = st.isInstanceOnline(inst.instanceId);
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
                            ? '在线 — ${inst.instanceId}'
                            : '离线 — ${inst.instanceId}',
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
                        color: inst.isServerMode
                            ? Colors.blue.withAlpha(30)
                            : Colors.orange.withAlpha(30),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        inst.mode,
                        style: TextStyle(
                          fontSize: 11,
                          color: inst.isServerMode
                              ? Colors.blue
                              : Colors.orange,
                        ),
                      ),
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
    bool hasBepInEx,
    BepInExStatus bepinexStatus,
    ModStatus modStatus,
  ) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (hasBepInEx)
          Expanded(
            child: _buildComponentCard(
              icon: Icons.build_rounded,
              title: 'BepInEx',
              statusBadge: _buildDetailedStatusBadge(bepinexStatus, l10n, cs),
              versionInfo: _buildVersionInfo('bepinex_5', cs),
              actions: _buildBepInExActions(st, l10n, cs, game.id),
            ),
          ),
        if (hasBepInEx) const SizedBox(width: 16),
        Expanded(
          child: _buildComponentCard(
            icon: Icons.extension_rounded,
            title: 'Mod',
            statusBadge: _buildModDetailedStatusBadge(modStatus, l10n, cs),
            versionInfo: _buildVersionInfo(
              game.supportedMods.isNotEmpty ? game.supportedMods.first : '',
              cs,
            ),
            actions: _buildModActions(st, l10n, cs, game),
          ),
        ),
      ],
    );
  }

  Widget _buildComponentCard({
    required IconData icon,
    required String title,
    required Widget statusBadge,
    required Widget? versionInfo,
    required Widget actions,
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

  Widget? _buildVersionInfo(String id, ColorScheme cs) {
    final installed = ModDeploymentService.getInstalledVersion(id);
    if (installed == null) return null;
    final l = AppLocalizations.of(context)!;
    return Text(
      l.installed(installed),
      style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
    );
  }

  // ── Icon-only action buttons ──

  Widget _buildBepInExActions(
    AppState st,
    AppLocalizations l10n,
    ColorScheme cs,
    String gameId,
  ) {
    if (st.deploymentBusy) {
      return const SizedBox(
        width: 20,
        height: 20,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }

    switch (st.bepinexStatusFor(gameId)) {
      case BepInExStatus.notInstalled:
        return _iconAction(
          icon: Icons.download_rounded,
          label: l10n.installBepInEx,
          color: cs.primaryContainer,
          fg: cs.onPrimaryContainer,
          onPressed: () => st.installBepInEx(gameId: gameId),
        );
      case BepInExStatus.managed:
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            _iconAction(
              icon: Icons.refresh_rounded,
              label: l10n.reinstallBepInEx,
              color: cs.primaryContainer,
              fg: cs.onPrimaryContainer,
              onPressed: () => st.installBepInEx(gameId: gameId),
            ),
            const SizedBox(width: 8),
            _iconAction(
              icon: Icons.delete_rounded,
              label: l10n.uninstallBepInEx,
              color: cs.errorContainer,
              fg: cs.onErrorContainer,
              onPressed: () => st.uninstallBepInEx(gameId: gameId),
            ),
          ],
        );
      case BepInExStatus.unmanaged:
        return _iconAction(
          icon: Icons.lock_outline_rounded,
          label: l10n.installBepInEx,
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
  ) {
    if (st.deploymentBusy) {
      return const SizedBox(
        width: 20,
        height: 20,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }

    final requiresBepInEx = game.supportedFrameworks.contains('bepinex_5');
    final bepinexPresent =
        !requiresBepInEx ||
        st.bepinexStatusFor(game.id) != BepInExStatus.notInstalled;

    if (!bepinexPresent) {
      return _iconAction(
        icon: Icons.add_rounded,
        label: l10n.installMod,
        color: cs.surfaceContainerHighest,
        fg: cs.onSurfaceVariant,
        onPressed: null,
      );
    }

    switch (st.modStatusFor(game.id)) {
      case ModStatus.notInstalled:
        return _iconAction(
          icon: Icons.download_rounded,
          label: l10n.installMod,
          color: cs.primaryContainer,
          fg: cs.onPrimaryContainer,
          onPressed: () => st.installMod(gameId: game.id),
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
              onPressed: () => st.installMod(gameId: game.id),
            ),
            const SizedBox(width: 8),
            _iconAction(
              icon: Icons.delete_rounded,
              label: l10n.uninstallMod,
              color: cs.errorContainer,
              fg: cs.onErrorContainer,
              onPressed: () => st.uninstallMod(gameId: game.id),
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

    if (game.supportedFrameworks.contains('bepinex_5')) {
      switch (st.bepinexStatusFor(game.id)) {
        case BepInExStatus.notInstalled:
          list.add(
            _badge('BepInEx: ${l10n.statusNotInstalled}', Colors.grey, cs),
          );
          break;
        case BepInExStatus.managed:
          list.add(_badge('BepInEx: ${l10n.statusManaged}', Colors.green, cs));
          break;
        case BepInExStatus.unmanaged:
          list.add(
            _badge('BepInEx: ${l10n.statusUnmanaged}', Colors.orange, cs),
          );
          break;
      }
      list.add(const SizedBox(width: 8));
    }

    switch (st.modStatusFor(game.id)) {
      case ModStatus.notInstalled:
        list.add(_badge('Mod: ${l10n.statusNotInstalled}', Colors.grey, cs));
        break;
      case ModStatus.installed:
        list.add(_badge('Mod: ${l10n.modInstalled}', Colors.green, cs));
        break;
    }

    return Row(children: list);
  }

  Widget _buildDetailedStatusBadge(
    BepInExStatus status,
    AppLocalizations l10n,
    ColorScheme cs,
  ) {
    switch (status) {
      case BepInExStatus.notInstalled:
        return _badge(l10n.statusNotInstalled, Colors.grey, cs);
      case BepInExStatus.managed:
        return _badge('${l10n.statusManaged} (5.4.23.5)', Colors.green, cs);
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
