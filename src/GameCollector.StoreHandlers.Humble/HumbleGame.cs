using System;
using System.Collections.Generic;
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
                         List<string>? Developers = null,
                         List<string>? Publishers = null) :
    GameData(GameId: HumbleGameId.ToString(),
             GameName: GameName,
             GamePath: FilePath,
             Launch: ExecutablePath,
             LaunchUrl: LaunchUrl,
             Icon: ExecutablePath,
             UninstallUrl: UninstallUrl,
             LastRunDate: LastPlayed,
             IsInstalled: IsInstalled,
             HasProblem: IsExpired,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["Description"] = new() { DescriptionText ?? "", },
                 ["ImageUrl"] = new() { IconPath ?? "", },
                 ["ImageWideUrl"] = new() { ImagePath ?? "", },
                 ["MachineName"] = new() { MachineName ?? "", },
                 ["Developers"] = Developers ?? new(),
                 ["Publishers"] = Publishers ?? new(),
             });
