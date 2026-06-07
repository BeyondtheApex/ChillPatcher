// Native backend: wraps OmniSdkClient, exposes same interface
// as the hand-written gRPC-Web service clients (for api_client.dart compatibility).

import '../generated/omni_mix_player/models/track.pb.dart';
import '../generated/omni_mix_player/models/query.pb.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../generated/omni_mix_player/services/library.pb.dart';
import '../generated/omni_mix_player/services/playback.pb.dart';
import '../generated/omni_mix_player/services/instance.pb.dart';
import 'omni_sdk_client.dart';

// ── LibraryService ──────────────────────────────────────────────

class LibraryServiceClient {
  final OmniSdkClient _sdk;
  LibraryServiceClient(this._sdk);

  Future<QueryTagsResponse> queryTags(TagQuery r) async {
    final tags = await _sdk.queryTags(moduleId: r.moduleId);
    return QueryTagsResponse()..tags.addAll(tags);
  }

  Future<QueryAlbumsResponse> queryAlbums(AlbumQuery r) async {
    final albums = await _sdk.queryAlbums(moduleId: r.moduleId);
    return QueryAlbumsResponse()..albums.addAll(albums);
  }

  Future<QueryPlaylistsResponse> queryPlaylists(PlaylistQuery r) async {
    final playlists = await _sdk.queryPlaylists(moduleId: r.moduleId);
    return QueryPlaylistsResponse()..playlists.addAll(playlists);
  }

  Future<QueryTracksResponse> queryTracks(TrackQuery r) async {
    final tracks = await _sdk.queryTracks(
      albumId: r.albumId,
      playlistId: r.playlistId,
      tagId: r.tagIds.isNotEmpty ? r.tagIds.first : '',
      moduleId: r.moduleId,
      isExcluded: r.isExcluded ? 1 : 0,
    );
    return QueryTracksResponse()..tracks.addAll(tracks);
  }

  Future<Track> getTrack(GetTrackRequest r) async {
    final t = await _sdk.getTrack(r.uuid);
    if (t == null) throw OmniSdkException('Track not found: ${r.uuid}');
    return t;
  }

  Future<UpsertTrackResponse> upsertTrack(UpsertTrackRequest r) async {
    await _sdk.setTrackExcluded(r.track.uuid, r.track.isExcluded);
    return UpsertTrackResponse();
  }
}

// ── PlaybackService ─────────────────────────────────────────────

class PlaybackServiceClient {
  final OmniSdkClient _sdk;
  PlaybackServiceClient(this._sdk);

  String _iid(instanceId) => instanceId is String ? instanceId : '';

  Future<PlayResponse> play(PlayRequest r) async {
    await _sdk.play(r.instanceId, uuid: r.uuid);
    return PlayResponse();
  }

  Future<PauseResponse> pause(PauseRequest r) async {
    await _sdk.pause(r.instanceId);
    return PauseResponse();
  }

  Future<ResumeResponse> resume(ResumeRequest r) async {
    await _sdk.resume(r.instanceId);
    return ResumeResponse();
  }

  Future<ToggleResponse> toggle(ToggleRequest r) async {
    await _sdk.toggle(r.instanceId);
    return ToggleResponse();
  }

  Future<NextResponse> next(NextRequest r) async {
    await _sdk.next(r.instanceId);
    return NextResponse();
  }

  Future<PrevResponse> prev(PrevRequest r) async {
    await _sdk.prev(r.instanceId);
    return PrevResponse();
  }

  Future<SeekResponse> seek(SeekRequest r) async {
    await _sdk.seek(r.instanceId, r.position);
    return SeekResponse();
  }

  Future<StopResponse> stop(StopRequest r) async {
    await _sdk.stop(r.instanceId);
    return StopResponse();
  }

  Future<SetVolumeResponse> setVolume(SetVolumeRequest r) async {
    await _sdk.setVolume(r.instanceId, r.volume);
    return SetVolumeResponse();
  }

