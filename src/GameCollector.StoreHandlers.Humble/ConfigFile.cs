using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Humble;

[UsedImplicitly]
internal record ConfigFile(
    ConfigSettings? Settings,
    ConfigUser? User,
    [property: JsonPropertyName("game-collection-4")]
    List<ConfigGame>? GameCollection4
);

[UsedImplicitly]
internal record ConfigSettings(
    string? DownloadLocation
);

[UsedImplicitly]
internal record ConfigUser(
    [property: JsonPropertyName("owns_active_content")]
    bool? OwnsActiveContent,
    [property: JsonPropertyName("is_paused")]
    bool? IsPaused,
    [property: JsonPropertyName("has_perks")]
    bool? HasPerks
);

[UsedImplicitly]
internal record ConfigGame(
    string? MachineName,
    string? GameName,
    string? Gamekey,
    string? ImagePath,
    string? Status,
    string? LastPlayed,
    List<ConfigPub>? Publishers,
    List<ConfigDev>? Developers,
    string? DescriptionText,
    ConfigCarousel CarouselContent,
    bool? IsAvailable,
    string? IconPath,
    string? DownloadMachineName,
    string? YoutubeLink,
    string? TroveCategory,
    bool? IsOwned,
    string? FilePath,
    string? ExecutablePath
);

[UsedImplicitly]
internal record ConfigPub(
    [property: JsonPropertyName("publisher-name")]
    string? PublisherName,
    [property: JsonPropertyName("publisher-url")]
    string? PublisherUrl
);

[UsedImplicitly]
internal record ConfigDev(
    [property: JsonPropertyName("developer-name")]
    string? DeveloperName,
    [property: JsonPropertyName("developer-url")]
    string? DeveloperUrl
);

[UsedImplicitly]
internal record ConfigCarousel(
    [property: JsonPropertyName("asm-demo-machine-name")]
    List<string>? AsmDemoMachineName,
    [property: JsonPropertyName("youtube-link")]
    List<string>? YoutubeLink,
    List<string>? Thumbnail,
    List<string>? Screenshot
);
