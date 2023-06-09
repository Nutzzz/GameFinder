using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch;

internal record ButlerGames
{
    public string? Id { get; init; }

    public string? Title { get; init; }

    [property: SqlColNameAttribute("short_text")]
    public string? ShortText { get; init; }

    public string? Classification { get; init; }

    [property: SqlColNameAttribute("cover_url")]
    public string? CoverUrl { get; init; }

    [property: SqlColNameAttribute("still_cover_url")]
    public string? StillCoverUrl { get; init; }
}
