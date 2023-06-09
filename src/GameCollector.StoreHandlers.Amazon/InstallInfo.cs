using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Amazon;

internal record InstallInfo
{
    [property: SqlColNameAttribute("Id")]
    public string? Id { get; init; }

    public string? InstallDirectory { get; init; }

    public string? ProductTitle { get; init; }
}
