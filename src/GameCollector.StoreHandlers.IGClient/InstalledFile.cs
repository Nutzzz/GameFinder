using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.IGClient;

[UsedImplicitly]
internal class InstGame
{
    [JsonPropertyName("target")]
    public InstTarget? Target { get; init; }
    [JsonPropertyName("path")]
    public List<string>? Path { get; init; }
}

[UsedImplicitly]
internal class InstTarget
{
    [JsonPropertyName("item_data")]
    public InstItemData? ItemData { get; init; }
    [JsonPropertyName("game_data")]
    public InstGameData? GameData { get; init; }
}

[UsedImplicitly]
internal class InstItemData
{
    [JsonPropertyName("dev_cover")]
    public string? DevCover { get; init; }
    [JsonPropertyName("dev_id")]
    public string? DevId { get; init; }
    [JsonPropertyName("dev_image")]
    public string? DevImage { get; init; }
    [JsonPropertyName("id_key_name")]
    public string? IdKeyName { get; init; }
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("slugged_name")]
    public string? SluggedName { get; init; }
}

[UsedImplicitly]
internal class InstGameData
{
    [JsonPropertyName("args")]
    public string? Args { get; init; }
    [JsonPropertyName("categories")]
    public List<string>? Categories { get; init; }
    [JsonPropertyName("description_short")]
    public string? DescriptionShort { get; init; }
    [JsonPropertyName("exe_path")]
    public string? ExePath { get; init; }
    [JsonPropertyName("rating")]
    public InstRating? Rating { get; init; }
    [JsonPropertyName("specs")]
    public List<string>? Specs { get; init; }
}

[UsedImplicitly]
internal class InstRating
{
    [JsonPropertyName("avg_rating")]
    public decimal? AvgRating { get; init; }
}
