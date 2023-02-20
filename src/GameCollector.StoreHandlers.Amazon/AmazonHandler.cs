using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using JetBrains.Annotations;
using static System.Environment;

namespace GameCollector.StoreHandlers.Amazon;

/// <summary>
/// Handler for finding games installed with Amazon Games.
/// </summary>
[PublicAPI]
public class AmazonHandler : AHandler<Game, string>
{
    internal const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation of
    /// <see cref="IRegistry"/> and the real file system with <see cref="FileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public AmazonHandler() : this(new WindowsRegistry(), new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>. This uses
    /// the real file system with <see cref="FileSystem"/>.
    /// </summary>
    /// <param name="registry"></param>
    public AmazonHandler(IRegistry registry) : this(registry, new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/> and
    /// <see cref="IFileSystem"/> when doing tests.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public AmazonHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    private readonly JsonDocumentOptions _jsonDocumentOptions =
        new()
        {
            AllowTrailingCommas = true,
        };

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var prodDb = _fileSystem.Path.Combine(GetDatabasePath(_fileSystem), "GameProductInfo.sqlite");
        var instDb = _fileSystem.Path.Combine(GetDatabasePath(_fileSystem), "GameInstallInfo.sqlite");
        if (!File.Exists(prodDb))
        {
            yield return Result.FromError<Game>($"The database file {prodDb} does not exist!");
            yield break;
        }
        if (!File.Exists(instDb))
        {
            yield return Result.FromError<Game>($"The database file {instDb} does not exist!");
            yield break;
        }

        foreach (var game in ParseDatabase(prodDb, instDb, installedOnly).OnlyGames())
        {
            yield return Result.FromGame(game);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    private IEnumerable<Result<Game>> ParseDatabase(string prodDb, string instDb, bool installedOnly = false)
    {
        List<Result<Game>> games = new();
        try
        {
            using SQLiteConnection prodCon = new($"Data source={prodDb}");
            using SQLiteConnection instCon = new($"Data source={instDb}");
            prodCon.Open();
            instCon.Open();
            using SQLiteCommand prodCmd = new("SELECT " +
                "ProductDescription, " +    // (0)
                "ProductIconUrl, " +        // (1)
                "ProductIdStr, " +          // (2)
                "ProductPublisher, " +      // (3)
                "ProductTitle, " +          // (4)
                "DevelopersJson, " +        // (5)
                "EsrbRating, " +            // (6)
                "GameModesJson, " +         // (7)
                "GenresJson, " +            // (8)
                //"PegiRating, " +
                "ProductLogoUrl, " +        // (9)
                "ReleaseDate " +            // (10)
                //"UskRating " +
                "FROM DbSet;", prodCon);
            using SQLiteCommand instCmd = new("SELECT " +
                "Id, " +                    // (0)
                "InstallDirectory, " +      // (1)
                "ProductTitle " +           // (2)
                "FROM DbSet;", instCon);
            using SQLiteDataReader prodRdr = prodCmd.ExecuteReader();
            using SQLiteDataReader instRdr = instCmd.ExecuteReader();
            
            while (prodRdr.Read())
            {
                string path = "";
                string launch = "";
                string icon = "";
                string uninst = "";
                string id = prodRdr.GetString(2);
                while (instRdr.Read())
                {
                    if (instRdr.GetString(0).Equals(id, StringComparison.OrdinalIgnoreCase))
                    {
                        path = instRdr.GetString(1);
                        if (installedOnly && string.IsNullOrEmpty(path))
                        {
                            games.Add(Result.FromError<Game>($"GameInstallInfo for {id} does not have the value \"InstallDirectory\""));
                            break;
                        }
                        launch = "amazon-games://play/" + id;
                        icon = ParseFuelFile(path);
                        Result<Game> regGame = ParseRegistryForId(_registry, id);
                        if (regGame.Game is not null)
                        {
                            if (string.IsNullOrEmpty(icon))
                                icon = regGame.Game.Icon;
                            uninst = regGame.Game.Uninstall;
                        }
                    }
                }
                
                Game game = new(
                    Id: id,
                    Name: prodRdr.GetString(4),
                    Path: path,
                    Launch: launch,
                    Icon: icon,
                    Uninstall: uninst,
                    Metadata: new(StringComparer.Ordinal)
                    {
                        ["Description"] = new() { prodRdr.GetString(0) },
                        ["IconUrl"] = new() { prodRdr.GetString(1) },
                        ["Publisher"] = new() { prodRdr.GetString(3) },
                        // should massage the AgeRating to get a consistent format?
                        ["AgeRating"] = new() { prodRdr.GetString(6) },
                        ["Developers"] = GetJsonArray(@prodRdr.GetString(5)),
                        ["Players"] = GetJsonArray(@prodRdr.GetString(7)),
                        ["Genres"] = GetJsonArray(@prodRdr.GetString(8)),
                        ["ReleaseDate"] = new() { prodRdr.GetString(10) },
                        // should massage the ReleaseDate format (e.g., "2020-04-03T24:00:00Z") to get a valid DateTime.ToString?
                        //["ReleaseDate"] = new() { prodRdr.GetDateTime(10).ToString(CultureInfo.InvariantCulture) },
                    });
                games.Add(Result.FromGame(game));
            }
            return games;
        }
        catch (Exception e)
        {
            return new[] { Result.FromException<Game>("Exception looking for Amazon games in database", e) };
        }
    }

    private IEnumerable<Result<Game>> ParseRegistry(IRegistry registry)
    {
        try
        {
            var currentUser = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

            using var unKey = currentUser.OpenSubKey(UninstallRegKey);
            if (unKey is null)
            {
                return new[]
                {
                    Result.FromError<Game>($"Unable to open HKEY_CURRENT_USER\\{UninstallRegKey}"),
                };
            }

            var subKeyNames = unKey.GetSubKeyNames().Where(
                keyName => keyName[(keyName.LastIndexOf('\\') + 1)..].StartsWith("AmazonGames/", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (subKeyNames.Length == 0)
            {
                return new[]
                {
                    Result.FromError<Game>($"Registry key {unKey.GetName()} has no sub-keys beginning with \"AmazonGames/\""),
                };
            }

            return subKeyNames
                .Select(subKeyName => ParseSubKey(unKey, subKeyName))
                .ToArray();
        }
        catch (Exception e)
        {
            return new[] { Result.FromException<Game>("Exception looking for Amazon games in registry", e) };
        }
    }

    private static Result<Game> ParseRegistryForId(IRegistry registry, string id)
    {
        try
        {
            var currentUser = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            
            using var unKey = currentUser.OpenSubKey(UninstallRegKey);
            if (unKey is null)
            {
                Result.FromError<Game>($"Unable to open HKEY_CURRENT_USER\\{UninstallRegKey}");
            }
            else
            {
                var subKeyNames = unKey.GetSubKeyNames().Where(
                    keyName => keyName[(keyName.LastIndexOf('\\') + 1)..].StartsWith("AmazonGames/", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (subKeyNames.Length == 0)
                {
                    Result.FromError<Game>($"Registry key {unKey.GetName()} has no sub-keys beginning with \"AmazonGames/\"");
                }

                foreach (var subKeyName in subKeyNames)
                {
                    var game = ParseSubKey(unKey, subKeyName, id);
                    if (game.Game is not null)
                    {
                        return game;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Result.FromException<Game>("Exception looking for Amazon games in registry", e);
        }
        return Result.FromError<Game>("ID not found");
    }

    private string ParseFuelFile(string dir)
    {
        try
        {
            string file = Path.Combine(dir, "fuel.json");
            if (File.Exists(file))
            {
                string strDocumentData = File.ReadAllText(file);

                if (!string.IsNullOrEmpty(strDocumentData))
                {
                    using (JsonDocument document = JsonDocument.Parse(@strDocumentData, _jsonDocumentOptions))
                    {
                        JsonElement root = document.RootElement;
                        if (root.TryGetProperty("Main", out JsonElement main) && main.TryGetProperty("Command", out JsonElement command))
                        {
                            string? exe = command.GetString();
                            if (!string.IsNullOrEmpty(exe))
                                return Path.Combine(dir, exe);
                        }
                    }
                }
            }
        }
        catch (Exception) { }
        return "";
    }

    private List<string> GetJsonArray(string json)
    {
        List<string> list = new();
        using (JsonDocument doc = JsonDocument.Parse(json, _jsonDocumentOptions))
        {
            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                list.Add(element.GetString() ?? "");
            }
        }
        return list;
    }


    private static Result<Game> ParseSubKey(IRegistryKey unKey, string subKeyName, string id = "")
    {
        try
        {
            using var subKey = unKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return Result.FromError<Game>($"Unable to open {unKey}\\{subKeyName}");
            }

            if (!subKey.TryGetString("UninstallString", out var uninst))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"UninstallString\"");
            }
            var gameId = uninst[uninst.LastIndexOf("Game -p " + 8, StringComparison.OrdinalIgnoreCase)..];
            if (!string.IsNullOrEmpty(id) && !id.Equals(gameId, StringComparison.OrdinalIgnoreCase))
            {
                return Result.FromError<Game>("ID does not match.");
            }

            if (!subKey.TryGetString("DisplayName", out var name))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"DisplayName\"");
            }

            if (!subKey.TryGetString("InstallLocation", out var path))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"InstallLocation\"");
            }

            if (!subKey.TryGetString("DisplayIcon", out var launch)) launch = "";
            Dictionary<string, List<string>> meta = new(StringComparer.OrdinalIgnoreCase);

            return Result.FromGame(new Game(gameId, name, path, launch, launch, uninst, meta));
        }
        catch (Exception e)
        {
            return Result.FromException<Game>($"Exception while parsing registry key {unKey}\\{subKeyName}", e);
        }
    }

    internal static string GetDatabasePath(IFileSystem fileSystem)
    {
        return fileSystem.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Amazon Games",
            "Data",
            "Games",
            "Sql");
    }
}
