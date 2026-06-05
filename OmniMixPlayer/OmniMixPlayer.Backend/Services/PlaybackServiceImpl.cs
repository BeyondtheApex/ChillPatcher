using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;
using OmniMixPlayer.SDK.Protos.Services;

namespace OmniMixPlayer.Backend.Services
{
    public class PlaybackServiceImpl : PlaybackService.PlaybackServiceBase
    {
        private readonly InstanceRegistry _registry;
        private readonly PlaybackSessionManager _sessions;
        private readonly PlaybackTimelineStore _timeline;
        private readonly ILibraryRegistry _library;

        public PlaybackServiceImpl(
            InstanceRegistry registry,
            PlaybackSessionManager sessions,
            PlaybackTimelineStore timeline,
            ILibraryRegistry library,
            ILogger<PlaybackServiceImpl> logger)
        {
            _registry = registry;
            _sessions = sessions;
            _timeline = timeline;
            _library = library;
        }

        private InstanceProfile GetProfileOrThrow(string instanceId)
        {
            var profile = _registry.Get(instanceId);
            if (profile == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Instance not found"));
            return profile;
        }

        private PlaybackController GetController(string instanceId)
        {
            GetProfileOrThrow(instanceId);
            var ctrl = _sessions.GetController(instanceId);
            if (ctrl == null)
                throw new RpcException(new Status(StatusCode.Unavailable, "Instance not online"));
            return ctrl;
        }

        private InstanceCapabilities GetCapabilities(string instanceId)
        {
            return InstanceCapabilityPolicy.Get(_registry, instanceId);
        }

        public override Task<PlayResponse> Play(PlayRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "play");
            GetController(request.InstanceId).Play(request.Uuid);
            return Task.FromResult(new PlayResponse());
        }

        public override Task<PauseResponse> Pause(PauseRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "pause");
            GetController(request.InstanceId).Pause();
            return Task.FromResult(new PauseResponse());
        }

