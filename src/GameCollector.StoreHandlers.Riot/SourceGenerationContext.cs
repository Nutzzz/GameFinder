using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Riot;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ClientInstallFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
