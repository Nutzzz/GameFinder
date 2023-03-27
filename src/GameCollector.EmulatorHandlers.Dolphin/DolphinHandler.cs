using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using static System.Environment;
using GameCollector.Common;
using GameCollector.RegistryUtils;

namespace GameCollector.EmulatorHandlers.Dolphin;

/// <summary>
/// Handler for finding ROMs for Dolphin.
/// </summary>
[PublicAPI]
public partial class DolphinHandler : AHandler<Game, string>
{
    private const string DolphinRegKey = @"Software\Dolphin Emulator";

    private static readonly ILogger logger = new NLogLoggerProvider().CreateLogger("Dolphin");
    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;
    private string _exePath = "";

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation of
    /// <see cref="IRegistry"/> and the real file system with <see cref="FileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public DolphinHandler() : this(new WindowsRegistry(), new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>. This uses
    /// the real file system with <see cref="FileSystem"/>.
    /// </summary>
    /// <param name="registry"></param>
    public DolphinHandler(IRegistry registry) : this(registry, new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/> and
    /// <see cref="IFileSystem"/> when doing tests.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public DolphinHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        return FindAllGames();
    }

    /// <summary>
    /// Finds all ROMs supported by this emulator. The return type <see cref="Result{TGame}"/>
    /// will always be a non-null game or a non-null error message.
    /// </summary>
    /// <param name="exePath">Path to Dolphin executable</param>
    /// <returns></returns>
    public IEnumerable<Result<Game>> FindAllGames(string exePath = "")
    {
        if (string.IsNullOrEmpty(exePath))
        {
            logger.LogWarning("Dolphin exePath not specified");
            exePath = @"C:\Dolphin\dolphin.exe";
        }

        if (_fileSystem.Path.GetExtension(exePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            _exePath = exePath;
        else
            _exePath = _fileSystem.Path.Combine(exePath, "dolphin.exe");

        if (!_fileSystem.File.Exists(_exePath))
        {
            yield return Result.FromError<Game>($"The file {_exePath} does not exist!");
            yield break;
        }

        var userPath = GetUserPath();
        /*
        List<string> files = new();
        foreach (var path in GetROMPaths(userPath, out var recurse))
        {
            files.AddRange(GetROMFiles(path, recurse));
        }
        */
        var romPaths = GetROMPaths(userPath, out var recurse);
        foreach (var game in ParseGameList(userPath, romPaths))
        {
            var file = game.File;
            if (!string.IsNullOrEmpty(file) && _fileSystem.File.Exists(file))
            {
                var name = _fileSystem.Path.GetFileNameWithoutExtension(file);
                var id = game.Gameid ?? name;
                var icon = (id.Length > 5) ?
                        _fileSystem.Path.Combine(userPath, "Cache", "GameCovers", string.Concat(id[..6], ".png")) : "";
                yield return Result.FromGame(new Game(
                    Id: id,
                    Name: game.Title ?? name,
                    Path: _fileSystem.Path.GetDirectoryName(file) ?? "",
                    Launch: _exePath,
                    LaunchArgs: file,
                    Icon: _fileSystem.File.Exists(icon) ? icon : "",
                    Metadata: new(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ReleaseDate"] = new() { game.ApploaderDate ?? "" },
                        //["Manufacturer"] = new() { game.Maker ?? "" },
                        ["Platform"] = new() { game.Platform.ToString() ?? "" },
                        //["Region"] = new() { game.Region ?? "" },
                        //["Country"] = new() { game.Country ?? "" },
                    }
                ));
            }
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    public string GetUserPath()
    {
        var userPath = "";
        var dolphinPath = _fileSystem.Path.GetDirectoryName(_exePath);
        var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using (var regKey = currentUser.OpenSubKey(DolphinRegKey))
        {
            if (regKey is not null)
            {
                // TODO: Determine correct order of these two checks by seeing which setting trumps the other
                if (regKey.TryGetString("LocalUserConfig", out var local))
                {
                    if (local.Equals("1", StringComparison.Ordinal) &&
                        !string.IsNullOrEmpty(dolphinPath))
                        userPath = _fileSystem.Path.Combine(dolphinPath, "User");
                }
                if (string.IsNullOrEmpty(userPath))
                    regKey.TryGetString("UserConfigPath", out userPath);
            }
        }

        if (string.IsNullOrEmpty(userPath) ||
            !_fileSystem.Directory.Exists(userPath))
            userPath = _fileSystem.Path.Combine(GetFolderPath(SpecialFolder.MyDocuments), "Dolphin Emulator");

        if ((string.IsNullOrEmpty(userPath) ||
            !_fileSystem.Directory.Exists(userPath)) &&
            !string.IsNullOrEmpty(dolphinPath))
        {
            userPath = _fileSystem.Path.Combine(dolphinPath, "User");
        }

        if (string.IsNullOrEmpty(userPath) ||
            !_fileSystem.Directory.Exists(userPath))
        {
            logger.LogError("Unable to find user path");
            return "";
        }
        return userPath;
    }

    // TODO: This is pretty ugly; since Dolphin is open source, a neater solution is probably available
    public IList<GameList> ParseGameList(string userPath, IList<string> romPaths)
    {
        List<GameList> gameList = new();
        GameList game = new();

        var path = _fileSystem.Path.Combine(userPath, "Cache", "gamelist.cache");
        if (!_fileSystem.File.Exists(path))
        {
            logger.LogError("File not found: {path}", path);
            return gameList;
        }

        var bytes = _fileSystem.File.ReadAllBytes(path);
        var byteString = Encoding.UTF8.GetString(bytes);

        var i = 0;
        var j = 0;
        var start = false;
        var done = false;
        var num = 0;
        var prevStr = "";
        foreach (var str in byteString.Split("\0"))
        {
            if (string.IsNullOrEmpty(str) ||
                str.Length == 1 ||
                str.Length > 260)
                continue;
            i++;
            foreach (var romPath in romPaths)
            {
                if (str.StartsWith(romPath.Replace('\\', '/'), StringComparison.Ordinal))
                {
                    i = 0; j = 0; start = true; done = false; num++;
                    game = new() { File = str[..^1], Platform = DolphinPlatform.Unknown };
                    //logger.LogDebug("GAME# {num}", num);
                    //logger.LogDebug("[0] filepath: {str}", game.File);
                    break;
                }
            }
            if (done)
                continue;
            if (!start || i == 0)
                continue;
            if (i == 1)
            {
                //logger.LogDebug("[1] filename: {str}", str);
                continue;
            }
            if (i == 2 ||
                (i == 3 && game.Platform == DolphinPlatform.Unknown))
            {
                if (str.EndsWith('W'))
                {
                    game.Platform = DolphinPlatform.GameCube;
                    //logger.LogDebug("[{i}] platform: GameCube", i);
                    continue;
                }
                if (str.EndsWith("H0\u0002", StringComparison.Ordinal) ||
                    str.EndsWith("$\u0018\u0001", StringComparison.Ordinal) ||
                    str.EndsWith("�\u000f", StringComparison.Ordinal) ||
                    str.EndsWith("��\u0001", StringComparison.Ordinal))
                {
                    game.Platform = DolphinPlatform.Wii;
                    //logger.LogDebug("[{i}] platform: Wii", i);
                    continue;
                }
                if (i == 3 && game.Platform == DolphinPlatform.Unknown)
                {
                    //logger.LogWarning("[3] unknown platform: {str}", str);
                }
                continue;
            }

            switch (str[^1])
            {
                case '': //VT
                case '': //SO
                    game.Description = str[..^1];
                    //logger.LogDebug("[{i}] description: {str}", i, game.Description);
                    continue;
                case '': //SOH, hollow happyface
                case '': //STX, filled happyface
                case '': //ETX, heart
                case '': //EOT, diamond
                case '': //ENQ, club
                case '': //BEL
                    //logger.LogDebug("[{i}] title translation: {str}", i, str);
                    continue;
                case '': //ACK, spade
                    j++;
                    if (j > 1) // The gameid will be the last line ending in ACK/spade
                    {
                        game.Gameid = str[..^1];
                        if (!string.IsNullOrEmpty(prevStr))
                        {
                            if (!char.IsAsciiLetter(prevStr[^1]))
                                game.Title = prevStr[..^1];
                            else
                                game.Title = prevStr;
                        }

                        // TODO: The best title seems to usually be the one prior to the gameid, but sometimes two back is preferable;
                        // However, I'm not sure if it's possible to make that determination automatically
                        //logger.LogDebug("[{i}] full title?: {prev} | gameid?: {str}", i, game.Title, str[..^1]);
                    }
                    if (!str.StartsWith("RVL", StringComparison.Ordinal)) // Skip this entry
                    {
                        //logger.LogDebug("[{i}] unknown ACK: {str}", i, str[..^1]);
                        prevStr = str;
                    }
                    //else
                    //    logger.LogWarning("[{i}] disregard?: {str}", i, str[..^1]);
                    continue;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(game.Gameid) &&
                str.StartsWith(game.Gameid, StringComparison.Ordinal))
            {
                game.Gameid = str;
                //logger.LogDebug("[{i}] gameidext: {str}", i, game.Gameid);
                continue;
            }

            var matches = DateRegex().Matches(str);
            if (matches.Count > 0)
            {
                game.ApploaderDate = matches[0].Value;
                //logger.LogDebug("[{i}] apploaderDate: {str}", i, game.ApploaderDate);
                done = true;
                gameList.Add(game);
                continue;
            }

            //logger.LogDebug("[{i}] unknown: {str}", i, str);
            prevStr = str;
        }

        return gameList;
    }

    /// <summary>
    ///     Returns a list of filenames of the ROMs in the Dolphin SOFTWARE directories.
    /// </summary>
    private List<string> GetROMFiles(string path, bool recurse)
    {
        List<string> roms = new();

        var search = SearchOption.TopDirectoryOnly;
        if (recurse)
            search = SearchOption.AllDirectories; 
        //logger.LogDebug("rompath: {path}", path);
        List<string> romFiles = new();
        if (_fileSystem.Directory.Exists(path))
        {
            romFiles = _fileSystem.Directory.GetFiles(path, "*.*", search).ToList();
            //logger.LogDebug("{romcount} ROMs found", romFiles.Count);
            foreach (var romFile in romFiles)
            {
                roms.Add(romFile);
            }
        }
        return roms;
    }

    /// <summary>
    ///     Retrieves absolute paths to the ROM directories as configured by Dolphin.
    /// </summary>
    private List<string> GetROMPaths(string userPath, out bool recurse)
    {
        List<string> romPaths = new();
        recurse = false;

        var lines = _fileSystem.File.ReadAllLines(_fileSystem.Path.Combine(userPath, "Config", "Dolphin.ini"));
        foreach (var line in lines)
        {
            if (line.StartsWith("ISOPath", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("ISOPaths", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains('=', StringComparison.Ordinal))
                    romPaths.Add(line[(line.IndexOf('=', StringComparison.Ordinal) + 1)..].Trim());
            }
            if (line.StartsWith("RecursiveISOPaths", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains('=', StringComparison.Ordinal) &&
                    line[..(line.IndexOf('=', StringComparison.Ordinal) + 1)].Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
                    recurse = true;

            }
        }
        return romPaths;
    }

    [GeneratedRegex("^[0-9]{4}/[0-9]{2}/[0-9]{2}")]
    private static partial Regex DateRegex();
}
