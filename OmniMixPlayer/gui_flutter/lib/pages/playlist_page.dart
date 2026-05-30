import 'package:flutter/material.dart';
import 'package:omnimix_gui/l10n/app_localizations.dart';
import '../providers/app_state.dart';
import '../models/node_data.dart';

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
  final AlbumInfo album;
  List<SongInfo> songs;
  bool expanded;

  _AlbumNode(this.album, {this.songs = const [], this.expanded = false});
}

class _TagNode {
  final TagInfo tag;
  final List<_AlbumNode> albums;
  bool expanded;

  _TagNode(this.tag, this.albums, {this.expanded = false});
}

// ── 扁平列表条目（供 ListView.builder 虚拟滚动） ──

enum _ItemKind { tag, album, song }

class _FlatItem {
  final _ItemKind kind;
  final String label;
  final String subtitle;
  final int indentLevel;
  final bool expandable;
  final bool expanded;
  // 数据引用
  final TagInfo? tag;
  final AlbumInfo? album;
  final SongInfo? song;

  const _FlatItem({
    required this.kind,
    required this.label,
    this.subtitle = '',
    required this.indentLevel,
    this.expandable = false,
    this.expanded = false,
    this.tag,
    this.album,
    this.song,
  });
}

// ── 页面状态 ──

class _PlaylistPageState extends State<PlaylistPage> {
  final List<_TagNode> _tree = [];
  final List<_FlatItem> _flatItems = [];
  bool _loading = false;
  String _error = '';
  int _totalSongs = 0;

  @override
  void initState() {
    super.initState();
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
    if (widget.state.backendOnline && _tree.isEmpty && !_loading) {
      _loadTree();
    }
    setState(() {});
  }

  // ── 数据加载：单次 GET /api/playlist 获取全部标签+专辑+曲目 ──

  Future<void> _loadTree() async {
    if (!widget.state.backendOnline) return;
    setState(() {
      _loading = true;
      _error = '';
    });
    try {
      final playlist = await widget.state.api.getPlaylist();
      if (!mounted) return;

      final tags = playlist.tags;
      final allAlbums = playlist.albums;
      final allSongs = playlist.songs;

      _tree.clear();
      var total = 0;
      for (final tag in tags) {
        final tagAlbums = allAlbums.where((a) => a.tagId == tag.id).toList();
        final albumNodes = <_AlbumNode>[];
        for (final album in tagAlbums) {
          final albumSongs = allSongs
              .where((s) => s.albumId == album.id)
              .toList();
          albumNodes.add(_AlbumNode(album, songs: albumSongs));
          total += albumSongs.length;
        }
        _tree.add(_TagNode(tag, albumNodes));
      }
      _totalSongs = total;
      _rebuildFlatList();
    } catch (e) {
      if (mounted) {
        final l10n = context.mounted ? AppLocalizations.of(context) : null;
        _error = '${l10n?.loadLibraryFailed(e.toString()) ?? '加载曲库失败: $e'}';
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  // ── 根据展开状态重建扁平列表（虚拟滚动数据源） ──

  void _rebuildFlatList() {
    _flatItems.clear();
    for (final tagNode in _tree) {
      _flatItems.add(
        _FlatItem(
          kind: _ItemKind.tag,
          label: tagNode.tag.name,
          subtitle: '${tagNode.tag.moduleId} · ${tagNode.albums.length} albums',
          indentLevel: 0,
          expandable: tagNode.albums.isNotEmpty,
          expanded: tagNode.expanded,
          tag: tagNode.tag,
        ),
      );
      if (!tagNode.expanded) continue;

      for (final albumNode in tagNode.albums) {
        _flatItems.add(
          _FlatItem(
            kind: _ItemKind.album,
            label: albumNode.album.name,
            subtitle: '${albumNode.songs.length} 首',
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
    // 用 notifyListeners 等效触发重建 — setState 会触发 build
    setState(() {});
  }

  // ── 展开/折叠 ──

  void _toggleTag(int flatIndex) {
    final item = _flatItems[flatIndex];
    if (item.tag == null) return;
    for (final t in _tree) {
      if (t.tag.id == item.tag!.id) {
        t.expanded = !t.expanded;
        _rebuildFlatList();
        return;
      }
    }
  }

  void _toggleAlbum(int flatIndex) {
    final item = _flatItems[flatIndex];
    if (item.album == null) return;
    for (final tagNode in _tree) {
      for (final albumNode in tagNode.albums) {
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
      case _ItemKind.tag:
        return _buildTagRow(item, index, cs);
      case _ItemKind.album:
        return _buildAlbumRow(item, index, cs);
      case _ItemKind.song:
        return _buildSongRow(item, cs);
    }
  }

  // ── 歌单行（一级） ──

  Widget _buildTagRow(_FlatItem item, int index, ColorScheme cs) {
    return InkWell(
      onTap: () => _toggleTag(index),
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
                Icons.folder_rounded,
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
              if (item.tag != null) ...[
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
                    '${_tree.firstWhere((t) => t.tag.id == item.tag!.id).albums.length}',
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
