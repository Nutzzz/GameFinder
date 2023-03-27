using System;
using System.Collections.Generic;
using System.Text.Json;
using GameCollector.SQLiteUtils;

namespace GameCollector.StoreHandlers.Amazon
{
    public class InstallInfo
    {
        [SqlColNameAttribute("Id")]
        public string? Id { get; init; }

        public string? InstallDirectory { get; init; }

        public string? ProductTitle { get; init; }
    }
}
