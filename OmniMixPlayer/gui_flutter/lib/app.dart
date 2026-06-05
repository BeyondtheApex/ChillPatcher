import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dynamic_color/dynamic_color.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import 'providers/app_state.dart';
import 'providers/core/app_state_bridge.dart';
import 'pages/home_page.dart';
import 'pages/playlist_page.dart';
import 'pages/settings_page.dart';
import 'pages/modules_page.dart';
import 'pages/about_page.dart';
import 'pages/game_integration_page.dart';
import 'pages/equalizer_page.dart';
import 'widgets/launchpad_grid.dart';
import 'widgets/proxy_node.dart';

class OmniMixApp extends ConsumerWidget {
  const OmniMixApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final st = ref.watch(appStateProvider);

    return DynamicColorBuilder(
      builder: (lightDynamic, darkDynamic) {
        final seed = Color(st.seedColor);
        final lightScheme = (st.useSystemColor && lightDynamic != null)
            ? lightDynamic
            : ColorScheme.fromSeed(seedColor: seed);
        final darkScheme = (st.useSystemColor && darkDynamic != null)
            ? darkDynamic
            : ColorScheme.fromSeed(
                seedColor: seed,
                brightness: Brightness.dark,
              );

        return MaterialApp(
          title: 'OmniMixPlayer',
          debugShowCheckedModeBanner: false,
          theme:
              ThemeData(
                colorScheme: lightScheme,
                useMaterial3: true,
                brightness: Brightness.light,
              ).copyWith(
                textTheme: ThemeData(brightness: Brightness.light).textTheme
                    .apply(
                      fontFamily: 'PingFang SC',
                      fontFamilyFallback: const [
                        'Microsoft YaHei',
                        'Hiragino Sans GB',
                        'Heiti SC',
                        'Noto Sans CJK SC',
                        'sans-serif',
                      ],
                    ),
              ),
          darkTheme:
              ThemeData(
                colorScheme: darkScheme,
                useMaterial3: true,
                brightness: Brightness.dark,
              ).copyWith(
                textTheme: ThemeData(brightness: Brightness.light).textTheme
                    .apply(
                      fontFamily: 'PingFang SC',
                      fontFamilyFallback: const [
                        'Microsoft YaHei',
                        'Hiragino Sans GB',
                        'Heiti SC',
                        'Noto Sans CJK SC',
                        'sans-serif',
                      ],
                    ),
              ),
          themeMode: st.themeMode == AppThemeMode.light
              ? ThemeMode.light
              : st.themeMode == AppThemeMode.dark
              ? ThemeMode.dark
              : ThemeMode.system,
          locale: _resolveLocale(st.language),
          supportedLocales: const [Locale('en'), Locale('zh')],
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          localeResolutionCallback: (locale, supported) {
            if (locale == null) return const Locale('en');
            for (final sup in supported) {
              if (sup.languageCode == locale.languageCode) return sup;
            }
            return const Locale('en');
          },
          home: const _MainShell(),
        );
      },
    );
  }

  Locale? _resolveLocale(String lang) {
    if (lang == 'system') return null;
    if (lang == 'zh') return const Locale('zh');
    return const Locale('en');
  }
}

// ═══════════════════════════════════════════════════════════
//  Internal widgets — all use ref.watch(appStateProvider)
// ═══════════════════════════════════════════════════════════

class _MainShell extends ConsumerWidget {
  const _MainShell();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= 800;
        return _MainContent(isWide: isWide);
      },
    );
  }
}

class _MainContent extends ConsumerStatefulWidget {
  final bool isWide;
  const _MainContent({required this.isWide});

  @override
  ConsumerState<_MainContent> createState() => _MainContentState();
}

