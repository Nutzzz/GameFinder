using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.RobotCache;

[UsedImplicitly]
internal record StateInfoFile(
    [property: JsonPropertyName("CurrentVersionInfo")]
    JsonElement CurrentVersionInfo,
    [property: JsonPropertyName("Execution")]
    List<Execution>? Execution,
    [property: JsonPropertyName("LatestDownloadedVersionInfo")]
    JsonElement LatestDownloadedVersionInfo,
    [property: JsonPropertyName("State")]
    State? State,
    [property: JsonPropertyName("eula")]
    string? Eula,
    [property: JsonPropertyName("formatVersion")]
    int? FormatVersion,
    [property: JsonPropertyName("gameId")]
    int? GameId
);

internal record Execution(
    [property: JsonPropertyName("depotInfo")]
    JsonElement DepotInfo,
    [property: JsonPropertyName("formatVersion")]
    int? FormatVersion,
    [property: JsonPropertyName("params")]
    string? Params,
    [property: JsonPropertyName("path")]
    string? Path,
    [property: JsonPropertyName("title")]
    string? Title,
    [property: JsonPropertyName("workingDir")]
    string? WorkingDir
);

internal record State(
    [property: JsonPropertyName("currentState")]
    int? CurrentState,
    [property: JsonPropertyName("downloadProgressVisual")]
    int? DownloadProgressVisual,
    [property: JsonPropertyName("firstInstall")]
    bool? FirstInstall,
    [property: JsonPropertyName("previousState")]
    int? PreviousState
);