        public override Task<ResumeResponse> Resume(ResumeRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "resume");
            GetController(request.InstanceId).Resume();
            return Task.FromResult(new ResumeResponse());
        }

        public override Task<ToggleResponse> Toggle(ToggleRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "toggle");
            GetController(request.InstanceId).Toggle();
            return Task.FromResult(new ToggleResponse());
        }

        public override Task<NextResponse> Next(NextRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "next");
            GetController(request.InstanceId).Next();
            return Task.FromResult(new NextResponse());
        }

        public override Task<PrevResponse> Prev(PrevRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "prev");
            GetController(request.InstanceId).Prev();
            return Task.FromResult(new PrevResponse());
        }

        public override Task<SeekResponse> Seek(SeekRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireSeek(caps, "seek");
            GetController(request.InstanceId).Seek(request.Position);
            return Task.FromResult(new SeekResponse());
        }

        public override Task<StopResponse> Stop(StopRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "stop");
            GetController(request.InstanceId).Stop();
            return Task.FromResult(new StopResponse());
        }

        public override Task<SetVolumeResponse> SetVolume(SetVolumeRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireVolumeControl(caps, "setVolume");
            _registry.SaveVolume(request.InstanceId, request.Volume);
            return Task.FromResult(new SetVolumeResponse { Saved = true });
        }

        public override Task<GetVolumeResponse> GetVolume(GetVolumeRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetVolumeResponse { Volume = GetProfileOrThrow(request.InstanceId).Volume });
        }

        public override Task<SetTargetLatencyResponse> SetTargetLatency(SetTargetLatencyRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireAudioPlayback(caps, "setTargetLatency");
            _registry.SaveTargetLatency(request.InstanceId, request.Latency);
            return Task.FromResult(new SetTargetLatencyResponse { Saved = true });
        }

        public override Task<GetTargetLatencyResponse> GetTargetLatency(GetTargetLatencyRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetTargetLatencyResponse { Latency = GetProfileOrThrow(request.InstanceId).TargetLatency });
        }

        public override Task<SetShuffleResponse> SetShuffle(SetShuffleRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireShuffle(caps, "setShuffle");
            _timeline.SetShuffle(request.InstanceId, request.Enabled);
            return Task.FromResult(new SetShuffleResponse());
        }

        public override Task<SetRepeatModeResponse> SetRepeatMode(SetRepeatModeRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireRepeat(caps, "setRepeatMode");
            _timeline.SetRepeatMode(request.InstanceId, request.Mode);
            return Task.FromResult(new SetRepeatModeResponse());
        }

        public override Task<GetQueueResponse> GetQueue(GetQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "getQueue");
            var resp = new GetQueueResponse();
            resp.Queue.AddRange(_timeline.Get(request.InstanceId).ManualQueueUuids.Select((u, i) => ToQueueTrack(u, i)));
            return Task.FromResult(resp);
        }

        public override Task<AddToQueueResponse> AddToQueue(AddToQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "addToQueue");
            _timeline.AddToQueue(request.InstanceId, request.Uuid);
            return Task.FromResult(new AddToQueueResponse());
        }

        public override Task<InsertIntoQueueResponse> InsertIntoQueue(InsertIntoQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "insertIntoQueue");
            _timeline.InsertIntoQueue(request.InstanceId, request.Uuids, request.Index);
            return Task.FromResult(new InsertIntoQueueResponse());
        }

        public override Task<SetQueueResponse> SetQueue(SetQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "setQueue");
            _timeline.SetQueue(request.InstanceId, request.Uuids);
            return Task.FromResult(new SetQueueResponse());
        }

        public override Task<RemoveFromQueueResponse> RemoveFromQueue(RemoveFromQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "removeFromQueue");
            switch (request.TargetCase)
            {
                case RemoveFromQueueRequest.TargetOneofCase.Index:
                    _timeline.RemoveFromQueue(request.InstanceId, request.Index);
                    break;
                case RemoveFromQueueRequest.TargetOneofCase.Uuid:
                    _timeline.RemoveFromQueue(request.InstanceId, request.Uuid);
                    break;
            }
            return Task.FromResult(new RemoveFromQueueResponse());
        }

        public override Task<MoveInQueueResponse> MoveInQueue(MoveInQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "moveInQueue");
            _timeline.MoveInQueue(request.InstanceId, request.FromIndex, request.ToIndex);
            return Task.FromResult(new MoveInQueueResponse());
        }

        public override Task<ClearQueueResponse> ClearQueue(ClearQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "clearQueue");
            _timeline.ClearQueue(request.InstanceId);
            return Task.FromResult(new ClearQueueResponse());
        }

        public override Task<GetHistoryResponse> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "getHistory");
            var resp = new GetHistoryResponse();
            resp.History.AddRange(_timeline.Get(request.InstanceId).HistoryUuids.Select((u, i) => ToQueueTrack(u, i)));
            return Task.FromResult(resp);
        }

        public override Task<RemoveFromHistoryResponse> RemoveFromHistory(RemoveFromHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "removeFromHistory");
            _timeline.RemoveFromHistory(request.InstanceId, request.Index);
            return Task.FromResult(new RemoveFromHistoryResponse());
        }

        public override Task<MoveInHistoryResponse> MoveInHistory(MoveInHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "moveInHistory");
            _timeline.MoveInHistory(request.InstanceId, request.FromIndex, request.ToIndex);
            return Task.FromResult(new MoveInHistoryResponse());
        }

        public override Task<ClearHistoryResponse> ClearHistory(ClearHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "clearHistory");
            _timeline.ClearHistory(request.InstanceId);
            return Task.FromResult(new ClearHistoryResponse());
        }

        public override Task<GetPlaylistSourcesResponse> GetPlaylistSources(GetPlaylistSourcesRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequirePlaylistManagement(caps, "getPlaylistSources");
            var resp = new GetPlaylistSourcesResponse();
            resp.Sources.AddRange(_timeline.GetPlaylistSources(request.InstanceId).Select(s => new SDK.Protos.Services.PlaylistSourceInfo
            {
                Id = s.Id,
                Name = s.Name,
                SongCount = s.SongCount,
                Kind = s.Kind,
                RefId = s.RefId ?? ""
            }));
            return Task.FromResult(resp);
        }

        public override Task<SetPlaylistSourcesResponse> SetPlaylistSources(SetPlaylistSourcesRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequirePlaylistManagement(caps, "setPlaylistSources");
            InstanceCapabilityPolicy.RequirePlaylistSourceLimit(caps, "setPlaylistSources", request.Sources.Count);
            _timeline.SetPlaylistSources(request.InstanceId, request.Sources.Select(s =>
                new PlaylistSourceRequest { id = s.Id, name = s.Name, kind = s.Kind, refId = s.RefId, uuids = s.Uuids.ToArray() }));
            return Task.FromResult(new SetPlaylistSourcesResponse());
        }

        public override Task<PlaybackStatus> GetStatus(GetStatusRequest request, ServerCallContext context)
        {
            var status = _sessions.GetPlaybackStatus(request.InstanceId);
            if (status != null)
                return Task.FromResult(status);

            var profile = GetProfileOrThrow(request.InstanceId);
            var timeline = _timeline.Get(request.InstanceId);
            status = new PlaybackStatus
            {
                Shuffle = timeline.Shuffle,
                RepeatMode = timeline.RepeatMode,
                Volume = profile.Volume
            };
            if (!string.IsNullOrWhiteSpace(timeline.CurrentUuid))
            {
                var track = _library.GetTrack(timeline.CurrentUuid);
                status.TrackUuid = timeline.CurrentUuid;
                status.Title = track?.Title ?? "";
                status.Artist = track?.Artist ?? "";
                status.AlbumId = track?.AlbumId ?? "";
                status.Duration = track?.Duration ?? 0;
            }
            return Task.FromResult(status);
        }

        public override Task<SDK.Protos.Models.EqualizerState> GetEqualizer(GetEqualizerRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireEqualizer(caps, "getEqualizer");
            return Task.FromResult(GetProfileOrThrow(request.InstanceId).Equalizer ?? new SDK.Protos.Models.EqualizerState { SoftClipEnabled = true });
        }

        public override Task<SetEqualizerResponse> SetEqualizer(SetEqualizerRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireEqualizer(caps, "setEqualizer");
            _registry.SaveEqualizer(request.InstanceId, request.State);
            return Task.FromResult(new SetEqualizerResponse { Saved = true });
        }

        private static QueueTrack ToQueueTrack(Track m, int index) => new()
        {
            Index = index,
            Uuid = m.Uuid,
            Title = m.Title,
            Artist = m.Artist,
            AlbumId = m.AlbumId,
            Duration = m.Duration,
            ModuleId = m.ModuleId,
            CoverUri = m.CoverUri
        };

        private QueueTrack ToQueueTrack(string uuid, int index)
        {
            var track = _library.GetTrack(uuid);
            if (track == null)
                return new QueueTrack { Index = index, Uuid = uuid ?? "" };
            return ToQueueTrack(track, index);
        }
    }
}
