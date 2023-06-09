using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.WargamingNet;

/// <summary>
/// Represents a game installed with Wargaming.net Game Center.
/// </summary>
/// <param name="AppId"></param>
/// <param name="Name"></param>
/// <param name="InstallLocation"></param>
/// <param name="Executable"></param>
/// <param name="Icon"></param>
/// <param name="Uninstall"></param>
/// <param name="UninstallArgs"></param>
/// <param name="IsInstalled"></param>
[PublicAPI]
public record WargamingNetGame(WargamingNetGameId AppId,
                      string Name,
                      AbsolutePath InstallLocation,
                      AbsolutePath Executable = new(),
                      AbsolutePath Icon = new(),
                      AbsolutePath Uninstall = new(),
                      string UninstallArgs = "",
                      bool IsInstalled = true) :
    GameData(GameId: AppId.ToString(),
             Name: Name,
             Path: InstallLocation,
             Launch: Executable,
             Icon: Icon,
             Uninstall: Uninstall,
             UninstallArgs: UninstallArgs,
             IsInstalled: IsInstalled);
