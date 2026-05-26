using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Backend.ModuleSystem.Registry
{
    public class MusicRegistry : IMusicRegistry
    {
        private static MusicRegistry _instance;
        public static MusicRegistry Instance => _instance;

        private readonly ILogger _logger;
        private readonly Dictionary<string, MusicInfo> _music = new Dictionary<string, MusicInfo>();
        private readonly Dictionary<string, List<string>> _musicByAlbum = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> _musicByTag = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> _musicByModule = new Dictionary<string, List<string>>();
        private readonly object _lock = new object();

        public event Action<MusicInfo> OnMusicRegistered;
        public event Action<string> OnMusicUnregistered;
        public event Action<MusicInfo> OnMusicUpdated;

        public static void Initialize(ILogger logger) { if (_instance != null) { logger.LogWarning("MusicRegistry already initialized"); return; } _instance = new MusicRegistry(logger); }
        private MusicRegistry(ILogger logger) { _logger = logger; }

        public void RegisterMusic(MusicInfo music, string moduleId)
        {
            if (music == null) throw new ArgumentNullException(nameof(music));
            if (string.IsNullOrEmpty(music.UUID)) throw new ArgumentException("Music UUID cannot be empty");

            lock (_lock)
            {
                if (_music.ContainsKey(music.UUID))
                {
                    var existing = _music[music.UUID];
                    if (music.TagIds != null) { foreach (var tagId in music.TagIds) { if (!string.IsNullOrEmpty(tagId) && !existing.TagIds.Contains(tagId)) { existing.TagIds.Add(tagId); if (!_musicByTag.ContainsKey(tagId)) _musicByTag[tagId] = new List<string>(); if (!_musicByTag[tagId].Contains(existing.UUID)) _musicByTag[tagId].Add(existing.UUID); } } }
                    OnMusicRegistered?.Invoke(existing);
                    return;
                }

                music.ModuleId = moduleId;
                _music[music.UUID] = music;

                if (!string.IsNullOrEmpty(music.AlbumId)) { if (!_musicByAlbum.ContainsKey(music.AlbumId)) _musicByAlbum[music.AlbumId] = new List<string>(); _musicByAlbum[music.AlbumId].Add(music.UUID); }
                if (music.TagIds != null && music.TagIds.Count > 0) { foreach (var tagId in music.TagIds) { if (string.IsNullOrEmpty(tagId)) continue; if (!_musicByTag.ContainsKey(tagId)) _musicByTag[tagId] = new List<string>(); _musicByTag[tagId].Add(music.UUID); } }
                if (!_musicByModule.ContainsKey(moduleId)) _musicByModule[moduleId] = new List<string>();
                _musicByModule[moduleId].Add(music.UUID);
                OnMusicRegistered?.Invoke(music);
            }
        }

        public void RegisterMusicBatch(IEnumerable<MusicInfo> musicList, string moduleId)
        {
            if (musicList == null) throw new ArgumentNullException(nameof(musicList));
            int count = 0;
            foreach (var music in musicList) { RegisterMusic(music, moduleId); count++; }
            _logger.LogInformation("Batch registered {Count} songs (module: {Module})", count, moduleId);
        }

        public void UnregisterMusic(string uuid)
        {
            lock (_lock)
            {
                if (!_music.TryGetValue(uuid, out var music)) return;
                if (!string.IsNullOrEmpty(music.AlbumId) && _musicByAlbum.TryGetValue(music.AlbumId, out var am)) am.Remove(uuid);
                if (music.TagIds != null) { foreach (var tagId in music.TagIds) { if (!string.IsNullOrEmpty(tagId) && _musicByTag.TryGetValue(tagId, out var tm)) tm.Remove(uuid); } }
                if (!string.IsNullOrEmpty(music.ModuleId) && _musicByModule.TryGetValue(music.ModuleId, out var mm)) mm.Remove(uuid);
                _music.Remove(uuid);
                OnMusicUnregistered?.Invoke(uuid);
            }
        }

        public MusicInfo GetMusic(string uuid) { lock (_lock) { return _music.TryGetValue(uuid, out var m) ? m : null; } }
        public MusicInfo GetByUUID(string uuid) => GetMusic(uuid);
        public IReadOnlyList<MusicInfo> GetAllMusic() { lock (_lock) { return _music.Values.ToList(); } }
        public IReadOnlyList<MusicInfo> GetMusicByAlbum(string albumId) { lock (_lock) { if (!_musicByAlbum.TryGetValue(albumId, out var uuids)) return new List<MusicInfo>(); return uuids.Select(id => _music.TryGetValue(id, out var m) ? m : null).Where(m => m != null).ToList(); } }
        public IReadOnlyList<MusicInfo> GetMusicByTag(string tagId) { lock (_lock) { if (!_musicByTag.TryGetValue(tagId, out var uuids)) return new List<MusicInfo>(); return uuids.Select(id => _music.TryGetValue(id, out var m) ? m : null).Where(m => m != null).ToList(); } }
        public IReadOnlyList<MusicInfo> GetMusicByModule(string moduleId) { lock (_lock) { if (!_musicByModule.TryGetValue(moduleId, out var uuids)) return new List<MusicInfo>(); return uuids.Select(id => _music.TryGetValue(id, out var m) ? m : null).Where(m => m != null).ToList(); } }
        public bool IsMusicRegistered(string uuid) { lock (_lock) { return _music.ContainsKey(uuid); } }

        public void UpdateMusic(MusicInfo music)
        {
            if (music == null || string.IsNullOrEmpty(music.UUID)) return;
            lock (_lock)
            {
                if (_music.TryGetValue(music.UUID, out var oldMusic))
                {
                    if (oldMusic.AlbumId != music.AlbumId) { if (!string.IsNullOrEmpty(oldMusic.AlbumId) && _musicByAlbum.TryGetValue(oldMusic.AlbumId, out var oldList)) oldList.Remove(music.UUID); if (!string.IsNullOrEmpty(music.AlbumId)) { if (!_musicByAlbum.ContainsKey(music.AlbumId)) _musicByAlbum[music.AlbumId] = new List<string>(); if (!_musicByAlbum[music.AlbumId].Contains(music.UUID)) _musicByAlbum[music.AlbumId].Add(music.UUID); } }
                    _music[music.UUID] = music;
                    OnMusicUpdated?.Invoke(music);
                }
            }
        }

        public void UnregisterAllByModule(string moduleId) { lock (_lock) { if (!_musicByModule.TryGetValue(moduleId, out var uuids)) return; foreach (var uuid in uuids.ToList()) UnregisterMusic(uuid); } }
        public int GetTotalCount() { lock (_lock) { return _music.Count; } }
        public int GetCountByAlbum(string albumId) { lock (_lock) { return _musicByAlbum.TryGetValue(albumId, out var uuids) ? uuids.Count : 0; } }
    }
}
