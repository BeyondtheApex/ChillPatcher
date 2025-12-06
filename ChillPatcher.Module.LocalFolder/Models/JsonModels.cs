using Newtonsoft.Json;

namespace ChillPatcher.Module.LocalFolder.Models
{
    /// <summary>
    /// playlist.json 的数据模型
    /// 仅用于用户自定义显示名称
    /// </summary>
    public class PlaylistJsonModel
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// album.json 的数据模型
    /// 仅用于用户自定义专辑显示名称
    /// </summary>
    public class AlbumJsonModel
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }
    }
}
