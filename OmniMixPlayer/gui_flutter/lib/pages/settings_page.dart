import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';

class SettingsPage extends StatefulWidget {
  final AppState state;

  const SettingsPage({super.key, required this.state});

  @override
  State<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends State<SettingsPage> {
  late TextEditingController _portCtrl;
  late TextEditingController _bindCtrl;

  @override
  void initState() {
    super.initState();
    _portCtrl = TextEditingController(text: widget.state.backendPort);
    _bindCtrl = TextEditingController(text: widget.state.backendBind);
  }

  @override
  void dispose() {
    _portCtrl.dispose();
    _bindCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;
    final st = widget.state;

    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        Text(
          l10n.settings,
          style: TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w400,
            color: cs.onSurface,
          ),
        ),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  Backend Control
        // ═══════════════════════════════════
        _SectionHeader(title: l10n.backendControl),
        const SizedBox(height: 8),
        Card(
          elevation: 0,
          color: cs.surfaceContainerHighest,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _StatusRow(l10n: l10n, running: st.backendRunning),
                const SizedBox(height: 12),
                _BackendToggleButton(state: st, l10n: l10n),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  Service Management
        // ═══════════════════════════════════
        _SectionHeader(title: l10n.backendService),
        const SizedBox(height: 8),
        _ServiceStatusCard(state: st, l10n: l10n),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  Backend Config (port / bind)
        // ═══════════════════════════════════
        _SectionHeader(title: 'Backend Config'),
        const SizedBox(height: 8),
        Card(
          elevation: 0,
          color: cs.surfaceContainerHighest,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _InputRow(
                  label: l10n.port,
                  controller: _portCtrl,
                  onChanged: (v) => st.setBackendPort(v),
                ),
                _InputRow(
                  label: l10n.bind,
                  controller: _bindCtrl,
                  onChanged: (v) => st.setBackendBind(v),
                ),
                const SizedBox(height: 8),
                Row(
                  spacing: 8,
                  children: [
                    FilledButton.icon(
                      onPressed: st.backendBusy
                          ? null
                          : () => _doSaveAndRestart(l10n),
                      icon: st.backendBusy
                          ? const SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Icon(Icons.restart_alt, size: 18),
                      label: Text(
                        st.backendBusy ? l10n.restarting : l10n.saveAndRestart,
                      ),
                    ),
                    OutlinedButton.icon(
                      onPressed: st.backendBusy ? null : () => _doReset(l10n),
                      icon: const Icon(Icons.restore, size: 18),
                      label: Text(l10n.resetToDefaults),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: cs.error,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  GUI Settings
        // ═══════════════════════════════════
        _SectionHeader(title: l10n.guiSettings),
        const SizedBox(height: 8),
        Card(
          elevation: 0,
          color: cs.surfaceContainerHighest,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _SwitchRow(
                  label: l10n.autoStart,
                  value: st.autostart,
                  onChanged: (v) => st.setAutostart(v),
                ),
                _SwitchRow(
                  label: l10n.minimizeToTray,
                  value: st.minimizeToTray,
                  onChanged: (v) => st.setMinimizeToTray(v),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  Appearance
        // ═══════════════════════════════════
        _SectionHeader(title: l10n.appearance),
        const SizedBox(height: 8),
        Card(
          elevation: 0,
          color: cs.surfaceContainerHighest,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _ComboRow(
                  label: l10n.theme,
                  value: st.themeMode.name,
                  items: const ['light', 'dark', 'system'],
                  displayNames: [
                    l10n.themeLight,
                    l10n.themeDark,
                    l10n.themeSystem,
                  ],
                  onChanged: (v) => st.setThemeMode(
                    AppThemeMode.values.firstWhere((e) => e.name == v),
                  ),
                ),
                _ComboRow(
                  label: l10n.language,
                  value: st.language,
                  items: const ['zh', 'en', 'system'],
                  displayNames: const ['中文', 'English', 'System'],
                  onChanged: (v) => st.setLanguage(v),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  About
        // ═══════════════════════════════════
        _SectionHeader(title: l10n.about),
        const SizedBox(height: 8),
        Card(
          elevation: 0,
          color: cs.surfaceContainerHighest,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${l10n.appTitle} ${l10n.version}',
                  style: TextStyle(color: cs.onSurfaceVariant),
                ),
                const SizedBox(height: 4),
                Text(
                  l10n.builtWith,
                  style: TextStyle(fontSize: 12, color: cs.outline),
                ),
                const SizedBox(height: 12),
                OutlinedButton(
                  onPressed: widget.state.openAbout,
                  child: Text(l10n.about),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),
      ],
    );
  }

  Future<void> _doSaveAndRestart(dynamic l10n) async {
    await widget.state.saveAndRestart();
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(l10n.saveAndRestart),
          duration: const Duration(seconds: 2),
        ),
      );
    }
  }

  Future<void> _doReset(dynamic l10n) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l10n.resetToDefaults),
        content: Text(l10n.resetConfirm),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(l10n.resetToDefaults),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    await widget.state.resetToDefaults();
    _portCtrl.text = widget.state.backendPort;
    _bindCtrl.text = widget.state.backendBind;
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(l10n.configReset),
          duration: const Duration(seconds: 2),
        ),
      );
    }
  }
}

