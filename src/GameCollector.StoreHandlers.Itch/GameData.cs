using System;
using System.Collections.Generic;
using System.Text.Json;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Itch
{
    public class GameData
    {
        [SqlColName("id")]
        public string? Id { get; init; }

        [SqlColName("title")]
        public string? Title { get; init; }

        [SqlColName("short_text")]
        public string? ShortText { get; init; }

        [SqlColName("classification")]
        public string? Classification { get; init; }

        [SqlColName("cover_url")]
        public string? CoverUrl { get; init; }

        [SqlColName("still_cover_url")]
        public string? StillCoverUrl { get; init; }
    }
}
