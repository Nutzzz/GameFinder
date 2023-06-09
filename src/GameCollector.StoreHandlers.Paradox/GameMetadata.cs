using System.Collections.Generic;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Paradox;

[UsedImplicitly]
internal record GameMetadata(
    Data? Data
);

[UsedImplicitly]
internal record Data(
    List<MetaGame>? Games
);

[UsedImplicitly]
internal record MetaGame(
    string? Id,
    string? Name,
    string? ExeArgs,
    string? ExePath,
    ThemeSettings? ThemeSettings
);

[UsedImplicitly]
internal record ThemeSettings(
    string? AppIcon,
    string? AppTaskbarIcon,
    string? Background,
    string? Logo
);
