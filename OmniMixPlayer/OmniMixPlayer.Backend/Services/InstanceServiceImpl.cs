using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OmniMixPlayer.Backend.Audio;
using OmniMixPlayer.SDK.Protos.Models;
using OmniMixPlayer.SDK.Protos.Services;

namespace OmniMixPlayer.Backend.Services
{
    public class InstanceServiceImpl : InstanceService.InstanceServiceBase
    {
        private readonly InstanceRegistry _registry;
        private readonly PlaybackSessionManager _sessions;

        public InstanceServiceImpl(InstanceRegistry registry, PlaybackSessionManager sessions, ILogger<InstanceServiceImpl> logger)
        {
            _registry = registry;
            _sessions = sessions;
        }

        public override Task<InstanceConnectResponse> Connect(InstanceConnectRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "client_id is required"));

            if (request.NoInstance)
                return Task.FromResult(new InstanceConnectResponse { NoInstance = true });

            var profile = _registry.ConnectOrCreate(request, out var isNew);
            _sessions.Attach(profile);
            return Task.FromResult(new InstanceConnectResponse { InstanceId = profile.Id, IsNew = isNew, Profile = profile });
        }

        public override Task<InstanceHeartbeatResponse> Heartbeat(InstanceHeartbeatRequest request, ServerCallContext context)
        {
            var alive = _sessions.Heartbeat(request.InstanceId);
            return Task.FromResult(new InstanceHeartbeatResponse { Alive = alive });
        }

        public override Task<InstanceDisconnectResponse> Disconnect(InstanceDisconnectRequest request, ServerCallContext context)
        {
            var disconnected = _sessions.Detach(request.InstanceId);
            return Task.FromResult(new InstanceDisconnectResponse { Disconnected = disconnected });
        }

        public override Task<DeleteInstanceResponse> DeleteInstance(DeleteInstanceRequest request, ServerCallContext context)
        {
            _sessions.Remove(request.InstanceId);
            var deleted = _registry.Delete(request.InstanceId);
            return Task.FromResult(new DeleteInstanceResponse { Deleted = deleted });
        }

        public override Task<ListInstancesResponse> ListInstances(ListInstancesRequest request, ServerCallContext context)
        {
            var summaries = _registry.ListSummaries(_sessions);
            var resp = new ListInstancesResponse();
            resp.Instances.AddRange(summaries);
            return Task.FromResult(resp);
        }

        public override Task<InstanceProfile> GetProfile(GetProfileRequest request, ServerCallContext context)
        {
            var profile = _registry.Get(request.InstanceId);
            if (profile == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Instance not found"));
            return Task.FromResult(profile);
        }

        public override Task<UpdateProfileResponse> UpdateProfile(UpdateProfileRequest request, ServerCallContext context)
        {
            _registry.Update(request.Profile);
            return Task.FromResult(new UpdateProfileResponse { Saved = true });
        }

        public override Task<PlaybackStatus> GetStatus(GetInstanceStatusRequest request, ServerCallContext context)
        {
            var status = _sessions.GetPlaybackStatus(request.InstanceId);
            if (status == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Instance not found or not online"));
            return Task.FromResult(status);
        }

        public override Task<ArchiveInstanceResponse> ArchiveInstance(ArchiveInstanceRequest request, ServerCallContext context)
        {
            var archive = _registry.SaveArchiveCopy(request.InstanceId, request.Label);
            if (archive == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Instance not found"));
            return Task.FromResult(new ArchiveInstanceResponse { Archived = true, Archive = archive });
        }

        public override Task<ListArchivesResponse> ListArchives(ListArchivesRequest request, ServerCallContext context)
        {
            var resp = new ListArchivesResponse();
            resp.Archives.AddRange(_registry.ListArchives());
            return Task.FromResult(resp);
        }

        public override Task<InstanceProfile> GetArchive(GetArchiveRequest request, ServerCallContext context)
        {
            var profile = _registry.GetArchive(request.ArchiveId);
            if (profile == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Archive not found"));
            return Task.FromResult(profile);
        }

        public override Task<DeleteArchiveResponse> DeleteArchive(DeleteArchiveRequest request, ServerCallContext context)
        {
            var deleted = _registry.DeleteArchive(request.ArchiveId);
            return Task.FromResult(new DeleteArchiveResponse { Deleted = deleted });
        }

        public override Task<InheritFromArchiveResponse> InheritFromArchive(InheritFromArchiveRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.NewInstanceId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "new_instance_id is required"));

            var profile = _registry.InheritFromArchive(request.NewInstanceId, request.ArchiveId);
            if (profile == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Archive not found"));

            return Task.FromResult(new InheritFromArchiveResponse { Inherited = true, Profile = profile });
        }
    }
}
