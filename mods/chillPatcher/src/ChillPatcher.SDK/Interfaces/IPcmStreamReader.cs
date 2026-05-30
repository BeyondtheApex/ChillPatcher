namespace ChillPatcher.SDK.Interfaces
{
    /// <summary>
    /// PCM 流式读取器接口
    /// 用于从 OmniMixPlayer 后端读取 PCM 数据
    /// </summary>
    public interface IPcmStreamReader
    {
        /// <summary>
        /// 曲目 UUID
        /// </summary>
        string Uuid { get; }

        /// <summary>
        /// PCM 流信息
        /// </summary>
        PcmStreamInfo Info { get; }

        /// <summary>
        /// 读取 PCM 帧数据
        /// </summary>
        long ReadFrames(float[] buffer, int framesToRead);

        /// <summary>
        /// 跳转到指定帧位置
        /// </summary>
        bool Seek(ulong frameIndex);

        /// <summary>
        /// 当前读取位置（帧数）
        /// </summary>
        ulong CurrentFrame { get; }

        /// <summary>
        /// 流是否已到达末尾
        /// </summary>
        bool IsEndOfStream { get; }

        /// <summary>
        /// 是否可 seek
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// 流是否就绪
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 是否有待定的 seek 请求
        /// </summary>
        bool HasPendingSeek { get; }

        /// <summary>
        /// 缓存进度 (0~1, -1 表示不支持)
        /// </summary>
        double CacheProgress { get; }

        /// <summary>
        /// 缓存是否已完成
        /// </summary>
        bool IsCacheComplete { get; }

        /// <summary>
        /// 待定 Seek 的目标帧（-1 表示无待定）
        /// </summary>
        long PendingSeekFrame { get; }

        /// <summary>
        /// 取消待定的 Seek 操作
        /// </summary>
        void CancelPendingSeek();

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// PCM 流信息
    /// </summary>
    public class PcmStreamInfo
    {
        /// <summary>
        /// 采样率 (如 44100)
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// 声道数 (如 2)
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// 总帧数
        /// </summary>
        public ulong TotalFrames { get; set; }

        /// <summary>
        /// 音频格式
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 是否支持 seek
        /// </summary>
        public bool CanSeek { get; set; }

        /// <summary>
        /// 总时长（秒）
        /// </summary>
        public float Duration =>
            SampleRate > 0 && TotalFrames > 0 ? (float)TotalFrames / SampleRate : 0;
    }
}
