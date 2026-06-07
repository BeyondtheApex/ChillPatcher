import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../providers/input/input_event_controller.dart';
import '../models/input/input_event.dart';

class ShortcutSettingsPage extends ConsumerStatefulWidget {
  final AppState appState;
  const ShortcutSettingsPage({super.key, required this.appState});

  @override
  ConsumerState<ShortcutSettingsPage> createState() =>
      _ShortcutSettingsPageState();
}

class _ShortcutSettingsPageState extends ConsumerState<ShortcutSettingsPage> {
  String? _capturingActionId;
  int? _capturingSlotIndex; // 0 for prefix, 1..4 for regular keys
  StreamSubscription<InputEvent>? _inputSubscription;

  @override
  void initState() {
    super.initState();
    // Suspend normal execution of shortcuts when setting them
    widget.appState.input.setShortcutsSuspended(true);
  }

  @override
  void dispose() {
    _inputSubscription?.cancel();
    widget.appState.input.setShortcutsSuspended(false);
    super.dispose();
  }

  void _startCapture(String actionId, int slotIndex) {
    _inputSubscription?.cancel();
    setState(() {
      _capturingActionId = actionId;
      _capturingSlotIndex = slotIndex;
    });

    _inputSubscription = widget.appState.input.events.listen((event) {
      if (!event.isPressed || event.key == null) return;
      if (event.key!.source != InputSource.gamepad) return;

      // Captured!
      final key = event.key!.forAnyDevice();
      _onKeyCaptured(actionId, slotIndex, key);
    });
  }

  bool _isKeyAlreadyUsed(
    CustomShortcutBinding binding,
    InputKeyId key,
    int currentSlotIndex,
  ) {
    // Check prefix slot (index 0)
    if (currentSlotIndex != 0 && binding.prefixKey == key) {
      return true;
    }
    // Check regular key slots (index 1..4)
    for (int i = 0; i < 4; i++) {
      final regSlotIndex = i + 1;
      if (currentSlotIndex != regSlotIndex && binding.regularKeys[i] == key) {
        return true;
      }
    }
    return false;
  }

