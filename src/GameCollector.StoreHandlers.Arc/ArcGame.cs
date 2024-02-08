using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Arc;

/// <summary>
/// Represents a game installed with Arc.
/// </summary>
/// <param name="AppId"></param>
/// <param name="Name"></param>
/// <param name="InstallPath"></param>
/// <param name="LauncherPath"></param>
/// <param name="Icon"></param>
[PublicAPI]
public record ArcGame(ArcGameId AppId,
                      string Name,
                      AbsolutePath InstallPath,
                      AbsolutePath LauncherPath = new(),
                      AbsolutePath Icon = new()) :
    GameData(GameId: AppId.ToString(),
             GameName: Name,
             GamePath: InstallPath,
             Launch: LauncherPath,
             Icon: Icon);
