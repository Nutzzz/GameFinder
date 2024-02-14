using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EADesktop;

[UsedImplicitly]
internal record InstallInfoFile(
    List<InstallInfo>? InstallInfos,
    Schema? Schema);

[UsedImplicitly]
internal record InstallInfo(
    string? BaseInstallPath,
    string? BaseSlug,
    [property: JsonPropertyName("dlcSubPath")]
    string? DLCSubPath,
    string? InstallCheck,
    [property: JsonPropertyName("softwareId")]
    string? SoftwareId,
    string? ExecutableCheck,
    string? ExecutablePath,
    LocalUninstallProperties? LocalUninstallProperties
);

[UsedImplicitly]
internal record Schema(int Version);

[UsedImplicitly]
internal record LocalUninstallProperties(
    string? UninstallCommand,
    string? UninstallParameters
);
