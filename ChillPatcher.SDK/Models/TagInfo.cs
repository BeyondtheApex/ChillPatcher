namespace ChillPatcher.SDK.Models
{
    /// <summary>
    /// Tag 信息模型
    /// </summary>
    public class TagInfo
    {
        /// <summary>
        /// Tag 唯一标识符
        /// </summary>
        public string TagId { get; set; }

        /// <summary>
        /// Tag 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 所属模块 ID
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Tag 的位值 (用于游戏内部的位运算)
        /// </summary>
        public ulong BitValue { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 图标路径 (如果适用)
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Tag 下的专辑数量 (运行时计算)
        /// </summary>
        public int AlbumCount { get; set; }

        /// <summary>
        /// Tag 下的歌曲数量 (运行时计算)
        /// </summary>
        public int SongCount { get; set; }

        /// <summary>
        /// 是否显示在 Tag 列表中
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 扩展数据 (模块自定义使用)
        /// </summary>
        public object ExtendedData { get; set; }
    }
}
