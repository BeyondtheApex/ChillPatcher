using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.SDK.Protos.Models;
using OmniMixPlayer.SDK.Protos.Services;

namespace OmniMixPlayer.Backend.Services
{
    /// <summary>
    /// PlaybackService gRPC 实现 — 实例级别播放控制
    /// </summary>
    public class PlaybackServiceImpl : PlaybackService.PlaybackServiceBase
    {
        private readonly InstanceRegistry _registry;
        private readonly PlaybackSessionManager _sessions;

        public PlaybackServiceImpl(InstanceRegistry registry, PlaybackSessionManager sessions, ILogger<PlaybackServiceImpl> logger)
        {
            _registry = registry;
            _sessions = sessions;
        }

        private PlaybackController GetController(string instanceId)
        {
            var profile = _registry.Get(instanceId);
            if (profile == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Instance not found"));
            var ctrl = _sessions.GetController(instanceId);
            if (ctrl == null)
                throw new RpcException(new Status(StatusCode.Unavailable, "Instance not online"));
            return ctrl;
        }

        private InstanceCapabilities GetCapabilities(string instanceId)
        {
            return InstanceCapabilityPolicy.Get(_registry, instanceId);
        }

        private void SaveQueueState(string instanceId, PlaybackController ctrl)
        {
            _registry.SavePlaybackQueue(instanceId, ctrl.CreateQueueSnapshot());
        }

        public override Task<PlayResponse> Play(PlayRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireServerPlayback(caps, "play");
            var ctrl = GetController(request.InstanceId);
            ctrl.Play(request.Uuid);
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
            GetController(request.InstanceId).SetVolume(request.Volume);
            _registry.SaveVolume(request.InstanceId, request.Volume);
            return Task.FromResult(new SetVolumeResponse { Saved = true });
        }

        public override Task<GetVolumeResponse> GetVolume(GetVolumeRequest request, ServerCallContext context)
        {
            var vol = GetController(request.InstanceId).Volume;
            return Task.FromResult(new GetVolumeResponse { Volume = vol });
        }

        public override Task<SetTargetLatencyResponse> SetTargetLatency(SetTargetLatencyRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireAudioPlayback(caps, "setTargetLatency");
            GetController(request.InstanceId).SetTargetLatency(request.Latency);
            _registry.SaveTargetLatency(request.InstanceId, request.Latency);
            return Task.FromResult(new SetTargetLatencyResponse { Saved = true });
        }

        public override Task<GetTargetLatencyResponse> GetTargetLatency(GetTargetLatencyRequest request, ServerCallContext context)
        {
            var lat = GetController(request.InstanceId).TargetLatency;
            return Task.FromResult(new GetTargetLatencyResponse { Latency = lat });
        }

        public override Task<SetShuffleResponse> SetShuffle(SetShuffleRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireShuffle(caps, "setShuffle");
            var ctrl = GetController(request.InstanceId);
            ctrl.SetShuffle(request.Enabled);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new SetShuffleResponse());
        }

        public override Task<SetRepeatModeResponse> SetRepeatMode(SetRepeatModeRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireRepeat(caps, "setRepeatMode");
            var ctrl = GetController(request.InstanceId);
            ctrl.SetRepeatMode(request.Mode);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new SetRepeatModeResponse());
        }

