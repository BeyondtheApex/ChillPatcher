using System.IO;
using System.Text.Json;

namespace OmniMixPlayer.Backend.Storage
{
    public static class StorageVersion
    {
        public const int Current = 1;
        public const string JsonKey = "storage_version";
        public const string LiteDbCollection = "storage_metadata";
        public const string LiteDbDocumentId = "schema";

        public static bool JsonHasCurrentVersion(string path)
        {
            if (!File.Exists(path)) return true;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty(JsonKey, out var version)
                    && version.TryGetInt32(out var value)
                    && value == Current;
            }
            catch
            {
                return false;
            }
        }
    }
}
