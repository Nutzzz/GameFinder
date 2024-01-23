using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using GameCollector.DataHandlers.TheGamesDb;
using GameCollector.DataHandlers.TheGamesDb.Properties;
using GameFinder.Common;
using JetBrains.Annotations;
using TheGamesDBApiWrapper.Data.ApiClasses;

namespace GameCollector.DataHandlers.TheGamesDb;

/// <summary>
/// Represents game metadata from TheGamesDB.net.
/// </summary>
/// <param name="Id"></param>
/// <param name="GameTitle"></param>
/// <param name="ReleaseDate"></param>
/// <param name="PlatformId"></param>
/// <param name="PlatformName"></param>
/// <param name="RegionId"></param>
/// <param name="CountryId"></param>
/// <param name="Overview"></param>
/// <param name="Youtube"></param>
/// <param name="Players"></param>
/// <param name="Coop"></param>
/// <param name="Rating"></param>
/// <param name="Developers"></param>
/// <param name="Genres"></param>
/// <param name="Publishers"></param>
/// <param name="Alternates"></param>
/// <param name="BannerUrl"></param>
/// <param name="BoxartUrl"></param>
/// <param name="BoxartBackUrl"></param>
/// <param name="ClearlogoUrl"></param>
/// <param name="FanartUrl"></param>
/// <param name="ScreenshotUrl"></param>
/// <param name="TitlescreenUrl"></param>
/// <param name="HDD"></param>
/// <param name="OS"></param>
/// <param name="Processor"></param>
/// <param name="RAM"></param>
/// <param name="Sound"></param>
/// <param name="Video"></param>
[PublicAPI]
public record TheGamesDbGame(TheGamesDbGameId Id,
                             string? GameTitle = null,
                             List<string>? Alternates = null,
                             DateTime? ReleaseDate = null,
                             int? PlatformId = null,
                             string? PlatformName = null,
                             ushort? RegionId = null,
                             ushort? CountryId = null,
                             string? Overview = null,
                             string? Youtube = null,
                             ushort? Players = null,
                             string? Coop = null,
                             string? Rating = null,
                             List<string>? Developers = null,
                             List<string>? Genres = null,
                             List<string>? Publishers = null,
                             string? BannerUrl = null,
                             string? BoxartUrl = null,
                             string? BoxartBackUrl = null,
                             string? ClearlogoUrl = null,
                             string? FanartUrl = null,
                             string? ScreenshotUrl = null,
                             string? TitlescreenUrl = null,
                             string? HDD = null,
                             string? OS = null,
                             string? Processor = null,
                             string? RAM = null,
                             string? Sound = null,
                             string? Video = null) :
    GameData(GameId: Id.ToString(),
             GameName: GameTitle ?? "",
             GamePath: new(),
             Metadata: new(StringComparer.OrdinalIgnoreCase)
             {
                 ["AlternateNames"] = Alternates ?? new(),
                 ["ReleaseDate"] = new() { ReleaseDate is null ? "" : ((DateTime)ReleaseDate).ToString(CultureInfo.InvariantCulture), },
                 ["Description"] = new() { Overview ?? "", },
                 ["BannerUrl"] = new() { BannerUrl ?? "", },
                 ["BoxartUrl"]  = new() { BoxartUrl ?? "", },
                 ["BoxartBackUrl"] = new() { BoxartBackUrl ?? "", },
                 ["ClearlogoUrl"] = new() { ClearlogoUrl ?? "", },
                 ["FanartUrl"] = new() { FanartUrl ?? "", },
                 ["ScreenshotUrl"] = new() { ScreenshotUrl ?? "", },
                 ["TitlescreenUrl"] = new() { TitlescreenUrl ?? "", },
                 ["Developers"] = Developers ?? new(),
                 ["Publishers"] = Publishers ?? new(),
                 ["AgeRating"] = new() { Rating ?? "" },
                 ["Players"] = new() { Players?.ToString() ?? "" },
                 ["Cooperative"] = new() { Coop is null ? "" : (Coop.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? "Yes" : "") },
                 ["Genres"] = Genres ?? new(),
                 ["Platform"] = new() { PlatformName ?? "" },
                 ["PlatformId"] = new() { PlatformId?.ToString(CultureInfo.InvariantCulture) ?? "" },
                 ["HDD"] = new() { HDD ?? "", },
                 ["OS"] = new() { OS ?? "", },
                 ["Processor"] = new() { Processor ?? "", },
                 ["RAM"] = new() { RAM ?? "", },
                 ["Sound"] = new() { Sound ?? "", },
                 ["Video"] = new() { Video ?? "", },
             });
