using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.RobotCache;

[UsedImplicitly]
internal record AppConfigFile(
    [property: JsonPropertyName("formatVersion")]
    int? FormatVersion,
    [property: JsonPropertyName("last_login")]
    JsonElement LastLogin,
    [property: JsonPropertyName("libraries")]
    List<string>? Libraries
);
