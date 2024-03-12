using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Origin;

/// <summary>
/// Represents a game installed with Origin.
/// </summary>
/// <param name="Id"></param>
/// <param name="InstallPath"></param>
[PublicAPI]
public record OriginGame(OriginGameId Id,
                         AbsolutePath InstallPath) :
    GameData(Handler: Handlers.StoreHandler_Origin,
             GameId: Id.ToString(),
             GameName: InstallPath.GetFileNameWithoutExtension(),
             GamePath: InstallPath);
