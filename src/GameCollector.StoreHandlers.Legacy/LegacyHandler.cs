using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;
using OneOf;

namespace GameCollector.StoreHandlers.Legacy;

/// <summary>
/// Handler for finding games installed with Legacy Games Launcher.
/// Uses Json file:
///   %AppData%\legacy-games-launcher\app-state.json
/// and Registry key:
///   HKCU\Software\Legacy Games
/// </summary>
[PublicAPI]
public class LegacyHandler : AHandler<LegacyGame, string>
{
    internal const string LegacyRegKey = @"Software\Legacy Games";
    internal const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.Strict,
            TypeInfoResolver = SourceGenerationContext.Default,
        };

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
    public LegacyHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEqualityComparer<string>? IdEqualityComparer => null;

    /// <inheritdoc/>
    public override Func<LegacyGame, string> IdSelector => game => game.GameId;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        if (_registry is not null)
        {
            var localMachine64 = _registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

            using var regKey = localMachine64.OpenSubKey(Path.Combine(UninstallRegKey, "da414c81-a9fd-5732-bd5e-8acced116298"));
            if (regKey is not null)
            {
                if (regKey.TryGetString("DisplayIcon", out var icon) && Path.IsPathRooted(icon))
                {
                    return _fileSystem.FromFullPath(_fileSystem.FromFullPath(SanitizeInputPath(icon)).Directory);
                }
            }
        }

        return default;
    }

    /// <inheritdoc/>
    public override IEnumerable<OneOf<LegacyGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        return ParseRegistry();
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(JsonSerializerOptions)} uses {nameof(SourceGenerationContext)} for type information.")]
    private (string descr, string url, Genre genre) ParseJsonFile(string uuid)
    {
        //List<string> libPaths = new();
        var jsonFile = GetLegacyJsonFile();
        if (!jsonFile.FileExists)
            return ("", "", (Genre)(-1));

        try
        {
            using var stream = jsonFile.Read();
            var appState = JsonSerializer.Deserialize<AppStateFile>(stream, JsonSerializerOptions);
            if (appState is null || appState.SiteData is null || appState.SiteData.Catalog is null)
                return ("", "", (Genre)(-1));

            foreach (var item in appState.SiteData.Catalog)
            {
                var genre = (Genre)(-1);
                if (item.Categories is not null)
                {
                    foreach (var category in item.Categories)
                    {
                        foreach (var val in Enum.GetValues<Genre>())
                        {
                            if (category.Id is not null && ((Genre)category.Id).Equals(val))
                                genre = val;
                        }
                    }
                }

                if (item.Games is null)
                    continue;

                foreach (var game in item.Games)
                {
                    if (game.InstallerUuid is not null && uuid.Equals(game.InstallerUuid, StringComparison.OrdinalIgnoreCase))
                        return (game.GameDescription ?? "", game.GameCoverart ?? "", genre);
                }
            }

            if (appState.SiteData.GiveawayCatalog is null)
                return ("", "", (Genre)(-1));

            foreach (var item in appState.SiteData.GiveawayCatalog)
            {
                if (item.Games is null)
                    continue;

                foreach (var game in item.Games)
                {
                    if (game.InstallerUuid is not null && uuid.Equals(game.InstallerUuid, StringComparison.OrdinalIgnoreCase))
                        return (game.GameDescription ?? "", game.GameCoverart ?? "", (Genre)(-1));
                }
            }

            return ("", "", (Genre)(-1));
        }
        catch (Exception)
        {
            return ("", "", (Genre)(-1));
        }
    }

    private (string icon, string uninst, string pub) ParseUninstall(string subKeyName)
    {
        try
        {
            var localMachine32 = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var unKey = localMachine32.OpenSubKey(Path.Combine(UninstallRegKey, subKeyName));
            if (unKey is null)
                return new();

            unKey.TryGetString("DisplayIcon", out var icon);
            unKey.TryGetString("UninstallString", out var uninst);
            unKey.TryGetString("Publisher", out var pub);

            return (icon ?? "", uninst ?? "", pub ?? "");
        }
        catch (Exception) { }

        return new();
    }

    private OneOf<LegacyGame, ErrorMessage> ParseSubKey(IRegistryKey legKey, string subKeyName)
    {
        try
        {
            using var subKey = legKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return new ErrorMessage($"Unable to open {legKey}\\{subKeyName}");
            }

            if (!subKey.TryGetString("InstallerUUID", out var id))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"InstallerUUID\"");
            }

            if (!subKey.TryGetString("ProductName", out var name))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"ProductName\"");
            }

            if (!subKey.TryGetString("InstDir", out var path))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"InstDir\"");
            }

            AbsolutePath instDir = new();
            AbsolutePath exePath = new();
            if (Path.IsPathRooted(path))
            {
                instDir = _fileSystem.FromFullPath(SanitizeInputPath(path));
                if (subKey.TryGetString("GameExe", out var exe))
                {
                    exePath = instDir.CombineUnchecked(SanitizeInputPath(exe));
                }
            }

            var (icon, uninstall, publisher) = ParseUninstall(name);
            var (description, imageUrl, genre) = ParseJsonFile(id);

            var game = new LegacyGame(
                InstallerUuid: LegacyGameId.From(id),
                ProductName: name,
                InstDir: instDir,
                ExePath: exePath,
                DisplayIcon: Path.IsPathRooted(icon) ? _fileSystem.FromFullPath(SanitizeInputPath(icon)) : exePath,
                UninstallString: Path.IsPathRooted(uninstall) ? _fileSystem.FromFullPath(SanitizeInputPath(uninstall)) : new(),
                Description: description,
                Publisher: publisher,
                Genre: genre,
                CoverArtUrl: imageUrl
            );

            return game;
        }
        catch (Exception e)
        {
            return new ErrorMessage(e, $"Exception while parsing registry key {legKey}\\{subKeyName}");
        }
    }

    private IEnumerable<OneOf<LegacyGame, ErrorMessage>> ParseRegistry()
    {
        if (_registry is null)
        {
            return new OneOf<LegacyGame, ErrorMessage>[] { new ErrorMessage("Unable to open registry"), };
        }

        try
        {
            var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser);

            using var legKey = currentUser.OpenSubKey(LegacyRegKey);
            if (legKey is null)
            {
                return new OneOf<LegacyGame, ErrorMessage>[]
                {
                    new ErrorMessage($"Unable to open HKEY_CURRENT_USER\\{LegacyRegKey}"),
                };
            }

            var subKeyNames = legKey.GetSubKeyNames().ToArray();
            if (subKeyNames.Length == 0)
            {
                return new OneOf<LegacyGame, ErrorMessage>[] { new ErrorMessage($"Registry key {legKey.GetName()} has no sub-keys"), };
            }

            return subKeyNames
                .Select(subKeyName => ParseSubKey(legKey, subKeyName))
                .ToArray();
        }
        catch (Exception e)
        {
            return new OneOf<LegacyGame, ErrorMessage>[] { new ErrorMessage(e, "Exception looking for Legacy games in registry") };
        }
    }

    public AbsolutePath GetLegacyJsonFile()
    {
        return _fileSystem.GetKnownPath(KnownPath.ApplicationDataDirectory)
            .CombineUnchecked("legacy-games-launcher")
            .CombineUnchecked("app-state.json");
    }
}
