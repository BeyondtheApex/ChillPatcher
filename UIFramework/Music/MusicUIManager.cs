using System;
using System.Collections.Generic;
using Bulbul;
using ChillPatcher.UIFramework.Core;
using ChillPatcher.ModuleSystem.Registry;
using ChillPatcher.ModuleSystem.Services;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 音乐UI管理器实现
    /// 注意: 歌单和音频加载功能现在由模块系统提供
    /// </summary>
    public class MusicUIManager : IMusicUIManager
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = 
            BepInEx.Logging.Logger.CreateLogSource("MusicUIManager");
            
        private VirtualScrollController _virtualScroll;
        private MixedVirtualScrollController _mixedVirtualScroll;
        private TagDropdownManager _tagDropdown;
        private PlaylistListBuilder _playlistListBuilder;
        
        /// <summary>
        /// 当前显示顺序的歌曲列表（按专辑分组后的歌曲顺序）
        /// 用于队列系统从歌单取歌时使用
        /// </summary>
        private List<GameAudioInfo> _displayOrderSongs = new List<GameAudioInfo>();
        
        /// <summary>
        /// 获取当前显示顺序的歌曲列表
        /// </summary>
        public IReadOnlyList<GameAudioInfo> DisplayOrderSongs => _displayOrderSongs;

        public IVirtualScrollController VirtualScroll => _virtualScroll;
        public MixedVirtualScrollController MixedVirtualScroll => _mixedVirtualScroll;
        
        // 这些功能现在由模块系统提供，保留接口但返回 null
        // 模块应通过 IModuleContext 访问注册表
        public IPlaylistRegistry PlaylistRegistry => null;
        public IAudioLoader AudioLoader => null;
        
        public ITagDropdownManager TagDropdown => _tagDropdown;
        public PlaylistListBuilder PlaylistListBuilder => _playlistListBuilder;
        
        /// <summary>
        /// 更新显示顺序列表
        /// 在 PlaylistListBuilder.BuildWithAlbumHeaders 完成后调用
        /// </summary>
        /// <param name="items">构建完成的播放列表项</param>
        public void UpdateDisplayOrderFromItems(IReadOnlyList<PlaylistListItem> items)
        {
            _displayOrderSongs.Clear();
            
            if (items == null || items.Count == 0)
            {
                Logger.LogDebug("UpdateDisplayOrderFromItems: items is empty");
                return;
            }
            
            foreach (var item in items)
            {
                if (item.ItemType == PlaylistItemType.Song && item.AudioInfo != null)
                {
                    _displayOrderSongs.Add(item.AudioInfo);
                }
            }
            
            Logger.LogInfo($"UpdateDisplayOrderFromItems: cached {_displayOrderSongs.Count} songs in display order");
        }
        
        /// <summary>
        /// 清空显示顺序列表
        /// </summary>
        public void ClearDisplayOrder()
        {
            _displayOrderSongs.Clear();
            Logger.LogDebug("Display order cleared");
        }

        /// <summary>
        /// 初始化音乐管理器
        /// </summary>
        public void Initialize()
        {
            _virtualScroll = new VirtualScrollController();
            _mixedVirtualScroll = new MixedVirtualScrollController();
            _tagDropdown = new TagDropdownManager();
            _playlistListBuilder = new PlaylistListBuilder();

            BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogInfo("MusicUIManager initialized");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _virtualScroll?.Dispose();
            _mixedVirtualScroll?.Dispose();
            _tagDropdown?.Dispose();

            _virtualScroll = null;
            _mixedVirtualScroll = null;
            _tagDropdown = null;
            _playlistListBuilder = null;

            BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogInfo("MusicUIManager cleaned up");
        }
    }
}

