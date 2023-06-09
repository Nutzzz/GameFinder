using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;
using System;

namespace GameCollector.StoreHandlers.Itch;

/// <summary>
/// Represents a game installed with itch.
/// </summary>
/// <param name="Id"></param>
/// <param name="Title"></param>
/// <param name="Path"></param>
/// <param name="LaunchPath"></param>
/// <param name="OpenUrl"></param>
/// <param name="InstalledAt"></param>
/// <param name="SecondsRun"></param>
/// <param name="IsInstalled"></param>
/// <param name="ShortText"></param>
/// <param name="Classification"></param>
[PublicAPI]
public record ItchGame(ItchGameId Id,
                      string Title,
                      AbsolutePath Path,
                      AbsolutePath LaunchPath = new(),
                      string OpenUrl = "",
                      DateTime? InstalledAt = null,
                      ulong? SecondsRun = null,
                      bool IsInstalled = true,
                      string? ShortText = null,
                      string? Classification = null) :
    GameData(GameId: Id.ToString(),
             Name: Title,
             Path: Path,
             Launch: LaunchPath,
             LaunchUrl: OpenUrl,
             Icon: LaunchPath,
             InstallDate: InstalledAt,
             RunTime: TimeSpan.FromSeconds(SecondsRun ?? 0),
             IsInstalled: IsInstalled,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["Description"] = new() { ShortText ?? "", },
                 ["Genres"] = new() { Classification ?? "", },
             });
