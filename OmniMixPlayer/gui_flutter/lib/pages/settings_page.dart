import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../models/mod_manifest.dart';
import '../services/mod_deployment_service.dart';

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
    final l10n = AppLocalizations.of(context);
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
        //  Instance & Archive Management
        // ═══════════════════════════════════
        _SectionHeader(title: '实例管理'),
        const SizedBox(height: 8),
        _InstanceManagementCard(state: st),
        const SizedBox(height: 24),

        // ═══════════════════════════════════
        //  Backend Config (port / bind)
        // ═══════════════════════════════════
        _SectionHeader(title: l10n.backendConfig),
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
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
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
                const SizedBox(height: 12),
                Row(
                  children: [
                    Text(
                      l10n.closeBehavior,
                      style: const TextStyle(fontSize: 14),
                    ),
                    const Spacer(),
                    DropdownButton<String>(
                      value: st.closeBehavior,
                      underline: const SizedBox(),
                      items: [
                        DropdownMenuItem(
                          value: 'minimize',
                          child: Text(
                            l10n.closeMinimize,
                            style: const TextStyle(fontSize: 13),
                          ),
                        ),
                        DropdownMenuItem(
                          value: 'exit',
                          child: Text(
                            l10n.closeExit,
                            style: const TextStyle(fontSize: 13),
                          ),
                        ),
                      ],
                      onChanged: (v) {
                        if (v != null) st.setCloseBehavior(v);
                      },
                    ),
                  ],
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
                const SizedBox(height: 8),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Text(l10n.themeColor, style: const TextStyle(fontSize: 14)),
                    const Spacer(),
                    SizedBox(
                      width: 280,
                      child: _ColorPickerRow(
                        currentColor: Color(st.seedColor),
                        onChanged: (c) => st.setSeedColor(c.toARGB32()),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        l10n.useSystemColor,
                        style: const TextStyle(fontSize: 14),
                      ),
                    ),
                    Switch(
                      value: st.useSystemColor,
                      onChanged: (v) => st.setUseSystemColor(v),
                    ),
                  ],
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
            if (svcState == 'running' || svcState == 'installed') ...[
              const Divider(height: 1),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      l10n.serviceAutoStart,
                      style: TextStyle(
                        fontSize: 13,
                        color: cs.onSurfaceVariant,
                      ),
                    ),
                  ),
                  Switch(
                    value: st.serviceAutoStart,
                    onChanged: busy
                        ? null
                        : (v) async {
                            final ok = await st.setServiceAutoStart(v);
                            if (mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text(
                                    ok
                                        ? (st.language == 'zh'
                                              ? '开机自启设置成功'
                                              : 'Service auto-start updated')
                                        : (st.language == 'zh'
                                              ? '开机自启设置失败'
                                              : 'Failed to update service auto-start'),
                                  ),
                                  duration: const Duration(seconds: 2),
                                ),
                              );
                            }
                          },
                  ),
                ],
              ),
              const SizedBox(height: 12),
            ],
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

// ── Instance Management Card ──

class _InstanceManagementCard extends StatefulWidget {
  final AppState state;
  const _InstanceManagementCard({required this.state});
  @override
  State<_InstanceManagementCard> createState() =>
      _InstanceManagementCardState();
}

