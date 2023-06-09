using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.GameJolt;

[JsonSourceGenerationOptions(WriteIndented = false, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Game))]
[JsonSerializable(typeof(GamesFile))]
[JsonSerializable(typeof(ManifestFile))]
[JsonSerializable(typeof(Package))]
[JsonSerializable(typeof(PackagesFile))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
