using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Itch;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Verdict))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
