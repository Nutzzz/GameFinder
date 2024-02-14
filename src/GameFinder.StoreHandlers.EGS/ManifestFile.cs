using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EGS;

[UsedImplicitly]
internal record ManifestFile(
    int? FormatVersion,
    string? LaunchExecutable,
    string? DisplayName,
    string? InstallationGuid,
    string? InstallLocation,
    string? CatalogItemId,
    string? AppName,
    string? MainGameAppName
);
