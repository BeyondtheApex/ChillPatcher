import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';

class HomePage extends StatelessWidget {
  final AppState state;

  const HomePage({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;
    final busy = state.backendBusy || state.serviceBusy;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (busy)
              const Padding(
                padding: EdgeInsets.only(bottom: 24),
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            else
              Icon(Icons.music_note_rounded, size: 64, color: cs.primary),
            const SizedBox(height: 16),
            Text(
              l10n.appTitle,
              style: TextStyle(
                fontSize: 32,
                fontWeight: FontWeight.w300,
                color: cs.onSurface,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              busy ? l10n.restarting : l10n.welcomeMessage,
              style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
            ),
            if (!busy) ...[
              const SizedBox(height: 24),
              Text(
                l10n.welcomeHint,
                style: TextStyle(fontSize: 12, color: cs.outline),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
