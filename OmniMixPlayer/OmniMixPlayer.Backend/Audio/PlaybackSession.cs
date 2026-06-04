using System;

namespace OmniMixPlayer.Backend.Audio
{
    /// <summary>
    /// PlaybackSession — 临时在线播放连接
    /// 实例是长期的 profile; session 是临时的在线连接
    /// </summary>
    public sealed class PlaybackSession : IDisposable
    {
        public string Id { get; init; }
        public string ClientId { get; init; }
        public SDK.Protos.Models.PlaybackModeType Mode { get; set; }
        public SDK.Protos.Models.InstanceKind Kind { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime? DetachedAt { get; set; }
        public SharedMemoryServer SharedMemory { get; init; }
        public PlaybackController Controller { get; init; }
        public bool IsAttached => DetachedAt == null;
        public string SharedMemoryName => SharedMemory?.MapName;

        public void Dispose()
        {
            Controller?.Dispose();
            SharedMemory?.Dispose();
        }
    }
}
