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
        final lightScheme =
            lightDynamic ?? ColorScheme.fromSeed(seedColor: Colors.deepPurple);
        final darkScheme =
            darkDynamic ??
            ColorScheme.fromSeed(
              seedColor: Colors.deepPurple,
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
    final l10n = AppLocalizations.of(context)!;
    final st = widget.state;
    final isWide = widget.isWide;
    final cs = Theme.of(context).colorScheme;

    // ── Overlay for module link/settings/about ──
    if (st.hasOverlay) {
      return _buildOverlay(context, l10n, st, isWide);
    }

    // ── Wide layout: AppBar + Sidebar ──
    if (isWide) {
      return Scaffold(
        backgroundColor: cs.surface,
        appBar: _buildTopBar(context, l10n, st),
        body: Row(
          children: [
            NavigationRail(
              selectedIndex: st.currentTab,
              backgroundColor: cs.surfaceContainerLow,
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
                  icon: const Icon(Icons.settings),
                  label: Text(l10n.settings),
                ),
              ],
            ),
            const VerticalDivider(width: 1),
            Expanded(child: _buildTabContent(st)),
          ],
        ),
      );
    }

    // ── Narrow layout: AppBar + BottomNav ──
    return Scaffold(
      backgroundColor: cs.surface,
      appBar: _buildTopBar(context, l10n, st),
      body: _buildTabContent(st),
      bottomNavigationBar: NavigationBar(
        selectedIndex: st.currentTab,
        onDestinationSelected: (i) => st.selectTab(i),
        destinations: [
          NavigationDestination(icon: const Icon(Icons.home), label: l10n.home),
          NavigationDestination(
            icon: const Icon(Icons.library_music),
            label: l10n.playlist,
          ),
          NavigationDestination(
            icon: const Icon(Icons.grid_view_rounded),
            label: l10n.launchpad,
          ),
          NavigationDestination(
            icon: const Icon(Icons.extension),
            label: l10n.modules,
          ),
          NavigationDestination(
            icon: const Icon(Icons.settings),
            label: l10n.settings,
          ),
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
    final showBack = st.hasModuleDetail || st.hasOverlay;

    return AppBar(
      backgroundColor: cs.surfaceContainer,
      title: showBack
          ? Text(
              st.overlayTitle.isNotEmpty
                  ? st.overlayTitle
                  : st.activeModuleId ?? '',
            )
          : null,
      leading: showBack
          ? IconButton(
              icon: const Icon(Icons.arrow_back),
              onPressed: () {
                if (st.hasOverlay) {
                  st.closeOverlay();
                } else {
                  st.closeModule();
                }
              },
            )
          : null,
      actions: [
        Padding(
          padding: const EdgeInsets.only(right: 16),
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
        return LaunchpadGrid(
          links: allLinks,
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
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: st.closeOverlay,
        ),
        title: Text(st.overlayTitle),
      ),
      body: st.overlayUiTree != null
          ? SingleChildScrollView(
              padding: const EdgeInsets.all(16),
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
