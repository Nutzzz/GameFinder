using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.RobotCache;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(AppConfigFile))]
[JsonSerializable(typeof(StateInfoFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
