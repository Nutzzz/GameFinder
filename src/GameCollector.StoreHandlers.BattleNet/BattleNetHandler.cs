using ProtoBuf;
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

namespace GameCollector.StoreHandlers.BattleNet;

/// <summary>
/// Handler for finding games installed with Blizzard Battle.net.
/// </summary>
[PublicAPI]
public class BattleNetHandler : AHandler<Game, string>
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
    public BattleNetHandler() : this(new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the <see cref="IFileSystem"/> implementation to use.
    /// </summary>
    /// <param name="fileSystem"></param>
    public BattleNetHandler(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var dataPath = _fileSystem.DirectoryInfo.New(
            Path.Combine(GetBattleNetPath(_fileSystem), "Agent", "data", "cache"));
        if (!dataPath.Exists)
        {
            yield return Result.FromError<Game>($"The data directory {dataPath.FullName} does not exist!");
            yield break;
        }
        var dbFile = _fileSystem.FileInfo.New(
            Path.Combine(GetBattleNetPath(_fileSystem), "Agent", "product.db"));
        if (!dbFile.Exists)
        {
            yield return Result.FromError<Game>($"The database file {dbFile.FullName} does not exist!");
            yield break;
        }
        var cfgFile = _fileSystem.FileInfo.New(
            Path.Combine(GetBattleNetPath(_fileSystem), "Battle.net.config"));
        var uninstallExe = _fileSystem.FileInfo.New(
            Path.Combine(GetBattleNetPath(_fileSystem), "Agent", "Blizzard Uninstaller.exe"));

        var dataFiles = dataPath
            .EnumerateFiles("*.", SearchOption.AllDirectories)
            .ToArray();

        if (dataFiles.Length == 0)
        {
            yield return Result.FromError<Game>($"The data directory {dataPath.FullName} does not contain any cached files!");
            yield break;
        }

        foreach (var dataFile in dataFiles)
        {
            yield return DeserializeGame(dataFile, dbFile, cfgFile, uninstallExe);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    private Result<Game> DeserializeGame(IFileInfo dataFile, IFileInfo dbFile, IFileInfo cfgFile, IFileInfo uninstallExe)
    {
        try
        {
            var (error, id, name, path, exe, description) = ParseDataFile(dataFile);
            if (error is not null)
                return Result.FromError<Game>(error);

            var (dbError, installPath, lang) = ParseDatabase(id, dbFile);
            if (dbError is not null)
                return Result.FromError<Game>(dbError);

            var lastRunDate = ParseConfigForLastRun(id, cfgFile);

            /*
            var (trName, trDescription) = ReparseForTranslations(lang, dataFile);
            if (!string.IsNullOrEmpty(trName))
                name = trName;
            if (!string.IsNullOrEmpty(trDescription))
                description = trDescription;
            */

            var uninstall = $"\"{uninstallExe}\" --lang={lang} --uid={id} --displayname=\"{name}\"";
            if (!string.IsNullOrEmpty(path))
                installPath = Path.Combine(installPath, path);
            var launch = "";
            if (!string.IsNullOrEmpty(exe))
                launch = Path.Combine(installPath, exe);

            return Result.FromGame(new Game(
                Id: id,
                Name: name,
                Path: installPath,
                Launch: launch,
                Icon: launch,
                Uninstall: uninstall,
                LastRunDate: lastRunDate,
                Metadata: new(StringComparer.OrdinalIgnoreCase) { ["Description"] = new() { description }, }
            ));
        }
        catch (Exception e)
        {
            return Result.FromError<Game>($"Unable to deserialize file {dataFile.FullName}:\n{e}");
        }
    }

    private (string? error, string id, string name, string path, string exe, string description)
        ParseDataFile(IFileInfo dataFile)
    {
        var id = "";
        var name = "";
        var path = "";
        var exe = "";
        var description = "";
        try
        {
            using var stream = dataFile.OpenRead();
            var cache = JsonSerializer.Deserialize<CacheFile>(stream, _jsonSerializerOptions);
            if (cache is null ||
                cache.All is null ||
                cache.All.Config is null ||
                cache.Platform is null ||
                cache.Platform.Win is null ||
                cache.Platform.Win.Config is null)
            {
                return ($"Unable to deserialize data file {dataFile.FullName}", "", "", "", "", "");
            }

            id = cache.All.Config.Product ?? "";
            path = cache.All.Config.SharedContainerDefaultSubfolder ?? "";
            if (string.IsNullOrEmpty(id) ||
                id.Equals("agent", StringComparison.OrdinalIgnoreCase) ||
                id.Equals("bna", StringComparison.OrdinalIgnoreCase) ||
                id.Equals("bts", StringComparison.OrdinalIgnoreCase))
            {
                return ($"Product \"{id}\" is not a game in file {dataFile.FullName}", "", "", "", "", "");
            }

            if (cache.All.Config.Form is not null &&
                cache.All.Config.Form.GameDir is not null)
            {
                name = cache.All.Config.Form.GameDir.Dirname ?? "";
            }
            if (string.IsNullOrEmpty(name) &&
                cache.Platform.Win.Config.Form is not null)
            {
                var form = cache.Platform.Win.Config.Form;
                if (form.GameDir is not null)
                    name = form.GameDir.Dirname ?? "";
            }
            if (string.IsNullOrEmpty(name))
                name = id;

            if (cache.Platform.Win.Config.Binaries is not null)
            {
                var bins = cache.Platform.Win.Config.Binaries;
                if (bins.Game is not null)
                {
                    exe = bins.Game.RelativePath64 ?? "";
                    if (string.IsNullOrEmpty(exe))
                        exe = bins.Game.RelativePath ?? "";
                }
            }

            if (string.IsNullOrEmpty(exe))
            {
                return ($"Data file {dataFile.FullName} does not have a value for \"relative_path\"", "", "", "", "", "");
            }

            if (cache.DefaultLanguage is not null &&
                cache.DefaultLanguage.Config is not null &&
                cache.DefaultLanguage.Config.Install is not null)
            {
                var installs = cache.DefaultLanguage.Config.Install;
                if (installs.Count > 0 &&
                    installs[0] is not null)
                {
                    var install = installs[0];
                    if (install.ProgramAssociations is not null)
                    {
                        description = install.ProgramAssociations.ApplicationDescription ?? "";
                    }
                }
            }
        }
        catch (Exception e)
        {
            return ($"Unable to deserialize file {dataFile.FullName}\n" + e.Message + "\n" + e.InnerException, "", "", "", "", "");
        }

        return (null, id, name, path, exe, description);
    }

    private static (string? error, string installPath, string lang)
        ParseDatabase(string id, IFileInfo dbFile)
    {
        try
        {
            using var stream = dbFile.OpenRead();
            var db = Serializer.Deserialize<BnetDatabase>(stream);

            foreach (var pi in db.productInstalls)
            {
                if (pi.productCode.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                    pi.Settings is not null)
                {
                    var installPath = pi.Settings.installPath ?? "";
                    var lang = pi.Settings.selectedTextLanguage;
                    if (string.IsNullOrEmpty(lang))
                        lang = "enUS"; // default

                    return (null, installPath, lang);
                }
            }
        }
        catch (Exception)
        {
            return ($"Unable to deserialize database file: {dbFile.FullName}", "", "");
        }
        return ($"Unable to find productCode \"{id}\" in database file: {dbFile.FullName}", "", "");
    }

    private DateTime ParseConfigForLastRun(string code, IFileInfo cfgFile)
    {
        try
        {
            using var stream = cfgFile.OpenRead();
            var config = JsonSerializer.Deserialize<ConfigFile>(stream, _jsonSerializerOptions);
            if (config is not null &&
                config.Games.TryGetProperty(code, out var cfgGame))
            {
                var game = JsonSerializer.Deserialize<ConfigGame>(cfgGame, _jsonSerializerOptions);
                if (game is not null &&
                    game.LastPlayed is not null &&
                    long.TryParse(game.LastPlayed, out long lLastRun))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(lLastRun).UtcDateTime;
                }
            }
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
        return DateTime.MinValue;
    }

    private (string name, string description) ReparseForTranslations(string lang, IFileInfo dataFile)
    {
        try
        {
            using var stream = dataFile.OpenRead();
            var root = JsonSerializer.Deserialize<JsonElement>(stream);
            root.TryGetProperty(lang.ToLower(CultureInfo.InvariantCulture), out var jLanguage);
            var cache = JsonSerializer.Deserialize<CacheFileConfig>(jLanguage);

            if (cache is not null &&
                cache.Config is not null &&
                cache.Config.Install is not null &&
                cache.Config.Install.Count > 0)
            {
                var install = cache.Config.Install[0];
                var name = "";
                var description = "";
                if (install.AddRemoveProgramsKey is not null)
                    name = install.AddRemoveProgramsKey.DisplayName ?? "";
                if (install.ProgramAssociations is not null)
                    description = install.ProgramAssociations.ApplicationDescription ?? "";
                return (name, description);
            }
        }
        catch (Exception)
        {
            return ("", "");
        }
        return ("", "");
    }

    internal static string GetBattleNetPath(IFileSystem fileSystem)
    {
        return fileSystem.Path.Combine(
            GetFolderPath(SpecialFolder.CommonApplicationData),
            "Battle.net");
    }
}
