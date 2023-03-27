using System;
using System.Collections.Generic;
using System.Text.Json;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch
{
    public class CaveData
    {
        [SqlColNameAttribute("game_id")]
        public string? GameId { get; init; }

        [SqlColNameAttribute("installed_at")]
        public DateTime? InstalledAt { get; init; }

        [SqlColNameAttribute("last_touched_at")]
        public DateTime? LastTouchedAt { get; init; }

        [SqlColNameAttribute("verdict")]
        public string? Verdict { get; init; }

        [SqlColNameAttribute("install_folder_name")]
        public string? InstallFolderName { get; init; }
    }
}
