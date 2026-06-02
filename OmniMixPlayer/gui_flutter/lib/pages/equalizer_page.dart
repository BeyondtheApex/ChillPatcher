import 'dart:async';
import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';

class EqualizerPoint {
  String id;
  double frequency;
  double gainDb;
  double q;
  String type; // 'Peaking', 'LowShelf', 'HighShelf', 'LowPass', 'HighPass'

  EqualizerPoint({
    required this.id,
    required this.frequency,
    required this.gainDb,
    required this.q,
    required this.type,
  });

  factory EqualizerPoint.fromJson(Map<String, dynamic> json) {
    return EqualizerPoint(
      id: json['id'] ?? json['Id'] ?? '',
      frequency: (json['frequency'] ?? json['Frequency'] ?? 1000.0).toDouble(),
      gainDb: (json['gainDb'] ?? json['GainDb'] ?? 0.0).toDouble(),
      q: (json['q'] ?? json['Q'] ?? 1.0).toDouble(),
      type: json['type'] ?? json['Type'] ?? 'Peaking',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'frequency': frequency,
    'gainDb': gainDb,
    'q': q,
    'type': type,
  };
}

class EqualizerState {
  bool enabled;
  double globalGainDb;
  bool softClipEnabled;
  List<EqualizerPoint> points;

  EqualizerState({
    required this.enabled,
    required this.globalGainDb,
    required this.softClipEnabled,
    required this.points,
  });

  factory EqualizerState.fromJson(Map<String, dynamic> json) {
    final pts = (json['points'] ?? json['Points']) as List? ?? [];
    return EqualizerState(
      enabled: json['enabled'] ?? json['Enabled'] ?? false,
      globalGainDb: (json['globalGainDb'] ?? json['GlobalGainDb'] ?? 0.0)
          .toDouble(),
      softClipEnabled:
          json['softClipEnabled'] ?? json['SoftClipEnabled'] ?? true,
      points: pts
          .map((p) => EqualizerPoint.fromJson(p as Map<String, dynamic>))
          .toList(),
    );
  }

  Map<String, dynamic> toJson() => {
    'enabled': enabled,
    'globalGainDb': globalGainDb,
    'softClipEnabled': softClipEnabled,
    'points': points.map((p) => p.toJson()).toList(),
  };
}

class BiquadCoefficients {
  double b0 = 0.0, b1 = 0.0, b2 = 0.0, a1 = 0.0, a2 = 0.0;

  BiquadCoefficients.calculate(
    double frequency,
    double gainDb,
    double q,
    String type,
    double sampleRate,
  ) {
    double w0 = 2.0 * math.pi * frequency / sampleRate;
    double sinW0 = math.sin(w0);
    double cosW0 = math.cos(w0);
    double alpha = sinW0 / (2.0 * q);

    double a0 = 1.0;

    switch (type) {
      case 'Peaking':
        double A = math.pow(10.0, gainDb / 40.0).toDouble();
        a0 = 1.0 + alpha / A;
        b0 = (1.0 + alpha * A) / a0;
        b1 = (-2.0 * cosW0) / a0;
        b2 = (1.0 - alpha * A) / a0;
        a1 = (-2.0 * cosW0) / a0;
        a2 = (1.0 - alpha / A) / a0;
        break;
      case 'LowShelf':
        double A = math.pow(10.0, gainDb / 40.0).toDouble();
        double sqrtA = math.sqrt(A);
        double twoSqrtAAlpha = 2.0 * sqrtA * alpha;
        a0 = (A + 1.0) + (A - 1.0) * cosW0 + twoSqrtAAlpha;
        b0 = (A * ((A + 1.0) - (A - 1.0) * cosW0 + twoSqrtAAlpha)) / a0;
        b1 = (2.0 * A * ((A - 1.0) - (A + 1.0) * cosW0)) / a0;
        b2 = (A * ((A + 1.0) - (A - 1.0) * cosW0 - twoSqrtAAlpha)) / a0;
        a1 = (-2.0 * ((A - 1.0) + (A + 1.0) * cosW0)) / a0;
        a2 = ((A + 1.0) + (A - 1.0) * cosW0 - twoSqrtAAlpha) / a0;
        break;
      case 'HighShelf':
        double A = math.pow(10.0, gainDb / 40.0).toDouble();
        double sqrtA = math.sqrt(A);
        double twoSqrtAAlpha = 2.0 * sqrtA * alpha;
        a0 = (A + 1.0) - (A - 1.0) * cosW0 + twoSqrtAAlpha;
        b0 = (A * ((A + 1.0) + (A - 1.0) * cosW0 + twoSqrtAAlpha)) / a0;
        b1 = (-2.0 * A * ((A - 1.0) + (A + 1.0) * cosW0)) / a0;
        b2 = (A * ((A + 1.0) + (A - 1.0) * cosW0 - twoSqrtAAlpha)) / a0;
        a1 = (2.0 * ((A - 1.0) - (A + 1.0) * cosW0)) / a0;
        a2 = ((A + 1.0) - (A - 1.0) * cosW0 - twoSqrtAAlpha) / a0;
        break;
      case 'LowPass':
        a0 = 1.0 + alpha;
        b0 = ((1.0 - cosW0) / 2.0) / a0;
        b1 = (1.0 - cosW0) / a0;
        b2 = ((1.0 - cosW0) / 2.0) / a0;
        a1 = (-2.0 * cosW0) / a0;
        a2 = (1.0 - alpha) / a0;
        break;
      case 'HighPass':
        a0 = 1.0 + alpha;
        b0 = ((1.0 + cosW0) / 2.0) / a0;
        b1 = -(1.0 + cosW0) / a0;
        b2 = ((1.0 + cosW0) / 2.0) / a0;
        a1 = (-2.0 * cosW0) / a0;
        a2 = (1.0 - alpha) / a0;
        break;
    }
  }

