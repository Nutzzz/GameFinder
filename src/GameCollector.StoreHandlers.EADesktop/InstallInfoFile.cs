using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EADesktop;

[UsedImplicitly]
internal class InstallInfoFile
{
    public List<InstallInfo>? InstallInfos { get; init; }
    public Schema? Schema { get; init; }
}

[UsedImplicitly]
internal class InstallInfo
{
    public string? BaseInstallPath { get; init; }
    public string? BaseSlug { get; init; }
    [JsonPropertyName("dlcSubPath")]
    public string? DLCSubPath { get; init; }
    public string? InstallCheck { get; init; }
    [JsonPropertyName("softwareId")]
    public string? SoftwareID { get; init; }
    public string? ExecutableCheck { get; init; }
    public JsonElement LocalUninstallProperties { get; init; }
}

[UsedImplicitly]
internal class Schema
{
    public int Version { get; init; }
}
