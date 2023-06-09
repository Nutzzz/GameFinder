using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCollector.StoreHandlers.GameJolt;

internal record PackagesFile(
    int? Version,
    JsonElement Objects
);

internal record Package(
    ulong? Id,
    [property: JsonPropertyName("install_dir")]
    string? InstallDir,
    [property: JsonPropertyName("game_id")]
    ulong? GameId,
    [property: JsonPropertyName("launch_options")]
    List<LaunchOption> LaunchOptions
);

internal record LaunchOption(
    string? Os,
    [property: JsonPropertyName("executable_path")]
    string? ExecutablePath
);
