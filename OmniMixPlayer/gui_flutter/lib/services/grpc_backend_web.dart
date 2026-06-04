/// gRPC service clients — hand-written gRPC-Web over HTTP/1.
/// Replaces the codegen stubs (package:grpc) to avoid the
/// package:web / dart:js_interop dependency that fails on native builds.
///
/// All methods return plain Future<T> (compatible with the
/// ResponseFuture<T> returned by generated stubs; both support .timeout()).

import '../generated/omni_mix_player/models/track.pb.dart';
import '../generated/omni_mix_player/models/album.pb.dart';
import '../generated/omni_mix_player/models/tag.pb.dart';
import '../generated/omni_mix_player/models/playlist.pb.dart';
import '../generated/omni_mix_player/models/query.pb.dart';
import '../generated/omni_mix_player/models/instance.pb.dart';
import '../generated/omni_mix_player/services/library.pb.dart';
import '../generated/omni_mix_player/services/playback.pb.dart';
import '../generated/omni_mix_player/services/instance.pb.dart';
import 'grpc_web_client_web.dart';

// ── LibraryService ──────────────────────────────────────────────

class LibraryServiceClient {
  static const _svc = 'omni_mix_player.LibraryService';
  final GrpcWebTransport _t;

  LibraryServiceClient(this._t);

  Future<QueryTagsResponse> queryTags(TagQuery r) =>
      _t.unary('/$_svc/QueryTags', r, QueryTagsResponse.fromBuffer);
  Future<QueryAlbumsResponse> queryAlbums(AlbumQuery r) =>
      _t.unary('/$_svc/QueryAlbums', r, QueryAlbumsResponse.fromBuffer);
  Future<QueryPlaylistsResponse> queryPlaylists(PlaylistQuery r) =>
      _t.unary('/$_svc/QueryPlaylists', r, QueryPlaylistsResponse.fromBuffer);
  Future<QueryTracksResponse> queryTracks(TrackQuery r) =>
      _t.unary('/$_svc/QueryTracks', r, QueryTracksResponse.fromBuffer);
  Future<Track> getTrack(GetTrackRequest r) =>
      _t.unary('/$_svc/GetTrack', r, Track.fromBuffer);
  Future<UpsertTrackResponse> upsertTrack(UpsertTrackRequest r) =>
      _t.unary('/$_svc/UpsertTrack', r, UpsertTrackResponse.fromBuffer);
}

// ── PlaybackService ─────────────────────────────────────────────

class PlaybackServiceClient {
  static const _svc = 'omni_mix_player.PlaybackService';
  final GrpcWebTransport _t;

  PlaybackServiceClient(this._t);

  // playback control
  Future<PlayResponse> play(PlayRequest r) =>
      _t.unary('/$_svc/Play', r, PlayResponse.fromBuffer);
  Future<PauseResponse> pause(PauseRequest r) =>
      _t.unary('/$_svc/Pause', r, PauseResponse.fromBuffer);
  Future<ResumeResponse> resume(ResumeRequest r) =>
      _t.unary('/$_svc/Resume', r, ResumeResponse.fromBuffer);
  Future<ToggleResponse> toggle(ToggleRequest r) =>
      _t.unary('/$_svc/Toggle', r, ToggleResponse.fromBuffer);
  Future<NextResponse> next(NextRequest r) =>
      _t.unary('/$_svc/Next', r, NextResponse.fromBuffer);
  Future<PrevResponse> prev(PrevRequest r) =>
      _t.unary('/$_svc/Prev', r, PrevResponse.fromBuffer);
  Future<SeekResponse> seek(SeekRequest r) =>
      _t.unary('/$_svc/Seek', r, SeekResponse.fromBuffer);
  Future<StopResponse> stop(StopRequest r) =>
      _t.unary('/$_svc/Stop', r, StopResponse.fromBuffer);

