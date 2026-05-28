import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';

class AboutPage extends StatelessWidget {
  final VoidCallback onBack;

  const AboutPage({super.key, required this.onBack});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: cs.surface,
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: onBack,
        ),
        title: Text(l10n.about),
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                l10n.appTitle,
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.w300,
                  color: cs.onSurface,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                l10n.version,
                style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
              ),
              const SizedBox(height: 24),
              Text(
                l10n.builtWith,
                style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
              ),
              const SizedBox(height: 4),
              Text(
                l10n.backendDesc,
                style: TextStyle(fontSize: 12, color: cs.outline),
              ),
              const SizedBox(height: 32),
              OutlinedButton(onPressed: onBack, child: Text(l10n.back)),
            ],
          ),
        ),
      ),
    );
  }
}
