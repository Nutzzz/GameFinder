using System.Collections.Generic;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Paradox;

[UsedImplicitly]
internal record LauncherSettings(
    string? GameId,
    string? ExePath,
    string? GameDataPath,
    bool? IsFallbackSettingsFile
);
