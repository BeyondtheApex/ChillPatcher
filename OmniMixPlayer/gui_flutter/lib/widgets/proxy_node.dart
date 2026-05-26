import 'package:flutter/material.dart';
import '../models/node_data.dart';
import '../services/logger.dart';

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

    if (node.direction == 'Horizontal') {
      return Padding(
        padding: EdgeInsets.all(node.padding),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          spacing: node.spacing,
          children: children,
        ),
      );
    } else {
      return Padding(
        padding: EdgeInsets.all(node.padding),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: node.spacing,
          children: children,
        ),
      );
    }
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
    final controller = TextEditingController(text: node.value);
    return Row(
      spacing: 8,
      children: [
        if (node.text.isNotEmpty)
          Text(node.text, style: const TextStyle(fontSize: 14)),
        Expanded(
          child: TextField(
            controller: controller,
            obscureText: node.inputType == 'password',
            decoration: InputDecoration(
              isDense: true,
              border: const OutlineInputBorder(),
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 8,
              ),
            ),
            onSubmitted: (v) => onDispatch(node.id, 'change', v),
          ),
        ),
      ],
    );
  }

  Widget _buildButton(BuildContext context) {
    final isDanger = node.buttonVariant == 'danger';
    return ElevatedButton(
      style: ElevatedButton.styleFrom(
        backgroundColor: isDanger
            ? Theme.of(context).colorScheme.error
            : Theme.of(context).colorScheme.primary,
        foregroundColor: isDanger
            ? Theme.of(context).colorScheme.onError
            : Theme.of(context).colorScheme.onPrimary,
      ),
      onPressed: () => onDispatch(node.id, 'click', ''),
      child: Text(node.text),
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
      GuiLogger().warn('_buildImage: empty source for node id=${node.id}');
      return const SizedBox.shrink();
    }
    final url = node.source.startsWith('/') && imageBaseUrl.isNotEmpty
        ? '$imageBaseUrl${node.source}'
        : node.source;
    GuiLogger().info(
      '_buildImage: id=${node.id} source=${node.source} baseUrl=$imageBaseUrl finalUrl=$url',
    );
    return Image.network(
      url,
      width: node.imageWidth,
      height: node.imageHeight,
      fit: _parseBoxFit(node.imageFit),
      errorBuilder: (ctx, error, stack) {
        GuiLogger().error(
          '_buildImage FAILED: id=${node.id} url=$url',
          error,
          stack,
        );
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

  Color? _parseColor(String hex) {
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
