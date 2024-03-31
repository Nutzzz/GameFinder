namespace GameCollector.StoreHandlers.Amazon;

internal record ProductInfo
{
    public string? ProductDescription { get; init; }

    public string? ProductIconUrl { get; init; }

    public string? ProductIdStr { get; init; }

    public string? ProductPublisher { get; init; }

    public string? ProductTitle { get; init; }

    public string? ScreenshotsJson { get; init; }

    public string? VideosJson { get; init; }

    public string? DevelopersJson { get; init; }

    public string? EsrbRating { get; init; }

    public string? GameModesJson { get; init; }

    public string? GenresJson { get; init; }

    public string? PegiRating { get; init; }

    public string? ProductLogoUrl { get; init; }

    public string? ReleaseDate { get; init; }

    public string? UskRating { get; init; }
}
