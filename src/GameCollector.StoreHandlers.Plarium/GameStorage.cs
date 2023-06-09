using System.Collections.Generic;
using System.Text.Json;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Plarium;

[UsedImplicitly]
internal record GameStorage(
    Dictionary<string, Game> InstalledGames);

[UsedImplicitly]
internal record Game(
    ulong? Id,
    int? IntegrationType,
    string? InstallationPath,
    Dictionary<string, string> InsalledGames // [sic]
);
