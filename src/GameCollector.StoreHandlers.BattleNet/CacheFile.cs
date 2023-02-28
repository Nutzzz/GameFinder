using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.BattleNet;

[UsedImplicitly]
internal class CacheFile
{
    [JsonPropertyName("all")]
    public JsonElement All { get; init; }
    [JsonPropertyName("enus")]
    public JsonElement Language { get; init; }
    [JsonPropertyName("platform")]
    public JsonElement Platform { get; init; }
}

[UsedImplicitly]
internal class AllConfig
{
    [JsonPropertyName("form")]
    public JsonElement? Form { get; init; }
    [JsonPropertyName("product")]
    public string? Product { get; init; }
    [JsonPropertyName("shared_container_default_subfolder")]
    public string? SharedContainerDefaultSubfolder { get; init; }
    [JsonPropertyName("supported_locales")]
    public List<string>? SupportedLocales { get; init; }
}

[UsedImplicitly]
internal class WinConfig
{
    [JsonPropertyName("binaries")]
    public JsonElement Binaries { get; init; }
    [JsonPropertyName("form")]
    public JsonElement? Form { get; init; }
}
