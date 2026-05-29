import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';

import '../models/node_data.dart';
import '../providers/app_state.dart';
import '../services/api_client.dart';

class HomePage extends StatefulWidget {
  final AppState state;

  const HomePage({super.key, required this.state});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  List<TagInfo> _tags = [];
  List<AlbumInfo> _albums = [];
  List<SongInfo> _songs = [];
  String _query = '';
  bool _libraryLoading = false;
  String _error = '';
  bool _wasOnline = false;
  int _libraryReloadGuard = 0;

  @override
  void initState() {
    super.initState();
    _wasOnline = widget.state.backendOnline;
    widget.state.addListener(_onStateChanged);
    if (_wasOnline) _loadLibrary();
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    super.dispose();
  }

  void _onStateChanged() {
    if (!mounted) return;
    final online = widget.state.backendOnline;
    if (online && !_wasOnline && _tags.isEmpty) {
      _loadLibrary();
    }
    // Reload library when playlist sources change
    if (online && _tags.isNotEmpty) {
      final guard = widget.state.playlistSources.length;
      if (guard != _libraryReloadGuard) {
        _libraryReloadGuard = guard;
        _loadLibrary();
      }
    }
    _wasOnline = online;
    setState(() {});
  }

  Future<void> _loadLibrary() async {
    if (!widget.state.backendOnline) return;
    setState(() {
      _libraryLoading = true;
      _error = '';
    });
    try {
      final results = await Future.wait([
        widget.state.api.getTags(),
        widget.state.api.getAlbums(),
        widget.state.api.getSongs(),
      ]);
      if (!mounted) return;
      setState(() {
        _tags = results[0] as List<TagInfo>;
        _albums = results[1] as List<AlbumInfo>;
        _songs = results[2] as List<SongInfo>;
      });
    } catch (e) {
      if (mounted) setState(() => _error = '加载曲库失败: $e');
    } finally {
      if (mounted) setState(() => _libraryLoading = false);
    }
  }

