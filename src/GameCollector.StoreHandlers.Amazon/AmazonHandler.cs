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
using JetBrains.Annotations;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using GameCollector.SQLiteUtils;
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

    private enum AzEsrbRating
    {
        NO_RATING = -1,
        everyone = 1,
        everyone_10_plus,
        teen,
        mature,
        adults_only,    // TODO: Confirm this; I don't own any Adults Only games from Amazon, so this name is a guess
        rating_pending,
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var prodDb = _fileSystem.Path.Combine(GetDatabasePath(_fileSystem), "GameProductInfo.sqlite");
        var instDb = _fileSystem.Path.Combine(GetDatabasePath(_fileSystem), "GameInstallInfo.sqlite");
        if (!_fileSystem.File.Exists(prodDb))
        {
            yield return Result.FromError<Game>($"The database file {prodDb} does not exist!");
            yield break;
        }
        if (!_fileSystem.File.Exists(instDb))
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
        var products = SQLiteHelpers.GetDataTable(prodDb, "SELECT * FROM DbSet;").ToList<ProductInfo>();
        var installs = SQLiteHelpers.GetDataTable(instDb, "SELECT * FROM DbSet;").ToList<InstallInfo>();
        if (products is null)
        {
            yield return Result.FromError<Game>($"Could not deserialize file {prodDb}");
            yield break;
        }
        foreach (var product in products)
        {
            var path = "";
            var icon = "";
            var uninstall = "";
            var isInstalled = false;

            var id = product.ProductIdStr;
            if (id is null)
            {
                yield return Result.FromError<Game>($"Value for \"ProductIdStr\" does not exist in file {prodDb}");
                continue;
            }
            
            if (installs is not null)
            {
                foreach (var install in installs)
                {
                    if (id.Equals(install.Id, StringComparison.Ordinal))
                        path = install.InstallDirectory;
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                if (installedOnly)
                {
                    yield return Result.FromError<Game>($"Value for \"InstallDirectory\" does not exist in file {instDb}");
                    continue;
                }
            }
            else
            {
                isInstalled = true;
                icon = ParseFuelFileForExe(path);
                var regGame = ParseRegistryForId(_registry, id);
                if (regGame.Game is not null)
                {
                    if (string.IsNullOrEmpty(icon))
                        icon = regGame.Game.Icon;
                    uninstall = regGame.Game.Uninstall;
                }
            }

            var launch = "amazon-games://play/" + id;
            _ = Enum.TryParse(product.EsrbRating ?? "NO_RATING", out AzEsrbRating ageRating);
            var developers = product.DevelopersJson ?? "";
            var players = product.GameModesJson ?? "";
            var genres = product.GenresJson ?? "";
            var releaseDate = product.ReleaseDate ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture);
            _ = DateTime.TryParseExact(releaseDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dtReleaseDate);

            yield return Result.FromGame<Game>(new(
                Id: id,
                Name: product.ProductTitle ?? "",
                Path: path,
                Launch: launch,
                Icon: icon,
                Uninstall: uninstall,
                IsInstalled: isInstalled,
                Metadata: new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Description"] = new() { product.ProductDescription ?? "" },
                    ["IconUrl"] = new() { product.ProductIconUrl ?? "" },
                    ["Publisher"] = new() { product.ProductPublisher ?? "" },
                    ["AgeRating"] = new() { ageRating.ToString() },
                    ["Developers"] = GetJsonArray(@developers),
                    ["Players"] = GetJsonArray(@players),
                    ["Genres"] = GetJsonArray(@genres),
                    // should massage the ReleaseDate format (e.g., "2020-04-03T24:00:00Z") to get a valid DateTime.ToString?
                    ["ReleaseDate"] = new() { dtReleaseDate.ToString(CultureInfo.InvariantCulture) ?? "" },
                }));
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

    private string ParseFuelFileForExe(string dir)
    {
        try
        {
            var file = _fileSystem.Path.Combine(dir, "fuel.json");
            if (_fileSystem.File.Exists(file))
            {
                var strDocumentData = _fileSystem.File.ReadAllText(file);

                if (!string.IsNullOrEmpty(strDocumentData))
                {
                    using (var document = JsonDocument.Parse(@strDocumentData, _jsonDocumentOptions))
                    {
                        var root = document.RootElement;
                        if (root.TryGetProperty("Main", out var main) && main.TryGetProperty("Command", out var command))
                        {
                            var exe = command.GetString();
                            if (!string.IsNullOrEmpty(exe))
                                return _fileSystem.Path.Combine(dir, exe);
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
        using (var doc = JsonDocument.Parse(json, _jsonDocumentOptions))
        {
            foreach (var element in doc.RootElement.EnumerateArray())
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

            return Result.FromGame(new Game(
                Id: gameId,
                Name: name,
                Path: path,
                Launch: launch,
                Icon: launch,
                Uninstall: uninst,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromException<Game>($"Exception while parsing registry key {unKey}\\{subKeyName}", e);
        }
    }

    internal static string GetDatabasePath(IFileSystem fileSystem)
    {
        return fileSystem.Path.Combine(
            GetFolderPath(SpecialFolder.LocalApplicationData),
            "Amazon Games",
            "Data",
            "Games",
            "Sql");
    }
}