class _InstanceManagementCardState extends State<_InstanceManagementCard> {
  @override
  void initState() {
    super.initState();
    widget.state.refreshInstances();
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final st = widget.state;
    final instances = st.instances;
    final archives = st.archives;

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
                const Icon(Icons.devices, size: 18),
                const SizedBox(width: 8),
                Text(
                  instances.isEmpty ? '暂无已安装实例' : '已安装实例 (${instances.length})',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                    color: cs.onSurface,
                  ),
                ),
                const Spacer(),
                TextButton.icon(
                  onPressed: () => _showArchiveDialog(context),
                  icon: const Icon(Icons.archive, size: 16),
                  label: Text(
                    '归档 (${archives.length})',
                    style: const TextStyle(fontSize: 13),
                  ),
                ),
              ],
            ),
            if (instances.isEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  '安装游戏 Mod 后，实例会自动注册',
                  style: TextStyle(fontSize: 12, color: cs.outline),
                ),
              ),
            if (instances.isNotEmpty) ...[
              const SizedBox(height: 8),
              ...instances.map((inst) => _buildInstanceTile(inst, cs)),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildInstanceTile(InstalledInstance inst, ColorScheme cs) {
    final online = widget.state.isInstanceOnline(inst.instanceId);
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Icon(
            online ? Icons.circle : Icons.circle_outlined,
            size: 10,
            color: online ? Colors.green : Colors.grey,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  inst.gameName,
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w500,
                    color: cs.onSurface,
                  ),
                ),
                Text(
                  inst.instanceId,
                  style: TextStyle(fontSize: 11, color: cs.outline),
                ),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
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
                color: inst.isServerMode ? Colors.blue : Colors.orange,
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _showArchiveDialog(BuildContext context) {
    final archives = widget.state.archives;
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('归档管理'),
        content: SizedBox(
          width: 500,
          child: archives.isEmpty
              ? const Text('暂无归档实例')
              : ListView.builder(
                  shrinkWrap: true,
                  itemCount: archives.length,
                  itemBuilder: (_, i) {
                    final a = archives[i];
                    return Card(
                      child: ListTile(
                        dense: true,
                        leading: Icon(
                          a.mode == 'server' ? Icons.dns : Icons.smartphone,
                          size: 20,
                        ),
                        title: Text(
                          a.displayName,
                          style: const TextStyle(fontSize: 13),
                        ),
                        subtitle: Text(
                          '${a.gameName} · ${a.mode} · ${a.archivedAt.toLocal().toString().substring(0, 16)}',
                          style: const TextStyle(fontSize: 11),
                        ),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: const Icon(Icons.edit, size: 16),
                              tooltip: '重命名',
                              onPressed: () => _renameArchive(ctx, a),
                            ),
                            IconButton(
                              icon: Icon(
                                Icons.delete,
                                size: 16,
                                color: Theme.of(context).colorScheme.error,
                              ),
                              tooltip: '删除',
                              onPressed: () => _deleteArchive(ctx, a),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('关闭'),
          ),
        ],
      ),
    );
  }

  void _renameArchive(BuildContext ctx, ArchiveEntry a) {
    final ctrl = TextEditingController(text: a.label);
    showDialog(
      context: ctx,
      builder: (c) => AlertDialog(
        title: const Text('重命名归档'),
        content: TextField(
          controller: ctrl,
          decoration: const InputDecoration(
            hintText: '输入归档名称',
            border: OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c),
            child: const Text('取消'),
          ),
          FilledButton(
            onPressed: () {
              ModDeploymentService.renameArchive(a.instanceId, ctrl.text);
              widget.state.refreshInstances();
              Navigator.pop(c);
            },
            child: const Text('保存'),
          ),
        ],
      ),
    );
  }

  void _deleteArchive(BuildContext ctx, ArchiveEntry a) async {
    final confirmed = await showDialog<bool>(
      context: ctx,
      builder: (c) => AlertDialog(
        title: const Text('删除归档'),
        content: Text('确定要删除 "${a.displayName}" 的归档吗？此操作不可撤销。'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: const Text('取消'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(c, true),
            style: FilledButton.styleFrom(
              backgroundColor: Theme.of(c).colorScheme.error,
            ),
            child: const Text('删除'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      ModDeploymentService.deleteArchive(a.instanceId);
      widget.state.refreshInstances();
    }
  }
}

/// Row of preset color circles + color picker trigger.
class _ColorPickerRow extends StatelessWidget {
  final Color currentColor;
  final ValueChanged<Color> onChanged;

  const _ColorPickerRow({required this.currentColor, required this.onChanged});

  static const _presets = <int>[
    0xFF673AB7, // deepPurple
    0xFF1976D2, // blue
    0xFF00897B, // teal
    0xFF689F38, // green
    0xFFFBC02D, // yellow
    0xFFF57C00, // orange
    0xFFD32F2F, // red
    0xFF8D6E63, // brown
    0xFF455A64, // blueGrey
    0xFF000000, // custom (last)
  ];

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: [
        for (final c in _presets)
          GestureDetector(
            onTap: () {
              if (c == 0xFF000000) {
                _pickCustom(context);
              } else {
                onChanged(Color(c));
              }
            },
            child: Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: c == 0xFF000000 ? null : Color(c),
                shape: BoxShape.circle,
                border: Border.all(
                  color: currentColor.toARGB32() == c
                      ? cs.primary
                      : cs.outlineVariant,
                  width: currentColor.toARGB32() == c ? 3 : 1,
                ),
                gradient: c == 0xFF000000
                    ? const LinearGradient(
                        colors: [Colors.red, Colors.green, Colors.blue],
                      )
                    : null,
              ),
              child: c == 0xFF000000
                  ? Icon(Icons.colorize_rounded, size: 20, color: Colors.white)
                  : null,
            ),
          ),
      ],
    );
  }

  Future<void> _pickCustom(BuildContext context) async {
    // Simple color picker via showDialog with HSV sliders
    final result = await showDialog<Color>(
      context: context,
      builder: (ctx) => _ColorPickerDialog(initial: currentColor),
    );
    if (result != null) onChanged(result);
  }
}

