using System;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch;

internal record ButlerCaves
{
    [property: SqlColNameAttribute("game_id")]
    public string? GameId { get; init; }

    [property: SqlColNameAttribute("installed_at")]
    public DateTime? InstalledAt { get; init; }

    [property: SqlColNameAttribute("seconds_run")]
    public ulong? SecondsRun { get; init; }

    public string? Verdict { get; init; }

    [property: SqlColNameAttribute("install_folder_name")]
    public string? InstallFolderName { get; init; }
}