  double getMagnitudeDb(double frequency, double sampleRate) {
    double w = 2.0 * math.pi * frequency / sampleRate;
    double cosW = math.cos(w);
    double sinW = math.sin(w);
    double cos2W = math.cos(2.0 * w);
    double sin2W = math.sin(2.0 * w);

    double numReal = b0 + b1 * cosW + b2 * cos2W;
    double numImag = -(b1 * sinW + b2 * sin2W);

    double denReal = 1.0 + a1 * cosW + a2 * cos2W;
    double denImag = -(a1 * sinW + a2 * sin2W);

    double numSq = numReal * numReal + numImag * numImag;
    double denSq = denReal * denReal + denImag * denImag;

    if (denSq < 1e-12) return 0.0;
    double ratio = numSq / denSq;
    if (ratio < 1e-12) return -120.0;
    return 10.0 * math.log(ratio) / math.ln10;
  }
}

/// A specialized Notifier to isolate drag updates and repaint events from the main layout tree.
class EqualizerNotifier extends ChangeNotifier {
  final EqualizerState state;
  String? _selectedPointId;

  EqualizerNotifier(this.state);

  String? get selectedPointId => _selectedPointId;
  set selectedPointId(String? id) {
    _selectedPointId = id;
    notifyListeners();
  }

  EqualizerPoint? get selectedPoint {
    if (_selectedPointId == null) return null;
    for (final p in state.points) {
      if (p.id == _selectedPointId) return p;
    }
    return null;
  }

  void notifyDragUpdate() {
    notifyListeners();
  }
}

class EqualizerPage extends StatefulWidget {
  final AppState state;

  const EqualizerPage({super.key, required this.state});

  @override
  State<EqualizerPage> createState() => _EqualizerPageState();
}

class _EqualizerPageState extends State<EqualizerPage> {
  EqualizerState? _eqState;
  EqualizerNotifier? _notifier;
  String? _lastActiveInstanceId;
  Timer? _saveDebounce;
  Map<String, EqualizerState> _presets = {};
  bool _loadingPresets = false;

  @override
  void initState() {
    super.initState();
    _lastActiveInstanceId = widget.state.activeInstanceId;
    _loadState();
    _loadPresets();
    widget.state.addListener(_onAppStateChanged);
  }

  @override
  void dispose() {
    widget.state.removeListener(_onAppStateChanged);
    _notifier?.removeListener(_onNotifierChanged);
    if (_saveDebounce != null && _saveDebounce!.isActive) {
      _saveDebounce!.cancel();
      _saveState();
    }
    super.dispose();
  }

