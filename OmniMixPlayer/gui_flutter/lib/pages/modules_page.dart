import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../models/node_data.dart';
import '../widgets/proxy_node.dart';

class ModulesPage extends StatelessWidget {
  final AppState state;

  const ModulesPage({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;

    // Module detail view
    if (state.hasModuleDetail && state.moduleUiTree != null) {
      final bgColor = parseHexColor(state.moduleUiTree!.color);
      return Container(
        width: double.infinity,
        height: double.infinity,
        color: bgColor,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                children: [
                  TextButton.icon(
                    onPressed: state.closeModule,
                    icon: const Icon(Icons.arrow_back, size: 18),
                    label: Text(l10n.back),
                  ),
                ],
              ),
            ),
            Expanded(
              child: SingleChildScrollView(
                child: ProxyNodeWidget(
                  node: state.moduleUiTree!,
                  onDispatch: state.dispatchUiEvent,
                  imageBaseUrl: state.apiBaseUrl,
                ),
              ),
            ),
          ],
        ),
      );
    }

    // Module list
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
          child: Text(
            l10n.modules,
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w400,
              color: cs.onSurface,
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Text(
            l10n.manageModules,
            style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
          ),
        ),
        const SizedBox(height: 16),
        Expanded(
          child: state.modulesLoading
              ? const Center(child: CircularProgressIndicator())
              : state.modules.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.extension_off, size: 48, color: cs.outline),
                      const SizedBox(height: 12),
                      Text(
                        state.backendOnline
                            ? l10n.noModuleLinks
                            : l10n.disconnected,
                        style: TextStyle(color: cs.onSurfaceVariant),
                      ),
                      if (state.backendOnline) ...[
                        const SizedBox(height: 12),
                        OutlinedButton.icon(
                          onPressed: () => state.refreshModules(),
                          icon: const Icon(Icons.refresh, size: 16),
                          label: Text(l10n.refresh),
                        ),
                      ],
                    ],
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: state.modules.length,
                  itemBuilder: (context, index) {
                    final mod = state.modules[index];
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: _ModuleListTile(
                        module: mod,
                        onToggle: (v) => state.setModuleEnabled(mod.id, v),
                        onSettings: mod.hasSettingsUi
                            ? () => state.openModule(mod.id)
                            : null,
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }
}

class _ModuleListTile extends StatelessWidget {
  final ModuleInfoResponse module;
  final void Function(bool) onToggle;
  final VoidCallback? onSettings;

  const _ModuleListTile({
    required this.module,
    required this.onToggle,
    this.onSettings,
  });

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;

    final loaded = module.loadedAt.isNotEmpty;
    final enabled = module.enabled;

    // Status compares loaded (actual state) vs enabled (config for next start)
    //   loaded=true, enabled=true  → Green  (active)
    //   loaded=true, enabled=false → Yellow (loaded now, disabled next start)
    //   loaded=false, enabled=true → Orange (will load on next start)
    //   loaded=false, enabled=false→ Red    (disabled)
    Color statusColor;
    String statusTooltip;
    if (loaded && enabled) {
      statusColor = Colors.green;
      statusTooltip = l10n.moduleStatusActive;
    } else if (loaded && !enabled) {
      statusColor = Colors.orange;
      statusTooltip = l10n.moduleStatusPending;
    } else if (!loaded && enabled) {
      statusColor = Colors.orange;
      statusTooltip = l10n.moduleStatusWillLoad;
    } else {
      statusColor = Colors.red;
      statusTooltip = l10n.moduleStatusDisabled;
    }

    return Card(
      elevation: 0,
      color: cs.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        child: Row(
          children: [
            // Enable/disable checkbox
            Checkbox(
              value: enabled,
              onChanged: (v) {
                if (v != null) onToggle(v);
              },
            ),
            const SizedBox(width: 4),
            // Module info
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    module.name,
                    style: TextStyle(
                      fontWeight: FontWeight.w500,
                      color: cs.onSurface,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'v${module.version}',
                    style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
            // Settings button (all module UIs are settings pages)
            if (onSettings != null) ...[
              SizedBox(
                width: 36,
                height: 36,
                child: IconButton(
                  icon: const Icon(Icons.settings_rounded, size: 18),
                  style: IconButton.styleFrom(
                    padding: EdgeInsets.zero,
                    minimumSize: const Size(36, 36),
                    foregroundColor: cs.onSurfaceVariant,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                  onPressed: onSettings,
                  tooltip: l10n.settings,
                ),
              ),
              const SizedBox(width: 8),
            ],
            // Status indicator with tooltip
            Tooltip(
              message: statusTooltip,
              child: Container(
                width: 10,
                height: 10,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: statusColor,
                  boxShadow: [
                    BoxShadow(color: statusColor.withAlpha(100), blurRadius: 4),
                  ],
                ),
              ),
            ),
            const SizedBox(width: 12),
          ],
        ),
      ),
    );
  }
}
