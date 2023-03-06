using System;
using System.Collections.Generic;
using System.Text.Json;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch
{
    public class CaveData
    {
        [SqlColName("game_id")]
        public string? GameId { get; init; }

        [SqlColName("installed_at")]
        public DateTime? InstalledAt { get; init; }

        [SqlColName("last_touched_at")]
        public DateTime? LastTouchedAt { get; init; }

        [SqlColName("verdict")]
        public string? Verdict { get; init; }

        [SqlColName("install_folder_name")]
        public string? InstallFolderName { get; init; }
    }
}