  // volume / latency
  Future<SetVolumeResponse> setVolume(SetVolumeRequest r) =>
      _t.unary('/$_svc/SetVolume', r, SetVolumeResponse.fromBuffer);
  Future<GetVolumeResponse> getVolume(GetVolumeRequest r) =>
      _t.unary('/$_svc/GetVolume', r, GetVolumeResponse.fromBuffer);
  Future<SetTargetLatencyResponse> setTargetLatency(
          SetTargetLatencyRequest r) =>
      _t.unary('/$_svc/SetTargetLatency', r,
          SetTargetLatencyResponse.fromBuffer);
  Future<GetTargetLatencyResponse> getTargetLatency(
          GetTargetLatencyRequest r) =>
      _t.unary('/$_svc/GetTargetLatency', r,
          GetTargetLatencyResponse.fromBuffer);

  // shuffle / repeat
  Future<SetShuffleResponse> setShuffle(SetShuffleRequest r) =>
      _t.unary('/$_svc/SetShuffle', r, SetShuffleResponse.fromBuffer);
  Future<SetRepeatModeResponse> setRepeatMode(SetRepeatModeRequest r) =>
      _t.unary('/$_svc/SetRepeatMode', r, SetRepeatModeResponse.fromBuffer);

  // queue
  Future<GetQueueResponse> getQueue(GetQueueRequest r) =>
      _t.unary('/$_svc/GetQueue', r, GetQueueResponse.fromBuffer);
  Future<AddToQueueResponse> addToQueue(AddToQueueRequest r) =>
      _t.unary('/$_svc/AddToQueue', r, AddToQueueResponse.fromBuffer);
  Future<InsertIntoQueueResponse> insertIntoQueue(InsertIntoQueueRequest r) =>
      _t.unary('/$_svc/InsertIntoQueue', r,
          InsertIntoQueueResponse.fromBuffer);
  Future<SetQueueResponse> setQueue(SetQueueRequest r) =>
      _t.unary('/$_svc/SetQueue', r, SetQueueResponse.fromBuffer);
  Future<RemoveFromQueueResponse> removeFromQueue(RemoveFromQueueRequest r) =>
      _t.unary('/$_svc/RemoveFromQueue', r,
          RemoveFromQueueResponse.fromBuffer);
  Future<MoveInQueueResponse> moveInQueue(MoveInQueueRequest r) =>
      _t.unary('/$_svc/MoveInQueue', r, MoveInQueueResponse.fromBuffer);
  Future<ClearQueueResponse> clearQueue(ClearQueueRequest r) =>
      _t.unary('/$_svc/ClearQueue', r, ClearQueueResponse.fromBuffer);

  // history
  Future<GetHistoryResponse> getHistory(GetHistoryRequest r) =>
      _t.unary('/$_svc/GetHistory', r, GetHistoryResponse.fromBuffer);
  Future<RemoveFromHistoryResponse> removeFromHistory(
          RemoveFromHistoryRequest r) =>
      _t.unary('/$_svc/RemoveFromHistory', r,
          RemoveFromHistoryResponse.fromBuffer);
  Future<MoveInHistoryResponse> moveInHistory(MoveInHistoryRequest r) =>
      _t.unary('/$_svc/MoveInHistory', r, MoveInHistoryResponse.fromBuffer);
  Future<ClearHistoryResponse> clearHistory(ClearHistoryRequest r) =>
      _t.unary('/$_svc/ClearHistory', r, ClearHistoryResponse.fromBuffer);

  // playlist sources
  Future<GetPlaylistSourcesResponse> getPlaylistSources(
          GetPlaylistSourcesRequest r) =>
      _t.unary('/$_svc/GetPlaylistSources', r,
          GetPlaylistSourcesResponse.fromBuffer);
  Future<SetPlaylistSourcesResponse> setPlaylistSources(
          SetPlaylistSourcesRequest r) =>
      _t.unary('/$_svc/SetPlaylistSources', r,
          SetPlaylistSourcesResponse.fromBuffer);

