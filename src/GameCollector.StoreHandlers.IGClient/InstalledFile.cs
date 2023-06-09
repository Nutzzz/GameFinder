using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.IGClient;

[UsedImplicitly]
internal record InstalledFile(
    InstTarget? Target,
    List<string>? Path
);

[UsedImplicitly]
internal record InstTarget(
    [property: JsonPropertyName("item_data")]
    InstItemData? ItemData,
    [property: JsonPropertyName("game_data")]
    InstGameData? GameData
);

[UsedImplicitly]
internal record InstItemData(
    [property: JsonPropertyName("dev_cover")]
    string? DevCover,
    [property: JsonPropertyName("dev_id")]
    string? DevId,
    [property: JsonPropertyName("dev_image")]
    string? DevImage,
    [property: JsonPropertyName("id_key_name")]
    string? IdKeyName,
    string? Name,
    [property: JsonPropertyName("slugged_name")]
    string? SluggedName
);

[UsedImplicitly]
internal record InstGameData(
    string? Args,
    List<string>? Categories,
    [property: JsonPropertyName("description_long")]
    string? DescriptionLong,
    [property: JsonPropertyName("description_short")]
    string? DescriptionShort,
    [property: JsonPropertyName("exe_path")]
    string? ExePath,
    InstRating? Rating,
    List<string>? Specs,
    List<string>? Tags
);

[UsedImplicitly]
internal record InstRating(
    [property: JsonPropertyName("avg_rating")]
    decimal? AvgRating
);
