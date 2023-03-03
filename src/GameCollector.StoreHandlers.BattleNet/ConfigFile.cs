using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.BattleNet;

[UsedImplicitly]
internal class ConfigFile
{
    public JsonElement Games { get; init; }
}

[UsedImplicitly]
internal class ConfigGame
{
    public string? LastPlayed { get; init; }
}
