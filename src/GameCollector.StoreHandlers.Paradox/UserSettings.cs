using System.Collections.Generic;
using System.Text.Json;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Paradox;

[UsedImplicitly]
internal record UserSettings(
    JsonElement GamesLaunched,
    List<JsonElement> GameLibraryPaths
);

[UsedImplicitly]
internal record UserGame(
    string? GameId,
    string? InstallationPath,
    string? LauncherSettingsDirPath
);
