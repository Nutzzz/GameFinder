using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using GameCollector.Common;
using GameCollector.SQLiteUtils;
using static System.Environment;

namespace GameCollector.StoreHandlers.Itch;

/// <summary>
/// Handler for finding games installed with itch.
/// </summary>
[PublicAPI]
public class ItchHandler : AHandler<Game, string>
{
    internal const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

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
    public ItchHandler() : this(new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the <see cref="IFileSystem"/> implementation to use.
    /// </summary>
    /// <param name="fileSystem"></param>
    public ItchHandler(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var database = _fileSystem.Path.Combine(GetDatabaseFilePath(_fileSystem));
        if (!_fileSystem.File.Exists(database))
        {
            yield return Result.FromError<Game>($"The database file {database} does not exist!");
            yield break;
        }

        foreach (var game in ParseDatabase(database, installedOnly))
        {
            yield return game;
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    private IEnumerable<Result<Game>> ParseDatabase(string database, bool installedOnly = false)
    {
        var games = SQLiteHelpers.GetDataTable(database, "SELECT * FROM games;").ToList<GameData>();
        var caves = SQLiteHelpers.GetDataTable(database, "SELECT * FROM caves;").ToList<CaveData>();
        if (games is null)
        {
            yield return Result.FromError<Game>($"Could not deserialize file {database}");
            yield break;
        }

        foreach (var game in games)
        {
            var id = game.Id;
            if (id is null)
            {
                yield return Result.FromError<Game>($"Value for \"id\" does not exist in table games in file {database}");
                continue;
            }
            var name = game.Title ?? "";
            var type = game.Classification;
            if (type is not null &&
                type.Equals("assets", StringComparison.OrdinalIgnoreCase))  // no "assets", just "game" or "tool"
            {
                yield return Result.FromError<Game>($"{name} is an asset (not a game)!");
                continue;
            }

            var path = "";
            var launch = "";
            var icon = "";
            var lastRun = DateTime.MinValue;
            var isInstalled = false;

            if (caves is not null)
            {
                (var error, path, launch, icon, lastRun, isInstalled) = ParseCavesForId(caves, id, name);
                if (error is not null)
                {
                    yield return Result.FromError<Game>(error);
                    continue;
                }
            }

            if (!isInstalled)
            {
                if (installedOnly)
                    continue;
                launch = "itch://games/" + id;
            }

            yield return Result.FromGame<Game>(new(
                Id: id,
                Name: name ?? "",
                Path: path ?? "",
                Launch: launch,
                Icon: icon,
                LastRunDate: lastRun,
                IsInstalled: isInstalled,
                Metadata: new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Description"] = new() { game.ShortText ?? "" },
                    ["Classification"] = new() { game.ShortText ?? "" },
                }));
        }
    }

    private (string? error, string path, string launch, string icon, DateTime lastRun, bool isInstalled)
        ParseCavesForId(List<CaveData> caves, string id, string name)
    {
        try
        {
            var path = "";
            var launch = "";
            var icon = "";
            var lastRun = DateTime.MinValue;
            var isInstalled = false;
            foreach (var install in caves)
            {
                if (id.Equals(install.GameId, StringComparison.OrdinalIgnoreCase))
                {
                    isInstalled = true;
                    //path = install.InstallFolderName;
                    if (install.Verdict is not null)
                    {
                        var verdict = JsonSerializer.Deserialize<Verdict>(install.Verdict, _jsonSerializerOptions);
                        if (verdict is not null)
                        {
                            if (verdict.BasePath is not null &&
                                verdict.Candidates is not null)
                            {
                                path = verdict.BasePath;
                                foreach (var candidate in verdict.Candidates)
                                {
                                    if (candidate.Path is not null)
                                    {
                                        launch = @$"{path}\{candidate.Path}";
                                        icon = launch;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (install.LastTouchedAt is not null)
                        lastRun = install.LastTouchedAt ?? DateTime.MinValue;
                    else if (install.InstalledAt is not null)
                        lastRun = install.InstalledAt ?? DateTime.MinValue;
                    return (null, path, launch, icon, lastRun, isInstalled);
                }
            }
        }
        catch (Exception e)
        {
            return ($"Exception while parsing table \"caves\" for {name} [{id}]\n{e.Message}\n{e.InnerException}", "", "", "", DateTime.MinValue, false);
        }
        return (null, "", "", "", DateTime.MinValue, false);
    }

    internal static string GetDatabaseFilePath(IFileSystem fileSystem)
    {
        return fileSystem.Path.Combine(
            GetFolderPath(SpecialFolder.ApplicationData),
            "itch",
            "db",
            "butler.db");
    }
}
