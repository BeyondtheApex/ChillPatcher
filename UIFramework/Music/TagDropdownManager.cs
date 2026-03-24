using System;
using System.Collections.Generic;
using System.Linq;
using Bulbul;
using ChillPatcher.UIFramework.Core;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 标签下拉菜单管理器实现
    /// </summary>
    public class TagDropdownManager : ITagDropdownManager, IDisposable
    {
        private readonly Dictionary<AudioTag, TagDropdownItem> _customTags;
        private bool _disposed = false;

        public event Action<AudioTag> OnTagSelected;

        public TagDropdownManager()
        {
            _customTags = new Dictionary<AudioTag, TagDropdownItem>();
        }

        public void AddCustomTag(AudioTag tag, TagDropdownItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_customTags.TryGetValue(tag, out var existing)
                && existing.DisplayName == item.DisplayName
                && existing.Priority == item.Priority
                && existing.ShowInDropdown == item.ShowInDropdown)
            {
                return; // 数据未变，跳过
            }

            _customTags[tag] = item;
            BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogInfo($"Added custom tag: {item.DisplayName} (Tag: {tag})");
        }

        public void RemoveCustomTag(AudioTag tag)
        {
            if (_customTags.Remove(tag))
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogInfo($"Removed custom tag: {tag}");
            }
        }

        public IReadOnlyList<TagDropdownItem> GetAllTags()
        {
            return _customTags.Values
                .Where(t => t.ShowInDropdown)
                .OrderBy(t => t.Priority)
                .ToList();
        }

        internal void RaiseTagSelected(AudioTag tag)
        {
            OnTagSelected?.Invoke(tag);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _customTags.Clear();
            _disposed = true;
        }
    }
}

