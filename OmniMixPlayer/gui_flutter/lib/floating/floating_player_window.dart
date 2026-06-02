import "dart:convert";
import "dart:math" as math;
import "dart:ui";

import "package:desktop_multi_window/desktop_multi_window.dart";
import "package:dynamic_color/dynamic_color.dart";
import "package:flutter/material.dart";
import "package:screen_retriever/screen_retriever.dart";
import "package:shared_preferences/shared_preferences.dart";
import "package:window_manager/window_manager.dart";

import "../services/floating_window_service.dart";
import "../models/input/input_event.dart";

class FloatingPlayerWindowApp extends StatefulWidget {
  final WindowController controller;
  final FloatingPlayerSnapshot initialSnapshot;

  const FloatingPlayerWindowApp({
    super.key,
    required this.controller,
    required this.initialSnapshot,
  });

  @override
  State<FloatingPlayerWindowApp> createState() =>
      _FloatingPlayerWindowAppState();
}

class _FloatingPlayerWindowAppState extends State<FloatingPlayerWindowApp>
    with TickerProviderStateMixin, WindowListener {
  static const _windowSize = Size(300, 560);

  late FloatingPlayerSnapshot _snapshot = widget.initialSnapshot;
  final _controlChannel = const WindowMethodChannel(
    FloatingWindowService.controlChannelName,
    mode: ChannelMode.unidirectional,
  );
  double? _draggingPosition;

  // Position tracking
  Offset? _lastPosition;
  bool _isAnimating = false;
  AnimationController? _moveController;
  Animation<Offset>? _moveAnimation;

  bool _isMouseHovered = false;

  // Gamepad/keyboard interaction V always active when visible
  bool _isPlayerVisible = false;
  int? _highlightedButtonIndex;
  InputKeyId? _triggerKey;
  bool _isCapturingTriggerKey = false;
  bool _showSettingsPanel = false;

  final _prevKey = GlobalKey<_RoundIconButtonState>();
  final _toggleKey = GlobalKey<_RoundIconButtonState>();
  final _nextKey = GlobalKey<_RoundIconButtonState>();

  @override
  void initState() {
    super.initState();
    windowManager.addListener(this);
    if (widget.initialSnapshot.lastManualX != 0.0 ||
        widget.initialSnapshot.lastManualY != 0.0) {
      _lastPosition = Offset(
        widget.initialSnapshot.lastManualX,
        widget.initialSnapshot.lastManualY,
      );
    }
    _initWindow();
    _loadSettings();
  }

  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    final sourceStr = prefs.getString("floating_trigger_key_source");
    final code = prefs.getString("floating_trigger_key_code");
    final deviceId = prefs.getString("floating_trigger_key_deviceId");
    if (sourceStr != null && code != null) {
      _triggerKey = InputKeyId(
        source: InputSource.values.firstWhere((e) => e.name == sourceStr),
        code: code,
        deviceId: deviceId,
      );
    } else {
      _triggerKey = const InputKeyId(source: InputSource.gamepad, code: "a");
    }
  }

  Future<void> _saveTriggerKey(InputKeyId key) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString("floating_trigger_key_source", key.source.name);
    await prefs.setString("floating_trigger_key_code", key.code);
    if (key.deviceId != null) {
      await prefs.setString("floating_trigger_key_deviceId", key.deviceId!);
    } else {
      await prefs.remove("floating_trigger_key_deviceId");
    }
    setState(() {
      _triggerKey = key;
    });
  }

  Future<void> _clearTriggerKey() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove("floating_trigger_key_source");
    await prefs.remove("floating_trigger_key_code");
    await prefs.remove("floating_trigger_key_deviceId");
    setState(() {
      _triggerKey = null;
    });
  }

  void _handleFloatingInputEvent(InputEvent event) {
    if (_isCapturingTriggerKey) {
      if (event.isPressed && event.key != null) {
        _isCapturingTriggerKey = false;
        _saveTriggerKey(event.key!);
      }
      return;
    }
    if (!_isPlayerVisible) return;
    if (_highlightedButtonIndex == null) return;
    if (event.isPressed && event.key != null) {
      final code = event.key!.code;
      if (code == "dpadLeft" ||
          code == "leftStickLeft" ||
          code == "ArrowLeft") {
        setState(() {
          _highlightedButtonIndex = (_highlightedButtonIndex! - 1).clamp(0, 2);
        });
      } else if (code == "dpadRight" ||
          code == "leftStickRight" ||
          code == "ArrowRight") {
        setState(() {
          _highlightedButtonIndex = (_highlightedButtonIndex! + 1).clamp(0, 2);
        });
      } else if (_triggerKey != null && event.key!.matches(_triggerKey!)) {
        _triggerHighlightedButtonClick();
      }
    }
  }

  void _triggerHighlightedButtonClick() {
    if (_highlightedButtonIndex == 0) {
      _prevKey.currentState?.triggerClick();
    } else if (_highlightedButtonIndex == 1) {
      _toggleKey.currentState?.triggerClick();
    } else if (_highlightedButtonIndex == 2) {
      _nextKey.currentState?.triggerClick();
    }
  }

  @override
  void dispose() {
    windowManager.removeListener(this);
    _moveController?.dispose();
    super.dispose();
  }

  Future<void> _initWindow() async {
    await windowManager.ensureInitialized();
    final options = WindowOptions(
      size: _windowSize,
      center: true,
      minimumSize: _windowSize,
      maximumSize: _windowSize,
      alwaysOnTop: true,
      backgroundColor: Colors.transparent,
      skipTaskbar: true,
      titleBarStyle: TitleBarStyle.hidden,
      title: "OmniMixPlayer Floating Player",
    );
    windowManager.waitUntilReadyToShow(options, () async {
      await windowManager.setAsFrameless();
      await windowManager.setResizable(false);
      await windowManager.setAlwaysOnTop(true);
      await windowManager.show(inactive: true);
      if (_lastPosition != null) {
        await windowManager.setPosition(_lastPosition!);
      } else {
        final pos = await windowManager.getPosition();
        _lastPosition = pos;
        _invoke("update_manual_position", {"x": pos.dx, "y": pos.dy});
      }
      if (mounted)
        setState(() {
          _highlightedButtonIndex = 1;
        });
    });

    await widget.controller.setWindowMethodHandler((call) async {
      if (call.method == "update_player" && call.arguments is Map) {
        final next = FloatingPlayerSnapshot.fromJson(
          Map<String, dynamic>.from(call.arguments as Map),
        );
        if (mounted) {
          setState(() {
            _snapshot = next;
            _draggingPosition = null;
            if (next.lastManualX != 0.0 || next.lastManualY != 0.0) {
              _lastPosition = Offset(next.lastManualX, next.lastManualY);
            }
          });
        }
        return true;
      } else if (call.method == "move_to_center_left_quad") {
        await _moveToCenterLeftQuad();
        return true;
      } else if (call.method == "visibility_changed") {
        final visible = call.arguments as bool? ?? false;
        _isPlayerVisible = visible;
        if (mounted)
          setState(() {
            _highlightedButtonIndex = visible ? 1 : null;
          });
        return true;
      } else if (call.method == "input_event" && call.arguments is Map) {
        _handleFloatingInputEvent(
          InputEvent.fromJson(Map<String, dynamic>.from(call.arguments as Map)),
        );
        return true;
      }
      return false;
    });
    await _controlChannel.invokeMethod("ready");
  }

  Future<void> _moveToCenterLeftQuad() async {
    final currentPos = await windowManager.getPosition();
    List<Display> displays;
    try {
      displays = await screenRetriever.getAllDisplays();
    } catch (_) {
      displays = [];
    }
    final windowCenter = Offset(
      currentPos.dx + _windowSize.width / 2,
      currentPos.dy + _windowSize.height / 2,
    );
    Display? activeDisplay;
    for (final display in displays) {
      final pos = display.visiblePosition ?? Offset.zero;
      final size = display.size;
      if (Rect.fromLTWH(
        pos.dx,
        pos.dy,
        size.width,
        size.height,
      ).contains(windowCenter)) {
        activeDisplay = display;
        break;
      }
    }
    final d = activeDisplay ?? (displays.isNotEmpty ? displays.first : null);
    final dp = d?.visiblePosition ?? Offset.zero;
    final ds = d?.size ?? const Size(1920, 1080);
    final targetX = dp.dx + ds.width / 4 - _windowSize.width / 2;
    final targetY = dp.dy + ds.height / 2 - _windowSize.height / 2;
    final targetPos = Offset(targetX, targetY);
    _lastPosition = targetPos;
    _isAnimating = true;
    await _animateWindowTo(targetPos);
    _isAnimating = false;
    await _invoke("update_manual_position", {"x": targetX, "y": targetY});
  }

  @override
  void onWindowMove() async {
    if (_isAnimating) return;
    final pos = await windowManager.getPosition();
    _lastPosition = pos;
    _invoke("update_manual_position", {"x": pos.dx, "y": pos.dy});
  }

  Future<void> _animateWindowTo(Offset targetPos) async {
    _moveController?.stop();
    _moveController?.dispose();
    final startPos = await windowManager.getPosition();
    _moveController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 300),
    );
    _moveAnimation = Tween<Offset>(begin: startPos, end: targetPos).animate(
      CurvedAnimation(parent: _moveController!, curve: Curves.easeOutCubic),
    );
    _moveController!.addListener(() {
      windowManager.setPosition(_moveAnimation!.value);
    });
    await _moveController!.forward();
  }

  Future<void> _invoke(String method, [Object? args]) async {
    try {
      await _controlChannel.invokeMethod(method, args);
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    return DynamicColorBuilder(
      builder: (lightDynamic, darkDynamic) {
        final seed = Color(_snapshot.seedColor);
        final lightScheme = (_snapshot.useSystemColor && lightDynamic != null)
            ? lightDynamic
            : ColorScheme.fromSeed(seedColor: seed);
        final darkScheme = (_snapshot.useSystemColor && darkDynamic != null)
            ? darkDynamic
            : ColorScheme.fromSeed(
                seedColor: seed,
                brightness: Brightness.dark,
              );
        final themeMode = switch (_snapshot.themeMode) {
          "light" => ThemeMode.light,
          "dark" => ThemeMode.dark,
          _ => ThemeMode.system,
        };
        return MaterialApp(
          debugShowCheckedModeBanner: false,
          themeMode: themeMode,
          theme: ThemeData(
            colorScheme: lightScheme,
            useMaterial3: true,
            brightness: Brightness.light,
          ),
          darkTheme: ThemeData(
            colorScheme: darkScheme,
            useMaterial3: true,
            brightness: Brightness.dark,
          ),
          home: _showSettingsPanel
              ? _FloatingSettingsPanel(
                  triggerKey: _triggerKey,
                  isCapturing: _isCapturingTriggerKey,
                  onStartCapture: () =>
                      setState(() => _isCapturingTriggerKey = true),
                  onClearKey: _clearTriggerKey,
                  onBack: () => setState(() {
                    _showSettingsPanel = false;
                    _isCapturingTriggerKey = false;
                  }),
                )
              : MouseRegion(
                  onEnter: (_) => setState(() => _isMouseHovered = true),
                  onExit: (_) {
                    setState(() {
                      _isMouseHovered = false;
                    });
                  },
                  child: _FloatingPlayerCard(
                    snapshot: _snapshot,
                    draggingPosition: _draggingPosition,
                    isMouseHovered: _isMouseHovered,
                    highlightedButtonIndex: _highlightedButtonIndex,
                    onDragPosition: (v) =>
                        setState(() => _draggingPosition = v),
                    onSeek: (v) async {
                      setState(() => _draggingPosition = null);
                      await _invoke("seek", v);
                    },
                    onToggle: () => _invoke("toggle"),
                    onPrevious: () => _invoke("previous"),
                    onNext: () => _invoke("next"),
                    prevKey: _prevKey,
                    toggleKey: _toggleKey,
                    nextKey: _nextKey,
                    onOpenSettings: () =>
                        setState(() => _showSettingsPanel = true),
                  ),
                ),
        );
      },
    );
  }
}

