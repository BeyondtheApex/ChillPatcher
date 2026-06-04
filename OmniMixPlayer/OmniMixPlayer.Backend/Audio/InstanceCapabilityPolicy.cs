using Grpc.Core;
using OmniMixPlayer.SDK.Protos.Models;

namespace OmniMixPlayer.Backend.Audio
{
    /// <summary>
    /// 实例能力策略 — 在服务层 enforce 能力限制
    /// </summary>
    public static class InstanceCapabilityPolicy
    {
        public static void Require(InstanceCapabilities caps, string operation, bool capability, string detail = null)
        {
            if (!capability)
                throw new RpcException(new Status(StatusCode.FailedPrecondition,
                    $"Operation '{operation}' not supported by this instance{(detail != null ? $": {detail}" : "")}"));
        }

        public static void RequireServerPlayback(InstanceCapabilities caps, string op)
            => Require(caps, op, caps.ServerControlledPlayback, "requires server-controlled playback");

        public static void RequireQueueManagement(InstanceCapabilities caps, string op)
            => RequireServerPlayback(caps, op);

        public static void RequirePlaylistManagement(InstanceCapabilities caps, string op)
            => Require(caps, op, SupportsLibraryManagement(caps), "library management not available");

        public static void RequireVolumeControl(InstanceCapabilities caps, string op)
            => RequireServerPlayback(caps, op);

        public static void RequireEqualizer(InstanceCapabilities caps, string op)
            => RequireServerPlayback(caps, op);

        public static void RequireShuffle(InstanceCapabilities caps, string op)
            => RequireServerPlayback(caps, op);

        public static void RequireRepeat(InstanceCapabilities caps, string op)
            => RequireServerPlayback(caps, op);

        public static void RequireSeek(InstanceCapabilities caps, string op)
            => RequireServerPlayback(caps, op);

        public static void RequireTagFiltering(InstanceCapabilities caps, string op)
            => Require(caps, op, caps.TagFiltering, "tag filtering not available");

        public static void RequireAlbumFiltering(InstanceCapabilities caps, string op)
            => RequirePlaylistManagement(caps, op);

        public static bool SupportsLibraryManagement(InstanceCapabilities caps)
            => caps != null && (caps.ServerControlledPlayback || caps.PlaylistManagement);

        /// <summary>
        /// Get capabilities for an instance (from its profile)
        /// </summary>
        public static InstanceCapabilities Get(InstanceRegistry registry, string instanceId)
        {
            var profile = registry.Get(instanceId);
            return profile?.Capabilities ?? new InstanceCapabilities
            {
                ServerControlledPlayback = true,
                ClientManagedPlayback = true,
                QueueManagement = true,
                PlaylistManagement = true,
                TagFiltering = true,
                AlbumFiltering = true,
                Shuffle = true,
                Repeat = true,
                Seek = true,
                VolumeControl = true,
                Equalizer = true
            };
        }
    }
}
