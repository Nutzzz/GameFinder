using ProtoBuf;
using System;
using System.Collections.Generic;
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

    private readonly JsonDocumentOptions _jsonDocumentOptions =
        new()
        {
            AllowTrailingCommas = true,
        };

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
            yield return Result.FromError<Game>($"The data directory {dataPath.FullName} does not contain any cached files");
            yield break;
        }

        foreach (var dataFile in dataFiles)
        {
            yield return DeserializeGame(dataFile, dbFile, cfgFile, uninstallExe, _fileSystem);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    private Result<Game> DeserializeGame(IFileInfo dataFile, IFileInfo dbFile, IFileInfo cfgFile, IFileInfo uninstallExe, IFileSystem fileSystem)
    {
        try
        {
            string? error = ParseDataFile(dataFile, fileSystem, out string id, out string? path, out string? name, out string? exe);
            if (error is not null)
                return Result.FromError<Game>(error);
            if (string.IsNullOrEmpty(name))
                name = id;

            error = ParseDatabase(id, dbFile, cfgFile, fileSystem, out string installPath, out string lang, out DateTime? lastRunDate);
            if (error is not null)
                return Result.FromError<Game>(error);

            /*
            Dictionary<string, List<string>> metadata = new();

            using JsonDocument document = JsonDocument.Parse(stream, _jsonDocumentOptions);
            document.RootElement.TryGetProperty(lang.ToLower(), out JsonElement jLanguage);
            JsonElement jLangConfig;
            if ((jLanguage.TryGetProperty("config", out jLangConfig) ||
                cache.Language.TryGetProperty("config", out jLangConfig)) &&
                jLangConfig.TryGetProperty("install", out JsonElement jLangInstall))
            {
                foreach (JsonElement install in jLangInstall.EnumerateArray())
                {
                    // name = jLangInstall[] > "add_remove_programs_key" > "display_name"
                    // description = jLangInstall[] > "program_associations" > "application_description"
                    metadata.Add("Description", new List<string>() { description });
                }
            }
            */

            string uninstall = $"\"{uninstallExe}\" --lang={lang} --uid={id} --displayname=\"{name}\"";
            if (!string.IsNullOrEmpty(path))
                installPath = Path.Combine(installPath, path);
            string launch = "";
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
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromError<Game>($"Unable to deserialize file {dataFile.FullName}:\n{e}");
        }
    }

    private string? ParseDataFile(IFileInfo dataFile, IFileSystem fileSystem, out string id, out string? path, out string? name, out string? exe)
    {
        id = "";
        path = "";
        name = "";
        exe = "";

        try
        {
            using var stream = dataFile.OpenRead();
            var cache = JsonSerializer.Deserialize<CacheFile>(stream, _jsonSerializerOptions);
            if (cache is null || !cache.All.TryGetProperty("config", out var jAllConfig))
            {
                return $"Unable to deserialize data file {dataFile.FullName}";
            }

            var allConfig = JsonSerializer.Deserialize<AllConfig>(jAllConfig, _jsonSerializerOptions);
            if (allConfig is null)
            {
                return $"Unable to deserialize \"all\">\"config\" data in file {dataFile.FullName}";
            }

            string? testId = allConfig.Product;
            if (string.IsNullOrEmpty(testId) ||
                testId.Equals("agent", StringComparison.OrdinalIgnoreCase) ||
                testId.Equals("bna", StringComparison.OrdinalIgnoreCase) ||
                testId.Equals("bts", StringComparison.OrdinalIgnoreCase))
            {
                return $"Product {testId} is not a game in file {dataFile.FullName}";
            }
            if (allConfig.Form is not null &&
                ((JsonElement)allConfig.Form).TryGetProperty("game_dir", out var jAllGameDir) &&
                jAllGameDir.TryGetProperty("dirname", out var jAllGameDirName))
            {
                name = jAllGameDirName.GetString();
            }
            id = testId;

            path = allConfig.SharedContainerDefaultSubfolder;

            if (!cache.Platform.TryGetProperty("win", out var jWinPlatform) || !jWinPlatform.TryGetProperty("config", out var jWinConfig))
            {
                return $"Data file {dataFile.FullName} does not have values for \"platform\">\"win\">\"config\"";
            }

            var winConfig = JsonSerializer.Deserialize<WinConfig>(jWinConfig, _jsonSerializerOptions);
            if (winConfig is null)
            {
                return $"Unable to deserialize \"platform\">\"win\">\"config\" data in file {dataFile.FullName}";
            }

            if (!winConfig.Binaries.TryGetProperty("game", out var jWinGame))
            {
                return $"Unable to deserialize \"platform\">\"win\">\"config\">\"binaries\" data in file {dataFile.FullName}";
            }
            if (string.IsNullOrEmpty(name) &&
                winConfig.Form is not null &&
                ((JsonElement)winConfig.Form).TryGetProperty("game_dir", out var jWinGameDir) &&
                jWinGameDir.TryGetProperty("dirname", out var jWinGameDirName))
            {
                name = jWinGameDirName.GetString();
            }

            if (string.IsNullOrEmpty(name))
            {
                return $"Data file {dataFile.FullName} does not have a value for \"dirname\"";
            }

            if (jWinGame.TryGetProperty("relative_path_64", out var jRelPath64))
                exe = jRelPath64.GetString();
            if (string.IsNullOrEmpty(exe))
            {
                if (jWinGame.TryGetProperty("relative_path", out var jRelPath32))
                    exe = jRelPath32.GetString();
            }
            if (string.IsNullOrEmpty(exe))
            {
                return $"Data file {dataFile.FullName} does not have a value for \"relative_path\"";
            }
        }
        catch (Exception e)
        {
            return $"Unable to deserialize file {dataFile.FullName}\n" + e.Message + "\n" + e.InnerException;
        }

        return null;
    }

    private string? ParseDatabase(string id, IFileInfo dbFile, IFileInfo cfgFile, IFileSystem fileSystem, out string installPath, out string lang, out DateTime? lastRunDate)
    {
        installPath = "";
        lang = "";
        lastRunDate = null;
        try
        {
            using var stream = File.OpenRead(dbFile.FullName);
            BnetDatabase db = Serializer.Deserialize<BnetDatabase>(stream);

            foreach (BnetProductInstall pi in db.productInstalls)
            {
                if (pi.productCode.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                    pi.Settings is not null)
                {
                    installPath = pi.Settings.installPath;
                    lang = pi.Settings.selectedTextLanguage;
                    if (string.IsNullOrEmpty(lang))
                        lang = "enUS"; // default

                    lastRunDate = ParseConfigForLastRun(id, cfgFile, fileSystem);
                    return null;
                }
            }
        }
        catch (Exception)
        {
            return $"Unable to deserialize database file: {dbFile.FullName}";
        }
        return $"Unable to find productCode \"{id}\" in database file: {dbFile.FullName}";
    }

    private DateTime? ParseConfigForLastRun(string code, IFileInfo cfgFile, IFileSystem fileSystem)
    {
        try
        {
            string strConfigData = fileSystem.File.ReadAllText(cfgFile.FullName);
            if (!string.IsNullOrEmpty(strConfigData))
            {
                using JsonDocument document = JsonDocument.Parse(@strConfigData, _jsonDocumentOptions);
                if (document.RootElement.TryGetProperty("Games", out JsonElement games) &&
                    games.TryGetProperty(code, out JsonElement cfgGame))
                {
                    cfgGame.TryGetProperty("LastPlayed", out JsonElement jLastPlayed);
                    if (long.TryParse(jLastPlayed.GetString(), out long lLastRun))
                        return DateTimeOffset.FromUnixTimeSeconds(lLastRun).UtcDateTime;
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
        return null;
    }

    internal static string GetBattleNetPath(IFileSystem fileSystem)
    {
        return fileSystem.Path.Combine(
            GetFolderPath(SpecialFolder.CommonApplicationData),
            "Battle.net");
    }
}
