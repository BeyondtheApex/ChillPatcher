import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../generated/omni_mix_player/models/playlist.pb.dart';
import '../generated/omni_mix_player/models/album.pb.dart';
import '../generated/omni_mix_player/models/track.pb.dart';

/// 曲库浏览器 — 树形三级结构：歌单(模块) → 专辑 → 曲目
///
/// 这是注册表的静态快照浏览器，不是播放控制组件。
/// 展开/折叠时重建扁平列表，ListView.builder 虚拟滚动保证性能。
class PlaylistPage extends StatefulWidget {
  final AppState state;

  const PlaylistPage({super.key, required this.state});

  @override
  State<PlaylistPage> createState() => _PlaylistPageState();
}

// ── 内部树节点 ──

class _AlbumNode {
  final Album album;
  List<Track> songs;
  bool expanded;

  _AlbumNode(this.album, {this.songs = const [], this.expanded = false});
}

class _PlaylistNode {
  final Playlist playlist;
  final List<_AlbumNode> albums;
  bool expanded;

  _PlaylistNode(this.playlist, this.albums, {this.expanded = false});
}

// ── 扁平列表条目（供 ListView.builder 虚拟滚动） ──

enum _ItemKind { playlist, album, song }

class _FlatItem {
  final _ItemKind kind;
  final String label;
  final String subtitle;
  final int indentLevel;
  final bool expandable;
  final bool expanded;

  final Playlist? playlist;
  final Album? album;
  final Track? song;

  const _FlatItem({
    required this.kind,
    required this.label,
    this.subtitle = '',
    required this.indentLevel,
    this.expandable = false,
    this.expanded = false,
    this.playlist,
    this.album,
    this.song,
  });
}

// Page state

class _PlaylistPageState extends State<PlaylistPage> {
  final List<_PlaylistNode> _tree = [];
  final List<_FlatItem> _flatItems = [];
  bool _loading = false;
  String _error = '';
  int _totalSongs = 0;
  int _lastLibGen = 0;
  int _loadSerial = 0;

  @override
  void initState() {
    super.initState();
    _lastLibGen = widget.state.libraryGeneration;
    widget.state.addListener(_onStateChanged);
    if (widget.state.backendOnline) _loadTree();
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    super.dispose();
  }

  void _onStateChanged() {
    if (!mounted) return;
    // Event-driven reload: when modules load/unload or playlist updates
    if (widget.state.backendOnline &&
        widget.state.libraryGeneration != _lastLibGen) {
      _lastLibGen = widget.state.libraryGeneration;
      _loadTree();
    }
    if (widget.state.backendOnline && _tree.isEmpty && !_loading) {
      _loadTree();
    }
    setState(() {});
  }

  // ── 数据加载：单次 GET 获取全部歌单+专辑+曲目 ──

  Future<void> _loadTree() async {
    if (!widget.state.backendOnline) return;
    final loadSerial = ++_loadSerial;
    setState(() {
      _loading = true;
      _error = '';
    });
    try {
      final playlists = await widget.state.api.getPlaylists();
      final allAlbums = await widget.state.api.getAlbums();
      if (!mounted || loadSerial != _loadSerial) return;

      final nextTree = <_PlaylistNode>[];
      var total = 0;
      for (final playlist in playlists) {
        final playlistSongs = await widget.state.api.getSongs(
          playlistId: playlist.id,
        );
        if (!mounted || loadSerial != _loadSerial) return;
        final tagAlbumIds = playlistSongs.map((s) => s.albumId).toSet();
        final filteredAlbums = allAlbums
            .where((a) => tagAlbumIds.contains(a.id))
            .toList();
        final albumNodes = <_AlbumNode>[];
        for (final album in filteredAlbums) {
          final albumSongs = playlistSongs
              .where((s) => s.albumId == album.id)
              .toList();
          albumNodes.add(_AlbumNode(album, songs: albumSongs));
          total += albumSongs.length;
        }
        nextTree.add(_PlaylistNode(playlist, albumNodes));
      }
      if (!mounted || loadSerial != _loadSerial) return;
      _tree
        ..clear()
        ..addAll(nextTree);
      _totalSongs = total;
      _rebuildFlatList();
    } catch (e) {
      if (mounted && loadSerial == _loadSerial) {
        final l10n = context.mounted ? AppLocalizations.of(context) : null;
        _error =
            l10n?.loadLibraryFailed(e.toString()) ??
            'Failed to load library: $e';
      }
    } finally {
      if (mounted && loadSerial == _loadSerial) {
        setState(() => _loading = false);
      }
    }
  }

