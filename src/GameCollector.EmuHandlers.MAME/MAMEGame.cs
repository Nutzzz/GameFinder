using System;
using System.Collections.Generic;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.EmuHandlers.MAME;

/// <summary>
/// Represents a ROM for MAME Emulator.
/// </summary>
/// <param name="GameName"></param>
/// <param name="Description"></param>
/// <param name="Path"></param>
/// <param name="MAMEExecutable"></param>
/// <param name="CommandLineArgs"></param>
/// <param name="Icon"></param>
/// <param name="IsAvailable"></param>
/// <param name="HasProblem"></param>
/// <param name="Parent"></param>
/// <param name="Year"></param>
/// <param name="Manufacturer"></param>
/// <param name="Categories"></param>
/// <param name="IsMature"></param>
/// <param name="Players"></param>
/// <param name="DriverStatus"></param>
/// <param name="DisplayType"></param>
/// <param name="DisplayRotation"></param>
/// <param name="VersionAdded"></param>
[PublicAPI]
public record MAMEGame(MAMEGameId GameName,
                         string Description,
                         AbsolutePath Path,
                         AbsolutePath MAMEExecutable = new(),
                         string CommandLineArgs = "",
                         AbsolutePath Icon = new(),
                         bool IsAvailable = true,
                         bool HasProblem = false,
                         string? Parent = null,
                         string? Year = null,
                         string? Manufacturer = null,
                         List<string>? Categories = null,
                         bool IsMature = false,
                         string? Players = null,
                         string? DriverStatus = null,
                         string? DisplayType = null,
                         string? DisplayRotation = null,
                         string? VersionAdded = null) :
    GameData(GameId: GameName.ToString() ?? "",
             Name: Description,
             Path: Path,
             Launch: MAMEExecutable,
             LaunchArgs: (GameName.ToString() ?? "") + (string.IsNullOrEmpty(CommandLineArgs) ? "" : " " + CommandLineArgs),
             Icon: Icon,
             IsInstalled: IsAvailable,
             HasProblem: HasProblem,
             BaseGame: Parent,
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["ReleaseDate"] = new() { Year ?? "", },
                 ["Manufacturer"] = new() { Manufacturer ?? "", },
                 ["Genres"] = Categories is null ? new() : Categories,
                 ["AgeRating"] = new() { IsMature ? "adults_only" : "", },
                 ["Players"] = new() { Players ?? "", },
                 ["DriverStatus"] = new() { DriverStatus ?? "", },
                 ["DisplayType"] = new() { DisplayType ?? "", },
                 ["DisplayRotation"] = new() { DisplayRotation ?? "", },
                 ["VersionAdded"] = new() { VersionAdded ?? "", },
             });
