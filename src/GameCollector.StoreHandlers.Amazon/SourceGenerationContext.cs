using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Amazon;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ExternalWebsites))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