  void _onAppStateChanged() {
    if (_lastActiveInstanceId != widget.state.activeInstanceId) {
      _lastActiveInstanceId = widget.state.activeInstanceId;
      _loadState();
    } else if (widget.state.equalizerGeneration != _lastEqGeneration) {
      _lastEqGeneration = widget.state.equalizerGeneration;
      _loadState();
    }
  }

  int _lastEqGeneration = -1;

  void _onNotifierChanged() {
    _saveDebounce?.cancel();
    _saveDebounce = Timer(const Duration(milliseconds: 60), () {
      _saveState();
    });
  }

  Future<void> _loadState() async {
    final instId = widget.state.activeInstanceId;
    if (instId == null) return;
    try {
      final jsonMap = await widget.state.api.getInstanceEqualizer(instId);
      if (!mounted) return;
      _notifier?.removeListener(_onNotifierChanged);
      final eq = EqualizerState.fromJson(jsonMap);
      setState(() {
        _eqState = eq;
        _notifier = EqualizerNotifier(eq);
        _notifier!.addListener(_onNotifierChanged);
      });
    } catch (e) {
      debugPrint("Failed to load EQ state: $e");
    }
  }

  Future<void> _loadPresets() async {
    final instId = widget.state.activeInstanceId;
    if (instId == null) return;
    if (!mounted) return;
    setState(() => _loadingPresets = true);
    try {
      final data = await widget.state.api.getInstanceEqualizerPresets(instId);
      if (!mounted) return;
      final map = <String, EqualizerState>{};
      data.forEach((k, v) {
        map[k] = EqualizerState.fromJson(v as Map<String, dynamic>);
      });
      setState(() {
        _presets = map;
      });
    } catch (e) {
      debugPrint("Failed to load EQ presets: $e");
    } finally {
      if (!mounted) return;
      setState(() => _loadingPresets = false);
    }
  }

  Future<void> _saveState() async {
    final instId = widget.state.activeInstanceId;
    if (instId == null || _eqState == null) return;
    try {
      await widget.state.api.updateInstanceEqualizer(
        instId,
        _eqState!.toJson(),
      );
    } catch (e) {
      debugPrint("Failed to save EQ state: $e");
    }
  }

  void _addPoint(double frequency, double gainDb) {
    if (_eqState == null || _notifier == null) return;
    final newId = "point_${DateTime.now().millisecondsSinceEpoch}";
    final pt = EqualizerPoint(
      id: newId,
      frequency: frequency,
      gainDb: gainDb,
      q: 1.0,
      type: 'Peaking',
    );
    _eqState!.points.add(pt);
    _notifier!.selectedPointId =
        newId; // triggers listener notification to save & redraw
  }

  void _deletePoint(String id) {
    if (_eqState == null || _notifier == null) return;
    _eqState!.points.removeWhere((p) => p.id == id);
    if (_notifier!.selectedPointId == id) {
      _notifier!.selectedPointId = null;
    } else {
      _notifier!.notifyDragUpdate();
    }
  }

