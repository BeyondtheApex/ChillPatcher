using System;
using Newtonsoft.Json;

namespace ChillPatcher.Module.Bilibili
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

    public class BiliQrCodeData
    {
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("qrcode_key")] public string Key { get; set; }
    }
}