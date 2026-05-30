import 'package:flutter/material.dart';
import '../models/node_data.dart';

class ModuleCard extends StatelessWidget {
  final ModuleInfoResponse module;
  final VoidCallback onTap;
  final VoidCallback? onSettingsTap;
  final void Function(int index)? onLinkTap;

  const ModuleCard({
    super.key,
    required this.module,
    required this.onTap,
    this.onSettingsTap,
    this.onLinkTap,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final isLoaded = module.loadedAt.isNotEmpty;

    return Card(
      elevation: 0,
      color: cs.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        module.name,
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w500,
                          color: cs.onSurface,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'v${module.version}',
                        style: TextStyle(
                          fontSize: 12,
                          color: cs.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: isLoaded ? Colors.green : Colors.grey,
                  ),
                ),
                const SizedBox(width: 8),
                if (module.hasSettingsUi)
                  FilledButton(onPressed: onTap, child: const Text('Open')),
              ],
            ),
            if (module.hasSettingsUi || module.linkEntries.isNotEmpty) ...[
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                children: [
                  if (module.hasSettingsUi)
                    TextButton.icon(
                      onPressed: onSettingsTap,
                      icon: const Icon(Icons.settings, size: 18),
                      label: const Text('Settings'),
                    ),
                  for (var i = 0; i < module.linkEntries.length; i++)
                    TextButton(
                      onPressed: () => onLinkTap?.call(i),
                      child: Text(module.linkEntries[i].title),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}
