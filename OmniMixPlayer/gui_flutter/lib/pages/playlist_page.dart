import 'package:flutter/material.dart';
import '../providers/app_state.dart';
import '../models/node_data.dart';

/// 歌单页面 — 显示当前活跃歌单内容（从已激活的播放源汇总的歌曲），
/// 并允许浏览标签/专辑并添加到歌单。
/// 实时响应 AppState 变化。
class PlaylistPage extends StatefulWidget {
  final AppState state;

  const PlaylistPage({super.key, required this.state});

  @override
  State<PlaylistPage> createState() => _PlaylistPageState();
}

class _PlaylistPageState extends State<PlaylistPage> {
  List<TagInfo> _tags = [];
  List<AlbumInfo> _albums = [];
  bool _loadingTags = false;
  String _error = '';

  @override
  void initState() {
    super.initState();
    widget.state.addListener(_onStateChanged);
    if (widget.state.backendOnline) _loadTags();
  }

  @override
  void dispose() {
    widget.state.removeListener(_onStateChanged);
    super.dispose();
  }

  void _onStateChanged() {
    if (!mounted) return;
    setState(() {});
  }

  Future<void> _loadTags() async {
    setState(() {
      _loadingTags = true;
      _error = '';
    });
    try {
      _tags = await widget.state.api.getTags();
    } catch (e) {
      _error = '加载标签失败: $e';
    }
    setState(() => _loadingTags = false);
  }

  Future<void> _loadAlbums(String tagId) async {
    setState(() {
      _error = '';
    });
    try {
      _albums = await widget.state.api.getAlbums(tagId: tagId);
    } catch (e) {
      _error = '加载专辑失败: $e';
    }
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final sources = widget.state.playlistSources;
    final currentSourceName = sources.isNotEmpty ? sources.first.name : '全部';
    final playlist = widget.state.activePlaylist;
    final canControl = widget.state.canControlActiveInstance;
    final busy = widget.state.backendBusy || widget.state.serviceBusy;

    if (busy) {
      return const Center(child: CircularProgressIndicator());
    }

    if (!widget.state.backendOnline) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.cloud_off_rounded, size: 48, color: cs.onSurfaceVariant.withAlpha(100)),
            const SizedBox(height: 12),
            Text('后端未连接', style: TextStyle(color: cs.onSurfaceVariant)),
          ],
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // ── 当前歌单标题 ──
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
          child: Row(
            children: [
              const Icon(Icons.queue_music_rounded, size: 28),
              const SizedBox(width: 12),
              Text(
                currentSourceName,
                style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
              ),
              const Spacer(),
              if (playlist.isNotEmpty)
                Text('${playlist.length} 首',
                    style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant)),
              const SizedBox(width: 12),
              if (canControl)
                PopupMenuButton<String>(
                  icon: Icon(Icons.add_rounded, color: cs.primary),
                  tooltip: '从曲库添加',
                  onSelected: (tagId) async {
                    final tag = _tags.firstWhere((t) => t.id == tagId);
                    await widget.state.addTagToActivePlaylist(tag);
                  },
                  itemBuilder: (_) => _tags.map((t) => PopupMenuItem(
                    value: t.id,
                    child: ListTile(
                      dense: true,
                      leading: Icon(Icons.folder_rounded, color: cs.primary, size: 20),
                      title: Text(t.name, style: const TextStyle(fontSize: 13)),
                      subtitle: Text(t.moduleId, style: const TextStyle(fontSize: 11)),
                      contentPadding: EdgeInsets.zero,
                    ),
                  )).toList(),
                ),
            ],
          ),
        ),
        const Divider(height: 1),

        // ── 曲库标签（快捷切换） ──
        if (_tags.isNotEmpty)
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
              children: [
                _filterChip('全部', null, cs, () {
                  final inst = widget.state.activeInstance;
                  if (inst == null) return;
                  widget.state.replacePlaylistSources(
                    inst.id,
                    sources: [],
                  );
                }),
                const SizedBox(width: 8),
                ..._tags.map((t) => _filterChip(t.name, t.id, cs, () {
                  widget.state.addTagToActivePlaylist(t);
                })),
              ],
            ),
          ),

        // ── 错误 ──
        if (_error.isNotEmpty)
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Icon(Icons.error_outline, size: 16, color: cs.error),
                const SizedBox(width: 8),
                Expanded(child: Text(_error, style: TextStyle(color: cs.error, fontSize: 12))),
              ],
            ),
          ),

        // ── 歌单内容 ──
        Expanded(
          child: playlist.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.playlist_remove_rounded, size: 48,
                          color: cs.onSurfaceVariant.withAlpha(80)),
                      const SizedBox(height: 12),
                      Text('歌单为空',
                          style: TextStyle(color: cs.onSurfaceVariant.withAlpha(150), fontSize: 14)),
                      const SizedBox(height: 8),
                      Text('点击上方标签添加快捷歌单',
                          style: TextStyle(color: cs.onSurfaceVariant.withAlpha(100), fontSize: 12)),
                    ],
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  itemCount: playlist.length,
                  separatorBuilder: (_, __) => Divider(height: 1, indent: 48,
                      color: cs.outlineVariant.withAlpha(60)),
                  itemBuilder: (_, i) {
                    final s = playlist[i];
                    return _SongTile(
                      song: s,
                      baseUrl: widget.state.apiBaseUrl,
                      canControl: canControl,
                      onPlay: canControl ? () => widget.state.playSongOnActive(s.uuid) : null,
                      onAdd: canControl ? () => widget.state.addSongToActiveQueue(s.uuid) : null,
                    );
                  },
                ),
        ),
      ],
    );
  }

  Widget _filterChip(String label, String? id, ColorScheme cs, VoidCallback onTap) {
    final isActive = id == null
        ? widget.state.playlistSources.isEmpty || widget.state.playlistSources.every((s) => s.id == 'all')
        : widget.state.playlistSources.any((s) => s.id == 'tag_$id');
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: FilterChip(
        label: Text(label, style: const TextStyle(fontSize: 12)),
        selected: isActive,
        onSelected: (_) => onTap(),
        visualDensity: VisualDensity.compact,
        selectedColor: cs.primaryContainer,
        checkmarkColor: cs.onPrimaryContainer,
        labelStyle: TextStyle(
          color: isActive ? cs.onPrimaryContainer : cs.onSurface,
          fontWeight: isActive ? FontWeight.w600 : FontWeight.normal,
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
//  Reusable song tile (copied from home_page to avoid import coupling)
// ═══════════════════════════════════════════════════════════

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
          child: Icon(Icons.music_note_rounded, size: 20, color: cs.onSurfaceVariant.withAlpha(80)),
        ),
      );
    }
    return Image.network(
      '$baseUrl/api/cover/$uuid',
      fit: BoxFit.cover,
      errorBuilder: (_, __, ___) => Container(
        color: cs.surfaceContainerHighest,
        child: Center(
          child: Icon(Icons.broken_image_rounded, size: 20, color: cs.onSurfaceVariant.withAlpha(80)),
        ),
      ),
      loadingBuilder: (_, child, progress) {
        if (progress == null) return child;
        return Container(
          color: cs.surfaceContainerHighest,
          child: Center(
            child: SizedBox(
              width: 16, height: 16,
              child: CircularProgressIndicator(
                value: progress.expectedTotalBytes != null
                    ? progress.cumulativeBytesLoaded / progress.expectedTotalBytes!
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
