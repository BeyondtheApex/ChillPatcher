using System.Text.Json;
using System.Text.Json.Serialization;
using OmniMixPlayer.Backend.Audio;

namespace OmniMixPlayer.Backend.Http;

/// <summary>
/// JSON source generation context for all API request/response types.
/// Required for trimmed (AOT) publishing where reflection-based
/// serialization is unavailable.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(InstanceConnectRequest))]
[JsonSerializable(typeof(PlayRequest))]
[JsonSerializable(typeof(SeekRequest))]
[JsonSerializable(typeof(VolumeRequest))]
[JsonSerializable(typeof(ShuffleRequest))]
[JsonSerializable(typeof(RepeatRequest))]
[JsonSerializable(typeof(MoveRequest))]
[JsonSerializable(typeof(QueueReplaceRequest))]
[JsonSerializable(typeof(QueueInsertRequest))]
[JsonSerializable(typeof(PlaylistSourcesReplaceRequest))]
[JsonSerializable(typeof(PlaylistSourceInsertRequest))]
[JsonSerializable(typeof(PlaylistSourceRequest))]
[JsonSerializable(typeof(FavoriteRequest))]
[JsonSerializable(typeof(ModuleToggleRequest))]
[JsonSerializable(typeof(ArchiveRenameRequest))]
[JsonSerializable(typeof(InstanceMetaRequest))]
[JsonSerializable(typeof(LatencyRequest))]
internal partial class ApiJsonContext : JsonSerializerContext
{
}
