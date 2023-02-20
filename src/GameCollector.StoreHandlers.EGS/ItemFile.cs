using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EGS;

[UsedImplicitly]
internal class ItemFile
{
    public Schema? Schema { get; init; }
    public string? LaunchExecutable { get; init; }
    public string? DisplayName { get; init; }
    [JsonPropertyName("InstallationGuid")]
    public string? InstallationGuid { get; init; }
    public string? InstallLocation { get; init; }
    public string? CatalogItemId { get; init; }
    public string? AppName { get; init; }
    public string? MainGameAppName { get; init; }
}

[UsedImplicitly]
internal class Schema
{
    public int FormatVersion { get; init; }
}
