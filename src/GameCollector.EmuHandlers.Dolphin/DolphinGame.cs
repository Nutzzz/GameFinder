using System;
using System.Globalization;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.EmuHandlers.Dolphin;

/// <summary>
/// Represents a ROM for Dolphin Emulator.
/// </summary>
/// <param name="DolphinGameId"></param>
/// <param name="Title"></param>
/// <param name="Path"></param>
/// <param name="DolphinExecutable"></param>
/// <param name="ROMFile"></param>
/// <param name="Cover"></param>
/// <param name="AppLoaderDate"></param>
/// <param name="Publisher"></param>
/// <param name="System"></param>
/// <param name="Region"></param>
[PublicAPI]
public record DolphinGame(DolphinGameId DolphinGameId,
                         string Title,
                         AbsolutePath Path,
                         AbsolutePath DolphinExecutable = new(),
                         string ROMFile = "",
                         AbsolutePath Cover = new(),
                         DateTime? AppLoaderDate = null,
                         string? Publisher = "",
                         DolphinSystem? System = (DolphinSystem)(-1),
                         DolphinRegion? Region = (DolphinRegion)(-1)) :
    GameData(GameId: DolphinGameId.ToString(),
             GameName: Title,
             GamePath: Path,
             Launch: DolphinExecutable,
             LaunchArgs: ROMFile,
             Icon: Cover,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["ReleaseDate"] = new() { AppLoaderDate is null ? "" : ((DateTime)AppLoaderDate).ToString(CultureInfo.InvariantCulture), },
                 ["Publishers"] = new() { Publisher ?? "", },
                 ["System"] = new() { System == (DolphinSystem)(-1) ? "" : System.ToString() ?? "", },
                 ["Region"] = new() { Region == (DolphinRegion)(-1) ? "" : Region.ToString() ?? "", },
             });