// _FloatingPlayerCard V no snap/cotentOffset params, always draggable
class _FloatingPlayerCard extends StatelessWidget {
  final FloatingPlayerSnapshot snapshot;
  final double? draggingPosition;
  final bool isMouseHovered;
  final int? highlightedButtonIndex;
  final ValueChanged<double> onDragPosition;
  final ValueChanged<double> onSeek;
  final VoidCallback onToggle;
  final VoidCallback onPrevious;
  final VoidCallback onNext;
  final GlobalKey<_RoundIconButtonState> prevKey;
  final GlobalKey<_RoundIconButtonState> toggleKey;
  final GlobalKey<_RoundIconButtonState> nextKey;
  final VoidCallback onOpenSettings;

  const _FloatingPlayerCard({
    required this.snapshot,
    required this.draggingPosition,
    required this.isMouseHovered,
    required this.highlightedButtonIndex,
    required this.onDragPosition,
    required this.onSeek,
    required this.onToggle,
    required this.onPrevious,
    required this.onNext,
    required this.prevKey,
    required this.toggleKey,
    required this.nextKey,
    required this.onOpenSettings,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final duration = snapshot.duration <= 0 ? 1.0 : snapshot.duration;
    final position = (draggingPosition ?? snapshot.position)
        .clamp(0.0, duration)
        .toDouble();

    return Scaffold(
      backgroundColor: Colors.transparent,
      body: Center(
        child: SizedBox(
          width: 300,
          height: 560,
          child: Padding(
            padding: const EdgeInsets.all(10),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(22),
              child: BackdropFilter(
                filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: cs.surfaceContainer.withAlpha(238),
                    borderRadius: BorderRadius.circular(22),
                    border: Border.all(color: cs.outlineVariant.withAlpha(120)),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withAlpha(45),
                        blurRadius: 24,
                        offset: const Offset(0, 12),
                      ),
                    ],
                  ),
                  child: Stack(
                    children: [
                      Padding(
                        padding: const EdgeInsets.fromLTRB(18, 18, 18, 14),
                        child: Column(
                          children: [
                            GestureDetector(
                              behavior: HitTestBehavior.translucent,
                              onPanStart: (_) => windowManager.startDragging(),
                              child: Column(
                                children: [
                                  _Cover(snapshot: snapshot),
                                  const SizedBox(height: 16),
                                  Text(
                                    snapshot.hasTrack &&
                                            snapshot.title.isNotEmpty
                                        ? snapshot.title
                                        : "\u6682\u65e0\u64ad\u653e",
                                    maxLines: 2,
                                    overflow: TextOverflow.ellipsis,
                                    textAlign: TextAlign.center,
                                    style: const TextStyle(
                                      fontSize: 19,
                                      fontWeight: FontWeight.w800,
                                      height: 1.15,
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    snapshot.artist.isNotEmpty
                                        ? snapshot.artist
                                        : "\u672a\u77e5\u827a\u672f\u5bb6",
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    textAlign: TextAlign.center,
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: cs.onSurfaceVariant,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 12),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                _RoundIconButton(
                                  key: prevKey,
                                  icon: Icons.skip_previous_rounded,
                                  enabled: snapshot.canControl,
                                  onPressed: onPrevious,
                                  highlighted: highlightedButtonIndex == 0,
                                ),
                                const SizedBox(width: 12),
                                _RoundIconButton(
                                  key: toggleKey,
                                  icon: snapshot.isPlaying
                                      ? Icons.pause_rounded
                                      : Icons.play_arrow_rounded,
                                  large: true,
                                  enabled: snapshot.canControl,
                                  onPressed: onToggle,
                                  highlighted: highlightedButtonIndex == 1,
                                ),
                                const SizedBox(width: 12),
                                _RoundIconButton(
                                  key: nextKey,
                                  icon: Icons.skip_next_rounded,
                                  enabled: snapshot.canControl,
                                  onPressed: onNext,
                                  highlighted: highlightedButtonIndex == 2,
                                ),
                              ],
                            ),
                            const Spacer(),
                            SliderTheme(
                              data: SliderTheme.of(context).copyWith(
                                trackHeight: 3,
                                thumbShape: const RoundSliderThumbShape(
                                  enabledThumbRadius: 6,
                                  disabledThumbRadius: 5,
                                ),
                                overlayShape: const RoundSliderOverlayShape(
                                  overlayRadius: 14,
                                ),
                              ),
                              child: Slider(
                                value: position,
                                min: 0,
                                max: duration,
                                onChanged:
                                    snapshot.canControl && snapshot.duration > 0
                                    ? onDragPosition
                                    : null,
                                onChangeEnd:
                                    snapshot.canControl && snapshot.duration > 0
                                    ? onSeek
                                    : null,
                              ),
                            ),
                            Row(
                              children: [
                                Text(_fmt(position)),
                                const Spacer(),
                                Text(_fmt(snapshot.duration)),
                              ],
                            ),
                          ],
                        ),
                      ),
                      Positioned(
                        top: 14,
                        right: 14,
                        child: AnimatedOpacity(
                          opacity: isMouseHovered ? 1.0 : 0.0,
                          duration: const Duration(milliseconds: 200),
                          curve: Curves.easeInOut,
                          child: IgnorePointer(
                            ignoring: !isMouseHovered,
                            child: IconButton(
                              icon: const Icon(Icons.settings_rounded),
                              onPressed: onOpenSettings,
                              style: IconButton.styleFrom(
                                backgroundColor: cs.surfaceContainerHighest
                                    .withAlpha(150),
                                foregroundColor: cs.onSurfaceVariant,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  static String _fmt(double sec) {
    if (sec <= 0) return "0:00";
    final m = sec ~/ 60;
    final s = (sec % 60).toInt().toString().padLeft(2, "0");
    return "$m:$s";
  }
}

// ── Settings panel (extensible, currently only trigger key) ──
class _FloatingSettingsPanel extends StatelessWidget {
  final InputKeyId? triggerKey;
  final bool isCapturing;
  final VoidCallback onStartCapture;
  final VoidCallback onClearKey;
  final VoidCallback onBack;

  const _FloatingSettingsPanel({
    required this.triggerKey,
    required this.isCapturing,
    required this.onStartCapture,
    required this.onClearKey,
    required this.onBack,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: Center(
        child: SizedBox(
          width: 300,
          height: 560,
          child: Padding(
            padding: const EdgeInsets.all(10),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(22),
              child: BackdropFilter(
                filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: cs.surfaceContainer.withAlpha(238),
                    borderRadius: BorderRadius.circular(22),
                    border: Border.all(color: cs.outlineVariant.withAlpha(120)),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withAlpha(45),
                        blurRadius: 24,
                        offset: const Offset(0, 12),
                      ),
                    ],
                  ),
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Header
                        Row(
                          children: [
                            IconButton(
                              icon: const Icon(Icons.arrow_back_rounded),
                              onPressed: onBack,
                              style: IconButton.styleFrom(
                                backgroundColor: cs.surfaceContainerHighest
                                    .withAlpha(150),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Text(
                              '\u8bbe\u7f6e',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w700,
                                color: cs.onSurface,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 20),

                        // Trigger key item
                        _TriggerKeyItem(
                          triggerKey: triggerKey,
                          isCapturing: isCapturing,
                          onStartCapture: onStartCapture,
                          onClearKey: onClearKey,
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Single trigger key settings item ──
class _TriggerKeyItem extends StatelessWidget {
  final InputKeyId? triggerKey;
  final bool isCapturing;
  final VoidCallback onStartCapture;
  final VoidCallback onClearKey;

  const _TriggerKeyItem({
    required this.triggerKey,
    required this.isCapturing,
    required this.onStartCapture,
    required this.onClearKey,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card(
      elevation: 0,
      color: cs.surfaceContainerLow,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: cs.outlineVariant.withOpacity(0.4)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              '\u89e6\u53d1\u5feb\u6377\u952e',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: cs.onSurface,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              '\u89e6\u53d1\u60ac\u6d6e\u7a97\u64ad\u653e/\u6682\u505c',
              style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
            ),
            const SizedBox(height: 16),
            // Key badge / empty slot
            if (triggerKey != null)
              _buildKeyBadge(cs, triggerKey!, onClearKey)
            else
              _buildEmptySlot(cs, isCapturing, onStartCapture),
          ],
        ),
      ),
    );
  }

  Widget _buildKeyBadge(ColorScheme cs, InputKeyId key, VoidCallback onClear) {
    final label = _keyLabel(key.code);
    final bg = _keyColor(key.code);
    final textCol = (key.code == 'y') ? Colors.black : Colors.white;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        GestureDetector(
          onTap: onClear,
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
            decoration: BoxDecoration(
              color: bg,
              borderRadius: BorderRadius.circular(8),
              boxShadow: [
                BoxShadow(
                  color: bg.withOpacity(0.3),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    color: textCol,
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
                const SizedBox(width: 8),
                Icon(Icons.close, size: 14, color: textCol.withOpacity(0.7)),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildEmptySlot(ColorScheme cs, bool isCapturing, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
        decoration: BoxDecoration(
          border: Border.all(
            color: isCapturing
                ? Colors.amber
                : cs.outlineVariant.withOpacity(0.6),
            width: isCapturing ? 2.0 : 1.2,
          ),
          borderRadius: BorderRadius.circular(8),
          color: isCapturing
              ? Colors.amber.withOpacity(0.08)
              : Colors.transparent,
        ),
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
                '\u70b9\u51fb\u8bbe\u7f6e',
                style: TextStyle(
                  fontSize: 13,
                  color: cs.onSurfaceVariant.withOpacity(0.5),
                ),
              ),
      ),
    );
  }

  static String _keyLabel(String code) {
    switch (code) {
      case 'a':
        return 'A';
      case 'b':
        return 'B';
      case 'x':
        return 'X';
      case 'y':
        return 'Y';
      case 'leftBumper':
        return 'LB';
      case 'rightBumper':
        return 'RB';
      case 'leftTrigger':
        return 'LT';
      case 'rightTrigger':
        return 'RT';
      case 'leftStick':
        return 'LS';
      case 'rightStick':
        return 'RS';
      case 'start':
        return 'Start';
      case 'back':
        return 'Back';
      case 'dpadUp':
        return '\u2191';
      case 'dpadDown':
        return '\u2193';
      case 'dpadLeft':
        return '\u2190';
      case 'dpadRight':
        return '\u2192';
      default:
        return code.toUpperCase();
    }
  }

  static Color _keyColor(String code) {
    switch (code) {
      case 'a':
        return const Color(0xFF2E7D32);
      case 'b':
        return const Color(0xFFC62828);
      case 'x':
        return const Color(0xFF1565C0);
      case 'y':
        return const Color(0xFFF9A825);
      case 'leftBumper':
        return const Color(0xFF37474F);
      case 'rightBumper':
        return const Color(0xFF37474F);
      case 'leftTrigger':
        return const Color(0xFFE65100);
      case 'rightTrigger':
        return const Color(0xFFE65100);
      case 'leftStick':
        return const Color(0xFF6A1B9A);
      case 'rightStick':
        return const Color(0xFF6A1B9A);
      case 'start':
        return const Color(0xFF006064);
      case 'back':
        return const Color(0xFF006064);
      default:
        return const Color(0xFF424242);
    }
  }
}

class _Cover extends StatelessWidget {
  final FloatingPlayerSnapshot snapshot;
  const _Cover({required this.snapshot});
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    if (snapshot.uuid.isEmpty) {
      return SizedBox.square(
        dimension: 240,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(16),
          child: _PlaceholderCover(colorScheme: cs),
        ),
      );
    }
    return SizedBox.square(
      dimension: 240,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: Image.network(
          '${snapshot.baseUrl}/api/track/cover?uuid=${snapshot.uuid}',
          key: ValueKey('cover_${snapshot.uuid}'),
          fit: BoxFit.cover,
          errorBuilder: (_, _, _) => _PlaceholderCover(colorScheme: cs),
        ),
      ),
    );
  }
}

class _PlaceholderCover extends StatelessWidget {
  final ColorScheme colorScheme;
  const _PlaceholderCover({required this.colorScheme});
  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [colorScheme.primaryContainer, colorScheme.tertiaryContainer],
        ),
      ),
      child: Icon(
        Icons.music_note_rounded,
        size: 56,
        color: colorScheme.onPrimaryContainer.withAlpha(210),
      ),
    );
  }
}

class _RoundIconButton extends StatefulWidget {
  final IconData icon;
  final bool enabled;
  final bool large;
  final VoidCallback onPressed;
  final bool highlighted;
  const _RoundIconButton({
    super.key,
    required this.icon,
    required this.enabled,
    required this.onPressed,
    this.large = false,
    this.highlighted = false,
  });
  @override
  State<_RoundIconButton> createState() => _RoundIconButtonState();
}

class _RoundIconButtonState extends State<_RoundIconButton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _clickController;
  late final Animation<double> _scaleAnimation;
  @override
  void initState() {
    super.initState();
    _clickController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 100),
    );
    _scaleAnimation = Tween<double>(
      begin: 1.0,
      end: 0.85,
    ).animate(CurvedAnimation(parent: _clickController, curve: Curves.easeOut));
  }

  @override
  void dispose() {
    _clickController.dispose();
    super.dispose();
  }

  Future<void> triggerClick() async {
    if (!widget.enabled) return;
    await _clickController.forward();
    await _clickController.reverse();
    widget.onPressed();
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final baseSize = widget.large ? 54.0 : 42.0;
    return AnimatedScale(
      scale: widget.highlighted ? 1.22 : 1.0,
      duration: const Duration(milliseconds: 200),
      curve: Curves.easeOutBack,
      child: ScaleTransition(
        scale: _scaleAnimation,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 250),
          width: baseSize,
          height: baseSize,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            boxShadow: widget.highlighted
                ? [
                    BoxShadow(
                      color: cs.primary.withOpacity(0.4),
                      blurRadius: 12,
                      spreadRadius: 2,
                    ),
                  ]
                : [],
          ),
          child: IconButton.filledTonal(
            onPressed: widget.enabled ? widget.onPressed : null,
            style: IconButton.styleFrom(
              backgroundColor: widget.highlighted
                  ? cs.primary
                  : (widget.large
                        ? cs.primaryContainer
                        : cs.secondaryContainer),
              foregroundColor: widget.highlighted
                  ? cs.onPrimary
                  : (widget.large
                        ? cs.onPrimaryContainer
                        : cs.onSecondaryContainer),
              disabledBackgroundColor: cs.surfaceContainerHighest,
            ),
            icon: Icon(widget.icon, size: widget.large ? 32 : 26),
          ),
        ),
      ),
    );
  }
}

FloatingPlayerSnapshot floatingPlayerSnapshotFromArguments(String arguments) {
  if (arguments.isEmpty) return const FloatingPlayerSnapshot();
  final decoded = jsonDecode(arguments) as Map<String, dynamic>;
  final snapshot = decoded["snapshot"];
  if (snapshot is Map)
    return FloatingPlayerSnapshot.fromJson(Map<String, dynamic>.from(snapshot));
  return const FloatingPlayerSnapshot();
}