  // ── 根据展开状态重建扁平列表（虚拟滚动数据源） ──

  void _rebuildFlatList() {
    final l10n = AppLocalizations.of(context)!;
    _flatItems.clear();
    for (final playlistNode in _tree) {
      _flatItems.add(
        _FlatItem(
          kind: _ItemKind.playlist,
          label: playlistNode.playlist.name,
          subtitle:
              '${playlistNode.playlist.moduleId} • ${l10n.albumCountLabel(playlistNode.albums.length)}',
          indentLevel: 0,
          expandable: playlistNode.albums.isNotEmpty,
          expanded: playlistNode.expanded,
          playlist: playlistNode.playlist,
        ),
      );
      if (!playlistNode.expanded) continue;

      for (final albumNode in playlistNode.albums) {
        _flatItems.add(
          _FlatItem(
            kind: _ItemKind.album,
            label: albumNode.album.title,
            subtitle: l10n.songCountLabel(albumNode.songs.length),
            indentLevel: 1,
            expandable: albumNode.songs.isNotEmpty,
            expanded: albumNode.expanded,
            album: albumNode.album,
          ),
        );
        if (!albumNode.expanded) continue;

        for (final song in albumNode.songs) {
          _flatItems.add(
            _FlatItem(
              kind: _ItemKind.song,
              label: song.title,
              subtitle: song.artist,
              indentLevel: 2,
              song: song,
            ),
          );
        }
      }
    }
    setState(() {});
  }

  // ── 展开/折叠 ──

  void _togglePlaylist(int flatIndex) {
    final item = _flatItems[flatIndex];
    if (item.playlist == null) return;
    for (final p in _tree) {
      if (p.playlist.id == item.playlist!.id) {
        p.expanded = !p.expanded;
        _rebuildFlatList();
        return;
      }
    }
  }

  void _toggleAlbum(int flatIndex) {
    final item = _flatItems[flatIndex];
    if (item.album == null) return;
    for (final playlistNode in _tree) {
      for (final albumNode in playlistNode.albums) {
        if (albumNode.album.id == item.album!.id) {
          albumNode.expanded = !albumNode.expanded;
          _rebuildFlatList();
          return;
        }
      }
    }
  }

  // ── 构建 ──

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;

