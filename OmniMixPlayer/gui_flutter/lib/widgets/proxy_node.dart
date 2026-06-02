import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import 'package:url_launcher/url_launcher.dart';
import '../models/node_data.dart';

/// Parse a hex color string ("#RRGGBB" or "AARRGGBB") to Color.
Color? parseHexColor(String hex) {
  if (hex.isEmpty) return null;
  try {
    final h = hex.replaceFirst('#', '');
    if (h.length == 6) {
      return Color(int.parse('FF$h', radix: 16));
    } else if (h.length == 8) {
      return Color(int.parse(h, radix: 16));
    }
  } catch (_) {}
  return null;
}

/// Recursively renders a NodeData tree from JSON into Flutter widgets.
/// This is the Flutter equivalent of SlintProxyNode.
class ProxyNodeWidget extends StatelessWidget {
  final RawNodeData node;
  final void Function(String nodeId, String action, String value) onDispatch;
  final String imageBaseUrl;

  const ProxyNodeWidget({
    super.key,
    required this.node,
    required this.onDispatch,
    this.imageBaseUrl = '',
  });

  @override
  Widget build(BuildContext context) {
    switch (node.nodeType) {
      case 'Container':
        return _buildContainer(context);
      case 'Text':
        return _buildText(context);
      case 'Input':
        return _buildInput(context);
      case 'Button':
        return _buildButton(context);
      case 'ExternalLink':
        return _buildExternalLink(context);
      case 'Switch':
        return _buildSwitch(context);
      case 'Image':
        return _buildImage(context);
      case 'Select':
        return _buildSelect(context);
      case 'List':
        return _buildList(context);
      default:
        return const SizedBox.shrink();
    }
  }

  Widget _buildContainer(BuildContext context) {
    final children = node.children
        .map(
          (c) => ProxyNodeWidget(
            node: c,
            onDispatch: onDispatch,
            imageBaseUrl: imageBaseUrl,
          ),
        )
        .toList();

    final bgColor = parseHexColor(node.color);

    // Determine cross-axis alignment from optional property
    CrossAxisAlignment crossAlign;
    switch (node.crossAxisAlignment) {
      case 'start':
        crossAlign = CrossAxisAlignment.start;
      case 'center':
        crossAlign = CrossAxisAlignment.center;
      case 'end':
        crossAlign = CrossAxisAlignment.end;
      case 'stretch':
        crossAlign = CrossAxisAlignment.stretch;
      default:
        crossAlign = CrossAxisAlignment.start;
    }

    final container = node.direction == 'Horizontal'
        ? Row(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.center,
                spacing: node.spacing,
                children: children,
              )
              as Widget
        : Column(
            crossAxisAlignment: crossAlign,
            spacing: node.spacing,
            children: children,
          );

    if (bgColor != null) {
      return Container(
        width: double.infinity,
        decoration: BoxDecoration(color: bgColor),
        padding: EdgeInsets.all(node.padding),
        child: container,
      );
    }
    return Padding(padding: EdgeInsets.all(node.padding), child: container);
  }

  Widget _buildText(BuildContext context) {
    final color =
        _parseColor(node.color) ?? Theme.of(context).colorScheme.onSurface;
    return Text(
      node.text,
      style: TextStyle(fontSize: node.fontSize, color: color),
    );
  }

  Widget _buildInput(BuildContext context) {
    return _InputNodeWidget(node: node, onDispatch: onDispatch);
  }

  Widget _buildButton(BuildContext context) {
    // 优先使用 node.color，无则按 variant 降级到主题色
    final Color textColor;
    final explicitColor = parseHexColor(node.color);
    if (explicitColor != null) {
      textColor = explicitColor;
    } else {
      switch (node.buttonVariant) {
        case 'danger':
          textColor = Theme.of(context).colorScheme.error;
        case 'primary':
          textColor = Theme.of(context).colorScheme.primary;
        default:
          textColor = Colors.white;
      }
    }
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: TextButton(
        style: TextButton.styleFrom(
          foregroundColor: textColor,
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          minimumSize: Size.zero,
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
        ),
        onPressed: () => onDispatch(node.id, 'click', node.value),
        child: Text(node.text, style: const TextStyle(fontSize: 14)),
      ),
    );
  }

  Widget _buildExternalLink(BuildContext context) {
    final url = node.value.isNotEmpty ? node.value : node.source;
    final uri = Uri.tryParse(url);
    final explicitColor = parseHexColor(node.color);
    final textColor = explicitColor ?? Theme.of(context).colorScheme.primary;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: TextButton.icon(
        style: TextButton.styleFrom(
          foregroundColor: textColor,
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          minimumSize: Size.zero,
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
        ),
        onPressed: uri == null
            ? null
            : () async {
                await launchUrl(uri, mode: LaunchMode.externalApplication);
                onDispatch(node.id, 'open', url);
              },
        icon: const Icon(Icons.open_in_new_rounded, size: 16),
        label: Text(node.text, style: const TextStyle(fontSize: 14)),
      ),
    );
  }

  Widget _buildSwitch(BuildContext context) {
    return Row(
      spacing: 8,
      children: [
        Text(node.text, style: const TextStyle(fontSize: 14)),
        Switch(
          value: node.checked,
          onChanged: (v) => onDispatch(node.id, 'toggle', v.toString()),
        ),
      ],
    );
  }

  Widget _buildImage(BuildContext context) {
    if (node.source.isEmpty) {
      return const SizedBox.shrink();
    }
    final url = node.source.startsWith('/') && imageBaseUrl.isNotEmpty
        ? '$imageBaseUrl${node.source}'
        : node.source;
    return Image.network(
      url,
      width: node.imageWidth,
      height: node.imageHeight,
      fit: _parseBoxFit(node.imageFit),
      errorBuilder: (ctx, error, stack) {
        return const Icon(Icons.broken_image);
      },
    );
  }

  Widget _buildSelect(BuildContext context) {
    return Row(
      spacing: 8,
      children: [
        if (node.text.isNotEmpty)
          Text(node.text, style: const TextStyle(fontSize: 14)),
        DropdownButton<String>(
          value: node.selectedValue.isNotEmpty ? node.selectedValue : null,
          items: node.options
              .map(
                (o) => DropdownMenuItem(value: o.value, child: Text(o.label)),
              )
              .toList(),
          onChanged: (v) {
            if (v != null) onDispatch(node.id, 'change', v);
          },
        ),
      ],
    );
  }

  Widget _buildList(BuildContext context) {
    if (node.items.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: node.items
          .map(
            (item) => ProxyNodeWidget(
              node: item,
              onDispatch: onDispatch,
              imageBaseUrl: imageBaseUrl,
            ),
          )
          .toList(),
    );
  }

  Color? _parseColor(String hex) => parseHexColor(hex);

  BoxFit _parseBoxFit(String fit) {
    switch (fit) {
      case 'fill':
        return BoxFit.fill;
      case 'contain':
        return BoxFit.contain;
      case 'cover':
        return BoxFit.cover;
      default:
        return BoxFit.contain;
    }
  }
}

