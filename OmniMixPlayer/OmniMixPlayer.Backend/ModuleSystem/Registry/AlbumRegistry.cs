using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Backend.ModuleSystem.Registry
{
    public class AlbumRegistry : IAlbumRegistry
    {
        private static AlbumRegistry _instance;
        public static AlbumRegistry Instance => _instance;

        private readonly ILogger _logger;
        private readonly Dictionary<string, AlbumInfo> _albums = new Dictionary<string, AlbumInfo>();
        private readonly Dictionary<string, List<string>> _albumsByTag = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> _albumsByModule = new Dictionary<string, List<string>>();
        private readonly object _lock = new object();

        public event Action<AlbumInfo> OnAlbumRegistered;
        public event Action<string> OnAlbumUnregistered;

        public static void Initialize(ILogger logger) { if (_instance != null) { logger.LogWarning("AlbumRegistry already initialized"); return; } _instance = new AlbumRegistry(logger); }
        private AlbumRegistry(ILogger logger) { _logger = logger; }

        public void RegisterAlbum(AlbumInfo album, string moduleId)
        {
            if (album == null) throw new ArgumentNullException(nameof(album));
            if (string.IsNullOrEmpty(album.AlbumId)) throw new ArgumentException("Album ID cannot be empty");

            lock (_lock)
            {
                if (_albums.ContainsKey(album.AlbumId)) UnregisterAlbum(album.AlbumId);
                album.ModuleId = moduleId;
                _albums[album.AlbumId] = album;

                if (album.TagIds != null && album.TagIds.Count > 0)
                {
                    foreach (var tagId in album.TagIds)
                    {
                        if (string.IsNullOrEmpty(tagId)) continue;
                        if (!_albumsByTag.ContainsKey(tagId)) _albumsByTag[tagId] = new List<string>();
                        if (!_albumsByTag[tagId].Contains(album.AlbumId)) _albumsByTag[tagId].Add(album.AlbumId);
                        if (album.IsGrowableAlbum) TagRegistry.Instance?.MarkAsGrowableTag(tagId, album.AlbumId);
                    }
                }

                if (!_albumsByModule.ContainsKey(moduleId)) _albumsByModule[moduleId] = new List<string>();
                _albumsByModule[moduleId].Add(album.AlbumId);
                var tagsInfo = album.TagIds != null && album.TagIds.Count > 0 ? string.Join(", ", album.TagIds) : "none";
                _logger.LogInformation("Registered Album: {Name} (ID: {Id}, Tags: [{Tags}], Module: {Module})", album.DisplayName, album.AlbumId, tagsInfo, moduleId);
                OnAlbumRegistered?.Invoke(album);
            }
        }

        public void UnregisterAlbum(string albumId)
        {
            lock (_lock)
            {
                if (!_albums.TryGetValue(albumId, out var album)) return;
                if (album.TagIds != null) { foreach (var tagId in album.TagIds) { if (!string.IsNullOrEmpty(tagId) && _albumsByTag.TryGetValue(tagId, out var tagAlbums)) tagAlbums.Remove(albumId); } }
                if (!string.IsNullOrEmpty(album.ModuleId) && _albumsByModule.TryGetValue(album.ModuleId, out var moduleAlbums)) moduleAlbums.Remove(albumId);
                _albums.Remove(albumId);
                OnAlbumUnregistered?.Invoke(albumId);
            }
        }

        public AlbumInfo GetAlbum(string albumId) { lock (_lock) { return _albums.TryGetValue(albumId, out var album) ? album : null; } }
        public IReadOnlyList<AlbumInfo> GetAllAlbums() { lock (_lock) { return _albums.Values.OrderBy(a => a.SortOrder).ToList(); } }
        public IReadOnlyList<AlbumInfo> GetAlbumsByTag(string tagId) { lock (_lock) { if (!_albumsByTag.TryGetValue(tagId, out var ids)) return new List<AlbumInfo>(); return ids.Select(id => _albums.TryGetValue(id, out var a) ? a : null).Where(a => a != null).OrderBy(a => a.SortOrder).ToList(); } }
        public IReadOnlyList<AlbumInfo> GetAlbumsByModule(string moduleId) { lock (_lock) { if (!_albumsByModule.TryGetValue(moduleId, out var ids)) return new List<AlbumInfo>(); return ids.Select(id => _albums.TryGetValue(id, out var a) ? a : null).Where(a => a != null).OrderBy(a => a.SortOrder).ToList(); } }
        public bool IsAlbumRegistered(string albumId) { lock (_lock) { return _albums.ContainsKey(albumId); } }
        public void UnregisterAllByModule(string moduleId) { lock (_lock) { if (_albumsByModule.TryGetValue(moduleId, out var ids)) foreach (var albumId in ids.ToList()) UnregisterAlbum(albumId); } }
        public void UpdateSongCount(string albumId, int count) { lock (_lock) { if (_albums.TryGetValue(albumId, out var album)) album.SongCount = count; } }
    }
}
