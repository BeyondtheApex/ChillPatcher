namespace ChillPatcher.SDK.Models
{
    /// <summary>
    /// 专辑信息模型
    /// </summary>
    public class AlbumInfo
    {
        /// <summary>
        /// 专辑唯一标识符
        /// </summary>
        public string AlbumId { get; set; }

        /// <summary>
        /// 专辑显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 专辑艺术家
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// 所属 Tag ID
        /// </summary>
        public string TagId { get; set; }

        /// <summary>
        /// 所属模块 ID
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// 专辑目录路径 (如果适用)
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// 封面图片路径 (如果适用)
        /// </summary>
        public string CoverPath { get; set; }

        /// <summary>
        /// 专辑中的歌曲数量 (运行时计算)
        /// </summary>
        public int SongCount { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 是否是默认专辑 (无专辑归类)
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 扩展数据 (模块自定义使用)
        /// </summary>
        public object ExtendedData { get; set; }
    }
}
