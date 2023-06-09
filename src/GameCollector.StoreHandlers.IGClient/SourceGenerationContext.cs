using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.IGClient;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(InstalledFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
