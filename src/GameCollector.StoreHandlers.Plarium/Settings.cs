using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Plarium;

[UsedImplicitly]
internal record Settings(
    string? CompanyName,
    string? ProductName);
