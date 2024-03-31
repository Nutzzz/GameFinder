using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Legacy;

[UsedImplicitly]
internal record AppStateFile(
    AppSettings? Settings,
    SiteData? SiteData
);

[UsedImplicitly]
internal record AppSettings(
    JsonElement GameLibraryPath
);

[UsedImplicitly]
internal record SiteData(
    StoreCategories? StoreCategories,
    List<CatalogItem>? Catalog,
    List<CatalogItem>? GiveawayCatalog
);

[UsedImplicitly]
internal record StoreCategories(
    JsonElement Genres
);

[UsedImplicitly]
internal record CatalogItem(
    List<Category>? Categories,
    List<Tag>? Tags,
    List<Game>? Games
);

[UsedImplicitly]
internal record Category(
    int? Id,
    string? Name
);

[UsedImplicitly]
internal record Tag(
    int? Id,
    string? Name
);

[UsedImplicitly]
internal record Game(
    [property: JsonPropertyName("game_id")]
    string? GameId,
    [property: JsonPropertyName("game_name")]
    string? GameName,
    [property: JsonPropertyName("game_description")]
    string? GameDescription,
    [property: JsonPropertyName("game_coverart")]
    string? GameCoverart,
    [property: JsonPropertyName("installer_uuid")]
    string? InstallerUuid
);
