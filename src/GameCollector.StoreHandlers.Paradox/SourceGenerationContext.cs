using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.Paradox;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(GameMetadata))]
[JsonSerializable(typeof(LauncherSettings))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