/// Minimal HSV-style color picker dialog.
class _ColorPickerDialog extends StatefulWidget {
  final Color initial;
  const _ColorPickerDialog({required this.initial});

  @override
  State<_ColorPickerDialog> createState() => _ColorPickerDialogState();
}

class _ColorPickerDialogState extends State<_ColorPickerDialog> {
  late double _hue;
  late double _sat;
  late double _val;
  late AppLocalizations _l;

  @override
  void initState() {
    super.initState();
    final hsv = HSVColor.fromColor(widget.initial);
    _hue = hsv.hue;
    _sat = hsv.saturation;
    _val = hsv.value;
  }

  Color get _current => HSVColor.fromAHSV(1, _hue, _sat, _val).toColor();

  @override
  Widget build(BuildContext context) {
    _l = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(_l.customColor),
      content: SizedBox(
        width: 300,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Preview
            Container(
              width: 80,
              height: 80,
              decoration: BoxDecoration(
                color: _current,
                shape: BoxShape.circle,
                boxShadow: [BoxShadow(color: Colors.black26, blurRadius: 8)],
              ),
            ),
            const SizedBox(height: 20),
            // Hue slider
            _hueSlider(),
            const SizedBox(height: 12),
            // Saturation slider
            _slide(
              _l.saturation,
              _sat,
              (v) => setState(() => _sat = v),
              HSVColor.fromAHSV(1, _hue, 0, _val).toColor(),
              HSVColor.fromAHSV(1, _hue, 1, _val).toColor(),
            ),
            const SizedBox(height: 12),
            // Value slider
            _slide(
              _l.brightness,
              _val,
              (v) => setState(() => _val = v),
              HSVColor.fromAHSV(1, _hue, _sat, 0).toColor(),
              HSVColor.fromAHSV(1, _hue, _sat, 1).toColor(),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(_l.cancel),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_current),
          child: Text(_l.confirm),
        ),
      ],
    );
  }

  Widget _hueSlider() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(_l.hue, style: const TextStyle(fontSize: 12)),
        const SizedBox(height: 4),
        _GradientSlider(
          value: _hue,
          max: 360,
          gradient: const LinearGradient(
            colors: [
              Color(0xFFFF0000),
              Color(0xFFFFFF00),
              Color(0xFF00FF00),
              Color(0xFF00FFFF),
              Color(0xFF0000FF),
              Color(0xFFFF00FF),
              Color(0xFFFF0000),
            ],
          ),
          onChanged: (v) => setState(() => _hue = v),
        ),
      ],
    );
  }

  Widget _slide(
    String label,
    double val,
    ValueChanged<double> onChanged,
    Color left,
    Color right,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(fontSize: 12)),
        const SizedBox(height: 4),
        _GradientSlider(
          value: val,
          max: 1,
          gradient: LinearGradient(colors: [left, right]),
          onChanged: onChanged,
        ),
      ],
    );
  }
}

class _GradientSlider extends StatelessWidget {
  final double value;
  final double max;
  final LinearGradient gradient;
  final ValueChanged<double> onChanged;

  const _GradientSlider({
    required this.value,
    required this.max,
    required this.gradient,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        return GestureDetector(
          onTapDown: (d) => onChanged(
            (d.localPosition.dx / constraints.maxWidth).clamp(0, 1) * max,
          ),
          onHorizontalDragUpdate: (d) => onChanged(
            (d.localPosition.dx / constraints.maxWidth).clamp(0, 1) * max,
          ),
          child: Container(
            height: 24,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(12),
              gradient: gradient,
            ),
            child: Stack(
              children: [
                Positioned(
                  left: (value / max) * constraints.maxWidth - 10,
                  top: 0,
                  child: Container(
                    width: 20,
                    height: 24,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: Colors.white, width: 2),
                      boxShadow: [
                        BoxShadow(color: Colors.black26, blurRadius: 2),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
