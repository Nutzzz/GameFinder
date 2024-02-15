using System;
using System.Buffers.Text;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using GameFinder.Common;
using NexusMods.Paths;
using OneOf;
using static System.Environment;

namespace GameCollector.StoreHandlers.EGS;

public partial class EGSHandler : AHandler<EGSGame, EGSGameId>
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(JsonSerializerOptions)} uses {nameof(SourceGenerationContext)} for type information.")]
    private static List<OneOf<EGSGame, ErrorMessage>> ParseCatCacheFile(IFileSystem fileSystem, bool baseOnly = false)
    {
        List<OneOf<EGSGame, ErrorMessage>> games = new();
        var catalogPath = Path.Combine(GetFolderPath(SpecialFolder.CommonApplicationData),
            @"Epic\EpicGamesLauncher\Data\Catalog\catcache.bin");
        if (!File.Exists(catalogPath))
        {
            games.Add(new ErrorMessage($"File does not exist: {catalogPath}"));
            return games;
        }

        try
        {
            Span<byte> byteSpan = File.ReadAllBytes(catalogPath);
            var os = Base64.DecodeFromUtf8InPlace(byteSpan, out var numBytes);
            if (os == OperationStatus.Done)
            {
                byteSpan = byteSpan[..numBytes];
                var plaintext = Encoding.UTF8.GetString(byteSpan);
#if DEBUG
                File.WriteAllText($"egs_catalog.json", plaintext);
#endif
                using var document = JsonDocument.Parse(plaintext, new() { AllowTrailingCommas = true });
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var game = JsonSerializer.Deserialize<Catalog>(element, JsonSerializerOptions);
                    if (game is null)
                    {
                        games.Add(new ErrorMessage($"Unable to deserialize CatCacheFile {catalogPath}"));
                        continue;
                    }

                    var id = "";
                    var space = "";
                    var imageUrl = "";
                    var wideImageUrl = "";
                    var appId = "";
                    var savePath = "";
                    string? baseGame = null;

                    if (game.MainGameItem is not null)
                    {
                        if (baseOnly)
                        {
                            games.Add(new ErrorMessage($"{id} is a DLC"));
                            continue;
                        }
                        baseGame = game.MainGameItem.Id;
                    }

                    space = game.Namespace ?? "";
                    // skip Twinmotion and Unreal Engine
                    if (space.Equals("poodle", StringComparison.OrdinalIgnoreCase) ||
                        space.Equals("ue", StringComparison.OrdinalIgnoreCase))
                        continue;

                    id = game.Id ?? "";
                    if (string.IsNullOrEmpty(id))
                        continue;
                    if (game.ReleaseInfo is not null)
                    {
                        foreach (var info in game.ReleaseInfo)
                        {
                            appId = info.AppId ?? "";
                        }
                    }

                    if (game.CustomAttributes is not null && game.CustomAttributes.CloudSaveFolder is not null)
                        savePath = game.CustomAttributes.CloudSaveFolder.Value ?? "";

                    if (game.KeyImages is not null)
                    {
                        foreach (var image in game.KeyImages)
                        {
                            if (image is not null && image.Type is not null)
                            {
                                imageUrl = image.Type.Equals("DieselGameBoxTall", StringComparison.OrdinalIgnoreCase) ? image.Url ?? "" : "";
                                wideImageUrl = image.Type.Equals("DieselGameBox", StringComparison.OrdinalIgnoreCase) ? image.Url ?? "" : "";
                            }
                        }
                    }

                    games.Add(new EGSGame(
                        CatalogItemId: EGSGameId.From(id),
                        // File seems to be encoded to Base64 improperly, so we need to remove Unicode replacement character
                        DisplayName: (game.Title ?? "").Replace("?", "", StringComparison.Ordinal),
                        InstallLocation: new(),
                        CloudSaveFolder: Path.IsPathRooted(savePath) ? fileSystem.FromUnsanitizedFullPath(savePath) : new(),
                        IsInstalled: false,
                        MainGame: baseGame,
                        ImageTallUrl: imageUrl,
                        ImageUrl: wideImageUrl,
                        Developer: game.Developer ?? "",
                        Namespace: space,
                        AppId: appId));
                }
            }
        }
        catch (Exception e)
        {
            games.Add(new ErrorMessage(e, $"Exception while decoding file {catalogPath}"));
        }
        return games;
    }

    private static IEnumerable<OneOf<EGSGame, ErrorMessage>> GetOwnedGames(
        Dictionary<EGSGameId, OneOf<EGSGame, ErrorMessage>> installedGames,
        IFileSystem fileSystem,
        bool baseOnly = false)
    {
        List<OneOf<EGSGame, ErrorMessage>> allGames = new();

        var ownedGames = ParseCatCacheFile(fileSystem, baseOnly);
        foreach (var game in ownedGames)
        {
            if (game.IsT1)
            {
                allGames.Add(game);
                continue;
            }

            var id = game.AsT0.CatalogItemId;
            if (!installedGames.ContainsKey(id))
            {
                allGames.Add(game);
                continue;
            }
            allGames.Add(new EGSGame(
                CatalogItemId: id,
                DisplayName: installedGames[id].AsT0.DisplayName,
                InstallLocation: installedGames[id].AsT0.InstallLocation,
                CloudSaveFolder: game.AsT0.CloudSaveFolder,
                InstallLaunch: installedGames[id].AsT0.InstallLaunch,
                IsInstalled: true,
                MainGame: game.AsT0.BaseGame,
                ImageTallUrl: game.AsT0.ImageTallUrl,
                ImageUrl: game.AsT0.ImageUrl,
                Developer: game.AsT0.Developer));
        }

        return allGames;
    }
}
