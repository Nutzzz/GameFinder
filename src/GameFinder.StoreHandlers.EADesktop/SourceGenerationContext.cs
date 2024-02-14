using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.EADesktop;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(InstallInfoFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