  List<SongInfo> get _filteredSongs {
    if (_query.isEmpty) return _songs;
    final q = _query.toLowerCase();
    return _songs.where((s) {
      return s.title.toLowerCase().contains(q) ||
          s.artist.toLowerCase().contains(q);
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final busy = widget.state.backendBusy || widget.state.serviceBusy;

    if (busy) {
      return _LoadingHome(title: l10n.appTitle, message: l10n.restarting);
    }

    if (!widget.state.backendOnline) {
      return _LoadingHome(title: l10n.appTitle, message: l10n.disconnected);
    }

    final cs = Theme.of(context).colorScheme;
    final instance = widget.state.activeInstance;
    final canControl = widget.state.canControlActiveInstance;

    return RefreshIndicator(
      onRefresh: () async {
        await widget.state.refreshPlayback();
        await _loadLibrary();
      },
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 980;
          final content = compact
              ? Column(
                  children: [
                    _buildNowPlaying(cs, instance, canControl),
                    const SizedBox(height: 16),
                    _buildLists(cs, compact: true),
                    const SizedBox(height: 16),
                    _buildLibrary(cs, compact: true),
                  ],
                )
              : Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(flex: 11, child: _buildNowPlaying(cs, instance, canControl)),
                    const SizedBox(width: 18),
                    Expanded(flex: 13, child: _buildLists(cs)),
                    const SizedBox(width: 18),
                    Expanded(flex: 10, child: _buildLibrary(cs)),
                  ],
                );

          return ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(24),
            children: [content],
          );
        },
      ),
    );
  }

  Widget _buildNowPlaying(ColorScheme cs, PlaybackInstanceInfo? instance, bool canControl) {
    final track = instance?.currentTrack;
    final duration = track?.duration ?? 0;
    final position = instance?.position ?? 0;

    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  '实例',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                ),
              ),
              if (widget.state.playbackLoading)
                const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
            ],
          ),
          const SizedBox(height: 12),
          _InstanceSelector(
            instances: widget.state.playbackInstances,
            activeId: instance?.id,
            onSelected: widget.state.selectPlaybackInstance,
          ),
          const SizedBox(height: 18),
          AspectRatio(
            aspectRatio: 1,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: _Cover(uuid: track?.uuid ?? '', baseUrl: widget.state.apiBaseUrl),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            track?.title.isNotEmpty == true ? track!.title : '没有正在播放的歌曲',
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 6),
          Text(
            track?.artist.isNotEmpty == true ? track!.artist : instance?.id ?? '等待音频实例连接',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: cs.onSurfaceVariant),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Text(_formatDuration(position), style: TextStyle(color: cs.onSurfaceVariant, fontSize: 12)),
              Expanded(
                child: SliderTheme(
                  data: SliderThemeData(
                    trackHeight: 4,
                    thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 6),
                    activeTrackColor: cs.primary,
                    inactiveTrackColor: cs.surfaceContainerHighest,
                    thumbColor: cs.primary,
                    overlayColor: cs.primary.withAlpha(20),
                  ),
                  child: Slider(
                    value: duration > 0 ? position.clamp(0, duration) : 0,
                    min: 0,
                    max: duration > 0 ? duration : 1,
                    onChanged: canControl && duration > 0 ? (value) => widget.state.seekActive(value) : null,
                  ),
                ),
              ),
              Text(_formatDuration(duration), style: TextStyle(color: cs.onSurfaceVariant, fontSize: 12)),
            ],
          ),
          const SizedBox(height: 10),
          _ModeNotice(instance: instance, canControl: canControl),
        ],
      ),
    );
  }

  Widget _buildLists(ColorScheme cs, {bool compact = false}) {
    final currentSourceName = widget.state.playlistSources.isNotEmpty
        ? widget.state.playlistSources.first.name
        : '全部';
    final hasPlaylist = widget.state.activePlaylist.isNotEmpty;

    return Column(
      children: [
        _Panel(
          child: _SectionList<PlaylistSourceInfo>(
            title: '激活歌单',
            icon: Icons.featured_play_list_rounded,
            items: widget.state.playlistSources,
            emptyText: '还没有添加歌单',
            action: widget.state.canControlActiveInstance
                ? TextButton.icon(
                    onPressed: _showAddPlaylistSheet,
                    icon: const Icon(Icons.add_rounded),
                    label: const Text('添加'),
                  )
                : null,
            itemBuilder: (source) => ListTile(
              dense: true,
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.queue_music_rounded),
              title: Text(source.name, maxLines: 1, overflow: TextOverflow.ellipsis),
              subtitle: Text('${source.songCount} 首'),
              trailing: source.id != 'all' && widget.state.canControlActiveInstance
                  ? IconButton(
                      tooltip: '移除',
                      icon: const Icon(Icons.close_rounded),
                      onPressed: () => widget.state.removePlaylistSource(source.id),
                    )
                  : null,
            ),
          ),
        ),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.playlist_remove_rounded, size: 20),
                  const SizedBox(width: 8),
                  Text(
                    currentSourceName,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  const Spacer(),
                  if (hasPlaylist)
                    Text('${widget.state.activePlaylist.length} 首',
                        style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
                ],
              ),
              if (!hasPlaylist)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 24),
                  child: Center(
                    child: Text('歌单为空',
                        style: TextStyle(color: cs.onSurfaceVariant.withAlpha(150), fontSize: 13)),
                  ),
                )
              else
                ...widget.state.activePlaylist.take(20).map((song) => _SongTile(
                      song: song,
                      baseUrl: widget.state.apiBaseUrl,
                      canControl: widget.state.canControlActiveInstance,
                      onPlay: widget.state.canControlActiveInstance
                          ? () => widget.state.playSongOnActive(song.uuid)
                          : null,
                      onAdd: widget.state.canControlActiveInstance
                          ? () => widget.state.addSongToActiveQueue(song.uuid)
                          : null,
                    )),
            ],
          ),
        ),
        const SizedBox(height: 16),
        _Panel(
          child: _SectionList<QueueItemInfo>(
            title: '播放队列',
            icon: Icons.playlist_play_rounded,
            items: widget.state.activeQueue,
            emptyText: '队列为空，可以从歌单或曲库添加',
            action: widget.state.canControlActiveInstance && widget.state.activeQueue.isNotEmpty
                ? TextButton(onPressed: widget.state.clearActiveQueue, child: const Text('清空'))
                : null,
            itemBuilder: (song) => _SongTile(
              song: song,
              baseUrl: widget.state.apiBaseUrl,
              canControl: widget.state.canControlActiveInstance,
              onRemove: widget.state.canControlActiveInstance
                  ? () => widget.state.removeQueueItem(song.index)
                  : null,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildLibrary(ColorScheme cs, {bool compact = false}) {
    return Column(
      children: [
        _Panel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.library_music_rounded, size: 20),
                  const SizedBox(width: 8),
                  Text(
                    '曲库',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  const Spacer(),
                  if (_tags.isNotEmpty)
                    Text('${_tags.length} 标签 · ${_albums.length} 专辑 · ${_songs.length} 歌曲',
                        style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
                ],
              ),
              const SizedBox(height: 12),
              TextField(
                onChanged: (v) => setState(() => _query = v),
                decoration: InputDecoration(
                  hintText: '搜索歌曲或艺术家...',
                  prefixIcon: const Icon(Icons.search_rounded, size: 20),
                  isDense: true,
                  contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                  filled: true,
                  fillColor: cs.surfaceContainerHighest.withAlpha(100),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Expanded(
          child: _buildLibraryContent(cs, compact),
        ),
      ],
    );
  }

  Widget _buildLibraryContent(ColorScheme cs, bool compact) {
    if (_libraryLoading) {
      return const Center(child: CircularProgressIndicator(strokeWidth: 2));
    }
    if (_error.isNotEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.error_outline_rounded, size: 28, color: cs.error),
            const SizedBox(height: 8),
            Text(_error, style: TextStyle(color: cs.error, fontSize: 12)),
            const SizedBox(height: 12),
            FilledButton.tonalIcon(
              onPressed: _loadLibrary,
              icon: const Icon(Icons.refresh_rounded, size: 16),
              label: const Text('重试'),
            ),
          ],
        ),
      );
    }

    if (_tags.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.library_music_outlined, size: 40, color: cs.onSurfaceVariant.withAlpha(80)),
            const SizedBox(height: 8),
            Text('曲库为空', style: TextStyle(color: cs.onSurfaceVariant)),
          ],
        ),
      );
    }

    final showSongs = _query.isNotEmpty;
    final songs = _filteredSongs;

    if (showSongs) {
      if (songs.isEmpty) {
        return Center(
          child: Text('未找到 "$_query"', style: TextStyle(color: cs.onSurfaceVariant)),
        );
      }
      return ListView.separated(
        itemCount: songs.length,
        separatorBuilder: (_, __) => Divider(height: 1, indent: 48, color: cs.outlineVariant.withAlpha(60)),
        itemBuilder: (_, i) {
          final s = songs[i];
          return _SongTile(
            song: QueueItemInfo(
              index: i,
              uuid: s.uuid,
              title: s.title,
              artist: s.artist,
              duration: s.duration,
            ),
            baseUrl: widget.state.apiBaseUrl,
            canControl: widget.state.canControlActiveInstance,
            onPlay: widget.state.canControlActiveInstance
                ? () => widget.state.playSongOnActive(s.uuid)
                : null,
            onAdd: widget.state.canControlActiveInstance
                ? () => widget.state.addSongToActiveQueue(s.uuid)
                : null,
          );
        },
      );
    }

    return DefaultTabController(
      length: 3,
      child: Column(
        children: [
          TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            labelColor: cs.primary,
            unselectedLabelColor: cs.onSurfaceVariant,
            indicatorColor: cs.primary,
            indicatorSize: TabBarIndicatorSize.label,
            labelStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
            tabs: const [
              Tab(text: '标签'),
              Tab(text: '专辑'),
              Tab(text: '歌曲'),
            ],
          ),
          const SizedBox(height: 8),
          Expanded(
            child: TabBarView(
              children: [
                _buildTagList(cs),
                _buildAlbumList(cs),
                _buildAllSongsList(cs, compact),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTagList(ColorScheme cs) {
    return ListView.separated(
      itemCount: _tags.length,
      separatorBuilder: (_, __) => Divider(height: 1, indent: 40, color: cs.outlineVariant.withAlpha(60)),
      itemBuilder: (_, i) {
        final t = _tags[i];
        return ListTile(
          dense: true,
          contentPadding: EdgeInsets.zero,
          leading: Icon(Icons.folder_rounded, color: cs.primary, size: 22),
          title: Text(t.name, maxLines: 1, overflow: TextOverflow.ellipsis),
          subtitle: Text(t.moduleId, style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
          trailing: Icon(Icons.chevron_right, size: 18, color: cs.onSurfaceVariant),
          onTap: widget.state.canControlActiveInstance
              ? () => widget.state.addTagToActivePlaylist(t)
              : null,
        );
      },
    );
  }

  Widget _buildAlbumList(ColorScheme cs) {
    return ListView.separated(
      itemCount: _albums.length,
      separatorBuilder: (_, __) => Divider(height: 1, indent: 40, color: cs.outlineVariant.withAlpha(60)),
      itemBuilder: (_, i) {
        final a = _albums[i];
        return ListTile(
          dense: true,
          contentPadding: EdgeInsets.zero,
          leading: ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: SizedBox(
              width: 32,
              height: 32,
              child: a.coverPath.isNotEmpty
                  ? Image.network(
                      '${widget.state.apiBaseUrl}/$a.coverPath',
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => Icon(Icons.album_rounded, color: cs.primary, size: 22),
                    )
                  : Icon(Icons.album_rounded, color: cs.primary, size: 22),
            ),
          ),
          title: Text(a.name, maxLines: 1, overflow: TextOverflow.ellipsis),
          subtitle: Text('${a.songCount} 首 · ${a.moduleId}', style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (widget.state.canControlActiveInstance)
                IconButton(
                  tooltip: '添加到歌单',
                  icon: Icon(Icons.playlist_add_rounded, size: 18, color: cs.primary),
                  onPressed: () => widget.state.addAlbumToActivePlaylist(a),
                ),
              Icon(Icons.chevron_right, size: 18, color: cs.onSurfaceVariant),
            ],
          ),
          onTap: () => _showAlbumSongs(a, cs),
        );
      },
    );
  }

  Widget _buildAllSongsList(ColorScheme cs, bool compact) {
    final songs = _filteredSongs;
    if (songs.isEmpty) {
      return Center(
        child: Text('暂无歌曲', style: TextStyle(color: cs.onSurfaceVariant)),
      );
    }
    return ListView.separated(
      itemCount: songs.length,
      separatorBuilder: (_, __) => Divider(height: 1, indent: 48, color: cs.outlineVariant.withAlpha(60)),
      itemBuilder: (_, i) {
        final s = songs[i];
        return _SongTile(
          song: QueueItemInfo(
            index: i,
            uuid: s.uuid,
            title: s.title,
            artist: s.artist,
            duration: s.duration,
          ),
          baseUrl: widget.state.apiBaseUrl,
          canControl: widget.state.canControlActiveInstance,
          onPlay: widget.state.canControlActiveInstance
              ? () => widget.state.playSongOnActive(s.uuid)
              : null,
          onAdd: widget.state.canControlActiveInstance
              ? () => widget.state.addSongToActiveQueue(s.uuid)
              : null,
        );
      },
    );
  }

  void _showAlbumSongs(AlbumInfo album, ColorScheme cs) {
    showModalBottomSheet(
      context: context,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return _AlbumSongsSheet(
          album: album,
          api: widget.state.api,
          apiBaseUrl: widget.state.apiBaseUrl,
          canControl: widget.state.canControlActiveInstance,
          onPlay: widget.state.canControlActiveInstance
              ? (uuid) {
                  widget.state.playSongOnActive(uuid);
                  Navigator.pop(ctx);
                }
              : null,
          onAdd: widget.state.canControlActiveInstance
              ? (uuid) => widget.state.addSongToActiveQueue(uuid)
              : null,
        );
      },
    );
  }

  void _showAddPlaylistSheet() {
    final cs = Theme.of(context).colorScheme;
    showModalBottomSheet(
      context: context,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return DraggableScrollableSheet(
          initialChildSize: 0.7,
          minChildSize: 0.4,
          maxChildSize: 0.85,
          expand: false,
          builder: (_, scrollController) {
            return Padding(
              padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: Container(
                      width: 40, height: 4,
                      decoration: BoxDecoration(
                        color: cs.onSurfaceVariant.withAlpha(80),
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Text('选择标签或专辑添加到歌单',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  const SizedBox(height: 12),
                  Expanded(
                    child: ListView(
                      controller: scrollController,
                      children: [
                        if (_tags.isNotEmpty) ...[
                          Padding(
                            padding: const EdgeInsets.only(bottom: 8),
                            child: Text('标签', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: cs.onSurfaceVariant)),
                          ),
                          ..._tags.map((t) => ListTile(
                            dense: true,
                            leading: Icon(Icons.folder_rounded, color: cs.primary, size: 22),
                            title: Text(t.name),
                            subtitle: Text(t.moduleId, style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
                            onTap: () {
                              widget.state.addTagToActivePlaylist(t);
                              Navigator.pop(ctx);
                            },
                          )),
                        ],
                        if (_albums.isNotEmpty) ...[
                          const SizedBox(height: 8),
                          Padding(
                            padding: const EdgeInsets.only(bottom: 8),
                            child: Text('专辑', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: cs.onSurfaceVariant)),
                          ),
                          ..._albums.map((a) => ListTile(
                            dense: true,
                            leading: Icon(Icons.album_rounded, color: cs.primary, size: 22),
                            title: Text(a.name),
                            subtitle: Text('${a.songCount} 首', style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
                            onTap: () {
                              widget.state.addAlbumToActivePlaylist(a);
                              Navigator.pop(ctx);
                            },
                          )),
                        ],
                        if (_tags.isEmpty && _albums.isEmpty)
                          Padding(
                            padding: const EdgeInsets.all(24),
                            child: Center(
                              child: Text('没有可添加的内容', style: TextStyle(color: cs.onSurfaceVariant)),
                            ),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  String _formatDuration(double seconds) {
    if (seconds <= 0) return '0:00';
    final m = (seconds ~/ 60).toString();
    final s = (seconds % 60).toInt().toString().padLeft(2, '0');
    return '$m:$s';
  }
}

// ═══════════════════════════════════════════════════════════
//  Private widgets used by HomePage
// ═══════════════════════════════════════════════════════════

class _LoadingHome extends StatelessWidget {
  final String title;
  final String message;

  const _LoadingHome({required this.title, required this.message});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.wifi_tethering_rounded, size: 64, color: cs.primary.withAlpha(120)),
          const SizedBox(height: 16),
          Text(title, style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          Text(message, style: TextStyle(color: cs.onSurfaceVariant)),
          const SizedBox(height: 24),
          const CircularProgressIndicator(strokeWidth: 2),
        ],
      ),
    );
  }
}

class _Panel extends StatelessWidget {
  final Widget child;

  const _Panel({required this.child});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      decoration: BoxDecoration(
        color: cs.surfaceContainerLow,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant.withAlpha(60)),
      ),
      padding: const EdgeInsets.all(16),
      child: child,
    );
  }
}

class _InstanceSelector extends StatelessWidget {
  final List<PlaybackInstanceInfo> instances;
  final String? activeId;
  final ValueChanged<String> onSelected;

  const _InstanceSelector({
    required this.instances,
    required this.activeId,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    if (instances.isEmpty) {
      return Text('没有音频实例', style: TextStyle(color: cs.onSurfaceVariant, fontSize: 13));
    }
    return Container(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: cs.outlineVariant.withAlpha(120)),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<String>(
          value: activeId != null && instances.any((i) => i.id == activeId) ? activeId : instances.first.id,
          isExpanded: true,
          icon: Icon(Icons.expand_more_rounded, color: cs.onSurfaceVariant),
          style: Theme.of(context).textTheme.bodyMedium,
          items: instances.map((i) {
            final isActive = i.id == activeId;
            return DropdownMenuItem(
              value: i.id,
              child: Row(
                children: [
                  Icon(
                    i.attached ? Icons.volume_up_rounded : Icons.volume_mute_rounded,
                    size: 16,
                    color: isActive ? cs.primary : cs.onSurfaceVariant,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      i.mode == 'ServerManaged' ? '${i.id} (服务端)' : i.id,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontWeight: isActive ? FontWeight.w700 : FontWeight.normal,
                        color: isActive ? cs.primary : null,
                      ),
                    ),
                  ),
                ],
              ),
            );
          }).toList(),
          onChanged: (v) {
            if (v != null) onSelected(v);
          },
        ),
      ),
    );
  }
}

class _Cover extends StatelessWidget {
  final String uuid;
  final String baseUrl;

  const _Cover({required this.uuid, required this.baseUrl});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    if (uuid.isEmpty) {
      return Container(
        color: cs.surfaceContainerHighest,
        child: Center(
          child: Icon(Icons.music_note_rounded, size: 64, color: cs.onSurfaceVariant.withAlpha(80)),
        ),
      );
    }
    return Image.network(
      '$baseUrl/api/cover/$uuid',
      fit: BoxFit.cover,
      errorBuilder: (_, __, ___) => Container(
        color: cs.surfaceContainerHighest,
        child: Center(
          child: Icon(Icons.broken_image_rounded, size: 48, color: cs.onSurfaceVariant.withAlpha(80)),
        ),
      ),
      loadingBuilder: (_, child, progress) {
        if (progress == null) return child;
        return Container(
          color: cs.surfaceContainerHighest,
          child: Center(
            child: CircularProgressIndicator(
              value: progress.expectedTotalBytes != null
                  ? progress.cumulativeBytesLoaded / progress.expectedTotalBytes!
                  : null,
              strokeWidth: 2,
            ),
          ),
        );
      },
    );
  }
}

class _ModeNotice extends StatelessWidget {
  final PlaybackInstanceInfo? instance;
  final bool canControl;

  const _ModeNotice({required this.instance, required this.canControl});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    if (instance == null) return const SizedBox.shrink();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: canControl ? cs.primaryContainer.withAlpha(100) : cs.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            canControl ? Icons.play_circle_fill_rounded : Icons.info_outline_rounded,
            size: 14,
            color: canControl ? cs.onPrimaryContainer : cs.onSurfaceVariant,
          ),
          const SizedBox(width: 6),
          Text(
            canControl ? '服务端管理模式' : '客户端管理模式 — 部分功能受限',
            style: TextStyle(
              fontSize: 11,
              color: canControl ? cs.onPrimaryContainer : cs.onSurfaceVariant,
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionList<T> extends StatelessWidget {
  final String title;
  final IconData icon;
  final List<T> items;
  final String emptyText;
  final Widget? action;
  final Widget Function(T) itemBuilder;

  const _SectionList({
    required this.title,
    required this.icon,
    required this.items,
    required this.emptyText,
    this.action,
    required this.itemBuilder,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Icon(icon, size: 20),
            const SizedBox(width: 8),
            Text(title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
            const Spacer(),
            if (action != null) action!,
          ],
        ),
        if (items.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 24),
            child: Center(
              child: Text(emptyText, style: TextStyle(color: cs.onSurfaceVariant.withAlpha(150), fontSize: 13)),
            ),
          )
        else
          ...items.map((item) => itemBuilder(item)),
      ],
    );
  }
}

class _SongTile extends StatelessWidget {
  final QueueItemInfo song;
  final String baseUrl;
  final bool canControl;
  final VoidCallback? onRemove;
  final VoidCallback? onPlay;
  final VoidCallback? onAdd;

  const _SongTile({
    required this.song,
    required this.baseUrl,
    required this.canControl,
    this.onRemove,
    this.onPlay,
    this.onAdd,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final menuItems = <PopupMenuEntry<VoidCallback>>[
      if (onPlay != null)
        PopupMenuItem(value: onPlay!, child: const ListTile(leading: Icon(Icons.play_arrow_rounded), title: Text('播放'), dense: true, contentPadding: EdgeInsets.zero)),
      if (onAdd != null)
        PopupMenuItem(value: onAdd!, child: const ListTile(leading: Icon(Icons.playlist_add_rounded), title: Text('添加到队列'), dense: true, contentPadding: EdgeInsets.zero)),
      if (onRemove != null)
        PopupMenuItem(value: onRemove!, child: const ListTile(leading: Icon(Icons.remove_circle_outline_rounded), title: Text('移除'), dense: true, contentPadding: EdgeInsets.zero)),
    ];

    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap: onPlay,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Row(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: SizedBox(
                width: 36, height: 36,
                child: _Cover(uuid: song.uuid, baseUrl: baseUrl),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(song.title, maxLines: 1, overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 13)),
                  const SizedBox(height: 2),
                  Text(song.artist.isNotEmpty ? song.artist : '未知艺术家',
                      maxLines: 1, overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
                ],
              ),
            ),
            if (song.duration > 0)
              Padding(
                padding: const EdgeInsets.only(right: 4),
                child: Text(_formatDuration(song.duration),
                    style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
              ),
            if (menuItems.isNotEmpty)
              PopupMenuButton(
                icon: Icon(Icons.more_vert_rounded, size: 16, color: cs.onSurfaceVariant),
                onSelected: (cb) => cb(),
              itemBuilder: (_) => menuItems,
                position: PopupMenuPosition.over,
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(),
              ),
          ],
        ),
      ),
    );
  }

  String _formatDuration(double seconds) {
    if (seconds <= 0) return '0:00';
    final m = (seconds ~/ 60).toString();
    final s = (seconds % 60).toInt().toString().padLeft(2, '0');
    return '$m:$s';
  }
}

