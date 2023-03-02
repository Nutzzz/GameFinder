using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Riot;

[UsedImplicitly]
internal class ClientInstallFile
{
    [JsonPropertyName("associated_client")]
    public JsonElement AssociatedClient { get; init; }
    [JsonPropertyName("rc_default")]
    public string? RcDefault { get; init; }
    [JsonPropertyName("rc_live")]
    public string? RcLive { get; init; }
}
