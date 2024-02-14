using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EGS;

[UsedImplicitly]
internal record CatCacheFile(
    List<Catalog>? Catalogs);

[UsedImplicitly]
internal record Catalog(
    [property: JsonPropertyName("id")]
    string? Id,
    [property: JsonPropertyName("namespace")]
    string? Namespace,
    [property: JsonPropertyName("title")]
    string? Title,
    [property: JsonPropertyName("developer")]
    string? Developer,
    [property: JsonPropertyName("keyImages")]
    List<KeyImages>? KeyImages,
    [property: JsonPropertyName("releaseInfo")]
    List<ReleaseInfo>? ReleaseInfo,
    [property: JsonPropertyName("customAttributes")]
    CustomAttributes CustomAttributes,
    [property: JsonPropertyName("mainGameItem")]
    MainGameItem? MainGameItem
);

[UsedImplicitly]
internal record KeyImages(
    [property: JsonPropertyName("type")]
    string? Type,
    [property: JsonPropertyName("url")]
    string? Url
);

[UsedImplicitly]
internal record ReleaseInfo(
    [property: JsonPropertyName("appId")]
    string? AppId
);

[UsedImplicitly]
internal record CustomAttributes(
    [property: JsonPropertyName("CloudSaveFolder")]
    Attribute? CloudSaveFolder
);

[UsedImplicitly]
internal record Attribute(
    [property: JsonPropertyName("value")]
    string? Value
);

[UsedImplicitly]
internal record MainGameItem(
    [property: JsonPropertyName("id")]
    string? Id
);
