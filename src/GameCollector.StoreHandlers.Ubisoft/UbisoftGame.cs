using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Ubisoft;

/// <summary>
/// Represents a game installed with Ubisoft Connect.
/// </summary>
/// <param name="GameCode"></param>
/// <param name="DisplayName"></param>
/// <param name="InstallPath"></param>
/// <param name="Executable"></param>
/// <param name="LaunchUrl"></param>
/// <param name="Icon"></param>
/// <param name="Uninstall"></param>
/// <param name="UninstallArgs"></param>
/// <param name="IsInstalled"></param>
/// <param name="IsDLC"></param>
[PublicAPI]
public record UbisoftGame(UbisoftGameId GameCode,
                       string DisplayName,
                       AbsolutePath InstallPath,
                       AbsolutePath Executable = new(),
                       string LaunchUrl = "",
                       AbsolutePath Icon = new(),
                       AbsolutePath Uninstall = new(),
                       string UninstallArgs = "",
                       bool IsInstalled = true,
                       bool IsDLC = false) :
    GameData(Handler: Handlers.StoreHandler_Ubisoft,
             GameId: GameCode.ToString(),
             GameName: DisplayName,
             GamePath: InstallPath,
             Launch: Executable,
             LaunchUrl: LaunchUrl,
             Icon: Icon,
             Uninstall: Uninstall,
             UninstallArgs: UninstallArgs,
             IsInstalled: IsInstalled,
             BaseGame: IsDLC ? (!IsDLC).ToString() : null);
