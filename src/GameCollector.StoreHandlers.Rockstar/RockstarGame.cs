using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Rockstar;

/// <summary>
/// Represents a game installed with Rockstar Games Launcher.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="InstallFolder"></param>
/// <param name="Launch"></param>
/// <param name="Uninstall"></param>
/// <param name="UninstallArgs"></param>
[PublicAPI]
public record RockstarGame(RockstarGameId Id,
                      string Name,
                      AbsolutePath InstallFolder,
                      AbsolutePath Launch = new(),
                      AbsolutePath Uninstall = new(),
                      string UninstallArgs = "") :
    GameData(Handler: Handler.StoreHandler_Rockstar,
             GameId: Id.ToString(),
             GameName: Name,
             GamePath: InstallFolder,
             Launch: Launch,
             Icon: Launch,
             Uninstall: Uninstall,
             UninstallArgs: UninstallArgs);