// ── Reusable widgets ──

class _SectionHeader extends StatelessWidget {
  final String title;
  const _SectionHeader({required this.title});
  @override
  Widget build(BuildContext context) {
    return Text(
      title,
      style: TextStyle(
        fontSize: 16,
        fontWeight: FontWeight.w500,
        color: Theme.of(context).colorScheme.onSurface,
      ),
    );
  }
}

class _StatusRow extends StatelessWidget {
  final dynamic l10n;
  final bool running;
  const _StatusRow({required this.l10n, required this.running});
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Row(
      spacing: 8,
      children: [
        Text(
          '${l10n.status}:',
          style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
        ),
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: running ? Colors.green : Colors.grey,
          ),
        ),
        Text(
          running ? l10n.running : l10n.stopped,
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w500,
            color: running ? Colors.green : Colors.grey,
          ),
        ),
      ],
    );
  }
}

// ── Single toggle button: Start / Stop ──

class _BackendToggleButton extends StatelessWidget {
  final AppState state;
  final dynamic l10n;
  const _BackendToggleButton({required this.state, required this.l10n});
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final running = state.backendRunning;
    final busy = state.backendBusy;
    if (busy)
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(8),
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      );
    return SizedBox(
      width: double.infinity,
      child: running
          ? OutlinedButton.icon(
              onPressed: () => state.toggleBackend(),
              icon: const Icon(Icons.stop_circle, size: 18),
              label: Text(l10n.stopBackend),
              style: OutlinedButton.styleFrom(
                foregroundColor: cs.error,
                side: BorderSide(color: cs.error),
              ),
            )
          : FilledButton.icon(
              onPressed: () => state.toggleBackend(),
              icon: const Icon(Icons.play_circle, size: 18),
              label: Text(l10n.startBackend),
            ),
    );
  }
}

class _ComboRow extends StatelessWidget {
  final String label;
  final String value;
  final List<String> items;
  final List<String>? displayNames;
  final void Function(String) onChanged;
  const _ComboRow({
    required this.label,
    required this.value,
    required this.items,
    this.displayNames,
    required this.onChanged,
  });
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          SizedBox(
            width: 100,
            child: Text(
              label,
              style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
            ),
          ),
          DropdownButton<String>(
            value: items.contains(value) ? value : items.first,
            items: List.generate(
              items.length,
              (i) => DropdownMenuItem(
                value: items[i],
                child: Text(displayNames?[i] ?? items[i]),
              ),
            ),
            onChanged: (v) {
              if (v != null) onChanged(v);
            },
          ),
        ],
      ),
    );
  }
}

class _InputRow extends StatelessWidget {
  final String label;
  final TextEditingController controller;
  final void Function(String)? onChanged;
  const _InputRow({
    required this.label,
    required this.controller,
    this.onChanged,
  });
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          SizedBox(
            width: 100,
            child: Text(
              label,
              style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
            ),
          ),
          SizedBox(
            width: 140,
            child: TextField(
              controller: controller,
              decoration: InputDecoration(
                isDense: true,
                border: const OutlineInputBorder(),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
              ),
              onChanged: onChanged,
            ),
          ),
        ],
      ),
    );
  }
}

class _SwitchRow extends StatelessWidget {
  final String label;
  final bool value;
  final void Function(bool) onChanged;
  const _SwitchRow({
    required this.label,
    required this.value,
    required this.onChanged,
  });
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          SizedBox(
            width: 100,
            child: Text(
              label,
              style: TextStyle(fontSize: 14, color: cs.onSurfaceVariant),
            ),
          ),
          Switch(value: value, onChanged: onChanged),
        ],
      ),
    );
  }
}

