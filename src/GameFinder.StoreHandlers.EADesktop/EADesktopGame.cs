using System;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameFinder.StoreHandlers.EADesktop;

/// <summary>
/// Represents a game installed with the EA Desktop app.
/// </summary>
/// <param name="EADesktopGameId">Id of the game.</param>
/// <param name="Name"></param>
/// <param name="BaseInstallPath">Absolute path to the game folder.</param>
/// <param name="Executable"></param>
/// <param name="UninstallCommand"></param>
/// <param name="UninstallParameters"></param>
/// <param name="IsInstalled"></param>
/// <param name="IsDLC"></param>
/// <param name="BaseSlug">Slug name of the game.</param>
[PublicAPI]
public record EADesktopGame(EADesktopGameId EADesktopGameId,
                            string Name,
                            AbsolutePath BaseInstallPath,
                            AbsolutePath Executable = new(),
                            AbsolutePath UninstallCommand = new(),
                            string UninstallParameters = "",
                            bool IsInstalled = true,
                            bool IsDLC = false,
                            string BaseSlug = "") :
    GameData(GameId: EADesktopGameId.ToString() ?? "",
             Name: Name,
             Path: BaseInstallPath,
             Launch: Executable,
             Icon: Executable,
             Uninstall: UninstallCommand,
             UninstallArgs: UninstallParameters,
             IsInstalled: IsInstalled,
             BaseGame: IsDLC ? (!IsDLC).ToString() : null,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["BaseSlug"] = new() { BaseSlug }
             })
{
    /// <summary>
    /// Returns the absolute path to the <c>installerdata.xml</c> file inside the <c>__Installer</c> folder
    /// of the game folder.
    /// </summary>
    /// <returns></returns>
    public AbsolutePath GetInstallerDataFile()
    {
        return BaseInstallPath
            .Combine("__Installer")
            .Combine("installerdata.xml");
    }
}
