using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OmniMixPlayer.Module.Bilibili
{
    [Serializable]
    public class BilibiliSession
    {
        public string SESSDATA;
        public string BiliJct;
        public string DedeUserID;
        public long LoginTime;

        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(SESSDATA) && !string.IsNullOrEmpty(DedeUserID);

        public string ToCookieString() => $"SESSDATA={SESSDATA}; bili_jct={BiliJct}; DedeUserID={DedeUserID};";
    }

    public class BiliVideoInfo
    {
        public string Bvid { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string CoverUrl { get; set; }
        public float Duration { get; set; }
    }

    public class BiliFolder
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("media_count")] public int MediaCount { get; set; }
    }

    public class BiliFolderListResult
    {
        public bool Success { get; set; }
        public List<BiliFolder> Folders { get; set; } = new();
        public string ErrorMessage { get; set; }

        public static BiliFolderListResult Ok(List<BiliFolder> folders) => new()
        {
            Success = true,
            Folders = folders ?? new List<BiliFolder>()
        };

        public static BiliFolderListResult Failed(string message = null) => new()
        {
            Success = false,
            ErrorMessage = message ?? ""
        };
    }

    public class BiliFolderVideosResult
    {
        public bool Success { get; set; }
        public List<BiliVideoInfo> Videos { get; set; } = new();
        public string ErrorMessage { get; set; }

        public static BiliFolderVideosResult Ok(List<BiliVideoInfo> videos) => new()
        {
            Success = true,
            Videos = videos ?? new List<BiliVideoInfo>()
        };

        public static BiliFolderVideosResult Failed(List<BiliVideoInfo> partialVideos = null, string message = null) => new()
        {
            Success = false,
            Videos = partialVideos ?? new List<BiliVideoInfo>(),
            ErrorMessage = message ?? ""
        };
    }

    public class BiliQrCodeData
    {
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("qrcode_key")] public string Key { get; set; }
    }
}
