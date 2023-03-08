using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Humble;

[UsedImplicitly]
internal class ConfigFile
{
    [JsonPropertyName("settings")]
    public ConfigSettings? Settings { get; init; }
    [JsonPropertyName("user")]
    public ConfigUser? User { get; init; }
    [JsonPropertyName("game-collection-4")]
    public List<ConfigGame>? GameCollection4 { get; init; }
}

internal class ConfigSettings
{
    [JsonPropertyName("downloadLocation")]
    public string? DownloadLocation { get; init; }
}

internal class ConfigUser
{
    [JsonPropertyName("owns_active_content")]
    public bool? OwnsActiveContent { get; init; }
    [JsonPropertyName("is_paused")]
    public bool? IsPaused { get; init; }
    [JsonPropertyName("has_perks")]
    public bool? HasPerks { get; init; }
}

internal class ConfigGame
{
    [JsonPropertyName("machineName")]
    public string? MachineName { get; init; }
    [JsonPropertyName("gameName")]
    public string? GameName { get; init; }
    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; init; }
    [JsonPropertyName("status")]
    public string? Status { get; init; }
    [JsonPropertyName("lastPlayed")]
    public string? LastPlayed { get; init; }
    [JsonPropertyName("publishers")]
    public List<ConfigPub>? Publishers { get; init; }
    [JsonPropertyName("developers")]
    public List<ConfigDev>? Developers { get; init; }
    [JsonPropertyName("descriptionText")]
    public string? DescriptionText { get; init; }
    [JsonPropertyName("isAvailable")]
    public bool? IsAvailable { get; init; }
    [JsonPropertyName("downloadMachineName")]
    public string? DownloadMachineName { get; init; }
    [JsonPropertyName("gamekey")]
    public string? Gamekey { get; init; }
    [JsonPropertyName("iconPath")]
    public string? IconPath { get; init; }
    [JsonPropertyName("filePath")]
    public string? FilePath { get; init; }
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; init; }
    [JsonPropertyName("troveCategory")]
    public string? TroveCategory { get; init; }
    [JsonPropertyName("isOwned")]
    public bool? IsOwned { get; init; }
}

internal class ConfigPub
{
    [JsonPropertyName("publisher-name")]
    public string? PublisherName { get; init; }
    [JsonPropertyName("publisher-url")]
    public string? PublisherUrl { get; init; }
}

internal class ConfigDev
{
    [JsonPropertyName("developer-name")]
    public string? DeveloperName { get; init; }
    [JsonPropertyName("developer-url")]
    public string? DeveloperUrl { get; init; }
}