  Future<GetVolumeResponse> getVolume(GetVolumeRequest r) async {
    final v = await _sdk.getVolume(r.instanceId);
    return GetVolumeResponse()..volume = v;
  }

  Future<SetTargetLatencyResponse> setTargetLatency(SetTargetLatencyRequest r) async {
    await _sdk.setTargetLatency(r.instanceId, r.latency);
    return SetTargetLatencyResponse();
  }

  Future<GetTargetLatencyResponse> getTargetLatency(GetTargetLatencyRequest r) async {
    final latency = await _sdk.getTargetLatency(r.instanceId);
    return GetTargetLatencyResponse()..latency = latency;
  }

  Future<SetShuffleResponse> setShuffle(SetShuffleRequest r) async {
    await _sdk.setShuffle(r.instanceId, r.enabled);
    return SetShuffleResponse();
  }

  Future<SetRepeatModeResponse> setRepeatMode(SetRepeatModeRequest r) async {
    await _sdk.setRepeatMode(r.instanceId, r.mode.value);
    return SetRepeatModeResponse();
  }

  Future<GetQueueResponse> getQueue(GetQueueRequest r) async {
    final q = await _sdk.getQueue(r.instanceId);
    return GetQueueResponse()..queue.addAll(q);
  }

  Future<AddToQueueResponse> addToQueue(AddToQueueRequest r) async {
    await _sdk.addToQueue(r.instanceId, r.uuid);
    return AddToQueueResponse();
  }

  Future<InsertIntoQueueResponse> insertIntoQueue(InsertIntoQueueRequest r) async {
    await _sdk.insertIntoQueue(r.instanceId, r.uuids, r.index);
    return InsertIntoQueueResponse();
  }

  Future<SetQueueResponse> setQueue(SetQueueRequest r) async {
    await _sdk.setQueue(r.instanceId, r.uuids);
    return SetQueueResponse();
  }

  Future<RemoveFromQueueResponse> removeFromQueue(RemoveFromQueueRequest r) async {
    await _sdk.removeFromQueue(r.instanceId, r.index);
    return RemoveFromQueueResponse();
  }

  Future<MoveInQueueResponse> moveInQueue(MoveInQueueRequest r) async {
    await _sdk.moveInQueue(r.instanceId, r.fromIndex, r.toIndex);
    return MoveInQueueResponse();
  }

  Future<ClearQueueResponse> clearQueue(ClearQueueRequest r) async {
    await _sdk.clearQueue(r.instanceId);
    return ClearQueueResponse();
  }

  Future<GetHistoryResponse> getHistory(GetHistoryRequest r) async {
    final h = await _sdk.getHistory(r.instanceId);
    return GetHistoryResponse()..history.addAll(h);
  }

  Future<RemoveFromHistoryResponse> removeFromHistory(RemoveFromHistoryRequest r) async {
    await _sdk.removeFromHistory(r.instanceId, r.index);
    return RemoveFromHistoryResponse();
  }

  Future<MoveInHistoryResponse> moveInHistory(MoveInHistoryRequest r) async {
    await _sdk.moveInHistory(r.instanceId, r.fromIndex, r.toIndex);
    return MoveInHistoryResponse();
  }

  Future<ClearHistoryResponse> clearHistory(ClearHistoryRequest r) async {
    await _sdk.clearHistory(r.instanceId);
    return ClearHistoryResponse();
  }

  Future<GetPlaylistSourcesResponse> getPlaylistSources(GetPlaylistSourcesRequest r) async {
    final sources = await _sdk.getPlaylistSources(r.instanceId);
    return GetPlaylistSourcesResponse()..sources.addAll(sources);
  }

  Future<SetPlaylistSourcesResponse> setPlaylistSources(SetPlaylistSourcesRequest r) async {
    await _sdk.setPlaylistSources(r.instanceId, r.sources);
    return SetPlaylistSourcesResponse();
  }

