using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.GameJolt;

internal record GamesFile(
    int? Version,
    JsonElement Objects
);

internal record Game(
    ulong? Id,
    string? Title,
    string? Slug,
    MediaItem? HeaderMediaItem,
    MediaItem? ThumbnailMediaItem,
    Developer? Developer
);

internal record MediaItem(
    [property: JsonPropertyName("img_url")]
    string? ImgUrl,
    [property: JsonPropertyName("mediaserver_url")]
    string? MediaServerUrl
);

internal record Developer(
    [property: JsonPropertyName("display_name")]
    string? DisplayName
);
