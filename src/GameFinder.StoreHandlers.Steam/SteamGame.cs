using System;
using System.Globalization;
using GameFinder.Common;
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
public record SteamGame(SteamGameId AppId,
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
    /// Returns the absolute path of the manifest for this game.
    /// </summary>
    /// <returns></returns>
    public AbsolutePath GetManifestPath()
    {
        var manifestName = $"{AppId.Value.ToString(CultureInfo.InvariantCulture)}.acf";
        return Path.Parent.Parent.CombineUnchecked(manifestName);
    }

    /// <summary>
    /// Returns the absolute path to the Wine prefix directory, managed by Proton.
    /// </summary>
    /// <returns></returns>
    public ProtonWinePrefix GetProtonPrefix()
    {
        var protonDirectory = Path
            .Parent
            .Parent
            .CombineUnchecked("compatdata")
            .CombineUnchecked(AppId.Value.ToString(CultureInfo.InvariantCulture));

        var configurationDirectory = protonDirectory.CombineUnchecked("pfx");
        return new ProtonWinePrefix
        {
            ConfigurationDirectory = configurationDirectory,
            ProtonDirectory = protonDirectory,
        };
    }
}
