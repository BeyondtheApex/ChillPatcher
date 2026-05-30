using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bulbul;
using ObservableCollections;
using UnityEngine;

namespace ChillPatcher.UIFramework.Core
{
    #region Core Interfaces

    /// <summary>
    /// 音乐UI管理器
    /// </summary>
    public interface IMusicUIManager
    {
        /// <summary>
        /// 虚拟滚动控制器
        /// </summary>
        IVirtualScrollController VirtualScroll { get; }

        /// <summary>
        /// 歌单注册表
        /// </summary>
        IPlaylistRegistry PlaylistRegistry { get; }

        /// <summary>
        /// 音频加载器
        /// </summary>
        IAudioLoader AudioLoader { get; }

        /// <summary>
        /// 标签下拉菜单管理器
        /// </summary>
        ITagDropdownManager TagDropdown { get; }
    }

    #endregion

    #region Virtual Scroll

    /// <summary>
    /// 虚拟滚动控制器
    /// </summary>
    public interface IVirtualScrollController
    {
        /// <summary>
        /// 可见范围起始索引
        /// </summary>
        int VisibleStartIndex { get; }

        /// <summary>
        /// 可见范围结束索引
        /// </summary>
        int VisibleEndIndex { get; }

        /// <summary>
        /// 总项目数
        /// </summary>
        int TotalItemCount { get; }

        /// <summary>
        /// 滚动百分比 (0-1)
        /// </summary>
        float ScrollPercentage { get; }

        /// <summary>
        /// 项目高度（像素）
        /// </summary>
        float ItemHeight { get; set; }

        /// <summary>
        /// 缓冲区项目数量
        /// </summary>
        int BufferCount { get; set; }

        /// <summary>
        /// 设置数据源
        /// </summary>
        void SetDataSource(ObservableCollections.IReadOnlyObservableList<GameAudioInfo> source);

        /// <summary>
        /// 刷新可见项
        /// </summary>
        void RefreshVisible();

        /// <summary>
        /// 刷新指定项
        /// </summary>
        void RefreshItem(int index);

        /// <summary>
        /// 滚动到指定项
        /// </summary>
        void ScrollToItem(int index, bool smooth = true);

        /// <summary>
        /// 可见范围变化事件
        /// </summary>
        event Action<int, int> OnVisibleRangeChanged;

        /// <summary>
        /// 滚动到底部事件
        /// </summary>
        event Action OnScrollToBottom;
    }

    /// <summary>
    /// UI对象池
    /// </summary>
    public interface IUIObjectPool<T> where T : Component
    {
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        T Get();

        /// <summary>
        /// 返回对象到池
        /// </summary>
        void Return(T item);

        /// <summary>
        /// 清空池
        /// </summary>
        void Clear();

        /// <summary>
        /// 活动对象数量
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// 池中对象数量
        /// </summary>
        int PooledCount { get; }
    }

    #endregion

    #region Playlist System

    /// <summary>
    /// 歌单注册表
    /// </summary>
    public interface IPlaylistRegistry
    {
        /// <summary>
        /// 注册歌单提供器
        /// </summary>
        void RegisterPlaylist(string id, IPlaylistProvider provider);

        /// <summary>
        /// 取消注册歌单
        /// </summary>
        void UnregisterPlaylist(string id);

        /// <summary>
        /// 获取歌单提供器
        /// </summary>
        IPlaylistProvider GetPlaylist(string id);

        /// <summary>
        /// 获取所有歌单
        /// </summary>
        IReadOnlyDictionary<string, IPlaylistProvider> GetAllPlaylists();

        /// <summary>
        /// 刷新指定歌单
        /// </summary>
        void RefreshPlaylist(string id);

        /// <summary>
        /// 刷新所有歌单
        /// </summary>
        void RefreshAll();

        /// <summary>
        /// 歌单变化事件
        /// </summary>
        event Action<PlaylistChangedEventArgs> OnPlaylistChanged;
    }

    /// <summary>
    /// 歌单提供器
    /// </summary>
    public interface IPlaylistProvider
    {
        /// <summary>
        /// 歌单唯一ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 图标
        /// </summary>
        Sprite Icon { get; }

        /// <summary>
        /// 关联的音频标签
        /// </summary>
        AudioTag Tag { get; }

        /// <summary>
        /// 构建歌单
        /// </summary>
        Task<List<GameAudioInfo>> BuildPlaylist();

        /// <summary>
        /// 是否支持实时更新
        /// </summary>
        bool SupportsLiveUpdate { get; }

        /// <summary>
        /// 歌单更新事件
        /// </summary>
        event Action OnPlaylistUpdated;
    }

    /// <summary>
    /// 歌单变化事件参数
    /// </summary>
    public class PlaylistChangedEventArgs : EventArgs
    {
        public string PlaylistId { get; set; }
        public PlaylistChangeType ChangeType { get; set; }
        public IPlaylistProvider Provider { get; set; }
    }

    public enum PlaylistChangeType
    {
        Added,
        Removed,
        Updated
    }

    #endregion

    #region Audio Loading

    /// <summary>
    /// 音频加载器
    /// </summary>
    public interface IAudioLoader
    {
        /// <summary>
        /// 从文件加载音频
        /// </summary>
        Task<GameAudioInfo> LoadFromFile(string filePath);

        /// <summary>
        /// 从文件加载音频（指定UUID）
        /// </summary>
        Task<GameAudioInfo> LoadFromFile(string filePath, string uuid);

        /// <summary>
        /// 批量加载音频
        /// </summary>
        Task<List<GameAudioInfo>> LoadFromDirectory(string directoryPath, bool recursive = false);

        /// <summary>
        /// 卸载音频
        /// </summary>
        void UnloadAudio(GameAudioInfo audio);

        /// <summary>
        /// 检查格式是否支持
        /// </summary>
        bool IsSupportedFormat(string filePath);

        /// <summary>
        /// 支持的音频格式
        /// </summary>
        IReadOnlyList<string> SupportedFormats { get; }
    }

    #endregion

    #region Tag Dropdown

    /// <summary>
    /// 标签下拉菜单管理器
    /// </summary>
    public interface ITagDropdownManager
    {
        /// <summary>
        /// 添加自定义标签
        /// </summary>
        void AddCustomTag(AudioTag tag, TagDropdownItem item);

        /// <summary>
        /// 移除自定义标签
        /// </summary>
        void RemoveCustomTag(AudioTag tag);

        /// <summary>
        /// 获取所有标签
        /// </summary>
        IReadOnlyList<TagDropdownItem> GetAllTags();

        /// <summary>
        /// 标签选择事件
        /// </summary>
        event Action<AudioTag> OnTagSelected;
    }

    /// <summary>
    /// 标签下拉菜单项
    /// </summary>
    public class TagDropdownItem
    {
        /// <summary>
        /// 标签值
        /// </summary>
        public AudioTag Tag { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        /// 优先级（越小越靠前）
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// 是否在下拉菜单中显示
        /// </summary>
        public bool ShowInDropdown { get; set; } = true;
    }

    #endregion
}
