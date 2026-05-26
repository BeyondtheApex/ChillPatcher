import 'package:flutter/material.dart';
import '../providers/app_state.dart';
import '../models/node_data.dart';

/// Minimal playlist browser — just verify data flows end-to-end.
/// Three levels: Tags → Albums → Songs
class PlaylistPage extends StatefulWidget {
  final AppState state;

  const PlaylistPage({super.key, required this.state});

  @override
  State<PlaylistPage> createState() => _PlaylistPageState();
}

class _PlaylistPageState extends State<PlaylistPage> {
  List<TagInfo>? _tags;
  List<AlbumInfo>? _albums;
  List<SongInfo>? _songs;
  String? _selectedTagId;
  String? _selectedAlbumId;
  bool _loading = false;
  String _error = '';

  @override
  void initState() {
    super.initState();
    _loadTags();
  }

  Future<void> _loadTags() async {
    setState(() {
      _loading = true;
      _error = '';
    });
    try {
      _tags = await widget.state.api.getTags();
    } catch (e) {
      _error = 'Failed to load tags: $e';
    }
    setState(() => _loading = false);
  }

  Future<void> _loadAlbums(String tagId) async {
    setState(() {
      _selectedTagId = tagId;
      _selectedAlbumId = null;
      _songs = null;
      _loading = true;
    });
    try {
      _albums = await widget.state.api.getAlbums(tagId: tagId);
    } catch (e) {
      _error = 'Failed to load albums: $e';
    }
    setState(() => _loading = false);
  }

  Future<void> _loadSongs(String albumId) async {
    setState(() {
      _selectedAlbumId = albumId;
      _loading = true;
    });
    try {
      _songs = await widget.state.api.getSongs(albumId: albumId);
    } catch (e) {
      _error = 'Failed to load songs: $e';
    }
    setState(() => _loading = false);
  }

  Future<void> _playSong(SongInfo song) async {
    try {
      await widget.state.api.play(uuid: song.uuid);
    } catch (e) {
      setState(() => _error = 'Play failed: $e');
    }
  }

  // ── Build ──

  @override
  Widget build(BuildContext context) {
    // Show songs if an album is selected
    if (_selectedAlbumId != null) {
      return _buildSongsView();
    }
    // Show albums if a tag is selected
    if (_selectedTagId != null) {
      return _buildAlbumsView();
    }
    // Default: show tags
    return _buildTagsView();
  }

  // ── Tags ──

  Widget _buildTagsView() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionHeader('歌单分类'),
        if (_loading)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(32),
              child: CircularProgressIndicator(),
            ),
          )
        else if (_error.isNotEmpty)
          _errorWidget()
        else if (_tags == null || _tags!.isEmpty)
          _emptyWidget('没有歌单标签 — 请先登录模块')
        else
          Expanded(
            child: ListView.builder(
              itemCount: _tags!.length,
              itemBuilder: (_, i) {
                final t = _tags![i];
                return ListTile(
                  leading: const Icon(Icons.folder),
                  title: Text(t.name),
                  subtitle: Text(
                    t.moduleId,
                    style: const TextStyle(fontSize: 11),
                  ),
                  trailing: t.isGrowable
                      ? const Icon(Icons.auto_awesome, size: 16)
                      : const Icon(Icons.chevron_right),
                  onTap: () => _loadAlbums(t.id),
                );
              },
            ),
          ),
      ],
    );
  }

  // ── Albums ──

  Widget _buildAlbumsView() {
    final tagName =
        _tags?.firstWhere((t) => t.id == _selectedTagId).name ??
        _selectedTagId ??
        '';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _backHeader(
          tagName,
          onBack: () {
            setState(() {
              _selectedTagId = null;
              _albums = null;
            });
          },
        ),
        if (_loading)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(32),
              child: CircularProgressIndicator(),
            ),
          )
        else if (_error.isNotEmpty)
          _errorWidget()
        else if (_albums == null || _albums!.isEmpty)
          _emptyWidget('此分类下没有专辑')
        else
          Expanded(
            child: ListView.builder(
              itemCount: _albums!.length,
              itemBuilder: (_, i) {
                final a = _albums![i];
                return ListTile(
                  leading: const Icon(Icons.album),
                  title: Text(a.name),
                  subtitle: Text(
                    '${a.songCount} 首',
                    style: const TextStyle(fontSize: 11),
                  ),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _loadSongs(a.id),
                );
              },
            ),
          ),
      ],
    );
  }

  // ── Songs ──

  Widget _buildSongsView() {
    final albumName =
        _albums?.firstWhere((a) => a.id == _selectedAlbumId).name ??
        _selectedAlbumId ??
        '';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _backHeader(
          albumName,
          onBack: () {
            setState(() {
              _selectedAlbumId = null;
              _songs = null;
            });
          },
        ),
        if (_loading)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(32),
              child: CircularProgressIndicator(),
            ),
          )
        else if (_error.isNotEmpty)
          _errorWidget()
        else if (_songs == null || _songs!.isEmpty)
          _emptyWidget('此专辑下没有歌曲')
        else
          Expanded(
            child: ListView.builder(
              itemCount: _songs!.length,
              itemBuilder: (_, i) {
                final s = _songs![i];
                return ListTile(
                  leading: const Icon(Icons.music_note),
                  title: Text(s.title),
                  subtitle: Text(
                    s.artist,
                    style: const TextStyle(fontSize: 11),
                  ),
                  trailing: Text(
                    _formatDuration(s.duration),
                    style: const TextStyle(fontSize: 11, color: Colors.grey),
                  ),
                  onTap: () => _playSong(s),
                );
              },
            ),
          ),
      ],
    );
  }

  // ── Helpers ──

  Widget _sectionHeader(String title) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
      child: Text(
        title,
        style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w400),
      ),
    );
  }

  Widget _backHeader(String title, {required VoidCallback onBack}) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 8, 24, 8),
      child: Row(
        children: [
          IconButton(icon: const Icon(Icons.arrow_back), onPressed: onBack),
          Text(
            title,
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w400),
          ),
        ],
      ),
    );
  }

  Widget _errorWidget() {
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        children: [
          const Icon(Icons.error_outline, size: 48, color: Colors.red),
          const SizedBox(height: 8),
          Text(_error, style: const TextStyle(color: Colors.red)),
          const SizedBox(height: 16),
          ElevatedButton(onPressed: _loadTags, child: const Text('重试')),
        ],
      ),
    );
  }

  Widget _emptyWidget(String msg) {
    return Expanded(
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Text(
            msg,
            style: TextStyle(color: Colors.grey.shade500, fontSize: 14),
          ),
        ),
      ),
    );
  }

  String _formatDuration(double seconds) {
    final m = (seconds ~/ 60).toString().padLeft(2, '0');
    final s = (seconds % 60).toInt().toString().padLeft(2, '0');
    return '$m:$s';
  }
}
