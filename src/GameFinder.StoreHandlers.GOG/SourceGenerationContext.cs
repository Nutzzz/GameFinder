using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.GOG;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(LinksJson))]
[JsonSerializable(typeof(ImagesJson))]
[JsonSerializable(typeof(ValueJson))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