  void _applyPreset(String name) {
    final preset = _presets[name];
    if (preset == null || _eqState == null || _notifier == null) return;
    setState(() {
      _eqState!.enabled = preset.enabled;
      _eqState!.globalGainDb = preset.globalGainDb;
      _eqState!.softClipEnabled = preset.softClipEnabled;
      _eqState!.points = preset.points
          .map(
            (p) => EqualizerPoint(
              id: p.id,
              frequency: p.frequency,
              gainDb: p.gainDb,
              q: p.q,
              type: p.type,
            ),
          )
          .toList();
      _notifier = EqualizerNotifier(_eqState!);
      _notifier!.addListener(_onNotifierChanged);
    });
    _saveState();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;
    final instId = widget.state.activeInstanceId;

    if (instId == null) {
      return Center(child: Text(l10n.noSelectedInstance));
    }

    if (_eqState == null || _notifier == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          // Header / Control bar (Static, does not rebuild during dragging)
          Row(
            children: [
              Text(
                l10n.equalizerControl,
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: cs.onSurface,
                ),
              ),
              const Spacer(),
              Switch(
                value: _eqState!.enabled,
                onChanged: (v) {
                  setState(() {
                    _eqState!.enabled = v;
                  });
                  _notifier!
                      .notifyDragUpdate(); // notifies server & triggers save
                },
              ),
              const SizedBox(width: 8),
              Text(
                _eqState!.enabled ? l10n.enabled : l10n.disabled,
                style: TextStyle(color: cs.onSurfaceVariant, fontSize: 13),
              ),
              const SizedBox(width: 24),
              // Preset Dropdown
              if (_presets.isNotEmpty)
                DropdownButton<String>(
                  hint: Text(
                    l10n.selectPreset,
                    style: const TextStyle(fontSize: 13),
                  ),
                  style: TextStyle(color: cs.onSurface, fontSize: 13),
                  items: _presets.keys.map((name) {
                    return DropdownMenuItem<String>(
                      value: name,
                      child: Text(name),
                    );
                  }).toList(),
                  onChanged: (val) {
                    if (val != null) _applyPreset(val);
                  },
                ),
              const SizedBox(width: 12),
              OutlinedButton.icon(
                icon: const Icon(Icons.refresh, size: 16),
                label: Text(l10n.reset, style: const TextStyle(fontSize: 12)),
                onPressed: () {
                  setState(() {
                    _eqState!.points.clear();
                    _eqState!.globalGainDb = 0.0;
                    _eqState!.softClipEnabled = true;
                    _notifier = EqualizerNotifier(_eqState!);
                    _notifier!.addListener(_onNotifierChanged);
                  });
                  _saveState();
                },
              ),
            ],
          ),
          const SizedBox(height: 12),
          // Log graph canvas (Isolated Build using AnimatedBuilder)
          Expanded(
            child: Card(
              elevation: 0,
              clipBehavior: Clip.antiAlias,
              color: cs.surfaceContainerLowest,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: cs.outlineVariant),
              ),
              child: LayoutBuilder(
                builder: (context, constraints) {
                  return AnimatedBuilder(
                    animation: _notifier!,
                    builder: (context, child) {
                      return GestureDetector(
                        onDoubleTapDown: (details) {
                          final pos = details.localPosition;
                          final f = _xToFrequency(pos.dx, constraints.maxWidth);
                          final g = _yToGain(pos.dy, constraints.maxHeight);
                          _addPoint(f, g);
                        },
                        onTapDown: (details) {
                          final pos = details.localPosition;
                          // Find hit point
                          String? hitId;
                          double bestDist = 18.0;
                          for (final p in _eqState!.points) {
                            final px = _frequencyToX(
                              p.frequency,
                              constraints.maxWidth,
                            );
                            final py = _gainToY(
                              p.gainDb,
                              constraints.maxHeight,
                            );
                            final dist = math.sqrt(
                              (px - pos.dx) * (px - pos.dx) +
                                  (py - pos.dy) * (py - pos.dy),
                            );
                            if (dist < bestDist) {
                              bestDist = dist;
                              hitId = p.id;
                            }
                          }
                          _notifier!.selectedPointId = hitId;
                        },
                        onPanUpdate: (details) {
                          if (_notifier!.selectedPointId == null) return;
                          final pos = details.localPosition;
                          final pt = _notifier!.selectedPoint;
                          if (pt != null) {
                            final f = _xToFrequency(
                              pos.dx,
                              constraints.maxWidth,
                            );
                            double g = pt.gainDb;
                            if (pt.type != 'LowPass' && pt.type != 'HighPass') {
                              g = _yToGain(pos.dy, constraints.maxHeight);
                            }
                            pt.frequency = Math.clamp(f, 20.0, 20000.0);
                            pt.gainDb = Math.clamp(g, -24.0, 24.0);

                            // Re-paint canvas immediately without rebuilding the whole layout
                            _notifier!.notifyDragUpdate();
                          }
                        },
                        onPanEnd: (_) {
                          // Flush save immediately on release, bypassing debounce.
                          _saveDebounce?.cancel();
                          _saveState();
                        },
                        onPanCancel: () {
                          // Flush save on cancelled gesture too.
                          _saveDebounce?.cancel();
                          _saveState();
                        },
                        child: CustomPaint(
                          size: Size(
                            constraints.maxWidth,
                            constraints.maxHeight,
                          ),
                          painter: _EqualizerPainter(
                            state: _eqState!,
                            selectedId: _notifier!.selectedPointId,
                            colorScheme: cs,
                          ),
                        ),
                      );
                    },
                  );
                },
              ),
            ),
          ),
          const SizedBox(height: 12),
          // Configuration Panel (Isolated build via AnimatedBuilder)
          AnimatedBuilder(
            animation: _notifier!,
            builder: (context, child) {
              final selectedPt = _notifier!.selectedPoint;
              return Card(
                elevation: 0,
                color: cs.surfaceContainerLow,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                  child: Column(
                    children: [
                      // Global Level Preamp
                      Row(
                        children: [
                          const Icon(Icons.volume_up, size: 20),
                          const SizedBox(width: 8),
                          Text(
                            l10n.globalGainPreamp,
                            style: const TextStyle(fontSize: 13),
                          ),
                          Expanded(
                            child: Slider(
                              value: _eqState!.globalGainDb,
                              min: -24.0,
                              max: 24.0,
                              label:
                                  "${_eqState!.globalGainDb.toStringAsFixed(1)} dB",
                              onChanged: (val) {
                                _eqState!.globalGainDb = val;
                                _notifier!.notifyDragUpdate();
                              },
                            ),
                          ),
                          SizedBox(
                            width: 60,
                            child: Text(
                              "${_eqState!.globalGainDb.toStringAsFixed(1)} dB",
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          const SizedBox(width: 16),
                          Switch(
                            value: _eqState!.softClipEnabled,
                            onChanged: (val) {
                              setState(() {
                                _eqState!.softClipEnabled = val;
                              });
                              _notifier!.notifyDragUpdate();
                            },
                          ),
                          const SizedBox(width: 6),
                          Text(
                            l10n.softClip,
                            style: const TextStyle(fontSize: 12),
                          ),
                        ],
                      ),
                      const Divider(height: 16),
                      Row(
                        children: [
                          Text(
                            selectedPt != null
                                ? l10n.controlPointSettingsActive(
                                    selectedPt.frequency.round(),
                                  )
                                : l10n.controlPointSettingsNone,
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.bold,
                              color: selectedPt != null
                                  ? cs.onSurface
                                  : cs.onSurface.withOpacity(0.4),
                            ),
                          ),
                          const Spacer(),
                          // Delete Button
                          TextButton.icon(
                            icon: Icon(
                              Icons.delete,
                              size: 16,
                              color: selectedPt != null
                                  ? Colors.red
                                  : cs.onSurface.withOpacity(0.3),
                            ),
                            label: Text(
                              l10n.delete,
                              style: TextStyle(
                                color: selectedPt != null
                                    ? Colors.red
                                    : cs.onSurface.withOpacity(0.3),
                                fontSize: 13,
                              ),
                            ),
                            onPressed: selectedPt != null
                                ? () => _deletePoint(selectedPt.id)
                                : null,
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          // Filter Type
                          Text(
                            l10n.typeLabel,
                            style: TextStyle(
                              fontSize: 12,
                              color: selectedPt != null
                                  ? cs.onSurface
                                  : cs.onSurface.withOpacity(0.4),
                            ),
                          ),
                          const SizedBox(width: 6),
                          DropdownButton<String>(
                            value: selectedPt?.type ?? 'Peaking',
                            style: TextStyle(
                              color: selectedPt != null
                                  ? cs.onSurface
                                  : cs.onSurface.withOpacity(0.4),
                              fontSize: 12,
                            ),
                            underline: const SizedBox(),
                            items: [
                              DropdownMenuItem(
                                value: 'Peaking',
                                child: Text(l10n.filterTypeBell),
                              ),
                              DropdownMenuItem(
                                value: 'LowShelf',
                                child: Text(l10n.filterTypeLowShelf),
                              ),
                              DropdownMenuItem(
                                value: 'HighShelf',
                                child: Text(l10n.filterTypeHighShelf),
                              ),
                              DropdownMenuItem(
                                value: 'LowPass',
                                child: Text(l10n.filterTypeLowPass),
                              ),
                              DropdownMenuItem(
                                value: 'HighPass',
                                child: Text(l10n.filterTypeHighPass),
                              ),
                            ],
                            onChanged: selectedPt != null
                                ? (val) {
                                    if (val != null) {
                                      selectedPt.type = val;
                                      if (val == 'LowPass' ||
                                          val == 'HighPass') {
                                        selectedPt.gainDb = 0.0;
                                      }
                                      _notifier!.notifyDragUpdate();
                                    }
                                  }
                                : null,
                          ),
                          const SizedBox(width: 24),
                          // Q factor slider
                          Text(
                            l10n.qFactorLabel,
                            style: TextStyle(
                              fontSize: 12,
                              color: selectedPt != null
                                  ? cs.onSurface
                                  : cs.onSurface.withOpacity(0.4),
                            ),
                          ),
                          Expanded(
                            child: Slider(
                              value: selectedPt?.q ?? 1.0,
                              min: 0.1,
                              max: 20.0,
                              label: selectedPt != null
                                  ? "Q: ${selectedPt.q.toStringAsFixed(2)}"
                                  : null,
                              onChanged: selectedPt != null
                                  ? (val) {
                                      selectedPt.q = val;
                                      _notifier!.notifyDragUpdate();
                                    }
                                  : null,
                            ),
                          ),
                          SizedBox(
                            width: 50,
                            child: Text(
                              "Q: ${(selectedPt?.q ?? 1.0).toStringAsFixed(2)}",
                              style: TextStyle(
                                fontSize: 12,
                                color: selectedPt != null
                                    ? cs.onSurface
                                    : cs.onSurface.withOpacity(0.4),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
          const SizedBox(height: 8),
          Center(
            child: Text(
              l10n.equalizerTip,
              style: const TextStyle(fontSize: 11, color: Colors.grey),
            ),
          ),
        ],
      ),
    );
  }

  // Coordinate mappings
  double _frequencyToX(double freq, double width) {
    double minF = 20.0;
    double maxF = 20000.0;
    double logMin = math.log(minF);
    double logMax = math.log(maxF);
    double t = (math.log(freq) - logMin) / (logMax - logMin);
    return t * width;
  }

  double _xToFrequency(double x, double width) {
    double minF = 20.0;
    double maxF = 20000.0;
    double logMin = math.log(minF);
    double logMax = math.log(maxF);
    double t = Math.clamp(x / width, 0.0, 1.0);
    return math.exp(logMin + t * (logMax - logMin));
  }

  double _gainToY(double gainDb, double height) {
    double minG = -24.0;
    double maxG = 24.0;
    double t = (gainDb - minG) / (maxG - minG);
    return height * (1.0 - t);
  }

  double _yToGain(double y, double height) {
    double minG = -24.0;
    double maxG = 24.0;
    double t = Math.clamp(1.0 - (y / height), 0.0, 1.0);
    return minG + t * (maxG - minG);
  }
}

class _EqualizerPainter extends CustomPainter {
  final EqualizerState state;
  final String? selectedId;
  final ColorScheme colorScheme;

  _EqualizerPainter({
    required this.state,
    required this.selectedId,
    required this.colorScheme,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final width = size.width;
    final height = size.height;

    // Draw Grid background
    final gridPaint = Paint()
      ..color = colorScheme.outlineVariant.withAlpha(50)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.0;

    final textPainter = TextPainter(textDirection: TextDirection.ltr);

    // 1. Draw logarithmic frequency grids
    final gridFreqs = [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000];
    for (final f in gridFreqs) {
      final x = _frequencyToX(f.toDouble(), width);
      canvas.drawLine(Offset(x, 0), Offset(x, height), gridPaint);

      // Label text
      final label = f >= 1000 ? "${(f / 1000).toStringAsFixed(0)}k" : "$f";
      textPainter.text = TextSpan(
        text: label,
        style: TextStyle(
          color: colorScheme.outline.withAlpha(150),
          fontSize: 9,
        ),
      );
      textPainter.layout();
      textPainter.paint(canvas, Offset(x + 2, height - 14));
    }

    // 2. Draw linear gain grids
    final gridGains = [-24, -18, -12, -6, 0, 6, 12, 18, 24];
    for (final g in gridGains) {
      final y = _gainToY(g.toDouble(), height);
      canvas.drawLine(Offset(0, y), Offset(width, y), gridPaint);

      // Label text
      final totalG = g + state.globalGainDb;
      final label = totalG == totalG.roundToDouble()
          ? totalG.round().toString()
          : totalG.toStringAsFixed(1);
      textPainter.text = TextSpan(
        text: "${totalG > 0 ? "+" : ""}$label dB",
        style: TextStyle(
          color: colorScheme.outline.withAlpha(150),
          fontSize: 9,
        ),
      );
      textPainter.layout();
      textPainter.paint(canvas, Offset(4, y - 11));
    }

    // 3. Calculate and draw combined response curve
    final curvePaint = Paint()
      ..color = colorScheme.primary
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.5;

    // Build filters coefficients
    final filters = <BiquadCoefficients>[];
    for (final p in state.points) {
      filters.add(
        BiquadCoefficients.calculate(
          p.frequency,
          p.gainDb,
          p.q,
          p.type,
          48000.0,
        ),
      );
    }

    // Sample points along the width
    final curvePoints = <Offset>[];
    const sampleCount = 200;
    for (int i = 0; i < sampleCount; i++) {
      final t = i / (sampleCount - 1);
      final freq = math.exp(
        math.log(20.0) + t * (math.log(20000.0) - math.log(20.0)),
      );

      double totalGainDb = 0.0;
      for (final filter in filters) {
        totalGainDb += filter.getMagnitudeDb(freq, 48000.0);
      }

      final x = t * width;
      final y = _gainToY(totalGainDb, height);
      curvePoints.add(Offset(x, y));
    }

    // Draw area under the curve
    if (curvePoints.isNotEmpty) {
      final fillPath = Path();
      fillPath.moveTo(0, _gainToY(0.0, height));
      for (final pt in curvePoints) {
        fillPath.lineTo(pt.dx, pt.dy);
      }
      fillPath.lineTo(width, _gainToY(0.0, height));
      fillPath.close();

      final areaPaint = Paint()
        ..shader = LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            colorScheme.primary.withAlpha(60),
            colorScheme.primary.withAlpha(5),
          ],
        ).createShader(Rect.fromLTRB(0, 0, width, height));
      canvas.drawPath(fillPath, areaPaint);
    }

    // Draw main curve path
    final curvePath = Path();
    if (curvePoints.isNotEmpty) {
      curvePath.moveTo(curvePoints.first.dx, curvePoints.first.dy);
      for (int i = 1; i < curvePoints.length; i++) {
        curvePath.lineTo(curvePoints[i].dx, curvePoints[i].dy);
      }
      canvas.drawPath(curvePath, curvePaint);
    }

    // 4. Draw control points on top
    for (final p in state.points) {
      final x = _frequencyToX(p.frequency, width);
      final y = _gainToY(p.gainDb, height);

      final isSelected = p.id == selectedId;

      // Halo for selection
      if (isSelected) {
        final selectPaint = Paint()
          ..color = colorScheme.primary.withAlpha(100)
          ..style = PaintingStyle.fill;
        canvas.drawCircle(Offset(x, y), 12.0, selectPaint);
      }

      final ptPaint = Paint()
        ..color = isSelected ? Colors.white : colorScheme.secondary
        ..style = PaintingStyle.fill;
      canvas.drawCircle(Offset(x, y), 6.0, ptPaint);

      final borderPaint = Paint()
        ..color = colorScheme.primary
        ..strokeWidth = 2.0
        ..style = PaintingStyle.stroke;
      canvas.drawCircle(Offset(x, y), 6.0, borderPaint);

      // Label for frequency near point
      textPainter.text = TextSpan(
        text: "${p.frequency.round()}Hz",
        style: TextStyle(
          color: colorScheme.onSurface.withAlpha(200),
          fontSize: 9,
          fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        ),
      );
      textPainter.layout();
      textPainter.paint(canvas, Offset(x - textPainter.width / 2, y - 18));
    }
  }

  // Linear / Log mappings matching the page logic
  double _frequencyToX(double freq, double width) {
    double minF = 20.0;
    double maxF = 20000.0;
    double logMin = math.log(minF);
    double logMax = math.log(maxF);
    double t = (math.log(freq) - logMin) / (logMax - logMin);
    return t * width;
  }

  double _gainToY(double gainDb, double height) {
    double minG = -24.0;
    double maxG = 24.0;
    double t = (gainDb - minG) / (maxG - minG);
    return height * (1.0 - t);
  }

  @override
  bool shouldRepaint(covariant _EqualizerPainter oldDelegate) {
    // state is mutable (points modified in place during drag), so we always
    // repaint when the painter is rebuilt by AnimatedBuilder.
    return true;
  }
}

class Math {
  static double clamp(double value, double min, double max) {
    if (value < min) return min;
    if (value > max) return max;
    return value;
  }
}
