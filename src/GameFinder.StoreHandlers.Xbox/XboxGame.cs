using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;
using System;

namespace GameFinder.StoreHandlers.Xbox;

/// <summary>
/// Represents a game installed with Xbox Game Pass.
/// </summary>
/// <param name="Id"></param>
/// <param name="DisplayName"></param>
/// <param name="Path"></param>
/// <param name="Logo"></param>
/// <param name="Description"></param>
/// <param name="Publisher"></param>
[PublicAPI]
public record XboxGame(XboxGameId Id,
                       string DisplayName,
                       AbsolutePath Path,
                       AbsolutePath Logo = new(),
                       string? Description = null,
                       string? Publisher = null) :
    GameData(GameId: Id.ToString(),
             GameName: DisplayName,
             GamePath: Path,
             Icon: Logo,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["Description"] = new() { Description ?? "" },
                 ["Publishers"] = new() { Publisher ?? "" },
             });