  // equalizer
  Future<EqualizerState> getEqualizer(GetEqualizerRequest r) =>
      _t.unary('/$_svc/GetEqualizer', r, EqualizerState.fromBuffer);
  Future<SetEqualizerResponse> setEqualizer(SetEqualizerRequest r) =>
      _t.unary('/$_svc/SetEqualizer', r, SetEqualizerResponse.fromBuffer);
}

// ── InstanceService ─────────────────────────────────────────────

class InstanceServiceClient {
  static const _svc = 'omni_mix_player.InstanceService';
  final GrpcWebTransport _t;

  InstanceServiceClient(this._t);

  Future<InstanceConnectResponse> connect(InstanceConnectRequest r) =>
      _t.unary('/$_svc/Connect', r, InstanceConnectResponse.fromBuffer);
  Future<InstanceHeartbeatResponse> heartbeat(InstanceHeartbeatRequest r) =>
      _t.unary('/$_svc/Heartbeat', r, InstanceHeartbeatResponse.fromBuffer);
  Future<ListInstancesResponse> listInstances(ListInstancesRequest r) =>
      _t.unary('/$_svc/ListInstances', r, ListInstancesResponse.fromBuffer);
  Future<InstanceProfile> getProfile(GetProfileRequest r) =>
      _t.unary('/$_svc/GetProfile', r, InstanceProfile.fromBuffer);
  Future<UpdateProfileResponse> updateProfile(UpdateProfileRequest r) =>
      _t.unary('/$_svc/UpdateProfile', r, UpdateProfileResponse.fromBuffer);
  Future<PlaybackStatus> getStatus(GetInstanceStatusRequest r) =>
      _t.unary('/$_svc/GetStatus', r, PlaybackStatus.fromBuffer);
  Future<ArchiveInstanceResponse> archiveInstance(ArchiveInstanceRequest r) =>
      _t.unary('/$_svc/ArchiveInstance', r,
          ArchiveInstanceResponse.fromBuffer);
  Future<ListArchivesResponse> listArchives(ListArchivesRequest r) =>
      _t.unary('/$_svc/ListArchives', r, ListArchivesResponse.fromBuffer);
  Future<InstanceProfile> getArchive(GetArchiveRequest r) =>
      _t.unary('/$_svc/GetArchive', r, InstanceProfile.fromBuffer);
  Future<DeleteArchiveResponse> deleteArchive(DeleteArchiveRequest r) =>
      _t.unary('/$_svc/DeleteArchive', r, DeleteArchiveResponse.fromBuffer);
  Future<DeleteInstanceResponse> deleteInstance(DeleteInstanceRequest r) =>
      _t.unary('/$_svc/DeleteInstance', r, DeleteInstanceResponse.fromBuffer);
  Future<InheritFromArchiveResponse> inheritFromArchive(
          InheritFromArchiveRequest r) =>
      _t.unary('/$_svc/InheritFromArchive', r,
          InheritFromArchiveResponse.fromBuffer);
}

// ── GrpcServices aggregator ────────────────────────────────────

class GrpcServices {
  final String _host;
  final int _port;
  GrpcWebTransport? _transport;
  bool _disposed = false;

  late final LibraryServiceClient library;
  late final PlaybackServiceClient playback;
  late final InstanceServiceClient instance;

  GrpcServices({required String host, required int port})
      : _host = host,
        _port = port {
    _connect();
  }

  String get host => _host;
  int get port => _port;

  void _connect() {
    _transport?.close();
    _transport = GrpcWebTransport(host: _host, port: _port);
    library = LibraryServiceClient(_transport!);
    playback = PlaybackServiceClient(_transport!);
    instance = InstanceServiceClient(_transport!);
  }

  Future<bool> checkHealth({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    if (_disposed) return false;
    try {
      await instance.listInstances(ListInstancesRequest());
      return true;
    } catch (_) {
      return false;
    }
  }

  void reconnect() {
    if (_disposed) return;
    _connect();
  }

  void dispose() {
    _disposed = true;
    _transport?.close();
  }
}
