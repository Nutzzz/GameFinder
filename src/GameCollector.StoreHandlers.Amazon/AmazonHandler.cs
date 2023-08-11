using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameCollector.SQLiteUtils;
using JetBrains.Annotations;
using NexusMods.Paths;
using OneOf;

namespace GameCollector.StoreHandlers.Amazon;

/// <summary>
/// Handler for finding games installed with Amazon Games.
/// Uses SQLite databases:
///   %LocalAppData%\Amazon Games\Data\Games\Sql\GameProductInfo.sqlite
///   %LocalAppData%\Amazon Games\Data\Games\Sql\GameInstallInfo.sqlite
/// and Registry key:
///   HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall
/// </summary>
[PublicAPI]
public class AmazonHandler : AHandler<AmazonGame, AmazonGameId>
{
    internal const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="registry">
    /// The implementation of <see cref="IRegistry"/> to use. For a shared instance
    /// use <see cref="WindowsRegistry.Shared"/> on Windows. On Linux use <c>null</c>.
    /// For tests either use <see cref="InMemoryRegistry"/>, a custom implementation or just a mock
    /// of the interface.
    /// </param>
    /// <param name="fileSystem">
    /// The implementation of <see cref="IFileSystem"/> to use. For a shared instance use
    /// <see cref="FileSystem.Shared"/>. For tests either use <see cref="InMemoryFileSystem"/>,
    /// a custom implementation or just a mock of the interface.
    /// </param>
    public AmazonHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEqualityComparer<AmazonGameId>? IdEqualityComparer => AmazonGameIdComparer.Default;

    /// <inheritdoc/>
    public override Func<AmazonGame, AmazonGameId> IdSelector => game => game.ProductId;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        if (_registry is not null)
        {
            var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser);

