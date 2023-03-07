using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using JetBrains.Annotations;
using GameCollector.Common;
using static System.Environment;

namespace GameCollector.StoreHandlers.IGClient;

/// <summary>
/// Handler for finding games installed with Indiegala IGClient.
/// Uses json files:
///   %AppData%\IGClient\storage\installed.json
///   %AppData%\IGClient\config.json
/// </summary>
[PublicAPI]
public class IGClientHandler : AHandler<Game, string>
{
    private readonly IFileSystem _fileSystem;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            AllowTrailingCommas = true,
        };

    /// <summary>
    /// Default constructor that uses the real filesystem <see cref="FileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public IGClientHandler() : this(new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the <see cref="IFileSystem"/> implementation to use.
    /// </summary>
    /// <param name="fileSystem"></param>
    public IGClientHandler(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var installFile = _fileSystem.FileInfo.New(
            _fileSystem.Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "IGClient", "storage", "installed.json"));
        if (!installFile.Exists)
        {
            yield return Result.FromError<Game>($"The data file {installFile.FullName} does not exist!");
            yield break;
        }
        var configFile = _fileSystem.FileInfo.New(
            _fileSystem.Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "IGClient", "config.json"));
        if (!configFile.Exists)
        {
            yield return Result.FromError<Game>($"The data file {configFile.FullName} does not exist!");
            yield break;
        }

        List<string> igIds = new();

        using (var streamInst = installFile.OpenRead())
        {
            var installs = JsonSerializer.Deserialize<List<InstGame>>(streamInst, _jsonSerializerOptions);
            if (installs is null || installs.Count == 0)
            {
                yield return Result.FromError<Game>($"Unable to deserialize data file {installFile.FullName}");
                yield break;
            }
            var id = "";
            var slugged = "";
            var name = "";
            var launch = "";
            var launchArgs = "";
            List<string> specs = new();
            var players = 0;
            var rating = 0m;

            var i = 0;
            foreach (var game in installs)
            {
                i++;
                if (game.Path is null ||
                    game.Path.Count == 0)
                {
                    yield return Result.FromError<Game>($"Unable to deserialize \"path\" property in data file {installFile.FullName} for game #{i.ToString(CultureInfo.InvariantCulture)}");
                    continue;
                }
                var path = game.Path[0];
                var target = game.Target;
                if (target is null ||
                    target.ItemData is null ||
                    target.GameData is null)
                {
                    yield return Result.FromError<Game>($"Unable to deserialize \"target\" property in data file {installFile.FullName} for game #{i.ToString(CultureInfo.InvariantCulture)}");
                    continue;
                }
                id = target.ItemData.IdKeyName ?? "";
                igIds.Add(id);
                slugged = target.ItemData.SluggedName ?? "";
                path = _fileSystem.Path.Combine(path, slugged);
                name = target.ItemData.Name ?? "";

                if (!string.IsNullOrEmpty(target.GameData.ExePath))
                {
                    launch = _fileSystem.Path.Combine(path, target.GameData.ExePath);
                    if (!string.IsNullOrEmpty(target.GameData.Args))
                        launchArgs = target.GameData.Args;
                }
                else
                    launch = _fileSystem.Path.Combine(path, FindExe(path, name, _fileSystem));
                specs = target.GameData.Specs ?? new();
                if (specs.Contains("Multi-player", StringComparer.Ordinal) ||
                    specs.Contains("Multiplayer", StringComparer.Ordinal))
                    players = 3;
                else if (specs.Contains("Co-op", StringComparer.Ordinal) ||
                    specs.Contains("Shared/Split Screen", StringComparer.Ordinal))
                    players = 2;
                else if (specs.Contains("Single-player", StringComparer.Ordinal))
                    players = 1;
                if (target.GameData.Rating is not null)
                    rating = target.GameData.Rating.AvgRating ?? 0m;
                yield return Result.FromGame(new Game(
                    Id: id,
                    Name: name,
                    Path: path,
                    Launch: launch,
                    LaunchArgs: launchArgs,
                    Icon: launch,
                    Metadata: new(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Description"] = new() { target.GameData.DescriptionShort ?? "" },
                        ["IconUrl"] = new() { $"https://www.indiegalacdn.com/imgs/devs/{target.ItemData.DevId}/products/{id}/prodmain/{target.ItemData.DevImage}" },
                        ["IconWideUrl"] = new() { $"https://www.indiegalacdn.com/imgs/devs/{target.ItemData.DevId}/products/{id}/prodcover/{target.ItemData.DevCover}" },
                        ["Players"] = new() { players.ToString(CultureInfo.InvariantCulture) },
                        ["Genres"] = target.GameData.Categories ?? new(),
                        ["Rating"] = new() { rating.ToString(CultureInfo.InvariantCulture) },
                    }
                ));
            }
        }

        if (!installedOnly)
        {
            using var streamCfg = configFile.OpenRead();
            var allGames = JsonSerializer.Deserialize<Dictionary<string, ConfigGame>>(streamCfg, _jsonSerializerOptions);
            if (allGames is null ||
                !allGames.ContainsKey("gala_data"))
            {
                yield return Result.FromError<Game>($"Unable to deserialize data file {configFile.FullName}");
                yield break;
            }
            var config = allGames["gala_data"];
            if (config.Data is null ||
                config.Data.ShowcaseContent is null ||
                config.Data.ShowcaseContent.Content is null ||
                config.Data.ShowcaseContent.Content.UserCollection is null)
            {
                yield return Result.FromError<Game>($"Unable to deserialize data file {configFile.FullName}");
                yield break;
            }
            var id = "";
            var slugged = "";
            var name = "";
            var description = "";
            List<string> genres = new();
            //List<string> specs = new();
            var rating = 0m;

            var c = 0;
            var games = config.Data.ShowcaseContent.Content.UserCollection ?? new();
            foreach (var game in games)
            {
                c++;
                id = game.ProdIdKeyName;
                name = game.ProdName;
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                {
                    yield return Result.FromError<Game>($"Unable to deserialize data in file {configFile.FullName} for game #{c.ToString(CultureInfo.InvariantCulture)}");
                    continue;
                }
                if (igIds.Contains(id, StringComparer.Ordinal))
                {
                    yield return Result.FromError<Game>($"{name} was already found!");
                    continue;
                }
                slugged = game.ProdSluggedName ?? "";

                if (allGames is not null &&
                    allGames.TryGetValue(slugged, out var value))
                {
                    var gameData = value;
                    description = gameData.DescriptionShort ?? "";
                    genres = gameData.Categories ?? new();
                    //specs = gameData.Specs ?? new();
                    if (gameData.Rating is not null)
                        rating = gameData.Rating.AvgRating ?? 0m;
                }

                yield return Result.FromGame<Game>(new(
                    Id: id,
                    Name: name,
                    Path: "",
                    IsInstalled: false,
                    Metadata: new(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Description"] = new() { description },
                        ["IconUrl"] = new() { $"https://www.indiegalacdn.com/imgs/devs/{game.ProdDevNamespace}/products/{id}/prodmain/{game.ProdDevImage}" },
                        ["IconWideUrl"] = new() { $"https://www.indiegalacdn.com/imgs/devs/{game.ProdDevNamespace}/products/{id}/prodcover/{game.ProdDevCover}" },
                        ["Genres"] = genres,
                        ["Rating"] = new() { rating.ToString(CultureInfo.InvariantCulture) },
                    }
                ));
            }
        }
    }

    //TODO: Explore ways of making FindExe() better
    private static string FindExe(string path, string name, IFileSystem fileSystem)
    {
        var exe = "";
        var exes = fileSystem.Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories).ToList();
        if (exes.Count == 1)
            exe = exes[0];
        else
        {
            var j = 0;
            foreach (var file in exes)
            {
                j++;
                var tmpFile = fileSystem.Path.GetFileName(file);
                List<string> badNames = new()
                {
                    "unins", "install", "patch", "redist", "prereq", "dotnet", "setup", "config", "w9xpopen", "edit", "help",
                    "python", "server", "service", "cleanup", "anticheat", "touchup", "error", "crash", "report", "helper", "handler",
                };
                List<string> goodNames = new()
                {
                    //"launch", "scummvm",
                };
                foreach (var badName in badNames)
                {
                    if (tmpFile.Contains(badName, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                var nameSanitized = string.Concat(name.Split(fileSystem.Path.GetInvalidFileNameChars()));
                var nameAlphanum = name.Where(c => c == 32 || (char.IsLetterOrDigit(c) && c < 128)).ToString();
                if (tmpFile.Contains(nameSanitized, StringComparison.OrdinalIgnoreCase) ||
                    tmpFile.Contains(nameSanitized.Replace(' ', '-'), StringComparison.OrdinalIgnoreCase) ||
                    tmpFile.Contains(nameSanitized.Replace(' ', '_'), StringComparison.OrdinalIgnoreCase) ||
                    tmpFile.Contains(nameSanitized.Remove(' '), StringComparison.OrdinalIgnoreCase) ||
                    (nameAlphanum is not null &&
                    (tmpFile.Contains(nameAlphanum, StringComparison.OrdinalIgnoreCase) ||
                    tmpFile.Contains(nameAlphanum.Replace(' ', '-'), StringComparison.OrdinalIgnoreCase) ||
                    tmpFile.Contains(nameAlphanum.Replace(' ', '_'), StringComparison.OrdinalIgnoreCase) ||
                    tmpFile.Contains(nameAlphanum.Remove(' '), StringComparison.OrdinalIgnoreCase))))
                {
                    exe = file;
                    break;
                }
                foreach (var goodName in goodNames)
                {
                    if (tmpFile.Contains(goodName, StringComparison.OrdinalIgnoreCase))
                    {
                        exe = file;
                        break;
                    }
                }
                exe = file;
            }
        }
        return exe;
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }
}
