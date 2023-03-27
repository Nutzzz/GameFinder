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
using GameCollector.RegistryUtils;
using static System.Environment;

namespace GameCollector.StoreHandlers.Humble;

/// <summary>
/// Handler for finding games installed with Humble App.
/// Uses json file:
///   %AppData%\Humble App\config.json
/// and Registry key:
///   HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall
/// </summary>
[PublicAPI]
public class HumbleHandler : AHandler<Game, string>
{
    internal const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            AllowTrailingCommas = true,
        };

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation of
    /// <see cref="IRegistry"/> and the real file system with <see cref="FileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public HumbleHandler() : this(new WindowsRegistry(), new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>. This uses
    /// the real file system with <see cref="FileSystem"/>.
    /// </summary>
    /// <param name="registry"></param>
    public HumbleHandler(IRegistry registry) : this(registry, new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/> and
    /// <see cref="IFileSystem"/> when doing tests.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public HumbleHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var configFile = _fileSystem.FileInfo.New(_fileSystem.Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "Humble App", "config.json"));
        if (!configFile.Exists)
        {
            yield return Result.FromError<Game>($"The configuration file {configFile.FullName} does not exist!");
            yield break;
        }

        using var stream = configFile.OpenRead();
        var config = JsonSerializer.Deserialize<ConfigFile>(stream, _jsonSerializerOptions);
        if (config is null)
        {
            yield return Result.FromError<Game>($"The configuration file {configFile.FullName} could not be deserialized!");
            yield break;
        }
        var hasChoice = false;
        var isPaused = false;
        if (config.User is not null)
        {
            if (config.User.OwnsActiveContent is not null &&
                (bool)config.User.OwnsActiveContent)
                hasChoice = true; // TODO: Is this right? Or should we be checking .HasPerks?
            if (config.User.IsPaused is not null &&
                (bool)config.User.IsPaused)
                isPaused = true;
        }
        if (config.GameCollection4 is not null)
        {
            foreach (var game in config.GameCollection4)
            {
                var launch = "";
                var isInstalled = false;
                var hasProblem = false;
                var machineName = "";

                if (game.IsAvailable is not null &&
                    !(bool)game.IsAvailable) // must be downloaded from the website and installed manually
                    continue;
                if (game.MachineName is not null)
                {
                    machineName = game.MachineName;
                    if (machineName.EndsWith("_collection", StringComparison.OrdinalIgnoreCase) && // Humble Choice
                        (!hasChoice || isPaused))
                        hasProblem = true;
                }
                if (game.Status is not null &&
                    (game.Status.Equals("downloaded", StringComparison.OrdinalIgnoreCase) ||
                    game.Status.Equals("installed", StringComparison.OrdinalIgnoreCase)))
                    isInstalled = true;
                if (!isInstalled && installedOnly)
                    continue;

                var id = game.DownloadMachineName;
                if (string.IsNullOrEmpty(id))
                    id = game.Gamekey ?? "";
                var exe = game.ExecutablePath ?? "";
                var path = game.FilePath ?? "";
                if (!string.IsNullOrEmpty(exe) &&
                    !string.IsNullOrEmpty(path))
                    launch = _fileSystem.Path.Combine(path, exe);

                var lastRunDate = DateTime.MinValue;
                if (game.LastPlayed is not null)
                {
                    if (long.TryParse(game.LastPlayed, CultureInfo.InvariantCulture, out var unixTime) &&
                        unixTime > 0)
                        lastRunDate = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                }

                List<string> publishers = new();
                if (game.Publishers is not null)
                {
                    foreach (var publisher in game.Publishers)
                    {
                        if (publisher.PublisherName is not null)
                            publishers.Add(publisher.PublisherName);
                    }
                }

                List<string> developers = new();
                if (game.Developers is not null)
                {
                    foreach (var developer in game.Developers)
                    {
                        if (developer.DeveloperName is not null)
                            publishers.Add(developer.DeveloperName);
                    }
                }

                var icon = launch;
                if (string.IsNullOrEmpty(launch))
                    launch = $"humble://launch/{id}";

                yield return Result.FromGame(new Game(
                    Id: id,
                    Name: game.GameName ?? "",
                    Path: path,
                    Launch: launch,
                    Icon: icon,
                    Uninstall: $"humble://uninstall/{id}",
                    LastRunDate: lastRunDate,
                    IsInstalled: isInstalled,
                    HasProblem: hasProblem,
                    Metadata: new(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Description"] = new() { game.DescriptionText ?? "" },
                        ["IconUrl"] = new() { game.IconPath ?? "" },
                        ["IconWideUrl"] = new() { game.ImagePath ?? "" },
                        ["MachineName"] = new() { machineName },
                        ["Publishers"] = publishers,
                        ["Developers"] = developers,
                    }));
            }
        }
    }

    private static (string uninstall, string icon) GetRegValues(string id, IRegistry registry)
    {
        try
        {
            var currentUser = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

            using var unKey = currentUser.OpenSubKey(UninstallRegKey);
            if (unKey is null)
            {
                return ("", "");
            }

            var subKeyNames = unKey.GetSubKeyNames().Where(
                keyName => keyName[(keyName.LastIndexOf('\\') + 1)..].StartsWith("Humble App ", StringComparison.OrdinalIgnoreCase)).ToList();
            if (subKeyNames.Count == 0)
            {
                return ("", "");
            }

            foreach (var subKeyName in subKeyNames)
            {
                if (id.Equals(subKeyName[(subKeyName.IndexOf("Humble App ", StringComparison.OrdinalIgnoreCase) + 11)..], StringComparison.OrdinalIgnoreCase))
                {
                    using var subKey = unKey.OpenSubKey(subKeyName);
                    if (subKey is null)
                        return ("", "");

                    _ = subKey.TryGetString("DisplayIcon", out var icon);
                    //_ = subKey.TryGetString("DisplayName", out var name);
                    //_ = subKey.TryGetString("InstallLocation", out var path);
                    _ = subKey.TryGetString("UninstallString", out var uninst);
                    return (uninst ?? "", icon ?? "");
                }
            }
        }
        catch (Exception) { }
        return ("", "");
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }
}
