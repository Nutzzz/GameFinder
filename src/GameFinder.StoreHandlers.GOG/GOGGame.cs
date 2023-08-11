using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GameFinder.StoreHandlers.GOG;

/// <summary>
/// Represents a game installed with GOG Galaxy.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Path"></param>
/// <param name="Launch"></param>
/// <param name="LaunchParam"></param>
/// <param name="LaunchUrl"></param>
/// <param name="Exe"></param>
/// <param name="UninstallCommand"></param>
/// <param name="InstallDate"></param>
/// <param name="LastPlayedDate"></param>
/// <param name="IsInstalled"></param>
/// <param name="IsHidden"></param>
/// <param name="Tags"></param>
/// <param name="MyRating"></param>
/// <param name="ReleaseDate"></param>
/// <param name="BoxArtUrl"></param>
/// <param name="LogoUrl"></param>
/// <param name="IconUrl"></param>
[PublicAPI]
public record GOGGame(GOGGameId Id,
                      string Name,
                      AbsolutePath Path,
                      AbsolutePath Launch = new(),
                      string LaunchParam = "",
                      string LaunchUrl = "",
                      AbsolutePath Exe = new(),
                      AbsolutePath UninstallCommand = new(),
                      DateTime? InstallDate = null,
                      DateTime? LastPlayedDate = null,
                      bool IsInstalled = true,
                      bool IsHidden = false,
                      List<string>? Tags = null,
                      ushort? MyRating = null,
                      DateTime? ReleaseDate = null,
                      string BoxArtUrl = "",
                      string LogoUrl = "",
                      string IconUrl = "") :
    GameData(GameId: Id.ToString() ?? "",
             GameName: Name,
             GamePath: Path,
             Launch: Launch,
             LaunchArgs: LaunchParam,
             LaunchUrl: LaunchUrl,
             Icon: Exe,
             Uninstall: UninstallCommand,
             InstallDate: InstallDate,
             LastRunDate: LastPlayedDate,
             IsInstalled: IsInstalled,
             IsHidden: IsHidden,
             Tags: Tags,
             MyRating: MyRating,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["ReleaseDate"] = new() { ReleaseDate is null ? "" : ((DateTime)ReleaseDate).ToString(CultureInfo.InvariantCulture) },
                 ["ImageUrl"] = new() { BoxArtUrl },
                 ["ImageWideUrl"] = new() { LogoUrl },
                 ["IconUrl"] = new() { IconUrl },
             });
