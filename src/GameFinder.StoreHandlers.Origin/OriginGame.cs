using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameFinder.StoreHandlers.Origin;

/// <summary>
/// Represents a game installed with Origin.
/// </summary>
/// <param name="Id"></param>
/// <param name="InstallPath"></param>
[PublicAPI]
public record OriginGame(OriginGameId Id,
                         AbsolutePath InstallPath) :
    GameData(GameId: Id.ToString(),
             Name: InstallPath.GetFileNameWithoutExtension(),
             Path: InstallPath);
