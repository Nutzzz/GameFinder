using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Humble;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ConfigFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
