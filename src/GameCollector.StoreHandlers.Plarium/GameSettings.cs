using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Plarium;

[UsedImplicitly]
internal record GameSettings(
    string? CompanyName,
    string? ProductName);
