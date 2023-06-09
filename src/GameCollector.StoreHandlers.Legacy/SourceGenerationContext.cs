using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Legacy;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(AppStateFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
