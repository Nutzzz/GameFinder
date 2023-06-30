using System;
using System.Globalization;
using GameFinder.Common;
using GameFinder.StoreHandlers.Steam.Models;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GameFinder.StoreHandlers.Steam.Services;
using FluentResults;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameFinder.StoreHandlers.Steam;

/// <summary>
/// Represents a game installed with Steam.
/// </summary>
/// <param name="AppId">ID of the game</param>
/// <param name="Name">Name of the game</param>
/// <param name="Path">Absolute path to the game installation folder</param>
/// <param name="CloudSavesDirectory">Absolute path to the cloud saves directory.</param>
/// <param name="DisplayIcon"></param>
/// <param name="IsInstalled"></param>
/// <param name="PlaytimeForever"></param>
/// <param name="IconUrl"></param>
[PublicAPI]
public sealed record SteamGame(SteamGameId AppId,
                        string Name,
                        AbsolutePath Path,
                        AbsolutePath? CloudSavesDirectory,
                        AbsolutePath DisplayIcon = new(),
                        bool IsInstalled = true,
                        TimeSpan PlaytimeForever = new(),
                        string IconUrl = "") :
    GameData(GameId: AppId.ToString(),
             Name: Name,
             Path: Path,
             SavePath: CloudSavesDirectory,
             LaunchUrl: RunGameProtocol + AppId.ToString(),
             Icon: DisplayIcon,
             UninstallUrl: UninstProtocol + AppId.ToString(),
             RunTime: PlaytimeForever,
             IsInstalled: IsInstalled,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["IconUrl"] = new() { IconUrl },
                 ["ImageUrl"] = new() { $"{SteamStaticUrl}{AppId}/library_600x900.jpg" },
                 ["ImageWideUrl"] = new() { $"{SteamStaticUrl}{AppId}/header.jpg" },
             })
{
    internal const string RunGameProtocol = "steam://rungameid/";
    internal const string UninstProtocol = "steam://uninstall/";
    internal const string SteamStaticUrl = "https://cdn.akamai.steamstatic.com/steam/apps/";

    /// <summary>
    /// Gets the parsed <see cref="AppManifest"/> of this game.
    /// </summary>
    public required AppManifest AppManifest { get; init; }

    /// <summary>
    /// Gets the library folder that contains this game.
    /// </summary>
    public required LibraryFolder LibraryFolder { get; init; }

    /// <summary>
    /// Gets the path to the global Steam installation.
    /// </summary>
    public required AbsolutePath SteamPath { get; init; }

    #region Helpers

    /// <inheritdoc cref="Models.AppManifest.AppId"/>
    public AppId AppId => AppManifest.AppId;

    /// <inheritdoc cref="Models.AppManifest.Name"/>
    public string Name => AppManifest.Name;

    /// <summary>
    /// Gets the absolute path to the game's installation directory.
    /// </summary>
    public AbsolutePath Path => AppManifest.GetInstallationDirectoryPath();

    /// <summary>
    /// Gets the absolute path to the cloud saves directory.
    /// </summary>
    public AbsolutePath GetCloudSavesDirectoryPath() => AppManifest.GetUserDataDirectoryPath(SteamPath);

    /// <summary>
    /// Gets the Wine prefix managed by Proton for this game, if it exists.
    /// </summary>
    public ProtonWinePrefix? GetProtonPrefix()
    {
        var protonDirectory = AppManifest.GetCompatabilityDataDirectoryPath();
        if (!protonDirectory.DirectoryExists()) return null;

        var configurationDirectory = protonDirectory.Combine("pfx");
        return new ProtonWinePrefix
        {
            ConfigurationDirectory = configurationDirectory,
            ProtonDirectory = protonDirectory,
        };
    }

    /// <summary>
    /// Uses <see cref="WorkshopManifestParser"/> to parse the workshop manifest
    /// file at <see cref="Models.AppManifest.GetWorkshopManifestFilePath"/>.
    /// </summary>
    /// <seealso cref="WorkshopManifestParser"/>
    [Pure]
    [System.Diagnostics.Contracts.Pure]
    [MustUseReturnValue]
    public Result<WorkshopManifest> ParseWorkshopManifest()
    {
        var workshopManifestFilePath = AppManifest.GetWorkshopManifestFilePath();
        var result = WorkshopManifestParser.ParseManifestFile(workshopManifestFilePath);
        return result;
    }

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public bool Equals(SteamGame? other) => AppManifest.Equals(other?.AppManifest);

    /// <inheritdoc/>
    public override int GetHashCode() => AppManifest.GetHashCode();

    #endregion
}