class _AlbumSongsSheet extends StatefulWidget {
  final AlbumInfo album;
  final ApiClient api;
  final String apiBaseUrl;
  final bool canControl;
  final void Function(String uuid)? onPlay;
  final void Function(String uuid)? onAdd;

  const _AlbumSongsSheet({
    required this.album,
    required this.api,
    required this.apiBaseUrl,
    required this.canControl,
    this.onPlay,
    this.onAdd,
  });

  @override
  State<_AlbumSongsSheet> createState() => _AlbumSongsSheetState();
}

class _AlbumSongsSheetState extends State<_AlbumSongsSheet> {
  late Future<List<SongInfo>> _songsFuture;

  @override
  void initState() {
    super.initState();
    _songsFuture = widget.api.getSongs(albumId: widget.album.id);
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return DraggableScrollableSheet(
      initialChildSize: 0.7,
      minChildSize: 0.4,
      maxChildSize: 0.85,
      expand: false,
      builder: (_, scrollController) {
        return Padding(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40, height: 4,
                  decoration: BoxDecoration(
                    color: cs.onSurfaceVariant.withAlpha(80),
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: SizedBox(
                      width: 48, height: 48,
                      child: widget.album.coverPath.isNotEmpty
                          ? Image.network(
                              '${widget.apiBaseUrl}/${widget.album.coverPath}',
                              fit: BoxFit.cover,
                              errorBuilder: (_, __, ___) => Icon(Icons.album_rounded, color: cs.primary),
                            )
                          : Icon(Icons.album_rounded, color: cs.primary, size: 32),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(widget.album.name,
                            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                        Text('${widget.album.songCount} 首',
                            style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant)),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Expanded(
                child: FutureBuilder<List<SongInfo>>(
                  future: _songsFuture,
                  builder: (_, snap) {
                    if (snap.connectionState == ConnectionState.waiting) {
                      return const Center(child: CircularProgressIndicator(strokeWidth: 2));
                    }
                    if (snap.hasError) {
                      return Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.error_outline, color: cs.error),
                            const SizedBox(height: 8),
                            Text('加载失败', style: TextStyle(color: cs.error)),
                          ],
                        ),
                      );
                    }
                    final songs = snap.data ?? [];
                    if (songs.isEmpty) {
                      return Center(child: Text('暂无歌曲', style: TextStyle(color: cs.onSurfaceVariant)));
                    }
                    return ListView.separated(
                      controller: scrollController,
                      itemCount: songs.length,
                      separatorBuilder: (_, __) => Divider(height: 1, indent: 48, color: cs.outlineVariant.withAlpha(60)),
                      itemBuilder: (_, i) {
                        final s = songs[i];
                        return _SongTile(
                          song: QueueItemInfo(
                            index: i,
                            uuid: s.uuid,
                            title: s.title,
                            artist: s.artist,
                            duration: s.duration,
                          ),
                          baseUrl: widget.apiBaseUrl,
                          canControl: widget.canControl,
                          onPlay: widget.onPlay != null ? () => widget.onPlay!(s.uuid) : null,
                          onAdd: widget.onAdd != null ? () => widget.onAdd!(s.uuid) : null,
                        );
                      },
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