class _MainContentState extends ConsumerState<_MainContent> {
  @override
  Widget build(BuildContext context) {
    // Listen for errors via Riverpod (must be in build, not initState)
    ref.listen(appStateProvider, (prev, next) {
      final error = next.consumeError();
      if (error != null && mounted) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(error),
                behavior: SnackBarBehavior.floating,
                duration: const Duration(seconds: 4),
              ),
            );
          }
        });
      }
    });

    final l10n = AppLocalizations.of(context);
    final st = ref.watch(appStateProvider);
    final isWide = widget.isWide;
    final cs = Theme.of(context).colorScheme;

    if (isWide) {
      return Scaffold(
        backgroundColor: cs.surfaceContainer,
        appBar: _buildTopBar(context, l10n, st, isWide),
        body: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            NavigationRail(
              selectedIndex: st.currentTab,
              backgroundColor: cs.surfaceContainer,
              indicatorColor: cs.secondaryContainer,
              labelType: NavigationRailLabelType.all,
              onDestinationSelected: (i) => st.selectTab(i),
              destinations: [
                NavigationRailDestination(
                  icon: const Icon(Icons.home),
                  label: Text(l10n.home),
                ),
                NavigationRailDestination(
                  icon: const Icon(Icons.library_music),
                  label: Text(l10n.playlist),
                ),
                NavigationRailDestination(
                  icon: const Icon(Icons.grid_view_rounded),
                  label: Text(l10n.launchpad),
                ),
                NavigationRailDestination(
                  icon: const Icon(Icons.extension),
                  label: Text(l10n.modules),
                ),
                if (!kIsWeb)
                  NavigationRailDestination(
                    icon: const Icon(Icons.sports_esports),
                    label: Text(l10n.gameIntegration),
                  ),
                NavigationRailDestination(
                  icon: const Icon(Icons.graphic_eq),
                  label: Text(l10n.equalizer),
                ),
                NavigationRailDestination(
                  icon: const Icon(Icons.settings),
                  label: Text(l10n.settings),
                ),
              ],
            ),
            const SizedBox(width: 1),
            Expanded(
              child: ClipRRect(
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(16),
                  topRight: Radius.circular(16),
                ),
                child: Container(
                  color: cs.surface,
                  child: st.hasOverlay
                      ? _buildOverlay(context, l10n, st, isWide)
                      : _buildTabContent(st),
                ),
              ),
            ),
          ],
        ),
      );
    }

    return Scaffold(
      backgroundColor: cs.surface,
      appBar: _buildTopBar(context, l10n, st, isWide),
      body: st.hasOverlay
          ? _buildOverlay(context, l10n, st, isWide)
          : _buildTabContent(st),
      bottomNavigationBar: NavigationBar(
        selectedIndex: st.currentTab,
        onDestinationSelected: (i) => st.selectTab(i),
        destinations: [
          const NavigationDestination(icon: Icon(Icons.home), label: ' '),
          const NavigationDestination(
            icon: Icon(Icons.library_music),
            label: ' ',
          ),
          const NavigationDestination(
            icon: Icon(Icons.grid_view_rounded),
            label: ' ',
          ),
          const NavigationDestination(icon: Icon(Icons.extension), label: ' '),
          if (!kIsWeb)
            const NavigationDestination(
              icon: Icon(Icons.sports_esports),
              label: ' ',
            ),
          const NavigationDestination(icon: Icon(Icons.graphic_eq), label: ' '),
          const NavigationDestination(icon: Icon(Icons.settings), label: ' '),
        ],
      ),
    );
  }

  PreferredSizeWidget _buildTopBar(
    BuildContext context,
    AppLocalizations l10n,
    AppState st,
    bool isWide,
  ) {
    final cs = Theme.of(context).colorScheme;
    return AppBar(
      scrolledUnderElevation: 0,
      backgroundColor: cs.surfaceContainer,
      automaticallyImplyLeading: false,
      titleSpacing: 0,
      title: SizedBox(
        width: double.infinity,
        child: Stack(
          alignment: Alignment.center,
          children: [
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 252),
              child: const _GlobalInstanceDropdown(),
            ),
            Positioned(
              right: 16,
              child: isWide
                  ? Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: st.backendOnline
                                ? Colors.green
                                : Colors.grey,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Text(
                          st.backendOnline ? l10n.connected : l10n.disconnected,
                          style: TextStyle(
                            fontSize: 12,
                            color: cs.onSurfaceVariant,
                          ),
                        ),
                      ],
                    )
                  : _RippleStatusDot(online: st.backendOnline),
            ),
          ],
        ),
      ),
      centerTitle: true,
    );
  }

  Widget _buildTabContent(AppState st) {
    final tab = st.currentTab;
    if (kIsWeb) {
      switch (tab) {
        case 0:
          return HomePage(state: st);
        case 1:
          return PlaylistPage(state: st);
        case 2:
          final allLinks = st.modules.expand((m) => m.linkEntries).toList();
          final moduleLinks = {for (final m in st.modules) m.id: m.linkEntries};
          return LaunchpadGrid(
            links: allLinks,
            baseUrl: st.apiBaseUrl,
            moduleLinks: moduleLinks,
            onTap: (entry) {
              final mod = st.modules.firstWhere(
                (m) => m.linkEntries.contains(entry),
              );
              st.openModuleLink(mod.id, entry.id);
            },
          );
        case 3:
          return ModulesPage(state: st);
        case 4:
          return EqualizerPage(state: st);
        case 5:
          return SettingsPage(state: st);
        default:
          return HomePage(state: st);
      }
    }
    switch (tab) {
      case 0:
        return HomePage(state: st);
      case 1:
        return PlaylistPage(state: st);
      case 2:
        final allLinks = st.modules.expand((m) => m.linkEntries).toList();
        final moduleLinks = {for (final m in st.modules) m.id: m.linkEntries};
        return LaunchpadGrid(
          links: allLinks,
          baseUrl: st.apiBaseUrl,
          moduleLinks: moduleLinks,
          onTap: (entry) {
            final mod = st.modules.firstWhere(
              (m) => m.linkEntries.contains(entry),
            );
            st.openModuleLink(mod.id, entry.id);
          },
        );
      case 3:
        return ModulesPage(state: st);
      case 4:
        return GameIntegrationPage(state: st);
      case 5:
        return EqualizerPage(state: st);
      case 6:
        return SettingsPage(state: st);
      default:
        return HomePage(state: st);
    }
  }

  Widget _buildOverlay(
    BuildContext context,
    AppLocalizations l10n,
    AppState st,
    bool isWide,
  ) {
    if (st.overlayMode == 'about') return AboutPage(onBack: st.closeOverlay);
    final overlayBg = st.overlayUiTree != null
        ? parseHexColor(st.overlayUiTree!.color)
        : null;
    return Container(
      width: double.infinity,
      height: double.infinity,
      color: overlayBg,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                TextButton.icon(
                  onPressed: st.closeOverlay,
                  icon: const Icon(Icons.arrow_back, size: 18),
                  label: Text(l10n.back),
                ),
              ],
            ),
          ),
          Expanded(
            child: st.overlayUiTree != null
                ? SingleChildScrollView(
                    child: ProxyNodeWidget(
                      node: st.overlayUiTree!,
                      onDispatch: st.dispatchUiEvent,
                      imageBaseUrl: st.apiBaseUrl,
                    ),
                  )
                : const Center(child: CircularProgressIndicator()),
          ),
        ],
      ),
    );
  }
}

