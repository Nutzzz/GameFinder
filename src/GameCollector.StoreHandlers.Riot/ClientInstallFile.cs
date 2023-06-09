using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Riot;

[UsedImplicitly]
internal record ClientInstallFile(
    [property: JsonPropertyName("associated_client")]
    JsonElement AssociatedClient,
    [property: JsonPropertyName("rc_default")]
    string? RcDefault,
    [property: JsonPropertyName("rc_live")]
    string? RcLive
);
