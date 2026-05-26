using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Models;

namespace OmniMixPlayer.Backend.ModuleSystem.Registry
{
    public class TagRegistry : ITagRegistry
    {
        private static TagRegistry _instance;
        public static TagRegistry Instance => _instance;

        private readonly ILogger _logger;
        private readonly Dictionary<string, TagInfo> _tags = new Dictionary<string, TagInfo>();
        private readonly Dictionary<ulong, TagInfo> _tagsByBitValue = new Dictionary<ulong, TagInfo>();
        private readonly object _lock = new object();

        private int _nextBitIndex = 5;
        private const int MAX_BIT_INDEX = 30;

        public event Action<TagInfo> OnTagRegistered;
        public event Action<string> OnTagUnregistered;

        public static void Initialize(ILogger logger)
        {
            if (_instance != null) { logger.LogWarning("TagRegistry already initialized"); return; }
            _instance = new TagRegistry(logger);
        }

        private TagRegistry(ILogger logger) { _logger = logger; }

        public TagInfo RegisterTag(string tagId, string displayName, string moduleId)
        {
            if (string.IsNullOrEmpty(tagId)) throw new ArgumentException("Tag ID cannot be empty", nameof(tagId));
            lock (_lock)
            {
                if (_tags.ContainsKey(tagId)) { _logger.LogWarning("Tag {TagId} already exists, returning existing", tagId); return _tags[tagId]; }
                if (_nextBitIndex > MAX_BIT_INDEX) throw new InvalidOperationException($"Tag limit reached ({MAX_BIT_INDEX - 4} custom tags)");

                var bitValue = 1UL << _nextBitIndex;
                _nextBitIndex++;
                var tagInfo = new TagInfo { TagId = tagId, DisplayName = displayName, ModuleId = moduleId, BitValue = bitValue, SortOrder = _tags.Count };
                _tags[tagId] = tagInfo;
                _tagsByBitValue[bitValue] = tagInfo;
                _logger.LogInformation("Registered Tag: {Name} (ID: {Id}, Bit: {Bit})", displayName, tagId, bitValue);
                OnTagRegistered?.Invoke(tagInfo);
                return tagInfo;
            }
        }

        public void SetLoadMoreCallback(string tagId, Func<Task<int>> loadMoreCallback)
        {
            lock (_lock) { if (_tags.TryGetValue(tagId, out var tag)) { tag.LoadMoreCallback = loadMoreCallback; _logger.LogInformation("Set LoadMore callback for Tag {TagId}", tagId); } }
        }

        public void MarkAsGrowableTag(string tagId, string growableAlbumId)
        {
            lock (_lock)
            {
                if (!_tags.TryGetValue(tagId, out var tag)) { _logger.LogWarning("Tag {TagId} not found for MarkAsGrowableTag", tagId); return; }
                if (tag.IsGrowableList && !string.IsNullOrEmpty(tag.GrowableAlbumId) && tag.GrowableAlbumId != growableAlbumId)
                    throw new InvalidOperationException($"Tag {tagId} already has growable album {tag.GrowableAlbumId}");
                tag.IsGrowableList = true;
                tag.GrowableAlbumId = growableAlbumId;
                if (tag.SortOrder < 1000) tag.SortOrder += 1000;
                _logger.LogInformation("Tag {TagId} marked as growable (album: {AlbumId})", tagId, growableAlbumId);
            }
        }

        public void UnregisterTag(string tagId)
        {
            lock (_lock) { if (_tags.TryGetValue(tagId, out var tag)) { _tags.Remove(tagId); _tagsByBitValue.Remove(tag.BitValue); _logger.LogInformation("Unregistered Tag: {Name} ({Id})", tag.DisplayName, tagId); OnTagUnregistered?.Invoke(tagId); } }
        }

        public TagInfo GetTag(string tagId) { lock (_lock) { return _tags.TryGetValue(tagId, out var tag) ? tag : null; } }
        public IReadOnlyList<TagInfo> GetAllTags() { lock (_lock) { return _tags.Values.OrderBy(t => t.SortOrder).ToList(); } }
        public IReadOnlyList<TagInfo> GetTagsByModule(string moduleId) { lock (_lock) { return _tags.Values.Where(t => t.ModuleId == moduleId).OrderBy(t => t.SortOrder).ToList(); } }
        public bool IsTagRegistered(string tagId) { lock (_lock) { return _tags.ContainsKey(tagId); } }
        public TagInfo GetTagByBitValue(ulong bitValue) { lock (_lock) { return _tagsByBitValue.TryGetValue(bitValue, out var tag) ? tag : null; } }

        public void UnregisterAllByModule(string moduleId)
        {
            lock (_lock) { foreach (var tagId in _tags.Values.Where(t => t.ModuleId == moduleId).Select(t => t.TagId).ToList()) UnregisterTag(tagId); }
        }

        private string _currentGrowableTagId;

        public TagInfo GetCurrentGrowableTag()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_currentGrowableTagId)) return null;
                return _tags.TryGetValue(_currentGrowableTagId, out var tag) ? tag : null;
            }
        }

        public void SetCurrentGrowableTag(string tagId)
        {
            lock (_lock) { _currentGrowableTagId = tagId; }
            _logger.LogInformation("Current growable tag set to: {TagId}", tagId ?? "null");
        }

        public IReadOnlyList<TagInfo> GetGrowableTags()
        {
            lock (_lock) { return _tags.Values.Where(t => t.IsGrowableList).OrderBy(t => t.SortOrder).ToList(); }
        }
    }
}
