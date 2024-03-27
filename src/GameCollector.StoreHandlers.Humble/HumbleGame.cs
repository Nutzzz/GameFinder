using System;
using System.Collections.Generic;
using System.Linq;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Humble;

/// <summary>
/// Represents a game installed with Humble App.
/// </summary>
/// <param name="HumbleGameId"></param>
/// <param name="GameName"></param>
/// <param name="FilePath"></param>
/// <param name="ExecutablePath"></param>
/// <param name="LaunchUrl"></param>
/// <param name="UninstallUrl"></param>
/// <param name="LastPlayed"></param>
/// <param name="IsInstalled"></param>
/// <param name="IsExpired"></param>
/// <param name="DescriptionText"></param>
/// <param name="IconPath"></param>
/// <param name="ImagePath"></param>
/// <param name="MachineName"></param>
/// <param name="Developers"></param>
/// <param name="Publishers"></param>
[PublicAPI]
public record HumbleGame(HumbleGameId HumbleGameId,
                         string GameName,
                         AbsolutePath FilePath,
                         AbsolutePath ExecutablePath = new(),
                         string LaunchUrl = "",
                         string UninstallUrl = "",
                         DateTime? LastPlayed = null,
                         bool IsInstalled = true,
                         bool IsExpired = false,
                         string? DescriptionText = null,
                         string? IconPath = null,
                         string? ImagePath = null,
                         string? MachineName = null,
                         IList<string>? Developers = null,
                         IList<string>? Publishers = null) :
    GameData(Handler: Handler.StoreHandler_Humble,
             GameId: HumbleGameId.ToString(),
             GameName: GameName,
             GamePath: FilePath,
             Launch: ExecutablePath,
             LaunchUrl: LaunchUrl,
             Icon: ExecutablePath,
             UninstallUrl: UninstallUrl,
             LastRunDate: LastPlayed,
             IsInstalled: IsInstalled,
             Problems: IsExpired ? new List<Problem>() { Problem.ExpiredTrial } : null,
             Metadata: new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
             {
                 ["Description"] = new() { DescriptionText ?? "", },
                 ["ImageUrl"] = new() { IconPath ?? "", },
                 ["ImageWideUrl"] = new() { ImagePath ?? "", },
                 ["MachineName"] = new() { MachineName ?? "", },
                 ["Developers"] = Developers?.ToList<string>() ?? new List<string>(),
                 ["Publishers"] = Publishers?.ToList<string>() ?? new List<string>(),
             });