// ── Service Status Card ──

class _ServiceStatusCard extends StatefulWidget {
  final AppState state;
  final dynamic l10n;
  const _ServiceStatusCard({required this.state, required this.l10n});
  @override
  State<_ServiceStatusCard> createState() => _ServiceStatusCardState();
}

class _ServiceStatusCardState extends State<_ServiceStatusCard> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback(
      (_) => widget.state.refreshServiceState(),
    );
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final st = widget.state;
    final l10n = widget.l10n;
    final svcState = st.serviceState;
    final busy = st.serviceBusy;
    final result = st.serviceResult;

    IconData statusIcon;
    Color statusColor;
    String statusText;
    String modeText;
    switch (svcState) {
      case 'running':
        statusIcon = Icons.check_circle;
        statusColor = Colors.green;
        statusText = l10n.serviceRunning;
        modeText = l10n.serviceMode;
      case 'installed':
        statusIcon = Icons.pause_circle;
        statusColor = Colors.orange;
        statusText = l10n.serviceInstalled;
        modeText = l10n.serviceMode;
      default:
        statusIcon = Icons.cancel;
        statusColor = Colors.grey;
        statusText = l10n.serviceNotInstalled;
        modeText = l10n.serviceMode;
    }

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
                Icon(Icons.miscellaneous_services, size: 20, color: cs.primary),
                const SizedBox(width: 8),
                Text(
                  l10n.serviceStatus,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                    color: cs.onSurface,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              spacing: 8,
              children: [
                Icon(statusIcon, size: 20, color: statusColor),
                Text(
                  statusText,
                  style: TextStyle(
                    fontSize: 13,
                    color: statusColor,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: cs.primaryContainer,
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    modeText,
                    style: TextStyle(
                      fontSize: 11,
                      color: cs.onPrimaryContainer,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            if (busy)
              const Center(
                child: Padding(
                  padding: EdgeInsets.all(8),
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              )
            else ...[
              Row(
                spacing: 8,
                children: [
                  if (svcState == 'not_installed' || svcState == 'unknown')
                    FilledButton.icon(
                      onPressed: () => _doInstall(),
                      icon: const Icon(Icons.download, size: 16),
                      label: Text(l10n.installService),
                    ),
                  if (svcState == 'installed' || svcState == 'running')
                    OutlinedButton.icon(
                      onPressed: () => _doUninstall(),
                      icon: const Icon(Icons.delete_outline, size: 16),
                      label: Text(l10n.uninstallService),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: cs.error,
                      ),
                    ),
                ],
              ),
            ],
            if (result != null) ...[
              const SizedBox(height: 8),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: result == 'failed'
                      ? cs.errorContainer
                      : cs.primaryContainer,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  spacing: 6,
                  children: [
                    Icon(
                      result == 'failed' ? Icons.error : Icons.check_circle,
                      size: 16,
                      color: result == 'failed' ? cs.error : Colors.green,
                    ),
                    Expanded(
                      child: Text(
                        _resultText(result, svcState, l10n),
                        style: TextStyle(
                          fontSize: 12,
                          color: result == 'failed'
                              ? cs.onErrorContainer
                              : cs.onPrimaryContainer,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _resultText(String result, String svcState, dynamic l10n) {
    if (result == 'failed')
      return svcState == 'running' || svcState == 'installed'
          ? l10n.serviceUninstallFailed
          : l10n.serviceInstallFailed;
    if (result == 'installed') return l10n.serviceInstallSuccess;
    if (result == 'not_installed') return l10n.serviceUninstallSuccess;
    return result;
  }

  Future<void> _doInstall() async {
    await widget.state.installService();
    if (mounted)
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            widget.state.serviceResult == 'failed'
                ? widget.l10n.serviceInstallFailed
                : widget.l10n.serviceInstallSuccess,
          ),
          duration: const Duration(seconds: 2),
        ),
      );
  }

  Future<void> _doUninstall() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(widget.l10n.uninstallService),
        content: const Text(
          'This will stop the backend service and remove it from the system.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(widget.l10n.uninstallService),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    await widget.state.uninstallService();
    if (mounted)
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            widget.state.serviceResult == 'failed'
                ? widget.l10n.serviceUninstallFailed
                : widget.l10n.serviceUninstallSuccess,
          ),
          duration: const Duration(seconds: 2),
        ),
      );
  }
}