    if (!widget.state.backendOnline) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.cloud_off_rounded,
              size: 48,
              color: cs.onSurfaceVariant.withAlpha(100),
            ),
            const SizedBox(height: 12),
            Text(
              l10n.backendNotConnected,
              style: TextStyle(color: cs.onSurfaceVariant),
            ),
          ],
        ),
      );
    }

    if (_loading && _tree.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // ── 标题栏 ──
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
          child: Row(
            children: [
              const Icon(Icons.library_music_rounded, size: 28),
              const SizedBox(width: 12),
              Text(
                l10n.libraryBrowser,
                style: const TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const Spacer(),
              if (_totalSongs > 0)
                Text(
                  l10n.playlistStats(_tree.length, _totalSongs),
                  style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant),
                ),
              const SizedBox(width: 8),
              if (_tree.isNotEmpty)
                IconButton(
                  icon: const Icon(Icons.refresh_rounded, size: 20),
                  tooltip: l10n.refresh,
                  onPressed: _loadTree,
                ),
            ],
          ),
        ),
        const Divider(height: 1),

        if (_error.isNotEmpty)
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Icon(Icons.error_outline, size: 16, color: cs.error),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _error,
                    style: TextStyle(color: cs.error, fontSize: 12),
                  ),
                ),
              ],
            ),
          ),

        // ── 树形浏览器（虚拟滚动） ──
        Expanded(
          child: _flatItems.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        Icons.playlist_remove_rounded,
                        size: 48,
                        color: cs.onSurfaceVariant.withAlpha(80),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        l10n.libraryEmpty,
                        style: TextStyle(
                          color: cs.onSurfaceVariant.withAlpha(150),
                          fontSize: 14,
                        ),
                      ),
                    ],
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  itemCount: _flatItems.length,
                  itemExtent: 48,
                  itemBuilder: (_, i) => _buildRow(_flatItems[i], i, cs),
                ),
        ),
      ],
    );
  }

  Widget _buildRow(_FlatItem item, int index, ColorScheme cs) {
    switch (item.kind) {
      case _ItemKind.playlist:
        return _buildPlaylistRow(item, index, cs);
      case _ItemKind.album:
        return _buildAlbumRow(item, index, cs);
      case _ItemKind.song:
        return _buildSongRow(item, cs);
    }
  }

  // ── 歌单行（一级） ──

  Widget _buildPlaylistRow(_FlatItem item, int index, ColorScheme cs) {
    return InkWell(
      onTap: () => _togglePlaylist(index),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 0),
        child: SizedBox(
          height: 48,
          child: Row(
            children: [
              Icon(
                item.expanded
                    ? Icons.expand_more_rounded
                    : Icons.chevron_right_rounded,
                size: 22,
                color: cs.onSurfaceVariant,
              ),
              const SizedBox(width: 8),
              Icon(
                Icons.queue_music_rounded,
                size: 22,
                color: cs.primary.withAlpha(180),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.label,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      item.subtitle,
                      style: TextStyle(
                        fontSize: 11,
                        color: cs.onSurfaceVariant,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
              if (item.playlist != null) ...[
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: cs.surfaceContainerHighest.withAlpha(120),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    '${_tree.firstWhere((p) => p.playlist.id == item.playlist!.id).albums.length}',
                    style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  // ── 专辑行（二级） ──

  Widget _buildAlbumRow(_FlatItem item, int index, ColorScheme cs) {
    return InkWell(
      onTap: () => _toggleAlbum(index),
      child: Padding(
        padding: const EdgeInsets.only(left: 68, right: 20, top: 0, bottom: 0),
        child: SizedBox(
          height: 48,
          child: Row(
            children: [
              Icon(
                item.expanded
                    ? Icons.expand_more_rounded
                    : Icons.chevron_right_rounded,
                size: 18,
                color: cs.onSurfaceVariant,
              ),
              const SizedBox(width: 6),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: Container(
                  width: 32,
                  height: 32,
                  color: cs.surfaceContainerHighest,
                  child: Center(
                    child: Icon(
                      Icons.album_rounded,
                      size: 18,
                      color: cs.onSurfaceVariant.withAlpha(120),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.label,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w500,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      item.subtitle,
                      style: TextStyle(
                        fontSize: 11,
                        color: cs.onSurfaceVariant,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ── 曲目行（三级） ──

  Widget _buildSongRow(_FlatItem item, ColorScheme cs) {
    final uuid = item.song?.uuid ?? '';
    final duration = item.song?.duration ?? 0.0;

    return InkWell(
      onTap: () {
        if (uuid.isNotEmpty) {
          widget.state.playSongOnActive(uuid);
        }
      },
      child: Padding(
        padding: const EdgeInsets.only(left: 112, right: 20, top: 0, bottom: 0),
        child: SizedBox(
          height: 48,
          child: Row(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: SizedBox(
                  width: 28,
                  height: 28,
                  child: _Cover(uuid: uuid, baseUrl: widget.state.apiBaseUrl),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.label,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w400,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    if (item.subtitle.isNotEmpty)
                      Text(
                        item.subtitle,
                        style: TextStyle(
                          fontSize: 11,
                          color: cs.onSurfaceVariant,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                  ],
                ),
              ),
              if (duration > 0)
                Padding(
                  padding: const EdgeInsets.only(left: 8),
                  child: Text(
                    _formatDuration(duration),
                    style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
                  ),
                ),
            ],
          ),
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

// ── 封面组件 ──

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
          child: Icon(
            Icons.music_note_rounded,
            size: 16,
            color: cs.onSurfaceVariant.withAlpha(80),
          ),
        ),
      );
    }
    return Image.network(
      '$baseUrl/api/track/cover?uuid=$uuid',
      fit: BoxFit.cover,
      errorBuilder: (_, __, ___) => Container(
        color: cs.surfaceContainerHighest,
        child: Center(
          child: Icon(
            Icons.broken_image_rounded,
            size: 16,
            color: cs.onSurfaceVariant.withAlpha(80),
          ),
        ),
      ),
      loadingBuilder: (_, child, progress) {
        if (progress == null) return child;
        return Container(
          color: cs.surfaceContainerHighest,
          child: Center(
            child: SizedBox(
              width: 14,
              height: 14,
              child: CircularProgressIndicator(
                value: progress.expectedTotalBytes != null
                    ? progress.cumulativeBytesLoaded /
                          progress.expectedTotalBytes!
                    : null,
                strokeWidth: 1.5,
              ),
            ),
          ),
        );
      },
    );
  }
}
