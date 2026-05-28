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
  final ScrollController _logScrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    _logScrollController.dispose();
    super.dispose();
  }

  void _onStateChanged() {
    if (mounted) {
      setState(() {});
      // Auto scroll deployment logs to bottom
      if (widget.state.deploymentLogs.isNotEmpty) {
        Future.delayed(const Duration(milliseconds: 50), () {
          if (_logScrollController.hasClients) {
            _logScrollController.animateTo(
              _logScrollController.position.maxScrollExtent,
              duration: const Duration(milliseconds: 200),
              curve: Curves.easeOut,
            );
          }
        });
      }
    }
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
  //  Game List View
  // ═══════════════════════════════════════════════════════════

  Widget _buildGameList(BuildContext context, AppLocalizations l10n, ColorScheme cs) {
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
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: cs.onSurfaceVariant,
                  ),
            ),
            const SizedBox(height: 24),
            Expanded(
              child: ListView.builder(
                itemCount: gameCatalog.length,
                itemBuilder: (context, index) {
                  final game = gameCatalog[index];

                  return Card(
                    clipBehavior: Clip.antiAlias,
                    elevation: 2,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                      side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
                    ),
                    child: InkWell(
                      onTap: () {
                        setState(() {
                          _activeGameId = game.id;
                        });
                      },
                      child: Padding(
                        padding: const EdgeInsets.all(20.0),
                        child: Row(
                          children: [
                            Container(
                              width: 64,
                              height: 64,
                              decoration: BoxDecoration(
                                color: cs.primaryContainer,
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Icon(
                                Icons.sports_esports,
                                size: 36,
                                color: cs.onPrimaryContainer,
                              ),
                            ),
                            const SizedBox(width: 20),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    game.name,
                                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                                          fontWeight: FontWeight.bold,
                                        ),
                                  ),
                                  const SizedBox(height: 6),
                                  _buildCompactStatusBadge(l10n, cs),
                                ],
                              ),
                            ),
                            Icon(
                              Icons.arrow_forward_ios,
                              size: 16,
                              color: cs.onSurfaceVariant,
                            ),
                          ],
                        ),
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

  Widget _buildCompactStatusBadge(AppLocalizations l10n, ColorScheme cs) {
    final st = widget.state;
    if (st.gamePath.isEmpty) {
      return _badge(l10n.statusNotInstalled, Colors.grey, cs);
    }

    final game = gameCatalog.firstWhere((g) => g.id == 'chill_with_you');
    final isValid = ModDeploymentService.verifyGameDirectory(st.gamePath, game);
    if (!isValid) {
      return _badge(l10n.statusNotInstalled, Colors.grey, cs);
    }

    final list = <Widget>[];

    // BepInEx badge
    switch (st.bepinexStatus) {
      case BepInExStatus.notInstalled:
        list.add(_badge('BepInEx: ${l10n.statusNotInstalled}', Colors.grey, cs));
        break;
      case BepInExStatus.managed:
        list.add(_badge('BepInEx: ${l10n.statusManaged}', Colors.green, cs));
        break;
      case BepInExStatus.unmanaged:
        list.add(_badge('BepInEx: ${l10n.statusUnmanaged}', Colors.orange, cs));
        break;
    }

    list.add(const SizedBox(width: 8));

    // Mod badge
    switch (st.modStatus) {
      case ModStatus.notInstalled:
        list.add(_badge('Mod: ${l10n.statusNotInstalled}', Colors.grey, cs));
        break;
      case ModStatus.installed:
        list.add(_badge('Mod: ${l10n.modInstalled}', Colors.green, cs));
        break;
    }

    return Row(children: list);
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

  // ═══════════════════════════════════════════════════════════
  //  Game Detail Setup View
  // ═══════════════════════════════════════════════════════════

  Widget _buildGameDetail(
      BuildContext context, AppLocalizations l10n, ColorScheme cs, GameDeclaration game) {
    final st = widget.state;
    final isPathEmpty = st.gamePath.isEmpty;
    final isValidPath = !isPathEmpty && ModDeploymentService.verifyGameDirectory(st.gamePath, game);

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: cs.onSurface),
          onPressed: () {
            setState(() {
              _activeGameId = null;
            });
          },
        ),
        title: Text(
          game.name,
          style: TextStyle(fontWeight: FontWeight.bold, color: cs.onSurface),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // 1. Path selector
            Card(
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
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
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
                              isPathEmpty ? l10n.chooseFolder : st.gamePath,
                              style: TextStyle(
                                fontSize: 13,
                                color: isPathEmpty ? cs.onSurfaceVariant : cs.onSurface,
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        ElevatedButton.icon(
                          onPressed: st.deploymentBusy
                              ? null
                              : () async {
                                  final path = await ModDeploymentService.selectDirectory();
                                  if (path != null) {
                                    await st.setGamePath(path);
                                  }
                                },
                          icon: const Icon(Icons.folder_open),
                          label: Text(l10n.chooseFolder),
                          style: ElevatedButton.styleFrom(
                            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
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
                        l10n.invalidGameDir,
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
            ),
            const SizedBox(height: 20),

            if (isValidPath) ...[
              // 2. BepInEx status alert card if unmanaged
              if (st.bepinexStatus == BepInExStatus.unmanaged) ...[
                Card(
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
                        const Icon(Icons.warning_amber_rounded, color: Colors.orange, size: 24),
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
                ),
                const SizedBox(height: 20),
              ],

              // 3. Components manager cards
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // BepInEx Card
                  Expanded(
                    child: Card(
                      elevation: 0,
                      color: cs.surfaceContainerLow,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                        side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
                      ),
                      child: Padding(
                        padding: const EdgeInsets.all(20.0),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              l10n.bepinexStatus,
                              style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 12),
                            _buildDetailedStatusBadge(st.bepinexStatus, l10n, cs),
                            const SizedBox(height: 24),
                            SizedBox(
                              width: double.infinity,
                              child: _buildBepInExActionButton(st, l10n, cs),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  // Mod Card
                  Expanded(
                    child: Card(
                      elevation: 0,
                      color: cs.surfaceContainerLow,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                        side: BorderSide(color: cs.outlineVariant.withOpacity(0.5)),
                      ),
                      child: Padding(
                        padding: const EdgeInsets.all(20.0),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              l10n.modStatus,
                              style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 12),
                            _buildModDetailedStatusBadge(st.modStatus, l10n, cs),
                            const SizedBox(height: 24),
                            SizedBox(
                              width: double.infinity,
                              child: _buildModActionButton(st, l10n, cs),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),

              // 4. Progress logs terminal
              if (st.deploymentLogs.isNotEmpty || st.deploymentBusy) ...[
                Text(
                  l10n.deploymentLogs,
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                const SizedBox(height: 8),
                if (st.deploymentBusy) ...[
                  const LinearProgressIndicator(),
                  const SizedBox(height: 4),
                ],
                Container(
                  height: 180,
                  width: double.infinity,
                  decoration: BoxDecoration(
                    color: Colors.black.withOpacity(0.85),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: cs.outlineVariant.withOpacity(0.5)),
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(12),
                    child: ListView.builder(
                      controller: _logScrollController,
                      padding: const EdgeInsets.all(12),
                      itemCount: st.deploymentLogs.length,
                      itemBuilder: (context, index) {
                        return Padding(
                          padding: const EdgeInsets.only(bottom: 4.0),
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
                ),
              ],
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildDetailedStatusBadge(BepInExStatus status, AppLocalizations l10n, ColorScheme cs) {
    switch (status) {
      case BepInExStatus.notInstalled:
        return _badge(l10n.statusNotInstalled, Colors.grey, cs);
      case BepInExStatus.managed:
        return _badge('${l10n.statusManaged} (5.4.23.5)', Colors.green, cs);
      case BepInExStatus.unmanaged:
        return _badge(l10n.statusUnmanaged, Colors.orange, cs);
    }
  }

  Widget _buildModDetailedStatusBadge(ModStatus status, AppLocalizations l10n, ColorScheme cs) {
    switch (status) {
      case ModStatus.notInstalled:
        return _badge(l10n.statusNotInstalled, Colors.grey, cs);
      case ModStatus.installed:
        return _badge('${l10n.modInstalled} (v1.0.0)', Colors.green, cs);
    }
  }

  Widget _buildBepInExActionButton(AppState st, AppLocalizations l10n, ColorScheme cs) {
    if (st.deploymentBusy) {
      return ElevatedButton(
        onPressed: null,
        child: const SizedBox(
          width: 16,
          height: 16,
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      );
    }

    switch (st.bepinexStatus) {
      case BepInExStatus.notInstalled:
        return ElevatedButton.icon(
          onPressed: () => st.installBepInEx(),
          icon: const Icon(Icons.download),
          label: Text(l10n.installBepInEx),
          style: ElevatedButton.styleFrom(
            backgroundColor: cs.primaryContainer,
            foregroundColor: cs.onPrimaryContainer,
          ),
        );
      case BepInExStatus.managed:
        return Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            ElevatedButton.icon(
              onPressed: () => st.installBepInEx(),
              icon: const Icon(Icons.refresh),
              label: Text(l10n.reinstallBepInEx),
            ),
            OutlinedButton.icon(
              onPressed: () => st.uninstallBepInEx(),
              icon: const Icon(Icons.delete_outline),
              label: Text(l10n.uninstallBepInEx),
              style: OutlinedButton.styleFrom(
                foregroundColor: cs.error,
                side: BorderSide(color: cs.error.withOpacity(0.5)),
              ),
            ),
          ],
        );
      case BepInExStatus.unmanaged:
        return OutlinedButton.icon(
          onPressed: null,
          icon: const Icon(Icons.lock_outline),
          label: Text(l10n.installBepInEx),
        );
    }
  }

  Widget _buildModActionButton(AppState st, AppLocalizations l10n, ColorScheme cs) {
    if (st.deploymentBusy) {
      return ElevatedButton(
        onPressed: null,
        child: const SizedBox(
          width: 16,
          height: 16,
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      );
    }

    final bepinexPresent = st.bepinexStatus != BepInExStatus.notInstalled;
    if (!bepinexPresent) {
      return ElevatedButton.icon(
        onPressed: null,
        icon: const Icon(Icons.add),
        label: Text(l10n.installMod),
      );
    }

    switch (st.modStatus) {
      case ModStatus.notInstalled:
        return ElevatedButton.icon(
          onPressed: () => st.installMod(),
          icon: const Icon(Icons.add),
          label: Text(l10n.installMod),
          style: ElevatedButton.styleFrom(
            backgroundColor: cs.primaryContainer,
            foregroundColor: cs.onPrimaryContainer,
          ),
        );
      case ModStatus.installed:
        return Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            ElevatedButton.icon(
              onPressed: () => st.installMod(),
              icon: const Icon(Icons.refresh),
              label: Text(l10n.reinstallMod),
              style: ElevatedButton.styleFrom(
                backgroundColor: cs.primaryContainer,
                foregroundColor: cs.onPrimaryContainer,
              ),
            ),
            OutlinedButton.icon(
              onPressed: () => st.uninstallMod(),
              icon: const Icon(Icons.delete_outline),
              label: Text(l10n.uninstallMod),
              style: OutlinedButton.styleFrom(
                foregroundColor: cs.error,
                side: BorderSide(color: cs.error.withOpacity(0.5)),
              ),
            ),
          ],
        );
    }
  }
}