            using var regKey = currentUser.OpenSubKey(Path.Combine(UninstallRegKey, "{4DD10B06-78A4-4E6F-AA39-25E9C38FA568}"));
            if (regKey is not null)
            {
                if (regKey.TryGetString("InstallLocation", out var app) && Path.IsPathRooted(app))
                    return _fileSystem.FromUnsanitizedFullPath(app).Combine("Amazon Games.exe");
            }
        }

        return default;
    }

    /// <inheritdoc/>
    public override IEnumerable<OneOf<AmazonGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        var prodDb = GetDatabasePath(_fileSystem).Combine("GameProductInfo.sqlite");
        var instDb = GetDatabasePath(_fileSystem).Combine("GameInstallInfo.sqlite");
        if (!prodDb.FileExists)
        {
            yield return new ErrorMessage($"The database file {prodDb} does not exist!");
            yield break;
        }
        if (!instDb.FileExists)
        {
            yield return new ErrorMessage($"The database file {instDb} does not exist!");
            yield break;
        }

        foreach (var game in ParseDatabase(prodDb, instDb, installedOnly))
        {
            if (game.IsGame())
            {
                yield return game;
            }
        }
    }

    private IEnumerable<OneOf<AmazonGame, ErrorMessage>> ParseDatabase(AbsolutePath prodDb, AbsolutePath instDb, bool installedOnly = false)
    {
        var products = SQLiteHelpers.GetDataTable(prodDb, "SELECT * FROM DbSet;").ToList<ProductInfo>();
        var installs = SQLiteHelpers.GetDataTable(instDb, "SELECT * FROM DbSet;").ToList<InstallInfo>();
        if (products is null)
        {
            yield return new ErrorMessage($"Could not deserialize file {prodDb}");
            yield break;
        }
        foreach (var product in products)
        {
            AbsolutePath path = new();
            AbsolutePath icon = new();
            AbsolutePath uninstall = new();
            var isInstalled = false;

            var id = product.ProductIdStr;
            if (id is null)
            {
                yield return new ErrorMessage($"Value for \"ProductIdStr\" does not exist in file {prodDb}");
                continue;
            }

            var found = false;
            if (installs is not null)
            {
                foreach (var install in installs)
                {
                    var dir = install.InstallDirectory;
                    if (id.Equals(install.Id, StringComparison.Ordinal) && dir is not null && Path.IsPathRooted(dir))
                    {
                        path =  _fileSystem.FromUnsanitizedFullPath(dir);
                        found = true;
                    }
                }
            }

            if (!found)
            {
                if (installedOnly)
                {
                    yield return new ErrorMessage($"Value for \"InstallDirectory\" does not exist in file {instDb}");
                    continue;
                }
            }
            else
            {
                isInstalled = true;
                icon = ParseFuelFileForExe(path);
                var regGame = ParseRegistryForId(_fileSystem, _registry, id);
                if (regGame.IsGame())
                {
                    if (icon == default)
                        icon = regGame.AsGame().Icon;
                    uninstall = regGame.AsGame().Uninstall;
                }
            }

            var url = "amazon-games://play/" + id;
            var releaseDate = product.ReleaseDate ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture);
            _ = DateTime.TryParseExact(releaseDate, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dtReleaseDate);
            var developers = product.DevelopersJson ?? "";
            _ = Enum.TryParse(product.EsrbRating ?? "NO_RATING", out EsrbRating ageRating);
            var players = product.GameModesJson ?? "";
            var genres = product.GenresJson ?? "";

            yield return new AmazonGame(
                ProductId: AmazonGameId.From(id),
                ProductTitle: product.ProductTitle,
                InstallDirectory: path,
                LaunchUrl: url,
                Icon: icon,
                Uninstall: uninstall,
                IsInstalled: isInstalled,
                ReleaseDate: dtReleaseDate,
                ProductDescription: product.ProductDescription,
                ProductIconUrl: product.ProductIconUrl,
                ProductLogoUrl: product.ProductLogoUrl,
                Developers: developers,
                ProductPublisher: product.ProductPublisher,
                EsrbRating: ageRating,
                GameModes: players,
                Genres: genres
            );
        }
    }

    private IEnumerable<OneOf<AmazonGame, ErrorMessage>> ParseRegistry()
    {
        if (_registry is null)
        {
            return new OneOf<AmazonGame, ErrorMessage>[] { new ErrorMessage("Unable to open registry"), };
        }

        try
        {
            var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser);

            using var unKey = currentUser.OpenSubKey(UninstallRegKey);
            if (unKey is null)
            {
                return new OneOf<AmazonGame, ErrorMessage>[] { new ErrorMessage($"Unable to open HKEY_CURRENT_USER\\{UninstallRegKey}"), };
            }

            var subKeyNames = unKey.GetSubKeyNames().Where(
                keyName => keyName[(keyName.LastIndexOf('\\') + 1)..].StartsWith("AmazonGames/", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (subKeyNames.Length == 0)
            {
                return new OneOf<AmazonGame, ErrorMessage>[] {
                    new ErrorMessage($"Registry key {unKey.GetName()} has no sub-keys beginning with \"AmazonGames/\""),
                };
            }

            return subKeyNames
                .Select(subKeyName => ParseSubKey(unKey, subKeyName, _fileSystem))
                .ToArray();
        }
        catch (Exception e)
        {
            return new OneOf<AmazonGame, ErrorMessage>[] { new ErrorMessage(e, "Exception looking for Amazon games in registry") };
        }
    }

    private static OneOf<AmazonGame, ErrorMessage> ParseRegistryForId(IFileSystem fileSystem, IRegistry registry, string id)
    {
        if (registry is null)
        {
            return new ErrorMessage("Unable to open registry");
        }

        try
        {
            var currentUser = registry.OpenBaseKey(RegistryHive.CurrentUser);

            using var unKey = currentUser.OpenSubKey(UninstallRegKey);
            if (unKey is null)
            {
                new ErrorMessage($"Unable to open HKEY_CURRENT_USER\\{UninstallRegKey}");
            }
            else
            {
                var subKeyNames = unKey.GetSubKeyNames().Where(
                    keyName => keyName[(keyName.LastIndexOf('\\') + 1)..].StartsWith("AmazonGames/", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (subKeyNames.Length == 0)
                {
                    new ErrorMessage($"Registry key {unKey.GetName()} has no sub-keys beginning with \"AmazonGames/\"");
                }

                foreach (var subKeyName in subKeyNames)
                {
                    var game = ParseSubKey(unKey, subKeyName, fileSystem, id);
                    if (game.IsGame())
                    {
                        return game;
                    }
                }
            }
        }
        catch (Exception e)
        {
            return new ErrorMessage(e, "Exception looking for Amazon games in registry");
        }
        return new ErrorMessage("ID not found");
    }

    private static AbsolutePath ParseFuelFileForExe(AbsolutePath dir)
    {
        try
        {
            var file = dir.Combine("fuel.json");
            if (file.FileExists)
            {
                var strDocumentData = File.ReadAllText(file.GetFullPath());

                if (!string.IsNullOrEmpty(strDocumentData))
                {
                    using var document = JsonDocument.Parse(@strDocumentData, new() { AllowTrailingCommas = true, });
                    var root = document.RootElement;
                    if (root.TryGetProperty("Main", out var main) && main.TryGetProperty("Command", out var command))
                    {
                        var exe = command.GetString();
                        if (!string.IsNullOrEmpty(exe))
                            return dir.Combine(exe);
                    }
                }
            }
        }
        catch (Exception) { }

        return default;
    }

    private static OneOf<AmazonGame, ErrorMessage> ParseSubKey(IRegistryKey unKey, string subKeyName, IFileSystem fileSystem, string id = "")
    {
        try
        {
            using var subKey = unKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return new ErrorMessage($"Unable to open {unKey}\\{subKeyName}");
            }

            if (!subKey.TryGetString("UninstallString", out var uninst))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"UninstallString\"");
            }
            var gameId = uninst[uninst.LastIndexOf("Game -p " + 8, StringComparison.OrdinalIgnoreCase)..];
            if (!string.IsNullOrEmpty(id) && !id.Equals(gameId, StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorMessage("ID does not match.");
            }

            if (!subKey.TryGetString("DisplayName", out var name))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"DisplayName\"");
            }

            if (!subKey.TryGetString("InstallLocation", out var path))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"InstallLocation\"");
            }

            if (!subKey.TryGetString("DisplayIcon", out var launch)) launch = "";

            return new AmazonGame(
                ProductId: AmazonGameId.From(gameId),
                ProductTitle: name,
                InstallDirectory: Path.IsPathRooted(path) ? fileSystem.FromUnsanitizedFullPath(path) : new(),
                Command: Path.IsPathRooted(launch) ? fileSystem.FromUnsanitizedFullPath(launch) : new(),
                Uninstall: Path.IsPathRooted(uninst) ? fileSystem.FromUnsanitizedFullPath(uninst) : new()
            );
        }
        catch (Exception e)
        {
            return new ErrorMessage(e, $"Exception while parsing registry key {unKey}\\{subKeyName}");
        }
    }

    internal static AbsolutePath GetDatabasePath(IFileSystem fileSystem)
    {
        return fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
            .Combine("Amazon Games")
            .Combine("Data")
            .Combine("Games")
            .Combine("Sql");
    }
}
