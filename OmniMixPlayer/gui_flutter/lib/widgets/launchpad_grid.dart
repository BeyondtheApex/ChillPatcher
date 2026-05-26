import 'package:flutter/material.dart';
import '../models/node_data.dart';

class LaunchpadGrid extends StatelessWidget {
  final List<ModuleLinkEntryResponse> links;
  final void Function(ModuleLinkEntryResponse entry) onTap;

  const LaunchpadGrid({super.key, required this.links, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    if (links.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.grid_view_rounded, size: 48, color: cs.outline),
            const SizedBox(height: 12),
            Text(
              'No module links available',
              style: TextStyle(color: cs.onSurfaceVariant),
            ),
          ],
        ),
      );
    }

    return GridView.builder(
      padding: const EdgeInsets.all(16),
      gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: 120,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 1,
      ),
      itemCount: links.length,
      itemBuilder: (context, index) {
        final link = links[index];
        return _LinkTile(link: link, onTap: () => onTap(link));
      },
    );
  }
}

class _LinkTile extends StatelessWidget {
  final ModuleLinkEntryResponse link;
  final VoidCallback onTap;

  const _LinkTile({required this.link, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final bgColor = link.backgroundColor.isNotEmpty
        ? _parseColor(link.backgroundColor)
        : cs.secondaryContainer;

    return Card(
      elevation: 0,
      color: bgColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                _iconFromName(link.icon),
                size: 32,
                color: _parseColor(link.iconColor) ?? cs.onSecondaryContainer,
              ),
              const SizedBox(height: 8),
              Text(
                link.title,
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 12,
                  color: _parseColor(link.iconColor) ?? cs.onSecondaryContainer,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Color? _parseColor(String hex) {
    if (hex.isEmpty) return null;
    try {
      return Color(int.parse('FF${hex.replaceFirst('#', '')}', radix: 16));
    } catch (_) {
      return null;
    }
  }

  IconData _iconFromName(String name) {
    switch (name) {
      case 'music_note':
        return Icons.music_note;
      case 'headphones':
        return Icons.headphones;
      case 'cloud':
        return Icons.cloud;
      case 'folder':
        return Icons.folder;
      case 'radio':
        return Icons.radio;
      default:
        return Icons.widgets;
    }
  }
}
