using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.RobotCache;

/// <summary>
/// Represents a game installed with the Robot Cache Client.
/// </summary>
/// <param name="Id"></param>
/// <param name="Title"></param>
/// <param name="InstallPath"></param>
/// <param name="ExecutionPath"></param>
/// <param name="ExecutionParams"></param>
/// <param name="Icon"></param>
[PublicAPI]
public record RobotCacheGame(RobotCacheGameId Id,
                       string Title,
                       AbsolutePath InstallPath,
                       string ExecutionPath,
                       string ExecutionParams,
                       AbsolutePath Icon) :
    GameData(GameId: Id.ToString(),
             GameName: Title,
             GamePath: InstallPath,
             LaunchUrl: $"{ROBOT_URL}{Id}?exePath={ExecutionPath}&params={ExecutionParams}",
             Icon: Icon)
{
    private const string ROBOT_URL = "robotcache://rungameid/";
}
