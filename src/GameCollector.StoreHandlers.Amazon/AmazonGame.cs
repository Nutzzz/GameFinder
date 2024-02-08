using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Amazon;

/// <summary>
/// Represents a game installed with Amazon Games.
/// </summary>
/// <param name="ProductId"></param>
/// <param name="ProductTitle"></param>
/// <param name="InstallDirectory"></param>
/// <param name="Command"></param>
/// <param name="LaunchUrl"></param>
/// <param name="Icon"></param>
/// <param name="Uninstall"></param>
/// <param name="IsInstalled"></param>
/// <param name="ReleaseDate"></param>
/// <param name="ProductDescription"></param>
/// <param name="ProductIconUrl"></param>
/// <param name="ProductLogoUrl"></param>
/// <param name="Developers"></param>
/// <param name="ProductPublisher"></param>
/// <param name="EsrbRating"></param>
/// <param name="GameModes"></param>
/// <param name="Genres"></param>
[PublicAPI]
public record AmazonGame(AmazonGameId ProductId,
                         string? ProductTitle,
                         AbsolutePath InstallDirectory,
                         AbsolutePath Command = new(),
                         string LaunchUrl = "",
                         AbsolutePath Icon = new(),
                         AbsolutePath Uninstall = new(),
                         bool IsInstalled = true,
                         DateTime? ReleaseDate = null,
                         string? ProductDescription = null,
                         string? ProductIconUrl = null,
                         string? ProductLogoUrl = null,
                         string? Developers = "",
                         string? ProductPublisher = null,
                         EsrbRating EsrbRating = (EsrbRating)(-1),
                         string? GameModes = "",
                         string? Genres = "") :
    GameData(GameId: ProductId.ToString(),
             GameName: ProductTitle ?? "",
             GamePath: InstallDirectory,
             Launch: Command,
             LaunchUrl: LaunchUrl,
             Icon: Icon == default ? Command : Icon,
             Uninstall: Uninstall,
             IsInstalled: IsInstalled,
             Metadata: new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
             {
                 ["ReleaseDate"] = new() { ReleaseDate is null ? "" : ((DateTime)ReleaseDate).ToString(CultureInfo.InvariantCulture), },
                 ["Description"] = new() { ProductDescription ?? "", },
                 ["ImageUrl"] = new() { ProductIconUrl ?? "", },
                 ["LogoUrl"] = new() { ProductLogoUrl ?? "", },
                 ["Developers"] = GetJsonArray(@Developers ?? ""),
                 ["Publishers"] = new() { ProductPublisher ?? "", },
                 ["AgeRating"] = new() { EsrbRating == (EsrbRating)(-1) ? "" : EsrbRating.ToString(), },
                 ["Players"] = GetJsonArray(@GameModes ?? ""),
                 ["Genres"] = GetJsonArray(@Genres ?? ""),
             })
{
    internal static List<string> GetJsonArray(string json)
    {
        List<string> list = new();
        using var doc = JsonDocument.Parse(json, new() { AllowTrailingCommas = true, });
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            list.Add(element.GetString() ?? "");
        }
        return list;
    }
}
