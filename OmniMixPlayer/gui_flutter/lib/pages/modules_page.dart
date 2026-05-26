import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../widgets/proxy_node.dart';

class ModulesPage extends StatelessWidget {
  final AppState state;

  const ModulesPage({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;

    // Module detail view
    if (state.hasModuleDetail && state.moduleUiTree != null) {
      return Column(
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
              padding: const EdgeInsets.all(16),
              child: ProxyNodeWidget(
                node: state.moduleUiTree!,
                onDispatch: state.dispatchUiEvent,
                imageBaseUrl: state.apiBaseUrl,
              ),
            ),
          ),
        ],
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
                          label: const Text('Refresh'),
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
                        name: mod.name,
                        version: mod.version,
                        loaded: mod.loadedAt.isNotEmpty,
                        hasUi: mod.hasUi,
                        onTap: mod.hasUi
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
  final String name;
  final String version;
  final bool loaded;
  final bool hasUi;
  final VoidCallback? onTap;

  const _ModuleListTile({
    required this.name,
    required this.version,
    required this.loaded,
    required this.hasUi,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Card(
      elevation: 0,
      color: cs.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: ListTile(
        title: Text(
          name,
          style: TextStyle(fontWeight: FontWeight.w500, color: cs.onSurface),
        ),
        subtitle: Text(
          'v$version',
          style: TextStyle(color: cs.onSurfaceVariant),
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 8,
              height: 8,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: loaded ? Colors.green : Colors.grey,
              ),
            ),
            if (hasUi) ...[
              const SizedBox(width: 12),
              FilledButton.icon(
                onPressed: onTap,
                icon: const Icon(Icons.open_in_new, size: 16),
                label: const Text('Open'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
