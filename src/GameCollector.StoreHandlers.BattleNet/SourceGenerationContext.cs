using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.BattleNet;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(CacheFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