  Future<EqualizerState> getEqualizer(GetEqualizerRequest r) async {
    return _sdk.getEqualizer(r.instanceId);
  }

  Future<SetEqualizerResponse> setEqualizer(SetEqualizerRequest r) async {
    await _sdk.setEqualizer(r.instanceId, r.state);
    return SetEqualizerResponse();
  }
}

// ── InstanceService ─────────────────────────────────────────────

class InstanceServiceClient {
  final OmniSdkClient _sdk;
  InstanceServiceClient(this._sdk);

  Future<InstanceConnectResponse> connect(InstanceConnectRequest r) async {
    if (r.noInstance) {
      return InstanceConnectResponse()..noInstance = true;
    }
    final profile = await _sdk.ensureConnected(
      modId: r.modId,
      gameName: r.gameName.isEmpty ? 'Flutter GUI' : r.gameName,
      displayName: r.displayName.isEmpty ? 'OmniMix GUI' : r.displayName,
    );
    return InstanceConnectResponse()
      ..instanceId = profile.id
      ..isNew = false
      ..profile = profile;
  }

  Future<InstanceHeartbeatResponse> heartbeat(InstanceHeartbeatRequest r) async {
    final alive = await _sdk.heartbeat(r.instanceId);
    return InstanceHeartbeatResponse()..alive = alive;
  }

  Future<ListInstancesResponse> listInstances(ListInstancesRequest r) async {
    final instances = await _sdk.listInstances();
    return ListInstancesResponse()..instances.addAll(instances);
  }

  Future<InstanceProfile> getProfile(GetProfileRequest r) async {
    return _sdk.getProfile(r.instanceId);
  }

  Future<UpdateProfileResponse> updateProfile(UpdateProfileRequest r) async {
    await _sdk.updateProfile(r.instanceId, r.profile);
    return UpdateProfileResponse();
  }

  Future<PlaybackStatus> getStatus(GetInstanceStatusRequest r) async {
    return _sdk.getStatus(r.instanceId);
  }

  Future<ArchiveInstanceResponse> archiveInstance(ArchiveInstanceRequest r) async {
    await _sdk.archiveInstance(r.instanceId, r.label);
    return ArchiveInstanceResponse();
  }

  Future<ListArchivesResponse> listArchives(ListArchivesRequest r) async {
    final archives = await _sdk.listArchives();
    return ListArchivesResponse()..archives.addAll(archives);
  }

  Future<InstanceProfile> getArchive(GetArchiveRequest r) async {
    return _sdk.getArchive(r.archiveId);
  }

  Future<DeleteArchiveResponse> deleteArchive(DeleteArchiveRequest r) async {
    await _sdk.deleteArchive(r.archiveId);
    return DeleteArchiveResponse();
  }

  Future<DeleteInstanceResponse> deleteInstance(DeleteInstanceRequest r) async {
    await _sdk.deleteInstance(r.instanceId);
    return DeleteInstanceResponse();
  }

  Future<InheritFromArchiveResponse> inheritFromArchive(InheritFromArchiveRequest r) async {
    final profile = await _sdk.inheritFromArchive(r.newInstanceId, r.archiveId);
    return InheritFromArchiveResponse()..profile = profile;
  }
}

// ── GrpcServices (native) ───────────────────────────────────────

class GrpcServices {
  final OmniSdkClient _sdk;
  late final LibraryServiceClient library;
  late final PlaybackServiceClient playback;
  late final InstanceServiceClient instance;

  GrpcServices({required String host, required int port}) : _sdk = OmniSdkClient() {
    library = LibraryServiceClient(_sdk);
    playback = PlaybackServiceClient(_sdk);
    instance = InstanceServiceClient(_sdk);
  }

  Future<bool> checkHealth({Duration timeout = const Duration(seconds: 3)}) async {
    try {
      await instance.listInstances(ListInstancesRequest());
      return true;
    } catch (_) {
      return false;
    }
  }

  void reconnect() {}
  void dispose() => _sdk.dispose();
}