/// Stateful input with draft-on-blur workflow:
///
///   1. User types → local only
///   2. Focus leaves → mark "待保存" (pending), keep draft in controller
///   3. User clicks 保存 → dispatch to backend, clear pending
///   4. User clicks 取消 → revert to backend value, clear pending
///   5. Backend push while no pending → sync normally
///   6. Leave page → draft discarded (cancel implicitly)
///
/// This avoids complex focus-vs-push race conditions entirely.
class _InputNodeWidget extends StatefulWidget {
  final RawNodeData node;
  final void Function(String nodeId, String action, String value) onDispatch;

  const _InputNodeWidget({required this.node, required this.onDispatch});

  @override
  State<_InputNodeWidget> createState() => _InputNodeWidgetState();
}

class _InputNodeWidgetState extends State<_InputNodeWidget> {
  late final TextEditingController _controller;
  final _focusNode = FocusNode();
  bool _pending =
      false; // true = draft differs from backend, awaiting save/cancel

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.node.value);
    _controller.addListener(_onTextChanged);
    _focusNode.addListener(_onFocusChanged);
  }

  @override
  void didUpdateWidget(_InputNodeWidget oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!_focusNode.hasFocus && !_pending) {
      _syncFromBackend();
    }
  }

  @override
  void dispose() {
    _focusNode.removeListener(_onFocusChanged);
    _controller.removeListener(_onTextChanged);
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  bool get _isDirty => _controller.text != widget.node.value;

  void _onTextChanged() {
    // Update pending state reactively — if user clears draft back to
    // original value, auto-dismiss save/cancel buttons.
    if (_pending && !_isDirty) {
      setState(() => _pending = false);
    }
  }

  void _onFocusChanged() {
    if (!_focusNode.hasFocus && _isDirty && !_pending) {
      // User finished typing → mark as pending (draft kept in controller)
      setState(() => _pending = true);
    }
  }

  void _syncFromBackend() {
    final newValue = widget.node.value;
    if (_controller.text != newValue) {
      _controller.text = newValue;
    }
  }

  void _save() {
    widget.onDispatch(widget.node.id, 'change', _controller.text);
    setState(() => _pending = false);
  }

  void _cancel() {
    _controller.text = widget.node.value;
    setState(() => _pending = false);
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Row(
      spacing: 6,
      children: [
        if (widget.node.text.isNotEmpty)
          Text(widget.node.text, style: const TextStyle(fontSize: 14)),
        Expanded(
          child: TextField(
            controller: _controller,
            focusNode: _focusNode,
            obscureText: widget.node.inputType == 'password',
            decoration: InputDecoration(
              isDense: true,
              border: const OutlineInputBorder(),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 8,
              ),
              suffixIcon: _pending
                  ? Padding(
                      padding: const EdgeInsets.only(right: 4),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.edit_note_rounded,
                            size: 16,
                            color: cs.tertiary,
                          ),
                          const SizedBox(width: 2),
                          Text(
                            AppLocalizations.of(context)!.pendingSave,
                            style: TextStyle(fontSize: 11, color: cs.tertiary),
                          ),
                        ],
                      ),
                    )
                  : null,
            ),
          ),
        ),
        if (_pending) ...[
          // Save
          SizedBox(
            width: 32,
            height: 32,
            child: IconButton(
              icon: const Icon(Icons.check_rounded, size: 20),
              style: IconButton.styleFrom(
                backgroundColor: cs.primaryContainer,
                foregroundColor: cs.onPrimaryContainer,
                padding: EdgeInsets.zero,
                minimumSize: const Size(32, 32),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(6),
                ),
              ),
              onPressed: _save,
              tooltip: AppLocalizations.of(context)!.save,
            ),
          ),
          // Cancel
          SizedBox(
            width: 32,
            height: 32,
            child: IconButton(
              icon: const Icon(Icons.close_rounded, size: 20),
              style: IconButton.styleFrom(
                backgroundColor: cs.surfaceContainerHighest,
                foregroundColor: cs.onSurfaceVariant,
                padding: EdgeInsets.zero,
                minimumSize: const Size(32, 32),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(6),
                ),
              ),
              onPressed: _cancel,
              tooltip: AppLocalizations.of(context)!.cancel,
            ),
          ),
        ],
      ],
    );
  }
}
