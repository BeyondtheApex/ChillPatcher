import 'package:flutter/material.dart';
import 'package:dynamic_color/dynamic_color.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../pages/home_page.dart';
import '../pages/playlist_page.dart';
import '../pages/settings_page.dart';
import '../pages/modules_page.dart';
import '../pages/about_page.dart';
import '../pages/game_integration_page.dart';
import '../widgets/launchpad_grid.dart';
import '../widgets/proxy_node.dart';

class OmniMixApp extends StatefulWidget {
  final AppState state;

  const OmniMixApp({super.key, required this.state});

  @override
  State<OmniMixApp> createState() => _OmniMixAppState();
}

class _OmniMixAppState extends State<OmniMixApp> {
  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    super.dispose();
  }

  void _onStateChanged() => setState(() {});

  @override
  Widget build(BuildContext context) {
    final st = widget.state;

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
          home: _MainShell(state: st),
        );
      },
    );
  }

  Locale? _resolveLocale(String lang) {
    if (lang == 'system') return null; // follow system
    if (lang == 'zh') return const Locale('zh');
    return const Locale('en');
  }
}

/// Responsive shell: sidebar on wide, bottom nav on narrow (breakpoint 800px).
class _MainShell extends StatelessWidget {
  final AppState state;

  const _MainShell({required this.state});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= 800;
        return _MainContent(state: state, isWide: isWide);
      },
    );
  }
}

class _MainContent extends StatefulWidget {
  final AppState state;
  final bool isWide;

  const _MainContent({required this.state, required this.isWide});

  @override
  State<_MainContent> createState() => _MainContentState();
}

class _MainContentState extends State<_MainContent> {
  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onChange);
  }

  @override
  void dispose() {
    widget.state.removeListener(_onChange);
    super.dispose();
  }

  void _onChange() {
    final error = widget.state.consumeError();
    if (error != null && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error),
          behavior: SnackBarBehavior.floating,
          duration: const Duration(seconds: 4),
        ),
      );
    }
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final st = widget.state;
    final isWide = widget.isWide;
    final cs = Theme.of(context).colorScheme;

    // ── Wide layout: AppBar + Sidebar (unified color) ──
    if (isWide) {
      return Scaffold(
        backgroundColor: cs.surfaceContainer,
        appBar: _buildTopBar(context, l10n, st),
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
                NavigationRailDestination(
                  icon: const Icon(Icons.sports_esports),
                  label: Text(l10n.gameIntegration),
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

    // ── Narrow layout: AppBar + BottomNav ──
    return Scaffold(
      backgroundColor: cs.surface,
      appBar: _buildTopBar(context, l10n, st),
      body: st.hasOverlay
          ? _buildOverlay(context, l10n, st, isWide)
          : _buildTabContent(st),
      bottomNavigationBar: NavigationBar(
        selectedIndex: st.currentTab,
        onDestinationSelected: (i) => st.selectTab(i),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.home), label: ' '),
          NavigationDestination(icon: Icon(Icons.library_music), label: ' '),
          NavigationDestination(
            icon: Icon(Icons.grid_view_rounded),
            label: ' ',
          ),
          NavigationDestination(icon: Icon(Icons.extension), label: ' '),
          NavigationDestination(icon: Icon(Icons.sports_esports), label: ' '),
          NavigationDestination(icon: Icon(Icons.settings), label: ' '),
        ],
      ),
    );
  }

  PreferredSizeWidget _buildTopBar(
    BuildContext context,
    AppLocalizations l10n,
    AppState st,
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
              constraints: const BoxConstraints(maxWidth: 420),
              child: _GlobalInstanceDropdown(state: st),
            ),
            Positioned(
              right: 16,
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: st.backendOnline ? Colors.green : Colors.grey,
                    ),
                  ),
                  const SizedBox(width: 6),
                  Text(
                    st.backendOnline ? l10n.connected : l10n.disconnected,
                    style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
      centerTitle: true,
    );
  }

  Widget _buildTabContent(AppState st) {
    switch (st.currentTab) {
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
    if (st.overlayMode == 'about') {
      return AboutPage(onBack: st.closeOverlay);
    }

    // Module link/settings overlay (ProxyNode rendering)
    final overlayBg = st.overlayUiTree != null
        ? parseHexColor(st.overlayUiTree!.color)
        : null;

    return Scaffold(
      backgroundColor: overlayBg,
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: st.closeOverlay,
        ),
        title: Text(st.overlayTitle),
      ),
      body: st.overlayUiTree != null
          ? SingleChildScrollView(
              child: ProxyNodeWidget(
                node: st.overlayUiTree!,
                onDispatch: st.dispatchUiEvent,
                imageBaseUrl: st.apiBaseUrl,
              ),
            )
          : const Center(child: CircularProgressIndicator()),
    );
  }
}

class _GlobalInstanceDropdown extends StatelessWidget {
  final AppState state;

  const _GlobalInstanceDropdown({required this.state});

  @override
  Widget build(BuildContext context) {
    final allInstances = state.instances;
    final onlineIds = state.backendOnline
        ? state.playbackInstances.map((i) => i.id).toSet()
        : <String>{};
    final cs = Theme.of(context).colorScheme;

    if (allInstances.isEmpty) {
      return Text(
        '没有实例',
        style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant),
      );
    }

    final activeId = state.activeInstanceId;
    final displayId =
        (activeId != null && allInstances.any((i) => i.instanceId == activeId))
        ? activeId
        : allInstances.first.instanceId;

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
            final online = onlineIds.contains(inst.instanceId);
            return DropdownMenuItem<String>(
              value: inst.instanceId,
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
                      inst.gameName,
                      style: const TextStyle(fontSize: 13),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    inst.mode,
                    style: TextStyle(fontSize: 11, color: cs.outline),
                  ),
                ],
              ),
            );
          }).toList(),
          onChanged: (v) {
            if (v != null) state.selectInstance(v);
          },
        ),
      ),
    );
  }
}
