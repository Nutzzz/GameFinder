using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.IGClient;

[UsedImplicitly]
internal record ConfigFile(
    ConfigData? Data,
    [property: JsonPropertyName("description_long")]
    string? DescriptionLong,
    [property: JsonPropertyName("description_short")]
    string? DescriptionShort,
    List<string>? Categories,
    ConfigRating? Rating,
    List<string>? Specs,
    List<string>? Tags
);

[UsedImplicitly]
internal record ConfigData(
    [property: JsonPropertyName("showcase_content")]
    ConfigContent? ShowcaseContent
);

[UsedImplicitly]
internal record ConfigContent(
    ConfigContentContent? Content
);

[UsedImplicitly]
internal record ConfigContentContent(
    [property: JsonPropertyName("user_collection")]
    List<ConfigProduct>? UserCollection
);

[UsedImplicitly]
internal record ConfigProduct(
    [property: JsonPropertyName("prod_id_key_name")]
    string? ProdIdKeyName,
    [property: JsonPropertyName("prod_name")]
    string? ProdName,
    [property: JsonPropertyName("prod_dev_namespace")]
    string? ProdDevNamespace,
    [property: JsonPropertyName("prod_dev_cover")]
    string? ProdDevCover,
    [property: JsonPropertyName("prod_dev_image")]
    string? ProdDevImage,
    [property: JsonPropertyName("prod_slugged_name")]
    string? ProdSluggedName
);

[UsedImplicitly]
internal record ConfigRating(
    [property: JsonPropertyName("avg_rating")]
    decimal? AvgRating
);
