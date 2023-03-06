using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.IGClient;

[UsedImplicitly]
internal class ConfigGame
{
    [JsonPropertyName("data")]
    public ConfigData? Data { get; init; }
    [JsonPropertyName("description_short")]
    public string? DescriptionShort { get; init; }
    [JsonPropertyName("categories")]
    public List<string>? Categories { get; init; }
    [JsonPropertyName("rating")]
    public ConfigRating? Rating { get; init; }
    [JsonPropertyName("specs")]
    public List<string>? Specs { get; init; }
}

[UsedImplicitly]
internal class ConfigData
{
    [JsonPropertyName("showcase_content")]
    public ConfigContent? ShowcaseContent { get; init; }
}

[UsedImplicitly]
internal class ConfigContent
{
    [JsonPropertyName("content")]
    public ConfigContentContent? Content { get; init; }
}

[UsedImplicitly]
internal class ConfigContentContent
{
    [JsonPropertyName("user_collection")]
    public List<ConfigProduct>? UserCollection { get; init; }
}

[UsedImplicitly]
internal class ConfigProduct
{
    [JsonPropertyName("prod_id_key_name")]
    public string? ProdIdKeyName { get; init; }
    [JsonPropertyName("prod_name")]
    public string? ProdName { get; init; }
    [JsonPropertyName("prod_dev_namespace")]
    public string? ProdDevNamespace { get; init; }
    [JsonPropertyName("prod_dev_cover")]
    public string? ProdDevCover { get; init; }
    [JsonPropertyName("prod_dev_image")]
    public string? ProdDevImage { get; init; }
    [JsonPropertyName("prod_slugged_name")]
    public string? ProdSluggedName { get; init; }
}

[UsedImplicitly]
internal class ConfigRating
{
    [JsonPropertyName("avg_rating")]
    public decimal? AvgRating { get; init; }
}
