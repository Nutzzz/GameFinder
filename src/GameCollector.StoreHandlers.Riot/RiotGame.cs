using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Riot;

/// <summary>
/// Represents a game installed with the Riot Client.
/// </summary>
/// <param name="ProductId"></param>
/// <param name="Name"></param>
/// <param name="ProductInstallPath"></param>
/// <param name="ClientPath"></param>
/// <param name="LaunchArgs"></param>
/// <param name="Icon"></param>
/// <param name="UninstallArgs"></param>
[PublicAPI]
public record RiotGame(RiotGameId ProductId,
                       string Name,
                       AbsolutePath ProductInstallPath,
                       AbsolutePath ClientPath,
                       string LaunchArgs,
                       AbsolutePath Icon,
                       string UninstallArgs) :
    GameData(GameId: ProductId.ToString(),
             GameName: Name,
             GamePath: ProductInstallPath,
             Launch: ClientPath,
             LaunchArgs: LaunchArgs,
             Icon: Icon,
             Uninstall: ClientPath,
             UninstallArgs: UninstallArgs);