        public override Task<GetQueueResponse> GetQueue(GetQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "getQueue");
            var ctrl = GetController(request.InstanceId);
            var resp = new GetQueueResponse();
            resp.Queue.AddRange(ctrl.Queue.Select((m, i) => ToQueueTrack(m, i)));
            return Task.FromResult(resp);
        }

        public override Task<AddToQueueResponse> AddToQueue(AddToQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "addToQueue");
            var ctrl = GetController(request.InstanceId);
            ctrl.AddToQueue(request.Uuid);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new AddToQueueResponse());
        }

        public override Task<InsertIntoQueueResponse> InsertIntoQueue(InsertIntoQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "insertIntoQueue");
            var ctrl = GetController(request.InstanceId);
            ctrl.InsertIntoQueue(request.Uuids, request.Index);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new InsertIntoQueueResponse());
        }

        public override Task<SetQueueResponse> SetQueue(SetQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "setQueue");
            var ctrl = GetController(request.InstanceId);
            ctrl.SetQueue(request.Uuids);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new SetQueueResponse());
        }

        public override Task<RemoveFromQueueResponse> RemoveFromQueue(RemoveFromQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "removeFromQueue");
            var ctrl = GetController(request.InstanceId);
            switch (request.TargetCase)
            {
                case RemoveFromQueueRequest.TargetOneofCase.Index:
                    ctrl.RemoveFromQueue(request.Index);
                    break;
                case RemoveFromQueueRequest.TargetOneofCase.Uuid:
                    ctrl.RemoveFromQueue(request.Uuid);
                    break;
            }
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new RemoveFromQueueResponse());
        }

        public override Task<MoveInQueueResponse> MoveInQueue(MoveInQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "moveInQueue");
            var ctrl = GetController(request.InstanceId);
            ctrl.MoveInQueue(request.FromIndex, request.ToIndex);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new MoveInQueueResponse());
        }

        public override Task<ClearQueueResponse> ClearQueue(ClearQueueRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "clearQueue");
            var ctrl = GetController(request.InstanceId);
            ctrl.ClearQueue();
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new ClearQueueResponse());
        }

        public override Task<GetHistoryResponse> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "getHistory");
            var ctrl = GetController(request.InstanceId);
            var resp = new GetHistoryResponse();
            resp.History.AddRange(ctrl.History.Select((m, i) => ToQueueTrack(m, i)));
            return Task.FromResult(resp);
        }

        public override Task<RemoveFromHistoryResponse> RemoveFromHistory(RemoveFromHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "removeFromHistory");
            var ctrl = GetController(request.InstanceId);
            ctrl.RemoveFromHistory(request.Index);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new RemoveFromHistoryResponse());
        }

        public override Task<MoveInHistoryResponse> MoveInHistory(MoveInHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "moveInHistory");
            var ctrl = GetController(request.InstanceId);
            ctrl.MoveInHistory(request.FromIndex, request.ToIndex);
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new MoveInHistoryResponse());
        }

        public override Task<ClearHistoryResponse> ClearHistory(ClearHistoryRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireQueueManagement(caps, "clearHistory");
            var ctrl = GetController(request.InstanceId);
            ctrl.ClearHistory();
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new ClearHistoryResponse());
        }

        public override Task<GetPlaylistSourcesResponse> GetPlaylistSources(GetPlaylistSourcesRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequirePlaylistManagement(caps, "getPlaylistSources");
            var sources = GetController(request.InstanceId).PlaylistSources;
            var resp = new GetPlaylistSourcesResponse();
            resp.Sources.AddRange(sources.Select(s => new SDK.Protos.Services.PlaylistSourceInfo
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
            var ctrl = GetController(request.InstanceId);
            ctrl.SetPlaylistSources(request.Sources.Select(s =>
                new PlaylistSourceRequest { id = s.Id, name = s.Name, kind = s.Kind, refId = s.RefId, uuids = s.Uuids.ToArray() }));
            SaveQueueState(request.InstanceId, ctrl);
            return Task.FromResult(new SetPlaylistSourcesResponse());
        }

        public override Task<PlaybackStatus> GetStatus(GetStatusRequest request, ServerCallContext context)
        {
            var status = _sessions.GetPlaybackStatus(request.InstanceId);
            if (status == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Instance not found"));
            return Task.FromResult(status);
        }

        public override Task<SDK.Protos.Models.EqualizerState> GetEqualizer(GetEqualizerRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireEqualizer(caps, "getEqualizer");
            var state = GetController(request.InstanceId).Equalizer.CurrentState;
            return Task.FromResult(MapEqualizerState(state));
        }

        public override Task<SetEqualizerResponse> SetEqualizer(SetEqualizerRequest request, ServerCallContext context)
        {
            var caps = GetCapabilities(request.InstanceId);
            InstanceCapabilityPolicy.RequireEqualizer(caps, "setEqualizer");
            var ctrl = GetController(request.InstanceId);
            ctrl.Equalizer.UpdateState(MapEqualizerStateToInternal(request.State));
            _registry.SaveEqualizer(request.InstanceId, request.State);
            return Task.FromResult(new SetEqualizerResponse { Saved = true });
        }

        // ── Helpers ──

        private static QueueTrack ToQueueTrack(Track m, int index) => new QueueTrack
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

        private static SDK.Protos.Models.EqualizerState MapEqualizerState(Audio.EqualizerState internalState)
        {
            var state = new SDK.Protos.Models.EqualizerState
            {
                Enabled = internalState.Enabled,
                GlobalGainDb = internalState.GlobalGainDb,
                SoftClipEnabled = internalState.SoftClipEnabled
            };
            foreach (var pt in internalState.Points)
            {
                state.Points.Add(new SDK.Protos.Models.EqualizerPoint
                {
                    Id = pt.Id,
                    Frequency = pt.Frequency,
                    GainDb = pt.GainDb,
                    Q = pt.Q,
                    Type = (SDK.Protos.Models.EqualizerFilterType)(int)pt.Type
                });
            }
            return state;
        }

        private static Audio.EqualizerState MapEqualizerStateToInternal(SDK.Protos.Models.EqualizerState proto)
        {
            var state = new Audio.EqualizerState
            {
                Enabled = proto.Enabled,
                GlobalGainDb = proto.GlobalGainDb,
                SoftClipEnabled = proto.SoftClipEnabled
            };
            foreach (var pt in proto.Points)
            {
                state.Points.Add(new Audio.EqualizerPoint
                {
                    Id = pt.Id,
                    Frequency = pt.Frequency,
                    GainDb = pt.GainDb,
                    Q = pt.Q,
                    Type = (Audio.EqualizerFilterType)(int)pt.Type
                });
            }
            return state;
        }
    }
}
