using System;
using System.Collections.Generic;

namespace ChillPatcher.UIFramework.Config
{
    /// <summary>
    /// 音乐库配置
    /// </summary>
    [Serializable]
    public class MusicLibraryConfig
    {
        /// <summary>
        /// 音乐文件夹列表
        /// </summary>
        public List<LibraryFolder> Folders { get; set; } = new List<LibraryFolder>();

        /// <summary>
        /// 是否自动扫描
        /// </summary>
        public bool AutoScan { get; set; } = true;
    }

    /// <summary>
    /// 音乐库文件夹
    /// </summary>
    [Serializable]
    public class LibraryFolder
    {
        /// <summary>
        /// 文件夹路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 歌单名称
        /// </summary>
        public string PlaylistName { get; set; }

        /// <summary>
        /// 是否递归扫描子目录
        /// </summary>
        public bool Recursive { get; set; } = false;

        /// <summary>
        /// 是否自动监视文件变化
        /// </summary>
        public bool AutoWatch { get; set; } = true;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// 歌单元数据
    /// </summary>
    [Serializable]
    public class PlaylistMetadata
    {
        /// <summary>
        /// 歌单唯一ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 歌单名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 歌单描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 图标路径（相对或绝对）
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// 歌曲元数据（文件名 -> 元数据）
        /// </summary>
        public Dictionary<string, SongMetadata> Songs { get; set; }
    }

    /// <summary>
    /// 歌曲元数据
    /// </summary>
    [Serializable]
    public class SongMetadata
    {
        /// <summary>
        /// 歌曲标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 艺术家
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// 专辑
        /// </summary>
        public string Album { get; set; }

        /// <summary>
        /// 年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 流派
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// 自定义标签
        /// </summary>
        public List<string> CustomTags { get; set; }
    }
}