  void _onKeyCaptured(String actionId, int slotIndex, InputKeyId key) {
    _inputSubscription?.cancel();
    _inputSubscription = null;

    final controller = widget.appState.input;
    final binding =
        controller.getBindingForAction(actionId) ??
        CustomShortcutBinding(actionId: actionId);

    // Enforce uniqueness across all slots in the row
    if (_isKeyAlreadyUsed(binding, key, slotIndex)) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            Localizations.localeOf(context).languageCode == 'zh'
                ? '该按键已被绑定在当前快捷键中，不能重复设置！'
                : 'This button is already configured in this shortcut!',
          ),
          duration: const Duration(seconds: 2),
        ),
      );
      setState(() {
        _capturingActionId = null;
        _capturingSlotIndex = null;
      });
      return;
    }

    CustomShortcutBinding updated;
    if (slotIndex == 0) {
      updated = binding.copyWith(prefixKey: key);
    } else {
      final regKeys = List<InputKeyId?>.from(binding.regularKeys);
      regKeys[slotIndex - 1] = key;
      updated = binding.copyWith(regularKeys: regKeys);
    }

    controller.saveCustomBinding(updated);

    setState(() {
      _capturingActionId = null;
      _capturingSlotIndex = null;
    });
  }

  void _clearSlot(String actionId, int slotIndex) {
    final controller = widget.appState.input;
    final binding = controller.getBindingForAction(actionId);
    if (binding == null) return;

    CustomShortcutBinding updated;
    if (slotIndex == 0) {
      updated = binding.copyWith(prefixKey: null);
    } else {
      final regKeys = List<InputKeyId?>.from(binding.regularKeys);
      regKeys[slotIndex - 1] = null;

      // If all regular keys are now null, clear the prefix key too!
      final hasRegularKeys = regKeys.any((k) => k != null);
      if (!hasRegularKeys) {
        updated = binding.copyWith(regularKeys: regKeys, prefixKey: null);
      } else {
        updated = binding.copyWith(regularKeys: regKeys);
      }
    }

    final hasKeys =
        updated.prefixKey != null || updated.regularKeys.any((k) => k != null);
    if (hasKeys) {
      controller.saveCustomBinding(updated);
    } else {
      controller.clearCustomBinding(actionId);
    }
    setState(() {});
  }

  void _toggleNegation(String actionId) {
    final controller = widget.appState.input;
    final binding =
        controller.getBindingForAction(actionId) ??
        CustomShortcutBinding(actionId: actionId);
    final updated = binding.copyWith(prefixNegated: !binding.prefixNegated);
    controller.saveCustomBinding(updated);
    setState(() {});
  }

  void _toggleOperator(String actionId, int opIndex) {
    final controller = widget.appState.input;
    final binding =
        controller.getBindingForAction(actionId) ??
        CustomShortcutBinding(actionId: actionId);
    final ops = List<String>.from(binding.operators);
    ops[opIndex] = ops[opIndex] == 'and' ? 'or' : 'and';
    final updated = binding.copyWith(operators: ops);
    controller.saveCustomBinding(updated);
    setState(() {});
  }

  void _clearActionBinding(String actionId) {
    widget.appState.input.clearCustomBinding(actionId);
    setState(() {});
  }

  String _resolveDescription(AppLocalizations l10n, String key) {
    switch (key) {
      case 'shortcutPlayPause':
        return l10n.shortcutPlayPause;
      case 'shortcutNext':
        return l10n.shortcutNext;
      case 'shortcutPrev':
        return l10n.shortcutPrev;
      case 'shortcutVolUp':
        return l10n.shortcutVolUp;
      case 'shortcutVolDown':
        return l10n.shortcutVolDown;
      case 'shortcutToggleFloatingPlayer':
        return l10n.shortcutToggleFloatingPlayer;
      case 'shortcutCenterLeftQuad':
        return l10n.shortcutCenterLeftQuad;
      default:
        return key;
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cs = Theme.of(context).colorScheme;
    final controller = widget.appState.input;
    final actions = controller.registeredActions;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.shortcutSettings),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: actions.length,
        itemBuilder: (context, index) {
          final action = actions[index];
          final binding = controller.getBindingForAction(action.id);
          final description = _resolveDescription(l10n, action.descriptionKey);

          return Card(
            margin: const EdgeInsets.only(bottom: 16),
            elevation: 0,
            color: cs.surfaceContainerLow,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
              side: BorderSide(color: cs.outlineVariant.withValues(alpha: 0.4)),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          description,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Visibility(
                        visible: binding != null,
                        maintainSize: true,
                        maintainAnimation: true,
                        maintainState: true,
                        child: SizedBox(
                          width: 28,
                          height: 28,
                          child: IconButton(
                            onPressed: () => _clearActionBinding(action.id),
                            icon: const Icon(
                              Icons.close,
                              size: 14,
                              color: Colors.white,
                            ),
                            padding: EdgeInsets.zero,
                            style: IconButton.styleFrom(
                              backgroundColor: Colors.red,
                              hoverColor: Colors.redAccent,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(6),
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: [
                        // --- PREFIX KEY SECTION ---
                        _buildPrefixSection(action.id, binding, l10n),
                        const SizedBox(width: 12),
                        Container(
                          height: 32,
                          width: 1.2,
                          color: cs.outlineVariant.withValues(alpha: 0.6),
                        ),
                        const SizedBox(width: 12),

                        // --- REGULAR KEYS SECTION ---
                        _buildRegularKeysSection(action.id, binding, l10n),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildPrefixSection(
    String actionId,
    CustomShortcutBinding? binding,
    AppLocalizations l10n,
  ) {
    final cs = Theme.of(context).colorScheme;
    final prefixKey = binding?.prefixKey;
    final prefixNegated = binding?.prefixNegated ?? false;
    final hasRegularKeys = binding?.regularKeys.any((k) => k != null) ?? false;

    final isCapturing =
        _capturingActionId == actionId && _capturingSlotIndex == 0;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        // Negation indicator toggle
        GestureDetector(
          onTap: (prefixKey != null && hasRegularKeys)
              ? () => _toggleNegation(actionId)
              : null,
          child: Opacity(
            opacity: (prefixKey != null && hasRegularKeys) ? 1.0 : 0.3,
            child: Container(
              width: 28,
              height: 38,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: (prefixKey != null && hasRegularKeys)
                    ? (prefixNegated
                          ? Colors.red.withValues(alpha: 0.12)
                          : Colors.green.withValues(alpha: 0.12))
                    : cs.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                  color: (prefixKey != null && hasRegularKeys)
                      ? (prefixNegated ? Colors.red : Colors.green)
                      : cs.outlineVariant.withValues(alpha: 0.5),
                  width: 1.2,
                ),
              ),
              child: Text(
                prefixNegated ? '!' : '+',
                style: TextStyle(
                  color: (prefixKey != null && hasRegularKeys)
                      ? (prefixNegated ? Colors.red : Colors.green)
                      : cs.onSurfaceVariant.withValues(alpha: 0.5),
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
            ),
          ),
        ),
        const SizedBox(width: 8),

        // Prefix slot (disabled if no regular keys set)
        if (!hasRegularKeys)
          Opacity(
            opacity: 0.3,
            child: _buildEmptySlot(
              label: l10n.prefixKey,
              isCapturing: false,
              onTap: () {},
            ),
          )
        else if (prefixKey != null)
          GestureDetector(
            onTap: () => _clearSlot(actionId, 0),
            child: _buildKeyBadge(prefixKey, () => _clearSlot(actionId, 0)),
          )
        else
          _buildEmptySlot(
            label: l10n.prefixKey,
            isCapturing: isCapturing,
            onTap: () => _startCapture(actionId, 0),
          ),
      ],
    );
  }

  Widget _buildRegularKeysSection(
    String actionId,
    CustomShortcutBinding? binding,
    AppLocalizations l10n,
  ) {
    final List<Widget> widgets = [];

    for (int i = 0; i < 4; i++) {
      final slotIndex = i + 1;
      final key = binding?.regularKeys[i];
      final isCapturing =
          _capturingActionId == actionId && _capturingSlotIndex == slotIndex;

      // Build key slot
      if (key != null) {
        widgets.add(_buildKeyBadge(key, () => _clearSlot(actionId, slotIndex)));
      } else {
        widgets.add(
          _buildEmptySlot(
            label: l10n.regularKeySlot(slotIndex),
            isCapturing: isCapturing,
            onTap: () => _startCapture(actionId, slotIndex),
          ),
        );
      }

      // Build operator between slots
      if (i < 3) {
        final opIndex = i;
        final op = binding?.operators[opIndex] ?? 'and';
        widgets.add(
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: _buildOperatorBadge(
              op,
              () => _toggleOperator(actionId, opIndex),
            ),
          ),
        );
      }
    }

    return Row(mainAxisSize: MainAxisSize.min, children: widgets);
  }

  Widget _buildOperatorBadge(String op, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 44,
        height: 38,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: op == 'and'
              ? Colors.blue.withValues(alpha: 0.12)
              : Colors.orange.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(
            color: op == 'and' ? Colors.blue : Colors.orange,
            width: 1.2,
          ),
        ),
        child: Text(
          op.toUpperCase(),
          style: TextStyle(
            color: op == 'and' ? Colors.blue : Colors.orange,
            fontWeight: FontWeight.bold,
            fontSize: 10,
          ),
        ),
      ),
    );
  }

  Widget _buildKeyBadge(InputKeyId key, VoidCallback onClear) {
    final code = key.code;
    String label = code;
    Color bg = Colors.grey[800]!;
    Color textCol = Colors.white;

    switch (code) {
      case 'a':
        label = 'A';
        bg = const Color(0xFF2E7D32); // Green
        break;
      case 'b':
        label = 'B';
        bg = const Color(0xFFC62828); // Red
        break;
      case 'x':
        label = 'X';
        bg = const Color(0xFF1565C0); // Blue
        break;
      case 'y':
        label = 'Y';
        bg = const Color(0xFFF9A825); // Yellow
        textCol = Colors.black;
        break;
      case 'leftBumper':
        label = 'LB';
        bg = const Color(0xFF37474F);
        break;
      case 'rightBumper':
        label = 'RB';
        bg = const Color(0xFF37474F);
        break;
      case 'leftTrigger':
        label = 'LT';
        bg = const Color(0xFFE65100);
        break;
      case 'rightTrigger':
        label = 'RT';
        bg = const Color(0xFFE65100);
        break;
      case 'leftStick':
        label = 'LS';
        bg = const Color(0xFF6A1B9A);
        break;
      case 'rightStick':
        label = 'RS';
        bg = const Color(0xFF6A1B9A);
        break;
      case 'start':
        label = 'Start';
        bg = const Color(0xFF006064);
        break;
      case 'back':
        label = 'Back';
        bg = const Color(0xFF006064);
        break;
      case 'dpadUp':
        label = '↑';
        bg = const Color(0xFF424242);
        break;
      case 'dpadDown':
        label = '↓';
        bg = const Color(0xFF424242);
        break;
      case 'dpadLeft':
        label = '←';
        bg = const Color(0xFF424242);
        break;
      case 'dpadRight':
        label = '→';
        bg = const Color(0xFF424242);
        break;
    }

    return Container(
      width: 80,
      height: 38,
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(8),
        boxShadow: [
          BoxShadow(
            color: bg.withValues(alpha: 0.3),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Stack(
        alignment: Alignment.center,
        children: [
          Positioned(
            left: 4,
            right: 22,
            child: Center(
              child: Text(
                label,
                style: TextStyle(
                  color: textCol,
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ),
          Positioned(
            right: 6,
            child: GestureDetector(
              onTap: onClear,
              child: Icon(
                Icons.close,
                size: 14,
                color: textCol.withValues(alpha: 0.7),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptySlot({
    required String label,
    required bool isCapturing,
    required VoidCallback onTap,
  }) {
    final cs = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 80,
        height: 38,
        decoration: BoxDecoration(
          border: Border.all(
            color: isCapturing
                ? Colors.amber
                : cs.outlineVariant.withValues(alpha: 0.6),
            style: BorderStyle.solid,
            width: isCapturing ? 2.0 : 1.2,
          ),
          borderRadius: BorderRadius.circular(8),
          color: isCapturing
              ? Colors.amber.withValues(alpha: 0.08)
              : Colors.transparent,
        ),
        child: Center(
          child: isCapturing
              ? const SizedBox(
                  width: 14,
                  height: 14,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    valueColor: AlwaysStoppedAnimation(Colors.amber),
                  ),
                )
              : Text(
                  label,
                  style: TextStyle(
                    fontSize: 11,
                    color: cs.onSurfaceVariant.withValues(alpha: 0.5),
                  ),
                ),
        ),
      ),
    );
  }
}
