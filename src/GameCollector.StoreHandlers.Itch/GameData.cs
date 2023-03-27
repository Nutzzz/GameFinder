using System;
using System.Collections.Generic;
using System.Text.Json;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch
{
    public class GameData
    {
        [SqlColNameAttribute("id")]
        public string? Id { get; init; }

        [SqlColNameAttribute("title")]
        public string? Title { get; init; }

        [SqlColNameAttribute("short_text")]
        public string? ShortText { get; init; }

        [SqlColNameAttribute("classification")]
        public string? Classification { get; init; }

        [SqlColNameAttribute("cover_url")]
        public string? CoverUrl { get; init; }

        [SqlColNameAttribute("still_cover_url")]
        public string? StillCoverUrl { get; init; }
    }
}