class _GlobalInstanceDropdown extends ConsumerWidget {
  const _GlobalInstanceDropdown();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final st = ref.watch(appStateProvider);
    final allInstances = st.playbackInstances
        .where((i) => i.id.isNotEmpty || i.isOnline)
        .toList();
    final onlineIds = st.backendOnline
        ? st.playbackInstances.where((i) => i.isOnline).map((i) => i.id).toSet()
        : <String>{};
    final cs = Theme.of(context).colorScheme;

    if (allInstances.isEmpty) {
      return Text(
        l10n.noInstances,
        style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant),
      );
    }

    final activeId = st.activeInstanceId;
    final displayId =
        (activeId != null && allInstances.any((i) => i.id == activeId))
        ? activeId
        : allInstances.first.id;

    return Container(
      height: 36,
      padding: const EdgeInsets.symmetric(horizontal: 10),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: cs.outlineVariant),
        color: cs.surface,
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<String>(
          value: displayId,
          isExpanded: true,
          icon: const Icon(Icons.expand_more_rounded),
          items: allInstances.map((inst) {
            final online = onlineIds.contains(inst.id);
            return DropdownMenuItem<String>(
              value: inst.id,
              child: Row(
                children: [
                  Icon(
                    online ? Icons.circle : Icons.circle_outlined,
                    size: 10,
                    color: online ? Colors.green : Colors.grey,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      inst.gameName.isNotEmpty
                          ? inst.gameName
                          : inst.displayName,
                      style: const TextStyle(fontSize: 13),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    inst.kind.name,
                    style: TextStyle(fontSize: 11, color: cs.outline),
                  ),
                ],
              ),
            );
          }).toList(),
          onChanged: (v) {
            if (v != null) st.selectInstance(v);
          },
        ),
      ),
    );
  }
}

/// A pulsing dot that indicates connection status — used in portrait layout
/// to save space compared to the full "Connected / Disconnected" text label.
class _RippleStatusDot extends StatefulWidget {
  final bool online;
  const _RippleStatusDot({required this.online});

  @override
  State<_RippleStatusDot> createState() => _RippleStatusDotState();
}

class _RippleStatusDotState extends State<_RippleStatusDot>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final color = widget.online ? Colors.green : Colors.grey;
    return AnimatedBuilder(
      animation: _ctrl,
      builder: (context, child) {
        final t = _ctrl.value; // 0.0 … 1.0
        return Container(
          width: 10 + t * 6,
          height: 10 + t * 6,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: color.withAlpha((80 + t * 175).round()),
            boxShadow: [
              BoxShadow(
                color: color.withAlpha((30 + t * 50).round()),
                blurRadius: 3 + t * 10,
                spreadRadius: t * 3,
              ),
            ],
          ),
        );
      },
    );
  }
}
