using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Plarium;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(GameStorage))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
