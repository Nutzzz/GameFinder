using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.EGS;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(CatCacheFile))]
[JsonSerializable(typeof(ManifestFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
