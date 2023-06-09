using System.Text.Json;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.BattleNet;

[UsedImplicitly]
internal record ConfigFile(
    JsonElement Games
);

[UsedImplicitly]
internal record ConfigGame(
    string? LastPlayed
);
