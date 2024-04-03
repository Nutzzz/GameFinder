using System;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch;

internal record ButlerCaves
{
    [property: SqlColName("id")]
    public string? Id { get; init; }

    [property: SqlColName("game_id")]
    public string? GameId { get; init; }

    [property: SqlColName("installed_at")]
    public DateTime? InstalledAt { get; init; }

    [property: SqlColName("seconds_run")]
    public ulong? SecondsRun { get; init; }

    public string? Verdict { get; init; }

    [property: SqlColName("install_folder_name")]
    public string? InstallFolderName { get; init; }
}
